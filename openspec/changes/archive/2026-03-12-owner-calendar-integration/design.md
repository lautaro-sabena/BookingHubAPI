# Design: Owner Calendar Navigation Fix

## Technical Approach

The solution is to move the Owner Calendar from its standalone route `/owner/calendar` to integrate it under the dashboard layout at `/dashboard/owner/calendar`. This leverages the existing dashboard layout which already includes the Sidebar navigation, providing consistent UX without creating new layout components.

## Architecture Decisions

### Decision: Calendar Route Location

**Choice**: Move calendar page from `/owner/calendar/page.tsx` to `/dashboard/owner/calendar/page.tsx`
**Alternatives considered**: 
- Keep `/owner/calendar` and create a new layout with back button
- Add conditional sidebar rendering based on route
**Rationale**: The dashboard layout already provides Sidebar, Navbar, and proper auth handling. Reusing it avoids code duplication and ensures consistency with all other owner pages.

### Decision: Sidebar Navigation Update

**Choice**: Update Sidebar to point to new calendar path
**Alternatives considered**: Keep both old and new paths
**Rationale**: Single source of truth for navigation. Old path will 404 anyway since file is moved.

## Data Flow

```
User clicks "Calendar" in Sidebar
        │
        ▼
Sidebar → href="/dashboard/owner/calendar"
        │
        ▼
Next.js Router → /dashboard/owner/calendar
        │
        ▼
Dashboard Layout (wraps page)
        │
        ├──▶ Navbar (top)
        ├──▶ Sidebar (left - highlights Calendar)
        └──▶ CalendarPage (main content)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `frontend/src/app/owner/calendar/page.tsx` | Delete | Old standalone calendar page |
| `frontend/src/app/dashboard/owner/calendar/page.tsx` | Create | New calendar page using dashboard layout |
| `frontend/src/components/layout/Sidebar.tsx` | Modify | Update calendar href from `/owner/calendar` to `/dashboard/owner/calendar` |

## Interfaces / Contracts

No new interfaces required. The page component signature remains:
```typescript
export default function OwnerCalendarPage() { ... }
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Manual | Navigation from sidebar to calendar | Click sidebar link, verify page loads |
| Manual | Back navigation | Add back button, verify navigation to /dashboard/owner |
| Manual | Calendar functionality | Verify reservations display, confirm/cancel work |
| Manual | Auth redirects | Test as non-owner, verify redirect |

## Migration / Rollout

No migration required. This is a pure frontend routing change with no database impact.

## Open Questions

- [ ] None - implementation is straightforward
