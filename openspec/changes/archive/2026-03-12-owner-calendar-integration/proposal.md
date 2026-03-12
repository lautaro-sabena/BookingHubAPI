# Proposal: Owner Calendar Navigation Fix

## Intent

The Owner Calendar page (`/owner/calendar`) is currently a standalone page without navigation back to the Owner dashboard. Users accessing the calendar have no way to return to the Owner hub (`/dashboard/owner`) except via browser back button. Additionally, the page lacks the consistent sidebar navigation present in other owner pages. This creates a fragmented user experience.

## Scope

### In Scope
- Add back navigation (arrow/button) from Calendar page to Owner dashboard
- Integrate Calendar page into the main owner dashboard layout with sidebar navigation
- Maintain existing calendar functionality (view reservations, confirm/cancel bookings)
- Preserve current calendar UI aesthetics

### Out of Scope
- Redesigning the calendar UI itself (colors, layout)
- Adding new calendar features
- Consolidating "Reservations" and "Calendar" menu items in sidebar

## Approach

**Recommended: Full Integration**
1. Move the Calendar page from `/owner/calendar` to `/dashboard/owner/calendar` to match the routing pattern of other owner pages
2. Update the sidebar to reference the new path
3. This gives the calendar page the same layout (sidebar + content area) as other owner pages

**Alternative: Minimal Change**
Add a back button at the top of the current `/owner/calendar` page pointing to `/dashboard/owner`

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `frontend/src/app/owner/calendar/page.tsx` | Modified | Add back button or integrate into dashboard layout |
| `frontend/src/components/layout/Sidebar.tsx` | Modified | Update calendar href to new path (if moving) |
| `frontend/src/app/dashboard/layout.tsx` | New/Modified | Ensure layout includes sidebar for owner routes |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Breaking existing calendar functionality | Low | Test all reservation features after change |
| Routing conflicts with existing dashboard routes | Low | Verify `/dashboard/owner/calendar` path doesn't conflict |
| Sidebar showing duplicate navigation items | Low | Remove or consolidate Reservations/Calendar items if needed |

## Rollback Plan

1. Revert the file move (if calendar was moved)
2. Restore previous `/owner/calendar/page.tsx` content
3. Revert Sidebar.tsx to previous href
4. No database changes required

## Dependencies

- None - all changes are frontend-only

## Success Criteria

- [ ] Calendar page accessible via sidebar navigation
- [ ] User can navigate from Calendar back to Owner dashboard using back arrow/button
- [ ] Calendar displays reservations correctly with status colors
- [ ] Confirm/Cancel reservation actions work
- [ ] No console errors on page load
