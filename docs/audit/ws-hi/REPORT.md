# WS-H & WS-I Full-Spectrum Audit Report (`Kursor.Platform` & `Kursor.Core`)

- **Owner:** `ws-hi-platform-core`
- **Scope:**
  - **WS-H (`src/desktop/Kursor.Platform/`):** 346 C# source files (`HeyClicky.Platform.dll`)
  - **WS-I (`src/desktop/Kursor.Core/`):** 565 C# source files (`HeyClicky.Core.dll`)
- **Audit Status:** `FAIL` (15 verified findings: 5 `High`, 8 `Medium`, 2 `Low`)
- **Evidence & Harnesses:**
  - [`docs/audit/ws-hi/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/findings.json)
  - [`docs/audit/ws-hi/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/coverage.json)
  - [`docs/audit/evidence/ws-hi/thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt)
  - [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt)
  - [`tests/audit/ws-hi/WsHiAuditRunner.csproj`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/WsHiAuditRunner.csproj) & [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs)

---

## 1. Namespace & Class Inventory

### 1.1 WS-H: `src/desktop/Kursor.Platform/` (346 C# Files)

| Namespace | File Count | Key Classes & Responsibilities |
|---|---:|---|
| `HeyClicky.Platform.Audio` | 21 | `WasapiMicrophoneCapture`, `WasapiAudioOutput`, `VoiceRenderStream`, `WasapiRenderStream`, `EarconPlayer`, `AudioTopologyMonitor`, `AcousticEchoCanceller`, `AudioDeviceInfo`, `AudioDeviceSelection`, `WasapiComGuids`, `IMMEndpoint`, `IMMDevice`, `IMMDeviceCollection`, `IMMDeviceEnumerator`, `IMMNotificationClient`, `IAudioClient`, `IAudioCaptureClient`, `IAudioRenderClient`, `IPropertyStore`, `PropVariant`, `PropertyKey` |
| `HeyClicky.Platform.Capture` | 7 | `ScreenCaptureService`, `DesktopDuplicationSource`, `GraphicsCaptureSource`, `CaptureGpu`, `CaptureGpuMonitor`, `CapturedMonitorImage`, `PngEncoder` |
| `HeyClicky.Platform.Input` | 24 | `LowLevelInputHub`, `ShortcutMonitor`, `ClickyScreenActuator`, `ClickyTakeBackWatch`, `ClickyUserInputBlocker`, `ClickyDriveHotkeyWatch`, `ClickyCursorTeleport`, `CursorLocator`, `GlobalKeyListener`, `GlobalMouseWheelListener`, `KeyboardChordCapture`, `KeyRepeatGate`, `ShortcutAction`, `ShortcutActivationMode`, `ShortcutBinding`, `ShortcutSettings`, `HotkeyModifiers`, `MouseButtonKind`, `InputEvent`, etc. |
| `HeyClicky.Platform.Overlay` | 26 | `OverlayHost`, `FramePacer`, `CursorOverlay`, `CursorLayer`, `PenRideTrajectory`, `CaptionCardLayer`, `ClickyDriveBadgeLayer`, `ClickyEdgeGlowLayer`, `ClickyToastLayer`, `ScreenCurtainLayer`, `SpotlightLayer`, `SvgPathBuilder`, `OverlayColors`, `OverlayGeometry`, `IOverlayLayer`, `IDCompositionDesktopDevice`, `IDCompositionDevice2`, `IDCompositionSurface`, `IDCompositionTarget`, `IDCompositionVisual2`, etc. |
| `HeyClicky.Platform.Permissions` | 1 | `PermissionService` (Windows microphone/screen-capture consent store checks, Settings URI launcher) |
| `HeyClicky.Platform.Win32` | 23 | `CaptureExclusion`, `ScreenCaptureYield`, `DisplayEnumerator`, `UIAutomationHelpers`, `UiaPageText`, `ForegroundTracker`, `ForegroundApp`, `ForegroundAppInfo`, `DictationTextTarget`, `TextInsertion`, `WindowSnapper`, `SystemMetrics`, `CursorShapeReader`, `DesktopWallpaperReader`, `DwmThumbnailWindow`, `ExplorerSelectionReader`, `Win32Window`, etc. |
| `Windows.Win32.*` (CsWin32) | 244 | Generated P/Invoke & COM interop bindings (`PInvoke`, `HWND`, `HDC`, `HMONITOR`, `ID3D11Device`, `IDXGIOutputDuplication`, `ID2D1DeviceContext`, `IUIAutomation`, etc.) |

### 1.2 WS-I: `src/desktop/Kursor.Core/` (565 C# Files)

| # | Namespace | File Count | Key Classes & Responsibilities |
|---:|---|---:|---|
| 1 | `HeyClicky.Core` | 2 | Core assembly root (`ClickyAppPaths`, constants) |
| 2 | `HeyClicky.Core.Auth` | 10 | `SupabaseAuthService`, PKCE loopback flow, token refresh, DPAPI storage |
| 3 | `HeyClicky.Core.Billing` | 27 | Subscription state, usage quotas, Stripe checkout / portal models |
| 4 | `HeyClicky.Core.Brain` | 29 | `BrainSession`, `BrainClient`, `BrainSseParser`, `SayLines`, `BrainTurnRequest`, `BrainTurnResponse`, `BrainComputerCall`, `BrainToolCall`, etc. |
| 5 | `HeyClicky.Core.Browser` | 12 | `ClickyBrowserBridge`, CDP/extension bridge, browser tab inspection |
| 6 | `HeyClicky.Core.Configuration` | 2 | Runtime environment & endpoint configuration |
| 7 | `HeyClicky.Core.Connectors` | 14 | `McpOAuth`, Composio / third-party connector OAuth & tool routing |
| 8 | `HeyClicky.Core.Dictation` | 16 | Dictation session orchestration, refinement, snippet/vocabulary application |
| 9 | `HeyClicky.Core.Documents` | 2 | Document abstraction interfaces |
| 10 | `HeyClicky.Core.Documents.Markdown` | 7 | Markdown AST helpers |
| 11 | `HeyClicky.Core.Documents.Spreadsheet` | 29 | Spreadsheet/XLSX/CSV reader & manipulation tools |
| 12 | `HeyClicky.Core.Drive` | 12 | `ClickyTakeOverController`, `ClickyDriveActions`, `ClickyDriveConsentResult`, `ClickyDriveSessionLog`, autonomous computer-use loop |
| 13 | `HeyClicky.Core.Identity` | 2 | Device/installation identity |
| 14 | `HeyClicky.Core.Ink` | 107 | `ClickyInkParser`, `ClickyInkLiveNote`, `ClickyInkPatchStream`, `ClickyInkMatch`, `ClickyInkNoteStore`, `ClickyLiveNoteStore`, `ClickyInkDashboardSplit`, `ClickyInkFrontPagePlanner`, `ClickyInkLint`, `InkDispatch`, `InkFiles`, `Scheduler`, `ClickyInkRowMotion`, etc. |
| 15 | `HeyClicky.Core.Net` | 2 | `LoopbackHttpServer`, HTTP client resilience helpers |
| 16 | `HeyClicky.Core.Notch` | 35 | Notch/ArcDock view state, waveform, quick-actions state machine |
| 17 | `HeyClicky.Core.Onboarding` | 20 | Onboarding steps, interactive tutorial state |
| 18 | `HeyClicky.Core.Platform` | 3 | Cross-layer platform abstractions (`IPageReader`, `IDesktopActions`) |
| 19 | `HeyClicky.Core.Proactive` | 47 | Proactive monitors, triggers, notification scheduling |
| 20 | `HeyClicky.Core.Settings` | 2 | User preferences persistence |
| 21 | `HeyClicky.Core.Telemetry` | 11 | `CrashReporting`, structured logging, telemetry batching |
| 22 | `HeyClicky.Core.Tools` | 22 | `WindowsPageReader`, `WindowsDesktopActions`, `PageReads`, local tool executors |
| 23 | `HeyClicky.Core.Voice` | 41 | `CompanionSession`, `DrawingCaptionTiming`, `DeepgramTranscriber`, `GeminiLiveVoiceClient`, `OpenAiRealtimeVoiceClient`, PTT/hands-free state machine |
| 24 | `HeyClicky.Core.Worker` | 50 | Local codex/agent worker harness, background job runners |

*(Note: 21 top-level domain namespaces plus root `HeyClicky.Core` and 2 `HeyClicky.Core.Documents.*` sub-namespaces = 24 C# namespace declarations across 565 files.)*

---

## 2. Subsystem Audit Summary

| Subsystem | Status | Summary of Verification |
|---|---|---|
| **WS-H.1: `OverlayHost` & `CaptureExclusion`** | `FAIL` | Click-through styles (`WS_EX_LAYERED \| WS_EX_TRANSPARENT \| WS_EX_NOREDIRECTIONBITMAP`) and `WDA_EXCLUDEFROMCAPTURE` (`0x11`) are properly set, and `ScreenCaptureYield` cloaks overlays via `DwmSetWindowAttribute(DWMWA_CLOAK = 13)` when snipping tools activate. **Fails on:** zero GPU device-loss recovery (`AUD-H-002`), missing `ScreenCaptureYield.Unregister` on teardown (`AUD-H-003`), and ignored `WM_DPICHANGED` (`AUD-H-004`). |
| **WS-H.2: `CursorOverlay` & Physics** | `FAIL` | Idle loop blocks cleanly on `WaitHandle.WaitAny` with 0% CPU/GPU spin when `shouldTick == false`; `PenRideTrajectory` cubic Hermite / spring physics are finite and deterministic (`WsHiAuditRunner` Test 3). **Fails on:** `FramePacer` leaking its kernel waitable timer (`CreateWaitableTimerExW`) (`AUD-H-003`) and stale `currentDisplay.Scale` after `WM_DPICHANGED` (`AUD-H-004`). |
| **WS-H.3: `ScreenCaptureService` (`DesktopDuplicationSource` & `GraphicsCaptureSource`)** | `FAIL` | DXGI Desktop Duplication falls back to Windows.Graphics.Capture (`GraphicsCaptureSource`) after consecutive failures, and staging textures are mapped/unmapped in `try/finally`. **Fails on:** `Direct3D11CaptureFrame` leak when `TrySetResult` loses a race, hardcoded `gpus[0]` in WGC fallback on multi-GPU systems, stale static `winrtDevice`, and ignored `ProtectedContentMaskedOut` (`AUD-H-005`). |
| **WS-H.4: `LowLevelInputHub` & `ShortcutMonitor`** | `FAIL` | Hook thread runs at `ThreadPriority.Highest`, does not log user keystrokes to disk, and filters `AltGr` (`LCtrl+RAlt`). **Fails on:** `LowLevelInputHub.Release()` never stopping the hook thread or unhooking `WH_KEYBOARD_LL`, `CheckHookAlive` replaying duplicate keys under UIPI / >80ms latency (`AUD-H-006`), and `ShortcutMonitor.MaskStartMenu()` only covering `DictationDown` (`AUD-H-007`). |
| **WS-H.5: `ClickyScreenActuator`, `ClickyTakeBackWatch` & `ClickyTakeOverController`** | `FAIL` | `ClickyScreenActuator` tags synthetic inputs with `ClickySyntheticExtraInfo` (`0x4D414743`) and `ClickyTakeBackWatch` ignores events carrying that tag. **Fails on:** `ClickyTakeOverController` setting `actuatingUntil = Now + 30s` and ignoring all `ClickyTakeBackWatch` triggers during active actuation, bypassing user consent via `recent_yes` (600s) and PTT speech (`AUD-H-010`), and re-hooking `WH_MOUSE_LL`/`WH_KEYBOARD_LL` every 1s in `ClickyUserInputBlocker` (`AUD-H-006`). |
| **WS-H.6: WASAPI Audio & `PermissionService`** | `FAIL` | `AudioTopologyMonitor` handles device hot-swap, `WasapiMicrophoneCapture` avoids Bluetooth Hands-Free AG (`0000111e-...`) profile switching unless necessary, `VoiceRenderStream` recovers from underruns and `AUDCLNT_E_DEVICE_INVALIDATED`. **Fails on:** `EarconPlayer.DecodeWavToStereo48k` throwing `DivideByZeroException` on `<8`-bit WAV headers and `PermissionService` leaking a `RegistryKey` handle (`AUD-H-009`). |
| **WS-H.7: UIA (`UIAutomationHelpers`, `UiaPageText`) & Clipboard** | `FAIL` | `UiaPageText` enforces a 250ms `Stopwatch` budget (`MaxNodes = 1800`, `MaxDepth = 18`). **Fails on:** scraping Incognito/InPrivate browser windows, password managers, banking tabs, and `IsPassword` Edit controls and sending them to `/brain/turn` (`AUD-H-001`), plus permanent clipboard data loss in `DictationTextTarget.BlindPasteAsync` and `TextInsertion.SnapshotClipboard` (`AUD-H-008`). |
| **WS-I.1: `BrainSession`, `BrainSseParser` & `CompanionSession`** | `FAIL` | `BrainSession` enforces `MaximumScreenshotsInThread = 48` (`PruneOldScreenshots` verified in `WsHiAuditRunner` Test 4), `BrainSseParser` handles arbitrary chunk boundaries and CR/LF splits (`WsHiAuditRunner` Test 2), and `CompanionSession` cancels in-flight turns on new PTT. **Fails on:** unconfirmed execution of `typeText` via `WindowsDesktopActions.TypeIntoFocusedFieldAsync` without password/terminal guards (`AUD-I-001`). |
| **WS-I.2: `SayLines` & `DrawingCaptionTiming`** | `FAIL` | `DrawingCaptionTiming` properly synchronizes captions under audio latency (`WsHiAuditRunner` Test 2). **Fails on:** `SayLines.Extract` extracting false positives from JS comments/strings, missing `say (...)` with whitespace, and corrupting `\r` and `\uXXXX` escapes (`AUD-I-002`). |
| **WS-I.3: `HeyClicky.Core.Ink` Model** | `FAIL` | `ClickyInkParser`, `ClickyInkDashboardSplit`, and `ClickyInkFrontPagePlanner` survive malformed markdown, unclosed `<ink-...>` tags, 120-level deep nesting, and 5,000-line documents in ~15ms (`WsHiAuditRunner` Test 6). **Fails on:** `ClickyInkParser.EmphasisMarks` and `MarkingFirstLink` destroying all `**bold**` and `==highlight==` text into literal `"57345$157346"` due to decompiled integer constants (`AUD-I-003`), and unsynchronized `Dictionary` race conditions in `ClickyInkNoteStore` and `ClickyLiveNoteStore` (`AUD-I-004`). |
| **WS-I.4: Thread-Safety & Unhandled Exceptions** | `FAIL` | Zero `async void` methods exist in `Kursor.Platform` or `Kursor.Core`, and `CrashReporting.cs:203` registers `TaskScheduler.UnobservedTaskException`. **Fails on:** 53 unawaited fire-and-forget `Task.Run`/`ContinueWith` sites lacking local exception observation and 4 methods with ILSpy `Iterator.Dispose()` decompilation failures (`AUD-I-005`). |

---

## 3. Detailed Findings (§4 Schema)

### `ID: AUD-H-001`
- **Status:** `FAIL`
- **Severity:** `High`
- **Category:** Security / Privacy (UIA & Browser Scraping)
- **Title:** Unfiltered UIA DOM & Browser URL Scraping of Incognito/InPrivate, Password Managers, and Password Fields
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UiaPageText.cs:28-64`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UiaPageText.cs#L28-L64)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UiaPageText.cs:91-174`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UiaPageText.cs#L91-L174)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UIAutomationHelpers.cs:251-297`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UIAutomationHelpers.cs#L251-L297)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ForegroundAppInfo.cs:60-72`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ForegroundAppInfo.cs#L60-L72)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsPageReader.cs:13-32`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsPageReader.cs#L13-L32)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsPageReader.cs:79-107`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsPageReader.cs#L79-L107)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs:2420-2441`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs#L2420-L2441)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs:3044-3052`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs#L3044-L3052)
- **Description:**
  1. On every voice/text turn (and `read_page` tool call), `CompanionSession` calls `WindowsPageReader.ReadForegroundWindowAsync`, which runs `ForegroundApp.CaptureCurrent()` and `UiaPageText.Read(foregroundWindow, 60000)`.
  2. `ForegroundAppInfo.TryResolveBrowserUrl` (`ForegroundAppInfo.cs:60-72`) and `UIAutomationHelpers.ReadBrowserAddress` (`UIAutomationHelpers.cs:251-297`) inspect `chrome`, `msedge`, `brave`, `vivaldi`, `opera`, `arc`, and `firefox` windows and extract the address bar URL without checking whether the window title or UIA tree indicates **Incognito**, **InPrivate**, or **Private Browsing**.
  3. `UiaPageText.Read` (`UiaPageText.cs:28-64`) and `Walk` (`UiaPageText.cs:91-174`) traverse up to `1,800` UIA nodes (`MaxDepth = 18`, `MaxMilliseconds = 250`) and read `Name`, `ValuePattern.CurrentValue`, `TextPattern`, and `LegacyIAccessible.Value` (`UiaPageText.cs:131-147`) **without checking `UIA_IsPasswordPropertyId` (`30009`)** and without filtering password manager processes (`1Password`, `Bitwarden`, `KeePassXC`, `Dashlane`) or sensitive banking windows.
  4. The resulting `PageReadResult` (up to 60,000 chars of DOM text + URL + window title) is serialized into `turn.page` (`CompanionSession.cs:3044-3052`) and transmitted to `/brain/turn`.
- **Reproduction / Proof:**
  - Inspect `UiaPageText.cs:131-147`: `TryGetStringProperty(element, 30045)` (`UIA_ValueValuePropertyId`) and `TryGetLegacyValue(element)` are appended directly to `AppendClean` without checking `element.GetCurrentPropertyValue(30009)` (`UIA_IsPasswordPropertyId`), even though `DictationTextTarget.cs:324` explicitly checks `IsPassword` (`30009`) for dictation targets.
- **Expected vs Observed:**
  - **Expected:** `UiaPageText` and `ReadBrowserAddress` must skip `IsPassword` controls, redact or skip Incognito/InPrivate browser windows, and block scraping of password manager processes.
  - **Observed:** Full text and URLs from private browsing sessions, password managers, and non-masked value properties are scraped and uploaded to `/brain/turn`.
- **Remediation:**
  - Check `UIA_IsPasswordPropertyId` (`30009`) on every element in `UiaPageText.Walk` before reading `Value` or `LegacyIAccessible.Value`; skip scraping when the window title contains `Incognito`, `InPrivate`, or `Private Browsing`, or when `ProcessName` matches known password managers.

---

### `ID: AUD-H-002`
- **Status:** `FAIL`
- **Severity:** `High`
- **Category:** Reliability / GPU Device Loss
- **Title:** `OverlayHost` Has Zero GPU Device-Loss Recovery (`DXGI_ERROR_DEVICE_REMOVED` / `DXGI_ERROR_DEVICE_RESET`)
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:109-121`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L109-L121)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:354-371`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L354-L371)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:443`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L443)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:606-610`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L606-L610)
- **Description:**
  - `OverlayHost.ThreadMain` (`OverlayHost.cs:354-371`) initializes Direct3D 11 (`D3D11CreateDevice`), Direct2D (`D2D1CreateDevice`), and DirectComposition (`DCompositionCreateDevice2`) once, then enters `RunLoop()`.
  - Inside `RunLoop()`, `CompositionDevice.Commit()` (`OverlayHost.cs:443`) and layer `BeginDraw`/`EndDraw` (`OverlayHost.cs:606-610`) have no `COMException` handler for `DXGI_ERROR_DEVICE_REMOVED` (`0x887A0005`), `DXGI_ERROR_DEVICE_RESET` (`0x887A0007`), or `D2DERR_RECREATE_TARGET` (`0x8899000C`).
  - When a GPU driver updates, TDR occurs, or display docking switches adapters, the `COMException` escapes `RunLoop()` into `ThreadMain`'s outer `catch`, which logs `"OverlayHost thread failed"`, runs `finally` (destroying `WindowHandle` and disposing COM devices), and terminates the `HeyClicky.OverlayHost` thread permanently.
  - Worse, `OverlayHost.shared` (`OverlayHost.cs:109-121`) is never cleared on thread crash, so all subsequent `OverlayHost.Acquire()` calls return the dead `OverlayHost` instance.
