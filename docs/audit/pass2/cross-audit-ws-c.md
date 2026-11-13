# Pass 2 Independent Cross-Audit Report — WS-C (Backend Worker, 111 HTTP Routes, Auth, Gzip/CORS Middleware, SQLite, Key Minting, Search, Connectors & Load/Soak)

- **Cross-Audit ID:** `PASS2-WS-C`
- **Cross-Auditor:** `ws-e-voice-dictation`
- **Timestamp:** `2026-11-13T12:31:00Z`
- **Target Workstream:** `WS-C` (`ws-c-backend`)
- **Primary Pass 1 Artifacts Audited:**
  - [`docs/audit/ws-c/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-c/REPORT.md)
  - [`docs/audit/ws-c/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-c/findings.json)
  - [`docs/audit/ws-c/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-c/coverage.json)
  - [`tests/audit/ws-c/test-backend-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-c/test-backend-audit.ts)
  - [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json)

---

## 1. Executive Summary

We performed an independent Pass 2 cross-audit of **WS-C** (`src/backend/src/`, `src/desktop/Kursor.Core/`, and all WS-C audit artifacts). Every Critical and High finding (`AUD-C-001` through `AUD-C-008`) as well as all Medium/Low findings (`AUD-C-009` through `AUD-C-015`) were independently re-verified against the TypeScript backend source files, the C# desktop client DTOs/handlers, and the live test evidence in `docs/audit/evidence/ws-c/`.

| Metric | Count |
| --- | :---: |
| **Pass 1 Findings Reviewed** | **15** (`AUD-C-001` .. `AUD-C-015`) |
| **`CONFIRMED` (Exact Match)** | **12** |
| **`CONFIRMED_WITH_REFINEMENT` (Core Defect Confirmed + Code Line/Mechanism Refined)** | **3** (`AUD-C-003`, `AUD-C-010`, `AUD-C-014`) |
| **`DISPUTED`** | **0** |
| **New Findings Discovered in Pass 2** | **3** (`AUD-C-016` High, `AUD-C-017` Medium, `AUD-C-018` Medium) |

---

## 2. Independent Verification of Pass 1 Findings (`AUD-C-001` .. `AUD-C-015`)

