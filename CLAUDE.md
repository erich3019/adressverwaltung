# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Adressverwaltung is a tutorial-style address management web app (current version 2.2.0): Next.js 15 frontend, ASP.NET Core 10 OData API, PostgreSQL 16, fronted by nginx. Code comments, identifiers in the domain layer, UI text and documentation are in German (Swiss spelling: `ss` instead of `ß`, e.g. `Strasse`) — keep to that when adding code.

`README.md` is outdated (describes v1.0.0 without auth, nginx, Cities or Settings). Trust the code and `documentation/Aenderungsprotokoll_v2.*.md` over the README.

## Commands

### Full stack (the normal way to run it)

```bash
docker compose up --build -d     # db, backend, frontend, nginx
docker compose logs -f backend
```

- App: https://localhost (self-signed cert expected in `nginx/ssl/`, not in git; port 80 redirects to 443)
- Backend directly: http://localhost:5000 (e.g. `/odata/Adressen`)
- Postgres: localhost:5432, database `adressverwaltung`
- The frontend container is not published on a host port; it is only reachable through nginx.
- Backend and Postgres are published on `127.0.0.1` only.
- Seeded login when the Users table is empty: `Seed:AdminEmail` (default `admin@example.com`) / `Seed:AdminPassword`. Without a configured password `DbSeeder` generates a random one and logs it once; there is no hardcoded default.

### Backend (`backend/AdressverwaltungApi`)

```bash
dotnet build
dotnet run                                        # http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development dotnet run     # also enables Swagger at /swagger
```

There is no `launchSettings.json`, so plain `dotnet run` starts in Production and Swagger is off. `appsettings.json` holds no secrets: `Jwt__Key` (at least 32 characters, startup fails otherwise) and a `ConnectionStrings__DefaultConnection` with the password must come from the environment.

### Frontend (`frontend`)

```bash
npm ci           # package-lock.json is tracked; the Dockerfile uses npm ci too
npm run dev      # http://localhost:3000
npm run build
npm run lint     # next lint (next/core-web-vitals)
```

Env vars come from `frontend/.env.local` (`NEXT_PUBLIC_API_URL`, `INTERNAL_API_URL`, `NEXTAUTH_URL`, `NEXTAUTH_SECRET`, optional `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET`).

### Tests

```bash
cd backend
dotnet test                                              # needs Docker running
dotnet test --filter "FullyQualifiedName~AdressenODataTests"
```

`backend/AdressverwaltungApi.Tests` holds xUnit integration tests for the OData API: `WebApplicationFactory<Program>` against a throwaway PostgreSQL container (Testcontainers), with `IEmailService` replaced by a fake. Tests share one factory (collection `Api`) and `ODataTestBase` empties the tables before each test. Both backend projects target net10.0. The frontend has no tests.

`backend/api-tests.http` holds manual REST Client requests (they predate auth, so they need an `Authorization: Bearer` header from `POST /auth/login` to work now).

### Helper scripts (`scripts/`)

All operate on the running `adressverwaltung-db` container via `docker exec`:

- `create_user.sh [email "Display Name" password]` — inserts a user directly into `Users`, generating an ASP.NET Core PasswordHasher V3 compatible hash in Python.
- `reset_password.sh [password]` — resets the password of `admin@example.com`.
- `import_cities.py` — imports the Swiss PLZ directory (`migration/AMTOVZ_CSV_LV95.csv`) into `Cities`; configured through `DB_*` / `CSV_PATH` env vars, needs `psycopg2`.

## Architecture

### Request routing

nginx (`nginx/nginx.conf`) terminates TLS and splits traffic by path prefix:

- `/odata/*`, `/auth/*`, `/settings` → backend (`backend:8080`)
- everything else → Next.js (`frontend:3000`)

Consequences:

- A new backend route prefix needs a matching `location` block in nginx, otherwise the browser's request lands on Next.js.
- Frontend page routes must not collide with those prefixes — which is why the settings page is `/einstellungen` while the API is `/settings`, and the login page is `/login` while NextAuth lives under `/api/auth` and the backend under `/auth`.

`frontend/lib/api.ts` picks its base URL by execution context: on the server (Server Components, NextAuth `authorize`) it uses `INTERNAL_API_URL` (`http://backend:8080` in Docker); in the browser it uses an empty base, i.e. same-origin relative URLs that rely on nginx. `next.config.mjs` defines no rewrites, so client-side API calls under bare `npm run dev` on :3000 have nothing to proxy them to the backend — browser-facing features need to be exercised through the nginx stack.

### Authentication (two JWT layers)

1. `AuthController` (`POST /auth/login`) verifies the password and issues a backend JWT (HS256, `Jwt:*` config, 8 h). `POST /auth/register` requires a valid JWT — every user has full access, so there is no anonymous self-registration.
2. NextAuth's Credentials provider (`frontend/lib/auth.ts`) calls `/auth/login` server-side and stores that backend JWT as `accessToken` inside its own encrypted session cookie; the `jwt`/`session` callbacks expose it as `session.accessToken` (typed in `types/next-auth.d.ts`).
3. `apiFetch` in `lib/api.ts` attaches it as `Authorization: Bearer …`, using `getServerSession(authOptions)` on the server and `getSession()` in the browser. All backend calls should go through `apiFetch`.
4. `frontend/middleware.ts` requires a session for every page except `/login` and `/api/auth`.

Session lifetime (NextAuth `maxAge`) and backend token lifetime (`Jwt:ExpiresInHours`) are both 8 h and should be changed together. Google sign-in is only registered when both Google env vars are set, and it yields no backend `accessToken`.

### Backend

