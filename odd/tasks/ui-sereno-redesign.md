# UI Redesign "Sereno"

## Objective
Apply the Claude Design "Sereno" redesign (`references/bookinghub-sereno.zip`) to the
Next.js frontend: new color system, typography, layout shell, navigation, feature
components and every screen.

## Problem / Why
The frontend logic is hardened and tested (project-hardening chain, PRs #1–#40, merged
2026-10-01), but the presentation is inconsistent: weak empty/loading/error states,
dark-mode gaps (e.g. hardcoded error colors) and a broken input focus ring.

## Scope
- `frontend/tailwind.config.ts`, `frontend/postcss.config.js`, `frontend/src/app/**`,
  `frontend/src/components/**` — only the files shipped in the zip, plus new
  presentational components it adds.
- `.gitignore` (ignore `references/`).

## Constraints
- Presentation only: no changes to hooks, data fetching, props contracts, routing or
  form submission (`src/hooks`, `src/lib`, `src/providers`, `src/proxy.ts` untouched
  unless a follow-up below explicitly needs it).
- Keep every accessible name and `role="alert"` the tests rely on.
- The design is applied on top of current `main`; any logic that differs between the
  zip and `main` is reconciled in favor of `main` (zip wins only on markup/classes).
- No new runtime dependencies.
- Planning heuristic ~400 authored changed lines per task (advisory only).

## TDD
- Mode: exception — presentational change with no meaningful RED; existing tests are the
  regression net. Runner: `npm test -- --run` in `frontend/`, plus `npm run lint`,
  `npx tsc --noEmit` and `npm run build`.

## Delivery
- Strategy: `ask-on-risk` → user chose `stacked-to-main` (2026-10-01).
- Forecast: ~1,800 authored changed lines across 54 files (actual: 2,796 across 57 incl. tests and this doc).
- Work was built as 4 local slices, then re-sliced before push (user decision, 2026-10-01)
  because 3 of them exceeded 400 lines. Same final tree; every slice passes tsc, lint and tests.
  1. `01-tokens` — tokens, font, theme base, this doc
  2. `02-ui-kit` — `components/ui/*` + StatusBadge tests
  3. `03-navigation` — layout components, dashboard/services layouts + Sidebar tests
  4. `04-booking-components` — BookingForm, SlotPicker + summary tests
  5. `05-feature-components` — calendar, availability, services, company, reservations
  6. `06-public-screens` — landing, login, register, catalogue, book
  7. `07-customer-screens` — customer dashboard pages, /bookings
  8. `08-owner-screens` — owner dashboard pages

## Tasks
- [x] T1 Foundation: `.gitignore`, `globals.css`, `tailwind.config.ts`, `postcss.config.js`,
  `app/layout.tsx`, `components/ui/*` (modified + new `skeleton`, `page-header`,
  `empty-state`, `stat-card`, `notice`). Route: delegated (writer trigger, 2+ non-trivial files).
- [x] T2 Navigation: `components/layout/*` (Navbar, Sidebar, new AppShell, AuthShell, Logo,
  ThemeToggle), `app/dashboard/layout.tsx`, `app/services/layout.tsx`. Route: delegated.
- [x] T3 Feature components: calendar, booking, availability, services (incl. new
  ServiceCard), company, new reservations/ReservationRow. Route: delegated.
- [x] T4 Screens: every `app/**/page.tsx` in the zip. Route: delegated.
- [x] T5 Follow-ups from the design notes: customer calendar `palette="light"` → check
  whether `"themed"` is needed for dark mode; history "Date:/Time:" label merge vs tests;
  add a `StatusBadge` test for known vs fallback statuses; decide whether `themeColor`
  should follow the class-based theme (both from the T1 review).

## Acceptance criteria
- All frontend tests, lint, typecheck and build pass on every slice.
- No diff under `src/hooks`, `src/lib`, `src/providers`, `src/proxy.ts` (except T5 if justified).
- Both themes render with the new tokens.

