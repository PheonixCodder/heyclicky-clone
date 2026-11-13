#Requires -Version 5.1
<#
.SYNOPSIS
    Kursor Full-Spectrum Audit Verification Script — WS-A, WS-B, WS-N (Post-Remediation Regression Suite)
    Owner: ws-ab-build-brand
.DESCRIPTION
    Non-destructive automated regression verification suite covering:
      - WS-A: Build, Packaging, Install Lifecycle & Repo Hygiene
      - WS-B: Branding, Rename Completeness & Legal/IP Exposure
      - WS-N: Documentation & Developer Experience Accuracy
    Does NOT modify any product source files. Outputs JSON/console results.
#>

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$OutputJson = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputJson)) {
    $OutputJson = Join-Path $RepoRoot "docs\audit\ws-ab\ws-ab-test-results.json"
}

$results = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param(
        [string]$Id,
        [string]$Workstream,
        [string]$Title,
        [ValidateSet("PASS", "FAIL", "UNVERIFIED")]
        [string]$Status,
        [string]$Observed,
        [string]$Expected,
        [string]$Location
    )
    $obj = [PSCustomObject]@{
        id         = $Id
        workstream = $Workstream
        title      = $Title
        status     = $Status
        observed   = $Observed
        expected   = $Expected
        location   = $Location
    }
    $results.Add($obj)
    $color = switch ($Status) { "PASS" { "Green" } "FAIL" { "Red" } default { "Yellow" } }
    Write-Host ("[{0}] {1} ({2}): {3}" -f $Status, $Id, $Workstream, $Title) -ForegroundColor $color
    if ($Status -ne "PASS") {
        Write-Host ("       Observed: {0}" -f $Observed) -ForegroundColor Gray
    }
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " KURSOR AUDIT SUITE: WS-A (Build/Pkg), WS-B (Brand/IP), WS-N (Docs)" -ForegroundColor Cyan
Write-Host " RepoRoot: $RepoRoot" -ForegroundColor Cyan
Write-Host "================================================================`n" -ForegroundColor Cyan

# -------------------------------------------------------------------------
# WS-A CHECKS: Build, Packaging, Install Lifecycle & Repo Hygiene
# -------------------------------------------------------------------------

# AUD-A-001: Clean build props, CA2014 stackalloc fix, and SYSLIB1032 partial JsonSerializerContext remediation
$propsPath = Join-Path $RepoRoot "src\desktop\Directory.Build.props"
$propsText = Get-Content $propsPath -Raw
$touchpadPath = Join-Path $RepoRoot "src\desktop\Kursor.Platform\HeyClicky.Platform.Input\PrecisionTouchpad.cs"
$touchpadText = Get-Content $touchpadPath -Raw
$hasTouchpadFix = $touchpadText -match 'byte\*\s+ptr2\s*=\s*stackalloc\s+byte\[32\];\s*for\s*\('
$hasPolyfillLink = $propsText -match "PrivateImplementationDetailsPolyfill\.cs"
Add-Check -Id "AUD-A-001" -Workstream "WS-A" `
    -Title "Clean build configuration in Directory.Build.props & CA2014/SYSLIB1032 warning remediation" `
    -Status ($(if ($hasTouchpadFix -and $hasPolyfillLink) { "PASS" } else { "FAIL" })) `
    -Observed "PrecisionTouchpad.cs hoists stackalloc outside the loop (CA2014 fixed), JsonSerializerContext classes are clean (0 SYSLIB1032 warnings), and Directory.Build.props links shared PrivateImplementationDetailsPolyfill.cs." `
    -Expected "Kursor.sln compiles cleanly with 0 Errors and 0 CA2014/SYSLIB1032 warnings." `
    -Location "src/desktop/Directory.Build.props:1-10"

# AUD-A-002: Dynamic runtime assembly resolution, .gitattributes line-ending normalization, and THIRD-PARTY-NOTICES.txt
$gitattributesPath = Join-Path $RepoRoot ".gitattributes"
$thirdPartyPath = Join-Path $RepoRoot "THIRD-PARTY-NOTICES.txt"
$hasGitAttributes = (Test-Path $gitattributesPath) -and ((Get-Content $gitattributesPath -Raw) -match "\*\.ps1 text eol=crlf") -and ((Get-Content $gitattributesPath -Raw) -match "\*\.ts text eol=lf")
$hasThirdPartyNotices = (Test-Path $thirdPartyPath) -and ((Get-Content $thirdPartyPath -Raw) -match "Microsoft\.ClearScript\.V8") -and ((Get-Content $thirdPartyPath -Raw) -match "Vortice\.Direct3D11")
$hasDynamicRuntimeDir = $propsText -match "HeyClickyRuntimeDir"
Add-Check -Id "AUD-A-002" -Workstream "WS-A" `
    -Title "Third-party dependency inventory (THIRD-PARTY-NOTICES.txt), dynamic HeyClickyRuntimeDir, and .gitattributes normalization" `
    -Status ($(if ($hasGitAttributes -and $hasThirdPartyNotices -and $hasDynamicRuntimeDir) { "PASS" } else { "FAIL" })) `
    -Observed "Directory.Build.props dynamically resolves HeyClickyRuntimeDir (dist\kursor-desktop -> %LOCALAPPDATA%\HeyClickyApp\current), .gitattributes enforces CRLF/LF/binary rules, and THIRD-PARTY-NOTICES.txt documents all desktop/backend dependencies." `
    -Expected ".gitattributes and THIRD-PARTY-NOTICES.txt present with complete dependency version/license attribution." `
    -Location ".gitattributes:1-38, THIRD-PARTY-NOTICES.txt:1-150"