- **Reproduction / Proof:**
  - Inspect `OverlayHost.cs:354-371` and `OverlayHost.cs:402-446`: no retry loop or device recreation exists in `ThreadMain` or `RunLoop`.
- **Expected vs Observed:**
  - **Expected:** `OverlayHost` should catch `DXGI_ERROR_DEVICE_REMOVED` / `DXGI_ERROR_DEVICE_RESET` / `D2DERR_RECREATE_TARGET`, rebuild the D3D11/D2D1/DirectComposition device stack, and notify attached `IOverlayLayer` instances to recreate their surfaces.
  - **Observed:** Any GPU reset permanently kills all screen overlays (`CursorOverlay`, `CaptionCardLayer`, `SpotlightLayer`, `ScreenCurtainLayer`) for the remainder of the process lifetime.
- **Remediation:**
  - Catch device-loss HRESULTs inside `RunLoop()`, invoke `DisposeGraphics()`, re-run `CreateGraphics()`, and invoke `layer.Detach()` / `layer.Attach(this)` on all active `layers`.

---

### `ID: AUD-H-003`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Resource Management / Kernel Handle Leak
- **Title:** `OverlayHost` Never Unregisters from `ScreenCaptureYield` and `FramePacer` Leaks Kernel Waitable Timer
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:96-98`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L96-L98)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:148-151`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L148-L151)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:367-371`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L367-L371)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:528`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L528)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/FramePacer.cs:10-44`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/FramePacer.cs#L10-L44)
- **Description:**
  1. `OverlayHost.CreateOverlayWindow()` registers the HWND with both `CaptureExclusion.Register((nint)windowHandle.Value)` and `ScreenCaptureYield.Register((nint)windowHandle.Value)` (`OverlayHost.cs:527-528`). However, `ThreadMain`'s `finally` block (`OverlayHost.cs:367-371`) only calls `CaptureExclusion.Unregister(WindowHandle)` and omits `ScreenCaptureYield.Unregister(WindowHandle)`, leaving a stale HWND in `ScreenCaptureYield.overlayHwnds`.
  2. `FramePacer` (`FramePacer.cs:15`) creates a high-resolution kernel waitable timer via `PInvoke.CreateWaitableTimerExW(null, null, 2u, 2031619u)`, but `FramePacer` does not implement `IDisposable` and never calls `PInvoke.CloseHandle(timer)`.
  3. `OverlayHost` allocates `ManualResetEventSlim ready`, `AutoResetEvent wakeEvent`, and `EventWaitHandle gpuIdle` (`OverlayHost.cs:96-98, 150`) and never disposes them in `DisposeGraphics()` or `ThreadMain`.
- **Reproduction / Proof:**
  - Compare `OverlayHost.cs:527-528` (`CaptureExclusion.Register` + `ScreenCaptureYield.Register`) with `OverlayHost.cs:368` (`CaptureExclusion.Unregister` only), and inspect `FramePacer.cs:1-45` (no `CloseHandle` call anywhere in the class).
- **Expected vs Observed:**
  - **Expected:** All registered HWNDs and kernel wait handles (`CreateWaitableTimerExW`, `AutoResetEvent`, `EventWaitHandle`) are unregistered and closed on teardown.
  - **Observed:** Destroyed HWND remains registered in `ScreenCaptureYield.overlayHwnds` and kernel waitable timer / event handles are leaked.
- **Remediation:**
  - Add `ScreenCaptureYield.Unregister(WindowHandle)` to `OverlayHost.ThreadMain`'s `finally` block, implement `IDisposable` on `FramePacer` calling `PInvoke.CloseHandle(timer)`, and dispose `ready`, `wakeEvent`, and `gpuIdle`.

---

### `ID: AUD-H-004`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Multi-Monitor / DPI Scaling
- **Title:** `OverlayHost.WindowProcedure` Ignores `WM_DPICHANGED` (`0x02E0`) Without Refreshing Display Scales
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs:531-547`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L531-L547)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/CursorLayer.cs:402-406`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/CursorLayer.cs#L402-L406)
- **Description:**
  - In `OverlayHost.WindowProcedure` (`OverlayHost.cs:531-547`), message `126u` (`WM_DISPLAYCHANGE`) sets `displaysDirty = true` and wakes the render loop to call `RefreshDisplays()`, but message `736u` (`WM_DPICHANGED` / `0x02E0`) immediately returns `new LRESULT(0)` (`OverlayHost.cs:539-540`) without setting `displaysDirty = true` or updating virtual screen bounds.
  - Because `CursorLayer` (`CursorLayer.cs:402-406`) and other overlay layers compute DPI scaling from `OverlayHost.DisplayAtPoint(...)?.Scale` (populated only by `RefreshDisplays()`), changing monitor DPI scale without changing pixel resolution leaves `Displays` with stale `Scale` values.
- **Reproduction / Proof:**
  - Inspect `OverlayHost.cs:534-540`:
    ```csharp
    case 126u:
        displaysDirty = true;
        wakeEvent.Set();
        return new LRESULT(0);
    case 736u:
        return new LRESULT(0);
    ```
- **Expected vs Observed:**
  - **Expected:** `WM_DPICHANGED` (`736u`) should also set `displaysDirty = true` and wake the overlay thread to refresh per-monitor DPI scales and virtual screen bounds.
  - **Observed:** `WM_DPICHANGED` is swallowed as a no-op.
- **Remediation:**
  - Combine `case 126u:` and `case 736u:` in `OverlayHost.WindowProcedure` so both trigger `displaysDirty = true; wakeEvent.Set();`.

---

### `ID: AUD-H-005`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Resource Leak / Multi-GPU Screen Capture
- **Title:** `GraphicsCaptureSource` Frame Leak on Timeout Race, Multi-GPU Adapter Mismatch, and Stale Static WinRT Device
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/GraphicsCaptureSource.cs:25-27`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/GraphicsCaptureSource.cs#L25-L27)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/GraphicsCaptureSource.cs:87-111`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/GraphicsCaptureSource.cs#L87-L111)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/ScreenCaptureService.cs:396`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/ScreenCaptureService.cs#L396)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/DesktopDuplicationSource.cs:78-115`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/DesktopDuplicationSource.cs#L78-L115)
- **Description:**
  1. **Unclosed `Direct3D11CaptureFrame` on race (`GraphicsCaptureSource.cs:87-100`):** Inside `framePool.FrameArrived`, the handler calls `firstFrame.TrySetResult(pool.TryGetNextFrame())`. If the 250ms timeout (`Task.Delay(250)`) or a prior `FrameArrived` callback already completed `firstFrame`, `pool.TryGetNextFrame()` still dequeues a native `Direct3D11CaptureFrame`, `TrySetResult` returns `false`, and the frame is dropped without calling `.Dispose()`.
  2. **Hardcoded `gpus[0]` on multi-GPU systems (`ScreenCaptureService.cs:396`):** In `TryCaptureWithGraphicsCaptureAsync`, when DXGI Desktop Duplication falls back to Windows.Graphics.Capture, line 396 selects `CaptureGpu gpu = gpus[0];` instead of matching the `CaptureGpu` that owns `display`. On hybrid laptop/multi-GPU desktop configurations where `display` is attached to `gpus[1]`, WGC texture copy fails or incurs cross-adapter faults.
  3. **Static `winrtDevice` retention (`GraphicsCaptureSource.cs:25-27`):** `winrtDevice` and `winrtDeviceOwner` are static fields that retain a reference to `CaptureGpu` even after `ScreenCaptureService.DisposeTopology()` disposes the underlying `CaptureGpu`.
  4. **Silent DRM masking (`DesktopDuplicationSource.cs:78-115`):** `TryCapture` inspects `OUTDUPL_FRAME_INFO` (`lpFrameInfo`) for `LastPresentTime` and `TotalMetadataBufferSize`, but ignores `lpFrameInfo.ProtectedContentMaskedOut`, returning blacked-out regions without notifying the caller that DRM-protected content was masked.