| Finding ID | Pass 1 Severity | Pass 2 Verdict | Independent Source & Evidence Verification |
| --- | :---: | :---: | --- |
| `AUD-C-001` | **Critical** | `CONFIRMED` | Verified `app.use("*", cors())` at [`src/backend/src/index.ts:19`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts#L19), unconditional `DEFAULT_USER_ID` auto-authentication at [`src/backend/src/middleware/auth.ts:34-88`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L34-L88), raw master API key exposure in [`src/backend/src/routes/voice.ts:18-31`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L18-L31) (`/deepgram/token`), [`53-83`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L53-L83) (`/v2/dictation/gemini-token`), [`169-211`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L169-L211) (`/realtime/token` fallback), and unauthenticated cross-origin read/write on `/memory` ([`src/backend/src/routes/worker.ts:520-560`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L520-L560)). |
| `AUD-C-002` | **High** | `CONFIRMED` | Verified [`src/backend/src/middleware/gzip.ts:18-20`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts#L18-L20): `await c.req.raw.arrayBuffer()` and `gunzipSync(compressedBuffer)` run synchronously on the main thread with no compressed size cap and no `maxOutputLength` option (`15,331 B` -> `15,728,652 B`, `1,025.9x` magnification). |
| `AUD-C-003` | **High** | `CONFIRMED_WITH_REFINEMENT` | Confirmed Open Redirect + 1-year `access_token` leak on `GET /auth/v1/authorize` ([`src/backend/src/routes/auth.ts:100-121`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L100-L121)). **Refinement:** `AUD-C-003` attributed the unvalidated `redirect_to` HTML link at lines `76-85` to `GET /auth/start`, whereas lines `56-94` are actually `GET /auth/callback` (which injects unvalidated `redirect_to` into both `href` and `window.location.href` — elevated separately as `AUD-C-016` below), while `GET /auth/start` (`lines 128-175`) builds `location` from `scheme === "kursor" ? "kursor" : "heyclicky"`. |
| `AUD-C-004` | **High** | `CONFIRMED` | Verified [`src/backend/src/routes/auth.ts:27-39`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L27-L39) (`createLocalJwt` hardcodes `base64UrlEncode("kursor-local-dev-signature")`), [`auth.ts:236-248`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L236-L248) (`/auth/v1/logout`), and [`src/backend/src/middleware/auth.ts:34-88`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L34-L88) (never verifies signature, `exp`, or `sessions` table membership). |
| `AUD-C-005` | **High** | `CONFIRMED` | Verified [`src/backend/src/routes/brain.ts:1021-1045`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L1021-L1045): `await tryCallUpstreamLlm(...)` is executed unconditionally at line 1021 before checking `if (upstream && !body.force_local_fallback)` at line 1045. |
| `AUD-C-006` | **High** | `CONFIRMED` | Verified [`src/backend/src/test-phase1.ts:155`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts#L155) (`assert.equal(data.mouth.primary_provider, "openai-realtime")` fails when `.env` sets `MOUTH_PRIMARY_PROVIDER=gemini-tts`) and [`src/backend/src/routes/voice.ts:263-320`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L263-L320) (`ensureWarmSynthWorker()` spawns `powershell.exe` at module load without `.unref()`, hanging `test-phase1.ts` and `test-phase2.ts` on exit). |
| `AUD-C-007` | **High** | `CONFIRMED` | Verified [`src/backend/src/middleware/auth.ts:44-60`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L44-L60) and [`src/backend/src/routes/worker.ts:206-340`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L206-L340) against [`WorkerClient.cs:59-68`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Worker/WorkerClient.cs#L59-L68) and `RegisteredDevice.cs`. |
| `AUD-C-008` | **High** | `CONFIRMED` | Verified [`src/backend/src/routes/worker.ts:406-485`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L406-L485) against [`StarterPlanResponse.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Worker/StarterPlanResponse.cs), [`StarterPlanClicky.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Worker/StarterPlanClicky.cs), and [`StarterPlanSuggestion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Worker/StarterPlanSuggestion.cs). |
| `AUD-C-009` | **Medium** | `CONFIRMED` | Verified hardcoded `const LOCAL_APP_CONFIG_PATH = "C:\\Users\\ubaid\\AppData\\Local\\HeyClicky\\app-config.json";` at [`src/backend/src/routes/worker.ts:8`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L8). |
| `AUD-C-010` | **Medium** | `CONFIRMED_WITH_REFINEMENT` | Verified zero `CREATE INDEX` statements in [`src/backend/src/db/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/db/index.ts), `SCAN sessions` and `SCAN notifications` + `USE TEMP B-TREE FOR ORDER BY`, unbounded `sessions` growth, and raw base64 screenshot storage in `threads.messages_json` ([`src/backend/src/routes/brain.ts:172-187`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L172-L187)). **Refinement:** In `db/index.ts:78-113`, the actual column names for the recommended indexes are `notifications(user_id, is_read, created_at DESC)`, `proactive_cards(user_id, dismissed)`, and `threads(user_id)` (there is no `threads.session_id` column; `sessionId` is stored in `threads.id`). |
| `AUD-C-011` | **Medium** | `CONFIRMED` | Verified [`src/backend/src/routes/agents.ts:943`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L943) (`/mcp/composio` -> 404) and [`src/backend/src/routes/worker.ts:983, 990`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L983-L990) (`/billing/local-max-active` -> 404, `/billing/local-portal` -> 404). |
| `AUD-C-012` | **Medium** | `CONFIRMED` | Verified no-op/hardcoded stubs in [`src/backend/src/routes/worker.ts:995-1050`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L995-L1050) (`/account/delete`, `/telemetry/client-error`, `/me/companion-history`) and [`src/backend/src/routes/agents.ts:929-934, 1112-1173`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L929-L934). |
| `AUD-C-013` | **Medium** | `CONFIRMED` | Verified volatile in-memory state in [`src/backend/src/routes/agents.ts:83-85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L83-L85) (`proactiveJobs`, `proactiveCardStatuses`), [`534`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L534) (`turnLeases`), [`711`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L711) (`integrationsCatalog`), and [`972`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L972) (`calendarEvents`). |
| `AUD-C-014` | **Medium** | `CONFIRMED_WITH_REFINEMENT` | Confirmed fragile regex in `searchDuckDuckGoHtml` ([`src/backend/src/routes/worker.ts:620-621`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L620-L621)) and silent HTTP 200 synthetic fallback (`worker.ts:756-766`). **Refinement:** In `decodeHtmlEntities` ([`src/backend/src/routes/worker.ts:571-582`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L571-L582)), `.replace(/&amp;/g, "&")` runs at line 573 before `.replace(/&lt;/g, "<").replace(/&gt;/g, ">")` at lines 574–575, and `.replace(/<[^>]+>/g, "")` runs at line 580 *after* entity decoding. This causes double-unescaping (`&amp;lt;` -> `&lt;` -> `<`) AND causes legitimate angle-bracket expressions in snippets (e.g., `a &lt; b &amp;&amp; c &gt; d` or `List&lt;string&gt;`) to be deleted by `/<[^>]+>/g`. |
| `AUD-C-015` | **Low** | `CONFIRMED` | Verified silent `try { body = await c.req.json(); } catch {}` fallback across `auth.ts`, `worker.ts`, `brain.ts`, and `agents.ts`, and unbounded payload acceptance in `PUT/POST /memory` ([`src/backend/src/routes/worker.ts:520-553`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts#L520-L553)). |

---

## 3. Newly Discovered Findings in Pass 2 (`AUD-C-016` .. `AUD-C-018`)

### `ID: AUD-C-016`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `GET /auth/callback` and `GET /callback` Allow DOM XSS via Unvalidated `javascript:` Scheme in `redirect_to` Parameter
- **Component:** Backend Auth Callback HTML Route
- **Location:** [`src/backend/src/routes/auth.ts:56-94`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L56-L94)
- **Description:** In `handleAuthCallback` (`GET /auth/callback` and `GET /callback`), line 58 reads `const redirectTo = c.req.query("redirect_to") || "heyclicky://auth-callback";` without validating the URL protocol or host. Line 60 constructs `targetUrl` via `buildCallbackUrl(redirectTo, { code, state })`, and lines 84 and 88 render `href="${escapeHtml(targetUrl)}"` and `<script>window.location.href = ${JSON.stringify(targetUrl)};</script>` inside an HTML page served from `http://127.0.0.1:8787`. Because neither `escapeHtml()` nor `JSON.stringify()` blocks the `javascript:` URI scheme, passing `redirect_to=javascript:alert(document.domain)//` causes the inline script to immediately execute `window.location.href = "javascript:alert(document.domain)//?code=..."` in the `http://127.0.0.1:8787` origin.
- **Reproduction Steps:**
  1. Request `GET http://127.0.0.1:8787/auth/callback?redirect_to=javascript:fetch('http://127.0.0.1:8787/deepgram/token',{method:'POST'})//`.
  2. Inspect lines 84 and 88 of the returned HTML document.
- **Observed Result:** The server returns `200 OK` (`text/html`) containing `<script>window.location.href = "javascript:fetch('http://127.0.0.1:8787/deepgram/token',{method:'POST'})//?code=...";</script>`, executing arbitrary JavaScript in the `http://127.0.0.1:8787` origin.
- **Expected Result:** Validate `redirect_to` against an explicit scheme/host allowlist (`heyclicky:`, `kursor:`, or loopback `http://127.0.0.1` / `http://localhost`) and reject `javascript:`, `data:`, and external URLs with HTTP 400.
- **Impact:** Same-origin arbitrary JavaScript execution (DOM XSS) in the `http://127.0.0.1:8787` origin, which would bypass even a fixed CORS configuration.
- **Remediation:** Validate the parsed URL protocol and hostname of `redirect_to` in `buildCallbackUrl` / `handleAuthCallback` (`src/backend/src/routes/auth.ts:48-94`) before embedding it in HTML or `window.location.href`.
- **Effort:** `S`

---

### `ID: AUD-C-017`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `nextSuggestionNumericId` Resets to `101` on Backend Restart and Overwrites/Un-Dismisses Existing `proactive_cards` Rows in SQLite
- **Component:** Backend Proactive Agents Persistence
- **Location:** [`src/backend/src/routes/agents.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L85), [`104-107`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L104-L107), [`156-175`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts#L156-L175)
- **Description:** `src/backend/src/routes/agents.ts:85` initializes `let nextSuggestionNumericId = 101;` as an in-memory counter rather than seeding it from `SELECT MAX(CAST(id AS INTEGER)) FROM proactive_cards`. When `createProactiveJob()` runs (`agents.ts:104-107, 156-175`), it assigns IDs `"101"`, `"102"`, etc. and executes `INSERT INTO proactive_cards (...) VALUES (...) ON CONFLICT(id) DO UPDATE SET title = excluded.title, ..., dismissed = 0`. Consequently, after every backend restart, the next proactive job creation reuses IDs `"101"`, `"102"`, ... and overwrites previously stored cards in SQLite, resetting `dismissed = 0` on cards the user had already dismissed or resolved.
- **Reproduction Steps:**
  1. Create a proactive job via `POST /proactive/jobs` (creates cards `101` and `102`) and resolve card `101` via `POST /proactive/cards/resolve` (`{"card_ids":["101"],"resolution":"dismissed"}`).
  2. Restart the backend process (`nextSuggestionNumericId` resets to `101`) and call `POST /proactive/jobs` again.
  3. Query `SELECT id, dismissed, payload_json FROM proactive_cards WHERE id = '101'`.
- **Observed Result:** Card `"101"` in SQLite is overwritten and its `dismissed` flag is reset from `1` back to `0` (`status: "presented"`).
- **Expected Result:** Seed `nextSuggestionNumericId` from `SELECT COALESCE(MAX(CAST(id AS INTEGER)), 100) + 1 FROM proactive_cards` at module startup.
- **Impact:** Dismissed or resolved proactive suggestion cards resurrect after a backend restart.
- **Remediation:** Query `MAX(CAST(id AS INTEGER))` from `proactive_cards` when initializing `nextSuggestionNumericId` in `src/backend/src/routes/agents.ts:85`.
- **Effort:** `S`

---

### `ID: AUD-C-018`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `GET /auth/v1/authorize` and `GET /auth/start` Mint Access Tokens Without Inserting Session Rows Into SQLite `sessions` Table
- **Component:** Backend Auth Session Persistence
- **Location:** [`src/backend/src/routes/auth.ts:107-121`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L107-L121), [`131-175`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts#L131-L175)
- **Description:** While `POST /auth/v1/token` (`src/backend/src/routes/auth.ts:198-207`) inserts each minted `access_token` and `refresh_token` into the SQLite `sessions` table, `GET /auth/v1/authorize` (`auth.ts:107-121`) and `GET /auth/start` (`auth.ts:131-175`) call `createLocalJwt(DEFAULT_USER_ID, DEFAULT_EMAIL)` and return the minted `access_token` and `refresh_token` directly to the caller without inserting any row into SQLite `sessions`.
- **Reproduction Steps:**
  1. Call `GET /auth/start?format=json` and extract `access_token`.
  2. Query `SELECT COUNT(*) FROM sessions WHERE access_token = ?` in `data/kursor.db`.
- **Observed Result:** `0` rows exist in SQLite `sessions` for tokens minted via `GET /auth/v1/authorize` or `GET /auth/start`.
- **Expected Result:** Every endpoint that mints an `access_token` and `refresh_token` must record the session in SQLite `sessions` so token validation, refresh, and logout behave consistently.
- **Impact:** Once `AUD-C-004` is remediated to enforce `sessions` table lookups in `clientHeadersAndAuthMiddleware`, tokens minted via `/auth/v1/authorize` or `/auth/start` will fail authentication unless those routes also insert into `sessions`.
- **Remediation:** Extract a shared `createAndPersistSession(userId, email)` helper in `src/backend/src/routes/auth.ts` and call it from `handleAuthorize`, `handleAuthStart`, and `handleToken`.
- **Effort:** `S`
