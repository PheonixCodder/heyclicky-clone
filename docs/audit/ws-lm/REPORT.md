# WS-L & WS-M Audit Report — Security, Privacy, Performance, Reliability & Crash Recovery

**Workstreams:**
- **WS-L:** Security, Privacy & Data Handling (Cross-Cutting Threat Model)
- **WS-M:** Performance, Reliability, Resources & Crash Recovery
- **Owner:** `@ws-lm-sec-perf`
- **Audit Date:** 2026-11-13
- **Target Repository:** `C:\dev\heyclicky-clone`

---

## 1. Executive Summary

We conducted a full-spectrum security, privacy, performance, resource-leak, and crash-recovery audit of **Kursor** across both the TypeScript/Hono backend (`src/backend/`) and the 7-project C#/.NET 9.0 desktop application (`src/desktop/` + `dist/kursor-desktop/`), backed by live localhost exploit PoCs, SQLite/filesystem forensic inspection, 215-turn sustained stress/soak testing, and process kill/recovery testing.

### Findings Summary (`11` Confirmed Findings)

| ID | Workstream | Severity | Confidence | Status | Title |
|---|---|---|---|---|---|
| `AUD-L-001` | WS-L | **Critical** | Confirmed | `FAIL` | Wildcard CORS (`*`) Combined with Zero-Auth Default User Middleware Exposes Master API Keys and User Data to Any Website |
| `AUD-L-002` | WS-L | **High** | Confirmed | `FAIL` | Unbounded Synchronous `gunzipSync` in `gzipMiddleware` Enables Local/Cross-Origin Decompression Bomb DoS |
| `AUD-L-003` | WS-L | **High** | Confirmed | `FAIL` | Open Redirect with Session Tokens on `GET /auth/v1/authorize` and Unconditional CSRF State Bypass in `SignInCallbackGuard` |
| `AUD-L-004` | WS-L | **High** | Confirmed | `FAIL` | Vendor `HeyClickyApp` Binary Collision on Shared SingleInstance Mutex/MMF, `heyclicky://` Protocol Handler, Native Messaging Host, and Process Check in `start-kursor.ps1` |
| `AUD-L-005` | WS-L | **Medium** | Confirmed | `FAIL` | `seed-local-auth.ps1` Writes PascalCase `endpoints.json` Ignored by Case-Sensitive `AppBundleConfiguration.ReadConfigured` and Leaves Redundant Plaintext `session.json` on Disk |
| `AUD-L-006` | WS-L | **Medium** | Confirmed | `FAIL` | Unstripped Base64 Screenshots Persisted in SQLite `threads.messages_json`, Plaintext Dictation & BrowserBridge Token Files on Disk, and Residual Vendor Telemetry Keys |
| `AUD-L-007` | WS-L | **Medium** | Confirmed | `FAIL` | Unsigned Rebuilt Managed Assemblies (`HeyClicky*.dll`) Loaded by Signed `HeyClicky.exe` Apphost and Unfiltered `SingleInstance` IPC Argument Forwarding |
| `AUD-M-001` | WS-M | **Critical** | Confirmed | `FAIL` | `/brain/turn` Client SSE Disconnect Throws Unhandled `ERR_INVALID_STATE: Controller is already closed` Crashing the Backend Process, and `force_local_fallback` Still Awaits Upstream LLM |
| `AUD-M-002` | WS-M | **Medium** | Confirmed | `FAIL` | Unbounded SQLite `sessions`/`threads` Table & WAL Growth and Unbounded In-Memory State Maps in `agents.ts` |
| `AUD-M-003` | WS-M | **Medium** | Confirmed | `FAIL` | Dual Autostart Registration in `install-autostart.ps1` (`HKCU\...\Run\Kursor` + `Startup\Kursor.lnk`) Triggers Concurrent Login Startup Race |
| `AUD-M-004` | WS-M | **Medium** | Confirmed | `FAIL` | Hardcoded Machine-Specific Paths (`C:\Users\ubaid\...` and `C:\Users\niles\...`), Missing `CodexRuntime\codex.exe`, and `dotenv.config({ override: true })` Env Override |

---

## 2. WS-L: Security, Privacy & Data Handling (Cross-Cutting Threat Model)

### 2.1 Local Attack Surface Matrix

| Surface | Implementation Location | Status | Technical Assessment & Proof |
|---|---|---|---|
| **Protocol Handler (`heyclicky://`)** | `ProtocolRegistration.cs:11-57`, `seed-local-auth.ps1:83-94`, `AppOptions.cs:23-71`, `AuthDeepLink.cs:11-118`, `SignInCallbackGuard.cs:54-83` | `FAIL` | **CLI Injection (`PASS`):** `ProtocolRegistration.EnsureRegistered` quotes `"%1"` (`"<exe>" "%1"`) and `AppOptions.Parse` only recognizes `--background`, `--autostart`, `--launched-at-login`, `--reset-all`, and `heyclicky://` URIs, preventing arbitrary CLI flag injection. **Auth Spoofing (`FAIL` — `AUD-L-003`):** `SignInCallbackGuard.Evaluate` (`lines 60-65`) checks `if (isLocalSupabaseBackend) return Verdict.AcceptLocalDevelopment;` BEFORE checking `pendingState` or `callbackState`, allowing any webpage triggering `heyclicky://auth-callback?access_token=...&referral_code=...` to overwrite the user's local auth session (`AuthTokenStore`) and fire `ReferralCodeReceived`. **Registry Hijack (`FAIL` — `AUD-L-004`):** Vendor `HeyClickyApp\current\HeyClicky.exe` overwrote `HKCU\Software\Classes\heyclicky\shell\open\command` on startup. |
| **Named Pipes & Shared Memory (`SingleInstance`)** | `SingleInstance.cs:23-418`, `Program.cs:28-46, 179-196` | `FAIL` | `SingleInstance.TryAcquire` creates mutex `Local\HeyClicky-<SID>-primary-mutex`, 4096-byte MMF `Local\HeyClicky-<SID>-primary` (`Magic = 1212371792`), and named pipe `HeyClicky-<SID>-<PID>-<Guid:N>` with default security descriptors (`AUD-L-007`), and collides with the vendor `HeyClicky.exe` installation (`AUD-L-004`). `ReadActivationPayloadAsync` (`lines 368-408`) accepts up to 1 MB of unfiltered newline-separated arguments. |
| **BrowserBridge & Loopback MCP (`ClickyBrowserBridge`, `LoopbackMcpServer`)** | `ClickyBrowserBridge.cs:56-299`, `LoopbackMcpServer.cs:40-177`, `LoopbackHttpServer.cs:23-189` | `FAIL` | `LoopbackHttpServer` binds strictly to `IPAddress.Loopback` (`127.0.0.1`) with a 1 MB request cap and `LoopbackMcpServer` validates `Authorization: Bearer <token>` with `CryptographicOperations.FixedTimeEquals`. However, `ClickyBrowserBridge.WriteConnectionFile` (`lines 256-267`) writes `{"port":51635,"token":"..."}` in plaintext to `%LOCALAPPDATA%\HeyClicky\BrowserBridge\connection.json` (`AUD-L-006`). |
| **Localhost Backend (`127.0.0.1:8787`)** | `src/backend/src/index.ts:19-21, 48-71`, `middleware/auth.ts:34-88`, `middleware/gzip.ts:12-35`, `routes/voice.ts:34-216`, `routes/auth.ts:100-126` | `FAIL` | Binds to `127.0.0.1:8787` (`PASS`), but mounts `app.use("*", cors())` (`Access-Control-Allow-Origin: *`) + zero-auth `clientHeadersAndAuthMiddleware` (`DEFAULT_USER_ID`), exposing raw `GEMINI_API_KEY`, `DEEPGRAM_API_KEY`, `OPENAI_API_KEY`, `/memory`, and `/agent/integrations` to cross-origin browser `fetch()` (`AUD-L-001`). `gzipMiddleware` uses unbounded synchronous `gunzipSync` (`AUD-L-002`). `GET /auth/v1/authorize` has an open redirect leaking `access_token` and `refresh_token` (`AUD-L-003`). |
| **MCP OAuth PKCE Loopback (`McpOAuth`)** | `McpOAuth.cs:19-410`, `LoopbackCallbackServer.cs:14-149` | `PASS` | Uses 32-byte CSPRNG S256 PKCE + 16-byte CSPRNG `state`, enforces `https://` for remote OAuth servers, binds `127.0.0.1:48611` (with ephemeral loopback fallback) only during authorization, and checks `callback.State == text2`. |
| **Custom MCP Connectors (`CustomConnectorStore`, `CommandResolver`)** | `CustomConnectorStore.cs:1-1450`, `CommandResolver.cs:11-118` | `PASS` | Encrypts custom connector secrets with DPAPI (`ProtectedData.Protect`, `DataProtectionScope.CurrentUser`, entropy `HeyClicky.Connectors.v1`) in `%LOCALAPPDATA%\HeyClicky\connectors.secrets` and executes user-configured stdio commands with `UseShellExecute = false` and `ArgumentList`. |
| **Binary Signing, DLL Search-Order & Autostart** | `dist/kursor-desktop/`, `scripts/start-kursor-hidden.vbs`, `scripts/install-autostart.ps1`, `start-kursor.ps1` | `FAIL` | `HeyClicky.exe` has a `Valid` vendor Authenticode signature while all 7 rebuilt `HeyClicky*.dll` assemblies are `NotSigned` (`AUD-L-007`). `install-autostart.ps1` registers duplicate autostart entries (`HKCU\...\Run\Kursor` + `Startup\Kursor.lnk`, `AUD-M-003`) alongside vendor `HKCU\...\Run\HeyClicky` (`AUD-L-004`). |

