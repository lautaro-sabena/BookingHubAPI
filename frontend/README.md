# BookingHub Frontend

Interfaz de usuario para el sistema de reservas BookingHub, desarrollada con Next.js 14.

## Requisitos Previos

- Node.js 18+
- npm

## Instalación

```bash
cd frontend
npm install
```

## Desarrollo

```bash
npm run dev
```

Abre [http://localhost:3000](http://localhost:3000) en tu navegador.

## Variables de Entorno

Crear un archivo `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Docker

```bash
docker build -t bookinghub-frontend .
docker run -p 3000:3000 bookinghub-frontend
```

O con Docker Compose:

```bash
docker-compose up --build
```

## Características

- **Autenticación**: Registro y login (roles Owner/Customer)
- **Gestión de empresa**: Crear y editar perfil de empresa
- **Gestión de servicios**: Crear, editar y eliminar servicios
- **Disponibilidad**: Configurar horarios de atención
- **Reservas**: Sistema de reservas con calendario
- **Dashboard**: Vistas diferenciadas por rol
- **Diseño responsive**: Adaptable a dispositivos móviles

## Stack Tecnológico

- **Next.js 14** - Framework (App Router)
- **TypeScript** - Lenguaje
- **Tailwind CSS** - Estilos
- **React Query** - Fetching y caché de datos
- **Zustand** - Gestión de estado global
- **Axios** - Cliente HTTP
- **Radix UI** - Componentes accesibles
- **Zod** - Validación de esquemas
- **React Hook Form** - Formularios

## Scripts

```bash
npm run dev      # Desarrollo
npm run build    # Producción
npm run start    # Servidor de producción
npm run lint     # Linting
npm test         # Tests unitarios
npm run test:coverage  # Coverage
npm run test:ui       # UI de tests
```

## Estructura

```
frontend/
├── src/
│   ├── app/           # Páginas y rutas (App Router)
│   ├── components/    # Componentes reutilizables
│   │   └── ui/        # Componentes base (shadcn)
│   ├── hooks/         # Custom hooks
│   ├── lib/           # Utilidades y configuración
│   │   └── api.ts     # Configuración de axios
│   └── stores/        # Estado global (Zustand)
├── public/            # Assets estáticos
└── Dockerfile
```
