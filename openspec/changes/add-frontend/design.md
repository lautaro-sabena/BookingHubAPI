# Design: Add Frontend Web Application

## Technical Approach

Build a Next.js 14+ (App Router) frontend with TypeScript and Tailwind CSS that consumes the existing REST API. The app will provide authentication flows, company management for owners, and booking functionality for customers. Use React Query for server state and Zustand for UI state.

## Architecture Decisions

### Decision: Next.js App Router over Pages Router

**Choice**: Next.js 14+ with App Router (server components where possible)
**Alternatives considered**: React + Vite, Next.js Pages Router
**Rationale**: App Router provides better SEO for public pages, streaming, and simpler routing. Server components reduce client bundle size.

### Decision: React Query over SWR or Redux

**Choice**: TanStack React Query v5
**Alternatives considered**: SWR, Redux Toolkit Query, Zustand-only
**Rationale**: Best-in-class caching, optimistic updates, and refetching built-in. Matches the "server state" pattern well.

### Decision: Tailwind CSS + shadcn/ui over MUI/Chakra

**Choice**: Tailwind CSS with shadcn/ui components
**Alternatives considered**: Material UI, Chakra UI, raw CSS modules
**Rationale**: shadcn/ui provides accessible, customizable components that we own (not a library dependency). Tailwind enables rapid styling without context switching.

### Decision: JWT in cookies over localStorage

**Choice**: httpOnly cookies for JWT storage
**Alternatives considered**: localStorage, sessionStorage
**Rationale**: httpOnly cookies are not accessible via JavaScript, providing XSS protection. Required for secure authentication.

### Decision: API URL via environment variable

**Choice**: External API URL configurable via NEXT_PUBLIC_API_URL
**Alternatives considered**: Hardcoded localhost, proxy via Next.js API routes
**Rationale**: Environment variable is simple, works with Docker, and avoids CORS issues when properly configured.

## Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (Next.js)                        │
├─────────────────────────────────────────────────────────────────┤
│  Pages:                                                         │
│  /login ──→ AuthContext ──→ API call ──→ Store cookie          │
│  /register                                                      │
│  /dashboard (Owner) ──→ React Query ──→ API                    │
│  /dashboard (Customer)                                          │
│  /services ──→ [list/create/edit]                              │
│  /bookings ──→ [list/create/cancel]                             │
│  /availability ──→ [manage working hours]                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS + JWT Bearer
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Backend API (.NET 9)                         │
├─────────────────────────────────────────────────────────────────┤
│  Endpoints:                                                     │
│  POST /api/auth/register, /api/auth/login                       │
│  GET/PUT /api/companies/me                                       │
│  GET/POST/PUT/DELETE /api/services                               │
│  GET/POST /api/reservations                                      │
│  GET/PUT /api/availability                                       │
└─────────────────────────────────────────────────────────────────┘
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `frontend/package.json` | Create | Next.js + dependencies |
| `frontend/tsconfig.json` | Create | TypeScript config |
| `frontend/tailwind.config.ts` | Create | Tailwind setup |
| `frontend/next.config.js` | Create | Next.js config |
| `frontend/.env.local` | Create | API URL config |
| `frontend/src/app/layout.tsx` | Create | Root layout with providers |
| `frontend/src/app/page.tsx` | Create | Landing/redirect page |
| `frontend/src/app/login/page.tsx` | Create | Login page |
| `frontend/src/app/register/page.tsx` | Create | Registration page |
| `frontend/src/app/dashboard/page.tsx` | Create | Role-based dashboard |
| `frontend/src/app/services/page.tsx` | Create | Services list |
| `frontend/src/app/bookings/page.tsx` | Create | Reservations page |
| `frontend/src/app/availability/page.tsx` | Create | Working hours |
| `frontend/src/components/ui/` | Create | shadcn/ui components |
| `frontend/src/components/auth/` | Create | Auth forms |
| `frontend/src/components/dashboard/` | Create | Dashboard components |
| `frontend/src/lib/api.ts` | Create | API client with axios |
| `frontend/src/lib/auth.ts` | Create | Auth utilities |
| `frontend/src/hooks/useAuth.ts` | Create | Auth context hook |
| `frontend/src/providers/` | Create | React Query + Auth providers |
| `frontend/Dockerfile` | Create | Frontend container |
| `src/BookingHubAPI.API/Program.cs` | Modify | Add CORS configuration |

## Interfaces / Contracts

### API Client (frontend/src/lib/api.ts)

```typescript
interface ApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, data?: unknown): Promise<T>;
  put<T>(url: string, data: unknown): Promise<T>;
  delete(url: string): Promise<void>;
  setToken(token: string | null): void;
}

interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  role: 'Owner' | 'Customer';
}
```

### Auth Context

```typescript
interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, role: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Auth logic, form validation, utilities | Vitest + React Testing Library |
| Integration | Auth flow, API integration | Vitest + MSW (Mock Service Worker) |
| E2E | Critical user journeys | Playwright |

Priority E2E tests:
- User registration flow
- Login and session persistence
- Owner creates service
- Customer books service

## Migration / Rollout

No database migration required. The frontend is a new consumer of existing APIs.

### CORS Configuration Needed

The backend currently has no CORS configured. Will add:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Docker Compose Update

Add frontend service:

```yaml
frontend:
  build: ./frontend
  ports:
    - "3000:3000"
  environment:
    - NEXT_PUBLIC_API_URL=http://api:5000
  depends_on:
    - api
```

## Open Questions

- [ ] Should the frontend use a form library (React Hook Form + Zod) or native forms?
- [ ] Do we need a separate "public" services page for unauthenticated browsing?
- [ ] Should we implement real-time availability updates via polling or WebSockets?
