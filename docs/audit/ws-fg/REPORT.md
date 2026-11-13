# WS-F & WS-G Audit Report: 2D Drawing Engine (`Kursor.Drawing`) & 3D Engine (`Kursor.Drawing3D`)

- **Owner:** `ws-fg-drawing-2d-3d`
- **Scope:**
  - **WS-F:** `src/desktop/Kursor.Drawing/` (86 C# files) + V8 JS engines (`Assets/Engine/clicky-draw-native.js`, `clicky-draw-engine.js`, `clicky-draw-pieces.js`) + prompt/doc parity (`src/backend/src/routes/brain.ts`, `README.md`, `docs/04-drawing-2d-3d-and-ink-engine.md`).
  - **WS-G:** `src/desktop/Kursor.Drawing3D/` (42 C# files) + Three.js V8 kernel (`Assets/Engine/clicky-three.js`, `clicky-three-parts.js`).
- **Automated Test Harnesses & Evidence:**
  - [`tests/audit/ws-fg/test-2d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-2d-engine.mjs) → [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) (**57 ClickyDraw programs executed**: 54 `PASS`, 3 `EXPECTED_FAIL_CONFIRMED` proving `AUD-F-001`, `AUD-F-002`, and syntax-error recovery, 0 `UNEXPECTED`).
  - [`tests/audit/ws-fg/test-3d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-3d-engine.mjs) → [`docs/audit/evidence/ws-fg/test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json) (**40 3D procedural geometry & modifier tests executed**: 17/17 normal shapes `PASS`, 6/6 geometry modifiers + 12 DSL-expanded parts `PASS`, 17/17 parameter extremes met expectations, and byte-for-byte proof of `AUD-G-001`).

---

## 1. Executive Summary

| Workstream | Files Audited | Automated Tests | Findings (`FAIL`) | Breakdown by Severity |
| :--- | :---: | :---: | :---: | :--- |
| **WS-F (`Kursor.Drawing` + 2D V8 JS)** | 86 `.cs` + 3 `.js` | 57 programs | 8 (`AUD-F-001` .. `AUD-F-008`) | 2 P1, 5 P2, 1 P3 |
| **WS-G (`Kursor.Drawing3D` + 3D V8 JS)** | 42 `.cs` + 2 `.js` | 40 build/modifier tests | 4 (`AUD-G-001` .. `AUD-G-004`) | 0 P1, 2 P2, 2 P3 |
| **Total** | **128 `.cs` + 5 `.js`** | **97 test cases** | **12 findings** | **2 P1, 7 P2, 3 P3** |

---

## 2. WS-F: 2D Drawing Engine Audit (`Kursor.Drawing` + V8 JS Scripts)

### 2.1 Complete Enumeration of `src/desktop/Kursor.Drawing/` (86 C# Files)

| Namespace / Folder | Files (86 total) | Audit Status |
| :--- | :--- | :---: |
| `HeyClicky.Drawing` (13 files) | `CanvasSession.cs`, `CompletedLessonVideo.cs`, `CursorPalette.cs`, `DrawingAssets.cs`, `DrawingCoord.cs`, `IDrawing3DBridge.cs`, `Log.cs`, `PhotoFetcher.cs`, `PhotoResolver.cs`, `ScreenBounds.cs`, `ScreenDrawingCanvas.cs`, `SystemComposition.cs`, `TargetOutline.cs` | Audited (`FAIL` in `SystemComposition.cs` → `AUD-F-005`) |
| `HeyClicky.Drawing.Audio` (6 files) | `ActiveStrokeLoop.cs`, `AudioOutput.cs`, `MarkSoundPlan.cs`, `ScheduledAccent.cs`, `SoundClip.cs`, `SoundPlayer.cs` | Audited (`PASS`) |
| `HeyClicky.Drawing.Engine` (22 files) | `AnalysisResult.cs`, `CardInfo.cs`, `CardWindowInfo.cs`, `DrainOp.cs`, `DrawingThread.cs`, `EngineScriptLoader.cs`, `HostBridge.cs`, `MotionItem.cs`, `NativeEngine.cs`, `ParagraphShape.cs`, `PointingAction.cs`, `RenderNode.cs`, `ScreenshotSampler.cs`, `StatementProgress.cs`, `SubtitleChip.cs`, `SubtitlePayload.cs`, `SvgArc.cs`, `SvgPath.cs`, `TextDesc.cs`, `TextLine.cs`, `TextShaper.cs`, `TimelineSnapshot.cs` | Audited (`PASS`) |
| `HeyClicky.Drawing.Media` (4 files) | `LessonVideoFiles.cs`, `LessonVideoRecorder.cs`, `MF.cs`, `Mp4VideoWriter.cs` | Audited (`FAIL` in `Mp4VideoWriter.cs` / `LessonVideoRecorder.cs` → `AUD-F-008`) |
| `HeyClicky.Drawing.Overlay` (19 files) | `ActionButtons.cs`, `CardHandles.cs`, `ChevronIcon.cs`, `CursorHalo.cs`, `CursorKind.cs`, `CursorOverlay.cs`, `CursorPose.cs`, `CursorSpring.cs`, `DpiAwareness.cs`, `HeaderBar.cs`, `InteractiveOverlayWindow.cs`, `LiveCaption.cs`, `MonitorEnumeration.cs`, `MonitorInfo.cs`, `OverlayHost.cs`, `OverlayWindow.cs`, `PendingButton.cs`, `PinnedCards.cs`, `Win32.cs` | Audited (`FAIL` in `OverlayWindow.cs` → `AUD-F-006`, `AUD-F-007`; `LiveCaption.cs`/`PinnedCards.cs`/`CardHandles.cs` → `AUD-F-005`) |
| `HeyClicky.Drawing.Painter` (22 files) | `ActiveMotion.cs`, `CardBackdrop.cs`, `CardBoardKind.cs`, `CardChromeSpec.cs`, `CubicBezierTiming.cs`, `DashSpec.cs`, `Drawing3DAdapter.cs`, `DrawingColor.cs`, `FilterDef.cs`, `FilterPad.cs`, `GradientStop.cs`, `LinearGradientDef.cs`, `MaskDef.cs`, `Matrix3x2Stack.cs`, `Merchants.cs`, `NodeAnim.cs`, `PaintUnit.cs`, ` radialGradientDef.cs` (`RadialGradientDef.cs`), `ScreenPainter.cs`, `SharedSurfaceLease.cs`, `Stage3DPicture.cs`, `StageSwapChain.cs` | Audited (`FAIL` in `ScreenPainter.cs` / `StageSwapChain.cs` → `AUD-F-005`) |

### 2.2 Complete Enumeration of V8 JS Exports & Primitives

1. **`clicky-draw-native.js` (`global.clickyCanvas` & globals)**:
   - `clickyCanvas` methods (18): `setPieces`, `beginStream`, `continueStream`, `state`, `feed`, `line`, `lineLength`, `finish`, `now`, `isFinished`, `timeline`, `clear`, `keepNotesOnly`, `dropPointing`, `cards`, `closeCard`, `zoomCard`, `moveCard`, `buttonSpot`, `markBox`, `tick3d`, `renderWhole`.
   - Native bridge globals: `__drainRender`, `__fireTimer`, `__clickyMotion`, `__inkFor`, `__busyFor`, `__lumFor`, `__figuresFloat`, `__shape3d`, `__hostDrawsPointers`, `__hostDrawsButtons`, `__hostDrawsWindows`, `__hostDrawsMarquee`.
2. **`clicky-draw-engine.js` (`global.ClickyDraw` & `makeApi` primitives)**:
   - `ClickyDraw` methods (17): `version`, `tones`, `realTones`, `setPen`, `statementEnds`, `statementScanner`, `runCode`, `attribute`, `exec`, `natural`, `markBox`, `isCardMark`, `pointsAtScreen`, `onCardTest`, `formatState`, `render`, `live`.
   - `makeApi` primitives exposed inside `with (api)` scope:
     - **Pacing & Memory Meta:** `step`, `say`, `seen`, `noted`, `__turn`.
     - **2D Shapes & Ink:** `line`, `curve`, `circle`, `ellipse`, `rect`, `poly`, `path`, `text`, `arrow`, `highlight`, `dot`, `image`, `molecule`, `__window`.
     - **Composites:** `figure`, `scene3d`, `graph`, `timeline`.
     - **Live Mutations:** `erase`, `recolor`, `restyle`, `move`, `moveTo`, `scale`, `startOver`.
     - **Math & Geometry Helpers:** `W`, `H`, `PI`, `sin`, `cos`, `abs`, `min`, `max`, `sqrt`, `round`, `floor`, `ceil`, `trunc`, `sign`, `pow`, `exp`, `log`, `log2`, `log10`, `hypot`, `atan2`, `atan`, `asin`, `acos`, `tan`, `sinh`, `cosh`, `tanh`, `cbrt`, `range`, `lerp`, `polar`, `add`, `sub`, `mul`, `mid`, `along`, `dist`, `angle`, `perp`, `rotate`, `Math`, `random`, plus all tone constants (`ink`, `grey`, `coral`, `red`, `amber`, `sun`, `leaf`, `green`, `mint`, `teal`, `sky`, `blue`, `indigo`, `purple`, `lilac`, `pink`, `peach`, `brown`, `white`, `cream`, `butter`).
3. **`clicky-draw-pieces.js` (prepended to every user program)**:
   - **Pointers & Callouts:** `point`, `target`, `hover`, `frame`, `ring`, `ink`, `inkCircle`, `write`, `num`, `underline`, `connect`, `trace`, `check`, `cross`, `strike`, `scroll`, `cheer`, `spotlight`, `label`, `tag`.
   - **Cards, Citations & Photos:** `sticky`, `paper`, `receipt`, `clipping`, `legend`, `photo`, `tie`, `source`, `sources`, `jobTicket`, `dreamMedal`.
   - **Host Actions & Audio:** `typeText`, `copyText`, `openPage`, `button`, `handOff`, `sfx`.
   - **Walkthrough & Onboarding Meta:** `lessonTitle`, `plan`, `task`, `waitForUser`, `onScreen`, `tourJob`, `tuckJobs`, `showNotch`, `fadeAway`, `nextQuestions`.

### 2.3 Prompt & Documentation Parity (`brain.ts`, `README.md`, `docs/04`) vs Engine Reality

| Promised Primitive / Parameter | Source Claiming It | Actual Engine Reality | Status |
| :--- | :--- | :--- | :---: |
| `openPage` + `writeOnPage` spawn draggable study cards | [`README.md:20-21`](file:///C:/dev/heyclicky-clone/README.md#L20-L21) | `writeOnPage` **does not exist** (`ReferenceError`). `openPage(url)` opens an HTTP/HTTPS URL in the browser. Study cards are `paper()`, `sticky()`, `receipt()`, `clipping()`. | **FAIL (`AUD-F-001`)** |
| `callout`, `brace`, `table`, `chart` | [`docs/04-drawing-2d-3d-and-ink-engine.md:31,35`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md#L31) | None of `callout`, `brace`, `table`, or `chart` exist in `clicky-draw-engine.js` or `clicky-draw-pieces.js` (`typeof === 'undefined'`). | **FAIL (`AUD-F-001`)** |
| `spot(x, y, label)` | [`docs/04:29,31,181`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md#L29), [`brain.ts:911-914`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L911-L914) | `spot` is **not defined** in `clicky-draw-pieces.js` (only `point(x, y, name)` is defined). Calling `spot()` without an inline polyfill throws `ReferenceError: spot is not defined`. | **FAIL (`AUD-F-002`)** |
| `around: { count: 10, axis: "x"\|"y"\|"z", at: [0,0,0] }` | [`src/backend/src/routes/brain.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L85) | `clicky-draw-engine.js:720` reads `around.center`, **ignoring** `around.at` and defaulting to `[0, 0, 0]`. | **FAIL (`AUD-F-003`)** |
| `mat: "cast" \| "carbon"` | [`src/backend/src/routes/brain.ts:88`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L88) | Neither `"cast"` nor `"carbon"` is in `FIGURE_MATERIALS` (`clicky-draw-engine.js:561`) or `Material3D.Studio` (`Material3D.cs:85`). Both degrade to `#B9BEC4` glossy plastic. | **FAIL (`AUD-F-004`)** |
| `f.cone({ ..., r: 0.6, top: 0.2, h: 0.7 })` | [`src/backend/src/routes/brain.ts:74`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L74) | `SHAPES.cone(o)` in `clicky-three-parts.js:372-377` completely ignores `o.top` and always builds a pointed cone. | **FAIL (`AUD-G-001`)** |
| All other `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` primitives | [`src/backend/src/routes/brain.ts:44-103`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L44-L103) | Verified working across 54 passing programs in `tests/audit/ws-fg/test-2d-engine.mjs`. | **PASS** |

### 2.4 C# Desktop Component Audit (`Kursor.Drawing`)

| Component | File | Key Audit Observations | Status |
| :--- | :--- | :--- | :---: |
| `NativeEngine` & `HostBridge` | [`NativeEngine.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/NativeEngine.cs), [`HostBridge.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Engine/HostBridge.cs) | Runs V8 on dedicated single-threaded `DrawingThread`. Syntax and runtime JS errors inside user programs are caught inside `ClickyDraw.runCode` / `Session.update` and returned in `AnalysisResult.Error` without faulting the V8 engine; outer `Invoke`/`Call` wrappers also catch `ScriptEngineException`. `TextShaper.cs` falls back to `"Segoe UI"` if a requested font family is missing on the system (`FindFamilyName` fallback at lines 103-107). | **PASS** |
| `ScreenPainter` & `SystemComposition` | [`ScreenPainter.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/ScreenPainter.cs), [`SystemComposition.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs), [`StageSwapChain.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/StageSwapChain.cs) | Uses `CanvasDevice.GetSharedDevice()` and `ICompositionDrawingSurfaceInterop.BeginDraw`. Zero handling for `DXGI_ERROR_DEVICE_REMOVED` (`0x887A0005`), `DXGI_ERROR_DEVICE_RESET` (`0x887A0007`), `D2DERR_RECREATE_TARGET`, or `CanvasDevice.DeviceLost`. `Marshal.ThrowExceptionForHR` in `SystemComposition.CopyInto` (lines 165, 180) and `StageSwapChain.Present` (line 158) will throw unhandled `COMException` on GPU reset. | **FAIL (`AUD-F-005`)** |
| `OverlayWindow` | [`OverlayWindow.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs) | Creates layered, click-through (`WS_EX_LAYERED \| WS_EX_TRANSPARENT \| WS_EX_TOPMOST \| WS_EX_TOOLWINDOW \| WS_EX_NOACTIVATE`) Win32 HWNDs. However: (1) `WindowProcedure` ignores `WM_DPICHANGED` (`0x02E0`) and `MoveTo()` does not refresh `scale` (`AUD-F-006`); (2) `ShowInScreenRecordings` setter fires and forgets `ApplyCapturePolicyToAllAsync()` (`AUD-F-007`). | **FAIL (`AUD-F-006`, `AUD-F-007`)** |
| `CardHandles` & `PinnedCards` | [`CardHandles.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/CardHandles.cs), [`PinnedCards.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/PinnedCards.cs) | Implements interactive drag/zoom/close handles and persistent pinned cards after session teardown. Relies on `OverlayWindow.MoveTo` during cross-monitor drag (`AUD-F-006`) and `CanvasDevice.GetSharedDevice()` without device-lost recovery (`AUD-F-005`). | **FAIL (`AUD-F-005`, `AUD-F-006`)** |
| `LiveCaption` | [`LiveCaption.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/LiveCaption.cs) | Renders debug beat/statement chips on an overlay window. Uses `CanvasDevice.GetSharedDevice()` without device-lost handling (`AUD-F-005`). | **FAIL (`AUD-F-005`)** |
| `SoundPlayer` | [`SoundPlayer.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Audio/SoundPlayer.cs), [`AudioOutput.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Audio/AudioOutput.cs) | Mixes procedural stroke loops (`felt`, `pencil`, `writing`, `eraser`) and accent clips (`tada`, `ding`, etc.) at 48 kHz stereo float via WASAPI shared mode. Gracefully no-ops (`available = false`) if WASAPI audio initialization fails or if an accent WAV file is missing (`clips.TryGetValue` returns false). | **PASS** |
| `LessonVideoRecorder` & `Mp4VideoWriter` | [`LessonVideoRecorder.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/LessonVideoRecorder.cs), [`Mp4VideoWriter.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/Mp4VideoWriter.cs) | Records desktop + WASAPI loopback + mic to H.264/AAC MP4 via Media Foundation `IMFSinkWriter`. `Mp4VideoWriter` is `public` but does not validate odd/zero dimensions and computes `width * height * 6` in 32-bit signed `int` before casting to `long`; `LessonVideoRecorder.Cancel()` runs background file deletion without awaiting in `Dispose()` (`AUD-F-008`). | **FAIL (`AUD-F-008`)** |

---

## 3. WS-G: 3D Engine Audit (`Kursor.Drawing3D` + `clicky-three.js` / `clicky-three-parts.js`)

### 3.1 Complete Enumeration of `src/desktop/Kursor.Drawing3D/` (42 C# Files)

| Namespace / Folder | Files (42 total) | Audit Status |
| :--- | :--- | :---: |
| `HeyClicky.Drawing3D` (42 files) | `AmbientCG.cs`, `BuiltShape.cs`, `Camera3D.cs`, `CameraCommand.cs`, `ClickyThreeKernel.cs`, `CompiledShader.cs`, `ContactShadow.cs`, `Drawing3DBridge.cs`, `EnvironmentMap.cs`, `FigureRenderer.cs`, `Framing.cs`, `Geometry.cs`, `GpuBuffer.cs`, `GpuDevice.cs`, `GpuMesh.cs`, `GpuTexture.cs`, `IBL.cs`, `IStageSurfaceHost.cs`, `Label3D.cs`, `LabelLayout.cs`, `LivePose.cs`, `Log.cs`, `Material3D.cs`, `Math3D.cs`, `MeshData.cs`, `Motion3D.cs`, `MotionKind.cs`, `PaperCard.cs`, `PaperTheme.cs`, `Part3D.cs`, `PlacedLabel.cs`, `Pipelines.cs`, `ProceduralTextures.cs`, `ReadoutBadge.cs`, `Scene3D.cs`, `ShaderCache.cs`, `Shaders.cs`, `SharedSurface.cs`, `Silhouette.cs`, `Spring.cs`, `StrokeFont.cs`, `ThreeStage.cs` | Audited (`FAIL` in `ThreeStage.cs` → `AUD-G-002`; `ClickyThreeKernel.cs` → `AUD-G-003`; `Pipelines.cs`/`GpuDevice.cs` → `AUD-G-004`) |

### 3.2 All 16 Procedural Shape Types (+ `cylinder` Alias) & Modifiers in `clicky-three-parts.js`

Verified by [`tests/audit/ws-fg/test-3d-engine.mjs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-fg/test-3d-engine.mjs) (evidence in [`docs/audit/evidence/ws-fg/test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json)):

| # | Shape Kind | Normal Params Test | Vertex / Index Count | Parameter Extremes Tested | Status |
| :---: | :--- | :--- | :---: | :--- | :---: |
| 1 | `box` | `shape_01_box_normal` (`size: [1.4,0.9,1.1], bevel: 0.08`) | 600 verts / 3,132 indices | `size: [0,0,0]`, negative `size: [-2,-3,-1], bevel: -0.5` | **PASS** |
| 2 | `cyl` (`cylinder`) | `shape_02_cyl_normal` & `shape_02b_cylinder_alias` | 627 verts / 3,552 indices | `NaN` / `±Infinity` radius, top, height, seg | **PASS** |
| 3 | `cone` | `shape_03_cone_normal` (`r: 0.8, h: 1.5, bevel: 0.04`) | 462 verts / 2,592 indices | `top: 0.4` ignored (`AUD-G-001`, byte-identical to pointed cone) | **FAIL (`AUD-G-001`)** |
| 4 | `tube` | `shape_04_tube_normal` (`r: 0.9, inner: 0.6, h: 1.4, arc: 270`) | 1,734 verts / 6,120 indices | Inner/outer radius clamping, partial arc | **PASS** |
| 5 | `arrow` | `shape_05_arrow_normal` (`points: 3, r: 0.08, head: 0.25`) | 1,105 verts / 6,192 indices | Curved multi-point shaft + merged cone head | **PASS** |
| 6 | `lathe` | `shape_06_lathe_normal` (6 profile points, `seg: 36`) | 222 verts / 1,080 indices | Empty `points: []` fallback, `thick: 0.12` solidify | **PASS** |
| 7 | `sphere` | `shape_07_sphere_normal` (`r: 0.85, seg: 32`) | 725 verts / 4,032 indices | `seg: 999999` clamped to 128, `arc: 270, dome: true, thick: 0.1` | **PASS** |
| 8 | `torus` | `shape_08_torus_normal` (`r: 1.0, tube: 0.25, arc: 300`) | 1,825 verts / 10,368 indices | Partial arc + axis orientation | **PASS** |
| 9 | `capsule` | `shape_09_capsule_normal` (`r: 0.45, h: 1.6`) | 625 verts / 3,456 indices | Axis rotation | **PASS** |
| 10 | `pipe` | `shape_10_pipe_normal` (4 3D spline points, `r: 0.14`) | 1,649 verts / 9,216 indices | Single point `[[1,2,3]]` fallback to 2-point segment | **PASS** |
| 11 | `spring` | `shape_11_spring_normal` (`r: 0.7, wire: 0.08, h: 1.8, coils: 6`) | 2,651 verts / 14,400 indices | `coils: 100000, r: 0, wire: 0, h: -5` clamped safely to 60 coils | **PASS** |
| 12 | `gear` | `shape_12_gear_normal` (`r: 1.0, teeth: 18, w: 0.35, hole: 0.3`) | 2,952 verts / 5,904 indices | `teeth: 5000, w: 0, hole: 5.0` clamped to 200 teeth | **PASS** |
| 13 | `extrude` | `shape_13_extrude_svg_with_hole` (SVG path + hole + bevel) | 408 verts / 816 indices | Invalid SVG path (`M NaN Infinity Z`), collinear points, `>120k` vertex cap throw | **PASS** |
| 14 | `blob` | `shape_14_blob_metaballs` (3 metaballs, marching cubes `softShape`) | 2,216 verts / 13,284 indices | `balls: []` fallback to organic icosahedron; 500 balls clamped | **PASS** |
| 15 | `poly` | `shape_15_poly_platonic_and_subdivided` (`faces: 12, detail: 1`) | 180 verts / 540 indices | `faces: 999, r: -5, detail: 999` clamped to `detail <= 3` | **PASS** |
| 16 | `surface` | `shape_16_surface_parametric_grid` (3×3 3D grid) | 9 verts / 24 indices | Empty `grid: []` fallback to `PlaneGeometry`, `thick: 0.15` solidify | **PASS** |

**Modifiers Verified:**
- `thick` (`solidify` in `clicky-three-parts.js:111-183`): Tested on open `lathe`, `surface` sheet, and cut `sphere` (`mod_01`, `mod_02`, `mod_05`).
- `stretch` & `mirror` (`clicky-three-parts.js:650-655` + `rewind` at `91-104`): Verified non-uniform scale and negative-determinant triangle winding reversal (`mod_03`, `mod_04`, `mod_06`).
- `around`, `copies`, `mirror`, `cut` DSL expansion (`clicky-draw-engine.js:697-762`): Verified in `dslModifierExpansion` (`totalExpandedParts: 12`, `cutSphereArcParam: 270`).
- `MAX_VERTICES = 120,000` guard (`clicky-three-parts.js:20,626`): Confirmed `ext_16_vertex_cap_120k_guard_throws` throws `'part too detailed (162048 vertices)'`.

### 3.3 C# Desktop Component Audit (`Kursor.Drawing3D`)

| Component | File | Key Audit Observations | Status |
| :--- | :--- | :--- | :---: |
| `ClickyThreeKernel` | [`ClickyThreeKernel.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs) | Dedicated background thread with V8 runtime, 300s idle shutdown, 2,500ms per-part execution watchdog (`CancelTimer`), and 256-entry FIFO shape cache. However, failed recipe keys are stored in an unbounded `HashSet<string> failed` that is never cleared even on idle V8 recycle, and `loadFailed` is permanent (`AUD-G-003`). | **FAIL (`AUD-G-003`)** |
| `ThreeStage` | [`ThreeStage.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ThreeStage.cs) | Manages 60fps render loop, camera orbit/zoom/pan interaction, labels, readouts, and `RunFigureTicks` bridge. However, `Dispose()` queues `running.Join(5000); Release();` on `ThreadPool`, which disposes `d2dTarget`, `backBuffer`, `renderer`, and `wake` even if `Join(5000)` times out while `FrameLoop` is still using them (`AUD-G-002`). | **FAIL (`AUD-G-002`)** |
| `Scene3D` & `FigureRenderer` | [`Scene3D.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Scene3D.cs), [`FigureRenderer.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/FigureRenderer.cs) | `FigureRenderer` implements both the DaVinci Illustrated pass (warm paper lighting, procedural grain/hatching, silhouette edge extraction, contact shadow) and Studio PBR pass (GGX Cook-Torrance, procedural environment IBL, studio 3-point + rim lights, SSAO, ACES tonemapping, cut-plane cross-hatching). Disposes per-stage textures and buffers cleanly in `FigureRenderer.Dispose()`. | **PASS** |
| `Pipelines`, `Shaders`, `ShaderCache`, `GpuDevice` | [`Pipelines.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Pipelines.cs), [`Shaders.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Shaders.cs), [`ShaderCache.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ShaderCache.cs), [`GpuDevice.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/GpuDevice.cs) | `Shaders.cs` embeds 4 HLSL sources (`MainHlsl`, `StudioHlsl`, `EdgeHlsl`, `PostHlsl`) with 27 shader entry points; `ShaderCache.cs` caches compiled DXBC bytecode in `%LOCALAPPDATA%\HeyClicky\ ShaderCache\shaders-v1.bin` keyed by SHA-256 of all HLSL sources using atomic `.tmp` rename. However, `GpuDevice.Shared` and `Pipelines.Shared` have no `DXGI_ERROR_DEVICE_REMOVED` recovery and permanently set `buildFailed = true` on any transient error (`AUD-G-004`). | **FAIL (`AUD-G-004`)** |

---

## 4. Detailed Findings (§4 Schema)

### `ID: AUD-F-001`
- **Workstream:** WS-F
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `README.md` and `docs/04` claim nonexistent `writeOnPage` / `callout` / `brace` / `table` / `chart` primitives and misdescribe `openPage` as a study-card builder
- **Component:** `Kursor.Drawing` / `Assets/Engine/clicky-draw-pieces.js` & `clicky-draw-engine.js`
- **Location:** [`README.md:20-21`](file:///C:/dev/heyclicky-clone/README.md#L20-L21); [`docs/04-drawing-2d-3d-and-ink-engine.md:31,35`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md#L31); [`src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js:457-460`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js#L457-L460)
- **Description:** `README.md` (lines 20–21) states that Kursor's Interactive Sticky Notes & Screen Ink Notebook uses `openPage` + `writeOnPage` to spawn draggable, expandable on-screen study cards with markdown headings, bullet lists, and math steps. In reality, `writeOnPage` does not exist anywhere in `clicky-draw-pieces.js`, `clicky-draw-engine.js`, or `clicky-draw-native.js` (calling it throws `ReferenceError: writeOnPage is not defined`), and `openPage(url)` (`clicky-draw-pieces.js:460`) emits an invisible action dot (`{role: 'open', payload: String(url)}`) that opens an HTTP/HTTPS URL in the user's browser on the final beat. Furthermore, `docs/04-drawing-2d-3d-and-ink-engine.md` (lines 31, 35) lists `callout`, `brace`, `table`, and `chart` as engine primitives, none of which exist in `makeApi` or `clicky-draw-pieces.js`.
- **Reproduction Steps:**
  1. Run `node tests/audit/ws-fg/test-2d-engine.mjs`.
  2. Inspect program `21_openPage_url_action_AUD_F_001` (`openPage("https://example.com/docs")`), program `22_writeOnPage_hallucinated_primitive_AUD_F_001` (`writeOnPage("Study Card", "Markdown body")`), and `missingPrimitives` in [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json).
- **Observed Result:** `writeOnPage(...)` fails with `code stopped at line 861: writeOnPage is not defined (kept the 0 marks before it)`; `openPage(...)` emits `{ role: 'open', text: 'https://example.com/docs' }` with `cardCount: 0`; `callout`, `brace`, `table`, and `chart` evaluate to `typeof === 'undefined'`.
- **Expected Result:** Documentation and prompts must accurately reflect the implemented study-card primitives (`paper`, `sticky`, `receipt`, `clipping`, `legend`) and browser action (`openPage(url)`).
- **Impact:** Any developer, prompt, or test relying on `README.md` or `docs/04` to create study cards via `openPage` + `writeOnPage` or draw tables/charts via `table()`/`chart()`/`callout()`/`brace()` triggers runtime `ReferenceError` failures in V8.
- **Remediation:** Update `README.md:20-21` and `docs/04-drawing-2d-3d-and-ink-engine.md:31,35` to document the actual card primitives (`paper`, `sticky`, `receipt`, `clipping`, `legend`) and graph/timeline APIs (`graph`, `timeline`), and remove nonexistent primitive names.
- **Effort:** S
- **Evidence:** [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) (programs 21 & 22; `missingPrimitives` array)

---

### `ID: AUD-F-002`
- **Workstream:** WS-F
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `spot()` primitive is missing from `clicky-draw-pieces.js` despite being documented in `docs/04` and polyfilled in `brain.ts` fallback
- **Component:** `Kursor.Drawing` / `Assets/Engine/clicky-draw-pieces.js` & `src/backend/src/routes/brain.ts`
- **Location:** [`src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js:180`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js#L180); [`src/backend/src/routes/brain.ts:911-914`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L911-L914); [`docs/04-drawing-2d-3d-and-ink-engine.md:29,31,181`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md#L29)
- **Description:** `clicky-draw-pieces.js:180` defines `point(x, y, name)` to ring and label a screen coordinate, but does not define `spot(x, y, name)`. `docs/04-drawing-2d-3d-and-ink-engine.md` (lines 29, 31, 181) documents `spot()` as the primary pointer primitive, and `src/backend/src/routes/brain.ts:911-914` (`buildLocalFallbackProgram`) has to inject an inline workaround `const spot = typeof point === "function" ? point : () => {};` before calling `spot(500, 300, "Kursor Active")`. Any LLM response that emits `spot(...)` without that inline polyfill fails at runtime with `ReferenceError: spot is not defined`.
- **Reproduction Steps:**
  1. Run `node tests/audit/ws-fg/test-2d-engine.mjs`.
  2. Compare program `03_spot_undefined_without_polyfill_AUD_F_002` against `04_spot_with_brain_ts_polyfill` in [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json).
- **Observed Result:** Program `03_spot_undefined_without_polyfill_AUD_F_002` aborts with `code stopped at line 861: spot is not defined (kept the 0 marks before it)`.
- **Expected Result:** `clicky-draw-pieces.js` should export `const spot = point;` as a first-class alias so both `point(x, y, label)` and `spot(x, y, label)` work without per-program polyfills.
- **Impact:** LLM-generated ClickyDraw programs that call `spot(x, y, label)` crash at the `spot()` statement and drop all subsequent drawing marks in that beat.
- **Remediation:** Add `const spot = point` in `src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-pieces.js` right after `const point = ...` (line 184).
- **Effort:** S
- **Evidence:** [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) (programs 03 & 04)

---

### `ID: AUD-F-003`
- **Workstream:** WS-F
- **Severity:** P1
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` in `brain.ts` teaches `around: { count, axis, at }` instead of `around.center`, breaking non-origin circular 3D arrays
- **Component:** Backend Prompt (`src/backend/src/routes/brain.ts`) vs `clicky-draw-engine.js`
- **Location:** [`src/backend/src/routes/brain.ts:85`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L85); [`src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js:706,718-720`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L718-L720)
- **Description:** In `src/backend/src/routes/brain.ts:85`, `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` instructs the LLM to repeat 3D parts around an axis using `around: { count: 10, axis: "x"|"y"|"z", at: [0,0,0] }`. However, `clicky-draw-engine.js:720` reads `around.center`, not `around.at`: `const center = v3(around.center, [0, 0, 0])`. When the LLM follows the system prompt and passes `at: [x, y, z]` inside `around`, `around.center` is `undefined` and silently defaults to `[0, 0, 0]`, revolving the parts around the world origin instead of the specified pivot.
- **Reproduction Steps:**
  1. Run `node tests/audit/ws-fg/test-2d-engine.mjs`.
  2. Inspect program `34_figure_materials_and_around_audit_AUD_F_003_004` in [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json), where `f.torus({ at: [2, 1, 0], ..., around: { count: 4, axis: "y", at: [2, 0, 0] } })` is executed.
- **Observed Result:** Instead of revolving around `[2, 0, 0]` (staying at `x = 2`), the 4 copies revolve at radius 2 around `[0, 0, 0]`, placing the 180° copy at `[-2, 1, 0]` (`aroundWithAtPositions[2][0] === -2`).
- **Expected Result:** `brain.ts:85` should teach `center: [0,0,0]` (and `clicky-draw-engine.js:720` should accept `v3(around.center, null) || v3(around.at, [0, 0, 0])`).
- **Impact:** Every 3D figure generated by OneBrain that uses `around` on an off-origin subassembly scatters the repeated parts around `[0, 0, 0]`.
- **Remediation:** Update `src/backend/src/routes/brain.ts:85` to `around: { count: 10, axis: "x"|"y"|"z", center: [0,0,0] }` and accept `around.center || around.at` in `clicky-draw-engine.js:720`.
- **Effort:** S
- **Evidence:** [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) (program 34 `custom.aroundWithAtPositions`)

---

### `ID: AUD-F-004`
- **Workstream:** WS-F
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` in `brain.ts` teaches unsupported 3D material names `"cast"` and `"carbon"`
- **Component:** Backend Prompt (`src/backend/src/routes/brain.ts`) vs `clicky-draw-engine.js` & `Material3D.cs`
- **Location:** [`src/backend/src/routes/brain.ts:88`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L88); [`src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js:561-562,635-641`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-draw-engine.js#L561-L562); [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Material3D.cs:85-211`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Material3D.cs#L85-L211)
- **Description:** `src/backend/src/routes/brain.ts:88` instructs the LLM that valid 3D materials include `"cast"` and `"carbon"`. Neither exists in `FIGURE_MATERIALS` (`clicky-draw-engine.js:561-562`, which instead has `'iron'` and `'black'`) or in `Material3D.Studio` (`Material3D.cs:85-211`). When `mat: "cast"` or `mat: "carbon"` is used, `FIGURE_MATERIALS.includes(kind)` is `false`, so `clicky-draw-engine.js:638-640` rewrites `material` to `{ mat: 'gloss', tint: 'cast' }` (or `'carbon'`). Subsequently, `Material3D.Studio` fails to parse `'cast'`/`'carbon'` as a hex color in `Math3D.LinearColor` and renders light-grey glossy plastic (`#B9BEC4`) instead of dark cast iron or matte carbon fiber.
- **Reproduction Steps:**
  1. Run `node tests/audit/ws-fg/test-2d-engine.mjs`.
  2. Inspect program `34_figure_materials_and_around_audit_AUD_F_003_004` in [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json).
- **Observed Result:** `f.box({ mat: "cast" })` produces `material: { mat: "gloss", tint: "cast" }`; `f.sphere({ mat: "carbon" })` produces `material: { mat: "gloss", tint: "carbon" }`.
- **Expected Result:** `brain.ts:88` and `FIGURE_MATERIALS` / `Material3D.Studio` must agree on material preset names (`"iron"` / `"black"`, or add `"cast"` and `"carbon"` to `FIGURE_MATERIALS` and `Material3D.Studio`).
- **Impact:** 3D parts assigned `mat: "cast"` or `mat: "carbon"` by OneBrain lose their dark metallic/matte appearance and render as pale grey glossy plastic.
- **Remediation:** Replace `"cast"` and `"carbon"` with `"iron"` and `"black"` in `src/backend/src/routes/brain.ts:88`, or add `"cast"` and `"carbon"` aliases to `FIGURE_MATERIALS` and `Material3D.Studio`.
- **Effort:** S
- **Evidence:** [`docs/audit/evidence/ws-fg/test-2d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-2d-engine-output.json) (program 34 `custom.castMaterial` & `custom.carbonMaterial`)

---

### `ID: AUD-F-005`
- **Workstream:** WS-F
- **Severity:** P1
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** No `DXGI_ERROR_DEVICE_REMOVED` / `DeviceLost` recovery across `ScreenPainter`, `SystemComposition`, `LiveCaption`, `PinnedCards`, `CardHandles`, or `StageSwapChain`
- **Component:** `Kursor.Drawing` (Direct2D / Win2D / DirectComposition)
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs:99-188`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs#L99-L188); [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/ScreenPainter.cs:402-460`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/ScreenPainter.cs#L402-L460); [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/StageSwapChain.cs:147-159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/StageSwapChain.cs#L147-L159); [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/LiveCaption.cs:83,310`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/LiveCaption.cs#L83); [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/PinnedCards.cs:455`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/PinnedCards.cs#L455)
- **Description:** `Kursor.Drawing` uses `CanvasDevice.GetSharedDevice()` (Win2D) in `ScreenPainter`, `LiveCaption`, `PinnedCards`, `CardHandles`, and `SystemComposition`, plus raw COM `ICompositionDrawingSurfaceInterop.BeginDraw` / `ID2D1DeviceContext` in `SystemComposition.CopyInto` (`SystemComposition.cs:143-188`) and `IDXGISwapChain1.Present` in `StageSwapChain.Present` (`StageSwapChain.cs:158`). None of these classes subscribe to `CanvasDevice.DeviceLost` or catch `DXGI_ERROR_DEVICE_REMOVED` (`0x887A0005`) / `DXGI_ERROR_DEVICE_RESET` (`0x887A0007`) / `D2DERR_RECREATE_TARGET` (`0x8899000C`). In `SystemComposition.CopyInto` (lines 165, 180), `Marshal.ThrowExceptionForHR` throws an unhandled `COMException` on the UI thread when `BeginDraw` or `EndDraw` returns device-removed.
- **Reproduction Steps:**
  1. Inspect `SystemComposition.cs:143-188`, `ScreenPainter.cs`, `LiveCaption.cs:83,310`, `PinnedCards.cs:455`, `CardHandles.cs:796`, and `StageSwapChain.cs:147-159`.
  2. Search `src/desktop/Kursor.Drawing/` for `DeviceLost`, `0x887A0005`, `0x8899000C`, or `IsDeviceLost` (0 matches).
- **Observed Result:** Zero handlers or recovery paths exist for GPU driver resets, TDR events, or display adapter transitions.
- **Expected Result:** Catch `COMException` for `DXGI_ERROR_DEVICE_REMOVED` / `DXGI_ERROR_DEVICE_RESET` / `D2DERR_RECREATE_TARGET`, invoke `CanvasDevice.RaiseDeviceLost()`, and recreate `CompositionGraphicsDevice` and swap chains.
- **Impact:** Any GPU driver update, TDR reset, laptop hybrid-GPU switch, or RDP reconnect while overlays are active causes unhandled `COMException` crashes or permanently blank overlays.
- **Remediation:** Handle `DXGI_ERROR_DEVICE_REMOVED`, `DXGI_ERROR_DEVICE_RESET`, and `D2DERR_RECREATE_TARGET` in `SystemComposition.CopyInto` and `StageSwapChain.Present`, and recreate `CompositionGraphicsDevice` / Win2D resources when `CanvasDevice.GetSharedDevice()` reports device loss.
- **Effort:** M
- **Evidence:** Static source audit of [`SystemComposition.cs:99-188`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing/SystemComposition.cs#L99-L188) and [`StageSwapChain.cs:147-159`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Painter/StageSwapChain.cs#L147-L159)

---

### `ID: AUD-F-006`
- **Workstream:** WS-F
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `OverlayWindow` ignores `WM_DPICHANGED` (`0x02E0`) and `MoveTo()` does not refresh `scale` across mixed-DPI monitors
- **Component:** `Kursor.Drawing` / `HeyClicky.Drawing.Overlay.OverlayWindow`
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs:220-226,338-407,462-466`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs#L220-L226)
- **Description:** `OverlayWindow.Fit(int widthPx, int heightPx)` (`OverlayWindow.cs:220-226`) queries `GetDpiForWindow(Handle)` and updates `scale = dpi / 96f`, `Root.Scale`, and `Root.Size`. However, `Fit()` is only called from `ShowOver()` and `Reframe()`. `WindowProcedure` (`OverlayWindow.cs:338-407`) does not handle `WM_DPICHANGED` (`0x02E0`), and `MoveTo(int x, int y)` (`OverlayWindow.cs:462-466`, called during interactive card dragging in `CardHandles.ContinueDrag` and `PinnedCards`) moves the HWND via `SetWindowPos` without updating `scale` or `Root.Scale` when crossing onto a monitor with a different DPI.
- **Reproduction Steps:**
  1. Inspect `OverlayWindow.cs:220-226` (`Fit`), `OverlayWindow.cs:338-407` (`WindowProcedure`), and `OverlayWindow.cs:462-466` (`MoveTo`).
- **Observed Result:** `MoveTo` calls `SetWindowPos` with `SWP_NOSIZE` and never re-evaluates `GetDpiForWindow(Handle)`, leaving `OverlayWindow.Scale` and `Root.Scale` at the source monitor's DPI scale.
- **Expected Result:** `WindowProcedure` should handle `WM_DPICHANGED` (`0x02E0`) and `MoveTo` should check `GetDpiForWindow(Handle)` and re-run `Fit` if the monitor DPI changed.
- **Impact:** Dragging pinned study cards or moving interactive overlays between a 100% DPI external monitor and a 150%/200% DPI laptop screen results in mis-scaled visuals and offset pointer hitboxes.
- **Remediation:** Handle `WM_DPICHANGED` (`0x02E0`) in `OverlayWindow.WindowProcedure` and check `GetDpiForWindow(Handle)` in `OverlayWindow.MoveTo` to update `scale`, `Root.Scale`, and window pixel bounds when DPI changes.
- **Effort:** S
- **Evidence:** Static source audit of [`OverlayWindow.cs:220-226,338-407,462-466`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs#L220-L226)

---

### `ID: AUD-F-007`
- **Workstream:** WS-F
- **Severity:** P3
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `OverlayWindow.ShowInScreenRecordings` property setter fires and forgets `ApplyCapturePolicyToAllAsync()` without observing exceptions
- **Component:** `Kursor.Drawing` / `HeyClicky.Drawing.Overlay.OverlayWindow`
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs:86-97,114-141`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs#L86-L97)
- **Description:** In `OverlayWindow.cs:94`, the static property setter `ShowInScreenRecordings` invokes `ApplyCapturePolicyToAllAsync();` without awaiting the returned `Task` or attaching an exception continuation. Moreover, callers such as `LessonVideoRecorder.Start` (`LessonVideoRecorder.cs:64`) set `OverlayWindow.ShowInScreenRecordings = true` and immediately assume `SetWindowDisplayAffinity` has been updated on all overlay HWNDs, even though `ApplyCapturePolicyToAllAsync` marshals asynchronously via `DispatcherQueue.EnqueueAsync` when called off the UI thread.
- **Reproduction Steps:**
  1. Inspect `OverlayWindow.cs:86-97` and `LessonVideoRecorder.cs:64-65`.
- **Observed Result:** `ApplyCapturePolicyToAllAsync()` is called as a fire-and-forget `Task` inside a synchronous property setter, creating both an unobserved-exception hazard and a race with `LessonVideoRecorder.Start`.
- **Expected Result:** `ApplyCapturePolicyToAllAsync` should wrap its body in a `try/catch` and `LessonVideoRecorder` should ensure capture affinity is applied before capturing frames.
- **Impact:** When `LessonVideoRecorder.Start` is called from a background thread, the first recorded frames can still have `WDA_EXCLUDEFROMCAPTURE` set on overlay windows, omitting the drawing overlay from the start of the MP4.
- **Remediation:** Add a `try/catch` inside `ApplyCapturePolicyToAllAsync` and expose an awaitable `SetShowInScreenRecordingsAsync(bool)` method for `LessonVideoRecorder`.
- **Effort:** S
- **Evidence:** Static source audit of [`OverlayWindow.cs:86-97`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Overlay/OverlayWindow.cs#L86-L97) and [`LessonVideoRecorder.cs:64`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/LessonVideoRecorder.cs#L64)

---

### `ID: AUD-F-008`
- **Workstream:** WS-F
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `Mp4VideoWriter` lacks input dimension validation (odd/zero dimensions) and overflows `int` bitrate arithmetic before `long` cast
- **Component:** `Kursor.Drawing` / `HeyClicky.Drawing.Media.Mp4VideoWriter` & `LessonVideoRecorder`
- **Location:** [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/Mp4VideoWriter.cs:5,41-60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/Mp4VideoWriter.cs#L41-L60); [`src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/LessonVideoRecorder.cs:158-167`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/LessonVideoRecorder.cs#L158-L167)
- **Description:** `Mp4VideoWriter` (`Mp4VideoWriter.cs:5`) is a `public sealed class` whose constructor (`Mp4VideoWriter.cs:41-60`) passes `width` and `height` directly to Media Foundation's H.264 encoder without validating that `width > 0`, `height > 0`, and both are even integers (H.264 requires even width/height). Furthermore, line 50 computes `Math.Min(2147483647L, width * height * 6)` where `width * height * 6` is evaluated in 32-bit signed `int` arithmetic before widening to `long` (`2147483647L`). Additionally, in `LessonVideoRecorder.Cancel()` (`LessonVideoRecorder.cs:158-167`), `Task.Run` joins the recording thread and deletes the output file asynchronously in the background without `Dispose()` waiting on that task.
- **Reproduction Steps:**
  1. Inspect `Mp4VideoWriter.cs:41-60` and `LessonVideoRecorder.cs:158-174`.
- **Observed Result:** Passing odd dimensions to `new Mp4VideoWriter(path, 1921, 1080)` throws raw Media Foundation `COMException` in `MF.Check`, and `width * height * 6` uses 32-bit `int` multiplication despite the `2147483647L` `long` literal.
- **Expected Result:** `Mp4VideoWriter` should clamp/round dimensions to even positive integers (or throw `ArgumentOutOfRangeException`), compute `(long)width * height * 6L`, and `LessonVideoRecorder.Dispose()` should wait for any pending `Cancel()` task.
- **Impact:** Direct callers of `Mp4VideoWriter` with non-even monitor crop sizes fail with raw `COMException`, and `LessonVideoRecorder.Cancel()` can race with immediate file cleanup.
- **Remediation:** Validate and round `width` and `height` to even positive bounds in `Mp4VideoWriter`, change bitrate calculation to `(uint)Math.Min(2147483647L, (long)width * height * 6L)`, and track the cancellation `Task` in `LessonVideoRecorder`.
- **Effort:** S
- **Evidence:** Static source audit of [`Mp4VideoWriter.cs:41-60`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/Mp4VideoWriter.cs#L41-L60) and [`LessonVideoRecorder.cs:158-174`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/HeyClicky.Drawing.Media/LessonVideoRecorder.cs#L158-L174)

---

### `ID: AUD-G-001`
- **Workstream:** WS-G
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` in `brain.ts` teaches `top` radius on `f.cone(...)`, which `clicky-three-parts.js` completely ignores
- **Component:** Backend Prompt (`src/backend/src/routes/brain.ts`) vs `clicky-three-parts.js`
- **Location:** [`src/backend/src/routes/brain.ts:74`](file:///C:/dev/heyclicky-clone/src/backend/src/routes/brain.ts#L74); [`src/desktop/Kursor.Drawing/Assets/Engine/clicky-three-parts.js:372-377`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing/Assets/Engine/clicky-three-parts.js#L372-L377)
- **Description:** In `src/backend/src/routes/brain.ts:74`, `ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS` teaches the LLM to build truncated cones/nozzles using `f.cone({ at: [x,y,z], r: 0.6, top: 0.2, h: 0.7, axis: "x"|"y"|"z", mat: "alu", name: "Nozzle" })`. However, `SHAPES.cone(o)` in `clicky-three-parts.js:372-377` never reads `o.top`—it always constructs a pointed cone with apex `[0, h / 2]` (`new T.ConeGeometry(r, h, ...)`). Only `SHAPES.cyl(o)` (`clicky-three-parts.js:365-371`) reads `o.top` to construct a frustum.
- **Reproduction Steps:**
  1. Run `node tests/audit/ws-fg/test-3d-engine.mjs`.
  2. Inspect `coneTopParameterIgnoredByteIdentical` in [`docs/audit/evidence/ws-fg/test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json).
- **Observed Result:** `clickyThreeBuild('cone', JSON.stringify({ r: 0.8, top: 0.4, h: 1.2, seg: 24 }))` produces a vertex buffer that is 100% byte-for-byte identical to `clickyThreeBuild('cone', JSON.stringify({ r: 0.8, h: 1.2, seg: 24 }))` (`coneTopParameterIgnoredByteIdentical: true`).
- **Expected Result:** `SHAPES.cone(o)` should delegate to `SHAPES.cyl(o)` when `typeof o.top === 'number' && o.top > 0`, or `brain.ts:74` should remove `top: 0.2` from `f.cone(...)`.
- **Impact:** Whenever OneBrain follows its own prompt example (`f.cone({ r: 0.6, top: 0.2, h: 0.7, name: "Nozzle" })`), nozzles and frustums render as sharp pointed cones instead of truncated cones.
- **Remediation:** In `clicky-three-parts.js:372`, if `num(o.top, 0) > 0`, forward `SHAPES.cone(o)` to `SHAPES.cyl(o)`, and remove misleading `top: 0.2` from `f.cone` in `src/backend/src/routes/brain.ts:74`.
- **Effort:** S
- **Evidence:** [`docs/audit/evidence/ws-fg/test-3d-engine-output.json`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-fg/test-3d-engine-output.json) (`coneTopParameterIgnoredByteIdentical: true`, test `ext_04_cone_with_top_param_AUD_G_001`)

---

### `ID: AUD-G-002`
- **Workstream:** WS-G
- **Severity:** P2
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `ThreeStage.Dispose()` disposes D3D11/D2D1 GPU resources on `ThreadPool` even if `frameThread.Join(5000)` times out, racing with active rendering
- **Component:** `Kursor.Drawing3D` / `HeyClicky.Drawing3D.ThreeStage`
- **Location:** [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ThreeStage.cs:1083-1120`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ThreeStage.cs#L1083-L1120)
- **Description:** In `ThreeStage.Dispose()` (`ThreeStage.cs:1083-1107`), if `frameThread.IsAlive` is true, `Dispose()` queues a background work item via `ThreadPool.QueueUserWorkItem`: `running.Join(5000); Release();`. If `running.Join(5000)` times out after 5 seconds (for example, if `frameThread` is blocked in `WaitForCompositorFrame` or V8 `RunTicks`), `Release()` (`ThreeStage.cs:1110-1120`) still executes unconditionally on the ThreadPool thread, calling `d2dTarget.Dispose()`, `backBuffer.Dispose()`, `renderer.Dispose()`, and `wake.Dispose()` while `frameThread` is still running and accessing those exact native COM pointers (`d2d`, `renderer`, `backBuffer`, `wake`).
- **Reproduction Steps:**
  1. Inspect `ThreeStage.cs:1083-1120` (`Dispose` and `Release`) and `ThreeStage.cs:1330-1375` (`FrameLoop`).
- **Observed Result:** `Release()` is called unconditionally after `running.Join(5000)` without checking whether `running.Join(5000)` returned `true` or `false`, and without synchronizing with `RenderFrame()`.
- **Expected Result:** `Release()` must only execute after `frameThread` has actually terminated (or inside a `finally` block at the exit of `FrameLoop` itself) so native D3D11/D2D1 COM objects are never disposed while `FrameLoop` is executing.
- **Impact:** Use-after-free native access violation (`0xC0000005`) or `ObjectDisposedException` on `wake` if a `ThreeStage` frame takes longer than 5 seconds during shutdown.
- **Remediation:** Move `Release()` into a `finally` block at the end of `FrameLoop()` when `disposed` is set, rather than calling `Release()` from an external `ThreadPool` thread after a bounded `Join(5000)`.
- **Effort:** S
- **Evidence:** Static source audit of [`ThreeStage.cs:1083-1120`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ThreeStage.cs#L1083-L1120)

---

### `ID: AUD-G-003`
- **Workstream:** WS-G
- **Severity:** P3
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `ClickyThreeKernel` permanently caches failed recipe keys in an unbounded `failed` HashSet and never recovers from `loadFailed`
- **Component:** `Kursor.Drawing3D` / `HeyClicky.Drawing3D.ClickyThreeKernel`
- **Location:** [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs:30,118-121,179-190,236-238`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L30)
- **Description:** `ClickyThreeKernel` (`ClickyThreeKernel.cs:30`) maintains a `HashSet<string> failed = new HashSet<string>()` of failed `"kind:recipeJson"` keys. While successful geometries in `shapes` are capped at `MaxCachedShapes = 256` with FIFO eviction (`CacheShape`, lines 142–158), `failed` has no size cap or eviction policy, and is never cleared even when the V8 runtime idles out after 300 seconds (`Run`, lines 179–190). Additionally, if `LoadEngine()` fails once (`loadFailed = true`, line 222), the kernel is permanently bricked for the lifetime of the process.
- **Reproduction Steps:**
  1. Inspect `ClickyThreeKernel.cs:30,118-158,179-240`.
- **Observed Result:** `failed` grows monotonically across the process lifetime without bound, and `MarkFailed` permanently blacklists keys even if failure was caused by a transient V8 teardown/interrupt.
- **Expected Result:** Cap `failed` (e.g., `<= 256` entries) and clear `failed` when the V8 runtime is recycled after the 300s idle timeout.
- **Impact:** Unbounded memory growth in `failed` over long sessions with malformed LLM recipes, and permanent blacklisting of recipes after any transient V8 error.
- **Remediation:** Clear `failed` when disposing the idle V8 engine in `Run()` and cap `failed.Count` to `256` entries in `MarkFailed()`.
- **Effort:** S
- **Evidence:** Static source audit of [`ClickyThreeKernel.cs:30,118-240`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/ClickyThreeKernel.cs#L30)

---

### `ID: AUD-G-004`
- **Workstream:** WS-G
- **Severity:** P3
- **Confidence:** Confirmed
- **Status:** `FAIL`
- **Title:** `Pipelines.Shared` permanently disables all 3D rendering (`buildFailed = true`) on any transient shader/pipeline error and `GpuDevice.Shared` cannot recover from device loss
- **Component:** `Kursor.Drawing3D` / `HeyClicky.Drawing3D.Pipelines` & `GpuDevice`
- **Location:** [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Pipelines.cs:20,145-165`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Pipelines.cs#L145-L165); [`src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/GpuDevice.cs:19-45`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/GpuDevice.cs#L19-L45)
- **Description:** `GpuDevice.Shared` (`GpuDevice.cs:19`) is a static `Lazy<GpuDevice>` singleton with no mechanism to recreate the `ID3D11Device` or `ID2D1Device` if `GetDeviceRemovedReason()` becomes non-zero. Furthermore, `Pipelines.Shared` (`Pipelines.cs:145-165`) catches any exception during pipeline construction, sets a static `buildFailed = true`, and returns `null` for the remainder of the process lifetime.
- **Reproduction Steps:**
  1. Inspect `GpuDevice.cs:19-45` and `Pipelines.cs:20,145-165`.
- **Observed Result:** Both `GpuDevice.Shared` and `Pipelines.Shared` are one-shot static singletons with no device-loss reset path.
- **Expected Result:** `GpuDevice` should check `D3D.GetDeviceRemovedReason()` and allow recreating the shared `GpuDevice` and `Pipelines` instances after a GPU reset.
- **Impact:** If the D3D11 device is removed or reset, all 3D stages (`ThreeStage.FrameLoop`) catch the exception, set `paused = true` (`ThreeStage.cs:1347`), and 3D rendering ceases until the application is restarted.
- **Remediation:** Replace the immutable `Lazy<GpuDevice>` singleton with a resettable device manager that recreates `GpuDevice` and `Pipelines` when `ID3D11Device.GetDeviceRemovedReason()` indicates device removal.
- **Effort:** M
- **Evidence:** Static source audit of [`GpuDevice.cs:19-45`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/GpuDevice.cs#L19-L45) and [`Pipelines.cs:145-165`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Drawing3D/HeyClicky.Drawing3D/Pipelines.cs#L145-L165)
