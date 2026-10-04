$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
Push-Location $projectRoot
try {
    & $dotnetCommand build src/LovenseIntegrator.Desktop -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & $dotnetCommand src/LovenseIntegrator.Desktop/bin/Release/net10.0-windows/LovenseIntegrator.dll --ble-recovery-harness
    if ($LASTEXITCODE -ne 0) { throw (Get-Content -Raw artifacts/ble-recovery/failure.txt) }
    $report = Get-Content -Raw artifacts/ble-recovery/report.json | ConvertFrom-Json
    Write-Host "BLE process recovery: $($report.Result), $($report.Checks.Count) checks. Demo transport only; no physical toy commands."
}
finally { Pop-Location }
