<#
Automated authorization-matrix smoke test for the Library API.
Covers auth, books, members, and borrowings across: no token / Member A (self) /
Member A (on Member B's data) / Admin. Creates its own test data and cleans up after itself.

Also covers the same coexistence-window matrix using Keycloak-issued tokens (see the
"Keycloak coexistence" section) — creates its own throwaway Keycloak users via the Admin
REST API and a dedicated `library-test-cli` client (auto-created on first run; see
docs/keycloak-authserver-phase4-prep-test-tooling.md for why a separate client instead of
enabling password grants on the real `library-flutter` client). Cleans those up too.

Usage:
    $env:ADMIN_EMAIL = "admin@example.com"
    $env:ADMIN_PASSWORD = "..."
    $env:KEYCLOAK_ADMIN = "admin"
    $env:KEYCLOAK_ADMIN_PASSWORD = "..."
    .\scripts\test-endpoints.ps1 [-BaseUrl "https://localhost:7282"] [-KeycloakBaseUrl "http://localhost:8081"] [-SkipKeycloak]

If ADMIN_EMAIL / ADMIN_PASSWORD / KEYCLOAK_ADMIN / KEYCLOAK_ADMIN_PASSWORD are not set, you'll
be prompted for them. Pass -SkipKeycloak to run only the legacy-auth matrix (e.g. Keycloak isn't
running locally right now).

NOTE: Must use the HTTPS base URL, not HTTP. The API's UseHttpsRedirection() 307-redirects
HTTP requests to HTTPS, and both curl and PowerShell's web client strip the Authorization
header when following a redirect to a different port — every authenticated call silently
loses its Bearer token and comes back 401 if you point this at the HTTP port. Confirmed
2026-08-27 (see docs/keycloak-session-2026-08-27-summary.md).
#>

param(
    [string]$BaseUrl = "https://localhost:7282",
    [string]$KeycloakBaseUrl = "http://localhost:8081",
    [string]$KeycloakRealm = "library",
    [string]$KeycloakTestClientId = "library-test-cli",
    [switch]$SkipKeycloak
)

$script:results = @()

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$Token = $null,
        # Escape hatch for calling a server other than $BaseUrl (Keycloak's own endpoints) —
        # when set, this is used verbatim instead of "$BaseUrl$Path".
        [string]$FullUri = $null,
        # Keycloak's token/admin-token endpoints take form-urlencoded, not JSON.
        [string]$ContentType = "application/json"
    )

    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method          = $Method
        Uri             = if ($FullUri) { $FullUri } else { "$BaseUrl$Path" }
        Headers         = $headers
        UseBasicParsing = $true
        ErrorAction     = "Stop"
    }
    if ($null -ne $Body) {
        if ($ContentType -eq "application/x-www-form-urlencoded") {
            $params["Body"] = $Body
        } else {
            # -InputObject (not the pipeline) is required here: piping a single-element array
            # into ConvertTo-Json unrolls it back to a bare object instead of a JSON array,
            # which breaks callers that need e.g. Keycloak role-mapping's `[{...}]` body shape.
            $params["Body"] = (ConvertTo-Json -InputObject $Body -Depth 10)
        }
        $params["ContentType"] = $ContentType
    }

    try {
        $response = Invoke-WebRequest @params
        $parsedBody = $null
        if ($response.Content) {
            try { $parsedBody = $response.Content | ConvertFrom-Json } catch { $parsedBody = $response.Content }
        }
        return [PSCustomObject]@{ StatusCode = [int]$response.StatusCode; Body = $parsedBody }
    }
    catch [System.Net.WebException] {
        # Windows PowerShell 5.1 throws a terminating WebException for any non-2xx response
        # (no -SkipHttpErrorValidation like PS7) — this is the normal path for 401/403/404 asserts.
        $webResponse = $_.Exception.Response
        if ($null -eq $webResponse) {
            Write-Host "  ! Request failed (no response): $($_.Exception.Message)" -ForegroundColor Yellow
            return [PSCustomObject]@{ StatusCode = 0; Body = $null }
        }
        $statusCode = [int]$webResponse.StatusCode
        $stream = $webResponse.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $content = $reader.ReadToEnd()
        $reader.Close()
        $parsedBody = $null
        if ($content) {
            try { $parsedBody = $content | ConvertFrom-Json } catch { $parsedBody = $content }
        }
        return [PSCustomObject]@{ StatusCode = $statusCode; Body = $parsedBody }
    }
    catch {
        Write-Host "  ! Unexpected error calling $Method $Path : $($_.Exception.Message)" -ForegroundColor Yellow
        return [PSCustomObject]@{ StatusCode = -1; Body = $null }
    }
}

function Assert-Status {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [int]$Expected,
        [Parameter(Mandatory)] [int]$Actual
    )
    $pass = ($Expected -eq $Actual)
    $script:results += [PSCustomObject]@{ Test = $Name; Expected = $Expected; Actual = $Actual; Result = if ($pass) { "PASS" } else { "FAIL" } }
    $color = if ($pass) { "Green" } else { "Red" }
    $label = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host ("  [{0}] {1} (expected {2}, got {3})" -f $label, $Name, $Expected, $Actual) -ForegroundColor $color
}

