# Keycloak headless admin token — outstanding fixes & test plan

**Status: all items resolved and verified end-to-end.** `GET
/api/keycloak-whoami` (JIT provisioning) and `GET /api/members`
(`AdminOnly`-gated) both returned `200` for `admin@gmail.com` using a real
Keycloak-issued token — see "Once all three items above are resolved" at the
bottom for the confirming evidence.

Working notes from debugging `postman/keycloak-admin-token.postman_collection.json`
against the `library` realm. Covers what was wrong, what's already fixed, what's
still outstanding, and the exact steps to finish and verify.

Test user throughout: `admin@gmail.com` (id `cae502eb-6f82-4874-a243-06d673ce836c`),
member of the `Library Admins` group. Client used for the password grant:
`library-test-cli` (confidential, direct-access-grants client — see
`docs/keycloak-authserver-phase4-prep-test-tooling.md`).

## Already fixed

- **Wrong client used for the password grant.** Early runs pointed `testClientId`
  at `admin-cli` (Postman Environment/Global variable shadowing the collection
  variable) and later at `library-flutter` (which has
  `directAccessGrantsEnabled: false` by design — never enable password grants on
  it). Fixed by setting the collection variable `testClientId`'s **current value**
  to `library-test-cli` and clearing/checking for conflicting Environment/Global
  variables of the same name.
- **`Admin` role missing from `realm_access.roles`.** `admin@gmail.com` gets admin
  access via membership in the `Library Admins` **group**, not a direct
  user-role mapping — that's why
  `GET /admin/realms/library/roles/Admin/users` (which only returns *direct*
  role holders) came back empty. The group's Role mapping tab had the *wrong*
  role assigned (`realm-admin`, a client role on Keycloak's own
  `realm-management` client — easy to pick by mistake in the same dialog as the
  real `Admin` realm role). Fixed by assigning the group the realm role `Admin`
  instead. Confirmed via decoded token: `realm_access.roles` now includes
  `Admin`.
- **`Admin` role composited `realm-admin`.** Even after fixing the group, tokens
  still carried the full `realm-admin` permission set in
  `resource_access.realm-management.roles`, because the realm role `Admin`
  itself had `realm-admin` nested under its **Associated roles** tab (visible
  as `Composite: True` on the role). Removed `realm-admin` from `Admin`'s
  Associated roles.

## Still outstanding

### 1. `realm-admin` is *still* showing up in freshly issued tokens

Every token decoded so far — even the most recent one, issued after fixing both
the group's role mapping and `Admin`'s Associated roles — still has the full
`realm-admin` composite set in `resource_access.realm-management.roles`. Since
the group-level and role-composite sources are now ruled out, the remaining
place this can come from is a **direct role assignment on the user itself**,
likely left over from very early troubleshooting (before the `Library Admins`
group was identified as the actual mechanism in use).

**Action:**
1. Keycloak console → `library` realm → **Users → admin@gmail.com → Role
   mapping** tab (the user's own tab, not Groups, not the `Admin` role's page).
2. Look for `realm-admin` (shown with client `realm-management`) in the
   directly-assigned list (not "Inherited").
3. If present, select it and **Unassign**.
4. Get a fresh token (see "How to get a fresh token" below) and decode it.
   `resource_access` should no longer contain a `realm-management` entry with
   the full admin permission set (`manage-users`, `manage-realm`,
   `impersonation`, `create-client`, etc.).

If it's *not* on the user's direct tab either, stop and report back rather than
guessing further — at that point something less obvious is going on (e.g. a
second group, a client scope's default role mappers, or a stale Keycloak
session/cache) and it's worth inspecting
`GET /admin/realms/library/users/{id}/role-mappings` directly via the admin
REST API for the full picture rather than the console UI.

