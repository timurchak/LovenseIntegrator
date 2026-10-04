param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $projectRoot
try {
    & $dotnetCommand build LovenseIntegrator.slnx -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. Install the .NET 10 SDK or run scripts/Setup-Sdk.ps1.' }
    & $dotnetCommand run --project tests/LovenseIntegrator.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Checks failed.' }
    if ($Publish) {
        & $dotnetCommand publish src/LovenseIntegrator.Desktop -c Release -r win-x64 --self-contained true -o dist/win-x64 --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    }
}
finally { Pop-Location }
