# Kursor (HeyClicky v1.1.5 Clone) — Master Knowledge Base & Index

> **IMPORTANT FOR AI AGENTS & FUTURE SESSIONS (POST-COMPACTION):**
> Read this index first whenever resuming work or spawning subagents. All reverse-engineered specifications, API contracts, voice protocols, V8/Direct3D11 drawing pipelines, Win32 overlay architectures, agent harnesses, and decompiled C# source trees are cataloged here.

---

## 1. Core Documentation Index (`C:\dev\heyclicky-clone\docs\`)

| Doc # | File | Contents & When to Read |
| :--- | :--- | :--- |
| **01** | [`01-architecture-overview.md`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md) | **System Architecture & Inventory**: Evolution from 7-month-old `farzaa/clicky` to `HeyClicky v1.1.5`; exact byte sizes, SHA-256 hashes, and TypeDef/MethodDef counts for all 7 `.NET 9.0` assemblies; sidecars (`clicky-harness.exe`, `ClickyBrowserHost.exe`); native DLLs (`ClearScriptV8`, `onnxruntime`, `DirectML`, `Vortice`); bundled JS/shader assets; and full `%LOCALAPPDATA%\HeyClicky\` runtime file schemas. |
| **02** | [`02-api-contracts-and-worker-spec.md`](file:///C:/dev/heyclicky-clone/docs/02-api-contracts-and-worker-spec.md) | **API Client & Backend Specification**: Complete HTTP route matrix for `https://api.heyclicky.com` & Supabase Auth; `endpoints.json` / `HEYCLICKY_WORKER_BASE_URL` local backend override; `ClickyClientHeadersHandler` headers; `ClickyHttp.PrewarmAll()` hosts; all DTO schemas in `HeyClicky.Contracts.dll`; **OneBrain** SSE protocol (`/brain/turn`, `SayLines`, `DrawingAction`, `BrainToolCall`, `BrainComputerCall`, 8 KB gzip compression, 45k/75k compaction); and verbatim system prompts. |
| **03** | [`03-voice-ears-mouth-dictation.md`](file:///C:/dev/heyclicky-clone/docs/03-voice-ears-mouth-dictation.md) | **Voice ("Ears" & "Mouth") & AI Dictation Pipeline**: `GeminiTranscriber` (`BidiGenerateContentConstrained` WS + `/v2/dictation/gemini-token`) and `DeepgramTranscriber` (`/v1/listen` WS + `/deepgram/token`) warm-socket pooling & `keyterm` injection; `RealtimeMouth` (`gpt-realtime-2.1-mini` out-of-band `response.create` verbatim reader + `gemini-3.8-flash-lite-tts`), `TimeStretcher`, and `DrawingCaptionTiming` lockstep stroke release; `DictationController` hotkey state machine, UIA focus capture, `/v2/dictation/cleanup`, and Levenshtein auto-learning. |
| **04** | [`04-drawing-2d-3d-and-ink-engine.md`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md) | **2D/3D Screen Drawing & Persistent Ink Engine**: `HeyClicky.Drawing.dll` (`DrawingThread`, `ScreenDrawingCanvas`, `SystemComposition` D3D11/D2D1/DirectComposition interop, `NativeEngine` ClearScript V8 host bridge, `ScreenshotSampler` luminance/contrast placement, `ScreenPainter`, `SoundPlayer` XAudio2, `LessonVideoRecorder`); `HeyClicky.Drawing3D.dll` (`ClickyThreeKernel` V8 `clicky-three.js` r186 + 14 procedural 3D part recipes, Surface Nets metaballs, `Scene3D`, `FigureRenderer` HLSL shaders for `StageLook.Illustrated` & `StageLook.Studio`); and `HeyClicky.Core.Ink` / `HeyClicky.App.Ink` interactive notebook & widgets. |
| **05** | [`05-platform-win32-and-ui-shell.md`](file:///C:/dev/heyclicky-clone/docs/05-platform-win32-and-ui-shell.md) | **Win32 Platform Layer & WinUI 3 Shell**: `HeyClicky.Platform.dll` (`OverlayHost` click-through virtual-screen window, `CursorOverlay` spring physics & flight paths, `LowLevelInputHub`, `ClickyScreenActuator`, `DesktopDuplicationSource` + `GraphicsCaptureSource` GPU screen capture, pure WASAPI COM audio engine, `UIAutomationHelpers` + `PageContext`, `QuietContext`, `CaptureExclusion`); `HeyClicky.dll` (`NotchView`, `ArcDock` top-arc card fan, `HeyClickyWindow`, `Onboarding`, `TutorialWindow`, 12-page `SettingsWindow`, `ClickyTakeOverController`, `BillingUi`). |
| **06** | [`06-agents-proactive-and-memory.md`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md) | **Autonomous Agents, Proactive Intelligence & Memory**: `HeyClicky.Agents.dll` (`AgentService`, `AgentHarnessRunnerProcess` driving `HarnessRuntime\clicky-harness.exe`, `CodexSession`, `CodexHomeManager`, `WindowsInstructionAdapter`, 16 `AgentTools`, `ClickyJobStore`, `RoutineScheduler`); Composio MCP OAuth (`AgentIntegrationsApi`, RFC PKCE `McpOAuth`, Gmail/Calendar/Notion/Linear/Obsidian/Spotify); `ProactiveAgentsClient` & `ProactiveCoordinator`; and `OneBrain` local + cloud memory (`UserMemoryCache`, `AGENTS.md`, `<LEARNED>`). |
| **07** | [`07-kursor-roadmap.md`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md) | **Kursor Master Engineering Roadmap**: Recommended two-track strategy (Track A: Self-hosted TypeScript `kursor-worker` backend + Track B: Reconstructed `.NET 9.0 + WinUI 3` `Kursor.sln` desktop suite) and 7-phase implementation plan. |

---

## 2. Decompiled Reference Source Code (`C:\dev\heyclicky-clone\reference\decompiled\`)

All 7 managed `.NET 9.0` assemblies from `HeyClicky v1.1.5` have been decompiled via `ilspycmd -p` into structured C# projects (**2,098 `.cs` files total**):

| Assembly | Decompiled Project Directory | File Count | Purpose |
| :--- | :--- | :---: | :--- |
| `HeyClicky.Contracts.dll` | [`reference/decompiled/HeyClicky.Contracts`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Contracts) | 56 | Shared DTO records, enums, `ClickyEndpoints`, `IScreenDrawingCanvas`, IPC contracts |
| `HeyClicky.Core.dll` | [`reference/decompiled/HeyClicky.Core`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Core) | 565 | `WorkerClient`, `BrainClient`, `SupabaseAuthService`, `RealtimeMouth`, `GeminiTranscriber`, `DeepgramTranscriber`, `DictationTranscriptionClient`, `ProactiveAgentsClient`, `ClickyInkNoteStore`, `OneBrain` |
| `HeyClicky.Agents.dll` | [`reference/decompiled/HeyClicky.Agents`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Agents) | 123 | `AgentService`, `AgentHarnessRunnerProcess`, `CodexSession`, `RoutineScheduler`, `McpOAuth`, 16 `AgentTools` |
| `HeyClicky.Drawing.dll` | [`reference/decompiled/HeyClicky.Drawing`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Drawing) | 86 | ClearScript V8 2D drawing engine, `ScreenPainter`, `NativeEngine`, `HostBridge`, `ScreenshotSampler`, `SoundPlayer`, `LessonVideoRecorder` |
| `HeyClicky.Drawing3D.dll` | [`reference/decompiled/HeyClicky.Drawing3D`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Drawing3D) | 42 | ClearScript V8 `ClickyThreeKernel`, 14 procedural 3D primitives, Surface Nets metaballs, `Scene3D`, D3D11 `FigureRenderer` |
| `HeyClicky.Platform.dll` | [`reference/decompiled/HeyClicky.Platform`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.Platform) | 346 | Win32 P/Invoke & CsWin32 bindings, `OverlayHost`, `CursorOverlay`, `LowLevelInputHub`, DXGI/WGC screen capture, WASAPI audio, UIA |
| `HeyClicky.dll` | [`reference/decompiled/HeyClicky.App`](file:///C:/dev/heyclicky-clone/reference/decompiled/HeyClicky.App) | 880 | WinUI 3 desktop frontend (`NotchView`, `ArcDock`, `HeyClickyWindow`, `Ink` views, `DictationController`, `SettingsWindow`, `Onboarding`, `TakeOverOverlay`) |

---

## 3. Active Kursor Source Code (`C:\dev\heyclicky-clone\src\` & `dist\`)

### A. Kursor Backend (`src/backend/`) — All Backend Phases (1, 2, 3, 6) Complete (25/25 Tests Passing)
- **Entry Point**: [`src/backend/src/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts) (Node.js 24 + Hono + TypeScript on `http://127.0.0.1:8787`)
- **Database**: [`src/backend/src/db/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/db/index.ts) (`node:sqlite` at `src/backend/data/kursor.db`)
- **Middleware**: [`src/backend/src/middleware/gzip.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts) & [`src/backend/src/middleware/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts)
- **Routes**:
  - [`src/backend/src/routes/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts) (`/auth/v1/*` Supabase Auth Mock)
  - [`src/backend/src/routes/worker.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts) (`WorkerClient` routes + zero-key web/image search)
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts) (Deepgram, Gemini Live, OpenAI Realtime token minting + Groq Dictation cleanup & transcription)
  - [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts) (`OneBrain` `/brain/turn` SSE streaming via OpenRouter/OpenAI/Gemini/Groq, `/brain/compact`, `/brain/session-end`, `/brain/prewarm`)
  - [`src/backend/src/utils/screenLayout.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts) (Deterministic `1280×720` pixel table/grid detector, cell-center coordinate snapper, blank-line detector & calm placement analyzer)
  - [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts) (`/proactive/*`, `/agents/*`, `/agent/*` turn leases & notifications, `/connectors/*`, `/calendar/upcoming`, `/tools/*`)
- **Test Suites**: [`src/backend/src/test-phase1.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts) (18/18 passing) & [`src/backend/src/test-phase2.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase2.ts) (7/7 passing)

### B. Kursor `.NET 9.0` Desktop Solution (`src/desktop/Kursor.sln`) — All 7 Projects Compiling (0 Warnings, 0 Errors)
- **Solution**: [`src/desktop/Kursor.sln`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.sln) & [`src/desktop/Directory.Build.props`](file:///C:/dev/heyclicky-clone/src/desktop/Directory.Build.props)
- **Compiled Projects (2,102 C# files, 0 Errors)**:
  1. [`src/desktop/Kursor.Contracts/Kursor.Contracts.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Contracts/Kursor.Contracts.csproj) (56 files)
  2. [`src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/Kursor.Drawing3D.csproj) (42 files)
  3. [`src/desktop/Kursor.Drawing/Kursor.Drawing.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Kursor.Drawing.csproj) (86 files)
  4. [`src/desktop/Kursor.Platform/Kursor.Platform.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/Kursor.Platform.csproj) (346 files)
  5. [`src/desktop/Kursor.Core/Kursor.Core.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/Kursor.Core.csproj) (565 files)
  6. [`src/desktop/Kursor.Agents/Kursor.Agents.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/Kursor.Agents.csproj) (123 files)
  7. [`src/desktop/Kursor.App/Kursor.App.csproj`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/Kursor.App.csproj) (884 files)

### C. Packaged Distribution, Multi-Device Setup & Autostart Scripts
- **Project Documentation & Git Ignore**: [`README.md`](file:///C:/dev/heyclicky-clone/README.md) & [`.gitignore`](file:///C:/dev/heyclicky-clone/.gitignore)
- **Runnable Distribution**: [`dist/kursor-desktop/`](file:///C:/dev/heyclicky-clone/dist/kursor-desktop)
- **Build & Stage Script**: [`scripts/build-dist.ps1`](file:///C:/dev/heyclicky-clone/scripts/build-dist.ps1)
- **One-Command Launcher**: [`start-kursor.ps1`](file:///C:/dev/heyclicky-clone/start-kursor.ps1) & [`start-kursor.cmd`](file:///C:/dev/heyclicky-clone/start-kursor.cmd)
- **One-Step New Device Setup**: [`setup-kursor.ps1`](file:///C:/dev/heyclicky-clone/setup-kursor.ps1) & [`setup-kursor.cmd`](file:///C:/dev/heyclicky-clone/setup-kursor.cmd)
- **Portable Release Packager**: [`scripts/package-portable.ps1`](file:///C:/dev/heyclicky-clone/scripts/package-portable.ps1) (builds `release/Kursor-Portable-win-x64.zip`)
- **Windows Autostart Integration**: [`scripts/install-autostart.ps1`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1), [`scripts/uninstall-autostart.ps1`](file:///C:/dev/heyclicky-clone/scripts/uninstall-autostart.ps1), & [`scripts/start-kursor-hidden.vbs`](file:///C:/dev/heyclicky-clone/scripts/start-kursor-hidden.vbs)

---

## 4. Key Workspace & System Paths

- **Workspace Root**: [`C:/dev/heyclicky-clone`](file:///C:/dev/heyclicky-clone)
- **Original Installer**: [`C:/dev/heyclicky-clone/executable-file/HeyClicky-Setup.exe`](file:///C:/dev/heyclicky-clone/executable-file/HeyClicky-Setup.exe)
- **Legacy Open-Source Precursor Repo (7–8 months old)**: [`C:/dev/heyclicky-clone/services/clicky`](file:///C:/dev/heyclicky-clone/services/clicky)
- **REA Reverse Engineering Tooling**: [`C:/dev/heyclicky-clone/services/rea`](file:///C:/dev/heyclicky-clone/services/rea)
- **Installed HeyClicky v1.1.5 Binaries & Assets**: [`C:/Users/ubaid/AppData/Local/HeyClickyApp/current`](file:///C:/Users/ubaid/AppData/Local/HeyClickyApp/current)
  - Bundled V8 Drawing & 3D Scripts: [`C:/Users/ubaid/AppData/Local/HeyClickyApp/current/Assets/Engine`](file:///C:/Users/ubaid/AppData/Local/HeyClickyApp/current/Assets/Engine)
  - Bundled Agent Harness Sidecar: [`C:/Users/ubaid/AppData/Local/HeyClickyApp/current/HarnessRuntime`](file:///C:/Users/ubaid/AppData/Local/HeyClickyApp/current/HarnessRuntime)
- **Installed HeyClicky Runtime Data & Prompts**: [`C:/Users/ubaid/AppData/Local/HeyClicky`](file:///C:/Users/ubaid/AppData/Local/HeyClicky)
  - Cached Server Config & Prompts: [`C:/Users/ubaid/AppData/Local/HeyClicky/app-config.json`](file:///C:/Users/ubaid/AppData/Local/HeyClicky/app-config.json)
  - Model Instructions: [`C:/Users/ubaid/AppData/Local/HeyClicky/codex-home/clicky-model-instructions.md`](file:///C:/Users/ubaid/AppData/Local/HeyClicky/codex-home/clicky-model-instructions.md)
