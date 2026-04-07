param(
    [string]$ConnectionString,
    [switch]$Preview,
    [switch]$Force,
    [switch]$SkipSetupData,
    [switch]$IncludeMasterData
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

function Get-DefaultConnectionString {
    if (-not [string]::IsNullOrWhiteSpace($env:ISS_EF_CONNECTION)) {
        return $env:ISS_EF_CONNECTION
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__Default)) {
        return $env:ConnectionStrings__Default
    }

    $repoRoot = Get-RepoRoot
    $appSettingsPath = Join-Path $repoRoot "backend\src\ISS.Api\appsettings.Development.json"
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $candidate = $appSettings.ConnectionStrings.Default
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            return $candidate
        }
    }

    throw "Unable to resolve a connection string. Pass -ConnectionString or set ISS_EF_CONNECTION / ConnectionStrings__Default."
}

function Quote-ConnInfoValue {
    param([string]$Value)

    return "'" + $Value.Replace("'", "''") + "'"
}

function Parse-KeyValueConnectionString {
    param([string]$ConnectionString)

    $values = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($segment in ($ConnectionString -split ";")) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $separatorIndex = $segment.IndexOf("=")
        if ($separatorIndex -lt 0) {
            continue
        }

        $key = $segment.Substring(0, $separatorIndex).Trim()
        $value = $segment.Substring($separatorIndex + 1).Trim()
        if (-not [string]::IsNullOrWhiteSpace($key)) {
            $values[$key] = $value
        }
    }

    return $values
}

function Parse-QueryStringValues {
    param([string]$Query)

    $values = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    if ([string]::IsNullOrWhiteSpace($Query)) {
        return $values
    }

    $trimmed = $Query.TrimStart("?")
    foreach ($segment in ($trimmed -split "&")) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $separatorIndex = $segment.IndexOf("=")
        if ($separatorIndex -lt 0) {
            $key = [System.Uri]::UnescapeDataString($segment)
            $values[$key] = ""
            continue
        }

        $key = [System.Uri]::UnescapeDataString($segment.Substring(0, $separatorIndex))
        $value = [System.Uri]::UnescapeDataString($segment.Substring($separatorIndex + 1))
        $values[$key] = $value
    }

    return $values
}

function Get-ParsedConnectionValue {
    param(
        [System.Collections.Generic.Dictionary[string, string]]$Values,
        [string[]]$Keys,
        [bool]$Required = $true,
        [string]$DefaultValue = ""
    )

    foreach ($key in $Keys) {
        if ($Values.ContainsKey($key)) {
            $value = [string]$Values[$key]
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                return $value
            }
        }
    }

    if ($Required) {
        throw "Missing required connection-string key. Tried: $($Keys -join ', ')"
    }

    return $DefaultValue
}

function Build-PsqlConnInfo {
    param([string]$ResolvedConnectionString)

    $pairs = [System.Collections.Generic.List[string]]::new()
    if ($ResolvedConnectionString -match '^\s*postgres(?:ql)?://') {
        $uri = [System.Uri]$ResolvedConnectionString
        $userInfo = $uri.UserInfo -split ":", 2
        if ($userInfo.Length -lt 1 -or [string]::IsNullOrWhiteSpace($userInfo[0])) {
            throw "Missing required connection-string key. Tried: Username, User ID"
        }

        $queryValues = Parse-QueryStringValues -Query $uri.Query
        $pairs.Add("host=" + (Quote-ConnInfoValue $uri.Host))
        $pairs.Add("port=" + (Quote-ConnInfoValue ($(if ($uri.Port -gt 0) { [string]$uri.Port } else { "5432" }))))
        $pairs.Add("dbname=" + (Quote-ConnInfoValue ([System.Uri]::UnescapeDataString($uri.AbsolutePath.TrimStart("/")))))
        $pairs.Add("user=" + (Quote-ConnInfoValue ([System.Uri]::UnescapeDataString($userInfo[0]))))

        if ($userInfo.Length -gt 1 -and -not [string]::IsNullOrWhiteSpace($userInfo[1])) {
            $pairs.Add("password=" + (Quote-ConnInfoValue ([System.Uri]::UnescapeDataString($userInfo[1]))))
        }

        if ($queryValues.ContainsKey("sslmode")) {
            $pairs.Add("sslmode=" + (Quote-ConnInfoValue ([string]$queryValues["sslmode"]).ToLowerInvariant()))
        }

        return ($pairs -join " ")
    }

    $values = Parse-KeyValueConnectionString -ConnectionString $ResolvedConnectionString
    $pairs.Add("host=" + (Quote-ConnInfoValue (Get-ParsedConnectionValue -Values $values -Keys @("Host", "Server"))))
    $pairs.Add("port=" + (Quote-ConnInfoValue (Get-ParsedConnectionValue -Values $values -Keys @("Port") -Required $false -DefaultValue "5432")))
    $pairs.Add("dbname=" + (Quote-ConnInfoValue (Get-ParsedConnectionValue -Values $values -Keys @("Database", "Initial Catalog"))))
    $pairs.Add("user=" + (Quote-ConnInfoValue (Get-ParsedConnectionValue -Values $values -Keys @("Username", "User ID", "UserId", "UID"))))

    if ($values.ContainsKey("Password")) {
        $pairs.Add("password=" + (Quote-ConnInfoValue ([string]$values["Password"])))
    } elseif ($values.ContainsKey("Pwd")) {
        $pairs.Add("password=" + (Quote-ConnInfoValue ([string]$values["Pwd"])))
    }

    if ($values.ContainsKey("SSL Mode")) {
        $pairs.Add("sslmode=" + (Quote-ConnInfoValue ([string]$values["SSL Mode"]).ToLowerInvariant()))
    } elseif ($values.ContainsKey("Ssl Mode")) {
        $pairs.Add("sslmode=" + (Quote-ConnInfoValue ([string]$values["Ssl Mode"]).ToLowerInvariant()))
    } elseif ($values.ContainsKey("SslMode")) {
        $pairs.Add("sslmode=" + (Quote-ConnInfoValue ([string]$values["SslMode"]).ToLowerInvariant()))
    }

    return ($pairs -join " ")
}

