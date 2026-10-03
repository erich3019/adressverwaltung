# Adressverwaltung – Tutorial WebApp v1.0.0

Vollständige Adressverwaltungs-WebApp mit CRUD-Funktionalität.

## Tech-Stack
- **Frontend:** Next.js 14 (TypeScript, Tailwind CSS, App Router)
- **Backend:** ASP.NET Core 8 Web API mit OData v4
- **Datenbank:** PostgreSQL 16
- **ORM:** Entity Framework Core 8 (Npgsql)
- **Container:** Docker / Docker Compose

## Schnellstart mit Docker

```bash
# Alle drei Services bauen und starten
docker-compose up --build

# Im Hintergrund starten
docker-compose up --build -d
```

Nach dem Start:
- **Frontend:** http://localhost:3000
- **Backend API:** http://localhost:5000/odata/Adressen
- **Swagger UI:** http://localhost:5000/swagger

## Lokale Entwicklung (ohne Docker)

### Voraussetzungen
- Node.js 20 LTS
- .NET SDK 8
- PostgreSQL 16

### Backend starten
```bash
cd backend/AdressverwaltungApi
# appsettings.json anpassen (Datenbankpasswort)
dotnet run
# Backend läuft auf: http://localhost:5000
```

### Frontend starten
```bash
cd frontend
npm install
npm run dev
# Frontend läuft auf: http://localhost:3000
```

## OData-Endpunkte
| Methode | URL | Beschreibung |
|--------|-----|--------------|
| GET | /odata/Adressen | Alle Adressen |
| GET | /odata/Adressen(1) | Adresse ID=1 |
| GET | /odata/Adressen?$filter=Ort eq 'Bern' | Gefiltert |
| GET | /odata/Adressen?$orderby=Name | Sortiert |
| POST | /odata/Adressen | Neue Adresse |
| PATCH | /odata/Adressen(1) | Aktualisieren |
| DELETE | /odata/Adressen(1) | Löschen |

## Projektstruktur
```
adressverwaltung/
├── backend/AdressverwaltungApi/
│   ├── Controllers/AdressenController.cs
│   ├── Models/Adresse.cs
│   ├── Data/AdresseDbContext.cs
│   ├── Migrations/
│   ├── Program.cs
│   └── Dockerfile
├── frontend/
│   ├── app/
│   │   ├── page.tsx                     (Adressliste)
│   │   └── adressen/
│   │       ├── neu/page.tsx             (Erstellen)
│   │       └── [id]/bearbeiten/page.tsx (Bearbeiten)
│   ├── components/AdresseForm.tsx
│   ├── lib/api.ts
│   ├── types/adresse.ts
│   └── Dockerfile
├── .vscode/
├── docker-compose.yml
└── README.md
```
# adressverwaltung
