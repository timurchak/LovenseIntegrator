param([Parameter(Mandatory)][string]$ReleaseDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetCommand)) { $dotnetCommand = 'dotnet' }
Push-Location $projectRoot
try {
    $releaseRoot = [IO.Path]::GetFullPath($ReleaseDirectory)
    $runRoot = Join-Path $projectRoot ("artifacts/installer/" + [guid]::NewGuid().ToString('N'))
    $installed = Join-Path $runRoot 'custom install path'
    $feed = Join-Path $runRoot 'feed'
    New-Item -ItemType Directory -Path $runRoot, $feed -Force | Out-Null
    $setup = Join-Path $releaseRoot 'LovenseIntegratorApp-win-Setup.exe'
    $packages = @(Get-ChildItem -LiteralPath $releaseRoot -Filter '*-full.nupkg')
    if ($packages.Count -ne 1) { throw 'Expected exactly one full update package.' }
    $package = $packages[0]
    if (-not (Test-Path -LiteralPath $setup)) { throw 'Setup executable not found.' }
    # Test installers use a different app ID and no shortcuts.
    $payload = Join-Path $runRoot 'payload'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($package.FullName, $payload)
    $packDir = Join-Path $payload 'lib/app'
    if (Test-Path (Join-Path $packDir 'LovenseBLE_Lib.dll')) { throw 'Vendor DLL leaked into the package.' }
    foreach ($version in @('0.0.1', '0.0.2')) {
        $args = @('pack', '--packId', 'LovenseIntegratorInstallerTest', '--packVersion', $version,
            '--packDir', $packDir, '--mainExe', 'LovenseIntegrator.exe', '--runtime', 'win-x64',
            '--channel', 'win', '--outputDir', $feed, '--delta', 'None', '--noPortable', '--shortcuts', 'None')
        & $dotnetCommand tool run vpk -- @args
        if ($LASTEXITCODE -ne 0) { throw 'Test package failed.' }
        if ($version -eq '0.0.1') {
            $testSetup = Join-Path $feed 'LovenseIntegratorInstallerTest-win-Setup.exe'
            $bootstrap = Join-Path $runRoot 'test-bootstrap.exe'
            Move-Item -LiteralPath $testSetup -Destination $bootstrap
            & (Join-Path $PSScriptRoot 'Wrap-Installer.ps1') -Bootstrapper $bootstrap -OutputDirectory $feed -Version $version -PackageId LovenseIntegratorInstallerTest
            # Refuse unrelated files instead of handing a nonempty directory to the bootstrapper.
            $occupied = Join-Path $runRoot 'unrelated files'
            New-Item -ItemType Directory -Path $occupied | Out-Null
            $keep = Join-Path $occupied 'keep.txt'; Set-Content -LiteralPath $keep 'Must be preserved'
            $reject = Start-Process -FilePath $testSetup -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/DIR="' + $occupied + '"')) -WindowStyle Hidden -PassThru
            if (-not $reject.WaitForExit(60000) -or $reject.ExitCode -eq 0 -or (Get-Content -LiteralPath $keep) -ne 'Must be preserved') { throw 'Wizard did not protect an occupied destination.' }
            $process = Start-Process -FilePath $testSetup -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/DIR="' + $installed + '"'), ('/LOG="' + (Join-Path $runRoot 'wizard-install.log') + '"')) -WindowStyle Hidden -PassThru
            if (-not $process.WaitForExit(120000) -or $process.ExitCode -ne 0) { throw 'Silent test installation failed.' }
            if (-not (Test-Path -LiteralPath (Join-Path $installed 'current/LovenseIntegrator.exe'))) { throw 'Wizard ignored the selected folder.' }
        }
    }
    $app = Join-Path $installed 'current/LovenseIntegrator.exe'
    $sentinel = Join-Path $runRoot 'preserved-profile.json'
    Copy-Item -LiteralPath (Join-Path $projectRoot 'examples/keyboard-assignments.json') -Destination $sentinel
    $original = (Get-FileHash -LiteralPath $sentinel).Hash
    function Run-UpdateProbe([string]$name, [string[]]$extra, [switch]$ExpectFailure) {
        $report = Join-Path $runRoot "$name.json"
        $arguments = @('--update-harness', ('"' + $feed + '"'), ('"' + $report + '"')) + $extra
        $p = Start-Process -FilePath $app -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
        if (-not $p.WaitForExit(120000) -or ($p.ExitCode -ne 0 -and -not $ExpectFailure) -or ($p.ExitCode -eq 0 -and $ExpectFailure)) {
            if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
            throw "Installed update probe failed: $name"
        }
        return Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    }
    $newPackage = Join-Path $feed 'LovenseIntegratorInstallerTest-0.0.2-full.nupkg'
    $validPackage = Join-Path $runRoot 'valid-package.nupkg'
    Copy-Item -LiteralPath $newPackage -Destination $validPackage
    $stream = [IO.File]::Open($newPackage, [IO.FileMode]::Append)
    try { $stream.WriteByte(42) } finally { $stream.Dispose() }
    $rejected = Run-UpdateProbe 'corrupt' @('--download') -ExpectFailure
    if ($rejected.Result -ne 'FAIL' -or $rejected.Error -notmatch 'checksum|hash|size') { throw 'Corrupt package was not rejected for integrity.' }
    Copy-Item -LiteralPath $validPackage -Destination $newPackage -Force
    $before = Run-UpdateProbe 'before' @('--download', '--apply')
    if ($before.Version -ne '0.0.1' -or $before.Available -ne '0.0.2' -or $before.Pending -ne '0.0.2') { throw 'Installed update state mismatch.' }
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 500
        $manifest = Join-Path $installed 'current/sq.version'
        $upgraded = (Test-Path -LiteralPath $manifest) -and ((Get-Content -LiteralPath $manifest -Raw) -match '<version>0.0.2</version>')
    } while (-not $upgraded -and [DateTime]::UtcNow -lt $deadline)
    if (-not $upgraded) { throw 'Timed out applying the update.' }
    # sq.version is replaced before every payload file is ready. Wait for this installation's updater.
    $updaterPath = Join-Path $installed 'Update.exe'
    $updaters = @(Get-Process -Name Update -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $updaterPath })
    foreach ($updater in $updaters) {
        if (-not $updater.WaitForExit(60000)) { throw 'Update process did not finish.' }
    }
    $after = Run-UpdateProbe 'after' @()
    if ($after.Version -ne '0.0.2' -or $after.Available) { throw 'Updated app version mismatch.' }
    # Upgrade/reinstall through the wizard retains a previously registered custom destination.
    $bootstrap = Join-Path $runRoot 'test-bootstrap-2.exe'
    $testSetup = Join-Path $feed 'LovenseIntegratorInstallerTest-win-Setup.exe'
    Move-Item -LiteralPath $testSetup -Destination $bootstrap
    & (Join-Path $PSScriptRoot 'Wrap-Installer.ps1') -Bootstrapper $bootstrap -OutputDirectory $feed -Version '0.0.2' -PackageId LovenseIntegratorInstallerTest
    $reinstall = Start-Process -FilePath $testSetup -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -WindowStyle Hidden -PassThru
    if (-not $reinstall.WaitForExit(120000) -or $reinstall.ExitCode -ne 0) { throw 'Wizard reinstall failed.' }
    $reinstalled = Run-UpdateProbe 'reinstalled' @()
    if ($reinstalled.Version -ne '0.0.2') { throw 'Wizard did not retain the existing custom destination.' }
    if ((Get-FileHash -LiteralPath $sentinel).Hash -ne $original) { throw 'External profile changed during update.' }
    @{ Result = 'PASS'; Before = $before; After = $after; WizardCustomFolder = $true; WizardExistingFolder = $true; OccupiedFolderRejected = $true; CorruptPackageRejected = $true; ExternalProfilePreserved = $true; VendorDllExcluded = $true } |
        ConvertTo-Json -Depth 5 | Set-Content (Join-Path $runRoot 'report.json')
    Write-Output "Installer/upgrade PASS: $runRoot"
}
finally {
    if ($installed -and (Test-Path -LiteralPath (Join-Path $installed 'Update.exe'))) {
        $uninstall = Start-Process -FilePath (Join-Path $installed 'Update.exe') -ArgumentList @('uninstall', '--silent') -WindowStyle Hidden -PassThru
        if (-not $uninstall.WaitForExit(60000) -or $uninstall.ExitCode -ne 0) { Write-Warning "Could not remove test installation: $installed" }
    }
    Pop-Location
}