- **OData CRUD**: `ODataCrudController<TEntity>` implements `Get`/`Get(key)`/`Post`/`Patch`/`Delete` with `[Authorize]` and `[EnableQuery]`. A concrete controller only supplies `Entities` and `EntityDisplayName` (see `CitiesController`); `AdressenController` additionally overrides the `OnCreatedAsync` hook to trigger a notification — don't copy `Post`. Exposing a new entity over OData takes: model, `DbSet` + configuration in `AdresseDbContext`, `modelBuilder.EntitySet<T>("Name")` in `Program.cs`, and a controller whose name matches the entity set.
- **Plain REST**: `AuthController` and `SettingsController` are ordinary `[ApiController]`s with DTOs in `Dtos/`. `Settings` is treated as a single row. Tokens are issued by `ITokenService` (`JwtTokenService`).
- **Configuration**: the `Jwt` and `SmtpSettings` sections are bound to `Options/JwtOptions.cs` and `Options/SmtpOptions.cs`; inject `IOptions<T>` instead of reading `IConfiguration` keys by string.
- **Entity configuration**: required/length constraints live as DataAnnotations on the models; `OnModelCreating` only adds table names, indexes and the audit columns.
- **Casing**: the EDM model uses `EnableLowerCamelCase()` and MVC JSON is camelCase, so payloads and OData query options use camelCase property names (`$filter=startswith(postalCode,'80')`). The `normalize*` functions in `lib/api.ts` still accept PascalCase as a fallback.
- **Audit fields**: every entity (`Adresse`, `City`, `User`, `Settings`) derives from `AuditableEntity` (`CreateDate`, `CreatedBy`, `ChangeDate`, `ChangedBy`, `DateFrom`, `DateTo`). `AdresseDbContext.SaveChanges[Async]` fills them automatically — user from the JWT name claim, otherwise `"system"` (request headers are deliberately not trusted) — and defaults `DateFrom` to today. Don't set them in controllers. Timestamps are stored as `timestamp without time zone`, so `DateTime` values must have `Kind=Unspecified` (Npgsql rejects UTC kinds for that column type). Scripts that write to the database with raw SQL bypass this and must populate the audit columns themselves, as `create_user.sh` and `import_cities.py` do.
- **Schema creation**: startup calls `db.Database.EnsureCreated()` followed by `DbSeeder.Seed`, not `Migrate()`. The files in `Migrations/` are not applied at runtime, and `EnsureCreated` does nothing once the database exists — a model change does not reach an existing database (e.g. the `postgres-data` volume) on its own.
- **Notifications**: `AdressenController.OnCreatedAsync` → `INotificationService` (`EmailNotificationService`) → `IEmailService` (SMTP, `SmtpSettings:*`). The recipient is `Settings.NotificationEmail`; failures are logged and swallowed so they never fail the API request.
- **OData limits**: `SetMaxTop(100)`; `Select`, `Filter`, `OrderBy`, `Count`, `Expand` are enabled.

### Frontend

- App Router with a mix of rendering modes: `app/cities/page.tsx` is a Server Component (`force-dynamic`), while `app/page.tsx` (address list) and the forms are Client Components. Because `lib/api.ts` is shared by both, anything added there must work in both contexts.
- `types/` mirrors the backend entities, with `auditable.ts` as the counterpart of `AuditableEntity`. `*Create` / `*Update` types are what gets sent to the API; derive them with `CreateOf<T>` / `UpdateOf<T>` from `auditable.ts`.
- New API functions in `lib/api.ts` go through `apiRequest` (throws on a non-OK status) with `jsonRequest` for bodies. Shared UI pieces: `components/formStyles.ts`, `Fehlermeldung`, `Ladeanzeige`, `ConfirmDialog` — no `alert()`/`confirm()`.
- The address list is the `/` page; there is no `/adressen` index route, only `/adressen/neu` and `/adressen/[id]/bearbeiten`.
- `AdresseForm` has a debounced PLZ autocomplete backed by `sucheStaedteNachPlz` (OData `startswith` on `Cities`) that fills PLZ and Ort.
- Domain-level names are German (`getAlleAdressen`, `erstelleAdresse`, `loescheCity`, `ladeDaten`, `fehler`), technical names English.
- Formatting per `.vscode/settings.json`: Prettier, 2 spaces, single quotes, semicolons.

### Code-review reference IDs

Comments such as `B-03`, `B-08`, `F-01`, `F-03` refer to findings in `documentation/CleanCode_Analyse.md` (B = backend, F = frontend), which explains the reasoning behind those refactorings. B-09 to B-13 and F-07 to F-12 are in its follow-up section at the end; `CleanCode_Analyse.pdf` predates that section.

## Repository notes

- Non-secret configuration for the Docker stack is inline in `docker-compose.yml`; secrets (`POSTGRES_PASSWORD`, `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `NEXTAUTH_SECRET`, SMTP and Google credentials) come from `env/db.env`, `env/backend.env` and `env/frontend.env` via `env_file`. The root `.env` holds variables for a different project and is not referenced by the compose file.
- `nginx/ssl/*.pem`, `env/*.env`, `frontend/.env.local` and `.env` are git-ignored. A fresh clone has to create them before the stack starts: a self-signed `cert.pem`/`key.pem` pair in `nginx/ssl/` (nginx mounts that directory) and the three env files from their tracked `env/*.env.example` templates.
- nginx rate-limits `/auth/` and `/api/auth/callback/credentials` (10 requests per minute per client IP) and sets the security headers; `documentation/Sicherheitsbericht.pdf` lists the security findings and what was done about them.
- `archive/*.zip` are tracked release bundles of earlier versions, not source.