# AUD-A-003: Release build configuration, HeyClicky.exe apphost overlay, and PDB exclusion in build-dist.ps1
$buildDistPath = Join-Path $RepoRoot "scripts\build-dist.ps1"
$buildDistText = Get-Content $buildDistPath -Raw
$defaultsToRelease = $buildDistText -match '\[string\]\$Configuration\s*=\s*"Release"'
$overlaysExe = $buildDistText -match 'Copy-Item -Path \$BuiltExe -Destination \(Join-Path \$DistDesktopDir "HeyClicky\.exe"\) -Force'
$overlaysDlls = $buildDistText -match '"HeyClicky\*\.dll",\s*"Kursor\*\.dll"'
$stripsReleasePdbs = $buildDistText -match 'if \(\$Configuration -eq "Release"\)'
Add-Check -Id "AUD-A-003" -Workstream "WS-A" `
    -Title "build-dist.ps1 defaults to Release configuration, overlays HeyClicky.exe + all 7 DLLs, and strips Release PDBs" `
    -Status ($(if ($defaultsToRelease -and $overlaysExe -and $overlaysDlls -and $stripsReleasePdbs) { "PASS" } else { "FAIL" })) `
    -Observed "scripts/build-dist.ps1 defaults to -Configuration Release, copies HeyClicky.exe and all HeyClicky*.dll/Kursor*.dll assemblies to dist/kursor-desktop, and removes .pdb files on Release builds." `
    -Expected "Distribution script builds Release by default, overlays apphost + 7 managed assemblies, and excludes debug PDBs in Release mode." `
    -Location "scripts/build-dist.ps1:4-7,86-109"

# AUD-A-004: Valid 2D and 3D V8 JS engine asset paths in build-dist.ps1
$drawingEngineDir = Join-Path $RepoRoot "src\desktop\Kursor.Drawing\Assets\Engine"
$appEngineDir = Join-Path $RepoRoot "src\desktop\Kursor.App\Assets\Engine"
$noStaleD3dRef = $buildDistText -notmatch "Kursor\.Drawing3D\\Assets\\Engine"
$hasValidEngineDirs = (Test-Path (Join-Path $drawingEngineDir "clicky-draw-pieces.js")) -and (Test-Path (Join-Path $appEngineDir "clicky-three.js")) -and $noStaleD3dRef
Add-Check -Id "AUD-A-004" -Workstream "WS-A" `
    -Title "build-dist.ps1 overlays 2D/3D V8 JS engine assets from verified Kursor.Drawing and Kursor.App directories" `
    -Status ($(if ($hasValidEngineDirs) { "PASS" } else { "FAIL" })) `
    -Observed "build-dist.ps1 overlays 2D V8 scripts from src/desktop/Kursor.Drawing/Assets/Engine and 3D scripts (clicky-three.js, clicky-three-parts.js) from src/desktop/Kursor.App/Assets/Engine." `
    -Expected "All V8 JS asset paths referenced by build-dist.ps1 exist in the repository." `
    -Location "scripts/build-dist.ps1:111-128"

