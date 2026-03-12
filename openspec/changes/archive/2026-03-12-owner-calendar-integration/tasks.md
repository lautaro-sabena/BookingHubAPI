# Tasks: Owner Calendar Navigation Fix

## Phase 1: Create New Calendar Page

- [x] 1.1 Create directory `frontend/src/app/dashboard/owner/calendar/`
- [x] 1.2 Copy calendar component from `frontend/src/app/owner/calendar/page.tsx` to `frontend/src/app/dashboard/owner/calendar/page.tsx`

## Phase 2: Update Sidebar Navigation

- [x] 2.1 Modify `frontend/src/components/layout/Sidebar.tsx` - update calendar href from `/owner/calendar` to `/dashboard/owner/calendar`

## Phase 3: Remove Old Calendar Page

- [x] 3.1 Delete `frontend/src/app/owner/calendar/page.tsx`
- [x] 3.2 Remove empty `frontend/src/app/owner/` directory if no other files exist

## Phase 4: Verification

- [ ] 4.1 Test: Navigate to Calendar via sidebar - page loads with sidebar
- [ ] 4.2 Test: Navigate back to Owner Dashboard - verify back button or use browser back
- [ ] 4.3 Test: View reservations - verify they display with correct status colors
- [ ] 4.4 Test: Confirm a reservation - verify status changes
- [ ] 4.5 Test: Cancel a reservation - verify status changes
- [ ] 4.6 Test: Navigate between months - verify calendar updates
- [ ] 4.7 Test: Non-Owner role accessing calendar - verify redirect to /dashboard
