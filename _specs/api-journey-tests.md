# Feature Spec: API Integration Tests — Member & Admin Journeys

branch: claude/feature/api-journey-tests
spec: _specs/api-journey-tests.md
owner: TharinduGodage
date: 2026-09-28

---

## 0) Summary

**One-liner:** Automate two chained, end-to-end API integration test flows against the
Library Management API using Playwright (TypeScript, `request` fixture — no browser).
**Why now:** Task 2 of the "Test Cases & Automation" assignment — the identified
chained flows need to be automated and demoed live.
**Who is this for:** Internal — repo owner / assignment reviewer.
**Success looks like:** `npx playwright test` runs both journeys against a locally
running `Library.Api` (via the Aspire AppHost) and passes end-to-end, asserting status
codes and response bodies at each step.

---

## 1) Goals and Non-Goals

### Goals
- Automate the **Member journey**: Registration -> Login -> View Book details ->
  Borrow Book -> View Borrowings -> Return Book -> View Profile -> Update Profile.
- Automate the **Admin journey**: Admin Login -> Create Book -> Update Book ->
  Delete Book -> View all Borrowings -> View all Users -> View User by Id ->
  Delete User.
- Each journey chains real HTTP calls where one call's response (an id, a token) feeds
  the next — matching `docs/api-integration-test-cases.md`'s "chained" test-case style.
- Tests are self-contained and repeatable (create their own data, clean up after
  themselves) so they can run repeatedly against the same local database.

### Non-Goals
- Keycloak-based auth for these two journeys — the chain explicitly names
  "Registration" and "Login" as steps, which map to the legacy
  `POST /api/auth/register` / `POST /api/auth/login` endpoints (still functional,
  `Library.Api/Endpoints/AuthEndpoints.cs`), not Keycloak's hosted registration flow.
  (Deferred: a Keycloak-token variant, if wanted later, is a separate spec.)
- UI/Deskline test automation (tracked separately per the assignment).
- CI wiring (running these on every push) — out of scope until the manual demo happens.

---

## 2) User Workflow

### Primary workflow — Member journey
1. `POST /api/auth/register` — create a throwaway member (`Name`, `Email`,
   `PhoneNumber`, `Password`). Capture `id`.
2. `POST /api/auth/login` — capture bearer `token`.
3. `GET /api/books/{id}` — view details of a book (pre-provisioned via Admin in test
   setup — see 7) Edge Cases for why this is needed).
4. `POST /api/borrowings` `{BookId, MemberId}` — borrow it. Capture `borrowingId`.
5. `GET /api/members/{id}/borrowings` — view own borrowings; assert the new borrowing
   is present.
6. `POST /api/borrowings/{borrowingId}/return` — return it.
7. `GET /api/members/me` — view own profile.
8. `PUT /api/members/me` — update `Name`/`PhoneNumber`; re-fetch (or assert response
   body) to confirm the change persisted.

### Secondary workflow — Admin journey
1. `POST /api/auth/login` with the seeded `AdminSeed` account
   (`Library.Infrastructure/Data/AdminSeeder.cs`) — capture Admin `token`.
2. `POST /api/books` — create a book. Capture `id`.
3. `PUT /api/books/{id}` — update it.
4. `DELETE /api/books/{id}` — delete it; follow with `GET /api/books/{id}` -> `404` to
   confirm.
5. `GET /api/borrowings` — view all borrowings (Admin-only list).
6. **Setup step (added, not in the original chain):** `POST /api/members` — Admin
   creates one throwaway member record, so steps 7–8 have a deterministic target
   instead of depending on whatever member rows happen to already exist. Capture `id`.
7. `GET /api/members` — view all users; assert the throwaway member's `id` is present.
8. `GET /api/members/{id}` — view that user by id.
9. `DELETE /api/members/{id}` — delete that user.

---

## 3) S****l API Surface (What you expose)

N/A — no new application code/API surface. This spec covers only test automation
against the existing, already-documented endpoints (`docs/api-integration-test-cases.md`
has the full endpoint inventory: methods, routes, auth, status codes).

