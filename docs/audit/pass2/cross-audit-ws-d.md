# Pass 2 Independent Cross-Audit Report — WS-D (OneBrain, Vision, Screen Layout & V8 Sandbox)

- **Cross-Auditor:** `ws-c-backend`
- **Target Workstream:** `WS-D` (`ws-d-brain-vision`)
- **Primary Report:** [`docs/audit/ws-d/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-d/REPORT.md), [`docs/audit/ws-d/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-d/findings.json), [`docs/audit/ws-d/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-d/coverage.json)
- **Cross-Audit JSON:** [`docs/audit/pass2/cross-audit-ws-d.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-d.json)

---

## 1. Executive Summary

We independently re-verified all **8 Pass 1 findings** (`AUD-D-001` through `AUD-D-008`) against [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts), [`src/backend/src/utils/screenLayout.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts), [`src/desktop/Kursor.Drawing/`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/), [`src/desktop/Kursor.Drawing3D/`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/), [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/), and the evidence artifacts in [`docs/audit/evidence/ws-d/`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/).

- **Pass 1 Verdicts:** **8 / 8 Confirmed** (`7 CONFIRMED`, `1 CONFIRMED_WITH_REFINEMENT`, `0 DISPUTED`).
- **New Findings Added in Pass 2:** **4** (`AUD-D-009` [High], `AUD-D-010` [Medium], `AUD-D-011` [Medium], `AUD-D-012` [Low]).

---

## 2. Verdicts on Pass 1 Findings (`AUD-D-001` .. `AUD-D-008`)

| Finding ID | Severity | Verdict | Independent Verification & Technical Refinements |
| :---: | :---: | :---: | --- |
| **`AUD-D-001`** | High | **CONFIRMED** | Verified at `brain.ts:433, 578-601, 660-705, 958-962` and `model-provider-verification.json`: (1) all 4 explicit `:free` models at lines 700–704 return HTTP 404 on OpenRouter; (2) `"baidu/ernie-5.5-flash-preview:free"` at line 601 is 100% unreachable dead code because `requestedModel` defaults to `"anthropic/claude-sonnet-4.6"` at line 962; (3) `ONEBRAIN_MODEL` in `README.md` is ignored; (4) `runGroqVisionEye` failure on screenshot turns retries `qwen/qwen3.8-27b` twice, disables `groq-120b` (`line 580`), and calls `qwen/qwen3.8-27b` a 3rd time at line 590; (5) `payload.tools` is omitted on all Groq and OpenRouter calls (`lines 661-668`). |
| **`AUD-D-002`** | High | **CONFIRMED** | Verified at `screenLayout.ts:68-82, 144-510` and `brain.ts:385, 976`: `analyzeScreenLayout` runs synchronously on the main Node.js thread before the `ReadableStream` SSE starts, decoding JPEGs via pure-JS `jpeg-js` without `maxResolutionInMP` or `maxMemoryUsageInMB` caps and blocking the event loop for `423 ms` p50 / `750 ms` p95 on `1280x720`, `1.36 s` on `1080p`, `2.18 s` on `1440p`, and `6.94 s` on `12 MP` JPEGs. |
| **`AUD-D-003`** | High | **CONFIRMED** | Verified at `screenLayout.ts:690-698` and `snap-program-adversarial.json` (`SNAP-01`, `SNAP-02`, `SNAP-03`): Case B's `/^[ \t]*(?:receipt|clipping)\s*\([\s\S]*?\)\s*;?[ \t]*$/gm` spans across newlines when a `receipt(...)` line has a trailing `// comment` (wiping out subsequent `step(2)`, `say()`, and `check()` calls up to the next line ending in `);`), or stops early on inner `)` in multiline calls and leaves a fatal `SyntaxError`. |
| **`AUD-D-004`** | High | **CONFIRMED** | Verified at `NativeEngine.cs:145-148, 249-301, 347-350, 583-588, 605-649`, `DrawingThread.cs:191-231`, `clicky-draw-native.js:650-652, 835-851`, and `ClickyThreeKernel.cs:290, 349`: zero `v8.Interrupt()` watchdog timers exist anywhere in `NativeEngine` or `ClickyThreeKernel`, `DrawingThread.Invoke` blocks indefinitely on `done.Wait()` (`line 220`), `ClickyThreeKernel.cs:290` omits `V8RuntimeConstraints`, and in `clicky-draw-native.js:835-851` (`clickyCanvas.tick3d`), `if (Date.now() - started > 8)` runs only *after* `fn(...)` returns. |
| **`AUD-D-005`** | High | **CONFIRMED_WITH_REFINEMENT** | Core finding confirmed (`__hostRaw` / `__host` exposed on `globalThis`, `with (__api) { return eval(__c); }` in shared global context, unbounded timer creation, and unguarded global/prototype mutation). **Two precision refinements from C# inspection:**<br>1. `HostBridge.cs:7` inherits from `V8FastHostObject<HostBridge>` (not a reflection-exposed CLR object), so it does **not** expose `.Speak()`, `.PlaySound()`, or `.GetType()`; instead, it exposes fast-proxy methods `schedule`, `cancel`, `log`, `caption`, `markSound`, `buttonReady`, `shape3d`, `textWidth`, `textBox`, `setPath`, `pathLength`, `pathPoint`, `pathBox`, `inkFor`, `busyFor`, `lumFor`, `figuresFloat`, and `r0`..`r3`.<br>2. `NativeEngine.Clear()` is at `NativeEngine.cs:575-581` (not `296-306`) and **does** call `CancelAllTimers()` on C# `timerTokens` (`line 579`); however, `clearNow()` in `clicky-draw-native.js` never clears the JS `timerCallbacks` `Map` (`line 25`), leaking all timer callback closures in V8 memory across turns. |
| **`AUD-D-006`** | Medium | **CONFIRMED** | Verified at `screenLayout.ts:95-515` and `screen-layout-benchmark.json`: `35.48%` strict table recall (`11/31`), `37.50%` strict answer-line recall (`6/16`), `0%` recall on solid underline and dashed worksheet blanks (causing Case A to overwrite valid inline `write()` calls with a floating `receipt()` card), 2 false-positive tables, and off-screen `calmCardSpot` (`x: 860`) on `640x480` screens. |
| **`AUD-D-007`** | Medium | **CONFIRMED** | Verified at `screenLayout.ts:645-880` and across all 19 failed cases in `snap-program-adversarial.json`: Case A produces `const w2 = ;` `SyntaxError` when a 2nd `write()` call is assigned to a variable (`line 685`), `hasTableMarks`/`hasCardAlready` match inside comments and `say()` strings (`lines 646-648`), options containing `)` break `writeFullRegex`/`point` regexes, RISC/CISC column indices are hardcoded (`lines 721-724`), ViT `720/565` rescaling corrupts valid 720p Y coordinates, and lines 829–832 discard answer lines above tables. |
| **`AUD-D-008`** | Medium | **CONFIRMED** | Verified at `brain.ts:512-556, 1039-1088, 1177-1295`: `/brain/compact` never persists to SQLite `threads.messages_json` and emits `note_item` with `role: "developer"` (`line 1224`) which `/brain/turn` drops at `line 517`; assistant turns without `say()` are dropped from history (`lines 538-550`); `tryCallUpstreamLlm` never populates `computerCalls`; and `/brain/session-end` performs zero memory consolidation. |

---

## 3. New WS-D Findings Discovered in Pass 2 (`AUD-D-009` .. `AUD-D-012`)

### `ID: AUD-D-009`
- **Severity:** High
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `POST /brain/turn` Drops All `function_call`, `function_call_output`, and `developer` Context Items From Thread History, Breaking Multi-Round Tool Execution
- **Component:** OneBrain Tool Continuation & Thread History (`brain.ts` vs `BrainSession.cs`)
- **Location:** `src/backend/src/routes/brain.ts:272-356, 512-556`, `src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs:463, 572, 616, 658-677, 732-750`
- **Description:** When the C# desktop client executes a tool call emitted by `/brain/turn` (`BrainSession.AppendToolResult` at `BrainSession.cs:732-750` and `CommitTurn` at `lines 658-677`), it appends `{ "type": "function_call", "call_id": ..., "name": ..., "arguments": ... }` and `{ "type": "function_call_output", "call_id": ..., "output": ... }` to `thread` and sends a continuation turn to `POST /brain/turn`. However, `tryCallUpstreamLlm` (`brain.ts:516-517`) only forwards items matching `item.type === "message" && (item.role === "user" || item.role === "assistant")`, silently dropping 100% of `function_call` and `function_call_output` items! In addition, `BrainSession.CommitTurn` records per-turn UIA/window context notes (`note` at line 572), web search results (`searchRecords` at line 616), and pending app notes (`line 463`) as `Message("developer", ...)`, all of which `brain.ts:517` also drops on subsequent turns.
- **Reproduction Steps:**
  1. Inspect `BrainSession.AppendToolResult` (`BrainSession.cs:732-750`) and `BrainSession.CommitTurn` (`lines 572, 616, 658-677`).
  2. Inspect `extractPromptInput` (`brain.ts:272-356`) and `tryCallUpstreamLlm` (`brain.ts:512-556`).
- **Observed Result:** On any tool-result continuation turn, `brain.ts` drops all `function_call` and `function_call_output` items from `messages` and re-sends only the prior user message, so the upstream LLM never receives the output of the tool it called.
- **Expected Result:** `tryCallUpstreamLlm` and `extractPromptInput` must convert `function_call`, `function_call_output`, and `role === "developer"` (search records / turn context notes) items in `thread` into OpenAI-compatible assistant `tool_calls` / `role: "tool"` (or structured context messages) so multi-round tool calls and search records reach the LLM.
- **Impact:** Multi-round tool execution (`fetch_web`, `bash`, `gmail_draft`, `calendar_list`, etc.) and search record persistence in `BrainSession` fail to deliver tool outputs to the LLM on continuation turns.
- **Remediation:** In `src/backend/src/routes/brain.ts:512-556`, map `item.type === "function_call"` and `item.type === "function_call_output"` (and `item.role === "developer"` notes) into `messages` instead of filtering solely for `user`/`assistant` `message` items.
- **Effort:** `M`
- **Evidence:** [`src/backend/src/routes/brain.ts:512-556`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L512-L556), [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs:572-750`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs#L572-L750)

---

### `ID: AUD-D-010`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `BrainReplyAccumulator` Compares Unstripped `lastReportedCode.Length` Against URL-Stripped `text3.Length` in `Finish()`, Dropping Final Code Updates When Links Are Stripped
- **Component:** Desktop OneBrain Stream Accumulator (`BrainReplyAccumulator.cs` & `ProgramText.cs`)
- **Location:** `src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs:228-250`, `src/desktop/Kursor.Core/HeyClicky.Core.Brain/ProgramText.cs:125-213`
- **Description:** During SSE streaming (`ReportCodeIfItGrew` at `BrainReplyAccumulator.cs:243-249`), `lastReportedCode` is assigned the raw `ProgramText.ProgramCode(rawText, isComplete: false)` *before* `ProgramText.WithoutLinks(text)` strips URLs/markdown links, so `lastReportedCode.Length` includes the length of any stripped URLs. When the stream finishes (`Finish()` at `lines 229-235`), `text3` is computed *after* `ProgramText.WithoutLinks(...)` and compared via `if (text3.Length > 0 && text3.Length > lastReportedCode.Length)`. If `WithoutLinks` stripped a URL or markdown link whose character length exceeds the final streamed chunk length, `text3.Length <= lastReportedCode.Length`, and `Finish()` silently skips `onCodeUpdate?.Invoke(text3)`, leaving the live drawing engine with an incomplete program.
- **Reproduction Steps:**
  1. Compare `BrainReplyAccumulator.ReportCodeIfItGrew` (`lines 243-249`) with `BrainReplyAccumulator.Finish` (`lines 229-235`).
  2. Trace a streamed program containing a URL outside strings (e.g., a comment `// https://example.com/very/long/path`) where the final chunk is `< 35` chars.
- **Observed Result:** `ReportCodeIfItGrew` stores the pre-`WithoutLinks` string in `lastReportedCode`, while `Finish()` compares the post-`WithoutLinks` `text3.Length` against `lastReportedCode.Length`, skipping the final `onCodeUpdate` callback when `text3.Length <= lastReportedCode.Length`.
- **Expected Result:** `ReportCodeIfItGrew` must assign `lastReportedCode = ProgramText.WithoutLinks(text)` so both streaming and completion compare post-`WithoutLinks` strings (or `Finish()` should check `text3 != lastReportedCode`).
- **Impact:** Streaming programs from which `WithoutLinks` strips URLs can drop their final `onCodeUpdate` invocation at stream completion.
- **Remediation:** In `BrainReplyAccumulator.ReportCodeIfItGrew` (`line 246`), set `string stripped = ProgramText.WithoutLinks(text);` and store `stripped` in `lastReportedCode`, and in `Finish()` (`line 231`) check `if (text3.Length > 0 && text3 != lastReportedCode)`.
- **Effort:** `S`
- **Evidence:** [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs:228-250`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs#L228-L250)

---

### `ID: AUD-D-011`
- **Severity:** Medium
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `POST /brain/turn` Always Wraps Output in Fenced ```` ```js ```` Blocks, Defeating `BrainReplyAccumulator.SpeechOnly` Mode
- **Component:** OneBrain `SpeechOnly` Mode Contract (`brain.ts` vs `BrainReplyAccumulator.cs`)
- **Location:** `src/backend/src/routes/brain.ts:36-41, 249, 682`, `src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs:37, 58, 215-226`
- **Description:** `BrainReplyAccumulator` (`BrainReplyAccumulator.cs:37, 58, 215-226`) supports a `SpeechOnly` mode where `Finish()` checks `if (SpeechOnly && !text.Contains("```", StringComparison.Ordinal))` to populate `Reply.SpokenSentence` and wrap plain spoken prose into `SayLine(...)`. However, `POST /brain/turn` (`brain.ts:249, 682`) unconditionally wraps every upstream and fallback response in ```` ```js ... ``` ```` fences regardless of request flags. Consequently, `!text.Contains("```")` is always `false` and `Reply.SpokenSentence` is never populated.
- **Reproduction Steps:**
  1. Inspect `BrainReplyAccumulator.Finish()` at `BrainReplyAccumulator.cs:215-226`.
  2. Inspect `tryCallUpstreamLlm` (`brain.ts:682`) and `buildLocalFallbackProgram` (`brain.ts:249`).
- **Observed Result:** Because `brain.ts` always wraps output in ```` ```js ```` fences, `BrainReplyAccumulator.Finish()` never enters the `SpeechOnly` branch and leaves `Reply.SpokenSentence = null`.
- **Expected Result:** When `SpeechOnly` is active (or in `BrainReplyAccumulator.Finish()`), extract `Reply.SpokenSentence` from `SayLines.InOrder(Reply.Code)` even when ```` ```js ```` fences are present.
- **Impact:** `Reply.SpokenSentence` is always `null` when consuming `/brain/turn` responses.
- **Remediation:** Populate `Reply.SpokenSentence` in `BrainReplyAccumulator.Finish()` via `SayLines.InOrder(Reply.Code)` when `SpeechOnly` is true and `text` contains code fences.
- **Effort:** `S`
- **Evidence:** [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs:215-226`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainReplyAccumulator.cs#L215-L226), [`src/backend/src/routes/brain.ts:249, 682`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L249)

---

### `ID: AUD-D-012`
- **Severity:** Low
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** Screenshot Reference Deduplication Protocol (`clicky_file` / `screenshot_file` / `409 screenshot_files_unavailable`) Is Unimplemented in `POST /brain/turn`
- **Component:** OneBrain Screenshot Reference Protocol (`brain.ts` vs `BrainSession.cs` / `BrainClient.cs`)
- **Location:** `src/backend/src/routes/brain.ts:318-378, 1131-1147`, `src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainClient.cs:77-80, 174-177`, `src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs:580-584, 1097-1126`
- **Description:** `BrainSession.cs` and `BrainClient.cs` implement a screenshot-by-reference protocol to avoid re-uploading up to 32 full base64 screenshots on every turn: the worker returns `screenshot_file` in `done` (`BrainReplyAccumulator.cs:118`), `BrainSession.ThreadForWorker` (`lines 1097-1126`) replaces `image_url` with `{"type": "input_image", "clicky_file": "..."}` on subsequent turns, and if a referenced `clicky_file` is missing on the worker, the worker returns HTTP 409 or SSE error `screenshot_files_unavailable` (`BrainClient.cs:77, 174`) so the client falls back to sending full `image_url` payloads. `src/backend/src/routes/brain.ts` never emits `screenshot_file` in `done` (`lines 1131-1147`) and `extractPromptInput` (`lines 318-378`) silently ignores `input_image` blocks that contain `clicky_file` without `image_url` instead of returning `409 screenshot_files_unavailable`.
- **Reproduction Steps:**
  1. Inspect `BrainSession.ThreadForWorker` (`BrainSession.cs:1097-1126`) and `BrainClient.StreamTurnAsync` (`BrainClient.cs:77-80`).
  2. Send a `POST /brain/turn` request containing `{"type": "input_image", "clicky_file": "scr_missing_01"}` in `thread` and inspect `extractPromptInput` (`brain.ts:318-378`).
- **Observed Result:** `brain.ts` silently ignores `clicky_file` blocks without returning `409 screenshot_files_unavailable` and never issues `screenshot_file` IDs in `done`, forcing the client to re-transmit and SQLite to re-store full base64 screenshots on every turn.
- **Expected Result:** Cache uploaded turn screenshots by hash/ID (`screenshot_file`) and resolve `clicky_file` references in `extractPromptInput`, returning HTTP 409 `{"error": "screenshot_files_unavailable"}` if a referenced file ID is not found.
- **Impact:** Multi-turn sessions re-upload up to 32 full base64 screenshots on every turn, and any `clicky_file`-only thread item is silently dropped instead of triggering the client's 409 fallback retry.
- **Remediation:** Implement an in-memory/disk screenshot cache keyed by `screenshot_file` in `src/backend/src/routes/brain.ts`, return `screenshot_file` in the `done` SSE event, and return HTTP 409 `screenshot_files_unavailable` when an unknown `clicky_file` is encountered.
- **Effort:** `M`
- **Evidence:** [`src/backend/src/routes/brain.ts:318-378`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L318-L378), [`src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs:1097-1126`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/BrainSession.cs#L1097-L1126)
