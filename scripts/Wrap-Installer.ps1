param(
    [Parameter(Mandatory)][string]$Bootstrapper,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [ValidatePattern('^[A-Za-z0-9]+$')][string]$PackageId = 'LovenseIntegratorApp'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = & (Join-Path $PSScriptRoot 'Setup-InstallerTool.ps1')
$bootstrapPath = (Resolve-Path -LiteralPath $Bootstrapper).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
& $compiler '/Qp' "/DAppVersion=$Version" "/DAppPackageId=$PackageId" "/DBootstrapper=$bootstrapPath" "/DReleaseDirectory=$outputPath" (Join-Path $root 'installer/Setup.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer wizard compilation failed.' }
