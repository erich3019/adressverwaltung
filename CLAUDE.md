# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Adressverwaltung is a tutorial-style address management web app (current version 2.5.0): Next.js 15 frontend, ASP.NET Core 10 OData API, PostgreSQL 16, fronted by nginx. Code comments, identifiers in the domain layer, UI text and documentation are in German (Swiss spelling: `ss` instead of `ß`, e.g. `Strasse`) — keep to that when adding code.

`documentation/Aenderungsprotokoll_v2.*.md` records what changed in each version; `documentation/Tutorial_WebApp_v2.5.html` is the teaching document and quotes the source verbatim, so a change to quoted code needs the tutorial (and its PDF) updated too.

## Commands

### Full stack (the normal way to run it)

```bash
docker compose up --build -d     # db, backend, frontend, nginx
docker compose logs -f backend
```

- App: https://localhost (cert expected in `nginx/ssl/`, not in git — issued with `mkcert` so browsers trust it; port 80 redirects to 443)
- Backend directly: http://localhost:5000 (e.g. `/odata/Adressen`)
- Postgres: localhost:5432, database `adressverwaltung`
- The frontend container is not published on a host port; it is only reachable through nginx.
- The backend connects as the non-superuser role `adressverwaltung_app` (`APP_DB_PASSWORD`). `db/init/10-app-rolle.sh` creates it on an empty volume; for an existing volume run `scripts/create_db_role.sh` once (also after changing the password). `postgres` stays for administration and the helper scripts.
- Backend and Postgres are published on `127.0.0.1` only.
- Seeded login when the Users table is empty: `Seed:AdminEmail` (default `admin@example.com`) / `Seed:AdminPassword`. Without a configured password `DbSeeder` generates a random one and logs it once; there is no hardcoded default.

### Backend (`backend/AdressverwaltungApi`)

```bash
dotnet build
dotnet run                                        # http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development dotnet run     # also enables Swagger at /swagger
```

There is no `launchSettings.json`, so plain `dotnet run` starts in Production and Swagger is off. `appsettings.json` holds no secrets: `Jwt__Key` (at least 32 characters, startup fails otherwise) and a `ConnectionStrings__DefaultConnection` with the password must come from the environment, e.g. derived from the root `.env` as shown in the README.

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

### Helper scripts (`scripts/`)

All operate on the running `adressverwaltung-db` container via `docker exec`:

- `create_user.sh [email "Display Name" password [User|Admin]]` — inserts a user directly into `Users`, generating an ASP.NET Core PasswordHasher V3 compatible hash in Python. The role defaults to `User`.
- `reset_password.sh [password]` — resets the password of `admin@example.com` and bumps `TokenVersion`, which revokes that user's tokens.
- `create_db_role.sh` — runs `db/init/10-app-rolle.sh` inside the container (see above).
- `import_cities.py` — imports the Swiss PLZ directory (`migration/AMTOVZ_CSV_LV95.csv`) into `Cities`; configured through `DB_*` / `CSV_PATH` env vars (password: `DB_PASS`, falling back to `POSTGRES_PASSWORD`), needs `psycopg2`.

## Architecture

### Request routing

nginx (`nginx/nginx.conf`) terminates TLS and splits traffic by path prefix:

- `/odata/*`, `/auth/*`, `/settings`, `/users` → backend (`backend:8080`)
- everything else → Next.js (`frontend:3000`)

Consequences:

- A new backend route prefix needs a matching `location` block in nginx, otherwise the browser's request lands on Next.js.
- Frontend page routes must not collide with those prefixes — which is why the settings page is `/einstellungen` while the API is `/settings`, the user management page is `/benutzer` while the API is `/users`, and the login page is `/login` while NextAuth lives under `/api/auth` and the backend under `/auth`.

`frontend/lib/api.ts` picks its base URL by execution context: on the server (Server Components) it calls the backend directly at `INTERNAL_API_URL` (`http://backend:8080` in Docker) with the bearer token; in the browser it calls the app's own proxy `/api/backend/*` (`app/api/backend/[...pfad]/route.ts`), which attaches the token server-side and forwards only `odata/…`, `settings` and `users`. The proxy is cookie-authenticated, so it requires the custom header from `lib/apiHeader.ts` and a same-host `Origin` as CSRF protection — a new backend area that the browser must reach has to be added to `ERLAUBTE_BEREICHE` there. The nginx `/odata/`, `/settings`, `/users` and `/auth/` locations remain for direct API clients (curl, Postman) with their own bearer token.

