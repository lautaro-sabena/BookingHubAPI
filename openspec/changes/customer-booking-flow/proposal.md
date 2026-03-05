# Proposal: Customer Booking Flow

## Intent

Enable Customers to browse services from all Owners and book appointments via a calendar interface. Owners should view reservations in a calendar with customer details.

## Scope

### In Scope
- **Service Discovery**: Customer sees list of all active services with full details (name, description, price, duration, company info)
- **Calendar Booking**: Customer selects date/time from available slots via calendar UI
- **Availability Integration**: Show real-time availability based on Company working hours and existing reservations
- **Owner Calendar View**: Owner sees reservations in calendar format with customer data

### Out of Scope
- Recurring bookings
- Payment processing
- Email/SMS notifications (already exists)
- Waiting list functionality

## Approach

**Frontend-heavy with API enhancement**

1. **Enhance Services List** (`/services`): Add company info to each service card
2. **Calendar Booking Page**: Replace manual date/time inputs with calendar component showing available slots
3. **Availability API Enhancement**: Use Company.WorkingHours instead of hardcoded 9-5
4. **Owner Calendar View**: New dashboard page for Owners showing reservations in calendar

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/BookingHubAPI.API/Controllers/AvailabilityController.cs` | Modified | Use Company.WorkingHours for slot generation |
| `src/BookingHubAPI.API/Controllers/ServicesController.cs` | Modified | Return company info with service list |
| `frontend/src/app/services/page.tsx` | Modified | Display company name/info per service |
| `frontend/src/app/services/[id]/book/page.tsx` | Modified | Calendar UI for slot selection |
| `frontend/src/app/owner/calendar/page.tsx` | New | Owner's reservation calendar view |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Time zone handling across regions | Medium | Use Company's configured TimeZone for all date operations |
| Calendar component complexity | Medium | Use existing UI library (shadcn/ui has calendar) |
| Concurrent booking conflicts | Low | Backend already has HasConflictAsync check |

## Rollback Plan

- Revert frontend changes to use previous date/time inputs
- Revert AvailabilityController to hardcoded hours
- No database migrations needed (schema unchanged)

## Dependencies

- shadcn/ui calendar component (already in project via existing UI components)

## Success Criteria

- [ ] Customer can view all services with company info
- [ ] Customer can select date and see available time slots
- [ ] Customer can complete booking and see confirmation
- [ ] Owner can view reservations in calendar with customer details
- [ ] All booking validations work (past dates, conflicts)
