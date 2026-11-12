using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Shell;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Platform.Overlay;

namespace HeyClicky.App.Demo;

public sealed class ModesEndToEnd(AppHost host, string outDir)
{
	private const uint LeftDown = 2u;

	private const uint LeftUp = 4u;

	private NotchPresentation Presentation => host.Services.Presenter.Presentation;

	private static void Say(string message)
	{
		Log.Info("e2e", "modes: " + message);
		try
		{
			File.AppendAllText(Path.Combine(Environment.GetEnvironmentVariable("HEYCLICKY_E2E_OUT") ?? Path.GetTempPath(), "e2e.log"), $"{DateTime.Now:HH:mm:ss.fff} modes: {message}{Environment.NewLine}");
		}
		catch
		{
		}
	}

	[DllImport("user32.dll", EntryPoint = "SetCursorPos")]
	private static extern bool SetCursorPosRaw(int x, int y);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	private static bool SetCursorPos(int x, int y)
	{
		int systemMetrics = GetSystemMetrics(76);
		int systemMetrics2 = GetSystemMetrics(77);
		int num = Math.Max(1, GetSystemMetrics(78) - 1);
		int num2 = Math.Max(1, GetSystemMetrics(79) - 1);
		int dx = (int)Math.Round((double)(x - systemMetrics) * 65535.0 / (double)num);
		int dy = (int)Math.Round((double)(y - systemMetrics2) * 65535.0 / (double)num2);
		mouse_event(49153u, dx, dy, 0u, 0);
		return true;
	}

	[DllImport("user32.dll")]
	private static extern void mouse_event(uint flags, int dx, int dy, uint data, nint extra);

	private Task OnUi(Action a)
	{
		TaskCompletionSource tcs = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(delegate
		{
			try
			{
				a();
				tcs.SetResult();
			}
			catch (Exception exception)
			{
				tcs.SetException(exception);
			}
		});
		return tcs.Task;
	}

	private Task<T> OnUi<T>(Func<T> f)
	{
		TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
		host.DispatcherQueue.TryEnqueue(delegate
		{
			try
			{
				tcs.SetResult(f());
			}
			catch (Exception exception)
			{
				tcs.SetException(exception);
			}
		});
		return tcs.Task;
	}

	private async Task Snap(string name)
	{
		string path = Path.Combine(outDir, "snap-" + name + ".req");
		string done = Path.Combine(outDir, "snap-" + name + ".done");
		try
		{
			File.Delete(done);
		}
		catch
		{
		}
		File.WriteAllText(path, name);
		Stopwatch sw = Stopwatch.StartNew();
		while (!File.Exists(done) && sw.ElapsedMilliseconds < 6000)
		{
			await Task.Delay(50);
		}
		Say(File.Exists(done) ? ("screenshot " + name + ".png") : ("screenshot " + name + ": no watcher"));
	}

	private static async Task Move(int x, int y, int steps = 12, int ms = 10)
	{
		Native.GetCursorPos(out var p);
		for (int i = 1; i <= steps; i++)
		{
			SetCursorPos(p.X + (x - p.X) * i / steps, p.Y + (y - p.Y) * i / steps);
			await Task.Delay(ms);
		}
	}

	private static async Task Click(int x, int y)
	{
		SetCursorPos(x, y);
		await Task.Delay(60);
		mouse_event(2u, 0, 0, 0u, 0);
		await Task.Delay(60);
		mouse_event(4u, 0, 0, 0u, 0);
	}

	private static string Describe(NotchPresentation p)
	{
		return NotchView.Describe(p) + ((p is NotchPresentation.OnboardingPinned onboardingPinned) ? $"({onboardingPinned.Step})" : "");
	}

