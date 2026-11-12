# 02 — API Contracts, Worker Endpoints & OneBrain SSE Protocol Specification

> [!IMPORTANT]
> This document specifies every shared contract in `HeyClicky.Contracts.dll`, every HTTP/WebSocket client and DTO record in `HeyClicky.Core.dll`, all Cloudflare Worker and Supabase endpoints, the complete **OneBrain** (`/brain/turn`) Server-Sent Events (SSE) protocol, and the exact system prompts and model configurations in **HeyClicky v1.1.5**.

---

## 1. Base URLs, Override Mechanism, Headers & Connection Prewarming

### 1.1 Production & Local Development Endpoints (`ClickyEndpoints` & `AppBundleConfiguration`)

`HeyClicky.ClickyEndpoints` (in `HeyClicky.Contracts.dll`) is an immutable record holding the three root coordinates:
```csharp
namespace HeyClicky;

public sealed record ClickyEndpoints(
    Uri WorkerBaseUrl,
    Uri SupabaseUrl,
    string SupabaseAnonKey
);
```

| Setting | Production Default (`AppBundleConfiguration.Defaults`) | Local Dev Constant |
| :--- | :--- | :--- |
| **`WorkerBaseUrl`** | `https://api.heyclicky.com` | `http://localhost:8787` / `http://127.0.0.1:8787` |
| **`SupabaseUrl`** | `https://mrpvynsdsnimuisyhkow.supabase.co` | `http://127.0.0.1:54321` |
| **`SupabaseAnonKey`** | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1ycHZ5bnNkc25pbXVpc3loa293Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzYyODAxNzgsImV4cCI6MjA5MTg1NjE3OH0.qf1-em-OB-OtyX4WTuGnSi28djq-5T0k0whKvcTtTAY` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0` |

#### Resolution Pipeline (`HeyClicky.Core.Configuration.AppBundleConfiguration`)
```mermaid
flowchart LR
    A["1. Compiled Defaults\n(api.heyclicky.com)"] --> B["2. AssemblyMetadata\n(HeyClicky.WorkerBaseUrl,\nSupabaseUrl, SupabaseAnonKey)"]
    B --> C["3. endpoints.json\n(%LOCALAPPDATA%\\HeyClicky\\endpoints.json)"]
    C --> D["4. HEYCLICKY_WORKER_BASE_URL\n(Loopback localhost / 127.0.0.1 / ::1 only)"]
    D --> E["5. use-production-backend.flag\n(Forces production if flag file exists)"]
```

- **`AppBundleConfiguration.AppDataDirectory`**: Reads `HEYCLICKY_DATA_DIR` env var; falls back to `HeyClicky.AppPaths.Root` (`%LOCALAPPDATA%\HeyClicky`).
- **`AppBundleConfiguration.OverrideFilePath`**: `%LOCALAPPDATA%\HeyClicky\endpoints.json`. Accepts JSON keys:
  ```json
  {
    "workerBaseUrl": "http://127.0.0.1:8787",
    "supabaseUrl": "https://mrpvynsdsnimuisyhkow.supabase.co",
    "supabaseAnonKey": "eyJ..."
  }
  ```
- **`AppBundleConfiguration.UseProductionBackendFlagPath`**: `%LOCALAPPDATA%\HeyClicky\use-production-backend.flag`.

---

### 1.2 Client Identity & HTTP Headers (`ClientIdentity` & `ClickyClientHeadersHandler`)

```csharp
namespace HeyClicky;

public sealed record ClientIdentity(
    string Platform,
    string DeviceId,
    string AppVersion,
    string AppBuild,
    string OsVersion,
    IReadOnlyList<string> Capabilities
);
```

`HeyClicky.Core.Net.ClickyClientHeadersHandler` is an `HttpMessageHandler` wrapper inside `ClickyHttp`. Whenever `IsClickyBackendUrl(request.RequestUri, endpoints)` returns `true` (matching `WorkerBaseUrl` or `SupabaseUrl`), `Apply()` injects five mandatory identification headers:

| HTTP Header Name | Source Property | Observed Windows v1.1.5 Value |
| :--- | :--- | :--- |
| **`X-Clicky-Platform`** | `ClientIdentity.Platform` | `"windows"` |
| **`X-Clicky-Device`** | `ClientIdentity.DeviceId` | `"87e8a4d1-1480-4340-a3e9-5e80714b2ad0"` (persisted UUID) |
| **`X-Clicky-App-Version`** | `ClientIdentity.AppVersion` | `"1.1.5"` |
| **`X-Clicky-App-Build`** | `ClientIdentity.AppBuild` | `"0"` (or build number) |
| **`X-Clicky-OS-Version`** | `ClientIdentity.OsVersion` | `"10.0.26300"` |

#### Per-Request Custom Headers
| Header Name | Sent By | Purpose |
| :--- | :--- | :--- |
| **`Authorization`** | All authenticated Worker & Supabase calls | `Bearer <supabase_access_token>` |
| **`apikey`** | `SupabaseAuthService` | Supabase `anon` JWT key |
| **`x-clicky-usage-context`** | `WorkerClient`, `BrainClient`, `DictationTranscriptionClient`, `GeminiTranscriber`, `DeepgramTranscriber`, `RealtimeMouth` | Identifies billing/telemetry context (`"companion"`, `"dictation"`, `"onboarding"`, etc.) |
| **`x-clicky-accepts-gzip`** | `BrainClient` (`BrainRequestCompression`) | Value `"1"`; signals Worker that the client supports gzip-compressed request payloads |
| **`Content-Encoding`** | `BrainRequestCompression` | Value `"gzip"` when request body `>= 8192` bytes and `workerAcceptsGzip` is true |

