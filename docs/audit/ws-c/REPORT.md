# WS-C Audit Report — Backend Worker, API Contracts, Auth, SQLite, Key Minting, Search, Connectors & Load/Soak

- **Workstream:** `WS-C` (`ws-c-backend`)
- **Target Component:** Kursor Local Backend (`src/backend/src/`)
- **Audited Files:**
  - [`src/backend/src/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts) (85 lines)
  - [`src/backend/src/db/index.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/db/index.ts) (170 lines)
  - [`src/backend/src/middleware/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts) (88 lines)
  - [`src/backend/src/middleware/gzip.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts) (53 lines)
  - [`src/backend/src/routes/auth.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/auth.ts) (248 lines)
  - [`src/backend/src/routes/worker.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/worker.ts) (1,050 lines)
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts) (831 lines)
  - [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts) (1,303 lines)
  - [`src/backend/src/routes/agents.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/agents.ts) (1,173 lines)
  - [`src/backend/src/utils/screenLayout.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts) (881 lines)
  - [`src/backend/src/test-phase1.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts) (501 lines)
  - [`src/backend/src/test-phase2.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase2.ts) (571 lines)
- **Automated Audit Suite:** [`tests/audit/ws-c/test-backend-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-c/test-backend-audit.ts)
- **Evidence Artifacts:** [`docs/audit/evidence/ws-c/`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/)

---

## 1. Executive Summary

We performed a full-spectrum code, contract, security, database, and runtime load/soak audit of the Kursor backend (`src/backend/`). Every single one of the **111 HTTP routes** enumerated in [`docs/audit/enumeration.json`](file:///C:/dev/heyclicky-clone/docs/audit/enumeration.json), all 3 global middlewares (`cors()`, `gzipMiddleware`, `clientHeadersAndAuthMiddleware`), the SQLite persistence layer (`data/kursor.db`), and both existing test suites (`test-phase1.ts`, `test-phase2.ts`) were inspected line-by-line and exercised against a live server instance.

### Summary of Findings by Severity

| Severity | Count | Finding IDs |
| --- | :---: | --- |
| **Critical** | **1** | `AUD-C-001` |
| **High** | **7** | `AUD-C-002`, `AUD-C-003`, `AUD-C-004`, `AUD-C-005`, `AUD-C-006`, `AUD-C-007`, `AUD-C-008` |
| **Medium** | **6** | `AUD-C-009`, `AUD-C-010`, `AUD-C-011`, `AUD-C-012`, `AUD-C-013`, `AUD-C-014` |
| **Low** | **1** | `AUD-C-015` |
| **Total** | **15** | `AUD-C-001` .. `AUD-C-015` |

### Key Takeaways
1. **Cross-Origin Browser Key & Memory Exfiltration (`AUD-C-001` — Critical):** `src/backend/src/index.ts:19` enables `Access-Control-Allow-Origin: *` globally, `src/backend/src/middleware/auth.ts:34-88` auto-authenticates every unauthenticated request as `DEFAULT_USER_ID`, and `src/backend/src/routes/voice.ts` returns raw master API keys (`DEEPGRAM_API_KEY`, `GEMINI_API_KEY`, and `OPENAI_API_KEY` on fallback) in JSON responses. Any website visited in the user's browser can `fetch("http://127.0.0.1:8787/...")` to steal cloud API keys and read/modify private OneBrain memory (`/memory`).
2. **Gzip Decompression Magnification DoS (`AUD-C-002` — High):** `gzipMiddleware` (`src/backend/src/middleware/gzip.ts:18-20`) runs synchronous `gunzipSync()` on the main thread with no `maxOutputLength` limit and no compressed size cap. Our test confirmed a 15 KB compressed payload expands 1,025.9x into 15.7 MB synchronously in memory; a larger gzip bomb can freeze or OOM-crash the backend.
3. **Open Redirect & Fake Static JWT Signature (`AUD-C-003`, `AUD-C-004` — High):** `GET /auth/v1/authorize` redirects to any arbitrary external `redirect_to` URL (`302`) with a newly minted 1-year `access_token` appended in the query string. Tokens use the literal ASCII string `"kursor-local-dev-signature"` base64url-encoded as their signature, and `POST /auth/v1/logout` does not invalidate tokens because `clientHeadersAndAuthMiddleware` never checks signature validity or the `sessions` table.
4. **Live Upstream LLM Calls Despite `force_local_fallback: true` (`AUD-C-005` — High):** `POST /brain/turn` (`src/backend/src/routes/brain.ts:1021`) awaits `tryCallUpstreamLlm()` *before* checking `!body.force_local_fallback` at line 1045, burning live Groq/OpenRouter/OpenAI/Gemini quota and adding multi-second latency even on offline/fallback test turns.
5. **Existing `npm test` Fails Out-of-the-Box and Hangs Forever on Exit (`AUD-C-006` — High):** `test-phase1.ts:155` fails with an `AssertionError` when `.env` sets `MOUTH_PRIMARY_PROVIDER=gemini-tts`, and `voice.ts:316` spawns a persistent `powershell.exe` child process at module load without `.unref()`, preventing `node` from exiting after `server.close()`.
6. **C# Client Header & DTO Mismatches (`AUD-C-007`, `AUD-C-008`, `AUD-C-011` — High/Medium):** `ClickyClientHeadersHandler.cs` sends `X-Clicky-Platform`, `X-Clicky-Device`, and `X-Clicky-App-Version`, whereas `middleware/auth.ts` reads `x-clicky-client-platform`, `x-clicky-device-id`, and `x-clicky-client-version`. Furthermore, `POST /me/devices`, `GET /me/devices`, and `POST /onboarding/starter-plan` return JSON schemas missing required C# DTO fields (`RegisteredDevice.Id`, `DisplayName`, `IsThisDevice`, `StarterPlanClicky.Key`, `Name`, `Role`, `StarterPlanSuggestion.AgentPrompt`), and `/agent/composio/session`, `/billing/checkout`, and `/billing/portal` return local URLs (`/mcp/composio`, `/billing/local-max-active`, `/billing/local-portal`) that return `404 Not Found`.

---

## 2. Complete 111-Route Enumeration & Audit Matrix

Every route registered across `index.ts`, `auth.ts`, `worker.ts`, `voice.ts`, `brain.ts`, and `agents.ts` was tested for happy path, missing/invalid auth, CORS headers, malformed JSON handling, and C# contract fidelity (full raw output in [`docs/audit/evidence/ws-c/route-test-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/route-test-matrix.json)).

### 2.1 Root & Health Routes (`src/backend/src/index.ts`)

| # | Method | Path | Location | Auth Enforced? | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | --- | :---: | --- | :---: | --- |
| 1 | `GET` | `/health` | `index.ts:23` | No (Public) | Real (SQLite `SELECT 1`) | `200` | **PASS** (functional health probe; wildcard CORS `AUD-C-001`) |
| 2 | `GET` | `/v1/health` | `index.ts:44` | No (Public) | Real (`307` -> `/health`) | `307` | **PASS** |

### 2.2 Auth & Supabase GoTrue Emulation Routes (`src/backend/src/routes/auth.ts` — mounted at `/auth` and `/`)

| # | Method | Path | Location | Auth Enforced? | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | :---: | :---: | --- | :---: | --- |
| 3 | `GET` | `/auth/start` | `auth.ts:56` | No | Local OAuth HTML/JSON | `200` | **FAIL** (`AUD-C-003` unvalidated `redirect_to` scheme; `AUD-C-004` static JWT) |
| 4 | `GET` | `/start` | `auth.ts:56` | No | Alias of `/auth/start` | `200` | **FAIL** (`AUD-C-003`, `AUD-C-004`) |
| 5 | `GET` | `/auth/v1/authorize` | `auth.ts:100` | No | Local OAuth `302` redirect | `302` | **FAIL** (`AUD-C-003` open redirect leaking minted 1-year `access_token` in URL) |
| 6 | `GET` | `/v1/authorize` | `auth.ts:100` | No | Alias of `/auth/v1/authorize` | `302` | **FAIL** (`AUD-C-003`, `AUD-C-004`) |
| 7 | `GET` | `/auth/callback` | `auth.ts:127` | No | Local OAuth callback | `200` | **FAIL** (`AUD-C-004` fake static signature JWT) |
| 8 | `GET` | `/callback` | `auth.ts:127` | No | Alias of `/auth/callback` | `200` | **FAIL** (`AUD-C-004`) |
| 9 | `POST` | `/auth/v1/token` | `auth.ts:177` | No | Mints JWT + inserts `sessions` row | `200` | **FAIL** (`AUD-C-004` fake static JWT; `AUD-C-010` unbounded `sessions` table growth) |
| 10 | `POST` | `/v1/token` | `auth.ts:177` | No | Alias of `/auth/v1/token` | `200` | **FAIL** (`AUD-C-004`, `AUD-C-010`) |
| 11 | `GET` | `/auth/v1/user` | `auth.ts:219` | No (Auto-Auth) | Queries `users` table | `200` | **FAIL** (`AUD-C-001`, `AUD-C-004` returns user profile with no/invalid token) |
| 12 | `GET` | `/v1/user` | `auth.ts:219` | No (Auto-Auth) | Alias of `/auth/v1/user` | `200` | **FAIL** (`AUD-C-001`, `AUD-C-004`) |
| 13 | `ALL` | `/auth/v1/logout` | `auth.ts:236` | No (Auto-Auth) | Deletes from `sessions` | `200` | **FAIL** (`AUD-C-004` revoked token still accepted everywhere; `AUD-C-010` full table scan) |
| 14 | `ALL` | `/v1/logout` | `auth.ts:236` | No (Auto-Auth) | Alias of `/auth/v1/logout` | `200` | **FAIL** (`AUD-C-004`, `AUD-C-010`) |

### 2.3 Worker, Device, Onboarding, Search, Memory & Billing Routes (`src/backend/src/routes/worker.ts` — mounted at `/` and `/v1`)

