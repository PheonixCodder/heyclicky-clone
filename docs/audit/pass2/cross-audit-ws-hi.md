# Pass 2 Independent Cross-Audit — WS-H (Win32 Platform) & WS-I (Core Library, Ink Engine, Analytics & Interop)

- **Cross-Auditor**: `ws-d-brain-vision`
- **Original Author**: `ws-hi-platform-core`
- **Target Artifacts**:
  - [`docs/audit/ws-hi/REPORT.md`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/REPORT.md)
  - [`docs/audit/ws-hi/findings.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/findings.json)
  - [`docs/audit/ws-hi/coverage.json`](file:///C:/dev/heyclicky-clone/docs/audit/ws-hi/coverage.json)
  - [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs)
  - [`docs/audit/evidence/ws-hi/test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt)
- **Machine-Readable Output**: [`docs/audit/pass2/cross-audit-ws-hi.json`](file:///C:/dev/heyclicky-clone/docs/audit/pass2/cross-audit-ws-hi.json)

---

## 1. Executive Summary

We independently re-verified all **15 findings** (`AUD-H-001` through `AUD-H-010` and `AUD-I-001` through `AUD-I-005`) reported by `ws-hi-platform-core` against the decompiled C# source in `src/desktop/Kursor.Platform/`, `src/desktop/Kursor.Core/`, `src/desktop/Kursor.App/`, and `src/desktop/Kursor.Agents/`, as well as the dynamic test runner in [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs).

| Metric | Count | Notes |
| :--- | ---: | :--- |
| **Original Findings Reviewed** | **15** | `AUD-H-001`..`AUD-H-010` (10) + `AUD-I-001`..`AUD-I-005` (5) |
| **`CONFIRMED` (Exact)** | **7** | `AUD-H-005`, `AUD-H-007`, `AUD-H-008`, `AUD-I-002`, `AUD-I-003`, `AUD-I-004`, `AUD-I-005` |
| **`CONFIRMED_WITH_REFINEMENT`** | **8** | `AUD-H-001`, `AUD-H-002`, `AUD-H-003`, `AUD-H-004`, `AUD-H-006`, `AUD-H-009`, `AUD-H-010`, `AUD-I-001` |
| **`DISPUTED`** | **0** | All 15 findings identify real defects in the codebase |
| **New Findings Discovered in Pass 2** | **4** | `AUD-H-011`, `AUD-H-012`, `AUD-I-006`, `AUD-I-007` |
| **Test Runner Assertion Issues Found** | **2** | False-negative assertion in `Program.cs:115` (`AUD-I-002`) and silent `-1` return in `Program.cs:268` (`AUD-I-007`) |

---

## 2. Per-Finding Verification & Refinements (`AUD-H-001`..`AUD-H-010`, `AUD-I-001`..`AUD-I-005`)

| ID | Original Sev | Verdict | Source Verification & Technical Refinements |
| :--- | :---: | :---: | :--- |
| **`AUD-H-001`** | High | `CONFIRMED_WITH_REFINEMENT` | Verified in [`UiaPageText.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UiaPageText.cs#L19-L174), [`UIAutomationHelpers.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/UIAutomationHelpers.cs#L251-L297), and [`WindowsPageReader.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Wiring/WindowsPageReader.cs#L13-L107). **Refinements**: (1) `WindowsPageReader.cs` is located in `src/desktop/Kursor.App/HeyClicky.App.Wiring/WindowsPageReader.cs` (not `Kursor.Core/HeyClicky.Core.Tools/`); (2) `UiaPageText.cs:19-21` uses `NodeCap = 3000` and `WallClock = 1.5s`, reading `CurrentName` on `Text`/`Hyperlink` controls and `UIA_TextPatternId`; (3) `UIAutomationHelpers.ReadBrowserAddress` (`lines 269, 290`) falls back to `FindFirst(TreeScope_Descendants, editCondition)` — the **first `Edit` control anywhere in the browser window** when `OmniboxViewViews` is absent — and reads `UIA_ValueValuePropertyId` (`30045`) without checking `UIA_IsPasswordPropertyId` (`30009`). |
| **`AUD-H-002`** | High | `CONFIRMED_WITH_REFINEMENT` | Verified in [`OverlayHost.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L354-L611). `EndDraw()`, `SwapChain.Present(0u, 0u)`, and `DCompDevice.Commit()` have zero `DXGI_ERROR_DEVICE_REMOVED` / `DXGI_ERROR_DEVICE_RESET` handling, and `OverlayHost.shared` remains non-null after thread death. **Refinement**: `ThreadMain`'s `finally` block (`line 369`) calls `CaptureExclusion.UnregisterCursorChrome(WindowHandle)`. |
| **`AUD-H-003`** | Medium | `CONFIRMED_WITH_REFINEMENT` | Verified in [`OverlayHost.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L96-L604) and [`FramePacer.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/FramePacer.cs#L15). **Additional leak found**: `OverlayHost.CreateGraphics()` (`line 578`) acquires `gpuFenceDevice = D3DDevice.QueryInterfaceOrNull<IDXGIDevice2>()`, and `DestroyGraphics()` (`lines 594-604`) never disposes `gpuFenceDevice`. |
| **`AUD-H-004`** | Medium | `CONFIRMED_WITH_REFINEMENT` | Verified in [`OverlayHost.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Overlay/OverlayHost.cs#L535-L549). `case 736u:` (`WM_DPICHANGED`, `lines 539-540`) returns `new LRESULT(0)` without refreshing display bounds. **Refinement**: `case 126u:` (`WM_DISPLAYCHANGE`, `lines 541-546`) calls `overlayHost?.Post(overlayHost.RefreshDisplays)`. |
| **`AUD-H-005`** | Medium | `CONFIRMED` | Verified in [`GraphicsCaptureSource.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/GraphicsCaptureSource.cs#L25-L115) and [`ScreenCaptureService.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Capture/ScreenCaptureService.cs#L393-L408). Also noted that `CaptureFrameAsync` returns a shared cached `sourceCopyTexture` whose `lock (gpu.Gate)` is released at `line 110` before `TryCaptureWithGraphicsCaptureAsync` re-acquires `lock (gpu.Gate)` at `line 399`. |
| **`AUD-H-006`** | Medium | `CONFIRMED_WITH_REFINEMENT` | Verified in [`LowLevelInputHub.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs#L166-L593) and [`ClickyUserInputBlocker.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ClickyUserInputBlocker.cs#L328-L353). **Refinement**: `CheckHookAlive()` (`lines 581-584`) also bypasses `ClickyUserInputBlocker.ShouldHoldKey` when replaying `recentRawKeys` (promoted to new finding `AUD-H-012` below). |
| **`AUD-H-007`** | Low | `CONFIRMED` | Verified in [`ShortcutMonitor.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/ShortcutMonitor.cs#L878-L884). |
| **`AUD-H-008`** | Medium | `CONFIRMED` | Verified in [`DictationTextTarget.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/DictationTextTarget.cs#L569) and [`TextInsertion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/TextInsertion.cs#L341-L395). |
| **`AUD-H-009`** | Low | `CONFIRMED_WITH_REFINEMENT` | Verified in [`EarconPlayer.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Audio/EarconPlayer.cs#L279-L330) and [`PermissionService.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Permissions/PermissionService.cs#L266). **Additional bug**: in `EarconPlayer.DecodeWavToStereo48k` (`lines 290-304`), a malformed WAV chunk with `num4 = -8` (`0xFFFFFFF8`) causes `i += 8 + num4 + (num4 & 1)` to evaluate to `i += 0`, hanging in an infinite loop. |
| **`AUD-H-010`** | High | `CONFIRMED_WITH_REFINEMENT` | Verified all three safety flaws in [`ClickyTakeOverController.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Drive/ClickyTakeOverController.cs#L445-L478) (`recent_yes` 600s silent consent bypass at `line 469`, `actuatingUntil = Now + 30s` blocking user take-back at `lines 963 & 1365`, and `StopBecauseUserSpoke` setting `offerAnsweredBySpeaking` in `PhaseKind.Offered` at `lines 1519-1525`). **Refinement**: `ClickyTakeOverController.cs` is in `src/desktop/Kursor.App/HeyClicky.App.Drive/ClickyTakeOverController.cs` (not `Kursor.Core/HeyClicky.Core.Drive/`). |
| **`AUD-I-001`** | Medium | `CONFIRMED_WITH_REFINEMENT` | Verified in [`WindowsDesktopActions.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Platform/WindowsDesktopActions.cs#L86-L251). **Refinement**: `WindowsDesktopActions.cs` is in `src/desktop/Kursor.Core/HeyClicky.Core.Platform/WindowsDesktopActions.cs` (not `HeyClicky.Core.Tools/`). |
| **`AUD-I-002`** | Medium | `CONFIRMED` | Verified in [`SayLines.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Brain/SayLines.cs#L42-L138). Note: [`tests/audit/ws-hi/Program.cs`](file:///C:/dev/heyclicky-clone/tests/audit/ws-hi/Program.cs#L115) checked `extracted3[0].StartsWith("Firstr\nSecondu0021")` instead of `"Firstr Secondu0021"` (`SayLines.Clean` replaces `\n` with `' '` at `line 133`), causing `test-runner-output.txt` line 30 to print `AUD-I-002 REPRODUCED: False` even though all three sub-tests in lines 20–29 reproduced the bug. |
| **`AUD-I-003`** | High | `CONFIRMED` | Verified in [`ClickyInkParser.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkParser.cs#L1593-L1619) and dynamically reproduced in Suite 1. |
| **`AUD-I-004`** | Medium | `CONFIRMED` | Verified in [`ClickyInkNoteStore.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkNoteStore.cs#L27-L50) and [`ClickyLiveNoteStore.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyLiveNoteStore.cs#L27-L105), and dynamically reproduced in Suite 4 (`50` `InvalidOperationException` failures). |
| **`AUD-I-005`** | Low | `CONFIRMED` | Verified in [`ClickyInkLiveNote.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkLiveNote.cs#L1420), [`ClickyInkRowMotion.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkRowMotion.cs#L472), [`ClickyBrowserBridge.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Browser/ClickyBrowserBridge.cs#L849), and [`McpOAuth.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Agents/HeyClicky.Agents.Mcp/McpOAuth.cs#L551). |

---

## 3. Newly Discovered Findings in Pass 2 (`AUD-H-011`..`AUD-H-012`, `AUD-I-006`..`AUD-I-007`)

### `ID: AUD-H-011` — `RecentFiles.Target` Binary `.lnk` Parser Lacks Bounds Checks and Enables Silent Arbitrary File Exfiltration via Window Titles
- **Severity**: **Medium** | **Status**: `FAIL`
- **Files**:
  - [`src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/RecentFiles.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Win32/RecentFiles.cs#L12-L143)
  - [`src/desktop/Kursor.App/HeyClicky.App.Wiring/WindowsPageReader.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.App/HeyClicky.App.Wiring/WindowsPageReader.cs#L33-L40)
  - [`src/desktop/Kursor.Core/HeyClicky.Core.Documents/DocumentText.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Documents/DocumentText.cs#L24-L35)
- **Evidence**:
  1. `RecentFiles.Target(shortcutPath)` (`RecentFiles.cs:67-118`) manually parses raw Windows Shell Link (`.lnk`) bytes without sufficient bounds checks:
     - Line 78 reads `BitConverter.ToUInt16(array, num2)` when `array.Length == 76` and `(num & 1) != 0` (`ArgumentOutOfRangeException`).
     - Lines 94–95 read 32-bit offsets at `num3 + 28` and `num3 + 32` after only checking `num2 + 28 <= array.Length` at line 80.
     - `ReadUnicode(byte[] data, int at)` (`line 130`) does not check `at >= 0 && at < data.Length`.
  2. `WindowsPageReader.CaptureReferenceAsync` (`WindowsPageReader.cs:33-39`) extracts any filename token from the active window title (e.g. `.env - Visual Studio Code` or `secrets.json - Notepad`), resolves its full path from `%APPDATA%\Microsoft\Windows\Recent\*.lnk`, and `WindowsPageReader.ExtractAsync` (`lines 87-93`) calls `DocumentText.Extract`, which explicitly includes `"env"`, `"conf"`, `"cfg"`, `"ini"`, `"json"`, `"yaml"`, `"yml"`, `"sql"`, `"ps1"`, and extensionless text files (`DocumentText.cs:26-35, 87`) and reads up to 60,000 characters of the local file into the cloud `/brain/turn` payload.

### `ID: AUD-H-012` — `LowLevelInputHub.CheckHookAlive` Raw-Input Replay Bypasses `ClickyUserInputBlocker.ShouldHoldKey`
- **Severity**: **Medium** | **Status**: `FAIL`
- **File**: [`src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Platform/HeyClicky.Platform.Input/LowLevelInputHub.cs#L465-L584)
- **Evidence**:
  - In `LowLevelInputHub.HookCallback` (`line 465`), keyboard events check `if (!ClickyUserInputBlocker.ShouldHoldKey(in keyEvent)) DeliverKey(in keyEvent);`.
  - However, in `CheckHookAlive()` (`lines 581-584`), when buffered `WM_INPUT` keys from `recentRawKeys` are replayed (after 80ms whenever `WH_KEYBOARD_LL` did not observe a key, such as when an elevated window has focus under UIPI), it calls `DeliverKey(in recentRawKeys[i % recentRawKeys.Length])` directly without calling `ClickyUserInputBlocker.ShouldHoldKey`.

### `ID: AUD-I-006` — `DocumentText.ReadSystemDocument` Decompresses ZIP-Based Office Documents Without Uncompressed Size Limits (Zip Bomb / OOM)
- **Severity**: **Medium** | **Status**: `FAIL`
- **File**: [`src/desktop/Kursor.Core/HeyClicky.Core.Documents/DocumentText.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Documents/DocumentText.cs#L182-L247)
- **Evidence**:
  - `DocumentText.ReadSystemDocument` (`line 188`) only checks the compressed file size on disk (`new FileInfo(path).Length > 67108864`), and `ReadEntry(ZipArchiveEntry)` (`lines 242-247`) calls `streamReader.ReadToEnd()` with no uncompressed `entry.Length` check or character cap before running full-string regexes (`WordXmlToText`, `SharedStringItem().Matches`, `DrawingTextRun().Matches`). A small compressed `.docx`/`.xlsx`/`.odt` with a multi-hundred-MB XML entry causes massive LOH allocations or `OutOfMemoryException`.

### `ID: AUD-I-007` — `ClickyInkPatchStream.PartialText` Rejects Unified Diff Hunk Headers (`@@ ... @@`) and Strips First Character of Unprefixed Lines
- **Severity**: **Low** | **Status**: `FAIL`
- **File**: [`src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkPatchStream.cs`](file:///C:/dev/heyclicky-clone/src/desktop/Kursor.Core/HeyClicky.Core.Ink/ClickyInkPatchStream.cs#L49-L81)
- **Evidence**:
  - `ClickyInkPatchStream.PartialText` (`lines 49-67`) takes `text3 = text2.Substring(2, text2.Length - 2).TrimWs()` from every `@@` line and requires a document line to match `Same(list[k], text3)`. Standard unified diff headers (`@@ -2,1 +2,2 @@`) or V4A headers with a trailing `@@` (`@@ Heading @@`) fail to match any document line and return `null` (which caused `WsHiAuditRunner` Suite 3 at [`test-runner-output.txt`](file:///C:/dev/heyclicky-clone/docs/audit/evidence/ws-hi/test-runner-output.txt#L34) to output `ClickyInkPatchStream patched length=-1`).
  - Line 80 also strips the first character of every non-`+` line (`l.Substring(1)`) without verifying that the line begins with `' '` or `'-'`.
