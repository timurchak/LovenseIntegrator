param(
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
Push-Location $projectRoot
try {
    if (-not $Version) { $Version = ([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.Version }
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a stable version: major.minor.patch.' }
    if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot "dist/releases/$Version" }
    $OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
    if ((Test-Path -LiteralPath $OutputDirectory) -and (Get-ChildItem -LiteralPath $OutputDirectory -Force | Select-Object -First 1)) {
        throw "Output must be empty to avoid publishing stale assets: $OutputDirectory"
    }
    $publishDirectory = Join-Path $projectRoot ("dist/package/" + [guid]::NewGuid().ToString('N'))
    & $dotnetCommand tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Velopack tool restore failed.' }
    & $dotnetCommand publish src/LovenseIntegrator.Desktop -c Release -r win-x64 --self-contained true -p:Version=$Version -p:IncludeVendorSdk=false -o $publishDirectory --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path (Join-Path $publishDirectory 'LovenseBLE_Lib.dll')) { throw 'Vendor DLL must not be redistributed.' }
    # A distinct package ID prevents uninstall from removing the separate profile directory.
    $packArgs = @('pack', '--packId', 'LovenseIntegratorApp', '--packVersion', $Version,
        '--packDir', $publishDirectory, '--mainExe', 'LovenseIntegrator.exe', '--runtime', 'win-x64',
        '--channel', 'win', '--packTitle', 'Lovense Integrator', '--packAuthors', 'timurchak',
        '--outputDir', $OutputDirectory, '--delta', 'None', '--noPortable')
    $notes = Join-Path $projectRoot "releases/$Version.md"
    if (Test-Path -LiteralPath $notes) { $packArgs += @('--releaseNotes', $notes) }
    & $dotnetCommand tool run vpk -- @packArgs
    if ($LASTEXITCODE -ne 0) { throw 'Installer packaging failed.' }
    # Replace the one-click EXE with a wizard, preserving Velopack's asset filename and update feed.
    $setup = Join-Path $OutputDirectory 'LovenseIntegratorApp-win-Setup.exe'
    $bootstrapper = Join-Path $publishDirectory 'Velopack-Setup.exe'
    Move-Item -LiteralPath $setup -Destination $bootstrapper
    & (Join-Path $PSScriptRoot 'Wrap-Installer.ps1') -Bootstrapper $bootstrapper -OutputDirectory $OutputDirectory -Version $Version
    Write-Output "Installer and update feed: $OutputDirectory"
}
finally { Pop-Location }