---

### 1.3 TLS & Connection Pool Prewarming (`ClickyHttp`)

`HeyClicky.Core.Net.ClickyHttp` maintains a singleton `HttpClient` and `HttpMessageInvoker` and exposes `PrewarmAll()` to eliminate cold TLS handshake latency before push-to-talk or dictation key-down:

| Method | Target Host URI | Purpose |
| :--- | :--- | :--- |
| `PrewarmWorker()` | `Endpoints.WorkerBaseUrl` (`https://api.heyclicky.com`) | Warms HTTP/2 + TLS connection to Cloudflare Worker |
| `PrewarmOpenAI()` | `https://api.openai.com` | Warms TLS connection for OpenAI Realtime WebSocket (`RealtimeMouth`) |
| `PrewarmDeepgram()` | `https://api.deepgram.com` | Warms TLS connection for Deepgram Nova-3 WebSocket (`DeepgramTranscriber`) |
| `PrewarmGemini()` | `https://generativelanguage.googleapis.com` | Warms TLS connection for Gemini Live Bidi & Gemini TTS WebSockets |
| `PrewarmAll()` | All 4 hosts above | Invoked on app launch, shortcut hover/focus, and `DictationController.WarmUpNetworkConnections()` |

---

## 2. Complete HTTP Route Matrix Across All Clients

### 2.1 `WorkerClient` (`HeyClicky.Core.Worker.WorkerClient`)

| Method Name | HTTP Verb & Route | Auth | Request DTO / Payload | Response DTO / Return Type | Description |
| :--- | :--- | :---: | :--- | :--- | :--- |
| `GetPlanAsync` | `GET /plan` | Bearer | — | `PlanSnapshot` | Returns user's current tier (`free`, `pro`, `unlimited`, `max`), message/agent/dictation `PlanUsageBucket` (`used`, `cap`), subscription details, and agent caps. |
| `GetAppConfigAsync` | `GET /config` | Optional | — | `AppConfigResponse` -> `AppConfig` | Returns remote links (`featureRequest`, `communityReddit`, `communityWhatsApp`, `changelog`) cached in `app-config.json`. |
| `GetRuntimeModelPolicyAsync` | `GET /config/runtime-models` | Bearer | — | `JsonObject` | Returns dynamic model routing overrides and provider availability flags. |
| `RegisterDeviceAsync` | `POST /devices/register` | Bearer | `DeviceRegistrationRequest` (`capabilities`, `display_name`, `model`) | `DeviceResponse` -> `RegisteredDevice` | Registers current PC hardware and capabilities; stamped in `device-registration.json`. |
| `GetDevicesAsync` | `GET /devices` | Bearer | — | `DevicesResponse` -> `IReadOnlyList<RegisteredDevice>` | Lists all devices registered to the user's account. |
| `SetDiscoveryChannelAsync` | `POST /onboarding/discovery-channel` | Bearer | `DiscoveryChannelRequest` (`discovery_channel`, `discovery_channel_other`) | `DiscoveryChannelResponse` | Records how the user heard about HeyClicky during onboarding. |
| `GetDiscoveryChannelAsync` | `GET /onboarding/discovery-channel` | Bearer | — | `DiscoveryChannelResponse` | Retrieves stored discovery channel attribution. |
| `GetMemoryAsync` | `GET /memory` | Bearer | — | `UserMemoryResponse` (`markdown`, `profile_markdown`, `context_markdown`, `has_profile`, `updated_at`) | Fetches user's cloud-synced OneBrain profile and memory markdown. |
| `SaveMemoryAsync` | `PUT /memory` | Bearer | `SaveMemoryRequest` (`memory`) | `SaveMemoryResponse` (`saved`, `error`) | Updates the user's cloud-synced memory markdown. |
| `ReportError` | `POST /telemetry/client-error` | Optional | `ErrorReport` (`stage`, `error_kind`, `error_message`, `user_facing_message`, `app_version`, `error_code`, `error_domain`, `supabase_user_id`, `user_email`) | Fire-and-forget | Reports client-side voice, auth, or brain errors to the Worker. |
| `BuildStarterPlanAsync` | `POST /onboarding/starter-plan` | Bearer | `StarterPlanRequest` (`interview`, `connected_integrations`, `available_integrations`, `clickys`, `first_asks_for_clicky`, `interview_saved`, `jobs`) | `StarterPlanResponse` (`clickys`, `suggested_agents`, `agent_familiarity`, `goal_line`) | Generates personalized starter Clickys and background jobs from the onboarding interview. |
| `SearchImagesAsync` | `POST /search/images` | Bearer | `ImageSearchRequest` (`query`, `limit`, `subject_photo`, `page_url`) | `ImageSearchResponse` -> `IReadOnlyList<ImageSearchResult>` | Searches web images (`thumbnail_url`, `full_url`, `source_url`, `title`) for 2D canvas `photo()` cards. |
| `SearchWebAsync` | `POST /search/web` | Bearer | `WebSearchRequest` (`query`, `count`, `freshness`) | `WebSearchResponse` (`query`, `results`: `title`, `url`, `description`, `age`, `highlights`) | Executes web search for OneBrain and background tools. |
| `CreateCheckoutAsync` | `POST /billing/checkout` | Bearer | `CheckoutRequest` (`plan`, `interval`, `checkout_ui`, `discount_code`) | `CheckoutResponse` (`url`, `discount_code_applied`) | Creates a Stripe Checkout session URL for upgrading to `pro` or `max`. |
| `CreatePortalSessionAsync` | `POST /billing/portal` | Bearer | `{}` | `PortalResponse` (`url`) | Creates a Stripe Customer Portal URL for managing billing. |
| `DeleteAccountAsync` | `POST /account/delete` | Bearer | `{"confirm": ...}` | `void` | Permanently deletes the user's HeyClicky account. |
| `GetReferralAsync` | `GET /referral` | Bearer | — | `JsonObject` | Fetches user's referral code, share URL, and stats. |
| `ClaimReferralAsync` | `POST /referral/claim` | Bearer | `ReferralClaimRequest` (`code`, `source`) | `ReferralOutcomeResponse` (`outcome`, `message`, `discount_percent`, `code`, `share_url`, `share_display_text`) | Redeems a referral code. |
| `ChangeReferralCodeAsync` | `POST /referral/code` | Bearer | `ReferralCodeRequest` (`code`) | `ReferralOutcomeResponse` | Customizes the user's personal referral code slug. |
| `GetLanguageHintsAsync` | `GET /voice/language-hints?lang={code}` | Bearer | Query param `lang` | `IReadOnlyList<string>` | Fetches language-specific phonetic/vocabulary hints. |