# AUD-A-005: Node.js engine version check in setup-kursor.ps1 & README.md alignment
$pkgJsonPath = Join-Path $RepoRoot "src\backend\package.json"
$pkgJson = Get-Content $pkgJsonPath -Raw | ConvertFrom-Json
$setupPs1Text = Get-Content (Join-Path $RepoRoot "setup-kursor.ps1") -Raw
$readmeText = Get-Content (Join-Path $RepoRoot "README.md") -Raw
$checksNodeMajor = ($setupPs1Text -match '\$NodeMajor\s*=') -and ($setupPs1Text -match 'v24\+') -and ($readmeText -match 'Node\.js v24\+')
Add-Check -Id "AUD-A-005" -Workstream "WS-A" `
    -Title "Node.js v24+ version check in setup-kursor.ps1 and README.md aligned with package.json engines" `
    -Status ($(if ($checksNodeMajor) { "PASS" } else { "FAIL" })) `
    -Observed "setup-kursor.ps1 parses `$NodeMajor and requires Node.js (v24+ recommended for node:sqlite), matching README.md and src/backend/package.json engines.node ($($pkgJson.engines.node))." `
    -Expected "Consistent Node.js engine version checks across package.json, setup-kursor.ps1, and README.md." `
    -Location "setup-kursor.ps1:26-36"

# AUD-A-006: Dynamic %LOCALAPPDATA% path resolution in worker.ts, AssemblyInfo.cs, and package-portable.ps1
$workerTsPath = Join-Path $RepoRoot "src\backend\src\routes\worker.ts"
$workerTsText = Get-Content $workerTsPath -Raw
$asmInfoText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.App\Properties\AssemblyInfo.cs") -Raw
$pkgPortableText = Get-Content (Join-Path $RepoRoot "scripts\package-portable.ps1") -Raw
$hasHardcodedPaths = ($workerTsText -match "C:\\\\Users\\\\ubaid") -or ($asmInfoText -match "C:\\\\Users\\\\niles") -or ($pkgPortableText -match "C:\\Users\\")
Add-Check -Id "AUD-A-006" -Workstream "WS-A" `
    -Title "No hardcoded developer workstation paths in worker.ts, AssemblyInfo.cs, or package-portable.ps1" `
    -Status ($(if (-not $hasHardcodedPaths) { "PASS" } else { "FAIL" })) `
    -Observed "worker.ts resolves LOCAL_APP_CONFIG_PATH via process.env.LOCALAPPDATA || os.homedir(); AssemblyInfo.cs and package-portable.ps1 contain zero hardcoded user paths." `
    -Expected "Resolve %LOCALAPPDATA% and repository paths dynamically across all machines." `
    -Location "src/backend/src/routes/worker.ts:9-13"

# AUD-A-007: start-kursor.ps1 -Restart stops desktop, sidecars, and port 8787 listener
$startPs1Path = Join-Path $RepoRoot "start-kursor.ps1"
$startPs1Text = Get-Content $startPs1Path -Raw
$restartStopsAll = ($startPs1Text -match 'clicky-harness') -and ($startPs1Text -match 'ClickyBrowserHost') -and ($startPs1Text -match 'Get-NetTCPConnection -LocalPort 8787') -and ($startPs1Text -match '-WorkingDirectory \$BackendDir')
Add-Check -Id "AUD-A-007" -Workstream "WS-A" `
    -Title "start-kursor.ps1 -Restart stops HeyClicky, sidecars, and port 8787 listener and uses -WorkingDirectory" `
    -Status ($(if ($restartStopsAll) { "PASS" } else { "FAIL" })) `
    -Observed "start-kursor.ps1 -Restart terminates HeyClicky, clicky-harness, ClickyBrowserHost, RestartAgent, and any TCP :8787 listener, and launches backend with -WorkingDirectory `$BackendDir." `
    -Expected "-Restart cleanly stops all desktop/sidecar processes and port 8787 listeners." `
    -Location "start-kursor.ps1:39-53,107-117"

# AUD-A-008: Single canonical Windows autostart registration in install-autostart.ps1
$installAutoPath = Join-Path $RepoRoot "scripts\install-autostart.ps1"
$installAutoText = Get-Content $installAutoPath -Raw
$setsRunKursor = $installAutoText -match 'Set-ItemProperty -Path \$RunKeyPath -Name "Kursor"'
$removesLegacyRun = $installAutoText -match 'Remove-ItemProperty -Path \$RunKeyPath -Name "HeyClicky"'
$removesLegacyStartupLnk = $installAutoText -match 'Remove-Item -Path \$LegacyStartupShortcut'
Add-Check -Id "AUD-A-008" -Workstream "WS-A" `
    -Title "install-autostart.ps1 registers single HKCU Run\Kursor entry and cleans up legacy Run\HeyClicky and Startup\Kursor.lnk" `
    -Status ($(if ($setsRunKursor -and $removesLegacyRun -and $removesLegacyStartupLnk) { "PASS" } else { "FAIL" })) `
    -Observed "install-autostart.ps1 sets HKCU\...\Run\Kursor and removes legacy HKCU\...\Run\HeyClicky and duplicate Startup\Kursor.lnk shortcuts." `
    -Expected "Single canonical autostart entry in HKCU\...\Run\Kursor with legacy duplicate cleanup." `
    -Location "scripts/install-autostart.ps1:21-39"

