# ==============================================================================
# Kursor Desktop Build & Distribution Stager (scripts/build-dist.ps1)
# ==============================================================================
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SolutionPath = Join-Path $RepoRoot "src\desktop\Kursor.sln"
$BaseRuntimeDir = if ($env:HEYCLICKY_BASE_RUNTIME -and (Test-Path $env:HEYCLICKY_BASE_RUNTIME)) {
    $env:HEYCLICKY_BASE_RUNTIME
} else {
    Join-Path $env:LOCALAPPDATA "HeyClickyApp\current"
}
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"
$DistDesktopDir = $DistDir

# Resolve dotnet CLI dynamically across machines
$DotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$DotnetExe = if ($DotnetCmd) {
    $DotnetCmd.Source
} elseif (Test-Path (Join-Path $env:USERPROFILE ".dotnet\dotnet.exe")) {
    Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
} else {
    throw ".NET 9.0 SDK ('dotnet') not found in PATH or ~/.dotnet/dotnet.exe. Install from https://dotnet.microsoft.com/download/dotnet/9.0"
}

if (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe")) -and -not (Test-Path (Join-Path $BaseRuntimeDir "HeyClicky.exe"))) {
    $InstallRuntimeScript = Join-Path $PSScriptRoot "install-runtime.ps1"
    if (Test-Path $InstallRuntimeScript) {
        Write-Host "==> 0. Bootstrapping HeyClicky v1.1.5 base runtime assets..." -ForegroundColor Cyan
        & $InstallRuntimeScript
    }
}

Write-Host "==> 1. Building Kursor.sln (9 .NET 9.0 projects, Configuration=$Configuration)..." -ForegroundColor Cyan
& $DotnetExe build $SolutionPath -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

Write-Host "==> 2. Staging base runtime & native assets to $DistDir..." -ForegroundColor Cyan
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

if (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe")) -and (Test-Path $BaseRuntimeDir)) {
    # Copy base WinUI 3 / ClearScript V8 / ONNX / Vortice / HarnessRuntime files if not already staged
    $null = robocopy $BaseRuntimeDir $DistDir /E /XO /NFL /NDL /NJH /NJS /NC /NS
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE"
    } else {
        $global:LASTEXITCODE = 0
    }
} elseif (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe"))) {
    $InstallRuntimeScript = Join-Path $PSScriptRoot "install-runtime.ps1"
    if (Test-Path $InstallRuntimeScript) {
        Write-Host "[*] Base runtime not found locally; bootstrapping via scripts/install-runtime.ps1..." -ForegroundColor Yellow
        & $InstallRuntimeScript
    } else {
        Write-Warning "Base runtime directory ($BaseRuntimeDir) not found and dist/kursor-desktop is not pre-populated."
    }
}