---

### 2.2 `BrainClient` (`HeyClicky.Core.Brain.BrainClient`)

| Method Name | HTTP Verb & Route | Request Headers | Request Body | Response Format |
| :--- | :--- | :--- | :--- | :--- |
| `StreamTurnAsync` | `POST /brain/turn` | `Authorization: Bearer ...`<br>`x-clicky-accepts-gzip: 1`<br>`x-clicky-usage-context: ...`<br>`Content-Encoding: gzip` (if >= 8 KB) | Streaming JSON written via `Utf8JsonWriter`: `session_id`, `thread`, `input`, `user_layer`, `user_layer_signature`, `drawing_code`, `canvas`, `environment_sent`, `turn_token`, `keep_reasoning`, etc. | `text/event-stream` (SSE parsed by `BrainSseParser` -> `BrainReplyAccumulator` -> `BrainReply`) |
| `CompactAsync` | `POST /brain/compact` | `Authorization: Bearer ...`<br>`x-clicky-accepts-gzip: 1` | JSON: `{"session_id": "...", "thread": [...], "input_tokens": 75000}` | JSON: `BrainCompactReply` (`compacted`: `note_item`, `keep_from_index`, `keep_leading_items`, `drop_leading_items`; `tokens_until_fold`) |
| `EndSessionOnWorker` (via `BrainSession`) | `POST /brain/session-end` | `Authorization: Bearer ...`<br>`x-clicky-accepts-gzip: 1` | JSON: `{"session_id": "...", "thread": [...], "screenshot_files": [...]}` | JSON status (triggers server-side memory extraction & screenshot cleanup) |
| `Prewarm` (via `BrainSession`) | `POST /brain/prewarm` | `Authorization: Bearer ...` | JSON: `{"session_id": "..."}` | JSON status (warms prompt cache on Worker/upstream LLM) |

> [!TIP]
> **Gzip Request Compression (`BrainRequestCompression`)**: `GzipPaths` contains `/brain/turn`, `/brain/compact`, and `/brain/session-end`. Every request includes `x-clicky-accepts-gzip: 1`. Once the Worker responds with header `x-clicky-accepts-gzip`, `workerAcceptsGzip` becomes `true`, and all subsequent JSON bodies `>= 8,192` bytes (`MinimumBytesToCompress`) are Gzip-compressed with `Content-Encoding: gzip`.

---

### 2.3 `SupabaseAuthService` (`HeyClicky.Core.Auth.SupabaseAuthService`)

| Operation | HTTP Verb & Route (`SupabaseUrl`) | Headers | Payload / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GoogleSignInStartUrl` | `GET /auth/v1/authorize` | — (Opened in system browser) | `?provider=google&redirect_to=heyclicky://auth/callback&code_challenge=...&code_challenge_method=S256&state=...` | Initiates Google OAuth 2.0 PKCE flow. |
| `HandleDeepLinkAsync` (PKCE Exchange) | `POST /auth/v1/token?grant_type=pkce` | `apikey: <SupabaseAnonKey>` | `{"auth_code": "...", "code_verifier": "..."}` | Exchanges OAuth authorization code for `access_token` and `refresh_token`. |
| `SignInWithPasswordAsync` | `POST /auth/v1/token?grant_type=password` | `apikey: <SupabaseAnonKey>` | `{"email": "...", "password": "..."}` | Direct email + password sign-in. |
| `PerformRefreshAsync` | `POST /auth/v1/token?grant_type=refresh_token` | `apikey: <SupabaseAnonKey>` | `{"refresh_token": "..."}` | Refreshes expired JWT access token (proactively refreshes before expiry with cooldown guard). |
| `FetchUserProfileAsync` | `GET /auth/v1/user` | `apikey: <SupabaseAnonKey>`<br>`Authorization: Bearer <token>` | — | Populates `HeyClicky.UserProfile` (`UserId`, `Email`, `DisplayName`, `AvatarUrl`). |
| `SignOutAsync` | `POST /auth/v1/logout` | `apikey: <SupabaseAnonKey>`<br>`Authorization: Bearer <token>` | — | Revokes session and deletes DPAPI `auth.bin`. |

---

### 2.4 Voice, Dictation, Proactive, Calendar & Tool Clients

