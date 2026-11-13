$ErrorActionPreference = "Stop"

# 1. Stop HeyClicky + sidecar processes so no DLLs in dist\kursor-desktop are locked
foreach ($procName in @("HeyClicky", "ClickyBrowserHost", "clicky-harness", "RestartAgent")) {
    Get-Process -Name $procName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

# 2. Overlay all 7 compiled Kursor DLLs + PDBs
$RepoRoot = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"
$AppBinDir = Join-Path $RepoRoot "src\desktop\Kursor.App\bin\Debug\net9.0"

$Assemblies = @(
    "HeyClicky.Contracts.dll",
    "HeyClicky.Contracts.pdb",
    "HeyClicky.Drawing3D.dll",
    "HeyClicky.Drawing3D.pdb",
    "HeyClicky.Drawing.dll",
    "HeyClicky.Drawing.pdb",
    "HeyClicky.Platform.dll",
    "HeyClicky.Platform.pdb",
    "HeyClicky.Core.dll",
    "HeyClicky.Core.pdb",
    "HeyClicky.Agents.dll",
    "HeyClicky.Agents.pdb",
    "HeyClicky.dll",
    "HeyClicky.pdb"
)

foreach ($file in $Assemblies) {
    $src = Join-Path $AppBinDir $file
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination (Join-Path $DistDir $file) -Force
        Write-Host "[+] Overlaid $file" -ForegroundColor Green
    } else {
        Write-Warning "[!] Missing: $src"
    }
}

# 3. Overlay patched V8 2D/3D engine scripts
$DistEngineDir = Join-Path $DistDir "Assets\Engine"
New-Item -ItemType Directory -Path $DistEngineDir -Force | Out-Null
$DrawingEngineAssets = Join-Path $RepoRoot "src\desktop\Kursor.Drawing\Assets\Engine\*"
if (Test-Path $DrawingEngineAssets) {
    Copy-Item -Path $DrawingEngineAssets -Destination $DistEngineDir -Force
    Write-Host "[+] Overlaid patched V8 Engine scripts" -ForegroundColor Green
}

# 4. Ensure endpoints.json & DPAPI auth session are intact
& (Join-Path $RepoRoot "scripts\seed-local-auth.ps1")

# 5. Live-verify POST /agent/realtime/speak on local backend
$speakBody = '{"voice":"Puck","text":"Hello! Kursor voice engine is online."}'
$speakResp = Invoke-WebRequest -Uri "http://127.0.0.1:8787/agent/realtime/speak" -Method Post -ContentType "application/json" -Body $speakBody -UseBasicParsing -TimeoutSec 10
Write-Host "[+] /agent/realtime/speak verified: HTTP $($speakResp.StatusCode), bytes=$($speakResp.RawContentLength)" -ForegroundColor Green

# 6. Relaunch patched HeyClicky.exe
$ExePath = Join-Path $DistDir "HeyClicky.exe"
$env:HEYCLICKY_WORKER_BASE_URL = "http://127.0.0.1:8787"
Start-Process -FilePath $ExePath -WorkingDirectory $DistDir

Start-Sleep -Seconds 5
$proc = Get-Process -Name "HeyClicky" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "[+] Patched HeyClicky running! PID=$($proc.Id -join ',')" -ForegroundColor Green
}

$logDir = Join-Path $env:LOCALAPPDATA "HeyClicky\logs"
$latestLog = Get-ChildItem -Path $logDir -Filter "HeyClicky-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latestLog) {
    Write-Host "`n--- Latest Log ($($latestLog.Name)) ---" -ForegroundColor Cyan
    Get-Content $latestLog.FullName | Where-Object { $_ -match "\[Auth\]|\[Mouth\]|\[Ears\]|\[Config\]|\[Billing\]|\[startup\]" }
}