function Assert-True {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [bool]$Condition
    )
    $script:results += [PSCustomObject]@{ Test = $Name; Expected = $true; Actual = $Condition; Result = if ($Condition) { "PASS" } else { "FAIL" } }
    $color = if ($Condition) { "Green" } else { "Red" }
    $label = if ($Condition) { "PASS" } else { "FAIL" }
    Write-Host ("  [{0}] {1}" -f $label, $Name) -ForegroundColor $color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "== $Title ==" -ForegroundColor Cyan
}

# --- Keycloak helpers (Admin REST API + token endpoints) ---
# See docs/keycloak-authserver-phase4-prep-test-tooling.md for why this uses a dedicated
# "library-test-cli" client rather than enabling password grants on the real library-flutter one.

function Get-KeycloakAdminToken {
    param([Parameter(Mandatory)] [string]$Username, [Parameter(Mandatory)] [string]$Password)

    # master realm's built-in "admin-cli" client has Direct Access Grants on by default in
    # stock Keycloak — no realm/client setup needed for this call.
    # Built via -join, not string interpolation with literal "&": Windows PowerShell 5.1's
    # parser misreads an "&" placed immediately next to a "$(...)"/"$var" interpolation inside
    # a double-quoted string ("the & operator is reserved for future use").
    $body = @(
        "grant_type=password"
        "client_id=admin-cli"
        "username=$([uri]::EscapeDataString($Username))"
        "password=$([uri]::EscapeDataString($Password))"
    ) -join [char]38
    $r = Invoke-Api -Method POST -FullUri "$KeycloakBaseUrl/realms/master/protocol/openid-connect/token" -Body $body -ContentType "application/x-www-form-urlencoded"
    if ($r.StatusCode -ne 200) { throw "Could not get Keycloak admin token (status $($r.StatusCode)): $($r.Body | Out-String)" }
    return $r.Body.access_token
}

function Ensure-KeycloakTestClient {
    param([Parameter(Mandatory)] [string]$AdminToken)

    $existing = Invoke-Api -Method GET -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/clients?clientId=$KeycloakTestClientId" -Token $AdminToken
    # Wrap in @() before checking .Count: Windows PowerShell 5.1's ConvertFrom-Json silently
    # unwraps a single-element JSON array into a bare object, which would make .Count $null here
    # and break idempotency on every run after the first (would try to re-create -> 409).
    $existingClients = @($existing.Body)
    if ($existing.StatusCode -eq 200 -and $existingClients.Count -gt 0) {
        $clientObj = $existingClients[0]
        Write-Host "  Keycloak test client '$KeycloakTestClientId' already exists — reusing it" -ForegroundColor Gray
    }
    else {
        $createBody = @{
            clientId                  = $KeycloakTestClientId
            name                      = "Library automated test tooling"
            protocol                  = "openid-connect"
            publicClient              = $false
            standardFlowEnabled       = $false
            directAccessGrantsEnabled = $true
            implicitFlowEnabled       = $false
            serviceAccountsEnabled    = $false
            protocolMappers           = @(
                @{
                    name            = "audience-mapper"
                    protocol        = "openid-connect"
                    protocolMapper  = "oidc-audience-mapper"
                    consentRequired = $false
                    config          = @{
                        "included.client.audience" = "library-flutter"
                        "id.token.claim"            = "false"
                        "access.token.claim"        = "true"
                    }
                }
            )
        }
        $create = Invoke-Api -Method POST -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/clients" -Body $createBody -Token $AdminToken
        if ($create.StatusCode -ne 201) { throw "Could not create Keycloak test client (status $($create.StatusCode)): $($create.Body | Out-String)" }
        Write-Host "  Created Keycloak test client '$KeycloakTestClientId'" -ForegroundColor Gray

        $lookup = Invoke-Api -Method GET -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/clients?clientId=$KeycloakTestClientId" -Token $AdminToken
        $clientObj = $lookup.Body[0]
    }

    $secretResp = Invoke-Api -Method GET -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/clients/$($clientObj.id)/client-secret" -Token $AdminToken
    if ($secretResp.StatusCode -ne 200) { throw "Could not fetch Keycloak test client secret (status $($secretResp.StatusCode))" }
    return $secretResp.Body.value
}

function New-KeycloakUser {
    param(
        [Parameter(Mandatory)] [string]$AdminToken,
        [Parameter(Mandatory)] [string]$Email,
        [Parameter(Mandatory)] [string]$FirstName,
        [Parameter(Mandatory)] [string]$Password
    )

    $body = @{
        username      = $Email
        email         = $Email
        firstName     = $FirstName
        enabled       = $true
        emailVerified = $true
        credentials   = @(@{ type = "password"; value = $Password; temporary = $false })
    }
    $create = Invoke-Api -Method POST -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/users" -Body $body -Token $AdminToken
    if ($create.StatusCode -ne 201) { throw "Could not create Keycloak user $Email (status $($create.StatusCode)): $($create.Body | Out-String)" }

    $lookupQuery = @(
        "email=$([uri]::EscapeDataString($Email))"
        "exact=true"
    ) -join [char]38
    $lookup = Invoke-Api -Method GET -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/users?$lookupQuery" -Token $AdminToken
    if ($lookup.StatusCode -ne 200 -or -not $lookup.Body -or $lookup.Body.Count -eq 0) { throw "Created Keycloak user $Email but could not look it up afterward" }
    return $lookup.Body[0].id
}