function Get-PsqlCommand {
    $command = Get-Command psql -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "psql was not found on PATH. Install PostgreSQL client tools or add psql to PATH before using this script."
    }

    return $command.Source
}

function Get-PreservedTables {
    param(
        [switch]$ExcludeSetupData,
        [switch]$ExcludeMasterData
    )

    $alwaysPreserved = @(
        "__EFMigrationsHistory",
        "AspNetRoleClaims",
        "AspNetRoles",
        "AspNetUserClaims",
        "AspNetUserLogins",
        "AspNetUserRoles",
        "AspNetUsers",
        "AspNetUserTokens",
        "DocumentSequences"
    )

    $setupTables = @(
        "AssistantAccessPolicies",
        "AssistantProviderProfiles",
        "AssistantUserPreferences",
        "Currencies",
        "CurrencyRates",
        "LedgerAccounts",
        "PaymentTypes",
        "ReferenceForms",
        "TaxCodes",
        "TaxConversions"
    )

    $masterTables = @(
        "Brands",
        "Customers",
        "EquipmentUnits",
        "ItemAttachments",
        "ItemCategories",
        "ItemSubcategories",
        "Items",
        "PettyCashFunds",
        "ReorderSettings",
        "ServiceContracts",
        "Suppliers",
        "UnitConversions",
        "UnitOfMeasures",
        "Warehouses"
    )

    $preservedTables = $alwaysPreserved

    if (-not $ExcludeSetupData) {
        $preservedTables += $setupTables
    }

    if (-not $ExcludeMasterData) {
        $preservedTables += $masterTables
    }

    return $preservedTables | Sort-Object -Unique
}

$resolvedConnectionString = if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Get-DefaultConnectionString
} else {
    $ConnectionString
}

$psql = Get-PsqlCommand
$connInfo = Build-PsqlConnInfo -ResolvedConnectionString $resolvedConnectionString
$preservedTables = Get-PreservedTables -ExcludeSetupData:$SkipSetupData -ExcludeMasterData:$IncludeMasterData
$preservedTablesSql = ($preservedTables | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ", "

$listSql = @"
SELECT tablename
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename <> ALL (ARRAY[$preservedTablesSql]::text[])
ORDER BY tablename;
"@

$tablesToClear = & $psql -d $connInfo -v ON_ERROR_STOP=1 -At -c $listSql
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read public tables from PostgreSQL."
}

Write-Host ""
Write-Host "Preserving tables:" -ForegroundColor Cyan
$preservedTables | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
if (-not $tablesToClear) {
    Write-Host "No tables matched the reset scope." -ForegroundColor Yellow
    return
}

Write-Host "Tables to clear:" -ForegroundColor Yellow
$tablesToClear | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
Write-Host "Notes:" -ForegroundColor Cyan
Write-Host "  - This only clears PostgreSQL tables in schema 'public'."
Write-Host "  - It does not delete files under backend App_Data or other filesystem storage."
Write-Host "  - By default it preserves auth tables, document sequences, setup tables, and master data."
if ($SkipSetupData) {
    Write-Host "  - Setup preservation is disabled." -ForegroundColor Yellow
}
if ($IncludeMasterData) {
    Write-Host "  - Master-data preservation is disabled; master tables will also be cleared." -ForegroundColor Yellow
}

if ($Preview) {
    Write-Host ""
    Write-Host "Preview only. No data was deleted." -ForegroundColor Green
    return
}

if (-not $Force) {
    $confirmation = Read-Host "Type CLEAR to permanently delete the listed data"
    if ($confirmation -cne "CLEAR") {
        throw "Reset cancelled."
    }
}

$truncateSqlTemplate = @'
DO $do$
DECLARE
    target_tables text;
BEGIN
    SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
    INTO target_tables
    FROM pg_tables
    WHERE schemaname = 'public'
      AND tablename <> ALL (ARRAY[__PRESERVED_TABLES_SQL__]::text[]);

    IF target_tables IS NULL THEN
        RAISE NOTICE 'No tables matched the reset scope.';
        RETURN;
    END IF;

    EXECUTE 'TRUNCATE TABLE ' || target_tables || ' RESTART IDENTITY CASCADE';
END
$do$;
'@
$truncateSql = $truncateSqlTemplate.Replace("__PRESERVED_TABLES_SQL__", $preservedTablesSql)

& $psql -d $connInfo -v ON_ERROR_STOP=1 -c $truncateSql
if ($LASTEXITCODE -ne 0) {
    throw "Failed while truncating PostgreSQL tables."
}

Write-Host ""
Write-Host "Business data reset completed." -ForegroundColor Green
