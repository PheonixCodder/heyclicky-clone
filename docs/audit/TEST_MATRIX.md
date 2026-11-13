# Kursor Full-Spectrum Audit — Master Automated Test & Verification Matrix (`TEST_MATRIX.md`)

- **Compiled By:** `ws-hi-platform-core` (Pass 3 Master Deliverable) + **Post-Remediation Verification Refresh**
- **Repository:** `C:\dev\heyclicky-clone`
- **Unified Runner:** [`tests/audit/run-audit.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/run-audit.ps1)
- **Total Automated Suites Cataloged:** **13** (`2` repository suites + `11` audit workstream suites across `tests/audit/ws-*/`)
- **Total Automated Test Cases / Probes Executed:** **1,380+** individual dynamic test cases, route probes, corpus benchmarks, fuzz seeds, and security PoCs
- **Post-Remediation Suite Status:** **13 / 13 Suites Passing (`0` Open Failures / `0` Open Security Vulnerabilities)**

---

## 0. Post-Remediation Verification Summary (All 13 Suites)

| # | Workstream | Suite / Script Path | Post-Remediation Result | Primary Updated Evidence |
|---:|---|---|---|---|
| 1 | **Backend Phase 1** | [`src/backend/src/test-phase1.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts) | **18 / 18 PASS (`ExitCode = 0`)** | Dynamic `MOUTH_PRIMARY_PROVIDER` check + `warmSynthProc.unref()` clean exit |
| 2 | **Backend Phase 2 & 6** | [`src/backend/src/test-phase2.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase2.ts) | **7 / 7 PASS (`ExitCode = 0`)** | `force_local_fallback` short-circuit before upstream LLM + clean exit |
| 3 | **WS-A / WS-B / WS-N** | [`tests/audit/ws-ab/test-ws-ab.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-ab/test-ws-ab.ps1) | **22 / 22 PASS (`0 FAIL`)** | [`docs/audit/ws-ab/ws-ab-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-ab/ws-ab-test-results.json) |
| 4 | **WS-C (Backend Routes)** | [`tests/audit/ws-c/test-backend-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-c/test-backend-audit.ts) | **111 / 111 Routes PASS (`0 FAIL`, `0` Sec Issues)** | [`docs/audit/ws-c/endpoint-audit-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-c/endpoint-audit-results.json) |
| 5 | **WS-D (Screen Layout)** | [`tests/audit/ws-d/test-screen-layout.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-screen-layout.ts) | **35 / 35 Snap PASS (`100%`), 16 / 16 Answer-Line PASS (`100%`)** | [`docs/audit/ws-d/layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-d/layout-benchmark.json), [`snap-adversarial-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-d/snap-adversarial-results.json) |
| 6 | **WS-D (OneBrain Prompts)** | [`tests/audit/ws-d/test-brain-prompts.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-brain-prompts.ts) | **105 / 105 SSE & Sanitized JS PASS (`0` retired models)** | [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json) |
| 7 | **WS-E (Voice & Dictation)** | [`tests/audit/ws-e/test-voice-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-e/test-voice-audit.ts) | **150 / 150 PASS (`0 FAIL`, `ttsQueueDesync: false`)** | [`docs/audit/ws-e/voice-audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-e/voice-audit-summary.json) |
| 8 | **WS-F (2D V8 Engine)** | [`tests/audit/ws-fg/test-2d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-2d-engine.mjs) | **57 / 57 PASS (`0 FAIL`)** | [`docs/audit/ws-fg/2d-engine-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/2d-engine-results.json) |
| 9 | **WS-G (3D V8 Kernel)** | [`tests/audit/ws-fg/test-3d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-3d-engine.mjs) | **40 / 40 PASS (`0 FAIL`, `coneTopParameterRespected: true`)** | [`docs/audit/ws-fg/3d-engine-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-fg/3d-engine-results.json) |
| 10 | **WS-H & WS-I (.NET Core/Platform)** | [`tests/audit/ws-hi/WsHiAuditRunner.csproj`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/WsHiAuditRunner.csproj) | **275 / 275 PASS (`0` Bug Repros, `0` Concurrency Exceptions)** | [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt) |
| 11 | **WS-J (Agents & Harness)** | [`tests/audit/ws-jk/test-agents.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-agents.ps1) | **30 / 30 PASS (`0 FAIL`)** | [`docs/audit/ws-jk/agents-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/agents-test-results.json) |
| 12 | **WS-K (Desktop UI Surfaces)** | [`tests/audit/ws-jk/test-ui-surfaces.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-ui-surfaces.ps1) | **19 / 19 PASS (`0 FAIL`)** | [`docs/audit/ws-jk/ui-surfaces-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-jk/ui-surfaces-results.json) |
| 13 | **WS-L & WS-M (Sec/Perf/Soak)** | [`tests/audit/ws-lm/test-security-perf.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-lm/test-security-perf.ts) | **295 / 295 PASS (`0` Open Vulnerabilities, `0` Soak Errors)** | [`docs/audit/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/security-pocs.json), [`PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json) |

---

