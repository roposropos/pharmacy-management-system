param(
    [string]$DbName = "Apteka",
    [string]$PgBin = "",
    [switch]$SkipSeed,
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Resolve-PostgresTool {
    param(
        [string]$Name,
        [string]$PgBin
    )

    if (-not [string]::IsNullOrWhiteSpace($PgBin)) {
        $candidate = Join-Path $PgBin $Name
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw "Cannot find $Name. Add PostgreSQL bin directory to PATH or pass -PgBin."
}

$psql = Resolve-PostgresTool -Name "psql.exe" -PgBin $PgBin
$createdb = Resolve-PostgresTool -Name "createdb.exe" -PgBin $PgBin
$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repoRoot
try {
    $exists = (& $psql -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$DbName'").Trim()
    if ($exists -ne "1") {
        & $createdb $DbName
    }

    Get-ChildItem -Path "db/migrations" -Filter "*.sql" | Sort-Object Name | ForEach-Object {
        & $psql -d $DbName -v ON_ERROR_STOP=1 -f $_.FullName
    }

    if (-not $SkipSeed) {
        & $psql -d $DbName -v ON_ERROR_STOP=1 -f "db/seeds/001_demo_data.sql"
    }

    if (-not $SkipSmokeTest) {
        & $psql -d $DbName -v ON_ERROR_STOP=1 -f "db/tests/001_smoke_regression.sql"
    }

    Write-Host "Database $DbName is ready."
}
finally {
    Pop-Location
}
