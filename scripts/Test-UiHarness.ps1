param([ValidateSet('en', 'ru')][string[]]$Languages = @('en', 'ru'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
Push-Location $projectRoot
try {
    & $dotnetCommand build src/LovenseIntegrator.Desktop -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    foreach ($language in $Languages) { foreach ($culture in @('ru-RU', 'en-US')) {
        & $dotnetCommand src/LovenseIntegrator.Desktop/bin/Release/net10.0-windows/LovenseIntegrator.dll --ui-harness --culture $culture --language $language
        if ($LASTEXITCODE -ne 0) { throw (Get-Content -Raw "artifacts/ui-harness/$language/$culture/failure.txt") }
        $report = Get-Content -Raw "artifacts/ui-harness/$language/$culture/report.json" | ConvertFrom-Json
        Write-Output "UI harness $language / $culture`: $($report.Result), $($report.Checks) assertions."
        $report.Groups | Format-Table Name, Checks, Result -AutoSize
    } }
}
finally { Pop-Location }