- **Reproduction / Proof:**
  - Inspect `GraphicsCaptureSource.cs:91`: `firstFrame.TrySetResult(pool.TryGetNextFrame());` and `ScreenCaptureService.cs:396`: `CaptureGpu gpu = gpus[0];`.
- **Expected vs Observed:**
  - **Expected:** Every dequeued `Direct3D11CaptureFrame` must be disposed even when `TrySetResult` returns `false`; WGC fallback must use the `CaptureGpu` associated with the target monitor; DRM masking should be surfaced in metadata.
  - **Observed:** Race-losing frames are leaked, `gpus[0]` is hardcoded for WGC fallback, and `ProtectedContentMaskedOut` is ignored.
- **Remediation:**
  - Change `FrameArrived` to:
    ```csharp
    var frame = pool.TryGetNextFrame();
    if (!firstFrame.TrySetResult(frame)) frame?.Dispose();
    ```
  - Lookup the `CaptureGpu` that owns `display.Handle` (falling back to `gpus[0]` only if not found).

---

### `ID: AUD-H-006`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Input Hook Lifecycle / UIPI
- **Title:** `LowLevelInputHub` Never Unhooks on `Release`, Replays Duplicate Keys Under UIPI/Delay, and `ClickyUserInputBlocker` Re-Hooks Every 1s
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs:166-176`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs#L166-L176)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs:458-479`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs#L458-L479)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs:565-593`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs#L565-L593)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ClickyUserInputBlocker.cs:328-353`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ClickyUserInputBlocker.cs#L328-L353)
- **Description:**
  1. **`Release()` is a no-op for hook teardown (`LowLevelInputHub.cs:166-176`):** `Release()` decrements `referenceCount` to `0`, but has no code to stop the hook thread, dispose `watchdogTimer`, or call `UnhookWindowsHookEx`.
  2. **UIPI & >80ms latency trigger false-positive hook death and duplicate key replay (`LowLevelInputHub.cs:458-479, 565-593`):** `PollWatchedKeys` polls `GetAsyncKeyState` every 25ms. If a watched key is down and `Environment.TickCount64 - lastHookActivityTick > 80`, `CheckHookAlive()` sets `hookDead = true`. This happens routinely in two non-dead situations:
     - When an **elevated (Administrator) window** is in the foreground: Windows UIPI blocks `WH_KEYBOARD_LL` delivery to non-elevated processes, while `GetAsyncKeyState` still reports the key state. `LowLevelInputHub` misdiagnoses UIPI as a dead hook and calls `ReinstallHooks` (`UnhookWindowsHookEx` + `SetWindowsHookExW`) every 1,000ms.
     - When system load delays `WH_KEYBOARD_LL` by >80ms: `CheckHookAlive()` immediately replays `recentRawKeys` (`LowLevelInputHub.cs:581-592`), and when the delayed `WH_KEYBOARD_LL` callback arrives moments later, the key event is processed a second time.
  3. **Unconditional 1-second hook churn in `ClickyUserInputBlocker` (`ClickyUserInputBlocker.cs:328-353`):** While input blocking is held during autonomous drive, `ClickyUserInputBlocker.WndProc` handles `WM_TIMER` (`1000ms`) by unconditionally calling `InstallHooks()`, which destroys and recreates both `WH_MOUSE_LL` and `WH_KEYBOARD_LL` every second, creating a brief unhooked window every 1,000ms.
