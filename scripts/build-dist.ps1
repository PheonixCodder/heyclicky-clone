# ==============================================================================
# Kursor Desktop Build & Distribution Stager (scripts/build-dist.ps1)
# ==============================================================================
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SolutionPath = Join-Path $RepoRoot "src\desktop\Kursor.sln"
$BaseRuntimeDir = if ($env:HEYCLICKY_BASE_RUNTIME -and (Test-Path $env:HEYCLICKY_BASE_RUNTIME)) {
    $env:HEYCLICKY_BASE_RUNTIME
} else {
    Join-Path $env:LOCALAPPDATA "HeyClickyApp\current"
}
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"

# Resolve dotnet CLI dynamically across machines
$DotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$DotnetExe = if ($DotnetCmd) {
    $DotnetCmd.Source
} elseif (Test-Path (Join-Path $env:USERPROFILE ".dotnet\dotnet.exe")) {
    Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
} else {
    throw ".NET 9.0 SDK ('dotnet') not found in PATH or ~/.dotnet/dotnet.exe. Install from https://dotnet.microsoft.com/download/dotnet/9.0"
}

Write-Host "==> 1. Building Kursor.sln (7 .NET 9.0 projects)..." -ForegroundColor Cyan
& $DotnetExe build $SolutionPath -c Debug --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

Write-Host "==> 2. Staging base runtime & native assets to $DistDir..." -ForegroundColor Cyan
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

if (Test-Path $BaseRuntimeDir) {
    # Copy base WinUI 3 / ClearScript V8 / ONNX / Vortice / HarnessRuntime files if not already staged
    $null = robocopy $BaseRuntimeDir $DistDir /E /XO /NFL /NDL /NJH /NJS /NC /NS
} elseif (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe"))) {
    Write-Warning "Base runtime directory ($BaseRuntimeDir) not found and dist/kursor-desktop is not pre-populated."
}

Write-Host "==> 3. Overlaying compiled Kursor assemblies & patched V8 engine scripts..." -ForegroundColor Cyan
$AppBinDir = Join-Path $RepoRoot "src\desktop\Kursor.App\bin\x64\Debug\net9.0-windows10.0.22621.0"
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
        Write-Host "   [+] Overlaid $file" -ForegroundColor Green
    } else {
        Write-Warning "   [!] Missing expected build artifact: $src"
    }
}

# Always overlay our patched 2D/3D V8 scripts so robocopy from BaseRuntimeDir never clobbers them
$DistEngineDir = Join-Path $DistDir "Assets\Engine"
if (-not (Test-Path $DistEngineDir)) {
    New-Item -ItemType Directory -Path $DistEngineDir -Force | Out-Null
}
$DrawingEngineAssets = Join-Path $RepoRoot "src\desktop\Kursor.Drawing\Assets\Engine\*"
$Drawing3DEngineAssets = Join-Path $RepoRoot "src\desktop\Kursor.Drawing3D\Assets\Engine\*"
if (Test-Path $DrawingEngineAssets) {
    Copy-Item -Path $DrawingEngineAssets -Destination $DistEngineDir -Force
    Write-Host "   [+] Overlaid patched 2D V8 Engine scripts (clicky-draw-pieces.js)" -ForegroundColor Green
}
if (Test-Path $Drawing3DEngineAssets) {
    Copy-Item -Path $Drawing3DEngineAssets -Destination $DistEngineDir -Force
    Write-Host "   [+] Overlaid 3D V8 Engine scripts (clicky-three.js)" -ForegroundColor Green
}

Write-Host "==> 4. Configuring Kursor local backend endpoints & pre-authenticated session..." -ForegroundColor Cyan
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
Set-Content -Path (Join-Path $DistDir "endpoints.json") -Value $EndpointsJson -Encoding UTF8

Write-Host "`n[SUCCESS] Kursor Desktop Suite staged at: $DistDir" -ForegroundColor Green
Write-Host " Executable: $(Join-Path $DistDir 'HeyClicky.exe')" -ForegroundColor Green
