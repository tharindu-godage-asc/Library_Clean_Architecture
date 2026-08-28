<#
Automated authorization-matrix smoke test for the Library API.
Covers auth, books, members, and borrowings across: no token / Member A (self) /
Member A (on Member B's data) / Admin. Creates its own test data and cleans up after itself.

Usage:
    $env:ADMIN_EMAIL = "admin@example.com"
    $env:ADMIN_PASSWORD = "..."
    .\scripts\test-endpoints.ps1 [-BaseUrl "https://localhost:7282"]

If ADMIN_EMAIL / ADMIN_PASSWORD are not set, you'll be prompted for them.

NOTE: Must use the HTTPS base URL, not HTTP. The API's UseHttpsRedirection() 307-redirects
HTTP requests to HTTPS, and both curl and PowerShell's web client strip the Authorization
header when following a redirect to a different port — every authenticated call silently
loses its Bearer token and comes back 401 if you point this at the HTTP port. Confirmed
2026-08-27 (see docs/keycloak-session-2026-08-27-summary.md).
#>

param(
    [string]$BaseUrl = "https://localhost:7282"
)

$script:results = @()

function Invoke-Api {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Path,
        [object]$Body = $null,
        [string]$Token = $null
    )

    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method          = $Method
        Uri             = "$BaseUrl$Path"
        Headers         = $headers
        UseBasicParsing = $true
        ErrorAction     = "Stop"
    }
    if ($null -ne $Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
        $params["ContentType"] = "application/json"
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

$suffix = [guid]::NewGuid().ToString("N").Substring(0, 8)
$testPassword = "TestPass123!"

$testBookId = $null
$memberA = $null
$memberB = $null
$adminCreatedMemberId = $null
$borrowingA = $null
$borrowingB = $null

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

    Write-Section "Summary"
    $script:results | Format-Table -AutoSize

    $failCount = ($script:results | Where-Object { $_.Result -eq "FAIL" }).Count
    $passCount = ($script:results | Where-Object { $_.Result -eq "PASS" }).Count
    Write-Host ""
    Write-Host "$passCount passed, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

    if ($failCount -gt 0) { exit 1 } else { exit 0 }
}