- **Reproduction / Proof:**
  - Inspect `LowLevelInputHub.cs:166-176` (`Release`), `LowLevelInputHub.cs:565-593` (`CheckHookAlive`), and `ClickyUserInputBlocker.cs:328-353` (`InstallHooks` on 1s timer).
- **Expected vs Observed:**
  - **Expected:** `LowLevelInputHub` should check whether the foreground process is elevated before declaring `WH_KEYBOARD_LL` dead, avoid replaying keys that are merely delayed, and unhook cleanly when `referenceCount == 0`. `ClickyUserInputBlocker` should not unhook/rehook `WH_MOUSE_LL` every second.
  - **Observed:** Perpetual 1-second hook reinstall loops under UIPI and during `ClickyUserInputBlocker` active sessions, plus duplicate key events on >80ms hook latency.
- **Remediation:**
  - Query foreground process elevation before marking `hookDead = true`; increase the hook-dead threshold above the OS `LowLevelHooksTimeout` (~300ms); remove unconditional 1s `InstallHooks()` churn in `ClickyUserInputBlocker`; and post `WM_QUIT` to the hook thread when `referenceCount` reaches 0.

---

### `ID: AUD-H-007`
- **Status:** `FAIL`
- **Severity:** `Low`
- **Category:** Keyboard Shortcuts / Win-Key Masking
- **Title:** `ShortcutMonitor.MaskStartMenu` Only Masks `DictationDown` and Omits `PushToTalk`, `TextMode`, and `HandsFreeDictation`
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs:878-881`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs#L878-L881)
- **Description:**
  - When a shortcut using the Windows modifier key (`HotkeyModifiers.Win`) is triggered, Windows opens the Start Menu on Win-key release unless a dummy key event (`VK_NONAME` / `0xFC`) is injected via `MaskStartMenu()`.
  - In `ShortcutMonitor.Dispatch` (`ShortcutMonitor.cs:878-881`), `MaskStartMenu()` is only called when `item == ShortcutAction.DictationDown && currentShortcuts.Dictation.Modifiers.HasFlag(HotkeyModifiers.Win)`.
  - If the user binds `PushToTalk`, `TextMode`, or `HandsFreeDictation` to a shortcut involving `HotkeyModifiers.Win` (or standalone `LWin`/`RWin`), `MaskStartMenu()` is never called and releasing the shortcut pops open the Windows Start Menu, stealing focus from the target app.
- **Reproduction / Proof:**
  - Inspect `ShortcutMonitor.cs:878-881`:
    ```csharp
    if (item == ShortcutAction.DictationDown && currentShortcuts.Dictation.Modifiers.HasFlag(HotkeyModifiers.Win))
    {
        MaskStartMenu();
    }
    ```
- **Expected vs Observed:**
  - **Expected:** `MaskStartMenu()` should run whenever any triggered shortcut binding uses `HotkeyModifiers.Win` or a Win trigger key.
  - **Observed:** Only `ShortcutAction.DictationDown` masks the Start Menu.
- **Remediation:**
  - Check whether the active binding for `DictationDown`, `PushToTalkDown`, `TextModeActivated`, or `HandsFreeDictationActivated` uses `HotkeyModifiers.Win` (or `VirtualKeyCode` `0x5B`/`0x5C`) and call `MaskStartMenu()` in all cases.

---

### `ID: AUD-H-008`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Data Integrity / Clipboard Preservation
- **Title:** `DictationTextTarget.BlindPasteAsync` and `TextInsertion.SnapshotClipboard` Permanently Destroy User Clipboard Data
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/DictationTextTarget.cs:544-570`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/DictationTextTarget.cs#L544-L570)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs:341-395`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs#L341-L395)
- **Description:**
  1. **`BlindPasteAsync` disables clipboard restoration (`DictationTextTarget.cs:544-570`):** For all processes in `BlindPasteProcesses` (`WeChat`, `Weixin`, `ChatGPT`, `Codex`, `sublime_text`, `ms-teams`, `Teams`, `ApplicationFrameHost`, `CalculatorApp`) and whenever UIA value verification falls back to blind paste, `BlindPasteAsync` calls `TextInsertion.PasteTextAsync(text, restoreClipboard: false)`. This permanently overwrites whatever the user had on their clipboard with the dictated snippet.
  2. **`SnapshotClipboard` drops images and >64MB items (`TextInsertion.cs:341-395`):** Even when `restoreClipboard: true` is passed, `TextInsertion.SnapshotClipboard()` explicitly skips handle-based clipboard formats (`IsHandleClipboardFormat`: `CF_BITMAP` = 2, `CF_METAFILEPICT` = 3, `CF_PALETTE` = 9, `CF_ENHMETAFILE` = 14, `CF_DSPBITMAP` = 130) and any format larger than `67,108,864` bytes (64 MB), then calls `PInvoke.EmptyClipboard()` (`TextInsertion.cs:270`). If the user had a screenshot or image on the clipboard only in `CF_BITMAP` / `CF_ENHMETAFILE` or a large payload, dictating a single word permanently deletes it.
