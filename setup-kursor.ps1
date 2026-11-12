# ==============================================================================
# Kursor One-Step Device Setup (setup-kursor.ps1)
# Sets up Kursor on a new Windows 10/11 PC:
#   1. Verifies Node.js (>= 22) and installs backend npm dependencies
#   2. Initializes src/backend/.env from .env.example if missing
#   3. Initializes %LOCALAPPDATA%\HeyClicky & %LOCALAPPDATA%\Kursor configs
#   4. Builds or verifies dist\kursor-desktop\HeyClicky.exe
#   5. Registers Windows Autostart & Desktop shortcut
#   6. Launches Kursor Backend + Desktop Suite
# ==============================================================================
param(
    [switch]$SkipAutostart,
    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "src\backend"
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"
$ExePath = Join-Path $DistDir "HeyClicky.exe"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  KURSOR — Multi-Device Setup & Autostart Installer       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verify Node.js is installed
$NodeCmd = Get-Command node -ErrorAction SilentlyContinue
if (-not $NodeCmd) {
    throw "Node.js (v22+ recommended) is required to run the local Kursor Backend (`node:sqlite`). Install from https://nodejs.org/"
}
$NodeVersion = (& node -v)
Write-Host "[+] Detected Node.js $NodeVersion" -ForegroundColor Green

# 2. Install Backend Dependencies & Create .env
Write-Host "`n==> 1. Setting up Kursor Backend (src\backend)..." -ForegroundColor Cyan
Push-Location $BackendDir
npm install
Pop-Location

$EnvFile = Join-Path $BackendDir ".env"
$EnvExample = Join-Path $BackendDir ".env.example"
if (-not (Test-Path $EnvFile) -and (Test-Path $EnvExample)) {
    Copy-Item -Path $EnvExample -Destination $EnvFile -Force
    Write-Host "   [!] Created src\backend\.env from .env.example." -ForegroundColor Yellow
    Write-Host "   [!] IMPORTANT: Open src\backend\.env and add your OPENROUTER_API_KEY / GEMINI_API_KEY / OPENAI_API_KEY / GROQ_API_KEY." -ForegroundColor Yellow
} else {
    Write-Host "   [+] Found existing src\backend\.env" -ForegroundColor Green
}

# 3. Initialize %LOCALAPPDATA%\HeyClicky and %LOCALAPPDATA%\Kursor
Write-Host "`n==> 2. Initializing local AppData configuration..." -ForegroundColor Cyan
$EndpointsJson = @"
{
  "WorkerBaseUrl": "http://127.0.0.1:8787",
  "SupabaseUrl": "http://127.0.0.1:8787",
  "SupabaseAnonKey": "kursor-local-anon-key"
}
"@

$SessionJson = @"
{
  "AccessToken": "kursor-local-access-token",
  "RefreshToken": "kursor-local-refresh-token",
  "ExpiresAtUnixSeconds": 4102444800,
  "User": {
    "Id": "00000000-0000-0000-0000-000000000001",
    "Email": "local@kursor.local"
  }
}
"@

foreach ($dirName in @("Kursor", "HeyClicky")) {
    $appDataPath = Join-Path $env:LOCALAPPDATA $dirName
    if (-not (Test-Path $appDataPath)) {
        New-Item -ItemType Directory -Path $appDataPath -Force | Out-Null
    }
    Set-Content -Path (Join-Path $appDataPath "endpoints.json") -Value $EndpointsJson -Encoding UTF8
    Set-Content -Path (Join-Path $appDataPath "session.json") -Value $SessionJson -Encoding UTF8
    $codexHome = Join-Path $appDataPath "codex-home"
    if (-not (Test-Path $codexHome)) {
        New-Item -ItemType Directory -Path $codexHome -Force | Out-Null
    }
    Write-Host "   [+] Initialized $appDataPath (endpoints.json + session.json)" -ForegroundColor Green
}

# 4. Verify or Build Desktop Distribution
Write-Host "`n==> 3. Checking Kursor Desktop Binaries..." -ForegroundColor Cyan
if (Test-Path $ExePath) {
    Write-Host "   [+] Pre-built Kursor Desktop Suite found at: $ExePath" -ForegroundColor Green
    Set-Content -Path (Join-Path $DistDir "endpoints.json") -Value $EndpointsJson -Encoding UTF8
} elseif (Test-Path (Join-Path $RepoRoot "src\desktop\Kursor.sln")) {
    Write-Host "   [*] Building Kursor Desktop Suite from source..." -ForegroundColor Yellow
    & (Join-Path $RepoRoot "scripts\build-dist.ps1")
} else {
    throw "Neither dist\kursor-desktop\HeyClicky.exe nor src\desktop\Kursor.sln was found."
}

# 5. Configure Windows Autostart & Shortcuts
if (-not $SkipAutostart) {
    Write-Host "`n==> 4. Registering Windows Autostart & Desktop Shortcut..." -ForegroundColor Cyan
    & (Join-Path $RepoRoot "scripts\install-autostart.ps1")
}

# 6. Start Kursor
if (-not $SkipLaunch) {
    Write-Host "`n==> 5. Launching Kursor..." -ForegroundColor Cyan
    & (Join-Path $RepoRoot "start-kursor.ps1")
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  KURSOR SETUP COMPLETE!                                  " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
