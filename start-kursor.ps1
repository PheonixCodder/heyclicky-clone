# ==============================================================================
# Kursor One-Command Launcher (start-kursor.ps1)
# Starts the self-hosted Kursor Backend (port 8787) and Kursor Desktop Suite.
# ==============================================================================
param(
    [switch]$Hidden,
    [switch]$Restart
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "src\backend"
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"
$ExePath = Join-Path $DistDir "HeyClicky.exe"

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  KURSOR — Self-Hosted HeyClicky v1.1.5 Desktop Suite " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 1. Ensure AppData directories & local endpoint overrides exist (critical on new devices)
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
    $sessionFile = Join-Path $appDataPath "session.json"
    if (-not (Test-Path $sessionFile)) {
        Set-Content -Path $sessionFile -Value $SessionJson -Encoding UTF8
    }
}

# 2. Build desktop distribution if missing
if (-not (Test-Path $ExePath)) {
    Write-Host "[!] Desktop distribution not built yet. Running scripts\build-dist.ps1..." -ForegroundColor Yellow
    & (Join-Path $RepoRoot "scripts\build-dist.ps1")
}

# 3. Ensure backend dependencies & .env file exist
$EnvFile = Join-Path $BackendDir ".env"
$EnvExample = Join-Path $BackendDir ".env.example"
if (-not (Test-Path $EnvFile) -and (Test-Path $EnvExample)) {
    Copy-Item -Path $EnvExample -Destination $EnvFile -Force
    Write-Host "[!] Created src\backend\.env from .env.example — remember to add your API keys!" -ForegroundColor Yellow
}

if (-not (Test-Path (Join-Path $BackendDir "node_modules"))) {
    Write-Host "[*] Installing backend npm dependencies..." -ForegroundColor Cyan
    Push-Location $BackendDir
    npm install
    Pop-Location
}

# 4. Check if backend is already listening on 127.0.0.1:8787
$BackendUp = $false
try {
    $resp = Invoke-RestMethod -Uri "http://127.0.0.1:8787/health" -TimeoutSec 2 -ErrorAction Stop
    if ($resp.status -eq "ok") {
        $BackendUp = $true
    }
} catch {
    $BackendUp = $false
}

if (-not $BackendUp) {
    Write-Host "[*] Starting Kursor Backend Server on http://127.0.0.1:8787..." -ForegroundColor Yellow
    $DataDir = Join-Path $BackendDir "data"
    if (-not (Test-Path $DataDir)) {
        New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
    }
    if ($Hidden) {
        $LogFile = Join-Path $DataDir "backend-autostart.log"
        Start-Process -FilePath "cmd.exe" `
            -ArgumentList "/c npm start >> `"$LogFile`" 2>&1" `
            -WorkingDirectory $BackendDir `
            -WindowStyle Hidden
    } else {
        Start-Process -FilePath "powershell.exe" `
            -ArgumentList "-NoExit", "-Command", "Set-Location '$BackendDir'; npm run dev" `
            -WorkingDirectory $BackendDir
    }
    Start-Sleep -Seconds 3
} else {
    Write-Host "[+] Kursor Backend Server is already running on http://127.0.0.1:8787" -ForegroundColor Green
}

# 5. Launch Kursor Desktop Suite (HeyClicky.exe)
$ExistingApp = Get-Process -Name "HeyClicky" -ErrorAction SilentlyContinue
if ($ExistingApp -and $Restart) {
    Write-Host "[*] Restarting existing HeyClicky.exe process..." -ForegroundColor Yellow
    Stop-Process -Name "HeyClicky" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    $ExistingApp = $null
}

$env:HEYCLICKY_WORKER_BASE_URL = "http://127.0.0.1:8787"
$env:HEYCLICKY_SUPABASE_URL = "http://127.0.0.1:8787"
$env:HEYCLICKY_SUPABASE_ANON_KEY = "kursor-local-anon-key"

if (-not $ExistingApp) {
    Write-Host "[+] Launching Kursor Desktop Suite ($ExePath)..." -ForegroundColor Green
    Start-Process -FilePath $ExePath -WorkingDirectory $DistDir
} else {
    Write-Host "[+] Kursor Desktop Suite is already running (PID: $($ExistingApp.Id -join ', '))" -ForegroundColor Green
}