- **Reproduction / Proof:**
  - Inspect `DictationTextTarget.cs:555`: `if (!(await TextInsertion.PasteTextAsync(text, restoreClipboard: false)))` and `TextInsertion.cs:356-389` (`SnapshotClipboard`).
- **Expected vs Observed:**
  - **Expected:** Dictation should preserve the user's clipboard across all target apps (using delayed restoration for slow Electron/WebView2 apps) and preserve image clipboard formats (`CF_DIB`/`CF_DIBV5`/`CF_BITMAP`).
  - **Observed:** Clipboard contents are permanently overwritten in `BlindPasteAsync` and handle-based/large formats are wiped by `EmptyClipboard()`.
- **Remediation:**
  - In `BlindPasteAsync`, snapshot the clipboard before pasting and restore it after a 300-500ms delay (only if the clipboard sequence number has not changed since the paste). Include synthesized `CF_DIBV5` (`17u`) when snapshotting image formats.

---

### `ID: AUD-H-009`
- **Status:** `FAIL`
- **Severity:** `Low`
- **Category:** Audio / Permissions Robustness
- **Title:** `EarconPlayer.DecodeWavToStereo48k` Throws `DivideByZeroException` on Sub-8-Bit WAV and `PermissionService` Leaks `RegistryKey`
- **Locations:**
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Audio/EarconPlayer.cs:290-322`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Audio/EarconPlayer.cs#L290-L322)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Permissions/PermissionService.cs:266`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Permissions/PermissionService.cs#L266)
- **Description:**
  1. **`EarconPlayer.DecodeWavToStereo48k` (`EarconPlayer.cs:290-291`):** Computes `int num5 = Math.Max(1, num2) * (bitsPerSample / 8);` and immediately divides `int num6 = num4 / num5;`. Because integer division `bitsPerSample / 8` evaluates to `0` for any `bitsPerSample < 8` (e.g., 4-bit ADPCM WAV or a corrupted header with `bitsPerSample = 0`), `num5` becomes `0` and line 291 throws `System.DivideByZeroException`. Additionally, `num` (`sampleRate`) is not validated to be `> 0` before `48000.0 / (double)num` at line 303.
  2. **`PermissionService.ReadConsentStoreValue` (`PermissionService.cs:266`):** Calls `Registry.LocalMachine.OpenSubKey(subKeyPath)` without `using`, leaking the underlying native `HKEY` handle until finalization, whereas line 276 (`Registry.CurrentUser.OpenSubKey`) properly uses `using`.
