param(
    [string]$PgBin = 'C:\Program Files\PostgreSQL\16\bin',
    [string]$ServerHost = '127.0.0.1',
    [int]$Port = 5432,
    [string]$SuperUser = 'postgres',
    [string]$RoleName = 'schoolbuffet_app',
    [string]$DatabaseName = 'SchoolBuffet'
)

$ErrorActionPreference = 'Stop'

function ConvertTo-PlainString([System.Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToGlobalAllocUnicode($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringUni($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeGlobalAllocUnicode($pointer)
    }
}

if (-not (Test-Path (Join-Path $PgBin 'psql.exe'))) {
    throw "psql.exe nuk u gjet ne $PgBin. Vendos -PgBin me drejtimin e instalimit te PostgreSQL."
}

$superPassword = Read-Host -Prompt "Fjalakalimi i superperdoruesit PostgreSQL ($SuperUser)" -AsSecureString
$appPassword = Read-Host -Prompt "Fjalakalimi i ri per rollogaritjen e aplikacionit ($RoleName)" -AsSecureString

$env:PGPASSWORD = ConvertTo-PlainString $superPassword
$env:SB_APP_PASSWORD = ConvertTo-PlainString $appPassword
$env:SB_ROLE = $RoleName
$env:SB_DB = $DatabaseName

try {
    & (Join-Path $PgBin 'psql.exe') -h $ServerHost -p $Port -U $SuperUser -d postgres -v ON_ERROR_STOP=1 -f (Join-Path $PSScriptRoot 'create-role-and-database.sql')
    if ($LASTEXITCODE -ne 0) {
        throw 'PostgreSQL nuk i kryen urdhrat e konfigurimit.'
    }

    Write-Host ''
    Write-Host 'Rolli dhe baza jane gati.'
    Write-Host 'Krijo skedarin Backend/SchoolBuffet.Api/appsettings.Local.json me kete struktur:'
    Write-Host '{'
    Write-Host '  "ConnectionStrings": {'
    Write-Host ("    `"SchoolBuffet`": `"Host={0};Port={1};Database={2};Username={3};Password=TE_TI_PATJETER`"" -f $ServerHost, $Port, $DatabaseName, $RoleName)
    Write-Host '  }'
    Write-Host '}'
    Write-Host 'Ruaje jashte repository-t dhe mos e dergo ne GitHub.'
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:SB_APP_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:SB_ROLE -ErrorAction SilentlyContinue
    Remove-Item Env:SB_DB -ErrorAction SilentlyContinue
}
