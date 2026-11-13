# Pass 2 Independent Cross-Audit — WS-A (Build/Packaging/Lifecycle), WS-B (Branding/Telemetry/Legal), & WS-N (Docs/DX)

**Cross-Auditor:** `ws-e-voice-dictation`  
**Pass 1 Author:** `ws-ab-build-brand`  
**Target Deliverables Reviewed:**
- [`docs/audit/ws-ab/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/REPORT.md)
- [`docs/audit/ws-ab/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/findings.json)
- [`docs/audit/ws-ab/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/coverage.json)
- [`tests/audit/ws-ab/test-ws-ab.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-ab/test-ws-ab.ps1)
- Evidence files under [`docs/audit/evidence/ws-ab/`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/)

---

## 1. Executive Summary

| Metric | Count |
|---|---:|
| **Pass 1 Findings Reviewed** | 20 (`AUD-A-001`..`010`, `AUD-B-001`..`005`, `AUD-N-001`..`005`) |
| **Confirmed (`CONFIRMED`)** | 17 |
| **Confirmed with Technical Refinement (`CONFIRMED_WITH_REFINEMENT`)** | 3 (`AUD-A-010`, `AUD-B-002`, `AUD-N-001`) |
| **Disputed (`DISPUTED`)** | 0 |
| **New Findings Uncovered in Pass 2** | **4** (`AUD-A-011` HIGH, `AUD-A-012` MEDIUM, `AUD-B-006` HIGH, `AUD-N-006` MEDIUM) |

---

## 2. Independent Verification of Pass 1 Findings