### Authentication (two JWT layers)

1. `AuthController` (`POST /auth/login`) verifies the password and issues a backend JWT (HS256, `Jwt:*` config, 8 h). `POST /auth/register` requires the `Admin` role (optional `role` in the body, default `User`), so there is no self-registration. `POST /auth/logout` increments the user's `TokenVersion`, which revokes all of that user's tokens. `ILoginThrottle` (`MemoryLoginThrottle`, in memory) blocks an e-mail address with 429 for 5 minutes after 3 failed logins (`MaxFailures`, `LockDuration`; the 5 minutes run from the third failure), whether or not the account exists. `authorize` in `lib/auth.ts` turns the 429 into the error `ANMELDUNG_GESPERRT` (`lib/anmeldung.ts`) so that the login form can show the lock; `SPERRE_MINUTEN` there only feeds UI texts and has to be changed together with `LockDuration`.
2. NextAuth's Credentials provider (`frontend/lib/auth.ts`) calls `/auth/login` server-side and stores that backend JWT as `accessToken` inside its own encrypted session cookie. The token never reaches the browser: the `session` callback of `authOptions` exposes only `user.id` and `user.role`; `serverAuthOptions` (same file) additionally sets `session.accessToken` and is meant only for `getServerSession` in Server Components — never pass it to the NextAuth route handler. NextAuth's `signOut` event calls `/auth/logout`.
3. `apiFetch` in `lib/api.ts` sends `Authorization: Bearer …` on the server (`getServerSession(serverAuthOptions)`) and goes through the `/api/backend` proxy in the browser; a 401 in the browser signs the user out. All backend calls should go through `apiFetch`.
4. `frontend/middleware.ts` requires a session that carries a backend `accessToken` for every page except `/login` and `/api/auth` (it uses `getToken` directly, not `withAuth`) and sets the Content-Security-Policy with a per-response nonce. `app/layout.tsx` awaits `headers()` so that every page renders dynamically — a statically rendered page would not get the nonce and its scripts would be blocked.

Session lifetime (NextAuth `maxAge`) and backend token lifetime (`Jwt:ExpiresInHours`) are both 8 h and should be changed together. Google sign-in is only registered when both Google env vars are set, and it yields no backend `accessToken` — the `signIn` callback in `lib/auth.ts` therefore rejects it (`/login?error=AccessDenied`). It stays unusable until the backend can issue a token for a Google identity.

### Backend

