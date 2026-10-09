#!/usr/bin/env bash
# ============================================================
# reset_password.sh
# Setzt das Passwort für admin@example.com neu.
# Verwendet ASP.NET Core PasswordHasher V3 Format (PBKDF2-HMAC-SHA256).
#
# Voraussetzung: Docker-Container müssen laufen
#   docker compose up -d
#
# Verwendung:
#   chmod +x reset_password.sh
#   ./reset_password.sh
#   ./reset_password.sh MeinNeuesPasswort!
# ============================================================

set -e

# ── Konfiguration (aus docker-compose.yml) ───────────────────
DB_CONTAINER="adressverwaltung-db"
DB_USER="postgres"
DB_NAME="adressverwaltung"
TARGET_EMAIL="admin@example.com"

# ── Passwort einlesen ────────────────────────────────────────
if [ -n "$1" ]; then
    NEW_PASSWORD="$1"
else
    echo ""
    echo "=== Passwort-Reset für: $TARGET_EMAIL ==="
    echo ""
    read -rsp "Neues Passwort eingeben: " NEW_PASSWORD
    echo ""
    read -rsp "Passwort bestätigen:    " NEW_PASSWORD_CONFIRM
    echo ""

    if [ "$NEW_PASSWORD" != "$NEW_PASSWORD_CONFIRM" ]; then
        echo "❌ Fehler: Passwörter stimmen nicht überein."
        exit 1
    fi
fi

# Gilt auch für ein als Argument übergebenes Passwort
if [ ${#NEW_PASSWORD} -lt 8 ]; then
    echo "❌ Fehler: Passwort muss mindestens 8 Zeichen lang sein."
    exit 1
fi

# ── Hash generieren (Python, PBKDF2-HMAC-SHA256, ASP.NET Core V3) ──
echo ""
echo "⏳ Generiere Passwort-Hash..."

# Passwort über die Umgebung übergeben, nicht in den Python-Quelltext einsetzen
NEW_HASH=$(NEW_PASSWORD="$NEW_PASSWORD" python3 - <<'PYEOF'
import struct, os, hashlib, base64

password = os.environ["NEW_PASSWORD"]

# ASP.NET Core Identity PasswordHasher V3
salt       = os.urandom(16)
iterations = 100000
key        = hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), salt, iterations, dklen=32)

buf = bytearray()
buf.extend(struct.pack('>B', 0x01))         # Version V3
buf.extend(struct.pack('>I', 0x00000001))   # PRF = HMACSHA256
buf.extend(struct.pack('>I', iterations))   # Iterationen
buf.extend(struct.pack('>I', len(salt)))    # Salt-Länge (16)
buf.extend(salt)                             # Salt (16 Bytes)
buf.extend(key)                              # Hash (32 Bytes)

print(base64.b64encode(bytes(buf)).decode('utf-8'))
PYEOF
)

if [ -z "$NEW_HASH" ]; then
    echo "❌ Fehler: Hash-Generierung fehlgeschlagen."
    exit 1
fi

echo "✅ Hash generiert."

# ── Prüfen ob Container läuft ────────────────────────────────
if ! docker ps --format '{{.Names}}' | grep -q "^${DB_CONTAINER}$"; then
    echo ""
    echo "❌ Fehler: Container '$DB_CONTAINER' läuft nicht."
    echo "   Starte die App zuerst mit: docker compose up -d"
    exit 1
fi

# ── Prüfen ob Tabelle "Users" überhaupt existiert ───────────
echo "⏳ Prüfe Datenbankzustand..."

TABLE_EXISTS=$(docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tAc \
    "SELECT EXISTS (
        SELECT FROM information_schema.tables
        WHERE table_schema = 'public'
        AND table_name = 'Users'
     );")

if [ "$TABLE_EXISTS" != "t" ]; then
    echo "❌ Fehler: Die Tabelle 'Users' existiert noch nicht."
    echo ""
    echo "   Das Backend muss zuerst gestartet werden, damit die Migrations ausgeführt werden:"
    echo "   docker compose up -d"
    echo ""
    echo "   Danach etwa 10 Sekunden warten, dann dieses Script erneut ausführen."
    exit 1
fi

# ── Prüfen ob User existiert ─────────────────────────────────
# Werte als psql-Variablen übergeben (:'name' quotet sicher) statt sie ins SQL einzusetzen
USER_EXISTS=$(docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA \
    -v ON_ERROR_STOP=1 -v email="$TARGET_EMAIL" <<'SQL'
SELECT COUNT(*) FROM "Users" WHERE "Email" = :'email';
SQL
)

if [ "$USER_EXISTS" -eq 0 ]; then
    echo ""
    echo "❌ Fehler: Kein User mit E-Mail '$TARGET_EMAIL' gefunden."
    echo "   Starte die App einmal komplett, damit der Seed-User angelegt wird."
    exit 1
fi

# ── Passwort-Hash in DB schreiben ────────────────────────────
docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" \
    -v ON_ERROR_STOP=1 -v email="$TARGET_EMAIL" -v hash="$NEW_HASH" <<'SQL'
UPDATE "Users" SET "PasswordHash" = :'hash' WHERE "Email" = :'email';
SQL

echo ""
echo "✅ Passwort erfolgreich zurückgesetzt!"
echo ""
echo "   E-Mail  : $TARGET_EMAIL"
echo ""
echo "   Bitte jetzt im Browser einloggen:"
echo "   https://localhost/login"
echo ""