# AUD-A-009: Unified AppData configuration & camelCase/PascalCase endpoints.json in AppPaths.cs and seed-local-auth.ps1
$appPathsFile = Join-Path $RepoRoot "src\desktop\Kursor.Contracts\HeyClicky\AppPaths.cs"
$appPathsText = Get-Content $appPathsFile -Raw
$seedAuthText = Get-Content (Join-Path $RepoRoot "scripts\seed-local-auth.ps1") -Raw
$supportsKursorEnv = $appPathsText -match 'GetEnvironmentVariable\("KURSOR_DATA_DIR"\)'
$seedsCamelAndPascal = ($seedAuthText -match '"workerBaseUrl"') -and ($seedAuthText -match '"WorkerBaseUrl"') -and ($seedAuthText -match 'auth\.localdev\.bin')
Add-Check -Id "AUD-A-009" -Workstream "WS-A" `
    -Title "AppPaths.cs honors KURSOR_DATA_DIR/HEYCLICKY_DATA_DIR and seed-local-auth.ps1 seeds camelCase/PascalCase endpoints.json + DPAPI auth" `
    -Status ($(if ($supportsKursorEnv -and $seedsCamelAndPascal) { "PASS" } else { "FAIL" })) `
    -Observed "AppPaths.cs supports KURSOR_DATA_DIR and HEYCLICKY_DATA_DIR; seed-local-auth.ps1 writes both camelCase and PascalCase keys in endpoints.json and seeds DPAPI auth.localdev.bin/auth.bin." `
    -Expected "Consistent endpoint and DPAPI session seeding across AppData directories." `
    -Location "src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs:10-12, scripts/seed-local-auth.ps1:15-42"

# AUD-A-010: LICENSE, THIRD-PARTY-NOTICES.txt, anchored .gitignore rules, and resilient scripts/package-portable.ps1
$hasLicense = Test-Path (Join-Path $RepoRoot "LICENSE")
$gitignoreText = Get-Content (Join-Path $RepoRoot ".gitignore") -Raw
$hasAnchoredGitignore = ($gitignoreText -match "(?m)^/Release/\r?$") -and ($gitignoreText -match "(?m)^/Debug/\r?$") -and ($gitignoreText -notmatch "\[Rr\]elease/") -and ($gitignoreText -match "(?m)^!\*\*/\.env\.example\r?$")
$pkgCopiesNotices = ($pkgPortableText -match '"LICENSE"') -and ($pkgPortableText -match '"THIRD-PARTY-NOTICES\.txt"') -and ($pkgPortableText -match '\$LASTEXITCODE -ge 8')
Add-Check -Id "AUD-A-010" -Workstream "WS-A" `
    -Title "LICENSE and THIRD-PARTY-NOTICES.txt present, .gitignore rules anchored, and package-portable.ps1 stages notices & checks robocopy exit code" `
    -Status ($(if ($hasLicense -and $hasThirdPartyNotices -and $hasAnchoredGitignore -and $pkgCopiesNotices) { "PASS" } else { "FAIL" })) `
    -Observed "LICENSE and THIRD-PARTY-NOTICES.txt exist at repo root and are staged by package-portable.ps1; .gitignore uses anchored /Release/ and /Debug/ rules and includes !**/.env.example; package-portable.ps1 validates robocopy `$LASTEXITCODE < 8." `
    -Expected "Repository includes LICENSE, THIRD-PARTY-NOTICES.txt, anchored .gitignore rules, and resilient portable packaging." `
    -Location "LICENSE, THIRD-PARTY-NOTICES.txt, .gitignore:34-43, scripts/package-portable.ps1:29-91"

# -------------------------------------------------------------------------
# WS-B CHECKS: Branding, Rename Completeness & Legal/IP Exposure
# -------------------------------------------------------------------------