- **Authorization**: `Program.cs` sets a fallback policy, so every endpoint (including `/odata` and `/odata/$metadata`) requires a JWT unless it is marked `[AllowAnonymous]` — only `POST /auth/login` is. `TokenUserValidator` runs on every request (`OnTokenValidated`): it rejects the token if the user no longer exists or `User.TokenVersion` differs from the token's `tv` claim, and adds the role claim from the database (the token itself carries no role). Roles are `Roles.Admin` and `Roles.User`; `[Authorize(Roles = Roles.Admin)]` guards `POST /auth/register`, `PUT /settings` and the whole `UsersController`.
- **OData CRUD**: `ODataCrudController<TEntity>` implements `Get`/`Get(key)`/`Post`/`Patch`/`Delete` with `[Authorize]` and `[EnableQuery]`. `Post` discards a client-supplied key and `Patch` ignores it. List queries return at most `MaxPageSize` (100) rows with an `@odata.nextLink`; the frontend pages with `$top`/`$skip`/`$count` (`leseSeite` in `lib/api.ts`, 25 rows per page). A concrete controller only supplies `Entities` and `EntityDisplayName` (see `CitiesController`); `AdressenController` additionally overrides the `OnCreatedAsync` hook to trigger a notification — don't copy `Post`. Exposing a new entity over OData takes: model, `DbSet` + configuration in `AdresseDbContext`, `modelBuilder.EntitySet<T>("Name")` in `Program.cs`, and a controller whose name matches the entity set.
- **Plain REST**: `AuthController`, `SettingsController` and `UsersController` are ordinary `[ApiController]`s with DTOs in `Dtos/`. `UsersController` (`/users`, Admin only) lists, creates, updates (display name, role, optional password — a new password bumps `TokenVersion`) and deletes users and lifts a login lock (`POST /users/{id}/unlock`); responses carry `lockedUntil` (from `ILoginThrottle.BlockedUntil`) and `isSelf`, never the password hash. An admin can neither delete nor demote themselves, which is what keeps at least one admin; the e-mail address is not editable. `Settings` is treated as a single row holding `NotificationEmail` (empty = no notification) and `AccentColor` (one of `AccentColors.All`). `GET /settings` also returns `canEdit` (caller has the `Admin` role, read from the database) — the settings page uses that instead of the role in the NextAuth session, which dates from login and can be stale. Tokens are issued by `ITokenService` (`JwtTokenService`).
- **Configuration**: the `Jwt` and `SmtpSettings` sections are bound to `Options/JwtOptions.cs` and `Options/SmtpOptions.cs`; inject `IOptions<T>` instead of reading `IConfiguration` keys by string.
- **Entity configuration**: required/length constraints live as DataAnnotations on the models; `OnModelCreating` only adds table names, indexes and the audit columns.
- **Casing**: the EDM model uses `EnableLowerCamelCase()` and MVC JSON is camelCase, so payloads and OData query options use camelCase property names (`$filter=startswith(postalCode,'80')`). The `normalize*` functions in `lib/api.ts` still accept PascalCase as a fallback.
- **Audit fields**: every entity (`Adresse`, `City`, `User`, `Settings`) derives from `AuditableEntity` (`CreateDate`, `CreatedBy`, `ChangeDate`, `ChangedBy`, `DateFrom`, `DateTo`). `AdresseDbContext.SaveChanges[Async]` fills them automatically — user from the JWT name claim, otherwise `"system"` (request headers are deliberately not trusted) — and defaults `DateFrom` to today. Don't set them in controllers. Timestamps are stored as `timestamp without time zone`, so `DateTime` values must have `Kind=Unspecified` (Npgsql rejects UTC kinds for that column type). Scripts that write to the database with raw SQL bypass this and must populate the audit columns themselves, as `create_user.sh` and `import_cities.py` do.
- **Schema creation**: startup calls `db.Database.EnsureCreated()` followed by `DbSeeder.Seed`, not `Migrate()`. The files in `Migrations/` are not applied at runtime, and `EnsureCreated` does nothing once the database exists — a model change does not reach an existing database (e.g. the `postgres-data` volume) on its own. `SchemaUpgrader.Apply` runs right after it and adds missing columns with idempotent SQL (`Users.Role`, `Users.TokenVersion`, `Settings.AccentColor`); a new column on an existing table needs an entry there, plus a database default if scripts insert rows with raw SQL.
- **Notifications**: `AdressenController.OnCreatedAsync` → `INotificationService` (`EmailNotificationService`) → `IEmailService` (SMTP, `SmtpSettings:*`). The recipient is `Settings.NotificationEmail`. Creating a user (`POST /users` and `POST /auth/register`) calls `NotifyNewUserAsync`, which mails the new user at their own address — never with the password. Deleting a user (`DELETE /users/{id}`) calls `NotifyUserDeletedAsync`, which reports it to `Settings.NotificationEmail` (nothing is sent when that is empty). In all cases failures are logged and swallowed so they never fail the API request.
- **OData limits**: `SetMaxTop(100)`; `Select`, `Filter`, `OrderBy`, `Count`, `Expand` are enabled. `ODataErrorDetailFilter` strips exception type and stack trace from OData error responses.
- **Response headers**: every backend response gets `Cache-Control: no-store`; the Kestrel `Server` header is off.

### Frontend

