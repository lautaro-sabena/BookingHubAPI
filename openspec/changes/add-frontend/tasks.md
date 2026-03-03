# Tasks: Add Frontend Web Application

## Phase 1: Infrastructure & Configuration

- [x] 1.1 Create `frontend/package.json` with Next.js 14, TypeScript, Tailwind, React Query, Zustand, Axios, shadcn dependencies
- [x] 1.2 Create `frontend/tsconfig.json` with TypeScript configuration
- [x] 1.3 Create `frontend/tailwind.config.ts` with custom theme
- [x] 1.4 Create `frontend/next.config.js` with Next.js configuration
- [x] 1.5 Create `frontend/.env.local` with `NEXT_PUBLIC_API_URL=http://localhost:5000`
- [x] 1.6 Create `frontend/Dockerfile` for containerization
- [x] 1.7 Modify `src/BookingHubAPI.API/Program.cs` to add CORS policy for frontend origin
- [x] 1.8 Update `docker-compose.yml` to add frontend service

## Phase 2: Core Components & Providers

- [x] 2.1 Create `frontend/src/types/index.ts` with shared TypeScript interfaces
- [x] 2.2 Create `frontend/src/lib/api.ts` with Axios instance and JWT handling
- [x] 2.3 Create `frontend/src/lib/auth.ts` with auth utilities (token storage, retrieval)
- [x] 2.4 Create `frontend/src/providers/QueryProvider.tsx` with React Query client
- [x] 2.5 Create `frontend/src/providers/AuthProvider.tsx` with auth context and state
- [x] 2.6 Create `frontend/src/hooks/useAuth.ts` hook for consuming auth context

## Phase 3: UI Components (shadcn/ui)

- [x] 3.1 Create `frontend/src/components/ui/button.tsx`
- [x] 3.2 Create `frontend/src/components/ui/input.tsx`
- [x] 3.3 Create `frontend/src/components/ui/label.tsx`
- [x] 3.4 Create `frontend/src/components/ui/card.tsx`
- [x] 3.5 Create `frontend/src/components/ui/form.tsx` (React Hook Form + Zod)
- [x] 3.6 Create `frontend/src/components/ui/select.tsx`
- [x] 3.7 Create `frontend/src/components/ui/tabs.tsx`
- [x] 3.8 Create `frontend/src/components/ui/table.tsx`
- [x] 3.9 Create `frontend/src/components/ui/dialog.tsx`
- [x] 3.10 Create `frontend/src/components/ui/toast.tsx` for notifications

## Phase 4: Authentication Pages

- [x] 4.1 Create `frontend/src/app/layout.tsx` with providers wrapper
- [x] 4.2 Create `frontend/src/app/page.tsx` redirect based on auth state
- [x] 4.3 Create `frontend/src/app/login/page.tsx` with login form
- [x] 4.4 Create `frontend/src/app/register/page.tsx` with registration form
- [x] 4.5 Implement login form validation (email format, password required)
- [x] 4.6 Implement registration form (email, password, confirm password, role selection)
- [x] 4.7 Implement redirect after auth (Owner → dashboard, Customer → services)

## Phase 5: Dashboard & Layout

- [x] 5.1 Create `frontend/src/components/layout/Navbar.tsx` with logout button
- [x] 5.2 Create `frontend/src/components/layout/Sidebar.tsx` for owner navigation
- [x] 5.3 Create `frontend/src/app/dashboard/layout.tsx` with auth guard
- [x] 5.4 Create `frontend/src/app/dashboard/page.tsx` redirect to role-specific view
- [x] 5.5 Create `frontend/src/app/dashboard/owner/page.tsx` owner dashboard
- [x] 5.6 Create `frontend/src/app/dashboard/customer/page.tsx` customer dashboard

## Phase 6: Company Management (Owner)

- [x] 6.1 Create `frontend/src/app/dashboard/company/page.tsx` view company details
- [x] 6.2 Create `frontend/src/app/dashboard/company/edit/page.tsx` edit company form
- [x] 6.3 Implement GET /api/companies/me integration
- [x] 6.4 Implement PUT /api/companies/me integration

## Phase 7: Service Management

- [x] 7.1 Create `frontend/src/app/services/page.tsx` service listing
- [x] 7.2 Create `frontend/src/app/services/[id]/page.tsx` service details
- [x] 7.3 Create `frontend/src/app/dashboard/services/page.tsx` owner service management
- [x] 7.4 Create `frontend/src/app/dashboard/services/new/page.tsx` create service form
- [x] 7.5 Create `frontend/src/app/dashboard/services/[id]/edit/page.tsx` edit service form
- [x] 7.6 Implement GET /api/services integration (paginated)
- [x] 7.7 Implement POST /api/services integration
- [x] 7.8 Implement PUT /api/services/{id} integration
- [x] 7.9 Implement DELETE /api/services/{id} integration

## Phase 8: Availability Management

- [x] 8.1 Create `frontend/src/app/dashboard/availability/page.tsx` working hours config
- [x] 8.2 Implement time slot selector component
- [x] 8.3 Implement GET /api/availability integration
- [x] 8.4 Implement PUT /api/availability integration

## Phase 9: Reservations/Bookings

- [x] 9.1 Create `frontend/src/app/bookings/page.tsx` customer reservations
- [x] 9.2 Create `frontend/src/app/services/[id]/book/page.tsx` booking flow
- [x] 9.3 Create `frontend/src/app/dashboard/reservations/page.tsx` owner view
- [x] 9.4 Create date/time picker component (using native Input type=date/time)
- [x] 9.5 Implement GET /api/reservations integration
- [x] 9.6 Implement POST /api/reservations integration
- [x] 9.7 Implement PUT /api/reservations/{id}/cancel integration
- [x] 9.8 Implement PUT /api/reservations/{id}/confirm integration (owner)

## Phase 10: Testing & Verification

- [ ] 10.1 Write unit tests for auth utilities (`frontend/src/lib/auth.test.ts`)
- [ ] 10.2 Write unit tests for API client (`frontend/src/lib/api.test.ts`)
- [ ] 10.3 Write integration tests for login flow with MSW
- [ ] 10.4 Write integration tests for registration flow with MSW
- [ ] 10.5 Write E2E test: Owner registration and login with Playwright
- [ ] 10.6 Write E2E test: Customer booking flow with Playwright
- [x] 10.7 Verify build compiles successfully

## Phase 11: Polish & Documentation

- [x] 11.1 Add loading states and skeleton components (basic loading text)
- [x] 11.2 Add error handling and toast notifications (inline errors)
- [x] 11.3 Ensure responsive design works on mobile (Tailwind responsive classes)
- [x] 11.4 Update README.md with frontend setup instructions
- [x] 11.5 Test Docker container builds and runs