| Client Class | Method Name | HTTP Verb & Route (`WorkerBaseUrl`) | Request Payload | Response Payload |
| :--- | :--- | :--- | :--- | :--- |
| **`DictationTranscriptionClient`** | `TranscribeAsync` | `POST /v2/dictation/transcribe` | Multipart / WAV audio + `language`, `known_terms` | `{"text": "...", "stt_ms": 123}` |
| **`DictationTranscriptionClient`** | `CleanUpAsync` | `POST /v2/dictation/cleanup` | `{"text": "...", "duration_seconds": 3.2, "language": "en", "fast_cleanup": true, "stt_ms": 140, "receipt": "...", "known_terms": [...], "context": {"app_name": "...", "site_host": "...", "before_text": "...", "selected_text": "...", "after_text": "...", "single_line": false}}` | `{"text": "..."}` (cleaned text ready for insertion) |
| **`GeminiTranscriber`** | `MintAsync` | `POST /v2/dictation/gemini-token` | `{"language": "en", "keyterms": [...], "sample_rate": 16000, "purpose": "dictation" \| "ears"}` | `MintedSession` (`token`, `model`, `setup`, `receipt`) |
| **`DeepgramTranscriber`** | `MintAsync` | `POST /deepgram/token` | `{"language": "en", "purpose": "ears"}` | `DeepgramTokenResponse` (`access_token`, `model`, `receipt`) |
| **`RealtimeMouth`** | `MintAsync` | `POST /realtime/token` | `RealtimeSessionRequest` (`voice`, `speed`, `prompt_context`, `client_capabilities`, `mouth_provider`, `language`, `languages`) | Ephemeral token (`client_secret.value` or Gemini token + `model`) |
| **`AgentBackendClient`** | `FetchUnreadAsync` | `GET /agents/notifications/unread` | — | `IReadOnlyList<AgentNotification>` |
| **`AgentBackendClient`** | `MarkReadAsync` | `POST /agents/notifications/{id}/read` | `{}` | `void` |
| **`AgentBackendClient`** | `MarkAllReadAsync` | `POST /agents/notifications/read-all` | `{}` | `void` |
| **`AgentBackendClient`** | `FetchCronDetailAsync` | `GET /agents/crons/{id}` | — | `JsonObject` |
| **`AgentBackendClient`** | `FetchIntegrationsAsync` | `GET /connectors/status` | — | `IReadOnlyList<AgentIntegrationInfo>` |
| **`AgentBackendClient`** | `ConnectIntegrationAsync` | `POST /connectors/connect` | `{"toolkit": "gmail"}` | `{"url": "https://..."}` (OAuth authorize URL) |
| **`ProactiveAgentsClient`** | `StartJobAsync` | `POST /proactive/jobs` | `ProactiveRequestInputs` (`reason`, `local_day`, `clickys`, `recent_history`, `connected_integrations`, `foreground_activity`, etc.) | `ProactiveJobStatus` (`job_id`, `state`, `cards`) |
| **`ProactiveAgentsClient`** | `PollJobAsync` | `GET /proactive/jobs/{jobId}` | — | `ProactiveJobStatus` (`state`, `cards`: `IReadOnlyList<ProactiveSuggestionCard>`) |
| **`ProactiveAgentsClient`** | `ReportResolutionAsync` | `POST /proactive/cards/resolve` | `{"card_ids": [...], "resolution": "accepted" \| "dismissed", "reason": "..."}` | `void` |
| **`ProactiveAgentsClient`** | `FetchStatusesAsync` | `POST /proactive/cards/status` | `{"card_ids": [...]}` | `IReadOnlyDictionary<int, string>` |
| **`ProactiveAgentsClient`** | `ClaimMorningInvitationAsync` | `POST /proactive/morning/claim` | `{"card_ids": [...]}` | `bool` |
| **`UpcomingMeetingsClient`** | `FetchAsync` | `GET /calendar/upcoming` | — | `UpcomingCalendarFetch` (`connected`, `events`: `id`, `title`, `starts_at`, `ends_at`, `is_all_day`, `meeting_url`, `location`, `html_link`) |
| **`WorkerTools`** | `RouteAsync` | `POST /tools/search-connectors`<br>`POST /tools/execute-connector`<br>`POST /tools/spotify/search` | Tool JSON arguments with `time_zone` injected | Tool execution JSON string |

---

## 3. OneBrain Streaming SSE Protocol (`BrainClient` & `BrainSseParser`)

When the user speaks to Clicky (Push-to-Talk) or submits text in the Notch composer, `CompanionSession` invokes `BrainSession` -> `BrainClient.StreamTurnAsync` (`POST /brain/turn`).

### 3.1 SSE Wire Format & `BrainSseParser`
`HeyClicky.Core.Brain.BrainSseParser` incremental byte parser reads the `text/event-stream` response stream and dispatches to `IBrainStreamSink` (implemented by `BrainReplyAccumulator`):
```csharp
namespace HeyClicky.Core.Brain;

public interface IBrainStreamSink
{
    void OnText(ReadOnlySpan<char> chunk);
    void OnEvent(string eventName, JsonElement data);
}
```

### 3.2 Complete Catalog of `/brain/turn` SSE Events (`BrainReplyAccumulator.OnEvent`)