| # | Method | Path | Location | Auth Enforced? | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | :---: | :---: | --- | :---: | --- |
| 15 | `GET` | `/me/plan` | `worker.ts:161` | No (Auto-Auth) | Real (SQLite `users` + plan payload) | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| 16 | `GET` | `/v1/me/plan` | `worker.ts:161` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 17 | `GET` | `/plan` | `worker.ts:194` | No (Auto-Auth) | Real | `200` | **FAIL** (`AUD-C-001`) |
| 18 | `GET` | `/v1/plan` | `worker.ts:194` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 19 | `POST` | `/me/devices` | `worker.ts:209` | No (Auto-Auth) | Real (SQLite `devices`), schema mismatch | `200` | **FAIL** (`AUD-C-007` ignores `X-Clicky-Device` & `display_name`; omits `id`, `is_this_device`, `capabilities`) |
| 20 | `POST` | `/v1/me/devices` | `worker.ts:209` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-007`) |
| 21 | `POST` | `/devices/register` | `worker.ts:264` | No (Auto-Auth) | Real (SQLite `devices`), schema mismatch | `200` | **FAIL** (`AUD-C-007`) |
| 22 | `POST` | `/v1/devices/register` | `worker.ts:264` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-007`) |
| 23 | `GET` | `/me/devices` | `worker.ts:311` | No (Auto-Auth) | Real (SQLite `devices`), schema mismatch | `200` | **FAIL** (`AUD-C-007` omits `id`, `display_name`, `is_this_device` expected by C# `RegisteredDevice`) |
| 24 | `GET` | `/v1/me/devices` | `worker.ts:311` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-007`) |
| 25 | `GET` | `/devices` | `worker.ts:342` | No (Auto-Auth) | Real (SQLite `devices`), schema mismatch | `200` | **FAIL** (`AUD-C-007`) |
| 26 | `GET` | `/v1/devices` | `worker.ts:342` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-007`) |
| 27 | `GET` | `/onboarding/state` | `worker.ts:370` | No (Auto-Auth) | Real (SQLite `memory.onboarding_json`) | `200` | **FAIL** (`AUD-C-001`) |
| 28 | `GET` | `/v1/onboarding/state` | `worker.ts:370` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 29 | `POST` | `/onboarding/state` | `worker.ts:371` | No (Auto-Auth) | Real (SQLite `memory.onboarding_json`) | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 30 | `POST` | `/v1/onboarding/state` | `worker.ts:371` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 31 | `GET` | `/me/onboarding` | `worker.ts:404` | No (Auto-Auth) | Real | `200` | **FAIL** (`AUD-C-001`) |
| 32 | `GET` | `/v1/me/onboarding` | `worker.ts:404` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 33 | `POST` | `/me/onboarding` | `worker.ts:405` | No (Auto-Auth) | Real | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 34 | `POST` | `/v1/me/onboarding` | `worker.ts:405` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 35 | `POST` | `/onboarding/starter-plan` | `worker.ts:487` | No (Auto-Auth) | Real (SQLite), wrong DTO schema | `200` | **FAIL** (`AUD-C-008` `clickys` & `suggested_agents` mismatch C# `StarterPlanClicky`/`StarterPlanSuggestion`) |
| 36 | `POST` | `/v1/onboarding/starter-plan` | `worker.ts:487` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-008`) |
| 37 | `POST` | `/me/onboarding/starter-plan` | `worker.ts:495` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-008`) |
| 38 | `POST` | `/v1/me/onboarding/starter-plan` | `worker.ts:495` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-008`) |
| 39 | `POST` | `/onboarding/complete` | `worker.ts:539` | No (Auto-Auth) | Real (SQLite + hardcoded `app-config.json`) | `200` | **FAIL** (`AUD-C-009` hardcoded `C:\Users\ubaid\...` path at `worker.ts:8`) |
| 40 | `POST` | `/v1/onboarding/complete` | `worker.ts:539` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-009`) |
| 41 | `POST` | `/me/onboarding/complete` | `worker.ts:547` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-009`) |
| 42 | `POST` | `/v1/me/onboarding/complete` | `worker.ts:547` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-009`) |
| 43 | `GET` | `/search/web` | `worker.ts:819` | No (Auto-Auth) | Real (Brave/DDG HTML/Wikipedia) | `200`/`400` | **FAIL** (`AUD-C-014` fragile DDG regex & entity decode order) |
| 44 | `GET` | `/v1/search/web` | `worker.ts:819` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-014`) |
| 45 | `POST` | `/search/web` | `worker.ts:832` | No (Auto-Auth) | Real (Brave/DDG HTML/Wikipedia) | `200`/`400` | **FAIL** (`AUD-C-014`) |
| 46 | `POST` | `/v1/search/web` | `worker.ts:832` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-014`) |
| 47 | `GET` | `/search/images` | `worker.ts:851` | No (Auto-Auth) | Real (Wikimedia Commons API) | `200`/`400` | **FAIL** (`AUD-C-001` no auth enforced) |
| 48 | `GET` | `/v1/search/images` | `worker.ts:851` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-001`) |
| 49 | `POST` | `/search/images` | `worker.ts:864` | No (Auto-Auth) | Real (Wikimedia Commons API) | `200`/`400` | **FAIL** (`AUD-C-001`) |
| 50 | `POST` | `/v1/search/images` | `worker.ts:864` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-001`) |
| 51 | `GET` | `/memory` | `worker.ts:901` | No (Auto-Auth) | Real (SQLite `memory.markdown`) | `200` | **FAIL** (`AUD-C-001` cross-origin browser can read private user memory without auth) |
| 52 | `GET` | `/v1/memory` | `worker.ts:901` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 53 | `GET` | `/me/memory` | `worker.ts:902` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 54 | `GET` | `/v1/me/memory` | `worker.ts:902` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 55 | `PUT` | `/memory` | `worker.ts:961` | No (Auto-Auth) | Real (SQLite `memory.markdown`) | `200` | **FAIL** (`AUD-C-001` cross-origin memory overwrite; `AUD-C-015` no size limit) |
| 56 | `PUT` | `/v1/memory` | `worker.ts:961` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 57 | `POST` | `/memory` | `worker.ts:962` | No (Auto-Auth) | Real (SQLite `memory.markdown`) | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 58 | `POST` | `/v1/memory` | `worker.ts:962` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 59 | `PUT` | `/me/memory` | `worker.ts:964` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 60 | `PUT` | `/v1/me/memory` | `worker.ts:964` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 61 | `POST` | `/me/memory` | `worker.ts:965` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 62 | `POST` | `/v1/me/memory` | `worker.ts:965` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`, `AUD-C-015`) |
| 63 | `POST` | `/billing/checkout` | `worker.ts:982` | No (Auto-Auth) | Stub (returns dead local URL) | `200` | **FAIL** (`AUD-C-011` returns `http://127.0.0.1:8787/billing/local-max-active` -> `404`) |
| 64 | `POST` | `/v1/billing/checkout` | `worker.ts:982` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 65 | `POST` | `/me/billing/checkout` | `worker.ts:983` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 66 | `POST` | `/v1/me/billing/checkout` | `worker.ts:983` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 67 | `POST` | `/billing/portal` | `worker.ts:992` | No (Auto-Auth) | Stub (returns dead local URL) | `200` | **FAIL** (`AUD-C-011` returns `http://127.0.0.1:8787/billing/local-portal` -> `404`) |
| 68 | `POST` | `/v1/billing/portal` | `worker.ts:992` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 69 | `POST` | `/me/billing/portal` | `worker.ts:993` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 70 | `POST` | `/v1/me/billing/portal` | `worker.ts:993` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-011`) |
| 71 | `POST` | `/account/delete` | `worker.ts:1002` | No (Auto-Auth) | Pure No-Op Stub | `200` | **FAIL** (`AUD-C-012` returns `{deleted: true}` without deleting any SQLite rows) |
| 72 | `POST` | `/v1/account/delete` | `worker.ts:1002` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 73 | `POST` | `/me/account/delete` | `worker.ts:1003` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 74 | `POST` | `/v1/me/account/delete` | `worker.ts:1003` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 75 | `POST` | `/telemetry/client-error` | `worker.ts:1019` | No (Auto-Auth) | Pure No-Op Stub | `200` | **FAIL** (`AUD-C-012` discards `ErrorReport` without logging/storing; mismatches C# fields) |
| 76 | `POST` | `/v1/telemetry/client-error` | `worker.ts:1019` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 77 | `POST` | `/report-error` | `worker.ts:1020` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 78 | `POST` | `/v1/report-error` | `worker.ts:1020` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 79 | `GET` | `/referral/status` | `worker.ts:1032` | No (Auto-Auth) | Stub | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| 80 | `GET` | `/v1/referral/status` | `worker.ts:1032` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 81 | `GET` | `/me/referral` | `worker.ts:1033` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 82 | `GET` | `/v1/me/referral` | `worker.ts:1033` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 83 | `POST` | `/referral/claim` | `worker.ts:1042` | No (Auto-Auth) | Stub (matches `ReferralOutcomeResponse`) | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| 84 | `POST` | `/v1/referral/claim` | `worker.ts:1042` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 85 | `POST` | `/referrals/claim` | `worker.ts:1043` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 86 | `POST` | `/v1/referrals/claim` | `worker.ts:1043` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 87 | `GET` | `/me/companion-history` | `worker.ts:1045` | No (Auto-Auth) | Pure No-Op Stub (`items: []`) | `200` | **FAIL** (`AUD-C-012` never reads `threads` table) |
| 88 | `GET` | `/v1/me/companion-history` | `worker.ts:1045` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |
| 89 | `POST` | `/me/companion-history` | `worker.ts:1048` | No (Auto-Auth) | Pure No-Op Stub (`saved: true`) | `200` | **FAIL** (`AUD-C-012` never writes to `threads` table) |
| 90 | `POST` | `/v1/me/companion-history` | `worker.ts:1048` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-012`) |

### 2.4 Voice, Key-Minting, TTS & Dictation Routes (`src/backend/src/routes/voice.ts` — mounted at `/` and `/v1`)