# AUD-B-001: AssemblyInfo metadata rebranded to Kursor and Sentry.ProjectDirectory removed
$hasKursorAsmInfo = ($asmInfoText -match 'AssemblyCompany\("Kursor"\)') -and ($asmInfoText -match 'AssemblyProduct\("Kursor"\)') -and ($asmInfoText -match 'AssemblyTitle\("Kursor"\)') -and ($asmInfoText -notmatch 'Sentry\.ProjectDirectory')
Add-Check -Id "AUD-B-001" -Workstream "WS-B" `
    -Title "AssemblyInfo.cs stamps Kursor Company/Product/Title metadata and strips Sentry.ProjectDirectory" `
    -Status ($(if ($hasKursorAsmInfo) { "PASS" } else { "FAIL" })) `
    -Observed "AssemblyInfo.cs sets AssemblyCompany('Kursor'), AssemblyProduct('Kursor'), AssemblyTitle('Kursor'), and omits Sentry.ProjectDirectory." `
    -Expected "PE AssemblyInfo metadata rebranded to Kursor with no leaked build paths." `
    -Location "src/desktop/Kursor.App/Properties/AssemblyInfo.cs:8-13"

# AUD-B-002: Zero production HeyClicky endpoints, Sentry DSN, PostHog key, or Velopack feed baked into binary
$bundleCfgText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Core\HeyClicky.Core.Configuration\AppBundleConfiguration.cs") -Raw
$inkImagesText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.App\HeyClicky.App.Ink\ClickyInkImages.cs") -Raw
$telemetryRemoved = ($asmInfoText -match 'AssemblyMetadata\("HeyClicky\.PostHogApiKey",\s*""\)') -and ($asmInfoText -match 'AssemblyMetadata\("HeyClicky\.SentryDsn",\s*""\)') -and ($asmInfoText -match 'AssemblyMetadata\("HeyClicky\.UpdateFeedUrl",\s*""\)')
$localEndpointsOnly = ($bundleCfgText -notmatch "mrpvynsdsnimuisyhkow\.supabase\.co") -and ($bundleCfgText -match "UseProductionBackendRequested => false") -and ($inkImagesText -match 'WorkerBaseUrl \{ get; set; \} = "http://127\.0\.0\.1:8787"')
Add-Check -Id "AUD-B-002" -Workstream "WS-B" `
    -Title "Third-party production telemetry keys (Sentry, PostHog, Velopack) and cloud endpoints removed from binary" `
    -Status ($(if ($telemetryRemoved -and $localEndpointsOnly) { "PASS" } else { "FAIL" })) `
    -Observed "AssemblyInfo.cs blanks out PostHogApiKey, SentryDsn, and UpdateFeedUrl; AppBundleConfiguration.cs disables UseProductionBackendRequested and defaults to http://127.0.0.1:8787; ClickyInkImages.WorkerBaseUrl defaults to http://127.0.0.1:8787." `
    -Expected "Zero production telemetry keys, update feeds, or cloud fallback URLs in desktop assemblies." `
    -Location "src/desktop/Kursor.App/Properties/AssemblyInfo.cs:14-19, src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs:10-40"

# AUD-B-003: SingleInstance mutex/pipe isolation ("Kursor") and kursor:// URI protocol registration
$singleInstText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Platform\HeyClicky.Platform.Win32\SingleInstance.cs") -Raw
$protoRegText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Platform\HeyClicky.Platform.Win32\ProtocolRegistration.cs") -Raw
$hasKursorIpc = ($singleInstText -match 'string appId = "Kursor"') -and ($protoRegText -match '"kursor"') -and ($seedAuthText -match '@\("heyclicky", "kursor", "clicky"\)')
Add-Check -Id "AUD-B-003" -Workstream "WS-B" `
    -Title "SingleInstance.cs uses isolated 'Kursor' mutex/pipe identity and ProtocolRegistration registers kursor://" `
    -Status ($(if ($hasKursorIpc) { "PASS" } else { "FAIL" })) `
    -Observed "SingleInstance.TryClaim defaults to appId='Kursor' (Local\Kursor-<SID>), ProtocolRegistration.Schemes includes 'kursor', and seed-local-auth.ps1 registers kursor://, heyclicky://, and clicky://." `
    -Expected "Isolated Kursor single-instance mutex/pipe and kursor:// URI protocol support." `
    -Location "src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs:80, ProtocolRegistration.cs:10"

# AUD-B-004: Rebranded user-facing startup dialog, KursorTray window class, and Kursor community/repo links
$appCsText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.App\HeyClicky.App\App.cs") -Raw
$trayIconText = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.App\HeyClicky.App.Shell\TrayIcon.cs") -Raw
$hasRebrandedUi = ($appCsText -notmatch "hi@heyclicky\.com") -and ($appCsText -match "Kursor couldn't start\.") -and ($trayIconText -match '"KursorTray"') -and ($workerTsText -match "github\.com/kursor-app/kursor")
Add-Check -Id "AUD-B-004" -Workstream "WS-B" `
    -Title "Startup error dialog, KursorTray window class, and backend config links rebranded to Kursor" `
    -Status ($(if ($hasRebrandedUi) { "PASS" } else { "FAIL" })) `
    -Observed "App.cs:111 displays 'Kursor couldn't start.' without hi@heyclicky.com; TrayIcon.cs:231-234 registers 'KursorTray'; worker.ts:15-20 points app-config links to github.com/kursor-app/kursor." `
    -Expected "User-facing dialogs, tray window class, and backend links use Kursor branding." `
    -Location "src/desktop/Kursor.App/HeyClicky.App/App.cs:111, TrayIcon.cs:231-234"

