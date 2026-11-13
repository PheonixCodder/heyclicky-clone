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
$InstalledExe = Join-Path $env:LOCALAPPDATA "HeyClickyApp\current\HeyClicky.exe"
if (-not (Test-Path $ExePath) -and (Test-Path $InstalledExe)) {
    $ExePath = $InstalledExe
    $DistDir = Split-Path -Parent $InstalledExe
}

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

# 1. Ensure AppData directories, camelCase endpoints.json & DPAPI auth session exist
$SeedAuthScript = Join-Path $RepoRoot "scripts\seed-local-auth.ps1"
if (Test-Path $SeedAuthScript) {
    & $SeedAuthScript
}

# 2. Build desktop distribution if missing
if (-not (Test-Path $ExePath)) {
    Write-Host "[!] Desktop distribution not built yet. Attempting build via scripts\build-dist.ps1..." -ForegroundColor Yellow
    try {
        & (Join-Path $RepoRoot "scripts\build-dist.ps1")
    } catch {
        Write-Warning "Desktop build step skipped: $_"
    }
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
    
    $NodeCmd = Get-Command node -ErrorAction SilentlyContinue
    $NodeExe = if ($NodeCmd) { $NodeCmd.Source } elseif (Test-Path "$env:APPDATA\fnm\aliases\default\node.exe") { "$env:APPDATA\fnm\aliases\default\node.exe" } else { "node.exe" }
    $TsxCli = Join-Path $BackendDir "node_modules\tsx\dist\cli.mjs"

    if ($Hidden) {
        $LogFile = Join-Path $DataDir "backend-autostart.log"
        Start-Process -FilePath $NodeExe `
            -ArgumentList "`"$TsxCli`"", "--env-file-if-exists=.env", "src/index.ts" `
            -WorkingDirectory $BackendDir `
            -WindowStyle Hidden `
            -RedirectStandardOutput $LogFile `
            -RedirectStandardError $LogFile
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

$InstallerExe = Join-Path $RepoRoot "executable-file\HeyClicky-Setup.exe"

if (-not $ExistingApp) {
    if (Test-Path $ExePath) {
        Write-Host "[+] Launching Kursor Desktop Suite ($ExePath)..." -ForegroundColor Green
        Start-Process -FilePath $ExePath -WorkingDirectory $DistDir
    } elseif (Test-Path $InstallerExe) {
        Write-Host "[*] Launching HeyClicky Desktop Setup ($InstallerExe)..." -ForegroundColor Cyan
        Start-Process -FilePath $InstallerExe
    } else {
        Write-Host "[!] HeyClicky desktop client executable not found at $ExePath." -ForegroundColor Yellow
        Write-Host "    Backend server is running on http://127.0.0.1:8787 and ready for the client." -ForegroundColor Green
        if ($Hidden) {
            Add-Type -AssemblyName System.Windows.Forms
            [System.Windows.Forms.MessageBox]::Show(
                "Kursor Backend Server is running at http://127.0.0.1:8787!`n`nTo open the visual desktop buddy, the HeyClicky Windows app installer is downloading/setting up.",
                "Kursor Assistant",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            )
        }
    }
} else {
    Write-Host "[+] Kursor Desktop Suite is already running (PID: $($ExistingApp.Id -join ', '))" -ForegroundColor Green
}
