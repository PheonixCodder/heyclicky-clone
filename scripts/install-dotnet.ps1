$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:USERPROFILE ".dotnet"
Write-Host "Downloading dotnet-install.ps1..."
$scriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $scriptPath
Write-Host "Installing .NET 9.0 SDK into $installDir..."
& $scriptPath -Channel 9.0 -InstallDir $installDir
Write-Host "Installed dotnet version:"
& (Join-Path $installDir "dotnet.exe") --version
