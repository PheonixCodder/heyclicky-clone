# 01 — Architecture Overview: HeyClicky v1.1.5 vs. Open-Source Clicky

> [!IMPORTANT]
> This document is the ground-truth architectural reference for **Kursor** (`C:\dev\heyclicky-clone`), an exact working clone of **HeyClicky v1.1.5** (`C:\Users\ubaid\AppData\Local\HeyClickyApp\current\`). All binary hashes, metadata counts, type names, native dependencies, and runtime directory structures were extracted directly from the shipped v1.1.5 artifacts and user data folder (`C:\Users\ubaid\AppData\Local\HeyClicky\`).

---

## 1. Executive Summary & Evolution: Open-Source `services/clicky` vs. Production HeyClicky v1.1.5

The open-source repository in [services/clicky](file:///C:/dev/heyclicky-clone/services/clicky/README.md) (`leanring-buddy`, ~7 months older) was an early macOS-only prototype (~6,500 lines of Swift + a 141-line Cloudflare Worker proxy). **HeyClicky v1.1.5** for Windows is a complete generational rewrite into a modular, multi-assembly **.NET 9.0 + WinUI 3 + Direct2D/Direct3D11 + ClearScript V8 + Codex Agent Harness** desktop platform.

### High-Level Architecture Diagram (HeyClicky v1.1.5)

```mermaid
flowchart TD
    subgraph UI["HeyClicky.exe / HeyClicky.dll (WinUI 3 Presentation Layer)"]
        Notch["NotchWindow & NotchPresenter"]
        Cursor["CursorOverlayWindow (Blue Buddy + Pen Ride)"]
        Canvas2D["ScreenDrawingCanvas (2D Ink + ClearScript V8)"]
        Canvas3D["Drawing3D (Direct3D11 + HLSL Shaders + Three.js)"]
        Settings["SettingsWindow / Onboarding / Clickys Hub"]
    end

    subgraph Core["HeyClicky.Core.dll (Central Brain, Voice, Dictation & State)"]
        Companion["CompanionSession (Push-to-Talk & Text Orchestrator)"]
        Brain["BrainSession + BrainClient + BrainSseParser"]
        Dictation["DictationController + DictationCorrectionDetector"]
        Ears["GeminiTranscriber + DeepgramTranscriber"]
        Mouth["RealtimeMouth (OpenAI Realtime + Gemini TTS)"]
        Proactive["ProactiveAgentsClient + AgentBackendClient + UpcomingMeetingsClient"]
        Auth["SupabaseAuthService + AuthTokenStore (DPAPI auth.bin)"]
        Worker["WorkerClient + ClickyHttp + ClickyClientHeadersHandler"]
    end

    subgraph Agents["HeyClicky.Agents.dll & Sidecars"]
        Harness["HarnessRuntime/clicky-harness.exe (Codex Agent Runtime)"]
        BrowserHost["ClickyBrowserHost.exe (Chromium Native Messaging)"]
        Mcp["MCP OAuth & Tool Catalog (Composio / Local Skills)"]
    end

    subgraph Platform["HeyClicky.Platform.dll (Windows Native OS Integration)"]
        WinCapture["ScreenCaptureService (DXGI / GraphicsCapture)"]
        WinAudio["MicrophoneCapture & XAudio2 Output"]
        WinInput["ShortcutMonitor (Low-Level Hook) & UIAutomation TextTarget"]
    end

    subgraph Cloud["Cloud & Upstream Services"]
        WorkerAPI["Cloudflare Worker API (https://api.heyclicky.com)"]
        Supabase["Supabase Auth (https://mrpvynsdsnimuisyhkow.supabase.co)"]
        OpenAI["OpenAI Realtime WS (wss://api.openai.com/v1/realtime)"]
        Gemini["Gemini Live & TTS WS (wss://generativelanguage.googleapis.com)"]
        Deepgram["Deepgram Nova-3 WS (wss://api.deepgram.com/v1/listen)"]
    end

    UI --> Core
    Core --> Platform
    Core --> Agents
    Worker --> WorkerAPI
    Brain --> WorkerAPI
    Auth --> Supabase
    Ears --> Gemini
    Ears --> Deepgram
    Mouth --> OpenAI
    Mouth --> Gemini
```

### Side-by-Side Architectural Comparison

| Dimension | Open-Source `services/clicky` (7 Months Ago) | Production **HeyClicky v1.1.5** (Current Target) |
| :--- | :--- | :--- |
| **Target OS & UI Stack** | macOS 14.2+ only; Swift / SwiftUI + AppKit `NSPanel` (`leanring-buddy`) | Windows 10/11 (`x86_64`); **.NET 9.0**, **WinUI 3** (`Microsoft.WinUI.dll`), **Win2D** (`Microsoft.Graphics.Canvas`), **Vortice DirectX** (`Direct2D1`, `Direct3D11`, `DirectComposition`, `XAudio2`) |
| **Installer & Updater** | Manual Xcode build (`leanring-buddy.xcodeproj`) | **Velopack** (`Velopack.dll`, `HeyClicky-Update.exe`, `sq.version`) installed at `%LOCALAPPDATA%\HeyClickyApp\current\` |
| **Cloudflare Worker Proxy** | 3 routes in [worker/src/index.ts](file:///C:/dev/heyclicky-clone/services/clicky/worker/src/index.ts): `POST /chat`, `POST /tts`, `POST /transcribe-token` | 40+ routes across `/brain/*`, `/v2/dictation/*`, `/realtime/*`, `/deepgram/*`, `/ears/*`, `/plan`, `/config`, `/devices`, `/memory`, `/search/*`, `/billing/*`, `/referral/*`, `/connectors/*`, `/proactive/*`, `/agents/*`, `/calendar/*`, `/telemetry/*` |
| **Authentication & Billing** | None (raw API keys stored in Worker secrets) | **Supabase Auth** (`https://mrpvynsdsnimuisyhkow.supabase.co`) with Google OAuth PKCE deep-links (`heyclicky://auth/callback`), email/password, DPAPI-encrypted `auth.bin`, Stripe Checkout & Portal (`/billing/checkout`, `/billing/portal`), tiered quotas (`free`, `pro`, `unlimited`, `max`) |
| **Primary AI Brain** | Direct proxy to Anthropic `claude-sonnet-4-6` / `claude-opus-4-6` via `/chat` | **OneBrain** (`BrainSession` + `BrainClient`) streaming via `POST /brain/turn` with gzip request compression (`x-clicky-accepts-gzip`), thread compaction (`POST /brain/compact` at 45k conversation / 75k input tokens), session-end summarization (`POST /brain/session-end`), tool calls (`BrainToolCall`), and computer-use actions (`BrainComputerCall` / Drive leases) |
| **Screen Pointing & Drawing** | Static `[POINT:x,y:label:screenN]` regex tags parsed from Claude text; bezier cursor flight | Full **2D & 3D Interactive Canvas Engine**: LLM streams JavaScript drawing programs executed inside embedded **ClearScript V8** (`clicky-draw-engine.js`, `clicky-draw-pieces.js`, `clicky-three.js`) synchronized step-by-step with `say(step, "text")` voice narration and pen-riding cursor animations (`PenStrokePath`, `ClickTargetRing`) |
| **Voice Input ("Ears")** | AssemblyAI v3 WebSocket (`u3-rt-pro`), OpenAI Whisper WAV upload, Apple Speech fallback | Dual low-latency streaming WebSockets with warm-socket pooling: **Google Gemini Live Bidi** (`GeminiTranscriber` via `/v2/dictation/gemini-token`) and **Deepgram Nova-3** (`DeepgramTranscriber` via `/deepgram/token`), plus batch fallback (`/ears/transcribe`, `/v2/dictation/transcribe`) |
| **Voice Output ("Mouth")** | ElevenLabs HTTP MP3 (`POST /tts` -> `eleven_flash_v2_5`) | **RealtimeMouth** dual streaming engine: **OpenAI Realtime WebSocket** (`gpt-realtime-2.1-mini` via `/realtime/token` with verbatim `response.create` instructions) and **Gemini TTS WebSocket** (`gemini-3.8-flash-lite-tts` for `gemini:*` expressive voices), plus local time-stretching (`TimeStretcher`) and XAudio2 playback |
| **System-Wide Dictation** | Basic push-to-talk for talking to Clicky only | Dedicated **System-Wide AI Dictation** (`DictationController`): hold or double-tap (hands-free) shortcut, active UIAutomation text field context capture (`DictationFocus`), LLM text cleanup (`POST /v2/dictation/cleanup`), automatic user correction learning (`DictationCorrectionDetector`), custom vocabulary (`DictationVocabulary`, max 50 terms), and local JSONL transcript archive |
| **Background Agents & "Clickys"** | None | **HeyClicky.Agents.dll** + bundled **88.5 MB Codex Agent Harness** (`HarnessRuntime\clicky-harness.exe`), custom "Clickys" (named background workers with custom avatars in `clickys.json` and workspaces in `Clickys\jobs\`), **Chromium Native Messaging Host** (`ClickyBrowserHost.exe`), and **Composio / MCP Connectors** |
| **Proactive Assistant** | None | **Notch UI** (`NotchPresenter`) with morning greetings, proactive task suggestions (`/proactive/jobs`), calendar meeting countdowns (`UpcomingMeetingsClient` -> `/calendar/upcoming`), and app-specific integration suggestions |

---

## 2. Core `.NET 9.0` Managed Assemblies Inventory

All core application binaries reside in `C:\Users\ubaid\AppData\Local\HeyClickyApp\current\` and target `.NETCoreApp,Version=v9.0` (`x86_64`, ReadyToRun precompiled CIL + native code, version `1.1.5.0`).

| Assembly / Executable | Size (Bytes) | SHA-256 Digest | Module MVID | TypeDefs | Fields | MethodDefs | Primary Responsibility |
| :--- | ---: | :--- | :--- | ---: | ---: | ---: | :--- |
| **`HeyClicky.exe`** | `436,032` | `26ab779203881266fb8fc0bd4664d26d7167c0c185c3b63638c79ea7eee3f4f9` | Native apphost | — | — | — | Native `.NET 9.0` Windows GUI apphost bootstrapper that loads `HeyClicky.dll`. |
| **`HeyClicky.dll`** | `11,296,064` | `16e24e332cbe75042de745fc9649a8f727ce0df49110298c51ce02ffd0077792` | `4dccb626-55fa-4dfe-ae1e-e62b530df6c0` | `2,869` | `12,318` | `17,520` | WinUI 3 application shell (`App`, entry token `0x060004f4`), Notch window, Cursor overlay window, Onboarding tour, Settings hub, Clickys job viewer, and tray icon management. |
| **`HeyClicky.Core.dll`** | `7,884,096` | `c7de513fa99a2d8eee2a8704cade3f5e02463f6f8d3210a83c4c61441c294f39` | `f36587be-dbb4-412b-8e2d-1efa463c8575` | `1,817` (506 top-level) | `9,924` | `15,579` | Core domain logic across 20 namespaces (`Auth`, `Billing`, `Brain`, `Browser`, `Configuration`, `Connectors`, `Dictation`, `Documents`, `Drive`, `Identity`, `Ink`, `Net`, `Notch`, `Onboarding`, `Platform`, `Proactive`, `Settings`, `Telemetry`, `Tools`, `Voice`, `Worker`). |
| **`HeyClicky.Agents.dll`** | `2,121,024` | `39ab1b748553caa297d05d71d5da4c7742780db87a73dd3ea1098ac6d884f6ac` | `8c83ddbd-b574-4137-b87d-5eec11189b49` | `524` | `2,458` | `4,118` | Background agent lifecycle (`IAgentService`), Codex `clicky-harness.exe` JSON-RPC / stdio protocol, skill discovery, job folder management (`output/`, `tmp/`, `.clicky/memory.md`), and browser bridge coordination. |
| **`HeyClicky.Platform.dll`** | `1,559,872` | `2fe710e074579b6b3e535f51d57327f6ab455c563f62fdb886f8ee9b6eb506c0` | `ce1b9503-13c1-4458-a996-decff463e753` | `594` | `3,854` | `3,980` | Windows-specific OS bindings: Win32 low-level keyboard/mouse hooks (`IShortcutMonitor`, `IPointerInput`), DXGI/WGC screen capture (`IScreenCaptureService`), WASAPI/XAudio2 audio (`IMicrophoneCapture`, `IAudioOutput`), and UIAutomation text target inspection/insertion (`IDictationTextTarget`). |
| **`HeyClicky.Drawing.dll`** | `1,473,856` | `0dc190a5c55c65e648800cc047a55468648a49eca756edacd056d7db3dc74117` | `05955e76-1d9f-42b1-b013-44dc42b11ec7` | `293` | `1,588` | `2,201` | 2D screen drawing overlay (`IScreenDrawingCanvas`), ClearScript V8 JavaScript sandbox host (`clicky-draw-engine.js`, `clicky-draw-native.js`, `clicky-draw-pieces.js`), Direct2D / Win2D geometry, procedural audio synthesis for ink strokes, and step-synced caption rendering. |
| **`HeyClicky.Drawing3D.dll`** | `798,016` | `683729abbe2dfab180d30f0f7d921aab9a3d30b7d889bbf9702c8d1456920397` | `8056ad8b-145a-4969-86f3-d5c85bdf2804` | `116` | `888` | `761` | 3D figure and scene rendering (`figure()`, `scene3d()`) powered by ClearScript V8 (`clicky-three.js`, `clicky-three-parts.js`) + Vortice Direct3D11 / DirectComposition with compiled HLSL shaders (`davinci_*`, `stage_*`, `studio_*`). |
| **`HeyClicky.Contracts.dll`** | `298,304` | `f5e75e61bcc6d52513f5092b267a51e57d3d35539cc567ca30b42a23a468ee46` | `56570812-4991-4be5-875e-df205ceb9334` | `104` (55 top-level) | `311` | `1,281` | Shared cross-assembly interfaces, immutable records, enums, Win32 window rules (`FullscreenGuard`, `NonRudeWindow`, `ShellOpen`, `ShellOpenRules`), logging (`Log`), and path resolution (`AppPaths`, `ClickyEndpoints`, `ClientIdentity`). |

### Namespace Breakdown of `HeyClicky.Core.dll` (506 Top-Level Types)

| Namespace | Top-Level Types | Key Classes & Responsibilities |
| :--- | ---: | :--- |
| `HeyClicky.Core.Agents` | `124` | `ClickyCatalog`, `ClickyDefinition`, `ClickyJobRecord`, `ClickyJobsStore`, `ClickyAppearance`, `AgentPromptBuilder`, `AgentWorkspace`, `SkillCatalog`, `StarterCards` |
| `HeyClicky.Core.Voice` | `41` | `CompanionSession`, `GeminiTranscriber`, `DeepgramTranscriber`, `RealtimeMouth`, `MouthConfig`, `GeminiMouthTurns`, `VoiceCatalog`, `DrawingCaptionTiming`, `TimeStretcher`, `Pcm16MonoResampler`, `HighlightCompositor` |
| `HeyClicky.Core.Worker` | `50` | `WorkerClient`, `PlanSnapshot`, `AppConfig`, `RegisteredDevice`, `UserMemoryResponse`, `StarterPlanRequest`, `ImageSearchRequest`, `WebSearchRequest`, `CheckoutRequest`, `RealtimeSessionRequest`, `DeepgramTokenResponse`, `WorkerHttpException` |
| `HeyClicky.Core.Proactive` | `47` | `ProactiveAgentsClient`, `AgentBackendClient`, `UpcomingMeetingsClient`, `ProactiveStore`, `ProactiveSuggestionCard`, `MorningGreetingPolicy`, `IntegrationSuggestionRules`, `MeetingCountdownRules` |
| `HeyClicky.Core.Onboarding` | `40` | `OnboardingController`, `OnboardingState`, `OnboardingInterview`, `TourStep`, `PermissionStep`, `WelcomeCard` |
| `HeyClicky.Core.Brain` | `29` | `BrainSession`, `BrainClient`, `BrainSseParser`, `BrainReplyAccumulator`, `BrainReply`, `BrainToolCall`, `BrainComputerCall`, `BrainRequestCompression`, `BrainCompactionPolicy`, `SayLines`, `ProgramText` |
| `HeyClicky.Core.Notch` | `28` | `NotchPresenter`, `NotchState`, `NotchQueue`, `NotchSoundPlayer`, `NotchLiveEventQueue` |
| `HeyClicky.Core.Tools` | `23` | `WorkerTools`, `WorkerPost`, `ToolArgs`, `LocalTools`, `AppleMusicTool`, `SpotifyTool` |
| `HeyClicky.Core.Drive` | `22` | `DriveController`, `DriveSession`, `DriveActionExecutor`, `DriveLease`, `DriveSafetyCheck` (Computer-use automation) |
| `HeyClicky.Core.Settings` | `20` | `SettingsStore`, `SettingsKeys`, `VoiceSettings`, `DictationSettings`, `ShortcutSettings` |
| `HeyClicky.Core.Browser` | `19` | `BrowserBridge`, `BrowserHostInstaller`, `BrowserTabContext`, `BrowserExtensionManifest` |
| `HeyClicky.Core.Dictation` | `16` | `DictationController`, `DictationTranscriptionClient`, `DictationCleanupContext`, `DictationVocabulary`, `DictationCorrectionDetector`, `DictationHotkeyStateMachine`, `DictationTranscriptArchive` |
| `HeyClicky.Core.Connectors` | `14` | `McpOAuth`, `McpToolCatalog`, `McpProbe`, `LoopbackCallbackServer`, `CodexStoredSignIns` |
| `HeyClicky.Core.Auth` | `10` | `SupabaseAuthService`, `AuthTokenStore`, `StoredAuthSession`, `SignInCallbackGuard`, `AuthDeepLink`, `AtomicFile` |
| `HeyClicky.Core.Telemetry` | `9` | `PostHogClient`, `SentryInitializer`, `TelemetryEvents`, `MemoryMonitor` |
| `HeyClicky.Core.Billing` | `6` | `BillingController`, `PaywallState`, `CreditTracker`, `UpgradeFlow` |
| `HeyClicky.Core.Documents` | `2` | `DocumentReader`, `DocumentAccessGuard` |
| `HeyClicky.Core.Configuration` | `2` | `AppBundleConfiguration`, `TelemetryConfiguration` |
| `HeyClicky.Core.Net` | `2` | `ClickyHttp`, `ClickyClientHeadersHandler` |
| `HeyClicky.Core.Identity` | `1` | `DeviceIdentityProvider` |
| `HeyClicky.Core.Ink` | `1` | `HighlightGestureAnalyzer` |

---

## 3. Sidecar Executables, Native DLLs & Bundled Assets

### 3.1 Sidecar & Helper Executables (`%LOCALAPPDATA%\HeyClickyApp\current\`)

| Relative Path | Size (Bytes) | Role & Architecture |
| :--- | ---: | :--- |
| `HarnessRuntime\clicky-harness.exe` | `88,566,080` | Self-contained background agent harness executable (Codex / Rust-based agent runtime). Invoked by `HeyClicky.Agents.dll` to execute multi-step shell, Python (`uv`), file, MCP, and browser tasks inside isolated job directories. |
| `ClickyBrowserHost\ClickyBrowserHost.exe` | `4,685,632` | Native `.NET 9.0` Chromium Native Messaging host registered via `com.heyclicky.browser.json`. Bridges the Chrome/Edge extension (`ClickyBrowserExtensionID.txt`) to HeyClicky over named pipes / local IPC (`%LOCALAPPDATA%\HeyClicky\BrowserBridge\`). |
| `HeyClicky-Update.exe` | `4,315,456` | **Velopack** standalone updater binary (`SHA256: 390e2fb0bc190a4a756eccbed074f1b08b562223a3035f6ceac2ce46d626229f`). |
| `RestartAgent.exe` | `77,624` | Lightweight Win32 helper process used to cleanly restart HeyClicky after updates or permission changes. |
| `createdump.exe` | `71,504` | Standard `.NET 9.0` crash dump generator. |

### 3.2 Key Native & Third-Party Libraries (`%LOCALAPPDATA%\HeyClickyApp\current\`)

> [!NOTE]
> **Persistence Engine Note (`e_sqlite3.dll` vs. Atomic JSON/JSONL)**: Unlike some Electron/mobile apps, HeyClicky v1.1.5 **does not ship or use `e_sqlite3.dll` or SQLite** in `current\`. Instead, all local state is persisted through `HeyClicky.Core.Auth.AtomicFile` using atomic write-and-rename JSON/JSONL files in `%LOCALAPPDATA%\HeyClicky\` (`settings.json`, `app-config.json`, `clickys.json`, `proactive.json`, `OneBrain\threads\*.json`, `DictationTranscripts\*.jsonl`, and DPAPI-encrypted `auth.bin`).

| Category | DLL Files | Size (Bytes) | Purpose in HeyClicky v1.1.5 |
| :--- | :--- | ---: | :--- |
| **JavaScript Engine (V8)** | `ClearScriptV8.win-x64.dll`<br>`ClearScript.V8.ICUData.dll`<br>`ClearScript.V8.dll`<br>`ClearScript.Core.dll` | `29,886,976`<br>`10,831,872`<br>`1,354,752`<br>`1,341,952` | Hosts embedded V8 isolates inside `HeyClicky.Drawing.dll` and `HeyClicky.Drawing3D.dll` to execute streamed LLM drawing code (`clicky-draw-engine.js`, `clicky-three.js`) at 60+ FPS without a browser DOM. |
| **Local AI / ONNX / NPU** | `onnxruntime.dll`<br>`DirectML.dll`<br>`Microsoft.Asg.SemanticIndex.AiFabric.Compatibility.dll`<br>`PerceptiveStreaming.dll`<br>`Microsoft.ML.OnnxRuntime.dll`<br>`NPUDetect.dll`<br>`Microsoft.Windows.AI.*.dll` | `21,659,192`<br>`18,526,776`<br>`3,952,184`<br>`1,763,392`<br>`810,040`<br>`286,784`<br>`~450,000` | Local ONNX Runtime + DirectML GPU/NPU acceleration, Windows AI Fabric compatibility, NPU hardware detection, and audio/perceptive streaming helpers. |
| **Graphics, 2D/3D & Audio** | `Microsoft.WinUI.dll`<br>`WinUIEdit.dll`<br>`d3dcompiler_47.dll`<br>`Microsoft.Graphics.Canvas.dll`<br>`Vortice.Direct3D11.dll`<br>`Vortice.Direct2D1.dll`<br>`Vortice.DirectComposition.dll`<br>`Vortice.DXGI.dll`<br>`Vortice.XAudio2.dll`<br>`Vortice.D3DCompiler.dll`<br>`Vortice.Mathematics.dll`<br>`WebView2Loader.dll` | `16,784,968`<br>`3,424,304`<br>`4,956,808`<br>`1,661,488`<br>`930,816`<br>`793,088`<br>`355,328`<br>`278,528`<br>`132,608`<br>`62,464`<br>`198,656`<br>`160,296` | WinUI 3 controls, Win2D immediate-mode 2D vector drawing (`Microsoft.Graphics.Canvas`), Vortice Direct3D 11 + DirectComposition transparent per-pixel overlay swapchains, HLSL shader compilation (`d3dcompiler_47.dll`), XAudio2 low-latency PCM audio output (`Vortice.XAudio2.dll`), and WebView2 host loader. |
| **Diagnostics, Updates & Core** | `Sentry.dll`<br>`Newtonsoft.Json.dll`<br>`Velopack.dll`<br>`CommunityToolkit.Mvvm.dll` | `2,045,952`<br>`1,852,960`<br>`712,192`<br>`391,824` | Crash/error telemetry (`Sentry.dll`), JSON serialization alongside `System.Text.Json`, Velopack auto-updates (`Velopack.dll`), and MVVM observables. |
| **.NET 9.0 Self-Contained Runtime** | `coreclr.dll`, `System.Private.CoreLib.dll`, `clrjit.dll`, etc. | `~65,000,000` | Full self-contained `.NET 9.0` (`v9.0`) runtime shipped in `current\` so the user needs no system-wide .NET installation. |

### 3.3 Bundled Assets (`Assets\` and `BundledResources\`)

#### `C:\Users\ubaid\AppData\Local\HeyClickyApp\current\BundledResources\`
| File / Subfolder | Size (Bytes) | Description |
| :--- | ---: | :--- |
| `clicky-model-instructions.md` | `3,962` | Ground-truth system prompt injected into background Codex/H1 agents (`clicky-harness.exe`). |
| `skills\` & `integration-skills\` | `Directories` | Bundled core skills (`clicky-browser`, `vercel-deploy`) and Composio integration skills (`gmail`, `linear`, `jira`, `hubspot`, `salesforce`, `clickup`, `dropbox`, `googledocs`, `googletasks`, `microsoft_teams`, `outlook`). |

#### `C:\Users\ubaid\AppData\Local\HeyClickyApp\current\Assets\Engine\` (V8 2D & 3D JavaScript Runtime)
| File Name | Size (Bytes) | Description |
| :--- | ---: | :--- |
| `clicky-three.js` | `429,525` | Bundled Three.js (r186) + custom 3D scene runtime loaded into ClearScript V8 by `HeyClicky.Drawing3D.dll`. |
| `clicky-three-parts.js` | `41,390` | Reusable 3D procedural primitives and 14 figure part recipes (`figure()`, `scene3d()`, `Assets\Engine\parts\*.js`) for V8 3D drawing. |
| `clicky-draw-engine.js` | `259,913` | Core 2D screen drawing engine (`point`/`spot`, `box`, `arrow`, `label`, `badge`, `paper`, `sticky`, `receipt`, `clipping`, `legend`, `graph`, `timeline`, `say`, animations, layout solver) executed in ClearScript V8. |
| `clicky-draw-pieces.js` | `66,086` (`66,985` with Kursor custom pieces) | Pre-built 2D visual components, diagrams, and icon recipes for `clicky-draw-engine.js`. |
| `clicky-draw-native.js` | `50,405` | Headless SVG DOM shim and JS-to-.NET bridge bindings connecting `clicky-draw-engine.js` to `HeyClicky.Drawing.dll` native Direct2D/Win2D primitives. |

#### `C:\Users\ubaid\AppData\Local\HeyClickyApp\current\Assets\` (9 Subfolders)
| Subfolder / Files | Count / Size | Description |
| :--- | :--- | :--- |
| `AppIcon.ico` / `AppIcon.png` | `151,575 B` / `23,774 B` | Application icon resources. |
| `TrayIcon.ico` / `TrayIcon.png` / `TrayIcon@2x.png` | `15,826 B` total | System tray icon assets. |
| `Assets\Audio\*.wav` | `1 file` | Ambient/onboarding audio asset. |
| `Assets\Earcons\*.wav` | `17 files` | UI & voice interaction earcons (`hatching.wav`, PTT cues, dictation cues, notification chimes). |
| `Assets\Engine\*.js` | `5 files` (+ `parts\` dir) | V8 2D/3D procedural drawing engines detailed above. |
| `Assets\Fonts\*.ttf` | `4 files` | Custom drawing typography (`ClickyDraw-PatrickHand.ttf`, `ClickyDraw-Barlow-Regular.ttf`, `ClickyDraw-Barlow-Medium.ttf`, `ClickyDraw-Barlow-SemiBold.ttf`). |
| `Assets\Logos\*.png` | `34 files` | Official brand & integration logos (`gmail`, `googlecalendar`, `notion`, `linear`, `github`, `slack`, `spotify`, `discord`, etc.). |
| `Assets\Referral\*.png` | `3 files` | Sample avatar assets (`referral-sample-*.png`) for the referral estimator card. |
| `Assets\Sounds\*.wav` | `63 files` | Procedural drawing sound effects (pen strokes, paper rustles, pops, swooshes, chimes) played by `SoundPlayer` via XAudio2. |
| `Assets\Video\*.mp4` | `3 files` | Onboarding and paywall video headers (`onboarding-intro-v2-windows.mp4`, `paywall-intro-v2.mp4`). |
| `Assets\VoicePreviews\*.mp3` | `40 files` | Instant local voice preview clips (`gemini-voice-preview-*.mp3` and `realtime-voice-preview-*.mp3`). |

---

## 4. Runtime User Data Directory Layout (`%LOCALAPPDATA%\HeyClicky\`)

By default, `HeyClicky.AppPaths.Root` resolves to `%LOCALAPPDATA%\HeyClicky` (`C:\Users\ubaid\AppData\Local\HeyClicky`), overridable via the `HEYCLICKY_DATA_DIR` environment variable.

```
C:\Users\ubaid\AppData\Local\HeyClicky\
├── app-config.json                 # Cached remote links from GET /config (featureRequest, communityReddit, communityWhatsApp, changelog)
├── settings.json                   # User preferences (38 keys: cursor color, squad member, voice_identifier, speech_speed, hotkeys, dictation, proactive)
├── auth.bin                        # DPAPI-encrypted (ProtectedData) Supabase session tokens (access_token, refresh_token, expires_at, user profile)
├── endpoints.json                  # [Optional] Developer override file for workerBaseUrl, supabaseUrl, supabaseAnonKey
├── use-production-backend.flag     # [Optional] Empty marker file forcing production Worker & Supabase even in local dev builds
├── device-registration.json        # Fingerprint stamp ("<deviceId>|<workerUrl>|0|<osBuild>") & registered_at_epoch_seconds
├── clickys.json                    # Custom Clicky definitions, avatar Appearances, TaskOwners, and StarterCards
├── proactive.json                  # Per-account proactive suggestion cards, queue, slots, morning greeting timestamps, and snoozed integrations
├── voice-usage.json                # Local voice/turn usage counters
├── Clicky.log                      # Active rolling diagnostic log written by HeyClicky.Log
├── logs\                           # Archived diagnostic and crash logs
├── OneBrain\                       # Persistent BrainSession state
│   ├── stats.json                  # Cumulative turn count, token stats, and latency metrics
│   └── threads\                    # Active and recent conversation threads (*.json, retained up to SavedThreadRetention)
│       └── <sessionId>.json        # Serialized thread items, user_layer, user_layer_signature, last_input_tokens, baseline_input_tokens
├── HeyClickyHistory\               # User-visible interaction history & saved screenshots
├── DictationTranscripts\           # Local archive of system-wide dictation sessions (DictationTranscriptArchive)
├── VoicePreviews\                  # Cached audio previews for VoiceCatalog voices (VoicePreviewStore)
├── billing\                        # Cached PlanSnapshot and subscription state
├── BrowserBridge\                  # Named-pipe / file IPC state between HeyClicky and ClickyBrowserHost.exe
├── BrowserHost\                    # Generated Chromium Native Messaging manifest (com.heyclicky.browser.json)
├── shadercache\                    # Compiled Direct3D11 HLSL bytecode blobs (*.cso) for HeyClicky.Drawing3D.dll
│   ├── davinci_ps_*.cso / davinci_vs_*.cso
│   ├── stage_ps_*.cso   / stage_vs_*.cso
│   └── studio_ps_*.cso  / studio_vs_*.cso
├── cache\                          # General HTTP/image/asset cache
├── agents\                         # Agent orchestration state & bundled harness environments
│   ├── defaults.json               # Job registry ("clicky.jobs.v1", "clicky.clickys.runningTurns.v1", "clicky.notePaperColors.v1")
│   ├── clickys.json                # Agent-scoped Clicky metadata
│   └── AgentHarness\               # Extracted runtime environments for clicky-harness.exe
│       ├── h1-local\               # Local execution profile
│       └── h1-mac\                 # Cross-platform job compatibility profile (skills, instructions, MCP wiring)
├── codex-home\                     # CODEX_HOME directory passed to clicky-harness.exe (auth, skills, config, sessions)
└── Clickys\                        # Working directories for background agent jobs
    ├── WORK.md                     # One-line-per-job summary index read by agents for historical context
    └── jobs\                       # Individual job workspaces (<slug>-<10charId>\)
        ├── research-buddy-04d7268442\
        │   ├── output\             # Final user-facing deliverables (e.g. PDFs, HTML, spreadsheets)
        │   ├── tmp\                # Scratch files and local Python virtualenvs (tmp/pylib)
        │   └── .clicky\memory.md   # Compaction-survivable job memory
        └── shippy-7832deea28\
            └── output\welcome.html
```

### 4.1 Key Runtime File Schemas

#### `settings.json` (`C:\Users\ubaid\AppData\Local\HeyClicky\settings.json`)
Managed by `HeyClicky.Core.Settings.SettingsStore`. Observed production keys:
```json
{
  "cursor_color_hex": "#3B82F6",
  "squad_member_id": "classic",
  "voice_identifier": "marin",
  "speech_speed": 1.07,
  "voice_language_code": "en",
  "voice_language_codes": ["en"],
  "dictation_language_code": "en",
  "dictation_custom_words": ["HeyClicky", "Ubaid"],
  "dictation_custom_words_seeded_v1": true,
  "dictation_account_name_seeded_v1": true,
  "shake_to_detach_enabled": true,
  "sound_effects_enabled": true,
  "voice_replies_muted": false,
  "launch_at_login": true,
  "onboarding_completed": true,
  "onboarding_step": 99,
  "proactive_enabled": true,
  "meeting_countdown_enabled": true,
  "one_brain_session_id": "..."
}
```

#### `app-config.json` (`C:\Users\ubaid\AppData\Local\HeyClicky\app-config.json`)
Fetched via `WorkerClient.GetAppConfigAsync` (`GET /config`):
```json
{
  "links": {
    "featureRequest": "https://heyclicky.featurebase.app",
    "communityReddit": "https://www.reddit.com/r/heyclicky/",
    "communityWhatsApp": "https://chat.whatsapp.com/...",
    "changelog": "https://www.heyclicky.com/changelog"
  }
}
```

#### `endpoints.json` & Backend Override Resolution (`AppBundleConfiguration`)
`HeyClicky.Core.Configuration.AppBundleConfiguration` resolves `ClickyEndpoints` (`WorkerBaseUrl`, `SupabaseUrl`, `SupabaseAnonKey`) in the following strict priority order:
1. **Default Constants**:
   - `WorkerBaseUrl`: `https://api.heyclicky.com`
   - `SupabaseUrl`: `https://mrpvynsdsnimuisyhkow.supabase.co`
   - `SupabaseAnonKey`: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1ycHZ5bnNkc25pbXVpc3loa293Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzYyODAxNzgsImV4cCI6MjA5MTg1NjE3OH0.qf1-em-OB-OtyX4WTuGnSi28djq-5T0k0whKvcTtTAY`
2. **Assembly Metadata Attributes**: Checks `[assembly: AssemblyMetadata("HeyClicky.WorkerBaseUrl", ...)]`, `"HeyClicky.SupabaseUrl"`, `"HeyClicky.SupabaseAnonKey"`.
3. **`%LOCALAPPDATA%\HeyClicky\endpoints.json`**: If present, reads JSON properties `workerBaseUrl`, `supabaseUrl`, and `supabaseAnonKey`.
4. **`HEYCLICKY_WORKER_BASE_URL` Environment Variable**: Applied **only** if the URI host is a loopback address (`localhost`, `127.0.0.1`, or `[::1]`). Non-loopback URLs log `"HEYCLICKY_WORKER_BASE_URL ignored (loopback URLs only)"`.
5. **`%LOCALAPPDATA%\HeyClicky\use-production-backend.flag`**: If `UseProductionBackendRequested` is true (flag file exists), `ApplyProductionBackendOverride` overrides any local development endpoints back to the production `https://api.heyclicky.com` and production Supabase URL/key.