## Progress / Evidence
- 2026-10-01: branch `refactor/ui-sereno-01-foundation` created from `main`; RDD on (global).
- 2026-10-01: T1 done in `63cde7d` (delegated writer). Handoff applied verbatim: all diffs were markup/classes only; tokens are additive (no aliases needed); `postcss.config.js` identical to repo. Checks: `npm test -- --run` 22 files / 136 tests passed; `npm run lint` clean; `npx tsc --noEmit` exit 0; `npm run build` exit 0.
- 2026-10-01: T2 done in `6b42ff4` (delegated writer). Handoff applied; layouts keep auth guards/redirects unchanged (only loading markup -> `LoadingState variant="screen"` and wrapper -> `AppShell`); `ThemeToggle` reuses existing `useTheme()` context; Sidebar active state now also matches sub-pages and adds `aria-current` + `aria-label="Main"`. Checks: `npm test -- --run` 22 files / 136 tests passed; `npm run lint` clean; `npx tsc --noEmit` exit 0; `npm run build` exit 0.
- 2026-10-01: T3 done in `0283e76` (delegated writer). Handoff applied; no repo logic dropped (submit payloads, `isPending`/`isSuccess` disabling, validation, `formatCompanyTime`/`formatCompanyDate` calls and props unchanged). Accepted: BookingForm pre-confirm summary (display-only; day label via `Date.UTC` + `timeZone: "UTC"`, time via `formatCompanyTime`); SlotPicker `aria-pressed`; MonthCalendar reservation chips `div` -> `button`; ReservationDetails header close button (`aria-label="Close details"`) and `StatusBadge` for status. Checks: `npm test -- --run` 22 files / 136 tests passed; `npm run lint` clean; `npx tsc --noEmit` exit 0; `npm run build` exit 0.
- 2026-10-01: T1 review (RDD, medium, consent granted): 1 lens (reliability) approved and acknowledged, lineage `review-d3dea816fc02186a`. Two informational suggestions folded into T5.
- 2026-10-01: T4 done in `2fc264f` (delegated writer). All 20 pages applied; hooks, mutations, role guards, redirects, 404/retry and confirmation flows unchanged. One repo-wins fix: history keeps a level-3 heading per reservation (`historyPage.test.tsx` queries `heading` level 3; `ReservationRow` wraps titles in `<p>`, so the page passes `<span role="heading" aria-level={3}>`). Dropped an unused `Button` import from the landing page. Accepted additions: login/register `FormError` (`role="alert"`), favorite/remove icon buttons gain `aria-label`, derived display-only counts (customer Active/Cancelled, owner pending), owner "Create Company" link and quick-action links to existing routes.
- 2026-10-01: T5 done. (1) `eb48e17`: customer calendar switched to `palette="themed"` (the light palette hardcodes `bg-white`/`bg-gray-50` cells, so dark mode showed near-white text on white; owner already used `themed`). (2) History "Date:/Time:" merge kept: `historyPage.test.tsx` passes. (3) `d07827f`: new tests for `StatusBadge` (known statuses vs `getStatusClasses` fallback), `Sidebar` (`aria-current`, sub-page match, prefix sibling not matched) and `BookingForm` picked-day label (same calendar day under `TZ` UTC-10 / UTC+14 / UTC). (4) Known limitation: `viewport.themeColor` follows `prefers-color-scheme`, not the class-based app theme; a correct fix also needs to update the meta on toggle in `ThemeProvider` (out of scope), so behavior is unchanged. Also noted: `formatPickedDay` uses the browser default locale (e.g. es-AR "lun, 7 ene") while other dates use `en-US`; candidate follow-up. Checks: `npm test -- --run` 25 files / 153 tests passed; `npm run lint` clean; `npx tsc --noEmit` exit 0; `npm run build` exit 0; `git diff --stat main..HEAD -- src/hooks src/lib src/providers src/proxy.ts` empty.
- 2026-10-01: slices 2+3 review (RDD, medium, consent granted, range `24f598e..d29999a`): reliability lens approved and acknowledged, lineage `review-b3e476b517e8653f`. Sidebar active-path (WARNING) and picked-day format (SUGGESTION) test gaps covered by `d07827f`.
- 2026-10-01: `98ccc36` (inline): booking summary day pinned to `en-US`, resolving the locale follow-up above.
- 2026-10-01: slice 4 review (RDD, medium, consent granted, range `d29999a..98ccc36`): reliability lens approved and acknowledged, lineage `review-ed338f5de7d8bc8f`. Two WARNINGs on the TZ test (restoring an unset `TZ` wrote the string "undefined"; the expected label reused the implementation's formatting, so it was tautological) fixed inline in `4be7fd8` (literal "Mon, Jan 7", `delete process.env.TZ` when originally unset). Checks after the fix: 25 files / 153 tests passed, lint clean, `tsc --noEmit` exit 0.
- Open follow-ups (not blocking): untested derived counts on the customer dashboard and pending copy on owner reservations (review SUGGESTIONs); `ReservationRow` `titleAs` prop to replace the `role="heading"` span in history; `themeColor` vs class theme.

## Next step
Review and merge the 8 chained PRs bottom-up; then the open follow-ups.
