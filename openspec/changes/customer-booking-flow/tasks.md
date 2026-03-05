# Tasks: Customer Booking Flow

## Phase 1: Backend Foundation

- [x] 1.1 Modify `ServiceResponse` in `src/BookingHubAPI.Application/DTOs/ServiceDTOs.cs` to add `CompanyName` and `CompanyDescription` properties
- [x] 1.2 Add `GetAllActiveAsync` method to `IServiceRepository` interface in `src/BookingHubAPI.Domain/Interfaces/IServiceRepository.cs`
- [x] 1.3 Implement `GetAllActiveAsync` in `ServiceRepository` with `Include(s => s.Company)` in `src/BookingHubAPI.Infrastructure/Repositories/ServiceRepository.cs`
- [x] 1.4 Add `GetByIdWithCompanyAsync` method to `IServiceRepository` to fetch service with company data

## Phase 2: Backend API Changes

- [x] 2.1 Create new `GET /api/services/all` endpoint in `ServicesController.cs` that calls `GetAllActiveAsync` and returns company info
- [x] 2.2 Modify `GET /api/services/{id}` in `ServicesController.cs` to return company info in response
- [x] 2.3 Modify `AvailabilityController.cs` to query `Company.WorkingHours` instead of using hardcoded 9-17 hours
- [x] 2.4 Add logic in `AvailabilityController.cs` to check the day of week against company working hours
- [x] 2.5 Modify `ReservationResponse` in `src/BookingHubAPI.Application/DTOs/ReservationDTOs.cs` to include `ServiceName` and `CustomerEmail`
- [x] 2.6 Ensure `GetReservations` in `ReservationsController.cs` populates customer email for owner view

## Phase 3: Frontend Types & API

- [x] 3.1 Update `Service` type in `frontend/src/types/index.ts` to include `companyName` and `companyDescription`
- [x] 3.2 Add `AvailableSlot` type to `frontend/src/types/index.ts`
- [x] 3.3 Update `api.ts` to handle new endpoints (services/all, availability)

## Phase 4: Frontend Service List

- [x] 4.1 Modify `frontend/src/app/services/page.tsx` to call `/api/services/all` endpoint
- [x] 4.2 Update service cards to display company name and description below service name

## Phase 5: Frontend Booking Page

- [x] 5.1 Install shadcn/ui calendar component in `frontend/`
- [x] 5.2 Create `frontend/src/app/services/[id]/book/page.tsx` calendar-based booking flow
- [x] 5.3 Update error handling to show "Time slot is not available" when slot is taken
- [x] 5.4 Add success message and redirect after booking

## Phase 6: Frontend Owner Calendar

- [x] 6.1 Create `frontend/src/app/owner/calendar/page.tsx` new page
- [x] 6.2 Fetch reservations for owner's company via existing `/api/reservations` endpoint
- [x] 6.3 Display reservations in calendar view grouped by date
- [x] 6.4 Show reservation details on click (customer email, service name, status)
- [x] 6.5 Add navigation to calendar (previous/next month)

## Phase 7: Testing

- [x] 7.1 Write unit tests for `ServiceResponse` mapping with company info
- [x] 7.2 Write unit tests for availability slot generation with working hours
- [x] 7.3 Test: Customer can view services with company info
- [x] 7.4 Test: Customer can select date and see available slots
- [x] 7.5 Test: Customer can complete booking successfully
- [x] 7.6 Test: Owner can view reservations in calendar with customer details
- [x] 7.7 Test: Past date booking returns error
- [x] 7.8 Test: Conflicting time slot returns error

## Phase 8: Cleanup

- [x] 8.1 Remove any unused imports or temporary code
- [x] 8.2 Verify frontend builds without errors
- [x] 8.3 Verify backend builds without errors
