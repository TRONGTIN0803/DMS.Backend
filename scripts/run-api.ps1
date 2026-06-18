$ErrorActionPreference = "Stop"

$dotnet = "dotnet"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $dotnet = "C:\Program Files\dotnet\dotnet.exe"
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$swaggerUrl = "http://localhost:5080/swagger/index.html"

$processIds = @()
$processIds += @(Get-Process -Name DMS.Api -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
$processIds += @(Get-NetTCPConnection -LocalPort 5080 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess)
$processIds = $processIds | Where-Object { $_ } | Select-Object -Unique

if ($processIds) {
    Stop-Process -Id $processIds -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Start-Job -ScriptBlock {
    param($url)

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                Start-Process $url
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
} -ArgumentList $swaggerUrl | Out-Null

& $dotnet run `
    --project "$PSScriptRoot\..\src\DMS.Api\DMS.Api.csproj" `
    --no-build `
    --urls "http://localhost:5080"