**Update:** confirmed via the console — `admin@gmail.com`'s own Role mapping
tab shows every `realm-management` role as `Inherited: True` with the
checkboxes disabled, so there was nothing to unassign there; the direct-user
theory is ruled out. A quick manual re-run of `roles/Admin/composites` also
didn't turn up a result before the master token expired. Rather than keep
chasing this one call at a time through the console/Postman-by-hand, use
`postman/keycloak-role-diagnostics.postman_collection.json` — it bundles this
exact set of read-only Admin REST API checks (role composites for `Admin` and
`default-roles-library`, the `Library Admins` group's own role-mappings, the
user's group memberships, and the user's direct vs. full effective
role-mappings) so the whole picture comes back in one pass instead of
one-off requests. Run request 1, then 2–8 in any order; each logs its result
to the Postman console. Start with request 2 (`Admin` role's composites) since
that's still the leading theory — a `realm-admin` removal that didn't
actually persist.

**Root cause found (via the diagnostics collection):** request 2
(`GET /admin/realms/library/roles/Admin/composites`) shows `Admin` directly
associated with 17 individual `realm-management` client roles
(`view-identity-providers`, `query-users`, `manage-events`, `manage-realm`,
`manage-users`, `impersonation`, `view-users`, `view-realm`,
`manage-clients`, `create-client`, `view-authorization`, `query-groups`,
`view-clients`, `query-clients`, `manage-identity-providers`, `view-events`,
`query-realms`) — `realm-admin` itself is genuinely gone, but at some point
during earlier troubleshooting these roles got ticked individually onto
`Admin`'s Associated roles tab as well, separately from the `realm-admin`
umbrella. Removing just the umbrella role never touched them. Request 7
(user's direct realm role-mappings — only `default-roles-library`, no
`Admin`) and request 3 (`default-roles-library` composites — clean) both
confirm this is the only remaining source.

**Action:**
1. Keycloak console → **Realm roles → Admin → Associated roles**.
2. Use the filter dropdown to filter by client **realm-management** (don't
   scan by eye for `realm-admin` — that role is already gone; the filter
   surfaces every individually-ticked role from that client instead).
3. Select all of them and **Unassign** in bulk.
4. Leave the `broker`/`account` entries (`read-token`, `view-profile`,
   `manage-account`, etc.) alone — those are harmless self-service roles
   every user already gets via `default-roles-library`, not admin
   permissions.
5. Re-run request 2 in the diagnostics collection to confirm no
   `realm-management` roles remain under `Admin`.
6. Get a fresh token (see below) and decode it — `resource_access` should no
   longer contain a `realm-management` entry at all.

**Update — `Admin`'s composites are clean, but the leak is still there, from
a second independent source.** Confirmed via the console: `Admin`'s
Associated roles tab now only lists harmless `account`/`broker` roles
(`Inherited: False` — intentional, directly assigned), no more
`realm-management`. A fresh `roles/Admin/composites` call matches. But
`admin@gmail.com`'s own Role mapping tab still shows every `realm-management`
role as `Inherited: True`. Since `Admin` is now provably clean, this can no
longer be coming from `Admin` — it points at diagnostics request 5
(`GET /groups/{id}/role-mappings` for `Library Admins`), which earlier showed
`clientMappings.realm-management` with the same 17 roles. That endpoint
returns *direct* mappings only (it doesn't expand composites), so those roles
are ticked **directly on the `Library Admins` group itself**, independent of
the `Admin` realm role the group also holds — almost certainly another
leftover from the same early-troubleshooting phase, just on the group
instead of the role this time.

**Action:**
1. Console → **Groups → Library Admins → Role mapping** tab.
2. Filter by client **realm-management**.
3. Select all of them and **Unassign** — leave only the realm role `Admin`
   assigned to the group.
4. Get a fresh token and decode it — `resource_access` should finally have
   no `realm-management` entry.
5. If it's *still* there after this, re-run the full diagnostics collection
   (all 8 requests) rather than guessing at a third source — at that point
   check the raw realm export (`keycloak/import/library-realm.json`) for a
   `clientRoleMappings` block under the `Library Admins` group definition
   that might be reapplying on every container restart, since this realm's
   live state is known to have drifted from that file more than once this
   session (see the `aud`/audience-mapper note below).

**Resolved.** Re-ran the full diagnostics collection after the group cleanup:
`Admin`'s composites (request 2), `default-roles-library`'s composites
(request 3), and — the one that mattered — the `Library Admins` group's own
role-mappings (request 5) are all clean now: `realmMappings: [Admin]` only,
`clientMappings` down to just harmless `broker`/`account` self-service roles,
no `realm-management` anywhere. The user's direct role-mappings (request 7)
remain just `default-roles-library`, as expected.

Confirmed end-to-end via a real `library`-realm access token for
`admin@gmail.com` (step 6 of
`postman/keycloak-admin-token.postman_collection.json`, decoded):
`resource_access` is down to just `broker` (`read-token`) and `account`
(self-service roles), no `realm-management` entry, and `realm_access.roles`
correctly includes `Admin` with no `realm-admin` leakage. **Issue fully
closed.**

As a side effect, `aud` also dropped `realm-management` (Keycloak only
includes a client in `aud` when a role mapping for it exists) — expected,
and it doesn't affect issue 2 below since `library-flutter` was never in
`aud` to begin with.

### 2. `aud` (audience) claim is missing `library-flutter`

Every token so far has had `"aud"` without `library-flutter` (most recently
`["broker", "account"]`, previously also `"realm-management"` before issue 1
was fixed). `Library.Api` requires `ValidAudience = "library-flutter"` with
`ValidateAudience = true` (`Library.Api/Program.cs:65`,
`Library.Api/appsettings.json:24`). Without `library-flutter` in `aud`, the API
will reject the token with `401` regardless of the role fixes above.

The realm import file (`keycloak/import/library-realm.json:77-89`) defines an
`audience-mapper` protocol mapper directly on `library-test-cli` that's
supposed to inject `library-flutter` into the token audience — it isn't taking
effect on the running realm, which means the running Keycloak instance's
`library-test-cli` client predates that mapper being added (realm import only
runs on first boot, not on every container start/restart).

**Action:**
1. Keycloak console → `library` realm → **Clients → library-test-cli → Client
   scopes → library-test-cli-dedicated → Mappers**.
2. Check whether a mapper named `audience-mapper` (type: Audience) exists.
3. If missing, add one: **Add mapper → By configuration → Audience** →
   - Included Client Audience: `library-flutter`
   - Add to ID token: off
   - Add to access token: **on**
   - Save.
4. Get a fresh token and decode it — `aud` should now include
   `"library-flutter"` alongside the existing entries.

**Resolved.** Fresh token decodes to `"aud": ["library-flutter", "broker",
"account"]`. All three checks from "How to get a fresh token" below now
pass: `Admin` in `realm_access.roles`, no `realm-management` in
`resource_access`, `library-flutter` in `aud`.

Longer-term (not urgent, just noted): this realm's live state had drifted from
`keycloak/import/library-realm.json` more than once during this session (the
group, and this mapper). The `audience-mapper` and the clean `Admin` role were
already correct in the file — the running container was just created before
they were added, so it never picked them up. The `Library Admins` group was
missing from the file entirely; added it (`realmRoles: ["Admin"]`, matching
the live cleanup above) so a future reimport recreates it correctly. Note the
file still has no `"users"` section, so a real reimport (wiping the Keycloak
volume) would still require manually re-adding `admin@gmail.com` to the
group afterward. Actually re-importing/recreating the container from the
current file is still an infra action to do manually when convenient, not
scripted automatically.

### 3. Postman `whoami` request has the wrong Authorization type

The `Library API > Auth > Login Copy` request (`GET {{base_url}}/whoami`) has
its **Authorization** tab set to Auth Type **"JWT Bearer"** with
Algorithm/Secret/Payload fields. That Postman auth type *generates and signs a
brand-new JWT itself* (HS256, shared secret) — it does not attach an
already-issued bearer token. Sending this request as configured will ignore
the real Keycloak `access_token` entirely and send a bogus self-signed token,
which `Library.Api` will reject no matter what the role/audience fixes above
achieve.

**Action:**
1. Open the request → **Authorization** tab.
2. Change **Auth Type** to **Bearer Token**.
3. Paste the current `access_token` value into the **Token** field (or bind it
   to a collection/environment variable, e.g. `{{adminAccessToken}}`, if
   chaining from `postman/keycloak-admin-token.postman_collection.json`).
4. Repeat this check for any other request in the `Library API` collection
   that needs to carry this token (e.g. the `api/members` request for Test 3).

**Resolved.** `GET /api/keycloak-whoami` and `GET /api/members` both
returned `200` with the real access token attached, confirming this got
fixed alongside the other requests — see "Once all three items above are
resolved" below for the response details.

## How to get a fresh token

For debugging *where a role comes from* rather than minting a token, use
`postman/keycloak-role-diagnostics.postman_collection.json` instead (see
"Update" note above).

Using `postman/keycloak-admin-token.postman_collection.json`:
- If nothing about the client/user has changed since your last full run, you
  don't need to redo every step — just re-run **step 3** (reset password, only
  needed if you're not sure of the current password) and **step 6** (password
  grant) using the already-known `adminUserId`, `testClientInternalId`, and
  `testClientSecret` collection variables.
- If you're unsure of any of those, run the whole collection top to bottom
  (steps 1–6).
- `masterToken` (step 1) expires in 60s — if steps 2–5 take longer than that,
  re-run step 1 first.

To decode and inspect a token without calling the API, drop the middle
(payload) segment of the JWT — the part between the two `.` separators — into
any base64url JSON decoder, or use this PowerShell one-liner:

```powershell
$token = '<paste the middle segment of the access_token here>'
$mod = $token.Length % 4
if ($mod -ne 0) { $token += ('=' * (4 - $mod)) }
$bytes = [Convert]::FromBase64String($token.Replace('-','+').Replace('_','/'))
[System.Text.Encoding]::UTF8.GetString($bytes)
```

Check for:
- `realm_access.roles` contains `"Admin"`.
- `resource_access` has no `realm-management` entry with the full admin
  permission set (or no `realm-management` entry at all).
- `aud` contains `"library-flutter"`.

## Once all three items above are resolved

Follow `docs/keycloak-manual-testing-postman-guide.md`, "Triggering JIT
provisioning" and "Test 3 — Admin role (`AdminOnly`)" sections:

1. **Trigger JIT provisioning** (creates the `Members` row for this Keycloak
   identity — `MemberProvisioningMiddleware` runs on every authenticated
   request):
   ```
   GET https://localhost:7282/api/keycloak-whoami
   Authorization: Bearer <access_token>
   ```
   Expect `200`. Optionally confirm the row exists:
   ```sql
   SELECT "Id", "Name", "Email", "KeycloakId" FROM "Members" WHERE "Email" = 'admin@gmail.com';
   ```

2. **Call the `AdminOnly`-gated endpoint:**
   ```
   GET https://localhost:7282/api/members
   Authorization: Bearer <access_token>
   ```
   Expect `200` with the full member list. This is the actual proof that
   `AdminOnly` recognizes the `Admin` realm role coming through Keycloak
   end-to-end.

If either call still fails, capture the exact status code + response body and
re-decode the token used — most likely culprit at that point would be a token
that's already expired (`access_token` from this collection lives 300s) rather
than a new role/audience issue.

**Confirmed working (2026-09-18).** Both calls returned `200` for
`admin@gmail.com` using a real Keycloak-issued `library-test-cli` token:
- `GET /api/keycloak-whoami` — JIT provisioning ran; response claims show
  `Admin` correctly mapped into ASP.NET's `ClaimTypes.Role`
  (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`), and
  `library-flutter` present in `aud`.
- `GET /api/members` — `200` with the full member list, including the
  provisioned `Members` row for `admin@gmail.com` (`Admin 1`). This is the
  end-to-end proof that `AdminOnly` recognizes the `Admin` realm role coming
  through Keycloak with no legacy-auth fallback involved.

All four "Still outstanding" items above are resolved. Headless admin
token flow, `Admin` role attribution, `library-flutter` audience, and the
Postman `whoami`/`api/members` requests are all working correctly.
