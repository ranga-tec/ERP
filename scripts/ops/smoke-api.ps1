[CmdletBinding()]
param(
    [Parameter()]
    [string]$ApiBaseUrl = "http://127.0.0.1:5257",

    [Parameter()]
    [switch]$SkipAuth,

    [Parameter()]
    [string]$Email,

    [Parameter()]
    [string]$Password,

    [Parameter()]
    [string]$BearerToken,

    [Parameter()]
    [int]$Retries = 30,

    [Parameter()]
    [int]$DelaySeconds = 2,

    [Parameter()]
    [int]$TimeoutSec = 15
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body,
        [hashtable]$Headers = @{}
    )

    $json = $Body | ConvertTo-Json -Depth 8
    return Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body $json -Headers $Headers -TimeoutSec $TimeoutSec
}

function Invoke-JsonGet {
    param(
        [string]$Url,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod -Method Get -Uri $Url -Headers $Headers -TimeoutSec $TimeoutSec
}

$baseUrl = $ApiBaseUrl.TrimEnd("/")
if ([string]::IsNullOrWhiteSpace($baseUrl)) {
    throw "ApiBaseUrl is required."
}

Write-Host "Smoke: waiting for health endpoint at $baseUrl/health"
$healthy = $false
for ($attempt = 1; $attempt -le [Math]::Max(1, $Retries); $attempt++) {
    try {
        $healthResponse = Invoke-WebRequest -Uri "$baseUrl/health" -Method Get -TimeoutSec $TimeoutSec
        if ($healthResponse.StatusCode -eq 200) {
            $healthy = $true
            Write-Host "Smoke: health check passed on attempt $attempt"
            break
        }
    }
    catch {
        if ($attempt -eq [Math]::Max(1, $Retries)) {
            throw
        }
    }

    if ($attempt -lt [Math]::Max(1, $Retries)) {
        Start-Sleep -Seconds ([Math]::Max(0, $DelaySeconds))
    }
}

if (-not $healthy) {
    throw "Health check failed after $Retries attempt(s)."
}

if ($SkipAuth) {
    Write-Host "Smoke: auth checks skipped."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        throw "Provide -BearerToken or both -Email and -Password (or use -SkipAuth)."
    }

    Write-Host "Smoke: logging in as $Email"
    $login = Invoke-JsonPost -Url "$baseUrl/api/auth/login" -Body @{
        email = $Email
        password = $Password
    }

    if (-not $login.token) {
        throw "Login response did not include a token."
    }

    $BearerToken = [string]$login.token
}

$headers = @{ Authorization = "Bearer $BearerToken" }

Write-Host "Smoke: fetching dashboard"
$dashboard = Invoke-JsonGet -Url "$baseUrl/api/reporting/dashboard" -Headers $headers
if ($null -eq $dashboard) {
    throw "Dashboard response was null."
}

Write-Host "Smoke: fetching audit logs"
$auditLogs = Invoke-JsonGet -Url "$baseUrl/api/audit-logs?take=5" -Headers $headers
if ($null -eq $auditLogs) {
    throw "Audit logs response was null."
}

Write-Host "Smoke: fetching master data lists"
[void](Invoke-JsonGet -Url "$baseUrl/api/items" -Headers $headers)
[void](Invoke-JsonGet -Url "$baseUrl/api/customers" -Headers $headers)

Write-Host "Smoke: API checks passed."
