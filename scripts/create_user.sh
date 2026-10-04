#!/usr/bin/env bash
# ============================================================
# create_user.sh
# Legt einen neuen Benutzer in der Users-Tabelle an.
# Verwendet ASP.NET Core PasswordHasher V3 (PBKDF2-HMAC-SHA256).
#
# Voraussetzung: Docker-Container müssen laufen
#   docker compose up -d
#
# Verwendung (interaktiv):
#   chmod +x create_user.sh
#   ./create_user.sh
#
# Verwendung (mit Argumenten):
#   ./create_user.sh email@example.com "Max Muster" MeinPasswort!
# ============================================================

set -e

# ── Konfiguration (aus docker-compose.yml) ───────────────────
DB_CONTAINER="adressverwaltung-db"
DB_USER="postgres"
DB_NAME="adressverwaltung"

# ── Argumente oder interaktive Eingabe ───────────────────────
if [ -n "$1" ] && [ -n "$2" ] && [ -n "$3" ]; then
    NEW_EMAIL="$1"
    DISPLAY_NAME="$2"
    NEW_PASSWORD="$3"
else
    echo ""
    echo "=== Neuen Benutzer anlegen ==="
    echo ""
    read -rp  "E-Mail-Adresse:  " NEW_EMAIL
    read -rp  "Anzeigename:     " DISPLAY_NAME
    read -rsp "Passwort:        " NEW_PASSWORD
    echo ""
    read -rsp "Passwort bestätigen: " NEW_PASSWORD_CONFIRM
    echo ""

    if [ "$NEW_PASSWORD" != "$NEW_PASSWORD_CONFIRM" ]; then
        echo "❌ Fehler: Passwörter stimmen nicht überein."
        exit 1
    fi
fi

# ── Eingabe-Validierung ──────────────────────────────────────
if [ -z "$NEW_EMAIL" ]; then
    echo "❌ Fehler: E-Mail darf nicht leer sein."
    exit 1
fi

if [ -z "$DISPLAY_NAME" ]; then
    echo "❌ Fehler: Anzeigename darf nicht leer sein."
    exit 1
fi

if [ ${#NEW_PASSWORD} -lt 8 ]; then
    echo "❌ Fehler: Passwort muss mindestens 8 Zeichen lang sein."
    exit 1
fi

# E-Mail-Format prüfen (einfache Prüfung)
if ! echo "$NEW_EMAIL" | grep -qE '^[^@]+@[^@]+\.[^@]+$'; then
    echo "❌ Fehler: Ungültiges E-Mail-Format: $NEW_EMAIL"
    exit 1
fi

# ── Prüfen ob Container läuft ────────────────────────────────
if ! docker ps --format '{{.Names}}' | grep -q "^${DB_CONTAINER}$"; then
    echo ""
    echo "❌ Fehler: Container '$DB_CONTAINER' läuft nicht."
    echo "   Starte die App zuerst mit: docker compose up -d"
    exit 1
fi

# ── Prüfen ob Tabelle "Users" überhaupt existiert ───────────
echo ""
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

# ── Prüfen ob E-Mail bereits existiert ──────────────────────
echo "⏳ Prüfe ob E-Mail bereits vergeben ist..."

# Werte als psql-Variablen übergeben (:'name' quotet sicher) statt sie ins SQL einzusetzen
EMAIL_EXISTS=$(docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA \
    -v ON_ERROR_STOP=1 -v email="$NEW_EMAIL" <<'SQL'
SELECT COUNT(*) FROM "Users" WHERE "Email" = :'email';
SQL
)

if [ "$EMAIL_EXISTS" -gt 0 ]; then
    echo "❌ Fehler: Ein Benutzer mit der E-Mail '$NEW_EMAIL' existiert bereits."
    echo "   Für ein Passwort-Reset: ./reset_password.sh"
    exit 1
fi

# ── Hash generieren (Python, PBKDF2-HMAC-SHA256, ASP.NET Core V3) ──
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

# ── Audit-Felder vorbereiten ─────────────────────────────────
NOW_UTC=$(date -u +"%Y-%m-%d %H:%M:%S")
TODAY=$(date +"%Y-%m-%d")
CREATED_BY="script/create_user.sh"

# ── User in DB einfügen ──────────────────────────────────────
echo "⏳ Benutzer wird in der Datenbank angelegt..."

docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" \
    -v ON_ERROR_STOP=1 \
    -v email="$NEW_EMAIL" -v hash="$NEW_HASH" -v name="$DISPLAY_NAME" \
    -v now="$NOW_UTC" -v by="$CREATED_BY" -v today="$TODAY" <<'SQL'
INSERT INTO "Users"
    ("Email", "PasswordHash", "DisplayName",
     "CreateDate", "CreatedBy",
     "ChangeDate", "ChangedBy",
     "DateFrom", "DateTo")
 VALUES
    (:'email', :'hash', :'name',
     :'now', :'by',
     NULL, NULL,
     :'today', NULL);
SQL

echo ""
echo "✅ Benutzer erfolgreich angelegt!"
echo ""
echo "   E-Mail      : $NEW_EMAIL"
echo "   Anzeigename : $DISPLAY_NAME"
echo "   Erstellt am : $NOW_UTC UTC"
echo "   Gültig ab   : $TODAY"
echo ""
echo "   Login unter: https://localhost/login"
echo ""