| SSE Event / JSON `type` | Payload Fields (`JsonElement`) | Accumulator & Client Behavior |
| :--- | :--- | :--- |
| **Raw text delta** (`OnText`) | Streaming code/text tokens | Appended to `rawText`. `BrainReplyAccumulator.ReportCodeIfItGrew()` extracts fenced or raw JS drawing code and feeds it live to `IScreenDrawingCanvas.Feed(code, isFinal: false)` and `SayLines.Scan()`. |
| **`say`** | `{"text": "...", "step": 1}` | Spoken narration line emitted either explicitly as an SSE event or extracted from `say(1, "...")` calls in the streamed drawing code (`SayLines`). Immediately queued into `RealtimeMouth.SpeakLine(text, step)`. |
| **`status`** | `{"text": "..."}` or `{"status": "..."}` | Updates live status label in the Notch UI (`onStatus`). |
| **`tool_call`** / **`tool_calls`** | `{"call_id": "...", "name": "...", "arguments": "{...}", "async": false}` | Deserialized into `BrainToolCall(CallId, Name, Arguments, IsAsync)`. Added to `Reply.ToolCalls` and immediately passed to `onToolCall` callback (`BrainToolRunner`). |
| **`computer_calls`** | `{"call_id": "...", "actions": [...], "pending_safety_checks": [...]}` | Deserialized via `BrainComputerCall.FromEvent()` into `BrainComputerCall(CallId, Actions, PendingSafetyChecks)` for GUI automation (`HeyClicky.Core.Drive`). |
| **`search_record`** | `{"query": "...", ...}` or raw string | Appended to `Reply.SearchRecords` for source attribution. |
| **`meta`** | `{"model": "...", "turn_token": "...", "keep_reasoning": true, "guided_max_rounds": 8, "drive_lease_id": "...", "screenshot_file": "...", "user_layer": "...", "user_layer_signature": "..."}` | Populates `Reply.ModelIdentifier`, `Reply.TurnToken`, `Reply.KeepsReasoning`, `Reply.GuidedMaxRounds`, `Reply.DriveLeaseId` (fires `OnDriveLeaseId`), `Reply.ScreenshotFile`, `Reply.UserLayer`, and `Reply.UserLayerSignature`. |
| **`items`** / **`output`** | `[...]` (array of OpenAI Responses API / thread items: `message`, `function_call`, `function_call_output`, `reasoning` with `encrypted_content`) | Appended to `Reply.Items` so `BrainSession.CommitTurn` can persist the exact conversation history in `OneBrain\threads\<sessionId>.json`. |
| **`done`** | `{"usage": {"input_tokens": ..., "last_input_tokens": ...}, "cost": 0.0042, "message_recorded": true, "message_refunded": false, "drive_messages_refunded": 0}` | Sets `Reply.SawDone = true`, records token counts (`InputTokens`, `LastInputTokens`), `CostUsd`, and quota flags (`MessageRecorded`, `MessageRefunded`, `DriveMessagesRefunded`). |
| **`error`** | `{"code": "...", "message": "..."}` | Sets `Reply.ErrorCode` and `Reply.Error`. If `code == "cyber_policy"`, `BrainSession.IsSafetyRefusal` triggers `SafetyRefusalLine` (`"My AI provider's safety filter blocked this because it looks like hands-on security work. I can still explain the concepts."`). If screenshot references were rejected, throws `ScreenshotFilesUnavailableException` and retries sending full base64 JPEGs for the rest of the session. |

### 3.3 `SayLines` & Synchronized 2D/3D Drawing Execution

As the LLM streams JavaScript code for the screen canvas, it embeds `say()` calls:
- `say("Single sentence narration")` (unnumbered)
- `say(1, "First step narration")`, `say(2, "Second step narration")` (step-indexed)

`HeyClicky.Core.Brain.SayLines` parses the growing JavaScript buffer in real time:
- `SayLines.Scan(code, (int? step, string line) => ...)`: Extracts complete `say(...)` string literals as soon as their closing `");` arrives in the SSE stream, **before** the full turn finishes.
- `RealtimeMouth.SpeakLine(line, step)` begins synthesizing audio immediately.
- When `RealtimeMouth` emits `LineAudioQueued(step, durationSeconds)` or `LineAudioChunk`, `IScreenDrawingCanvas.SetLineLength(step, durationSeconds)` and `IScreenDrawingCanvas.ReleaseLine(step)` release the corresponding visual ink strokes (`spot`, `box`, `arrow`, `penRide`) on screen in exact millisecond lockstep with the voice!
- If the drawing program calls interactive actions (e.g., `openUrl`, `copyText`, `askFollowUp`), the V8 engine emits `HeyClicky.DrawingAction(string Kind, JsonElement Payload)` via `IScreenDrawingCanvas.ActionRequested`.

### 3.4 Thread Compaction & Session Lifecycle (`BrainSession` & `BrainCompactionPolicy`)

- **Screenshot Pruning**: `BrainSession.MaximumScreenshotsInThread = 48`. When exceeded, `BrainSession` trims older `input_image` / `clicky_file` items, keeping the newest `ScreenshotsKeptAfterTrim = 32` (`"more than 48 screenshots in the thread: kept the newest 32"`).
- **Automatic Thread Compaction (`BrainCompactionPolicy`)**:
  - `CompactionConversationTokenThreshold = 45,000` tokens
  - `CompactionInputTokenThreshold = 75,000` tokens
  - When `BrainCompactionPolicy.ShouldAskWorker(inputTokens, baselineInputTokens, nextCheckInputTokens)` returns `true`, `BrainSession.CompactThreadIfLarge` calls `POST /brain/compact` asynchronously.
  - The Worker returns `BrainCompactReply(BrainCompactResult Compacted, int? TokensUntilFold)` where `BrainCompactResult` specifies `(JsonNode NoteItem, int KeepFromIndex, int KeepLeadingItems, int DropLeadingItems)`.
  - If the thread has not moved on (`compactionGeneration` matches), `ApplyCompaction` replaces the folded prefix with `NoteItem`.

---

## 4. `HeyClicky.Contracts.dll` Complete Type & Interface Reference

All 55 top-level types in `HeyClicky.Contracts.dll` reside in the root `HeyClicky` namespace:

