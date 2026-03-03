# Proposal: Add Frontend Web Application

## Intent

Completar el sistema BookingHubAPI agregando una capa de presentación web que permita a los usuarios finales interactuar con el sistema de reservas de manera visual y funcional. Actualmente solo existe el backend API; los usuarios no tienen forma de acceder al sistema sin herramientas como Postman.

## Scope

### In Scope
- Aplicación web frontend (React/Next.js)
- Página de registro de usuarios (Owner y Customer)
- Página de login con JWT
- Dashboard para Owners (gestión de compañía, servicios, disponibilidad)
- Portal para Customers (ver servicios, reservar turnos, mis reservas)
- Diseño responsivo y estético
- Integración con APIs existentes del backend

### Out of Scope
- Backend adicional (ya existe y funciona)
- Mobile apps (iOS/Android)
- Notificaciones en tiempo real
- Pasarela de pagos

## Approach

Usar Next.js con TypeScript y Tailwind CSS por su madurez, soporte SSR/SSG, y ecosistema. La arquitectura será:

- **Autenticación**: JWT almacenado en cookies httpOnly, manejo de estado con React Context
- **Gestión de estado**: React Query para datos del servidor, Zustand para estado local UI
- **Componentes**: shadcn/ui como base de componentes accesibles
- **Estilos**: Tailwind CSS con diseño limpio y profesional

La aplicación se conectará a los endpoints existentes del API.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `frontend/` | New | Nueva aplicación Next.js |
| `src/BookingHubAPI.API/` | Modified | CORS configurado para permitir frontend |
| `docker-compose.yml` | Modified | Agregar servicio de frontend |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| CORS/Autenticación cross-origin | Medium | Configurar correctamente cookies y headers |
| Duplicación de validación | Low | Reusar DTOs del backend en frontend |
| Dependencia de API inestable | Medium | Mock data durante desarrollo |

## Rollback Plan

1. Revertir cambios en git
2. Eliminar directorio frontend/
3. Revertir cambios de CORS en API
4. Actualizar docker-compose.yml

## Dependencies

- Node.js 18+
- Backend API corriendo en puerto 5000
- API existente con endpoints de auth, companies, services, reservations

## Success Criteria

- [ ] Usuario puede registrarse como Owner o Customer
- [ ] Usuario puede hacer login y mantener sesión
- [ ] Owner puede ver y editar su compañía
- [ ] Owner puede crear y gestionar servicios
- [ ] Owner puede definir disponibilidad
- [ ] Customer puede ver servicios disponibles
- [ ] Customer puede crear reservas
- [ ] Customer puede ver y cancelar sus reservas
- [ ] Diseño responsivo funciona en móvil y desktop
- [ ] Aplicación corre en contenedor Docker