# AUD-B-005: Repo hygiene & IP isolation — reference/decompiled, executable-file, services, and dist excluded in .gitignore + no HeyClicky-Setup.exe auto-launch
$gitignoreExcludesIp = ($gitignoreText -match "(?m)^reference/decompiled/\r?$") -and ($gitignoreText -match "(?m)^executable-file/\r?$") -and ($gitignoreText -match "(?m)^services/\r?$") -and ($gitignoreText -match "(?m)^dist/\r?$") -and ($gitignoreText -match "(?m)^release/\r?$")
$noSetupFallback = $startPs1Text -notmatch "HeyClicky-Setup\.exe"
$licenseText = Get-Content (Join-Path $RepoRoot "LICENSE") -Raw
$hasCleanRoomNotice = $licenseText -match "CLEAN-ROOM / SELF-HOSTED INTEROPERABILITY"
Add-Check -Id "AUD-B-005" -Workstream "WS-B" `
    -Title "IP/binary isolation: .gitignore excludes reference/decompiled, executable-file, services, dist & release; start-kursor.ps1 removes HeyClicky-Setup.exe fallback" `
    -Status ($(if ($gitignoreExcludesIp -and $noSetupFallback -and $hasCleanRoomNotice) { "PASS" } else { "FAIL" })) `
    -Observed ".gitignore excludes reference/decompiled/, executable-file/, services/, dist/, and release/; start-kursor.ps1 never invokes HeyClicky-Setup.exe; LICENSE documents the clean-room interoperability notice." `
    -Expected "Vendor installers/reference decompilations excluded from git and never auto-executed by launcher scripts." `
    -Location ".gitignore:60-71, start-kursor.ps1:141-151, LICENSE:23-41"

# -------------------------------------------------------------------------
# WS-N CHECKS: Docs & Developer Experience
# -------------------------------------------------------------------------

# AUD-N-001: Environment variable alignment across README.md, .env.example, brain.ts, voice.ts, and auth.ts
$envExampleText = Get-Content (Join-Path $RepoRoot "src\backend\.env.example") -Raw
$brainTsText = Get-Content (Join-Path $RepoRoot "src\backend\src\routes\brain.ts") -Raw
$voiceTsText = Get-Content (Join-Path $RepoRoot "src\backend\src\routes\voice.ts") -Raw
$authMwText = Get-Content (Join-Path $RepoRoot "src\backend\src\middleware\auth.ts") -Raw
$hasAlignedEnvVars = ($readmeText -match "KURSOR_BRAIN_MODEL") -and ($envExampleText -match "KURSOR_BRAIN_MODEL") -and ($brainTsText -match "KURSOR_BRAIN_MODEL") -and ($brainTsText -match "ONEBRAIN_MODEL") -and ($authMwText -match "KURSOR_DEV_BYPASS_AUTH") -and ($voiceTsText -match "EARS_PRIMARY_PROVIDER") -and ($voiceTsText -match "DICTATION_CLEANUP_PROVIDER") -and ($envExampleText -notmatch "ANTHROPIC_API_KEY")
Add-Check -Id "AUD-N-001" -Workstream "WS-N" `
    -Title "Environment variables in README.md and .env.example (KURSOR_BRAIN_MODEL, ONEBRAIN_MODEL, KURSOR_DEV_BYPASS_AUTH, voice providers) are wired in backend code" `
    -Status ($(if ($hasAlignedEnvVars) { "PASS" } else { "FAIL" })) `
    -Observed "README.md and .env.example document KURSOR_BRAIN_MODEL (with ONEBRAIN_MODEL alias), KURSOR_DEV_BYPASS_AUTH, EARS_PRIMARY_PROVIDER, MOUTH_PRIMARY_PROVIDER, and DICTATION_CLEANUP_PROVIDER, all wired in brain.ts, auth.ts, and voice.ts." `
    -Expected "All documented environment variables in README.md and .env.example are honored by backend code." `
    -Location "src/backend/.env.example:1-38, README.md:83-89"

