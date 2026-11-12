using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Harness;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.DevFakes;
using HeyClicky.App.Home;
using HeyClicky.App.Interop;
using HeyClicky.App.Onboarding;
using HeyClicky.App.Portraits;
using HeyClicky.App.Shell;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Audio;
using HeyClicky.Platform.Overlay;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Demo;

public sealed class EndToEnd
{
	private static class SyntheticInput
	{
		private const uint MouseLeftDown = 2u;

		private const uint MouseLeftUp = 4u;

		private const uint KeyUp = 2u;

		private const byte VkEscape = 27;

		public static bool Click(PointD screenPx)
		{
			if (!SetCursorPos((int)Math.Round(screenPx.X), (int)Math.Round(screenPx.Y)))
			{
				return false;
			}
			Thread.Sleep(60);
			mouse_event(2u, 0u, 0u, 0u, 0u);
			Thread.Sleep(30);
			mouse_event(4u, 0u, 0u, 0u, 0u);
			return true;
		}

		public static void PressEscape()
		{
			keybd_event(27, 0, 0u, 0u);
			keybd_event(27, 0, 2u, 0u);
		}

		[DllImport("user32.dll")]
		private static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);

		[DllImport("user32.dll")]
		private static extern void keybd_event(byte key, byte scan, uint flags, nuint extra);
	}

	private readonly HashSet<int> launchedNotepads = new HashSet<int>();

	private const byte VkLControl = 162;

	private const byte VkLMenu = 164;

	private const byte VkLWin = 91;

	private const byte VkLShift = 160;

	private const byte VkS = 83;

	private const byte VkEscape = 27;

	private const uint KeyUp = 2u;

	private readonly AppHost host;

	private readonly string outDir;

	private static string? sayFile;

	private const string LessonQuestion = "Explain how a cache speeds up a website. Draw it on my screen in three or four short steps and call the drawing lessonTitle(\"How a cache works\").";

	private string LogFile => Path.Combine(Log.Directory, $"{Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "HeyClicky")}-{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}.log");

	private async Task BrainAsync()
	{
		HashSet<string> steps = (Environment.GetEnvironmentVariable("HEYCLICKY_E2E_STEPS") ?? "context,page,document,attach,tools,agent").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
		host.Real.BrainContext.Refresh();
		await Task.Delay(2500);
		if (steps.Contains("context"))
		{
			await AskAsync("context", "In one sentence: which Clickys do I have, and how many agent messages do I have left this period?");
		}
		if (steps.Contains("page"))
		{
			await PageAsync();
		}
		if (steps.Contains("document"))
		{
			await DocumentAsync();
		}
		if (steps.Contains("attach"))
		{
			string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_IMAGE");
			if (environmentVariable != null && environmentVariable.Length > 0)
			{
				await AskAsync("attach", "What number is written in this picture? Just the number.", new global::_003C_003Ez__ReadOnlySingleElementList<string>(environmentVariable));
			}
		}
		if (steps.Contains("tools"))
		{
			await ToolsAsync();
		}
		if (steps.Contains("agent"))
		{
			await BrainAgentAsync();
		}
		if (steps.Contains("muted"))
		{
			await MutedAsync();
		}
		if (steps.Contains("escape"))
		{
			await EscapeAsync();
		}
		if (steps.Contains("cue"))
		{
			await StartCueAsync();
		}
	}

	private async Task EscapeAsync()
	{
		RealStack real = host.Real;
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_ESCAPE_PROBE");
		if (environmentVariable == null || !File.Exists(environmentVariable))
		{
			Say("escape: set HEYCLICKY_E2E_ESCAPE_PROBE");
			return;
		}
		string probeLog = Path.Combine(outDir, "escape-probe.log");
		File.Delete(probeLog);
		Process probe = Process.Start(new ProcessStartInfo("powershell.exe")
		{
			ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", environmentVariable, probeLog },
			UseShellExecute = false,
			CreateNoWindow = true
		});
		try
		{
			await Task.Delay(3000);
			nint probeWindow = FindWindowW(null, "HeyClicky Escape Probe");
			keybd_event(18, 0, 0u, 0u);
			keybd_event(18, 0, 2u, 0u);
			SetForegroundWindow(probeWindow);
			await Task.Delay(300);
			keybd_event(18, 0, 0u, 0u);
			keybd_event(18, 0, 2u, 0u);
			await Task.Delay(300);
			Say($"escape: probe window in front = {GetForegroundWindow() == probeWindow && probeWindow != 0}");
			SyntheticInput.PressEscape();
			await Task.Delay(600);
			Say($"escape: idle press reached the app = {Count() == 1} ({Count()} logged)");
			real.Companion.SubmitTextAsync("Tell me a five sentence story about a lighthouse keeper.", true);
			Stopwatch sw = Stopwatch.StartNew();
			while (real.Companion.State != VoiceState.Responding && sw.Elapsed < TimeSpan.FromSeconds(30L))
			{
				await Task.Delay(100);
			}
			await Task.Delay(800);
			VoiceState stateBefore = real.Companion.State;
			SyntheticInput.PressEscape();
			await Task.Delay(800);
			Say($"escape: pressed while {stateBefore}: reached the app = {Count() > 1} ({Count()} logged), HeyClicky now {real.Companion.State}");
			await WaitIdleAsync(real.Companion, 30000);
			SyntheticInput.PressEscape();
			await Task.Delay(600);
			Say($"escape: idle again, press reached the app = {Count() == 2} ({Count()} logged)");
		}
		finally
		{
			CloseTree(probe, "the Escape probe");
		}
		int Count()
		{
			if (!File.Exists(probeLog))
			{
				return 0;
			}
			return File.ReadAllLines(probeLog).Length;
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint FindWindowW(string? className, string title);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint window);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte key, byte scan, uint flags, nuint extra);

	private async Task StartCueAsync()
	{
		RealStack real = host.Real;
		List<string> said = new List<string>();
		real.Companion.ResponseTextUpdated += OnText;
		try
		{
			Task cue = real.AgentVoice.SpeakStartCueAsync("Find me three cheap flights from Delhi to Tokyo next month.");
			await Task.Delay(2500);
			CursorOverlay inner = real.Cursor.Inner;
			if (inner != null)
			{
				string text = Path.Combine(outDir, "e2e-cursor-cue.png");
				inner.SnapshotClusterPng(text);
				Say("cue: cursor PNG → " + text);
			}
			await cue;
			lock (said)
			{
				Say("cue: spoken = " + (said.LastOrDefault() ?? "(none)"));
			}
		}
		finally
		{
			real.Companion.ResponseTextUpdated -= OnText;
		}
		void OnText(object? s, string t)
		{
			lock (said)
			{
				said.Add(t);
			}
		}
	}

	private async Task<string> AskAsync(string step, string text, IReadOnlyList<string>? files = null, int typingMs = 1500)
	{
		CompanionSession companion = host.Real.Companion;
		host.DispatcherQueue.TryEnqueue(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.TextInput(""));
		});
		await Task.Delay(typingMs);
		Stopwatch sw = Stopwatch.StartNew();
		Say($"{step}: ask \"{text}\"{((files != null && files.Count > 0) ? $" with {files.Count} file(s)" : "")}");
		if (files == null || files.Count <= 0)
		{
			companion.SubmitTextAsync(text);
		}
		else
		{
			companion.SubmitTextAsync(text, files);
		}
		await WaitIdleAsync(companion, 120000);
		string answer = companion.ResponseText;
		Say($"{step}: answer after {sw.ElapsedMilliseconds} ms = {Trim(answer, 500)}");
		host.DispatcherQueue.TryEnqueue(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.Idle());
		});
		await Task.Delay(400);
		return answer;
	}

	private static Process? OpenBrowser(string url, string profile)
	{
		string text = new string[2] { "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe", "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe" }.FirstOrDefault(File.Exists);
		if (text == null)
		{
			return null;
		}
		return Process.Start(new ProcessStartInfo(text)
		{
			ArgumentList = 
			{
				"--user-data-dir=" + profile,
				"--guest",
				"--no-first-run",
				"--no-default-browser-check",
				"--new-window",
				"--force-renderer-accessibility",
				url
			},
			UseShellExecute = false
		});
	}

	private static void CloseTree(Process? process, string what)
	{
		if (process == null)
		{
			return;
		}
		try
		{
			process.Kill(entireProcessTree: true);
			Say("closed " + what);
		}
		catch (Exception ex)
		{
			Say("closing " + what + ": " + ex.Message);
		}
	}

	private async Task PageAsync()
	{
		string url = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_PAGE") ?? "https://en.wikipedia.org/wiki/Darjeeling_tea";
		string profile = Path.Combine(Path.GetTempPath(), "heyclicky-e2e-edge-" + Environment.ProcessId);
		Process browser = OpenBrowser(url, profile);
		if (browser == null)
		{
			Say("page: no Edge on this PC");
			return;
		}
		try
		{
			await Task.Delay(6000);
			PageReference pageReference = await host.Real.Companion.PageReaderForTest.CaptureReferenceAsync();
			Say("page: reference = " + pageReference?.Kind + " " + pageReference?.Reference);
			if ((object)pageReference != null)
			{
				Stopwatch sw = Stopwatch.StartNew();
				PageContext pageContext = await host.Real.Companion.PageReaderForTest.ExtractAsync(pageReference);
				Say($"page: read {pageContext.Text?.Length ?? 0} chars in {sw.ElapsedMilliseconds} ms: {Trim(pageContext.Text ?? "", 160)}");
			}
			await AskAsync("page", "According to the article I have open, at what elevations is this tea grown? Answer from the article.");
		}
		finally
		{
			CloseTree(browser, "the test browser");
		}
	}

	private async Task DocumentAsync()
	{
		string text = Path.Combine(Path.GetTempPath(), "heyclicky-e2e-docs");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, "clicky-notes.txt");
		File.WriteAllText(text2, "Project notes\nThe code word for the launch is MARMALADE-7.\nShip date: November 3.");
		Process notepad = Process.Start(new ProcessStartInfo(text2)
		{
			UseShellExecute = true
		});
		try
		{
			await Task.Delay(3000);
			PageReference pageReference = await host.Real.Companion.PageReaderForTest.CaptureReferenceAsync();
			Say("document: reference = " + pageReference?.Kind + " " + pageReference?.Reference);
			await AskAsync("document", "What's the code word in the document I have open?");
		}
		finally
		{
			foreach (Process item in from p in Process.GetProcessesByName("Notepad")
				where p.MainWindowTitle.Contains("clicky-notes", StringComparison.OrdinalIgnoreCase)
				select p)
			{
				CloseTree(item, "Notepad");
			}
			CloseTree(notepad, "Notepad (launcher)");
		}
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_PDF");
		if (environmentVariable != null && environmentVariable.Length > 0 && File.Exists(environmentVariable))
		{
			string profile = Path.Combine(Path.GetTempPath(), "heyclicky-e2e-edge-pdf-" + Environment.ProcessId);
			Process browser = OpenBrowser(new Uri(environmentVariable).AbsoluteUri, profile);
			try
			{
				await Task.Delay(6000);
				PageReference pageReference2 = await host.Real.Companion.PageReaderForTest.CaptureReferenceAsync();
				Say("pdf: reference = " + pageReference2?.Kind + " " + pageReference2?.Reference);
				await AskAsync("pdf", "According to the PDF I have open, what's the total on the invoice?");
			}
			finally
			{
				CloseTree(browser, "the PDF browser");
			}
		}
	}

	private async Task ToolsAsync()
	{
		await AskAsync("tools-shell", "Use a shell command to count the files in C:\\Windows\\System32\\drivers\\etc and tell me the number.");
		await AskAsync("tools-files", "List the folder C:\\Windows\\System32\\drivers\\etc with file_list and name two of the files.");
		await AskAsync("tools-calendar", "What's on my Google Calendar today?");
		await AskAsync("tools-spotify", "Play Never Gonna Give You Up by Rick Astley on Spotify.");
		await AskAsync("tools-copy", "Copy the word pineapple to my clipboard.");
		Say("tools-copy: clipboard = \"" + await TextInsertion.ReadTextAsync() + "\"");
		await AskAsync("tools-home", "Open my agents dashboard.");
		await Task.Delay(1500);
		Say($"tools-home: notch open = {(host.Services.Presenter as NotchPresenter)?.IsShowingNotchAgentPeek}");
		await AskAsync("tools-home-close", "Close the agents dashboard.");
		await Task.Delay(800);
		Say($"tools-home-close: notch open = {(host.Services.Presenter as NotchPresenter)?.IsShowingNotchAgentPeek}");
	}

	private async Task BrainAgentAsync()
	{
		RealStack real = host.Real;
		List<string> spoken = new List<string>();
		real.Companion.ResponseTextUpdated += OnText;
		HashSet<string> tasksBefore = real.Agents.Tasks.Select((AgentTaskSnapshot t) => t.TaskId).ToHashSet();
		try
		{
			await AskAsync("agent", "Start a Clicky to write a two-line poem about tea into a file named tea-poem.txt in its output folder.");
			AgentTaskSnapshot task = real.Agents.Tasks.FirstOrDefault((AgentTaskSnapshot t) => !tasksBefore.Contains(t.TaskId));
			Say($"agent: launched = {(object)task != null} ({task?.Title})");
			if ((object)task == null)
			{
				return;
			}
			Stopwatch sw = Stopwatch.StartNew();
			while (true)
			{
				bool flag = sw.Elapsed < TimeSpan.FromMinutes(5L);
				bool flag2;
				if (flag)
				{
					AgentTaskStatus? agentTaskStatus = real.Agents.Tasks.FirstOrDefault((AgentTaskSnapshot t) => t.TaskId == task.TaskId)?.Status;
					if (agentTaskStatus.HasValue)
					{
						AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
						if ((uint)valueOrDefault <= 1u)
						{
							flag2 = true;
							goto IL_02fe;
						}
					}
					flag2 = false;
					goto IL_02fe;
				}
				goto IL_0302;
				IL_02fe:
				flag = flag2;
				goto IL_0302;
				IL_0302:
				if (!flag)
				{
					break;
				}
				await Task.Delay(500);
			}
			AgentTaskDetail detail = real.Agents.GetTaskDetail(task.TaskId);
			Say($"agent: {detail?.Snapshot.Status} after {sw.Elapsed.TotalSeconds:0} s: {Trim(detail?.Summary ?? detail?.Snapshot.FinalResponseMarkdown ?? detail?.FailureMessage ?? "", 300)}");
			Stopwatch spokenWait = Stopwatch.StartNew();
			while (spokenWait.Elapsed < TimeSpan.FromSeconds(60L))
			{
				lock (spoken)
				{
					if (spoken.Any((string t) => t.Contains("is done", StringComparison.OrdinalIgnoreCase)))
					{
						break;
					}
				}
				await Task.Delay(250);
			}
			lock (spoken)
			{
				Say("agent: spoken finish = " + (spoken.LastOrDefault((string t) => t.Contains("is done", StringComparison.OrdinalIgnoreCase)) ?? "(none)"));
			}
			await WaitIdleAsync(real.Companion, 60000);
			await AskAsync("agent-memory", "What did " + (detail?.ClickyName ?? "that Clicky") + " just make? Read me the file it wrote.");
			await AskAsync("agent-open", "Show me that file in File Explorer.");
			await Task.Delay(1500);
			foreach (Process item in from p in Process.GetProcessesByName("explorer")
				where p.MainWindowTitle.Contains("output", StringComparison.OrdinalIgnoreCase)
				select p)
			{
				Say("agent-open: an Explorer window is open on \"" + item.MainWindowTitle + "\"");
			}
		}
		finally
		{
			real.Companion.ResponseTextUpdated -= OnText;
		}
		void OnText(object? s, string t)
		{
			lock (spoken)
			{
				spoken.Add(t);
			}
		}
	}

	private async Task MutedAsync()
	{
		_ = host.Real;
		string before = SystemAudioOutputVolume.CurrentSilenceKind();
		Say("muted: speaker before = " + (before ?? "audible"));
		SystemAudioOutputVolume.SetMuted(muted: true);
		try
		{
			Say("muted: speaker now = " + (SystemAudioOutputVolume.CurrentSilenceKind() ?? "audible"));
			await VoiceAsync();
			Say("muted: clipboard = " + Trim((await TextInsertion.ReadTextAsync()) ?? ""));
			Say($"muted: notch = {host.Services.Presenter.Presentation}");
			await SnapNotchAsync("e2e-muted-card");
			bool value = SystemAudioOutputVolume.RestoreAudibleOutput();
			host.DispatcherQueue.TryEnqueue(delegate
			{
				host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.VoiceReplyMutedCopiedToClipboard));
			});
			Say($"muted: after the card's fix changed={value}, speaker = {SystemAudioOutputVolume.CurrentSilenceKind() ?? "audible"}");
		}
		finally
		{
			if (before != "muted")
			{
				SystemAudioOutputVolume.SetMuted(muted: false);
			}
		}
	}

	private async Task SnapNotchAsync(string name)
	{
		TaskCompletionSource tcs = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			try
			{
				await Snapshot.SaveAsync(host.NotchUi.Window.Root, name);
			}
			catch (Exception ex)
			{
				Say("snapshot " + name + ": " + ex.Message);
			}
			tcs.TrySetResult();
		});
		await tcs.Task;
	}

	private async Task BundleAsync()
	{
		if (!(await SignInAsync()))
		{
			return;
		}
		HashSet<string> steps = (Environment.GetEnvironmentVariable("HEYCLICKY_E2E_STEPS") ?? "highlight,save,escape,yield,exit,idle,muted,mouth").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
		Say($"bundle: steps {string.Join(",", steps)}; audio topology {AudioTopologyMonitor.Shared.Snapshot}");
		string[] array = new string[8] { "highlight", "save", "escape", "yield", "exit", "idle", "muted", "mouth" };
		foreach (string step in array)
		{
			if (!steps.Contains(step))
			{
				continue;
			}
			try
			{
				string text = step;
				if (text != null)
				{
					switch (text.Length)
					{
					case 4:
						switch (text[0])
						{
						case 's':
							if (text == "save")
							{
								await SaveThatAsync();
							}
							break;
						case 'e':
							if (text == "exit")
							{
								await WalkthroughPassiveExitAsync();
							}
							break;
						case 'i':
							if (text == "idle")
							{
								await IdleFoundingAsync();
							}
							break;
						}
						break;
					case 5:
						switch (text[1])
						{
						case 'i':
							if (text == "yield")
							{
								await CaptureYieldAsync();
							}
							break;
						case 'u':
							if (text == "muted")
							{
								await MutedExemptionAsync();
							}
							break;
						case 'o':
							if (text == "mouth")
							{
								await MouthIdleAsync();
							}
							break;
						}
						break;
					case 9:
						if (text == "highlight")
						{
							await HighlightTextAsync();
						}
						break;
					case 6:
						if (text == "escape")
						{
							await EscapeClearSoundAsync();
						}
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Say("bundle: " + step + " FAILED: " + ex.Message);
				Log.Error("e2e", step, ex);
			}
			await Task.Delay(800);
		}
	}

	private List<string> LogSince(int marker)
	{
		try
		{
			using FileStream stream = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using StreamReader streamReader = new StreamReader(stream);
			return (from l in streamReader.ReadToEnd().Split('\n').Skip(marker)
				select l.TrimEnd('\r')).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	private int LogMark()
	{
		return LogSince(0).Count;
	}

	private bool UseWav(string name)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_WAVS");
		string environmentVariable2 = Environment.GetEnvironmentVariable("HEYCLICKY_TEST_MIC_WAV");
		if (environmentVariable == null || environmentVariable2 == null || !File.Exists(Path.Combine(environmentVariable, name)))
		{
			Say("bundle: no " + name + " (HEYCLICKY_E2E_WAVS / HEYCLICKY_TEST_MIC_WAV)");
			return false;
		}
		File.Copy(Path.Combine(environmentVariable, name), environmentVariable2, overwrite: true);
		File.SetLastWriteTimeUtc(environmentVariable2, DateTime.UtcNow);
		return true;
	}

	private double WavSeconds()
	{
		if (!(host.Real.Microphone is WavFileMicrophone wavFileMicrophone))
		{
			return 3.0;
		}
		return wavFileMicrophone.DurationSeconds;
	}

	private async Task<(Process Process, nint Hwnd)?> LaunchNotepadAsync(string fileName, string text, (int X, int Y, int W, int H) rect)
	{
		if (Process.GetProcessesByName("Notepad").Any((Process p) => !launchedNotepads.Contains(p.Id)))
		{
			Say("bundle: Notepad is already running (not ours): skipped so nothing opens in the user's window");
			return null;
		}
		string text2 = Path.Combine(Path.GetTempPath(), "heyclicky-e2e-bundle");
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, fileName);
		File.WriteAllText(text3, text);
		Process launcher = Process.Start(new ProcessStartInfo("notepad.exe", "\"" + text3 + "\"")
		{
			UseShellExecute = true
		});
		string stem = Path.GetFileNameWithoutExtension(fileName);
		for (int i = 0; i < 60; i++)
		{
			await Task.Delay(250);
			Process[] processesByName = Process.GetProcessesByName("Notepad");
			foreach (Process p2 in processesByName)
			{
				p2.Refresh();
				if (p2.MainWindowHandle != 0 && p2.MainWindowTitle.Contains(stem, StringComparison.OrdinalIgnoreCase))
				{
					launchedNotepads.Add(p2.Id);
					MoveWindow(p2.MainWindowHandle, rect.X, rect.Y, rect.W, rect.H, repaint: true);
					SetForegroundWindow(p2.MainWindowHandle);
					await Task.Delay(700);
					return (p2, p2.MainWindowHandle);
				}
			}
		}
		CloseTree(launcher, "Notepad launcher");
		Say("bundle: Notepad didn't open");
		return null;
	}

	private void CloseNotepads()
	{
		foreach (int item in launchedNotepads.ToList())
		{
			try
			{
				using Process process = Process.GetProcessById(item);
				process.Kill(entireProcessTree: true);
				Say($"bundle: closed Notepad {item}");
			}
			catch
			{
			}
			launchedNotepads.Remove(item);
		}
	}

	private static async Task HoldPushToTalkAsync(double seconds, Func<Task>? whileHeld = null)
	{
		keybd_event(162, 0, 0u, 0u);
		keybd_event(164, 0, 0u, 0u);
		Stopwatch held = Stopwatch.StartNew();
		try
		{
			if (whileHeld != null)
			{
				await whileHeld();
			}
			TimeSpan timeSpan = TimeSpan.FromSeconds(seconds) - held.Elapsed;
			if (timeSpan > TimeSpan.Zero)
			{
				await Task.Delay(timeSpan);
			}
		}
		finally
		{
			keybd_event(164, 0, 2u, 0u);
			keybd_event(162, 0, 2u, 0u);
		}
	}

	private static void PressKeys(params byte[] keys)
	{
		for (int i = 0; i < keys.Length; i++)
		{
			keybd_event(keys[i], 0, 0u, 0u);
		}
		Thread.Sleep(40);
		foreach (byte item in Enumerable.Reverse(keys))
		{
			keybd_event(item, 0, 2u, 0u);
		}
	}

	private async Task<bool> TypedTurnAsync(string text, int idleTimeoutMs = 120000)
	{
		CompanionSession companion = host.Real.Companion;
		companion.SubmitTextAsync(text, true);
		await Task.Delay(1200);
		await WaitIdleAsync(companion, idleTimeoutMs);
		Say("bundle: answer = " + Trim(companion.ResponseText));
		return true;
	}

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern bool MoveWindow(nint hwnd, int x, int y, int w, int h, bool repaint);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

	private static bool IsCloaked(nint hwnd)
	{
		if (DwmGetWindowAttribute(hwnd, 14, out var value, 4) == 0)
		{
			return value != 0;
		}
		return false;
	}

	private (int X, int Y, int W, int H) LeftHalf()
	{
		Displays.Monitor monitor = Displays.ForWindow(host.NotchUi.Window.Hwnd);
		return (X: monitor.Work.Left + 40, Y: monitor.Work.Top + 120, W: Math.Min(1100, monitor.Work.Width / 2), H: Math.Min(720, monitor.Work.Height - 200));
	}

	private async Task HighlightTextAsync()
	{
		RealStack real = host.Real;
		if (!UseWav("highlight.wav"))
		{
			return;
		}
		(int X, int Y, int W, int H) rect = LeftHalf();
		string text = string.Join("\n", from i in Enumerable.Range(1, 14)
			select (i != 4) ? $"Line {i}: nothing to see here" : "Invoice 4719 is due on Friday the 24th");
		(Process, nint)? tuple = await LaunchNotepadAsync("clicky-highlight.txt", text, rect);
		if (!tuple.HasValue)
		{
			return;
		}
		(Process, nint) valueOrDefault = tuple.GetValueOrDefault();
		try
		{
			int mark = LogMark();
			double scale = Displays.ForWindow(valueOrDefault.Item2).Scale;
			PointD center = new PointD((double)rect.X + 230.0 * scale, (double)rect.Y + 174.6 * scale);
			Say($"bundle: highlight: holding Ctrl+Alt and circling {center.X:0},{center.Y:0} over Notepad ({WavSeconds():0.0} s WAV)");
			await HoldPushToTalkAsync(WavSeconds() + 0.8, async delegate
			{
				await Task.Delay(250);
				for (int j = 0; j <= 60; j++)
				{
					double num = (double)j / 60.0 * Math.PI * 2.2;
					SetCursorPos((int)(center.X + Math.Cos(num) * 160.0 * scale), (int)(center.Y + Math.Sin(num) * 22.0 * scale));
					await Task.Delay(18);
				}
			});
			await Task.Delay(1500);
			await WaitIdleAsync(real.Companion, 120000);
			string lastHighlightText = real.Companion.LastHighlightText;
			Say("bundle: highlight: text under the ink = " + ((lastHighlightText == null) ? "(no highlight)" : ("\"" + Trim(lastHighlightText, 400) + "\"")));
			Say("bundle: highlight: " + (((lastHighlightText != null && lastHighlightText.Contains("4719")) || (lastHighlightText != null && lastHighlightText.Contains("Line "))) ? "PASS" : "FAIL") + " the harvest read Notepad's text");
			Say("bundle: highlight: answer = " + Trim(real.Companion.ResponseText));
			foreach (string item in from l in LogSince(mark)
				where l.Contains("highlight text") || l.Contains("[Companion] turn context")
				select l)
			{
				Say("  log: " + Trim(item, 300));
			}
		}
		finally
		{
			CloseNotepads();
		}
	}

	private async Task SaveThatAsync()
	{
		RealStack real = host.Real;
		LessonService lessons = host.Lessons;
		if (lessons == null)
		{
			Say("bundle: save: no lesson service");
			return;
		}
		real.Core.Settings.Update(delegate(AppSettings s)
		{
			s.SaveDrawings = true;
		});
		int savedBefore = lessons.Store.List().Count;
		int recentBefore = lessons.RecentDrawings.Count;
		int mark = LogMark();
		string caption = "";
		CancellationTokenSource sampling = new CancellationTokenSource();
		try
		{
			Task sampler = Task.Run(async delegate
			{
				while (!sampling.IsCancellationRequested)
				{
					string captionText = real.Canvas.CaptionText;
					if (captionText.Length > 0 && caption.Length == 0)
					{
						caption = captionText;
						Native.RECT bounds = Displays.ForWindow(host.NotchUi.Window.Hwnd).Bounds;
						await ScreenGrab.SaveAsync(Path.Combine(outDir, "bundle-caption.png"), bounds.Left, bounds.Top + bounds.Height - (int)(320.0 * Displays.ForWindow(host.NotchUi.Window.Hwnd).Scale), bounds.Width, (int)(320.0 * Displays.ForWindow(host.NotchUi.Window.Hwnd).Scale));
					}
					try
					{
						await Task.Delay(120, sampling.Token);
					}
					catch
					{
					}
				}
			});
			await TypedTurnAsync("Explain how DNS turns a name into an address in three short spoken steps, drawn as a little diagram, and give it a lessonTitle.");
			sampling.Cancel();
			await sampler;
			Say($"bundle: caption: {((caption.Length > 0) ? "PASS" : "FAIL")} the debug subtitle showed under the drawing (\"{Trim(caption, 120)}\"; bundle-caption.png)");
			for (int i = 0; i < 20; i++)
			{
				if (lessons.RecentDrawings.Count != recentBefore)
				{
					break;
				}
				await Task.Delay(250);
			}
			LessonService.Recent recent = lessons.RecentDrawings.LastOrDefault();
			Say($"bundle: save: drawing told = {lessons.RecentDrawings.Count > recentBefore} (\"{recent?.Record.Title}\", id {recent?.Handle})");
			if (!UseWav("save.wav"))
			{
				return;
			}
			await HoldPushToTalkAsync(WavSeconds() + 0.8);
			await Task.Delay(1500);
			await WaitIdleAsync(real.Companion, 90000);
			int count = lessons.Store.List().Count;
			Say($"bundle: save: {((count > savedBefore) ? "PASS" : "FAIL")} \"save that\" by voice kept it ({savedBefore} → {count} saved drawings); answer = {Trim(real.Companion.ResponseText)}");
			foreach (string item in from l in LogSince(mark)
				where l.Contains("save_drawing") || l.Contains("[clicky drawing]") || l.Contains("[lessons]")
				select l)
			{
				Say("  log: " + Trim(item, 300));
			}
		}
		finally
		{
			if (sampling != null)
			{
				((IDisposable)sampling).Dispose();
			}
		}
	}

	private async Task EscapeClearSoundAsync()
	{
		RealStack real = host.Real;
		int mark = LogMark();
		await TypedTurnAsync("Circle the middle of my screen and say one short line about it.");
		Say($"bundle: escape: drawing showing = {real.Canvas.IsShowingDrawing}");
		PressKeys(27);
		await Task.Delay(800);
		bool flag = LogSince(mark).Any((string l) => l.Contains("page-clear"));
		Say($"bundle: escape: {((flag && !real.Canvas.IsShowing) ? "PASS" : "FAIL")} a real Escape cleared it with the paper sweep (page-clear logged {flag}, showing {real.Canvas.IsShowing})");
	}

	private async Task CaptureYieldAsync()
	{
		nint notch = host.NotchUi.Window.Hwnd;
		int mark = LogMark();
		Say("bundle: yield: pressing Win+Shift+S");
		PressKeys(91, 160, 83);
		bool cloakedSeen = false;
		bool yieldingSeen = false;
		for (int i = 0; i < 30; i++)
		{
			await Task.Delay(100);
			cloakedSeen |= IsCloaked(notch);
			yieldingSeen |= ScreenCaptureYield.IsYielding;
			if (i == 15)
			{
				Native.RECT bounds = Displays.ForWindow(notch).Bounds;
				await ScreenGrab.SaveAsync(Path.Combine(outDir, "bundle-yield-snip.png"), bounds.Left, bounds.Top, bounds.Width, bounds.Height / 3);
			}
		}
		Say($"bundle: yield: while the snip UI is up: yielding {yieldingSeen}, notch cloaked {cloakedSeen}");
		PressKeys(27);
		bool flag = false;
		for (int i = 0; i < 40; i++)
		{
			if (flag)
			{
				break;
			}
			await Task.Delay(100);
			flag = !ScreenCaptureYield.IsYielding && !IsCloaked(notch);
		}
		(bool, int) lastWatch = ScreenCaptureYield.LastWatch;
		Say($"bundle: yield: {((yieldingSeen && cloakedSeen && flag) ? "PASS" : "FAIL")} the overlays stepped aside and came back (snip UI seen {lastWatch.Item1}, aside {lastWatch.Item2} ms)");
		foreach (string item in from l in LogSince(mark)
			where l.Contains("[CaptureYield]")
			select l)
		{
			Say("  log: " + Trim(item, 300));
		}
	}

	private async Task WalkthroughPassiveExitAsync()
	{
		RealStack real = host.Real;
		CompanionSession companion = real.Companion;
		(int, int, int, int) rect = LeftHalf();
		(Process, nint)? tuple = await LaunchNotepadAsync("clicky-walkthrough.txt", "A short note.\nTurn on word wrap for me.", rect);
		if (!tuple.HasValue)
		{
			return;
		}
		SetForegroundWindow(tuple.GetValueOrDefault().Item2);
		await Task.Delay(800);
		int mark = LogMark();
		Process other = null;
		try
		{
			await TypedTurnAsync("In this Notepad window, walk me through turning on word wrap, one step at a time. Give me just the first step, with a click target on the View menu.");
			ClickTargetRing armedClickTarget = companion.ArmedClickTarget;
			Say($"bundle: exit: ring armed = {(object)armedClickTarget != null}{(((object)armedClickTarget == null) ? "" : (" on \"" + armedClickTarget.Label + "\""))}");
			if ((object)armedClickTarget == null)
			{
				return;
			}
			SyntheticInput.Click(armedClickTarget.CenterPx);
			await Task.Delay(1700);
			Say($"bundle: exit: state {companion.State} when another app comes forward");
			other = Process.Start(new ProcessStartInfo("conhost.exe", "cmd.exe /k title HeyClicky-e2e-other")
			{
				UseShellExecute = true
			});
			await WaitIdleAsync(companion, 60000);
			bool flag = LogSince(mark).Any((string l) => l.Contains("passive exit"));
			Say($"bundle: exit: {((flag && (object)companion.ArmedClickTarget == null && companion.State == VoiceState.Idle) ? "PASS" : "FAIL")} the walkthrough ended quietly (passive exit logged {flag}, ring {(object)companion.ArmedClickTarget != null}, state {companion.State}, answer \"{Trim(companion.ResponseText, 120)}\")");
			foreach (string item in from l in LogSince(mark)
				where l.Contains("guided") || l.Contains("passive")
				select l)
			{
				Say("  log: " + Trim(item, 300));
			}
		}
		finally
		{
			CloseTree(other, "the test console");
			CloseNotepads();
		}
	}

	private async Task IdleFoundingAsync()
	{
		HashSet<string> before = ClickyRoster.Shared.Clickys.Select((RosterClicky c) => c.Slug).ToHashSet();
		await TypedTurnAsync("Make me a Clicky that keeps my reading list tidy. Just make it, don't start any work.");
		RosterClicky made = null;
		for (int i = 0; i < 60; i++)
		{
			if ((object)made != null)
			{
				break;
			}
			await Task.Delay(250);
			made = ClickyRoster.Shared.Clickys.FirstOrDefault((RosterClicky c) => !before.Contains(c.Slug));
		}
		if ((object)made == null)
		{
			Say("bundle: idle: FAIL no Clicky was founded");
			return;
		}
		IReadOnlyList<string> asks = Array.Empty<string>();
		for (int i = 0; i < 80; i++)
		{
			asks = ClickyRoster.Shared.Clickys.FirstOrDefault((RosterClicky c) => c.Slug == made.Slug)?.SuggestedAsks ?? Array.Empty<string>();
			if (asks.Count >= 2)
			{
				break;
			}
			await Task.Delay(250);
		}
		bool flag = host.Real.Agents.Tasks.Any(delegate(AgentTaskSnapshot t)
		{
			AgentTaskStatus status = t.Status;
			return (uint)status <= 1u && host.Real.Agents.GetTaskDetail(t.TaskId)?.ClickySlug == made.Slug;
		});
		Say($"bundle: idle: {((asks.Count > 0 && !flag) ? "PASS" : "FAIL")} \"{made.Name}\" founded idle (running {flag}) with {asks.Count} first asks: {string.Join(" | ", asks.Select((string a) => Trim(a, 80)))}");
	}

	private async Task MutedExemptionAsync()
	{
		RealStack real = host.Real;
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_TEST_SPEAKER_SILENT");
		bool cardShown;
		if (environmentVariable == null || environmentVariable.Length <= 0)
		{
			Say("bundle: muted: set HEYCLICKY_TEST_SPEAKER_SILENT=muted to run this");
		}
		else
		{
			if (!UseWav("hello.wav"))
			{
				return;
			}
			Func<bool> previous = FlowHooks.MutedSpeakerReplyExempt;
			int mark = LogMark();
			cardShown = false;
			host.Services.Presenter.PresentationChanged += OnPresented;
			try
			{
				FlowHooks.MutedSpeakerReplyExempt = () => true;
				await HoldPushToTalkAsync(WavSeconds() + 0.8);
				await Task.Delay(1500);
				await WaitIdleAsync(real.Companion, 90000);
				bool flag = LogSince(mark).Any((string l) => l.Contains("no clipboard card"));
				Say($"bundle: muted: {((flag && !cardShown) ? "PASS" : "FAIL")} a silent speaker during the introduction raised no clipboard card (exemption logged {flag}, card shown {cardShown})");
			}
			finally
			{
				FlowHooks.MutedSpeakerReplyExempt = previous;
				host.Services.Presenter.PresentationChanged -= OnPresented;
			}
		}
		void OnPresented(object? s, EventArgs e)
		{
			if (host.Services.Presenter.Presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.VoiceReplyMutedCopiedToClipboard)
			{
				cardShown = true;
			}
		}
	}

	private async Task MouthIdleAsync()
	{
		RealtimeMouth mouth = host.Real.Companion.Mouth;
		TimeSpan window = mouth.IdleTeardownWindow;
		Say($"bundle: mouth: open {mouth.IsOpen}, idle window {window.TotalSeconds:0} s");
		if (window > TimeSpan.FromSeconds(60L))
		{
			Say("bundle: mouth: set HEYCLICKY_TEST_MOUTH_IDLE_SECONDS to run this");
			return;
		}
		if (!mouth.IsOpen)
		{
			await mouth.EnsureOpenAsync();
		}
		Stopwatch sw = Stopwatch.StartNew();
		while (mouth.IsOpen && sw.Elapsed < window + TimeSpan.FromSeconds(15L))
		{
			await Task.Delay(250);
		}
		Say($"bundle: mouth: {((!mouth.IsOpen) ? "PASS" : "FAIL")} the mouth closed after {sw.Elapsed.TotalSeconds:0.0} s idle");
	}

	private async Task MicAsync()
	{
		if (!(await SignInAsync()))
		{
			return;
		}
		IMicrophoneCapture microphone = host.Real.Microphone;
		if (!(microphone is WasapiMicrophoneCapture mic))
		{
			Say("mic: needs the real microphone (unset HEYCLICKY_TEST_MIC_WAV)");
			return;
		}
		int mark = LogMark();
		NotchLiveEvent.MicrophoneTrouble card = null;
		host.Services.Presenter.PresentationChanged += OnPresented;
		try
		{
			Say($"mic: device '{mic.DeviceName}' ({mic.CurrentDevice?.TransportCategory}), bluetooth {mic.IsBluetoothDevice}; topology {AudioTopologyMonitor.Shared.Snapshot}");
			await HoldPushToTalkAsync(6.0);
			await Task.Delay(2500);
			Say($"mic: after the press: failover pin {((mic.FailoverDeviceId == null) ? "none" : "set")}, holding open {mic.IsHoldingDeviceOpen}, card {(((object)card == null) ? "none" : $"\"{card.Message}\" / \"{card.Subtitle}\" (Mic settings {card.ShowsMicrophoneSettingsAction})")}");
			if (mic.IsHoldingDeviceOpen)
			{
				Stopwatch sw = Stopwatch.StartNew();
				while (mic.IsHoldingDeviceOpen && sw.Elapsed < TimeSpan.FromSeconds(40L))
				{
					await Task.Delay(250);
				}
				Say($"mic: {((!mic.IsHoldingDeviceOpen) ? "PASS" : "FAIL")} the held Bluetooth mic closed {sw.Elapsed.TotalSeconds + 2.5:0.0} s after release");
			}
			foreach (string item in from l in LogSince(mark)
				where l.Contains("[Mic]") || l.Contains("mic ") || l.Contains("Bluetooth") || l.Contains("no audio")
				select l)
			{
				Say("  log: " + Trim(item, 300));
			}
		}
		finally
		{
			host.Services.Presenter.PresentationChanged -= OnPresented;
		}
		void OnPresented(object? s, EventArgs e)
		{
			if (host.Services.Presenter.Presentation is NotchPresentation.LiveEvent { Event: NotchLiveEvent.MicrophoneTrouble @event })
			{
				card = @event;
			}
		}
	}

	public EndToEnd(AppHost host)
	{
		this.host = host;
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_OUT");
		outDir = ((environmentVariable != null && environmentVariable.Length > 0) ? environmentVariable : Path.Combine(Path.GetTempPath(), "heyclicky-e2e"));
		Directory.CreateDirectory(outDir);
		sayFile = Path.Combine(outDir, "e2e.log");
	}

	private static void Say(string message)
	{
		Log.Info("e2e", message);
		try
		{
			if (sayFile != null)
			{
				File.AppendAllText(sayFile, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
			}
		}
		catch
		{
		}
	}

	public async Task RunAsync(string scenario)
	{
		Say("e2e runs only in Debug builds");
	}

	private async Task<bool> SignInAsync()
	{
		RealStack real = host.Real;
		if (real.Core.Auth.IsSignedIn)
		{
			Say("signin: already signed in as " + real.Core.Auth.User?.Email);
		}
		else
		{
			Stopwatch sw = Stopwatch.StartNew();
			bool flag = await host.DevSignInAsync();
			Say($"signin: {(flag ? "ok" : "FAILED")} in {sw.ElapsedMilliseconds} ms");
			if (!flag)
			{
				return false;
			}
		}
		Say("signin: /me/plan " + (((object)(await real.Core.Worker.GetPlanAsync()) == null) ? "null" : "ok"));
		await real.Core.Worker.RegisterThisDeviceIfNeededAsync();
		IReadOnlyList<RegisteredDevice> readOnlyList = await real.Core.Worker.GetDevicesAsync();
		Say($"signin: devices registered = {readOnlyList.Count} ({string.Join(", ", readOnlyList.Select((RegisteredDevice d) => d.ToString()))})");
		return true;
	}

	private async Task TypedAsync(string text)
	{
		RealStack real = host.Real;
		bool flag = !real.Core.Auth.IsSignedIn;
		if (flag)
		{
			flag = !(await SignInAsync());
		}
		if (flag)
		{
			return;
		}
		CompanionSession companion = real.Companion;
		TaskCompletionSource<long> firstText = new TaskCompletionSource<long>();
		Stopwatch sw = Stopwatch.StartNew();
		companion.ResponseTextUpdated += OnText;
		host.DispatcherQueue.TryEnqueue(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.TextInput(text));
		});
		await Task.Delay(700);
		sw.Restart();
		Say("typed: submit \"" + text + "\"");
		companion.SubmitTextAsync(text);
		CancellationTokenSource cursorWatch = new CancellationTokenSource();
		try
		{
			List<string> cursorModes = new List<string>();
			List<PointD> cursorPositions = new List<PointD>();
			Task watcher = Task.Run(async delegate
			{
				while (!cursorWatch.IsCancellationRequested)
				{
					CursorOverlay inner = real.Cursor.Inner;
					(long, long, PointD, string) diagnostics;
					if (inner != null)
					{
						diagnostics = inner.GetDiagnostics();
						if (cursorModes.Count != 0)
						{
							List<string> list = cursorModes;
							if (!(list[list.Count - 1] != diagnostics.Item4))
							{
								goto IL_0072;
							}
						}
						cursorModes.Add(diagnostics.Item4);
						goto IL_0072;
					}
					goto IL_0096;
					IL_0096:
					try
					{
						await Task.Delay(40, cursorWatch.Token);
					}
					catch (OperationCanceledException)
					{
					}
					continue;
					IL_0072:
					if (diagnostics.Item4 == "RidingPen")
					{
						cursorPositions.Add(diagnostics.Item3);
					}
					goto IL_0096;
				}
			});
			long value = ((await Task.WhenAny(firstText.Task, Task.Delay(60000)) == firstText.Task) ? firstText.Task.Result : (-1));
			Say($"typed: first text after {value} ms");
			await WaitIdleAsync(companion, 90000);
			await Task.Delay(1500);
			cursorWatch.Cancel();
			await watcher;
			double value2 = cursorPositions.Zip(cursorPositions.Skip(1), (PointD a, PointD b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y))).Sum();
			string obj = $"typed: cursor modes {string.Join(" â†’ ", cursorModes)}; riding samples {cursorPositions.Count}, travelled {value2:0} px";
			object obj2;
			if (cursorPositions.Count <= 0)
			{
				obj2 = "";
			}
			else
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 2);
				defaultInterpolatedStringHandler.AppendLiteral(", from ");
				defaultInterpolatedStringHandler.AppendFormatted(cursorPositions[0]);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				List<PointD> list2 = cursorPositions;
				defaultInterpolatedStringHandler.AppendFormatted(list2[list2.Count - 1]);
				obj2 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			Say(obj + (string?)obj2);
			companion.ResponseTextUpdated -= OnText;
			Say("typed: answer = " + Trim(companion.ResponseText));
			Say($"typed: canvas showing={real.Canvas.IsShowing} drawing={real.Canvas.IsShowingDrawing}");
			await SaveArtifactsAsync("typed");
		}
		finally
		{
			if (cursorWatch != null)
			{
				((IDisposable)cursorWatch).Dispose();
			}
		}
		void OnText(object? s, string t)
		{
			if (t.Length > 0)
			{
				firstText.TrySetResult(sw.ElapsedMilliseconds);
			}
		}
	}

	private async Task ContinueDrawingAsync()
	{
		RealStack real = host.Real;
		await TypedAsync("Draw a box around the top-left quarter of my screen and label it A.");
		string s = real.Canvas.DrawingStateForModel() ?? "";
		Say("continue: before = " + Trim(s, 300));
		await TypedAsync("Now add a second box around the bottom-right quarter labelled B, keep the first one.");
		string text = real.Canvas.DrawingStateForModel() ?? "";
		Say("continue: after = " + Trim(text, 400));
		int count = Regex.Matches(text, "\\bTurn \\d+").Count;
		Say($"continue: built on the first drawing = {count >= 2} ({count} turns on the card)");
	}

	private async Task StaleDrawingAsync()
	{
		RealStack real = host.Real;
		await TypedAsync("Circle the start button on my taskbar.");
		await Task.Delay(2000);
		Say($"stale: after 2 s of nothing, drawing showing = {real.Canvas.IsShowingDrawing}");
		using Process other = Process.Start(new ProcessStartInfo("conhost.exe", "cmd.exe /k title HeyClicky-e2e-other")
		{
			UseShellExecute = true
		});
		Stopwatch deadline = Stopwatch.StartNew();
		while (real.Canvas.IsShowing && deadline.ElapsedMilliseconds < 4000)
		{
			await Task.Delay(100);
		}
		Say($"stale: another app in front, drawing showing = {real.Canvas.IsShowing} after {deadline.ElapsedMilliseconds} ms");
		try
		{
			other?.Kill(entireProcessTree: true);
		}
		catch (Exception ex)
		{
			Say("stale: closing the console failed: " + ex.Message);
		}
	}

	private async Task WalkthroughAsync()
	{
		RealStack real = host.Real;
		CompanionSession companion = real.Companion;
		List<(string Trigger, int? Step)> turns = new List<(string, int?)>();
		companion.TurnStarted += OnTurn;
		try
		{
			await TypedAsync(Environment.GetEnvironmentVariable("HEYCLICKY_E2E_TEXT") ?? "Walk me through opening Windows Settings, one step at a time. Give me just the first step, with a click target on the Start button on my taskbar.");
			ClickTargetRing ring = companion.ArmedClickTarget;
			string obj = $"walkthrough: ring armed = {(object)ring != null}";
			object obj2;
			if ((object)ring != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 5);
				defaultInterpolatedStringHandler.AppendLiteral(" at ");
				defaultInterpolatedStringHandler.AppendFormatted(ring.CenterPx.X, "0");
				defaultInterpolatedStringHandler.AppendLiteral(",");
				defaultInterpolatedStringHandler.AppendFormatted(ring.CenterPx.Y, "0");
				defaultInterpolatedStringHandler.AppendLiteral(" radius ");
				defaultInterpolatedStringHandler.AppendFormatted(ring.RadiusPx, "0");
				defaultInterpolatedStringHandler.AppendLiteral(" px, control ");
				RectD? controlFramePx = ring.ControlFramePx;
				object value;
				if (controlFramePx.HasValue)
				{
					RectD valueOrDefault = controlFramePx.GetValueOrDefault();
					value = $"{valueOrDefault.X:0},{valueOrDefault.Y:0} {valueOrDefault.Width:0}x{valueOrDefault.Height:0}";
				}
				else
				{
					value = "none";
				}
				defaultInterpolatedStringHandler.AppendFormatted((string?)value);
				defaultInterpolatedStringHandler.AppendLiteral(", label \"");
				defaultInterpolatedStringHandler.AppendFormatted(ring.Label);
				defaultInterpolatedStringHandler.AppendLiteral("\"");
				obj2 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				obj2 = "";
			}
			Say(obj + (string?)obj2);
			if ((object)ring == null)
			{
				return;
			}
			CursorOverlay inner = real.Cursor.Inner;
			if (inner != null)
			{
				(ClickTargetRing, double, bool) clickTargetDiagnostics = inner.GetClickTargetDiagnostics();
				Say($"walkthrough: overlay ring shown = {(object)clickTargetDiagnostics.Item1 != null}, reveal {clickTargetDiagnostics.Item2:0.00}, pulsing = {clickTargetDiagnostics.Item3}");
				string text = Path.Combine(outDir, "e2e-walkthrough-ring.png");
				inner.SnapshotClickTargetPng(text);
				Say("walkthrough: ring PNG â†’ " + text);
			}
			Stopwatch clicked = Stopwatch.StartNew();
			bool realClick = SyntheticInput.Click(ring.CenterPx);
			while (realClick && !HasFollowUp() && clicked.ElapsedMilliseconds < 1500)
			{
				await Task.Delay(50);
			}
			string how = "a real click through the hook";
			if (!HasFollowUp())
			{
				how = "a press at the hook's entry point (synthetic input didn't arrive)";
				companion.SimulatePressForTest(ring.CenterPx);
			}
			while (!HasFollowUp() && clicked.ElapsedMilliseconds < 15000)
			{
				await Task.Delay(50);
			}
			(string, int?) tuple;
			lock (turns)
			{
				tuple = turns.LastOrDefault<(string, int?)>(((string Trigger, int? Step) t) => t.Trigger == "clicked_target");
			}
			Say($"walkthrough: clicked by {how}; follow-up turn = {HasFollowUp()} (trigger {tuple.Item1}, step {tuple.Item2}) {clicked.ElapsedMilliseconds} ms after the click");
			await WaitIdleAsync(companion, 90000);
			Say("walkthrough: step 1 answer = " + Trim(companion.ResponseText));
			ClickTargetRing next = companion.ArmedClickTarget;
			Say($"walkthrough: step 1 ring armed = {(object)next != null}" + (((object)next == null) ? "" : $" at {next.CenterPx.X:0},{next.CenterPx.Y:0}, label \"{next.Label}\""));
			await SaveArtifactsAsync("walkthrough");
			if ((object)next != null)
			{
				companion.SimulatePressForTest(new PointD(next.CenterPx.X + next.RadiusPx + 400.0, next.CenterPx.Y + next.RadiusPx + 300.0));
				await Task.Delay(300);
				Say($"walkthrough: after a click elsewhere, ring armed = {(object)companion.ArmedClickTarget != null}");
			}
			if (realClick)
			{
				SyntheticInput.PressEscape();
			}
		}
		finally
		{
			companion.TurnStarted -= OnTurn;
		}
		bool HasFollowUp()
		{
			lock (turns)
			{
				return turns.Any<(string, int?)>(((string Trigger, int? Step) t) => t.Trigger == "clicked_target");
			}
		}
		void OnTurn(object? s, (string Trigger, int? WalkthroughStep) turn)
		{
			lock (turns)
			{
				turns.Add(turn);
			}
		}
	}

	private async Task VoiceAsync()
	{
		RealStack real = host.Real;
		bool flag = !real.Core.Auth.IsSignedIn;
		if (flag)
		{
			flag = !(await SignInAsync());
		}
		if (flag)
		{
			return;
		}
		if (!(real.Microphone is WavFileMicrophone))
		{
			Say("voice: set HEYCLICKY_TEST_MIC_WAV to a WAV to run this");
			return;
		}
		int result;
		int repeat = ((!int.TryParse(Environment.GetEnvironmentVariable("HEYCLICKY_E2E_REPEAT"), out result)) ? 1 : Math.Clamp(result, 1, 20));
		for (int press = 1; press <= repeat; press++)
		{
			if (repeat > 1)
			{
				Say($"voice: press {press} of {repeat}");
			}
			await VoicePressAsync(real);
			if (press < repeat)
			{
				await Task.Delay(2000);
			}
		}
		await SaveArtifactsAsync("voice");
	}

	private async Task VoicePressAsync(RealStack real)
	{
		WavFileMicrophone wav = (WavFileMicrophone)real.Microphone;
		CompanionSession companion = real.Companion;
		TaskCompletionSource<long> transcript = new TaskCompletionSource<long>();
		TaskCompletionSource<long> firstAudio = new TaskCompletionSource<long>();
		string lastTranscript = "";
		Stopwatch sw = Stopwatch.StartNew();
		double audioSeconds = 0.0;
		int linesQueued = 0;
		companion.UserTranscriptUpdated += OnTranscript;
		companion.Mouth.LineAudioQueued += OnAudio;
		Say($"voice: press (WAV {wav.DurationSeconds:0.00} s)");
		await companion.BeginPushToTalkAsync();
		await Task.Delay(TimeSpan.FromSeconds(wav.DurationSeconds + 0.6));
		sw.Restart();
		await companion.EndPushToTalkAsync();
		long t2 = ((await Task.WhenAny(transcript.Task, Task.Delay(30000)) == transcript.Task) ? transcript.Task.Result : (-1));
		long t3 = ((await Task.WhenAny(firstAudio.Task, Task.Delay(60000)) == firstAudio.Task) ? firstAudio.Task.Result : (-1));
		await WaitIdleAsync(companion, 90000);
		companion.UserTranscriptUpdated -= OnTranscript;
		companion.Mouth.LineAudioQueued -= OnAudio;
		Say($"voice: transcript = \"{lastTranscript}\" ({t2} ms after key-up; final transcript events may arrive before release)");
		Say($"voice: first audio queued {t3} ms after key-up; lines with audio {linesQueued}, audio {audioSeconds:0.00} s, mouth enqueued {companion.Mouth.EnqueuedSecondsSinceFlush:0.00} s, played {real.Audio.PlayedSeconds:0.00} s");
		Say("voice: answer = " + Trim(companion.ResponseText));
		void OnAudio(int line, double seconds)
		{
			Interlocked.Increment(ref linesQueued);
			audioSeconds += seconds;
			firstAudio.TrySetResult(sw.ElapsedMilliseconds);
		}
		void OnTranscript(object? s, string t)
		{
			lastTranscript = t;
			if (t.Length > 0 && sw.IsRunning)
			{
				transcript.TrySetResult(sw.ElapsedMilliseconds);
			}
		}
	}

	private async Task AgentAsync()
	{
		RealStack real = host.Real;
		bool flag = !real.Core.Auth.IsSignedIn;
		if (flag)
		{
			flag = !(await SignInAsync());
		}
		if (flag)
		{
			return;
		}
		AgentService agents = real.Agents;
		Stopwatch sw = Stopwatch.StartNew();
		host.DispatcherQueue.TryEnqueue(delegate
		{
			host.ShowInChat(null, null);
		});
		string prompt = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_AGENT") ?? "Create a text file named hello-windows.txt in your output folder that says 'Hello from HeyClicky on Windows', then reply done.";
		string id = await agents.LaunchAsync(prompt);
		Say($"agent: launched {id} in {sw.ElapsedMilliseconds} ms");
		AgentTaskSnapshot snap = null;
		while (sw.Elapsed < TimeSpan.FromMinutes(4L))
		{
			snap = agents.Tasks.FirstOrDefault((AgentTaskSnapshot t) => t.TaskId == id);
			AgentTaskStatus? agentTaskStatus = snap?.Status;
			if (agentTaskStatus.HasValue)
			{
				AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
				if ((uint)(valueOrDefault - 3) <= 2u)
				{
					flag = true;
					goto IL_0285;
				}
			}
			flag = false;
			goto IL_0285;
			IL_0285:
			if (flag)
			{
				break;
			}
			await Task.Delay(500);
		}
		AgentTaskDetail taskDetail = agents.GetTaskDetail(id);
		Say($"agent: {snap?.Status} after {sw.Elapsed.TotalSeconds:0.0} s; title \"{snap?.Title}\"; reply {Trim(snap?.FinalResponseMarkdown ?? taskDetail?.FailureMessage ?? "")}");
		string text = taskDetail?.WorkspacePath;
		if (text != null && Directory.Exists(text))
		{
			List<string> source = (from f in Directory.EnumerateFiles(text, "*", SearchOption.AllDirectories)
				where File.GetLastWriteTimeUtc(f) > DateTime.UtcNow - sw.Elapsed - TimeSpan.FromSeconds(5L)
				select f).ToList();
			Say("agent: files written in " + text + ": " + string.Join(", ", source.Select(Path.GetFileName)));
		}
		Say($"agent: timings {agents.Timings}");
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			await Snapshot.SaveAsync(host.NotchUi.Window.Root, "e2e-agent-chat");
		});
	}

	private async Task InterviewAsync()
	{
		RealStack real = host.Real;
		bool flag = !real.Core.Auth.IsSignedIn;
		if (flag)
		{
			flag = !(await SignInAsync());
		}
		if (flag)
		{
			return;
		}
		string text = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_ANSWERS");
		if (text == null)
		{
			global::_003C_003Ey__InlineArray4<string> buffer = default(global::_003C_003Ey__InlineArray4<string>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 0) = "I make YouTube videos about indie game development, and I'm building a small cozy farming game on the side.";
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 1) = "Getting the channel to 100k subscribers this year and shipping the game's demo on Steam by December.";
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 2) = "Scripting. I spend hours on the hook and researching what other devlog channels are doing, and I keep putting off the Steam page.";
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 3) = "Mostly ChatGPT. I tried Claude Code once to fix a Godot script.";
			text = string.Join("|", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray4<string>, string>(in buffer, 4));
		}
		string[] answers = text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		InterviewController interview = null;
		await OnUi(delegate
		{
			host.DemoTutorial(TutorialWindow.Step.ClickyInterview);
			interview = host.Interview;
		});
		InterviewController iv = interview;
		Stopwatch sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(30L) && (!iv.HasHeardFirstQuestion || iv.IsOpenerSpeaking))
		{
			await Task.Delay(250);
		}
		Say($"interview: opener heard={iv.HasHeardFirstQuestion} after {sw.ElapsedMilliseconds} ms: {Trim(iv.SpokenText, 300)}");
		await SnapAsync("e2e-interview-opener");
		int i;
		for (i = 0; i < answers.Length; i++)
		{
			Stopwatch turn = Stopwatch.StartNew();
			await OnUi(delegate
			{
				iv.SubmitTyped(answers[i]);
			});
			Say($"interview: answer {i + 1}: {answers[i]}");
			await Task.Delay(800);
			await WaitIdleAsync(real.Companion, 90000);
			Say($"interview: reply {i + 1} after {turn.ElapsedMilliseconds} ms: {Trim(real.Companion.ResponseText, 400)}");
			if (i == 1)
			{
				await SnapAsync("e2e-interview-answer2");
			}
		}
		await SnapAsync("e2e-interview-wrapup");
		Stopwatch planWait = Stopwatch.StartNew();
		while (planWait.Elapsed < TimeSpan.FromMinutes(4L) && !iv.HasBuiltPlan && (iv.IsPlanInFlight || iv.IsActive))
		{
			await Task.Delay(500);
		}
		Say($"interview: transcript {iv.Transcript.Count} lines ({iv.Transcript.Count((InterviewLine l) => l.Role == "user")} answers)");
		foreach (InterviewLine item in iv.Transcript)
		{
			Say("interview:   " + item.Role + ": " + Trim(item.Text, 300));
		}
		Say($"interview: plan built={iv.HasBuiltPlan} in {planWait.Elapsed.TotalSeconds:0.0} s after the wrap-up watch; error={iv.PlanError ?? "none"}");
		StarterPlanResponse lastPlan = iv.LastPlan;
		if ((object)lastPlan != null)
		{
			Say("interview: goal_line=\"" + lastPlan.GoalLine + "\" agent_familiarity=" + lastPlan.AgentFamiliarity);
			foreach (StarterPlanClicky item2 in lastPlan.Clickys ?? Array.Empty<StarterPlanClicky>())
			{
				Say($"interview: clicky {item2.Key}: {item2.Name} ({item2.Role}) — {item2.Description} | reveal: {item2.RevealLine} | intro: {string.Join(" / ", item2.IntroMessages ?? Array.Empty<string>())} | first asks: {string.Join(" / ", item2.FirstAsks ?? Array.Empty<string>())}");
			}
			foreach (StarterPlanSuggestion item3 in lastPlan.SuggestedAgents ?? Array.Empty<StarterPlanSuggestion>())
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 5);
				defaultInterpolatedStringHandler.AppendLiteral("interview: task \"");
				defaultInterpolatedStringHandler.AppendFormatted(item3.Title);
				defaultInterpolatedStringHandler.AppendLiteral("\" for ");
				defaultInterpolatedStringHandler.AppendFormatted(item3.ClickyKey ?? item3.ClickySlug);
				defaultInterpolatedStringHandler.AppendLiteral(" [");
				defaultInterpolatedStringHandler.AppendFormatted(item3.Deliverable);
				StarterPlanRoutine routine = item3.Routine;
				defaultInterpolatedStringHandler.AppendFormatted(((object)routine != null) ? $", routine every {routine.EveryMinutes} min" : "");
				defaultInterpolatedStringHandler.AppendLiteral("] — ");
				defaultInterpolatedStringHandler.AppendFormatted(item3.Description);
				Say(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}
		await OnUi(delegate
		{
			host.DemoTutorial(TutorialWindow.Step.Finale);
		});
		await Task.Delay(600);
		TaskCompletionSource tourDone = new TaskCompletionSource();
		await OnUi(delegate
		{
			host.DemoTutorialWindow()?.Hide();
			host.RunFinaleRevealAsync().ContinueWith((Task _) => tourDone.TrySetResult());
		});
		Stopwatch tourWait = Stopwatch.StartNew();
		await Task.WhenAny(tourDone.Task, Task.Delay(TimeSpan.FromMinutes(4L)));
		Say($"interview: home tour finished={tourDone.Task.IsCompleted} in {tourWait.Elapsed.TotalSeconds:0.0} s");
		ClickyRoster shared = ClickyRoster.Shared;
		Say($"interview: roster now [{string.Join(", ", shared.Clickys.Select((RosterClicky c) => c.Slug))}]; agents' store [{string.Join(", ", real.Agents.Clickys.Select((ClickyInfo c) => c.Slug))}]");
	}

	private Task OnUi(Action a)
	{
		TaskCompletionSource tcs = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(delegate
		{
			try
			{
				a();
				tcs.TrySetResult();
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
		});
		return tcs.Task;
	}

	private async Task SnapAsync(string name)
	{
		TaskCompletionSource tcs = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			Environment.SetEnvironmentVariable("HEYCLICKY_SNAPSHOT_DIR", outDir);
			UIElement uIElement = host.DemoTutorialRoot();
			if ((object)uIElement != null)
			{
				await Snapshot.SaveAsync(uIElement, name);
			}
			tcs.TrySetResult();
		});
		await tcs.Task;
	}

	private static async Task WaitIdleAsync(ICompanionSession companion, int timeoutMs)
	{
		Stopwatch sw = Stopwatch.StartNew();
		await Task.Delay(500);
		while (companion.State != 0 && sw.ElapsedMilliseconds < timeoutMs)
		{
			await Task.Delay(200);
		}
		await Task.Delay(800);
	}

	private async Task SaveArtifactsAsync(string name)
	{
		RealStack real = host.Real;
		try
		{
			byte[] png = real.Canvas.RenderDrawingPng(null, 1.0, overScreenshot: true, includeButtons: true);
			if (png != null)
			{
				string path = Path.Combine(outDir, "e2e-" + name + "-drawing.png");
				await File.WriteAllBytesAsync(path, png);
				Say($"{name}: drawing PNG {png.Length} bytes â†’ {path}");
			}
			else
			{
				Say(name + ": no drawing to render");
			}
			Say(name + ": drawing state = " + Trim(real.Canvas.DrawingStateForModel() ?? "", 300));
		}
		catch (Exception ex)
		{
			Say(name + ": drawing render failed: " + ex.Message);
		}
		TaskCompletionSource tcs = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			Environment.SetEnvironmentVariable("HEYCLICKY_SNAPSHOT_DIR", outDir);
			await Snapshot.SaveAsync(host.NotchUi.Window.Root, "e2e-" + name + "-notch");
			tcs.TrySetResult();
		});
		await tcs.Task;
	}

	private static string Trim(string s, int max = 200)
	{
		s = s.Replace('\n', ' ').Trim();
		if (s.Length <= max)
		{
			return s;
		}
		return s.Substring(0, max) + "…";
	}

	private async Task HarnessAsync()
	{
		RealStack real = host.Real;
		AgentService agents = real.Agents;
		(AgentHarness, AgentHarnessPlace) tuple = AgentHarnessThreads.Choice(null);
		Say("h1: new conversations run on " + tuple.Item1.DisplayName() + " " + tuple.Item2.DisplayName());
		bool value;
		try
		{
			await AgentHarnessRunnerProcess.EnsureRunningAsync();
			value = true;
		}
		catch (AgentHarnessException ex)
		{
			value = false;
			Say("h1: " + ex.Message);
		}
		Say($"h1: runner available={value} (bundled runner {(File.Exists(AgentHarnessRunnerProcess.BundledRunnerPath) ? "present" : "missing")} at {AgentHarnessRunnerProcess.BundledRunnerPath})");
		string jobTaskId = null;
		new List<string>();
		agents.TaskEvent += OnEvent;
		agents.LiveSteps.Changed += OnLive;
		try
		{
			Stopwatch sw = Stopwatch.StartNew();
			string prompt = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_AGENT") ?? "Find today's date and the current time, write them into a file called today.txt in your output folder, and tell me what you wrote.";
			jobTaskId = await agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = prompt,
				NewClickyName = "Writing today's date",
				LaunchSource = "text",
				LaunchContext = "The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it."
			});
			Say($"h1: launched task {jobTaskId} in {sw.ElapsedMilliseconds} ms");
			AgentTaskDetail detail = await WaitForTaskEndAsync(agents, jobTaskId, TimeSpan.FromMinutes(6L));
			ReportJobTurn(agents, jobTaskId, detail, sw);
			await WaitForHandBackAsync(detail?.Snapshot.ThreadId, TimeSpan.FromSeconds(90L));
			string jobId = detail?.JobId;
			if (jobId != null && Environment.GetEnvironmentVariable("HEYCLICKY_E2E_SKIP_FOLLOWUP") != "1")
			{
				Stopwatch follow = Stopwatch.StartNew();
				string followId = await agents.LaunchAsync(new AgentLaunchRequest
				{
					Prompt = "Add one more line to today.txt with the day of the week, then tell me the file's contents.",
					JobId = jobId,
					LaunchSource = "text"
				});
				Say($"h1: follow-up sent to job {jobId} (task {followId}; same task: {followId == jobTaskId})");
				jobTaskId = followId;
				await Task.Delay(1500);
				AgentTaskDetail agentTaskDetail = await WaitForTaskEndAsync(agents, followId, TimeSpan.FromMinutes(6L));
				ReportJobTurn(agents, followId, agentTaskDetail, follow);
				await WaitForHandBackAsync(agentTaskDetail?.Snapshot.ThreadId, TimeSpan.FromSeconds(90L));
			}
		}
		finally
		{
			agents.TaskEvent -= OnEvent;
			agents.LiveSteps.Changed -= OnLive;
		}
		void OnEvent(object? _, AgentTaskEvent e)
		{
			if (!(e.TaskId != jobTaskId))
			{
				switch (e.Kind)
				{
				case AgentTaskEventKind.PlanUpdated:
				{
					IReadOnlyList<AgentPlanStep> source = agents.GetTaskDetail(e.TaskId)?.Plan ?? Array.Empty<AgentPlanStep>();
					Say("h1: plan [" + string.Join(" | ", source.Select((AgentPlanStep s) => s.Text + " (" + s.Status + ")")) + "]");
					break;
				}
				case AgentTaskEventKind.StatusChanged:
				case AgentTaskEventKind.ThinkingHeading:
				case AgentTaskEventKind.Commentary:
				case AgentTaskEventKind.ToolActivity:
					Say($"h1: {e.Kind}: {Trim(e.Text ?? "", 160)}");
					break;
				case AgentTaskEventKind.Finished:
				case AgentTaskEventKind.Failed:
				case AgentTaskEventKind.Stopped:
					Say($"h1: {e.Kind}: {Trim(e.Text ?? "", 300)}");
					break;
				}
			}
		}
		void OnLive(string key)
		{
			ClickyLiveLine clickyLiveLine = agents.LiveSteps.Line(key);
			if ((object)clickyLiveLine != null)
			{
				Say($"h1: live line ({key}): {clickyLiveLine.Kind} {clickyLiveLine.Text}");
			}
		}
	}

	private static async Task<AgentTaskDetail?> WaitForTaskEndAsync(AgentService agents, string taskId, TimeSpan limit)
	{
		Stopwatch sw = Stopwatch.StartNew();
		while (true)
		{
			bool flag = sw.Elapsed < TimeSpan.FromSeconds(20L);
			bool flag2;
			if (flag)
			{
				AgentTaskStatus? agentTaskStatus = agents.GetTaskDetail(taskId)?.Snapshot.Status;
				if (agentTaskStatus.HasValue)
				{
					AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
					if ((uint)valueOrDefault <= 1u)
					{
						flag2 = true;
						goto IL_00ea;
					}
				}
				flag2 = false;
				goto IL_00ea;
			}
			goto IL_00f0;
			IL_00f0:
			if (!flag)
			{
				break;
			}
			await Task.Delay(250);
			continue;
			IL_00ea:
			flag = !flag2;
			goto IL_00f0;
		}
		while (sw.Elapsed < limit)
		{
			AgentTaskDetail taskDetail = agents.GetTaskDetail(taskId);
			AgentTaskStatus? agentTaskStatus = taskDetail?.Snapshot.Status;
			bool flag;
			if (agentTaskStatus.HasValue)
			{
				AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
				if ((uint)(valueOrDefault - 2) <= 3u)
				{
					flag = true;
					goto IL_0150;
				}
			}
			flag = false;
			goto IL_0150;
			IL_0150:
			if (flag)
			{
				return taskDetail;
			}
			await Task.Delay(500);
		}
		return agents.GetTaskDetail(taskId);
	}

	private void ReportJobTurn(AgentService agents, string taskId, AgentTaskDetail? detail, Stopwatch sw)
	{
		if ((object)detail == null)
		{
			Say("h1: FAILED no task detail");
			return;
		}
		Say($"h1: {detail.Snapshot.Status} after {sw.Elapsed.TotalSeconds:0.0} s; harness turn={detail.IsHarnessTurn}; thread {detail.Snapshot.ThreadId}; turn {detail.TurnId}");
		Say("h1: title \"" + detail.Snapshot.Title + "\"; summary: " + Trim(detail.Summary ?? detail.FailureMessage ?? "", 300));
		Say("h1: final plan [" + string.Join(" | ", (detail.Plan ?? Array.Empty<AgentPlanStep>()).Select((AgentPlanStep s) => s.Text + " (" + s.Status + ")")) + "]");
		ClickyAnswer answer = detail.Answer;
		if ((object)answer != null)
		{
			Say($"h1: ANSWER outcome \"{answer.Line}\"; blocks [{string.Join(",", answer.Blocks)}]; report JSON {answer.ReportJson}");
		}
		else
		{
			Say("h1: no <ANSWER> block");
		}
		Say($"h1: artifacts [{string.Join(", ", detail.Artifacts)}]; next actions [{string.Join(" | ", detail.NextActions)}]");
		Say("h1: full report: " + Trim(detail.Snapshot.FinalResponseMarkdown ?? "", 600));
		string jobId = detail.JobId;
		if (jobId != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(jobId);
			if ((object)clickyJob != null)
			{
				Say($"h1: job {clickyJob.Id} \"{clickyJob.Title}\" outcome={clickyJob.Outcome} phase={agents.RailPhase(clickyJob)} folder={clickyJob.FolderPath}");
				Say("h1: job runs: " + string.Join(" / ", (clickyJob.Runs ?? Array.Empty<ClickyJobRun>()).Select((ClickyJobRun r) => $"[{r.Outcome}] {Trim(r.Ask, 60)} thread={r.ThreadId}")));
				Say($"h1: job summary: {Trim(clickyJob.Summary ?? "")}; files from latest reply: [{string.Join(", ", clickyJob.FilesFromLatestReply)}]");
				if (Directory.Exists(Path.Combine(clickyJob.FolderPath, "output")))
				{
					foreach (string item in Directory.EnumerateFiles(Path.Combine(clickyJob.FolderPath, "output")))
					{
						Say("h1: output file " + Path.GetFileName(item) + ": " + Trim(File.ReadAllText(item)));
					}
				}
				string path = Path.Combine(Path.GetDirectoryName(clickyJob.FolderPath), "WORK.md");
				if (File.Exists(path))
				{
					Say("h1: WORK.md: " + Trim(File.ReadAllText(path), 300));
				}
				return;
			}
		}
		Say("h1: (not a job)");
	}

	private async Task WaitForHandBackAsync(string? threadId, TimeSpan limit)
	{
		if (threadId != null)
		{
			OneBrainJobReports reports = host.JobReports;
			if (reports != null)
			{
				Stopwatch sw = Stopwatch.StartNew();
				while (sw.Elapsed < limit && !reports.HasPresented(threadId))
				{
					await Task.Delay(500);
				}
				IReadOnlyList<string> readOnlyList = reports.Lines(threadId);
				Say($"h1: hand-back after {sw.Elapsed.TotalSeconds:0.0} s: presented={reports.HasPresented(threadId)} lines={readOnlyList.Count}: {Trim(string.Join(" ", readOnlyList), 400)}");
				ClickyAnswer clickyAnswer = host.Real.Agents.ResultCard(threadId);
				if ((object)clickyAnswer != null)
				{
					Say($"h1: hand-back card: outcome \"{clickyAnswer.Line}\" blocks [{string.Join(",", clickyAnswer.Blocks)}]");
				}
				else
				{
					Say("h1: hand-back card: none");
				}
				return;
			}
		}
		Say("h1: hand-back: not wired");
	}

	private async Task IntroNotchAsync()
	{
		if (!(await SignInAsync()))
		{
			return;
		}
		NotchPresenter p = host.CorePresenter;
		if (p == null)
		{
			Say("intro-notch: no core presenter");
			return;
		}
		await OnUi(delegate
		{
			host.DemoIntroSequence(isReplay: true);
		});
		await Task.Delay(500);
		Say($"intro-notch: started, intro owns the stage={p.IsOnboardingIntroOwningStage}");
		await HoverAndClickAsync("the welcome");
		for (int i = 0; i < 300; i++)
		{
			if ((object)host.CursorCompanion.CurrentPlayer != null)
			{
				break;
			}
			await Task.Delay(100);
		}
		Say($"intro-notch: video playing={(object)host.CursorCompanion.CurrentPlayer != null}");
		await HoverAndClickAsync("the video");
		await OnUi(delegate
		{
			p.SetPointerInside(inside: true);
		});
		await OnUi(delegate
		{
			host.CursorCompanion.SkipVideo();
		});
		for (int i = 0; i < 150; i++)
		{
			if (!host.IsIntroSequenceActive)
			{
				break;
			}
			await Task.Delay(100);
		}
		await Task.Delay(1000);
		Say($"intro-notch: after the reel: intro active={host.IsIntroSequenceActive} owns the stage={p.IsOnboardingIntroOwningStage} hovering={p.IsHovering} shown={Shown(p)}");
		await OnUi(delegate
		{
			p.SetPointerInside(inside: false);
		});
		Say("intro-notch: done");
		async Task HoverAndClickAsync(string phase)
		{
			await OnUi(delegate
			{
				p.SetPointerInside(inside: false);
				p.SetPointerInside(inside: true);
			});
			await Task.Delay(1500);
			Say($"intro-notch: hover during {phase}: hovering={p.IsHovering} shown={Shown(p)} open={p.IsShowingNotchAgentPeek}");
			await OnUi(delegate
			{
				host.SimulateNotchIdleClick();
			});
			await Task.Delay(500);
			Say("intro-notch: click during " + phase + ": shown=" + Shown(p));
			await OnUi(delegate
			{
				p.SetPointerInside(inside: false);
			});
		}
		static string Shown(NotchPresenter n)
		{
			if (!(n.Presentation is NotchPresentation.LiveEvent liveEvent))
			{
				return n.Presentation.GetType().Name;
			}
			return "LiveEvent." + liveEvent.Event.GetType().Name;
		}
	}

	private async Task LessonReplayAsync()
	{
		if (!(await SignInAsync()))
		{
			return;
		}
		LessonService lessons = host.Lessons;
		if (lessons == null)
		{
			Say("lesson-replay: no lesson service");
			return;
		}
		StoredLesson lesson = lessons.Store.List().FirstOrDefault();
		if (lesson == null)
		{
			Say("lesson-replay: no lesson in " + LessonStore.Root);
			return;
		}
		Say($"lesson-replay: \"{lesson.Data.Title}\", {lesson.Data.Lines.Count} lines over {lesson.Data.Duration:0.0} s");
		ConcurrentDictionary<string, int> thrown = new ConcurrentDictionary<string, int>();
		AppDomain.CurrentDomain.FirstChanceException += OnThrown;
		int mark = LogMark();
		await OnUi(delegate
		{
			lessons.PlayAsync(lesson);
		});
		DateTime until = DateTime.UtcNow + TimeSpan.FromSeconds(lesson.Data.Duration + 20.0);
		new List<string>();
		List<string> source;
		while (DateTime.UtcNow < until)
		{
			await Task.Delay(500);
			source = LogSince(mark);
			if (source.Any((string l) => l.Contains("[Drawing] timeline")))
			{
				break;
			}
		}
		await Task.Delay(500);
		AppDomain.CurrentDomain.FirstChanceException -= OnThrown;
		Say($"lesson-replay: {thrown.Values.Sum()} exceptions thrown while it played");
		foreach (var (value, value2) in thrown.OrderByDescending<KeyValuePair<string, int>, int>((KeyValuePair<string, int> p) => p.Value).Take(8))
		{
			Say($"  {value2} x {value}");
		}
		source = LogSince(mark);
		int num2 = source.FindIndex((string l) => l.Contains("[Drawing] timeline"));
		if (num2 < 0)
		{
			Say("lesson-replay: no timeline logged");
			return;
		}
		foreach (string item in source.Skip(num2 + 1).TakeWhile((string l) => l.Length > 0 && !char.IsDigit(l[0])))
		{
			Say("  " + item.Trim());
		}
		int value3 = source.Skip(num2 + 1).TakeWhile((string l) => l.Length > 0 && !char.IsDigit(l[0])).Count((string l) => l.Contains("<<"));
		int value4 = source.Skip(num2 + 1).TakeWhile((string l) => l.Length > 0 && !char.IsDigit(l[0])).Count((string l) => l.Contains("not drawn"));
		Say($"lesson-replay: {value3} calls started late, {value4} not drawn");
		void OnThrown(object? sender, FirstChanceExceptionEventArgs e)
		{
			thrown.AddOrUpdate($"{e.Exception.GetType().Name} in {e.Exception.TargetSite?.DeclaringType?.Name}.{e.Exception.TargetSite?.Name}", 1, (string _, int n) => n + 1);
		}
	}

	private async Task LessonsAsync()
	{
		if (!(await SignInAsync()))
		{
			return;
		}
		RealStack real = host.Real;
		LessonService lessons = host.Lessons;
		if (lessons == null)
		{
			Say("lessons: no lesson service");
			return;
		}
		real.Core.Settings.Update(delegate(AppSettings s)
		{
			s.SaveDrawings = false;
			s.KeepDrawingsInChat = false;
		});
		int recentBefore = lessons.RecentDrawings.Count;
		int mark = LogMark();
		await TypedTurnAsync("Circle the middle of my screen and explain in three short spoken steps what a screen's middle is good for. Call it lessonTitle(\"The middle\").");
		await Task.Delay(1500);
		List<string> source = LogSince(mark);
		Check("with Save drawings off nothing is kept", lessons.RecentDrawings.Count == recentBefore && !source.Any((string l) => l.Contains("drawing told:")));
		Check("with Save drawings off the drawing left up gets Clear only", source.Any((string l) => l.Contains("clear offer shown (Clear only)")) || !real.Canvas.IsShowingDrawing);
		await OnUi(delegate
		{
			real.Canvas.Clear(animated: false);
		});
		await Task.Delay(800);
		real.Core.Settings.Update(delegate(AppSettings s)
		{
			s.SaveDrawings = true;
			s.KeepDrawingsInChat = false;
		});
		recentBefore = lessons.RecentDrawings.Count;
		mark = LogMark();
		await TypedTurnAsync("Explain how a cache speeds up a website. Draw it on my screen in three or four short steps and call the drawing lessonTitle(\"How a cache works\").");
		for (int i = 0; i < 40; i++)
		{
			if (lessons.RecentDrawings.Count != recentBefore)
			{
				break;
			}
			await Task.Delay(250);
		}
		if (lessons.RecentDrawings.Count == recentBefore)
		{
			Check("an explanation was told", passed: false);
			return;
		}
		IReadOnlyList<LessonService.Recent> recentDrawings = lessons.RecentDrawings;
		LessonService.Recent recent = recentDrawings[recentDrawings.Count - 1];
		DrawingRecord record = recent.Record;
		byte[] narration = record.Narration;
		double num = ((narration != null) ? ((double)narration.Length / 2.0 / (double)record.NarrationSampleRate) : 0.0);
		Say($"lessons: told \"{record.Title}\": {record.Lines.Count} lines at [{string.Join(", ", record.Lines.Select((DrawingRecord.Line l) => $"{l.Start:0.00}+{l.Length:0.00}"))}], {record.Duration:0.0} s, voice {num:0.0} s, {recent.Photos.Count} photos");
		Check("each line's voice was recorded into one track", record.Narration != null && Math.Abs(num - (double)(int)(record.Duration * (double)record.NarrationSampleRate) / (double)record.NarrationSampleRate) < 0.01);
		Check("every line lasts as long as its audio", record.Lines.All(delegate(DrawingRecord.Line l)
		{
			double? length = l.Length;
			return length.HasValue && length.GetValueOrDefault() > 0.2;
		}));
		Check("the save offer shows Save", LogSince(mark).Any((string l) => l.Contains("[lessons] save offer shown")));
		TaskCompletionSource<StoredLesson?> saveTask = new TaskCompletionSource<StoredLesson>();
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			TaskCompletionSource<StoredLesson?> taskCompletionSource = saveTask;
			taskCompletionSource.TrySetResult(await lessons.SaveAsync(recent));
		});
		StoredLesson saved = await saveTask.Task;
		if (saved == null)
		{
			Check("the lesson was saved", passed: false);
			return;
		}
		(byte[], int)? tuple = ((byte[], int)?)(File.Exists(saved.NarrationPath) ? ((ValueType)LessonNarration.FromWav(File.ReadAllBytes(saved.NarrationPath))) : ((ValueType)((byte[], int)?)null));
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
		defaultInterpolatedStringHandler.AppendLiteral("narration.wav saved with the lesson (");
		double value;
		if (tuple.HasValue)
		{
			(byte[], int) valueOrDefault = tuple.GetValueOrDefault();
			value = (double)valueOrDefault.Item1.Length / 2.0 / (double)valueOrDefault.Item2;
		}
		else
		{
			value = 0.0;
		}
		defaultInterpolatedStringHandler.AppendFormatted(value, "0.0");
		defaultInterpolatedStringHandler.AppendLiteral(" s)");
		Check(defaultInterpolatedStringHandler.ToStringAndClear(), saved.Data.HasNarration && tuple.HasValue);
		int num2 = (Directory.Exists(saved.PhotosFolder) ? Directory.GetFiles(saved.PhotosFolder).Length : 0);
		Check($"its photos were saved with it ({num2} of {recent.Photos.Count})", num2 == recent.Photos.Count);
		await Task.Delay(2500);
		await OnUi(delegate
		{
			real.Canvas.Clear(animated: false);
		});
		await Task.Delay(800);
		mark = LogMark();
		OnUi(delegate
		{
			lessons.PlayAsync(saved);
		});
		double untilSecondLine = ((saved.Data.Lines.Count > 1) ? (saved.Data.Lines[1].Start - saved.ReplayShift) : 2.0);
		int ridingSamples = 0;
		await Task.Run(async delegate
		{
			DateTime until = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Min(20.0, untilSecondLine + 1.2));
			while (DateTime.UtcNow < until)
			{
				if (real.Cursor.Inner?.GetDiagnostics().Mode == "RidingPen")
				{
					ridingSamples++;
				}
				await Task.Delay(40);
			}
		});
		if (real.Cursor.Inner != null)
		{
			Check($"the cursor rides the pen during the replay ({ridingSamples} samples)", ridingSamples > 0);
		}
		List<string> source2 = LogSince(mark);
		foreach (string item in source2.Where((string l) => l.Contains("[lessons] replay")))
		{
			Say("  log: " + Trim(item, 220));
		}
		Check("the full-screen replay plays its recorded voice", source2.Any((string l) => l.Contains("with its recorded voice")));
		Check("lines are released in step with the voice", source2.Count((string l) => l.Contains("[lessons] replay line")) >= 2 && lessons.IsReplaying);
		byte[] array = real.Canvas.RenderDrawingPng(null, 0.5);
		if (array != null)
		{
			await File.WriteAllBytesAsync(Path.Combine(outDir, "lessons-replay.png"), array);
		}
		PressKeys(27);
		await Task.Delay(1000);
		List<string> escapeLog = LogSince(mark);
		Check("a real Escape stops the replay (StopReplay)", !lessons.IsReplaying && escapeLog.Any((string l) => l.Contains("replay stopped (Escape)")));
		await Task.Delay(1500);
		Check("nothing more is released after Escape", LogSince(mark).Count((string l) => l.Contains("[lessons] replay line")) == escapeLog.Count((string l) => l.Contains("[lessons] replay line")));
		static void Check(string what, bool passed)
		{
			Say("lessons: " + (passed ? "PASS" : "FAIL") + " " + what);
		}
	}
}
