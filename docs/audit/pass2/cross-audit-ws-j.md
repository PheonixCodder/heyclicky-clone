# Pass 2 Independent Cross-Audit — WS-J (Autonomous Agents & Sidecars)

- **Cross-Auditor**: `ws-fg-drawing-2d-3d`
- **Primary Pass 1 Author**: `ws-jk-agents-ui` (`#26`)
- **Date**: 2026-11-13
- **Scope Audited**:
  - `docs/audit/ws-jk/REPORT.md` (§1, §2, §4 `AUD-J-001`..`AUD-J-008`, §5)
  - `docs/audit/ws-jk/findings.json` (`AUD-J-001`..`AUD-J-008`)
  - `docs/audit/ws-jk/coverage.json`
  - `docs/audit/evidence/ws-jk/agents-harness-drivers.txt` & `docs/audit/evidence/ws-jk/agents-harness-test-results.json`
  - `tests/audit/ws-jk/test-agents-harness.ps1`
  - All 119 C# source files across 8 namespaces in `src/desktop/Kursor.Agents/` (`HeyClicky.Agents`, `HeyClicky.Agents.Backend`, `HeyClicky.Agents.Clickys`, `HeyClicky.Agents.Harness`, `HeyClicky.Agents.Jobs`, `HeyClicky.Agents.Routines`, `HeyClicky.Agents.Runtime`, `HeyClicky.Agents.Turns`)
  - All 1,173 lines of `src/backend/src/routes/agents.ts` and `src/backend/src/routes/worker.ts:460-561`

---

## 1. Executive Summary of Cross-Audit

