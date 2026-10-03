using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdressverwaltungApi.Migrations
{
    /// <summary>
    /// Migration: Fügt Audit-Felder und Gültigkeitszeitraum zu allen Entitäten hinzu.
    ///
    /// Neue Spalten (für alle Tabellen: Adressen, Cities, Users, Settings):
    ///   CreateDate  — Zeitstempel der Erstellung (UTC, NOT NULL)
    ///   CreatedBy   — Benutzername, der erstellt hat (NOT NULL, max. 100 Zeichen)
    ///   ChangeDate  — Zeitstempel der letzten Änderung (UTC, nullable)
    ///   ChangedBy   — Benutzername, der zuletzt geändert hat (nullable, max. 100 Zeichen)
    ///   DateFrom    — Gültig ab (date, NOT NULL)
    ///   DateTo      — Gültig bis (date, nullable; null = unbegrenzt)
    ///
    /// Bestehende Zeilen erhalten Standardwerte:
    ///   CreateDate  = NOW() UTC
    ///   CreatedBy   = 'migration'
    ///   DateFrom    = 2024-01-01
    /// </summary>
    public partial class AddAuditFields : Migration
    {
        // Tabellennamen als Konstanten für Übersichtlichkeit
        private static readonly string[] Tables = ["Adressen", "Cities", "Users", "Settings"];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                // ── CreateDate ────────────────────────────────────────────────
                migrationBuilder.AddColumn<DateTime>(
                    name: "CreateDate",
                    table: table,
                    type: "timestamp without time zone",
                    nullable: false,
                    defaultValueSql: "NOW() AT TIME ZONE 'UTC'");

                // ── CreatedBy ─────────────────────────────────────────────────
                migrationBuilder.AddColumn<string>(
                    name: "CreatedBy",
                    table: table,
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false,
                    defaultValue: "migration");

                // ── ChangeDate ────────────────────────────────────────────────
                migrationBuilder.AddColumn<DateTime>(
                    name: "ChangeDate",
                    table: table,
                    type: "timestamp without time zone",
                    nullable: true);

                // ── ChangedBy ─────────────────────────────────────────────────
                migrationBuilder.AddColumn<string>(
                    name: "ChangedBy",
                    table: table,
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true);

                // ── DateFrom ──────────────────────────────────────────────────
                migrationBuilder.AddColumn<DateOnly>(
                    name: "DateFrom",
                    table: table,
                    type: "date",
                    nullable: false,
                    defaultValue: new DateOnly(2024, 1, 1));

                // ── DateTo ────────────────────────────────────────────────────
                migrationBuilder.AddColumn<DateOnly>(
                    name: "DateTo",
                    table: table,
                    type: "date",
                    nullable: true);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "CreateDate",  table: table);
                migrationBuilder.DropColumn(name: "CreatedBy",   table: table);
                migrationBuilder.DropColumn(name: "ChangeDate",  table: table);
                migrationBuilder.DropColumn(name: "ChangedBy",   table: table);
                migrationBuilder.DropColumn(name: "DateFrom",    table: table);
                migrationBuilder.DropColumn(name: "DateTo",      table: table);
            }
        }
    }
}