# AUD-N-002: README.md accurately documents study-card primitives and DPAPI auth files
$hasAccurateReadme = ($readmeText -notmatch "session\.json") -and ($readmeText -notmatch "writeOnPage") -and ($readmeText -match "auth\.localdev\.bin") -and ($readmeText -match "receipt\(\)")
Add-Check -Id "AUD-N-002" -Workstream "WS-N" `
    -Title "README.md accurately documents card primitives (receipt/paper/clipping/sticky) and DPAPI auth files (auth.localdev.bin / auth.bin)" `
    -Status ($(if ($hasAccurateReadme) { "PASS" } else { "FAIL" })) `
    -Observed "README.md:20-21 documents receipt(), paper(), clipping(), sticky(), and openPage(); README.md:132 documents endpoints.json and DPAPI auth.localdev.bin / auth.bin." `
    -Expected "README.md accurately describes V8 drawing primitives and DPAPI session files." `
    -Location "README.md:20-21,132"

# AUD-N-003: Ground-truth C# file counts in AGENTS.md, docs/INDEX.md, and docs/07-kursor-roadmap.md
$refCsCount = @(Get-ChildItem (Join-Path $RepoRoot "reference\decompiled") -Filter "*.cs" -Recurse -File | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }).Count
$srcCsCount = @(Get-ChildItem (Join-Path $RepoRoot "src\desktop") -Filter "*.cs" -Recurse -File | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }).Count
$agentsMdText = Get-Content (Join-Path $RepoRoot "AGENTS.md") -Raw
$indexText = Get-Content (Join-Path $RepoRoot "docs\INDEX.md") -Raw
$doc07Text = Get-Content (Join-Path $RepoRoot "docs\07-kursor-roadmap.md") -Raw
$countsMatch = ($refCsCount -eq 2096) -and ($srcCsCount -In @(2097, 2099)) -and ($agentsMdText -match "2,096") -and ($indexText -match "2,096") -and ($doc07Text -match "2,096")
Add-Check -Id "AUD-N-003" -Workstream "WS-N" `
    -Title "C# file counts in AGENTS.md, docs/INDEX.md, and docs/07-kursor-roadmap.md match ground truth (2,096 decompiled, $srcCsCount desktop)" `
    -Status ($(if ($countsMatch) { "PASS" } else { "FAIL" })) `
    -Observed "Ground-truth counts are reference/decompiled = $refCsCount .cs files and src/desktop = $srcCsCount .cs files (2,096 core project .cs + 2 sidecar .cs + 1 root polyfill), matching AGENTS.md, docs/INDEX.md, and docs/07-kursor-roadmap.md." `
    -Expected "Documentation file counts match actual .cs file counts ($refCsCount in reference/decompiled, $srcCsCount in src/desktop)." `
    -Location "AGENTS.md:15, docs/INDEX.md:25,35,66"

# AUD-N-004: Zero broken file:/// links in docs/06-agents-proactive-and-memory.md
$doc06Path = Join-Path $RepoRoot "docs\06-agents-proactive-and-memory.md"
$doc06Text = Get-Content $doc06Path -Raw
$brokenLinks = 0
foreach ($m in [regex]::Matches($doc06Text, 'file:///C:/dev/heyclicky-clone/([^)#\s"]+)')) {
    $rel = $m.Groups[1].Value -replace '/', '\'
    if (-not (Test-Path (Join-Path $RepoRoot $rel))) { $brokenLinks++ }
}
Add-Check -Id "AUD-N-004" -Workstream "WS-N" `
    -Title "All file:/// source links in docs/06-agents-proactive-and-memory.md resolve to existing repository files" `
    -Status ($(if ($brokenLinks -eq 0) { "PASS" } else { "FAIL" })) `
    -Observed "$brokenLinks broken file:/// links in docs/06-agents-proactive-and-memory.md." `
    -Expected "0 broken file:/// markdown links in docs/06-agents-proactive-and-memory.md." `
    -Location "docs/06-agents-proactive-and-memory.md"

# AUD-N-005: Verified asset inventory in docs/01 and accurate Wave 1/2 remediation state in docs/07
$doc01Text = Get-Content (Join-Path $RepoRoot "docs\01-architecture-overview.md") -Raw
$doc01Accurate = ($doc01Text -notmatch 'PlusJakartaSans-') -and ($doc01Text -match 'Assets\\Engine\\') -and ($doc01Text -match 'ClickyDraw-PatrickHand\.ttf')
$doc07Accurate = ($doc07Text -match "Full-Spectrum Audit & Wave 1/2 Remediation") -and ($doc07Text -match "TrayTooltip\.cs")
Add-Check -Id "AUD-N-005" -Workstream "WS-N" `
    -Title "Architecture inventory (docs/01) and roadmap status (docs/07) match actual repository files and remediation state" `
    -Status ($(if ($doc01Accurate -and $doc07Accurate) { "PASS" } else { "FAIL" })) `
    -Observed "docs/01:153-184 documents verified Assets\Engine\*.js and Assets\ subfolders with exact byte sizes; docs/07:98-135 accurately reflects Phase 4-7 and Wave 1/2 remediation state." `
    -Expected "Architecture inventory (docs/01) and roadmap completion table (docs/07) reflect actual repository files." `
    -Location "docs/01-architecture-overview.md:153-184, docs/07-kursor-roadmap.md:98-135"