| Metric | Count | Details |
|---|---|---|
| **Pass 1 WS-J Findings Audited** | `8` | `AUD-J-001` through `AUD-J-008` (`1 Critical`, `2 High`, `4 Medium`, `1 Low`) |
| **Pass 1 Findings `CONFIRMED` (Exact)** | `5` | `AUD-J-002`, `AUD-J-004`, `AUD-J-005`, `AUD-J-006`, `AUD-J-007` |
| **Pass 1 Findings `CONFIRMED_WITH_REFINEMENT`** | `3` | `AUD-J-001` (line numbers in `AgentHarnessRunnerRuntime.cs`), `AUD-J-003` (`CodexSession.cs` vs `CodexHomeManager.cs`), `AUD-J-008` (Pass 1's own table cited 3 non-existent namespaces in `Kursor.Agents`) |
| **Pass 1 Findings `DISPUTED`** | `0` | None |
| **New WS-J Findings Discovered in Pass 2** | `4` | `AUD-J-009` (`High`), `AUD-J-010` (`High`), `AUD-J-011` (`Medium`), `AUD-J-012` (`Medium`) |

---

## 2. Independent Re-Verification of Pass 1 Findings (`AUD-J-001` .. `AUD-J-008`)

### `AUD-J-001` (Critical) — `CONFIRMED_WITH_REFINEMENT`
- **Pass 1 Title**: `clicky-harness.exe` (`h1-mac`) and `Kursor.Agents` depend on 7 backend worker routes (`/agent/openai/v1/responses`, `/agent/anthropic/v1/messages`, `/agent/web-search`, `/agent/harness/lab/h1/*`, `/mcp/composio`) that are completely unimplemented (`404 Not Found`) in `src/backend/`.
- **Independent Verification**:
  - Verified in [`docs/audit/evidence/ws-jk/agents-harness-drivers.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-drivers.txt):
    - `lines 32-38` (`R3` / `F3`): constructs OpenAI client with `baseURL: "${e.baseUrl.replace(/\/+$/, "")}/agent/openai/v1"` (`maxRetries: 4`), calling `POST /agent/openai/v1/responses`.
    - `lines 40-52` (`M3` / `j3`): constructs Anthropic client with `baseURL: "${e.baseUrl.replace(/\/+$/, "")}/agent/anthropic"` (`maxRetries: 4`), calling `POST /agent/anthropic/v1/messages`.
    - `lines 72-86` (`V_` / `H_`): calls `GET ${e.baseUrl}/agent/web-search?q=...` and `POST ${e.baseUrl}/agent/web-search`.
    - `lines 147-182` (`E5` `RemoteLabHarnessDriver`): calls `POST ${r}/agent/harness/lab/h1/jobs`, `GET/POST /agent/harness/lab/h1/jobs/${s}/events|steer|interrupt`, and `GET ${n}/agent/harness/lab/h1/probe`.
  - Verified in [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L943): `POST /agent/composio/session` returns `"mcp_url": "http://127.0.0.1:8787/mcp/composio"`, which has no route handler anywhere in `src/backend/src/`.
  - Verified dynamic execution results in [`docs/audit/evidence/ws-jk/agents-harness-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-test-results.json) (`Scenarios 14-27`).
- **Refinement**:
  - Pass 1 cited `AgentHarnessRunnerRuntime.cs:356-411` (`BuildStartSpec`). In the actual source file [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerRuntime.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerRuntime.cs#L31-L138) (`372` lines total), the `turn/start` payload with `["worker"] = new JsonObject { ["baseUrl"] = turn.Context.WorkerBaseUrl.TrimEnd('/'), ["sessionToken"] = turn.Context.SessionToken }` is constructed inline inside `RunTurnAsync` at **lines `91-95`**.

### `AUD-J-002` (High) — `CONFIRMED`
- **Pass 1 Title**: `AgentHarnessRuntimes.cs` exposes 13 `(AgentHarness, AgentHarnessPlace)` combinations (`H1`..`H5` × `Mac`/`Lab`/`Cloud` plus `Codex` × `Lab`), but `clicky-harness.exe` only registers `"h1-mac"`, throwing `"Unknown harness driver"` for 12 of 13 pairs; `(Codex, Mac)` returns `null`.
- **Independent Verification**:
  - Verified [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRuntimes.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRuntimes.cs#L14-L28): `Runtime(AgentHarness harness, AgentHarnessPlace place)` returns `null` when `(harness == AgentHarness.Codex && place == AgentHarnessPlace.Mac)` (`0, 0`), which causes [`CodexSession.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs#L852) to throw `new AgentHarnessException("Codex on this PC isn't available in this build.", notAvailable: true)`.
  - All other 13 pairs return `new AgentHarnessRunnerRuntime(harness, place)`, which sends `harness: "<h>"`, `place: "<p>"` to `clicky-harness.exe`.
  - Verified [`docs/audit/evidence/ws-jk/agents-harness-drivers.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-drivers.txt#L186): `var b5 = new Map([["h1-mac", new D_]])` — only `"h1-mac"` is registered in the driver map, and `h5` (`line 139`) hardcodes `A5("/usr/bin/tar", ["-xzf", i, "-C", o])`.

### `AUD-J-003` (High) — `CONFIRMED_WITH_REFINEMENT`
- **Pass 1 Title**: `clicky-harness.exe` (`h1-mac`) and `CodexSession.cs` execute arbitrary shell commands (`powershell.exe -ExecutionPolicy Bypass` / `codex.exe -s danger-full-access`) and unrestricted filesystem `write`/`edit`/`read` outside the workspace with zero user approval gate.
- **Independent Verification**:
  - **Confirmed (`clicky-harness.exe`)**: [`docs/audit/evidence/ws-jk/agents-harness-drivers.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-drivers.txt#L106-L133) confirms `h1-mac` runs `powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command ...` (`bashTimeoutS: 300`) and resolves `write`/`edit`/`read` paths via `path.resolve(workspace, p)` without checking that the resolved path stays inside `workspace` (proven dynamically in `Scenario 26`).
- **Crucial Source Refinement (`CodexSession.cs` vs `CodexHomeManager.cs`)**:
  - Pass 1 cited `src/desktop/Kursor.Agents/HeyClicky.Agents.Codex/CodexSession.cs:88-115` (`CodexSession.BuildExecArgs()` passing `codex.exe exec --json -c approval_policy="never" -s danger-full-access`).
  - **In reality**, `CodexSession.cs` is at [`src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexSession.cs#L1-L1232) (`1,232` lines total), has **no `BuildExecArgs()` method**, and **never spawns `codex.exe` at all** (`lines 88-115` are `RememberFinishedTurn` and `HandleNotification`).
  - Instead, `approval_policy = "never"` and `sandbox_mode = "danger-full-access"` are written to `%LOCALAPPDATA%\HeyClicky\codex-home\config.toml` by **`CodexHomeManager.WriteConfigToml()`** at [`src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L96-L97)!

### `AUD-J-004` (Medium) — `CONFIRMED`
- **Pass 1 Title**: `clicky-harness.exe` (`88.5 MB` Bun binary) and `ClickyBrowserHost.exe` (`4.68 MB` Go binary) are closed-source prebuilt executables signed by `Humansongs, Inc` with no source code in the repository.
- **Independent Verification**: Confirmed against [`AgentHarnessRunnerProcess.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L59) and [`docs/audit/evidence/ws-jk/agents-harness-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-test-results.json).

### `AUD-J-005` (Medium) — `CONFIRMED`
- **Pass 1 Title**: `AgentHarnessRunnerProcess.cs` contains a hardcoded upstream developer path (`C:\Users\niles\code\clicky-closed\windows\src\HeyClicky.Agents\Harness\AgentHarnessRunnerRuntime.cs`) and unreachable `NodeExecutablePath()` / `RunnerScriptPath` dead code.
- **Independent Verification**: Confirmed at [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L61-L77) (`RunnerScriptPath`) and [`lines 173-263`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessRunnerProcess.cs#L173-L263) (`NodeExecutablePath` and `ResolveLaunch`).

### `AUD-J-006` (Medium) — `CONFIRMED`
- **Pass 1 Title**: macOS-specific prompt & tool naming leakage (`mac_step` in `TurnPromptBuilder.cs`, `AgentHarnessPlace.Mac` (`"mac"`), and `"The user's own skills, kept on this Mac"` in `clicky-harness.exe`).
- **Independent Verification**: Confirmed at [`src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/TurnPromptBuilder.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Turns/TurnPromptBuilder.cs#L38-L89) (`ClickyDrivePromptSection` lines `38` and `89`), [`src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessNames.cs#L20-L35), and [`docs/audit/evidence/ws-jk/agents-harness-drivers.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-drivers.txt#L65-L66).

### `AUD-J-007` (Medium) — `CONFIRMED`
- **Pass 1 Title**: `RoutineKeepAwake.cs` holds a continuous Windows `PowerRequestSystemRequired` (`PowerSetRequest`) lock whenever any unpaused routine exists on AC power, preventing the PC from ever sleeping even when the next routine is hours/days away.
- **Independent Verification**: Confirmed at [`src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineKeepAwake.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineKeepAwake.cs#L46-L68) and [`src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineScheduler.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Routines/RoutineScheduler.cs#L112-L309) (`lines 112` and `309`).

### `AUD-J-008` (Low) — `CONFIRMED_WITH_REFINEMENT`
- **Pass 1 Title**: `docs/06-agents-proactive-and-memory.md` has architectural documentation drift regarding `Kursor.Agents` namespaces, primary execution engine (`CodexSession` vs `AgentHarnessRunnerRuntime`), and `AgentHarnessModel`.
- **Independent Verification & Correction of Pass 1's Namespace Table**:
  - Confirmed that [`docs/06-agents-proactive-and-memory.md`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md) has architectural drift vs `src/desktop/Kursor.Agents/`.
  - **Correction to Pass 1 (`AUD-J-008`, `REPORT.md` §1 lines 27–37, and `coverage.json` items 1–7)**:
    Pass 1 claimed that `src/desktop/Kursor.Agents/` contains 10 namespaces including `HeyClicky.Agents.Codex`, `HeyClicky.Agents.Memory` (`MemoryBulletParser.cs`), `HeyClicky.Agents.Proactive` (`ProactiveAgentsClient.cs`, `ProactiveContextCollector.cs`, `ProactiveSuggestionStore.cs`), and `HeyClicky.Agents.Sidecar` (`SidecarRecorder.cs`, `SidecarTurnSync.cs`), and named the model enum file `AgentModelOption.cs`.
    **Direct enumeration of all 119 `.cs` files in `src/desktop/Kursor.Agents/` proves that `HeyClicky.Agents.Codex`, `HeyClicky.Agents.Memory`, `HeyClicky.Agents.Proactive`, `HeyClicky.Agents.Sidecar`, and `AgentModelOption.cs` do NOT exist in `src/desktop/Kursor.Agents/`!**
    The actual 8 namespaces (and file counts) in `src/desktop/Kursor.Agents/` are:
    1. `HeyClicky.Agents` (`18` files — `AgentService.cs`, `AgentRuntimeOptions.cs`, `AgentTask.cs`, etc.)
    2. `HeyClicky.Agents.Backend` (`13` files — `AgentWorkerApi.cs`, `AgentIntegrationsApi.cs`, `AgentIntegration.cs`, `AgentIntegrationAccount.cs`, `AgentIntegrationCatalog.cs`, `AgentIntegrationToolkit.cs`, `AgentBackendException.cs`, `AgentPaywallException.cs`, `AgentSessionTokenProvider.cs`, `AgentTurnLease.cs`, `ComposioSessionReuse.cs`, `UserMemoryCache.cs`, `UserMemoryFetchStatus.cs`)
    3. `HeyClicky.Agents.Clickys` (`2` files — `ClickyInfo.cs`, `ClickyStore.cs`)
    4. `HeyClicky.Agents.Harness` (`17` files — including [`AgentHarnessModel.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Harness/AgentHarnessModel.cs#L1-L14))
    5. `HeyClicky.Agents.Jobs` (`40` files)
    6. `HeyClicky.Agents.Routines` (`6` files)
    7. `HeyClicky.Agents.Runtime` (`11` files — `CodexHomeManager.cs`, `CodexRolloutHistory.cs`, `CodexSession.cs`, `ComposioMcpConfiguration.cs`, `ComposioTurnErrors.cs`, `CustomConnectorRegistration.cs`, `CustomConnectorTransport.cs`, `IntegrationUse.cs`, `KillOnCloseJob.cs`, `TurnOutcome.cs`, `WindowsInstructionAdapter.cs`)
    8. `HeyClicky.Agents.Turns` (`8` files — `TurnPromptBuilder.cs`, `StructuredFinalReply.cs`, `StandingContextLedger.cs`, etc.)
    *(Proactive classes such as `ProactiveAgentService.cs` and `ProactiveDeciderClient.cs` live in `src/desktop/Kursor.Core/HeyClicky.Core.ProactiveAgents/`.)*

---

## 3. New WS-J Findings Discovered in Pass 2 (`AUD-J-009` .. `AUD-J-012`)

### ID: `AUD-J-009`
- **Workstream**: WS-J (Autonomous Agents & Sidecars)
- **Severity**: High
- **Status**: FAIL
- **Category**: API Contract / DTO Schema Mismatch (`Kursor.Agents` <-> `agents.ts`)
- **Title**: Complete C#-to-Backend DTO schema mismatch across `GET /agent/integrations`, `POST /agent/integrations/:toolkit/connect`, and `POST /agent/composio/session` breaks all agent integrations and Composio MCP session setup
- **Location**:
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentIntegration.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentIntegration.cs#L64-L83) (`AgentIntegration.Parse`)
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L98-L181) (`ListIntegrationsAsync`, `ConnectIntegrationAsync`, `ComposioSessionAsync`)
  - [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L843-L947)
- **Symptoms**:
  1. `AgentService.ListIntegrationsAsync()` always returns an empty catalog (`0` integrations) even though `GET /agent/integrations` returns 8 integrations in JSON.
  2. `AgentService.ConnectIntegrationAsync(toolkit)` always throws `AgentBackendException` (`"HeyClicky received an invalid integration connect URL."`).
  3. `AgentWorkerApi.ComposioSessionAsync()` always returns `(ComposioFetchKind.NoConnectedApps, Config: null, Error: null)` on every turn, so `composio` is permanently `null` even before reaching `/mcp/composio`.
- **Root Cause**:
  1. **Catalog item & account key mismatch**: In [`agents.ts:843-876`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L843-L876), each catalog entry has `{ toolkit, name, description, category, connected, accounts: [{ id, label, status, connected_at }] }`. In [`AgentIntegration.cs:66-78`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentIntegration.cs#L66-L78), `AgentIntegration.Parse(JsonObject o)` checks `string text = o["id"]?.ToString(); if (string.IsNullOrEmpty(text)) return null;`, and requires `o["display_name"]`, `o["status"]` (`"connected"` | `"partial"` | `"not_connected"`), `o["toolkits"]` (`[{ slug, name, logo, is_connected }]`), and `a["connected_account_id"]` on each account. Because `o["id"]` is `null` on all 8 items, `AgentIntegration.Parse` returns `null` for every item and `AgentIntegrationsApi.ParseCatalog` drops all 8 entries.
  2. **Connect redirect URL key mismatch**: In [`AgentWorkerApi.cs:116-120`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L116-L120), `ConnectIntegrationAsync` requires a non-empty `redirect_url` string (`ParseObject(text)?["redirect_url"]?.ToString()`). In [`agents.ts:893-899`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L893-L899), `POST /agent/integrations/:toolkit/connect` returns `"connect_url": null` and no `redirect_url` field.
  3. **Composio session key mismatch**: In [`AgentWorkerApi.cs:160-165`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L160-L165), `ComposioSessionAsync` checks `jsonObject["enabled"] == true` and `jsonObject["proxy_url"]`. In [`agents.ts:936-946`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L936-L946), `POST /agent/composio/session` returns `"mcp_url"` (not `"proxy_url"`) and omits `"enabled": true`.
- **Evidence**:
  - Direct code inspection of [`AgentIntegration.cs:64-83`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentIntegration.cs#L64-L83), [`AgentWorkerApi.cs:98-181`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L98-L181), and [`agents.ts:843-947`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L843-L947).
- **Reproduction**:
  1. Call `GET http://127.0.0.1:8787/agent/integrations?limit=1000` -> returns 8 items with `"toolkit"` and `"name"`, no `"id"`.
  2. Pass response to `AgentIntegrationsApi.ParseCatalog` -> returns `Integrations.Count == 0`.
  3. Call `POST http://127.0.0.1:8787/agent/composio/session` -> returns `mcp_url` without `enabled` or `proxy_url`; `ComposioSessionAsync` returns `ComposioFetchKind.NoConnectedApps`.
- **User Impact**: The desktop app's Connected Apps / Integrations UI cannot display or connect any integration, and `Kursor.Agents` never attaches the Composio MCP server to any agent turn.
- **Recommended Fix**: Update `src/backend/src/routes/agents.ts` (`GET /agent/integrations`, `POST /agent/integrations/:toolkit/connect`, `POST /agent/composio/session`) to emit the exact fields required by `AgentIntegration.Parse` (`id`, `display_name`, `status`, `connected_count`, `total_count`, `toolkits: [{ slug, name, logo, is_connected }]`, `accounts: [{ connected_account_id, account_name, label, status, connected_at }]`) and `AgentWorkerApi` (`redirect_url`, `enabled: true`, `proxy_url`).
- **Verification Criteria**: `AgentIntegrationsApi.ParseCatalog` parses all 8 integrations (`Count == 8`), `ConnectIntegrationAsync` returns a valid redirect URL without throwing, and `ComposioSessionAsync` returns `ComposioFetchKind.Connected` when at least one app is connected.

---

### ID: `AUD-J-010`
- **Workstream**: WS-J (Autonomous Agents & Sidecars)
- **Severity**: High
- **Status**: FAIL
- **Category**: API Contract / Caching & Model Selection Bug
- **Title**: `POST /agent/session-token` returns `expires_at` as an ISO string instead of numeric Unix seconds and omits `capabilities.developer_model_selection`, permanently defeating `AgentSessionTokenProvider` caching (`NeedsRefresh == true`) and locking all turns to `AgentHarnessModel.Standard`
- **Location**:
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L78-L96) (`MintSessionTokenAsync`)
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentSessionTokenProvider.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentSessionTokenProvider.cs#L29-L41) (`NeedsRefresh`, `AllowsDeveloperModelSelection`)
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents/AgentRuntimeOptions.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentRuntimeOptions.cs#L24) (`SessionTokenRefreshMargin`)
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents/AgentService.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentService.cs#L2692-L3673) (`RunTurnAsync`, `HarnessTurnContextAsync`)
  - [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L911-L922) (`POST /agent/session-token`)
- **Symptoms**:
  1. `AgentSessionTokenProvider.NeedsRefresh` is **always `true`** immediately after minting a token, causing `tokens.GetAsync(ct)` to make a fresh `POST /agent/session-token` HTTP request on every call (at least twice per agent turn).
  2. `tokens.AllowsDeveloperModelSelection` is permanently `false`, so `AgentService.cs:3672` forces `Model = AgentHarnessModel.Standard` and ignores user/developer model selection (`Astra`, `Sol`, `Terra`, `Luna`, `Fable`, `Opus`, `Sonnet`, `Haiku`).
  3. `POST /agent/session-token` returns two different random UUIDs for `session_token` and `token` in the same JSON object.
- **Root Cause**:
  1. In [`AgentWorkerApi.cs:91-92`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L91-L92):
     `DateTimeOffset item = ((jsonObject["expires_at"] is JsonValue jsonValue && jsonValue.TryGetValue<double>(out value)) ? DateTimeOffset.FromUnixTimeMilliseconds((long)(value * 1000.0)) : DateTimeOffset.UtcNow.AddMinutes(15.0));`
     Because [`agents.ts:916`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L916) returns `expires_at: new Date(Date.now() + 3600 * 1000).toISOString()` (a string, not a number), `TryGetValue<double>` returns `false` and `ExpiresAt` falls back to `UtcNow + 15 minutes`.
  2. [`AgentRuntimeOptions.cs:24`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentRuntimeOptions.cs#L24) sets `SessionTokenRefreshMargin = TimeSpan.FromMinutes(30L)`. In [`AgentSessionTokenProvider.cs:37`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentSessionTokenProvider.cs#L37), `expiresAt - DateTimeOffset.UtcNow <= refreshMargin` (`15 min <= 30 min`) is immediately `true`!
  3. [`AgentWorkerApi.cs:94`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Backend/AgentWorkerApi.cs#L94) checks `jsonObject["capabilities"]?["developer_model_selection"]`, which [`agents.ts:911-922`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L911-L922) never includes.
- **Evidence**:
  - Direct trace across `agents.ts:911-922`, `AgentWorkerApi.cs:78-96`, `AgentSessionTokenProvider.cs:29-41`, `AgentRuntimeOptions.cs:24`, and `AgentService.cs:2692, 3672`.
- **Reproduction**:
  1. Call `POST /agent/session-token` -> `expires_at` is an ISO-8601 string and `capabilities` is absent.
  2. In `AgentSessionTokenProvider.GetAsync()`, `ExpiresAt` becomes `UtcNow + 15m`, which is `<= SessionTokenRefreshMargin` (`30m`), so `NeedsRefresh` remains `true` on the very next call.
- **User Impact**: Every `tokens.GetAsync()` call incurs an uncached HTTP round-trip to `/agent/session-token`, and choosing any non-default model (`Astra`, `Sol`, `Terra`, `Luna`, `Fable`, `Opus`, `Sonnet`, `Haiku`) in settings is silently overridden to `Standard`.
- **Recommended Fix**: In `src/backend/src/routes/agents.ts:911-922`, use a single token variable for both `token` and `session_token`, return `expires_at: Math.floor(Date.now() / 1000) + 3600` (numeric Unix seconds, `> 1800s` so it exceeds the 30-minute refresh margin), and include `capabilities: { developer_model_selection: true }`.
- **Verification Criteria**: `MintSessionTokenAsync` parses `ExpiresAt` ~60 minutes in the future (`NeedsRefresh == false` for 30 minutes) and sets `AllowsDeveloperModelSelection == true`.

---

### ID: `AUD-J-011`
- **Workstream**: WS-J (Autonomous Agents & Sidecars)
- **Severity**: Medium
- **Status**: FAIL
- **Category**: Hardcoded Production URL & Plaintext Credential Storage
- **Title**: `CodexHomeManager.cs` hardcodes `https://worker.heyclicky.com/agent/openai/v1` and writes plaintext session tokens, bearer tokens, and `sandbox_mode = "danger-full-access"` to `%LOCALAPPDATA%\HeyClicky\codex-home\` even though local `Codex` is retired
- **Location**:
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L15-L153) (`OpenAiBaseUrl`, `PrepareRuntimeConfiguration`, `WriteAuthJson`, `WriteConfigToml`)
  - [`src/desktop/Kursor.Agents/HeyClicky.Agents/AgentService.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents/AgentService.cs#L2315)
- **Symptoms**:
  - On every runtime preparation (`AgentService.cs:2315`), `CodexHomeManager.WriteConfigToml()` writes `%LOCALAPPDATA%\HeyClicky\codex-home\config.toml` pointing to `base_url = "https://worker.heyclicky.com/agent/openai/v1"` (`line 15, 100`) with `approval_policy = "never"`, `sandbox_mode = "danger-full-access"`, and plaintext MCP bearer tokens / custom connector environment variables (`lines 96-148`).
- **Root Cause**:
  - `CodexHomeManager.cs:15` defines `private const string OpenAiBaseUrl = "https://worker.heyclicky.com/agent/openai/v1";` instead of using the configured `WorkerBaseUrl`, and writes `config.toml` / `auth.json` for `codex.exe` even though `AgentHarnessRuntimes.cs:16-19` retired `(AgentHarness.Codex, AgentHarnessPlace.Mac)` (`returns null`).
- **Evidence**:
  - [`CodexHomeManager.cs:15, 57-61, 76-153`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Runtime/CodexHomeManager.cs#L15-L153).
- **Reproduction**:
  - Inspect `CodexHomeManager.WriteConfigToml()` (`lines 90-153`): `base_url` is hardcoded to `https://worker.heyclicky.com/agent/openai/v1` and custom connector secrets are serialized in plaintext into `config.toml`.
- **User Impact**: Plaintext bearer tokens and connector environment secrets are written to `%LOCALAPPDATA%\HeyClicky\codex-home\config.toml` on disk, and any tool reading `config.toml` targets the dead upstream `worker.heyclicky.com` domain.
- **Recommended Fix**: Pass `WorkerBaseUrl` into `CodexHomeManager` (replacing hardcoded `https://worker.heyclicky.com/agent/openai/v1`) and stop writing plaintext tokens into `config.toml` / `auth.json` when `Codex` runtime is disabled.
- **Verification Criteria**: No hardcoded `worker.heyclicky.com` URL or unused plaintext bearer tokens are written to `%LOCALAPPDATA%\HeyClicky\codex-home\config.toml`.

---

### ID: `AUD-J-012`
- **Workstream**: WS-J (Autonomous Agents & Sidecars)
- **Severity**: Medium
- **Status**: FAIL
- **Category**: Persistence / State Reset Bug
- **Title**: `POST /proactive/jobs` resets `nextSuggestionNumericId = 101` in memory on every backend restart, colliding with existing SQLite `proactive_cards` rows and un-dismissing previously dismissed proactive suggestions
- **Location**:
  - [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L85-L177) (`nextSuggestionNumericId`, `POST /proactive/jobs`)
- **Symptoms**:
  - After restarting the backend server, the next `POST /proactive/jobs` call generates `numericId = 101` (`id = "101"`), which collides with row `"101"` in SQLite `proactive_cards` and executes `ON CONFLICT(id) DO UPDATE SET ... dismissed = 0`, overwriting the historical card and making dismissed cards reappear.
- **Root Cause**:
  - [`src/backend/src/routes/agents.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L85) initializes `let nextSuggestionNumericId = 101;` in memory without seeding from `SELECT MAX(CAST(id AS INTEGER)) FROM proactive_cards`, and [`lines 152-166`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L152-L166) upserts `proactive_cards` with `id = String(numericId)` and `dismissed = 0`.
- **Evidence**:
  - [`src/backend/src/routes/agents.ts:85, 134, 152-166`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L85-L166) (also cross-referenced with `AUD-C-017` in `cross-audit-ws-c.md`).
- **Reproduction**:
  1. Create a proactive suggestion via `POST /proactive/jobs` (`id = "101"`) and dismiss it via `POST /proactive/suggestions/101/resolve`.
  2. Restart the backend and call `POST /proactive/jobs` again -> `nextSuggestionNumericId` starts at `101` again, overwriting row `"101"` in `proactive_cards` with `dismissed = 0`.
- **User Impact**: Previously dismissed or resolved proactive agent suggestions reappear after backend restarts and historical proactive cards are overwritten.
- **Recommended Fix**: Seed `nextSuggestionNumericId` at startup from `SELECT COALESCE(MAX(CAST(id AS INTEGER)), 100) + 1 FROM proactive_cards` (or use `Date.now()`).
- **Verification Criteria**: Restarting the backend and posting a new `/proactive/jobs` request assigns a strictly increasing `suggestion.id` without overwriting existing rows in `proactive_cards`.