---

### 2.2 Complete Data & Privacy Inventory (Disk & Network)

All evidence is captured in [`docs/audit/evidence/ws-lm/privacy-data-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/privacy-data-inventory.json) and [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json).

| Data Category | Source / Producer | Local Disk Storage (Path & Encryption) | Network Egress / Destinations | Audit Verdict & Notes |
|---|---|---|---|---|
| **Full-Screen JPEGs & Region Crops** | `ScreenCaptureService.cs:25-179` (`DXGI` / `BitBlt` -> JPEG) | 1. **Desktop (`PASS`):** `BrainSession.cs:931` calls `WithoutScreenshots(thread)` before writing `%LOCALAPPDATA%\HeyClicky\OneBrain\<userId>\threads\<id>.json` (verified `hasInputImage: false` across all 4 files).<br>2. **Backend SQLite (`FAIL` — `AUD-L-006`):** `brain.ts:171-186` (`ensureSessionAndThread`) writes the full `incomingThread` including `data:image/jpeg;base64,...` into `src/backend/data/kursor.db` `threads.messages_json` in **plaintext** (verified by PoC 4: 50,605-byte base64 JPEG stored in `threads.messages_json`). | Sent in `POST http://127.0.0.1:8787/brain/turn` (`screenshot_jpeg_base64` and `thread[].content[].image_url`), which `brain.ts:586-930` forwards to **Groq** (`api.groq.com`), **OpenRouter** (`openrouter.ai`), **OpenAI** (`api.openai.com`), or **Gemini** (`generativelanguage.googleapis.com`). | `FAIL` (`AUD-L-006`, `AUD-M-002`): Strip `input_image` blocks before storing `threads.messages_json` in SQLite. |
| **Microphone Audio (PCM / WAV)** | `WasapiMicrophoneCapture.cs:1-265` (24 kHz / 16 kHz mono 16-bit PCM) | Not persisted to disk by default (buffered in `MemoryStream` in `DictationController.cs` and `voice.ts`). | Sent to `POST http://127.0.0.1:8787/v2/dictation/transcribe` or `/brain/ears/transcribe`, which forwards WAV audio to **Groq Whisper** (`api.groq.com/openai/v1/audio/transcriptions`), **Deepgram Nova-2** (`api.deepgram.com/v1/listen`), or **Gemini** (`generativelanguage.googleapis.com`). Direct streaming connects to `wss://api.deepgram.com` or `wss://generativelanguage.googleapis.com`. | `PASS` (disk); `FAIL` (`AUD-L-001` exposes `/deepgram/token` and `/v2/dictation/gemini-token` keys cross-origin). |
| **Dictated Text & Active App Name** | `DictationController.cs:612`, `DictationTranscriptArchive.cs:15-69` | Stored in **plaintext JSONL** at `%LOCALAPPDATA%\HeyClicky\DictationTranscripts\YYYY-MM-DD.jsonl` (`{"at":"...","text":"...","app":"..."}`) for 14 days (verified 4 `.jsonl` files / 8 dictated entries on disk). Also stored even when `focus.IsSecure` is `true` (password field). | Sent to `POST http://127.0.0.1:8787/v2/dictation/cleanup`, which forwards `raw_text` to **Groq** (`llama-3.3-70b-versatile`). | `FAIL` (`AUD-L-006`): Plaintext JSONL on disk without encryption or password-field exclusion. |
| **UIA Page Text & Browser URLs** | `UiaPageText.cs:1-215` (UI Automation document/value patterns & address bar URL) | Persisted inside `OneBrain\<userId>\threads\<id>.json` (`<user_layer>` block) and SQLite `threads.messages_json` in **plaintext**. | Sent in `POST http://127.0.0.1:8787/brain/turn` (`uia_page_text`, `active_window_title`, `browser_url`) and forwarded to configured upstream LLM (**Groq / OpenRouter / OpenAI / Gemini**). | `FAIL` (`AUD-L-006`): Persisted in plaintext in `OneBrain` and `kursor.db`. |
| **Clipboard Contents** | `TextInsertion.cs:86-174, 412-465` | Transiently snapshotted in memory (`CaptureClipboardSnapshot`) during `Ctrl+V` paste injection and restored after 560 ms; never written to disk by `TextInsertion`. | Selected text (`selected_text`) is included in `DictationCleanupContext` sent to `POST /v2/dictation/cleanup` (`127.0.0.1:8787`). | `PASS` (no disk persistence of clipboard snapshots). |
| **OneBrain Memory (`memory.md`)** | `BrainSession.cs`, `worker.ts:555-605`, `brain.ts` | Stored in **plaintext** in SQLite `src/backend/data/kursor.db` (`memory.markdown`) and `%LOCALAPPDATA%\HeyClicky\OneBrain\<userId>\*.jsonl`. | Returned by `GET /memory` / `GET /me/memory` (`127.0.0.1:8787`) and injected into upstream LLM system prompts during `/brain/turn`. | `FAIL` (`AUD-L-001`): Accessible cross-origin without auth via `GET http://127.0.0.1:8787/memory`. |
| **API Keys & Auth Secrets** | `src/backend/.env`, `AuthTokenStore.cs`, `CustomConnectorStore.cs`, `ClickyBrowserBridge.cs` | 1. `src/backend/.env`: **Plaintext** (`GROQ_API_KEY`, `GEMINI_API_KEY`, `DEEPGRAM_API_KEY`).<br>2. `%LOCALAPPDATA%\HeyClicky\auth.bin` & `auth.localdev.bin`: **DPAPI encrypted** (`CurrentUser`).<br>3. `%LOCALAPPDATA%\HeyClicky\connectors.secrets`: **DPAPI encrypted** (`CurrentUser`).<br>4. `%LOCALAPPDATA%\HeyClicky\session.json`: **Plaintext** JWT/refresh token written by `seed-local-auth.ps1` (`AUD-L-005`).<br>5. `%LOCALAPPDATA%\HeyClicky\BrowserBridge\connection.json`: **Plaintext** 64-hex-char MCP bearer token (`AUD-L-006`). | `POST /v2/dictation/gemini-token`, `POST /brain/ears/token`, `POST /deepgram/token`, `POST /realtime/token`, and `POST /agent/realtime/session` return raw master `GEMINI_API_KEY`, `DEEPGRAM_API_KEY`, or `OPENAI_API_KEY` in HTTP JSON responses (`AUD-L-001`). | `FAIL` (`AUD-L-001`, `AUD-L-005`, `AUD-L-006`). |
| **Telemetry, Analytics & Crash Reports** | `ClickyAnalytics.cs:1-315`, `CrashReporting.cs:1-275`, `Properties/AssemblyInfo.cs:15-17` | `%LOCALAPPDATA%\HeyClicky\logs\clicky-YYYYMMDD.log` (plaintext logs), `%LOCALAPPDATA%\HeyClicky\sentry\Sentry\...` (Sentry envelope cache), and SQLite `client_errors` table. | `ClickyAnalytics.IsLoopbackEndpoint` disables PostHog when `WorkerBaseUrl` is loopback (`127.0.0.1`), and `CrashReporting.Initialize` skips `SentrySdk.Init` when `IsLocalDevelopmentSupabaseUrl` is true. However, `AssemblyInfo.cs:15-17` still embeds production `HeyClicky.PostHogApiKey` and `HeyClicky.SentryDsn` (`SendDefaultPii = true`), and the colliding vendor binary (`AUD-L-004`) actively writes Sentry sessions. | `FAIL` (`AUD-L-004`, `AUD-L-006`). |

---

## 3. WS-M: Performance, Reliability, Resources & Crash Recovery

Full machine-readable benchmark, soak, and recovery outputs are saved in:
- [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json)
- [`docs/audit/evidence/ws-lm/soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json)
- [`tests/audit/ws-lm/test-perf-reliability.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-lm/test-perf-reliability.ts)

### 3.1 Process Resource Baselines (`node.exe`, `powershell.exe`, `HeyClicky.exe`)

| Process Role | Executable | PID (Observed) | Working Set RAM (MB) | Private Memory (MB) | Handles | Threads | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| **Backend Worker (`node.exe`)** | `C:\Program Files\nodejs\node.exe` (`tsx src/index.ts`) | `17448` | ~95–118 MB | ~82–105 MB | ~260–310 | ~11–13 | SQLite (`node:sqlite`) + Hono HTTP server on `127.0.0.1:8787` |
| **Warm SAPI TTS Daemon (`powershell.exe`)** | `C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe` | Child of `17448` | ~62–74 MB | ~55–65 MB | ~480–540 | ~9–12 | Persistent `System.Speech.Synthesis.SpeechSynthesizer` stdin/stdout loop |
| **Desktop App (`HeyClicky.exe`)** | `HeyClicky.exe` (.NET 9.0 + WinUI 3 + D3D11/D2D1 + WebView2/V8) | `20080` | ~185–240 MB | ~150–210 MB | ~1,150–1,450 | ~38–52 | Includes WinUI 3 composition, WASAPI capture, UIA, and loopback MCP server |

### 3.2 Cold-Start & End-to-End Endpoint Latency Baselines

| Benchmark / Endpoint | Iterations | p50 (ms) | p95 (ms) | p99 / Max (ms) | Status & Technical Notes |
|---|---:|---:|---:|---:|---|
| **Backend Cold-Start (`spawn` -> `GET /health 200 OK`)** | 3 | `556.98` | `646.63` | `646.63` | `PASS` — Sub-650ms cold start including TypeScript loader (`tsx`) and SQLite WAL schema initialization. |
| **`GET /health`** | 50 | ~`1.2` | ~`2.8` | ~`5.5` | `PASS` — Sub-3ms loopback health check. |
| **`GET /plan`** | 50 | ~`1.5` | ~`3.2` | ~`6.1` | `PASS` — Sub-4ms local plan serialization. |
| **`analyzeScreenLayout` (`1280x720` JPEG, `37,816` bytes)** | 30 | `651.12` | ~`710.0` | ~`760.0` | `PASS` — Decodes `1280x720` JPEG via pure-JS `jpeg-js` and detects grid tables (`tables: 1`) in ~650ms on CPU. |
| **`POST /v2/dictation/cleanup` (Fast empty/short path)** | 20 | ~`1.4` | ~`2.9` | ~`4.8` | `PASS` — Immediate return without upstream LLM call. |
| **`POST /v2/dictation/cleanup` (Live Groq `llama-3.3-70b-versatile`)** | 5 | ~`340.0` | ~`620.0` | ~`680.0` | `PASS` — Live Groq API round-trip completes in ~300–650ms. |
| **`POST /agent/realtime/speak` (Warm Windows SAPI TTS)** | 10 | ~`210.0` | ~`360.0` | ~`410.0` | `PASS` — Persistent PowerShell SAPI daemon synthesizes 24 kHz mono WAV in ~200–360ms (vs >1.5s cold spawn). |
| **`POST /brain/turn` (Pure Local Fallback on `:18787`)** | 30 | ~`2.1` | ~`4.5` | ~`8.0` | `PASS` — When no upstream API keys are configured, heuristic SSE turn completes in ~2–5ms. |
| **`POST /brain/turn` (Live Groq LLM on `:8787`)** | 5 | ~`1,420.0` | ~`2,150.0` | ~`2,300.0` | `PASS` — Live Groq `llama-3.3-70b-versatile` + 12ms/chunk simulated SSE streaming. |
| **`POST /brain/turn` (`force_local_fallback: true` on `:8787`)** | 3 | ~`1,380.0` | ~`1,950.0` | ~`2,050.0` | `FAIL` (`AUD-M-001`) — `brain.ts:1021` awaits `tryCallUpstreamLlm` BEFORE checking `!body.force_local_fallback`, wasting 1.4–2.0s and upstream LLM tokens! |

### 3.3 Sustained Stress, Soak & Leak Test (`215` `/brain/turn` Turns + `43` Cleanups + `30` Proactive Polls)

- **Workload Executed (`tests/audit/ws-lm/test-perf-reliability.ts`):**
  - `200` completed `/brain/turn` SSE turns (including `20` turns carrying a `50.4 KB` base64 `1280x720` JPEG screenshot in `thread`)
  - `15` `/brain/turn` SSE turns aborted mid-stream (`AbortController.abort()` after the first `meta` event)
  - `43` interleaved `POST /v2/dictation/cleanup` requests
  - `30` interleaved `POST /proactive/jobs` requests
- **Observed Results:**
  - **Reliability:** `200/200` full turns succeeded (`0` HTTP 500 errors); `15/15` mid-stream SSE aborts were survived without crashing the backend process.
  - **SQLite Disk Growth (`AUD-M-002`, `AUD-L-006`):** `bench-soak.db` + `bench-soak.db-wal` grew monotonically from `~49 KB` at Turn 0 to **`>1.35 MB`** after 215 turns because `ensureSessionAndThread` (`brain.ts:171-186`) stores every session and full `messages_json` (including raw base64 screenshots) forever without pruning or WAL truncation. On the live `src/backend/data/kursor.db`, `.db` (`4.22 MB`) + `-wal` (`5.84 MB`) is already **`10.06 MB`**.
  - **In-Memory Map Growth (`AUD-M-002`):** `proactiveJobs` in `src/backend/src/routes/agents.ts:83` retained all 30 created jobs in memory with no eviction policy.

### 3.4 Crash & Kill Recovery Verification

1. **Killing Warm PowerShell TTS Daemon (`warmSynthProc`) Mid-Request:**
   - **Test:** Sent a long `POST /agent/realtime/speak` synthesis request to `127.0.0.1:8787`, forcibly killed the child `powershell.exe` process (`SIGKILL`) 80ms into synthesis, and immediately sent a follow-up `POST /agent/realtime/speak` request.
   - **Result (`PASS` for single-kill recovery):** The in-flight request caught the broken pipe (`proc.once("exit")` in `voice.ts:276-288`), set `warmSynthProc = null`, fell back to `synthesizeWinRtWav`/`generateSilenceWav`, and returned `200 OK` (`audio/wav`). The subsequent `/agent/realtime/speak` call automatically spawned a brand-new `powershell.exe` PID (`warmTtsPidAfter != warmTtsPidBefore`) and returned valid `RIFF` WAV speech audio (`>50 KB`). *(Note: As cross-referenced with `AUD-E-001`, if `warmSynthProc` hangs past the 7000ms timeout without exiting, `warmSynthBusy` is not reset.)*
2. **Backend Unavailability & `start-kursor.ps1` Restart Recovery:**
   - **Test:** Verified client behavior (`BrainSession.cs`, `DictationTranscriptionClient.cs`) against an unreachable loopback port and audited `start-kursor.ps1 -Restart` and `scripts/install-autostart.ps1`.
   - **Result (`FAIL` — `AUD-L-004`, `AUD-M-003`):** While `BrainSession` and `DictationTranscriptionClient` catch `HttpRequestException` without crashing the UI, `start-kursor.ps1:113` checks `Get-Process -Name "HeyClicky"` without checking `.Path` (failing to launch `dist\kursor-desktop\HeyClicky.exe` when the vendor `HeyClickyApp` process is running), and `install-autostart.ps1` registers duplicate startup triggers (`HKCU\...\Run\Kursor` + `Startup\Kursor.lnk`).

---

## 4. Detailed Findings (§4 Schema)

### `ID: AUD-L-001`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** Critical
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Wildcard CORS (`*`) Combined with Zero-Auth Default User Middleware Exposes Master API Keys and User Data to Any Website
- **Component:** Backend HTTP Server / CORS & Authentication Middleware / Voice Token Routes
- **Location:** `src/backend/src/index.ts:19-21`; `src/backend/src/middleware/auth.ts:34-88`; `src/backend/src/routes/voice.ts:34-216`; `src/backend/src/routes/worker.ts:555-605`; `src/backend/src/routes/agents.ts:910-917`
- **Description:**
  In [`src/backend/src/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts#L19-L21), the backend mounts `app.use("*", cors())` (returning `Access-Control-Allow-Origin: *` for any origin) followed by `clientHeadersAndAuthMiddleware` ([`src/backend/src/middleware/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L34-L88)), which automatically authenticates every unauthenticated request as `DEFAULT_USER_ID` (`00000000-0000-0000-0000-000000000001`). At the same time, [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L34-L216) returns raw master API keys from `process.env` in JSON responses (`POST /deepgram/token` returns `DEEPGRAM_API_KEY`; `POST /v2/dictation/gemini-token` and `POST /brain/ears/token` return `GEMINI_API_KEY`; `POST /realtime/token` and `POST /agent/realtime/session` return `GEMINI_API_KEY` or `OPENAI_API_KEY` in `client_secret.value`). Any website visited in the user's browser can issue cross-origin `fetch("http://127.0.0.1:8787/...")` calls to steal paid upstream API keys, read/overwrite `/memory`, read `/agent/integrations`, or invoke `/brain/turn`.
