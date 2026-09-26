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
├── frontend/                # Aplicación Next.js 14
│   ├── src/
│   │   ├── app/            # Páginas y rutas
│   │   ├── components/     # Componentes UI
│   │   ├── hooks/          # Custom hooks
│   │   ├── lib/            # Utilidades y configuración
│   │   └── stores/         # Estado global (Zustand)
│   └── Dockerfile
│
├── docker-compose.yml       # Orquestación de servicios
└── openspec/                # Documentación de especificaciones
```

## Requisitos Previos

- Docker y Docker Compose
- .NET 9 SDK (para desarrollo local)
- Node.js 18+ y npm (para desarrollo local)

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

## Ejecución con Docker

```bash
# Iniciar todos los servicios
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
- Next.js 14
- React 18
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
- Headers de seguridad en producción

## Licencia

MIT