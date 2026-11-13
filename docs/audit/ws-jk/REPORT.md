# Workstream WS-J & WS-K Audit Report: Autonomous Agents, Sidecars & Desktop UI Shell

- **Workstreams**: **WS-J** (`src/desktop/Kursor.Agents/` & Sidecar Binaries) + **WS-K** (`src/desktop/Kursor.App/` Shell & Every UI Surface)
- **Owner**: `@ws-jk-agents-ui`
- **Repository**: `C:\dev\heyclicky-clone`
- **Total Files Audited**: **1,007 C# files** (`122` in `src/desktop/Kursor.Agents/`, `885` in `src/desktop/Kursor.App/`), plus `AgentTools.cs` & `ClickyBrowserBridge.cs` in `Kursor.Core`, `src/backend/src/routes/agents.ts`, and 2 precompiled sidecar binaries (`clicky-harness.exe` and `ClickyBrowserHost.exe`)
- **Total Confirmed Findings**: **18 (`AUD-J-001` – `AUD-J-008`, `AUD-K-001` – `AUD-K-010`)**
  - **Critical**: 1 (`AUD-J-001`)
  - **High**: 3 (`AUD-J-002`, `AUD-J-003`, `AUD-K-002`)
  - **Medium**: 9 (`AUD-J-004`, `AUD-J-005`, `AUD-J-006`, `AUD-J-007`, `AUD-K-001`, `AUD-K-003`, `AUD-K-004`, `AUD-K-007`, `AUD-K-008`)
  - **Low**: 5 (`AUD-J-008`, `AUD-K-005`, `AUD-K-006`, `AUD-K-009`, `AUD-K-010`)
- **Executable Audit Scripts**:
  - [`tests/audit/ws-jk/test-agents-harness.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-agents-harness.ps1) (WS-J 30-scenario harness, sidecar, tool & safety test suite)
  - [`tests/audit/ws-jk/test-ui-surfaces.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-ui-surfaces.ps1) (WS-K HWND, UIA, DPAPI & static UI surface audit script)
- **Machine-Readable Deliverables**:
  - [`docs/audit/ws-jk/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/findings.json)
  - [`docs/audit/ws-jk/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/coverage.json)

---

# Part I — Workstream WS-J: Autonomous Agents (`src/desktop/Kursor.Agents/` & Sidecars)

## 1. Architecture & Complete 9-Namespace Enumeration (`src/desktop/Kursor.Agents/`)

`src/desktop/Kursor.Agents/` contains **122 C# files** organized across **9 namespaces** (compiled into `HeyClicky.Agents.dll`), plus `AgentTools.cs` (`HeyClicky.Core.Conversation`) and `ClickyBrowserBridge.cs` (`HeyClicky.Core.Browser`) in `src/desktop/Kursor.Core/`:

