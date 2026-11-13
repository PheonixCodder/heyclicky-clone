# ==============================================================================
# Kursor Base Runtime Bootstrapper (scripts/install-runtime.ps1)
# Downloads HeyClicky v1.1.5 base native/WinUI runtime assets into
# dist\kursor-desktop and %LOCALAPPDATA%\HeyClickyApp\current on fresh PCs.
# ==============================================================================
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DestDir = Join-Path $RepoRoot "dist\kursor-desktop"
$AppDataDir = Join-Path $env:LOCALAPPDATA "HeyClickyApp\current"
$Url = "https://github.com/heyclicky/clicky-releases-windows/releases/download/v1.1.5/HeyClickyApp-1.1.5-win-x64-full.nupkg"
$TempZip = Join-Path $env:TEMP "HeyClickyApp-1.1.5-full.zip"
$ExtractTemp = Join-Path $env:TEMP "HeyClickyExtract"

if (-not (Test-Path $TempZip)) {
    Write-Host "[*] Downloading HeyClicky v1.1.5 base runtime package (~277 MB)..." -ForegroundColor Cyan
    & curl.exe -L --retry 3 -o $TempZip $Url
    if ($LASTEXITCODE -ne 0) {
        Invoke-WebRequest -Uri $Url -OutFile $TempZip -UseBasicParsing
    }
}

Write-Host "[*] Extracting base runtime assets..." -ForegroundColor Cyan
if (Test-Path $ExtractTemp) {
    Remove-Item $ExtractTemp -Recurse -Force
}
Expand-Archive -Path $TempZip -DestinationPath $ExtractTemp -Force

$PayloadDir = if (Test-Path (Join-Path $ExtractTemp "lib\app")) {
    Join-Path $ExtractTemp "lib\app"
} elseif (Test-Path (Join-Path $ExtractTemp "lib\net9.0")) {
    Join-Path $ExtractTemp "lib\net9.0"
} elseif (Test-Path (Join-Path $ExtractTemp "current")) {
    Join-Path $ExtractTemp "current"
} else {
    $ExtractTemp
}

foreach ($dir in @($DestDir, $AppDataDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $PayloadDir "*") -Destination $dir -Recurse -Force
}

Write-Host "[+] Base runtime staged to: $DestDir" -ForegroundColor Green
