# ==============================================================================
# Kursor Portable Release Packager (scripts/package-portable.ps1)
# Creates a self-contained portable archive (release/Kursor-Portable-win-x64.zip)
# that can be copied to any Windows 10/11 PC and installed via setup-kursor.cmd.
# ==============================================================================
param(
    [switch]$IncludeEnv
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ReleaseDir = Join-Path $RepoRoot "release"
$StageDir = Join-Path $ReleaseDir "Kursor-Portable"
$ZipPath = Join-Path $ReleaseDir "Kursor-Portable-win-x64.zip"
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"

if (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe"))) {
    Write-Host "[*] Building dist\kursor-desktop first..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "build-dist.ps1")
}

Write-Host "==> 1. Preparing staging folder at $StageDir..." -ForegroundColor Cyan
if (Test-Path $StageDir) {
    Remove-Item -Path $StageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $StageDir -Force | Out-Null

# Copy root launcher & setup scripts + docs
$RootFiles = @(
    "README.md",
    "AGENTS.md",
    ".gitignore",
    "setup-kursor.ps1",
    "setup-kursor.cmd",
    "start-kursor.ps1",
    "start-kursor.cmd"
)
foreach ($f in $RootFiles) {
    $srcPath = Join-Path $RepoRoot $f
    if (Test-Path $srcPath) {
        Copy-Item -Path $srcPath -Destination (Join-Path $StageDir $f) -Force
    }
}

# Copy scripts/
Copy-Item -Path (Join-Path $RepoRoot "scripts") -Destination (Join-Path $StageDir "scripts") -Recurse -Force

# Copy docs/
if (Test-Path (Join-Path $RepoRoot "docs")) {
    Copy-Item -Path (Join-Path $RepoRoot "docs") -Destination (Join-Path $StageDir "docs") -Recurse -Force
}

# Copy src/backend (excluding node_modules, data, and .env unless -IncludeEnv is specified)
Write-Host "==> 2. Staging Kursor Backend (src\backend)..." -ForegroundColor Cyan
$BackendStage = Join-Path $StageDir "src\backend"
New-Item -ItemType Directory -Path $BackendStage -Force | Out-Null
Copy-Item -Path (Join-Path $RepoRoot "src\backend\src") -Destination (Join-Path $BackendStage "src") -Recurse -Force
foreach ($bf in @("package.json", "package-lock.json", "tsconfig.json", ".env.example")) {
    $bsrc = Join-Path $RepoRoot "src\backend\$bf"
    if (Test-Path $bsrc) {
        Copy-Item -Path $bsrc -Destination (Join-Path $BackendStage $bf) -Force
    }
}
if ($IncludeEnv -and (Test-Path (Join-Path $RepoRoot "src\backend\.env"))) {
    Copy-Item -Path (Join-Path $RepoRoot "src\backend\.env") -Destination (Join-Path $BackendStage ".env") -Force
    Write-Host "   [!] Included src\backend\.env (API keys bundled for personal device transfer)" -ForegroundColor Yellow
}

# Copy pre-built dist/kursor-desktop
Write-Host "==> 3. Staging Kursor Desktop Suite (dist\kursor-desktop)..." -ForegroundColor Cyan
$DistStage = Join-Path $StageDir "dist\kursor-desktop"
New-Item -ItemType Directory -Path $DistStage -Force | Out-Null
$null = robocopy $DistDir $DistStage /E /NFL /NDL /NJH /NJS /NC /NS

# Compress into zip
Write-Host "==> 4. Compressing portable archive to $ZipPath..." -ForegroundColor Cyan
if (Test-Path $ZipPath) {
    Remove-Item -Path $ZipPath -Force
}
Compress-Archive -Path "$StageDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal

$ZipSizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Write-Host "`n[SUCCESS] Portable package created: $ZipPath ($ZipSizeMB MB)" -ForegroundColor Green
Write-Host "  Copy this zip to any Windows 10/11 PC, extract it, and run setup-kursor.cmd!" -ForegroundColor Gray