### 3.1 Test entry points (in place of a function surface)
- `tests/api/tests/member-journey.spec.ts` — one Playwright `test()` covering the full
  Member journey, with each chain step wrapped in `test.step(...)` for readable
  reporting.
- `tests/api/tests/admin-journey.spec.ts` — one Playwright `test()` covering the full
  Admin journey, same pattern.

---

## 4) Data Contracts (DF schemas + join keys)

N/A — no DataFrames. "Data contracts" here are the existing request/response DTOs,
already inventoried:
- `RegisterRequest` / `LoginRequest` / `LoginResponse` (`Library.Application/Contracts/Auth`)
- `CreateBookRequest` / `UpdateBookRequest` / `BookResponse` (`Library.Application/Contracts/Books`)
- `CreateBorrowingRequest` / `BorrowingResponse` (`Library.Application/Contracts/Borrowings`)
- `UpdateMemberRequest` / `MemberResponse` (`Library.Application/Contracts/Members`)

JSON responses are camelCase (ASP.NET Core default `System.Text.Json` casing) — e.g.
`availableCopies`, `phoneNumber`, `returnedAt`.

---

## 5) Business Rules

### 5.1 Auth
- Member journey: legacy register/login, dual-scheme JWT accepted by every policy.
- Admin journey: legacy login using the `AdminSeed:Email` / `AdminSeed:Password`
  credentials (already configured in `Library.Api`'s user secrets locally). Tests read
  these from `tests/api/.env` (`ADMIN_EMAIL`, `ADMIN_PASSWORD`), gitignored — never
  hardcoded into a committed spec file.

### 5.2 Ownership / authorization expectations exercised
- Member can view/update only their own profile (`/api/members/me`) and their own
  borrowings (`OwnMember`/`OwnBorrowing` policies) — the journey never needs to touch
  another member's data, so no 403 assertions are part of these two happy-path chains.
- Admin-only endpoints (`POST/PUT/DELETE /api/books`, `GET /api/borrowings`,
  `GET /api/members`, `DELETE /api/members/{id}`) require the `Admin` role, satisfied by
  the seeded admin account.

### 5.3 Test data isolation
- Every run generates unique emails/ISBNs via a timestamp+random suffix, so repeated
  runs never collide on `409 Conflict` (duplicate email/ISBN).
- Each spec cleans up what it created (`afterAll`/inline deletes) so the local database
  doesn't accumulate throwaway rows across runs.

---

## 6) Architecture (must match TECHNICAL_BLUEPRINT)

Adapted to this repo's actual layout (no DataFrame `clients/`/`constants/`/`utils/`
pipeline exists; the equivalent Playwright structure is):

### 6.1 `tests/api/utils/` (I/O boundary + pure helpers)
- `auth.ts` — `registerMember()`, `login()`, `authHeaders()`: wraps the two auth HTTP
  calls and header construction so both spec files share one implementation.
- `types.ts` — TypeScript interfaces mirroring the API's response DTOs (camelCase).

### 6.2 `tests/api/playwright.config.ts` (config, not "constants" in the DF sense)
- `baseURL` from `BASE_URL` env var (default `https://localhost:7282`).
- `ignoreHTTPSErrors: true` (self-signed local dev cert).

### 6.3 `tests/api/tests/*.spec.ts` (orchestration layer)
- Each spec: `beforeAll` (only where setup data is needed, e.g. Member journey's
  pre-provisioned book), the chained `test()` with `test.step()` per chain link,
  `afterAll` cleanup.
- No shared/global state between the two spec files — both provision and tear down
  their own data independently so they can run in parallel (`fullyParallel: true`).

---

## 7) Edge Cases

- **Member journey needs a book to view/borrow, but "View Book details" isn't preceded
  by a "Create Book" step in the given chain.** Resolved by having the Member spec's
  `beforeAll` log in as Admin (reusing the same `AdminSeed` credentials as the Admin
  journey) purely to create one throwaway book, and deleting it in `afterAll`. This is
  test setup, not part of the asserted chain itself.
