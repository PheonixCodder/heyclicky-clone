using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Ink;
using HeyClicky.Core.Voice;
using HeyClicky.Platform.Audio;
using HeyClicky.Platform.Overlay;
using Windows.Foundation;

namespace WsHiAuditRunner;

public static class Program
{
    private sealed class DummyFrontPageMeasurer : IClickyInkFrontPageMeasurer
    {
        public double ContentWidth => 240.0;
        public double Group(ClickyInkGroup group) => 60.0 + group.Blocks.Count * 24.0;
        public double Table(ClickyInkGroup group, double width) => 120.0;
        public double Headline(ClickyInkDashboardBoard board, double width, bool isMission) => 72.0;
        public double Tiles(IReadOnlyList<ClickyInkLiveNote.Block> blocks, double width) => 84.0;
        public double MissionTileHeight(IReadOnlyList<ClickyInkDocument.Block> blocks) => 64.0;
    }

    public static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("WS-H & WS-I AUDIT TEST & FUZZ RUNNER (Kursor.Platform & Kursor.Core)");
        Console.WriteLine($"Started at: {DateTimeOffset.UtcNow:O}");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        int passedSuites = 0;
        int reproducedBugs = 0;

        // ---------------------------------------------------------------------------
        // SUITE 1: AUD-I-003 Reproduction — ClickyInkParser EmphasisMarks & MarkingFirstLink
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 1] ClickyInkParser.EmphasisMarks & MarkingFirstLink PUA Interpolation Bug (AUD-I-003)");
        {
            string boldInput = "Pay **$420** by Friday";
            string boldCleaned = ClickyInkParser.Clean(boldInput);
            Console.WriteLine($"  Input:    {boldInput}");
            Console.WriteLine($"  Clean():  {boldCleaned}");
            Console.WriteLine($"  Strip():  {ClickyInkParser.Emphasis.Strip(boldCleaned)}");

            string hlInput = "Status is ==CRITICAL== right now";
            string hlCleaned = ClickyInkParser.Clean(hlInput);
            Console.WriteLine($"  Input:    {hlInput}");
            Console.WriteLine($"  Clean():  {hlCleaned}");

            string linkInput = "Read [Security Guide](https://example.com/sec) today";
            string linkMarked = ClickyInkParser.MarkingFirstLink(linkInput);
            Console.WriteLine($"  Input:    {linkInput}");
            Console.WriteLine($"  Marked:   {linkMarked}");

            var doc = ClickyInkParser.Parse("# Sprint Note\n- [ ] Fix **authentication** bug\n- Review ==urgent== PR\n- [Open Portal](https://example.com) now");
            var firstPage = doc.Pages[0];
            Console.WriteLine($"  Parsed Checklist Item[0].Text: {firstPage.Blocks[0].Items[0].Text}");
            Console.WriteLine($"  Parsed List Item[0].Text:      {firstPage.Blocks[1].Items[0].Text}");
            Console.WriteLine($"  Parsed List Item[1].Text:      {firstPage.Blocks[1].Items[1].Text}");

            bool bugConfirmed = boldCleaned.Contains("57345$157346")
                && hlCleaned.Contains("57347$157348")
                && linkMarked.Contains("57349Security Guide57350");
            Console.WriteLine($"  -> AUD-I-003 REPRODUCED: {bugConfirmed}");
            if (bugConfirmed) reproducedBugs++;
        }
        Console.WriteLine();

        // ---------------------------------------------------------------------------
        // SUITE 2: AUD-I-002 Reproduction — SayLines JS Scanner Edge Cases
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 2] SayLines JS Scanner Edge Cases (AUD-I-002)");
        {
            // 2a: Comments & string literals containing say(...)
            string commentAndLiteralCode = @"
// say(""This line is inside a single-line comment and must not be spoken"");
/* step(99); say(""This line is inside a block comment""); */
write(100, 200, ""Example call: say('injected via write string literal')"");
say(""Actual spoken line"");
";
            var extracted1 = SayLines.InOrder(commentAndLiteralCode);
            Console.WriteLine($"  2a (Comments & String Literals) extracted {extracted1.Count} lines:");
            foreach (var l in extracted1) Console.WriteLine($"    - \"{l}\"");

            // 2b: Whitespace before '(' in say (...) and step (...)
            string whitespaceCallCode = @"
step (1);
say (""Line with space before paren"");
say(""Line without space"");
";
            var extracted2 = SayLines.ByStep(whitespaceCallCode);
            Console.WriteLine($"  2b (Whitespace before '(') extracted {extracted2.Count} lines:");
            foreach (var l in extracted2) Console.WriteLine($"    - Step={l.Key}, Text=\"{l.Value}\"");

            // 2c: Escape sequences (\r\n, \uXXXX) and template literals (${expr})
            string escapesAndTemplateCode = "say(\"First\\r\\nSecond\\u0021\");\nsay(`Total is ${2 + 2} items`);";
            var extracted3 = SayLines.InOrder(escapesAndTemplateCode);
            Console.WriteLine($"  2c (Escapes & Template Literals) extracted {extracted3.Count} lines:");
            foreach (var l in extracted3) Console.WriteLine($"    - \"{l.Replace("\n", "\\n").Replace("\r", "\\r")}\"");

            bool bugConfirmed = extracted1.Count == 4
                && extracted2.Count == 1
                && extracted3[0].StartsWith("Firstr\nSecondu0021")
                && extracted3[1].Contains("${2 + 2}");
            Console.WriteLine($"  -> AUD-I-002 REPRODUCED: {bugConfirmed}");
            if (bugConfirmed) reproducedBugs++;
        }
        Console.WriteLine();

        // ---------------------------------------------------------------------------
        // SUITE 3: ClickyInkParser Fuzz & Property Testing (Malformed MD, Unclosed Tags, Deep Nesting, Huge Input)
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 3] ClickyInkParser, ClickyInkLint, ClickyInkPatchStream, ClickyInkMatch Property & Fuzz Suite");
        {
            var sw = Stopwatch.StartNew();
            var fuzzCases = new List<(string Name, string Input, bool IsPartial)>
            {
                ("Empty", "", false),
                ("WhitespaceOnly", "   \r\n\t  \n ", false),
                ("UnclosedInkHtmlTags", "# Title\n<ink-card attr=\"1\">Unclosed tag\n<ink-widget><ink-nested>deep", false),
                ("UnclosedCodeFencePartial", "# Title\n```draw\ncircle 10 20 30\nline 0 0 100 100", true),
                ("UnclosedCodeFenceFull", "# Title\n```python\ndef foo():\n  pass", false),
                ("UnclosedContainerDirective", "# Title\n:::receipt[Dinner]{total=$45}\n- Burger | $20\n- Fries | $10", false),
                ("ChainedInlineDirectives", "::figure[Revenue]{value=$10k} ::status[API]{state=up} ::spark[Trend]{values=\"1,2,3,4\"}", false),
                ("DeeplyIndentedListsAndContainers", string.Join("\n", Enumerable.Range(0, 120).Select(i => new string(' ', (i % 10) * 2) + $"- [x] Item {i} {{now}} [Link](https://example.com/{i})")), false),
                ("ExcessPagesAndBlocks", string.Join("\n", Enumerable.Range(1, 20).Select(p => $"## Page {p} {{board}}\n" + string.Join("\n", Enumerable.Range(1, 50).Select(b => $"::sticky[Note {p}-{b}]{{color=yellow}}")))), false),
                ("MalformedTableRows", "# Table Test\n| Col1 | Col2 | Col3 | Col4 | Col5 |\n|---|---|\n| only one |\n| a | b | c | d | e | f | g |\n| |||||", false),
                ("AllContainerKinds", @"# Comprehensive Note
:::receipt[Invoice]
Item A | $10
Item B : $20
:::
:::agenda[Today]
9:00am - 10:30am — Standup — Room 1
Morning: Deep work
:::
:::letter[Hello World]{to=alice@example.com}
Dear Alice,
Here is the draft.
:::
:::drawing[Arch]
box 10 10 50 50
:::
:::chart[Sales]
Q1 | 100
Q2 | 200
:::
:::review[Approve PR]
Check tests
Verify build
:::
:::form[Config]
Host: localhost
Port: 8080
:::
:::quiz[Study]
Q: What is 2+2?
A: 4
Capital of France | Paris
:::
:::gallery[Mockups]
/tmp/a.png — Option A {pick}
https://example.com/b.png — Option B
:::
:::criteria[Filter]
Price: < $50
Rating: >= 4.5
:::
:::pipeline[Sales Funnel]
Leads | 120
Qualified | 45
Won | 12
:::
:::spark[CPU]
10:00 | 25%
10:05 | 60%
:::
:::tiles
::figure[Users]{value=1200}
::status[DB]{state=healthy}
::progress[Goal]{value=75, max=100}
::tally[Bugs]{value=7}
::streak[Gym]{days=MTWTF--}
::countdown[Launch]{date=2026-12-01}
::run[Daily]{text=""Checked 14 emails""}
::quiet[All good]
::connect[Slack]{app=slack}
::event[Sync]{time=""3:00 PM""}
:::", false),
                ("MalformedTrailingAttributes", "Paragraph with {unclosed=1 and {a=\"unterminated quote, b=2, =3, #tag .cls}", false),
                ("SurrogatePairsInDirectives", "::figure\ud83d\ude80[Launch \ud83c\udf1f]{value=\"\ud83d\udcaf%\"}", false),
                ("HugeInput_200KB", "# Huge Note\n" + string.Concat(Enumerable.Repeat("## Section\n- [ ] Task item with detail and attributes {needs}\n| A | B | C |\n|---|---|---|\n| 1 | 2 | 3 |\n\n", 1800)), false)
            };

            int fuzzPassed = 0;
            foreach (var tc in fuzzCases)
            {
                var doc = ClickyInkParser.Parse(tc.Input, tc.IsPartial);
                var problems = ClickyInkLint.Problems(tc.Input);
                if (doc.Pages.Count <= ClickyInkParser.MaximumPages &&
                    doc.Pages.All(p => p.Blocks.Count <= ClickyInkParser.MaximumBlocksPerPage))
                {
                    fuzzPassed++;
                }
            }

            // Random adversarial fuzzing (250 seeds)
            var rng = new Random(42);
            string[] tokens = { "# ", "## ", "### ", "::", ":::", "```", "```draw", "|", "- [ ] ", "- [x] ", "1. ", "> ", "{", "}", "[", "]", "(", ")", "**", "==", "~~", "`", "<ink-box>", "</ink-box>", "\r\n", "\n", " | ", " — ", ": ", "\ue001", "\ue002", "\ud83d\ude00" };
            for (int seed = 0; seed < 250; seed++)
            {
                var sb = new StringBuilder();
                int len = rng.Next(1, 120);
                for (int k = 0; k < len; k++)
                {
                    sb.Append(tokens[rng.Next(tokens.Length)]);
                    if (rng.Next(4) == 0) sb.Append("word" + rng.Next(100));
                }
                string input = sb.ToString();
                var d1 = ClickyInkParser.Parse(input, isPartial: false);
                var d2 = ClickyInkParser.Parse(input, isPartial: true);
                _ = ClickyInkLint.Problems(input);
                fuzzPassed++;
            }

            // Test ClickyInkPatchStream & ClickyInkLiveNote & ClickyInkDashboardSplit & ClickyInkFrontPagePlanner
            string v1 = "# Project Board\n## Overview {board}\n::figure[Users]{value=100}\n::figure[Revenue]{value=$5k}\n- [ ] First task\n- [ ] Second task\n- [ ] Third task\n- [ ] Fourth task\n| Name | Role | Status | Score |\n|---|---|---|---|\n| Alice | Eng | Active | 95 |\n| Bob | PM | Active | 88 |";
            string diff = "@@ - [ ] Second task\n-- [ ] Second task\n+- [x] Second task\n+- [ ] Added task";
            string? patched = ClickyInkPatchStream.PartialText(v1, "update", diff);
            var liveNote = new ClickyInkLiveNote();
            liveNote.Apply(ClickyInkParser.Parse(v1), 0.0, isEdit: false, startsNewEdit: null, isPartial: false);
            if (patched != null)
            {
                liveNote.Apply(ClickyInkParser.Parse(patched, isPartial: true), 0.5, isEdit: true, startsNewEdit: true, isPartial: true);
            }
            liveNote.Apply(ClickyInkParser.Parse(patched ?? v1, isPartial: false), 1.2, isEdit: true, startsNewEdit: true, isPartial: false);
            liveNote.Finish(1.5);

            var focuses = liveNote.PenFocusList();
            var splitRows = ClickyInkDashboardSplit.Plan(liveNote, g => 80.0);
            var measurer = new DummyFrontPageMeasurer();
            var newspaperPlan = ClickyInkFrontPagePlanner.Make(liveNote, ClickyInkFrontPageVariant.Broadsheet, measurer);
            var missionPlan = ClickyInkFrontPagePlanner.Make(liveNote, ClickyInkFrontPageVariant.MissionControl, measurer);

            // Also test decompiler state machine methods ClickyInkLiveNote.FirstChangedRow and ClickyInkRowMotion.Spans
            var sampleBlock = liveNote.Pages[0].Blocks.First(b => b.Items.Count > 0);
            var firstChanged = ClickyInkLiveNote.FirstChangedRow(sampleBlock, 0.5, 2.0);
            var rowSpans = ClickyInkRowMotion.Spans(sampleBlock.Items[0].Words).ToList();

            sw.Stop();
            Console.WriteLine($"  Executed {fuzzPassed} ClickyInkParser fuzz cases in {sw.ElapsedMilliseconds} ms (0 unhandled exceptions).");
            Console.WriteLine($"  ClickyInkPatchStream patched length={patched?.Length ?? -1}, LiveNote pages={liveNote.Pages.Count}, focuses={focuses.Count}, splitRows={splitRows.Items.Count}, newspaperCols={newspaperPlan.Columns}, missionCols={missionPlan.Columns}, rowSpans={rowSpans.Count}");
            passedSuites++;
        }
        Console.WriteLine();

        // ---------------------------------------------------------------------------
        // SUITE 4: AUD-I-004 Reproduction — ClickyInkNoteStore & ClickyLiveNoteStore Thread-Safety Race
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 4] ClickyInkNoteStore & ClickyLiveNoteStore Concurrent Access & Persistence Test (AUD-I-004)");
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "kursor-audit-ws-hi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var store = new ClickyInkNoteStore(tempDir);
                string notePath = Path.Combine(tempDir, "note.md");
                File.WriteAllText(notePath, "# Initial\n- [ ] Item 1\n- [ ] Item 2\n");

                // Test normal persistence & checkbox toggle
                bool recorded = store.ReadNoteNow(notePath, isEdit: false, DateTimeOffset.UtcNow);
                bool toggled = store.ToggleCheckbox(notePath, "Item 1");
                store.WaitForPendingSaves();
                Console.WriteLine($"  Single-threaded ReadNoteNow={recorded}, ToggleCheckbox={toggled}, Versions={store.Versions(notePath).Count}");

                // Now test concurrent access to ClickyInkNoteStore (simulating SSE StreamPatch / PreloadVersions / UI InkNote reads)
                int exceptionsCaught = 0;
                Exception? firstEx = null;
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var tasks = new List<Task>();
                for (int t = 0; t < 8; t++)
                {
                    int threadId = t;
                    tasks.Add(Task.Run(() =>
                    {
                        int iter = 0;
                        while (!cts.IsCancellationRequested && iter < 400)
                        {
                            iter++;
                            try
                            {
                                string id = Path.Combine(tempDir, $"concurrent_{iter % 5}.md");
                                store.NoteTurnStarted(id, isKept: iter % 2 == 0);
                                store.RecordVersion(id, $"# Note {iter}\n- Item {threadId}-{iter}\n", isEdit: true, isPartial: iter % 3 == 0);
                                _ = store.LiveNote(id);
                                _ = store.SettledNote(id);
                                store.NoteTurnEnded(id);
                                if (iter % 10 == 0)
                                {
                                    store.StartFreshPage(id);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Interlocked.Increment(ref exceptionsCaught) == 1)
                                {
                                    firstEx = ex;
                                }
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                store.WaitForPendingSaves();
                Console.WriteLine($"  Concurrent ClickyInkNoteStore stress test: exceptionsCaught={exceptionsCaught}");
                if (firstEx != null)
                {
                    Console.WriteLine($"  First concurrent exception: {firstEx.GetType().FullName}: {firstEx.Message}");
                    reproducedBugs++;
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
        Console.WriteLine();

        // ---------------------------------------------------------------------------
        // SUITE 5: DrawingCaptionTiming & BrainSseParser & BrainSession Tests
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 5] DrawingCaptionTiming, BrainSseParser, and BrainSession Tests");
        {
            double spokenNormal = DrawingCaptionTiming.SpokenSeconds("Click the blue button on the top right.", 1.0);
            double spokenFast = DrawingCaptionTiming.SpokenSeconds("Click the blue button on the top right.", 2.0);
            double spokenZeroSpeed = DrawingCaptionTiming.SpokenSeconds("Click the blue button.", 0.0);
            double spokenNaN = DrawingCaptionTiming.SpokenSeconds("Click the blue button.", double.NaN);
            double revealNaN = DrawingCaptionTiming.RevealSeconds("Click the blue button.", double.NaN);
            Console.WriteLine($"  DrawingCaptionTiming: normal={spokenNormal:F3}s, fast(2x)={spokenFast:F3}s, zeroSpeed={spokenZeroSpeed:F3}s, NaN_speed={spokenNaN}, NaN_reading={revealNaN}");
            if (double.IsNaN(spokenNaN) || double.IsNaN(revealNaN))
            {
                Console.WriteLine("  -> Note: DrawingCaptionTiming propagates NaN when SpeechSpeed or readingSeconds is NaN.");
            }

            // BrainSseParser chunked UTF-8 & large line test
            var sink = new TestBrainStreamSink();
            {
                string ssePayload =
                    "data: {\"type\":\"text\",\"text\":\"Hello \\\"world\\\"\\nline2\"}\r\n\r\n" +
                    "data: {\"type\":\"status\",\"status\":\"thinking\"}\n\n" +
                    "data: " + new string('a', 20000) + "\n\n" + // > 16KB line forcing buffer growth, invalid JSON ignored
                    "data: [DONE]\n";
                byte[] bytes = Encoding.UTF8.GetBytes(ssePayload);
                using var parser = new BrainSseParser();
                for (int offset = 0; offset < bytes.Length; offset += 7)
                {
                    int len = Math.Min(7, bytes.Length - offset);
                    parser.Feed(bytes.AsSpan(offset, len), sink);
                }
                parser.Complete(sink);
            }
            Console.WriteLine($"  BrainSseParser: textDeltas={sink.TextDeltas.Count} (\"{sink.TextDeltas.FirstOrDefault()?.Replace("\n", "\\n")}\"), events={sink.Events.Count} (\"{sink.Events.FirstOrDefault()}\")");

            // BrainSession 48 -> 32 screenshot compaction test
            string brainTemp = Path.Combine(Path.GetTempPath(), "kursor-brain-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(brainTemp);
            try
            {
                using var session = new BrainSession(null!, _ => Task.FromResult<string?>(null), brainTemp);
                session.StartFreshSession("audit-user");
                string sid = session.SessionId;
                for (int i = 0; i < 50; i++)
                {
                    session.CommitTurn(
                        sid,
                        $"[Turn {i}]",
                        $"Question {i}",
                        $"data:image/jpeg;base64,SCREENSHOT_{i}",
                        new List<JsonNode>(),
                        new List<string>(),
                        wasInterrupted: false,
                        keepReasoning: false,
                        drawingCode: "say(\"hi\");",
                        screenshotFile: $"file_{i}.jpg");
                }
                var snapshot = session.ThreadSnapshotForWorker();
                int remainingScreenshots = snapshot
                    .OfType<JsonObject>()
                    .Where(m => (string?)m["role"] == "user")
                    .SelectMany(m => (m["content"] as JsonArray) ?? new JsonArray())
                    .OfType<JsonObject>()
                    .Count(p => (string?)p["type"] == "input_image");
                Console.WriteLine($"  BrainSession after 50 turns with screenshots: remainingScreenshots={remainingScreenshots} (expected <= {BrainSession.MaximumScreenshotsInThread}, trimmed toward {BrainSession.ScreenshotsKeptAfterTrim})");
                passedSuites++;
            }
            finally
            {
                try { Directory.Delete(brainTemp, recursive: true); } catch { }
            }
        }
        Console.WriteLine();

        // ---------------------------------------------------------------------------
        // SUITE 6: WS-H Platform Tests (EarconPlayer WAV Parser Crash Proof & PenRideTrajectory)
        // ---------------------------------------------------------------------------
        Console.WriteLine("[SUITE 6] WS-H Platform Tests (EarconPlayer.DecodeWavToStereo48k & PenRideTrajectory) (AUD-H-009)");
        {
            // Test PenRideTrajectory physics via reflection (PointD is internal in HeyClicky.Platform.Overlay)
            var traj = new PenRideTrajectory();
            var beginMethod = typeof(PenRideTrajectory).GetMethod("Begin")!;
            var pointDType = beginMethod.GetParameters()[0].ParameterType;
            object ptStart = Activator.CreateInstance(pointDType, 100.0, 100.0)!;
            beginMethod.Invoke(traj, new object[] { ptStart, 0.0, 1920.0, 1080.0 });
            var sample0 = traj.Sample(0.0);
            var sampleEnd = traj.Sample(traj.EndTime);
            Console.WriteLine($"  PenRideTrajectory: endTime={traj.EndTime:F3}s, EndsSettled={traj.EndsSettled}, sample0.Scale={sample0.Scale:F2}, sampleEnd.Scale={sampleEnd.Scale:F2}");

            // Test EarconPlayer.DecodeWavToStereo48k via reflection with malformed WAV (bitsPerSample=0 -> DivideByZeroException)
            var earconType = typeof(WasapiAudioOutput).Assembly.GetType("HeyClicky.Platform.Audio.EarconPlayer");
            var decodeMethod = earconType?.GetMethod("DecodeWavToStereo48k", BindingFlags.NonPublic | BindingFlags.Static);
            if (decodeMethod != null)
            {
                byte[] malformedWav = new byte[44];
                Encoding.ASCII.GetBytes("RIFF").CopyTo(malformedWav, 0);
                Encoding.ASCII.GetBytes("WAVE").CopyTo(malformedWav, 8);
                Encoding.ASCII.GetBytes("fmt ").CopyTo(malformedWav, 12);
                BitConverter.GetBytes(16).CopyTo(malformedWav, 16); // fmt chunk size
                BitConverter.GetBytes((short)1).CopyTo(malformedWav, 20); // PCM = 1
                BitConverter.GetBytes((short)2).CopyTo(malformedWav, 22); // channels = 2
                BitConverter.GetBytes(48000).CopyTo(malformedWav, 24); // sampleRate = 48000
                BitConverter.GetBytes((short)0).CopyTo(malformedWav, 34); // bitsPerSample = 0 (MALFORMED!)
                Encoding.ASCII.GetBytes("data").CopyTo(malformedWav, 36);
                BitConverter.GetBytes(4).CopyTo(malformedWav, 40);

                try
                {
                    decodeMethod.Invoke(null, new object[] { malformedWav });
                    Console.WriteLine("  EarconPlayer.DecodeWavToStereo48k(bitsPerSample=0): did not throw");
                }
                catch (TargetInvocationException tie) when (tie.InnerException is DivideByZeroException dbz)
                {
                    Console.WriteLine($"  -> AUD-H-009 REPRODUCED: EarconPlayer.DecodeWavToStereo48k threw {dbz.GetType().Name} on bitsPerSample=0!");
                    reproducedBugs++;
                }
            }
            passedSuites++;
        }

        Console.WriteLine();
        Console.WriteLine("================================================================================");
        Console.WriteLine($"SUMMARY: {passedSuites} test suites completed, {reproducedBugs} bug reproductions verified dynamically.");
        Console.WriteLine("================================================================================");
        return 0;
    }

    private sealed class TestBrainStreamSink : IBrainStreamSink
    {
        public List<string> TextDeltas { get; } = new List<string>();
        public List<string> Events { get; } = new List<string>();

        public void OnText(ReadOnlySpan<char> text) => TextDeltas.Add(text.ToString());
        public void OnEvent(string type, JsonElement root) => Events.Add(type);
    }
}