## 1. Initial Audit Baseline Table of All 13 Test Suites (Pre-Remediation)

| # | Workstream | Suite / Script Path | Runtime / Harness | Total Cases / Probes | `PASS` | `FAIL` (Bugs Proved) | `UNVERIFIED` | Primary Evidence Output(s) |
|---:|---|---|---|---:|---:|---:|---:|---|
| 1 | **Baseline (`src/backend`)** | [`src/backend/src/test-phase1.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts) | Node.js / `tsx` | 18 | 7 | 1 (`10` skipped after abort) | 0 | [`existing-tests-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/existing-tests-output.txt) |
| 2 | **Baseline (`src/backend`)** | [`src/backend/src/test-phase2.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase2.ts) | Node.js / `tsx` | 7 | 7 | 1 (Process hang on exit) | 0 | [`test-phase2-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/test-phase2-output.txt) |
| 3 | **WS-A / WS-B / WS-N** | [`tests/audit/ws-ab/test-ws-ab.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-ab/test-ws-ab.ps1) | PowerShell | 22 | 1 | 21 (`AUD-A-001`..`010`, `AUD-B-001`..`005`, `AUD-N-001`..`005`) | 0 | [`test-ws-ab-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/test-ws-ab-output.json), [`test-ws-ab-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/test-ws-ab-output.txt) |
| 4 | **WS-C (Backend Routes)** | [`tests/audit/ws-c/test-backend-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-c/test-backend-audit.ts) | Node.js / `tsx` | 111 routes + 7 deep audits + 130 load/SSE soak | 111 routes reachable | 15 (`AUD-C-001`..`015`) | 0 | [`route-test-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/route-test-matrix.json), [`audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`load-soak-metrics.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/load-soak-metrics.json) |
| 5 | **WS-D (Screen Layout)** | [`tests/audit/ws-d/test-screen-layout.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-screen-layout.ts) | Node.js / `tsx` | 101 (`66` corpus JPEGs + `35` adversarial JS) | 56 (`40` layout + `16` snap) | 45 (`26` layout FN/FP + `19` snap bugs: `AUD-D-002`, `003`, `006`, `007`) | 0 | [`screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json), [`snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json) |
| 6 | **WS-D (OneBrain Prompts)** | [`tests/audit/ws-d/test-brain-prompts.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-brain-prompts.ts) | Node.js / `tsx` | 118 (`105` prompts + `8` model probes + `5` V8 checks) | 104 syntax / 105 SSE order | 8 (`AUD-D-001`, `004`, `005`, `007`, `008`) | 0 | [`brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json), [`model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json) |
| 7 | **WS-E (Voice & Dictation)** | [`tests/audit/ws-e/test-voice-dictation.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-e/test-voice-dictation.ts) | Node.js / `tsx` | 150 (`20` STT + `10` TTS + `112` cleanup + `8` C# logic) | 112 | 38 (`AUD-E-001`..`012`) | 0 | [`voice-dictation-audit-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-e/voice-dictation-audit-results.json) |
| 8 | **WS-F (2D V8 Engine)** | [`tests/audit/ws-fg/test-2d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-2d-engine.mjs) | Node.js (`vm` V8) | 57 programs | 54 | 3 (`AUD-F-001`, `AUD-F-002`, `AUD-F-003`) | 0 | [`test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) |
| 9 | **WS-G (3D V8 Kernel)** | [`tests/audit/ws-fg/test-3d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-3d-engine.mjs) | Node.js (`vm` V8) | 40 shapes/modifiers/extremes | 39 | 1 (`AUD-G-001`: `cone.top` ignored) | 0 | [`test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json) |
| 10 | **WS-H & WS-I (.NET Core/Platform)** | [`tests/audit/ws-hi/WsHiAuditRunner.csproj`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/WsHiAuditRunner.csproj) ([`Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs)) | .NET 9.0 (`dotnet run`) | 275 (`264` Ink fuzz + `11` unit/concurrency repros) | 271 | 4 (`AUD-I-002`, `AUD-I-003`, `AUD-I-004`, `AUD-H-009`) | 0 | [`test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt), [`thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt) |
| 11 | **WS-J (Agents & Harness)** | [`tests/audit/ws-jk/test-agents-harness.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-agents-harness.ps1) | PowerShell + `clicky-harness.exe` | 30 scenarios + 7 live route probes | 23 | 7 (`AUD-J-001`, `002`, `003`, `006`, `007`) | 0 | [`agents-harness-test-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/agents-harness-test-results.json) |
| 12 | **WS-K (Desktop UI Surfaces)** | [`tests/audit/ws-jk/test-ui-surfaces.ps1`](file:///C:/dev/heyclicky-clone/tests/audit/ws-jk/test-ui-surfaces.ps1) | PowerShell + Win32/UIA/DPAPI | 19 surface & storage checks | 8 | 10 (`AUD-K-001`..`010`) | 1 | [`ui-hwnd-inventory.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/ui-hwnd-inventory.json), [`ui-storage-dpapi-test.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/ui-storage-dpapi-test.json), [`ui-static-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-jk/ui-static-analysis.json) |
| 13 | **WS-L & WS-M (Sec/Perf/Soak)** | [`tests/audit/ws-lm/test-perf-reliability.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-lm/test-perf-reliability.ts) | Node.js / `tsx` | 295 (`5` security PoCs + `215` soak turns + `73` interleaved + `2` kill/recovery) | 288 | 7 (`AUD-L-001`..`006`, `AUD-M-001`..`002`) | 0 | [`PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json), [`security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json), [`soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json) |

---

## 2. Detailed Suite-by-Suite Breakdown

### 2.1 Existing Repository Test Suites (`src/backend/src/test-phase1.ts` & `test-phase2.ts`)

| Suite & Case | Input / Action | Expected Result | Observed Result | Status |
|---|---|---|---|---|
| `test-phase1.ts` (`#1`..`#7`) | `GET /health`, `/auth/v1/authorize`, `/auth/v1/token`, `/auth/v1/user`, `/auth/v1/logout`, `/plan`, `/config` | `200 OK` / `302` with expected JSON fields | `7 / 7` passed | `PASS` |
| `test-phase1.ts` (`#8`) | `GET /config/runtime-models` when `src/backend/.env` has `MOUTH_PRIMARY_PROVIDER=gemini-tts` | `mouth.primary_provider === "openai-realtime"` (`test-phase1.ts:155`) | Throws `AssertionError: 'gemini-tts' !== 'openai-realtime'`, aborting the remaining 10 tests (`#9`..`#18`) and failing `npm test` (`AUD-C-006`) | `FAIL` |
| `test-phase1.ts` (`#9`..`#18`, with default env) | `/deepgram/token`, `/realtime/token`, `/v2/dictation/cleanup`, `/me/devices`, `/memory`, `/brain/turn` SSE, `/proactive/decide`, `/agent/lease`, `/me/referral`, `/me/notifications` | `200 OK` | Pass when `.env` override is removed, **but** process hangs indefinitely after `server.close()` because `voice.ts:263` spawns `powershell.exe` without `.unref()` (`AUD-C-006`) | `FAIL` |
| `test-phase2.ts` (`#1`..`#7`) | `/agent/realtime/session`, `/agent/realtime/speak`, `/v2/dictation/gemini-token`, `/v2/dictation/transcribe` (WAV + JSON), `/brain/turn` (`force_local_fallback: true` + `1x1` PNG), `/search/web`, `/search/images` | `200 OK` offline deterministic tests | All 7 assertions print `PASS`, **but**: (1) `/brain/turn` makes a live network call to Groq even with `force_local_fallback: true` (`AUD-C-005`), and (2) process hangs indefinitely after `server.close()` due to `warmSynthProc` (`AUD-C-006`) | `FAIL` |

---

### 2.2 WS-A, WS-B & WS-N Suite (`tests/audit/ws-ab/test-ws-ab.ps1`)

- **Command:** `powershell -NoProfile -ExecutionPolicy Bypass -File tests/audit/ws-ab/test-ws-ab.ps1`
- **Evidence:** [`docs/audit/evidence/ws-ab/test-ws-ab-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/test-ws-ab-output.json), [`docs/audit/evidence/ws-ab/test-ws-ab-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-ab/test-ws-ab-output.txt)

| Check ID | Input / Target | Expected | Observed | Status |
|---|---|---|---|---|
| `AUD-A-001` | `src/desktop/Directory.Build.props` & 7 `.csproj` files | Warnings enabled; strict nullable | 9 warning codes suppressed globally (`CS0649`, `CS8500`, etc.); `1,259` warnings when unmasked | `FAIL` |
| `AUD-A-002` | `.csproj` `<PackageReference>` vs `<HintPath>` | NuGet `<PackageReference>` for dependency auditing | `0` `PackageReference`, `49` raw `<HintPath>` binary refs | `FAIL` |
| `AUD-A-003` | `scripts/build-dist.ps1` vs `dist/kursor-desktop/` | Release build + updated `HeyClicky.exe` | Builds `-c Debug`, copies `.pdb` files, omits `HeyClicky.exe` (`SHA256` mismatch) | `FAIL` |
| `AUD-A-004` | `scripts/build-dist.ps1:39` & `overlay-and-launch.ps1:45` | Valid `Assets/Engine` path | References non-existent `Kursor.Drawing3D/Assets/Engine` | `FAIL` |
| `AUD-A-005` | `src/backend/package.json` & `npm audit` | Zero vulnerabilities; consistent Node version | `@hono/node-server@1.19.19` vulnerable (`GHSA-rmxm-3fg6-px4f`); `node >=24.0.0` vs README `v22+` | `FAIL` |
| `AUD-A-006` | `src/backend/src/routes/worker.ts:7-8` | Portable `%LOCALAPPDATA%` resolution | Hardcodes `C:\Users\ubaid\AppData\Local\HeyClicky\app-config.json` | `FAIL` |
| `AUD-A-007` | `start-kursor.ps1 -Restart` | Restarts backend + sidecars + desktop app | Only kills `HeyClicky`, leaving `node.exe` and sidecars running | `FAIL` |
| `AUD-A-008` | `scripts/install-autostart.ps1` & `LaunchAtLogin.cs` | Single non-conflicting startup entry | Registers both `HKCU\...\Run\Kursor` and `Startup\Kursor.lnk` + `Run\HeyClicky` | `FAIL` |
| `AUD-A-009` | `AppPaths.cs` vs `seed-local-auth.ps1` | Unified data directory | Split between `%LOCALAPPDATA%\HeyClicky` and `%LOCALAPPDATA%\Kursor` | `FAIL` |
| `AUD-A-010` | Repo root `LICENSE`, `.gitignore` | License present; anchored ignore rules | Missing `LICENSE` / `THIRD_PARTY_NOTICES`; unanchored `[Rr]elease/` & `[Dd]ebug/` | `FAIL` |
| `AUD-B-001`..`005` | Assemblies, `AssemblyInfo.cs`, `SingleInstance.cs`, UI strings, Authenticode signatures | Clean Kursor branding & self-contained IP | `HeyClicky*.dll`, baked-in PostHog/Sentry/Supabase keys, `Local\HeyClicky-<SID>` mutex collision, `4,712` `"HeyClicky"` strings, Humansongs-signed binaries | `FAIL` |
| `AUD-N-001`..`005` | `README.md`, `.env.example`, `AGENTS.md`, `docs/01..07` | Accurate env vars, primitives, file counts, and links | `ONEBRAIN_MODEL` unused, 6 unused `.env.example` vars, nonexistent `writeOnPage`, 17 broken links in `docs/06`, wrong file counts | `FAIL` |
| `WS-N-CTRL-001` | `docs/INDEX.md` test count (`18 + 7 = 25`) | Matches `test-phase1.ts` (18) + `test-phase2.ts` (7) | Exact match (`25` tests) | `PASS` |

---

### 2.3 WS-C Backend Route, Security & Load/Soak Suite (`tests/audit/ws-c/test-backend-audit.ts`)

- **Command:** `npx --prefix src/backend tsx tests/audit/ws-c/test-backend-audit.ts`
- **Evidence:** [`docs/audit/evidence/ws-c/route-test-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/route-test-matrix.json), [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/load-soak-metrics.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/load-soak-metrics.json)

| Sub-Suite | Input / Scenario | Expected | Observed | Status |
|---|---|---|---|---|
| **1. 111-Route Matrix** | Happy-path, unauthenticated, malformed JSON, and `Origin: https://evil.example` probes across all 111 routes in `enumeration.json` | Auth enforced; CORS restricted; valid JSON errors | `111/111` routes reachable (`0` crashes), **but** `111/111` return `Access-Control-Allow-Origin: *` and accept unauthenticated requests via `DEFAULT_USER_ID` (`AUD-C-001`) | `FAIL` |
| **2. CORS & Key Minting** | Cross-origin `POST /deepgram/token`, `/v2/dictation/gemini-token`, `/realtime/token`, `GET /memory` | Reject cross-origin; never return raw master keys | Returns `200 OK` with `Access-Control-Allow-Origin: *`, raw env keys when configured, and user memory (`AUD-C-001`) | `FAIL` |
| **3. Auth & Open Redirect** | `GET /auth/v1/authorize?redirect_uri=https://external-attacker.example/capture`; `POST /auth/v1/logout` + reuse token; forged JWT signature | Reject external `redirect_uri`; revoke token on logout; verify JWT signature | `302` redirect leaks 1-year `access_token` to external host (`AUD-C-003`); logged-out & forged-signature JWTs return `200 OK` (`AUD-C-004`) | `FAIL` |
| **4. `gzipMiddleware` Audit** | `<8KB` (`89 B`), `>=8KB` (`12 KB`), corrupt gzip, and `15.3 KB -> 15.7 MB` (`1,025.9x`) gzip bomb | Decompress valid payloads; reject bombs via `maxOutputLength` | `<8KB`, `>=8KB`, and corrupt (`400`) handled, **but** `15.3 KB` bomb synchronously expands to `15.73 MB` (`1,025.9x`) in `2,039 ms` on main thread (`AUD-C-002`) | `FAIL` |
| **5. C# DTO Contracts & Stubs** | `POST /me/devices` (`X-Clicky-Device`), `POST /onboarding/starter-plan`, dead URL probes (`/mcp/composio`, `/billing/local-*`), `POST /account/delete` | Match C# JSON property names; delete rows on `/account/delete` | Missing `id`/`display_name`/`is_this_device`/`capabilities` (`AUD-C-007`); mismatched `StarterPlanClicky` keys (`AUD-C-008`); `/mcp/composio` returns `404` (`AUD-C-011`); `/account/delete` deletes `0` rows (`AUD-C-012`) | `FAIL` |
| **6. SQLite Schema & Query Plans** | `EXPLAIN QUERY PLAN` on `sessions` and `notifications` | Indexed lookups | `SCAN sessions` and `SCAN notifications` + `USE TEMP B-TREE` (`0` secondary indexes: `AUD-C-010`) | `FAIL` |
| **7. 120-Request Load + 10 SSE Aborts** | `120` concurrent requests across `/health`, `/plan`, `/memory`, `/proactive/decide`, `/brain/turn` + `10` mid-stream `AbortController` disconnects | `0` HTTP 5xx errors; clean SSE abort handling | `120/120` succeeded (`0` errors, `+10.52 MB` RSS / `+2.21 MB` heap delta); `10/10` SSE aborts survived (`PASS`) | `PASS` |

---

### 2.4 WS-D OneBrain, Screen Layout & Adversarial Snap Suites (`tests/audit/ws-d/`)

1. **`tests/audit/ws-d/test-screen-layout.ts`** (`66` synthetic screenshot JPEGs in `tests/audit/ws-d/corpus/` + `35` adversarial `snapProgramToScreenLayout` cases):
   - **66-Image `analyzeScreenLayout` Corpus:**
     - **Tables (`31` positive, `35` negative):** `11/31` strict TP (`35.48%` recall), `16/31` any-table TP (`51.61%`), `15` FN (`0%` recall on dark mode, borderless, horizontal-rule-only, `<20px` narrow cols, `>115px` tall rows, 2-row tables, `>=2°` skew), `2` FP (IDE split panes & bar chart with gridlines) (`AUD-D-006`).
     - **Worksheet Answer Lines (`16` positive, `50` negative):** `6/16` strict TP (`37.50%`), `8/16` any-line TP (`50.00%`), `8` FN (`0%` recall on solid underlines, dashed lines, `2x` DPI 1440p/4K displays) (`AUD-D-006`).
     - **Main-Thread Blocking (`AUD-D-002`):** `1280x720` `p50 = 423.12 ms` (`p95 = 750.45 ms`); `1920x1080` `1,362.25 ms`; `2560x1440` `2,182.75 ms`; `4000x3000` `6,938.76 ms`.
   - **35-Case `snapProgramToScreenLayout` Adversarial Suite:**
     - **`16 PASS`, `19 FAIL` (`AUD-D-003`, `AUD-D-007`):** Proved greedy Case B regex deleting `step(2)` and `check()` calls when `receipt()` has a trailing `// comment` (`SNAP-01`, `SNAP-03`); `const w2 = write(...)` producing `SyntaxError: const w2 = ;` (`SNAP-04`); hardcoded RISC/CISC column swap (`SNAP-11`); false-positive `needsVitRescale` Y-axis warp (`SNAP-15`); and comment/string false matches (`SNAP-31`, `SNAP-32`).
2. **`tests/audit/ws-d/test-brain-prompts.ts`** (`105` prompts across 8 categories + live model probes + V8 sandbox checks):
   - **Model Probes (`AUD-D-001`):** Confirmed all 4 OpenRouter `:free` fallback models return `HTTP 404` and `baidu/ernie-5.5-flash-preview:free` is unreachable dead code.
   - **105-Prompt Matrix:** `105/105` valid SSE ordering (`meta -> status -> text -> ... -> done`), `104/105` valid JS syntax (`MATH-05` fails with `SyntaxError: const w2 = ;`), `0/15` `computer_calls` emitted on laptop action prompts (`AUD-D-008`), and `0%` filtering of `while(true){}`, `__hostRaw`, or `setInterval` floods (`AUD-D-004`, `AUD-D-005`).

---

### 2.5 WS-E Voice ("Ears" & "Mouth") & Dictation Suite (`tests/audit/ws-e/test-voice-dictation.ts`)

- **Command:** `npx --prefix src/backend tsx tests/audit/ws-e/test-voice-dictation.ts`
- **Evidence:** [`docs/audit/evidence/ws-e/voice-dictation-audit-results.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-e/voice-dictation-audit-results.json)

| Sub-Suite | Cases | `PASS` | `FAIL` | Key Findings Proved |
|---|---:|---:|---:|---|
| **1. STT ("Ears") Matrix** | 20 | 12 | 8 | `0 B`, `10 B`, `44 B`, corrupt WAV, and missing/invalid keys all return `HTTP 200` with `"Hello from Kursor local dictation transcription."` (`AUD-E-003`); 3-letter ISO language `yue` dropped (`AUD-E-012`); 1s silence hallucinates `"Thank you."` |
| **2. TTS ("Mouth") Latency & Concurrency** | 10 | 8 | 2 | Short/sequential/8-burst utterances succeed (`p50 = 961 ms`), **but** a `>7000ms` utterance times out without queue eviction, spawns a duplicate `powershell.exe`, and stalls subsequent `"Hi"` requests for `8.5s–9.5s` (`AUD-E-001`); `0` process exit listeners (`AUD-E-009`) |
| **3. Dictation Cleanup Benchmark** | 112 | 86 | 26 | `9/13` (`69.2%`) prompt-injection transcripts hijack live LLM cleanup into outputting `"PWNED"` / `"HACKED_BY_PROMPT_INJECTION"` (`AUD-E-005`); all 7 `DictationCleanupContext` fields ignored (`AUD-E-005`); `localCleanupHeuristic` corrupts pure fillers (`"um"`), URLs (`Https://...`), CLI commands, and emojis (`AUD-E-007`) |
| **4. C# `DictationCorrectionDetector` & Hotkeys** | 8 | 6 | 2 | Confirmed false-positive vocabulary learning on capitalized sentence starters with Levenshtein similarity `>= 0.3` (`AUD-E-010`) and `Ctrl+Alt+<Key>` chord triggering `PushToTalkDown` (`AUD-E-008`) |

---

### 2.6 WS-F & WS-G 2D/3D V8 Engine Suites (`tests/audit/ws-fg/test-2d-engine.mjs` & `test-3d-engine.mjs`)

1. **`node tests/audit/ws-fg/test-2d-engine.mjs`** (`57` ClickyDraw V8 programs):
   - **`54 PASS`, `3 EXPECTED_FAIL_CONFIRMED` (`0` unexpected failures):**
   - Verified all implemented 2D primitives (`line`, `curve`, `circle`, `ellipse`, `rect`, `poly`, `path`, `text`, `arrow`, `highlight`, `dot`, `point`, `target`, `hover`, `frame`, `ring`, `ink`, `write`, `check`, `cross`, `strike`, `sticky`, `paper`, `receipt`, `clipping`, `legend`, `graph`, `timeline`, `figure`, `scene3d`, `erase`, `recolor`, `move`, `scale`, `startOver`).
   - Proved `writeOnPage`, `callout`, `brace`, `table`, `chart` are `undefined` (`AUD-F-001`), `spot()` throws `ReferenceError` without polyfill (`AUD-F-002`), and `around.at` is ignored in favor of `around.center` (`AUD-F-003`).
2. **`node tests/audit/ws-fg/test-3d-engine.mjs`** (`40` Three.js procedural geometry & modifier tests):
   - **`39 PASS`, `1 FAIL` (`AUD-G-001`):**
   - Verified all 16 shape types + `cylinder` alias (`box`, `cyl`, `cone`, `tube`, `arrow`, `lathe`, `sphere`, `torus`, `capsule`, `pipe`, `spring`, `gear`, `extrude`, `blob`, `poly`, `surface`), all 6 modifiers (`thick`, `stretch`, `mirror`, `around`, `copies`, `cut`), and 17 parameter extremes (`NaN`, `±Infinity`, `0`, negative dimensions, `120,000` vertex cap guard).
   - Proved byte-for-byte that `SHAPES.cone` ignores `o.top` (`AUD-G-001`).

---

### 2.7 WS-H & WS-I .NET 9.0 Platform & Core Suite (`tests/audit/ws-hi/WsHiAuditRunner.csproj`)

- **Command:** `dotnet run --project tests/audit/ws-hi/WsHiAuditRunner.csproj`
- **Evidence:** [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt), [`docs/audit/evidence/ws-hi/thread-safety-scan.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/thread-safety-scan.txt)

| Suite # | Component Under Test | Cases Run | Result | Key Observations |
|---:|---|---:|---|---|
| **Suite 1** | `ClickyInkParser.EmphasisMarks` & `MarkingFirstLink` | 3 | `FAIL` (`AUD-I-003`) | Dynamically proved `**$420**` -> `"57345$157346"`, `==CRITICAL==` -> `"57347$157348"`, and `[Security Guide](...)` -> `"[57349Security Guide57350](...)"` |
| **Suite 2** | `SayLines.InOrder` & `SayLines.ByStep` | 3 | `FAIL` (`AUD-I-002`) | Proved extraction of 3 false-positive lines from `//` comments, `/* */` comments, and `write("...")` strings; missed `step (1)` / `say ("...")` with whitespace; corrupted `\r` -> `'r'` and `\u0021` -> `'u0021'` |
| **Suite 3** | `ClickyInkParser`, `ClickyInkLint`, `ClickyInkPatchStream`, `ClickyInkLiveNote`, `ClickyInkDashboardSplit`, `ClickyInkFrontPagePlanner` | 264 (`14` structured + `250` random fuzz seeds) | `PASS` (Resilience) | Completed 264 fuzz cases (including 200 KB huge note, 120-deep lists, unclosed `<ink-card>` tags, malformed tables) in `339 ms` with `0` unhandled exceptions |
| **Suite 4** | `ClickyInkNoteStore` & `ClickyLiveNoteStore` | 2 (Single-thread + 8-thread stress) | `FAIL` (`AUD-I-004`) | Single-threaded persistence & checkbox toggle passed; 8-thread concurrent access threw `50` `InvalidOperationException` errors (`Operations that change non-concurrent collections must have exclusive access`) |
| **Suite 5** | `DrawingCaptionTiming`, `BrainSseParser`, `BrainSession` | 3 | `PASS` | `BrainSseParser` parsed 7-byte chunked UTF-8 + `>16KB` line; `BrainSession` compacted 50 screenshot turns down to `33` (`<= 48`); noted `DrawingCaptionTiming` propagates `NaN` if speed is `NaN` |
| **Suite 6** | `PenRideTrajectory` & `EarconPlayer.DecodeWavToStereo48k` | 2 | `FAIL` (`AUD-H-009`) | `PenRideTrajectory` sampled cleanly; `EarconPlayer.DecodeWavToStereo48k` threw `DivideByZeroException` on WAV header with `bitsPerSample = 0` |

---

### 2.8 WS-J & WS-K Autonomous Agents & Desktop UI Suites (`tests/audit/ws-jk/`)

1. **`tests/audit/ws-jk/test-agents-harness.ps1`** (`30` scenarios + `7` live backend route probes):
   - **`23 PASS`, `7 FAIL`:**
   - Verified `h1-mac` local runtime protocol (`POST /turn`, `POST /steer`, `POST /cancel`, `GET /events`) and all 16 H1 tools (`update_plan`, `progress`, `write`, `read`, `edit`, `bash`, `note`, `note_add`, `exec`, `fetch_url`, `web_search`, `ask_user`, HTTP MCP, stdio MCP bridge) against a mock upstream responder.
   - Proved `7/7` missing Worker/MCP proxy routes on `127.0.0.1:8787` (`AUD-J-001`), `HTTP 500` on unregistered `h1-cloud`, `codex-cloud`, and `pi-mac` drivers (`AUD-J-002`), zero filesystem sandbox when `write` targets paths outside the job workspace (`AUD-J-003`), unadapted `mac_step` instruction (`AUD-J-006`), and 24/7 `PowerRequestSystemRequired` sleep lock (`AUD-J-007`).
2. **`tests/audit/ws-jk/test-ui-surfaces.ps1`** (`19` UI surface, Win32, UIA, DPAPI & static checks):
   - **`8 PASS`, `10 FAIL`, `1 UNVERIFIED`:**
   - Verified live HWND styles (`WS_EX_NOREDIRECTIONBITMAP`, `WDA_EXCLUDEFROMCAPTURE = 0x11` on Notch), DPAPI `AuthTokenStore` (`auth.localdev.bin`) and `CustomConnectorStore` (`connectors.secrets`) round-trip encryption, and all 33 `Kursor.App` namespaces.
   - Proved missing keyboard accessibility on `Pressable` (`AUD-K-002`), orphaned `FriendCodeGroup()` (`AUD-K-001`), hardcoded `DeveloperFlags.FullAgentActivity => false` (`AUD-K-003`), hardcoded Gmail bug report URL + Sentry upload (`AUD-K-004`), and `JsonAppSettings` vs `CoreAppSettings` conflict (`AUD-K-007`).

---

### 2.9 WS-L & WS-M Security PoC, Performance & Soak Suite (`tests/audit/ws-lm/test-perf-reliability.ts`)

- **Command:** `npx --prefix src/backend tsx tests/audit/ws-lm/test-perf-reliability.ts`
- **Evidence:** [`docs/audit/ws-lm/PERF_DATA.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-lm/PERF_DATA.json), [`docs/audit/evidence/ws-lm/security-pocs.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/security-pocs.json), [`docs/audit/evidence/ws-lm/soak-and-recovery-log.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-lm/soak-and-recovery-log.json)

| Sub-Suite | Description | Result | Key Metrics / Proof |
|---|---|---|---|
| **PoC 1: Cross-Origin Key & Memory Exfiltration** | `Origin: https://evil.attacker.example` requests to `/v2/dictation/gemini-token`, `/deepgram/token`, `/realtime/token`, `/memory`, `/agent/integrations` | `FAIL` (`AUD-L-001`) | `200 OK` with `Access-Control-Allow-Origin: *` and live API keys / memory returned without auth |
| **PoC 2: Gzip Bomb Magnification** | `16.4 KB` gzipped zero-payload sent to `/v2/dictation/cleanup` | `FAIL` (`AUD-L-002`) | Synchronously decompressed `16 MB` on main event loop |
| **PoC 3: Open Redirect Token Leak** | `GET /auth/v1/authorize?redirect_uri=https://evil.attacker.example/steal` | `FAIL` (`AUD-L-003`) | `302` redirect with `access_token` & `refresh_token` in query string |
| **PoC 4: Unstripped Base64 Screenshot in SQLite** | `/brain/turn` with `50.4 KB` JPEG in `thread` | `FAIL` (`AUD-L-006`) | Raw `data:image/jpeg;base64,...` persisted inside SQLite `threads.messages_json` |
| **215-Turn Soak + 15 SSE Aborts + 73 Interleaved** | `200` full `/brain/turn` + `15` aborted SSE + `43` `/v2/dictation/cleanup` + `30` `/proactive/jobs` | `PASS` (Stability) / `FAIL` (`AUD-M-002` DB growth) | `200/200` turns succeeded, `15/15` aborts survived; SQLite `.db + .db-wal` grew from `49 KB` to `>1.35 MB` (`10.06 MB` on live `kursor.db`) |
| **Warm PowerShell TTS Kill Recovery** | `SIGKILL` child `powershell.exe` 80ms into `/agent/realtime/speak` + immediate retry | `PASS` | In-flight call returned `200 OK` fallback WAV; next call respawned new `powershell.exe` PID and returned valid speech WAV |

---

## 3. Pass 2 Independent Cross-Audit Verification Matrix (`docs/audit/pass2/`)

Every Pass 1 workstream (`WS-A` through `WS-N`) was independently cross-audited in Pass 2 by a different specialist agent who did **not** author the Pass 1 report:

| Pass 2 Report & JSON | Workstreams Audited | Primary Author | Independent Cross-Auditor | Pass 1 Findings Checked | `CONFIRMED` | `DISPUTED` | New Pass 2 Findings Added |
|---|---|---|---|---:|---:|---:|---|
| [`cross-audit-ws-ab.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-ab.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-ab.json) | **WS-A, WS-B, WS-N** | `ws-ab-build-brand` | `ws-lm-sec-perf` | 20 | 20 (`19` exact + `1` refined) | 0 | `3` (`AUD-A-011`, `AUD-B-006`, `AUD-N-006`) |
| [`cross-audit-ws-c.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-c.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-c.json) | **WS-C** | `ws-c-backend` | `ws-e-voice-dictation` | 15 | 15 (`12` exact + `3` refined) | 0 | `3` (`AUD-C-016`..`018`) |
| [`cross-audit-ws-d.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-d.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-d.json) | **WS-D** | `ws-d-brain-vision` | `ws-c-backend` | 8 | 8 (`7` exact + `1` refined) | 0 | `4` (`AUD-D-009`..`012`) |
| [`cross-audit-ws-e.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-e.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-e.json) | **WS-E** | `ws-e-voice-dictation` | `ws-fg-drawing-2d-3d` | 12 | 12 (`12` exact) | 0 | `3` (`AUD-E-013`..`015`) |
| [`cross-audit-ws-fg-k.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-fg-k.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-fg-k.json) | **WS-F, WS-G, WS-K** | `ws-fg-drawing-2d-3d` & `ws-jk-agents-ui` | `ws-ab-build-brand` | 26 (`16` F/G + `10` K) | 26 (`25` exact + `1` refined) | 0 | `3` (`AUD-F-011`, `AUD-G-007`, `AUD-K-011`) |
| [`cross-audit-ws-hi.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-hi.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-hi.json) | **WS-H, WS-I** | `ws-hi-platform-core` | `ws-d-brain-vision` | 15 | 15 (`7` exact + `8` refined) | 0 | `4` (`AUD-H-011`..`012`, `AUD-I-006`..`007`) |
| [`cross-audit-ws-j.md`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-j.md) / [`.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-j.json) | **WS-J** | `ws-jk-agents-ui` | `ws-fg-drawing-2d-3d` | 8 | 8 (`5` exact + `3` refined) | 0 | `4` (`AUD-J-009`..`012`) |

---

## 4. Running the Master Non-Destructive Audit Runner (`tests/audit/run-audit.ps1`)

### 4.1 Quick / Default Mode (Fast Live Suites + Evidence Verification)
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/audit/run-audit.ps1 -Quick
```
- **What it runs live (`~15–25s`):**
  1. `ws-ab` (`tests/audit/ws-ab/test-ws-ab.ps1` — 22 checks)
  2. `ws-f-2d` (`tests/audit/ws-fg/test-2d-engine.mjs` — 57 V8 programs)
  3. `ws-g-3d` (`tests/audit/ws-fg/test-3d-engine.mjs` — 40 Three.js shape/modifier/extreme tests)
  4. `ws-hi-dotnet` (`tests/audit/ws-hi/WsHiAuditRunner.csproj` — 264 Ink fuzz cases + 11 unit/concurrency repros)
  5. `ws-k-ui` (`tests/audit/ws-jk/test-ui-surfaces.ps1` — 19 Win32/UIA/DPAPI/surface checks)
- **What it verifies from captured evidence (unless `-Full` / `-IncludeLongBenchmarks` is passed):**
  6. `ws-j-agents` (`docs/audit/evidence/ws-jk/agents-harness-test-results.json` — 30 harness scenarios)
  7. `ws-c-backend` (`docs/audit/evidence/ws-c/route-test-matrix.json` — 111 routes + 120 load + 10 SSE aborts)
  8. `ws-d-layout` (`docs/audit/evidence/ws-d/screen-layout-benchmark.json` — 66 JPEGs + 35 adversarial snap cases)
  9. `ws-d-prompts` (`docs/audit/evidence/ws-d/brain-prompt-matrix.json` — 105 prompts + 8 model probes + 5 V8 checks)
  10. `ws-e-voice` (`docs/audit/evidence/ws-e/voice-dictation-audit-results.json` — 20 STT + 10 TTS + 112 cleanup + 8 C# checks)
  11. `ws-lm-sec-perf` (`docs/audit/ws-lm/PERF_DATA.json` — 5 security PoCs + 215 soak turns + kill recovery)

### 4.2 Full Mode (Re-runs All Live Network, LLM & Multi-Minute Soak Suites)
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/audit/run-audit.ps1 -Full
```

