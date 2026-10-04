$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$nativeRoot = Join-Path $projectRoot 'native'
New-Item -ItemType Directory -Path $nativeRoot -Force | Out-Null
$dllPath = Join-Path $nativeRoot 'LovenseBLE_Lib.dll'
Invoke-WebRequest -Uri 'https://developer.lovense.com/LovenseBLE_Lib.dll' -OutFile $dllPath
$expectedHash = '74424f9047078d0e02735dc9f7106897161e401de502d9f18597a3d9d3afa9cb'
if ((Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Lovense changed the binary. Verify the new vendor release and update the pinned hash before using it.'
}
Write-Host 'Official Lovense BLE DLL downloaded. Rebuild to include it in the local application.'