Write-Host "==> 3. Overlaying compiled Kursor assemblies & patched V8 engine scripts..." -ForegroundColor Cyan
foreach ($procName in @("HeyClicky", "ClickyBrowserHost", "clicky-harness", "RestartAgent")) {
    Get-Process -Name $procName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

$CandidateBinDirs = @(
    (Join-Path $RepoRoot "src\desktop\Kursor.App\bin\$Configuration\net9.0"),
    (Join-Path $RepoRoot "src\desktop\Kursor.App\bin\x64\$Configuration\net9.0"),
    (Join-Path $RepoRoot "src\desktop\Kursor.App\bin\x64\$Configuration\net9.0-windows10.0.22621.0"),
    (Join-Path $RepoRoot "src\desktop\Kursor.App\bin\Debug\net9.0")
)
$AppBin = $CandidateBinDirs | Where-Object { Test-Path (Join-Path $_ "HeyClicky.Core.dll") } | Select-Object -First 1
if (-not $AppBin) {
    $AppBin = $CandidateBinDirs[0]
}
$AppBinDir = $AppBin

# Copy HeyClicky.exe if built in $AppBin
$BuiltExe = Join-Path $AppBin "HeyClicky.exe"
if (Test-Path $BuiltExe) {
    Copy-Item -Path $BuiltExe -Destination (Join-Path $DistDesktopDir "HeyClicky.exe") -Force
    Write-Host "   [+] Overlaid HeyClicky.exe" -ForegroundColor Green
}

# Copy all HeyClicky*.dll and Kursor*.dll assemblies to $DistDesktopDir
$BuiltDlls = Get-ChildItem -Path (Join-Path $AppBin "*") -Include "HeyClicky*.dll", "Kursor*.dll" -File -ErrorAction SilentlyContinue
foreach ($dll in $BuiltDlls) {
    Copy-Item -Path $dll.FullName -Destination (Join-Path $DistDesktopDir $dll.Name) -Force
    Write-Host "   [+] Overlaid $($dll.Name)" -ForegroundColor Green
}

# Include .pdb files only for Debug builds; exclude .pdb files when Configuration is Release
if ($Configuration -eq "Release") {
    Get-ChildItem -Path $DistDesktopDir -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
} else {
    $BuiltPdbs = Get-ChildItem -Path (Join-Path $AppBin "*") -Include "HeyClicky*.pdb", "Kursor*.pdb" -File -ErrorAction SilentlyContinue
    foreach ($pdb in $BuiltPdbs) {
        Copy-Item -Path $pdb.FullName -Destination (Join-Path $DistDesktopDir $pdb.Name) -Force
        Write-Host "   [+] Overlaid $($pdb.Name)" -ForegroundColor Green
    }
}

# Always overlay our patched 2D/3D V8 scripts so robocopy from BaseRuntimeDir never clobbers them
$DistEngineDir = Join-Path $DistDesktopDir "Assets\Engine"
if (-not (Test-Path $DistEngineDir)) {
    New-Item -ItemType Directory -Path $DistEngineDir -Force | Out-Null
}
$DrawingEngineAssets = Join-Path $RepoRoot "src\desktop\Kursor.Drawing\Assets\Engine\*"
if (Test-Path $DrawingEngineAssets) {
    Copy-Item -Path $DrawingEngineAssets -Destination $DistEngineDir -Force
    Write-Host "   [+] Overlaid patched 2D V8 Engine scripts (clicky-draw-pieces.js)" -ForegroundColor Green
}
$AppEngineDir = Join-Path $RepoRoot "src\desktop\Kursor.App\Assets\Engine"
foreach ($threeAsset in @("clicky-three.js", "clicky-three-parts.js")) {
    $threeSrc = Join-Path $AppEngineDir $threeAsset
    if (Test-Path $threeSrc) {
        Copy-Item -Path $threeSrc -Destination (Join-Path $DistEngineDir $threeAsset) -Force
        Write-Host "   [+] Overlaid 3D V8 Engine script ($threeAsset) from Kursor.App\Assets\Engine" -ForegroundColor Green
    }
}

# Overlay source-built ClickyBrowserHost sidecar (AUD-J-004)
$BrowserHostBin = Join-Path $RepoRoot "src\desktop\Kursor.BrowserHost\bin\$Configuration\net9.0"
if (Test-Path (Join-Path $BrowserHostBin "ClickyBrowserHost.exe")) {
    $DistBrowserHostDir = Join-Path $DistDesktopDir "ClickyBrowserHost"
    if (-not (Test-Path $DistBrowserHostDir)) {
        New-Item -ItemType Directory -Path $DistBrowserHostDir -Force | Out-Null
    }
    foreach ($asset in @("ClickyBrowserHost.exe", "ClickyBrowserHost.dll", "ClickyBrowserHost.runtimeconfig.json", "ClickyBrowserHost.deps.json")) {
        $srcAsset = Join-Path $BrowserHostBin $asset
        if (Test-Path $srcAsset) {
            Copy-Item -Path $srcAsset -Destination (Join-Path $DistBrowserHostDir $asset) -Force
        }
    }
    Write-Host "   [+] Overlaid source-built ClickyBrowserHost sidecar" -ForegroundColor Green
}

# Overlay source-built clicky-harness sidecar (AUD-J-005)
$HarnessBin = Join-Path $RepoRoot "src\desktop\Kursor.Harness\bin\$Configuration\net9.0"
if (Test-Path (Join-Path $HarnessBin "clicky-harness.exe")) {
    $DistHarnessDir = Join-Path $DistDesktopDir "HarnessRuntime"
    if (-not (Test-Path $DistHarnessDir)) {
        New-Item -ItemType Directory -Path $DistHarnessDir -Force | Out-Null
    }
    foreach ($asset in @("clicky-harness.exe", "clicky-harness.dll", "clicky-harness.runtimeconfig.json", "clicky-harness.deps.json")) {
        $srcAsset = Join-Path $HarnessBin $asset
        if (Test-Path $srcAsset) {
            Copy-Item -Path $srcAsset -Destination (Join-Path $DistHarnessDir $asset) -Force
        }
    }
    Write-Host "   [+] Overlaid source-built clicky-harness sidecar" -ForegroundColor Green
}

# Copy LICENSE and THIRD-PARTY-NOTICES.txt from repository root if present
foreach ($legalFile in @("LICENSE", "THIRD-PARTY-NOTICES.txt")) {
    $legalSrc = Join-Path $RepoRoot $legalFile
    if (Test-Path $legalSrc) {
        Copy-Item -Path $legalSrc -Destination (Join-Path $DistDesktopDir $legalFile) -Force
        Write-Host "   [+] Staged $legalFile to $DistDesktopDir" -ForegroundColor Green
    }
}

Write-Host "==> 4. Configuring Kursor local backend endpoints & pre-authenticated session..." -ForegroundColor Cyan
$SeedAuthScript = Join-Path $RepoRoot "scripts\seed-local-auth.ps1"
if (Test-Path $SeedAuthScript) {
    & $SeedAuthScript
}

Write-Host "`n[SUCCESS] Kursor Desktop Suite staged at: $DistDesktopDir" -ForegroundColor Green
Write-Host " Executable: $(Join-Path $DistDesktopDir 'HeyClicky.exe')" -ForegroundColor Green
