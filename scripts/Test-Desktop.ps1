$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
Push-Location $projectRoot
try {
    & $dotnetCommand build src/LovenseIntegrator.Desktop -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & $dotnetCommand src/LovenseIntegrator.Desktop/bin/Release/net10.0-windows/LovenseIntegrator.dll --smoke
    if ($LASTEXITCODE -ne 0) { throw (Get-Content -Raw artifacts/ui-smoke.txt) }
    Get-Content artifacts/ui-smoke.txt
}
finally { Pop-Location }