| Type Name | Kind | Properties / Methods / Signature |
| :--- | :--- | :--- |
| **`AppPaths`** | `static class` | `string Root { get; }`, `string DocumentsRoot { get; }`, `bool IsOverridden { get; }`, `string Combine(params string[] parts)` |
| **`ClickyEndpoints`** | `sealed record` | `(Uri WorkerBaseUrl, Uri SupabaseUrl, string SupabaseAnonKey)` |
| **`ClientIdentity`** | `sealed record` | `(string Platform, string DeviceId, string AppVersion, string AppBuild, string OsVersion, IReadOnlyList<string> Capabilities)` |
| **`PointD`** | `readonly record struct` | `(double X, double Y)` |
| **`RectD`** | `readonly record struct` | `(double X, double Y, double Width, double Height)` |
| **`DisplayInfo`** | `sealed record` | `(int Index, string DeviceName, RectD BoundsPx, RectD WorkAreaPx, double Scale, bool IsPrimary)` |
| **`ShortcutAction`** | `enum` | `PushToTalkDown (0)`, `PushToTalkUp (1)`, `TextComposerToggle (2)`, `DictationDown (3)`, `DictationUp (4)`, `Escape (5)`, `DictationCancel (6)`, `HandsFreeDictationToggle (7)`, `ProactiveDebugSweep (8)`, `MorningPreviewChord (9)`, `SystemScreenshotShortcut (10)` |
| **`HotkeyModifiers`** | `[Flags] enum` | `None (0)`, `Control (1)`, `Alt (2)`, `Shift (4)`, `Win (8)` |
| **`HotkeyTrigger`** | `enum` | `Hold (0)`, `DoubleTap (1)` |
| **`HotkeyBinding`** | `sealed record` | `(HotkeyModifiers Modifiers, HotkeyTrigger Trigger)` + static defaults (`PushToTalkDefault`, `TextModeDefault`, `DictationDefault`, `HandsFreeDictationDefault`) |
| **`HotkeyBindings`** | `sealed record` | `(HotkeyBinding PushToTalk, HotkeyBinding TextMode, HotkeyBinding Dictation, HotkeyBinding HandsFreeDictation)` |
| **`IShortcutMonitor`** | `interface` | `event EventHandler<ShortcutAction> ShortcutTriggered; bool IsPushToTalkHeld { get; } void Start(); void Stop();` |
| **`DictationFocus`** | `sealed record` | `(object Anchor, int ProcessId, nint WindowHandle, string AppName, string SiteHost, string Role, bool IsSecure, bool IsSingleLine, string BeforeText, string SelectedText, string AfterText)` |
| **`DictationWorkApp`** | `sealed record` | `(int ProcessId, nint WindowHandle, string AppName, string SiteHost, bool AllowsBlindPaste)` |
| **`DictationInsertOutcome`** | `enum` | `Inserted (0)`, `RefusedUnsafeTarget (1)`, `FocusLost (2)`, `ModifiersHeld (3)`, `ClipboardWriteFailed (4)`, `EmptyTranscript (5)` |
| **`IDictationTextTarget`** | `interface` | `CaptureFocusAsync(ct)`, `ForegroundWorkApp()`, `InsertAsync(text, focus, onProgress)`, `BlindPasteAsync(text, workApp)`, `CopyToClipboardAsync(text)`, `ReadFieldValueAsync(focus)` |
| **`ScreenCaptureFrame`** | `sealed record` | `(DisplayInfo Display, byte[] Jpeg, int PixelWidth, int PixelHeight, bool ContainsCursor, PointD CursorInImage)` |
| **`IScreenCaptureService`** | `interface` | `GetDisplays()`, `GetDisplayUnderCursor()`, `CaptureAllAsync(maxEdge, jpegQuality, ct)` |
| **`IMicrophoneCapture`** | `interface` | `event Action<ReadOnlyMemory<byte>> Frame; event Action<float> Level; int SampleRate { get; } bool IsRunning { get; } bool Start(); void Stop();` |
| **`IAudioOutput`** | `interface` | `int SampleRate { get; } double QueuedSeconds { get; } void EnqueuePcm16(ReadOnlySpan<byte> pcm16Mono); void Flush();` |
| **`PermissionKind`** | `enum` | `Microphone (0)`, `ScreenCapture (1)` |
| **`PermissionStatus`** | `enum` | `NotDetermined (0)`, `Prompting (1)`, `WaitingInSettings (2)`, `WaitingForPicker (3)`, `RestartRequired (4)`, `Granted (5)`, `Denied (6)` |
| **`MicrophoneBlock`** | `enum` | `None (0)`, `Policy (1)`, `Device (2)`, `AllApps (3)`, `DesktopApps (4)` |
| **`IPermissionService`** | `interface` | `Check(PermissionKind)`, `RequestAsync(PermissionKind, ct)`, `DiagnoseMicrophoneBlock()`, `OpenSettings(PermissionKind)` |
| **`UserProfile`** | `sealed record` | `(string UserId, string Email, string DisplayName, string AvatarUrl)` |
| **`IAuthService`** | `interface` | `bool IsSignedIn { get; } UserProfile? User { get; } SignInWithGoogleAsync(ct); SignOutAsync(); GetValidAccessTokenAsync(ct);` |
| **`VoiceState`** | `enum` | `Idle (0)`, `Listening (1)`, `Processing (2)`, `Responding (3)` |
| **`CompanionError`** | `sealed record` | `(string Code, string Message, JsonElement? Paywall)` |
| **`ICompanionSession`** | `interface` | Orchestrates Push-to-Talk (`BeginPushToTalk`, `EndPushToTalk`), text turns (`SendTextAsync`), voice state transitions, and canvas lifecycle. |
| **`DrawingAction`** | `sealed record` | `(string Kind, JsonElement Payload)` |
| **`DrawingFeedResult`** | `sealed record` | `(IReadOnlyList<string> Lines, IReadOnlyList<DrawingAction> Actions, int MarkCount, JsonElement Raw)` |
| **`DrawingContinuation`** | `sealed record` | `(int FirstLine, int Marks, string Code)` |
| **`IScreenDrawingCanvas`** | `interface` | `BeginProgram(frame, code)`, `Feed(code, isFinal)`, `ReleaseLine(step)`, `SetLineLength(step, seconds)`, `Clear(animate)`, `Finish()`, `ShowLiveCaption(text, duration, progress)` |
| **`ClickTargetRing`** | `sealed record` | `(PointD CenterPx, double RadiusPx, RectD? ControlFramePx, string Label)` |
| **`PenStrokePath`** | `sealed record` | `(IReadOnlyList<PointD> PointsPx, double BeginTime, double Duration, double X1, double Y1, double X2, double Y2)` |
| **`HighlightStroke`** | `sealed record` | `(IReadOnlyList<PointD> PointsPx, IReadOnlyList<PointD> DwellsPx, int DisplayIndex)` |
| **`IHighlightOverlay`** | `interface` | User circle/highlight gesture capture while holding push-to-talk. |
| **`ICursorOverlay`** | `interface` | `Show()`, `Hide()`, `SetColor(hex)`, `SetVoiceState(state, level)`, `PointAtAsync(pt, label, ct)`, `RidePen(paths)`, `ShowClickTarget(ring)`, `DockAsync(pt)`, `UndockAsync(pt)` |
| **`IPointerInput`** | `interface` | `IDisposable WatchPresses(Action<PointD> onPress); PointD CursorPosition { get; } bool IsAnyButtonDown { get; }` |
| **`NotchActivityPhase`** | `enum` | `Listening (0)`, `Thinking (1)`, `Speaking (2)`, `Custom (3)`, `AgentMulti (4)`, `Docked (5)`, `Dictation (6)` |
| **`NotchLiveEvent`** | `abstract record` | Base type for 29 strongly-typed Notch toast/card records (`AgentTask`, `IntegrationSuggestion`, `IntegrationConnected`, `MicrophoneTrouble`, `VoiceReplyMutedCopiedToClipboard`, `AppUpdated`, `MeetingCountdown`, `ClickyUpdate`, `MorningSuggestions`, `AgentPeek`, `HandoffSent`, `HandoffQueued`, `SidecarLearningSuggestion`, `DictionaryWordLearned`, `DocumentFolderAccessDenied`, `IntroSkillPing`, `SkillPinned`, etc.) |
| **`NotchPresentation`** | `abstract record` | Derived: `Hidden`, `Compact`, `LiveEvent`, `OnboardingPinned`, `TextInput`, `TextResponse` |
| **`INotchPresenter`** | `interface` | Controls top-center Notch UI presentation, live events, and text input bar. |
| **`AgentTaskStatus`** | `enum` | `Starting (0)`, `Running (1)`, `WaitingForUser (2)`, `Finished (3)`, `Failed (4)`, `Stopped (5)` |
| **`AgentTaskSnapshot`** | `sealed record` | `(string TaskId, string ThreadId, string Title, AgentTaskStatus Status, string LatestCommentary, string FinalResponseMarkdown, DateTimeOffset StartedAt)` |
| **`IAgentService`** | `interface` | Starts, stops, and monitors background Codex agent tasks. |

