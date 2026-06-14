# Stop stale Zebrahoof EMR instances, build once, then start the app.
$ErrorActionPreference = "Stop"
$port = 5222
$root = Split-Path $PSScriptRoot -Parent

function Stop-PortListeners {
    param([int]$Port)

    Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
        ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }

    Get-Process -Name "Zebrahoof_EMR" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

    for ($i = 0; $i -lt 10; $i++) {
        if (-not (Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue)) {
            return
        }

        Start-Sleep -Milliseconds 500
    }

    Write-Warning "Port $Port may still be in use."
}

Set-Location $root
Stop-PortListeners -Port $port

Write-Host "Building Zebrahoof EMR..."
dotnet build "Zebrahoof EMR.csproj" -v q
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Build succeeded. Starting at http://localhost:$port ..."
Write-Host "Press Ctrl+C to stop.`n"
dotnet run --project "Zebrahoof EMR.csproj" --launch-profile http --no-build