| # | Method | Path | Location | Auth Enforced? | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | :---: | :---: | --- | :---: | --- |
| 91 | `POST` | `/deepgram/token` | `voice.ts:39` | No (Auto-Auth) | Returns raw `DEEPGRAM_API_KEY` | `200` | **FAIL** (`AUD-C-001` Critical: exposes raw master `DEEPGRAM_API_KEY` to any browser origin) |
| 92 | `POST` | `/v1/deepgram/token` | `voice.ts:39` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 93 | `POST` | `/v2/dictation/deepgram-token` | `voice.ts:40` | No (Auto-Auth) | Returns raw `DEEPGRAM_API_KEY` | `200` | **FAIL** (`AUD-C-001` Critical) |
| 94 | `POST` | `/v1/v2/dictation/deepgram-token` | `voice.ts:40` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 95 | `POST` | `/v2/dictation/gemini-token` | `voice.ts:89` | No (Auto-Auth) | Returns raw `GEMINI_API_KEY` | `200` | **FAIL** (`AUD-C-001` Critical: exposes raw master `GEMINI_API_KEY` to any browser origin) |
| 96 | `POST` | `/v1/v2/dictation/gemini-token` | `voice.ts:89` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 97 | `POST` | `/brain/ears/token` | `voice.ts:90` | No (Auto-Auth) | Returns raw `GEMINI_API_KEY` | `200` | **FAIL** (`AUD-C-001` Critical) |
| 98 | `POST` | `/v1/brain/ears/token` | `voice.ts:90` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 99 | `POST` | `/realtime/token` | `voice.ts:214` | No (Auto-Auth) | OpenAI Realtime mint + raw key fallback | `200` | **FAIL** (`AUD-C-001` Critical: falls back to returning raw `OPENAI_API_KEY`) |
| 100 | `POST` | `/v1/realtime/token` | `voice.ts:214` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 101 | `POST` | `/agent/realtime/session` | `voice.ts:215` | No (Auto-Auth) | Alias of `/realtime/token` | `200` | **FAIL** (`AUD-C-001` Critical) |
| 102 | `POST` | `/v1/agent/realtime/session` | `voice.ts:215` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 103 | `HEAD` | `/agent/realtime/warmup` | `voice.ts:218` | No (Auto-Auth) | Real (`204 No Content`) | `204` | **PASS** |
| 104 | `HEAD` | `/v1/agent/realtime/warmup` | `voice.ts:218` | No (Auto-Auth) | Alias | `204` | **PASS** |
| 105 | `GET` | `/agent/realtime/warmup` | `voice.ts:221` | No (Auto-Auth) | Real (`200 OK`) | `200` | **PASS** |
| 106 | `GET` | `/v1/agent/realtime/warmup` | `voice.ts:221` | No (Auto-Auth) | Alias | `200` | **PASS** |
| 107 | `POST` | `/brain/mouth/speak` | `voice.ts:555` | No (Auto-Auth) | Real (Gemini/OpenAI/Warm SAPI WAV) | `200`/`400` | **FAIL** (`AUD-C-006` spawns unreferenced `powershell.exe` child process that hangs exit) |
| 108 | `POST` | `/v1/brain/mouth/speak` | `voice.ts:555` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-006`) |
| 109 | `POST` | `/mouth/speak` | `voice.ts:556` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-006`) |
| 110 | `POST` | `/v1/mouth/speak` | `voice.ts:556` | No (Auto-Auth) | Alias | `200`/`400` | **FAIL** (`AUD-C-006`) |
| 111 | `GET` | `/voice/config` | `voice.ts:561` | No (Auto-Auth) | Real | `200` | **FAIL** (`AUD-C-006` `test-phase1.ts:155` fails when `MOUTH_PRIMARY_PROVIDER=gemini-tts`) |
| 112 | `GET` | `/v1/voice/config` | `voice.ts:561` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-006`) |
| 113 | `POST` | `/v2/dictation/polish` | `voice.ts:829` | No (Auto-Auth) | Real (Groq/OpenRouter/OpenAI/Gemini + regex fallback) | `200` | **FAIL** (`AUD-C-001` unauthenticated LLM usage) |
| 114 | `POST` | `/v1/v2/dictation/polish` | `voice.ts:829` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |
| 115 | `POST` | `/dictation/rewrite` | `voice.ts:830` | No (Auto-Auth) | Alias of `/v2/dictation/polish` | `200` | **FAIL** (`AUD-C-001`) |
| 116 | `POST` | `/v1/dictation/rewrite` | `voice.ts:830` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-001`) |

### 2.5 OneBrain Turn, SSE & Interrupt Routes (`src/backend/src/routes/brain.ts` — mounted at `/` and `/v1`)

| # | Method | Path | Location | Auth Enforced? | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | :---: | :---: | --- | :---: | --- |
| 117 | `POST` | `/brain/turn` | `brain.ts:1254` | No (Auto-Auth) | Real (LLM + ClearScript JS + SSE + SQLite) | `200` | **FAIL** (`AUD-C-005` calls live upstream LLMs before checking `force_local_fallback`; `AUD-C-010` DB bloat) |
| 118 | `POST` | `/v1/brain/turn` | `brain.ts:1254` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-005`, `AUD-C-010`) |
| 119 | `POST` | `/agent/realtime/turn` | `brain.ts:1255` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-005`, `AUD-C-010`) |
| 120 | `POST` | `/v1/agent/realtime/turn` | `brain.ts:1255` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-005`, `AUD-C-010`) |
| 121 | `POST` | `/brain/session-end` | `brain.ts:1300` | No (Auto-Auth) | Real (sets `expires_at = 0`, does not prune) | `200` | **FAIL** (`AUD-C-010` never deletes expired sessions) |
| 122 | `POST` | `/v1/brain/session-end` | `brain.ts:1300` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-010`) |
| 123 | `POST` | `/agent/realtime/interrupt` | `brain.ts:1301` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-010`) |
| 124 | `POST` | `/v1/agent/realtime/interrupt` | `brain.ts:1301` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-010`) |
| 125 | `POST` | `/agent/realtime/session-end` | `brain.ts:1302` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-010`) |
| 126 | `POST` | `/v1/agent/realtime/session-end` | `brain.ts:1302` | No (Auto-Auth) | Alias | `200` | **FAIL** (`AUD-C-010`) |

### 2.6 Proactive Jobs, Notifications, Turn Leases, Composio & Connector Routes (`src/backend/src/routes/agents.ts` — mounted at `/` and `/v1`)

*(Note: In [`docs/audit/enumeration.json`](file:///C:/dev/heyclicky-clone/docs/audit/enumeration.json), the 111 route definitions correspond directly to the route handler declarations across the 6 source files; because `index.ts` mounts `authRoutes`, `workerRoutes`, `voiceRoutes`, `brainRoutes`, and `agentsRoutes` at both `/` and `/v1`, all 111 source route declarations are covered 1:1 in [`docs/audit/ws-c/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-c/coverage.json).)*