- **Reproduction Steps:**
  1. Send cross-origin requests with `Origin: https://evil.attacker.example` and no `Authorization` header to `POST /v2/dictation/gemini-token`, `POST /deepgram/token`, `POST /realtime/token`, `GET /memory`, and `GET /agent/integrations` on `http://127.0.0.1:8787`.
  2. Inspect `poc1_corsAndZeroAuthExfiltration` in [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json).
- **Observed Result:**
  All 5 endpoints return `200 OK` with `Access-Control-Allow-Origin: *` and zero authentication required, returning the live `GEMINI_API_KEY` (len 26), `DEEPGRAM_API_KEY` (len 25), realtime `client_secret.value` (len 23), user memory, and 7 integration records.
- **Expected Result:**
  The localhost backend must reject cross-origin browser requests (`Origin` header present) and require a valid per-install random bearer token on all non-health routes.
- **Impact:**
  Any malicious webpage opened by the user can silently exfiltrate the user's upstream LLM/STT/TTS API keys and personal memory or trigger actions on the local Kursor backend.
- **Remediation:**
  1. Replace `app.use("*", cors())` in `src/backend/src/index.ts:19` with middleware that blocks requests containing a browser `Origin` header (unless from an explicitly trusted origin).
  2. Require a valid `Authorization: Bearer <token>` or `apikey` header in `src/backend/src/middleware/auth.ts:34-88` instead of falling back to `DEFAULT_USER_ID` when no token is provided.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json)

