# Design: BookingHubAPI - Initial Project Setup

## Technical Approach

Build a multi-tenant SaaS booking system using Clean Architecture with .NET 8, Entity Framework Core, and JWT authentication. The system will support companies managing services, availability, and reservations with row-level tenant isolation.

## Architecture Decisions

### Decision: Clean Architecture Layers

**Choice**: Domain → Application → Infrastructure → API layer structure
**Alternatives considered**: MVC-only, onions, hexagonal
**Rationale**: Clean separation enables testability, maintainability, and clear dependencies. Domain layer has no external dependencies. Each layer depends only on the one beneath it.

### Decision: Multi-tenant Isolation Strategy

**Choice**: Row-level filtering with TenantId on all queries
**Alternatives considered**: Database per tenant, schema per tenant
**Rationale**: Simpler operation, lower cost, sufficient isolation for this use case. EF Core global query filters handle the implementation.

### Decision: JWT Authentication

**Choice**: JWT Bearer tokens with role-based claims
**Alternatives considered**: Session-based auth, OAuth providers
**Rationale**: Stateless, scales horizontally, works well with APIs. Role claims enable authorization at controller level.

### Decision: Database

**Choice**: SQL Server via Docker Compose
**Alternatives considered**: PostgreSQL, SQLite
**Rationale**: Industry standard for .NET enterprise apps. Docker ensures consistent dev environment.

### Decision: Validation

**Choice**: FluentValidation for request validation
**Alternatives considered**: Data annotations only, manual validation
**Rationale**: Separation of concerns, testable, fluent API. Works well with MediatR pipeline.

### Decision: Object Mapping

**Choice**: AutoMapper
**Alternatives considered**: Manual mapping, Mapster
**Rationale**: Convention-based, reduces boilerplate, well-established in .NET ecosystem.

### Decision: Repository Pattern

**Choice**: Generic repository with EF Core
**Alternatives considered**: Direct DbContext usage, CQRS without repositories
**Rationale**: Abstracts data access, enables unit testing with in-memory data, follows existing .NET conventions.

## Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                            │
│  Controllers → FluentValidation → MediatR Handlers         │
└─────────────────────────┬───────────────────────────────────┘
                          │ DTOs
┌─────────────────────────▼───────────────────────────────────┐
│                    Application Layer                         │
│  Use Cases → AutoMapper → Domain Services                    │
└─────────────────────────┬───────────────────────────────────┘
                          │ Domain Entities
┌─────────────────────────▼───────────────────────────────────┐
│                      Domain Layer                            │
│  Entities → Value Objects → Domain Events                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                      │
│  EF Core DbContext → Repositories → SQL Server              │
│  JWT Provider → Notification Service (Mock)                │
└─────────────────────────────────────────────────────────────┘
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/BookingHubAPI.Domain/BookingHubAPI.Domain.csproj` | Create | Domain entities and interfaces |
| `src/BookingHubAPI.Domain/Entities/*.cs` | Create | User, Company, Service, Customer, Reservation entities |
| `src/BookingHubAPI.Domain/Interfaces/*Repository.cs` | Create | Repository interfaces |
| `src/BookingHubAPI.Application/BookingHubAPI.Application.csproj` | Create | Application layer |
| `src/BookingHubAPI.Application/DTOs/*.cs` | Create | Data transfer objects |
| `src/BookingHubAPI.Application/Mappings/*.cs` | Create | AutoMapper profiles |
| `src/BookingHubAPI.Application/Validators/*.cs` | Create | FluentValidation validators |
| `src/BookingHubAPI.Application/UseCases/*/*.cs` | Create | Use case handlers |
| `src/BookingHubAPI.Infrastructure/BookingHubAPI.Infrastructure.csproj` | Create | Infrastructure layer |
| `src/BookingHubAPI.Infrastructure/Data/*DbContext.cs` | Create | EF Core context, configurations |
| `src/BookingHubAPI.Infrastructure/Repositories/*.cs` | Create | Repository implementations |
| `src/BookingHubAPI.Infrastructure/Auth/*.cs` | Create | JWT service, token validation |
| `src/BookingHubAPI.Infrastructure/Services/*.cs` | Create | Notification mock service |
| `src/BookingHubAPI.API/BookingHubAPI.API.csproj` | Create | API layer |
| `src/BookingHubAPI.API/Program.cs` | Create | App entry, DI configuration |
| `src/BookingHubAPI.API/Controllers/*.cs` | Create | API controllers |
| `src/BookingHubAPI.API/Middleware/*.cs` | Create | Error handling, tenant filter |
| `src/BookingHubAPI.API/Filters/*.cs` | Create | Validation, auth filters |
| `tests/BookingHubAPI.UnitTests/BookingHubAPI.UnitTests.csproj` | Create | Unit tests project |
| `tests/BookingHubAPI.UnitTests/*Tests.cs` | Create | Domain and use case tests |
| `tests/BookingHubAPI.IntegrationTests/BookingHubAPI.IntegrationTests.csproj` | Create | Integration tests |
| `docker-compose.yml` | Create | API + SQL Server containers |
| `BookingHubAPI.sln` | Create | Solution file |

## Interfaces / Contracts

### Domain Entities

```csharp
public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
}

public enum UserRole { Owner, Customer }

public class Company : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string TimeZone { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Service> Services { get; set; }
    public ICollection<WorkingHours> WorkingHours { get; set; }
}

public class Service : BaseEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class Reservation : BaseEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; }
}

public enum ReservationStatus { Pending, Confirmed, Cancelled, Completed }
```

### API Contracts

```csharp
// Auth
POST /api/auth/register { email, password, role }
POST /api/auth/login { email, password } → { token, user }

// Companies
GET /api/companies/me
PUT /api/companies/me
POST /api/companies

// Services
GET /api/services?page=1&pageSize=10&search=
POST /api/services
PUT /api/services/{id}
DELETE /api/services/{id}

// Availability
GET /api/availability/{serviceId}?date=2024-01-15

// Reservations
POST /api/reservations
GET /api/reservations?status=&page=1
PUT /api/reservations/{id}/confirm
PUT /api/reservations/{id}/cancel
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Domain entities, validation, use cases | xUnit + Moq + FluentAssertions |
| Unit | Reservation conflict detection | Theory tests for time overlaps |
| Integration | API endpoints, auth flow | WebApplicationFactory + in-memory DB |
| Integration | Multi-tenant isolation | Test each tenant's data isolation |

## Migration / Rollback

No migration required - fresh project. Database will be created via EF Core migrations on first run.

### Rollback Plan
1. Revert git changes
2. `docker-compose down -v` to clean volumes
3. Delete generated migration files

## Open Questions

- [ ] Should we use MediatR for CQRS pattern, or direct service injection?
- [ ] Which SQL Server edition: Express (free) or LocalDB for dev?
- [ ] Do we need soft-delete for reservations?
- [ ] How to handle timezone conversions - store in UTC or company local time?
