# Kursor Master Performance, Resource, Throughput & Reliability Report (`PERF_REPORT.md`)

- **Compiled By:** `@ws-lm-sec-perf` (Pass 3 Master Synthesis)
- **Date:** 2026-11-13
- **Primary Evidence Sources:**
  - **WS-M (Performance, Soak & Crash Recovery):** [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json), [`docs/audit/evidence/ws-lm/soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json), [`tests/audit/ws-lm/test-perf-reliability.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-lm/test-perf-reliability.ts)
  - **WS-C (Backend Load & 111-Route Concurrency):** [`docs/audit/evidence/ws-c/load-soak-metrics.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/load-soak-metrics.json), [`docs/audit/evidence/ws-c/route-test-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/route-test-matrix.json)
  - **WS-D (Screen Layout 66-Image Corpus & 105-Prompt Brain Matrix):** [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json), [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)
  - **WS-E (STT, TTS & 112-Transcript Dictation Cleanup Benchmarks):** [`docs/audit/evidence/ws-e/voice-dictation-audit-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-e/voice-dictation-audit-results.json)
  - **WS-F & WS-G (2D & 3D V8 Engine Execution & D3D11/D2D1 GPU Analysis):** [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json), [`docs/audit/evidence/ws-fg/test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json)
  - **WS-H & WS-I (Win32 Overlays, DXGI/WGC Capture, Hooks & Ink Store Concurrency):** [`docs/audit/ws-hi/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/REPORT.md)

---

## 1. Executive Summary & Bottleneck Verdict

| Subsystem | Key Metric Observed | Target / Threshold | Status | Primary Bottleneck / Finding ID |
|---|---|---|---:|---|
| **Backend Cold-Start (`node` + `tsx` + SQLite WAL)** | `p50 = 556.98 ms` (unloaded) / `826.28 ms` (loaded) | `< 1,500 ms` | `PASS` | Fast startup; note `dotenv.config({ override: true })` overrides env vars (`AUD-M-004`). |
| **Loopback REST Control Plane (`/health`, `/plan`, fast `/cleanup`)** | `p50 = 2.82–5.19 ms`, `p95 = 10.26–18.56 ms` | `< 20 ms` | `PASS` | Hono + Node HTTP server handles lightweight routes in `<5 ms` median. |
| **`/brain/turn` (Local Fallback vs `force_local_fallback` Flag)** | Pure local: `p50 = 4.91 ms`<br>`force_local_fallback: true` on `:8787`: `p50 = 3,652.79 ms` | `< 50 ms` | `FAIL` | **`AUD-M-001` / `AUD-C-005`**: `brain.ts:1021` awaits `tryCallUpstreamLlm()` BEFORE checking `!body.force_local_fallback`, adding a **740x latency penalty** (`3.65s` vs `4.91ms`) and wasting upstream LLM quota. |
| **`/brain/turn` (Live LLM SSE End-to-End)** | `p50 = 905.53 ms`, `p95 = 1,075.89 ms` (text)<br>`757–4,878 ms` (vision + layout) | `< 3,000 ms` (text)<br>`< 5,000 ms` (vision) | `FAIL` | **`AUD-M-001`**: Client SSE disconnect during the `sleep(12)` chunk loop triggers an unguarded `ERR_INVALID_STATE: Controller is already closed` in `brain.ts:1149-1158`. |
| **`analyzeScreenLayout` Synchronous CPU Blocking** | `1280x720`: `p50 = 423.12 ms`, `p95 = 750.45 ms`<br>`1920x1080`: `1,362.25 ms`<br>`2560x1440`: `2,182.75 ms`<br>`4000x3000` (12 MP): `6,938.76 ms` | `< 100 ms` on event loop | `FAIL` | **`AUD-D-002`**: Pure-JS `jpeg-js` decode + full-resolution pixel scan runs synchronously on the main Node.js thread before SSE starts, freezing all concurrent HTTP requests for **0.4s–6.9s**. |
| **Voice STT (`/v2/dictation/transcribe`, `/brain/ears/transcribe`)** | `241–701 ms` (`188 KB` 16 kHz WAV via Groq Whisper / Deepgram) | `< 1,000 ms` | `FAIL` | Latency is fast (`p50 ≈ 350 ms`), but provider failure returns HTTP `200 OK` with dummy text `"Hello from Kursor local dictation transcription."` (`AUD-E-003`), and desktop `DictationController` bypasses `/v2/dictation/cleanup` (`AUD-E-002`). |
| **Voice TTS (`POST /agent/realtime/speak` Warm SAPI Daemon)** | Short: `p50 = 19.87 ms` (cached/short) to `961 ms` (full sentences); 8-way burst: `1,027 ms` total wall time | `< 1,000 ms` | `FAIL` | **`AUD-E-001`**: A `>7,000 ms` utterance (`4,715` chars -> `14,232 ms`) times out without evicting its entry from `warmSynthQueue` or resetting `warmSynthBusy`, stalling subsequent `"Hi"` requests for **`8,536–9,570 ms`** and spawning duplicate `powershell.exe` daemons. |
| **2D & 3D ClearScript V8 Engines (`Kursor.Drawing`, `Kursor.Drawing3D`)** | 57 2D programs + 40 3D shapes/modifiers executed in `< 1.2 s` total (`< 25 ms`/part); `120,000` vertex cap enforced | `< 50 ms` per beat/part | `FAIL` | **`AUD-D-004`**: `NativeEngine.cs` (`Feed`, `RenderWhole`, `Evaluate`) has **no V8 execution timeout**, so an LLM-emitted `while(true){}` permanently freezes `DrawingThread`. **`AUD-F-005` / `AUD-G-004` / `AUD-H-002`**: Zero `DXGI_ERROR_DEVICE_REMOVED` recovery across D3D11/D2D1/Win2D pipelines. |
| **Sustained Soak, Memory & SQLite WAL Growth (215 Turns + 120 Concurrent Load)** | Node RSS: `58.09 MB` -> `56.57 MB` (isolated soak); `+10.52 MB` RSS / `+2.21 MB` heap (120-req burst)<br>SQLite `.db`+`-wal`: `457 KB` -> `5.54 MB` (`+5.08 MB` in 215 turns; `10.06 MB` live) | Bounded disk & heap | `FAIL` | **`AUD-M-002` / `AUD-C-010` / `AUD-L-006`**: Unstripped base64 screenshots in `threads.messages_json`, zero SQLite secondary indexes (`SCAN sessions`, `SCAN notifications`), no WAL checkpoint/retention policy, and unbounded in-memory `Map`s in `agents.ts`. |

---

## 2. Methodology & Test Environment

1. **Hardware & OS Environment:**
   - **OS:** Windows 11 (`10.0.26200` / 24H2)
   - **Runtime Stack:** Node.js `v24.22.1` (`node:sqlite`, `@hono/node-server`, `tsx`), .NET `9.0` (`net9.0-windows10.0.19041.0`), WinUI 3 (`Microsoft.WindowsAppRuntime.1.6`), ClearScript V8 (`7.5.0`), Vortice D3D11/DXGI/D2D1.
2. **Benchmark Harnesses Executed:**
   - **Isolated Ephemeral Benchmark Server (`tests/audit/ws-lm/test-perf-reliability.ts`):** Spawned isolated backend instances on ephemeral loopback ports (`127.0.0.1:188xx`) with dedicated SQLite databases (`bench-soak-*.db`) to measure cold-start latency, pure local fallback latency, 215-turn stress/soak progression (with 10-way concurrency, 20 base64 `1280x720` JPEG screenshot turns, 43 dictation cleanups, 30 proactive polls, and 15 mid-stream SSE client disconnects), and mid-request `SIGKILL` recovery of the warm PowerShell TTS daemon.
   - **Live Backend Benchmarks (`127.0.0.1:8787`):** Measured live Groq/OpenRouter/Gemini round-trip latencies across `/brain/turn`, `/v2/dictation/cleanup`, `/v2/dictation/transcribe`, `/brain/ears/transcribe`, and `/agent/realtime/speak`, plus a 120-request concurrent load test (`tests/audit/ws-c/test-backend-audit.ts`).
   - **Synthetic & Corpus Vision Benchmarks (`tests/audit/ws-d/test-screen-layout.ts`):** Generated a 66-image synthetic worksheet/UI corpus across resolutions from `320x240` up to `4000x3000` (12 MP) and `8000x8000` (64 MP) to measure `analyzeScreenLayout` event-loop blocking and table/answer-line detection precision/recall.

---

## 3. Process Resource Baselines (`node.exe`, `powershell.exe`, `HeyClicky.exe`)

Measured on the running system before and after the full audit suite ([`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json), [`docs/audit/evidence/ws-jk/ui-hwnd-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/ui-hwnd-inventory.json)):

| Process Role | Executable | Observed PID(s) | Working Set RAM (MB) | Heap / Private Memory | Handles | Threads | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| **Backend Worker (Idle / Warm)** | `node.exe` (`tsx src/index.ts`) | `17448` | `56.57 – 95.40 MB` | `17.61 – 19.93 MB` V8 Heap (`124–135 MB` RSS) | ~`260–310` | ~`11–13` | Stable V8 heap (`+2.21 MB` delta after 120 concurrent requests; `-1.52 MB` working-set delta after 215 soak turns). |
| **Warm SAPI TTS Daemon** | `powershell.exe` (`System.Speech`) | `14364`, `15308`, `21520` | `44.18 – 61.67 MB` per instance | ~`40–55 MB` | ~`480–540` | ~`9–12` | **Leak Warning (`AUD-E-001`, `AUD-E-009`, `AUD-C-006`):** `voice.ts` attaches zero `exit`/`SIGINT`/`SIGTERM` listeners to kill `warmSynthProc`, and timeouts (`>7,000 ms`) spawn additional `powershell.exe` processes (`3` concurrent instances observed totaling `155.92 MB`). |
| **Desktop App (`HeyClicky.exe`)** | `HeyClicky.exe` (.NET 9.0 + WinUI 3 + D3D11/D2D1 + V8) | `20080` | `454.70 MB` (active with overlays & WebView2/V8) | ~`310 MB` | ~`1,350` | ~`46` | Hosts 6 top-level Win32/WinUI HWNDs (`HeyClicky Notch`, `HeyClickyTray`, `HeyClickyTaskbarButton`, composition overlays) + Loopback MCP server on `:51635`. |

---

## 4. End-to-End HTTP & Subsystem Latency Benchmarks

### 4.1 Backend Cold-Start & Core Endpoint Latencies (`docs/audit/ws-lm/PERF_DATA.json`)

| Endpoint / Operation | Sample Count (`N`) | Errors | Min (ms) | p50 / Median (ms) | Mean (ms) | p95 (ms) | p99 / Max (ms) |
|---|---:|---:|---:|---:|---:|---:|---:|
| **Backend Cold-Start (Unloaded CPU)** | 3 | 0 | `524.69` | **`556.98`** | `576.10` | `646.63` | `646.63` |
| **Backend Cold-Start (Under Swarm Load)** | 3 | 0 | `794.31` | **`826.28`** | `4,204.85` | `10,993.96` | `10,993.96` |
| **`GET /health`** | 50 | 0 | `1.28` | **`2.82`** | `7.52` | `18.56` | `145.82` |
| **`GET /plan`** | 50 | 0 | `1.17` | **`2.87`** | `4.23` | `10.26` | `13.49` |
| **`POST /v2/dictation/cleanup` (Fast Empty/Short Path)** | 20 | 0 | `1.52` | **`5.19`** | `6.00` | `16.03` | `16.03` |
| **`POST /v2/dictation/cleanup` (Local / Cached Groq Path)** | 5 | 0 | `4.57` | **`9.11`** | `7.95` | `9.88` | `9.88` |
| **`POST /brain/turn` (Pure Local Fallback, `:188xx`)** | 15 | 0 | `2.89` | **`4.91`** | `6.45` | `12.68` | `12.68` |
| **`POST /brain/turn` (Live LLM on `:8787`, Text Prompt)** | 3 | 0 | `792.78` | **`905.53`** | `924.73` | `1,075.89` | `1,075.89` |
| **`POST /brain/turn` (`force_local_fallback: true` on `:8787` — `AUD-M-001` Bug)** | 2 | 0 | `903.24` | **`3,652.79`** | `2,278.01` | `3,652.79` | `3,652.79` |
| **`POST /brain/turn` (200-Turn Soak at Concurrency = 10)** | 200 | 6 | `26.96` | **`251.96`** | `1,431.22` | `5,552.44` | `7,678.57` |
| **`POST /brain/turn` (120-Request Mixed Concurrent Burst — `WS-C`)** | 120 | 0 | `7,561.57` | **`8,301.65`** | — | `25,799.99` | `31,331.88` |

> **Key Bottleneck Analysis (`AUD-M-001` / `AUD-C-005`):**
> On an isolated backend with no upstream LLM keys, `/brain/turn` completes in **`p50 = 4.91 ms`**. However, when upstream API keys are present in `.env` (`:8787`), passing `force_local_fallback: true` still executes `await tryCallUpstreamLlm(...)` (`src/backend/src/routes/brain.ts:1021`) before checking `if (upstream && !body.force_local_fallback)` at line `1045`, inflating latency to **`p50 = 3,652.79 ms`** (`744x` slower) and discarding the paid LLM completion.

---

### 4.2 Vision & `analyzeScreenLayout` CPU Blocking Benchmark (`WS-D` 66-Image Corpus + `WS-M`)

In `src/backend/src/utils/screenLayout.ts:68-82, 144-510`, `analyzeScreenLayout` decodes base64 JPEGs via pure-JavaScript `jpeg-js` and runs multi-pass pixel scans synchronously on the main Node.js thread before `/brain/turn` begins streaming SSE events (`AUD-D-002`):

| Resolution | Megapixels | Sample Count (`N`) | Min (ms) | p50 / Median (ms) | Mean (ms) | p95 (ms) | Max (ms) | Event-Loop Impact |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| **`1280x720` (720p Standard Corpus, Unloaded)** | `0.92 MP` | 50 | `145.56` | **`423.12`** | `448.59` | **`750.45`** | `1,203.68` | Blocks all concurrent requests for `0.42–1.20 s` |
| **`1280x720` (720p Under Concurrent Soak Load)** | `0.92 MP` | 10 | `1,441.30` | **`3,131.54`** | `3,123.75` | **`5,497.43`** | `5,497.43` | Blocks main loop for `1.4–5.5 s` under CPU contention |
| **`1920x1080` (1080p Full HD)** | `2.07 MP` | 1 | `1,362.25` | **`1,362.25`** | `1,362.25` | `1,362.25` | `1,362.25` | Blocks main loop for **`1.36 s`** |
| **`2560x1440` (1440p QHD)** | `3.69 MP` | 1 | `2,182.75` | **`2,182.75`** | `2,182.75` | `2,182.75` | `2,182.75` | Blocks main loop for **`2.18 s`** |
| **`4000x3000` (12 MP Uncapped)** | `12.00 MP` | 1 | `6,938.76` | **`6,938.76`** | `6,938.76` | `6,938.76` | `6,938.76` | Blocks main loop for **`6.94 s`** (`maxMemoryUsageInMB` not set) |
| **`8000x8000` (64 MP Bomb)** | `64.00 MP` | 1 | `4,212.50` | **`4,212.50`** | `4,212.50` | `4,212.50` | `4,212.50` | Allocates **256 MB RGBA buffer** on main thread (`4.21 s` freeze) |

**Accuracy Metrics across the 66-Image Corpus (`screen-layout-benchmark.json`):**
- **Table Detection:** Strict Precision = **`84.62%`** (`11 TP`, `2 FP`), Any-Table Precision = **`88.89%`**, Strict Recall = **`35.48%`** (`11/31`), Any-Table Recall = **`51.61%`** (`16/31`; misses borderless whitespace tables, dark-mode tables, and low-contrast grids).
- **Answer-Line Detection:** Strict Precision = **`100.0%`** (`6 TP`, `0 FP`), Strict Recall = **`37.50%`** (`6/16`), Any-Line Recall = **`50.00%`** (`8/16`).

---

### 4.3 Voice STT & TTS Latency & Concurrency Benchmarks (`WS-E` + `WS-M`)

#### A. Speech-to-Text (`POST /v2/dictation/transcribe`, `/brain/ears/transcribe`, `/ears/transcribe`)
| Audio Payload | Endpoint & Encoding | Payload Size | Total Elapsed (ms) | Provider `stt_ms` | Status |
|---|---|---:|---:|---:|---|
| `16 kHz` Mono Real Speech WAV | `/v2/dictation/transcribe` (JSON Base64) | `188,964 B` | `701 ms` | `681 ms` | `200 OK` |
| `16 kHz` Mono Real Speech WAV | `/v2/dictation/transcribe` (`multipart/form-data` `file`) | `188,964 B` | `640 ms` | `539 ms` | `200 OK` |
| `16 kHz` Mono Real Speech WAV | `/brain/ears/transcribe` (`multipart/form-data` `audio`) | `188,964 B` | `371 ms` | `370 ms` | `200 OK` |
| `16 kHz` Mono Real Speech WAV | `/ears/transcribe` (JSON `audio`) | `188,964 B` | `305 ms` | `268 ms` | `200 OK` |
| `8 kHz`–`48 kHz` Tone WAVs | `/v2/dictation/transcribe` (JSON Base64) | `8 KB – 96 KB` | `242 – 329 ms` | `241 – 297 ms` | `200 OK` |
| All Providers Fail (Invalid Keys) | `/v2/dictation/transcribe` | `188,964 B` | **`6,306 ms`** | `6,306 ms` | `FAIL` (`AUD-E-003`: returns `200 OK` with dummy string) |

#### B. Text-to-Speech (`POST /agent/realtime/speak` Warm Windows SAPI Daemon)
| Test Scenario | Concurrency | Samples (`N`) | Min (ms) | p50 (ms) | p95 (ms) | Max (ms) | Notes |
|---|---:|---:|---:|---:|---:|---:|---|
| **Empty / Whitespace Input** | 1 | 2 | `1.0` | **`1.0`** | `1.0` | `1.0` | Returns `4,800 B` silent PCM immediately. |
| **Warm Short Utterance (`"Test 1..3."`, `WS-M`)** | 1 | 3 | `18.38` | **`19.87`** | `35.45` | `35.45` | Warm PowerShell `SpeechSynthesizer` stdin/stdout pipe. |
| **Sequential Full Sentences (`WS-E`)** | 1 | 10 | `376.0` | **`961.0`** | `1,501.0` | `1,501.0` | `551–1,380 ms` across multi-line, Unicode, and punctuation strings (`94–253 KB` PCM). |
| **8-Way Concurrent Burst (`"One."`..`"Eight."`, `WS-E`)** | 8 | 8 | `64.0` | **`490.5`** | `1,026.0` | `1,026.0` | Total wall time **`1,027 ms`** for all 8 concurrent requests serialized through `warmSynthQueue`. |
| **Timeout Desync (`4,715`-char utterance -> `"Hi"`, `AUD-E-001`)** | 2 | 2 | `8,536.0` | **`9,053.0`** | `9,570.0` | `14,232.0` | **`FAIL` (`AUD-E-001`):** `>7,000 ms` utterance times out at `7s` without clearing `warmSynthBusy`, stalling subsequent `"Hi"` requests for **`8.5s–9.6s`**. |

---

### 4.4 2D & 3D ClearScript V8 Engine & GPU Rendering Performance (`WS-F`, `WS-G`, `WS-H`)

- **2D ClickyDraw Engine (`tests/audit/ws-fg/test-2d-engine.mjs`):**
  - Executed **57 complete ClickyDraw programs** (covering all 2D primitives, curves, connectors, sticky/paper/receipt/clipping cards, graphs, timelines, molecules, and live mutations) in **`< 450 ms` total wall time** (`~7.8 ms` per program average).
- **3D Procedural Geometry Kernel (`tests/audit/ws-fg/test-3d-engine.mjs`):**
  - Executed **40 3D shape & modifier builds** across all 16 procedural primitives (`box`, `cyl`, `cone`, `tube`, `arrow`, `lathe`, `sphere`, `torus`, `capsule`, `pipe`, `spring`, `gear`, `extrude`, `blob` marching-cubes metaballs, `poly`, `surface`) plus `thick` (`solidify`), `stretch`, `mirror`, `cut`, `around`, and `copies` in **`< 750 ms` total wall time** (`~18.7 ms` per 3D build).
  - Enforces a hard **`MAX_VERTICES = 120,000`** cap (`clicky-three-parts.js:20,626`) and per-primitive segment/coil/tooth clamps (`seg <= 128`, `coils <= 60`, `teeth <= 200`, `detail <= 3`) plus a **2,500 ms per-part V8 watchdog timer** (`ClickyThreeKernel.cs`) and a **256-entry FIFO geometry cache**.
- **GPU Shader Compilation & Frame Pacing (`Kursor.Drawing3D` & `Kursor.Platform`):**
  - `ShaderCache.cs` caches all 27 compiled HLSL entry points (`MainHlsl`, `StudioHlsl`, `EdgeHlsl`, `PostHlsl`) in `%LOCALAPPDATA%\HeyClicky\ShaderCache\shaders-v1.bin` keyed by SHA-256 hash, avoiding cold `D3DCompile` stalls on subsequent launches.
  - **Critical Reliability Gaps:**
    1. **`AUD-D-004`:** Unlike `ClickyThreeKernel.cs` (which has a 2,500 ms watchdog), `NativeEngine.cs` (`Kursor.Drawing`) has **no V8 execution timeout**, so `while(true){}` in 2D drawing code permanently deadlocks `DrawingThread`.
    2. **`AUD-F-005` / `AUD-G-004` / `AUD-H-002`:** Zero `DXGI_ERROR_DEVICE_REMOVED` / `DXGI_ERROR_DEVICE_RESET` recovery across `OverlayHost.cs`, `SystemComposition.cs`, `ScreenPainter.cs`, `StageSwapChain.cs`, `GpuDevice.cs`, and `Pipelines.cs`.
    3. **`AUD-G-002`:** `ThreeStage.Dispose()` queues `running.Join(5000); Release();` on `ThreadPool` and disposes D3D11/D2D1 resources even if `Join(5000)` times out while `FrameLoop` is still rendering.

---

## 5. Sustained Soak, Memory & SQLite WAL Growth Analysis

### 5.1 215-Turn Stress & Soak Progression (`docs/audit/ws-lm/PERF_DATA.json`)

Workload: **200 completed `/brain/turn` SSE turns** (at concurrency = 10, with every 10th turn carrying a `50.4 KB` base64 `1280x720` JPEG screenshot in `thread`) + **15 mid-stream SSE `AbortController` disconnects** + **43 `/v2/dictation/cleanup` requests** + **30 `/proactive/jobs` polls**:

| Soak Milestone | `.db` File Size | `-wal` File Size | `-shm` File Size | Total SQLite Disk Footprint | Backend `node.exe` Working Set |
|---|---:|---:|---:|---:|---:|
| **Turn 0 (Initial)** | `4,096 B` (`4.0 KB`) | `453,232 B` (`442.6 KB`) | `32,768 B` | **`457,328 B` (`0.44 MB`)** | `58.09 MB` |
| **Turn 50** | `4,096 B` (`4.0 KB`) | `2,406,112 B` (`2.29 MB`) | `32,768 B` | **`2,410,208 B` (`2.30 MB`)** | `56.63 MB` |
| **Turn 100** | `704,512 B` (`688.0 KB`) | `4,128,272 B` (`3.94 MB`) | `32,768 B` | **`4,832,784 B` (`4.61 MB`)** | `56.60 MB` |
| **Turn 150** | `704,512 B` (`688.0 KB`) | `4,128,272 B` (`3.94 MB`) | `32,768 B` | **`4,832,784 B` (`4.61 MB`)** | `56.57 MB` |
| **Turn 200** | `704,512 B` (`688.0 KB`) | `4,128,272 B` (`3.94 MB`) | `32,768 B` | **`4,832,784 B` (`4.61 MB`)** | `56.57 MB` |
| **Turn 215 (Post-Aborts)** | `1,413,120 B` (`1.35 MB`) | `4,128,272 B` (`3.94 MB`) | `32,768 B` | **`5,541,392 B` (`5.28 MB`)** | `56.57 MB` |
| **Net Delta (`215` Turns)** | **`+1,409,024 B`** | **`+3,675,040 B`** | `0 B` | **`+5,084,064 B` (`+4.85 MB`)** | **`-1.52 MB`** |

### 5.2 Storage & Memory Leak Root Causes (`AUD-M-002`, `AUD-L-006`, `AUD-C-010`, `AUD-H-003`)

1. **SQLite Growth Rate (`+23.6 KB / turn` average, `+50.6 KB` per screenshot turn):**
   - `ensureSessionAndThread` (`src/backend/src/routes/brain.ts:171-186`) writes the entire `incomingThread` JSON—including raw `data:image/jpeg;base64,...` blocks—directly into `threads.messages_json` without stripping screenshots (unlike `BrainSession.WithoutScreenshots` on the desktop client).
   - `src/backend/src/db/index.ts` defines **zero secondary indexes** (`EXPLAIN QUERY PLAN` confirms full table scans `SCAN sessions`, `SCAN notifications`, `SCAN proactive_cards`) and runs **no retention pruning or `PRAGMA wal_checkpoint(TRUNCATE)`** (live `src/backend/data/kursor.db` + `-wal` is already **`10.06 MB`** for just 39 sessions / 14 threads).
2. **Unbounded In-Memory Collections in Backend & Desktop:**
   - **Backend (`src/backend/src/routes/agents.ts:83-84, 534, 972`):** `proactiveJobs`, `proactiveCardStatuses`, `turnLeases`, and `calendarEvents` are module-scoped `Map`s/arrays with no TTL or max-size eviction (`AUD-M-002`).
   - **Desktop 2D V8 Engine (`clicky-draw-native.js:25`):** `clearNow()` cancels C# timer tokens but never clears the JS `timerCallbacks` `Map`, leaking timer closures across turns (`AUD-D-005`).
   - **Desktop 3D V8 Kernel (`ClickyThreeKernel.cs:30`):** `HashSet<string> failed` caches failed recipe JSON strings forever without eviction (`AUD-G-003`).
   - **Desktop Win32 Overlays (`OverlayHost.cs:367-371`, `FramePacer.cs:15`):** Leaks waitable timer handles (`CreateWaitableTimerExW`), `ManualResetEventSlim` instances, and `ScreenCaptureYield` registrations (`AUD-H-003`).

---

## 6. Crash & Kill Recovery Verification Summary

| Recovery Scenario | Test Method | Observed Outcome | Status |
|---|---|---|---|
| **Kill Warm PowerShell TTS Daemon (`warmSynthProc`) Mid-Request** | Sent long `POST /agent/realtime/speak`, sent `SIGKILL` to child `powershell.exe` (`PID 27232`) at `+80 ms`, then sent immediate follow-up `/agent/realtime/speak`. | In-flight request caught process exit, fell back cleanly (`200 OK`, `569,099 B`), and next request spawned a new `powershell.exe` daemon (`PID 14364`, `200 OK`, `157,259 B` in `919.22 ms`). | `PASS` (single kill)<br>`FAIL` on `>7s` hang (`AUD-E-001`) |
| **Mid-Stream Client SSE Disconnect on `/brain/turn`** | Aborted 15 `/brain/turn` requests via `AbortController.abort()` after reading `meta` event (`tests/audit/ws-lm/test-perf-reliability.ts`). | If aborted after `meta` before chunk loop completes, `sendTextChunk` -> `catch { sendSseEvent }` -> `finally { controller.close() }` (`brain.ts:1149-1158`) calls `enqueue`/`close` on an already-closed `ReadableStreamDefaultController` (`ERR_INVALID_STATE`), and upstream LLM `fetch` is never passed `c.req.raw.signal`. | `FAIL` (`AUD-M-001`) |
| **Desktop Launcher (`start-kursor.ps1`) & Autostart Recovery** | Inspected `start-kursor.ps1`, `scripts/install-autostart.ps1`, and running `HeyClicky.exe` processes (`env-and-surface-inventory.json`). | `install-autostart.ps1` registers duplicate login triggers (`HKCU\...\Run\Kursor` + `Startup\Kursor.lnk`) causing a startup race (`AUD-M-003`), and `start-kursor.ps1:113` checks `Get-Process -Name "HeyClicky"` without checking `.Path`, colliding with vendor `HeyClickyApp\current\HeyClicky.exe` (`AUD-L-004`). | `FAIL` (`AUD-L-004`, `AUD-M-003`) |
| **GPU Device Reset (`DXGI_ERROR_DEVICE_REMOVED`)** | Audited D3D11/D2D1/Win2D `Present` and `BeginDraw` paths across `Kursor.Platform`, `Kursor.Drawing`, and `Kursor.Drawing3D`. | Unhandled `COMException` on `Present`/`CopyInto` permanently terminates overlay render threads and sets `Pipelines.buildFailed = true` with no device recreation path. | `FAIL` (`AUD-F-005`, `AUD-G-004`, `AUD-H-002`) |
