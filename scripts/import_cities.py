#!/usr/bin/env python3
"""
import_cities.py
================
Liest AMTOVZ_CSV_LV95.csv ein und importiert die Spalten
  PLZ4         -> PostalCode
  Ortschaftsname -> CityName
in die PostgreSQL-Tabelle "Cities" der Adressverwaltungs-Applikation.

Regeln:
  - Kein TRUNCATE — bestehende Einträge bleiben erhalten
  - Duplikate (gleiche PostalCode + CityName) werden übersprungen
  - Audit-Felder werden gesetzt: CreateDate, CreatedBy, DateFrom

Konfiguration via Umgebungsvariablen (oder direkte Anpassung der DEFAULTS):
  DB_HOST   Datenbankserver     (Standard: localhost)
  DB_PORT   Port                (Standard: 5432)
  DB_NAME   Datenbankname       (Standard: adressverwaltung)
  DB_USER   Datenbankbenutzer   (Standard: postgres)
  DB_PASS   Passwort            (Standard: POSTGRES_PASSWORD, sonst muss es gesetzt werden)
  CSV_PATH  Pfad zur CSV-Datei  (Standard: AMTOVZ_CSV_LV95.csv)
  CREATED_BY  Wert für CreatedBy (Standard: import_script)

Ausführung (Beispiel):
  DB_PASS=geheim CSV_PATH=/pfad/zur/AMTOVZ_CSV_LV95.csv python3 import_cities.py
"""

import csv
import os
import sys
from datetime import datetime, timezone

try:
    import psycopg2
    from psycopg2.extras import execute_values
except ImportError:
    print("Fehler: psycopg2 ist nicht installiert.")
    print("Bitte installieren mit:  pip install psycopg2-binary")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Konfiguration
# ---------------------------------------------------------------------------
DB_HOST    = os.getenv("DB_HOST",    "localhost")
DB_PORT    = int(os.getenv("DB_PORT", "5432"))
DB_NAME    = os.getenv("DB_NAME",    "adressverwaltung")
DB_USER    = os.getenv("DB_USER",    "postgres")
# DB_PASS oder, falls nicht gesetzt, POSTGRES_PASSWORD aus der .env des Projekts
DB_PASS    = os.getenv("DB_PASS") or os.getenv("POSTGRES_PASSWORD", "")
CSV_PATH   = os.getenv("CSV_PATH",   "AMTOVZ_CSV_LV95.csv")
CREATED_BY = os.getenv("CREATED_BY", "import_script")

# ---------------------------------------------------------------------------
# Schritt 1: CSV einlesen und deduplizieren
# ---------------------------------------------------------------------------
print(f"[1/4] Lese CSV-Datei: {CSV_PATH}")

if not os.path.isfile(CSV_PATH):
    print(f"Fehler: Datei nicht gefunden: {CSV_PATH}")
    sys.exit(1)

csv_rows: set[tuple[str, str]] = set()  # (PostalCode, CityName)

with open(CSV_PATH, encoding="utf-8-sig", newline="") as f:
    reader = csv.DictReader(f, delimiter=";")
    for row in reader:
        postal_code = row.get("PLZ4", "").strip()
        city_name   = row.get("Ortschaftsname", "").strip()
        if postal_code and city_name:
            csv_rows.add((postal_code, city_name))

print(f"    {len(csv_rows)} eindeutige (PLZ, Ortschaftsname)-Kombinationen gefunden.")

# ---------------------------------------------------------------------------
# Schritt 2: Datenbankverbindung herstellen
# ---------------------------------------------------------------------------
print(f"[2/4] Verbinde mit Datenbank {DB_USER}@{DB_HOST}:{DB_PORT}/{DB_NAME} …")

if not DB_PASS:
    print("Warnung: DB_PASS ist nicht gesetzt. Verbindung wird trotzdem versucht.")

try:
    conn = psycopg2.connect(
        host=DB_HOST,
        port=DB_PORT,
        dbname=DB_NAME,
        user=DB_USER,
        password=DB_PASS,
    )
    conn.autocommit = False
    cur = conn.cursor()
    print("    Verbindung erfolgreich.")
except psycopg2.OperationalError as e:
    print(f"Fehler beim Verbinden mit der Datenbank:\n  {e}")
    print("\nHinweise:")
    print("  - Ist Docker gestartet? (docker compose up -d)")
    print("  - Stimmt das Passwort? (DB_PASS=<passwort>)")
    print("  - Läuft PostgreSQL auf Port 5432?")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Schritt 3: Bestehende Einträge laden → Duplikate in Python filtern
# ---------------------------------------------------------------------------
print("[3/4] Lade bestehende Einträge aus der Datenbank …")

cur.execute('SELECT "PostalCode", "CityName" FROM "Cities"')
existing: set[tuple[str, str]] = set(cur.fetchall())
print(f"    {len(existing)} bestehende Einträge in der Tabelle.")

# Nur neue Einträge einfügen
new_rows = csv_rows - existing
skipped  = len(csv_rows) - len(new_rows)
print(f"    {skipped} Duplikate werden übersprungen.")
print(f"    {len(new_rows)} neue Einträge werden eingefügt.")

if not new_rows:
    print("\nKeine neuen Einträge — Import abgeschlossen, nichts geändert.")
    cur.close()
    conn.close()
    sys.exit(0)

# ---------------------------------------------------------------------------
# Schritt 4: INSERT mit Audit-Feldern
# ---------------------------------------------------------------------------
print("[4/4] Füge neue Einträge ein …")

now = datetime.now(timezone.utc)

# Tupel: (PostalCode, CityName, CreateDate, CreatedBy, DateFrom)
records = [
    (postal_code, city_name, now, CREATED_BY, now.date())
    for postal_code, city_name in sorted(new_rows)
]

insert_sql = """
    INSERT INTO "Cities"
        ("PostalCode", "CityName", "CreateDate", "CreatedBy", "DateFrom")
    VALUES %s
"""

try:
    execute_values(cur, insert_sql, records, page_size=500)
    conn.commit()
    print(f"    {len(records)} Einträge erfolgreich eingefügt. ✓")
except Exception as e:
    conn.rollback()
    print(f"Fehler beim Einfügen der Daten:\n  {e}")
    cur.close()
    conn.close()
    sys.exit(1)

cur.close()
conn.close()
print("\nImport erfolgreich abgeschlossen.")
