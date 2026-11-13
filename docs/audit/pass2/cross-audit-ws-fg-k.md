# Pass 2 Independent Cross-Audit Report — WS-F, WS-G & WS-K

- **Pass 2 Auditor**: `ws-ab-build-brand`
- **Date**: 2026-11-13
- **Reviewed Workstreams**:
  - **WS-F**: 2D Drawing Engine, ClearScript V8 & Overlays ([`docs/audit/ws-fg/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/REPORT.md), [`docs/audit/ws-fg/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/findings.json))
  - **WS-G**: 3D Procedural Engine, Shaders & GPU Pipeline ([`docs/audit/ws-fg/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/REPORT.md), [`docs/audit/ws-fg/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/findings.json))
  - **WS-K**: UI Surfaces, Settings, Ink, Tray & App Shell ([`docs/audit/ws-jk/ws-k-report.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/ws-k-report.md), [`docs/audit/ws-jk/ws-k-findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/ws-k-findings.json))
- **Machine-Readable Output**: [`docs/audit/pass2/cross-audit-ws-fg-k.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-fg-k.json)

---

## 1. Executive Summary

| Metric | WS-F (2D Drawing) | WS-G (3D Engine) | WS-K (UI Surfaces) | Total |
| :--- | :---: | :---: | :---: | :---: |
| **Pass 1 Findings Reviewed** | 8 (`AUD-F-001`..`008`) | 4 (`AUD-G-001`..`004`) | 10 (`AUD-K-001`..`010`) | **22** |
| **Confirmed (`CONFIRMED`)** | 8 | 4 | 10 | **22** |
| **Disputed (`DISPUTED`)** | 0 | 0 | 0 | **0** |
| **Precision Corrections Added** | 0 | 1 (`AUD-G-003`) | 1 (`AUD-K-003`) | **2** |
| **New Findings Discovered in Pass 2** | 2 (`NEW-FG-001`, `NEW-FG-003`) | 1 (`NEW-FG-002`) | 1 (`NEW-K-001`) | **4** |

### Key Conclusions
1. **All 22 Pass 1 findings across WS-F, WS-G, and WS-K are `CONFIRMED`** by direct C#/JS source inspection and independent re-execution of the V8 2D/3D test harnesses and UI surface audit evidence scripts.
2. **All 3 High-severity findings (`AUD-F-003`, `AUD-F-005`, `AUD-K-002`) were independently re-verified line-by-line**:
   - `AUD-F-003`: [`brain.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L85) instructs the model to pass `around: { count: 10, axis: "x"|"y"|"z", at: [0,0,0] }`, whereas [`clicky-draw-engine.js:720`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L720) reads `const center = v3(around.center, [0, 0, 0])` and ignores `around.at`.
   - `AUD-F-005`: [`SystemComposition.cs:165-180`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs#L165-L180) calls `Marshal.ThrowExceptionForHR` directly on `EndDraw`/`Present`/`Commit` with zero `DXGI_ERROR_DEVICE_REMOVED` (`0x887A0005`) / `D2DERR_RECREATE_TARGET` (`0x8899000C`) recovery across `SystemComposition`, `ScreenPainter`, or `StageSwapChain`.
   - `AUD-K-002`: [`Pressable.cs:18,181-242`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.UI/Pressable.cs#L181-L242) inherits from `Grid` and wires only pointer events (`PointerEntered`, `PointerExited`, `PointerPressed`, `PointerReleased`, `PointerCaptureLost`), never setting `IsTabStop = true` or `UseSystemFocusVisuals = true` and omitting any `KeyDown` handler for `Enter`/`Space`.
3. **4 New Findings (`1 HIGH`, `2 MEDIUM`, `1 LOW`) were discovered during Pass 2**:
   - **`NEW-FG-001` (`HIGH`)**: [`NativeEngine.cs:145-174,284`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L145-L174) executes untrusted LLM-generated ClickyDraw JavaScript synchronously inside an in-process ClearScript `V8ScriptEngine` on the single STA `DrawingThread` **without** the `1500 ms` Web Worker `WorkerRunner` watchdog (`clicky-draw-engine.js:1149-1187`) and without any timer calling `engine.Interrupt()`. Any infinite or long loop in an LLM drawing script permanently hangs `DrawingThread`.
   - **`NEW-FG-002` (`MEDIUM`)**: [`ClickyThreeKernel.cs:67-79,96-106,179-189,244-274,290-349`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L244-L274) has a thread-safety race on `engine` disposal vs `HeapBytes`/`WarmUp()`, runs `V8ScriptEngine` without heap or execution-time limits, and in `ReleaseCachedGpuBuffers()` disposes cached `Mesh3D.Gpu` (`ID3D11Buffer`) instances **before** checking `if (ThreeStage.LiveCounts.Stages == 0)` (`line 269`), racing with active `ThreeStage` render threads.
   - **`NEW-FG-003` (`LOW`)**: [`PhotoLoader.cs:524-526,557-589`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L557-L589) SSRF filter `IsPublic(IPAddress)` fails to block IPv6 transition addresses that embed loopback/private IPv4 targets (NAT64 `64:ff9b::/96`, 6to4 `2002::/16`, Teredo `2001::/32`), `IPAddress.IPv6Any` (`::`), and multicast (`ff00::/8`), and contains unreachable decompiler code at `IL_042f` (`lines 524-526`).
   - **`NEW-K-001` (`MEDIUM`)**: [`SettingsContent.cs:4247,4252-4260`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L4247-L4260) contains a decompiler-damaged iterator `DemoAgentMomentRows()` (`//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()`) returning raw state machine `_003CDemoAgentMomentRows_003Ed__65`, plus an unbranded `"Relaunch HeyClicky"` action row on line 4247.

---

## 2. Independent Test Harness Re-Execution

| Harness | Command | Exit Code | Independent Verification Result |
| :--- | :--- | :---: | :--- |
| **WS-F 2D Engine Harness** | `node tests/audit/ws-fg/test-2d-engine.mjs` | `0` | **Verified**: 57 programs executed against `clicky-draw-pieces.js` + `clicky-draw-engine.js` (`54 PASS`, `3 EXPECTED_FAIL_CONFIRMED` reproducing `AUD-F-001`, `AUD-F-002`, and syntax error recovery, `0 UNEXPECTED`). |
| **WS-G 3D Engine Harness** | `node tests/audit/ws-fg/test-3d-engine.mjs` | `0` | **Verified**: `clicky-three.js` + `clicky-three-parts.js` built `17/17` normal shapes, `6/6` modifiers (`stretch`, `bend`, `taper`, `twist`, `chamfer`, `fillet`), and `17/17` extreme parameter sets without NaN/Infinity; confirmed `coneTopParameterIgnoredByteIdentical: true` (`AUD-G-001`). |
| **WS-K UI Surfaces Harness** | [`tests/audit/ws-jk/test-ui-surfaces.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-ui-surfaces.ps1) | `0` | **Verified**: Inspected script & generated artifacts in [`docs/audit/evidence/ws-jk/`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/) confirming DPAPI round-trip (`HeyClicky.Connectors.v1` and `HeyClicky.Auth.v1`), 886-file / 33-namespace inventory, 10 `async void` methods, and 15 `heyclicky.com` references. |

---

## 3. Finding-by-Finding Cross-Audit Verdicts (`AUD-F-*`, `AUD-G-*`, `AUD-K-*`)

| ID | WS | Severity | Verdict | Verified `file:line` & Cross-Audit Notes |
| :--- | :---: | :---: | :---: | :--- |
| `AUD-F-001` | WS-F | MEDIUM | **CONFIRMED** | [`clicky-draw-pieces.js:1-1410`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js#L1-L120), [`clicky-draw-engine.js:1221-1255`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L1221-L1255). `writeOnPage`, `callout`, `brace`, `table`, and `chart` do not exist in the 2D engine; `openPage(url)` opens a browser URL. |
| `AUD-F-002` | WS-F | MEDIUM | **CONFIRMED** | [`brain.ts:94`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L94) vs [`clicky-draw-pieces.js:61-74`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js#L61-L74). Prompt teaches `spot`, engine only defines `point(target, opts)`. |
| `AUD-F-003` | WS-F | **HIGH** | **CONFIRMED** | [`brain.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L85) vs [`clicky-draw-engine.js:718-724`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L718-L724). `expandFigurePart()` reads `around.center` and ignores `around.at`. |
| `AUD-F-004` | WS-F | MEDIUM | **CONFIRMED** | [`brain.ts:88`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L88) vs [`clicky-draw-engine.js:561-562,670-689`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L561-L562). `FIGURE_MATERIALS` has `'iron'` and `'black'`, not `'cast'` or `'carbon'`. |
| `AUD-F-005` | WS-F | **HIGH** | **CONFIRMED** | [`SystemComposition.cs:99-188`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs#L99-L188), [`ScreenPainter.cs:45-48`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/ScreenPainter.cs#L45-L48), [`StageSwapChain.cs:147-159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Figures/StageSwapChain.cs#L147-L159). Unhandled `HRESULT` exceptions on GPU reset/TDR. |
| `AUD-F-006` | WS-F | MEDIUM | **CONFIRMED** | [`OverlayWindow.cs:220-226,338-407,462-466`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/OverlayWindow.cs#L220-L226). `scale` is cached once and `WM_DPICHANGED` (`0x02E0`) is not handled. |
| `AUD-F-007` | WS-F | LOW | **CONFIRMED** | [`OverlayWindow.cs:91-97,111-125`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/OverlayWindow.cs#L91-L97). `ShowInScreenRecordings` setter fires and forgets `ApplyCapturePolicyToAllAsync()`. |
| `AUD-F-008` | WS-F | LOW | **CONFIRMED** | [`Mp4VideoWriter.cs:41-60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Video/Mp4VideoWriter.cs#L41-L60). Missing even-dimension validation for NV12/H.264 and 32-bit `int` multiplication `width * height * 6`. |
| `AUD-G-001` | WS-G | MEDIUM | **CONFIRMED** | [`brain.ts:74`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L74) vs [`clicky-three-parts.js:373-377`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-three-parts.js#L373-L377). `SHAPES.cone(o)` ignores `o.top`. |
| `AUD-G-002` | WS-G | MEDIUM | **CONFIRMED** | [`ThreeStage.cs:1089-1126`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ThreeStage.cs#L1089-L1126). `Dispose()` calls `Release()` even when `running.Join(5000)` times out. |
| `AUD-G-003` | WS-G | LOW | **CONFIRMED** *(with precision note)* | [`ClickyThreeKernel.cs:18,30,226-238,311`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L226-L238). `failed` (`HashSet<string>`) is unbounded and never cleared, and `loadFailed = true` is permanent. **Precision correction**: `shapes` is bounded by byte budget `CacheByteBudget = 50331648L` (48 MB, line 18/226), not a 256-item count limit. |
| `AUD-G-004` | WS-G | MEDIUM | **CONFIRMED** | [`Pipelines.cs:145-165`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Pipelines.cs#L145-L165), [`GpuDevice.cs:19-45`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/GpuDevice.cs#L19-L45). `buildFailed = true` is permanent and `GpuDevice.Shared` has no device-loss reset path. |
| `AUD-K-001` | WS-K | MEDIUM | **CONFIRMED** | [`SettingsContent.cs:6142-6172,6940-6989`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L6142-L6172). `BuildReferralPage()` never calls `FriendCodeGroup()` (defined at line 6940). |
| `AUD-K-002` | WS-K | **HIGH** | **CONFIRMED** | [`Pressable.cs:18,181-242`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.UI/Pressable.cs#L181-L242). `Pressable : Grid` has zero keyboard focus (`IsTabStop`) or `KeyDown` (`Enter`/`Space`) handling. |
| `AUD-K-003` | WS-K | MEDIUM | **CONFIRMED** *(with precision note)* | [`DeveloperFlags.cs:17-42`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/DeveloperFlags.cs#L17-L42), [`SettingsContent.cs:8771`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L8771). `FullAgentActivity` getter always returns `false` while setter writes `developer-flags.json`, leaving `DeveloperFlags.Read()` dead code. **Precision note**: `SettingsContent.cs:4195-4203` is `ProactiveDeveloperGroups()` and `"Show agent names"`, not a `FullAgentActivity` toggle. |
| `AUD-K-004` | WS-K | MEDIUM | **CONFIRMED** | [`BugReport.cs:20-21,75,91-168`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/BugReport.cs#L75-L168). Opens Gmail compose to `hi@heyclicky.com` and uploads 3 MB of logs + state JSON to Sentry. |
| `AUD-K-005` | WS-K | LOW | **CONFIRMED** | [`SettingsContent.cs:1564-1567,6042-6052`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L1564-L1567), [`OnboardingView.cs:1451-1457`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Onboarding/OnboardingView.cs#L1451-L1457), [`KbdChip.cs:13`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.UI/KbdChip.cs#L13). macOS `mac_step` and `\u2318` holdovers confirmed. |
| `AUD-K-006` | WS-K | LOW | **CONFIRMED** | [`CustomConnectorStore.cs:25-33`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/CustomConnectorStore.cs#L25-L33) vs [`docs/01-architecture-overview.md:191`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md#L191). Code uses `connectors.json` + `connectors.secrets`, whereas doc states `custom-connectors.json`. |
| `AUD-K-007` | WS-K | MEDIUM | **CONFIRMED** | [`JsonAppSettings.cs:370-427`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Services/JsonAppSettings.cs#L370-L427). Non-atomic `File.WriteAllText` in `Save()` and silent catch on `IOException`. |
| `AUD-K-008` | WS-K | MEDIUM | **CONFIRMED** | [`ClickyDrawingWindowViewer.cs:28-55`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Notch.Peek/ClickyDrawingWindowViewer.cs#L28-L55), [`AppHost.cs:2003,2190,2385,6432`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/AppHost.cs#L2003). `Present()` is `async void` without a `try/catch` around `await service.LoadCodeAsync(stored.Id)`. |
| `AUD-K-009` | WS-K | LOW | **CONFIRMED** | [`SpreadsheetDocumentView.cs:245,863-918`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Ink.Views/SpreadsheetDocumentView.cs#L863-L918). Fixed 28×8 grid with no row/column expansion UI. |
| `AUD-K-010` | WS-K | LOW | **CONFIRMED** | [`AppHost.cs:3819`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App/AppHost.cs#L3819), [`BillingUi.cs:146,503`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Billing/BillingUi.cs#L146), [`ChromeAutoInstall.cs:103`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Browser/ChromeAutoInstall.cs#L103). 15 upstream URLs/emails confirmed. |

---

## 4. Newly Discovered Findings in Pass 2 (`NEW-FG-001`..`003`, `NEW-K-001`)

### `ID: NEW-FG-001` — In-Process ClearScript V8 `NativeEngine` Runs LLM-Generated JavaScript Synchronously on `DrawingThread` Without the Web Worker Sandbox or Any Execution Timeout / `Interrupt()` Watchdog
- **Severity**: `HIGH` (`P1`)
- **Workstream**: `WS-F` (`Kursor.Drawing`)
- **Component**: [`NativeEngine.cs:145-174,280-291`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L145-L174), [`DrawingThread.cs:102-125`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/DrawingThread.cs#L102-L125), [`clicky-draw-engine.js:1129-1187`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L1129-L1187)
- **Description**:
  In [`clicky-draw-engine.js:1149-1187`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L1149-L1187), `WorkerRunner` was designed to run LLM-generated drawing scripts inside a browser Web Worker with a `1500 ms` watchdog timer (`setTimeout(() => { w.terminate(); ... }, limit)`). However, in the desktop app, [`NativeEngine.cs:145-174`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L145-L174) loads `clicky-draw-native.js` + `clicky-draw-engine.js` directly into an in-process ClearScript `V8ScriptEngine` on the single STA `DrawingThread` and calls `value.Invoke(asConstructor: false, arguments)` ([`line 284`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L284)) synchronously on `DrawingThread` with **no `V8RuntimeConstraints` heap cap and no watchdog timer calling `engine.Interrupt()`**.
- **Reproduction Steps**:
  1. Inspect [`NativeEngine.cs:145-174`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L145-L174) and [`NativeEngine.cs:280-291`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L280-L291).
  2. Observe that `new V8ScriptEngine(V8ScriptEngineFlags.DisableGlobalMembers)` is constructed with no `V8RuntimeConstraints`, and `Invoke(string name, params object[] arguments)` executes synchronously on `DrawingThread` with no timer calling `engine.Interrupt()`.
- **Observed vs. Expected**:
  - **Observed**: Any LLM-generated drawing program or `tick()` callback that contains an accidental infinite `while`/`for` loop permanently blocks `DrawingThread`, freezing all screen overlays, live captions, and cursor animations for the rest of the process lifetime.
  - **Expected**: `NativeEngine` should arm a watchdog timer (e.g., `1500 ms` for `Run`, `100 ms` for `RenderFrame`) that calls `engine.Interrupt()` if script execution hangs, and set `V8RuntimeConstraints` heap limits.

---

### `ID: NEW-FG-002` — `ClickyThreeKernel` Thread-Safety Race on `V8ScriptEngine` Disposal, Missing V8 Heap/Execution Constraints, and Unconditional Disposal of Active Stage GPU Buffers on Idle Timeout
- **Severity**: `MEDIUM` (`P2`)
- **Workstream**: `WS-G` (`Kursor.Drawing3D`)
- **Component**: [`ClickyThreeKernel.cs:67-79,96-106,179-189,244-274,290-311,349`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L244-L274)
- **Description**:
  `ClickyThreeKernel` has three lifecycle and concurrency bugs:
  1. **Unsynchronized `engine` access vs disposal**: `WarmUp()` ([`lines 98-105`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L98-L105)) reads `if (engine == null && !loadFailed)` under `lock (gate)`, and `HeapBytes` ([`line 73`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L73)) calls `engine?.GetRuntimeHeapInfo().TotalHeapSize` with no lock at all, while `Run()` ([`lines 181-186`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L181-L186)) disposes `engine` (`engine.Dispose(); engine = null;`) and `LoadIfNeeded()` ([`lines 290-311`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L290-L311)) assigns `engine` and `loadFailed` on the worker thread **without acquiring `lock (gate)`**.
  2. **Missing V8 constraints**: `LoadIfNeeded()` ([`line 290`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L290)) constructs `new V8ScriptEngine("Clicky 3D kernel", V8ScriptEngineFlags.DisableGlobalMembers)` with no `V8RuntimeConstraints` and no `Interrupt()` timeout around `buildFunction.Invoke(false, kind, recipe)` ([`line 349`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L349)).
  3. **Premature disposal of `Mesh3D.Gpu` while `ThreeStage` is still rendering**: When `work.TryTake(out var item, IdleRelease)` times out after 5 minutes with no *new* shape builds, `ReleaseCachedGpuBuffers()` ([`lines 244-274`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L244-L274)) iterates through `shapes.Values` and disposes `item.Gpu` (`ID3D11Buffer` vertex/index buffers) and sets `item.Gpu = null` ([`lines 255-267`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L255-L267)) **before** checking `if (ThreeStage.LiveCounts.Stages == 0)` at [`line 269`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L269), and without holding `GpuDevice.Shared.ContextLock`. If a 3D stage remains open for 5 minutes without building new shapes, the kernel thread disposes its live `ID3D11Buffer` instances concurrently with `FigureRenderer.Render` on the stage's `frameThread`.
- **Reproduction Steps**:
  1. Inspect [`ClickyThreeKernel.cs:244-274`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L244-L274).
  2. Note that `item.Gpu?.Dispose()` runs unconditionally for all cached `Mesh3D` entries prior to `if (ThreeStage.LiveCounts.Stages == 0)` at line 269.
- **Observed vs. Expected**:
  - **Observed**: `ReleaseCachedGpuBuffers()` disposes `Mesh3D.Gpu` buffers while `ThreeStage.LiveCounts.Stages > 0` without holding `GpuDevice.Shared.ContextLock`, and `engine` is mutated/disposed outside `lock (gate)`.
  - **Expected**: `if (ThreeStage.LiveCounts.Stages > 0) return;` should guard the entire `ReleaseCachedGpuBuffers()` method (and buffer disposal should synchronize with `GpuDevice.Shared.ContextLock`), and `engine` lifecycle transitions should be synchronized.

---

### `ID: NEW-FG-003` — `PhotoLoader.IsPublic` IPv6 SSRF Filter Bypass (Missing NAT64 `64:ff9b::/96`, 6to4 `2002::/16`, Teredo `2001::/32`, `::`, and Multicast) and Unreachable Decompiler Code
- **Severity**: `LOW` (`P3`)
- **Workstream**: `WS-F` (`Kursor.Drawing`)
- **Component**: [`PhotoLoader.cs:524-526,557-589`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L524-L589)
- **Description**:
  `PhotoLoader.FetchByHttpAsync` resolves hostnames and checks `PhotoLoader.IsPublic(IPAddress)` ([`lines 557-589`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L557-L589)) to prevent LLM-generated `photo({ url })` calls from probing loopback or private RFC1918 networks. However, for `AddressFamily.InterNetworkV6` (`lines 575-586`), `IsPublic` only checks `IPAddress.IsLoopback`, `IsIPv6LinkLocal`, `IsIPv6SiteLocal`, `IsIPv4MappedToIPv6`, and ULA `fc00::/7` (`(bytes[0] & 0xFE) == 252`). It returns `true` for `IPAddress.IPv6Any` (`::`), `IsIPv6Multicast` (`ff00::/8`), and IPv6 transition mechanisms that embed private/loopback IPv4 addresses: NAT64 (`64:ff9b::/96`), 6to4 (`2002::/16`), and Teredo (`2001:0000::/32`). Additionally, [`lines 524-526`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L524-L526) contain unreachable decompiler code (`IL_042f: byte[] result3; return result3;`).
- **Reproduction Steps**:
  1. Inspect [`PhotoLoader.cs:524-526`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L524-L526) and [`PhotoLoader.cs:557-589`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/PhotoLoader.cs#L557-L589).
  2. Trace `IsPublic(IPAddress.Parse("64:ff9b::7f00:1"))` or `IsPublic(IPAddress.IPv6Any)` — both return `true`.
- **Observed vs. Expected**:
  - **Observed**: Non-public IPv6 transition, unspecified (`::`), and multicast addresses pass `IsPublic()`, and dead label `IL_042f` remains at lines 524–526.
  - **Expected**: `IsPublic()` should reject `IPAddress.IPv6Any`, `IsIPv6Multicast`, NAT64 (`64:ff9b::/96`), 6to4 (`2002::/16`), and Teredo (`2001::/32`), and dead code at lines 524–526 should be removed.

---

### `ID: NEW-K-001` — Decompiler-Damaged Iterator `DemoAgentMomentRows()` (`_003CDemoAgentMomentRows_003Ed__65`) and Unbranded `"Relaunch HeyClicky"` Action on Developer Settings Page
- **Severity**: `MEDIUM` (`P2`)
- **Workstream**: `WS-K` (`Kursor.App`)
- **Component**: [`SettingsContent.cs:4247,4252-4260`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L4247-L4260)
- **Description**:
  In [`SettingsContent.cs:4252-4260`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L4252-L4260), the iterator method `DemoAgentMomentRows()` failed ILSpy `yield return` decompilation (`//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()`) and instantiates the raw compiler-generated state machine `_003CDemoAgentMomentRows_003Ed__65`. Immediately above it at [`line 4247`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L4247), `BuildDeveloperPage()` renders `SK.ActionRow("Relaunch HeyClicky", "Quits and opens HeyClicky again, e.g. after switching the JIT.", actions.Relaunch, null, null)` with hardcoded upstream `"HeyClicky"` branding.
- **Reproduction Steps**:
  1. Read [`SettingsContent.cs:4245-4260`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs#L4245-L4260).
- **Observed vs. Expected**:
  - **Observed**: Raw compiler-generated state machine `_003CDemoAgentMomentRows_003Ed__65` and `"Relaunch HeyClicky"` string are present in `SettingsContent.cs`.
  - **Expected**: Reconstruct `DemoAgentMomentRows()` as a clean `yield return` iterator and rename `"Relaunch HeyClicky"` to `"Relaunch Kursor"`.
