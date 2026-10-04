# Adressverwaltung – Tutorial WebApp v2.2.0

Adressverwaltungs-WebApp mit Login, CRUD für Adressen und Städte (PLZ-Verzeichnis), PLZ-Wertehilfe und E-Mail-Benachrichtigung bei neuen Adressen.

## Tech-Stack

- **Frontend:** Next.js 15 (TypeScript, Tailwind CSS, App Router), NextAuth.js
- **Backend:** ASP.NET Core 10 Web API mit OData v4, JWT-Bearer-Authentifizierung
- **Datenbank:** PostgreSQL 16
- **ORM:** Entity Framework Core 10 (Npgsql)
- **Reverse Proxy:** nginx (TLS-Terminierung, HTTP → HTTPS)
- **Container:** Docker / Docker Compose

## Architektur

![Systemarchitektur](documentation/Systemarchitektur.svg)

nginx verteilt die Anfragen nach Pfad-Präfix:

| Pfad | Ziel |
|------|------|
| `/odata/*`, `/auth/*`, `/settings` | Backend (`backend:8080`) |
| alle übrigen Pfade | Frontend (`frontend:3000`) |

### Ablauf: Login und neue Adresse erfassen

![Sequenzdiagramm](documentation/Sequenzdiagramm.svg)
\nJeder der 38 Schritte ist in der Schulungsunterlage `documentation/Schulungsunterlage_Sequenzdiagramm.pdf` erläutert.

Beim Login stellt das Backend ein JWT aus, das NextAuth im verschlüsselten Session-Cookie ablegt. Bei jedem API-Aufruf liest `apiFetch` das Token aus der Session und sendet es als `Authorization: Bearer` mit.

Die Quellen der Diagramme liegen in `documentation/Systemarchitektur.puml` und `documentation/Sequenzdiagramm.puml`. SVG neu erzeugen:

```bash
docker run --rm -v "$PWD/documentation:/data" plantuml/plantuml -tsvg "/data/*.puml"
```

## Schnellstart mit Docker

### 1. TLS-Zertifikat erzeugen (einmalig)

Die Zertifikate sind nicht im Repository enthalten. Für die lokale Entwicklung genügt ein selbstsigniertes Zertifikat:

```bash
mkdir -p nginx/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/ssl/key.pem -out nginx/ssl/cert.pem \
  -subj "/CN=localhost"
```

### 2. Geheimnisse anlegen

Passwörter und Schlüssel liegen in `env/*.env` und sind nicht im Repository enthalten. Vorlagen kopieren und die Platzhalter ersetzen:

```bash
for f in db backend frontend; do cp env/$f.env.example env/$f.env; done
openssl rand -hex 24      # Datenbankpasswort (in db.env und backend.env identisch eintragen)
openssl rand -base64 48   # je ein Wert für Jwt__Key und NEXTAUTH_SECRET
```

### 3. Services starten

```bash
# Alle vier Services (db, backend, frontend, nginx) bauen und starten
docker compose up --build -d

# Logs verfolgen
docker compose logs -f backend
```

Nach dem Start:

- **App:** https://localhost (Zertifikatswarnung des Browsers bestätigen; Port 80 leitet auf 443 um)
- **Backend direkt:** http://localhost:5000 (z.B. `/odata/Adressen`, erfordert Bearer-Token)
- **PostgreSQL:** `localhost:5432`, Datenbank `adressverwaltung`

Backend und PostgreSQL sind nur an `127.0.0.1` gebunden; aus dem Netzwerk ist ausschliesslich nginx erreichbar.

### 4. Anmelden

Beim ersten Start mit leerer `Users`-Tabelle wird ein Admin-Benutzer angelegt (`Seed:AdminEmail`, Standard `admin@example.com`). Das Passwort stammt aus `Seed__AdminPassword` in `env/backend.env`; fehlt der Wert, erzeugt das Backend ein Zufallspasswort und schreibt es einmalig ins Log (`docker compose logs backend`). Das Passwort lässt sich jederzeit neu setzen:

