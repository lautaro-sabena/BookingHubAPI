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
- **SQL Server** - Base de datos
- **JWT** - Autenticación
- **FluentValidation** - Validación
- **AspNetCoreRateLimit** - Rate limiting
- **AutoMapper** - Mapeo de objetos

## Configuración

### Variables de Entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Connection string SQL Server | `Server=localhost;Database=BookingHubDB;User Id=sa;Password=...` |
| `Jwt__SecretKey` | Clave JWT (mín. 32 caracteres) | `TuClaveSegura12345678901234567890` |
| `Jwt__Issuer` | Emisor del token | `BookingHubAPI` |
| `Jwt__Audience` | Audiencia del token | `BookingHubAPI` |
| `Jwt__ExpirationMinutes` | Expiración del token (default: 60) | `60` |
| `Cors__AllowedOrigins` | Origins permitidos (separados por coma) | `http://localhost:3000,https://tudominio.com` |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}"
  },
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "BookingHubAPI",
    "Audience": "BookingHubAPI",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": "${CORS_ALLOWED_ORIGINS}"
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