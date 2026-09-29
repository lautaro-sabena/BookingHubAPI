# Backend - BookingHubAPI

API REST desarrollada en .NET 9 para el sistema de reservas.

## Estructura

```
backend/
├── src/
│   ├── BookingHubAPI.API/           # Controllers, Middleware, Program
│   ├── BookingHubAPI.Application/   # DTOs, Mapeos, Interfaces de servicios
│   ├── BookingHubAPI.Domain/        # Entidades, Interfaces de repositorios
│   └── BookingHubAPI.Infrastructure/# Repositorios, Servicios externos, Auth
├── tests/
│   ├── BookingHubAPI.UnitTests/
│   └── BookingHubAPI.IntegrationTests/
└── Dockerfile
```

## Tecnologías

- **.NET 9** - Framework
- **Entity Framework Core 9** - ORM
- **PostgreSQL** (Npgsql) - Base de datos
- **JWT** - Autenticación
- **FluentValidation** - Validación
- **AspNetCoreRateLimit** - Rate limiting

## Configuración

### Variables de Entorno

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Npgsql (PostgreSQL) connection string | `Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=...` |
| `Jwt__SecretKey` | JWT signing key (min. 32 characters) - required, no default | `YourSecureKey1234567890123456789012` |
| `Jwt__Issuer` | Token issuer (default: `BookingHubAPI`) | `BookingHubAPI` |
| `Jwt__Audience` | Token audience (default: `BookingHubAPI`) | `BookingHubAPI` |
| `Jwt__ExpirationMinutes` | Token expiration in minutes (default: 60) | `60` |
| `Cors__AllowedOrigins` | Allowed origins: a comma-separated list in a single variable, or an indexed array (`Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...) | `https://yourdomain.com,https://admin.yourdomain.com` |

Both forms are resolved by
`backend/src/BookingHubAPI.API/Configuration/CorsOriginsResolver.cs`. When both are present at
once (e.g. an appsettings*.json array layered under a flat `Cors__AllowedOrigins` environment
variable, as docker-compose and Render do), the flat scalar wins over the JSON array - an
explicitly set environment value always takes precedence.

### Development secrets (user-secrets)

`appsettings.Development.json` no longer contains `Jwt:SecretKey` or a connection string -
both are required, and the app fails fast at startup (rejecting a missing, empty, or
whitespace-only value) with an explicit message naming the missing key. Configure them locally
with
[.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
(loaded automatically when `ASPNETCORE_ENVIRONMENT=Development`), run from
`backend/src/BookingHubAPI.API`:

```bash
dotnet user-secrets set "Jwt:SecretKey" "<a random value of at least 32 characters>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=<your-local-postgres-password>"
```

If you run the backend through `docker-compose` (from the repo root) instead of `dotnet run`,
this step isn't needed: compose reads `JWT_SECRET_KEY` and `DB_PASSWORD` from a `.env` file
(see the root `.env.example`) and injects them as container environment variables.

**Secrets previously committed in this file (`Password123!`,
`DevSecretKey1234567890123456789012`) must be treated as compromised** and must not be
reused in any real environment.

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=..."
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "BookingHubAPI",
    "Audience": "BookingHubAPI",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

## Desarrollo Local

```bash
# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run --project src/BookingHubAPI.API

# La base de datos se migra aparte (ver "Database migrations runbook")

# Con perfiles específicos
dotnet run --project src/BookingHubAPI.API --configuration Debug
```

## Database migrations runbook

The API **never creates or changes the schema at startup** (`EnsureCreated()`/`Migrate()` are gone).
The schema is managed with EF Core migrations in `src/BookingHubAPI.Infrastructure/Data/Migrations`
and applied by hand, with a backup, before deploying the matching API version.

| Migration | What it does |
|-----------|--------------|
| `Baseline` | The whole current schema (replaces the stale `InitialCreate`). Never runs on production, which already has these tables: it is only recorded in the history (step 3). |
| `ConvertReservationTimesToUtc` | One-time data fix: reservations held company-local wall-clock times labelled UTC; they become real UTC instants. Companies in `UTC` are skipped. It detects the real column type (`timestamp with time zone` or `timestamp without time zone`) and only runs the matching conversion. Runs once (tracked in the history), never twice. |
| `NormalizeUserEmails` | Lower-cases `Users.Email`. **Fails without changing anything** if two e-mails differ only by case, and prints them. The existing unique index `IX_Users_Email` then guarantees case-insensitive uniqueness because the app always stores lower-case. |

### Startup schema check

On startup the API compares the database with the migrations in the build (`GetPendingMigrationsAsync`; skipped for
non-relational providers such as the EF InMemory used by the integration tests). If any are pending it logs a critical error
naming them, pointing here, and **refuses to start**. Setting `Database:FailOnPendingMigrations`
(env `Database__FailOnPendingMigrations`) controls it: default `true` outside Development, `false` in Development, where it
only logs a warning. An unreachable database also fails startup when the check is on. Migrate first, then deploy the API.

### Tooling

`dotnet-ef` is pinned in `.config/dotnet-tools.json` (same version as EF Core). From `backend/`:

```bash
dotnet tool restore
dotnet ef migrations list   --project src/BookingHubAPI.Infrastructure       # names only; a "database access" warning is expected offline
dotnet ef migrations add <Name> --project src/BookingHubAPI.Infrastructure   # after changing the model
dotnet ef migrations script --idempotent --project src/BookingHubAPI.Infrastructure -o migrate.sql
```

`BookingDbContextFactory` provides the design-time context with a placeholder connection string:
generating migrations and scripts needs no secrets and never connects. Always use `--project src/BookingHubAPI.Infrastructure`.
A unit test (`ModelSnapshotTests`) fails when the model changed without a new migration.

### Timestamp columns

`Program.cs` keeps `Npgsql.EnableLegacyTimestampBehavior`. With it, EF maps every `DateTime` to
**`timestamp without time zone`**, which is what `EnsureCreated()` created in production (T8b stores UTC
instants in those columns and relies on this mapping). The `Baseline` therefore declares
`timestamp without time zone` everywhere; the old `InitialCreate` declared `timestamp with time zone` and also lacked the
`Favorites` table, so it was never usable against production. The design-time factory sets the same switch, so generated
migrations match the runtime. Removing the switch would need a separate data-and-code migration to `timestamptz`; it is not part of this change.

### Production: first deploy of the migrations

Production already has the tables (created by `EnsureCreated()`) but no `__EFMigrationsHistory`. Do this in a maintenance
window, with the **old API stopped** (a running old API would keep writing local wall-clock times while the conversion runs).
Use `psql -v ON_ERROR_STOP=1` for every script below.

1. **Back up**: `pg_dump --format=custom --file=bookinghub-$(date +%F).dump "<connection string>"`.
   Try restoring it into a scratch database at least once before relying on it.
2. **Diagnose** (read-only):

   ```sql
   -- Real column types. Expected for a database created by EnsureCreated(): "timestamp without time zone".
   SELECT table_name, column_name, data_type FROM information_schema.columns
   WHERE table_schema = current_schema() AND data_type LIKE 'timestamp%' ORDER BY 1, 2;

   -- Are all six tables there? (EnsureCreated does not add tables that were introduced later, e.g. "Favorites".)
   SELECT t AS missing_table FROM unnest(ARRAY['Companies','Favorites','Reservations','Services','Users','WorkingHours']) t
   WHERE to_regclass(format('%I.%I', current_schema(), t)) IS NULL;

   -- E-mails that differ only by case. Must return 0 rows; otherwise resolve them first (merge/rename accounts).
   SELECT lower("Email") AS email, count(*) AS accounts, array_agg("Id") AS ids
   FROM "Users" GROUP BY lower("Email") HAVING count(*) > 1;

   -- Non-ASCII e-mails. NormalizeUserEmails uses SQL lower(), the app uses .NET ToLowerInvariant(); they can differ for
   -- non-ASCII letters (depends on the database collation). Review each row: after migrating, the app must find the account
   -- by lower-casing the typed e-mail, so the stored value must equal ToLowerInvariant(email). Fix mismatches by hand.
   SELECT "Id", "Email" FROM "Users" WHERE "Email" ~ '[^\x01-\x7F]';

   -- Time zones stored, and how many reservations will be converted. Each must be a valid IANA id known to PostgreSQL.
   SELECT c."TimeZone", count(r."Id") AS reservations, EXISTS (SELECT 1 FROM pg_timezone_names WHERE name = c."TimeZone") AS known_to_postgres
   FROM "Companies" c LEFT JOIN "Reservations" r ON r."CompanyId" = c."Id" GROUP BY c."TimeZone" ORDER BY 2 DESC;

   -- Server time zone (should be UTC) and whether history already exists.
   SHOW timezone;
   SELECT to_regclass('"__EFMigrationsHistory"');
   ```

   Also spot-check reservations that fall in a DST gap or overlap of their company zone; they need manual review after the conversion.
3. **Baseline the history** (safe to run any number of times: it refuses if a table is missing and inserts the row only once):

   ```sql
   DO $bootstrap$
   DECLARE
       missing text;
   BEGIN
       SELECT string_agg(t, ', ') INTO missing
       FROM unnest(ARRAY['Companies', 'Favorites', 'Reservations', 'Services', 'Users', 'WorkingHours']) AS t
       WHERE to_regclass(format('%I.%I', current_schema(), t)) IS NULL;

       IF missing IS NOT NULL THEN
           RAISE EXCEPTION 'Cannot baseline: missing tables: %. Create them first (see "Missing tables").', missing;
       END IF;

       CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
           "MigrationId" character varying(150) NOT NULL,
           "ProductVersion" character varying(32) NOT NULL,
           CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
       );

       INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
       VALUES ('20260929224051_Baseline', '9.0.0')
       ON CONFLICT ("MigrationId") DO NOTHING;
   END
   $bootstrap$;
   ```

   *Missing tables*: if only `Favorites` is missing, create it, then repeat this step:

   ```sql
   CREATE TABLE "Favorites" (
       "Id" uuid NOT NULL,
       "CustomerId" uuid NOT NULL,
       "ServiceId" uuid NOT NULL,
       "CreatedAt" timestamp without time zone NOT NULL,
       "UpdatedAt" timestamp without time zone,
       CONSTRAINT "PK_Favorites" PRIMARY KEY ("Id"),
       CONSTRAINT "FK_Favorites_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE,
       CONSTRAINT "FK_Favorites_Users_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Users" ("Id") ON DELETE CASCADE
   );
   CREATE UNIQUE INDEX "IX_Favorites_CustomerId_ServiceId" ON "Favorites" ("CustomerId", "ServiceId");
   CREATE INDEX "IX_Favorites_ServiceId" ON "Favorites" ("ServiceId");
   ```

   For any other missing table, take its `CREATE TABLE`/`CREATE INDEX` statements from `dotnet ef migrations script 0 Baseline`.
4. **Generate and review** the script (`dotnet ef migrations script --idempotent ... -o migrate.sql`, see Tooling). Read it: the
   `Baseline` part is skipped because step 3 recorded it; the two data migrations are the parts that touch data.
5. **Run it** in one transaction (the script has its own `START TRANSACTION`/`COMMIT`; an error aborts everything):
   `psql -v ON_ERROR_STOP=1 -f migrate.sql "<connection string>"`. Re-running is harmless: applied migrations are skipped.
   If `NormalizeUserEmails` reports colliding e-mails, nothing was changed (not even the reservation conversion); fix the accounts and run again.
6. **Verify**: `SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;` lists the three migrations; spot-check a few
   reservations in a known-time-zone company (e.g. a 09:00 Buenos Aires booking is now stored as 12:00).
7. **Deploy the new API** and start it. Only after this step is it safe to accept traffic again.

Column-type handling: if step 2 shows `timestamp with time zone` instead of `timestamp without time zone`, nothing extra is needed
for this deploy: `ConvertReservationTimesToUtc` converts correctly for both types, and the app works with either as long as the
server time zone is UTC. The schema then differs from the `Baseline` only in those column types (EF does not compare types at runtime).

**Rollback**: restore the backup taken in step 1 into the database (the script is transactional, so a failed run leaves the
database as it was; only a run that completed needs a restore), then redeploy the previous API version.
`ConvertReservationTimesToUtc` has a `Down` for `dotnet ef database update`, but the backup is the supported rollback;
`NormalizeUserEmails` cannot restore the original casing.

### Later deploys

Repeat steps 1, 4, 5, 7 (skip the diagnostics and the baseline). Never edit an applied migration; add a new one.

### Local development

```bash
cd backend
dotnet tool restore
dotnet ef database update --project src/BookingHubAPI.Infrastructure \
  --connection "Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=<your-local-postgres-password>"
```

With `docker-compose` (PostgreSQL is published on `127.0.0.1:5432`), start `postgres` first
(`docker compose up -d postgres`), run the command above with the `DB_PASSWORD` from your `.env`, then start the rest.
The API does not create tables itself, so a fresh database must be migrated before the first request.
For a database that already exists from an older `EnsureCreated()` run, either drop and recreate it or follow the production steps above.

## Testing

```bash
# Todos los tests
dotnet test

# Tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Tests específicos
dotnet test --filter "FullyQualifiedName~UnitTests"
```

## Endpoints Principales

### Autenticación
- `POST /api/auth/register` - Registro de usuario
- `POST /api/auth/login` - Login

### Usuarios
- `GET /api/users/me` - Usuario actual
- `PUT /api/users/me` - Actualizar usuario

### Empresas
- `GET /api/companies` - Listar empresas
- `POST /api/companies` - Crear empresa (Owner)
- `GET /api/companies/{id}` - Ver empresa
- `PUT /api/companies/{id}` - Actualizar empresa

### Servicios
- `GET /api/services` - Listar servicios
- `POST /api/services` - Crear servicio (Owner)
- `GET /api/services/{id}` - Ver servicio
- `PUT /api/services/{id}` - Actualizar servicio
- `DELETE /api/services/{id}` - Eliminar servicio

### Reservas
- `GET /api/reservations` - Listar reservas
- `POST /api/reservations` - Crear reserva
- `GET /api/reservations/{id}` - Ver reserva
- `PUT /api/reservations/{id}/cancel` - Cancelar reserva

### Disponibilidad
- `GET /api/availability/{serviceId}` - Ver disponibilidad
- `POST /api/availability` - Configurar disponibilidad (Owner)

## Respuestas de error

Todos los errores (400, 401, 403, 404, 409, 500) usan RFC 7807 (`application/problem+json`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Time slot is not available",
  "traceId": "00-..."
}
```

- `detail` lleva el mensaje para el usuario; los errores de validación del modelo añaden `errors` (campo → mensajes).
- Un token sin un id de usuario válido responde 401. Las excepciones no controladas responden 500 con un `detail` genérico; el mensaje y el stack solo se registran en logs.
- La paginación (`page` 1–1000000, `pageSize` 1–100) se valida en `/api/services`, `/api/services/all` y `/api/reservations`; fuera de rango devuelve 400 con `errors`.
- El frontend lee el mensaje con `getApiErrorMessage` (`frontend/src/lib/apiError.ts`).

## Salud

- `GET /health` - Health checks

## Seguridad

- **Rate Limiting**: 100 pedidos/minuto por IP
- **JWT**: Tokens con expiración configurable
- **CORS**: Orígenes configurables por entorno
- **Passwords**: Hasheados con bcrypt
- **HTTPS**: Redirección automática en producción

## Docker

```bash
docker build -t bookinghubapi-api:latest .
docker run -p 5000:8080 bookinghubapi-api:latest
```

El contenedor expone el puerto 8080 internamente (mapeado a 5000 en docker-compose).