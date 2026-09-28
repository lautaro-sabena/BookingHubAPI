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
- Slices (stacked to main; PR1 over budget so split in 3):
  - #1 `refactor/hardening-01-hygiene` (base main): 9af03f0, 64751e6 — T1, T1b. 135 authored lines + generated deletions.
  - #2 `refactor/hardening-02-security-fixes` (base #1): 0770bf6, 3e97637 — T2. 311 lines.
  - #3 `refactor/hardening-03-proxy-client-ip` (base #2): 7b1f644, 536af0b, 310d43a — T2b, T2c. 314 lines.
  - Next slices continue from `refactor/project-hardening` on top of #3; retarget each PR to main after its parent merges.
- RDD: on (global). Per-commit `gentle-ai review assess` after each work-unit commit.

## Tasks
- [x] T1 — Untrack `bin/`/`obj/` (git rm --cached), verify `.gitignore`. Route: inline (mechanical). Evidence: 1110 files untracked; tracked bin/obj count = 0; `dotnet build` 0 errors; build no longer dirties status.
- [x] T1b — Fix integration test harness: test factory registers EF InMemory alongside Npgsql (6/7 integration tests fail on base: "Only a single database provider"). Route: inline (3 small test files). Commit 64751e6; review assess: medium, 80 lines, under_budget (pending in slice). Root cause: EF Core 9 keeps provider config in `IDbContextOptionsConfiguration<T>`; also `Guid.NewGuid()` inside the options lambda gave every DbContext its own DB. Fix: reusable `BookingApiFactory`; removed placeholder `UnitTest1.cs`. Evidence: `dotnet test backend/BookingHubAPI.sln` → UnitTests 31/31, IntegrationTests 6/6.
- [x] T2 — Security bug fixes: JWT `companyId` claim (JwtService), load rate-limit rules into configuration, CORS in ErrorHandlingMiddleware uses configured whitelist. Tests for each. (Exception-message exposure moved to T7: controllers likely rely on user-facing messages.) Route: delegated writer (3+ non-trivial files). Evidence: build 0 errors; UnitTests 33/33, IntegrationTests 10/10. Extra finding: rate-limit.json used `GeneralRule`/`Rules` keys that AspNetCoreRateLimit never binds (fixed to `GeneralRules`). Test factory relaxes limits by default (`RelaxRateLimiting`).
- [x] T2b — Rate-limit follow-ups from review (lineage review-ff7be9de3820308c, advisory): (a) R4-002 limiter keys on connection IP; behind Render proxy all users share one counter → configure ForwardedHeaders/RealIpHeader (X-Forwarded-For) with trusted proxies; exclude /health from limits. (b) R3-001/R4-001 rate-limit.json added after env vars overrides them and optional:false fails startup outside content root → move rules into appsettings.json (or insert source before env vars). (c) R3-002 test real login throttle (6th login → 429). (d) test nits: R2-ratelimit-magic-count, R2-cors-origin-fallback, R2-relax-doc-overstates. Route: delegated writer.
  Evidence: build 0 errors; UnitTests 33/33, IntegrationTests 13/13 (10 baseline + 3 new: health-whitelist, forwarded-IP-independent-counters, login-throttle-429). Mechanism chosen: ASP.NET Core `ForwardedHeadersMiddleware` (`app.UseForwardedHeaders()`, first in pipeline in `Program.cs`), not AspNetCoreRateLimit's `RealIpHeader` — it rewrites `RemoteIpAddress` once for every downstream consumer instead of only affecting the limiter, and supports trusted-proxy validation. Since Render doesn't publish a fixed proxy IP/CIDR, trust is gated by new flag `ForwardedHeaders:TrustAllProxies` (env `ForwardedHeaders__TrustAllProxies`, default `false`; set to `true` in `render.yaml` for the API service only) which clears `KnownNetworks`/`KnownProxies` and uses `ForwardLimit = 1`. Tradeoff documented in a `Program.cs` comment: with the flag on, a client that reaches the container directly (bypassing Render's proxy) could spoof `X-Forwarded-For` to evade/pool rate limits. `IpRateLimiting` (incl. new `EndpointWhitelist: ["get:/health"]`) moved from deleted `rate-limit.json` into `appsettings.json`; `AddJsonFile("rate-limit.json", ...)` call removed (no csproj/Dockerfile reference existed). Test nits fixed: `RateLimitingTests.cs` derives loop count from a shared `TinyLimit` const; `ErrorHandlingCorsTests.cs` binds `Cors:AllowedOrigins` as an array only (scalar fallback removed); `BookingApiFactory.RelaxRateLimiting` doc now says rules are replaced by a single permissive wildcard rule. Deploy config change: `render.yaml` API service env now sets `ForwardedHeaders__TrustAllProxies: "true"`.
- [x] T2c — Review follow-ups for T2b (lineage review-872697bc69eb974c approved+acknowledged; advisory R3 findings). Found TestServer leaves RemoteIpAddress null, so ForwardedHeaders skipped the proxy check and the trusted-proxy test passed vacuously; tests now simulate a real proxy peer. Program.cs read TrustAllProxies eagerly (invisible to host config overrides) → now read lazily via options. Added untrusted-default spoof test; /health asserts 200. Open advisory: R3-forwardlimit-render-hops (ForwardLimit=1 assumes one proxy hop on Render; verify real X-Forwarded-For shape after deploy). Evidence: UnitTests 33/33, IntegrationTests 14/14. Route: inline. Commit 536af0b; review (lineage review-1d39023568790e82) approved+acknowledged; advisory: assert 404 on first probe in spoof test (R3-untrusted-first-request-weak-assert).
- [x] T3b — Commit 8206c85; review lineage review-cbd6b25e2aaed2c8 approved + acknowledged (boundary → 8206c85); advisory: ToDto() throws if Service/Company not loaded (repository Includes both today) — add GET /favorites integration test in T6. Done: AutoMapper removed (only Favorite→FavoriteDto was used; explicit `ToDto()` + tests; unused IMapper injections and dead profile maps deleted); unused Microsoft.OpenApi removed from IntegrationTests; `dotnet list package --vulnerable --include-transitive` clean; frontend `npm audit fix` (no --force): prod vulns 10→2. Evidence: build 0 warnings, UnitTests 44/44, IntegrationTests 20/20, frontend build+lint pass. Original scope — Vulnerable dependencies: NU1903 high-severity advisories on AutoMapper 13.0.1 (API, Application; GHSA-rvv3-g6hj-g44x) and Microsoft.OpenApi 2.4.1 (IntegrationTests; GHSA-v5pm-xwqc-g5wc). Upgrade or replace (AutoMapper 15+ changed license — consider removing it in favor of explicit mapping during T5/T6). Also run `npm audit` on frontend. Route: inline/delegated.
- [x] T3 — Secrets & config: remove hardcoded secrets from `appsettings.Development.json`/compose, use user-secrets/env vars, fix SQL Server vs Postgres connection-string mismatch, update `.env.example`/README. Also: `appsettings.Development.json` `Cors:AllowedOrigins` is a comma string vs array in appsettings.json, so only localhost:3000 binds. Route: delegated writer. Commit b7ab2ac. Done: secrets removed from appsettings.Development.json (user-secrets via UserSecretsId); StartupConfigurationValidator (Infrastructure) fails fast naming the missing key; Postgres connection string without password; CorsOriginsResolver (API layer) accepts array or comma-separated scalar; docker-compose.yml tracked as template with ${VAR:?} (Compose v2); render.yaml JWT_SECRET_KEY → Jwt__SecretKey (old name never bound). Evidence: UnitTests 38/38, IntegrationTests 19/19. Follow-ups: USER must set Jwt__SecretKey in Render dashboard; treat old dev secrets in git history as compromised (rotate); stale nested backend/src/BookingHubAPI.API/.env.example (SQL Server, unused var names) deleted in follow-up commit. Review lineage review-d6a24cfb338f4173 (T3 + e1fa27c, 447 lines, high): approved + acknowledged; boundary → e1fa27c.
- [x] T3c — Commit 08d73c8; review lineage review-08f0185d632fc77c approved + acknowledged (boundary → 08d73c8); advisory: test blank-scalar fallback to JSON array, README precedence wording. Evidence: UnitTests 42/42, IntegrationTests 20/20. Resolver now prefers the section scalar (env var) over JSON array children. T3 review follow-ups: CORS resolver ignores env scalar when JSON array exists (real bug); drop dev connection string so fail-fast triggers; reject whitespace config; README CORS docs; duplicated comment; ContainSingle test nit; Postgres bound to 127.0.0.1 in compose. USER: confirm root .env.example lists DB_PASSWORD and JWT_SECRET_KEY (agents cannot read .env* files). Route: delegated writer.
- [ ] T3d — (code done; OPEN until user browser smoke test passes — review lineage review-a423480f4b898e0c approved + acknowledged, advisory: runtime unverified, Node 20 EOL → moved to node:22-alpine + engines >=22, add booking-page test in T11, track react-hooks warning count) Next 14.2.35→16.3.6, React 19, eslint 9 flat config (`eslint .`), async params in services/[id]/book, Node 22 in Dockerfiles (dc3002f, reviewed+acknowledged; add Docker build smoke check to user test list); lucide-react + @testing-library/react bumped for React 19 peers. Evidence: build pass, lint 0 errors/21 warnings, vitest 8 pass/6 known fail, `npm audit --omit=dev` 0 vulns. Needs manual browser smoke test (auth, booking, dashboards, calendars, favorites, theme, icons). Follow-ups: react-hooks set-state-in-effect/immutability downgraded to warn (19 findings, fix in T11); vitest 5 (dev-only critical) pending; auth.test.ts fails with `localStorage is not defined` (test env lacks jsdom/localStorage) — fix with T12; build warns about stray C:SERSaUTAPACKAGE-LOCK.JSON (SET TURBOPACK.ROOT). ORIGINAL NOTE — FRONTEND FOLLOW-UPS: (a) remaining advisories need major upgrades — next 14.2.35→16.x (critical, also fixes postcss high), eslint-config-next 16, vitest 5 (dev-only critical) — needs user decision; (b) 6 pre-existing failures in `frontend/src/lib/__tests__/auth.test.ts` (also fail on original lockfile), investigate with T12 (auth storage).
- [x] T4 — Commit e3655ab; review lineage review-e132f03ab1078151 approved + acknowledged (boundary → e3655ab). GetCompanyId() deferred (no consumer yet; add in T5/T6 if needed). Done: `User.GetUserId()` extension (API/Extensions) replaces 5 private copies, same semantics (NameIdentifier, Guid.Parse); `RoleNames` constants in Domain (nameof(UserRole.X)) used in [Authorize(Roles=...)]. Evidence: build 0 warnings, UnitTests 46/46, IntegrationTests 23/23. Route: delegated writer. Original scope — Auth plumbing reuse: `ClaimsPrincipal` extensions (`GetUserId`, `GetCompanyId`), role constants instead of magic strings, remove 5x duplicated `GetUserId()`. Route: delegated writer.
- [ ] T5 — Application layer: use-case services for Reservations (conflict detection, status transitions, ownership), FluentValidation validators, Result pattern; thin controller. Tests for conflict + IDOR paths. Route: delegated writer.
- [ ] T6 — Application layer: same for Services, Companies, WorkingHours, Favorites, Auth. Route: delegated writer (may split).
- [ ] T7 — Also: GetUserId() throws ArgumentNullException/FormatException (→500) on missing/invalid NameIdentifier claim; should map to 401. Consistent error handling: ProblemDetails, Result→HTTP mapping, domain exceptions; stop exposing raw ArgumentException/InvalidOperationException messages. Route: delegated writer.
- [ ] T8 — Domain enrichment: invariants/behavior on entities (Reservation status transitions, etc.). Route: delegated writer.
- [ ] T9 — Database: replace `EnsureCreated()` with migrations, reconcile migration with model. Route: delegated writer.
- [ ] T10 — Remove dead scaffolding (empty folders; `UnitTest1.cs` already removed in T1b), README cleanup (openspec refs), move Postman collection to `docs/`. Route: inline.
- [ ] T11 — Frontend: extract data/form hooks and reusable components from the large calendar/booking pages; shared calendar logic between owner/customer. Route: delegated writer.
- [ ] T12 — Frontend auth hardening: stop storing JWT in `localStorage` (httpOnly cookie, backend support). Route: delegated writer. Needs user confirmation (cross-cutting behavior change).

## Acceptance criteria
- `dotnet build` + `dotnet test` green; `npm run build` + `npm test` + lint green.
- Each audit security finding fixed or explicitly deferred with reason.
- No duplicated auth boilerplate across controllers; controllers delegate to Application.

## Progress
- Branch `refactor/project-hardening` created from `main` (8960e9f).
- Baseline: UnitTests 31/31 pass; IntegrationTests 1/7 pass (6 fail: dual EF provider in test factory).
- T1 commit: 9af03f0 "chore: stop tracking build artifacts in git". Review: assessed high (heuristic on a deleted .dll name); user granted; START refused `lens_context_budget_exceeded` (36812 generated lines; cannot be split meaningfully). Structural readback only: diff is 1110 bin/obj deletions + this doc.
- Local: `.atl/` and `entrevista-repaso.md` added to `.git/info/exclude` (local-only, not committed).

- Review boundary advanced to 9af03f0 by user decision (T1 not natively reviewable: budget exceeded).
- T2 commit 0770bf6. Slice 9af03f0..0770bf6 (T1b+T2, 381 lines) assessed high (auth hot path); user granted; 4-lens review approved, acknowledged (authority burned). Review boundary → 0770bf6. 8 advisory findings → T2b.

## Next step
PRs #1–#3 open (user merges). Next: T3 (secrets & config), T3b (vulnerable deps).
