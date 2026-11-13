# Pass 2 Independent Cross-Audit — WS-E (Voice: Ears, Mouth, Dictation, Hotkeys & Text Injection)

- **Cross-Auditor**: `ws-fg-drawing-2d-3d`
- **Primary Auditor**: `ws-e-voice-dictation`
- **Primary Report Audited**: [`docs/audit/ws-e/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-e/REPORT.md), [`docs/audit/ws-e/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-e/findings.json), [`docs/audit/ws-e/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-e/coverage.json)
- **Date**: 2026-11-13

---

## 1. Executive Summary

We independently re-verified all **12 Pass 1 findings (`AUD-E-001` through `AUD-E-012`)** line-by-line against:
- Backend voice/dictation routes: [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts)
- Desktop Voice (`Kursor.Core`): [`GeminiTranscriber.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs), [`DeepgramTranscriber.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs), [`RealtimeMouth.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/RealtimeMouth.cs), [`CompanionSession.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs)
- Desktop Dictation (`Kursor.Core`): [`DictationController.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs), [`DictationTranscriptionClient.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationTranscriptionClient.cs), [`DictationCorrectionDetector.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationCorrectionDetector.cs)
- Desktop Platform (`Kursor.Platform`): [`TextInsertion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs), [`DictationTextTarget.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/DictationTextTarget.cs), [`ShortcutMonitor.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs)

### Summary of Cross-Audit Verdicts

| Pass 1 Finding ID | Severity | Verdict | Notes |
|---|---|---|---|
| `AUD-E-001` | Critical | **CONFIRMED** | Exact line match in `voice.ts:231-311, 385-408`. |
| `AUD-E-002` | High | **CONFIRMED** | Exact line match in `voice.ts:53-84`, `GeminiTranscriber.cs:26, 356-360, 515`, `DictationController.cs:499-589`. |
| `AUD-E-003` | High | **CONFIRMED** | Exact line match in `voice.ts:690-827`, `DictationController.cs:567-570`, `CompanionSession.cs:1570-1586`. |
| `AUD-E-004` | Medium | **CONFIRMED** | Exact line match in `TextInsertion.cs:86-174, 238-250, 419-437`, `DictationTextTarget.cs:440-479`. |
| `AUD-E-005` | Medium | **CONFIRMED** | Confirmed in `voice.ts:504-552` and `DictationTranscriptionClient.cs:151-185`. Also noted comment vs model mismatch (`llama-3.3-70b-versatile` comment at line 453 vs `openai/gpt-oss-120b` at line 532). |
| `AUD-E-006` | Medium | **CONFIRMED** | Exact line match in `ShortcutMonitor.cs:765-791, 833-890`. |
| `AUD-E-007` | Medium | **CONFIRMED** | Exact line match in `voice.ts:459-480`. |
| `AUD-E-008` | Low | **CONFIRMED** | Exact line match in `TextInsertion.cs:186-208`. |
| `AUD-E-009` | Low | **CONFIRMED** | Confirmed eager spawn at `voice.ts:314-320` without `process.on('exit'\|'SIGINT'\|'SIGTERM')` cleanup. (Minor precision note: `warmSynthProc.on("exit")` at `voice.ts:281-283` *does* drain `warmSynthQueue`.) |
| `AUD-E-010` | Low | **CONFIRMED** | Exact line match in `ShortcutMonitor.cs:793-831, 878-881`. |
| `AUD-E-011` | Low | **CONFIRMED** | Exact line match in `DictationCorrectionDetector.cs:14-99`, `DictationController.cs:950`. |
| `AUD-E-012` | Medium | **CONFIRMED** | Exact line match in `voice.ts:18-84, 146-199, 705`, `RealtimeMouth.cs:345-378`. |

In addition, our Pass 2 cross-audit uncovered **3 new findings (`AUD-E-013`, `AUD-E-014`, `AUD-E-015`)** missed in Pass 1.

---

## 2. Re-Verification of Pass 1 Findings (`AUD-E-001` .. `AUD-E-012`)

### `AUD-E-001` (Critical) — Warm Windows SAPI PowerShell Worker Queue Desynchronization & Dual-Process Race
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - In [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L231-L311), `synthesizeViaWarmWindowsSapi(text)` pushes `{ b64Text, resolve }` onto `warmSynthQueue` (line 309) after registering `const timer = setTimeout(() => resolve(null), 7000)` (line 303).
  - When the 7000 ms timeout fires, `resolve(null)` is called, but:
    1. If the request had not been shifted yet, it remains in `warmSynthQueue`.
    2. If `pumpWarmSynthQueue` (`voice.ts:289-299`) had already shifted it and written `item.b64Text` to `warmSynthProc.stdin`, `warmSynthBusy` remains `true`, and `warmSynthProc.stdout`'s `rl.on("line")` handler (`voice.ts:269-277`) is replaced on every `pumpWarmSynthQueue()` call (`warmSynthQueue.shift()` at line 292!).
    3.Wait — look even closer at `voice.ts:269-299`! In `ensureWarmSynthWorker()`, `rl.on("line")` at lines 269-277 calls `const current = warmSynthQueue.shift();` AND `pumpWarmSynthQueue()` at line 292 ALSO calls `const item = warmSynthQueue.shift();` and attaches `warmSynthProc.stdout?.once("data", ...)`! Having **both** `rl.on("line")` (which shifts `warmSynthQueue`) AND `pumpWarmSynthQueue` (which shifts `warmSynthQueue` and attaches a raw `stdout.once("data")` listener on chunk boundaries rather than newline boundaries!) is an even worse double-shift/chunk-split bug in `voice.ts:269-299`!
  - Furthermore, when `synthesizeViaWarmWindowsSapi` returns `null`, `synthesizePcm24kMono` (`voice.ts:385-408`) immediately spawns a second one-shot `powershell.exe` while `warmSynthProc` is still synthesizing.

### `AUD-E-002` (High) — Dictation Streaming Pipeline Always Fails in Local Mode & Batch Fallback Bypasses Cleanup
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L53-L84) returns `process.env.GEMINI_API_KEY?.trim() || "gem_kursor_local_dev_token"` as `access_token`.
  - [`GeminiTranscriber.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs#L356-L360) checks `if (session.Token != null && session.Token.StartsWith("gem_kursor_", StringComparison.Ordinal))` and immediately calls `Fail("Local Groq Whisper batch transcription active.")`.
  - Even when `GEMINI_API_KEY` (`AIza...`) is set, `GeminiTranscriber.ConnectAsync` (`GeminiTranscriber.cs:26, 515`) connects to `wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContentConstrained` with `Authorization: Token <token>`, which requires a minted ephemeral `auth_tokens/...` token and rejects raw `AIza...` keys.
  - [`DictationController.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs#L562-L589) (`RunBatchAsync`) calls `batchClient.TranscribeAsync()` (`POST /v2/dictation/transcribe`) and passes the raw result straight to `DeliverAsync(token, item, usedBatch: true, ...)` without calling `batchClient.CleanUpAsync(...)`, while `handleTranscribe` (`voice.ts:648-827`) also returns raw STT output without LLM cleanup.

### `AUD-E-003` (High) — `POST /v2/dictation/transcribe` and `/brain/ears/transcribe` Return Hardcoded Fake Transcript on Fallback
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L690-L827) guards Groq (`line 690`), Deepgram (`line 725`), and OpenAI (`line 783`) behind `audioBuffer.byteLength > 44`. When `audioBuffer.byteLength <= 44` or when all configured STT providers fail, lines 823-826 return HTTP 200 `{ text: "Hello from Kursor local dictation transcription.", stt_ms: ... }`.
  - Both [`DictationController.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs#L567-L570) and [`CompanionSession.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/CompanionSession.cs#L1570-L1586) treat this HTTP 200 payload as genuine user speech.

### `AUD-E-004` (Medium) — `TextInsertion` Clipboard Restoration Race & `OpenClipboard` Contention
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`DictationTextTarget.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/DictationTextTarget.cs#L440-L479) only constructs a non-null `landed` delegate when UIA `ReadValue` (`ValuePattern` / `TextPattern`) returns a non-null string. For Electron/Chromium/Monaco/terminal fields where `ReadValue` returns `null`, `landed` is `null`.
  - In [`TextInsertion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs#L86-L174), when `landed == null`, `verification` is set to `"unverifiable"` (line 113), and after waiting only `minimumRestoreDelay` (`DictationRestoreDelay` = 250ms, or 350ms in `PasteTextAsync`), lines 150-161 restore the previous clipboard (`PInvoke.EmptyClipboard()`), racing with slow target applications processing the synthesized `Ctrl+V`.
  - `OpenClipboardWithRetry` (`TextInsertion.cs:238-250`) only retries 20 × 10ms = 200ms, and `RestoreClipboard` (`TextInsertion.cs:419-437`) permanently loses the user's clipboard snapshot if `OpenClipboardWithRetry()` returns `false`.

### `AUD-E-005` (Medium) — `POST /v2/dictation/cleanup` Ignores 7 of 9 `DictationCleanupContext` Fields & Has Prompt Injection Risk
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`DictationTranscriptionClient.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationTranscriptionClient.cs#L151-L185) serializes `context` with `app_name`, `before_text`, `selected_text`, `after_text`, `single_line`, `site_host`, and `known_terms`.
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L504-L517) only reads `body.app_context` or `contextObj.process_name` / `contextObj.window_title` (neither of which is sent by the C# client), discarding all 7 fields actually sent.
  - Also verified that `voice.ts:512-516` concatenates `rawText` without delimiter escaping, and `voice.ts:453` comments `llama-3.3-70b-versatile` while `voice.ts:532` actually sends `model: "openai/gpt-oss-120b"`.

### `AUD-E-006` (Medium) — `ShortcutMonitor` Lock-Free Ring Buffer Race Condition
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`ShortcutMonitor.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs#L765-L791) (`Enqueue`, `EnqueueClick`, `EnqueueRecording`) writes to 64-element arrays without checking if `ringWriteIndex - ringReadIndex >= 64`, and `EnqueueClick` can be called concurrently from the hook thread (`OnMouseButton`, line 743) and UI thread (`NotifyOverlayMouseButtonDown`, line 179), racing on `ringWriteIndex`.

### `AUD-E-007` (Medium) — `localCleanupHeuristic` Returns Raw Filler Words When Input Is 100% Fillers & Appends Unconditional Period
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L459-L480) has `if (!cleaned) return raw.trim();` at line 469 (returning `"um"` when input is `"um"`), capitalizes `cleaned.charAt(0)` at line 472, and appends `.` at lines 475-477 whenever `!/[.!?…]$/.test(cleaned)`.

### `AUD-E-008` (Low) — `TextInsertion.ReplacingEmDashesForInsertion` Unconditionally Replaces Em-Dashes
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`TextInsertion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs#L186-L208) unconditionally replaces `[ \t]*—[ \t]*` with `", "` and strips leading `^, ` across all target applications (`DictationTextTarget.cs:414`).

### `AUD-E-009` (Low) — Orphaned Warm PowerShell Process on Backend Exit
- **Verdict**: **CONFIRMED** (with minor precision note)
- **Independent Verification**:
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L314-L320) spawns `powershell.exe` at module import time on Windows and registers no `process.on('exit' | 'SIGINT' | 'SIGTERM')` handlers to kill `warmSynthProc`.
  - *Precision note*: `warmSynthProc.on("exit")` at `voice.ts:278-284` *does* drain `warmSynthQueue` (`while (warmSynthQueue.length > 0) { warmSynthQueue.shift()?.resolve(null); }`), so the child-exit queue drain is present in current code, whereas the missing parent process exit/signal cleanup is 100% confirmed.

### `AUD-E-010` (Low) — `ShortcutMonitor` Synthesized `VK__none_` (`0xFF`) Start-Menu Mask Key
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`ShortcutMonitor.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs#L793-L831) (`MaskStartMenu` / `SendMaskKey`) injects `VIRTUAL_KEY.VK__none_` (`0xFF`) on every `DictationDown` when `Win` is part of the dictation chord (`lines 878-881`), even if the user is pressing a standard Windows shortcut (`Win+Ctrl+Left`, `Win+Shift+S`) that immediately triggers `DictationCancel` (`lines 429-433`).

### `AUD-E-011` (Low) — `DictationCorrectionDetector` Fails on Multi-Word Corrections, Lowercase Terms, or Fields > 20,000 Chars
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`DictationController.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs#L950) aborts when `baseline.Length > 20000`, and [`DictationCorrectionDetector.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationCorrectionDetector.cs#L18-L99) requires identical whitespace-split token counts, a single changed token index, and `IsDistinctiveLearnableWord` (which rejects all-lowercase words without `_` or internal uppercase letters).

### `AUD-E-012` (Medium) — Provider Env Vars Ignored, 3-Letter Languages Dropped, Keyterms Omitted, and OpenAI Realtime Fallback Returns Raw `sk-...` Key
- **Verdict**: **CONFIRMED**
- **Independent Verification**:
  - [`src/backend/src/routes/voice.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/voice.ts#L18-L84) ignores `EARS_PRIMARY_PROVIDER`, `DICTATION_CLEANUP_PROVIDER`, `body.keyterms`, and `body.language`.
  - `voice.ts:705` checks `normalizedLang.length === 2`, dropping 3-letter codes (`"yue"`, `"fil"`).
  - `voice.ts:193-199` (`handleRealtimeSession`) returns `client_secret: { value: openAiKey }` (`"sk-..."`) when minting `gpt-realtime-2.1-mini` fails, which bypasses `RealtimeMouth.cs:355` (`Secret.StartsWith("ek_kursor_") || Secret.StartsWith("gem_tts_")`) and causes `RealtimeMouth` to fail connecting to `wss://api.openai.com/v1/realtime` instead of falling back to `/agent/realtime/speak`.

---

## 3. Newly Discovered Findings in WS-E (Pass 2)

### ID: `AUD-E-013`
- **Workstream**: WS-E (Dictation Controller & Context Keyterms)
- **Severity**: Medium
- **Category**: Spec Drift / Context Keyterm Loss
- **Status**: `FAIL`
- **Title**: `DictationController.SetUpSessionAsync` drops UIA context keyterms (`BeforeText`/`SelectedText`/`AfterText`) on cold starts for `DictationTranscriptionClient` and on warm starts for `GeminiTranscriber`
- **File(s) & Line(s)**:
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs:359-430`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Dictation/DictationController.cs#L359-L430)
- **Description**:
  In `DictationController.StartSession()` (`lines 359-362`), `newClient.KnownTerms` is initialized to `readOnlyList` (which contains only user-configured `DictationCustomWords`). Next, `SetUpSessionAsync` (`lines 385-430`) captures UI Automation focus (`dictationFocus2`), extracts up to 50 context keyterms (`readOnlyList3`) from the focused field's `BeforeText`, `SelectedText`, and `AfterText` via `DictationVocabulary.ContextKeyterms(...)`, and merges them into `list = customWords.Concat(readOnlyList3).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList()` (`line 412`).
  However, lines 416-430 branch as follows:
  ```csharp
  if (adopted != null)
  {
      newClient.KnownTerms = list;
      transcriber = adopted;
  }
  else
  {
      transcriber = NewStreamingTranscriber(language, list, usageContext);
      transcriber.Start();
      lock (frameGate)
      {
          transcriber.PushAudio(newClient.AccumulatedAudio());
          streamer = transcriber;
      }
  }
  ```
  - When `adopted == null` (every cold start, and every session when warm sockets are unavailable), **`newClient.KnownTerms = list;` is omitted**! As a result, `newClient.KnownTerms` never receives the UIA context keyterms (`readOnlyList3`) for `/v2/dictation/cleanup` or `/v2/dictation/transcribe`.
  - Conversely, when `adopted != null` (warm socket), `adopted` was already minted during `PrepareWarmConnection()` (`lines 901-924`) with only `DictationCustomWords`, so the streaming `GeminiTranscriber` never receives the focused field's context keyterms (`readOnlyList3`).
- **Remediation**:
  Move `newClient.KnownTerms = list;` outside the `if (adopted != null)` branch so `newClient` always receives the merged custom + UIA context keyterms on both warm and cold sessions.
- **Effort**: `S`

---

### ID: `AUD-E-014`
- **Workstream**: WS-E (Voice / GeminiTranscriber)
- **Severity**: Medium
- **Category**: Concurrency / Premature Stream Termination
- **Status**: `FAIL`
- **Title**: `GeminiTranscriber.FinishAsync` sets `finishSent = true` before queued audio finishes uploading, causing intermediate `inputTranscription` events to truncate utterances
- **File(s) & Line(s)**:
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs:240-267`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs#L240-L267)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs:587-597`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs#L587-L597)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs:742-754`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/GeminiTranscriber.cs#L742-L754)
- **Description**:
  In `GeminiTranscriber.FinishAsync` (`lines 243-250`), `finishSent = true` is set inside `lock (gate)` at the moment the end-of-stream sentinel `new AudioFrame(null, 0)` is enqueued to `outgoing.Writer`, **before** `SendLoopAsync` has finished draining earlier `AudioFrame`s and sending `ActivityEnd` (`activityEndSent.TrySetResult(true)` at line 596).
  In `HandleMessage` (`lines 742-754`), whenever Gemini sends a `serverContent.inputTranscription` message for an earlier clause, it checks `if (finishSent)` (line 750) instead of checking `if (activityEndSent.Task.IsCompleted)`. If the user releases the hotkey while buffered audio frames are still in `outgoing` waiting to be sent and Gemini emits an `inputTranscription` for an earlier phrase, `HandleMessage` immediately resolves `delivered.TrySetResult(string.Join(" ", finals).Trim())` (line 752), causing `FinishAsync` (line 259) to return early and abort uploading the tail of the user's speech.
- **Remediation**:
  In `GeminiTranscriber.HandleMessage` (`line 750`), check `if (activityEndSent.Task.IsCompleted)` (or set `finishSent = true` only after `ActivityEnd` has been transmitted in `SendLoopAsync` at line 596) before resolving `delivered`.
- **Effort**: `S`

---

### ID: `AUD-E-015`
- **Workstream**: WS-E (Voice / DeepgramTranscriber)
- **Severity**: Low
- **Category**: Resource Leak (`ArrayPool<byte>.Shared`)
- **Status**: `FAIL`
- **Title**: `DeepgramTranscriber` leaks `ArrayPool<byte>.Shared` buffers when `BoundedChannelFullMode.DropOldest` drops frames and during `DisposeAsync`
- **File(s) & Line(s)**:
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs:54-60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs#L54-L60)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs:166-177`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs#L166-L177)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs:501-523`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Voice/DeepgramTranscriber.cs#L501-L523)
- **Description**:
  1. `DeepgramTranscriber` initializes its `outgoing` channel with `BoundedChannelFullMode.DropOldest` (`lines 54-60`), while `PushAudio` (`lines 166-177`) rents `byte[]` buffers from `ArrayPool<byte>.Shared`. When `outgoing` reaches capacity (e.g. while connecting or on a stalled socket), `TryWrite` always returns `true` and silently drops the oldest `OutgoingFrame` without returning its pooled `byte[]` to `ArrayPool<byte>.Shared`.
  2. In `DisposeAsync()` (`lines 501-523`), `lifetime.Cancel()` and `outgoing.Writer.TryComplete()` are called, but unlike `GeminiTranscriber.DisposeAsync()` (`lines 827-834`), `DeepgramTranscriber.DisposeAsync()` never drains remaining `OutgoingFrame` items from `outgoing.Reader` to return their pooled buffers, nor does it await `RunAsync` before calling `lifetime.Dispose()`.
- **Remediation**:
  Use `BoundedChannelFullMode.Wait` (matching `GeminiTranscriber.cs:56-61`), await the background `RunAsync` task in `DisposeAsync()`, and drain `outgoing.Reader` in `DisposeAsync()` to return all `item.Pooled` buffers to `ArrayPool<byte>.Shared`.
- **Effort**: `S`
