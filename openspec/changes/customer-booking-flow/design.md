# Design: Customer Booking Flow

## Technical Approach

Implement a customer-facing booking flow with calendar-based slot selection and an owner calendar view for reservations. The backend will be enhanced to use Company working hours for availability, and the frontend will display company info with services and provide a calendar UI.

## Architecture Decisions

### Decision: Services List Returns Company Info

**Choice**: Modify `ServiceResponse` to include company name and description
**Alternatives considered**: Create separate endpoint for company info, include company ID only
**Rationale**: Single request gets all info needed for service card display; follows existing DTO pattern

### Decision: Availability Uses Company Working Hours

**Choice**: Query `Company.WorkingHours` entity to determine operating hours per day
**Alternatives considered**: Hardcode default hours (9-5), add hours field to Company entity
**Rationale**: Existing `WorkingHours` entity already exists in domain model; supports different hours per day of week

### Decision: Calendar UI Component

**Choice**: Use shadcn/ui Calendar + time slot selection pattern
**Alternatives considered**: Full calendar library (FullCalendar), custom date picker + dropdown
**Rationale**: Consistent with existing UI patterns in project; shadcn/ui already available

### Decision: Owner Calendar Page

**Choice**: New `/owner/calendar` route with monthly calendar view
**Alternatives considered**: Extend existing dashboard, use table view
**Rationale**: Calendar is more intuitive for booking management; separates concerns

## Data Flow

```
[Customer] → /services → [ServicesController] → [ServiceRepository] → DB
                ↓
         ServiceResponse (with company info)

[Customer] → /availability/{serviceId}?date= → [AvailabilityController] 
                → [ServiceRepository] (get company) 
                → [CompanyRepository] (get working hours)
                → [ReservationRepository] (check conflicts)
                → Available slots

[Customer] → POST /reservations → [ReservationsController] 
                → Validate service/company active
                → Check HasConflictAsync
                → Create reservation
                → Return ReservationResponse

[Owner] → /owner/calendar → [ReservationsController] 
                → GetByCompanyIdAsync
                → Return list with customer details
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/BookingHubAPI.Application/DTOs/ServiceDTOs.cs` | Modify | Add companyName, companyDescription to ServiceResponse |
| `src/BookingHubAPI.API/Controllers/ServicesController.cs` | Modify | GET /services returns company info; add endpoint for all services |
| `src/BookingHubAPI.API/Controllers/AvailabilityController.cs` | Modify | Use Company.WorkingHours for slot generation |
| `src/BookingHubAPI.Domain/Interfaces/IServiceRepository.cs` | Modify | Add GetAllActiveAsync method |
| `src/BookingHubAPI.Infrastructure/Repositories/ServiceRepository.cs` | Modify | Implement GetAllActiveAsync with company include |
| `frontend/src/types/index.ts` | Modify | Add companyName, companyDescription to Service type |
| `frontend/src/app/services/page.tsx` | Modify | Display company info in service cards |
| `frontend/src/app/services/[id]/book/page.tsx` | Modify | Replace inputs with calendar + slot selection |
| `frontend/src/app/owner/calendar/page.tsx` | Create | Owner's reservation calendar view |
| `frontend/src/components/ui/calendar.tsx` | Create | shadcn/ui Calendar component |

## Interfaces / Contracts

### Backend DTOs

```csharp
// Modified ServiceResponse
public record ServiceResponse(
    Guid Id, 
    string Name, 
    string? Description, 
    int DurationMinutes, 
    decimal Price, 
    bool IsActive, 
    Guid CompanyId,
    string CompanyName,           // NEW
    string? CompanyDescription   // NEW
);
```

### Frontend Types

```typescript
// Modified Service type
interface Service {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
  companyId: string;
  companyName: string;           // NEW
  companyDescription?: string;   // NEW
}

interface AvailableSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}
```

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/services/all` | GET | Get all active services with company info (for customers) |
| `/api/availability/{serviceId}?date={date}` | GET | Get available slots using company working hours |
| `/api/reservations` | GET | Get reservations (filtered by role) |
| `/api/owner/calendar` | GET | Get all reservations for owner's company |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | ServiceResponse mapping, availability slot generation | xUnit with mocked repositories |
| Integration | API endpoints return correct data | Integration tests with in-memory DB |
| E2E | Full booking flow, calendar interactions | Manual or Playwright |

## Migration / Rollback

**No migration required.** Schema unchanged. Rollback involves reverting code changes:
1. Revert ServiceResponse to previous shape
2. Revert AvailabilityController to hardcoded hours
3. Revert frontend to simple date/time inputs

## Open Questions

- [ ] Should we add pagination to `/api/services/all` endpoint?
- [ ] Should we support timezone conversion for customers in different timezones?
- [ ] Do we need to include customer phone/contact info in owner calendar view?