```bash
./scripts/reset_password.sh
```

Weitere Benutzer lassen sich mit `./scripts/create_user.sh` anlegen.

## Konfiguration

Die Konfiguration des Docker-Stacks steht in `docker-compose.yml`, Geheimnisse (Datenbankpasswort, `Jwt__Key`, `NEXTAUTH_SECRET`, SMTP- und Google-Zugangsdaten) in `env/*.env`:

| Variable | Service | Bedeutung |
|----------|---------|-----------|
| `ConnectionStrings__DefaultConnection` | backend | Verbindung zur Datenbank |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiresInHours` | backend | Signatur und Gültigkeit des Bearer-Tokens (8 h) |
| `SmtpSettings__*` | backend | SMTP-Server für E-Mail-Benachrichtigungen (optional) |
| `NEXTAUTH_URL`, `NEXTAUTH_SECRET` | frontend | NextAuth-Session |
| `INTERNAL_API_URL` | frontend | Backend-URL für serverseitige Aufrufe (`http://backend:8080`) |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | frontend | Google-Login (optional, leer = deaktiviert) |

`appsettings.json` enthält keine Geheimnisse. Das Backend startet nicht, wenn `Jwt:Key` fehlt, kürzer als 32 Zeichen oder ein Platzhalter ist.

Der Sicherheitsbericht mit allen Befunden und Korrekturen liegt unter `documentation/Sicherheitsbericht.pdf`.

## Funktionen

- **Adressen** (`/`): Liste, Erfassen, Bearbeiten, Löschen
- **Städte** (`/cities`): PLZ-Verzeichnis pflegen; dient als Wertehilfe im Adressformular (Vorschläge ab zwei Ziffern)
- **Einstellungen** (`/einstellungen`): E-Mail-Adresse für Benachrichtigungen bei neuen Adressen
- **Login** (`/login`): E-Mail und Passwort, optional Google
- **Audit-Felder**: Alle Datensätze führen `CreateDate`, `CreatedBy`, `ChangeDate`, `ChangedBy`, `DateFrom`, `DateTo`; das Backend setzt sie automatisch

### PLZ-Verzeichnis importieren

```bash
pip install psycopg2-binary
DB_PASS=<passwort> CSV_PATH=migration/AMTOVZ_CSV_LV95.csv python3 scripts/import_cities.py
```

## API

Alle Endpunkte ausser `/auth/login` erfordern den Header `Authorization: Bearer <token>`.

### Token beziehen

```bash
curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"<passwort>"}'
# → { "id": "...", "name": "...", "email": "...", "token": "<JWT>" }

curl -s http://localhost:5000/odata/Adressen -H "Authorization: Bearer <JWT>"
```

### Endpunkte

| Methode | URL | Beschreibung |
|---------|-----|--------------|
| POST | `/auth/login` | Anmelden, liefert JWT |
| POST | `/auth/register` | Benutzer registrieren (nur mit Bearer-Token) |
| GET | `/odata/Adressen` | Alle Adressen |
| GET | `/odata/Adressen(1)` | Adresse mit Id 1 |
| GET | `/odata/Adressen?$filter=ort eq 'Bern'` | Gefiltert |
| GET | `/odata/Adressen?$orderby=name` | Sortiert |
| POST | `/odata/Adressen` | Neue Adresse (löst Benachrichtigung aus) |
| PATCH | `/odata/Adressen(1)` | Aktualisieren |
| DELETE | `/odata/Adressen(1)` | Löschen |
| GET/POST/PATCH/DELETE | `/odata/Cities` | Städte, gleiche Operationen wie Adressen |
| GET | `/odata/Cities?$filter=startswith(postalCode,'80')&$top=10` | PLZ-Suche |
| GET / PUT | `/settings` | Einstellungen lesen / speichern |

