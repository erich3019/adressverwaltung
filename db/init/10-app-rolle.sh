#!/bin/sh
# ============================================================
# 10-app-rolle.sh
# Legt die Datenbankrolle an, mit der sich das Backend verbindet.
#
# Das Backend soll nicht als Superuser "postgres" arbeiten: Die Rolle
# "adressverwaltung_app" besitzt nur die Datenbank der Anwendung und kann weder
# andere Datenbanken lesen noch Rollen anlegen oder Dateien des Servers lesen.
#
# Läuft im Datenbank-Container:
#   - automatisch beim ersten Start mit leerem Volume (docker-entrypoint-initdb.d)
#   - für eine bestehende Datenbank über ./scripts/create_db_role.sh
# Das Skript lässt sich beliebig oft ausführen; es setzt dabei auch das Passwort neu.
# ============================================================
set -e

if [ -z "$APP_DB_PASSWORD" ]; then
    echo "APP_DB_PASSWORD ist nicht gesetzt." >&2
    exit 1
fi

# Das Passwort geht als psql-Variable hinein (:'pw' quotet sicher), nicht in den SQL-Text
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v pw="$APP_DB_PASSWORD" <<'SQL'
SELECT 'CREATE ROLE adressverwaltung_app LOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'adressverwaltung_app') \gexec

ALTER ROLE adressverwaltung_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD :'pw';

-- Eigentümer der Datenbank: darf im Schema "public" Tabellen anlegen (EnsureCreated)
SELECT format('ALTER DATABASE %I OWNER TO adressverwaltung_app', current_database()) \gexec

-- Bestehende Tabellen übergeben; ihre Sequenzen wechseln den Eigentümer mit
SELECT format('ALTER TABLE public.%I OWNER TO adressverwaltung_app', tablename)
FROM pg_tables WHERE schemaname = 'public' \gexec
SQL
