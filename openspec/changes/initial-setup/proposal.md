# Proposal: BookingHubAPI - Initial Project Setup

## Intent

Build a multi-tenant SaaS booking system backend that enables businesses (clinics, barbershops, studios, consultants) to manage services, availability, and reservations. This is a real-world system with production-quality patterns, not a tutorial CRUD.

## Scope

### In Scope
- Multi-tenant architecture with tenant isolation
- User authentication (register/login) with JWT
- Company management (businesses can sign up and configure)
- Service management (create/edit services)
- Availability management (define working hours)
- Reservation system with conflict validation
- Cancellation support
- Reservation status workflow (Pending, Confirmed, Cancelled, Completed)
- Mock notifications (logging)
- Real pagination and filtering
- Global error handling
- Docker Compose setup (API + SQL Server/PostgreSQL)
- Unit tests and integration tests
- Health checks
- Rate limiting

### Out of Scope
- Real email notifications (mock only)
- Payment processing
- Recurring reservations
- Calendar integrations (Google, Outlook)
- Mobile/Web frontend

## Approach

Use Clean Architecture with:
- **API Layer**: Controllers with JWT auth, FluentValidation
- **Application Layer**: Use cases, AutoMapper profiles, DTOs
- **Domain Layer**: Entities, value objects, domain events
- **Infrastructure Layer**: EF Core, repositories, notification mocks

Multi-tenant strategy: Row-level isolation using TenantId filter on all queries.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Domain/` | New | Entities: Company, Service, Customer, Reservation, User |
| `src/Application/` | New | Use cases, DTOs, Mappings, Interfaces |
| `src/Infrastructure/` | New | EF Core DbContext, Repositories, Auth |
| `src/API/` | New | Controllers, Middleware, Filters, Validation |
| `tests/` | New | Unit tests, Integration tests |
| `docker-compose.yml` | New | API + Database containerization |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Multi-tenant data isolation bugs | Medium | Add integration tests per tenant scenario |
| EF Core performance with filters | Medium | Add indexing, audit queries |
| Complex validation logic | Low | Use FluentValidation extensively |

## Rollback Plan

1. Revert git commit
2. Remove created database migrations
3. Delete generated files (restore clean state)
4. Docker: `docker-compose down -v` to remove volumes

## Dependencies

- .NET 8 SDK
- SQL Server or PostgreSQL (via Docker)
- xUnit, FluentAssertions, Moq (testing)

## Success Criteria

- [ ] API starts successfully with Docker Compose
- [ ] User can register and login, receive JWT token
- [ ] Company can create services with availability
- [ ] Customer can book a service without conflicts
- [ ] Customer can cancel a reservation
- [ ] All endpoints return proper pagination/filtering
- [ ] Unit tests cover core domain logic (>70% coverage)
- [ ] Integration tests verify API behavior
- [ ] Health check endpoint returns healthy status
- [ ] Rate limiting protects API from abuse