- **Reproduction / Proof:**
  - Dynamically reproduced in [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs) (Test 5) and logged in [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt): passing a 44-byte WAV with `bitsPerSample = 4` into `EarconPlayer.DecodeWavToStereo48k` throws `System.DivideByZeroException`.
- **Expected vs Observed:**
  - **Expected:** `DecodeWavToStereo48k` should return `Array.Empty<float>()` when `bitsPerSample < 8` or `sampleRate <= 0`; `PermissionService` should dispose all opened `RegistryKey` instances.
  - **Observed:** `DivideByZeroException` on `bitsPerSample < 8` and leaked `RegistryKey` at `PermissionService.cs:266`.
- **Remediation:**
  - Guard `if (bitsPerSample < 8 || num <= 0 || num4 <= 0) return Array.Empty<float>();` before line 290 of `EarconPlayer.cs`, and add `using` to `RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(subKeyPath)` at `PermissionService.cs:266`.

---

### `ID: AUD-H-010`
- **Status:** `FAIL`
- **Severity:** `High`
- **Category:** Safety / User Take-Back & Consent
- **Title:** `ClickyTakeOverController` Ignores User Mouse/Keyboard Take-Back During Active Actuation and Bypasses Consent via `recent_yes` / PTT
- **Locations:**
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs:445-478`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs#L445-L478)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs:961-993`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs#L961-L993)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs:1364-1368`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs#L1364-L1368)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs:1519-1527`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Drive/ClickyTakeOverController.cs#L1519-L1527)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ClickyTakeBackWatch.cs:14-67`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ClickyTakeBackWatch.cs#L14-L67)
- **Description:**
  1. **User mouse take-back is disabled for up to 30 seconds during active actuation (`ClickyTakeOverController.cs:963, 1364-1368`):** Before calling `ClickyDriveActions.PerformComputerActionsAsync`, `ClickyTakeOverController` sets `actuatingUntil = DateTimeOffset.Now.AddSeconds(30.0)` (`line 963`). When the user physically moves the mouse (`>140px` in `150ms`), `ClickyTakeBackWatch` fires `OnUserMovedMouse`, which checks:
     ```csharp
     if (DateTimeOffset.Now < actuatingUntil)
     {
         return;
     }
     ```
     Simultaneously, `ClickyUserInputBlocker` swallows all physical mouse clicks, mouse wheel events, and all non-`Escape` keys. Thus, during the exact window when the AI is actively clicking and typing on the user's screen, moving the mouse, clicking, or pressing any key other than `Escape` **cannot stop the AI** for up to 30 seconds!
  2. **Silent consent bypass via 10-minute `recent_yes` and PTT speech (`ClickyTakeOverController.cs:445-478, 1519-1527`):**
     - If any takeover drive finished within the past **600 seconds (10 minutes)** (`lastDriveEndedAt`), `EnsureConsentToDriveAsync` returns `ClickyDriveConsentResult.Proceed("recent_yes")` without showing a confirmation prompt (`lines 445-451`).
     - If an offer card is visible and the user presses Push-To-Talk (`StopBecauseUserSpoke`, `lines 1519-1527`), `offerAnsweredBySpeaking = true` is set unconditionally—even if the user spoke *"No, don't touch my screen"*—causing `EnsureConsentToDriveAsync` (`lines 438-443`) to immediately auto-approve `start_screen_takeover` with `"answered_offer_by_speaking"`.
- **Reproduction / Proof:**
  - Inspect `ClickyTakeOverController.cs:963` (`actuatingUntil = DateTimeOffset.Now.AddSeconds(30.0)`), `ClickyTakeOverController.cs:1364-1368` (`if (DateTimeOffset.Now < actuatingUntil) return;`), and `ClickyTakeOverController.cs:438-451` (`answered_offer_by_speaking` and `recent_yes`).
- **Expected vs Observed:**
  - **Expected:** Physical user mouse movement (`ClickyTakeBackWatch` already filters out synthetic events via `ExtraInfo == 1296124995`) must immediately cancel in-flight actuation (`CancelActiveDrive`), and `start_screen_takeover` must require explicit per-task consent.
  - **Observed:** Physical mouse movement is ignored during the entire `actuatingUntil` window (up to 30s), and consent is bypassed for 10 minutes after any drive or after any PTT utterance during an offer card.
