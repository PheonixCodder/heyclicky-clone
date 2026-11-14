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
$DesktopExe = Join-Path $DistDir "HeyClicky.exe"
$ExePath = $DesktopExe

# 0. Ensure Node / npm / fnm / dotnet paths are in PATH
$fnmExe = Get-Command fnm -ErrorAction SilentlyContinue
if (-not $fnmExe -and (Test-Path "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Schniz.fnm_Microsoft.Winget.Source_8wekyb3d8bbwe\fnm.exe")) {
    $fnmExe = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Schniz.fnm_Microsoft.Winget.Source_8wekyb3d8bbwe\fnm.exe"
}
if ($fnmExe) {
    try {
        & $fnmExe env --shell powershell | Out-String | Invoke-Expression
    } catch {}
}
if (Test-Path "$env:APPDATA\fnm\aliases\default") {
    $env:PATH = "$env:APPDATA\fnm\aliases\default;$env:PATH"
}
if (Test-Path "$env:USERPROFILE\.dotnet") {
    $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
}

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  KURSOR — Self-Hosted HeyClicky v1.1.5 Desktop Suite " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 0b. Handle -Restart: stop HeyClicky, sidecars, and any process listening on TCP port 8787
if ($Restart) {
    Write-Host "[*] Restarting Kursor: stopping HeyClicky, clicky-harness, ClickyBrowserHost, and port 8787 listener..." -ForegroundColor Yellow
    foreach ($procName in @("HeyClicky", "clicky-harness", "ClickyBrowserHost", "RestartAgent")) {
        Get-Process -Name $procName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    $Port8787Listeners = Get-NetTCPConnection -LocalPort 8787 -State Listen -ErrorAction SilentlyContinue
    if ($Port8787Listeners) {
        foreach ($conn in $Port8787Listeners) {
            if ($conn.OwningProcess -gt 0) {
                Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
            }
        }
    }
    Start-Sleep -Seconds 1
}

# 1. Ensure AppData directories, camelCase/PascalCase endpoints.json & DPAPI auth session exist
$SeedAuthScript = Join-Path $RepoRoot "scripts\seed-local-auth.ps1"
if (Test-Path $SeedAuthScript) {
    & $SeedAuthScript
}

# 2. Build desktop distribution if missing
if (-not (Test-Path $DesktopExe)) {
    Write-Host "[!] Desktop distribution not built yet. Building via scripts\build-dist.ps1..." -ForegroundColor Yellow
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
function Test-BackendRunning {
    try {
        $resp = Invoke-RestMethod -Uri "http://127.0.0.1:8787/health" -TimeoutSec 2 -ErrorAction Stop
        return ($resp.status -eq "ok")
    } catch {
        return $false
    }
}

$BackendUp = Test-BackendRunning

if (-not $BackendUp) {
    Write-Host "[*] Starting Kursor Backend Server on http://127.0.0.1:8787..." -ForegroundColor Yellow
    $DataDir = Join-Path $BackendDir "data"
    if (-not (Test-Path $DataDir)) {
        New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
    }
    
    $NodeCmd = Get-Command node -ErrorAction SilentlyContinue
    $NodeExe = if ($NodeCmd) { $NodeCmd.Source } elseif (Test-Path "$env:APPDATA\fnm\aliases\default\node.exe") { "$env:APPDATA\fnm\aliases\default\node.exe" } else { "node.exe" }
    $TsxCli = Join-Path $BackendDir "node_modules\tsx\dist\cli.mjs"

    if ($Hidden) {
        $OutLogFile = Join-Path $DataDir "backend-autostart.out.log"
        $ErrLogFile = Join-Path $DataDir "backend-autostart.err.log"
        Start-Process -FilePath $NodeExe `
            -ArgumentList "`"$TsxCli`"", "--env-file-if-exists=.env", "src/index.ts" `
            -WorkingDirectory $BackendDir `
            -WindowStyle Hidden `
            -RedirectStandardOutput $OutLogFile `
            -RedirectStandardError $ErrLogFile
    } else {
        $OutLogFile = Join-Path $DataDir "backend.out.log"
        $ErrLogFile = Join-Path $DataDir "backend.err.log"
        Start-Process -FilePath $NodeExe `
            -ArgumentList "`"$TsxCli`"", "--env-file-if-exists=.env", "src/index.ts" `
            -WorkingDirectory $BackendDir `
            -WindowStyle Hidden `
            -RedirectStandardOutput $OutLogFile `
            -RedirectStandardError $ErrLogFile
    }
    for ($i = 0; $i -lt 20; $i++) {
        if (Test-BackendRunning) { break }
        Start-Sleep -Milliseconds 500
    }
} else {
    Write-Host "[+] Kursor Backend Server is already running on http://127.0.0.1:8787" -ForegroundColor Green
}

# 5. Launch Kursor Desktop Suite (dist\kursor-desktop\HeyClicky.exe)
$RunningHeyClicky = @(Get-Process -Name "HeyClicky" -ErrorAction SilentlyContinue)
$ForeignHeyClicky = @($RunningHeyClicky | Where-Object { $_.Path -and ($_.Path -ne $DesktopExe) })
if ($ForeignHeyClicky.Count -gt 0) {
    Write-Host "[!] Stopping foreign HeyClicky.exe instance ($($ForeignHeyClicky.Path -join ', ')) to prevent mutex/protocol collision..." -ForegroundColor Yellow
    $ForeignHeyClicky | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    if (Test-Path $SeedAuthScript) {
        & $SeedAuthScript
    }
}

$ExistingApp = @(Get-Process -Name "HeyClicky" -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $DesktopExe })

$env:HEYCLICKY_WORKER_BASE_URL = "http://127.0.0.1:8787"
$env:HEYCLICKY_SUPABASE_URL = "http://127.0.0.1:8787"
$env:HEYCLICKY_SUPABASE_ANON_KEY = "kursor-local-anon-key"

if (-not $ExistingApp) {
    if (-not (Test-Path $DesktopExe)) {
        Write-Host "[*] Desktop executable still missing at $DesktopExe; running scripts\build-dist.ps1..." -ForegroundColor Yellow
        & (Join-Path $RepoRoot "scripts\build-dist.ps1")
    }
    if (-not (Test-Path $DesktopExe)) {
        throw "Kursor Desktop executable not found at $DesktopExe after running scripts\build-dist.ps1."
    }
    Write-Host "[+] Launching Kursor Desktop Suite ($DesktopExe)..." -ForegroundColor Green
    Start-Process -FilePath $DesktopExe -WorkingDirectory $DistDir
} else {
    Write-Host "[+] Kursor Desktop Suite is already running (PID: $($ExistingApp.Id -join ', '))" -ForegroundColor Green
}