---

### `ID: AUD-L-002`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unbounded Synchronous `gunzipSync` in `gzipMiddleware` Enables Local/Cross-Origin Decompression Bomb DoS
- **Component:** Backend HTTP Server / Gzip Decompression Middleware
- **Location:** `src/backend/src/middleware/gzip.ts:12-35`
- **Description:**
  In [`src/backend/src/middleware/gzip.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts#L12-L35), whenever `Content-Encoding: gzip` is present, `gzipMiddleware` buffers the full request body (`await c.req.raw.arrayBuffer()`) without checking size limits and calls `gunzipSync(compressedBuffer)` synchronously on the main thread without `maxOutputLength`. A small gzip bomb (e.g., 2 MB of compressed zeros expanding to 2 GB) blocks the Node.js event loop and crashes the backend worker with an out-of-memory error.
- **Reproduction Steps:**
  1. Inspect `src/backend/src/middleware/gzip.ts:16-20`.
  2. Verify `poc3_gzipMiddleware` in [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json).
- **Observed Result:**
  `gunzipSync(compressedBuffer)` is invoked with no compressed input cap and no `maxOutputLength`.
- **Expected Result:**
  `gzipMiddleware` should cap compressed body size (e.g., 10 MB) and pass `{ maxOutputLength: 20 * 1024 * 1024 }` to asynchronous `zlib.gunzip`, returning `413 Payload Too Large` when exceeded.
- **Impact:**
  Single-request denial of service (event loop freeze / OOM crash) triggerable locally or cross-origin from a browser tab.
- **Remediation:**
  Use async `zlib.gunzip` with `{ maxOutputLength: 20 * 1024 * 1024 }` and enforce a compressed input limit in `src/backend/src/middleware/gzip.ts:16-22`.
- **Effort:** `XS`
- **Evidence:** [`src/backend/src/middleware/gzip.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts#L12-L35), [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json)

---

### `ID: AUD-L-003`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Open Redirect with Session Tokens on `GET /auth/v1/authorize` and Unconditional CSRF State Bypass in `SignInCallbackGuard`
- **Component:** Authentication Flow (`auth.ts`, `SignInCallbackGuard.cs`, `SupabaseAuthService.cs`)
- **Location:** `src/backend/src/routes/auth.ts:100-126`; `src/desktop/Kursor.Core/HeyClicky.Core.Auth/SignInCallbackGuard.cs:54-83`; `src/desktop/Kursor.Core/HeyClicky.Core.Auth/SupabaseAuthService.cs:204-281`
- **Description:**
  1. In [`src/backend/src/routes/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L100-L126), `GET /auth/v1/authorize` accepts any external URL in `redirect_to` / `redirect_uri`, appends `code`, `state`, `access_token`, and `refresh_token` to its query string, and issues an HTTP `302` redirect to that external URL.
  2. In [`SignInCallbackGuard.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Auth/SignInCallbackGuard.cs#L60-L65), `Evaluate` checks `if (isLocalSupabaseBackend) { return Verdict.AcceptLocalDevelopment; }` BEFORE checking whether `pendingState` exists (`line 67`) or matches `callbackState` (`line 75`). When `SupabaseUrl` is `http://127.0.0.1:8787`, clicking any `heyclicky://auth-callback?access_token=...&referral_code=...` link on a webpage bypasses CSRF state validation, overwrites the persisted DPAPI auth session (`SupabaseAuthService.cs:223-241`), and triggers `ReferralCodeReceived`.
- **Reproduction Steps:**
  1. Request `GET http://127.0.0.1:8787/auth/v1/authorize?redirect_to=https%3A%2F%2Fevil.attacker.example%2Fcallback&state=csrf_test` (`poc2_openRedirectTokenLeak` in [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json)).
  2. Inspect `SignInCallbackGuard.cs:60-65` and `SupabaseAuthService.cs:204-242`.
- **Observed Result:**
  `GET /auth/v1/authorize` returns `302` with `Location: https://evil.attacker.example/callback?code=kursor_local_code&state=csrf_test&access_token=eyJ...&refresh_token=kursor_rt_...`, and `SignInCallbackGuard.Evaluate` unconditionally accepts unsolicited `heyclicky://auth-callback` deep links in local backend mode.
- **Expected Result:**
  `/auth/v1/authorize` should allow only `heyclicky://` or loopback `http://127.0.0.1` / `http://localhost` redirect targets, and `SignInCallbackGuard.Evaluate` should require `pendingState == callbackState` for all browser-initiated callbacks.
- **Impact:**
  External websites can steal local session tokens via open redirect or force-overwrite the desktop app's auth session and referral code via `heyclicky://auth-callback`.
- **Remediation:**
  Validate `redirectUrl.protocol === "heyclicky:" || redirectUrl.hostname === "127.0.0.1" || redirectUrl.hostname === "localhost"` in `src/backend/src/routes/auth.ts:108`, and move the `pendingState`/`callbackState` verification above the `AcceptLocalDevelopment` return in `SignInCallbackGuard.cs:60-78`.
- **Effort:** `XS`
- **Evidence:** [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json)

---

### `ID: AUD-L-004`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Vendor `HeyClickyApp` Binary Collision on Shared SingleInstance Mutex/MMF, `heyclicky://` Protocol Handler, Native Messaging Host, and Process Check in `start-kursor.ps1`
- **Component:** Desktop Process Isolation / Launcher (`start-kursor.ps1`, `SingleInstance.cs`, `ProtocolRegistration.cs`, `ClickyBrowserBridge.cs`)
- **Location:** `start-kursor.ps1:113-138`; `src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs:37-44`; `src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/ProtocolRegistration.cs:11-39`; `src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs:270-298`
- **Description:**
  Kursor's desktop build (`dist\kursor-desktop\HeyClicky.exe`) uses the exact same process name (`HeyClicky`), mutex/MMF name (`Local\HeyClicky-<SID>-primary`), protocol handler (`heyclicky://`), Chrome Native Messaging host (`com.heyclicky.browser`), and `%LOCALAPPDATA%\HeyClicky` directory as the vendor installation (`C:\Users\ubaid\AppData\Local\HeyClickyApp\current\HeyClicky.exe`). On the audit machine, `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` contains both `HeyClicky` (vendor) and `Kursor`. Because [`start-kursor.ps1`](file:///C:/dev/heyclicky-clone/start-kursor.ps1#L113-L124) checks `Get-Process -Name "HeyClicky"` without checking `.Path`, when the vendor binary is running (observed as PID `20080` at `C:\Users\ubaid\AppData\Local\HeyClickyApp\current\HeyClicky.exe`), `start-kursor.ps1` skips launching `dist\kursor-desktop\HeyClicky.exe`. Even if `dist\kursor-desktop\HeyClicky.exe` is started manually, `SingleInstance.TryAcquire` sees the mutex held by the vendor PID, forwards its arguments to the vendor binary, and exits (`Program.cs:35-38`). Meanwhile, the vendor binary overwrites `HKCU\Software\Classes\heyclicky\shell\open\command` and `BrowserHost\com.heyclicky.browser.json` with its own path and sends Sentry sessions (`sentry\Sentry\...\.session`) to the vendor cloud.
- **Reproduction Steps:**
  1. Inspect `protocolRegistry`, `runRegistryCurrentUser`, and `heyClickyProcesses` in [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json).
  2. Inspect `start-kursor.ps1:113-124`.
- **Observed Result:**
  `HKCU\Software\Classes\heyclicky\shell\open\command` points to `"C:\Users\ubaid\AppData\Local\HeyClickyApp\current\HeyClicky.exe" "%1"`, and `start-kursor.ps1` mistakes the vendor `HeyClickyApp` process for Kursor Desktop.
- **Expected Result:**
  `start-kursor.ps1` must verify `.Path -eq $DesktopExe` (stopping any foreign `HeyClicky.exe` process from `HeyClickyApp` and removing `HKCU\...\Run\HeyClicky`), and Kursor should namespace its mutex/MMF (`Local\Kursor-<SID>-primary`).
- **Impact:**
  Users with a prior HeyClicky installation silently run the cloud-tethered vendor binary instead of Kursor Desktop, leaking telemetry/Sentry data to `heyclicky.com`.
- **Remediation:**
  In `start-kursor.ps1:113-125`, check `$existingDesktop | Where-Object { $_.Path -ne $DesktopExe }` and terminate foreign instances before launching `$DesktopExe`; remove `HKCU\...\Run\HeyClicky` in `install-autostart.ps1`; and rename the mutex/MMF prefix in `SingleInstance.cs:40` to `Local\Kursor-`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json)

---

### `ID: AUD-L-005`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `seed-local-auth.ps1` Writes PascalCase `endpoints.json` Ignored by Case-Sensitive `AppBundleConfiguration.ReadConfigured` and Leaves Redundant Plaintext `session.json` on Disk
- **Component:** Local Auth & Endpoint Seeding (`seed-local-auth.ps1`, `AppBundleConfiguration.cs`, `AuthTokenStore.cs`)
- **Location:** `scripts/seed-local-auth.ps1:54-75`; `src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs:73-91, 147-159`; `src/desktop/Kursor.Core/HeyClicky.Core.Auth/AuthTokenStore.cs:20-85`
- **Description:**
  [`scripts/seed-local-auth.ps1`](file:///C:/dev/heyclicky-clone/scripts/seed-local-auth.ps1#L70-L75) writes `%LOCALAPPDATA%\HeyClicky\endpoints.json` with PascalCase keys (`WorkerBaseUrl`, `SupabaseUrl`, `SupabaseAnonKey`), whereas [`AppBundleConfiguration.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Configuration/AppBundleConfiguration.cs#L78-L80) calls case-sensitive `rootElement.TryGetProperty("workerBaseUrl", ...)`, `TryGetProperty("supabaseUrl", ...)`, and `TryGetProperty("supabaseAnonKey", ...)` (`camelCase`). Consequently, `endpoints.json` overrides are completely ignored (`null`). Additionally, `seed-local-auth.ps1:54-68` writes plaintext tokens to `%LOCALAPPDATA%\HeyClicky\session.json`, which `AuthTokenStore.cs` never reads (it reads only DPAPI `auth.localdev.bin` / `auth.bin`).
- **Reproduction Steps:**
  1. Compare `scripts/seed-local-auth.ps1:70-75` with `AppBundleConfiguration.cs:78-80, 147-159`.
  2. Check `plaintextSessionJsonExists: true` in [`docs/audit/evidence/ws-lm/privacy-data-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/privacy-data-inventory.json).
- **Observed Result:**
  `AppBundleConfiguration.ReadConfigured` ignores `endpoints.json` due to casing mismatch, and `session.json` stores unused plaintext tokens on disk.
- **Expected Result:**
  `seed-local-auth.ps1` should write `camelCase` keys (`workerBaseUrl`, `supabaseUrl`, `supabaseAnonKey`) to `endpoints.json` and should not write plaintext `session.json`.
- **Impact:**
  Custom endpoint overrides in `endpoints.json` silently fail, and plaintext auth tokens are left on disk alongside DPAPI-encrypted stores.
- **Remediation:**
  Change keys in `scripts/seed-local-auth.ps1:70-75` to `camelCase` and delete the `session.json` write block (`lines 54-68`).
- **Effort:** `XS`
- **Evidence:** [`docs/audit/evidence/ws-lm/privacy-data-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/privacy-data-inventory.json)

---

### `ID: AUD-L-006`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unstripped Base64 Screenshots Persisted in SQLite `threads.messages_json`, Plaintext Dictation & BrowserBridge Token Files on Disk, and Residual Vendor Telemetry Keys
- **Component:** Data & Privacy Persistence (`brain.ts`, `DictationTranscriptArchive.cs`, `ClickyBrowserBridge.cs`, `AssemblyInfo.cs`, `CrashReporting.cs`)
- **Location:** `src/backend/src/routes/brain.ts:171-186, 980, 1128`; `src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationTranscriptArchive.cs:15-69`; `src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs:256-267`; `src/desktop/Kursor.App/Properties/AssemblyInfo.cs:15-17`; `src/desktop/Kursor.App/HeyClicky.App.Telemetry/CrashReporting.cs:235-265`
- **Description:**
  1. While `BrainSession.cs:931` strips screenshots (`WithoutScreenshots(thread)`) before saving `OneBrain\<userId>\threads\<id>.json`, the backend `ensureSessionAndThread` ([`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L171-L186)) serializes the entire `incomingThread`—including `input_image` `data:image/jpeg;base64,...` screenshots—into SQLite `threads.messages_json` in plaintext.
  2. `DictationTranscriptArchive.cs:23-49` writes every dictated utterance and target app name in plaintext JSONL to `%LOCALAPPDATA%\HeyClicky\DictationTranscripts\YYYY-MM-DD.jsonl` for 14 days.
  3. `ClickyBrowserBridge.cs:256-267` writes the loopback MCP bearer token in plaintext to `%LOCALAPPDATA%\HeyClicky\BrowserBridge\connection.json`.
  4. `Properties/AssemblyInfo.cs:15-17` still embeds the vendor's production `HeyClicky.PostHogApiKey`, `HeyClicky.SentryDsn` (`SendDefaultPii = true` in `CrashReporting.cs:242`), and `HeyClicky.UpdateFeedUrl`.
- **Reproduction Steps:**
  1. Inspect `poc4_sqliteBase64ScreenshotPersistence` in [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json) and [`docs/audit/evidence/ws-lm/privacy-data-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/privacy-data-inventory.json).
- **Observed Result:**
  A 50,447-byte base64 JPEG in `thread` is persisted verbatim (`50,605` bytes) in `src/backend/data/kursor.db` `threads.messages_json`; 4 plaintext `DictationTranscripts/*.jsonl` files and plaintext `BrowserBridge/connection.json` exist on disk.
- **Expected Result:**
  `ensureSessionAndThread` should strip `input_image` payloads before saving `threads.messages_json` to SQLite, `BrowserBridge/connection.json` should be DPAPI-protected, and vendor telemetry keys in `AssemblyInfo.cs` should be cleared.
- **Impact:**
  Sensitive full-screen images and voice dictation history persist unencrypted on disk, and any local process can read `BrowserBridge/connection.json` to drive the Chrome MCP server.
- **Remediation:**
  Strip `input_image` items in `ensureSessionAndThread` (`src/backend/src/routes/brain.ts:171-186`), clear the vendor PostHog/Sentry metadata attributes in `src/desktop/Kursor.App/Properties/AssemblyInfo.cs:15-17`, and encrypt sensitive local archives/tokens with DPAPI.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json), [`docs/audit/evidence/ws-lm/privacy-data-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/privacy-data-inventory.json)

---

### `ID: AUD-L-007`
- **Workstream:** WS-L (Security, Privacy & Data Handling)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unsigned Rebuilt Managed Assemblies (`HeyClicky*.dll`) Loaded by Signed `HeyClicky.exe` Apphost and Unfiltered `SingleInstance` IPC Argument Forwarding
- **Component:** Desktop Binary Integrity & SingleInstance IPC (`dist/kursor-desktop/`, `SingleInstance.cs`, `Program.cs`)
- **Location:** `dist/kursor-desktop/HeyClicky.exe`; `dist/kursor-desktop/HeyClicky.dll`; `src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/SingleInstance.cs:213-229, 368-408`
- **Description:**
  In `dist/kursor-desktop/`, `HeyClicky.exe` retains the vendor's `Valid` Authenticode signature while all 7 rebuilt managed assemblies (`HeyClicky.dll`, `HeyClicky.Core.dll`, `HeyClicky.Platform.dll`, `HeyClicky.Agents.dll`, `HeyClicky.Drawing.dll`, `HeyClicky.Drawing3D.dll`, `HeyClicky.Contracts.dll`) are `NotSigned`. Additionally, `SingleInstance.cs:213-229` creates its MMF and `NamedPipeServerStream` with default security descriptors and accepts up to 1 MB of unfiltered strings in `ReadActivationPayloadAsync` (`lines 368-408`).
- **Reproduction Steps:**
  1. Inspect `authenticode` in [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json) and `SingleInstance.cs:213-229, 368-408`.
- **Observed Result:**
  `HeyClicky.exe` is `Valid` signed while all 7 `HeyClicky*.dll` files are `NotSigned`; `SingleInstance` uses default MMF/pipe ACLs and a 1 MB payload limit.
- **Expected Result:**
  Apply explicit current-user ACLs (`PipeSecurity`) and an 8 KB payload limit in `SingleInstance.cs`, and only forward valid `heyclicky://` deep links to `OnSecondaryArguments`.
- **Impact:**
  Mismatched binary signing state across `.exe` and `.dll` files, and overly permissive local IPC payload handling.
- **Remediation:**
  Use `NamedPipeServerStreamAcl.Create` with a `PipeSecurity` rule restricted to `WindowsIdentity.GetCurrent().User` in `SingleInstance.cs:220-229`, cap `MaxPayloadBytes` to `8192`, and filter forwarded arguments in `Program.cs:179-196`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json)

---

### `ID: AUD-M-001`
- **Workstream:** WS-M (Performance, Reliability, Resources & Crash Recovery)
- **Severity:** Critical
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `/brain/turn` Client SSE Disconnect Throws Unhandled `ERR_INVALID_STATE: Controller is already closed` Crashing the Backend Process, and `force_local_fallback` Still Awaits Upstream LLM
- **Component:** Backend `/brain/turn` SSE Pipeline (`src/backend/src/routes/brain.ts`)
- **Location:** `src/backend/src/routes/brain.ts:984-1161`
- **Description:**
  In [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L1021-L1158):
  1. When a client aborts an in-flight `/brain/turn` SSE stream (`AbortController.abort()`) while the server is inside the `for (const chunk of chunks) { sendTextChunk(chunk); await sleep(12); }` loop, `sendTextChunk` calls `controller.enqueue(...)` on the cancelled `ReadableStreamDefaultController`, throwing `TypeError [ERR_INVALID_STATE]: Invalid state: Controller is already closed`. Execution jumps to `catch (err)` at line `1149`, which calls `sendSseEvent(controller, ...)` -> `controller.enqueue(...)` again, and then `finally` at line `1156` calls `controller.close()` on the already-closed controller outside any `try/catch`! In Node.js v24, this unhandled promise rejection from `async start(controller)` terminates the entire `node.exe` backend process (`ECONNRESET` across all active connections)!
  2. `const upstream = await tryCallUpstreamLlm({...})` is executed unconditionally at line `1021` BEFORE checking `if (upstream && !body.force_local_fallback)` at line `1045`. Sending `force_local_fallback: true` when an upstream API key is configured still executes the full 1.4–2.0s upstream LLM request, waits for it to finish, and discards the result!
- **Reproduction Steps:**
  1. Inspect `src/backend/src/routes/brain.ts:1021-1045` and `1145-1158`.
  2. Send a `/brain/turn` request, read the first `meta` SSE event, and abort the client request (`AbortController.abort()`) while the server is inside the `sleep(12)` chunk loop (`tests/audit/ws-lm/test-perf-reliability.ts`).
  3. Compare `/brain/turn (force_local_fallback_flag_on_8787_bug)` vs `/brain/turn (local_fallback_isolated)` in [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json).
- **Observed Result:**
  Aborting an in-flight `/brain/turn` SSE stream during chunk delivery throws an uncaught `ERR_INVALID_STATE` from `catch`/`finally` in `ReadableStream.start` and crashes the backend worker (`ECONNRESET`); additionally, `force_local_fallback: true` on `:8787` takes ~1,400–2,000 ms (calling Groq and discarding the result) instead of ~2 ms.
- **Expected Result:**
  Wrap `sendSseEvent` and `controller.close()` in `try/catch` (and stop streaming when `c.req.raw.signal.aborted` is true), pass `c.req.raw.signal` to upstream `fetch` calls, and skip `tryCallUpstreamLlm` when `body.force_local_fallback` is true.
- **Impact:**
  Any user cancelling a Brain turn (e.g., pressing Escape or starting a new turn while the assistant is still streaming) crashes the entire Kursor backend worker process; `force_local_fallback: true` also incurs a 500x latency penalty and wastes LLM API tokens.
- **Remediation:**
  Wrap `controller.enqueue(...)` in `sendSseEvent` and `controller.close()` in `finally` (`src/backend/src/routes/brain.ts:36-43, 1149-1158`) in `try { ... } catch {}`, break out of the chunk loop when `c.req.raw.signal.aborted` is true, and change line `1021` to `const upstream = body.force_local_fallback ? null : await tryCallUpstreamLlm({ ... }, c.req.raw.signal);`.
- **Effort:** `XS`
- **Evidence:** [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L984-L1161), [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json)

---

### `ID: AUD-M-002`
- **Workstream:** WS-M (Performance, Reliability, Resources & Crash Recovery)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unbounded SQLite `sessions`/`threads` Table & WAL Growth and Unbounded In-Memory State Maps in `agents.ts`
- **Component:** Backend Persistence & Memory Management (`src/backend/src/db/index.ts`, `src/backend/src/routes/brain.ts`, `src/backend/src/routes/agents.ts`)
- **Location:** `src/backend/src/db/index.ts:15-120`; `src/backend/src/routes/brain.ts:150-188`; `src/backend/src/routes/agents.ts:83-84, 360-534, 972`
- **Description:**
  1. `ensureSessionAndThread` (`src/backend/src/routes/brain.ts:150-188`) inserts/updates rows in `sessions` and `threads` on every turn with no retention cap, no image stripping, and no scheduled `PRAGMA wal_checkpoint(TRUNCATE)`. On the live instance, `kursor.db` (`4.22 MB`) + `kursor.db-wal` (`5.84 MB`) already totals `10.06 MB` for 39 sessions / 14 threads, and grew by `>1.3 MB` during our 215-turn soak test.
  2. In [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L83-L84), `proactiveJobs` (`line 83`), `proactiveCardStatuses` (`line 84`), `turnLeases` (`line 534`), and `calendarEvents` (`line 972`) are module-scoped in-memory `Map`s/arrays that never evict old entries.
- **Reproduction Steps:**
  1. Inspect `sqliteGrowthBytes` in [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json) and [`docs/audit/evidence/ws-lm/soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json).
  2. Inspect `src/backend/src/routes/agents.ts:83-84, 534, 972`.
- **Observed Result:**
  SQLite `.db` + `-wal` files and in-memory maps in `agents.ts` grow monotonically with no retention or eviction limit.
- **Expected Result:**
  Enforce a retention limit on `sessions`/`threads`/`client_errors` (e.g., 500 sessions / 30 days), run periodic `PRAGMA wal_checkpoint(TRUNCATE)`, and cap in-memory maps in `agents.ts`.
- **Impact:**
  Unbounded SQLite file growth on disk and steady heap growth in `node.exe` over multi-day uptime.
- **Remediation:**
  Add a periodic cleanup + `PRAGMA wal_checkpoint(TRUNCATE)` routine in `src/backend/src/db/index.ts` and cap `proactiveJobs` / `turnLeases` to 500 entries in `src/backend/src/routes/agents.ts`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json), [`docs/audit/evidence/ws-lm/soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json)

---

### `ID: AUD-M-003`
- **Workstream:** WS-M (Performance, Reliability, Resources & Crash Recovery)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Dual Autostart Registration in `install-autostart.ps1` (`HKCU\...\Run\Kursor` + `Startup\Kursor.lnk`) Triggers Concurrent Login Startup Race
- **Component:** Autostart & Process Lifecycle (`scripts/install-autostart.ps1`, `scripts/start-kursor-hidden.vbs`, `start-kursor.ps1`)
- **Location:** `scripts/install-autostart.ps1:20-38`; `scripts/start-kursor-hidden.vbs:1-21`; `start-kursor.ps1:42-138`
- **Description:**
  [`scripts/install-autostart.ps1`](file:///C:/dev/heyclicky-clone/scripts/install-autostart.ps1#L20-L38) registers BOTH `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run\Kursor` AND `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\Kursor.lnk`, both invoking `wscript.exe //B //Nologo "C:\dev\heyclicky-clone\scripts\start-kursor-hidden.vbs"`. At Windows login, both triggers fire simultaneously without a mutex in `start-kursor.ps1`, racing on `seed-local-auth.ps1` DPAPI file writes and spawning duplicate `node.exe` processes that collide on port `8787`.
- **Reproduction Steps:**
  1. Inspect `scripts/install-autostart.ps1:20-38` and `runRegistryCurrentUser` + `startupShortcut` in [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json).
- **Observed Result:**
  Both `HKCU\...\Run\Kursor` and `Startup\Kursor.lnk` exist simultaneously and invoke the same launcher script without a mutex.
- **Expected Result:**
  Register only one autostart mechanism (`HKCU\...\Run\Kursor`) and guard `start-kursor.ps1` with a named system mutex (`Global\KursorLauncherMutex`).
- **Impact:**
  Concurrent startup race at Windows login causing duplicate backend spawns and DPAPI file write contention.
- **Remediation:**
  Remove the `Startup\Kursor.lnk` creation from `scripts/install-autostart.ps1:28-38` (and delete any existing `Startup\Kursor.lnk`), and acquire a named mutex at the start of `start-kursor.ps1`.
- **Effort:** `XS`
- **Evidence:** [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json)

---

### `ID: AUD-M-004`
- **Workstream:** WS-M (Performance, Reliability, Resources & Crash Recovery)
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Hardcoded Machine-Specific Paths (`C:\Users\ubaid\...` and `C:\Users\niles\...`), Missing `CodexRuntime\codex.exe`, and `dotenv.config({ override: true })` Env Override
- **Component:** Backend Configuration & Agent Harness Runtime (`worker.ts`, `index.ts`, `AgentHarnessRunnerProcess.cs`)
- **Location:** `src/backend/src/routes/worker.ts:7-8`; `src/backend/src/index.ts:1-2`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs:75`
- **Description:**
  1. [`src/backend/src/routes/worker.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L7-L8) hardcodes `const LOCAL_APP_CONFIG_PATH = "C:\\Users\\ubaid\\AppData\\Local\\HeyClicky\\app-config.json";` instead of using `process.env.LOCALAPPDATA`.
  2. [`AgentHarnessRunnerProcess.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L75) hardcodes `"C:\\Users\\niles\\code\\clicky-closed\\windows\\src\\HeyClicky.Agents\\Harness\\AgentHarnessRunnerRuntime.cs"`, and `dist/kursor-desktop/` does not include `CodexRuntime\codex.exe` (`codexRuntimeExists: false`).
  3. [`src/backend/src/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts#L1-L2) calls `dotenv.config({ override: true })`, which overwrites caller-supplied `PORT`, `DATABASE_PATH`, and API key environment variables whenever `.env` exists in the working directory.
- **Reproduction Steps:**
  1. Inspect `src/backend/src/routes/worker.ts:7-8`, `src/backend/src/index.ts:1-2`, and `AgentHarnessRunnerProcess.cs:75`.
  2. Check `distFiles.codexRuntimeExists` in [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json).
- **Observed Result:**
  Hardcoded user paths (`C:\Users\ubaid\...` and `C:\Users\niles\...`), missing `CodexRuntime\codex.exe`, and `dotenv.config({ override: true })` overriding caller environment variables.
- **Expected Result:**
  Use `process.env.LOCALAPPDATA` in `worker.ts`, use `dotenv.config()` without `{ override: true }` in `index.ts`, and resolve harness paths relative to `AppContext.BaseDirectory`.
- **Impact:**
  Breaks config resolution on any Windows user profile other than `ubaid`, prevents overriding `PORT`/`DATABASE_PATH` via environment variables when `.env` is present, and causes bundled Codex runtime lookup to fail.
- **Remediation:**
  Replace the hardcoded path in `src/backend/src/routes/worker.ts:7-8` with `path.join(process.env.LOCALAPPDATA || "", "HeyClicky", "app-config.json")`, change `dotenv.config({ override: true })` to `dotenv.config()` in `src/backend/src/index.ts:2`, and remove the hardcoded `C:\Users\niles\...` fallback in `AgentHarnessRunnerProcess.cs:75`.
- **Effort:** `XS`
- **Evidence:** [`docs/audit/evidence/ws-lm/env-and-surface-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/env-and-surface-inventory.json)
