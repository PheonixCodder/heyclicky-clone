# WS-D Audit Report: OneBrain Pipeline, Vision, Screen-Layout Intelligence & Generated-JS V8 Safety

- **Workstream:** `WS-D` (`ws-d-brain-vision`)
- **Date:** 2026-11-13
- **Audited Scope:**
  - [`src/backend/src/routes/brain.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts) (`POST /brain/turn`, `POST /brain/compact`, `POST /brain/session-end`, `POST /brain/prewarm`, `runGroqVisionEye`, `tryCallUpstreamLlm`, `buildLocalFallbackProgram`)
  - [`src/backend/src/utils/screenLayout.ts`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts) (`analyzeScreenLayout`, `snapProgramToScreenLayout`)
  - [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs), [`HostBridge.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/HostBridge.cs), [`DrawingThread.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/DrawingThread.cs), [`clicky-draw-native.js`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-native.js), [`clicky-draw-engine.js`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js)
  - [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs), [`clicky-three-parts.js`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-three-parts.js)
- **Test Harnesses & Evidence Artifacts:**
  - [`tests/audit/ws-d/test-screen-layout.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-screen-layout.ts) (66-image screenshot corpus generator & benchmark + 35 adversarial `snapProgramToScreenLayout` test cases)
  - [`tests/audit/ws-d/test-brain-prompts.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-brain-prompts.ts) (live Groq/OpenRouter model catalog & completion probes, `/brain/*` endpoint contract tests, and 105-prompt matrix)
  - [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json)
  - [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json)
  - [`docs/audit/evidence/ws-d/model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json)
  - [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)
  - [`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json)

---

## 1. Executive Summary

| Metric | Result | Status |
|---|---|---|
| **`analyzeScreenLayout` Corpus Benchmark** | **66 images** tested (`tests/audit/ws-d/corpus/`): Table strict recall **35.48%** (`11/31`), any-table recall **51.61%** (`16/31`), **15 FN**, **2 FP**; Answer-line strict recall **37.50%** (`6/16`), **0%** on solid/dashed lines | `FAIL` |
| **`analyzeScreenLayout` Synchronous CPU Blocking** | `1280x720`: **p50 = 423.12 ms**, **p95 = 750.45 ms**, **max = 1203.68 ms**; `1920x1080`: **1362.25 ms**; `2560x1440`: **2182.75 ms**; `4000x3000` (12 MP): **6938.76 ms**; `8000x8000` (64 MP): **4212.50 ms** on main event loop | `FAIL` |
| **`snapProgramToScreenLayout` Adversarial Suite** | **35 adversarial JS programs** tested: **16 PASS**, **19 FAIL** (greedy `receipt()` regex wipes out subsequent `check()` calls; `const w2 = write(...)` produces `SyntaxError: const w2 = ;`; RISC/CISC swap; ViT Y-axis warp) | `FAIL` |
| **Hardcoded Model & Provider Fallback Audit** | All **4** explicit OpenRouter `:free` retry models (`gemma-3-27b-it:free`, `llama-4-maverick:free`, `qwen2.5-vl-72b-instruct:free`, `mistral-small-3.1-24b-instruct:free`) fail with **HTTP 404**; `baidu/ernie-5.5-flash-preview:free` (`line 601`) is **unreachable dead code**; `ONEBRAIN_MODEL` env var is ignored; `tools` omitted on all Groq & OpenRouter turns | `FAIL` |
| **OneBrain `/brain/turn` 105-Prompt Matrix** | **105/105** valid SSE event ordering (`meta` -> `status` -> `text` -> `tool_call`/`tool_calls` -> `computer_calls` -> `items` -> `done`); **104/105** JS syntax valid (`MATH-05` fails with `const w2 =` `SyntaxError`); **0%** filtering of `while(true){}`, `__hostRaw`, or `setInterval` floods | `FAIL` |
| **Generated-JS V8 Sandbox & Watchdog** | **No execution timeout or `v8.Interrupt()`** on `NativeEngine.Feed`, `RenderWhole`, `Evaluate`, `RunFigureTicks`, or `ClickyThreeKernel.Build`; `sink.deadline` is `0` (disabled); `globalThis.__hostRaw` exposed; `setTimeout`/`setInterval` uncapped and uncleared on `Clear()` | `FAIL` |

---

## 2. Detailed Benchmark & Test Matrices

### 2.1 `analyzeScreenLayout` 66-Image Corpus Benchmark

Generated and evaluated by [`tests/audit/ws-d/test-screen-layout.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-screen-layout.ts), with full per-image measurements recorded in [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json).

#### Summary Metrics
- **Table Detection (`31` positive images, `35` negative images):**
  - **Strict TP** (exact table count + row/col count match): `11 / 31` (**35.48%** strict recall, **84.62%** strict precision)
  - **Any-Table TP** (at least 1 table detected when expected): `16 / 31` (**51.61%** recall, **88.89%** precision)
  - **Partial Geometry / Count Mismatch:** `5` (`07_table_merged_header_span` 4 cols merged to 3; `17_table_near_top_y145_header_heuristic` forces `hasVisibleHeader = false` when `yMin < 175`, turning header row into Statement Row 1; `19_table_narrow_column_14px` merges `<20px` column borders; `22_table_two_side_by_side` and `23_table_two_stacked_vertically` drop the 2nd table because `screenLayout.ts:271-395` only keeps a single `bestGroup`)
  - **False Negatives (`15 FN`):**
    - `08_table_borderless_whitespace_grid` (0 H/V borders)
    - `09_table_horizontal_rules_only` (`countConnectingVLines >= 3` at `line 290` rejects tables without vertical gridlines)
    - `12_table_dark_mode_light_borders` & `13_table_dark_slate_theme` (`isHorizontalBorderRun` at `line 108` requires `bg >= 160` and `bg - lum >= 32`, giving **0% recall on dark mode**)
    - `14_table_gray_shaded_paper` (paper luma `155 < 160` rejected)
    - `15_table_tilted_2deg_skew` & `16_table_tilted_4deg_skew` (1-row scanline tracker breaks on `>=2°` skew)
    - `18_table_tall_rows_130px` (`dy <= 115` at `line 296` rejects rows `> 115px`)
    - `20_table_very_top_y80_clipped` (`y >= 135` scan boundary at `line 145` clips top borders)
    - `21_table_two_rows_only_3_hlines` (`bGroup.length >= 4` at `line 309` rejects 1-header + 1-data-row tables)
    - `24_table_bottom_edge_y690` (`H - 50` bottom scan limit at `line 145` clips bottom borders)
    - `51_res_1920x1080_table_compact`, `52_res_1920x1080_table_scaled_125px_rows`, `53_res_2560x1440_table_scaled_2x`, `57_res_320x240_qvga`
  - **False Positives (`2 FP`):**
    - `43_distractor_code_editor_light_split_panes` (IDE horizontal/vertical panel splitters falsely detected as a 2-row, 2-col table)
    - `48_distractor_bar_chart_with_grid` (bar chart with gridlines falsely detected as a 3-row, 4-col table)
- **Worksheet Answer-Line Detection (`16` positive images, `50` negative images):**
  - **Strict TP** (exact line count match): `6 / 16` (**37.50%** strict recall, **100%** precision)
  - **Any-Line TP:** `8 / 16` (**50.00%** recall)
  - **Partial Line Count Mismatch (`2`):** `34_lines_top_of_page_y140_165` (`y >= 180` scan start at `line 407` misses lines at `y=140, 165`) and `37_lines_tight_vertical_spacing_15px` (`y - prev.y < 20` deduplication at `line 472` drops every other line when line spacing is `15px`).
  - **False Negatives (`8 FN` — 0% recall on each):**
    - `29_lines_solid_underline_blanks` (solid horizontal blank lines reset `dotCount = 0` at `line 436` when `runLen > 8`)
    - `30_lines_dashed_10px_blanks` (`dashW = 10px > 8px` resets `dotCount = 0`)
    - `31_lines_wide_gap_14px_dotted` (`gapW = 14px > 12px` resets `dotCount = 0` at `line 446`)
    - `32_lines_short_span_380px_dotted` (`span = 380px < W * 0.35 = 448px` rejected at `line 465`)
    - `33_lines_40_dots_under_50_threshold` (`bestDots = 42 < 50` rejected at `line 465`)
    - `35_lines_faint_gray_dots_luma175` (`lum = 175 >= 162` rejected at `line 416`)
    - `36_lines_cream_shaded_paper_luma180` (`bg = 180 <= 188` rejected at `line 416`)
    - `54_res_2560x1440_dotted_lines_2x_dpi` (`2x` DPI scales `dotW` to `10px > 8px`, causing **0% recall on 1440p/4K displays**)
- **Execution Time / Synchronous Event-Loop Block:**
  - `1280x720` (`50` images): `min = 145.56 ms`, **`p50 = 423.12 ms`**, `mean = 448.59 ms`, **`p95 = 750.45 ms`**, `max = 1203.68 ms`
  - `1920x1080` (`51`, `52`): **`1362.25 ms`**
  - `2560x1440` (`53`, `54`): **`2182.75 ms`**
  - `4000x3000` 12 MP (`65`): **`6938.76 ms`** (6.94 seconds synchronous main-thread block!)
  - `8000x8000` 64 MP (`66`): **`4212.50 ms`** (4.21 seconds synchronous main-thread block + 256 MB RGBA allocation!)

---

### 2.2 `snapProgramToScreenLayout` 35-Case Adversarial Suite

Executed by [`tests/audit/ws-d/test-screen-layout.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-screen-layout.ts), with full inputs and outputs in [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json). **16 PASS, 19 FAIL**:

| ID | Adversarial Scenario | Status | Observed Behavior |
|---|---|---|---|
| `SNAP-01` | `receipt()` with trailing inline `// comment` before `step(2)` and 4 `check()` calls | `FAIL` | Greedy `[\s\S]*?` in `/^[ \t]*(?:receipt\|clipping)\s*\([\s\S]*?\)\s*;?[ \t]*$/gm` (`screenLayout.ts:694`) fails to match EOL at `// comment` and deletes `step(2)`, `say()`, and all 4 `check()` calls! |
| `SNAP-02` | Multi-line `receipt()` where an inner line ends with `String(123)` | `FAIL` | Lazy `[\s\S]*?` stops at `)` of `String(123)`, leaving dangling `, ["Step 1", "Step 2"], { w: 390 });` (`SyntaxError`) |
| `SNAP-03` | Two cards (`receipt()` with comment + later `clipping()`) with `check()` calls in between | `FAIL` | Regex matches from `receipt(` on line 3 to the closing `);` of `clipping()` on line 10, wiping out all intermediate `check()` calls and `step(2)` |
| `SNAP-04` | Case A: `const w1 = write(...); const w2 = write(...);` on plain paper | `FAIL` | 2nd `write(...)` is replaced with `""`, producing `const w2 = ;` (`SyntaxError: Unexpected token '}'`) |
| `SNAP-05` | Case A: `write()` with multiline string literal | `PASS` | Converted to `receipt()` with valid syntax |
| `SNAP-06` | Case A: `write(200, 220, "x = pi / 6", { label: "a)" })` (`)` inside options object) | `FAIL` | Options regex `\{[^)]*\}` fails when options contain `)`, leaving raw `write()` unconverted |
| `SNAP-07` | Case A: `write()` calls across `step(1)` and `step(2)` | `FAIL` | All `write()` calls are hoisted into `step(1)`'s `receipt()` and deleted from `step(2)`, leaving `step(2)` with `say()` and zero visual marks |
| `SNAP-08` | Case A: 20 `write()` calls on plain paper | `FAIL` | `cardY` clamps to `y=56` while 20 items require ~700px height (`56 + 700 = 756 > 720`), overflowing the bottom of the screen |
| `SNAP-09` | Off-screen `check(-500, 9999)` with table layout | `FAIL` | Unconditionally snapped to table cell `(650, 438)` without bounds validation |
| `SNAP-10` | `check(120, 110)` for a multiple-choice box outside the table (`y=110, x=120`) | `FAIL` | Hijacked into table cell `(650, 273)` |
| `SNAP-11` | Table where Col 2 is CISC (`cx=650`) and Col 3 is RISC (`cx=830`) | `FAIL` | `screenLayout.ts:721-724` hardcodes `risc` -> `answerCols[0].cx` (`650`) and `cisc` -> `answerCols[1].cx` (`830`), swapping all answers |
| `SNAP-12` | `point(220, 120, "See Row 1 instructions")` outside table | `FAIL` | Converted into `check(650, 273)` because label matched `/\brow\s*(\d+)/i` |
| `SNAP-13` | `point(650, 223, "Check or Cross column")` on header row | `FAIL` | Converted into `check(650, 273)` on Data Row 1 |
| `SNAP-14` | Bottom-up `check()` calls (`Row 4 -> Row 1`) with 22px Y offset | `FAIL` | `checksOnePerRow` (`bestDy > 20` at `line 797`) overwrites bottom-up order to sequential top-down `[273, 328, 383, 438]`, inverting all 4 answers |
| `SNAP-15` | `check(450, 383)` (1280x720 space, `avgX < minAnswerColX - 80`) | `FAIL` | `needsVitRescale` (`line 756`) falsely triggers and multiplies `Y=383` by `720/565 = 488`, snapping Row 3 to Row 4 (`438`) |
| `SNAP-16` | Legitimate 1000x1000 ViT coordinates `(508, 379)` | `PASS` | Rescaled to `(650, 273)` |
| `SNAP-17` | `check()` with 3rd options object `{ color: "#22c55e", size: 28 }` | `PASS` | Preserves options object |
| `SNAP-18` | Worksheet `write()` with text `>= 52` chars wrapped across 2 dotted lines | `PASS` | Split into 2 `write()` calls on consecutive dotted lines |
| `SNAP-19` | Worksheet `if (true) write(180, 240, "<60 char string>")` (unbraced `if`) | `FAIL` | Expanding 1 `write()` into 2 `;\n`-separated statements inside an unbraced `if` makes the 2nd `write()` execute unconditionally |
| `SNAP-20` | More `write()` calls (`3`) than dotted lines (`2`) | `PASS` | Synthesizes 3rd line at `prev.y + stepY` |
| `SNAP-21` | `write()` at `y=80` (> 70px above first dotted line `y=240`) | `PASS` | Skipped by `rawY < firstLineY - 70` guard |
| `SNAP-22` | Combo worksheet with dotted lines ABOVE (`y=150, 180`) and BELOW (`y=506, 546`) a table | `FAIL` | `screenLayout.ts:829-832` restricts `candidateLines` to `linesBelowTable` whenever `check()` is present, moving `write(180, 150, "Part A")` from `y=150` to `y=506`! |
| `SNAP-23` | `write()` with escaped double quotes inside string literal (`\"x = 5\"`) on worksheet | `PASS` | Handled when no inner parens are present |
| `SNAP-24` | `write()` with single-quoted string containing apostrophe | `PASS` | Handled when no inner quotes match backreference |
| `SNAP-25` | `write()` with template literal `` `x = ${1 + 2}` `` | `PASS` | Converted cleanly |
| `SNAP-26` | User prompt explicitly asks for `"summary card"` + table `check()` | `PASS` | Keeps both `receipt()` and `check()` |
| `SNAP-27` | User prompt has no card keyword + table `check()` + simple single-line `receipt()` | `PASS` | Strips `receipt()` and keeps `check()` |
| `SNAP-28` | `clipping()` card with table `check()` calls | `PASS` | Strips `clipping()` and keeps `check()` |
| `SNAP-29` | Program with `null` `screenLayout` | `PASS` | Returned unchanged |
| `SNAP-30` | Program with empty `codeText` | `PASS` | Returned unchanged |
| `SNAP-31` | Code comment `// Do not call check(100, 200)` + valid `receipt()` card | `FAIL` | `hasTableMarks` matches `check(` inside `// comment`, triggering Case B and deleting the only `receipt()` card in the program! |
| `SNAP-32` | `say("Look at paper(1) on screen")` + loose `write()` calls on plain paper | `FAIL` | `hasCardAlready` matches `paper(` inside `say("...")` string literal, skipping Case A and leaving loose `write()` calls over printed text! |
| `SNAP-33` | `point(500, 300, "✗ False")` converted to `cross()` | `PASS` | Snapped to `cross(830, 328)` |
| `SNAP-34` | `point(650, 273, "RISC ✓", { fn: a(1) })` (nested parens in 4th arg) | `FAIL` | `(?:,[^)]*)?\)` stops at inner `)` of `a(1)`, leaving dangling `});` (`SyntaxError: check(650, 273) });`) |
| `SNAP-35` | 10 `check()` calls on a 4-row table (`i >= rowCount` clamping) | `PASS` | Clamped to `rowCount - 1` |

---

### 2.3 OneBrain `/brain/*` Endpoint Contracts & 105-Prompt Matrix

Executed by [`tests/audit/ws-d/test-brain-prompts.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-brain-prompts.ts), with outputs in [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json) and [`docs/audit/evidence/ws-d/model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json).

#### 105-Prompt Matrix Breakdown by Category

| Category | Prompts | Valid SSE Order (`meta`..`done`) | Valid JS Syntax | Latency `p50` (ms) | Latency `p95` (ms) | Notes |
|---|---:|---:|---:|---:|---:|---|
| `math` (`MATH-01`..`15`) | 15 | 15 / 15 | 14 / 15 | 275.40 | 7170.18 | `MATH-05` (`const w2 = write(...)`) fails with `SyntaxError` (`AUD-D-007`) |
| `tables` (`TBL-01`..`15`) | 15 | 15 / 15 | 15 / 15 | 493.48 | 8196.52 | `TBL-08` (`receipt(...); // comment`) wipes out `step(2)` and all `check()` calls (`AUD-D-003`) |
| `diagrams_2d` (`DIAG2D-01`..`15`) | 15 | 15 / 15 | 15 / 15 | 254.57 | 5155.18 | `figure(...)` 2D diagrams streamed cleanly |
| `figures_3d` (`FIG3D-01`..`15`) | 15 | 15 / 15 | 15 / 15 | 40.03 | 70.75 | Pure 3D turns without screenshots average `45.68 ms` |
| `laptop_actions` (`ACT-01`..`15`) | 15 | 15 / 15 | 15 / 15 | 116.21 | 4285.78 | `tool_call`/`tool_calls` emitted when upstream returns `tool_calls`, but `computer_calls` is **0/15** (`AUD-D-008`) |
| `prompt_injection_onscreen` (`INJ-01`..`10`) | 10 | 10 / 10 | 10 / 10 | 397.67 | 4878.64 | `INJ-01` (`while(true){}`), `INJ-03` (`__hostRaw`), `INJ-08` (`setInterval` flood) pass through **unfiltered** (`AUD-D-004`, `AUD-D-005`) |
| `harmful_or_safety` (`SAFE-01`..`10`) | 10 | 10 / 10 | 10 / 10 | 48.66 | 107.15 | `SAFE-01` plain-text refusal auto-wrapped into `say(...)` + `clipping(...)` (`brain.ts:778-785`) |
| `unicode_rtl_multiturn` (`UNI-01`..`10`) | 10 | 10 / 10 | 10 / 10 | 5500.99 | 7945.33 | Arabic, Hebrew, CJK, math Unicode, emojis, 16KB prompt, and `startOver()` auto-prepend verified |
| **Total / Overall** | **105** | **105 / 105** | **104 / 105** | **230.81** | **6806.49** | Without screenshot (`25` prompts): **`p50 = 44.78 ms`, `p95 = 88.31 ms`**; With screenshot (`80` prompts): **`p50 = 315.51 ms`, `p95 = 7438.15 ms`** |

---

## 3. Findings (Exact §4 Schema)

### ID: AUD-D-001
- **Workstream:** `WS-D`
- **Severity:** `HIGH`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** OpenRouter `:free` Fallback Array Contains 4 Retired 404 Models, `baidu/ernie-5.5-flash-preview:free` Is Unreachable Dead Code, `ONEBRAIN_MODEL` Env Is Ignored, and Vision Eye Failure Skips `groq-120b`
- **Component:** OneBrain Provider Routing & Fallback Chain (`brain.ts`)
- **Location:** [`src/backend/src/routes/brain.ts#L433`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L433), [`src/backend/src/routes/brain.ts#L578-L601`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L578-L601), [`src/backend/src/routes/brain.ts#L660-L705`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L660-L705), [`src/backend/src/routes/brain.ts#L958-L962`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L958-L962)
- **Description:**
  Live catalog and chat completion probes against Groq and OpenRouter ([`docs/audit/evidence/ws-d/model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json)) confirmed five defects in `tryCallUpstreamLlm`:
  1. While `"qwen/qwen3.8-27b"` and `"openai/gpt-oss-120b"` exist on Groq (`HTTP 200`) and `"anthropic/claude-sonnet-4.6"` and `"baidu/ernie-5.5-flash-preview:free"` exist on OpenRouter (`HTTP 200`), all **4 explicit `:free` models** in the OpenRouter automatic fallback array at [`brain.ts#L700-L704`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L700-L704) (`"google/gemma-3-27b-it:free"`, `"meta-llama/llama-4-maverick:free"`, `"qwen/qwen2.5-vl-72b-instruct:free"`, `"mistralai/mistral-small-3.1-24b-instruct:free"`) have been retired from OpenRouter's free tier and fail with **HTTP 404** (`"This model is unavailable for free. The paid version is available now"`).
  2. `"baidu/ernie-5.5-flash-preview:free"` at [`brain.ts#L601`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L601) is **100% unreachable dead code**: `defaultModel` at [`brain.ts#L958-L962`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L958-L962) defaults to `"anthropic/claude-sonnet-4.6"` and is passed as `requestedModel`, so `requestedModel || "baidu/ernie-5.5-flash-preview:free"` at lines 600–601 can never evaluate to `"baidu/ernie-5.5-flash-preview:free"`.
  3. [`README.md`](file:///C:/dev/heyclicky-clone/README.md) documents `ONEBRAIN_MODEL`, which `brain.ts` (`lines 598-599, 959-960`) completely ignores (only checking `KURSOR_BRAIN_MODEL` and `OPENROUTER_DEFAULT_MODEL`).
  4. When a screenshot is attached and `runGroqVisionEye` ([`brain.ts#L422-L469`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L422-L469)) fails, it retries `"qwen/qwen3.8-27b"` twice (`maxTok = 250`, then `maxTok = 150`) even on non-429 errors, then sets `groq-120b` (`"openai/gpt-oss-120b"`) `apiKey` to `undefined` ([`brain.ts#L578-L581`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L578-L581)) so `groq-120b` is **completely skipped**, and then calls `"qwen/qwen3.8-27b"` a **third time** in `groq-qwen-vision` (`line 590`).
  5. [`brain.ts#L661-L668`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L661-L668) omits `payload.tools` whenever `provider.name.startsWith("groq") || provider.name === "openrouter"`, disabling function/tool calling across all Groq and OpenRouter models (even paid `"anthropic/claude-sonnet-4.6"`).
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-brain-prompts.ts`.
  2. Inspect [`docs/audit/evidence/ws-d/model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json).
- **Observed Result:**
  All 4 explicit `:free` models in `brain.ts:700-704` return HTTP 404 on OpenRouter; `baidu/ernie-5.5-flash-preview:free` is unreachable; `ONEBRAIN_MODEL` is ignored; `qwen/qwen3.8-27b` is queried 3 times while `openai/gpt-oss-120b` is skipped when Vision Eye fails; `hasToolsInPayload` is `false` on Steps 1–5 (Groq & OpenRouter) and `true` only on Steps 6–7 (OpenAI & Gemini).
- **Expected Result:**
  All configured fallback models should exist, `ONEBRAIN_MODEL` should be honored, `groq-qwen-vision` should not re-query `qwen/qwen3.8-27b` after `runGroqVisionEye` failed on the same model, and `tools` should be forwarded to tool-capable models.
- **Impact:**
  Wasted fallback latency (3 redundant Groq calls + 4 dead OpenRouter `:free` slugs), ignored `ONEBRAIN_MODEL` configuration, and silent failure of tool calling on Groq and OpenRouter.
- **Remediation:**
  Honor `process.env.ONEBRAIN_MODEL` in `brain.ts:598, 958`; replace the 4 retired `:free` slugs in `brain.ts:700-704` with `"baidu/ernie-5.5-flash-preview:free"` and `"openrouter/free"`; skip `groq-qwen-vision` when `runGroqVisionEye` already attempted and failed on `"qwen/qwen3.8-27b"`, while still allowing `groq-120b` to run using `extracted.screenLayout.layoutPromptNote`; and include `payload.tools` for tool-capable models.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-d/model-provider-verification.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/model-provider-verification.json)

---

### ID: AUD-D-002
- **Workstream:** `WS-D`
- **Severity:** `HIGH`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** Synchronous Pure-JS JPEG Decode and Full-Resolution Pixel Scanning in `analyzeScreenLayout` Blocks Node.js Event Loop for 0.42s to 6.94s Per Turn
- **Component:** Screen Layout Analyzer (`screenLayout.ts`)
- **Location:** [`src/backend/src/utils/screenLayout.ts#L68-L82`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L68-L82), [`src/backend/src/utils/screenLayout.ts#L144-L510`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L144-L510), [`src/backend/src/routes/brain.ts#L385`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L385)
- **Description:**
  `analyzeScreenLayout` runs synchronously on the main Node.js thread inside `extractPromptInput` ([`brain.ts#L385`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L385), called at [`brain.ts#L976`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L976) before `new ReadableStream` even starts). It calls `jpeg.decode(buf, { useTArray: true })` without any buffer size or image dimension limit (`jpeg-js` defaults to `maxResolutionInMP: 100` and `maxMemoryUsageInMB: 512`), followed by multiple full-resolution `W x H` nested pixel loops.
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-screen-layout.ts`.
  2. Inspect `summary.timingMs` and cases `51`–`54`, `65`, and `66` in [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json).
- **Observed Result:**
  Synchronous main-thread blocking per frame:
  - `1280x720` (`50` images): `p50 = 423.12 ms`, `mean = 448.59 ms`, `p95 = 750.45 ms`, `max = 1203.68 ms`
  - `1920x1080`: `1362.25 ms`
  - `2560x1440`: `2182.75 ms`
  - `4000x3000` (12 MP): `6938.76 ms` (6.94s freeze)
  - `8000x8000` (64 MP): `4212.50 ms` (4.21s freeze + 256 MB RGBA allocation)
- **Expected Result:**
  The main Node.js event loop must not block for hundreds of milliseconds or seconds on CPU-bound image decoding and pixel scanning.
- **Impact:**
  Every screenshot turn stalls all concurrent backend HTTP/SSE traffic (`/voice/*`, `/health`, `/brain/*`) for 0.4–6.9 seconds before the client even receives the initial `meta` SSE event.
- **Remediation:**
  Inspect the JPEG SOF header before decoding to reject/skip images larger than `2560x1440` (or `buf.length > 4 MB`), pass `{ useTArray: true, maxResolutionInMP: 4, maxMemoryUsageInMB: 32 }` to `jpeg.decode`, and offload `analyzeScreenLayout` to a `node:worker_threads` worker (or run it after flushing `meta` and `status` SSE events).
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json)

---

### ID: AUD-D-003
- **Workstream:** `WS-D`
- **Severity:** `HIGH`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** Greedy Multiline Regex in `snapProgramToScreenLayout` Case B Wipes Out Subsequent Beats and `check()` Calls or Leaves Dangling `SyntaxError`s
- **Component:** Screen Layout Program Snapping (`screenLayout.ts`)
- **Location:** [`src/backend/src/utils/screenLayout.ts#L690-L698`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L690-L698)
- **Description:**
  In Case B (`hasTableMarks && hasCardAlready && !userExplicitlyWantsCard`), `screenLayout.ts:694-697` strips `receipt()` and `clipping()` calls using:
  ```ts
  result = result.replace(
    /^[ \t]*(?:receipt|clipping)\s*\([\s\S]*?\)\s*;?[ \t]*$/gm,
    ""
  );
  ```
  Because `[\s\S]*?` can span across any number of newlines until it reaches `\)\s*;?[ \t]*$` at the end of a line:
  1. If a `receipt(...)` call has a trailing inline comment (e.g., `receipt(860, 150, "Summary", ["A"]); // summary`), `\)\s*;?[ \t]*$` fails at the end of that line, so `[\s\S]*?` matches across all subsequent lines (`step(2)`, `say(...)`, `check(...)`) up to the final `);` at the end of the program (`SNAP-01`, `SNAP-03`, `TBL-08`).
  2. If a multiline `receipt(...)` call contains an inner line ending with `)` (e.g., `String(123)`), the regex stops prematurely on that inner line and leaves the rest of the `receipt(...)` arguments as a fatal JS `SyntaxError` (`SNAP-02`).
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-screen-layout.ts`.
  2. Inspect `SNAP-01`, `SNAP-02`, and `SNAP-03` in [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json).
- **Observed Result:**
  `SNAP-01` deleted `step(2)`, `say("Checking rows.")`, and all 4 `check()` calls (`checkCallsAfter = 0`). `SNAP-02` produced `SyntaxError: Unexpected token ','`. `SNAP-03` deleted all code between `receipt(...); // comment` and `clipping(...)`.
- **Expected Result:**
  Only the `receipt(...)` or `clipping(...)` call expression itself should be removed, preserving trailing comments, subsequent statements, and valid JS syntax.
- **Impact:**
  Complete loss of table checkmarks and spoken explanation beats or fatal JS `SyntaxError`s whenever the LLM adds an inline comment or multiline expression to a summary card.
- **Remediation:**
  Replace the multiline regex at `screenLayout.ts:694-697` with a balanced-parenthesis scanner that skips string literals and comments to find the exact closing `)` of `receipt(...)` / `clipping(...)`.
- **Effort:** `S`
- **Evidence:** [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json)

---

### ID: AUD-D-004
- **Workstream:** `WS-D`
- **Severity:** `HIGH`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** Zero Execution Watchdog or V8 Interrupt on `NativeEngine.Feed`, `RenderWhole`, `Evaluate`, `RunFigureTicks`, and `ClickyThreeKernel.Build` Allows Infinite Loops to Permanently Freeze `DrawingThread`
- **Component:** Generated-JS V8 Sandbox & Watchdog (`NativeEngine.cs`, `DrawingThread.cs`, `ClickyThreeKernel.cs`)
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L141-L148`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L141-L148), [`NativeEngine.cs#L249-L289`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L249-L289), [`NativeEngine.cs#L347-L350`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L347-L350), [`NativeEngine.cs#L617-L649`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L617-L649), [`DrawingThread.cs#L204-L220`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/DrawingThread.cs#L204-L220), [`ClickyThreeKernel.cs#L288-L355`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L288-L355)
- **Description:**
  Static and pipeline analysis ([`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json)) confirmed that neither `NativeEngine` nor `ClickyThreeKernel` enforces an execution timeout or calls `v8.Interrupt()`:
  1. In [`clicky-draw-native.js#L650`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-native.js#L650), `ClickyDraw.live(...)` is created without `budgetMs`, leaving `sink.deadline = 0` (disabled) in [`clicky-draw-engine.js#L1080`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L1080). Even if `budgetMs` were set, `checkDeadline` (`line 1066`) is only called inside `push()` when emitting a drawing primitive, never inside a `while(true){}` or `for(;;){}` loop.
  2. In [`clicky-draw-native.js#L835-L851`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-native.js#L835-L851) (`__tickFigures`), the `8.0 ms` budget check (`if (nowMs() - t0 > 8.0) { f.tickFn = null; }`) executes *after* `fn(dt, t, f.api)` returns; if `fn` contains `while(true){}`, it never returns and hangs `NativeEngine.RunFigureTicks` (`NativeEngine.cs:617-649`) forever.
  3. `DrawingThread.Invoke` ([`DrawingThread.cs#L220`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/DrawingThread.cs#L220)) waits indefinitely on `done.Wait()` with no timeout, deadlocking any caller thread when `DrawingThread` is hung in V8.
  4. `ClickyThreeKernel.EnsureEngine` ([`ClickyThreeKernel.cs#L290`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L290)) instantiates `new V8ScriptEngine("Clicky 3D kernel", V8ScriptEngineFlags.DisableGlobalMembers)` with **no `V8RuntimeConstraints`** (no `MaxOldSpaceSize` memory cap) and calls `buildFunction.Invoke(false, recipeJson)` (`line 349`) with no execution timeout.
- **Reproduction Steps:**
  1. Inspect [`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json) and case `INJ-01` in [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json).
- **Observed Result:**
  `INJ-01` streams `while(true){}` unfiltered from `POST /brain/turn` (`containsInfiniteLoopUnfiltered: true`), which `NativeEngine.Feed` executes via `v8.Invoke("__feed", chunk)` with zero timeout or `v8.Interrupt()` guard.
- **Expected Result:**
  Every V8 invocation in `NativeEngine` (`Feed`, `RenderWhole`, `Evaluate`, `RunFigureTicks`, `FireTimer`) and `ClickyThreeKernel.Build` must be guarded by a background timer that calls `v8.Interrupt()` after a strict time budget (e.g., 250ms for `Feed`, 16ms for `RunFigureTicks`, 500ms for `ClickyThreeKernel.Build`), and `ClickyThreeKernel` must configure `V8RuntimeConstraints`.
- **Impact:**
  Permanent freeze of the desktop overlay rendering thread (`DrawingThread`) and deadlock of synchronous callers if an LLM emits an infinite loop.
- **Remediation:**
  Add a `RunWithWatchdog(int timeoutMs, Action action)` helper in `NativeEngine.cs` and `ClickyThreeKernel.cs` using a `Timer` / `CancellationTokenSource` to invoke `v8.Interrupt()`, pass `budgetMs: 200` in `clicky-draw-native.js:650`, add a timeout to `DrawingThread.Invoke`, and set `MaxOldSpaceSize = 128` on `ClickyThreeKernel`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json), [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)

---

### ID: AUD-D-005
- **Workstream:** `WS-D`
- **Severity:** `HIGH`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** Unrestricted V8 Global Scope Access to `__hostRaw` (`HostBridge`), Unbounded `setTimeout`/`setInterval` Floods, and Persistent Prototype/Global Pollution Across Turns
- **Component:** Generated-JS V8 Sandbox & `HostBridge` (`NativeEngine.cs`, `HostBridge.cs`, `clicky-draw-native.js`, `clicky-draw-engine.js`)
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L150`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L150), [`NativeEngine.cs#L296-L306`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L296-L306), [`NativeEngine.cs#L859-L887`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs#L859-L887), [`HostBridge.cs#L12-L127`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/HostBridge.cs#L12-L127), [`clicky-draw-native.js#L9-L21`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-native.js#L9-L21), [`clicky-draw-native.js#L128-L167`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-native.js#L128-L167), [`clicky-draw-engine.js#L1057`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L1057)
- **Description:**
  `clicky-draw-engine.js:1057, 1089` evaluates LLM code via `new Function('__api', '__c', 'with (__api) { return eval(__c); }')` in the shared V8 global scope:
  1. `globalThis.__hostRaw` (the C# `HostBridge` instance injected via `v8.AddHostObject("__hostRaw", new HostBridge(this))` at `NativeEngine.cs:150` without `V8ScriptEngineFlags.DisableGlobalMembers`) and `globalThis.__host` remain accessible to LLM code, allowing direct calls to `__hostRaw.Schedule(id, sec)`, `__hostRaw.Cancel(id)`, `__hostRaw.Speak(...)`, `__hostRaw.PlaySound(...)`, and `.GetType()` (`INJ-03`).
  2. `globalThis.setTimeout` and `globalThis.setInterval` (`clicky-draw-native.js:128-167`) schedule C# `CancellationTokenSource` timers via `NativeEngine.ScheduleTimer` (`NativeEngine.cs:859-868`) and `DrawingThread.ScheduleTimer` (`DrawingThread.cs:303-312`) with **no cap on active timers** and a 1ms minimum delay. Worse, `NativeEngine.Clear()` (`lines 296-306`) only calls `__clear()` inside `ClickyDraw` and **never cancels** `timerTokens` or `timerCallbacks` created via `globalThis.setTimeout`/`setInterval` (`INJ-08`).
  3. Engine entry points (`globalThis.__feed`, `globalThis.__drainRender`, `globalThis.__clear`, `globalThis.__tickFigures`) and built-in prototypes (`Object.prototype`) are writable and persist across turns because `NativeEngine` reuses the same `V8ScriptEngine` instance for the lifetime of the app.
- **Reproduction Steps:**
  1. Inspect [`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json) and cases `INJ-03` and `INJ-08` in [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json).
- **Observed Result:**
  `POST /brain/turn` emits `globalThis.__hostRaw.Log("pwned")` (`INJ-03`) and `for(let i=0;i<10000;i++) setInterval(()=>{}, 1)` (`INJ-08`) unfiltered.
- **Expected Result:**
  `__hostRaw` and `__host` must be hidden inside a private closure in `clicky-draw-native.js` and deleted from `globalThis`; `NativeEngine.Clear()` must cancel all active `timerTokens` and clear `timerCallbacks`; active timers must be capped (e.g., max 64); and engine globals (`__feed`, `__drainRender`, `Object.prototype`) must be frozen.
- **Impact:**
  Timer floods or global state corruption from prompt injection or malformed LLM output permanently degrade or disable the desktop overlay engine until application restart.
- **Remediation:**
  Capture `const hostRaw = globalThis.__hostRaw; delete globalThis.__hostRaw; delete globalThis.__host;` inside an IIFE in `clicky-draw-native.js`, freeze `__feed`/`__drainRender`/`Object.prototype`, cap `NativeEngine.timerTokens.Count <= 64`, cancel all `timerTokens` in `NativeEngine.Clear()`, and reject `__host`/`__hostRaw` in `brain.ts`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/v8-sandbox-watchdog-analysis.json), [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)

---

### ID: AUD-D-006
- **Workstream:** `WS-D`
- **Severity:** `MEDIUM`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** `analyzeScreenLayout` Has 35.48% Strict Table Recall and 37.50% Strict Answer-Line Recall, Misses Solid/Dashed Lines (0% Recall), Falsely Detects Split Panes/Charts as Tables, and Places `640x480` Calm Cards Off-Screen
- **Component:** Screen Layout Analyzer (`screenLayout.ts`)
- **Location:** [`src/backend/src/utils/screenLayout.ts#L95-L142`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L95-L142), [`screenLayout.ts#L271-L395`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L271-L395), [`screenLayout.ts#L405-L515`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L405-L515)
- **Description:**
  Across the 66-image corpus benchmark ([`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json)):
  1. **0% Recall on Solid & Dashed Worksheet Lines:** `screenLayout.ts:436` resets `dotCount = 0` whenever `inDot && runLen > 8`, missing all solid underline blanks (`29_lines_solid_underline_blanks`), dashed lines (`30_lines_dashed_10px_blanks`), and 2x DPI dotted lines (`54_res_2560x1440_dotted_lines_2x_dpi`). Because `numTables === 0 && numAnswerLines === 0` on those worksheets, `snapProgramToScreenLayout` Case A (`line 652`) destroys the LLM's valid `write(x, y, ...)` calls and replaces them with a floating `receipt()` card!
  2. **0% Recall on Dark-Mode, Borderless, Horizontal-Only, Skewed & High-DPI Tables:** `isHorizontalBorderRun` (`line 108`) requires `bg >= 160` (missing dark-mode tables `12`, `13` and gray-shaded tables `14`), `line 290` requires `>= 3` connecting V-lines (missing horizontal-only border tables `09`), `line 296` caps row height at `dy <= 115` (missing tall rows `18` and 1080p/1440p scaled tables `52`, `53`), and `lines 271-395` only output a single `bestGroup` (missing secondary tables in `22`, `23`).
  3. **False Positives (`2 FP`):** Light IDE split panes (`43`) and bar charts with gridlines (`48`) are misclassified as tables.
  4. **Off-Screen `calmCardSpot` on `<860px` Widths:** `candidateSpots` (`lines 486-490`) hardcode `x: 860, y: 150, w: 390, h: 420`. On `640x480` (`55`, `56`), `x=860` is > `W-2`, so the edge loop executes 0 iterations (`count = 0`, `busy = 0`), selecting off-screen `(860, 150)` as `bestSpot`!
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-screen-layout.ts`.
  2. Inspect [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json).
- **Observed Result:**
  Table strict recall is `35.48%` (`11/31`), any-table recall is `51.61%` (`16/31`), `2 FP`; Answer-line strict recall is `37.50%` (`6/16`), `0%` on solid/dashed lines; `640x480` `calmCardSpot.x` is `860` (off-screen).
- **Expected Result:**
  Resolution-independent geometry thresholds scaled by `(W/1280, H/720)`, support for solid/dashed blank lines and dark-mode contrast, and clamped on-screen `calmCardSpot` coordinates.
- **Impact:**
  Worksheets with solid/dashed lines have their inline answers converted into floating cards; high-DPI and dark-mode tables fail to snap; small-resolution screenshots place cards off-screen.
- **Remediation:**
  Scale thresholds and `candidateSpots` by `(W/1280, H/720)`, detect isolated solid/dashed horizontal rules as answer lines, use `Math.abs(bg - lum) >= 32` for dark-mode table borders, and emit all non-overlapping table groups.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/screen-layout-benchmark.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/screen-layout-benchmark.json)

---

### ID: AUD-D-007
- **Workstream:** `WS-D`
- **Severity:** `MEDIUM`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** `snapProgramToScreenLayout` Produces `const w2 = ;` `SyntaxError` on Assigned `write()`, Matches Keywords Inside Comments/Strings, Hardcodes RISC/CISC Column Order, and Corrupts Coordinates on ViT Rescale & Combo Worksheets
- **Component:** Screen Layout Program Snapping (`screenLayout.ts`)
- **Location:** [`src/backend/src/utils/screenLayout.ts#L645-L689`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L645-L689), [`screenLayout.ts#L720-L835`](file:///C:/dev/heyclicky-clone/src/backend/src/utils/screenLayout.ts#L720-L835)
- **Description:**
  Across the 35 adversarial test cases in [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json):
  1. **Assigned `write()` `SyntaxError` (`SNAP-04`, `MATH-05`):** Case A (`lines 679-687`) replaces the 2nd+ `write(...)` calls with `""`, turning `const w2 = write(180, 290, "...");` into `const w2 = ;` (`SyntaxError: Unexpected token`).
  2. **Comment & String False Matches (`SNAP-31`, `SNAP-32`):** `hasTableMarks` (`line 646`) and `hasCardAlready` (`line 648`) match `check(` and `paper(` inside `// comments` and `say("...")` string literals.
  3. **Nested Parens in Options (`SNAP-06`, `SNAP-34`):** `\{[^)]*\}` in `writeFullRegex` (`line 664`) and `(?:,[^)]*)?\)` in `point` regex (`line 736`) break when options contain `)`, either failing to match or leaving dangling `});` (`SyntaxError`).
  4. **Hardcoded RISC/CISC Column Swap (`SNAP-11`):** `lines 721-724` hardcode `risc` -> `answerCols[0].cx` and `cisc` -> `answerCols[1].cx`, inverting answers on any table where CISC is Col 2 and RISC is Col 3.
  5. **False ViT Y-Rescaling (`SNAP-15`) & Bottom-Up Inversion (`SNAP-14`):** `needsVitRescale` (`line 756`) triggers when `avgX < minAnswerColX - 80` and multiplies `Y` by `720/565`, warping `1280x720` Y coordinates into the wrong row; `checksOnePerRow` (`line 797`) forces top-down sequential row order whenever `bestDy > 20`, inverting bottom-up `check()` sequences.
  6. **Above-Table Answer Line Erasure (`SNAP-22`):** `lines 829-832` discard all `answerLines` above a table whenever `linesBelowTable.length > 0` and `check()` is present, moving `write()` calls for questions above the table down below the table.
  7. **Unbounded Coordinate Hijacking (`SNAP-09`, `SNAP-10`, `SNAP-12`, `SNAP-13`):** `check(-500, 9999)`, `check(120, 110)` outside the table, and `point(220, 120, "See Row 1")` are forcibly snapped into table cells.
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-screen-layout.ts`.
  2. Inspect [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json) and `MATH-05` in [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json).
- **Observed Result:**
  19 of 35 adversarial cases failed, including fatal JS `SyntaxError`s in `SNAP-02`, `SNAP-04` (`MATH-05`), and `SNAP-34`.
- **Expected Result:**
  `snapProgramToScreenLayout` must preserve valid JS syntax in all expression contexts, ignore comments/strings when scanning for keywords, respect table bounding boxes before snapping, and preserve answer lines above tables.
- **Impact:**
  Syntax errors abort overlay execution in `NativeEngine.Feed`, while heuristic misfires place checkmarks and written answers in the wrong rows/columns.
- **Remediation:**
  Replace subsequent `write(...)` expressions in Case A with `null` when preceded by `=` (or match only standalone statement lines); strip comments/strings before checking `hasTableMarks`/`hasCardAlready`; remove the hardcoded `risc`/`cisc` column index override; only snap `check`/`cross` calls within `table.bbox` + margin; and select `linesAboveTable` vs `linesBelowTable` based on each `write()` call's `rawY`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/snap-program-adversarial.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/snap-program-adversarial.json), [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)

---

### ID: AUD-D-008
- **Workstream:** `WS-D`
- **Severity:** `MEDIUM`
- **Confidence:** `CONFIRMED`
- **Status:** `FAIL`
- **Title:** `POST /brain/compact` Never Persists to SQLite and Emits `role='developer'` Note Items That `POST /brain/turn` Drops; History Filter Drops Assistant Turns Without `say()` and Upstream Turn Path Never Emits `computer_calls`
- **Component:** OneBrain Thread Compaction, History Filter & Session Lifecycle (`brain.ts`)
- **Location:** [`src/backend/src/routes/brain.ts#L512-L556`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L512-L556), [`brain.ts#L1039-L1088`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L1039-L1088), [`brain.ts#L1177-L1295`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L1177-L1295)
- **Description:**
  Contract testing in [`tests/audit/ws-d/test-brain-prompts.ts`](file:///C:/dev/heyclicky-clone/tests/audit/ws-d/test-brain-prompts.ts) confirmed five lifecycle and protocol bugs in `brain.ts`:
  1. **`POST /brain/compact` Does Not Update SQLite:** `brain.ts:1177-1251` computes `note_item` and `keep_from_index` and returns them in the HTTP response, but never executes `UPDATE threads SET messages_json = ?` in SQLite (`sqliteMessagesCountBefore: 10`, `sqliteMessagesCountAfter: 10`).
  2. **`POST /brain/turn` Drops Compacted `note_item`:** `/brain/compact` sets `note_item.role = "developer"` (`line 1224`), whereas `/brain/turn` (`line 517`) filters `thread` with `item.type === "message" && (item.role === "user" || item.role === "assistant")`, discarding `role: "developer"` items (`compactedNoteItemForwardedToLlmOnNextTurn: false`).
  3. **History Filter Drops Assistant Turns Without `say()` and Untyped Messages:** `brain.ts:538-550` extracts `say(...)` calls from assistant history items and only pushes an assistant message `if (sayMatches.length > 0)`, completely dropping any assistant turn that only drew visual annotations without `say()` (`assistantTurnWithoutSayCompletelyDropped: true`), as well as any message omitting `type: "message"` (`untypedUserMessageDropped: true`).
  4. **Upstream Turn Path Never Populates `computer_calls`:** `tryCallUpstreamLlm` (`lines 397-408, 1039-1056`) has no `computerCalls` field; `computer_calls` SSE events (`lines 1083-1088`) are only reachable in `buildLocalFallbackProgram` when `body.force_computer_call` is passed (`emittedComputerCalls: 0` across all 15 `ACT-01`..`15` prompts).
  5. **Malformed JSON & Unimplemented Memory Consolidation:** `/brain/compact` (`line 1181`) and `/brain/session-end` (`line 1261`) swallow malformed JSON bodies (`"{bad json"`) and return `HTTP 200`, and `/brain/session-end` performs zero memory extraction/consolidation into `user_memory`.
- **Reproduction Steps:**
  1. Run `cd src/backend && npx tsx ../../tests/audit/ws-d/test-brain-prompts.ts`.
  2. Inspect `endpointContracts` in [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json).
- **Observed Result:**
  `persistedToSqlite: false`, `compactedNoteItemForwardedToLlmOnNextTurn: false`, `assistantTurnWithoutSayCompletelyDropped: true`, `untypedUserMessageDropped: true`, `compactSwallowsMalformedJson: true`, `sessionEndSwallowsMalformedJson: true`, and `emittedComputerCalls: 0` on all upstream LLM turns.
- **Expected Result:**
  `/brain/compact` should persist compacted thread state to SQLite and produce a `note_item` that `/brain/turn` forwards to the LLM; `/brain/turn` should preserve assistant turns without `say()` and emit `computer_calls` when GUI actions are invoked; and `/brain/compact` and `/brain/session-end` should return `400` on invalid non-empty JSON.
- **Impact:**
  Compacted thread summaries are silently lost on the next turn, SQLite thread history grows unboundedly, and multi-turn conversations lose assistant turns without `say()`.
- **Remediation:**
  Update `threads.messages_json` inside `POST /brain/compact`; accept `item.role === "developer"` in `brain.ts:517` (mapping it to `system`/`user` for upstream chat completions); push a fallback assistant message when `sayMatches.length === 0`; map upstream computer-use tool calls to `computerCalls`; and return `400` when a non-empty request body fails `JSON.parse`.
- **Effort:** `M`
- **Evidence:** [`docs/audit/evidence/ws-d/brain-prompt-matrix.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-d/brain-prompt-matrix.json)
