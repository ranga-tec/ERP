[CmdletBinding()]
param(
    [string]$DatabaseHost = "127.0.0.1",
    [int]$DatabasePort = 5433,
    [string]$DatabaseName = "neuedge",
    [string]$DatabaseUser = "pgadmin",
    [string]$DatabasePassword = "vesper",
    [int]$ApiPort = 5257,
    [int]$WebPort = 3000,
    [switch]$NoBuild,
    [switch]$FrontendOnly,
    [switch]$ProductionFrontend,
    [switch]$ApiOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = ".\backend\src\ISS.Api\ISS.Api.csproj"
$apiAssembly = Join-Path $repoRoot "backend\src\ISS.Api\bin\Debug\net8.0\ISS.Api.dll"
$apiAssemblyArgument = ".\backend\src\ISS.Api\bin\Debug\net8.0\ISS.Api.dll"
$frontendRoot = Join-Path $repoRoot "frontend"
$apiOutLog = Join-Path $repoRoot ".local-api-demo.out.log"
$apiErrLog = Join-Path $repoRoot ".local-api-demo.err.log"
$webOutLog = Join-Path $repoRoot ".local-frontend-demo.out.log"
$webErrLog = Join-Path $repoRoot ".local-frontend-demo.err.log"

function Test-ListeningPort([int]$Port) {
    return [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $Port
}

if ($FrontendOnly -and $ApiOnly) {
    throw "FrontendOnly and ApiOnly cannot be used together."
}
if (-not $FrontendOnly -and (Test-ListeningPort $ApiPort)) {
    throw "API port $ApiPort is already in use. Stop the existing process or choose -ApiPort."
}
if ($FrontendOnly -and -not (Test-ListeningPort $ApiPort)) {
    throw "FrontendOnly requires an API already listening on port $ApiPort."
}
if (-not $ApiOnly -and (Test-ListeningPort $WebPort)) {
    throw "Frontend port $WebPort is already in use. Stop the existing process or choose -WebPort."
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
$env:ConnectionStrings__Default = "Host=$DatabaseHost;Port=$DatabasePort;Database=$DatabaseName;Username=$DatabaseUser;Password=$DatabasePassword;Include Error Detail=true"
$env:Database__InitializationMode = "Migrate"
$env:Jwt__Issuer = "neuedge"
$env:Jwt__Audience = "neuedge"
$env:Jwt__Key = "dev-only-change-me-need-32-chars-minimum-for-hs256"
$env:Security__EnforceHttps = "false"
$env:Auth__AllowFirstUserBootstrapRegistration = "true"
$env:Auth__AllowSelfRegistration = "true"

$apiArguments = if ($NoBuild) {
    if (-not (Test-Path $apiAssembly)) {
        throw "No existing API build was found at $apiAssembly. Run without -NoBuild first."
    }
    @($apiAssemblyArgument)
}
else {
    @("run", "--project", $apiProject, "--no-launch-profile")
}

$api = $null
if (-not $FrontendOnly) {
    $api = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList $apiArguments `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $apiOutLog `
        -RedirectStandardError $apiErrLog `
        -WindowStyle Hidden `
        -PassThru

    $apiReady = $false
    for ($attempt = 1; $attempt -le 180; $attempt++) {
        Start-Sleep -Seconds 2
        try {
            $health = Invoke-WebRequest "http://127.0.0.1:$ApiPort/health" -UseBasicParsing -TimeoutSec 10
            if ($health.StatusCode -eq 200) {
                $apiReady = $true
                break
            }
        }
        catch {
            if ($api.HasExited) { break }
        }
    }

    if (-not $apiReady) {
        if (-not $api.HasExited) { Stop-Process -Id $api.Id }
        throw "API did not become healthy. Check $apiOutLog and $apiErrLog."
    }
}
else {
    $health = Invoke-WebRequest "http://127.0.0.1:$ApiPort/health" -UseBasicParsing -TimeoutSec 30
    if ($health.StatusCode -ne 200) {
        throw "Existing API health check returned HTTP $($health.StatusCode)."
    }
}

if ($ApiOnly) {
    [pscustomobject]@{
        ApiPid = $api.Id
        ApiHealth = "http://127.0.0.1:$ApiPort/health"
        Database = "$DatabaseHost`:$DatabasePort/$DatabaseName"
    }
    return
}

$env:NEUEDGE_API_BASE_URL = "http://127.0.0.1:$ApiPort"
$env:NEUEDGE_SECURE_COOKIES = "false"
$env:NEXT_PUBLIC_NEUEDGE_ALLOW_SELF_REGISTRATION = "true"
$env:PORT = $WebPort.ToString()

$webScript = if ($ProductionFrontend) { "start" } else { "dev" }
$web = Start-Process `
    -FilePath "npm.cmd" `
    -ArgumentList @("run", $webScript) `
    -WorkingDirectory $frontendRoot `
    -RedirectStandardOutput $webOutLog `
    -RedirectStandardError $webErrLog `
    -WindowStyle Hidden `
    -PassThru

$webReady = $false
for ($attempt = 1; $attempt -le 45; $attempt++) {
    Start-Sleep -Seconds 2
    try {
        $login = Invoke-WebRequest "http://127.0.0.1:$WebPort/login" -UseBasicParsing -TimeoutSec 3
        if ($login.StatusCode -eq 200) {
            $webReady = $true
            break
        }
    }
    catch {
        if ($web.HasExited) { break }
    }
}

if (-not $webReady) {
    if (-not $web.HasExited) { Stop-Process -Id $web.Id }
    if ($null -ne $api) { Stop-Process -Id $api.Id -ErrorAction SilentlyContinue }
    throw "Frontend did not become ready. Check $webOutLog and $webErrLog."
}

[pscustomobject]@{
    ApiPid = if ($null -ne $api) { $api.Id } else { "existing" }
    WebPid = $web.Id
    ApiHealth = "http://127.0.0.1:$ApiPort/health"
    Login = "http://127.0.0.1:$WebPort/login"
    Database = "$DatabaseHost`:$DatabasePort/$DatabaseName"
}
