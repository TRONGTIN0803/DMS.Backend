$ErrorActionPreference = "Stop"

$psql = "psql"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    $psql = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
}

$env:PGPASSWORD = "dms_password"

& $psql `
    -h localhost `
    -p 5432 `
    -U dms_app `
    -d dms