---

## 5. Exact System Prompts & Model Configurations

### 5.1 Active Models Across HeyClicky v1.1.5

| Subsystem | Provider & Model Identifier | Where Configured / Used |
| :--- | :--- | :--- |
| **OneBrain (`/brain/turn`)** | Server-side routed (OpenAI `o3` / `gpt-5` / `gpt-4.1` / Anthropic `claude-sonnet-4-6` with `reasoning` & `encrypted_content`) | `BrainClient` -> `https://api.heyclicky.com/brain/turn` (`GET /config/runtime-models`) |
| **Realtime Mouth (OpenAI)** | `gpt-realtime-2.1-mini` | `HeyClicky.Core.Voice.RealtimeMouth` (`wss://api.openai.com/v1/realtime?model=gpt-realtime-2.1-mini`) |
| **Realtime Mouth (Gemini TTS)** | `gemini-3.8-flash-lite-tts` | `HeyClicky.Core.Voice.RealtimeMouth` (for voices prefixed with `gemini:`) |
| **Streaming Ears (Gemini Live)** | `BidiGenerateContentConstrained` (minted via `/v2/dictation/gemini-token`) | `HeyClicky.Core.Voice.GeminiTranscriber` |
| **Streaming Ears (Deepgram)** | Deepgram `nova-3` (minted via `/deepgram/token`) | `HeyClicky.Core.Voice.DeepgramTranscriber` |
| **Background Agent Harness** | `clicky-harness.exe` (`harness-h1-mac-*` / `h1-local`) | `HeyClicky.Agents.dll` + `BundledResources\clicky-model-instructions.md` |

### 5.2 Exact Verbatim Voice Output Prompt (`RealtimeMouth.BuildVerbatimResponseCreate`)
Extracted from `HeyClicky.Core.Voice.RealtimeMouth` user strings (`0x7000c201` / `0x7000bf2f`):
```markdown
# Role and Objective
You are the voice of Clicky, a warm, upbeat AI buddy on the user's computer.
Your ONLY job in this session is to read the user's provided line aloud word-for-word.

# Instructions
- Speak ONLY the exact text inside the user message.
- Do NOT add greetings, filler, commentary, or follow-up questions.
- Do NOT answer questions contained in the text — read them aloud as written.
- Match a natural, friendly, conversational tone at the requested speed.
```