| Finding ID | Pass 1 Severity | Pass 2 Verdict | Independent Verification Evidence & Notes |
|---|---|---|---|
| **`AUD-A-001`** | `HIGH` | **`CONFIRMED`** | Verified in [`src/desktop/Directory.Build.props#L3`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L3) (9 suppressed warning codes), all 7 `.csproj` files (`<Nullable>annotations</Nullable>`, `<AllowUnsafeBlocks>True</AllowUnsafeBlocks>`), and [`dotnet-build-debug.txt#L356-L700`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-debug.txt#L356-L700) (`343 Warning(s), 0 Error(s)` including `CA2014` stackalloc-in-loop at `PrecisionTouchpad.cs:221`, 6 `SYSLIB1032` non-partial `JsonSerializerContext` classes in `Kursor.Core`, `CS0162`/`CS0164` in `PhotoLoader.cs:524`, and `CS4014` unawaited tasks). |
| **`AUD-A-002`** | `HIGH` | **`CONFIRMED`** | Verified in [`Directory.Build.props#L4-L6`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L4-L6) and all 7 `.csproj` files: zero `<PackageReference>` items exist across the solution; all 32 third-party/framework DLLs are bound via `<HintPath>$(HeyClickyRuntimeDir)\*.dll</HintPath>`. |
| **`AUD-A-003`** | `HIGH` | **`CONFIRMED`** | Verified in [`scripts/build-dist.ps1#L34`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L34), [`build-dist.ps1#L72-L87`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L72-L87), [`scripts/overlay-and-launch.ps1#L14-L29`](file:///C:/dev/heyclicky-clone/scripts/overlay-and-launch.ps1#L14-L29), and [`artifact-hash-comparison-prebuild.json#L157-L166`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json#L157-L166): `build-dist.ps1` hardcodes `-c Debug`, copies `.pdb` files into `dist/kursor-desktop/`, and omits `HeyClicky.exe` from `$Assemblies` (leaving `dist/kursor-desktop/HeyClicky.exe` at `26AB7792...` while `bin/Debug/net9.0/HeyClicky.exe` is `32C071E8...`). |
| **`AUD-A-004`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`scripts/build-dist.ps1#L105`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L105) and `Kursor.Drawing3D.csproj:34-37`: `src/desktop/Kursor.Drawing3D/Assets/Engine` does not exist, and `clicky-draw-pieces.js` in `%LOCALAPPDATA%\HeyClickyApp\current` (`66,086 B`) differs from `dist/kursor-desktop/` (`66,985 B`). |
| **`AUD-A-005`** | `MEDIUM` | **`CONFIRMED`** | Verified in `src/backend/package.json:14,25`, [`setup-kursor.ps1#L27-L32`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L27-L32), and [`npm-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/npm-audit.json): `@hono/node-server@1.19.19` has `GHSA-rmxm-3fg6-px4f` (`<2.1.3`), and `setup-kursor.ps1` does not check Node's major version against `engines.node` (`>=24.0.0`). |
| **`AUD-A-006`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`src/backend/src/routes/worker.ts#L7-L8`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L7-L8) (`C:\Users\ubaid\AppData\Local\HeyClicky\app-config.json`), [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L8`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L8) (`C:\Users\niles\code\clicky-closed\windows\src\HeyClicky.App\`), and `AgentHarnessRunnerProcess.cs:75`. *(Minor line refinement: in `AssemblyInfo.cs`, `Sentry.ProjectDirectory` is on line 8, while line 14 is `AssemblyTitle("HeyClicky")`.)* |
| **`AUD-A-007`** | `HIGH` | **`CONFIRMED`** | Verified in [`start-kursor.ps1#L74-L120`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L74-L120): `-Restart` (`lines 115-120`) only stops `HeyClicky.exe` and leaves the backend `node.exe` on port `8787` and sidecars running; lines 84–108 sleep 3 seconds without re-verifying `/health`; line 105 interpolates `$BackendDir` inside single quotes. |
| **`AUD-A-008`** | `HIGH` | **`CONFIRMED`** | Verified in [`scripts/install-autostart.ps1#L21-L38`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L21-L38), [`scripts/uninstall-autostart.ps1#L7-L22`](file:///C:/dev/heyclicky-clone/scripts/uninstall-autostart.ps1#L7-L22), [`LaunchAtLogin.cs#L12-L33`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs#L12-L33), and [`system-state-check.json#L2-L8`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json#L2-L8): `install-autostart.ps1` registers **both** `HKCU\...\Run\Kursor` and `Startup\Kursor.lnk` (double launch at login), while `LaunchAtLogin.cs` writes `HKCU\...\Run\HeyClicky`, and all three are currently active on the host. |
| **`AUD-A-009`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`setup-kursor.ps1#L56-L61`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L56-L61), [`scripts/seed-local-auth.ps1#L25-L30`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L25-L30), and `AppPaths.cs:12`: setup scripts seed both `%LOCALAPPDATA%\HeyClicky` and `%LOCALAPPDATA%\Kursor`, while `AppPaths.Root` uses `%LOCALAPPDATA%\HeyClicky`. |
| **`AUD-A-010`** | `MEDIUM` | **`CONFIRMED_WITH_REFINEMENT`** | Confirmed missing `LICENSE` / `THIRD_PARTY_NOTICES`, unanchored `[Dd]ebug/` and `[Rr]elease/` in `.gitignore:38-39`, and lack of running-process lock checks in [`scripts/package-portable.ps1#L23-L74`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1#L23-L74). **Refinement:** `package-portable.ps1:17-20` *does* check `if (-not (Test-Path (Join-Path $DistDir "HeyClicky.exe")))` and invokes `build-dist.ps1`, so that pre-flight check exists, but `robocopy` at line 73 leaves a non-zero `$LASTEXITCODE` (see new finding `AUD-A-012`). |
| **`AUD-B-001`** | `HIGH` | **`CONFIRMED`** | Verified in all 7 `.csproj` files and [`AssemblyInfo.cs#L8-L17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L8-L17): all projects compile `HeyClicky*.dll` / `HeyClicky.exe` and stamp `AssemblyCompany`/`Product`/`Title("HeyClicky")`. |
| **`AUD-B-002`** | `CRITICAL` | **`CONFIRMED_WITH_REFINEMENT`** | Verified in [`AssemblyInfo.cs#L15-L17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L15-L17), [`AppBundleConfiguration.cs#L36-L55`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L36-L55), [`CrashReporting.cs#L210-L265`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs#L210-L265) (`SendDefaultPii = true` at line 242), [`ClickyAnalytics.cs#L290-L325`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/ClickyAnalytics.cs#L290-L325), and [`ClickyInkImages.cs#L60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkImages.cs#L60) (`WorkerBaseUrl = "https://api.heyclicky.com"`). **Additional Critical Mechanism Uncovered in Pass 2:** Because `build-dist.ps1` omits `HeyClicky.exe`, `dist/kursor-desktop/HeyClicky.exe` is the original Humansongs, Inc entry assembly (`SHA256 26AB7792...`). In `AppBundleConfiguration.WithBuildMetadata` (`lines 109-145`), `Assembly.GetEntryAssembly()` reads `HeyClicky.exe`'s baked-in `AssemblyMetadata` (`HeyClicky.WorkerBaseUrl`, `HeyClicky.SupabaseUrl`, `HeyClicky.SupabaseAnonKey`)! Combined with a PascalCase-only `endpoints.json` on disk (see new finding `AUD-A-011`), `SupabaseUrl` can silently fall back to `HeyClicky.exe`'s entry-assembly metadata! |
| **`AUD-B-003`** | `HIGH` | **`CONFIRMED`** | Verified in `SingleInstance.cs:80,312-341`, `ProtocolRegistration.cs:10`, `FileOpenRegistration.cs:13-47`, and [`scripts/seed-local-auth.ps1#L94-L102`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L94-L102). |
| **`AUD-B-004`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`src/backend/src/routes/worker.ts#L9-L16`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L9-L16), `App.cs:111`, `TrayIcon.cs:232`, `TrayTooltip.cs:25`, and `CodexHomeManager.cs:74-91`. |
| **`AUD-B-005`** | `CRITICAL` | **`CONFIRMED`** | Verified in [`legal-ip-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/legal-ip-inventory.json) and `scripts/install-runtime.ps1:11`: 2,096 ILSpy-decompiled C# files in `reference/decompiled/`, 5 executables digitally signed by `CN="Humansongs, Inc"`, 171 proprietary media/font/JS assets (89.4 MB), and automated `.nupkg` download from HeyClicky's GitHub releases. |
| **`AUD-N-001`** | `HIGH` | **`CONFIRMED_WITH_REFINEMENT`** | Verified in [`README.md#L84`](file:///C:/dev/heyclicky-clone/README.md#L84) (`ONEBRAIN_MODEL` instead of `KURSOR_BRAIN_MODEL`) and [`src/backend/.env.example`](file:///C:/dev/heyclicky-clone/src/backend/.env.example). **Refinements:** (1) In the 40-line `src/backend/.env.example`, the 6 unread variables are at lines `9, 20, 34, 38-40` (Pass 1 cited lines `7, 23, 55, 61-63` from the 63-line `.env` file). (2) Two additional variables in `.env.example` (`EARS_PRIMARY_PROVIDER` at line 28 and `DICTATION_CLEANUP_PROVIDER` at line 32) are only echoed as strings in `worker.ts` and are completely ignored by `voice.ts` (see `AUD-N-006`). |
| **`AUD-N-002`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`README.md#L20-L21`](file:///C:/dev/heyclicky-clone/README.md#L20-L21), [`README.md#L133`](file:///C:/dev/heyclicky-clone/README.md#L133), and [`scripts/seed-local-auth.ps1#L81-L90`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L81-L90): `seed-local-auth.ps1` writes DPAPI `auth.localdev.bin` and `auth.bin` (never `session.json`), and `writeOnPage` does not exist anywhere in the codebase. |
| **`AUD-N-003`** | `LOW` | **`CONFIRMED`** | Verified in [`docs-audit.json#L155-L212`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json#L155-L212): actual `.cs` counts are `2,096` (`reference/decompiled/`) and `2,097` (`src/desktop/`), vs `2,098` and `2,102` in docs. |
| **`AUD-N-004`** | `MEDIUM` | **`CONFIRMED`** | Verified in [`docs-audit.json#L2-L107`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json#L2-L107): all 17 broken `file:///` links in `docs/06-agents-proactive-and-memory.md` and the misplaced `TrayTooltip.cs` project in `docs/07-kursor-roadmap.md:119` are confirmed. |
| **`AUD-N-005`** | `MEDIUM` | **`CONFIRMED`** | Verified in `docs/01-architecture-overview.md:155-177` and `docs/07-kursor-roadmap.md:31-32,98-132`. |

---

## 3. Newly Discovered Findings in Pass 2 (`AUD-A-011`, `AUD-A-012`, `AUD-B-006`, `AUD-N-006`)

### `ID: AUD-A-011`
- **Severity:** `HIGH`
- **Category:** Configuration & Endpoint Override Fidelity
- **Title:** `%LOCALAPPDATA%\HeyClicky\endpoints.json` on Disk Contains Only PascalCase Keys (Ignored by Case-Sensitive `AppBundleConfiguration.LoadConfigured`), Risking Entry-Assembly Metadata Fallback to Production Supabase
- **Location:** [`src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L57-L159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L57-L159), [`docs/audit/evidence/ws-ab/system-state-check.json#L87-L88`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json#L87-L88)
- **Description:** Inspection of `LocalAppData_HeyClicky_EndpointsJson` in `docs/audit/evidence/ws-ab/system-state-check.json:87-88` shows that the live `C:\Users\ubaid\AppData\Local\HeyClicky\endpoints.json` file on disk contains **only PascalCase keys**:
  ```json
  {
    "WorkerBaseUrl": "http://127.0.0.1:8787",
    "SupabaseUrl": "http://127.0.0.1:8787",
    "SupabaseAnonKey": "kursor-local-anon-key"
  }
  ```
  However, `AppBundleConfiguration.LoadConfigured` (`AppBundleConfiguration.cs:78-80`) calls `ReadConfigured(rootElement, "workerBaseUrl")`, `ReadConfigured(rootElement, "supabaseUrl")`, and `ReadConfigured(rootElement, "supabaseAnonKey")`, which use **case-sensitive** `root.TryGetProperty(name, out var value)` (`line 149`). When `endpoints.json` has only PascalCase keys, all three return `null`!
  Furthermore, `AppBundleConfiguration.LoadConfigured` starts from `WithBuildMetadata(Defaults)` (`line 59`), which reads `Assembly.GetEntryAssembly()` (`HeyClicky.exe` — the unmodified Humansongs, Inc binary in `dist/kursor-desktop/HeyClicky.exe`!). And when `start-kursor.ps1` sets `HEYCLICKY_WORKER_BASE_URL=http://127.0.0.1:8787`, `ApplyWorkerOverride` (`lines 91-107`) **only** overrides `WorkerBaseUrl` — leaving `SupabaseUrl` and `SupabaseAnonKey` un-overridden if `endpoints.json` only had PascalCase keys!
- **Remediation:** Make `AppBundleConfiguration.ReadConfigured` (`lines 147-159`) match property names case-insensitively (`StringComparison.OrdinalIgnoreCase`), and extend `ApplyWorkerOverride` to also read `HEYCLICKY_SUPABASE_URL` and `HEYCLICKY_SUPABASE_ANON_KEY`.

---

### `ID: AUD-A-012`
- **Severity:** `MEDIUM`
- **Category:** Packaging Script Exit Code & Portable Archive Hygiene
- **Title:** `scripts/package-portable.ps1` and `scripts/build-dist.ps1` Leave Non-Zero `robocopy` `$LASTEXITCODE` (`1`–`7`), and `package-portable.ps1` Bundles Unusable `build-dist.ps1` Without `src/desktop` Plus Internal `docs/audit/`
- **Location:** [`scripts/package-portable.ps1#L45-L84`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1#L45-L84), [`scripts/build-dist.ps1#L46`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L46)
- **Description:**
  1. In `scripts/package-portable.ps1:73` (`$null = robocopy $DistDir $DistStage /E /NFL /NDL /NJH /NJS /NC /NS`) and `scripts/build-dist.ps1:46`, `robocopy` sets `$LASTEXITCODE` to `1`–`3` on successful file copies. Because neither script checks `if ($LASTEXITCODE -ge 8) { throw ... }` or resets `$global:LASTEXITCODE = 0`, `package-portable.ps1` finishes with a non-zero `$LASTEXITCODE` (`1` or `3`), causing CI or parent scripts checking `$LASTEXITCODE` to fail, while actual `robocopy` errors (`>= 8`) are silently ignored.
  2. `scripts/package-portable.ps1:46-51` copies all of `scripts/` (including `build-dist.ps1` and `overlay-and-launch.ps1`, which fail inside the portable zip because `src/desktop/` is not included) and all of `docs/` (including `docs/audit/` and reverse-engineering specs) into the end-user portable zip.
- **Remediation:** Check `if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" } else { $global:LASTEXITCODE = 0 }` after every `robocopy` invocation, and copy only runtime launcher scripts into `$StageDir`.

---

### `ID: AUD-B-006`
- **Severity:** `HIGH`
- **Category:** Production Installer Fallback / Upstream Binary Execution
- **Title:** `start-kursor.ps1` Automatically Executes Proprietary `executable-file\HeyClicky-Setup.exe` (Humansongs, Inc Velopack Installer) When `dist\kursor-desktop\HeyClicky.exe` Is Missing
- **Location:** [`start-kursor.ps1#L49-L56`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L49-L56), [`start-kursor.ps1#L126-L135`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L126-L135)
- **Description:** In `start-kursor.ps1:49-56`, if `dist\kursor-desktop\HeyClicky.exe` is missing and `scripts\build-dist.ps1` fails, the `catch` block swallows the build failure (`Write-Warning "Desktop build step skipped: $_"`). Then at `lines 126-135`, `start-kursor.ps1` falls back to:
  ```powershell
  elseif (Test-Path $InstallerExe) {
      Write-Host "[*] Launching HeyClicky Desktop Setup ($InstallerExe)..." -ForegroundColor Cyan
      Start-Process -FilePath $InstallerExe
  }
  ```
  where `$InstallerExe` is `executable-file\HeyClicky-Setup.exe` — the official, Authenticode-signed Humansongs, Inc Velopack installer! Running it installs the unpatched upstream HeyClicky v1.1.5 client into `%LOCALAPPDATA%\HeyClickyApp\current` and launches it directly against HeyClicky's production cloud.
- **Remediation:** Remove the `executable-file\HeyClicky-Setup.exe` fallback from `start-kursor.ps1:126,132-135` and fail fast with an actionable error if `dist\kursor-desktop\HeyClicky.exe` cannot be found or built.

---

### `ID: AUD-N-006`
- **Severity:** `MEDIUM`
- **Category:** Configuration & Environment Variable Wiring Drift
- **Title:** `EARS_PRIMARY_PROVIDER` and `DICTATION_CLEANUP_PROVIDER` in `.env.example` Are Only Echoed in `worker.ts` Metadata and Completely Ignored by `voice.ts`, While `start-kursor.ps1` Sets Unread `HEYCLICKY_SUPABASE_URL` & `HEYCLICKY_SUPABASE_ANON_KEY`
- **Location:** [`src/backend/.env.example#L28-L32`](file:///C:/dev/heyclicky-clone/src/backend/.env.example#L28-L32), [`src/backend/src/routes/voice.ts#L18-L84`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L18-L84), [`start-kursor.ps1#L122-L124`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L122-L124)
- **Description:** Pass 1's `docs-audit.json` counted `EARS_PRIMARY_PROVIDER` and `DICTATION_CLEANUP_PROVIDER` as used in backend code because `src/backend/src/routes/worker.ts:201-208` includes them in the `/config/runtime-models` JSON payload. However, `src/backend/src/routes/voice.ts` — which actually executes STT ("Ears") and `/v2/dictation/cleanup` — never reads `process.env.EARS_PRIMARY_PROVIDER` or `process.env.DICTATION_CLEANUP_PROVIDER`! Likewise, `start-kursor.ps1:123-124` sets `$env:HEYCLICKY_SUPABASE_URL` and `$env:HEYCLICKY_SUPABASE_ANON_KEY`, neither of which is read anywhere in the C# desktop codebase.
- **Remediation:** Honor `EARS_PRIMARY_PROVIDER` and `DICTATION_CLEANUP_PROVIDER` in `src/backend/src/routes/voice.ts`, and honor `HEYCLICKY_SUPABASE_URL` and `HEYCLICKY_SUPABASE_ANON_KEY` in `AppBundleConfiguration.cs`.
