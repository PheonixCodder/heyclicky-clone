# Kursor Full-Spectrum Audit Report — WS-A, WS-B, WS-N

- **Audit Owner**: `ws-ab-build-brand`
- **Repository Root**: [`C:/dev/heyclicky-clone`](file:///C:/dev/heyclicky-clone)
- **Head Commit Audited**: `80c92f53a461489e04bf6e91b14bf98c4fb045dc` (`chore: add portable release packager and Windows autostart integration`)
- **Workstreams Covered**:
  - **WS-A**: Repo, Build, Packaging, Install Lifecycle & Repo Hygiene (`AUD-A-001` – `AUD-A-010`)
  - **WS-B**: Branding, Rename Completeness & Legal/IP Exposure (`AUD-B-001` – `AUD-B-005`)
  - **WS-N**: Documentation & Developer Experience (`AUD-N-001` – `AUD-N-005`)
- **Machine-Readable Deliverables**:
  - Findings JSON: [`docs/audit/ws-ab/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/findings.json)
  - Coverage Matrix JSON: [`docs/audit/ws-ab/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/coverage.json)
  - Automated Verification Script: [`tests/audit/ws-ab/test-ws-ab.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-ab/test-ws-ab.ps1)
  - Evidence Artifacts Directory: [`docs/audit/evidence/ws-ab/`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab)

---

## 1. Executive Summary

| Workstream | CRITICAL | HIGH | MEDIUM | LOW | Total Findings |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **WS-A (Build, Packaging, Install Lifecycle & Repo Hygiene)** | 0 | 5 | 5 | 0 | **10** |
| **WS-B (Branding, Rename Completeness & Legal/IP Exposure)** | 2 | 2 | 1 | 0 | **5** |
| **WS-N (Documentation & Developer Experience)** | 0 | 1 | 3 | 1 | **5** |
| **Total** | **2** | **8** | **9** | **1** | **20** |

### Key Takeaways
1. **Desktop Build & Dependency Reality (`AUD-A-001`, `AUD-A-002`)**: Contrary to [`README.md`](file:///C:/dev/heyclicky-clone/README.md#L53) and [`docs/INDEX.md`](file:///C:/dev/heyclicky-clone/docs/INDEX.md#L53) (`"0 Warnings, 0 Errors"`), a clean non-incremental build (`dotnet build src/desktop/Kursor.sln -c Debug --no-incremental` and `-c Release --no-incremental`) emits **343 warnings** (`472` warning lines when `<NoWarn>` in [`Directory.Build.props`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L3) is unmasked), including `CA2014` (`stackalloc` inside a loop at [`PrecisionTouchpad.cs:221`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/PrecisionTouchpad.cs#L221)) and 6 `SYSLIB1032` warnings where `JsonSerializerContext` classes lack `partial` so `System.Text.Json` source generation is completely inactive. Furthermore, all 7 `.csproj` files have **zero** NuGet `<PackageReference>` items and instead bind 32 third-party/framework DLLs via local `<HintPath>$(HeyClickyRuntimeDir)\*.dll</HintPath>`, blinding `dotnet list package --vulnerable`.
2. **Stale & Out-of-Sync Packaged Artifacts (`AUD-A-003`, `AUD-A-004`)**: [`scripts/build-dist.ps1`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L34) compiles `-c Debug` (never `Release`), copies `.pdb` debug symbols into `dist/kursor-desktop/`, and omits `HeyClicky.exe` from the staged artifact list — leaving `dist/kursor-desktop/HeyClicky.exe` (`SHA256: 26AB7792...`, signed by `Humansongs, Inc`) out of sync with `src/desktop/Kursor.App/bin/Debug/net9.0/HeyClicky.exe` (`SHA256: 32C071E8...`). In addition, `src/desktop/Kursor.Drawing3D/Assets/Engine/` does not exist despite being referenced by [`scripts/build-dist.ps1:105`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L105) and [`Kursor.Drawing3D.csproj:34`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj#L34).
3. **Triple Windows Autostart & AppData Split (`AUD-A-007`, `AUD-A-008`, `AUD-A-009`)**: [`scripts/install-autostart.ps1`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L21-L38) registers Kursor in **both** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Kursor` **and** `%APPDATA%\...\Startup\Kursor.lnk` (causing Windows to launch `start-kursor-hidden.vbs` twice concurrently at login), while the in-app "Launch at login" setting ([`LaunchAtLogin.cs:12`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs#L12)) writes `HKCU\...\Run\HeyClicky` (which launches `HeyClicky.exe` without starting the local Node.js backend). All three autostart entries are active simultaneously on the audited machine. Meanwhile, [`setup-kursor.ps1`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L56-L61) seeds `%LOCALAPPDATA%\Kursor`, which is completely ignored by [`AppPaths.cs:12`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs#L12) (`%LOCALAPPDATA%\HeyClicky`).
4. **Live HeyClicky Cloud Fallback & Telemetry Exfiltration Risk (`AUD-B-002`)**: [`AssemblyInfo.cs:15-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L15-L17) bakes HeyClicky's live PostHog API key (`phc_xcQPygmh...`), Sentry DSN (`https://9e98b32e...@o4511254886154240.ingest.us.sentry.io/...` with `SendDefaultPii = true`), and GitHub Velopack feed URL into `HeyClicky.dll`. If `%LOCALAPPDATA%\HeyClicky\use-production-backend.flag` exists (toggled via Developer Settings) or a user configures a non-loopback LAN/Tailscale backend URL in `endpoints.json`, `IsLocalDevelopmentConfiguration` returns `false` and the app transmits crash reports (with PII) and analytics directly to HeyClicky's production Sentry and PostHog servers.
5. **Direct Legal/IP Exposure (`AUD-B-005`)**: The repository commits `2,096` ILSpy-decompiled C# files in `reference/decompiled/` and `2,097` derived files in `src/desktop/`, redistributes 5 executables bearing valid Authenticode signatures from `CN="Humansongs, Inc"` (`HeyClicky.exe`, `HeyClicky-Update.exe`, `clicky-harness.exe`, `ClickyBrowserHost.exe`, `HeyClicky-Setup.exe`) plus `89.4 MB` of proprietary media/prompt assets, and [`scripts/install-runtime.ps1:11`](file:///C:/dev/heyclicky-clone/scripts/install-runtime.ps1#L11) downloads `HeyClickyApp-1.1.5-win-x64-full.nupkg` directly from `github.com/heyclicky/clicky-releases-windows`.
6. **Documentation & Configuration Drift (`AUD-N-001` – `AUD-N-005`)**: [`README.md:84`](file:///C:/dev/heyclicky-clone/README.md#L84) documents `ONEBRAIN_MODEL` (never read by backend code, which reads `KURSOR_BRAIN_MODEL || OPENROUTER_DEFAULT_MODEL` in [`brain.ts:598`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L598)), [`src/backend/.env.example`](file:///C:/dev/heyclicky-clone/src/backend/.env.example) contains 6 dead environment variables never read in code (including `KURSOR_DEV_BYPASS_AUTH`), [`docs/06-agents-proactive-and-memory.md`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md#L27-L35) contains 17 broken `file:///` links, and [`docs/01`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L155-L177) / [`docs/07`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L31-L32) contain inaccurate asset inventories and mark unimplemented rebranding phases as `✅ COMPLETED`.

---

## 2. Verification Tables & Evidence Summary

### 2.1 Desktop Build & Compiler Warning Matrix (`WS-A`)
Evidence: [`dotnet-build-debug.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-debug.txt), [`dotnet-build-release.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-release.txt), [`dotnet-build-unmasked-warnings.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-unmasked-warnings.txt)

| Build Command | Exit Code | Error Count | Warning Count | Elapsed | Notes |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `dotnet build Kursor.sln -c Debug --no-incremental` | `0` | `0` | **343** | `00:06:21.13` | Even with 9 warning codes suppressed in `Directory.Build.props` |
| `dotnet build Kursor.sln -c Release --no-incremental` | `0` | `0` | **343** | `00:03:02.52` | Release configuration never used by `scripts/build-dist.ps1` |
| `dotnet build Kursor.sln -c Debug --no-incremental "/p:NoWarn="` | `0` | `0` | **472** | `00:03:18.40` | Unmasks 78 `CS0649` unassigned fields, 4 `CS0169`, etc. |

**Breakdown of Warning Codes (Unmasked Clean Build):**

| Warning Code | Count | Severity / Risk | Representative Location |
| :--- | :---: | :--- | :--- |
| `CS0618` | 280 | Obsolete `__KnownINotifyPropertyChangingArgs` / `ChangedArgs` decompiler artifacts | [`PreSignInIntroViewModel.cs:101`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Onboarding/PreSignInIntroViewModel.cs#L101) |
| `CS0649` | 78 | Field is never assigned to, and will always have its default value (suppressed by `Directory.Build.props`) | Across decompiled DTO/Interop structs |
| `CS4014` | 68 | Unawaited async call (fire-and-forget `Task` where exceptions may go unobserved) | [`SingleInstance.cs:178`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L178), [`AppHost.cs:6669`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/AppHost.cs#L6669) |
| `SYSLIB1032` | 12 (6×2) | Derived `JsonSerializerContext` is not marked `partial`; source generator skipped | [`CoreJsonContext.cs:51`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core/CoreJsonContext.cs#L51), [`InkJsonContext.cs:15`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/InkJsonContext.cs#L15) |
| `CS8123` | 12 (6×2) | Tuple element name ignored due to target tuple signature mismatch | [`DrawingCaption.cs:138`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/DrawingCaption.cs#L138), [`AppHost.cs:6769`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/AppHost.cs#L6769) |
| `CS0169` | 4 | Private field is never used (suppressed by `Directory.Build.props`) | Decompiled state classes |
| `CS9193` | 4 | Argument should be a variable because it is passed to a `ref readonly` parameter | [`ClickyDriveLines.cs:70`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyDriveLines.cs#L70), [`AppHost.cs:5952`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/AppHost.cs#L5952) |
| `CA2014` | 2 (1×2) | **Potential stack overflow: `stackalloc` inside a loop** | [`PrecisionTouchpad.cs:221`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/PrecisionTouchpad.cs#L221) |
| `CS0162` / `CS0164` | 4 | Unreachable code detected & unreferenced label from decompiled control flow | [`PhotoLoader.cs:524`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L524) |
| `CA2255` | 2 (1×2) | `[ModuleInitializer]` used in class library | [`GlobalVtableLookup.cs:14`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/WinRT.HeyClicky_DrawingGenericHelpers/GlobalVtableLookup.cs#L14) |

### 2.2 SHA-256 Artifact Comparison: `bin/Debug/net9.0` vs `dist/kursor-desktop/` vs `%LOCALAPPDATA%\HeyClickyApp\current`
Evidence: [`artifact-hash-comparison-prebuild.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json), [`fresh-debug-build-hashes.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/fresh-debug-build-hashes.json), [`js-assets-comparison.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/js-assets-comparison.json)

| Artifact | Pre-Audit `bin/Debug/net9.0` SHA-256 | `dist/kursor-desktop/` SHA-256 | Fresh Clean Debug Build SHA-256 | Status |
| :--- | :--- | :--- | :--- | :---: |
| `HeyClicky.exe` | `32C071E81A067719...` (`420,352 B`) | `26AB779203881266...` (`429,920 B`) | `32C071E81A067719...` (`420,352 B`) | **FAIL (Stale/Omitted)** |
| `HeyClicky.dll` | `73197D9033A5A6BC...` (`4,762,112 B`) | `73197D9033A5A6BC...` (`4,762,112 B`) | `73F2DFDFDEE243B0...` (`4,762,112 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Core.dll` | `E942899AF6888DD3...` (`3,549,696 B`) | `E942899AF6888DD3...` (`3,549,696 B`) | `E26110B44CCEB067...` (`3,549,696 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Agents.dll` | `CD93A737DE112925...` (`960,512 B`) | `CD93A737DE112925...` (`960,512 B`) | `57136EED525E91F4...` (`960,512 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Platform.dll` | `CA6AF428D0B9FA44...` (`715,264 B`) | `CA6AF428D0B9FA44...` (`715,264 B`) | `E12BF3740A217B89...` (`715,264 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Drawing.dll` | `203799BFDA04A446...` (`543,232 B`) | `203799BFDA04A446...` (`543,232 B`) | `E7ABBE00287DF651...` (`543,232 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Drawing3D.dll` | `3362838E6B37E3ED...` (`399,360 B`) | `3362838E6B37E3ED...` (`399,360 B`) | `1B9E95AD9A0FE753...` (`399,360 B`) | **FAIL (Post-build `.csproj` edit)** |
| `HeyClicky.Contracts.dll` | `FBF480E87CE0F43B...` (`130,048 B`) | `FBF480E87CE0F43B...` (`130,048 B`) | `CAD323CF271705E7...` (`130,048 B`) | **FAIL (Post-build `.csproj` edit)** |
| `clicky-draw-pieces.js` | `EAAC727FD91CE33B...` (`66,985 B`) | `EAAC727FD91CE33B...` (`66,985 B`) | `8FE2A67F97DF36E4...` (`66,086 B` in `%LOCALAPPDATA%\HeyClickyApp\current`) | **FAIL (Out of sync with LocalAppData)** |
| Other 4 `Assets/Engine/*.js` | Match `dist/kursor-desktop/` | Match `src/desktop/Kursor.Drawing/` | Match `%LOCALAPPDATA%\HeyClickyApp\current` | **PASS** |

---

## 3. Detailed Findings (§4 Schema)

---

### `ID: AUD-A-001`
- **Severity**: `HIGH`
- **Category**: `WS-A — Build & Compiler Hygiene`
- **Title**: Clean non-incremental build emits 343 compiler/analyzer warnings (`CA2014` stackalloc-in-loop, `SYSLIB1032`, `CS4014`) while `Directory.Build.props` globally suppresses 9 additional warning codes and disables nullable warnings
- **File:Line**: [`src/desktop/Directory.Build.props:3`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L3), [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/PrecisionTouchpad.cs:221`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/PrecisionTouchpad.cs#L221), [`src/desktop/Kursor.Core/HeyClicky.Core/CoreJsonContext.cs:51`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core/CoreJsonContext.cs#L51)
- **Description**: Contrary to [`README.md:53`](file:///C:/dev/heyclicky-clone/README.md#L53) and [`docs/INDEX.md:53`](file:///C:/dev/heyclicky-clone/docs/INDEX.md#L53) (`"All 7 Projects Compiling (0 Warnings, 0 Errors)"`), running a clean non-incremental build (`dotnet build src/desktop/Kursor.sln -c Debug --no-incremental` or `-c Release --no-incremental`) emits **343 warnings** (`472` warnings when `/p:NoWarn=` is passed). Previous incremental runs printed `0 Warning(s)` only because MSBuild skipped already-built compilation targets. High-impact warnings include:
  - `CA2014` at [`PrecisionTouchpad.cs:221`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/PrecisionTouchpad.cs#L221): `stackalloc` inside a `for`/`while` loop when enumerating raw input devices, risking stack overflow.
  - `SYSLIB1032` across 6 `JsonSerializerContext` classes in `Kursor.Core` ([`JsonNodeContext.cs:13`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/JsonNodeContext.cs#L13), [`InkJsonContext.cs:15`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/InkJsonContext.cs#L15), [`NotchChatJsonContext.cs:16`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Notch/NotchChatJsonContext.cs#L16), [`ProactiveJsonContext.cs:15`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Proactive/ProactiveJsonContext.cs#L15), [`EarsLanguageJson.cs:13`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/EarsLanguageJson.cs#L13), [`CoreJsonContext.cs:51`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core/CoreJsonContext.cs#L51)): decompiled classes lack the `partial` keyword, disabling `System.Text.Json` source generation.
  - `CS4014` (`68` occurrences): unawaited async calls across UI and platform services.
  - [`Directory.Build.props:3`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L3) globally masks `SYSLIB1050;SYSLIB1043;CS0649;CS0169;CS0414;CS8632;CS8981;CS8500;CS9191`, and all 7 `.csproj` files set `<Nullable>annotations</Nullable>` (disabling nullable reference warnings) and `<AllowUnsafeBlocks>True</AllowUnsafeBlocks>` (even on `Kursor.Contracts`).
- **Reproduction Steps**:
  1. Run `dotnet build C:\dev\heyclicky-clone\src\desktop\Kursor.sln -c Debug --no-incremental`.
  2. Run `dotnet build C:\dev\heyclicky-clone\src\desktop\Kursor.sln -c Release --no-incremental`.
  3. Inspect [`docs/audit/evidence/ws-ab/dotnet-build-debug.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-debug.txt) and [`docs/audit/evidence/ws-ab/dotnet-build-release.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-release.txt).
- **Observed vs Expected Result**:
  - **Observed**: `343 Warning(s), 0 Error(s)` in both Debug and Release (`472 Warning(s)` with `/p:NoWarn=`).
  - **Expected**: Zero dangerous runtime warnings (`CA2014`, `SYSLIB1032`), `<Nullable>enable</Nullable>`, `<AllowUnsafeBlocks>` limited to Win32/D3D11 interop projects, and truthful build metrics in documentation.
- **Evidence**: [`docs/audit/evidence/ws-ab/dotnet-build-debug.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-debug.txt), [`docs/audit/evidence/ws-ab/dotnet-build-release.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-release.txt), [`docs/audit/evidence/ws-ab/dotnet-build-unmasked-warnings.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-build-unmasked-warnings.txt)
- **Remediation**: Hoist the `stackalloc` buffer outside the loop in `PrecisionTouchpad.cs:221`, fix `SYSLIB1032` on the 6 `JsonSerializerContext` classes, remove `<AllowUnsafeBlocks>True</AllowUnsafeBlocks>` from `Kursor.Contracts.csproj`, and narrow `<NoWarn>` in `Directory.Build.props`.

---

### `ID: AUD-A-002`
- **Severity**: `HIGH`
- **Category**: `WS-A — Dependency & Vulnerability Management`
- **Title**: All 7 desktop `.csproj` files use zero NuGet `<PackageReference>` items and bind 32 third-party/framework DLLs via raw local `<HintPath>`
- **File:Line**: [`src/desktop/Directory.Build.props:4-11`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L4-L11), [`src/desktop/Kursor.App/Kursor.App.csproj:39-145`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Kursor.App.csproj#L39-L145)
- **Description**: None of the 7 `.csproj` files in `src/desktop/Kursor.sln` declare any `<PackageReference>`. Instead, [`Directory.Build.props:4-11`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L4-L11) sets `$(HeyClickyRuntimeDir)` to `dist/kursor-desktop/` (falling back to `%LOCALAPPDATA%\HeyClickyApp\current`), and the projects use `<Reference Include="..."><HintPath>$(HeyClickyRuntimeDir)\*.dll</HintPath></Reference>` across 32 third-party/framework DLLs (`Sentry.dll`, `Velopack.dll`, `Newtonsoft.Json.dll`, `ClearScript.Core.dll`, `ClearScript.V8.dll`, `CommunityToolkit.Mvvm.dll`, `Vortice.*.dll`, `SharpGen.Runtime.*.dll`, `Microsoft.WinUI.dll`, `Microsoft.Windows.SDK.NET.dll`, etc.). Consequently, `dotnet list src/desktop/Kursor.sln package --vulnerable --include-transitive` is completely blinded and reports no packages, and the solution cannot build on a fresh git clone unless `dist/kursor-desktop/` or an installed HeyClicky binary directory is already populated.
- **Reproduction Steps**:
  1. Run `dotnet list C:\dev\heyclicky-clone\src\desktop\Kursor.sln package --vulnerable --include-transitive`.
  2. Inspect [`src/desktop/Directory.Build.props:4-11`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props#L4-L11) and [`src/desktop/Kursor.App/Kursor.App.csproj:39-145`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Kursor.App.csproj#L39-L145).
- **Observed vs Expected Result**:
  - **Observed**: `0` `<PackageReference>` items across all 7 `.csproj` files; `dotnet list package --vulnerable` audits zero third-party packages.
  - **Expected**: All third-party dependencies declared via version-pinned NuGet `<PackageReference>` items so `dotnet restore` and `dotnet list package --vulnerable` work deterministically.
- **Evidence**: [`docs/audit/evidence/ws-ab/dotnet-list-vulnerable.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/dotnet-list-vulnerable.txt)
- **Remediation**: Migrate raw `<Reference HintPath="$(HeyClickyRuntimeDir)\*.dll">` entries to standard NuGet `<PackageReference>` items in `Directory.Packages.props` and each `.csproj`.

---

### `ID: AUD-A-003`
- **Severity**: `HIGH`
- **Category**: `WS-A — Packaging & Distribution Integrity`
- **Title**: `scripts/build-dist.ps1` and `scripts/overlay-and-launch.ps1` package `Debug` assemblies + `.pdb` symbols, omit `HeyClicky.exe` apphost (`SHA-256` mismatch), and leave `dist/kursor-desktop/*.dll` stale after `.csproj` edits
- **File:Line**: [`scripts/build-dist.ps1:34,72-87`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L34), [`scripts/overlay-and-launch.ps1:14-29`](file:///C:/dev/heyclicky-clone/scripts/overlay-and-launch.ps1#L14-L29)
- **Description**:
  1. [`scripts/build-dist.ps1:34`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L34) hardcodes `& $dotnet build $SlnPath -c Debug -v minimal` and lines 83–86 copy `.pdb` debug symbols into `dist/kursor-desktop/`.
  2. [`scripts/build-dist.ps1:72-80`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L72-L80) and [`scripts/overlay-and-launch.ps1:14-22`](file:///C:/dev/heyclicky-clone/scripts/overlay-and-launch.ps1#L14-L22) copy the 7 `.dll` files (`HeyClicky.Contracts.dll` .. `HeyClicky.dll`) from `Kursor.App/bin/Debug/net9.0`, but **omit `HeyClicky.exe`**. As a result, `src/desktop/Kursor.App/bin/Debug/net9.0/HeyClicky.exe` (`420,352` bytes, SHA256 `32C071E81A0677191F9160A5BC116E5E0B49BA60FFBF817A4EC659088679034D`) does not match `dist/kursor-desktop/HeyClicky.exe` (`429,920` bytes, SHA256 `26AB779203881266FB8FC0BD4664D26D7167C0C185C3B63638C79EA7EEE3F4F9`).
  3. 5 `.csproj` files and `Directory.Build.props` were modified in commit `bd1ab7d` (`2026-11-13T10:59:36Z`), after the `.dll` binaries in `dist/kursor-desktop/` (`2026-11-11` / `2026-11-12`) were compiled, and a clean rebuild produces new SHA-256 hashes for all 7 `.dll`s.
- **Reproduction Steps**:
  1. Compare SHA-256 hashes in [`docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json) and [`docs/audit/evidence/ws-ab/fresh-debug-build-hashes.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/fresh-debug-build-hashes.json).
  2. Inspect [`scripts/build-dist.ps1:34,72-87`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L72-L87) and [`docs/audit/evidence/ws-ab/project-staleness-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/project-staleness-check.json).
- **Observed vs Expected Result**:
  - **Observed**: `HeyClicky.exe` SHA-256 mismatches between `bin/Debug/net9.0` and `dist/kursor-desktop/`; `dist/kursor-desktop/` ships `Debug` builds with `.pdb` files.
  - **Expected**: `build-dist.ps1` builds `-c Release` by default, stages the compiled `.exe` apphost alongside the 7 `.dll`s, and excludes `.pdb` debug symbols from release packages.
- **Evidence**: [`docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/artifact-hash-comparison-prebuild.json), [`docs/audit/evidence/ws-ab/fresh-debug-build-hashes.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/fresh-debug-build-hashes.json), [`docs/audit/evidence/ws-ab/project-staleness-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/project-staleness-check.json)
- **Remediation**: Add `[string]$Configuration = "Release"` to `scripts/build-dist.ps1`, copy the compiled apphost executable, and re-run `build-dist.ps1` after any `.csproj` or source change.

---

### `ID: AUD-A-004`
- **Severity**: `MEDIUM`
- **Category**: `WS-A — Asset Staging & Packaging`
- **Title**: `scripts/build-dist.ps1` and `scripts/overlay-and-launch.ps1` reference non-existent `src/desktop/Kursor.Drawing3D/Assets/Engine/`, and `clicky-draw-pieces.js` is out of sync with `%LOCALAPPDATA%\HeyClickyApp\current`
- **File:Line**: [`scripts/build-dist.ps1:105`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L105), [`scripts/overlay-and-launch.ps1:44`](file:///C:/dev/heyclicky-clone/scripts/overlay-and-launch.ps1#L44), [`src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj:34-37`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj#L34-L37)
- **Description**: [`scripts/build-dist.ps1:105`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1#L105), [`scripts/overlay-and-launch.ps1:44`](file:///C:/dev/heyclicky-clone/scripts/overlay-and-launch.ps1#L44), and [`Kursor.Drawing3D.csproj:34-37`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj#L34-L37) reference `src/desktop/Kursor.Drawing3D/Assets/Engine/*`, which does not exist on disk (`<DIR_MISSING>`). All 5 V8 JS files (`clicky-draw-engine.js`, `clicky-draw-native.js`, `clicky-draw-pieces.js`, `clicky-three-parts.js`, `clicky-three.js`) actually reside in `src/desktop/Kursor.Drawing/Assets/Engine/`. Additionally, `clicky-draw-pieces.js` in `src/desktop/Kursor.Drawing/Assets/Engine/` and `dist/kursor-desktop/Assets/Engine/` (`66,985` bytes, SHA256 `EAAC727FD91CE33B815EC6660372E9CC7B1760725782834CE730C336A0BF1C57`) differs from `%LOCALAPPDATA%\HeyClickyApp\current\Assets\Engine\clicky-draw-pieces.js` (`66,086` bytes, SHA256 `8FE2A67F97DF36E454DB6DE6B68810185B0239D49123B1F09427B585B2E9434F`).
- **Reproduction Steps**:
  1. Run `Test-Path C:\dev\heyclicky-clone\src\desktop\Kursor.Drawing3D\Assets\Engine` (`False`).
  2. Inspect [`docs/audit/evidence/ws-ab/js-assets-comparison.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/js-assets-comparison.json).
- **Observed vs Expected Result**:
  - **Observed**: `Kursor.Drawing3D/Assets/Engine` is missing; `%LOCALAPPDATA%\HeyClickyApp\current\Assets\Engine\clicky-draw-pieces.js` is stale (`66,086` B vs `66,985` B).
  - **Expected**: Consistent JS engine asset directory structure across `.csproj` files and staging scripts.
- **Evidence**: [`docs/audit/evidence/ws-ab/js-assets-comparison.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/js-assets-comparison.json)
- **Remediation**: Either move `clicky-three.js` and `clicky-three-parts.js` into `src/desktop/Kursor.Drawing3D/Assets/Engine/` or remove the non-existent path from `Kursor.Drawing3D.csproj`, `build-dist.ps1`, and `overlay-and-launch.ps1`.

---

### `ID: AUD-A-005`
- **Severity**: `MEDIUM`
- **Category**: `WS-A — Backend Dependency Security & Engine Contract`
- **Title**: Moderate vulnerability in `@hono/node-server@1.19.19` (`GHSA-rmxm-3fg6-px4f`) and unverified Node.js engine version mismatch (`>=24.0.0` vs `v22+`)
- **File:Line**: [`src/backend/package.json:14,25`](file:///C:/dev/heyclicky-clone/src/backend/package.json#L14), [`setup-kursor.ps1:27-32`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L27-L32), [`README.md:127,140`](file:///C:/dev/heyclicky-clone/README.md#L127)
- **Description**:
  1. Running `npm audit --json` in `src/backend/` reports 1 moderate vulnerability: `GHSA-rmxm-3fg6-px4f` (CVSS 5.3, CWE-174: Double decoding of percent-encoded paths in `@hono/node-server` `<2.1.3`, currently installed at `1.19.19` via `"@hono/node-server": "^1.14.0"`).
  2. [`src/backend/package.json:25`](file:///C:/dev/heyclicky-clone/src/backend/package.json#L25) specifies `"engines": { "node": ">=24.0.0" }`, whereas [`setup-kursor.ps1:29`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L29) and [`README.md:127,140`](file:///C:/dev/heyclicky-clone/README.md#L127) instruct users to install `Node.js v22+`, and `setup-kursor.ps1:27-32` only tests `if (-not $nodeVer)` without parsing or enforcing the major version number (so Node 18/20 passes `setup-kursor.ps1` and crashes at runtime on `import { DatabaseSync } from "node:sqlite"`).
- **Reproduction Steps**:
  1. Run `npm audit --json` in `C:\dev\heyclicky-clone\src\backend`.
  2. Compare [`src/backend/package.json:14,25`](file:///C:/dev/heyclicky-clone/src/backend/package.json#L14) with [`setup-kursor.ps1:27-32`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L27-L32).
- **Observed vs Expected Result**:
  - **Observed**: `npm audit` reports 1 moderate vulnerability (`GHSA-rmxm-3fg6-px4f`); `setup-kursor.ps1` does not validate the Node major version.
  - **Expected**: `0` vulnerabilities in `npm audit` (`@hono/node-server >= 2.1.3`) and explicit major-version enforcement in `setup-kursor.ps1`.
- **Evidence**: [`docs/audit/evidence/ws-ab/npm-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/npm-audit.json), [`docs/audit/evidence/ws-ab/npm-outdated.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/npm-outdated.json)
- **Remediation**: Upgrade `@hono/node-server` to `^2.1.3` in `src/backend/package.json` and parse/validate the major version of `node -v` in `setup-kursor.ps1`.

---

### `ID: AUD-A-006`
- **Severity**: `MEDIUM`
- **Category**: `WS-A — Portability & Hardcoded Developer Paths`
- **Title**: Hardcoded developer filesystem paths in `src/backend/src/routes/worker.ts` (`C:\Users\ubaid\...`) and desktop C# files (`C:\Users\niles\...`)
- **File:Line**: [`src/backend/src/routes/worker.ts:7-8`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L7-L8), [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:14`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L14), [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs:75`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L75)
- **Description**:
  - [`src/backend/src/routes/worker.ts:7-8`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L7-L8) hardcodes `const LOCAL_APP_CONFIG_PATH = "C:\\Users\\ubaid\\AppData\\Local\\HeyClicky\\app-config.json";`. On any machine where the Windows username is not `ubaid`, `existsSync(LOCAL_APP_CONFIG_PATH)` is `false` and `/config` / `/app-config` falls back to `DEFAULT_APP_CONFIG_LINKS` pointing to `heyclicky.canny.io`, `reddit.com/r/heyclicky_`, and `heyclicky.com/changelog`.
  - [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:14`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L14) hardcodes `C:\Users\niles\code\clicky-closed\windows\src\HeyClicky.App\`.
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs:75`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L75) hardcodes `C:\Users\niles\code\clicky-closed\windows\src\HeyClicky.Agents\Harness\AgentHarnessRunnerRuntime.cs`.
- **Reproduction Steps**:
  1. Read [`src/backend/src/routes/worker.ts:7-8`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L7-L8).
  2. Read [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:14`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L14) and [`AgentHarnessRunnerProcess.cs:75`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L75).
- **Observed vs Expected Result**:
  - **Observed**: User-specific paths (`C:\Users\ubaid\...` and `C:\Users\niles\...`) are hardcoded in source.
  - **Expected**: Dynamic path resolution via `process.env.LOCALAPPDATA` / `os.homedir()` / `AppContext.BaseDirectory`.
- **Evidence**: [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)
- **Remediation**: Compute `LOCAL_APP_CONFIG_PATH` dynamically using `process.env.LOCALAPPDATA` or `os.homedir()` in `worker.ts`, and remove `C:\Users\niles\...` from `AssemblyInfo.cs` and `AgentHarnessRunnerProcess.cs`.

---

### `ID: AUD-A-007`
- **Severity**: `HIGH`
- **Category**: `WS-A — Launcher Lifecycle & Error Handling`
- **Title**: `start-kursor.ps1 -Restart` leaves backend `node.exe` and sidecars running, fails silently when port `8787` is occupied by a non-Kursor service, and breaks on paths with single quotes
- **File:Line**: [`start-kursor.ps1:76-120`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L76-L120)
- **Description**:
  1. **Incomplete `-Restart` (`lines 114-120`)**: Passing `-Restart` executes `Stop-Process -Name "HeyClicky" -Force`, but never stops the backend `node.exe` listening on port `8787` or child sidecars (`clicky-harness.exe`, `ClickyBrowserHost.exe`). Thus `start-kursor.cmd -Restart` does not restart the backend or reload `.env`.
  2. **Silent Port `8787` Conflict (`lines 76-109`)**: `Test-BackendRunning` requests `http://127.0.0.1:8787/health` and checks `$res.status -eq "ok"`. If a non-Kursor process occupies port `8787`, `Test-BackendRunning` returns `$false`, `start-kursor.ps1` spawns a background `npm run dev` that crashes with `EADDRINUSE`, sleeps 3 seconds without re-checking `Test-BackendRunning`, and launches the desktop client against the wrong port `8787` listener.
  3. **Unescaped Single-Quote Interpolation (`lines 104-106`)**: `"Set-Location '$BackendDir'; npm run dev"` breaks if `$BackendDir` contains a single quote (`'`).
- **Reproduction Steps**:
  1. Read [`start-kursor.ps1:76-120`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L76-L120).
  2. Run `start-kursor.ps1 -Restart -BackendOnly` while the backend is running and verify the `node.exe` PID on port `8787` is unchanged.
- **Observed vs Expected Result**:
  - **Observed**: `-Restart` only kills `HeyClicky.exe`; port `8787` readiness is never verified after spawning `npm run dev`.
  - **Expected**: `-Restart` stops `HeyClicky`, sidecars, and the port `8787` listener; startup polls `Test-BackendRunning` and fails with an explicit error if port `8787` is occupied by another process.
- **Evidence**: [`start-kursor.ps1:76-120`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L76-L120), [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json)
- **Remediation**: In `start-kursor.ps1`, kill `HeyClicky`, `clicky-harness`, `ClickyBrowserHost`, and the owning PID of TCP port `8787` on `-Restart`, pass `-WorkingDirectory $BackendDir` to `Start-Process`, and poll `Test-BackendRunning` after spawning.

---

### `ID: AUD-A-008`
- **Severity**: `HIGH`
- **Category**: `WS-A — Windows Autostart & Registry Lifecycle`
- **Title**: Triple Windows autostart collision across `install-autostart.ps1` (`Run\Kursor` + `Startup\Kursor.lnk`) and in-app `LaunchAtLogin.cs` (`Run\HeyClicky`), plus incomplete `uninstall-autostart.ps1` cleanup
- **File:Line**: [`scripts/install-autostart.ps1:21-38`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L21-L38), [`scripts/uninstall-autostart.ps1:10-25`](file:///C:/dev/heyclicky-clone/scripts/uninstall-autostart.ps1#L10-L25), [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs:12-18`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs#L12-L18)
- **Description**:
  1. [`scripts/install-autostart.ps1:23`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L23) sets `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run\Kursor` = `wscript.exe //B //Nologo "...\scripts\start-kursor-hidden.vbs"`.
  2. [`scripts/install-autostart.ps1:28-36`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L28-L36) **also** creates `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\Kursor.lnk` pointing to the same `start-kursor-hidden.vbs`. At Windows logon, Explorer executes both `HKCU\...\Run` and the `Startup` folder concurrently, launching `start-kursor.ps1` twice in parallel.
  3. Meanwhile, the in-app "Launch at login" toggle ([`LaunchAtLogin.cs:12,18`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs#L12-L18)) reads and writes `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run\HeyClicky` = `"...\HeyClicky.exe" --launched-at-login` (which starts `HeyClicky.exe` without starting the local Node.js backend).
  4. Live system inspection ([`system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json)) confirms all **three** autostart triggers (`RegistryRun_Kursor`, `RegistryRun_HeyClicky`, and `StartupShortcut_Kursor_Exists`) are active simultaneously on the host machine.
  5. [`scripts/uninstall-autostart.ps1`](file:///C:/dev/heyclicky-clone/scripts/uninstall-autostart.ps1#L10-L25) removes `Run\Kursor` and `Startup\Kursor.lnk`, but leaves `Run\HeyClicky` and `Desktop\Kursor.lnk` behind.
- **Reproduction Steps**:
  1. Read [`scripts/install-autostart.ps1:21-49`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L21-L49), [`scripts/uninstall-autostart.ps1:10-25`](file:///C:/dev/heyclicky-clone/scripts/uninstall-autostart.ps1#L10-L25), and [`LaunchAtLogin.cs:12-18`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/LaunchAtLogin.cs#L12-L18).
  2. Inspect [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json).
- **Observed vs Expected Result**:
  - **Observed**: Three conflicting autostart mechanisms (`Run\Kursor`, `Run\HeyClicky`, `Startup\Kursor.lnk`) exist simultaneously.
  - **Expected**: One canonical autostart registration (`HKCU\...\Run\Kursor` only) shared by `install-autostart.ps1`, `uninstall-autostart.ps1`, and `LaunchAtLogin.cs`.
- **Evidence**: [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json)
- **Remediation**: Remove the redundant `Startup\Kursor.lnk` creation from `install-autostart.ps1`, update `LaunchAtLogin.cs` to read/write `Run\Kursor` pointing to `start-kursor-hidden.vbs`, and clean up `Run\HeyClicky` and `Desktop\Kursor.lnk` in `uninstall-autostart.ps1`.

---

### `ID: AUD-A-009`
- **Severity**: `MEDIUM`
- **Category**: `WS-A — AppData Storage Lifecycle`
- **Title**: Split `%LOCALAPPDATA%\HeyClicky` vs `%LOCALAPPDATA%\Kursor` state between setup scripts and `AppPaths.cs`
- **File:Line**: [`src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs:7-28`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs#L7-L28), [`setup-kursor.ps1:56-61`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L56-L61), [`scripts/seed-local-auth.ps1:25-30,81-90`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L25-L30)
- **Description**: [`setup-kursor.ps1:56-61`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1#L56-L61) and [`scripts/seed-local-auth.ps1:25-30,81-90`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L25-L30) create and populate both `%LOCALAPPDATA%\HeyClicky` and `%LOCALAPPDATA%\Kursor`. However, [`AppPaths.cs:12`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs#L12) defaults `Root` to `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeyClicky")` unless `KURSOR_DATA_DIR` or `HEYCLICKY_DATA_DIR` is set — which neither `start-kursor.ps1` nor `setup-kursor.ps1` sets. Thus `%LOCALAPPDATA%\Kursor` is an orphaned decoy directory never read or written by the desktop app.
- **Reproduction Steps**:
  1. Read [`src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs:7-28`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Contracts/HeyClicky/AppPaths.cs#L7-L28).
  2. Compare `LocalAppData_HeyClicky_Files` vs `LocalAppData_Kursor_Files` in [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json).
- **Observed vs Expected Result**:
  - **Observed**: Desktop app reads/writes `%LOCALAPPDATA%\HeyClicky` exclusively while `%LOCALAPPDATA%\Kursor` holds stale decoy files.
  - **Expected**: Single unified `%LOCALAPPDATA%\Kursor` (or `%LOCALAPPDATA%\HeyClicky`) directory across `AppPaths.cs`, `worker.ts`, and setup scripts.
- **Evidence**: [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json)
- **Remediation**: Change `AppPaths.cs:12` default to `"Kursor"` (with fallback migration from `"HeyClicky"`) or remove the duplicate `%LOCALAPPDATA%\Kursor` seeding from `setup-kursor.ps1` and `seed-local-auth.ps1`.

---

### `ID: AUD-A-010`
- **Severity**: `MEDIUM`
- **Category**: `WS-A — Repository & Licensing Hygiene`
- **Title**: Missing `LICENSE` / `THIRD_PARTY_NOTICES`, unanchored `[Rr]elease/` and `[Dd]ebug/` rules in `.gitignore`, and non-resilient `scripts/package-portable.ps1`
- **File:Line**: [`.gitignore:38`](file:///C:/dev/heyclicky-clone/.gitignore#L38), [`scripts/package-portable.ps1:25-56`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1#L25-L56)
- **Description**:
  1. No `LICENSE` or `THIRD_PARTY_NOTICES` file exists in the repository root ([`git-repo-hygiene.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/git-repo-hygiene.json)).
  2. [`.gitignore:38-39`](file:///C:/dev/heyclicky-clone/.gitignore#L38-L39) uses unanchored `[Dd]ebug/` and `[Rr]elease/`, which ignore any directory named `Release` or `Debug` anywhere in the source tree.
  3. [`scripts/package-portable.ps1:25-56`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1#L25-L56) does not check whether `dist/kursor-desktop/HeyClicky.exe` exists before staging and fails if `HeyClicky.exe` or `clicky-harness.exe` is running and locking files in `dist/kursor-desktop/`.
- **Reproduction Steps**:
  1. Check `Test-Path C:\dev\heyclicky-clone\LICENSE` (`False`).
  2. Read [`.gitignore:35-46`](file:///C:/dev/heyclicky-clone/.gitignore#L35-L46) and [`scripts/package-portable.ps1:25-56`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1#L25-L56).
- **Observed vs Expected Result**:
  - **Observed**: Missing `LICENSE` and `THIRD_PARTY_NOTICES`; unanchored `[Rr]elease/` and `[Dd]ebug/` in `.gitignore`; no pre-flight check in `package-portable.ps1`.
  - **Expected**: Explicit licensing/attribution files, anchored `/release/` and `**/bin/[Rr]elease/` rules in `.gitignore`, and pre-flight validation in `package-portable.ps1`.
- **Evidence**: [`docs/audit/evidence/ws-ab/git-repo-hygiene.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/git-repo-hygiene.json)
- **Remediation**: Add `LICENSE` and `THIRD_PARTY_NOTICES.md`, anchor build output rules in `.gitignore`, and add a pre-flight check in `scripts/package-portable.ps1`.

---

### `ID: AUD-B-001`
- **Severity**: `HIGH`
- **Category**: `WS-B — Binary & Assembly Branding`
- **Title**: All 7 desktop `.csproj` projects still compile as `HeyClicky*.dll` / `HeyClicky.exe` and stamp `HeyClicky` PE `AssemblyInfo` metadata
- **File:Line**: [`src/desktop/Kursor.App/Kursor.App.csproj:8`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Kursor.App.csproj#L8), [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:8-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L8-L17)
- **Description**: All 7 `.csproj` files in `src/desktop/` still set `<AssemblyName>HeyClicky*</AssemblyName>` (`HeyClicky.Contracts`, `HeyClicky.Drawing3D`, `HeyClicky.Drawing`, `HeyClicky.Platform`, `HeyClicky.Core`, `HeyClicky.Agents`, `HeyClicky`) and `<RootNamespace>HeyClicky*</RootNamespace>`. Furthermore, [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:8-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L8-L17) stamps `AssemblyCompany("HeyClicky")`, `AssemblyProduct("HeyClicky")`, `AssemblyTitle("HeyClicky")`, and `Sentry.ProjectDirectory = "C:\\Users\\niles\\code\\clicky-closed\\windows\\src\\HeyClicky.App\\"` into the compiled PE headers.
- **Reproduction Steps**:
  1. Read [`src/desktop/Kursor.App/Kursor.App.csproj:1-15`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Kursor.App.csproj#L1-L15) and [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:1-18`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L1-L18).
  2. Inspect [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json).
- **Observed vs Expected Result**:
  - **Observed**: Output binaries are named `HeyClicky.exe` / `HeyClicky.*.dll` with PE Company/Product/Title = `"HeyClicky"`.
  - **Expected**: PE metadata and assembly names rebranded to `Kursor`.
- **Evidence**: [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)
- **Remediation**: Update `AssemblyCompany`, `AssemblyProduct`, and `AssemblyTitle` in `Properties/AssemblyInfo.cs` to `"Kursor"` and remove `Sentry.ProjectDirectory`.

---

### `ID: AUD-B-002`
- **Severity**: `CRITICAL`
- **Category**: `WS-B — Data Exfiltration / Production Cloud Fallback & Telemetry`
- **Title**: Desktop binary bakes in live HeyClicky production Sentry DSN (`SendDefaultPii = true`), PostHog key, Velopack feed, and `api.heyclicky.com` / Supabase JWT fallbacks
- **File:Line**: [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:15-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L15-L17), [`src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs:36-43`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L36-L43), [`src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs:215-265`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs#L215-L265), [`src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/ClickyAnalytics.cs:307-315`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/ClickyAnalytics.cs#L307-L315), [`src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkImages.cs:60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkImages.cs#L60)
- **Description**: The desktop application embeds live production endpoints and telemetry keys belonging to HeyClicky:
  1. [`AssemblyInfo.cs:15-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L15-L17) and [`TelemetryConfiguration.cs:20-26`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/TelemetryConfiguration.cs#L20-L26) embed:
     - `HeyClicky.PostHogApiKey`: `"phc_xcQPygmhTMzzYh8wNW92CCwoXmnzqyChAixh8zgpqC3C"` (`https://us.i.posthog.com`)
     - `HeyClicky.SentryDsn`: `"https://9e98b32e152dad3e7e5d30448fd929d9@o4511254886154240.ingest.us.sentry.io/4512274263572480"`
     - `HeyClicky.UpdateFeedUrl`: `"https://github.com/heyclicky/clicky-releases-windows/releases/latest/download"`
  2. [`AppBundleConfiguration.cs:36-43`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L36-L43): If `%LOCALAPPDATA%\HeyClicky\use-production-backend.flag` exists (which can be created via the in-app Developer Settings page), `ApplyProductionBackendOverride` overrides `endpoints.json` and connects to `https://api.heyclicky.com` and `https://mrpvynsdsnimuisyhkow.supabase.co` with HeyClicky's live Supabase anon JWT (`eyJ...qf1-em-OB-OtyX4WTuGnSi28djq-5T0k0whKvcTtTAY`).
  3. Both [`CrashReporting.StartSentry()`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs#L225) and [`ClickyAnalytics.Configure()`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/ClickyAnalytics.cs#L298) only disable Sentry and PostHog when `AppBundleConfiguration.IsLocalDevelopmentConfiguration(endpoints)` returns `true` (`localhost` or `127.0.0.1`). If `use-production-backend.flag` is present, or a user configures `endpoints.json` with a LAN/Tailscale IP for their self-hosted Kursor backend, `IsLocalDevelopmentConfiguration` returns `false` — activating Sentry crash uploads (`SendDefaultPii = true` at [`CrashReporting.cs:233`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs#L233)), PostHog event batching ([`PostHogAnalyticsSink.cs:86`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/PostHogAnalyticsSink.cs#L86)), and Velopack auto-update checks against HeyClicky's GitHub repository ([`UpdateService.cs:159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services.Updates/UpdateService.cs#L159)).
  4. [`ClickyInkImages.cs:60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkImages.cs#L60) initializes `public static string WorkerBaseUrl { get; set; } = "https://api.heyclicky.com";` with `User-Agent: Mozilla/5.0 (Windows NT 10.0) HeyClicky`.
- **Reproduction Steps**:
  1. Read [`src/desktop/Kursor.App/Properties/AssemblyInfo.cs:15-17`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Properties/AssemblyInfo.cs#L15-L17).
  2. Read [`src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs:36-43,167-189`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L36-L43).
  3. Read [`src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs:215-265`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs#L215-L265) and [`ClickyInkImages.cs:60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkImages.cs#L60).
- **Observed vs Expected Result**:
  - **Observed**: Live HeyClicky Sentry DSN, PostHog key, Velopack feed URL, Supabase anon JWT, and `https://api.heyclicky.com` fallback are compiled into the desktop binary.
  - **Expected**: Zero third-party HeyClicky telemetry credentials, update feeds, or production cloud fallbacks in Kursor binaries.
- **Evidence**: [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)
- **Remediation**: Remove `HeyClicky.PostHogApiKey`, `HeyClicky.SentryDsn`, and `HeyClicky.UpdateFeedUrl` from `AssemblyInfo.cs`, delete `ApplyProductionBackendOverride` in `AppBundleConfiguration.cs`, and change `ClickyInkImages.WorkerBaseUrl` default to `http://127.0.0.1:8787`.

---

### `ID: AUD-B-003`
- **Severity**: `HIGH`
- **Category**: `WS-B — OS IPC, Process Killer & Registry Hijacking`
- **Title**: `SingleInstance.cs` collides on `Local\HeyClicky-<SID>` mutex/pipe and kills `HeyClicky.exe` processes, while `ProtocolRegistration` and `FileOpenRegistration` hijack `heyclicky://`, `clicky://`, and `HeyClicky.Document`
- **File:Line**: [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs:80,312-341`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L80), [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ProtocolRegistration.cs:10`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ProtocolRegistration.cs#L10), [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/FileOpenRegistration.cs:13-47`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/FileOpenRegistration.cs#L13-L47), [`scripts/seed-local-auth.ps1:94`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L94)
- **Description**:
  1. [`SingleInstance.cs:80`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L80) defaults to `string appId = "HeyClicky"`, creating mutex `Local\HeyClicky-<SID>`, memory-mapped file `Local\HeyClicky-<SID>-primary`, and named pipe `HeyClicky-<SID>-args`. In `EndProcess` ([`SingleInstance.cs:312-341`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L312-L341)), it checks `process.ProcessName.Equals("HeyClicky", StringComparison.OrdinalIgnoreCase)` and calls `process.Kill(entireProcessTree: true)`.
  2. [`ProtocolRegistration.cs:10`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ProtocolRegistration.cs#L10) and [`scripts/seed-local-auth.ps1:94`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L94) register `HKCU\Software\Classes\heyclicky` and `HKCU\Software\Classes\clicky` (never `kursor://`).
  3. [`FileOpenRegistration.cs:13-47`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/FileOpenRegistration.cs#L13-L47) registers `HKCU\Software\Classes\HeyClicky.Document` and `HKCU\Software\Classes\Applications\HeyClicky.exe` across 22 file extensions (`.md`, `.txt`, `.pdf`, `.png`, `.jpg`, `.docx`, `.xlsx`, `.pptx`, `.csv`, `.json`, `.yaml`, `.html`, etc.).
- **Reproduction Steps**:
  1. Read [`SingleInstance.cs:80-115,312-348`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L80-L115), [`ProtocolRegistration.cs:1-50`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ProtocolRegistration.cs#L1-L50), and [`FileOpenRegistration.cs:1-60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/FileOpenRegistration.cs#L1-L60).
  2. Inspect [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json).
- **Observed vs Expected Result**:
  - **Observed**: `RegistryClasses_heyclicky_Exists = true`, `RegistryClasses_clicky_Exists = true`, `RegistryClasses_kursor_Exists = false`; mutex/pipe use `HeyClicky-<SID>`.
  - **Expected**: Isolated `Local\Kursor-<SID>` mutex/pipe, `kursor://` URI scheme, and `Kursor.Document` file associations.
- **Evidence**: [`docs/audit/evidence/ws-ab/system-state-check.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/system-state-check.json), [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)
- **Remediation**: Change `appId` in `SingleInstance.cs` to `"Kursor"`, register `"kursor"` in `ProtocolRegistration.cs` and `seed-local-auth.ps1`, and rename `HeyClicky.Document` to `Kursor.Document` in `FileOpenRegistration.cs`.

---

### `ID: AUD-B-004`
- **Severity**: `MEDIUM`
- **Category**: `WS-B — UI, Prompts & Support Branding`
- **Title**: 4,712 `HeyClicky` and 19,632 `Clicky` references remain across desktop UI dialogs, tray window class, agent system prompts, and backend `/app-config` links
- **File:Line**: [`src/desktop/Kursor.App/HeyClicky.App/App.cs:111`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/App.cs#L111), [`src/desktop/Kursor.App/HeyClicky.App.Shell/TrayIcon.cs:232`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Shell/TrayIcon.cs#L232), [`src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs:25`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs#L25), [`src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs:74-91`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L74-L91), [`src/backend/src/routes/worker.ts:9-15`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L9-L15)
- **Description**: Codebase-wide regex scanning ([`branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)) found `4,712` occurrences of `HeyClicky` and `19,632` occurrences of `Clicky` across `src/desktop/`. Specific user-visible surfaces still branded as `HeyClicky` include:
  - Fatal startup error dialog in [`App.cs:111`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/App.cs#L111) (`"HeyClicky couldn't start... email hi@heyclicky.com"`, caption `"HeyClicky"`) and [`SingleInstance.cs:345`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L345) (`"HeyClicky is already running... email hi@heyclicky.com"`).
  - Support contact strings in [`BillingUi.cs:503`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Billing/BillingUi.cs#L503) and [`SettingsContent.cs:1205,2540`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L1205) (`hi@heyclicky.com`).
  - Win32 tray window class `"HeyClickyTray"` in [`TrayIcon.cs:232`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Shell/TrayIcon.cs#L232) and plural tray tooltip `"${workingClickyNames.Count} Clickys working"` in [`TrayTooltip.cs:25`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs#L25).
  - Agent prompt instructions in [`CodexHomeManager.cs:74,75,88,91`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L74-L91) telling the agent to direct users to `"HeyClicky Settings -> Integrations"`.
  - Backend `/config` & `/app-config` response links in [`src/backend/src/routes/worker.ts:9-15`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L9-L15) (`https://heyclicky.canny.io/feature-requests`, `https://www.reddit.com/r/heyclicky_`, `https://heyclicky.com/changelog?p=windows`).
- **Reproduction Steps**:
  1. Inspect [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json).
  2. Read [`App.cs:111`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/App.cs#L111), [`SingleInstance.cs:345`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs#L345), [`TrayTooltip.cs:25`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs#L25), [`CodexHomeManager.cs:74-91`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L74-L91), and [`worker.ts:9-15`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L9-L15).
- **Observed vs Expected Result**:
  - **Observed**: Error dialogs, support emails, agent prompt injections, tray class, and backend config links still reference `HeyClicky`.
  - **Expected**: Complete user-facing and prompt-facing rebranding to `Kursor`.
- **Evidence**: [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json)
- **Remediation**: Replace remaining `HeyClicky` and `hi@heyclicky.com` strings in `App.cs`, `SingleInstance.cs`, `BillingUi.cs`, `SettingsContent.cs`, `TrayIcon.cs`, `TrayTooltip.cs`, `CodexHomeManager.cs`, and `src/backend/src/routes/worker.ts`.

---

### `ID: AUD-B-005`
- **Severity**: `CRITICAL`
- **Category**: `WS-B — Legal / IP / Copyright Exposure`
- **Title**: Repository commits 2,096 ILSpy-decompiled proprietary C# files, redistributes binaries digitally signed by `Humansongs, Inc`, bundles 89.4 MB of proprietary assets, and downloads official HeyClicky releases
- **File:Line**: [`scripts/install-runtime.ps1:11`](file:///C:/dev/heyclicky-clone/scripts/install-runtime.ps1#L11), [`executable-file/HeyClicky-Setup.exe`](file:///C:/dev/heyclicky-clone/executable-file/HeyClicky-Setup.exe), [`dist/kursor-desktop/`](file:///C:/dev/heyclicky-clone/dist/kursor-desktop)
- **Description**: Factual intellectual-property inventory ([`legal-ip-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/legal-ip-inventory.json) and [`git-repo-hygiene.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/git-repo-hygiene.json)):
  1. **Committed Decompiled Source**: Git tracks `2,096` C# files in `reference/decompiled/` decompiled via `ilspycmd -p` from proprietary `HeyClicky v1.1.5` assemblies, plus `2,097` directly derived C# files in `src/desktop/`.
  2. **Digitally Signed Proprietary Executables (`CN="Humansongs, Inc", O="Humansongs, Inc", L=San Francisco, S=California, C=US`)**:
     | Binary Path | Size (Bytes) | SHA-256 | Authenticode Status | Signer Subject |
     | :--- | :---: | :--- | :---: | :--- |
     | `executable-file/HeyClicky-Setup.exe` | `4,300,320` | `DE18B2E6733A952E...` | `Valid` | `CN="Humansongs, Inc"` |
     | `dist/kursor-desktop/HeyClicky.exe` | `429,920` | `26AB779203881266...` | `Valid` | `CN="Humansongs, Inc"` |
     | `dist/kursor-desktop/HeyClicky-Update.exe` | `4,043,616` | `EE17F66FF17D42CF...` | `Valid` | `CN="Humansongs, Inc"` |
     | `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` | `114,494,008` | `9D5AF4ED32EC7762...` | `Valid` | `CN="Humansongs, Inc"` |
     | `dist/kursor-desktop/ClickyBrowserHost/ClickyBrowserHost.exe` | `152,416` | `78A05E4306BE3BBA...` | `Valid` | `CN="Humansongs, Inc"` |
  3. **Bundled Proprietary Media & Prompt Assets in `dist/kursor-desktop/`**:
     - `Assets/`: `171` files (`89,405,205` bytes / ~85.3 MB) including `Audio/` (1), `Earcons/` (17), `Engine/` (5 JS engines), `Fonts/` (4), `Logos/` (34), `Referral/` (3), `Sounds/` (63), `Video/` (3 onboarding MP4s), and `VoicePreviews/` (40).
     - `BundledResources/`: `26` files (`150,225` bytes) including verbatim `clicky-model-instructions.md` and 25 skill/integration-skill files.
  4. **Direct Upstream Binary Download**: [`scripts/install-runtime.ps1:11`](file:///C:/dev/heyclicky-clone/scripts/install-runtime.ps1#L11) downloads `https://github.com/heyclicky/clicky-releases-windows/releases/latest/download/HeyClickyApp-1.1.5-win-x64-full.nupkg`.
- **Reproduction Steps**:
  1. Inspect [`docs/audit/evidence/ws-ab/legal-ip-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/legal-ip-inventory.json).
  2. Read [`scripts/install-runtime.ps1:1-40`](file:///C:/dev/heyclicky-clone/scripts/install-runtime.ps1#L1-L40).
- **Observed vs Expected Result**:
  - **Observed**: Direct inclusion of decompiled proprietary C# code in git and redistribution of `Humansongs, Inc`-signed binaries and proprietary media/prompt assets.
  - **Expected**: Clean-room reimplementation with original/licensed media, sidecars, and prompts before any external or commercial distribution.
- **Evidence**: [`docs/audit/evidence/ws-ab/legal-ip-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/legal-ip-inventory.json), [`docs/audit/evidence/ws-ab/git-repo-hygiene.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/git-repo-hygiene.json)
- **Remediation**: Replace `clicky-harness.exe` and `ClickyBrowserHost.exe` with clean-room sidecars, replace proprietary audio/video/font/prompt assets, remove `install-runtime.ps1` downloads of `HeyClickyApp-1.1.5-win-x64-full.nupkg`, and do not redistribute `reference/decompiled/` or `src/desktop/` decompiled code.

---

### `ID: AUD-N-001`
- **Severity**: `HIGH`
- **Category**: `WS-N — Configuration & Env Var Documentation Drift`
- **Title**: `README.md` documents non-existent `ONEBRAIN_MODEL` (actual: `KURSOR_BRAIN_MODEL`), `.env.example` omits `KURSOR_BRAIN_MODEL`, and 6 env vars in `.env.example` are dead no-ops in backend code
- **File:Line**: [`README.md:84`](file:///C:/dev/heyclicky-clone/README.md#L84), [`src/backend/.env.example:7,23,55,61-63`](file:///C:/dev/heyclicky-clone/src/backend/.env.example#L7), [`src/backend/src/routes/brain.ts:598-599,959-960`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L598-L599), [`src/backend/src/middleware/auth.ts:1-60`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L1-L60)
- **Description**: Full cross-check of environment variables across [`README.md`](file:///C:/dev/heyclicky-clone/README.md), [`src/backend/.env.example`](file:///C:/dev/heyclicky-clone/src/backend/.env.example), and `src/backend/src/**/*.ts` ([`docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)):

| Env Var Name | In `README.md` | In `.env.example` | Read in `src/backend/src/*.ts` | Audit Status |
| :--- | :---: | :---: | :---: | :--- |
| `ONEBRAIN_MODEL` | Yes (`line 84`) | **No** | **No** | **FAIL**: Ignored by backend! |
| `KURSOR_BRAIN_MODEL` | **No** | **No** | Yes (`brain.ts:598,959`) | **FAIL**: Undocumented in `README.md` & `.env.example`! |
| `OPENROUTER_DEFAULT_MODEL` | **No** | Yes (`line 13`) | Yes (`brain.ts:599,960`) | **PASS** (fallback behind `KURSOR_BRAIN_MODEL`) |
| `KURSOR_DEV_BYPASS_AUTH` | No | Yes (`line 7`) | **No** | **FAIL**: `middleware/auth.ts` never checks it! |
| `ANTHROPIC_API_KEY` | No | Yes (`line 23`) | **No** | **FAIL**: Dead env var |
| `COMPOSIO_API_KEY` | No | Yes (`line 55`) | **No** | **FAIL**: Dead env var |
| `WEB_SEARCH_PROVIDER` | No | Yes (`line 61`) | **No** | **FAIL**: Dead env var (`worker.ts` hardcodes DuckDuckGo/Wiki) |
| `BRAVE_API_KEY` | No | Yes (`line 62`) | **No** | **FAIL**: Dead env var |
| `SERPER_API_KEY` | No | Yes (`line 63`) | **No** | **FAIL**: Dead env var |

- **Reproduction Steps**:
  1. Inspect `EnvVarMatrix` in [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json).
  2. Read [`README.md:80-87`](file:///C:/dev/heyclicky-clone/README.md#L80-L87), [`src/backend/.env.example`](file:///C:/dev/heyclicky-clone/src/backend/.env.example), and [`src/backend/src/routes/brain.ts:598-599`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L598-L599).
- **Observed vs Expected Result**:
  - **Observed**: Setting `ONEBRAIN_MODEL` in `.env` (per `README.md:84`) or `KURSOR_DEV_BYPASS_AUTH=false` (per `.env.example:7`) does nothing.
  - **Expected**: `README.md`, `.env.example`, and backend code must agree on all environment variable names and honor `KURSOR_DEV_BYPASS_AUTH`.
- **Evidence**: [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)
- **Remediation**: Read `process.env.KURSOR_BRAIN_MODEL || process.env.ONEBRAIN_MODEL || process.env.OPENROUTER_DEFAULT_MODEL` in `brain.ts`, add `KURSOR_BRAIN_MODEL` to `README.md` and `.env.example`, enforce `KURSOR_DEV_BYPASS_AUTH` in `middleware/auth.ts`, and remove or wire the 5 unused API/provider keys in `.env.example`.

---

### `ID: AUD-N-002`
- **Severity**: `MEDIUM`
- **Category**: `WS-N — README Accuracy (Auth & Drawing Primitives)`
- **Title**: `README.md` falsely claims `setup-kursor.cmd` writes `session.json` (actual: DPAPI `auth.localdev.bin` / `auth.bin`) and documents non-existent `writeOnPage` drawing primitive
- **File:Line**: [`README.md:20-21,133`](file:///C:/dev/heyclicky-clone/README.md#L20-L21), [`scripts/seed-local-auth.ps1:83-88`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L83-L88), [`src/backend/src/routes/brain.ts:106`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L106)
- **Description**:
  1. [`README.md:133`](file:///C:/dev/heyclicky-clone/README.md#L133) claims `setup-kursor.cmd` "Configures `%LOCALAPPDATA%\HeyClicky\endpoints.json` & `session.json`". In reality, [`scripts/seed-local-auth.ps1:83-88`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L83-L88) writes DPAPI-encrypted `auth.localdev.bin` and `auth.bin` (never `session.json`).
  2. [`README.md:20-21`](file:///C:/dev/heyclicky-clone/README.md#L20-L21) claims `openPage` + `writeOnPage` spawn "draggable, expandable on-screen study cards with markdown headings, bullet lists, and explanations". In reality, `writeOnPage` does not exist anywhere in `Assets/Engine/*.js` or `brain.ts`, and `openPage("https://...")` ([`brain.ts:106`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L106)) opens a web URL in the browser; floating study cards are drawn with `receipt()`, `clipping()`, `paper()`, and `sticky()`.
- **Reproduction Steps**:
  1. Read [`README.md:20-21,133`](file:///C:/dev/heyclicky-clone/README.md#L20-L21).
  2. Read [`scripts/seed-local-auth.ps1:75-92`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L75-L92) and [`src/backend/src/routes/brain.ts:75-110`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L75-L110).
- **Observed vs Expected Result**:
  - **Observed**: `writeOnPage` does not exist in code; `seed-local-auth.ps1` writes `auth.localdev.bin` and `auth.bin`.
  - **Expected**: Accurate documentation of floating card functions (`receipt`, `clipping`, `paper`, `sticky`) and DPAPI auth files (`auth.localdev.bin`, `auth.bin`).
- **Evidence**: [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)
- **Remediation**: Update [`README.md:20-21`](file:///C:/dev/heyclicky-clone/README.md#L20-L21) and [`README.md:133`](file:///C:/dev/heyclicky-clone/README.md#L133).

---

### `ID: AUD-N-003`
- **Severity**: `LOW`
- **Category**: `WS-N — Documentation Metrics & File Count Drift`
- **Title**: Inaccurate C# file counts across `README.md` (`2,102`), `AGENTS.md` (`2,098`), `docs/INDEX.md`, `docs/06`, and `docs/07` vs actual repository file counts (`2,096` and `2,097`)
- **File:Line**: [`README.md:3`](file:///C:/dev/heyclicky-clone/README.md#L3), [`AGENTS.md:14`](file:///C:/dev/heyclicky-clone/AGENTS.md#L14), [`docs/INDEX.md:24-34,55-62`](file:///C:/dev/heyclicky-clone/docs/INDEX.md#L24-L34), [`docs/06-agents-proactive-and-memory.md:6`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md#L6), [`docs/07-kursor-roadmap.md:31,100`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L31)
- **Description**: Ground-truth `.cs` file counts ([`docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)) vs documented claims:

| Project | `reference/decompiled/` Actual `.cs` | `docs/INDEX.md` Claim (Sec 2) | `src/desktop/` Actual `.cs` | `docs/INDEX.md` Claim (Sec 3B) | Reason for Discrepancy |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `Contracts` | `55` | `56` | `55` | `56` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `Drawing3D` | `41` | `42` | `41` | `42` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `Drawing` | `85` | `86` | `85` | `86` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `Platform` | `345` | `346` | `345` | `346` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `Core` | `564` | `565` | `564` | `565` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `Agents` | `122` | `123` | `122` | `123` | `.csproj` file was counted as a `.cs` file (`+1`) |
| `App` | `884` | `880` | `884` | `884` | Sec 2 undercounted `HeyClicky.App` by 4 files |
| `src/desktop/` root | — | — | `1` (`PrivateImplementationDetailsPolyfill.cs`) | — | Root polyfill `.cs` file |
| **Total `.cs` Files** | **2,096** | **2,098** | **2,097** | **2,102** | Off by `+2` (`reference/decompiled`) and `+5` (`src/desktop`) |

- **Reproduction Steps**:
  1. Inspect `FileCountTable`, `RefDecompiledTotalCs` (`2096`), and `SrcDesktopTotalCs` (`2097`) in [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json).
- **Observed vs Expected Result**:
  - **Observed**: `reference/decompiled/` has `2,096` `.cs` files and `src/desktop/` has `2,097` `.cs` files.
  - **Expected**: Documentation tables in `README.md`, `AGENTS.md`, `docs/INDEX.md`, `docs/06`, and `docs/07` should match actual `.cs` counts.
- **Evidence**: [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)
- **Remediation**: Update the C# file count tables in `README.md`, `AGENTS.md`, `docs/INDEX.md`, `docs/06`, and `docs/07`.

---

### `ID: AUD-N-004`
- **Severity**: `MEDIUM`
- **Category**: `WS-N — Broken Documentation Links`
- **Title**: 17 broken `file:///` source links in `docs/06-agents-proactive-and-memory.md` and wrong `TrayTooltip.cs` path in `docs/07-kursor-roadmap.md`
- **File:Line**: [`docs/06-agents-proactive-and-memory.md:27-35,445-580`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md#L27-L35), [`docs/07-kursor-roadmap.md:119`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L119)
- **Description**: Automated link validation of all `154` `file:///C:/dev/heyclicky-clone/...` links across `README.md`, `AGENTS.md`, and `docs/*.md` ([`docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)) identified **17 broken links** in [`docs/06-agents-proactive-and-memory.md`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md#L27-L35) pointing to non-existent namespace subdirectories (`HeyClicky.Agents.Codex`, `HeyClicky.Agents.Worker`, `HeyClicky.Agents.Prompt`, `HeyClicky.Agents.Clickys`, `HeyClicky.Agents.Integrations`, `HeyClicky.Core.Services/WorkerClient.cs`, `HeyClicky.Core.Connectors/McpOAuth.cs`). In addition, [`docs/07-kursor-roadmap.md:119`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L119) states that `TrayTooltip.cs` is in `Kursor.App`, whereas it actually resides in [`src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Notch/TrayTooltip.cs).
- **Reproduction Steps**:
  1. Inspect `BrokenLinks` (17 entries) in [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json).
  2. Read [`docs/06-agents-proactive-and-memory.md:27-35`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md#L27-L35) and [`docs/07-kursor-roadmap.md:119`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L119).
- **Observed vs Expected Result**:
  - **Observed**: 17 `file:///` links in `docs/06-agents-proactive-and-memory.md` fail `Test-Path`.
  - **Expected**: 0 broken `file:///` links across all repository documentation.
- **Evidence**: [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)
- **Remediation**: Replace the 17 broken paths in `docs/06-agents-proactive-and-memory.md` with the actual directories (`HeyClicky.Agents.Runtime`, `HeyClicky.Agents.Backend`, `HeyClicky.Agents.Jobs`, `HeyClicky.Core.Worker/WorkerClient.cs`, `HeyClicky.Core.Connectors/McpOAuthClient.cs`) and fix the project name for `TrayTooltip.cs` in `docs/07-kursor-roadmap.md:119`.

---

### `ID: AUD-N-005`
- **Severity**: `MEDIUM`
- **Category**: `WS-N — Architecture Inventory & Roadmap Status Falsification`
- **Title**: `docs/01-architecture-overview.md` cites wrong paths/byte sizes and non-existent assets, while `docs/07-kursor-roadmap.md` marks unimplemented rebranding & packaging items as `COMPLETED`
- **File:Line**: [`docs/01-architecture-overview.md:155-177`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L155-L177), [`docs/07-kursor-roadmap.md:31-32,98-132`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L31-L32)
- **Description**:
  1. [`docs/01-architecture-overview.md:155-165`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L155-L165) places the 5 V8 `.js` files under `BundledResources\` with wrong byte sizes (`781,132 B`, `64,849 B`, `110,937 B`, `84,965 B`, `23,372 B`) and lists a non-existent `BundledResources\ClickyBrowserExtensionID.txt` (`34 B`). In reality, the 5 `.js` files are in `Assets\Engine\` with sizes `429,525 B`, `41,390 B`, `259,913 B`, `66,086 B` (`66,985 B` patched), and `50,405 B`.
  2. [`docs/01-architecture-overview.md:171-177`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L171-L177) lists non-existent asset directories (`Assets\Clicky\*.png`, `Assets\Squad\*.png`, `Assets\StarterCards\*.png`, `Assets\ToolkitLogos\*.png`) and fonts (`PlusJakartaSans-*.ttf`). Actual `Assets\` subdirectories ([`docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json)) are `Audio` (1), `Earcons` (17), `Engine` (5), `Fonts` (4 `ClickyDraw-Barlow-*` and `ClickyDraw-PatrickHand.ttf`), `Logos` (34), `Referral` (3), `Sounds` (63), `Video` (3), and `VoicePreviews` (40).
  3. [`docs/07-kursor-roadmap.md:31-32,98-132`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L31-L32) marks Phases 4–7 as `✅ COMPLETED`, claiming that `HeyClicky.*` namespaces were rebranded to `Kursor.*` (line 103), `heyclicky://` was replaced with `kursor://` (line 130), `.kursor` file association was registered (line 130), and `%LOCALAPPDATA%\HeyClicky\` was migrated to `%LOCALAPPDATA%\Kursor\` (line 131) — none of which were actually implemented in the C# codebase.
- **Reproduction Steps**:
  1. Compare [`docs/01-architecture-overview.md:155-177`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L155-L177) with [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json) and [`docs/audit/evidence/ws-ab/js-assets-comparison.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/js-assets-comparison.json).
  2. Compare [`docs/07-kursor-roadmap.md:31-32,98-132`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md#L31-L32) with [`docs/audit/evidence/ws-ab/branding-scan.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/branding-scan.json).
- **Observed vs Expected Result**:
  - **Observed**: `docs/01` lists non-existent asset folders and wrong JS sizes; `docs/07` marks unimplemented rebranding/protocol/AppData work as `✅ COMPLETED`.
  - **Expected**: Accurate asset inventory in `docs/01` and truthful phase status tracking in `docs/07`.
- **Evidence**: [`docs/audit/evidence/ws-ab/docs-audit.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/docs-audit.json), [`docs/audit/evidence/ws-ab/js-assets-comparison.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/js-assets-comparison.json)
- **Remediation**: Update `docs/01-architecture-overview.md:155-177` with the verified `Assets\` inventory and byte sizes, and update `docs/07-kursor-roadmap.md` to reflect which Phase 4–7 tasks remain pending.
