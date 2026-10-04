$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$toolDirectory = Join-Path $root '.tools/inno-6.7.3'
$compiler = Join-Path $toolDirectory 'ISCC.exe'
if (Test-Path -LiteralPath $compiler) { return $compiler }
$download = Join-Path $root '.tools/innosetup-6.7.3.exe'
New-Item -ItemType Directory -Path (Split-Path $download) -Force | Out-Null
Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $download
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne '9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732') {
    throw 'Inno Setup download checksum mismatch.'
}
# Official portable mode does not register an uninstaller, shortcuts or file associations.
$process = Start-Process -FilePath $download -ArgumentList @('/PORTABLE=1', '/CURRENTUSER', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/DIR="' + $toolDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(120000) -or $process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $compiler)) { throw 'Portable Inno Setup installation failed.' }
return $compiler
