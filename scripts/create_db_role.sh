#!/usr/bin/env bash
# ============================================================
# create_db_role.sh
# Richtet in einer BESTEHENDEN Datenbank die Rolle "adressverwaltung_app" ein,
# mit der sich das Backend verbindet, und übergibt ihr die vorhandenen Tabellen.
# Bei einer neuen Datenbank (leeres Volume) geschieht das beim ersten Start von selbst.
#
# Voraussetzung: APP_DB_PASSWORD steht in .env und der Datenbank-Container läuft
#   docker compose up -d db
#
# Verwendung:
#   ./scripts/create_db_role.sh
#   docker compose up -d backend     # Backend mit der neuen Verbindung starten
# ============================================================

set -e

DB_CONTAINER="adressverwaltung-db"

if ! docker ps --format '{{.Names}}' | grep -q "^${DB_CONTAINER}$"; then
    echo "❌ Fehler: Container '$DB_CONTAINER' läuft nicht."
    echo "   Starte ihn zuerst mit: docker compose up -d db"
    exit 1
fi

# Das Skript liegt im Container (siehe docker-compose.yml) und liest APP_DB_PASSWORD
# aus dessen Umgebung – das Passwort erscheint hier in keiner Befehlszeile.
docker exec "$DB_CONTAINER" sh /docker-entrypoint-initdb.d/10-app-rolle.sh

echo ""
echo "✅ Rolle 'adressverwaltung_app' eingerichtet."
echo ""