| # | Namespace | Files | Key Classes & Responsibilities | Status |
|---|---|---:|---|---|
| 1 | `HeyClicky.Agents` | 19 | [`AgentService.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentService.cs) (1,474 lines — top-level orchestrator for jobs, routines, memory, and Composio), [`AgentWorkerApi.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentWorkerApi.cs) (385 lines — `/agent/lease`, `/agent/composio/session`, `/agent/memory`), [`AgentIntegrationsApi.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentIntegrationsApi.cs) (180 lines), `AgentSkillsService.cs`, `AgentCatalog.cs` | `FAIL` (`AUD-J-001`) |
| 2 | `HeyClicky.Agents.Harness` | 9 | [`AgentHarnessRunnerRuntime.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerRuntime.cs) (603 lines — HTTP client to `clicky-harness.exe`), [`AgentHarnessRunnerProcess.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs) (334 lines — spawns `clicky-harness.exe` & reads `{"ready":true,"port":<port>}`), [`AgentHarnessRuntimes.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRuntimes.cs), [`AgentHarnessNames.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs), [`AgentHarness.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarness.cs) (`Codex, Pi, H1`), [`AgentHarnessPlace.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessPlace.cs) (`Mac, Cloud`), [`AgentModelOption.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentModelOption.cs) (27 model presets) | `FAIL` (`AUD-J-002`, `AUD-J-005`, `AUD-J-006`) |
| 3 | `HeyClicky.Agents.Runtime` | 11 | [`CodexSession.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs) (512 lines — spawns `codex.exe exec --json -c approval_policy="never" -s danger-full-access`), [`CodexHomeManager.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs) (147 lines — manages `%LOCALAPPDATA%\HeyClicky\codex-home\AGENTS.md` & `skills/`), [`WindowsInstructionAdapter.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/WindowsInstructionAdapter.cs) (58 lines), [`KillOnCloseJob.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/KillOnCloseJob.cs) (88 lines — Win32 Job Object `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000`) | `FAIL` (`AUD-J-003`, `AUD-J-008`) |
| 4 | `HeyClicky.Agents.Jobs` | 13 | [`ClickyJobStore.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Jobs/ClickyJobStore.cs) (415 lines — persists `jobs.json` atomically via `.tmp` + `File.Move`), [`ClickyWorkFolderFiles.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Jobs/ClickyWorkFolderFiles.cs) (118 lines — lists workspace deliverables, skipping `.clicky`, `tmp`, `node_modules`, `.git`, and `ReparsePoint` symlinks), `ClickyJob.cs`, `ClickyArtifact.cs` | `PASS` |
| 5 | `HeyClicky.Agents.Turns` | 16 | [`TurnPromptBuilder.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/TurnPromptBuilder.cs) (312 lines), [`StructuredFinalReply.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/StructuredFinalReply.cs) (395 lines — parses 9 XML blocks: `<SUMMARY>`, `<ANSWER>`, `<DONE_TITLE>`, `<ARTIFACTS>`, `<NEXT_ACTIONS>`, `<ROUTINE>`, `<COMPUTER_USE_REQUEST>`, `<WAIT_FOR>`, `<LEARNED>`), `TurnRunner.cs`, `TurnEvent.cs` | `FAIL` (`AUD-J-006`) |
| 6 | `HeyClicky.Agents.Routines` | 7 | [`RoutineScheduler.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineScheduler.cs) (314 lines — 30s timer tick, 6h max catch-up window), [`ClickyRoutine.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/ClickyRoutine.cs) (195 lines — `NextSetTime` with `TimeZoneInfo` DST fallback), [`RoutineKeepAwake.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineKeepAwake.cs) (115 lines), [`RoutineWakeGate.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineWakeGate.cs) (108 lines — 25s post-resume settle delay on `PBT_APMRESUMEAUTOMATIC`) | `FAIL` (`AUD-J-007`) |
| 7 | `HeyClicky.Agents.Memory` | 2 | [`UserMemoryCache.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Memory/UserMemoryCache.cs) (95 lines — caches `GET /agent/memory` markdown and merges `<LEARNED>` bullets via `POST /agent/memory/append`), `MemoryBulletParser.cs` | `PASS` |
| 8 | `HeyClicky.Agents.Proactive` | 30 | [`ProactiveDeciderClient.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Proactive/ProactiveDeciderClient.cs) (`POST /proactive/decide`), `ProactiveContextCollector.cs`, `ProactiveSuggestionStore.cs` | `PASS` |
| 9 | `HeyClicky.Agents.Sidecar` | 14 | [`SidecarRecorder.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Sidecar/SidecarRecorder.cs), `SidecarTurnSync.cs` (`POST /sidecar/turns`) | `PASS` |

---

## 2. Sidecar Binary Provenance & Reverse Engineering (`clicky-harness.exe` & `ClickyBrowserHost.exe`)

| Property | `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` | `dist/kursor-desktop/ClickyBrowserHost/ClickyBrowserHost.exe` |
|---|---|---|
| **File Size** | `88,566,080` bytes (84.46 MB) | `4,685,632` bytes (4.47 MB) |
| **SHA-256** | `31DFAF8C74BAB71F07607EAFA3D9940B82C7E5540FA0E825169278AC8E7A30DA` | `E7BD3FD9179313B98C10A79B18E6631BBB8EE6C7FB264FA4825106F4DB084A86` |
| **Authenticode Status** | `Valid` (`CN="Humansongs, Inc", O="Humansongs, Inc", L=San Francisco, S=California, C=US`, Thumbprint `3F234731E458612EFEE78459EBC9FA556F198582`) | `Valid` (`CN="Humansongs, Inc", O="Humansongs, Inc", L=San Francisco, S=California, C=US`, Thumbprint `3F234731E458612EFEE78459EBC9FA556F198582`) |
| **Runtime / Compiler** | **Bun 1.3.11** standalone Windows x64 executable embedding a minified JS bundle + `B:/~BUN/root/photon_rs_bg-bq08arze.wasm` | **Go** Windows x64 executable (`Go build ID: "3l8p8J3eH5R-_tF52U0r/s1R-o2W9h6m_v1k8z3A7/..."`) |
| **Source in Repo?** | **No** (`AUD-J-004`) | **No** (`AUD-J-004`) |
| **Wire Protocol** | Reads `CLICKY_HARNESS_TOKEN` and `CLICKY_HARNESS_STATE` from env; binds HTTP server on `127.0.0.1:0`; prints `{"ready":true,"port":<port>}` on stdout; closes when stdin closes; serves `POST /turn`, `POST /steer`, `POST /cancel`, `POST /prewarm`, and `GET /events?job=<id>&after=<seq>&wait=<ms>` | Chrome Native Messaging host (4-byte little-endian length-prefixed JSON on stdin/stdout); reads TCP port from `%LOCALAPPDATA%\HeyClicky\browser-bridge.port` and proxies JSON frames over `127.0.0.1:<port>` |

### Critical Discovery: How `clicky-harness.exe` Calls the LLM & Why Live Turns Fail (`AUD-J-001` & `AUD-J-002`)

By extracting the embedded JS bundle from `clicky-harness.exe` ([`docs/audit/evidence/ws-jk/agents-harness-bundle-excerpt.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-bundle-excerpt.txt) and [`docs/audit/evidence/ws-jk/agents-harness-drivers.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-drivers.txt)), we verified the exact internal architecture of `clicky-harness.exe`:

1. **Only `"h1-mac"` Is Registered (`AUD-J-002`)**:
   - Line 186 of `agents-harness-drivers.txt`:
     ```javascript
     Yf({drivers:{"h1-mac":{run:yf,steer:X_,cancel:R_}}});
     ```
   - When `POST /turn` is called, `rI(A)` (`agents-harness-bundle-excerpt.txt:123`) constructs `t = "${A.harness}-${A.place}"` and looks up `Xf[t]`.
   - Any request with `harness: "pi"` (`"pi-mac"` / `"pi-cloud"`), `harness: "h1", place: "cloud"` (`"h1-cloud"`), or `harness: "codex", place: "cloud"` (`"codex-cloud"`) throws `Error("No ${t} harness in this runner.")` (HTTP 500).
2. **7 Missing Kursor Backend Routes (`AUD-J-001`)**:
   - When `h1-mac` starts (`l_(A, e)`), it configures `globalThis.H1_RESPONSES_URL = "${worker.base}/agent/openai/v1/responses"` and routes Claude models (`claude-fable-5-1`, `claude-opus-5-5`, `claude-sonnet-5-5`, `claude-haiku-5-5`) to `"${worker.base}/agent/anthropic/v1/messages"`.
   - It also probes `GET ${worker.base}/agent/web-search` (`u5(e)`, `agents-harness-drivers.txt:116`), calls `POST ${worker.base}/agent/web-search` when executing `web_search`, calls `/agent/harness/lab/h1/jobs` and `/agent/harness/lab/h1/probe` when `place === "cloud"`, and connects to `http://127.0.0.1:8787/mcp/composio` (returned as `proxy_url` by `src/backend/src/routes/agents.ts:943`).
   - **None of these 7 routes exist in `src/backend/`** — every live probe against `http://127.0.0.1:8787` returns `HTTP 404 Not Found`.

---

## 3. 30-Scenario Agent Harness, Tool & Safety Execution Matrix

All 30 scenarios were executed via [`tests/audit/ws-jk/test-agents-harness.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-agents-harness.ps1) and recorded in [`docs/audit/evidence/ws-jk/agents-harness-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-test-results.json):

| # | Category | Scenario | Status | Observed Result |
|---:|---|---|---|---|
| 01 | `LiveBackend` | `h1-mac` OpenAI turn against live Kursor backend (`127.0.0.1:8787`) | `FAIL` | `turn_end status=failed, error='model: model: 404: 404 Not Found'` (`POST /agent/openai/v1/responses` missing: `AUD-J-001`) |
| 02 | `LiveBackend` | `h1-mac` Anthropic turn against live Kursor backend (`127.0.0.1:8787`) | `FAIL` | `turn_end status=failed, error='model: model: 404: 404 Not Found'` (`POST /agent/anthropic/v1/messages` missing: `AUD-J-001`) |
| 03 | `HarnessDrivers` | `POST /turn` with `harness='h1', place='cloud'` (`AgentHarness.H1 + Cloud`) | `FAIL` | `HTTP 500: {"error":"No h1-cloud harness in this runner."}` (`AUD-J-002`) |
| 04 | `HarnessDrivers` | `POST /turn` with `harness='codex', place='cloud'` and `harness='pi', place='mac'` | `FAIL` | `HTTP 500: {"error":"No codex-cloud harness in this runner."}` & `{"error":"No pi-mac harness in this runner."}` (`AUD-J-002`) |
| 05 | `H1Runtime` | `h1-mac` basic turn lifecycle & long-poll event stream (`answer` -> `stats` -> `turn_end`) | `PASS` | `status=completed, answerLen=114` |
| 06 | `H1Runtime` | `h1-mac` multi-turn state persistence in `h1-mac/<job>/h1.json` | `PASS` | `h1.json turn=2, lastSeq=10, jobId=scen-05-basic-qa` |
| 07 | `Tools` | `h1-mac` `update_plan` tool emission | `PASS` | `plan` event emitted with 2 steps (`completed`, `in_progress`) |
| 08 | `Tools` | `h1-mac` `progress` tool commentary emission | `PASS` | `commentary='Scanning workspace files now'` |
| 09 | `H1Runtime` | `h1-mac` reasoning summary `**bold heading**` extraction -> `thinking` event | `PASS` | `thinking='Analyzing system architecture'` |
| 10 | `Tools` | `h1-mac` `write` tool creating file in workspace | `PASS` | `created-by-agent.txt` created with expected content |
| 11 | `Tools` | `h1-mac` `read` tool reading workspace file | `PASS` | `tool` event `name=read, phase=start/end` emitted and content returned |
| 12 | `Tools` | `h1-mac` `edit` tool modifying workspace file in place | `PASS` | `Line 2` replaced with `Line 2 Edited` |
| 13 | `Tools` | `h1-mac` `bash` tool executing Windows PowerShell command | `PASS` | `tool name=bash` executed PowerShell and returned `PS_EXEC_OK:5` |
| 14 | `NoteTools` | `h1-mac` `note` tool composing `note.md` and emitting `note` (`phase=saved`) event | `PASS` | `noteSavedEvent=True, noteDiskLen=87` |
| 15 | `NoteTools` | `h1-mac` `note_add` incremental markdown edit on `note.md` | `PASS` | `note_add` inserted `- Added via note_add` under `## Findings` in `note.md` |
| 16 | `Tools` | `h1-mac` `exec` JS program sandbox (`store`, `load`, `progress`, `text`) | `PASS` | `progressEmitted=True, execOk=True` in `h1-rounds.jsonl` |
| 17 | `Tools` | `h1-mac` `fetch_url` HTML extraction & `program_step` `batch` event | `PASS` | `batch siteCount=1, sitesDone=1` |
| 18 | `Tools` | `h1-mac` `web_search` tool via Worker `/agent/web-search` & `batch` event | `PASS` | `searches count=1, done=1, query='Kursor agent audit'` |
| 19 | `Tools` | `h1-mac` `ask_user` pause (`pendingAsk` in `h1.json`) and answer resumption | `PASS` | `turn1 pendingAsk='1-0'`, `turn2` answered `Production` and cleared `pendingAsk` |
| 20 | `MCP` | `h1-mac` HTTP MCP server `initialize`, `tools/list`, and `tools/call` | `PASS` | `server=mock_mcp, ok=True, tools=1` |
| 21 | `MCP` | `h1-mac` Composio MCP failure -> emits `apps_unreachable` and completes turn | `PASS` | `apps_unreachable=True, turn_end=completed` |
| 22 | `MCP` | `h1-mac` local stdio custom MCP connector bridged via loopback HTTP proxy (`q_`) | `PASS` | `server=custom-stdio-123456, ok=True, tools=1` |
| 23 | `H1Runtime` | `POST /steer` mid-turn injection while turn is running | `PASS` | `steer HTTP=200, ok=True, turn_end=completed` |
| 24 | `H1Runtime` | `POST /cancel` mid-turn cancellation -> `turn_end status=interrupted` | `PASS` | `cancel HTTP=200, turn_end status=interrupted` |
| 25 | `H1Runtime` | `h1-mac` Anthropic Messages SSE adapter (`claude-sonnet-5-5` -> `/agent/anthropic/v1/messages`) | `PASS` | `turn_end=completed, answer='Anthropic SSE stream verified.'` |
| 26 | `Security` | Filesystem sandbox boundary check (`write` outside job workspace) | `FAIL` | `outsideFileCreated=True` outside workspace with zero sandbox or approval gate (`AUD-J-003`) |
| 27 | `Portability` | `WindowsInstructionAdapter.Adapt` vs `TurnPromptBuilder` macOS leftover check | `FAIL` | `TurnPromptBuilder.cs:38` injects unadapted `call the mac_step tool` (`AUD-J-006`) |
| 28 | `Turns` | `StructuredFinalReply.Parse` across all 9 XML tags (`<SUMMARY>`..`<LEARNED>`) | `PASS` | All 9 tags parsed and stripped accurately |
| 29 | `Routines` | `ClickyRoutine.NextSetTime` & `RoutineKeepAwake` system sleep lock audit | `FAIL` | `RoutineScheduler.cs:308` holds `PowerRequestSystemRequired` indefinitely whenever any routine is unpaused on AC power (`AUD-J-007`) |
| 30 | `Jobs` | `ClickyWorkFolderFiles.List` workspace scanning & hidden folder exclusion | `PASS` | Lists user artifacts while excluding `.clicky`, `tmp`, `node_modules`, `.git`, and symlinks |

---

## 4. WS-J Findings (`AUD-J-001` – `AUD-J-008`)

### ID: AUD-J-001
- **Workstream**: WS-J
- **Severity**: Critical
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Autonomous Agents (`clicky-harness.exe`) Completely Non-Functional Against Local Kursor Backend Due to 7 Missing Worker & MCP Proxy Routes (HTTP 404)
- **Component**: Agent Harness Sidecar <-> Local Kursor Backend (`src/backend/src/routes/agents.ts`)
- **Location**: `src/backend/src/routes/agents.ts:943`; `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` (embedded H1 runtime `l_` / `d5` / `u5`); `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerRuntime.cs:402-407`
- **Description**: When `AgentService` starts a turn via `AgentHarnessRunnerRuntime.RunAsync` (`AgentHarnessRunnerRuntime.cs:356-411`), it passes `worker: { base: "http://127.0.0.1:8787", sessionToken }` to `clicky-harness.exe` `POST /turn`. Inside `clicky-harness.exe`, the embedded H1 driver (`l_` in `agents-harness-drivers.txt`) rewrites `globalThis.H1_RESPONSES_URL` to `${worker.base}/agent/openai/v1/responses` and Anthropic requests to `${worker.base}/agent/anthropic/v1/messages`, probes `${worker.base}/agent/web-search` (`GET` and `POST`), routes cloud H1 jobs to `${worker.base}/agent/harness/lab/h1/jobs` and `/agent/harness/lab/h1/probe`, and connects to the Composio MCP proxy URL (`http://127.0.0.1:8787/mcp/composio`) returned by `POST /agent/composio/session` (`src/backend/src/routes/agents.ts:943`). None of these 7 routes are implemented anywhere in `src/backend/src/routes/*`. Every live agent turn against Kursor's local backend immediately fails with HTTP 404 (`"model: model: 404: 404 Not Found"`).
- **Reproduction Steps**:
  1. Start the Kursor local backend on `http://127.0.0.1:8787`.
  2. Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/audit/ws-jk/test-agents-harness.ps1` (Scenarios 1 & 2 and Section 5 Live Backend Probes).
  3. Observe that `POST /agent/openai/v1/responses`, `POST /agent/anthropic/v1/messages`, `GET /agent/web-search`, `POST /agent/web-search`, `POST /agent/harness/lab/h1/jobs`, `GET /agent/harness/lab/h1/probe`, and `POST /mcp/composio` all return `HTTP 404 Not Found`.
  4. Observe that `clicky-harness.exe` emits `turn_end` with `status="failed"` and `error="model: model: 404: 404 Not Found"`.
- **Observed Result**: All 7 backend proxy routes required by `clicky-harness.exe` return `HTTP 404 Not Found`; every autonomous agent turn fails on its first model call.
- **Expected Result**: `src/backend/src/routes/agents.ts` must implement `POST /agent/openai/v1/responses` (SSE proxy to OpenAI/OpenRouter), `POST /agent/anthropic/v1/messages` (SSE proxy to Anthropic/OpenRouter), `GET` & `POST /agent/web-search`, and `POST /mcp/composio` (JSON-RPC MCP handler).
- **Impact**: 100% of autonomous agent jobs, background tasks, and scheduled routines in Kursor fail immediately when executed against the local backend.
- **Remediation**: Implement `POST /agent/openai/v1/responses`, `POST /agent/anthropic/v1/messages`, `GET`/`POST /agent/web-search`, and `POST /mcp/composio` in `src/backend/src/routes/agents.ts`, forwarding streaming Responses/Messages SSE payloads to the configured LLM provider.
- **Effort**: L
- **Evidence**:
  - `src/backend/src/routes/agents.ts:936-949`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerRuntime.cs:356-411`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-002
- **Workstream**: WS-J
- **Severity**: High
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Unregistered Harness Drivers (`pi-mac`, `h1-cloud`, `codex-cloud`) and Hardcoded `/usr/bin/tar` in `clicky-harness.exe` Cause Immediate HTTP 500 Failures
- **Component**: Agent Harness Sidecar & Runtime Selection (`AgentHarnessRuntimes` / `clicky-harness.exe`)
- **Location**: `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRuntimes.cs:14-28`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs:14-68`; `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` (`agents-harness-drivers.txt:139, 186`)
- **Description**: Although `clicky-harness.exe` bundles `@earendil-works/pi-coding-agent` dead code in its Bun bundle, its entrypoint registers ONLY a single driver: `Yf({drivers:{"h1-mac":{run:yf,steer:X_,cancel:R_}}})` (`agents-harness-drivers.txt:186`). However, `AgentHarnessRuntimes.Create()` (`AgentHarnessRuntimes.cs:14-28`) routes `AgentHarness.Pi` (both `Mac` and `Cloud`), `AgentHarness.H1` with `AgentHarnessPlace.Cloud`, and `AgentHarness.Codex` with `AgentHarnessPlace.Cloud` to `AgentHarnessRunnerRuntime` (`clicky-harness.exe`). Selecting any of these in Developer Settings or agent defaults causes `POST /turn` to return HTTP 500 `{"error":"No <harness>-<place> harness in this runner."}`. Furthermore, `h5()` in `clicky-harness.exe` (`agents-harness-drivers.txt:139`) hardcodes `A5("/usr/bin/tar", ["-xzf", i, "-C", o])` when unpacking cloud workspaces, which always fails on Windows because `/usr/bin/tar` does not exist.
- **Reproduction Steps**:
  1. Launch `clicky-harness.exe` and send `POST /turn` with `{harness:'h1', place:'cloud'}`, `{harness:'codex', place:'cloud'}`, or `{harness:'pi', place:'mac'}` (Scenarios 3 & 4 in `tests/audit/ws-jk/test-agents-harness.ps1`).
  2. Inspect `agents-harness-drivers.txt` line 186 (`Yf({drivers:{"h1-mac":...}})`) and line 139 (`A5("/usr/bin/tar", ...)`).
- **Observed Result**: `clicky-harness.exe` returns HTTP 500 with `{"error":"No h1-cloud harness in this runner."}`, `{"error":"No codex-cloud harness in this runner."}`, and `{"error":"No pi-mac harness in this runner."}`.
- **Expected Result**: Either `clicky-harness.exe` must register drivers for all harness/place combinations routed to `AgentHarnessRunnerRuntime` (and use Windows `tar.exe` instead of `/usr/bin/tar`), or `AgentHarnessRuntimes` / `SettingsContent` must restrict selection to supported runtimes.
- **Impact**: Selecting Pi or Cloud harness modes in Developer Settings breaks all agent execution with an unhandled HTTP 500 runner error.
- **Remediation**: Restrict `AgentHarness` / `AgentHarnessPlace` options in `Kursor.Agents` to supported local drivers (`"h1-mac"` and local `Codex`), or implement the missing drivers in an open-source replacement harness runner and replace `"/usr/bin/tar"` with `"tar.exe"`.
- **Effort**: M
- **Evidence**:
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRuntimes.cs:14-28`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs:14-68`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-003
- **Workstream**: WS-J
- **Severity**: High
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Unrestricted PowerShell (`bash` Tool) and Filesystem Write Access Outside Job Workspace Without Sandbox or User Approval
- **Component**: Agent Harness Tool Execution & Safety Boundary
- **Location**: `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` (`agents-harness-bundle-excerpt.txt:123`, `agents-harness-drivers.txt:106-107`); `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs:88-115`
- **Description**: In `clicky-harness.exe` (`"h1-mac"`), the local box tools (`bash`, `write`, `edit`, `read`) execute directly with the user's full Windows account privileges. On Windows (`$f = true`), the `bash` tool executes arbitrary Windows PowerShell commands with a 300-second timeout (`bashTimeoutS: 300`, `agents-harness-drivers.txt:106, 133`), and the `write` and `edit` tools resolve any absolute or relative path on disk without enforcing that the target path stays within `A.workspace` (`agents-harness-bundle-excerpt.txt:123`: `We(n, a?.cwd || A)`). Similarly, `CodexSession.BuildExecArgs()` (`CodexSession.cs:96-99`) passes `-c approval_policy="never" -s danger-full-access`. There is no user confirmation/approval gate for shell execution or file writes outside the job workspace. Because agents routinely ingest untrusted external content via `fetch_url`, `web_search`, browser tabs, and Composio emails/messages, indirect prompt injection can execute arbitrary PowerShell commands or overwrite arbitrary user files.
- **Reproduction Steps**:
  1. Run Scenario 13 and Scenario 26 in `tests/audit/ws-jk/test-agents-harness.ps1`.
  2. Observe that in Scenario 26, a model response invoking `write` with `path="<tempStateDir>/outside-sandbox-proof.txt"` (outside the job's `workspace` directory) succeeds without error or approval prompt and creates the file on disk.
- **Observed Result**: The `write`, `edit`, and `bash` (PowerShell) tools execute arbitrary filesystem modifications outside the job workspace and arbitrary PowerShell commands with zero sandboxing or user approval.
- **Expected Result**: File `write`/`edit` operations should be confined to the job's workspace directory unless explicitly approved by the user, and shell command execution should require user approval or run inside a restricted sandbox.
- **Impact**: High risk of arbitrary code execution and data exfiltration via indirect prompt injection when agents process untrusted web pages, emails, or documents.
- **Remediation**: Enforce canonical path prefix validation against the job workspace directory for `write`/`edit` tools and gate PowerShell command execution or out-of-workspace writes behind a user approval prompt (similar to `TakeOverConsentCard` / `UploadApprovalCard`).
- **Effort**: M
- **Evidence**:
  - `docs/audit/evidence/ws-jk/agents-harness-bundle-excerpt.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs:88-115`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-004
- **Workstream**: WS-J
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Opaque Proprietary Sidecar Binaries (`clicky-harness.exe` & `ClickyBrowserHost.exe`) Signed by `Humansongs, Inc` With No Source in Repository
- **Component**: Agent Sidecar Binaries (`HarnessRuntime` & `ClickyBrowserHost`)
- **Location**: `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe`; `dist/kursor-desktop/ClickyBrowserHost/ClickyBrowserHost.exe`
- **Description**: Kursor relies on two precompiled sidecar executables in `dist/kursor-desktop/` that have no source code in `src/`:
  1. `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` (`88,566,080 bytes`, SHA-256 `31DFAF8C74BAB71F07607EAFA3D9940B82C7E5540FA0E825169278AC8E7A30DA`), a Bun 1.3.11 standalone executable containing an embedded minified JS bundle and `photon_rs_bg.wasm`.
  2. `dist/kursor-desktop/ClickyBrowserHost/ClickyBrowserHost.exe` (`4,685,632 bytes`, SHA-256 `E7BD3FD9179313B98C10A79B18E6631BBB8EE6C7FB264FA4825106F4DB084A86`), a Go-compiled binary (`Go build ID: "3l8p8J3eH5R-_tF52U0r/..."`) that bridges Chrome Native Messaging to the local TCP socket in `%LOCALAPPDATA%\HeyClicky\browser-bridge.port`.
  Both binaries carry valid Authenticode signatures from `CN="Humansongs, Inc"` (`Thumbprint 3F234731E458612EFEE78459EBC9FA556F198582`) and hardcode HeyClicky paths/strings.
- **Reproduction Steps**:
  1. Run `Get-FileHash` and `Get-AuthenticodeSignature` on `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` and `dist/kursor-desktop/ClickyBrowserHost/ClickyBrowserHost.exe` (Section 1 of `tests/audit/ws-jk/test-agents-harness.ps1`).
  2. Inspect `docs/audit/evidence/ws-jk/agents-browserhost-strings.txt` and `docs/audit/evidence/ws-jk/agents-harness-test-results.json`.
- **Observed Result**: Both sidecars are closed-source binaries signed by `Humansongs, Inc`; `ClickyBrowserHost.exe` hardcodes `%LOCALAPPDATA%\HeyClicky\browser-bridge.port` and `clicky-harness.exe` hardcodes `[HeyClicky]` prompts and `H1_PHOTON_WASM_PATH`.
- **Expected Result**: Kursor should build both sidecars from first-party TypeScript/C# source in `src/` so they can be audited, customized, and rebranded cleanly.
- **Impact**: Cannot patch bugs (such as `AUD-J-002` and `AUD-J-003`) or rebrand paths in the harness runner or browser native messaging host without replacing the binaries from source.
- **Remediation**: Replace `ClickyBrowserHost.exe` with a lightweight C# or Node native messaging proxy built from `src/`, and replace `clicky-harness.exe` with a TypeScript/Node or C# harness runner built from `src/`.
- **Effort**: L
- **Evidence**:
  - `docs/audit/evidence/ws-jk/agents-browserhost-strings.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-005
- **Workstream**: WS-J
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Hardcoded Author Workstation Path (`C:\Users\niles\...`) and Unreachable Node Launch Fallback in `AgentHarnessRunnerProcess.cs`
- **Component**: Agent Harness Process Launcher
- **Location**: `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs:61-78, 200-263`
- **Description**: In `AgentHarnessRunnerProcess.cs:255-263`, `ResolveLaunch()` checks only `BundledRunnerPath()` (`HarnessRuntime\clicky-harness.exe`) and returns `null` if it does not exist. Meanwhile, lines 200–253 define private helpers `RunnerScriptPath()` and `NodeExecutablePath()` (along with the `Launch.Node` enum variant at line 61) that are never called anywhere in the class (dead code). Additionally, line 75 hardcodes the original developer's source path `"C:\\Users\\niles\\code\\clicky-closed\\windows\\src\\HeyClicky.Agents\\Harness\\AgentHarnessRunnerRuntime.cs"` as the `[CallerFilePath]` default in `RunnerScriptFromSourceTree()`.
- **Reproduction Steps**:
  1. Inspect `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs` lines 61–78 and 200–263.
- **Observed Result**: `ResolveLaunch()` never calls `RunnerScriptPath()` or `NodeExecutablePath()`, leaving 55 lines of dead code and a hardcoded `C:\Users\niles\code\clicky-closed\...` path.
- **Expected Result**: `ResolveLaunch()` should either wire up the Node.js script fallback (allowing Kursor to run an open-source JS/TS harness runner when `clicky-harness.exe` is absent) or remove the dead methods and hardcoded workstation path.
- **Impact**: Prevents falling back to a source-tree Node.js harness script during development and leaks upstream developer path metadata.
- **Remediation**: Wire `RunnerScriptPath()` + `NodeExecutablePath()` into `ResolveLaunch()` as a fallback (or primary dev runner) and remove the hardcoded `C:\Users\niles\...` path.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs:61-78, 200-263`

---

### ID: AUD-J-006
- **Workstream**: WS-J
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Unadapted macOS Tool Name (`mac_step`) and Platform References Across `TurnPromptBuilder`, `ClickyBrowserBridge`, and `clicky-harness.exe`
- **Component**: Agent Prompt Builder, Browser Bridge & Harness Instructions
- **Location**: `src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/TurnPromptBuilder.cs:38`; `src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs:261, 879, 962`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs:55`; `dist/kursor-desktop/HarnessRuntime/clicky-harness.exe` (`agents-harness-drivers.txt:65-66`)
- **Description**: Although `WindowsInstructionAdapter.Adapt()` (`WindowsInstructionAdapter.cs:11-56`) scrubs `"macOS"`/`"Mac"`/`"mdfind"` from `BundledResources/clicky-model-instructions.md`, several runtime prompt builders bypass `WindowsInstructionAdapter` and inject macOS-specific tool names and strings directly into Windows agent turns:
  1. `TurnPromptBuilder.BuildFirstTurnPrompt()` (`TurnPromptBuilder.cs:38`) injects `"To use any other desktop app on the user's computer, call the mac_step tool (HeyClicky's screen agent)"`.
  2. `ClickyBrowserBridge.cs:261, 879, 962` returns tool guidance referencing `mac_step` when Chrome is unavailable or on a restricted page.
  3. `AgentHarnessNames.WireValue(AgentHarnessPlace.Mac)` (`AgentHarnessNames.cs:55`) uses the wire token `"mac"` on Windows.
  4. `clicky-harness.exe` (`agents-harness-drivers.txt:65-66`) injects `"<their_skills> The user's own skills, kept on this Mac"` even when `$f` (Windows) is true, and its `fetch_url` tool sends a hardcoded `"Macintosh; Intel Mac OS X 10_15_7"` User-Agent header.
- **Reproduction Steps**:
  1. Run Scenario 27 in `tests/audit/ws-jk/test-agents-harness.ps1`.
  2. Inspect `TurnPromptBuilder.cs:38`, `ClickyBrowserBridge.cs:261, 879, 962`, and `agents-harness-drivers.txt:65-66`.
- **Observed Result**: Windows agent prompts instruct the model to call `mac_step` and state that user skills are `"kept on this Mac"`.
- **Expected Result**: All Windows prompt builders and browser bridge error messages should use platform-appropriate terminology and pass through `WindowsInstructionAdapter`.
- **Impact**: Confuses the LLM regarding the host OS and causes failed tool calls when the model attempts to invoke an unregistered `mac_step` tool locally.
- **Remediation**: Update `TurnPromptBuilder.cs:38` and `ClickyBrowserBridge.cs:261, 879, 962` to reference the actual registered computer-use mechanism (`<COMPUTER_USE_REQUEST>` or Windows screen tool) and sanitize `<their_skills>` in the harness.
- **Effort**: S
- **Evidence**:
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/TurnPromptBuilder.cs:38`
  - `src/desktop/Kursor.Core/HeyClicky.Core.Browser/ClickyBrowserBridge.cs:261, 879, 962`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-007
- **Workstream**: WS-J
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: `RoutineScheduler` Holds Indefinite Win32 System Sleep Lock (`PowerRequestSystemRequired`) Whenever Any Routine Is Unpaused on AC Power
- **Component**: Scheduled Routines & Power Management (`RoutineScheduler` / `RoutineKeepAwake`)
- **Location**: `src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineScheduler.cs:112, 307-309`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineKeepAwake.cs:46-88`
- **Description**: In `RoutineScheduler.cs:112` and `307-309`, `RefreshKeepAwake()` calls `RoutineKeepAwake.Update(routines.Any((ClickyRoutine r) => !r.IsPaused))`. Inside `RoutineKeepAwake.Reconcile()` (`lines 46-68`), whenever `_hasUnpausedRoutines && OnAcPower()` is true, it creates a Win32 power request (`"HeyClicky scheduled routines"`) and calls `PowerSetRequest(_request, PowerRequestType.PowerRequestSystemRequired)` (`value 1`) and holds it indefinitely. Consequently, if a user has even a single weekly or daily routine enabled while plugged into AC power, Kursor prevents the Windows PC from ever entering system sleep—even when no routine is running and the next scheduled run is days away.
- **Reproduction Steps**:
  1. Run Scenario 29 in `tests/audit/ws-jk/test-agents-harness.ps1`.
  2. Inspect `RoutineScheduler.cs:112, 307-309` and `RoutineKeepAwake.cs:46-68`.
  3. With one unpaused routine and AC power connected, run `powercfg /requests` in an elevated prompt to observe the persistent `SYSTEM` power request.
- **Observed Result**: `PowerSetRequest(PowerRequestSystemRequired)` is held continuously 24/7 whenever any routine is unpaused on AC power.
- **Expected Result**: `PowerRequestSystemRequired` should only be held while a routine turn is actively executing (or within a short window around a due run), or wake timers should be used instead of blocking system sleep indefinitely.
- **Impact**: Prevents desktop and laptop PCs on AC power from ever sleeping once a user creates any scheduled routine, wasting energy and violating Windows power management expectations.
- **Remediation**: Acquire `PowerRequestSystemRequired` only while a routine is actively running in `FireAsync()` (or make `"Prevent sleep on AC power for routines"` an explicit opt-in toggle in Settings).
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineScheduler.cs:112, 307-309`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineKeepAwake.cs:46-88`
  - `docs/audit/evidence/ws-jk/agents-harness-test-results.json`

---

### ID: AUD-J-008
- **Workstream**: WS-J
- **Severity**: Low
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Severe Architectural Documentation Drift in `docs/06-agents-proactive-and-memory.md` vs. Actual `Kursor.Agents` Codebase
- **Component**: Agent Architecture Documentation (`docs/06-agents-proactive-and-memory.md`)
- **Location**: `docs/06-agents-proactive-and-memory.md:1-320`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs:1-147`; `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarness.cs:1-8`
- **Description**: `docs/06-agents-proactive-and-memory.md` contains major inaccuracies compared to the actual 122 C# files in `src/desktop/Kursor.Agents/`:
  1. It lists non-existent namespaces `HeyClicky.Agents.Codex`, `HeyClicky.Agents.Worker`, `HeyClicky.Agents.Integrations`, and `HeyClicky.Agents.Prompt` (actual namespaces are `HeyClicky.Agents`, `HeyClicky.Agents.Harness`, `HeyClicky.Agents.Jobs`, `HeyClicky.Agents.Memory`, `HeyClicky.Agents.Proactive`, `HeyClicky.Agents.Routines`, `HeyClicky.Agents.Runtime`, `HeyClicky.Agents.Sidecar`, and `HeyClicky.Agents.Turns`).
  2. §2.3 claims `CodexHomeManager.cs` writes `config.toml` with `base_url = "https://api.heyclicky.com/agent/codex"`, `approval_policy = "never"`, and `sandbox_mode = "danger-full-access"`, whereas `CodexHomeManager.cs` (147 lines) only writes `AGENTS.md`, `skills/`, and `skills-manifest.json` and never writes `config.toml` (those flags are passed via CLI arguments `-c approval_policy="never" -s danger-full-access` in `CodexSession.cs:96-99`).
  3. §1.2 claims `AgentHarness` has 27 enum values, whereas `AgentHarness.cs` has only 3 (`Codex, Pi, H1`) and the 27-value enum is `AgentModelOption.cs`.
- **Reproduction Steps**:
  1. Compare `docs/06-agents-proactive-and-memory.md` §1–§2 against `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs`, `CodexSession.cs`, `AgentHarness.cs`, and `AgentModelOption.cs`.
- **Observed Result**: Documentation references non-existent file paths/namespaces, attributes Codex CLI flags to a non-existent `config.toml` generator in `CodexHomeManager.cs`, and confuses `AgentHarness` (3 values) with `AgentModelOption` (27 values).
- **Expected Result**: `docs/06-agents-proactive-and-memory.md` should accurately reflect the 9 namespaces and class responsibilities in `src/desktop/Kursor.Agents/`.
- **Impact**: Misleads engineers and security auditors inspecting Codex/H1 configuration and namespace layout.
- **Remediation**: Update `docs/06-agents-proactive-and-memory.md` to match the actual 9 namespaces in `src/desktop/Kursor.Agents/`, document `CodexSession.BuildExecArgs()` as the source of `-c approval_policy="never" -s danger-full-access`, and distinguish `AgentHarness` from `AgentModelOption`.
- **Effort**: XS
- **Evidence**:
  - `docs/06-agents-proactive-and-memory.md:1-120`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs:1-147`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs:88-115`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarness.cs:1-8`
  - `src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentModelOption.cs:1-33`

---

# Part II — Workstream WS-K: App Shell & Every UI Surface (`src/desktop/Kursor.App/`)

## 5. Executive Summary & 33-Namespace Inventory (`src/desktop/Kursor.App/`)

Workstream **WS-K** audited the entire WinUI 3 / Win32 desktop shell and all UI surfaces in `src/desktop/Kursor.App/` (**885 C# files**, **132,000+ lines of C#** across 33 namespace/directory buckets), alongside runtime HWND/UIA/log inspection of the live `HeyClicky.exe` process and cryptographic round-trip testing of DPAPI-backed storage (`CustomConnectorStore` and `AuthTokenStore`).

### Complete 33-Namespace Bucket Inventory (`src/desktop/Kursor.App/`)

| # | Namespace / Directory Bucket | File Count | Primary Responsibility & Key Classes |
|---|---|---:|---|
| 1 | `(root)` (`<PrivateImplementationDetails>.cs`) | 1 | Compiler-generated static array initializers |
| 2 | `HeyClicky.App` | 14 | Application entry & orchestrator: `Program.cs`, `App.cs`, `AppHost.cs` (8,601 lines), `SingleInstance.cs`, `AppPaths.cs`, `Log.cs` |
| 3 | `HeyClicky.App.Billing` | 7 | Billing & Paywall UI: `BillingUi.cs` (598 lines), `PaywallCard.cs` (1,056 lines), `PaywallWindow.cs` (192 lines), `PaywallFit.cs`, `PaywallPalette.cs` |
| 4 | `HeyClicky.App.Browser` | 7 | Browser extension setup & drive orchestration: `ChromeSetupFlow.cs` (693 lines), `ChromeAutoInstall.cs` (137 lines), `ChromeInternalPages.cs`, `ChromeSetupBriefs.cs` |
| 5 | `HeyClicky.App.Demo` | 11 | Demo mode & showcase flows |
| 6 | `HeyClicky.App.Design` | 8 | Design system tokens & motion: `DS.cs`, `Anim.cs`, `Easing.cs`, `SwiftSpring.cs`, `SpringCurve.cs` |
| 7 | `HeyClicky.App.DevFakes` | 12 | Development/test fakes for offline UI previewing |
| 8 | `HeyClicky.App.Dock` | 87 | Top-right ArcDock agent deck: `ArcDock.cs` (2,373 lines), `ArcDockView.cs` (742 lines), `ArcCardView.cs` (1,248 lines), `TopArcLayout.cs`, `PaperStock.cs`, `DeckScrollGestures.cs`, `CardFooterRequest.cs` |
| 9 | `HeyClicky.App.Documents` | 28 | Built-in Markdown & Spreadsheet viewers: `MarkdownViewerContent.cs`, `MarkdownViewerTextView.cs`, `SpreadsheetDocumentView.cs`, `SpreadsheetGridRenderer.cs` (698 lines), `SpreadsheetGridView.cs` |
| 10 | `HeyClicky.App.Drive` | 13 | Desktop GUI takeover & consent overlays: `ClickyTakeOverController.cs` (1,713 lines), `TakeOverOverlay.cs` (547 lines), `TakeOverConsentCard.cs` (333 lines), `UploadApprovalCard.cs` (47 lines), `AgentDriveConsent.cs`, `DriveArt.cs` |
| 11 | `HeyClicky.App.HeyClicky_App_XamlTypeInfo` | 13 | WinUI 3 XAML metadata provider tables |
| 12 | `HeyClicky.App.Home` | 19 | Home workspace data sources, lesson playback & artifact thumbnails: `AgentHomeSource.cs`, `LessonService.cs` (612 lines), `ChatBlockViews.cs` (676 lines), `ArtifactThumbnails.cs`, `WebPageSnapshotter.cs` |
| 13 | `HeyClicky.App.Ink` | 269 | Screen Ink Notebook & 8 interactive widget families: `ClickyInkSheetView.cs` (857 lines), `ClickyInkTodo.cs`, `ClickyInkQuiz.cs`, `ClickyInkForm.cs`, `ClickyInkQuestionAnswers.cs`, `ClickyInkCriteria.cs`, `ClickyInkEvent.cs`, `ClickyInkConnect.cs`, `ClickyInkGalleryTile.cs` |
| 14 | `HeyClicky.App.Interop` | 5 | Win32 P/Invoke & security properties: `Native.cs`, `NonRudeWindow.cs`, `FullscreenGuard.cs`, `FileSecurityProperties.cs` |
| 15 | `HeyClicky.App.Notch` | 12 | Top-center Notch shell: `NotchView.cs` (1,332 lines), `NotchSurfaces.cs` (139 lines), `NotchGeometry.cs` (469 lines), `NotchCornerGlow.cs`, `NotchSilhouette.cs`, `NotchSystemLayer.cs` |
| 16 | `HeyClicky.App.Notch.Peek` | 46 | Notch Peek & inline chat/drawing transcript: `NotchPeekSurface.cs` (669 lines), `NotchChatView.cs`, `NotchChatComposer.cs` (1,143 lines), `ClickyDrawingWindowViewer.cs` (122 lines), `ClickyDrawingWindowView.cs` (919 lines), `NotchDocumentRenderers.cs` (336 lines) |
| 17 | `HeyClicky.App.Notch.Surfaces` | 32 | 22 Notch surface implementations: `ActivitySurface.cs` (491 lines), `TextInputSurface.cs` (313 lines), `TextResponseSurface.cs` (630 lines), `ComposerBox.cs` (850 lines), `ClickyUpdateSurface.cs` (633 lines), `OnboardingCardSurface.cs` (511 lines), `MorningSuggestionsSurface.cs` (272 lines), `DictationClipboardSurface.cs`, `MicrophoneTroubleSurface.cs`, `DocumentAccessSurface.cs`, `TutorialInviteSurface.cs` |
| 18 | `HeyClicky.App.Notch.Window` | 10 | Expanded conversation & workspace window: `HeyClickyWindow.cs` (3,108 lines), `HeyClickyWindowConversation.cs` (749 lines), `HeyClickyWindowOpeningFlight.cs` (520 lines), `HeyClickyWindowUsageRing.cs` |
| 19 | `HeyClicky.App.Onboarding` | 26 | Pre-signin intro & interactive tutorial: `IntroWindow.cs` (1,452 lines), `TutorialWindow.cs` (2,372 lines), `InterviewController.cs` (1,054 lines), `InterviewStage.cs` (652 lines), `CursorCompanion.cs` (756 lines), `HomeTour.cs` (419 lines) |
| 20 | `HeyClicky.App.OneBrain` | 10 | OneBrain morning brief & onboarding tour cards: `GoodMorningBrief.cs` (513 lines), `ClickyTourJobCards.cs` (789 lines), `DrawnOnboardingTour.cs` |
| 21 | `HeyClicky.App.Portraits` | 25 | Vector character portraits & expressions: `PortraitView.cs` (1,252 lines), `PortraitArt.cs` (1,121 lines), `ClickyRoster.cs` (499 lines) |
| 22 | `HeyClicky.App.Proactive` | 4 | Proactive background coordinator & tap voice capture: `ProactiveCoordinator.cs` (3,412 lines), `TapVoiceCapture.cs` (156 lines) |
| 23 | `HeyClicky.App.Services` | 15 | App services & crash/stall monitoring: `CoreAppSettings.cs` (272 lines), `JsonAppSettings.cs` (651 lines), `CrashReporting.cs` (596 lines), `MainThreadStallMonitor.cs`, `ConnectedApps.cs` |
| 24 | `HeyClicky.App.Services.Updates` | 7 | Velopack auto-updater & idle install policy: `UpdateService.cs` (583 lines), `IdleInstallPolicy.cs`, `UpdateWindow.cs` |
| 25 | `HeyClicky.App.Settings` | 23 | Settings window, 14 pages & stores: `SettingsWindow.cs` (49 lines), `SettingsContent.cs` (9,083 lines), `CustomConnectorStore.cs` (1,483 lines), `ConnectorAuth.cs` (685 lines), `BugReport.cs` (310 lines), `SettingsHost.cs` (210 lines), `ObsidianVault.cs` (119 lines), `DeveloperFlags.cs` (82 lines) |
| 26 | `HeyClicky.App.Shell` | 9 | Win32 window wrappers & tray/taskbar shell: `OverlayWindow.cs` (688 lines), `PanelWindow.cs` (333 lines), `TrayIcon.cs` (442 lines), `TaskbarButton.cs` (454 lines), `OneBrainStatsPanel.cs` (200 lines), `Displays.cs` (71 lines), `ZOrder.cs` (94 lines), `FocusWatch.cs` (125 lines), `TransparentBackdrop.cs` (62 lines) |
| 27 | `HeyClicky.App.UI` | 28 | Reusable WinUI 3 controls & accessibility peers: `Pressable.cs` (327 lines), `PressableAutomationPeer.cs` (112 lines), `GelButton.cs`, `GelToggle.cs`, `GelSurface.cs`, `ShadowLayer.cs`, `Ui.cs`, `Icons.cs` |
| 28 | `HeyClicky.App.Wiring` | 15 | Core-to-UI service bridges: `RealStack.cs` (725 lines), `OneBrainJobReports.cs` (492 lines), `HotkeyRouter.cs` (328 lines), `AgentToolHost.cs` (324 lines), `AgentVoiceRelay.cs` (248 lines), `DeferredCursorOverlay.cs` (233 lines) |
| 29 | `Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Common` | 1 | Windows App SDK bootstrapper helper |
| 30 | `Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS` | 2 | Undocked Reg-Free WinRT initializer |
| 31 | `Properties` | 1 | AssemblyInfo attributes |
| 32 | `Sentry.Generated` | 1 | Sentry source-generated initializer |
| 33 | `System.Text.RegularExpressions.Generated` / `WinRT.*` | 112 | Source-generated Regex (9), `WinRT.HeyClickyGenericHelpers` (97), `WinRT.HeyClickyVtableClasses` (6) |

---

## 6. Deep-Dive Audit by UI Surface Category

### 6.1 App Shell & Window Management (`HeyClicky.App` & `HeyClicky.App.Shell`)
- **`AppHost` (`HeyClicky.App/AppHost.cs:1-8601`)**: Orchestrates startup (`Start()`), `NotchView`, `ArcDock`, `HeyClickyWindow`, `IntroWindow`, `TutorialWindow`, `PaywallWindow`, `TrayIcon`, `TaskbarButton`, `OneBrainStatsPanel`, `HotkeyRouter`, and `RealStack`. Includes a 15s background watchdog thread on `Quit()` (`lines 2003-2099`) that releases stranded `SendInput` keys (`ClickyScreenActuator.ReleaseStrandedKeys("quitting")`). **Defect (`AUD-K-008`)**: `public async void SetCursorDocked(bool dock)` (`lines 2190-2204`) awaits `Services.Cursor.DockAsync()` outside any `try/catch` block.
- **`OverlayWindow` (`HeyClicky.App.Shell/OverlayWindow.cs:1-688`)**: Subclasses the WinUI 3 `Window` HWND via `SetWindowLongPtr(Hwnd, -4, ...)`, strips standard chrome (`~0xCF0000`), sets `WS_POPUP` and `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOPMOST`, calls `SetWindowDisplayAffinity(Hwnd, 0x11u)` (`WDA_EXCLUDEFROMCAPTURE`), and updates interactive hit regions via GDI regions (`SetInteractiveRectsPx`).
- **`PanelWindow` (`HeyClicky.App.Shell/PanelWindow.cs:1-333`)**: Borderless rounded panel shell used by `SettingsWindow`, `IntroWindow`, and `TutorialWindow`; handles `WM_DPICHANGED` (`msg 736`) by rescaling `(widthDip * Scale, heightDip * Scale)`.
- **`TrayIcon` (`HeyClicky.App.Shell/TrayIcon.cs:1-442`)**: Message-only window (`HWND_MESSAGE = -3`, class `"HeyClickyTray"`) with `Shell_NotifyIconW` and `TaskbarCreated` recovery.
- **`TaskbarButton` (`HeyClicky.App.Shell/TaskbarButton.cs:1-454`)**: Off-screen (`-32000, -32000`) iconic window (`"HeyClickyTaskbarButton"`) managed via COM `ITaskbarList`.
- **`OneBrainStatsPanel` (`HeyClicky.App.Shell/OneBrainStatsPanel.cs:1-200`)**: `340x380` DIP click-through HUD overlay refreshing every 1s when `ShowOneBrainStats` is enabled.

### 6.2 Notch, Peek, Expanded `HeyClickyWindow`, and `ArcDock`
- **`NotchView` (`NotchView.cs:1-1332`) & `NotchSurfaces` (`NotchSurfaces.cs:1-139`)**: Top-center `OverlayWindow` (`820 x envH` DIP) with `NotchSilhouette`, `NotchSystemShape`, `NotchCornerGlow`, `NotchWorkingSweep`, and file drag-and-drop (`OnDrop`, `lines 596-632`). Dispatches all **22 `NotchSurface` subclasses** (`ActivitySurface`, `TextInputSurface`, `TextResponseSurface`, `OnboardingCardSurface`, `AgentTaskSurface`, `RestingMarkSurface`, `ClickyUpdateSurface`, `AppUpdatedSurface`, `DictationClipboardSurface`, `MutedSpeakerSurface`, `MicrophoneTroubleSurface`, `MeetingCountdownSurface`, `TutorialInviteSurface`, `MorningSuggestionsSurface`, `IntegrationSuggestionSurface`, `IntegrationConnectedSurface`, `NotchPeekSurface`, `HandoffSurface`, `SkillPinnedSurface`, `IntroSkillPingSurface`, `AppTipSurface`, `DictionaryLearnedSurface`, `DocumentAccessSurface`).
- **`HeyClickyWindow` (`HeyClickyWindow.cs:1-3108`) & `HeyClickyWindowOpeningFlight` (`520 lines`)**: Multi-column conversation/ink/document workspace window persisting geometry and split ratio to `%LOCALAPPDATA%\HeyClicky\heyclicky-window.json`.
- **`ArcDock` (`ArcDock.cs:1-2373`, `ArcDockView.cs:1-742`, `ArcCardView.cs:1-1248`)**: Top-right fanned deck supporting up to 30 agent cards (`MaximumFaces = 30`), wheel/trackpad scrolling (`DeckScrollGestures.cs`), and interactive card footers (`CardFooterRequest.cs`).

### 6.3 Settings Window (All 14 Pages) & Cryptographic Storage
- **`SettingsWindow.Page` (`SettingsWindow.cs:13-29`, `SettingsContent.cs:8760-8985`)**: Implements **14 pages**: `General`, `Shortcuts`, `Audio`, `Appearance`, `Dictation`, `OneBrain`, `Agents`, `ConnectedApps`, `Browser`, `CustomConnectors`, `Account`, `Referral`, `Updates`, and `Developer`.
- **DPAPI Storage (`CustomConnectorStore.cs`, `AuthTokenStore.cs`)**: Verified round-trip encryption/decryption and wrong-entropy rejection in [`docs/audit/evidence/ws-jk/ui-storage-dpapi-test.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/ui-storage-dpapi-test.json).

### 6.4 Screen Ink Notebook & All 8 Interactive Ink Widgets (`HeyClicky.App.Ink` — 269 Files)
- **`ClickyInkSheetView.cs` (`857 lines`)** and **`ClickyInkSheetRowView.cs` (`86 lines`)** render and freeze settled rows across all 8 interactive ink widget families: `ClickyInkTodo`, `ClickyInkQuiz`, `ClickyInkForm`, `ClickyInkQuestionAnswers`, `ClickyInkCriteria`, `ClickyInkEvent`, `ClickyInkConnect`, and `ClickyInkGalleryTile`.

### 6.5 Drive Overlay, Onboarding, and Billing
- **`TakeOverOverlay.cs` (`547 lines`)**, **`TakeOverConsentCard.cs` (`333 lines`, 45s timeout)**, and **`UploadApprovalCard.cs` (`47 lines`, 120s timeout)**.
- **`IntroWindow.cs` (`1,452 lines`, 5 steps)**, **`TutorialWindow.cs` (`2,372 lines`, 10 steps)**, **`PaywallWindow.cs` (`192 lines`)**, **`PaywallCard.cs` (`1,056 lines`)**, and **`BillingUi.cs` (`598 lines`)**.

---

## 7. WS-K Findings (`AUD-K-001` – `AUD-K-010`)

### ID: AUD-K-001
- **Workstream**: WS-K
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Dead/Orphaned `FriendCodeGroup()` UI on Referral Settings Page Prevents Claiming Referral Codes
- **Component**: Settings Window / Invite & Earn (Referral Page)
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:6142-6172, 6940-7019`
- **Description**: `SettingsContent.cs` fully implements `private FrameworkElement FriendCodeGroup()` (lines 6940–6988) and `private async Task ClaimFriendCodeAsync()` (lines 6990–7019) to provide a text input and `"Apply"` button that calls `host.ClaimReferralCodeAsync(text)` (`POST /me/referral/claim`). However, `BuildReferralPage()` (lines 6142–6172) constructs a fixed 7-element `FrameworkElement[]` array containing `HeldCodeCard()`, `NegativeBalanceNote()`, `TermsCard()`, `EarningsGroup()`, `FriendsGroup()`, `EstimatorGroup()`, and `PayoutGroup()`, and never adds `FriendCodeGroup()` to the visual tree.
- **Reproduction Steps**:
  1. Open Kursor Settings and navigate to the `"Invite & Earn"` (`Referral`) page.
  2. Scroll through the entire Referral page looking for the `"Friend's code"` input box and `"Apply"` button.
  3. Inspect `BuildReferralPage()` at `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:6142-6172` and search for callers of `FriendCodeGroup()` (defined at line 6940).
- **Observed Result**: `FriendCodeGroup()` has zero callers in the codebase; the `"Friend's code"` input group is never rendered on the Referral page.
- **Expected Result**: `BuildReferralPage()` should include `FriendCodeGroup()` so signed-in users without an existing claimed referral can enter and apply a friend's referral code.
- **Impact**: Users cannot manually enter or redeem a friend's referral code from the Settings UI, making the `POST /me/referral/claim` manual redemption flow unreachable.
- **Remediation**: Include `FriendCodeGroup()` in the `FrameworkElement[]` array returned by `BuildReferralPage()` in `SettingsContent.cs` (e.g., immediately after `HeldCodeCard()` or `FriendsGroup()`).
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:6142-6172`
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:6940-7019`
  - `docs/audit/evidence/ws-jk/ui-static-analysis.json`

---

### ID: AUD-K-002
- **Workstream**: WS-K
- **Severity**: High
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Universal `Pressable` Control Lacks Keyboard Focus (`IsTabStop`) and `Enter`/`Space` Key Handling
- **Component**: App-Wide UI Primitives / Accessibility
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.UI/Pressable.cs:18, 181-242`; `src/desktop/Kursor.App/HeyClicky.App.UI/PressableAutomationPeer.cs:8-112`
- **Description**: `Pressable` is the universal interactive control used for buttons (`GelButton`, `GelActionButton`), toggle switches (`GelToggle`), sidebar navigation rows, cards, and action chips across Settings, Notch, ArcDock, Home, Paywall, and Onboarding. `Pressable` inherits from `Microsoft.UI.Xaml.Controls.Grid` (`Pressable.cs:18`), where `UIElement.IsTabStop` defaults to `false`. Its constructor (`Pressable.cs:181-242`) only subscribes to `PointerEntered`, `PointerExited`, `PointerPressed`, `PointerReleased`, and `PointerCaptureLost`; it never sets `IsTabStop = true`, never displays a keyboard focus visual, and never handles `KeyDown` for `VirtualKey.Enter` or `VirtualKey.Space`. Across all 885 C# files in `Kursor.App`, `IsTabStop = true` is set in only 6 places.
- **Reproduction Steps**:
  1. Open Kursor Settings (or Intro/Tutorial/Paywall/Home window).
  2. Press the `Tab` key repeatedly to navigate between sidebar items, toggle switches, and action buttons, and press `Enter` or `Space` to activate a control.
  3. Inspect `src/desktop/Kursor.App/HeyClicky.App.UI/Pressable.cs:181-242` and `docs/audit/evidence/ws-jk/ui-static-analysis.json`.
- **Observed Result**: `Tab` navigation skips all `Pressable` controls (sidebar items, `GelButton`, `GelToggle`, cards, close buttons), and pressing `Enter` or `Space` does not trigger `Click`.
- **Expected Result**: Interactive `Pressable` controls should set `IsTabStop = true` (when enabled), `UseSystemFocusVisuals = true`, and invoke `RaiseClick()` on `KeyDown` for `VirtualKey.Enter` and `VirtualKey.Space`.
- **Impact**: Severe accessibility failure (WCAG 2.1.1 Keyboard): keyboard-only and assistive-technology users cannot navigate or operate the Settings window, Onboarding flows, Paywall cards, or Notch/Dock actions via keyboard.
- **Remediation**: In `Pressable(bool handCursor = true)`, set `IsTabStop = handCursor; UseSystemFocusVisuals = true;`, update `IsTabStop` when `Enabled` changes, and add a `KeyDown` handler that invokes `RaiseClick()` when `Enabled && (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)`.
- **Effort**: S
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.UI/Pressable.cs:18, 181-242`
  - `src/desktop/Kursor.App/HeyClicky.App.UI/PressableAutomationPeer.cs:8-112`
  - `docs/audit/evidence/ws-jk/ui-static-analysis.json`

---

### ID: AUD-K-003
- **Workstream**: WS-K
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: `DeveloperFlags.FullAgentActivity` Getter Hardcoded to `false` and Developer Settings Page Hidden Behind Server Flag
- **Component**: Settings Window / Developer Page & `DeveloperFlags`
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/DeveloperFlags.cs:17-42`; `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsHost.cs:64`; `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:4195-4203, 8771`
- **Description**: In `DeveloperFlags.cs:17-27`, the property `FullAgentActivity` has a getter that unconditionally returns `false` (`get { return false; }`) while its setter writes `"full_agent_activity_debug"` to `%LOCALAPPDATA%\HeyClicky\developer-flags.json` via `Write("full_agent_activity_debug", value)`. Because the getter never calls `DeveloperFlags.Read("full_agent_activity_debug")`, the private `Read(string key)` helper (`lines 31-42`) is completely dead code and the `"Full agent activity"` toggle in `SettingsContent.cs:4195-4203` can never read back `true`. Additionally, `SettingsContent.cs:8771` only adds the `Developer` page to the sidebar when `host.ShowsDeveloperSection` is true (`SettingsHost.cs:64`: `Agents?.AllowsDeveloperModelSelection ?? false`).
- **Reproduction Steps**:
  1. Inspect `src/desktop/Kursor.App/HeyClicky.App.Settings/DeveloperFlags.cs` lines 17–42.
  2. Observe that `FullAgentActivity.get` returns literal `false` instead of `Read("full_agent_activity_debug")`.
  3. Inspect `SettingsContent.cs:8771` and `SettingsHost.cs:64` to see that `SettingsWindow.Page.Developer` is omitted from the sidebar unless `Agents.AllowsDeveloperModelSelection` is true.
- **Observed Result**: `DeveloperFlags.FullAgentActivity` always returns `false` regardless of `developer-flags.json` contents, and the Developer settings section is hidden for standard/local accounts unless `AllowsDeveloperModelSelection` is true.
- **Expected Result**: `DeveloperFlags.FullAgentActivity.get` should return `Read("full_agent_activity_debug")`, and developer/debug builds or local dev mode should allow accessing the Developer settings page.
- **Impact**: Toggling `"Full agent activity"` in Developer settings has no effect at runtime because every caller reading `DeveloperFlags.FullAgentActivity` receives `false`.
- **Remediation**: Change `DeveloperFlags.FullAgentActivity.get` to `return Read("full_agent_activity_debug");` and allow `ShowsDeveloperSection` when running in local dev mode or when a developer environment variable/flag is set.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/DeveloperFlags.cs:17-42`
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsHost.cs:64`
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:4195-4203, 8771`

---

### ID: AUD-K-004
- **Workstream**: WS-K
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: `BugReport.Run()` Hardcodes Gmail Webmail URL to `hi@heyclicky.com` and Uploads Local Logs/State to Sentry
- **Component**: Settings Window / General & System Tray Bug Reporting
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/BugReport.cs:75, 91-168`; `src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs:235-265, 484-497`
- **Description**: Clicking `"Report a bug"` in Settings -> General (`SettingsContent.cs:4598-4601`) or the System Tray context menu (`TrayIcon.cs:299`) invokes `BugReport.Run(host)` (`BugReport.cs:27-97`). `BugReport.Run()` constructs a hardcoded Google Mail web compose URL (`"https://mail.google.com/mail/?view=cm&fs=1&to=hi%40heyclicky.com&su=..."` at line 75) and opens it in the browser, failing for users who do not use Gmail webmail and directing reports to HeyClicky's support address. Simultaneously, if `CrashReporter.Current.IsEnabled` is true, `SendToSentry()` (`lines 99-168`) bundles up to 3 MB of local log files and `state-<id>.json` (containing full settings, audio devices, permissions, and user email) and uploads them to Sentry alongside `CrashReporting.cs` (which sets `SendDefaultPii = true` and uploads minidumps up to 20 MB on startup).
- **Reproduction Steps**:
  1. Click `"Report a bug"` in Settings -> General or right-click the system tray icon and click `"Report a bug..."`.
  2. Inspect `src/desktop/Kursor.App/HeyClicky.App.Settings/BugReport.cs` lines 75 and 91–168.
- **Observed Result**: Opens `mail.google.com` addressed to `hi@heyclicky.com` with subject `"hey clicky — bug report (#<id>)"` and uploads logs/`state-<id>.json` to Sentry.
- **Expected Result**: Bug reporting should use a configurable Kursor support URL or standard `mailto:` URI and obtain explicit user consent before uploading local logs and state snapshots.
- **Impact**: Users without Gmail webmail cannot submit the prefilled bug report email, reports go to `hi@heyclicky.com` instead of Kursor, and diagnostic logs/PII are uploaded without a consent prompt.
- **Remediation**: Replace the hardcoded Gmail compose URL and `hi@heyclicky.com` address in `BugReport.cs` with a configurable support endpoint or `mailto:` link, and gate log/state attachment uploads behind user confirmation.
- **Effort**: S
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/BugReport.cs:75, 91-168`
  - `src/desktop/Kursor.App/HeyClicky.App.Services/CrashReporting.cs:235-265, 484-497`
  - `docs/audit/evidence/ws-jk/ui-static-analysis.json`

---

### ID: AUD-K-005
- **Workstream**: WS-K
- **Severity**: Low
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Incomplete Kursor Rebranding and Unported macOS Holdovers (`⌘`, `mac_step`, `HEYCLICKY`) Across UI Surfaces
- **Component**: App-Wide UI Copy, Window Titles, and Browser/Agent Steering
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:2040, 8769`; `src/desktop/Kursor.App/HeyClicky.App.Browser/ChromeSetupFlow.cs:55, 57, 65, 364`; `src/desktop/Kursor.App/HeyClicky.App.Browser/ChromeAutoInstall.cs:103`; `src/desktop/Kursor.App/HeyClicky.App.Billing/BillingUi.cs:146, 503`
- **Description**: Rebranding from HeyClicky to Kursor was applied to `SettingsWindow.cs:35` (`"Kursor Settings"`), `IntroWindow.cs:207` (`"Kursor"`), and `TrayIcon.cs:297, 304` (`"Pause Kursor"`, `"Quit Kursor"`), but dozens of UI surfaces still display `"HeyClicky"` or macOS-specific symbols/tools:
  1. `SettingsContent.cs:8769` renders the main sidebar header as `"HEYCLICKY"`.
  2. `SettingsContent.cs:2040, 8769, 8937` uses the macOS Command glyph (`"⌘"`) for the Windows `Shortcuts` page.
  3. `SettingsHost.cs:71` formats the footer as `"HeyClicky {version}"`.
  4. `ChromeSetupFlow.cs:55, 57, 65` instructs the agent to `"call mac_step (HeyClicky does it on their screen while they watch)"` when Chrome is not connected, and line 364 logs `"jobs aren't ported on Windows yet"`.
  5. `ChromeAutoInstall.cs:103` hardcodes `"https://chromewebstore.google.com/detail/heyclicky/bphdnlcghkpiaoiplkhfofpfboddahce"`.
  6. `OverlayWindow` titles in `NotchView.cs:190` (`"HeyClicky Notch"`), `PaywallWindow.cs:41` (`"HeyClicky"`), `TakeOverOverlay.cs:77, 438` (`"HeyClicky drive"` / `"HeyClicky is driving"`), and `OneBrainStatsPanel.cs:91` (`"HeyClicky one brain stats"`) retain HeyClicky naming.
- **Reproduction Steps**:
  1. Open Kursor Settings and inspect the top-left sidebar group header (`"HEYCLICKY"`), the Shortcuts icon (`"⌘"`), the footer (`"HeyClicky ..."`), General page (`"Pause HeyClicky"`), and Account page (`"Quit HeyClicky"`).
  2. Inspect `ChromeSetupFlow.cs:55-65` and `ChromeAutoInstall.cs:103`.
  3. Review the full list of occurrences in `docs/audit/evidence/ws-jk/ui-static-analysis.json`.
- **Observed Result**: UI surfaces mix `"Kursor"` and `"HeyClicky"` branding, display macOS `"⌘"` glyphs on Windows, and steer agents to call the macOS tool name `mac_step`.
- **Expected Result**: All user-facing titles, sidebar headers, support links, Windows glyphs, and agent steering prompts should consistently use Kursor branding and Windows tool names.
- **Impact**: Inconsistent product identity across windows/menus and confusing macOS references (`⌘`, `mac_step`) on Windows.
- **Remediation**: Centralize brand constants (product name, support email, web store URL) and replace `"HEYCLICKY"`, `"⌘"`, and `"mac_step"` across `SettingsContent.cs`, `SettingsHost.cs`, `ChromeSetupFlow.cs`, `BillingUi.cs`, `UpdateService.cs`, and overlay windows.
- **Effort**: M
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:2040, 2309, 4247, 4524, 4556, 8769, 8937`
  - `src/desktop/Kursor.App/HeyClicky.App.Browser/ChromeSetupFlow.cs:55, 57, 65, 364`
  - `src/desktop/Kursor.App/HeyClicky.App.Browser/ChromeAutoInstall.cs:103`
  - `docs/audit/evidence/ws-jk/ui-static-analysis.json`

---

### ID: AUD-K-006
- **Workstream**: WS-K
- **Severity**: Low
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: `CustomConnectorStore` Filename (`connectors.json` vs `custom-connectors.json`) and Env-Var Prefix Drift
- **Component**: Settings / Custom MCP Connectors Storage
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/CustomConnectorStore.cs:977-985, 1427`
- **Description**: `CustomConnectorStore.cs:983-985` persists custom MCP connector definitions in `%LOCALAPPDATA%\HeyClicky\connectors.json` and DPAPI-encrypted secrets in `%LOCALAPPDATA%\HeyClicky\connectors.secrets` using entropy `"HeyClicky.Connectors.v1"` (line 977), whereas `docs/05-platform-win32-and-ui-shell.md` §8 documents the file as `custom-connectors.json`. Additionally, `CustomConnectorStore.Registrations()` (line 1427) prefixes injected environment variables for stdio/HTTP MCP servers with `"HEYCLICKY_" + item.ServerKey.ToUpperInvariant()`.
- **Reproduction Steps**:
  1. Inspect `src/desktop/Kursor.App/HeyClicky.App.Settings/CustomConnectorStore.cs` lines 977–985 and 1427.
  2. Compare with `docs/05-platform-win32-and-ui-shell.md` §8 and `docs/audit/evidence/ws-jk/ui-storage-dpapi-test.json`.
- **Observed Result**: `CustomConnectorStore` writes `connectors.json` and `connectors.secrets` (with DPAPI entropy `"HeyClicky.Connectors.v1"`) and injects `HEYCLICKY_<SERVERKEY>_HEADER_*` environment variables.
- **Expected Result**: Documentation and implementation should agree on `connectors.json` + `connectors.secrets` and Kursor environment variable prefixes.
- **Impact**: Tools or migration scripts looking for `custom-connectors.json` will miss user-configured MCP connectors.
- **Remediation**: Align documentation with `connectors.json` / `connectors.secrets` and support `KURSOR_` (with fallback to `HEYCLICKY_`) environment variable prefixes.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/CustomConnectorStore.cs:977-985, 1427`
  - `docs/audit/evidence/ws-jk/ui-storage-dpapi-test.json`

---

### ID: AUD-K-007
- **Workstream**: WS-K
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Dead/Duplicate `JsonAppSettings` Class Uses Incompatible `settings.json` Schema and `--background` Startup Flag
- **Component**: App Services / Settings Persistence
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Services/JsonAppSettings.cs:12-651`; `src/desktop/Kursor.App/HeyClicky.App.Services/CoreAppSettings.cs:8-272`; `src/desktop/Kursor.Core/HeyClicky.Core.Settings/SettingsStore.cs:37-45`
- **Description**: `Kursor.App` contains two competing `IAppSettings` implementations: `CoreAppSettings` (which wraps `HeyClicky.Core.Settings.SettingsStore` and persists the full `AppSettings` schema to `%LOCALAPPDATA%\HeyClicky\settings.json`) and `JsonAppSettings` (a 651-line standalone class with its own source-generated serializer for a 10-field `Data` class that also defaults to `Path.Combine(AppPaths.Root, "settings.json")`). If `JsonAppSettings` is instantiated with its default directory and `Save()` is called (line 643), it overwrites `%LOCALAPPDATA%\HeyClicky\settings.json` with only 10 PascalCase properties, wiping out hotkeys, audio devices, dictation words, and agent preferences. Furthermore, `JsonAppSettings.LaunchAtLogin` (line 611) writes `"\"<exe>\" --background"` to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, whereas `Program.cs` / `LaunchAtLogin.cs` expects `--autostart`.
- **Reproduction Steps**:
  1. Inspect `src/desktop/Kursor.App/HeyClicky.App.Services/JsonAppSettings.cs` lines 14–35, 600–618, and 623–649.
  2. Compare with `CoreAppSettings.cs` and `SettingsStore.cs`.
- **Observed Result**: `JsonAppSettings` targets the same `settings.json` filename with an incompatible 10-field schema and writes `--background` to the Windows Run registry key.
- **Expected Result**: Only `CoreAppSettings` / `SettingsStore` should manage `%LOCALAPPDATA%\HeyClicky\settings.json`, or `JsonAppSettings` should be removed/isolated to a test-only filename.
- **Impact**: Accidental use of `JsonAppSettings` clobbers the user's primary `settings.json` file and writes an unrecognized startup flag to the registry.
- **Remediation**: Remove `JsonAppSettings.cs` (or restrict it to an isolated test file name like `devfakes-settings.json`) so `SettingsStore` is the sole owner of `settings.json`.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Services/JsonAppSettings.cs:12-35, 600-650`
  - `src/desktop/Kursor.App/HeyClicky.App.Services/CoreAppSettings.cs:8-272`
  - `src/desktop/Kursor.Core/HeyClicky.Core.Settings/SettingsStore.cs:37-45`

---

### ID: AUD-K-008
- **Workstream**: WS-K
- **Severity**: Medium
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Unguarded `async void` UI Entrypoints and Fire-and-Forget Spreadsheet Loader Risk Unhandled Exceptions and Infinite Spinner
- **Component**: Notch Drawing Viewer / AppHost Cursor Dock / Spreadsheet Document View
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Notch.Peek/ClickyDrawingWindowViewer.cs:28-77`; `src/desktop/Kursor.App/HeyClicky.App/AppHost.cs:2190-2204`; `src/desktop/Kursor.App/HeyClicky.App.Documents/SpreadsheetDocumentView.cs:22-44`
- **Description**: Three UI async paths lack exception handling:
  1. `ClickyDrawingWindowViewer.Present(StoredLesson, LessonService)` (`line 28`) is declared `public async void Present(...)` with no `try/catch` around `await newPlayer.LoadAsync(...)` or subsequent `OverlayWindow` creation.
  2. `AppHost.SetCursorDocked(bool)` (`line 2190`) is `public async void SetCursorDocked(bool dock)` and awaits `Services.Cursor.DockAsync(default(PointD))` at line 2201 outside any `try/catch` block.
  3. `SpreadsheetDocumentView` (`line 26`) invokes `LoadAsync(fallback)` fire-and-forget from its constructor without a `try/catch` inside `LoadAsync` (`lines 29-44`), so any exception thrown by `SpreadsheetAssetCache.LoadAsync(Path)` faults unobserved and leaves the `"Loading document…"` `ProgressRing` spinning forever.
- **Reproduction Steps**:
  1. Inspect `ClickyDrawingWindowViewer.cs:28-77`, `AppHost.cs:2190-2204`, and `SpreadsheetDocumentView.cs:22-44`.
  2. Pass a corrupt or inaccessible spreadsheet path to `SpreadsheetDocumentView` where `SpreadsheetAssetCache.LoadAsync` throws.
- **Observed Result**: Exceptions in `Present()` or `SetCursorDocked()` propagate as unhandled `async void` exceptions on the UI synchronization context; exceptions in `SpreadsheetDocumentView.LoadAsync()` leave the loading spinner active indefinitely.
- **Expected Result**: All `async void` methods and fire-and-forget UI loaders should wrap awaits in `try/catch` blocks and render fallback error states.
- **Impact**: Potential UI thread crash or permanently stuck loading spinner when opening a corrupt lesson or spreadsheet.
- **Remediation**: Wrap the body of `ClickyDrawingWindowViewer.Present()`, the early return branch of `AppHost.SetCursorDocked()`, and `SpreadsheetDocumentView.LoadAsync()` in `try/catch` blocks that log the error and display the fallback view.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Notch.Peek/ClickyDrawingWindowViewer.cs:28-77`
  - `src/desktop/Kursor.App/HeyClicky.App/AppHost.cs:2190-2204`
  - `src/desktop/Kursor.App/HeyClicky.App.Documents/SpreadsheetDocumentView.cs:22-44`

---

### ID: AUD-K-009
- **Workstream**: WS-K
- **Severity**: Low
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Trailing `null` Element in `SettingsContent.AgentsPage()` Autonomy Group Array
- **Component**: Settings Window / Agents Page
- **Location**: `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:2711-2728`
- **Description**: In `SettingsContent.AgentsPage()` (lines 2711–2728), the `"Autonomy"` settings group is constructed by passing `new FrameworkElement[3]` to `SK.Group("Autonomy", ...)`, where elements 0 and 1 are populated (`"Use my browser"` and `"Work across my apps"`) but element 2 is left as `null` after a third toggle was removed.
- **Reproduction Steps**:
  1. Inspect `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs` lines 2711–2728.
- **Observed Result**: `new FrameworkElement[3]` is allocated with index 2 uninitialized (`null`).
- **Expected Result**: The array passed to `SK.Group("Autonomy", ...)` should have length 2 with no trailing `null` element.
- **Impact**: Minor code hygiene hazard; any group layout helper that does not filter `null` entries would throw a `NullReferenceException`.
- **Remediation**: Change `new FrameworkElement[3]` to `new FrameworkElement[2]` at `SettingsContent.cs:2711`.
- **Effort**: XS
- **Evidence**:
  - `src/desktop/Kursor.App/HeyClicky.App.Settings/SettingsContent.cs:2711-2728`

---

### ID: AUD-K-010
- **Workstream**: WS-K
- **Severity**: Low
- **Confidence**: Confirmed
- **Status**: FAIL
- **Title**: Architectural Documentation Class & Namespace Drift Across `Kursor.App` UI Surfaces
- **Component**: Architecture Documentation vs. `Kursor.App` Codebase
- **Location**: `src/desktop/Kursor.App/HeyClicky.App/AppHost.cs:1`; `src/desktop/Kursor.App/HeyClicky.App.Notch.Surfaces/ActivitySurface.cs:17`; `src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkSheetView.cs:16`
- **Description**: Several class names and file locations documented in `docs/05-platform-win32-and-ui-shell.md` and `docs/04-drawing-2d-3d-and-ink-engine.md` differ from the actual decompiled `Kursor.App` codebase:
  1. `AppHost.cs` is in `HeyClicky.App/AppHost.cs` (8,601 lines), not `HeyClicky.App.Wiring/AppHost.cs`.
  2. Notch listening/thinking/speaking states are implemented by `ActivitySurface.cs` (not `ListeningSurface.cs`), text replies by `TextResponseSurface.cs` (not `ResponseSurface.cs`), mic errors by `MicrophoneTroubleSurface.cs` (not `MicrophoneDisconnectedSurface.cs`), and permission prompts by `OnboardingCardSurface.cs` and `DocumentAccessSurface.cs` (not `PermissionPromptSurface.cs`).
  3. Notch chat transcript is `NotchChatView.cs` (not `NotchChatTranscriptView.cs`).
  4. Screen Ink Notebook sheet view is `ClickyInkSheetView.cs` (857 lines, not `ClickyInkInteractiveSheetView.cs`).
  5. Home views are implemented inside `HeyClickyWindow.cs` + `HeyClicky.App.Home/*` sources rather than standalone `HomeWindow.cs` / `HomeView.cs` classes.
- **Reproduction Steps**:
  1. Compare class names in `docs/05-platform-win32-and-ui-shell.md` and `docs/04-drawing-2d-3d-and-ink-engine.md` against `docs/audit/evidence/ws-jk/ui-namespace-surface-inventory.json`.
- **Observed Result**: Multiple documented UI surface filenames do not match the actual filenames in `src/desktop/Kursor.App/`.
- **Expected Result**: Architecture docs should reference the exact namespace and class names present in `src/desktop/Kursor.App/`.
- **Impact**: Engineers and automated scripts navigating the 885-file `Kursor.App` project by documented class names encounter missing file paths.
- **Remediation**: Update `docs/04-drawing-2d-3d-and-ink-engine.md` and `docs/05-platform-win32-and-ui-shell.md` with the exact class names from `ui-namespace-surface-inventory.json`.
- **Effort**: XS
- **Evidence**:
  - `docs/audit/evidence/ws-jk/ui-namespace-surface-inventory.json`
  - `src/desktop/Kursor.App/HeyClicky.App/AppHost.cs:1`
  - `src/desktop/Kursor.App/HeyClicky.App.Notch/NotchSurfaces.cs:20-138`
  - `src/desktop/Kursor.App/HeyClicky.App.Ink/ClickyInkSheetView.cs:16`

---

## 8. Manual Verification Checklist for Interactive GUI Surfaces (`UNVERIFIED`)

Because the audit shell runs in a non-interactive window station where cross-desktop GDI `CopyFromScreen` and live OAuth/microphone/Stripe browser interactions cannot be driven without interrupting the active user desktop (`PID 20080`), the following interactive flows are marked `UNVERIFIED` for live GUI click-through and provided with a step-by-step manual verification checklist:

1. **Pre-SignIn `IntroWindow` & OAuth Loopback/Protocol Sign-In (`IntroWindow.cs`)**:
   - [ ] Trigger `"Reset Onboarding"` from the System Tray menu (`TrayIcon.cs:302`).
   - [ ] Verify `IntroWindow` opens centered (`440x280` DIP), displays `"WhatIsClicky"`, launches browser OAuth on `"Sign in"`, returns via `heyclicky://auth` deep link, and advances through `"WhereHeardAbout"` -> `"PickLanguage"` -> `"OpenClicky"` -> `"Hatched"`.
2. **10-Step Interactive `TutorialWindow` (`TutorialWindow.cs`)**:
   - [ ] Speak into the selected microphone during `Step.MicCheck` and confirm `MicMeter` (`MicMeter.cs`) animates and unlocks `"Continue"` after `loudSeconds` threshold.
   - [ ] Hold `Ctrl + Alt` during `Step.VoiceHello`, `Step.DrawToAsk`, and `Step.Dictation` and verify live STT transcript and TTS response playback.
3. **Live Stripe Checkout & Customer Portal (`BillingUi.cs`, `PaywallWindow.cs`)**:
   - [ ] Click `"Upgrade"` in Settings -> Account to open `PaywallWindow` (`PaywallCard.cs`).
   - [ ] Toggle Monthly/Yearly interval and click `"Upgrade to Pro"`, verifying browser redirect to Stripe Checkout and return handling via `heyclicky://checkout-complete`.
4. **Live Chrome Extension Auto-Install Drive (`ChromeSetupFlow.cs`, `ChromeAutoInstall.cs`, `TakeOverOverlay.cs`)**:
   - [ ] In Settings -> Browser, click `"Connect"` / `"Do it for me"` and verify `TakeOverOverlay` displays the conic edge glow and `"esc to stop"` pill while driving Chrome, and stops immediately when pressing `Esc` or moving the mouse.