function Add-KeycloakRealmRole {
    param(
        [Parameter(Mandatory)] [string]$AdminToken,
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [string]$RoleName
    )

    $role = Invoke-Api -Method GET -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/roles/$RoleName" -Token $AdminToken
    if ($role.StatusCode -ne 200) { throw "Could not look up Keycloak realm role '$RoleName' (status $($role.StatusCode))" }

    $assign = Invoke-Api -Method POST -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/users/$UserId/role-mappings/realm" -Body @($role.Body) -Token $AdminToken
    if ($assign.StatusCode -ne 204) { throw "Could not assign role '$RoleName' to Keycloak user $UserId (status $($assign.StatusCode))" }
}

function Remove-KeycloakUser {
    param([Parameter(Mandatory)] [string]$AdminToken, [string]$UserId)

    if (-not $UserId) { return }
    $r = Invoke-Api -Method DELETE -FullUri "$KeycloakBaseUrl/admin/realms/$KeycloakRealm/users/$UserId" -Token $AdminToken
    if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted Keycloak user $UserId" -ForegroundColor Gray }
    else { Write-Host "  ! Could not delete Keycloak user $UserId (status $($r.StatusCode))" -ForegroundColor Yellow }
}

function Get-KeycloakUserToken {
    param(
        [Parameter(Mandatory)] [string]$ClientSecret,
        [Parameter(Mandatory)] [string]$Email,
        [Parameter(Mandatory)] [string]$Password
    )

    $body = @(
        "grant_type=password"
        "client_id=$KeycloakTestClientId"
        "client_secret=$([uri]::EscapeDataString($ClientSecret))"
        "username=$([uri]::EscapeDataString($Email))"
        "password=$([uri]::EscapeDataString($Password))"
        "scope=openid%20profile%20email"
    ) -join [char]38
    $r = Invoke-Api -Method POST -FullUri "$KeycloakBaseUrl/realms/$KeycloakRealm/protocol/openid-connect/token" -Body $body -ContentType "application/x-www-form-urlencoded"
    if ($r.StatusCode -ne 200) { throw "Could not get Keycloak token for $Email (status $($r.StatusCode)): $($r.Body | Out-String)" }
    return $r.Body.access_token
}

# --- Credentials ---

$adminEmail = $env:ADMIN_EMAIL
$adminPassword = $env:ADMIN_PASSWORD