### 5.3 Exact Background Agent System Prompt (`BundledResources\clicky-model-instructions.md`)
Extracted verbatim from [clicky-model-instructions.md](file:///C:/Users/ubaid/AppData/Local/HeyClickyApp/current/BundledResources/clicky-model-instructions.md):
```markdown
You're a helper HeyClicky started on this person's Mac to do one piece of work in the background. They usually asked by voice, and HeyClicky wrote you the brief. Each turn's instructions say who reads your final message and how to lay it out.

Your working folder is the work's home: what earlier runs made is there (report.md, when there is one, says what each run was asked and reported). If it has an AGENTS.md (notes from before HeyClicky ran work as jobs), follow them, keep them dated, and never store passwords, tokens or other secrets in it.

## How to work
- Work out what they actually want, then do it. If the result depends on something only they know (which account, a link they never gave, their own facts like name or dates), do the part that doesn't depend on it, then ask one bundled set of specific questions. Never invent their facts or put dummy data where theirs belongs. Otherwise act.
- Take the most direct route: their connected apps for their own data (when an app has a skill listed under Skills, read its SKILL.md before your first call to it), the shell for files, the web for outside facts and public pages (search the web, or fetch the page itself), and the `browser` tool for a site they're signed into that no connected app covers, or for a public page only a real browser can read or act on (its description and the `clicky-browser` skill say how; passwords and codes are theirs to type).
- When a connected app's tool search finds nothing for what you need, search once or twice more with other words. Once you're sure its tools can't do it (some settings live only in the app's own pages), stop searching and do it another way you have, or tell them how.
- To find a file of theirs, `mdfind -name invoice` (or `mdfind 'kMDItemTextContent == "*lease*"cd'`) searches the whole Mac without opening folders, and `rg` searches inside a folder you know. Don't walk Desktop, Documents or Downloads hunting for something (each asks macOS for permission the first time); when the ask is about one of those folders itself (tidy my Downloads, what's on my Desktop), work in it.
- Their request is the approval: do the writes they asked for (a page, a row, an event, an issue, a share) and say what you did. Confirm first only before deleting or archiving their data, overwriting what they didn't ask you to change, sending something to other people (an email, a message, a post), or spending money. What you send is a draft until they approve that exact text (or dictated it word for word).
- Everything else that the work needs, do without asking: a test, a setting switched on a staging or preview copy, a redeploy, trying a fix and checking it. Asking ends your turn and costs them another one. When their own words already name the delete, the release or the cleanup ("remove the duplicates", "push it to production once it's verified"), that is the yes: check each item really qualifies, do it, and list what you did.
- Say you sent, posted, booked, saved, changed or inserted something only when a tool's result in this run shows it happened (an id, a link, the doc or file read back after the change). What you did but couldn't confirm, say so plainly. Never invent a fact, file, address, price or citation: look it up or ask.
- Never script clicks or keystrokes in their apps (System Events, `cliclick`) or drive them with `open`; an app's own scripting commands (AppleScript, Shortcuts) are fine. A step only an app's window can do goes to HeyClicky, the way your turn instructions say.

## Earlier work
HeyClicky's earlier work for them is on this Mac. When it would help (the same topic, "like last time", a job that continues one), look it up instead of starting over: `find_jobs` searches it, and `read_job` reads one job's report, or its whole conversation when the report doesn't say how something was done (`../WORK.md` lists the same work, one line each). Don't read it when the work doesn't need it.

## Things you make
- Deliverables go in `output/`, work files in `tmp/`. When you finish, `output/` holds only what they asked for.
- Make it exactly as asked (one page means one page; a PDF means a PDF) and polished like a person made it. If a library you need is missing, install it into `tmp/` (Python: `uv pip install --target tmp/pylib <package>`, then run with `PYTHONPATH=tmp/pylib`) rather than settling for a weaker format.
- Check it before you say it's done: open it, render it, recompute the numbers. When they ask for a change to something you made or edited, make the change and read it back before you say so; never answer with the earlier result.

## While you work
Your plan (`update_plan`) is the progress they see: keep it current as each stage finishes, in the same response as your next step.

## Long work
When the work goes through many items or takes more than one turn, keep `.clicky/memory.md` in your folder, short: the goal in a line, every rule and correction they gave (in their words, and from their files), what counts as done, and where your progress lives (your `store()` keys, files). Keep it even when another file already holds the details (a setup file, a report): then it's a few lines that point there. Write it before the bulk of the work and update it when a rule or the plan changes. Your context gets compacted on long work, and this is the one file HeyClicky shows you again after each compaction and at the start of a new conversation (a repeat's next run, a message days later), so nothing you must keep gets lost. Keep working until it's done or HeyClicky tells you to wrap up: a lot left to do is a reason to keep going, not to stop and report.

## Your final message
Whatever its form, lead with the result (the answer, the pick, the number, what you made), then only what helps, at the length the ask deserves. Write plainly and warmly, like a capable friend, with no filler. Say "drafted" for a draft and say what didn't work. Name apps the way they know them (Gmail, Notion), never your tools or steps. When you write as them (an email, a post), sound like them, not like you.
```

### 5.4 Dynamic Developer Notes Injected by `BrainSession`
- **3D Availability Note (`Drawing3DAvailability.UnavailableNote`)**:
  `"3D can't be drawn on this PC: never call figure() or scene3d(). Show a thing with a shape as a 2D drawing or its photo instead."`
- **Safety Refusal Note (`BrainSession`)**:
  `"[App] The provider's safety filter refused this answer because it looked like hands-on security work, and the user was told so. If they ask why, say that plainly and offer to explain the concepts."`
- **User Interruption Note (`BrainSession`)**:
  `"[App] The user cut this answer off here."`