Property-Namen sind in Payloads und OData-Abfragen camelCase (`ort`, `postalCode`). `$top` ist auf 100 begrenzt.

## Lokale Entwicklung (ohne Docker)

### Voraussetzungen

- Node.js 20 LTS
- .NET SDK 10
- PostgreSQL 16

### Backend starten

```bash
cd backend/AdressverwaltungApi
# Geheimnisse kommen aus der Umgebung, nicht aus appsettings.json
export Jwt__Key="$(openssl rand -base64 48)"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=adressverwaltung;Username=postgres;Password=<passwort>"
ASPNETCORE_ENVIRONMENT=Development dotnet run
# Backend:    http://localhost:5000
# Swagger UI: http://localhost:5000/swagger (nur in Development)
```

Das Datenbankschema wird beim Start mit `EnsureCreated()` angelegt. Modelländerungen gelangen damit nicht automatisch in eine bereits bestehende Datenbank.

### Backend-Tests

xUnit-Integrationstests für die OData-API. Die API läuft dabei im Speicher (`WebApplicationFactory`) gegen eine PostgreSQL-Datenbank, die pro Testlauf als Wegwerf-Container gestartet wird (Testcontainers). Voraussetzung: Docker läuft.

```bash
cd backend
dotnet test
```

### Frontend starten

```bash
cd frontend
npm ci           # installiert exakt die Versionen aus package-lock.json
npm run dev      # http://localhost:3000
npm run lint
```

Benötigt `frontend/.env.local` mit `NEXT_PUBLIC_API_URL`, `INTERNAL_API_URL`, `NEXTAUTH_URL` und `NEXTAUTH_SECRET`.

Client-Komponenten rufen die API über relative URLs auf (`/odata/...`) und sind darauf angewiesen, dass nginx diese ans Backend weiterleitet. Für Funktionen im Browser deshalb den Docker-Stack unter https://localhost verwenden.

## Projektstruktur

```
adressverwaltung/
├── backend/AdressverwaltungApi/
│   ├── Controllers/      ODataCrudController<T>, Adressen, Cities, Auth, Settings
│   ├── Models/           Adresse, City, User, Settings (alle: AuditableEntity)
│   ├── Data/             AdresseDbContext (inkl. Audit-Felder)
│   ├── Dtos/             Request-/Response-Typen für Auth und Settings
│   ├── Options/          Typisierte Konfiguration (JwtOptions, SmtpOptions)
│   ├── Services/         E-Mail-, Benachrichtigungs- und Token-Dienst
│   ├── DbSeeder.cs       Admin-Benutzer beim ersten Start
│   └── Program.cs
├── frontend/
│   ├── app/              Seiten: / (Adressliste), /adressen/…, /cities, /einstellungen, /login
│   ├── components/       AdresseForm, CityForm, ConfirmDialog, NavBar, Fehlermeldung, Ladeanzeige
│   ├── lib/              api.ts (apiFetch), auth.ts (NextAuth)
│   ├── types/
│   └── middleware.ts     Route-Schutz
├── nginx/                nginx.conf, ssl/ (nicht im Repository)
├── scripts/              create_user.sh, reset_password.sh, import_cities.py
├── migration/            PLZ-Verzeichnis (CSV)
├── documentation/        Änderungsprotokolle, Tutorials, Architekturdiagramm
└── docker-compose.yml
```

## Versionen

| Version | Inhalt |
|---------|--------|
| 2.2.0 | JWT-Authentifizierung im Backend, Bearer-Token aus der NextAuth-Session |
| 2.1.0 | Umsetzung der Clean-Code-Analyse (14 Befunde) |
| 2.0.0 | Login (NextAuth), Städte, Einstellungen mit E-Mail-Benachrichtigung, Audit-Felder |

Details stehen in den Änderungsprotokollen unter `documentation/`.
