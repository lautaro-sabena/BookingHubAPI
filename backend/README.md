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
- **AutoMapper** - Mapeo de objetos

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

# Con perfiles específicos
dotnet run --project src/BookingHubAPI.API --configuration Debug
```

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