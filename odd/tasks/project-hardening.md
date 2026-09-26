# Project Hardening Refactor

## Objective
Refactor BookingHubAPI (backend .NET 9 + frontend Next.js 14) to apply best practices,
harden security, apply clean code, and consolidate duplicated modules — without changing
observable business behavior except where it fixes a bug or a vulnerability.

## Problem / Why
The project was generated with weaker AI models. The audit (2026-09-26) found real bugs
(dropped JWT claim, inert rate limiting, CORS reflection on errors), committed build
artifacts and dev secrets, business logic and authorization checks duplicated across
controllers, an empty Application layer, and thin tests around the risky paths.

## Scope
- Backend: `backend/src/**`, `backend/tests/**`, Docker/deploy config.
- Frontend: `frontend/src/**`.
- Repo hygiene: tracked `bin/`/`obj/`, docs.

## Constraints
- Every task keeps the build green and existing tests passing; tests land with behavior.
- No git history rewrite (purging `bin/`/`obj/` from history needs an explicit user decision; this plan only untracks them).
- No push, PR, or merge without user decision.
- Planning heuristic ~400 authored changed lines per task (advisory only).

## TDD
- Mode: off (source: no project/session configuration found; not chosen by user).
- Runners: backend `dotnet test backend/BookingHubAPI.sln`; frontend `npm test` in `frontend/`.
- Ordinary functional checks still run per task.

## Delivery
- Strategy: `ask-on-risk` (default). Forecast exceeds ~400 lines → chain strategy: `stacked-to-main` (user choice, 2026-09-26).
- Slices: PR1 = T1 + T1b (hygiene + test harness).
- RDD: on (global). Per-commit `gentle-ai review assess` after each work-unit commit.

## Tasks
- [x] T1 — Untrack `bin/`/`obj/` (git rm --cached), verify `.gitignore`. Route: inline (mechanical). Evidence: 1110 files untracked; tracked bin/obj count = 0; `dotnet build` 0 errors; build no longer dirties status.
- [ ] T1b — Fix integration test harness: test factory registers EF InMemory alongside Npgsql (6/7 integration tests fail on base: "Only a single database provider"). Route: inline.
- [ ] T2 — Security bug fixes: JWT `companyId` claim (JwtService), load rate-limit rules into configuration, CORS in ErrorHandlingMiddleware uses configured whitelist, stop leaking raw exception messages. Tests for each. Route: delegated writer (3+ non-trivial files).
- [ ] T3 — Secrets & config: remove hardcoded secrets from `appsettings.Development.json`/compose, use user-secrets/env vars, fix SQL Server vs Postgres connection-string mismatch, update `.env.example`/README. Route: delegated writer.
- [ ] T4 — Auth plumbing reuse: `ClaimsPrincipal` extensions (`GetUserId`, `GetCompanyId`), role constants instead of magic strings, remove 5x duplicated `GetUserId()`. Route: delegated writer.
- [ ] T5 — Application layer: use-case services for Reservations (conflict detection, status transitions, ownership), FluentValidation validators, Result pattern; thin controller. Tests for conflict + IDOR paths. Route: delegated writer.
- [ ] T6 — Application layer: same for Services, Companies, WorkingHours, Favorites, Auth. Route: delegated writer (may split).
- [ ] T7 — Consistent error handling: ProblemDetails, Result→HTTP mapping, domain exceptions. Route: delegated writer.
- [ ] T8 — Domain enrichment: invariants/behavior on entities (Reservation status transitions, etc.). Route: delegated writer.
- [ ] T9 — Database: replace `EnsureCreated()` with migrations, reconcile migration with model. Route: delegated writer.
- [ ] T10 — Remove dead scaffolding (empty folders, `UnitTest1.cs`), README cleanup (openspec refs), move Postman collection to `docs/`. Route: inline.
- [ ] T11 — Frontend: extract data/form hooks and reusable components from the large calendar/booking pages; shared calendar logic between owner/customer. Route: delegated writer.
- [ ] T12 — Frontend auth hardening: stop storing JWT in `localStorage` (httpOnly cookie, backend support). Route: delegated writer. Needs user confirmation (cross-cutting behavior change).

## Acceptance criteria
- `dotnet build` + `dotnet test` green; `npm run build` + `npm test` + lint green.
- Each audit security finding fixed or explicitly deferred with reason.
- No duplicated auth boilerplate across controllers; controllers delegate to Application.

## Progress
- Branch `refactor/project-hardening` created from `main` (8960e9f).
- Baseline: UnitTests 31/31 pass; IntegrationTests 1/7 pass (6 fail: dual EF provider in test factory).
- T1 commit: see `git log` "chore: stop tracking build artifacts in git".

## Next step
T1b (test harness), then T2.
