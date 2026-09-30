# BookingHubAPI

🔗 **Demo en producción**: https://bookinghubapi-frontend-h0ui.onrender.com/

Plataforma de reservas de servicios entre empresas y clientes.

## Estructura del Proyecto

```
├── backend/                 # API REST en .NET 9
│   ├── src/
│   │   ├── BookingHubAPI.API          # Punto de entrada
│   │   ├── BookingHubAPI.Application  # Lógica de negocio
│   │   ├── BookingHubAPI.Domain       # Entidades y reglas de dominio
│   │   └── BookingHubAPI.Infrastructure # Datos y servicios externos
│   ├── tests/
│   │   ├── BookingHubAPI.UnitTests
│   │   └── BookingHubAPI.IntegrationTests
│   └── Dockerfile
│
├── frontend/                # Aplicación Next.js 16
│   ├── src/
│   │   ├── app/            # Páginas y rutas
│   │   ├── components/     # Componentes UI
│   │   ├── hooks/          # Custom hooks
│   │   ├── lib/            # Utilidades y configuración
│   │   └── stores/         # Estado global (Zustand)
│   └── Dockerfile
│
├── docs/                    # Colección de Postman
└── docker-compose.yml       # Orquestación de servicios
```

## Requisitos Previos

- Docker y Docker Compose
- .NET 9 SDK (para desarrollo local)
- Node.js 22+ y npm (para desarrollo local)

## Configuración

1. Copiar `.env.example` a `.env` y configurar las variables (docker-compose las lee de este archivo):

```bash
# Backend
JWT_SECRET_KEY=YourSecureKeyMin32Characters
DB_PASSWORD=YourSecurePassword
CORS_ALLOWED_ORIGINS=https://yourdomain.com

# Frontend
NEXT_PUBLIC_API_URL=https://api.yourdomain.com
```

Para desarrollo local sin Docker (`dotnet run`), ver la sección de user-secrets en
`backend/README.md` en su lugar - `.env`/`.env.example` solo aplican al flujo de docker-compose.

## Base de datos

La API no crea ni modifica el esquema al arrancar: se gestiona con migraciones de EF Core.
Antes del primer arranque (y en cada deploy con migraciones nuevas) hay que aplicarlas.
Los pasos para desarrollo local y el runbook de producción están en
[`backend/README.md` → Database migrations runbook](backend/README.md#database-migrations-runbook).

## Ejecución con Docker

```bash
# 1. Levantar PostgreSQL y aplicar las migraciones
docker-compose up -d postgres
cd backend
dotnet tool restore
dotnet ef database update --project src/BookingHubAPI.Infrastructure \
  --connection "Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=<DB_PASSWORD de tu .env>"
cd ..

# 2. Iniciar el resto de los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener servicios
docker-compose down
```

**Puertos:**
- Frontend: http://localhost:3000
- API: http://localhost:5000
- PostgreSQL: localhost:5432

## Ejecución Local

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/BookingHubAPI.API
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Tecnologías

### Backend
- .NET 9
- Entity Framework Core
- PostgreSQL (Npgsql)
- JWT Authentication
- FluentValidation
- AspNetCoreRateLimit

### Frontend
- Next.js 16
- React 19
- TypeScript
- Tailwind CSS
- Zustand (gestión de estado)
- React Query
- Axios

## Testing

```bash
# Frontend
cd frontend
npm test              # Tests unitarios
npm run test:coverage # Coverage

# Backend
cd backend
dotnet test          # Todos los tests
```

## Seguridad

- Autenticación JWT con tokens de acceso
- Contraseñas hasheadas con bcrypt
- Rate limiting integrado
- CORS configurado por entorno
- Errores con formato RFC 7807 (ProblemDetails), sin detalles internos

## API

La colección de Postman está en [`docs/BookingHubAPI.postman_collection.json`](docs/BookingHubAPI.postman_collection.json).

## Licencia

MIT