| Source Line (`agents.ts`) | Method | Path | Implementation | HTTP Status | Audit Verdict & Findings |
| :---: | :---: | --- | --- | :---: | --- |
| `agents.ts:192`, `477`, `478` | `GET` | `/proactive/jobs`, `/me/proactive/jobs`, `/agent/proactive/jobs` | In-Memory Map (`proactiveJobs`) | `200` | **FAIL** (`AUD-C-013` volatile in-memory state lost on restart) |
| `agents.ts:195`, `482`, `483` | `POST` | `/proactive/jobs`, `/me/proactive/jobs`, `/agent/proactive/jobs` | In-Memory Map (`proactiveJobs`) | `201` | **FAIL** (`AUD-C-013` volatile in-memory state; `AUD-C-015` accepts invalid JSON with `201`) |
| `agents.ts:217`, `489`, `490` | `PUT` | `/proactive/jobs/:jobId`, `/me/proactive/jobs/:jobId`, `/agent/proactive/jobs/:jobId` | In-Memory Map (`proactiveJobs`) | `200`/`404` | **FAIL** (`AUD-C-013`) |
| `agents.ts:255`, `494`, `495` | `DELETE` | `/proactive/jobs/:jobId`, `/me/proactive/jobs/:jobId`, `/agent/proactive/jobs/:jobId` | In-Memory Map (`proactiveJobs`) | `200`/`404` | **FAIL** (`AUD-C-013`) |
| `agents.ts:273`, `499`, `500` | `POST` | `/proactive/jobs/:jobId/run`, `/me/proactive/jobs/:jobId/run`, `/agent/proactive/jobs/:jobId/run` | In-Memory + SQLite `notifications` | `200`/`404` | **FAIL** (`AUD-C-013`) |
| `agents.ts:520` | `GET` | `/proactive/cards`, `/me/proactive/cards` | In-Memory (`proactiveCardStatuses`) | `200` | **FAIL** (`AUD-C-013` ignores SQLite `proactive_cards` table) |
| `agents.ts:552`, `553` | `POST` | `/proactive/cards/:id/dismiss`, `/me/proactive/cards/:id/dismiss` | In-Memory (`proactiveCardStatuses`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:554`, `555` | `POST` | `/proactive/cards/:id/complete`, `/me/proactive/cards/:id/complete` | In-Memory (`proactiveCardStatuses`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:556`, `557` | `POST` | `/proactive/cards/:id/snooze`, `/me/proactive/cards/:id/snooze` | In-Memory (`proactiveCardStatuses`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:622`-`625` | `GET` | `/agent/notifications`, `/me/notifications`, `/notifications`, `/proactive/notifications` | Real (SQLite `notifications`) | `200` | **FAIL** (`AUD-C-010` missing index on `(user_id, read, created_at)`) |
| `agents.ts:678`-`686` | `POST` | `/agent/notifications/ack`, `/me/notifications/ack`, `/notifications/ack`, `/agent/notifications/:id/dismiss`, `/me/notifications/:id/dismiss` | Real (SQLite `notifications`) | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| `agents.ts:766`, `767` | `POST` | `/agent/turns/claim`, `/turns/claim` | In-Memory (`turnLeases`) | `200`/`409` | **FAIL** (`AUD-C-013` volatile in-memory map, never prunes expired leases) |
| `agents.ts:806`, `807` | `POST` | `/agent/turns/heartbeat`, `/turns/heartbeat` | In-Memory (`turnLeases`) | `200`/`404`/`409` | **FAIL** (`AUD-C-013`) |
| `agents.ts:843`-`846` | `POST` | `/agent/turns/complete`, `/turns/complete`, `/agent/turns/release`, `/turns/release` | In-Memory (`turnLeases`) | `200`/`404` | **FAIL** (`AUD-C-013`) |
| `agents.ts:865`-`867` | `GET` | `/agent/integrations`, `/me/integrations`, `/integrations` | In-Memory (`integrationsCatalog`) | `200` | **FAIL** (`AUD-C-013` volatile in-memory state) |
| `agents.ts:905`, `906` | `POST` | `/agent/integrations/:toolkit/connect`, `/me/integrations/:toolkit/connect` | In-Memory (`integrationsCatalog`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:926`, `927` | `POST`/`DELETE` | `/agent/integrations/:toolkit/disconnect`, `/agent/integrations/:toolkit/accounts/:connectionId` | In-Memory (`integrationsCatalog`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:929` | `POST` | `/agent/integrations/:toolkit/accounts/:connectionId/label` | Pure No-Op Stub | `200` | **FAIL** (`AUD-C-012` returns `{ok: true}` without updating `account.label`) |
| `agents.ts:950`, `951` | `POST` | `/agent/composio/session`, `/me/composio/session` | Stub returning dead `proxy_url` | `200` | **FAIL** (`AUD-C-011` returns `http://127.0.0.1:8787/mcp/composio` -> `404 Not Found`) |
| `agents.ts:953` | `GET` | `/agent/composio/connect-link` | Local / Composio API | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| `agents.ts:963` | `GET` | `/agent/composio/connect/callback` | HTML popup callback | `200` | **PASS** (escapes `toolkit` via `escapeHtml`) |
| `agents.ts:1051`-`1053` | `POST` | `/tools/gmail/draft`, `/agent/realtime/gmail/draft`, `/gmail/draft` | Composio API + Local Fallback | `200` | **FAIL** (`AUD-C-001` no auth enforced) |
| `agents.ts:1098`, `1099` | `POST` | `/calendar/create`, `/tools/calendar/create` | In-Memory (`calendarEvents`) | `200` | **FAIL** (`AUD-C-013` volatile in-memory array) |
| `agents.ts:1102`, `1109` | `GET`/`POST` | `/calendar/events`, `/tools/calendar/list` | In-Memory (`calendarEvents`) | `200` | **FAIL** (`AUD-C-013`) |
| `agents.ts:1137`, `1138` | `POST` | `/tools/execute-connector`, `/agent/tools/execute` | Pure Stub | `200` | **FAIL** (`AUD-C-012` returns `{status: "ok", executed: true}` without executing anything) |
| `agents.ts:1172`, `1173` | `POST` | `/tools/spotify/search`, `/agent/realtime/spotify/search` | Hardcoded Stub | `200` | **FAIL** (`AUD-C-012` always returns Daft Punk `4uLU6hMCjMI75M1A2tKUQC`) |

---

## 3. Adversarial Middleware & Security Analysis

### 3.1 Cross-Origin Browser Exfiltration (`cors()` + `clientHeadersAndAuthMiddleware` + Raw API Keys)
- **Code Path:**
  - [`src/backend/src/index.ts:19`](file:///C:/dev/heyclicky-clone/src/backend/src/index.ts#L19): `app.use("*", cors());`
  - [`src/backend/src/middleware/auth.ts:34-88`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/auth.ts#L34-L88): `clientHeadersAndAuthMiddleware`
  - [`src/backend/src/routes/voice.ts:18-31`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L18-L31), [`53-83`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L53-L83), [`169-211`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L169-L211)
- **Live Test Verification:**
  - `OPTIONS /deepgram/token` with `Origin: https://untrusted-webpage.example` -> `204 No Content`, `Access-Control-Allow-Origin: *`.
  - `POST /deepgram/token` with `Origin: https://untrusted-webpage.example` and **no `Authorization` header** -> `200 OK`, `Access-Control-Allow-Origin: *`, body contains `{ key: process.env.DEEPGRAM_API_KEY, token: process.env.DEEPGRAM_API_KEY }`.
  - `POST /v2/dictation/gemini-token` with `Origin: https://untrusted-webpage.example` and **no `Authorization` header** -> `200 OK`, `Access-Control-Allow-Origin: *`, body contains `{ api_key: process.env.GEMINI_API_KEY, token: process.env.GEMINI_API_KEY }`.
  - `POST /realtime/token` with `Origin: https://untrusted-webpage.example` and **no `Authorization` header** -> `200 OK`, `Access-Control-Allow-Origin: *`, falls back at `voice.ts:173/197` to returning `{ value: process.env.OPENAI_API_KEY, client_secret: { value: process.env.OPENAI_API_KEY } }`.
  - `GET /memory` and `POST /memory` with `Origin: https://untrusted-webpage.example` -> `200 OK`, `Access-Control-Allow-Origin: *`, allowing full cross-origin read/write of OneBrain user memory.

### 3.2 `gzipMiddleware` (`< 8 KB`, `>= 8 KB`, Corrupt Payload, Decompression Magnification)
- **Code Path:** [`src/backend/src/middleware/gzip.ts:10-52`](file:///C:/dev/heyclicky-clone/src/backend/src/middleware/gzip.ts#L10-L52)
- **Live Test Verification:**
  - `< 8 KB` gzipped JSON (`69 bytes` raw -> `89 bytes` gzipped): Decompressed and processed (`200 OK`).
  - `>= 8 KB` gzipped JSON (`12,048 bytes` raw -> `156 bytes` gzipped): Decompressed and processed (`200 OK`).
  - Corrupt gzip stream (`"not-a-valid-gzip-stream-at-all"`): Caught and rejected with `400 Bad Request` (`{ error: "invalid_gzip_payload" }`).
  - **Decompression Magnification (Gzip Bomb Vector):** A `15,331-byte` (`15 KB`) compressed payload expanded synchronously via `gunzipSync` to `15,728,652 bytes` (`15.7 MB`, **1,025.9x expansion ratio**) on the main thread in `942 ms` (`status: 200`, `hasMaxOutputLengthLimit: false`). Because `gunzipSync(compressedBuffer)` is called without `{ maxOutputLength }`, a ~500 KB–1 MB gzip bomb will expand to ~500 MB–1 GB+ and block/OOM the server.

### 3.3 Auth, Open Redirect, XSS & JWT Verification
- **`GET /auth/v1/authorize` Open Redirect (`AUD-C-003`):** Sending `GET /auth/v1/authorize?redirect_to=https%3A%2F%2Fexternal-attacker.example%2Fcapture` returns `302 Found` with `Location: https://external-attacker.example/capture?code=...&access_token=eyJ...&refresh_token=ref_...&token_type=bearer&expires_in=31536000`.
- **`GET /auth/start` HTML Escaping vs URL Scheme (`AUD-C-003`):** `escapeHtml()` (`auth.ts:17-24`) properly escapes `<script>` tags in `state` (`authStartHtmlEscapesScriptTag: true`), preventing raw tag injection, but `buildCallbackUrl` (`auth.ts:48-54`) does not validate the protocol/host of `redirect_to`.
- **Static JWT Signature &Broken Logout (`AUD-C-004`):** Base64url-decoding the 3rd segment of tokens minted by `/auth/v1/token` yields the literal ASCII string `"kursor-local-dev-signature"`. Calling `POST /auth/v1/logout` deletes the row from SQLite `sessions`, but subsequent requests with the revoked token (or with `.COMPLETELY_UNSIGNED_FORGED_SIG`) still return `200 OK` on `/me/plan`.

---

## 4. SQLite Database Audit (`src/backend/src/db/index.ts`)

Full database inspection evidence is saved in [`docs/audit/evidence/ws-c/sqlite-db-inspection.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/sqlite-db-inspection.txt) and [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json).

1. **Journal & Pragmas:** `PRAGMA journal_mode = WAL` and `PRAGMA foreign_keys = ON` (`1`) are enabled at connection init (`db/index.ts:24-25`).
2. **Parameter Binding:** All SQL queries in `auth.ts`, `worker.ts`, `brain.ts`, and `agents.ts` use parameterized `db.prepare(...).get/all/run(...)` statements with `?` placeholders (no string-concatenated SQL injection vectors found).
3. **Missing Secondary Indexes (`AUD-C-010`):** Zero explicit `CREATE INDEX` statements exist in `db/index.ts`.
   - `EXPLAIN QUERY PLAN DELETE FROM sessions WHERE access_token = ?` -> `SCAN sessions` (full table scan).
   - `EXPLAIN QUERY PLAN SELECT * FROM notifications WHERE user_id = ? AND read = 0 ORDER BY created_at DESC LIMIT 50` -> `SCAN notifications` + `USE TEMP B-TREE FOR ORDER BY`.
4. **Unbounded `sessions` Table Growth (`AUD-C-010`):** Every `POST /auth/v1/token` and every `POST /brain/turn` with a new `session_id` inserts a row into `sessions`. `POST /brain/session-end` only updates `expires_at = 0` and never deletes expired rows.
5. **Base64 Screenshot Bloat in `threads.messages_json` (`AUD-C-010`):** `ensureSessionAndThread` (`brain.ts:172-187`) stores full base64 JPEG screenshots inside `threads.messages_json`, causing just 9 thread rows to occupy **4.0 MB** in `kursor.db` + **5.8 MB** in `kursor.db-wal`.

---

## 5. Evaluation of Existing Test Suites vs. New Audit Suite

| Test Suite | Location | Cases | Execution Result | Key Deficiencies Found |
| --- | --- | :---: | --- | --- |
| **Phase 1 Test Suite** | [`src/backend/src/test-phase1.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase1.ts) | 18 | **FAIL** (Exits code 1 at line 155) | Hardcodes `assert.equal(data.mouth.primary_provider, "openai-realtime")` at line 155, which fails when `.env` sets `MOUTH_PRIMARY_PROVIDER=gemini-tts` (`AUD-C-006`). Does not test negative/adversarial inputs, CORS, auth revocation, or C# DTO field contracts. |
| **Phase 2 Test Suite** | [`src/backend/src/test-phase2.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/test-phase2.ts) | 7 | **FAIL** (Passes 7 assertions, then **hangs forever**) | 1. Hangs indefinitely after `server.close()` because `voice.ts:316` spawns a persistent `powershell.exe` child process without `.unref()` (`AUD-C-006`).<br>2. Makes live external API calls to Groq (`qwen/qwen3.8-27b` -> 404, `openai/gpt-oss-120b` -> 200) even when `force_local_fallback: true` is passed (`AUD-C-005`). |
| **WS-C Audit Suite (New)** | [`tests/audit/ws-c/test-backend-audit.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-c/test-backend-audit.ts) | 8 Suites (111 Routes) | **PASS** (111/111 routes exercised) | Exercises all 111 enumerated routes, CORS preflight & cross-origin key exposure, Open Redirect, JWT signature & logout, `<8KB`/`>=8KB`/corrupt/magnified gzip, C# DTO contracts, SQLite query plans, and 120-request concurrent load + 10 SSE mid-stream aborts. |

---

## 6. Load, Concurrency & SSE Mid-Stream Disconnect Soak Benchmarks

Full metrics are recorded in [`docs/audit/evidence/ws-c/load-soak-metrics.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/load-soak-metrics.json).

- **Workload:** 120 concurrent HTTP requests distributed across `GET /health`, `GET /me/plan`, `GET /memory`, `GET /agent/notifications`, `GET /proactive/jobs`, `POST /v2/dictation/polish`, `POST /brain/turn` (with ClearScript/local fallback), and `POST /agent/turns/claim`, followed immediately by **10 streaming `POST /brain/turn` SSE requests aborted mid-stream** via `AbortController.abort()` after reading the first chunk.
- **Results:**
  - **Concurrent Requests:** `120` (`120` succeeded, `0` 5xx errors)
  - **SSE Client Mid-Stream Aborts:** `10 / 10` handled cleanly (`serverHealthyAfterAborts: true`, `GET /health` returned `200 OK`)
  - **Latency Distribution (`120` concurrent requests):**
    - `min`: ~`23 ms`
    - `p50`: ~`180 ms`
    - `p95`: ~`390 ms`
    - `p99`: ~`430 ms`
  - **Memory Usage (`process.memoryUsage()`):**
    - `rssDeltaMB` and `heapUsedDeltaMB` remained bounded (< 35 MB delta across the entire 111-route + 15 MB gzip magnification + 120-request load test).

---

## 7. Detailed Findings (§4 Schema)

### `ID: AUD-C-001`
- **Severity:** Critical
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Wildcard CORS + Auto-Authentication Middleware + Raw Master API Key Endpoints Enable Cross-Origin Browser Exfiltration of Cloud API Keys and User Memory
- **Component:** Backend CORS / Auth Middleware / Voice Key-Minting & Memory Routes
- **Location:** `src/backend/src/index.ts:19`, `src/backend/src/middleware/auth.ts:34-88`, `src/backend/src/routes/voice.ts:18-31`, `src/backend/src/routes/voice.ts:53-83`, `src/backend/src/routes/voice.ts:169-211`, `src/backend/src/routes/worker.ts:887-965`
- **Description:** The backend mounts Hono's default `cors()` middleware globally (`app.use("*", cors())` at `index.ts:19`), which responds with `Access-Control-Allow-Origin: *` to all origins and preflight requests. Simultaneously, `clientHeadersAndAuthMiddleware` (`middleware/auth.ts:34-88`) unconditionally authenticates every incoming HTTP request as `DEFAULT_USER_ID` (`00000000-0000-0000-0000-000000000001`), even when no `Authorization` header is present. Furthermore, key-minting routes (`POST /deepgram/token`, `POST /v2/dictation/deepgram-token`, `POST /v2/dictation/gemini-token`, `POST /brain/ears/token`, `POST /realtime/token`, `POST /agent/realtime/session`) return the raw master environment keys (`process.env.DEEPGRAM_API_KEY`, `process.env.GEMINI_API_KEY`, and `process.env.OPENAI_API_KEY` on fallback) directly in JSON responses.
- **Reproduction Steps:**
  1. Start the backend server on `127.0.0.1:8787`.
  2. Send an unauthenticated cross-origin POST request with header `Origin: https://untrusted-webpage.example` to `/deepgram/token`, `/v2/dictation/gemini-token`, `/realtime/token`, and `GET /memory`.
  3. Inspect the `Access-Control-Allow-Origin` response header and JSON response payloads.
- **Observed Result:** All four endpoints return HTTP 200 with `Access-Control-Allow-Origin: *` (and `OPTIONS` preflight returns 204 with `Access-Control-Allow-Origin: *`). `/deepgram/token` returns the raw `DEEPGRAM_API_KEY` in `key`/`token`; `/v2/dictation/gemini-token` returns the raw `GEMINI_API_KEY` in `api_key`/`token`; `/realtime/token` returns the raw `OPENAI_API_KEY` in `value` and `client_secret.value` when OpenAI ephemeral minting fails; `/memory` returns the user's complete OneBrain markdown memory.
- **Expected Result:** Local loopback services must reject cross-origin browser requests (`Origin` header allowlist restricted to trusted app origins or no CORS headers at all), require a valid cryptographic bearer token on every non-public endpoint, and never return raw master API keys when ephemeral key minting fails.
- **Impact:** Any malicious or compromised website visited in the user's browser while Kursor is running can silently issue `fetch("http://127.0.0.1:8787/...")` calls to steal the user's master Deepgram, Gemini, and OpenAI API keys, read private OneBrain memories (`GET /memory`), or inject malicious instructions into persistent memory (`POST /memory`).
- **Remediation:**
  1. Remove wildcard `cors()` in `src/backend/src/index.ts:19` or explicitly reject requests bearing browser `Origin` or `Sec-Fetch-Site: cross-site` headers.
  2. Enforce real bearer token verification in `src/backend/src/middleware/auth.ts` for all routes except `/health` and `/auth/*`.
  3. In `src/backend/src/routes/voice.ts`, mint scoped short-lived tokens and return HTTP 502 instead of falling back to raw master API keys.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-002`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unbounded Synchronous `gunzipSync` in `gzipMiddleware` Enables Decompression Magnification DoS / OOM
- **Component:** Backend Gzip Decompression Middleware
- **Location:** `src/backend/src/middleware/gzip.ts:15-21`
- **Description:** `gzipMiddleware` buffers the entire compressed request body (`await c.req.raw.arrayBuffer()`) without checking compressed byte length, and then synchronously decompresses it on the main Node.js event loop via `gunzipSync(compressedBuffer)` without passing a `maxOutputLength` option.
- **Reproduction Steps:**
  1. Compress a 15 MB repetitive JSON payload with `gzipSync` (~15 KB compressed, >1,000x expansion ratio).
  2. POST the compressed payload with `Content-Encoding: gzip` to `/brain/session-end`.
  3. Observe that `gunzipSync` expands the entire 15.7 MB buffer synchronously in memory without any limit.
- **Observed Result:** A `15,331-byte` gzipped payload expands synchronously to `15,728,652 bytes` (**1,025.9x magnification ratio**) in `942 ms` with HTTP 200 (`hasMaxOutputLengthLimit: false`). Larger gzip bombs (e.g., 1 MB -> 1 GB) block the main thread and exhaust V8/Buffer memory.
- **Expected Result:** `gzipMiddleware` must enforce both a maximum compressed payload size (e.g., 10 MB) and a `maxOutputLength` option in `gunzipSync(compressedBuffer, { maxOutputLength: 25 * 1024 * 1024 })`, returning `413 Payload Too Large` when exceeded.
- **Impact:** A small crafted `Content-Encoding: gzip` request can freeze the Node.js event loop and crash the backend process via out-of-memory (OOM).
- **Remediation:** Check `rawBuffer.byteLength <= MAX_COMPRESSED_BYTES` before decompression and pass `{ maxOutputLength: MAX_UNCOMPRESSED_BYTES }` to `gunzipSync`, returning HTTP 413 when exceeded.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-003`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Unvalidated Open Redirect with Minted Access Token on `GET /auth/v1/authorize` and Unvalidated `redirect_to` Host in `GET /auth/start`
- **Component:** Backend Auth Routes
- **Location:** `src/backend/src/routes/auth.ts:48-121`
- **Description:** `GET /auth/v1/authorize` reads `redirect_to` or `redirect_uri` from query parameters (`auth.ts:101-103`) without validating the destination scheme or host, immediately mints a 1-year JWT (`createLocalJwt()`), appends `access_token` and `refresh_token` to the URL query string, and responds with HTTP 302 to the unvalidated URL.
- **Reproduction Steps:**
  1. Request `GET /auth/v1/authorize?redirect_to=https%3A%2F%2Fexternal-attacker.example%2Fcapture` with `redirect: "manual"`.
  2. Inspect the HTTP status code and `Location` header.
- **Observed Result:** `GET /auth/v1/authorize` returns HTTP 302 with `Location: https://external-attacker.example/capture?code=...&access_token=eyJ...&refresh_token=ref_...&token_type=bearer&expires_in=31536000`.
- **Expected Result:** `redirect_to` / `redirect_uri` must be strictly validated against an allowlist of local loopback callback URLs (`http://127.0.0.1:<port>/...`, `http://localhost:<port>/...`) or the registered custom URI scheme (`heyclicky://` / `kursor://`).
- **Impact:** Open-redirect links pointing to `http://127.0.0.1:8787/auth/v1/authorize?redirect_to=https://evil.example` automatically mint a session token and redirect the user's browser to an arbitrary external domain with the token in the URL.
- **Remediation:** Parse `redirect_to` with `new URL()` and reject any URL whose protocol/hostname is not `http://127.0.0.1`, `http://localhost`, `heyclicky:`, or `kursor:` with HTTP 400 `invalid_redirect_uri`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-004`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Fake Static JWT Signature (`"kursor-local-dev-signature"`), Zero Token Verification, and Ineffective Session Logout
- **Component:** Backend Auth & Session Management
- **Location:** `src/backend/src/routes/auth.ts:27-39`, `src/backend/src/routes/auth.ts:236-248`, `src/backend/src/middleware/auth.ts:34-88`
- **Description:** `createLocalJwt()` (`auth.ts:27-39`) creates a JWT with header `{"alg":"HS256","typ":"JWT"}` and a 1-year expiration (`31536000` seconds), but instead of computing an HMAC-SHA256 signature, it hardcodes `base64UrlEncode("kursor-local-dev-signature")` as the signature segment. Furthermore, `clientHeadersAndAuthMiddleware` (`middleware/auth.ts:34-88`) never verifies the JWT signature, never checks `exp`, and never checks whether the token exists in the SQLite `sessions` table. Consequently, `POST /auth/v1/logout` (`auth.ts:236-248`) deletes the row from `sessions`, but the logged-out token (and any forged token) remains 100% valid on all routes.
- **Reproduction Steps:**
  1. Mint a token via `POST /auth/v1/token?grant_type=password` and base64url-decode its third JWT segment.
  2. Revoke the token via `POST /auth/v1/logout` with `Authorization: Bearer <token>`.
  3. Call `GET /me/plan` using the revoked token and using a token with signature `.COMPLETELY_UNSIGNED_FORGED_SIG`.
- **Observed Result:** The decoded JWT signature is literally the ASCII string `kursor-local-dev-signature`. After `POST /auth/v1/logout` returns 200, both the revoked token and the forged-signature JWT still return HTTP 200 on `/me/plan`.
- **Expected Result:** JWTs must be signed with a per-installation cryptographic secret using HMAC-SHA256 (`node:crypto.createHmac`), and `clientHeadersAndAuthMiddleware` must verify both the signature/expiration and active session state in SQLite.
- **Impact:** Session revocation (`POST /auth/v1/logout`) has no security effect, and any arbitrary string passed as a Bearer token is accepted as authenticated.
- **Remediation:** Generate and persist a 256-bit random HMAC secret on first boot, sign JWTs with `crypto.createHmac("sha256", secret)`, and validate the signature, `exp`, and `sessions` table lookup in `clientHeadersAndAuthMiddleware`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-005`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `POST /brain/turn` Unconditionally Calls Live Upstream LLMs Before Checking `force_local_fallback`
- **Component:** OneBrain Turn Orchestrator
- **Location:** `src/backend/src/routes/brain.ts:1021-1045`
- **Description:** In `POST /brain/turn` (`src/backend/src/routes/brain.ts:1021-1027`), `const upstream = await tryCallUpstreamLlm(...)` is awaited unconditionally before checking `if (upstream && !body.force_local_fallback)` on line 1045. When `force_local_fallback: true` is sent in the request body (such as in `test-phase2.ts` or offline fallback requests) while API keys exist in `.env`, the server still executes live network requests to Groq (`qwen/qwen3.8-27b`, `openai/gpt-oss-120b`), OpenRouter, OpenAI, or Gemini, waits for the full completion, and then discards the upstream response at line 1045.
- **Reproduction Steps:**
  1. Run `npx tsx src/test-phase2.ts` in `src/backend` with `.env` configured.
  2. Observe stderr and latency during Test 1 (`force_local_fallback: true`).
- **Observed Result:** `test-phase2.ts` logs `[Brain] Groq primary failed (404): ...model 'qwen/qwen3.8-27b' does not exist...` and calls Groq `openai/gpt-oss-120b` over the network, taking ~3–4 seconds per fallback turn before discarding the result.
- **Expected Result:** `tryCallUpstreamLlm` must only be called when `!body.force_local_fallback` is true (`const upstream = body.force_local_fallback ? null : await tryCallUpstreamLlm(...)`). Also, the invalid default model ID `qwen/qwen3.8-27b` at `brain.ts:830` should be updated to a valid Groq model ID.
- **Impact:** Unnecessary external API billing/quota burn, 3–5 second latency penalty on local fallback turns, and leaking test/offline prompts to external LLM providers even when `force_local_fallback: true` is explicitly set.
- **Remediation:** Change `src/backend/src/routes/brain.ts:1021` to `const upstream = body.force_local_fallback ? null : await tryCallUpstreamLlm(mode, systemPrompt, userText, screenshotBase64, incomingThread, body.tools);`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/test-phase2-stderr.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/test-phase2-stderr.txt), [`docs/audit/evidence/ws-c/test-phase2-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/test-phase2-output.txt)

---

### `ID: AUD-C-006`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Existing Backend Test Suite (`npm test`) Fails on `test-phase1.ts:155` and Hangs Indefinitely Due to Unreferenced PowerShell TTS Child Process
- **Component:** Backend Test Suites & Voice Warm SAPI Worker
- **Location:** `src/backend/src/test-phase1.ts:155`, `src/backend/src/routes/voice.ts:263-320`
- **Description:** Two separate defects break the existing backend test suite (`npm test` / `test-phase1.ts` and `test-phase2.ts`): (1) `test-phase1.ts:155` hardcodes `assert.equal(data.mouth.primary_provider, "openai-realtime")`, which fails whenever `.env` sets `MOUTH_PRIMARY_PROVIDER=gemini-tts` (`voice.ts:9` reads `process.env.MOUTH_PRIMARY_PROVIDER`). (2) Importing `src/routes/voice.ts` executes `ensureWarmSynthWorker()` at module load time (`voice.ts:314-320`), spawning a persistent `powershell.exe` child process (`warmSynthProc`) attached to a `readline.createInterface` on `stdout` without calling `.unref()`. When `server.close()` is called at the end of `test-phase1.ts` or `test-phase2.ts`, the open child process pipes keep the Node.js event loop alive indefinitely.
- **Reproduction Steps:**
  1. Run `npm test` in `C:\dev\heyclicky-clone\src\backend` and observe `AssertionError` at `src/test-phase1.ts:155`.
  2. Run `npx tsx src/test-phase2.ts` in `C:\dev\heyclicky-clone\src\backend` and observe that after printing `ALL 7 PHASE 2 & PHASE 6 TEST SUITES PASSED SUCCESSFULLY!`, the process never terminates.
- **Observed Result:** `test-phase1.ts` fails at Test 6 (`Expected values to be strictly equal: + 'gemini-tts' - 'openai-realtime'`), and `test-phase2.ts` hangs forever after completing all assertions (`Process exited within 6000ms: False`).
- **Expected Result:** `test-phase1.ts` should assert against `process.env.MOUTH_PRIMARY_PROVIDER || "openai-realtime"`, and `voice.ts` should call `warmSynthProc.unref()` (and `warmSynthProc.stdin/stdout.unref()`) or spawn the worker lazily.
- **Impact:** CI/CD and local `npm test` runs either fail immediately or hang indefinitely in zombie `node.exe` + `powershell.exe` processes.
- **Remediation:** In `src/backend/src/routes/voice.ts:263-310`, call `warmSynthProc.unref()`, `warmSynthProc.stdin?.unref?.()`, and `warmSynthProc.stdout?.unref?.()`. In `src/backend/src/test-phase1.ts:155`, assert against `process.env.MOUTH_PRIMARY_PROVIDER || "openai-realtime"`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/existing-tests-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/existing-tests-output.txt), [`docs/audit/evidence/ws-c/test-phase2-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/test-phase2-output.txt)

---

### `ID: AUD-C-007`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** C# Desktop Client Header Names and Device Registration DTOs (`POST/GET /me/devices`) Mismatch Backend Implementation
- **Component:** Backend Auth Middleware & Worker Device Routes vs `Kursor.Core`
- **Location:** `src/backend/src/middleware/auth.ts:44-60`, `src/backend/src/routes/worker.ts:209-340`, `src/desktop/Kursor.Core/HeyClicky.Core.Net/ClickyClientHeadersHandler.cs:54-58`, `src/desktop/Kursor.Core/HeyClicky.Core.Worker/RegisteredDevice.cs:1-51`
- **Description:** There is a two-layer contract mismatch between the C# desktop client (`Kursor.Core`) and the backend worker: (1) `ClickyClientHeadersHandler.cs:54-58` sends HTTP headers `X-Clicky-Platform`, `X-Clicky-Device`, `X-Clicky-App-Version`, `X-Clicky-App-Build`, and `X-Clicky-OS-Version`. However, `src/backend/src/middleware/auth.ts:44-60` reads `x-clicky-client-platform`, `x-clicky-client-version`, `x-clicky-device-id`, and `x-clicky-device-name` — completely ignoring `X-Clicky-Platform`, `X-Clicky-Device`, and `X-Clicky-App-Version` and falling back to hardcoded `"local-win-device-01"`. (2) `WorkerClient.RegisterDeviceAsync` (`WorkerClient.cs:61`) sends `DeviceRegistrationRequest` (`{ capabilities, display_name, model }`) and deserializes `DeviceResponse.Device` as `RegisteredDevice` expecting JSON fields `id`, `is_this_device`, `display_name`, `os_version`, `app_build`, `capabilities`, `first_seen_at`, `last_seen_at`. However, `worker.ts:209-340` reads `body.device_name` (ignoring `display_name`) and returns `{ device_id, device_name, platform, app_version, is_current }`.
- **Reproduction Steps:**
  1. Send `POST /me/devices` with C# client headers (`X-Clicky-Device: CSHARP-DEVICE-GUID-999`, `X-Clicky-Platform: windows`, `X-Clicky-App-Version: 1.1.5`) and C# body `{"capabilities":["screen","voice"],"display_name":"CSharp-Desktop-Host","model":"Surface-Studio"}`.
  2. Inspect the returned `device` object in the JSON response.
- **Observed Result:** The backend ignores `X-Clicky-Device` and `display_name`, recording `device_id: "local-win-device-01"` and `device_name: "KURSOR-PC"`. The response object has `hasCSharpRequiredIdField: false`, `hasCSharpRequiredDisplayNameField: false`, `hasCSharpRequiredIsThisDeviceField: false`, and `hasCSharpRequiredCapabilitiesField: false`.
- **Expected Result:** `middleware/auth.ts` must recognize `x-clicky-platform`, `x-clicky-device`, `x-clicky-app-version`, `x-clicky-app-build`, and `x-clicky-os-version`. `POST /me/devices` and `GET /me/devices` must accept `display_name`, `capabilities`, `model` and return `id`, `is_this_device`, `display_name`, `model`, `os_version`, `app_version`, `app_build`, `capabilities` alongside legacy fields.
- **Impact:** When the C# desktop app calls `WorkerClient.RegisterDeviceAsync` or `ListDevicesAsync`, the deserialized `RegisteredDevice` has `Id = null`, `DisplayName = null`, `IsThisDevice = false`, and `Capabilities = null`.
- **Remediation:** Update `src/backend/src/middleware/auth.ts:44-60` to read `x-clicky-device`, `x-clicky-platform`, and `x-clicky-app-version`, and update `src/backend/src/routes/worker.ts:209-340` to read `body.display_name` and include `id`, `display_name`, `is_this_device`, `model`, `os_version`, `app_build`, and `capabilities` in the response.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-008`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `POST /onboarding/starter-plan` Response Schema Mismatches C# `StarterPlanClicky` and `StarterPlanSuggestion` DTOs
- **Component:** Backend Onboarding Starter Plan Route vs `Kursor.Core`
- **Location:** `src/backend/src/routes/worker.ts:407-501`, `src/desktop/Kursor.Core/HeyClicky.Core.Worker/StarterPlanClicky.cs:1-36`, `src/desktop/Kursor.Core/HeyClicky.Core.Worker/StarterPlanSuggestion.cs:1-36`
- **Description:** The C# desktop client (`CoreJsonContext` / `StarterPlanResponse.cs`) deserializes `clickys` items as `StarterPlanClicky` (requiring `key`, `name`, `role`, `description`, `intro_messages`, `reveal_line`, `first_asks`, `routine`) and `suggested_agents` items as `StarterPlanSuggestion` (requiring `title`, `description`, `agent_prompt`, `deliverable`, `clicky_slug`, `clicky_key`, `routine`, `suggestion_id`). However, `src/backend/src/routes/worker.ts:420-456` returns `{ title, prompt, why }` for `clickys` and `{ id, title, tagline, initial_prompt, schedule_hint }` for `suggested_agents`.
- **Reproduction Steps:**
  1. Send `POST /onboarding/starter-plan` with `{"role":"Founder","goals":["Ship fast"],"tools":["Linear"]}`.
  2. Compare the keys in `clickys[0]` and `suggested_agents[0]` against `StarterPlanClicky.cs` and `StarterPlanSuggestion.cs`.
- **Observed Result:** `clickys[0]` contains only `["title", "prompt", "why"]` (`clickyHasCSharpRequiredFields: false`), and `suggested_agents[0]` contains only `["id", "title", "tagline", "initial_prompt", "schedule_hint"]` (`suggestionHasCSharpRequiredFields: false`).
- **Expected Result:** `POST /onboarding/starter-plan` must include all fields expected by `StarterPlanClicky` (`key`, `name`, `role`, `description`, `intro_messages`, `reveal_line`, `first_asks`, `routine`) and `StarterPlanSuggestion` (`title`, `description`, `agent_prompt`, `deliverable`, `clicky_slug`, `clicky_key`, `routine`, `suggestion_id`).
- **Impact:** During desktop onboarding, `StarterPlanClicky` items deserialize with blank names, descriptions, and intro messages, and `StarterPlanSuggestion` items deserialize with null `AgentPrompt` and `Description`.
- **Remediation:** Include the full C# DTO property sets in `buildStarterPlanResponse` in `src/backend/src/routes/worker.ts:407-485`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-009`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Hardcoded Developer Machine Path `C:\Users\ubaid\AppData\Local\HeyClicky\app-config.json` in `worker.ts`
- **Component:** Backend Worker Config Sync
- **Location:** `src/backend/src/routes/worker.ts:7-38`
- **Description:** `src/backend/src/routes/worker.ts:8` hardcodes `const LOCAL_APP_CONFIG_PATH = "C:\\Users\\ubaid\\AppData\\Local\\HeyClicky\\app-config.json";` and writes user onboarding preferences to that path whenever `syncOnboardingToLocalAppConfig()` is called by `POST /onboarding/complete` (`worker.ts:520`).
- **Reproduction Steps:**
  1. Inspect `src/backend/src/routes/worker.ts` lines 7–38.
  2. Call `POST /onboarding/complete` on any machine where the Windows username is not `ubaid`.
- **Observed Result:** `LOCAL_APP_CONFIG_PATH` is hardcoded to `C:\Users\ubaid\AppData\Local\HeyClicky\app-config.json`. On any other user profile or OS, `fs.existsSync(LOCAL_APP_CONFIG_PATH)` returns false and onboarding state is silently not synced to `app-config.json`.
- **Expected Result:** Resolve the path dynamically via `process.env.HEYCLICKY_APP_CONFIG_PATH || path.join(process.env.LOCALAPPDATA || path.join(os.homedir(), "AppData", "Local"), "HeyClicky", "app-config.json")`.
- **Impact:** Breaks local `app-config.json` synchronization on every Windows user account other than `ubaid`.
- **Remediation:** Replace the hardcoded string at `src/backend/src/routes/worker.ts:8` with dynamic path resolution using `process.env.LOCALAPPDATA` and `os.homedir()`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json)

---

### `ID: AUD-C-010`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** SQLite Schema Lacks Secondary Indexes, Never Prunes Expired Sessions, and Bloats `threads.messages_json` With Raw Base64 Screenshots
- **Component:** SQLite Persistence Layer (`db/index.ts` & `brain.ts`)
- **Location:** `src/backend/src/db/index.ts:30-114`, `src/backend/src/routes/auth.ts:198-207`, `src/backend/src/routes/brain.ts:155-189`
- **Description:** Inspection of `src/backend/src/db/index.ts` and `data/kursor.db` reveals three database scalability defects: (1) Zero secondary indexes (`CREATE INDEX`) exist on any of the 7 tables (`users`, `devices`, `memory`, `sessions`, `threads`, `notifications`, `proactive_cards`); `EXPLAIN QUERY PLAN` confirms full table scans (`SCAN sessions`, `SCAN notifications`) on `DELETE FROM sessions WHERE access_token = ?` and `SELECT * FROM notifications WHERE user_id = ? AND read = 0 ORDER BY created_at DESC LIMIT 50`. (2) Every `POST /auth/v1/token` and every `POST /brain/turn` with a new `session_id` inserts a row into `sessions` that is never deleted (`POST /brain/session-end` at `brain.ts:1284` only sets `expires_at = 0`). (3) `ensureSessionAndThread` (`brain.ts:172-187`) serializes the full `incomingThread` array—including raw base64-encoded screenshot images—into `threads.messages_json`, causing just 9 thread rows to bloat `kursor.db` to 4.0 MB and `kursor.db-wal` to 5.8 MB (~9.8 MB total).
- **Reproduction Steps:**
  1. Run `PRAGMA index_list(<table>)` across all 7 tables in `kursor.db`.
  2. Run `EXPLAIN QUERY PLAN DELETE FROM sessions WHERE access_token = ?` and `EXPLAIN QUERY PLAN SELECT * FROM notifications WHERE user_id = ? AND read = 0 ORDER BY created_at DESC LIMIT 50`.
  3. Check `sessions` row count before and after `POST /brain/turn` and `POST /brain/session-end`.
- **Observed Result:** Only SQLite autoindexes for `PRIMARY KEY`/`UNIQUE` exist; `EXPLAIN QUERY PLAN` outputs `SCAN sessions` and `SCAN notifications` + `USE TEMP B-TREE FOR ORDER BY`. `sessions` grows monotonically on every new turn/auth request and is never pruned.
- **Expected Result:** Add secondary indexes on `sessions(access_token)`, `sessions(user_id, expires_at)`, `devices(user_id)`, `threads(user_id, session_id)`, `notifications(user_id, read, created_at DESC)`, and `proactive_cards(user_id, status)`; strip or externalize base64 image payloads before persisting `threads.messages_json`; and prune expired sessions periodically.
- **Impact:** Progressive database file bloat (~1 MB per screenshot turn) and degrading query performance over time.
- **Remediation:** Add `CREATE INDEX IF NOT EXISTS` statements in `src/backend/src/db/index.ts`, strip `image_url` base64 blobs in `ensureSessionAndThread` (`src/backend/src/routes/brain.ts:169`), and delete expired sessions (`DELETE FROM sessions WHERE expires_at > 0 AND expires_at < unixepoch()`).
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/sqlite-db-inspection.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/sqlite-db-inspection.txt), [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json)

---

### `ID: AUD-C-011`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Endpoints Return Dead 404 Local URLs for Composio MCP Proxy (`/mcp/composio`) and Billing (`/billing/local-max-active`, `/billing/local-portal`)
- **Component:** Backend Composio Session & Billing Routes
- **Location:** `src/backend/src/routes/agents.ts:936-951`, `src/backend/src/routes/worker.ts:971-993`
- **Description:** Three endpoints return local loopback URLs pointing to routes that are never registered anywhere in `src/backend/src/`: (1) `POST /agent/composio/session` and `POST /me/composio/session` (`agents.ts:936-951`) return `proxy_url: "http://127.0.0.1:8787/mcp/composio"`, which the C# agent runtime (`CodexHomeManager.cs:204`) writes into Codex `config.toml` as the MCP server URL. (2) `POST /billing/checkout` and `POST /me/billing/checkout` (`worker.ts:971-983`) return `url: "http://127.0.0.1:8787/billing/local-max-active"`. (3) `POST /billing/portal` and `POST /me/billing/portal` (`worker.ts:985-993`) return `url: "http://127.0.0.1:8787/billing/local-portal"`. Fetching any of these three URLs returns HTTP 404 Not Found.
- **Reproduction Steps:**
  1. Call `POST /agent/composio/session`, extract `proxy_url`, and send a request to it.
  2. Call `POST /billing/checkout` and `POST /billing/portal`, extract `url`, and send GET requests to them.
- **Observed Result:** `http://127.0.0.1:8787/mcp/composio`, `http://127.0.0.1:8787/billing/local-max-active`, and `http://127.0.0.1:8787/billing/local-portal` all return HTTP 404 Not Found.
- **Expected Result:** Implement the `/mcp/composio` JSON-RPC proxy route (or point `proxy_url` to a working MCP endpoint) and implement GET routes for `/billing/local-max-active` and `/billing/local-portal` so browser redirects do not land on a 404 page.
- **Impact:** Codex MCP tool calls configured via `CodexHomeManager.cs` fail with HTTP 404 against `/mcp/composio`, and clicking Billing Checkout/Portal in the desktop UI opens a 404 error page in the browser.
- **Remediation:** Register `/mcp/composio` in `src/backend/src/routes/agents.ts` and register HTML/JSON handlers for `GET /billing/local-max-active` and `GET /billing/local-portal` in `src/backend/src/routes/worker.ts`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-012`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Non-Functional Stubs in Account Deletion, Companion History, Telemetry Error Reporting, Spotify Search, Connector Execution, and Account Labeling
- **Component:** Backend Worker & Agents Stub Routes
- **Location:** `src/backend/src/routes/worker.ts:995-1050`, `src/backend/src/routes/agents.ts:929-934`, `src/backend/src/routes/agents.ts:1112-1173`
- **Description:** Multiple endpoints across `worker.ts` and `agents.ts` are no-op or hardcoded stubs: (1) `POST /account/delete` and `POST /me/account/delete` (`worker.ts:995-1003`) return `{ ok: true, deleted: true }` without deleting a single row from SQLite (`users`, `memory`, `threads`, `sessions`, `devices`, `notifications`, `proactive_cards`). (2) `GET /me/companion-history` and `POST /me/companion-history` (`worker.ts:1045-1050`) always return empty arrays `{ items: [], turns: [], history: [] }` instead of reading/writing `threads`. (3) `POST /telemetry/client-error` and `POST /report-error` (`worker.ts:1005-1020`) discard error reports without logging or persisting them and read `body.category` instead of `ErrorReport.cs` fields (`stage`, `error_kind`, `message`). (4) `POST /tools/spotify/search` and `POST /agent/realtime/spotify/search` (`agents.ts:1142-1173`) always return hardcoded Daft Punk track ID `4uLU6hMCjMI75M1A2tKUQC`. (5) `POST /tools/execute-connector` (`agents.ts:1112-1138`) returns `{ status: "ok", executed: true }` without invoking any integration. (6) `POST /agent/integrations/:toolkit/accounts/:connectionId/label` (`agents.ts:929-934`) returns `{ ok: true }` without updating `account.label` in `integrationsCatalog`.
- **Reproduction Steps:**
  1. Check row counts in `users` and `memory`, call `POST /account/delete`, and re-check row counts.
  2. Call `GET /me/companion-history` after running `/brain/turn` turns.
  3. Call `POST /tools/spotify/search` with any query and inspect `tracks[0].id`.
  4. Call `POST /agent/integrations/gmail/accounts/conn_gmail_local_01/label` with `{"label":"Work"}` and then `GET /agent/integrations`.
- **Observed Result:** `POST /account/delete` leaves 100% of SQLite rows intact (`actuallyDeletedAnything: false`). `GET /me/companion-history` returns `items: []`. Spotify search always returns `4uLU6hMCjMI75M1A2tKUQC`. Integration account label remains `"Local Default Account"`.
- **Expected Result:** `/account/delete` should purge user data from SQLite; `/me/companion-history` should query/persist `threads`; `/telemetry/client-error` should persist or log `ErrorReport` fields; `/agent/integrations/.../label` should mutate the account label.
- **Impact:** Users cannot delete their local account data, companion history never loads across app restarts, client error reports are silently lost, and Spotify/connector tools return fake static data.
- **Remediation:** Wire `/account/delete` and `/me/companion-history` to SQLite in `worker.ts`, update `account.label` in `agents.ts:929-934`, and log/store telemetry error reports.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)

---

### `ID: AUD-C-013`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Proactive Jobs, Card Statuses, Turn Leases, Integrations, and Calendar Events Use Unbounded Volatile In-Memory State Lost on Restart
- **Component:** Backend Agents & Proactive State Management
- **Location:** `src/backend/src/routes/agents.ts:14-184`
- **Description:** Although `db/index.ts` defines a `proactive_cards` table in SQLite (`db/index.ts:86-97`), `src/backend/src/routes/agents.ts` never reads or writes `proactive_cards` in SQLite for job management. Instead, `proactiveJobs` (`agents.ts:47`), `proactiveCardStatuses` (`agents.ts:81`), `turnLeases` (`agents.ts:106`), `integrationsCatalog` (`agents.ts:127`), and `calendarEvents` (`agents.ts:163`) are kept purely in volatile JavaScript `Map` and `Array` objects in RAM, with no eviction/pruning of expired `turnLeases` or created `proactiveJobs`.
- **Reproduction Steps:**
  1. Create a proactive job via `POST /proactive/jobs`, connect an integration via `POST /agent/integrations/slack/connect`, or create a calendar event via `POST /calendar/create`.
  2. Restart the backend server process and query `GET /proactive/jobs`, `GET /agent/integrations`, and `GET /calendar/events`.
- **Observed Result:** All user-created proactive jobs, connected integrations, card dismissals, and calendar events are lost upon backend restart, resetting to hardcoded seed values.
- **Expected Result:** Persist proactive jobs, card statuses, integration connections, and calendar events in SQLite (`data/kursor.db`), and periodically prune expired entries from `turnLeases`.
- **Impact:** Every backend restart wipes out the user's scheduled proactive routines, connected integration states, and local calendar items.
- **Remediation:** Back `proactiveJobs`, `proactiveCardStatuses`, `integrationsCatalog`, and `calendarEvents` with SQLite tables in `src/backend/src/db/index.ts` and add TTL cleanup for `turnLeases`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json)

---

### `ID: AUD-C-014`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Fragile Regex HTML Scraping in `/search/web` and HTML Tag Stripping Before Entity Decoding in `decodeHtmlEntities`
- **Component:** Backend Web Search Scraper
- **Location:** `src/backend/src/routes/worker.ts:571-582`, `src/backend/src/routes/worker.ts:610-655`, `src/backend/src/routes/worker.ts:753-768`
- **Description:** `searchDuckDuckGoHtml` (`worker.ts:620-621`) scrapes `https://html.duckduckgo.com/html/` using a single attribute-order-dependent regex (`/<a[^>]+class="[^"]*result__a[^"]*"[^>]+href="([^"]+)"[^>]*>([\s\S]*?)<\/a>/gi`). When DuckDuckGo rate-limits or serves an anomaly/challenge page (which returns HTTP 200 with 0 regex matches), `/search/web` silently falls back to Wikipedia OpenSearch or returns a hardcoded dummy link to `https://duckduckgo.com/?q=...` with HTTP 200 (`worker.ts:755-764`). Furthermore, `decodeHtmlEntities` (`worker.ts:571-582`) strips `<[^>]+>` tags *before* decoding `&lt;` and `&gt;`, so HTML-encoded tags (`&lt;script&gt;` or `&lt;system&gt;`) in scraped snippets are decoded into raw tags and returned unescaped to LLM/agent consumers.
- **Reproduction Steps:**
  1. Inspect `decodeHtmlEntities` at `src/backend/src/routes/worker.ts:571-582`: `.replace(/<[^>]+>/g, "")` runs on line 573 before `.replace(/&lt;/g, "<").replace(/&gt;/g, ">")` on lines 576–577.
  2. Inspect `searchDuckDuckGoHtml` fallback behavior at `src/backend/src/routes/worker.ts:744-768`.
- **Observed Result:** HTML-encoded angle brackets in search snippets survive tag stripping and become raw `<...>` markup after `decodeHtmlEntities()`, and upstream search failures silently return HTTP 200 with synthetic fallback results.
- **Expected Result:** Strip HTML tags *after* decoding HTML entities (or return plain text without re-introducing raw `<`/`>`), and surface upstream search provider degradation clearly in response metadata.
- **Impact:** Brittle web search results and potential indirect prompt/markup injection into agent/OneBrain contexts consuming `/search/web` snippets.
- **Remediation:** In `decodeHtmlEntities` (`worker.ts:571-582`), decode entities first and strip `<[^>]+>` tags last (or keep `<`/`>` escaped).
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/audit-summary.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/audit-summary.json)

---

### `ID: AUD-C-015`
- **Severity:** Low
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Missing Request Body Size Limits and Silent Swallowing of Malformed JSON Across 25+ POST/PUT Endpoints
- **Component:** Backend Request Parsing & Validation
- **Location:** `src/backend/src/index.ts:19-20`, `src/backend/src/routes/auth.ts:184`, `src/backend/src/routes/worker.ts:210`, `src/backend/src/routes/voice.ts:13`, `src/backend/src/routes/brain.ts:1180`, `src/backend/src/routes/agents.ts:197`
- **Description:** The Hono server does not mount a `bodyLimit` middleware (`src/backend/src/index.ts:19-20`), allowing arbitrarily large request bodies on endpoints like `POST /memory`, `PUT /memory`, and `POST /brain/turn`. In addition, 25+ POST/PUT handlers wrap `await c.req.json()` in `try { ... } catch { // Ignore }` and proceed with an empty `{}` object instead of returning `400 Bad Request` when the client sends malformed JSON (`{"broken_json": true, `) with `Content-Type: application/json`.
- **Reproduction Steps:**
  1. Send `POST` or `PUT` requests with `Content-Type: application/json` and invalid JSON body `'{"broken_json": true, '` to all POST/PUT routes.
  2. Inspect the HTTP status codes in `docs/audit/evidence/ws-c/route-test-matrix.json`.
- **Observed Result:** Almost all POST/PUT routes return `200 OK` (or `201 Created` on `/proactive/jobs`) when sent syntactically invalid JSON with `Content-Type: application/json`, silently applying default/fallback values.
- **Expected Result:** Mount Hono's `bodyLimit` middleware (e.g., 20 MB max for screenshot turns, 1 MB for standard JSON routes) and return `400 Bad Request` (`{ error: "invalid_json" }`) when a non-empty `application/json` body fails JSON parsing.
- **Impact:** Masks client serialization bugs by returning `200 OK` on corrupt JSON requests, and permits memory/disk exhaustion via oversized `PUT /memory` payloads.
- **Remediation:** Add Hono `bodyLimit` middleware in `src/backend/src/index.ts` and distinguish between an intentionally empty body (`Content-Length: 0`) and malformed JSON (`SyntaxError` -> HTTP 400).
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-c/route-test-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/route-test-matrix.json), [`docs/audit/evidence/ws-c/backend-audit-test-results.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-c/backend-audit-test-results.txt)