	private async Task<bool> WaitFor(Func<bool> condition, int timeoutMs, int pollMs = 50)
	{
		Stopwatch sw = Stopwatch.StartNew();
		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			if (condition())
			{
				return true;
			}
			await Task.Delay(pollMs);
		}
		return condition();
	}

	public async Task RunAsync(Func<Task<bool>> signIn)
	{
		RealStack real = host.Real;
		CursorOverlay cursor = real.Cursor.Inner;
		if (cursor == null)
		{
			Say("FAIL no cursor overlay (offscreen run?)");
			return;
		}
		Displays.Monitor m = Displays.Primary();
		int cx = m.Bounds.Left + m.Bounds.Width / 2;
		int cy = m.Bounds.Top + m.Bounds.Height / 2;
		double sc = m.Scale;
		Process backdrop = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.Form; $f.WindowState = 'Maximized'; $f.BackColor = 'Gainsboro'; $f.Text = 'HeyClicky e2e backdrop'; $f.ShowDialog()\"")
		{
			UseShellExecute = false,
			CreateNoWindow = true
		});
		await Task.Delay(2500);
		try
		{
			await RunStepsAsync(signIn, real, cursor, m, cx, cy, sc);
		}
		finally
		{
			try
			{
				backdrop?.Kill(entireProcessTree: true);
			}
			catch
			{
			}
		}
	}

	private async Task RunStepsAsync(Func<Task<bool>> signIn, RealStack real, CursorOverlay cursor, Displays.Monitor m, int cx, int cy, double sc)
	{
		await Move(cx - 200, cy);
		Task said = host.SayAtCursorAsync("Your report is ready. I saved it to your Documents folder, and the chart is on page two.", streamed: true, 3.0);
		await Task.Delay(2600);
		(string, double, bool, bool, bool, double) stateDiagnostics = cursor.GetStateDiagnostics();
		Say($"{((stateDiagnostics.Item1.Length > 20 && stateDiagnostics.Item2 > 0.9) ? "PASS" : "FAIL")} message bubble streaming: \"{stateDiagnostics.Item1}\" opacity {stateDiagnostics.Item2:0.00}");
		cursor.SnapshotMessagePng(Path.Combine(outDir, "cursor-message-offscreen.png"));
		await Snap("01-message-bubble");
		await Task.WhenAny(said, Task.Delay(10000));
		stateDiagnostics = cursor.GetStateDiagnostics();
		Say(((said.IsCompleted && stateDiagnostics.Item2 == 0.0) ? "PASS" : "FAIL") + " message bubble faded after its hold");
		host.SayAtCursorAsync("Building that snake game.", streamed: false, 2.0);
		await Task.Delay(500);
		Say(((cursor.GetStateDiagnostics().MessageText == "Building that snake game.") ? "PASS" : "FAIL") + " start cue pops in whole");
		await Snap("02-start-cue");
		await Task.Delay(2600);
		Task welcome = host.PlayCursorWelcomeAsync();
		await Task.Delay(800);
		stateDiagnostics = cursor.GetStateDiagnostics();
		Say(((stateDiagnostics.Item1 == "hi, im heyclicky") ? "PASS" : "FAIL") + " welcome typed: \"" + stateDiagnostics.Item1 + "\"");
		await Snap("03-welcome");
		await Task.WhenAny(welcome, Task.Delay(5000));
		Say((welcome.IsCompleted ? "PASS" : "FAIL") + " welcome finished");
		await OnUi(() => host.Services.Settings.ShakeToDetachEnabled = true);
		await Task.Delay(300);
		await Move(cx, cy);
		await Task.Delay(400);
		await Shake(cx, cy);
		bool detached = await WaitFor(() => cursor.IsDetached, 1000);
		PointD frozen = cursor.GetDiagnostics().PositionPx;
		Say($"{(detached ? "PASS" : "FAIL")} shake detached the cursor at {frozen}");
		await Move(cx + 160, cy + 120);
		await Task.Delay(600);
		Say(((cursor.GetDiagnostics().PositionPx == frozen) ? "PASS" : "FAIL") + " detached cursor stays frozen while the mouse moves");
		await Snap("04-detached");
		await Move((int)frozen.X, (int)frozen.Y, 8);
		bool flag2 = await WaitFor(() => cursor.GetStateDiagnostics().DetachHovered, 1500);
		Say($"{(flag2 ? "PASS" : "FAIL")} hovering the frozen cursor (click catcher; mouse at {frozen}, still detached {cursor.IsDetached})");
		await Snap("05-detached-hover");
		await Move(cx + 300, cy + 200);
		Say(((await WaitFor(() => !cursor.IsDetached, 4500)) ? "PASS" : "FAIL") + " auto-reattached after 3 s without a click");
		await Move(cx, cy);
		await Task.Delay(300);
		await Shake(cx, cy);
		detached = await WaitFor(() => cursor.IsDetached, 1000);
		frozen = cursor.GetDiagnostics().PositionPx;
		await Click((int)frozen.X, (int)frozen.Y);
		bool flag3 = await WaitFor(() => real.Companion.State == VoiceState.Listening, 4000);
		Say($"{((detached && flag3) ? "PASS" : "FAIL")} click on the detached cursor starts listening (state {real.Companion.State})");
		await Task.Delay(700);
		await Snap("06-detached-listening-pill");
		await Click((int)frozen.X, (int)frozen.Y);
		bool flag4 = await WaitFor(() => real.Companion.State != VoiceState.Listening && !cursor.IsDetached, 4000);
		Say($"{(flag4 ? "PASS" : "FAIL")} second click stops recording and reattaches (state {real.Companion.State})");
		real.Companion.Cancel();
		await Task.Delay(800);
		await Move(cx, cy);
		await Task.Delay(300);
		await Shake(cx, cy);
		detached = await WaitFor(() => cursor.IsDetached, 1000);
		frozen = cursor.GetDiagnostics().PositionPx;
		await Click((int)frozen.X, (int)frozen.Y);
		await Task.Delay(150);
		real.Companion.Cancel();
		await Move(cx + 300, cy + 200);
		bool flag5 = await WaitFor(() => !cursor.IsDetached, 7000);
		Say($"{((detached && flag5) ? "PASS" : "FAIL")} a click whose recording ended at once still reattaches (detached {detached}, now detached {cursor.IsDetached}, state {real.Companion.State})");
		await OnUi(() => host.Services.Settings.ShakeToDetachEnabled = false);
		await Task.Delay(1500);
		await Move(cx - 300, cy + 100);
		await Task.Delay(800);
		PointD beforeHide = cursor.GetDiagnostics().PositionPx;
		await OnUi(delegate
		{
			host.SetCursorDocked(dock: true);
		});
		bool goodbyeTyped = await WaitFor(() => cursor.GetStateDiagnostics().MessageText == "okay see you <3", 2000);
		await Task.Delay(300);
		(string, double, bool, bool, bool, double) stateDiagnostics2 = cursor.GetStateDiagnostics();
		bool flag7 = Math.Abs(cursor.GetDiagnostics().PositionPx.X - beforeHide.X) < 4.0 && Math.Abs(cursor.GetDiagnostics().PositionPx.Y - beforeHide.Y) < 4.0;
		Say($"{((goodbyeTyped && flag7 && !stateDiagnostics2.Item5 && cursor.IsSayingGoodbye && !host.Services.Settings.CursorDocked && stateDiagnostics2.Item6 > 0.9) ? "PASS" : "FAIL")} goodbye: \"{stateDiagnostics2.Item1}\" beside the cursor where it was (no flight), not docked yet, opacity {stateDiagnostics2.Item6:0.00} (typed {goodbyeTyped}, stayed {flag7}: {beforeHide} → {cursor.GetDiagnostics().PositionPx}, docked {stateDiagnostics2.Item5}, saying goodbye {cursor.IsSayingGoodbye}, setting docked {host.Services.Settings.CursorDocked})");
		await Snap("07-goodbye-bubble");
		await OnUi(delegate
		{
			host.SetCursorDocked(dock: true);
		});
		Say(((cursor.IsSayingGoodbye && !cursor.IsDocked) ? "PASS" : "FAIL") + " a second hide during the goodbye is ignored");
		bool flag8 = await WaitFor(delegate
		{
			(string, double, bool, bool, bool, double) stateDiagnostics3 = cursor.GetStateDiagnostics();
			double item = stateDiagnostics3.Item6;
			if (item > 0.15 && item < 0.75)
			{
				item = stateDiagnostics3.Item2;
				if (item > 0.15)
				{
					return item < 0.75;
				}
				return false;
			}
			return false;
		}, 3000, 15);
		(string, double, bool, bool, bool, double) stateDiagnostics4 = cursor.GetStateDiagnostics();
		Say($"{(flag8 ? "PASS" : "FAIL")} cursor and bubble fade together (cursor {stateDiagnostics4.Item6:0.00}, bubble {stateDiagnostics4.Item2:0.00})");
		await Snap("07b-goodbye-fade");
		bool docked = await WaitFor(() => cursor.IsDocked && host.Services.Settings.CursorDocked, 3000);
		Say($"{((docked && cursor.GetStateDiagnostics().Opacity == 0.0) ? "PASS" : "FAIL")} docked only after the fade (opacity {cursor.GetStateDiagnostics().Opacity:0.00})");
		await Task.Delay(600);
		NotchPresentation presentation = Presentation;
		bool flag9 = ((presentation is NotchPresentation.Idle || (presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.RestingMark)) ? true : false);
		bool flag10 = flag9;
		Say($"{((docked && flag10) ? "PASS" : "FAIL")} hidden: cursor gone, notch at rest {Describe(Presentation)} {(Presentation as NotchPresentation.LiveEvent)?.Event}, opacity {cursor.GetStateDiagnostics().Opacity:0.00}");
		await Move(cx + 200, cy + 100);
		await Task.Delay(500);
		Say(((cursor.GetStateDiagnostics().Opacity == 0.0) ? "PASS" : "FAIL") + " docked cursor hidden while the mouse moves");
		await Snap("08-docked");
		host.SayAtCursorAsync("Docked, but I still talk beside your cursor.", streamed: false, 1.5);
		await Task.Delay(600);
		Say(((cursor.GetStateDiagnostics().Opacity > 0.5) ? "PASS" : "FAIL") + " docked cursor comes out to anchor a message");
		await Snap("09-docked-message");
		await Task.Delay(2500);
		await OnUi(delegate
		{
			host.SetCursorDocked(dock: false);
		});
		bool undocked = await WaitFor(() => !cursor.IsDocked && cursor.GetDiagnostics().Mode == "FollowingCursor", 3000);
		await Task.Delay(500);
		await Snap("10-shown-again");
		PointD item2 = cursor.GetDiagnostics().PositionPx;
		double num = (double)(cx + 200) + 35.0 * sc;
		double num2 = (double)(cy + 100) + 25.0 * sc;
		bool flag11 = Math.Abs(item2.X - num) < 4.0 * sc && Math.Abs(item2.Y - num2) < 4.0 * sc;
		Say($"{((undocked && flag11 && cursor.GetStateDiagnostics().Opacity > 0.9) ? "PASS" : "FAIL")} shown again: back beside the mouse ({item2}, expected {num:0},{num2:0}), opacity {cursor.GetStateDiagnostics().Opacity:0.00}, notch {Describe(Presentation)}");
		await real.Core.Auth.SignOutAsync();
		await Task.Delay(800);
		await Move(cx, m.Bounds.Top + 3);
		Say(((await WaitFor(() => Presentation is NotchPresentation.OnboardingPinned onboardingPinned && onboardingPinned.Step == -1, 2500)) ? "PASS" : "FAIL") + " signed-out hover shows the sign-in card (" + Describe(Presentation) + ")");
		await Snap("11-signed-out-hover");
		await Move(cx, cy);
		await Task.Delay(500);
		if (!(await signIn()))
		{
			Say("FAIL could not sign back in");
			return;
		}
		await Task.Delay(1500);
		await OnUi(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.TextInput(""));
		});
		await Task.Delay(500);
		real.Companion.SubmitTextAsync("What's 2+2? One short sentence, no drawing.");
		bool answered = await WaitFor(() => ((host.Services.Presenter as NotchPresenter)?.TextResponseCountdown).HasValue && real.Companion.State == VoiceState.Idle, 60000, 200);
		await Task.Delay(800);
		Say($"{(answered ? "PASS" : "FAIL")} answer countdown armed: {(host.Services.Presenter as NotchPresenter)?.TextResponseCountdown}");
		await Snap("12-answer-clock");
		await Move(cx, m.Bounds.Top + 40);
		await Task.Delay(900);
		await Snap("13-answer-hover-pills");
		TimeSpan? before = (host.Services.Presenter as NotchPresenter)?.TextResponseCountdown?.Remaining;
		await Task.Delay(1000);
		TimeSpan? timeSpan = (host.Services.Presenter as NotchPresenter)?.TextResponseCountdown?.Remaining;
		Say($"{((before == timeSpan) ? "PASS" : "FAIL")} countdown holds while hovered ({before} → {timeSpan})");
		await OnUi(delegate
		{
			(host.Services.Presenter as NotchPresenter)?.PauseTextResponseAutoDismiss();
		});
		await Move(cx, cy);
		await Task.Delay(800);
		await Snap("14-answer-paused");
		await OnUi(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.Idle());
		});
		await Task.Delay(800);
		AgentService agents = real.Agents;
		NotchPresenter presenter = host.Services.Presenter as NotchPresenter;
		string a = await agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = "Create a file named notch-a.txt in the output folder containing the word alpha. Keep it quick.",
			NewClickyName = "Strip Tester",
			LaunchSource = "text"
		});
		string b = await agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = "Create a file named notch-b.txt in the output folder containing the word beta. Keep it quick.",
			NewClickyName = "Corner Tester",
			LaunchSource = "text"
		});
		Say(((await WaitFor(() => Presentation is NotchPresentation.LiveEvent liveEvent2 && liveEvent2.Event is NotchLiveEvent.RestingMark, 5000)) ? "PASS" : "FAIL") + " working Clickys show the resting mark (" + Describe(Presentation) + ")");
		await Task.Delay(1500);
		await Snap("15-resting-mark");
		int steers = 0;
		agents.TaskEvent += OnSteer;
		try
		{
			await agents.SteerAsync(a, "Also add a second line that says done");
		}
		catch (Exception ex)
		{
			Say("steer: " + ex.Message);
		}
		await Task.Delay(500);
		agents.TaskEvent -= OnSteer;
		Say($"{((steers > 0) ? "PASS" : "INFO")} steer raised SteerAccepted x{steers} (only when the turn was still running)");
		Say(((await WaitFor(() => agents.Tasks.Where((AgentTaskSnapshot t) => t.TaskId == a || t.TaskId == b).All(delegate(AgentTaskSnapshot t)
		{
			AgentTaskStatus status = t.Status;
			return (uint)(status - 3) <= 2u;
		}), 150000, 500)) ? "PASS" : "FAIL") + " agents finished; notch " + Describe(Presentation));
		Say(((await WaitFor(() => Presentation is NotchPresentation.LiveEvent liveEvent3 && liveEvent3.Event is NotchLiveEvent.ClickyUpdate, 60000, 250)) ? "PASS" : "FAIL") + " the notch unfolds the update card (" + Describe(Presentation) + ")");
		await Snap("16-update-card");
		int num3 = NotchChat.HandOffs?.Entries.Count ?? 0;
		Say($"{((num3 > 0) ? "PASS" : "FAIL")} the chat holds {num3} hand-off event(s)");
		await Move(cx - 100, cy - 50);
		bool num4 = await WaitFor(() => cursor.GetStateDiagnostics().MessageOpacity > 0.5 && cursor.GetStateDiagnostics().MessageText.Length > 0, 12000);
		Say((num4 ? "PASS" : "INFO") + " agent finish line beside the cursor: \"" + cursor.GetStateDiagnostics().MessageText + "\"");
		if (num4)
		{
			await Snap("17-finish-line");
		}
		await OnUi(delegate
		{
			presenter?.Updates.Reset();
		});
		await Task.Delay(1500);
		await Move(cx, m.Bounds.Top + 3);
		bool flag12 = await WaitFor(() => Presentation is NotchPresentation.LiveEvent liveEvent4 && liveEvent4.Event is NotchLiveEvent.AgentPeek, 2500);
		Say($"{(flag12 ? "PASS" : "FAIL")} hover opens the notch ({Describe(Presentation)}, tab {presenter?.Tabs.Selected})");
		await Move(cx, m.Bounds.Top + 60, 6);
		await Task.Delay(900);
		await Snap("18-notch-home");
		await OnUi(delegate
		{
			presenter?.Tabs.Select(NotchTab.Chat);
		});
		await Task.Delay(900);
		await Snap("19-notch-chat");
		await Move(cx, cy);
		Say(((await WaitFor(delegate
		{
			NotchPresenter notchPresenter = presenter;
			return notchPresenter != null && !notchPresenter.IsShowingNotchAgentPeek;
		}, 3000)) ? "PASS" : "FAIL") + " the notch folds when the pointer leaves");
		await Task.Delay(800);
		Process fullscreen = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.Form; $f.FormBorderStyle = 'None'; $f.WindowState = 'Maximized'; $f.BackColor = 'DarkSlateGray'; $f.Text = 'Fullscreen test'; $f.ShowDialog()\"")
		{
			UseShellExecute = false,
			CreateNoWindow = true
		});
		bool flag13 = await WaitFor(() => presenter?.IsAmbientSuppressed ?? false, 8000, 200);
		Say($"{(flag13 ? "PASS" : "FAIL")} fullscreen app: ambient states hidden (notch {Describe(Presentation)}, sliver hidden {host.NotchUi.IsAmbientHidden})");
		await Snap("20-fullscreen");
		try
		{
			fullscreen?.Kill(entireProcessTree: true);
		}
		catch
		{
		}
		Say(((await WaitFor(delegate
		{
			NotchPresenter notchPresenter2 = presenter;
			return notchPresenter2 != null && !notchPresenter2.IsAmbientSuppressed;
		}, 4000, 200)) ? "PASS" : "FAIL") + " fullscreen ended: ambient states back (" + Describe(Presentation) + ")");
		await Snap("21-after-fullscreen");
		await OnUi(delegate
		{
			agents.DismissTask(a);
			agents.DismissTask(b);
		});
		void OnSteer(object? s, AgentTaskEvent e)
		{
			if (e.Kind == AgentTaskEventKind.SteerAccepted)
			{
				Interlocked.Increment(ref steers);
			}
		}
	}

	private static async Task Shake(int x, int y)
	{
		for (int i = 0; i < 16; i++)
		{
			SetCursorPos(x + ((i % 2 == 0) ? 70 : (-70)), y + ((i % 3 == 0) ? 6 : (-6)));
			await Task.Delay(18);
		}
		SetCursorPos(x, y);
	}
}