- App Router with a mix of rendering modes: `app/cities/page.tsx` is a Server Component (`force-dynamic`; paging and search via `?seite=` / `?suche=`), while `app/page.tsx` (address list, paged in state) and the forms are Client Components. `components/Seitenwahl.tsx` renders the pager for both (links via `hrefFuer`, buttons via `onWechsel`). Because `lib/api.ts` is shared by both, anything added there must work in both contexts.
- `types/` mirrors the backend entities, with `auditable.ts` as the counterpart of `AuditableEntity`. `*Create` / `*Update` types are what gets sent to the API; derive them with `CreateOf<T>` / `UpdateOf<T>` from `auditable.ts`.
- New API functions in `lib/api.ts` go through `apiRequest` (throws an `ApiFehler` on a non-OK status, carrying `status` and the backend's `message` as `meldung`) with `jsonRequest` for bodies. The user management pages (`app/benutzer`, `components/BenutzerForm.tsx`) show `meldung` to the user and treat a 403 as "not an admin" — like the settings page they do not rely on the role in the session. Shared UI pieces: `components/formStyles.ts`, `Fehlermeldung`, `Ladeanzeige`, `ConfirmDialog` — no `alert()`/`confirm()`.
- The address list is the `/` page; there is no `/adressen` index route, only `/adressen/neu` and `/adressen/[id]/bearbeiten`. Saving the settings also returns to `/`.
- `AdresseForm` has a debounced PLZ autocomplete backed by `sucheStaedteNachPlz` (OData `startswith` on `Cities`) that fills PLZ and Ort.
- Domain-level names are German (`getAdressenSeite`, `erstelleAdresse`, `loescheCity`, `ladeDaten`, `fehler`), technical names English.
- Styling is Tailwind CSS 4: configuration lives in `app/globals.css` (`@import 'tailwindcss'`, no `tailwind.config.ts`), PostCSS uses `@tailwindcss/postcss`. Utility names follow v4 (`shadow-sm`, `rounded-sm`, `outline-hidden` instead of v3's `shadow`, `rounded`, `outline-none`).
- Accent colour: components use the `akzent-*` palette (`bg-akzent-600`), never `blue-*`. `app/globals.css` maps it to a Tailwind palette per `[data-farbe='…']` block; `app/layout.tsx` reads the colour server-side (`getAkzentfarbe`) and sets `data-farbe` on `<html>`. The login page has no session and therefore always shows the default blue. A new colour needs a CSS block, an entry in `lib/farben.ts` and one in `Models/AccentColors.cs`.
- Formatting per `.vscode/settings.json`: Prettier, 2 spaces, single quotes, semicolons.

### Code-review reference IDs

Comments such as `B-03`, `B-08`, `F-01`, `F-03` refer to findings in `documentation/CleanCode_Analyse.md` (B = backend, F = frontend), which explains the reasoning behind those refactorings. B-09 to B-13 and F-07 to F-12 are in its follow-up section at the end.

## Repository notes

- Non-secret configuration for the Docker stack is inline in `docker-compose.yml`; security-relevant values come from the root `.env` through compose interpolation: `POSTGRES_PASSWORD` (superuser), `APP_DB_PASSWORD` (inserted into the backend connection string), `JWT_KEY` → `Jwt__Key`, `NEXTAUTH_SECRET`, `SEED_ADMIN_PASSWORD` → `Seed__AdminPassword`, `SMTP_USERNAME`/`SMTP_PASSWORD`, `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET`. `POSTGRES_PASSWORD`, `APP_DB_PASSWORD`, `JWT_KEY` and `NEXTAUTH_SECRET` are required (`${VAR:?…}`), so compose refuses to start without them. A new secret goes into `.env`, `.env.example` and the compose file — never inline.
- `nginx/ssl/*.pem`, `frontend/.env.local` and `.env` are git-ignored. A fresh clone has to create them before the stack starts: a `cert.pem`/`key.pem` pair in `nginx/ssl/` (nginx mounts that directory; `mkcert -cert-file nginx/ssl/cert.pem -key-file nginx/ssl/key.pem localhost 127.0.0.1 ::1`, a plain self-signed pair also works but shows as not secure) and `.env` from the tracked `.env.example`.
- nginx rate-limits `/auth/` and `/api/auth/callback/credentials` (10 requests per minute per client IP) and sets the security headers. The CSP for pages comes from `frontend/middleware.ts` (nonce-based, `default-src 'self'`); nginx adds a fixed CSP only to responses that bring none (API, static files, error pages). A resource loaded from another origin (font, script, image, API) needs a matching entry in both places; `documentation/Sicherheitsbericht.pdf` lists the security findings and what was done about them.
- `archive/*.zip` are tracked release bundles of earlier versions, not source.