- **Admin journey's "View User by Id"/"Delete User" need a concrete, disposable user.**
  Resolved by inserting a `POST /api/members` (Admin-created member, no password/login
  needed since it's never authenticated as) immediately before "View all Users" — see
  Primary Workflow step 6 above.
- HTTPS-only: hitting the HTTP port (`5281`) causes `UseHttpsRedirection()` to 307 and
  strip the `Authorization` header on languages/clients that follow redirects — Playwright's
  `APIRequestContext` does follow redirects by default, so tests must point `baseURL` at
  the HTTPS port directly to avoid silently-unauthenticated requests.
- `AdminSeed` not configured locally -> Admin journey fails at login with `401`; this is
  a local setup precondition, not a test bug (documented in `.env.example`).

---

## 8) Acceptance Criteria

- [ ] Member journey spec passes end-to-end against a locally running `Library.Api`.
- [ ] Admin journey spec passes end-to-end against a locally running `Library.Api`.
- [ ] Both specs clean up all data they create (verified by re-running twice in a row
      without any `409 Conflict` from leftover rows).
- [ ] Each chain step is a separate `test.step()` so a failure clearly identifies which
      link in the chain broke.
- [ ] No hardcoded secrets committed — `ADMIN_EMAIL`/`ADMIN_PASSWORD` come from a
      gitignored `.env`.

---

## 9) Test Plan

### 9.1 Golden fixture (required)
N/A — no static fixture dataset; each run generates and tears down its own live data
against the running API + Postgres.

### 9.2 Unit tests (utils/)
N/A — `utils/auth.ts` is exercised implicitly by both specs; not unit-tested in
isolation (thin HTTP wrappers, low value in isolating further for this scope).

### 9.3 Integration tests (orchestration)
This spec's two `.spec.ts` files **are** the integration tests:
- Happy path only for both journeys (matches the assignment's ask for automating the
  identified valuable flows, not a full negative-case matrix — those are covered
  separately by `scripts/test-endpoints.ps1` and the reserve candidates in
  `docs/api-integration-test-cases.md`).

---

## 10) References Used (required)

- `docs/api-integration-test-cases.md`: original chained test-case inventory (API-01
  through API-04) this spec supersedes/narrows to the two journeys the user actually
  wants automated first.
- `docs/playwright-api-automation-plan.md`: earlier Keycloak-based automation
  blueprint — superseded for these two journeys by the legacy register/login approach
  (see Non-Goals), since the given chain explicitly names "Registration"/"Login" as
  steps.
- `scripts/test-endpoints.ps1`: reference implementation of the same endpoints/flows in
  PowerShell; confirms request/response shapes and status codes used here.
- `Library.Api/Endpoints/*.cs`, `Library.Application/Contracts/**`: endpoint and DTO
  inventory.

---

## 11) Open Questions

- Q: Should the Admin journey's "View all Borrowings" / "View all Users" steps assert
  anything beyond `200` + array type (e.g. that counts increased by an expected
  amount), given other tests/data may exist concurrently in the same local database?

  Trying to assert counts is brittle (other tests may run in parallel, or the local
  database may already have seeded data). The assignment's "chained" framing doesn't
  explicitly require asserting counts, so the spec defaults to asserting only status
  codes + presence of the specific created record (throwaway member) in the array.

  Decision owner / date: TharinduGodage — default to the weaker assertion (status +
  presence of the specific created record) unless told otherwise.

- Q: Keep both journeys as single long `test()` chains (current plan, matches "chained"
  framing) vs. splitting into multiple smaller `test()`s within the same `describe`
  sharing `beforeAll`-created state? 
  
  splitting into multiple `test()`s would allow Playwright to run them in parallel, 
  but the assignment's "chained" framing implies a single linear flow. 
  The spec defaults to a single chained `test()` per journey unless told otherwise.

  
  Decision owner / date: TharinduGodage — default to
  single chained `test()` per journey per the assignment's explicit "chained" wording.
