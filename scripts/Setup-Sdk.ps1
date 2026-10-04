$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $projectRoot '.tools'
New-Item -ItemType Directory -Path $toolsRoot -Force | Out-Null
$sdkVersion = '10.0.401'
$sdkArchive = Join-Path $toolsRoot 'dotnet-sdk.zip'
$sdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-win-x64.zip"
Invoke-WebRequest -Uri $sdkUrl -OutFile $sdkArchive
$expectedHash = '24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430'
if ((Get-FileHash -LiteralPath $sdkArchive -Algorithm SHA512).Hash -ne $expectedHash) { throw 'SDK checksum mismatch.' }
Expand-Archive -LiteralPath $sdkArchive -DestinationPath (Join-Path $toolsRoot 'dotnet') -Force
Write-Host "Portable SDK $sdkVersion ready. No global installation required."