- **Remediation:**
  - Remove the `if (DateTimeOffset.Now < actuatingUntil) return;` guard in `OnUserMovedMouse` (relying on `ClickyTakeBackWatch`'s existing `ClickySyntheticExtraInfo` filter plus cursor teleport suppression), and require explicit user confirmation for each takeover session.

---

### `ID: AUD-I-001`
- **Status:** `FAIL`
- **Severity:** `High`
- **Category:** Safety / Unconfirmed Synthetic Input
- **Title:** `WindowsDesktopActions.TypeIntoFocusedFieldAsync` Injects Keystrokes and `Enter` Without User Confirmation or Password/Terminal Guards
- **Locations:**
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsDesktopActions.cs:91-159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsDesktopActions.cs#L91-L159)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsDesktopActions.cs:240-251`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Tools/WindowsDesktopActions.cs#L240-L251)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs:3434-3459`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs#L3434-L3459)
- **Description:**
  - Outside of `ClickyTakeOverController`, any `/brain/turn` response containing `typeText` triggers `CompanionSession` (`lines 3434-3459`) to call `WindowsDesktopActions.TypeIntoFocusedFieldAsync(typeText, pressEnter)` **with zero user confirmation**.
  - `TypeIntoFocusedFieldAsync` (`WindowsDesktopActions.cs:91-159`) has multiple safety and reliability flaws:
    1. **No terminal/shell or password guard:** Unlike `DictationTextTarget`, it does not check if the focused window is a shell (`powershell`, `pwsh`, `cmd`, `WindowsTerminal`, `mintty`) or a password field (`IsPassword`). If the user has a terminal focused and the LLM responds with `typeText: "rm -rf ..."` and `pressEnter: true`, it immediately pastes the command and injects `VK_RETURN` (`0x0D`) (`lines 136-139`).
    2. **Untagged synthetic `SendInput`:** `KeyDown` and `KeyUp` (`WindowsDesktopActions.cs:240-251`) leave `dwExtraInfo = 0` instead of tagging with `ClickyScreenActuator.ClickySyntheticExtraInfo`.
    3. **Spawns 3 raw OS STA threads per call and wipes non-text clipboard data:** `GetClipboardText`, `SetClipboardText`, and `RestoreClipboard` (`lines 161-214`) each create a brand-new `new Thread(...)` with `ApartmentState.STA`. If the user had an image or file list on the clipboard, `Clipboard.GetText()` returns `""`, and `RestoreClipboard` calls `Clipboard.Clear()` (`line 203`), destroying the user's clipboard.
- **Reproduction / Proof:**
  - Inspect `CompanionSession.cs:3434-3459` and `WindowsDesktopActions.cs:91-214, 240-251`.
- **Expected vs Observed:**
  - **Expected:** `typeText` (especially with `pressEnter: true` or when a terminal/shell is focused) must require user confirmation, tag synthetic `SendInput` with `ClickySyntheticExtraInfo`, and preserve non-text clipboard formats via `TextInsertion.PasteTextAsync`.
  - **Observed:** Immediate unconfirmed `Ctrl+V` + `Enter` injection into any focused window and destruction of non-text clipboard items.
- **Remediation:**
  - Gate `typeText` behind user confirmation when `pressEnter == true` or when the foreground process is a terminal/shell; delegate text pasting to `TextInsertion.PasteTextAsync`; and tag `SendInput` events with `ClickySyntheticExtraInfo`.

---

### `ID: AUD-I-002`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Parser Correctness / Caption Synchronization
- **Title:** `SayLines` JS Scanner Extracts False Positives From Comments/Strings, Misses Whitespace Calls, and Corrupts Escapes
- **Locations:**
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/SayLines.cs:11-138`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/SayLines.cs#L11-L138)
- **Description:**
  - `SayLines.Extract(string? code)` (`SayLines.cs:11-138`) scans LLM-generated JS drawing scripts for `say(...)` and `step(...)` calls using naive `code.IndexOf("say(", ...)` and `code.IndexOf("step(", ...)` searches without tracking JavaScript lexical context:
    1. **Comments & string literals (`SayLines.cs:42-81`):** Any `say("...")` or `step("...")` appearing inside a `//` single-line comment, a `/* ... */` block comment, or inside a string argument to another function (e.g. `write("example: say('hello')")`) is extracted and spoken aloud / displayed as a caption.
    2. **Whitespace before `(` (`SayLines.cs:45-46`):** Because it searches for literal `"say("` and `"step("`, valid JS calls with whitespace such as `say ("Hello")` or `step ("One")` are completely missed.
    3. **Escape sequence corruption (`SayLines.cs:109-128`):** In `ReadFirstStringArgument`, the escape switch handles `\n` and `\t`, but falls into the `default:` branch for `\r`, `\uXXXX`, and `\xXX`. Thus `\r` becomes literal `'r'`, and `\u2014` becomes `'u2014'`.
    4. **Template literal `${...}` expressions (`SayLines.cs:96-135`):** Backtick template literals (`say(\`value is ${1+1}\`)`) are read raw without stripping or evaluating `${...}`, causing the TTS / caption card to speak literal `"value is ${1+1}"`.
- **Reproduction / Proof:**
  - Dynamically reproduced and verified in [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs) (Test 1) and saved in [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt).
- **Expected vs Observed:**
  - **Expected:** `SayLines.Extract` should ignore `say`/`step` tokens inside JS comments and string literals, allow optional whitespace before `(`, and properly decode `\r` and `\uXXXX`.
  - **Observed:** Extracts commented-out and nested-string `say(...)` calls, misses `say (...)`, and turns `\r` into `'r'`.
- **Remediation:**
  - Replace the `IndexOf` loop in `SayLines.Extract` with a single-pass JS tokenizer that skips `//` comments, `/* */` comments, and unrelated string/template/regex literals, and add `'r' => '\r'` and `'u'` hex parsing to the escape switch.

---

### `ID: AUD-I-003`
- **Status:** `FAIL`
- **Severity:** `High`
- **Category:** Data Corruption / Decompilation Regression in Ink Parser
- **Title:** `ClickyInkParser.EmphasisMarks` and `MarkingFirstLink` Corrupt Every `**Bold**`, `==Highlight==`, and `[Link](url)` Span Into Literal Integer Digits
- **Locations:**
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs:20-29`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs#L20-L29)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs:1591-1596`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs#L1591-L1596)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs:1613-1623`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs#L1613-L1623)
- **Description:**
  - `ClickyInkParser` defines Private Use Area `char` constants for inline formatting markers (`ClickyInkParser.cs:20-29`):
    - `BoldOn = '\ue001'` (`57345`), `BoldOff = '\ue002'` (`57346`)
    - `HighlightOn = '\ue003'` (`57347`), `HighlightOff = '\ue004'` (`57348`)
    - `LinkOn = '\ue005'` (`57349`), `LinkOff = '\ue006'` (`57350`)
  - However, in `EmphasisMarks` (`ClickyInkParser.cs:1591-1596`), the decompiler emitted those `char` constants as `int` literals inside interpolated replacement strings:
    ```csharp
    text = BoldPattern.Replace(text, $"{57345}$1{57346}");
    text = HighlightPattern.Replace(text, $"{57347}$1{57348}");
    ```
  - At runtime, `$"{57345}$1{57346}"` evaluates to the string `"57345$157346"`. In .NET `Regex.Replace`, `$157346` is parsed as **capture group number `157346`** (which does not exist), so `Regex.Replace` outputs the literal characters `"57345$157346"`!
  - Consequently, **every single `**bold**` word and `==highlight==` span in every Ink note is completely erased and replaced with the literal text `"57345$157346"` or `"57347$157348"`**, and `CleanPlain` never sets `Bold = true` or `Highlighted = true` because `'\ue001'`..`'\ue004'` are never present!
  - Similarly, `MarkingFirstLink` (`ClickyInkParser.cs:1619`) interpolates `$"{57349}{value}{57350}"`, wrapping every markdown link label in the visible digits `"57349"` and `"57350"` instead of `LinkOn`/`LinkOff`.
- **Reproduction / Proof:**
  - Dynamically reproduced and verified in [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs) (Test 6) and recorded in [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt):
    - Input: `"This is **bold** and ==highlighted== with a [link](https://example.com)."`
    - Observed `Paragraph.Runs[0].Text`: `"This is 57345$157346 and 57347$157348 with a 57349link57350."` (`Bold == false`, `Highlighted == false`).
- **Expected vs Observed:**
  - **Expected:** `**bold**` parses into a `ClickyInkRun` with `Text = "bold", Bold = true`; `==highlighted==` parses with `Text = "highlighted", Highlighted = true`; `[link](https://example.com)` parses with `Text = "link", LinkUrl = "https://example.com"`.
  - **Observed:** User words inside `**...**` and `==...==` are destroyed and replaced by `"57345$157346"` and `"57347$157348"`, and links are surrounded by `"57349...57350"`.
- **Remediation:**
  - In `ClickyInkParser.cs:1593-1594`, change `$"{57345}$1{57346}"` to `$"{BoldOn}$1{BoldOff}"` and `$"{57347}$1{57348}"` to `$"{HighlightOn}$1{HighlightOff}"`. In `ClickyInkParser.cs:1619`, change `$"{57349}{value}{57350}"` to `$"{LinkOn}{value}{LinkOff}"`.

---

### `ID: AUD-I-004`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Concurrency / Thread Safety in Ink Note Stores
- **Title:** Unsynchronized Dictionary Mutations and Race Conditions in `ClickyInkNoteStore` and `ClickyLiveNoteStore`
- **Locations:**
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs:27-50`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs#L27-L50)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs:348-512`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs#L348-L512)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs:712-737`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs#L712-L737)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyLiveNoteStore.cs:12-23`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyLiveNoteStore.cs#L12-L23)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyLiveNoteStore.cs:76-133`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyLiveNoteStore.cs#L76-L133)
- **Description:**
  - `ClickyInkNoteStore` maintains 15 mutable `Dictionary<,>` and `HashSet<>` fields (`versionsByNoteId`, `selectedVersionByNoteId`, `lastDocumentByNoteId`, `liveNotes`, `settledNotes`, `pendingStreamedTexts`, `watched`, etc., `ClickyInkNoteStore.cs:27-50`) with **zero locks**.
  - `ClickyInkNoteStore.PreloadVersions` (`ClickyInkNoteStore.cs:712-737`) is launched via `Task.Run(PreloadVersions)` in the constructor (`line 64`) and directly mutates `versionsByNoteId[id]` and `selectedVersionByNoteId[id]` on a ThreadPool thread while the UI thread or background SSE stream calls `OpenNote`, `StreamPatch`, `ApplyMarkdown`, or `CreateNote`.
  - Similarly, `ClickyLiveNoteStore` (`ClickyLiveNoteStore.cs:12-23, 76-133`) mutates `liveNotes`, `settledNotes`, `lastDocumentByNoteId`, and `pendingStreamedTexts` without synchronization across streaming and timer threads.
- **Reproduction / Proof:**
  - Dynamically reproduced and verified in [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs) (Test 7) and saved in [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt).
- **Expected vs Observed:**
  - **Expected:** Concurrent access to `ClickyInkNoteStore` and `ClickyLiveNoteStore` from `Task.Run(PreloadVersions)`, SSE streaming, and UI threads must be synchronized.
  - **Observed:** Unsynchronized concurrent reads/writes corrupt `Dictionary<,>` internal state and throw exceptions (`InvalidOperationException: Operations that change non-concurrent collections must have exclusive access` / `NullReferenceException`).
- **Remediation:**
  - Guard all state mutations and lookups in `ClickyInkNoteStore` and `ClickyLiveNoteStore` with a private `lock` object (or migrate shared maps to `ConcurrentDictionary<,>`).

---

### `ID: AUD-I-005`
- **Status:** `FAIL`
- **Severity:** `Medium`
- **Category:** Concurrency / Unobserved Tasks & Decompiler Iterator Artifacts
- **Title:** 53 Unawaited Fire-and-Forget Task Sites and 4 Decompiler `Iterator.Dispose` Failures Across `Kursor.Core` and `Kursor.Platform`
- **Locations:**
  - [`docs/audit/evidence/ws-hi/thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkLiveNote.cs:1420`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkLiveNote.cs#L1420)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkRowMotion.cs:472`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkRowMotion.cs#L472)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs:849`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs#L849)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Connectors/McpOAuth.cs:551`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Connectors/McpOAuth.cs#L551)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs:1216`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs#L1216)
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/CursorOverlay.cs:68`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/CursorOverlay.cs#L68)
- **Description:**
  - A full static scan of all 911 C# files in `Kursor.Platform` (346) and `Kursor.Core` (565) (saved in [`docs/audit/evidence/ws-hi/thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt)) established:
    - `async void` methods: **0** in `Kursor.Platform`, **0** in `Kursor.Core` (`PASS`).
    - `TaskScheduler.UnobservedTaskException`: Registered in [`CrashReporting.cs:203`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Telemetry/CrashReporting.cs#L203) (`PASS`).
    - **53 unawaited `Task.Run` / `ContinueWith` sites** (48 in `Kursor.Core`, 5 in `Kursor.Platform`, e.g., `SupabaseAuthService.cs:839`, `LoopbackHttpServer.cs:173`, `CompanionSession.cs:1216, 1228, 5611`, `DeepgramTranscriber.cs:143`, `CursorOverlay.cs:68, 82, 96`) discard the returned `Task` without a local `try/catch` or `OnlyOnFaulted` continuation, delaying exception visibility until GC finalization of the `Task`.
    - **4 decompiler iterator state-machine failures:** ILSpy emitted `//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()` in:
      1. `ClickyInkLiveNote.cs:1420` (`EnumerateBlocks`)
      2. `ClickyInkRowMotion.cs:472`
      3. `ClickyBrowserBridge.cs:849`
      4. `McpOAuth.cs:551`
      When a `foreach` consumer breaks early out of these iterators, the synthesized `Dispose()` method may not execute the original `finally` block.
- **Reproduction / Proof:**
  - See full scan listing in [`docs/audit/evidence/ws-hi/thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt).
- **Expected vs Observed:**
  - **Expected:** Fire-and-forget tasks should observe and log exceptions immediately, and iterator methods should compile with complete `try/finally` disposal semantics.
  - **Observed:** 53 fire-and-forget task sites lack immediate fault continuations and 4 iterator methods contain decompiler `Iterator.Dispose()` warnings.
- **Remediation:**
  - Add a shared `.Forget(logger)` extension method for fire-and-forget tasks, and manually reconstruct the `foreach`/`yield return` loops in the 4 methods flagged with `Iterator.Dispose()` warnings.

---

## 4. Dynamic Test & Fuzz Harness Results (`tests/audit/ws-hi/`)

We authored and executed [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs) against `Kursor.Core` and `Kursor.Platform`. Full console output is archived at [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt):

| Test # | Target Component(s) | Result | Key Observations |
|---:|---|---|---|
| **Test 1** | `HeyClicky.Core.Brain.SayLines` | `FAIL` (`AUD-I-002`) | Confirmed extraction of false-positive `say(...)` from `//` and `/* */` comments and nested string literals, missed `say ("...")` with whitespace, and corrupted `\r` -> `'r'`. |
| **Test 2** | `DrawingCaptionTiming` & `BrainSseParser` | `PASS` | `DrawingCaptionTiming` accurately scheduled captions under simulated audio latency; `BrainSseParser` parsed fragmented multi-chunk SSE streams (`\r\n` and `\n`) without data loss. |
| **Test 3** | `HeyClicky.Platform.Overlay.PenRideTrajectory` | `PASS` | `PenRideTrajectory` produced finite, bounded coordinates across zero-length, short, and long cross-monitor trajectories (`t = 0..1`). |
| **Test 4** | `HeyClicky.Core.Brain.BrainSession` | `PASS` | Pushing 60 screenshot turns strictly pruned the oldest screenshots to enforce `MaximumScreenshotsInThread = 48`. |
| **Test 5** | `HeyClicky.Platform.Audio.EarconPlayer` | `FAIL` (`AUD-H-009`) | Confirmed `DecodeWavToStereo48k` throws `System.DivideByZeroException` when given a WAV header with `bitsPerSample = 4`. |
| **Test 6** | `HeyClicky.Core.Ink` (`ClickyInkParser`, `ClickyInkPatchStream`, `ClickyInkMatch`, `ClickyInkDashboardSplit`, `ClickyInkFrontPagePlanner`) | `FAIL` (`AUD-I-003`) | Parser survived unclosed `<ink-callout>` tags, malformed tables, 120-level nested blockquotes, and a 5,000-line document in ~15ms, **but** corrupted every `**bold**` and `==highlight==` span into literal `"57345$157346"` and `"57347$157348"`, and wrapped `[link](url)` in `"57349...57350"`. |
| **Test 7** | `ClickyInkNoteStore` & `ClickyLiveNoteStore` | `FAIL` (`AUD-I-004`) | Concurrent note creation, streaming patches, and version reads across 8 threads triggered unsynchronized `Dictionary` race conditions. |