if (-not $adminEmail) {
    $adminEmail = Read-Host "Admin email"
}
if (-not $adminPassword) {
    $securePwd = Read-Host "Admin password" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    $adminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

$keycloakAdmin = $env:KEYCLOAK_ADMIN
$keycloakAdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD

if (-not $SkipKeycloak) {
    if (-not $keycloakAdmin) {
        $keycloakAdmin = Read-Host "Keycloak admin username"
    }
    if (-not $keycloakAdminPassword) {
        $secureKcPwd = Read-Host "Keycloak admin password" -AsSecureString
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKcPwd)
        $keycloakAdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

$suffix = [guid]::NewGuid().ToString("N").Substring(0, 8)
$testPassword = "TestPass123!"

$testBookId = $null
$memberA = $null
$memberB = $null
$adminCreatedMemberId = $null
$borrowingA = $null
$borrowingB = $null

# Keycloak coexistence-section state (cleaned up in `finally` below)
$kcAdminUserId = $null
$kcMemberAUserId = $null
$kcMemberBUserId = $null
$kcAdminMemberId = $null
$kcMemberAId = $null
$kcMemberBId = $null

try {
    Write-Section "Auth setup"

    $regA = Invoke-Api -Method POST -Path "/api/auth/register" -Body @{
        Name = "Member A"; Email = "membera.$suffix@test.local"; PhoneNumber = "+15550000$suffix".Substring(0,15); Password = $testPassword
    }
    Assert-Status "Register Member A -> 201" -Expected 201 -Actual $regA.StatusCode
    $memberA = @{ Id = $regA.Body.id; Name = "Member A"; Email = "membera.$suffix@test.local"; PhoneNumber = "+15550000$suffix".Substring(0,15) }

    $regB = Invoke-Api -Method POST -Path "/api/auth/register" -Body @{
        Name = "Member B"; Email = "memberb.$suffix@test.local"; PhoneNumber = "+15550001$suffix".Substring(0,15); Password = $testPassword
    }
    Assert-Status "Register Member B -> 201" -Expected 201 -Actual $regB.StatusCode
    $memberB = @{ Id = $regB.Body.id; Email = "memberb.$suffix@test.local" }

    $regDup = Invoke-Api -Method POST -Path "/api/auth/register" -Body @{
        Name = "Dup"; Email = $memberA.Email; PhoneNumber = "+15559999999"; Password = $testPassword
    }
    Assert-Status "Register duplicate email -> 409" -Expected 409 -Actual $regDup.StatusCode

    $regInvalid = Invoke-Api -Method POST -Path "/api/auth/register" -Body @{
        Name = ""; Email = "notanemail"; PhoneNumber = ""; Password = "short"
    }
    Assert-Status "Register invalid body -> 400" -Expected 400 -Actual $regInvalid.StatusCode

    $loginAdmin = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ Email = $adminEmail; Password = $adminPassword }
    Assert-Status "Login Admin -> 200" -Expected 200 -Actual $loginAdmin.StatusCode
    $adminToken = $loginAdmin.Body.token

    $loginA = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ Email = $memberA.Email; Password = $testPassword }
    Assert-Status "Login Member A -> 200" -Expected 200 -Actual $loginA.StatusCode
    $tokenA = $loginA.Body.token

    $loginB = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ Email = $memberB.Email; Password = $testPassword }
    Assert-Status "Login Member B -> 200" -Expected 200 -Actual $loginB.StatusCode
    $tokenB = $loginB.Body.token

    $loginWrongPw = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ Email = $memberA.Email; Password = "wrong-password" }
    Assert-Status "Login wrong password -> 401" -Expected 401 -Actual $loginWrongPw.StatusCode

    if (-not $adminToken -or -not $tokenA -or -not $tokenB) {
        throw "Could not obtain all required tokens - aborting remaining tests."
    }

    Write-Section "Books"

    $noTokenBooks = Invoke-Api -Method GET -Path "/api/books"
    Assert-Status "GET /api/books no token -> 200" -Expected 200 -Actual $noTokenBooks.StatusCode

    $memberBooks = Invoke-Api -Method GET -Path "/api/books" -Token $tokenA
    Assert-Status "GET /api/books as Member -> 200" -Expected 200 -Actual $memberBooks.StatusCode

    $bookBody = @{ Title = "Test Book $suffix"; Author = "Test Author"; Isbn = "ISBN-$suffix"; PublishedYear = 2020; TotalCopies = 3 }

    $createBookAdmin = Invoke-Api -Method POST -Path "/api/books" -Body $bookBody -Token $adminToken
    Assert-Status "POST /api/books as Admin -> 201" -Expected 201 -Actual $createBookAdmin.StatusCode
    $testBookId = $createBookAdmin.Body.id

    $createBookMember = Invoke-Api -Method POST -Path "/api/books" -Body $bookBody -Token $tokenA
    Assert-Status "POST /api/books as Member -> 403" -Expected 403 -Actual $createBookMember.StatusCode

    $createBookNoToken = Invoke-Api -Method POST -Path "/api/books" -Body $bookBody
    Assert-Status "POST /api/books no token -> 401" -Expected 401 -Actual $createBookNoToken.StatusCode

    $getBookNoToken = Invoke-Api -Method GET -Path "/api/books/$testBookId"
    Assert-Status "GET /api/books/{id} no token -> 200" -Expected 200 -Actual $getBookNoToken.StatusCode

    $updateBookBody = @{ Title = "Test Book $suffix (updated)"; Author = "Test Author"; Isbn = "ISBN-$suffix"; PublishedYear = 2020; TotalCopies = 3 }

    $updateBookAdmin = Invoke-Api -Method PUT -Path "/api/books/$testBookId" -Body $updateBookBody -Token $adminToken
    Assert-Status "PUT /api/books/{id} as Admin -> 200" -Expected 200 -Actual $updateBookAdmin.StatusCode

    $updateBookMember = Invoke-Api -Method PUT -Path "/api/books/$testBookId" -Body $updateBookBody -Token $tokenA
    Assert-Status "PUT /api/books/{id} as Member -> 403" -Expected 403 -Actual $updateBookMember.StatusCode

    $updateBookNoToken = Invoke-Api -Method PUT -Path "/api/books/$testBookId" -Body $updateBookBody
    Assert-Status "PUT /api/books/{id} no token -> 401" -Expected 401 -Actual $updateBookNoToken.StatusCode

    $deleteBookMember = Invoke-Api -Method DELETE -Path "/api/books/$testBookId" -Token $tokenA
    Assert-Status "DELETE /api/books/{id} as Member -> 403" -Expected 403 -Actual $deleteBookMember.StatusCode

    Write-Section "Members"

    $listMembersAdmin = Invoke-Api -Method GET -Path "/api/members" -Token $adminToken
    Assert-Status "GET /api/members as Admin -> 200" -Expected 200 -Actual $listMembersAdmin.StatusCode

    $listMembersMember = Invoke-Api -Method GET -Path "/api/members" -Token $tokenA
    Assert-Status "GET /api/members as Member -> 403" -Expected 403 -Actual $listMembersMember.StatusCode

    $listMembersNoToken = Invoke-Api -Method GET -Path "/api/members"
    Assert-Status "GET /api/members no token -> 401" -Expected 401 -Actual $listMembersNoToken.StatusCode

    $createMemberBody = @{ Name = "Admin Created $suffix"; Email = "admargen.$suffix@test.local"; PhoneNumber = "+15550002000" }

    $createMemberAdmin = Invoke-Api -Method POST -Path "/api/members" -Body $createMemberBody -Token $adminToken
    Assert-Status "POST /api/members as Admin -> 201" -Expected 201 -Actual $createMemberAdmin.StatusCode
    $adminCreatedMemberId = $createMemberAdmin.Body.id

    $createMemberMember = Invoke-Api -Method POST -Path "/api/members" -Body $createMemberBody -Token $tokenA
    Assert-Status "POST /api/members as Member -> 403" -Expected 403 -Actual $createMemberMember.StatusCode

    $getOwnMemberA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)" -Token $tokenA
    Assert-Status "GET /api/members/{A} as Member A -> 200" -Expected 200 -Actual $getOwnMemberA.StatusCode

    $getOtherMemberA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)" -Token $tokenB
    Assert-Status "GET /api/members/{A} as Member B -> 403" -Expected 403 -Actual $getOtherMemberA.StatusCode

    $getMemberAAdmin = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)" -Token $adminToken
    Assert-Status "GET /api/members/{A} as Admin -> 200" -Expected 200 -Actual $getMemberAAdmin.StatusCode

    $getMemberANoToken = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)"
    Assert-Status "GET /api/members/{A} no token -> 401" -Expected 401 -Actual $getMemberANoToken.StatusCode

    $newName = "Member A Renamed $suffix"
    $updateOwnBody = @{ Name = $newName; Email = $memberA.Email; PhoneNumber = $memberA.PhoneNumber }
    $updateOwnMemberA = Invoke-Api -Method PUT -Path "/api/members/$($memberA.Id)" -Body $updateOwnBody -Token $tokenA
    Assert-Status "PUT /api/members/{A} as Member A -> 200" -Expected 200 -Actual $updateOwnMemberA.StatusCode
    Assert-True "PUT /api/members/{A} as Member A -> Name updated" -Condition ($updateOwnMemberA.Body.name -eq $newName)

    $updateOtherMemberA = Invoke-Api -Method PUT -Path "/api/members/$($memberA.Id)" -Body $updateOwnBody -Token $tokenB
    Assert-Status "PUT /api/members/{A} as Member B -> 403" -Expected 403 -Actual $updateOtherMemberA.StatusCode

    $deactivateBody = @{ Name = $newName; Email = $memberA.Email; PhoneNumber = $memberA.PhoneNumber; IsActive = $false }
    $deactivateMemberA = Invoke-Api -Method PUT -Path "/api/members/$($memberA.Id)" -Body $deactivateBody -Token $adminToken
    Assert-Status "PUT /api/members/{A} Admin sets IsActive=false -> 200" -Expected 200 -Actual $deactivateMemberA.StatusCode
    Assert-True "PUT /api/members/{A} Admin sets IsActive=false -> IsActive is false" -Condition ($deactivateMemberA.Body.isActive -eq $false)

    $selfReactivateName = "Member A Self-Reactivate Attempt $suffix"
    $selfReactivateBody = @{ Name = $selfReactivateName; Email = $memberA.Email; PhoneNumber = $memberA.PhoneNumber; IsActive = $true }
    $selfReactivateMemberA = Invoke-Api -Method PUT -Path "/api/members/$($memberA.Id)" -Body $selfReactivateBody -Token $tokenA
    Assert-Status "PUT /api/members/{A} self IsActive=true attempt -> 200 (rest of edit succeeds)" -Expected 200 -Actual $selfReactivateMemberA.StatusCode
    Assert-True "PUT /api/members/{A} self IsActive=true attempt -> Name still updated" -Condition ($selfReactivateMemberA.Body.name -eq $selfReactivateName)
    Assert-True "PUT /api/members/{A} self IsActive=true attempt -> IsActive silently ignored (still false)" -Condition ($selfReactivateMemberA.Body.isActive -eq $false)

    $reactivateBody = @{ Name = $selfReactivateName; Email = $memberA.Email; PhoneNumber = $memberA.PhoneNumber; IsActive = $true }
    $reactivateMemberA = Invoke-Api -Method PUT -Path "/api/members/$($memberA.Id)" -Body $reactivateBody -Token $adminToken
    Assert-Status "PUT /api/members/{A} Admin reactivates -> 200" -Expected 200 -Actual $reactivateMemberA.StatusCode
    Assert-True "PUT /api/members/{A} Admin reactivates -> IsActive true" -Condition ($reactivateMemberA.Body.isActive -eq $true)

    $ownBorrowingsA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)/borrowings" -Token $tokenA
    Assert-Status "GET /api/members/{A}/borrowings as Member A -> 200" -Expected 200 -Actual $ownBorrowingsA.StatusCode

    $otherBorrowingsA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)/borrowings" -Token $tokenB
    Assert-Status "GET /api/members/{A}/borrowings as Member B -> 403" -Expected 403 -Actual $otherBorrowingsA.StatusCode

    $adminBorrowingsA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)/borrowings" -Token $adminToken
    Assert-Status "GET /api/members/{A}/borrowings as Admin -> 200" -Expected 200 -Actual $adminBorrowingsA.StatusCode

    $noTokenBorrowingsA = Invoke-Api -Method GET -Path "/api/members/$($memberA.Id)/borrowings"
    Assert-Status "GET /api/members/{A}/borrowings no token -> 401" -Expected 401 -Actual $noTokenBorrowingsA.StatusCode

    $deleteAdminCreatedMember = Invoke-Api -Method DELETE -Path "/api/members/$adminCreatedMemberId" -Token $tokenA
    Assert-Status "DELETE /api/members/{id} as Member -> 403" -Expected 403 -Actual $deleteAdminCreatedMember.StatusCode

    $deleteAdminCreatedMemberAdmin = Invoke-Api -Method DELETE -Path "/api/members/$adminCreatedMemberId" -Token $adminToken
    Assert-Status "DELETE /api/members/{id} as Admin -> 204" -Expected 204 -Actual $deleteAdminCreatedMemberAdmin.StatusCode
    $adminCreatedMemberId = $null

    Write-Section "Borrowings"

    $borrowSelf = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $memberA.Id } -Token $tokenA
    Assert-Status "POST /api/borrowings self -> 201" -Expected 201 -Actual $borrowSelf.StatusCode
    $borrowingA = $borrowSelf.Body.id

    $borrowMismatched = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $memberB.Id } -Token $tokenA
    Assert-Status "POST /api/borrowings mismatched MemberId -> 403" -Expected 403 -Actual $borrowMismatched.StatusCode

    $borrowNoToken = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $memberA.Id }
    Assert-Status "POST /api/borrowings no token -> 401" -Expected 401 -Actual $borrowNoToken.StatusCode

    $borrowAdminForB = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $memberB.Id } -Token $adminToken
    Assert-Status "POST /api/borrowings as Admin for Member B -> 201" -Expected 201 -Actual $borrowAdminForB.StatusCode
    $borrowingB = $borrowAdminForB.Body.id

    $listBorrowingsAdmin = Invoke-Api -Method GET -Path "/api/borrowings" -Token $adminToken
    Assert-Status "GET /api/borrowings as Admin -> 200" -Expected 200 -Actual $listBorrowingsAdmin.StatusCode

    $listBorrowingsMember = Invoke-Api -Method GET -Path "/api/borrowings" -Token $tokenA
    Assert-Status "GET /api/borrowings as Member -> 403" -Expected 403 -Actual $listBorrowingsMember.StatusCode

    $getOwnBorrowingA = Invoke-Api -Method GET -Path "/api/borrowings/$borrowingA" -Token $tokenA
    Assert-Status "GET /api/borrowings/{own} as Member A -> 200" -Expected 200 -Actual $getOwnBorrowingA.StatusCode

    $getOtherBorrowingA = Invoke-Api -Method GET -Path "/api/borrowings/$borrowingA" -Token $tokenB
    Assert-Status "GET /api/borrowings/{other} as Member B -> 403" -Expected 403 -Actual $getOtherBorrowingA.StatusCode

    $getBorrowingAAdmin = Invoke-Api -Method GET -Path "/api/borrowings/$borrowingA" -Token $adminToken
    Assert-Status "GET /api/borrowings/{id} as Admin -> 200" -Expected 200 -Actual $getBorrowingAAdmin.StatusCode

    $getBorrowingANoToken = Invoke-Api -Method GET -Path "/api/borrowings/$borrowingA"
    Assert-Status "GET /api/borrowings/{id} no token -> 401" -Expected 401 -Actual $getBorrowingANoToken.StatusCode

    $returnOtherBorrowing = Invoke-Api -Method POST -Path "/api/borrowings/$borrowingA/return" -Token $tokenB
    Assert-Status "POST /api/borrowings/{other}/return as Member B -> 403" -Expected 403 -Actual $returnOtherBorrowing.StatusCode

    $returnOwnBorrowing = Invoke-Api -Method POST -Path "/api/borrowings/$borrowingA/return" -Token $tokenA
    Assert-Status "POST /api/borrowings/{own}/return as Member A -> 200/204" -Expected 200 -Actual $(if ($returnOwnBorrowing.StatusCode -eq 204) { 200 } else { $returnOwnBorrowing.StatusCode })

    $returnBorrowingBAdmin = Invoke-Api -Method POST -Path "/api/borrowings/$borrowingB/return" -Token $adminToken
    Assert-Status "POST /api/borrowings/{id}/return as Admin -> 200/204" -Expected 200 -Actual $(if ($returnBorrowingBAdmin.StatusCode -eq 204) { 200 } else { $returnBorrowingBAdmin.StatusCode })

    Write-Section "Keycloak coexistence"

    if ($SkipKeycloak) {
        Write-Host "  Skipped (-SkipKeycloak)" -ForegroundColor Yellow
    }
    else {
        $kcAdminToken = Get-KeycloakAdminToken -Username $keycloakAdmin -Password $keycloakAdminPassword
        $kcClientSecret = Ensure-KeycloakTestClient -AdminToken $kcAdminToken

        $kcAdminEmail = "adminkc.$suffix@test.local"
        $kcMemberAEmail = "memberakc.$suffix@test.local"
        $kcMemberBEmail = "memberbkc.$suffix@test.local"

        $kcAdminUserId = New-KeycloakUser -AdminToken $kcAdminToken -Email $kcAdminEmail -FirstName "Admin KC" -Password $testPassword
        Add-KeycloakRealmRole -AdminToken $kcAdminToken -UserId $kcAdminUserId -RoleName "Admin"
        $kcMemberAUserId = New-KeycloakUser -AdminToken $kcAdminToken -Email $kcMemberAEmail -FirstName "Member AKC" -Password $testPassword
        $kcMemberBUserId = New-KeycloakUser -AdminToken $kcAdminToken -Email $kcMemberBEmail -FirstName "Member BKC" -Password $testPassword

        $kcAdminUserToken = Get-KeycloakUserToken -ClientSecret $kcClientSecret -Email $kcAdminEmail -Password $testPassword
        $kcMemberAToken = Get-KeycloakUserToken -ClientSecret $kcClientSecret -Email $kcMemberAEmail -Password $testPassword
        $kcMemberBToken = Get-KeycloakUserToken -ClientSecret $kcClientSecret -Email $kcMemberBEmail -Password $testPassword

        # MemberProvisioningMiddleware JIT-provisions a Member row on any request carrying a
        # valid Keycloak token — /api/keycloak-whoami is just the cheapest way to trigger it.
        Invoke-Api -Method GET -Path "/api/keycloak-whoami" -Token $kcAdminUserToken | Out-Null
        Invoke-Api -Method GET -Path "/api/keycloak-whoami" -Token $kcMemberAToken | Out-Null
        Invoke-Api -Method GET -Path "/api/keycloak-whoami" -Token $kcMemberBToken | Out-Null

        $kcMembersList = Invoke-Api -Method GET -Path "/api/members" -Token $kcAdminUserToken
        Assert-Status "GET /api/members as Keycloak Admin -> 200" -Expected 200 -Actual $kcMembersList.StatusCode

        $kcAdminMember = $kcMembersList.Body | Where-Object { $_.email -eq $kcAdminEmail }
        $kcMemberARecord = $kcMembersList.Body | Where-Object { $_.email -eq $kcMemberAEmail }
        $kcMemberBRecord = $kcMembersList.Body | Where-Object { $_.email -eq $kcMemberBEmail }
        Assert-True "JIT-provisioned Member row exists for Keycloak Admin" -Condition ($null -ne $kcAdminMember)
        Assert-True "JIT-provisioned Member row exists for Keycloak Member A" -Condition ($null -ne $kcMemberARecord)
        Assert-True "JIT-provisioned Member row exists for Keycloak Member B" -Condition ($null -ne $kcMemberBRecord)

        $kcAdminMemberId = $kcAdminMember.id
        $kcMemberAId = $kcMemberARecord.id
        $kcMemberBId = $kcMemberBRecord.id

        $kcListMembersAsMember = Invoke-Api -Method GET -Path "/api/members" -Token $kcMemberAToken
        Assert-Status "GET /api/members as Keycloak Member -> 403" -Expected 403 -Actual $kcListMembersAsMember.StatusCode

        $kcListBorrowingsAdmin = Invoke-Api -Method GET -Path "/api/borrowings" -Token $kcAdminUserToken
        Assert-Status "GET /api/borrowings as Keycloak Admin -> 200" -Expected 200 -Actual $kcListBorrowingsAdmin.StatusCode

        $kcListBorrowingsMember = Invoke-Api -Method GET -Path "/api/borrowings" -Token $kcMemberAToken
        Assert-Status "GET /api/borrowings as Keycloak Member -> 403" -Expected 403 -Actual $kcListBorrowingsMember.StatusCode

        if ($kcMemberAId) {
            $kcGetOwnMemberA = Invoke-Api -Method GET -Path "/api/members/$kcMemberAId" -Token $kcMemberAToken
            Assert-Status "GET /api/members/{A} as Keycloak Member A (self) -> 200" -Expected 200 -Actual $kcGetOwnMemberA.StatusCode

            $kcGetOtherMemberA = Invoke-Api -Method GET -Path "/api/members/$kcMemberAId" -Token $kcMemberBToken
            # This is the specific regression the Phase 3 "Follow-up fix" targeted: a mismatched
            # id under a Keycloak token used to come back 401 (unauthenticated-looking) instead
            # of 403 (authenticated but forbidden) before every named policy accepted both schemes.
            Assert-Status "GET /api/members/{A} as Keycloak Member B (mismatch) -> 403" -Expected 403 -Actual $kcGetOtherMemberA.StatusCode

            $kcGetMemberAAdmin = Invoke-Api -Method GET -Path "/api/members/$kcMemberAId" -Token $kcAdminUserToken
            Assert-Status "GET /api/members/{A} as Keycloak Admin -> 200" -Expected 200 -Actual $kcGetMemberAAdmin.StatusCode
        }

        if ($testBookId -and $kcMemberAId -and $kcMemberBId) {
            $kcBorrowSelf = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $kcMemberAId } -Token $kcMemberAToken
            Assert-Status "POST /api/borrowings self (Keycloak Member A) -> 201" -Expected 201 -Actual $kcBorrowSelf.StatusCode
            $kcBorrowingA = $kcBorrowSelf.Body.id

            $kcBorrowMismatched = Invoke-Api -Method POST -Path "/api/borrowings" -Body @{ BookId = $testBookId; MemberId = $kcMemberBId } -Token $kcMemberAToken
            Assert-Status "POST /api/borrowings mismatched MemberId (Keycloak) -> 403" -Expected 403 -Actual $kcBorrowMismatched.StatusCode

            if ($kcBorrowingA) {
                $kcReturnOther = Invoke-Api -Method POST -Path "/api/borrowings/$kcBorrowingA/return" -Token $kcMemberBToken
                Assert-Status "POST /api/borrowings/{other}/return as Keycloak Member B -> 403" -Expected 403 -Actual $kcReturnOther.StatusCode

                $kcReturnOwn = Invoke-Api -Method POST -Path "/api/borrowings/$kcBorrowingA/return" -Token $kcMemberAToken
                Assert-Status "POST /api/borrowings/{own}/return as Keycloak Member A -> 200/204" -Expected 200 -Actual $(if ($kcReturnOwn.StatusCode -eq 204) { 200 } else { $kcReturnOwn.StatusCode })
            }
        }
        else {
            Write-Host "  ! Skipping Keycloak borrowing tests - no test book / provisioned members available" -ForegroundColor Yellow
        }
    }
}
finally {
    Write-Section "Cleanup"

    if ($testBookId -and $adminToken) {
        $r = Invoke-Api -Method DELETE -Path "/api/books/$testBookId" -Token $adminToken
        if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted test book $testBookId" -ForegroundColor Gray }
        else { Write-Host "  ! Could not delete test book $testBookId (status $($r.StatusCode))" -ForegroundColor Yellow }
    }

    if ($adminCreatedMemberId -and $adminToken) {
        $r = Invoke-Api -Method DELETE -Path "/api/members/$adminCreatedMemberId" -Token $adminToken
        if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted admin-created test member $adminCreatedMemberId" -ForegroundColor Gray }
        else { Write-Host "  ! Could not delete admin-created test member (status $($r.StatusCode))" -ForegroundColor Yellow }
    }

    if ($memberA -and $memberA.Id -and $adminToken) {
        $r = Invoke-Api -Method DELETE -Path "/api/members/$($memberA.Id)" -Token $adminToken
        if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted Member A $($memberA.Id)" -ForegroundColor Gray }
        else { Write-Host "  ! Could not delete Member A (status $($r.StatusCode))" -ForegroundColor Yellow }
    }

    if ($memberB -and $memberB.Id -and $adminToken) {
        $r = Invoke-Api -Method DELETE -Path "/api/members/$($memberB.Id)" -Token $adminToken
        if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted Member B $($memberB.Id)" -ForegroundColor Gray }
        else { Write-Host "  ! Could not delete Member B (status $($r.StatusCode))" -ForegroundColor Yellow }
    }

    # Deletes the Member rows via the legacy admin token (AdminOnly accepts both schemes since
    # the Phase 3 follow-up fix, and the legacy JWT's longer expiry is safer to rely on here than
    # a possibly-stale Keycloak access token from earlier in the run).
    if ($adminToken) {
        foreach ($kcId in @($kcAdminMemberId, $kcMemberAId, $kcMemberBId)) {
            if (-not $kcId) { continue }
            $r = Invoke-Api -Method DELETE -Path "/api/members/$kcId" -Token $adminToken
            if ($r.StatusCode -in 200, 204) { Write-Host "  Deleted Keycloak-provisioned Member $kcId" -ForegroundColor Gray }
            else { Write-Host "  ! Could not delete Keycloak-provisioned Member $kcId (status $($r.StatusCode))" -ForegroundColor Yellow }
        }
    }

    if (-not $SkipKeycloak -and $keycloakAdmin -and $keycloakAdminPassword) {
        try {
            $kcCleanupToken = Get-KeycloakAdminToken -Username $keycloakAdmin -Password $keycloakAdminPassword
            Remove-KeycloakUser -AdminToken $kcCleanupToken -UserId $kcAdminUserId
            Remove-KeycloakUser -AdminToken $kcCleanupToken -UserId $kcMemberAUserId
            Remove-KeycloakUser -AdminToken $kcCleanupToken -UserId $kcMemberBUserId
        }
        catch {
            Write-Host "  ! Keycloak user cleanup failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    Write-Section "Summary"
    $script:results | Format-Table -AutoSize

    $failCount = ($script:results | Where-Object { $_.Result -eq "FAIL" }).Count
    $passCount = ($script:results | Where-Object { $_.Result -eq "PASS" }).Count
    Write-Host ""
    Write-Host "$passCount passed, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

    if ($failCount -gt 0) { exit 1 } else { exit 0 }
}