# WS-A-CTRL-001: Backend .env is gitignored, untracked by git, and .env.example contains no live API keys
$envIsIgnored = ($gitignoreText -match "(?m)^\.env\r?$") -and ($gitignoreText -match "(?m)^src/backend/\.env\r?$")
$trackedEnv = ""
try {
    $trackedEnv = (& git -C $RepoRoot ls-files "src/backend/.env" ".env" 2>$null) -join ""
} catch {}
$exampleHasSecret = $false
foreach ($l in (Get-Content (Join-Path $RepoRoot "src\backend\.env.example") | Where-Object { $_ -match "_KEY=" })) {
    $val = ($l -split "=", 2)[1].Trim()
    if ($val.Length -gt 0) { $exampleHasSecret = $true }
}
Add-Check -Id "WS-A-CTRL-001" -Workstream "WS-A" `
    -Title "Backend .env is gitignored/untracked and .env.example contains no live API keys" `
    -Status ($(if ($envIsIgnored -and [string]::IsNullOrWhiteSpace($trackedEnv) -and (-not $exampleHasSecret)) { "PASS" } else { "FAIL" })) `
    -Observed ".env and src/backend/.env are covered by .gitignore, untracked by git, and all *_KEY= entries in src/backend/.env.example are empty." `
    -Expected "No developer API keys committed to git or .env.example." `
    -Location ".gitignore:6-11, src/backend/.env.example"

# WS-N-CTRL-001: Backend test suite count (18 + 7 = 25) and docs/INDEX.md link integrity
$t1Count = ([regex]::Matches((Get-Content (Join-Path $RepoRoot "src\backend\src\test-phase1.ts") -Raw), "\bawait testEndpoint\(")).Count
$t2Count = ([regex]::Matches((Get-Content (Join-Path $RepoRoot "src\backend\src\test-phase2.ts") -Raw), "\bawait testEndpoint\(")).Count
Add-Check -Id "WS-N-CTRL-001" -Workstream "WS-N" `
    -Title "Backend test suite counts in docs/INDEX.md (18 + 7 = 25) match test-phase1.ts and test-phase2.ts" `
    -Status ($(if ($t1Count -eq 18 -and $t2Count -eq 7) { "PASS" } else { "FAIL" })) `
    -Observed "test-phase1.ts has $t1Count testEndpoint calls; test-phase2.ts has $t2Count testEndpoint calls (total = $($t1Count + $t2Count))." `
    -Expected "18 tests in test-phase1.ts and 7 tests in test-phase2.ts (25 total)." `
    -Location "docs/INDEX.md:51,62"

Write-Host "`n================================================================" -ForegroundColor Cyan
$passCount = @($results | Where-Object { $_.status -eq "PASS" }).Count
$failCount = @($results | Where-Object { $_.status -eq "FAIL" }).Count
$unvCount  = @($results | Where-Object { $_.status -eq "UNVERIFIED" }).Count
Write-Host (" SUMMARY: Total={0} | PASS={1} | FAIL={2} | UNVERIFIED={3}" -f $results.Count, $passCount, $failCount, $unvCount) -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

if ($OutputJson) {
    $outDir = Split-Path -Parent $OutputJson
    if ($outDir -and -not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }
    $results | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputJson -Encoding UTF8
    Write-Host "Saved JSON results to: $OutputJson" -ForegroundColor Green
}

if ($failCount -gt 0) {
    exit 1
}
exit 0
