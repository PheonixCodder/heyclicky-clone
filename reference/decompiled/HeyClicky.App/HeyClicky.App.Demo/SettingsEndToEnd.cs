using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Settings;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Settings;
using HeyClicky.Platform.Audio;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Demo;

public sealed class SettingsEndToEnd(AppHost host, string outDir, Action<string> say)
{
	[CompilerGenerated]
	private sealed class _003CDescendants_003Ed__11 : IEnumerable<DependencyObject>, IEnumerable, IEnumerator<DependencyObject>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DependencyObject _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private DependencyObject root;

		public DependencyObject _003C_003E3__root;

		private int _003Cn_003E5__2;

		private int _003Ci_003E5__3;

		private DependencyObject _003Cchild_003E5__4;

		private IEnumerator<DependencyObject> _003C_003E7__wrap4;

		DependencyObject IEnumerator<DependencyObject>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CDescendants_003Ed__11(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 2)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003Cchild_003E5__4 = null;
			_003C_003E7__wrap4 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003Cn_003E5__2 = VisualTreeHelper.GetChildrenCount(root);
					_003Ci_003E5__3 = 0;
					break;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E7__wrap4 = Descendants(_003Cchild_003E5__4).GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_00c2;
				case 2:
					{
						_003C_003E1__state = -3;
						goto IL_00c2;
					}
					IL_00c2:
					if (_003C_003E7__wrap4.MoveNext())
					{
						DependencyObject current = _003C_003E7__wrap4.Current;
						_003C_003E2__current = current;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap4 = null;
					_003Cchild_003E5__4 = null;
					_003Ci_003E5__3++;
					break;
				}
				if (_003Ci_003E5__3 < _003Cn_003E5__2)
				{
					_003Cchild_003E5__4 = VisualTreeHelper.GetChild(root, _003Ci_003E5__3);
					_003C_003E2__current = _003Cchild_003E5__4;
					_003C_003E1__state = 1;
					return true;
				}
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap4 != null)
			{
				_003C_003E7__wrap4.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<DependencyObject> IEnumerable<DependencyObject>.GetEnumerator()
		{
			_003CDescendants_003Ed__11 _003CDescendants_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CDescendants_003Ed__ = this;
			}
			else
			{
				_003CDescendants_003Ed__ = new _003CDescendants_003Ed__11(0);
			}
			_003CDescendants_003Ed__.root = _003C_003E3__root;
			return _003CDescendants_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DependencyObject>)this).GetEnumerator();
		}
	}

	private int passed;

	private int failed;

	private SettingsContent View => NotchSettingsTab.ShownContent;

	private AppSettings Saved => host.Real.Core.Settings.Current;

	private void Check(string name, bool ok, string detail = "")
	{
		if (ok)
		{
			passed++;
		}
		else
		{
			failed++;
		}
		say($"settings: {(ok ? "PASS" : "FAIL")} {name}{((detail.Length > 0) ? (" (" + detail + ")") : "")}");
	}

	private static Task Wait(int ms)
	{
		return Task.Delay(ms);
	}

	private async Task Page(SettingsWindow.Page page, string shot)
	{
		View.Show(page);
		await Wait(900);
		await Snapshot.SaveAsync(View, "settings-" + shot);
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_SCREENSHOT");
		if (environmentVariable != null && environmentVariable.Length > 0 && File.Exists(environmentVariable))
		{
			string value = Path.Combine(outDir, "desktop-settings-" + shot + ".png");
			using Process process = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{environmentVariable}\" -Path \"{value}\"")
			{
				CreateNoWindow = true,
				UseShellExecute = false
			});
			process?.WaitForExit(15000);
		}
		say($"settings: page {page} shown");
	}

	[IteratorStateMachine(typeof(_003CDescendants_003Ed__11))]
	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDescendants_003Ed__11(-2)
		{
			_003C_003E3__root = root
		};
	}

	private TextBlock? FindText(string text, DependencyObject? within = null)
	{
		return Descendants(within ?? View).OfType<TextBlock>().FirstOrDefault((TextBlock t) => t.Text == text);
	}

	private bool Click(string text, bool trailingControl = false, bool last = false)
	{
		TextBlock textBlock = (last ? Descendants(View).OfType<TextBlock>().LastOrDefault((TextBlock x) => x.Text == text) : FindText(text));
		if ((object)textBlock == null)
		{
			say("settings: no \"" + text + "\" on screen");
			return false;
		}
		DependencyObject dependencyObject = textBlock;
		if (!trailingControl)
		{
			while ((object)dependencyObject != null)
			{
				if (dependencyObject is Pressable pressable)
				{
					pressable.RaiseClick();
					return true;
				}
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}
		}
		dependencyObject = textBlock;
		while ((object)dependencyObject != null)
		{
			if (dependencyObject is Grid grid && grid.ColumnDefinitions.Count >= 2)
			{
				Pressable pressable2 = Descendants(grid).OfType<Pressable>().LastOrDefault();
				if ((object)pressable2 != null)
				{
					pressable2.RaiseClick();
					return true;
				}
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		say("settings: no control beside \"" + text + "\"");
		return false;
	}

	private TextBox? FindTextBox(string placeholder)
	{
		return Descendants(View).OfType<TextBox>().FirstOrDefault((TextBox b) => b.PlaceholderText == placeholder);
	}

	private static void Key(byte vk, bool down)
	{
		Native.keybd_event(vk, 0, (!down) ? 2u : 0u, 0u);
	}

	public async Task<bool> RunAsync()
	{
		RealStack real = host.Real;
		SettingsStore store = real.Core.Settings;
		AppSettings original = store.Current;
		host.OpenSettings(NotchSettingsSection.Account);
		await Wait(1500);
		await Page(SettingsWindow.Page.Account, "account");
		await Wait(1500);
		Check("plan card loaded from /me/plan", (object)host.Billing?.State.Plan != null && (object)FindText(host.Billing.PlanCard.Title) != null, host.Billing?.PlanCard.Title ?? "no billing");
		Check("usage meters", (object)FindText("Talk to HeyClicky") != null && (object)FindText("Agent messages") != null);
		Check("account rows", (object)FindText("Log out") != null && (object)FindText("Delete account") != null && (object)FindText("Quit HeyClicky") != null);
		Click("Delete account");
		await Wait(500);
		Check("delete account: step 1", (object)FindText("Delete your account forever?") != null);
		Click("Too buggy");
		await Wait(200);
		Click("Continue");
		await Wait(500);
		Check("delete account: step 2 (type DELETE)", (object)FindText("One last step.") != null);
		await Snapshot.SaveAsync(View, "settings-account-delete");
		Click("Go back");
		await Wait(300);
		Click("Never mind");
		await Wait(300);
		Check("delete account: cancelled, nothing deleted", (object)FindText("Delete your account forever?") == null && real.Core.Auth.IsSignedIn);
		Check("account: no Invite & Earn row", (object)FindText("Get paid a share of every payment your friends make.") == null);
		await Page(SettingsWindow.Page.Referral, "referral");
		await Page(SettingsWindow.Page.General, "general");
		Click("Show in screen recordings", trailingControl: true);
		await Wait(300);
		Check("show in screen recordings: off → saved + windows excluded", !Saved.ShowInScreenRecordings && !OverlayWindow.ShowInScreenRecordings);
		Click("Show in screen recordings", trailingControl: true);
		await Wait(300);
		Check("show in screen recordings: on again", Saved.ShowInScreenRecordings && OverlayWindow.ShowInScreenRecordings);
		Check("updates row (Velopack updater)", (object)FindText("What's new") != null && Descendants(View).OfType<TextBlock>().Any(delegate(TextBlock t)
		{
			switch (t.Text)
			{
			case "App updates":
			case "Update available":
			case "Update ready to install":
				return true;
			default:
				return false;
			}
		}));
		Click("What's new");
		Click("Request a feature");
		Click("WhatsApp community");
		Click("Reddit");
		Click("Report a bug");
		await Wait(1500);
		string[] files = Directory.GetFiles(AppPaths.Combine("bug-reports"), "*.zip");
		Check("report a bug wrote diagnostics", files.Any((string r) => DateTime.Now - File.GetLastWriteTime(r) < TimeSpan.FromMinutes(1L)), $"{files.Length} report(s)");
		await Page(SettingsWindow.Page.Voice, "voice");
		Click("1.25x");
		await Wait(300);
		Check("speech speed 1.25x saved", Math.Abs(Saved.SpeechSpeed - 1.25) < 0.001);
		Click("Marin");
		await Wait(800);
		Check("voice Marin saved", Saved.VoiceIdentifier == "marin");
		IReadOnlyList<AudioDeviceInfo> source = AudioDevices.List(capture: false);
		AudioDeviceInfo output = source.FirstOrDefault((AudioDeviceInfo d) => !d.IsDefault) ?? source.FirstOrDefault();
		if ((object)output != null)
		{
			Click(output.Name, trailingControl: false, last: true);
			await Wait(600);
			Check("output device picked (" + output.Name + ")", Saved.OutputDeviceId == output.Id && AudioDevices.PreferredRenderId == output.Id);
			Click("Play a test sound");
			await Wait(800);
			Click("System Default");
			await Wait(300);
			Check("output back to System Default", Saved.OutputDeviceId == null && AudioDevices.PreferredRenderId == null);
		}
		else
		{
			say("settings: no output devices listed");
		}
		await Page(SettingsWindow.Page.Microphone, "microphone");
		IReadOnlyList<AudioDeviceInfo> source2 = AudioDevices.List(capture: true);
		say("settings: microphones: " + string.Join(" | ", source2.Select((AudioDeviceInfo m) => m.Name + (m.IsDefault ? " (default)" : ""))));
		AudioDeviceInfo mic = source2.FirstOrDefault((AudioDeviceInfo d) => !d.IsDefault) ?? source2.FirstOrDefault();
		if ((object)mic != null)
		{
			Click(mic.Name, trailingControl: false, last: true);
			await Wait(800);
			Check("microphone picked (" + mic.Name + ")", Saved.MicrophoneDeviceId == mic.Id && AudioDevices.PreferredCaptureId == mic.Id);
		}
		Click("Test microphone");
		await Wait(2600);
		string text = (from t in Descendants(View).OfType<TextBlock>()
			select t.Text).FirstOrDefault(delegate(string t)
		{
			switch (t)
			{
			case "Receiving audio":
			case "HeyClicky can't hear you. Try different microphone.":
			case "Listening...":
				return true;
			default:
				return false;
			}
		});
		Check("microphone test runs", text != null, text ?? "no status");
		await Snapshot.SaveAsync(View, "settings-microphone-test");
		Click("Stop");
		await Wait(300);
		Click("System Default");
		await Wait(500);
		Check("microphone back to System Default", Saved.MicrophoneDeviceId == null);
		await Page(SettingsWindow.Page.Dictation, "dictation");
		Click("Auto-detect language", trailingControl: true);
		await Wait(300);
		Check("auto-detect off → the pinned language", Saved.DictationLanguageCode == Saved.DictationSpecificLanguageCode && Saved.DictationLanguageCode != "multi");
		Click("Spanish");
		await Wait(300);
		Check("pinned Spanish → language code es", Saved.DictationLanguageCode == "es" && Saved.DictationSpecificLanguageCode == "es");
		TextBox search = FindTextBox("Search languages");
		if ((object)search != null)
		{
			search.Text = "port";
			await Wait(300);
			Check("language search filters", (object)FindText("Portuguese") != null && (object)FindText("Spanish") == null);
			search.Text = "";
		}
		Click("Auto-detect language", trailingControl: true);
		await Wait(300);
		Check("auto-detect on → multi, pin kept", Saved.DictationLanguageCode == "multi" && Saved.DictationSpecificLanguageCode == "es");
		TextBox textBox = FindTextBox("Add a word");
		if ((object)textBox != null)
		{
			textBox.Text = "Farza";
			await Wait(100);
			Click("Add");
			await Wait(400);
		}
		Check("dictionary word added", Saved.DictationCustomWords.Contains("Farza"));
		if (FindText("Farza")?.Parent is StackPanel stackPanel)
		{
			Pressable pressable = stackPanel.Children.OfType<Pressable>().FirstOrDefault();
			if ((object)pressable != null)
			{
				pressable.RaiseClick();
				await Wait(300);
			}
		}
		Check("dictionary word removed", !Saved.DictationCustomWords.Contains("Farza"));
		await Page(SettingsWindow.Page.Shortcuts, "shortcuts");
		List<ShortcutAction> fired;
		if (OverlayWindow.Offscreen || Environment.GetEnvironmentVariable("HEYCLICKY_E2E_KEYS") != "1")
		{
			say("settings: offscreen run: shortcut recording (real key presses) skipped");
		}
		else
		{
			Click("Change");
			await Wait(400);
			Check("recorder armed (monitor recording)", real.Shortcuts.IsRecording);
			Key(162, down: true);
			await Wait(80);
			Key(160, down: true);
			await Wait(600);
			Key(160, down: false);
			await Wait(60);
			Key(162, down: false);
			await Wait(500);
			await Snapshot.SaveAsync(View, "settings-shortcuts-recorded");
			Click("Save");
			await Wait(500);
			Check("push to talk rebound to Ctrl+Shift (settings.json + monitor)", Saved.EffectiveHotkeys.PushToTalk.Modifiers == (HotkeyModifiers.Control | HotkeyModifiers.Shift) && real.Shortcuts.Bindings.PushToTalk.Modifiers == (HotkeyModifiers.Control | HotkeyModifiers.Shift) && !real.Shortcuts.IsRecording, real.Shortcuts.Bindings.PushToTalk.DisplayText);
			fired = new List<ShortcutAction>();
			real.Shortcuts.ShortcutTriggered += OnShortcut;
			host.Hotkeys.AllowsPushToTalk = () => false;
			Key(162, down: true);
			await Wait(60);
			Key(164, down: true);
			await Wait(300);
			Key(164, down: false);
			await Wait(60);
			Key(162, down: false);
			await Wait(300);
			bool oldComboSilent;
			lock (fired)
			{
				oldComboSilent = !fired.Contains(ShortcutAction.PushToTalkDown);
			}
			Key(162, down: true);
			await Wait(60);
			Key(160, down: true);
			await Wait(300);
			Key(160, down: false);
			await Wait(60);
			Key(162, down: false);
			await Wait(300);
			bool flag;
			lock (fired)
			{
				flag = fired.Contains(ShortcutAction.PushToTalkDown) && fired.Contains(ShortcutAction.PushToTalkUp);
			}
			real.Shortcuts.ShortcutTriggered -= OnShortcut;
			host.Hotkeys.AllowsPushToTalk = () => real.Tutorial.AllowsPushToTalk;
			Check("old Ctrl+Alt no longer talks; new Ctrl+Shift does", oldComboSilent && flag, string.Join(",", fired));
			List<Pressable> list = (from t in Descendants(View).OfType<TextBlock>()
				where t.Text == "Change"
				select t).Select(FindAncestorPressable).OfType<Pressable>().Distinct()
				.ToList();
			if (list.Count > 1)
			{
				list[1].RaiseClick();
				await Wait(300);
			}
			Key(162, down: true);
			await Wait(60);
			Key(160, down: true);
			await Wait(600);
			Key(160, down: false);
			await Wait(60);
			Key(162, down: false);
			await Wait(400);
			Click("Save");
			await Wait(400);
			Check("conflicting combo refused", (object)FindText("Already used by Push to talk. Pick a different combo.") != null && Saved.EffectiveHotkeys.TextMode == HotkeyBinding.TextModeDefault);
			await Snapshot.SaveAsync(View, "settings-shortcuts-conflict");
			Click("Cancel");
			await Wait(300);
			Click("Reset to defaults");
			await Wait(400);
			Check("reset to defaults", (object)Saved.Hotkeys == null && real.Shortcuts.Bindings == HotkeyBindings.Defaults);
		}
		await Page(SettingsWindow.Page.Cursor, "cursor");
		Click("Hide floating cursor", trailingControl: true);
		await Wait(3200);
		Check("hide floating cursor toggles it", Saved.CursorDocked != original.CursorDocked);
		Click("Hide floating cursor", trailingControl: true);
		await Wait(3200);
		Check("hide floating cursor back as it was", Saved.CursorDocked == original.CursorDocked);
		Click("Screen highlight", trailingControl: true);
		await Wait(200);
		Check("screen highlight off → overlay", !Saved.ScreenHighlightEnabled && !real.Highlight.IsEnabled);
		Click("Screen highlight", trailingControl: true);
		await Wait(200);
		await Page(SettingsWindow.Page.Agents, "agents");
		Click("Auto-approve extra usage", trailingControl: true);
		await Wait(200);
		Check("auto-approve extra usage → AgentService", Saved.AgentAutoApproveExtraUsage && real.Agents.AutoApproveExtraUsage);
		Click("Auto-approve extra usage", trailingControl: true);
		await Wait(200);
		Check("auto-approve off again", !Saved.AgentAutoApproveExtraUsage && !real.Agents.AutoApproveExtraUsage);
		Click("Speak when a job starts or finishes", trailingControl: true);
		await Wait(200);
		Check("voice announcements off saved", !Saved.AgentAnnouncementsVoiceEnabled);
		Click("Speak when a job starts or finishes", trailingControl: true);
		await Wait(200);
		string folder = Path.Combine(Path.GetTempPath(), "heyclicky-e2e-agent-folder");
		store.Update(delegate(AppSettings s)
		{
			s.AgentFolderPath = folder;
		});
		await Wait(500);
		Check("agent folder → AgentService.ProjectsRoot", real.Agents.ProjectsRoot == folder && (object)FindText("Reset to default folder") != null);
		Click("Reset to default folder");
		await Wait(500);
		Check("agent folder reset to the default", Saved.AgentFolderPath == original.AgentFolderPath || Saved.AgentFolderPath == null);
		if (original.AgentFolderPath != null)
		{
			store.Update(delegate(AppSettings s)
			{
				s.AgentFolderPath = original.AgentFolderPath;
			});
		}
		await Page(SettingsWindow.Page.Integrations, "integrations");
		await Wait(4000);
		await Snapshot.SaveAsync(View, "settings-integrations");
		Check("integrations catalog listed", (object)FindText("AVAILABLE INTEGRATIONS") != null || (object)FindText("CONNECTED") != null, ((object)FindText("Integrations aren't set up on this server.") != null) ? "Composio not configured on this Worker" : "");
		Click("Add custom connector");
		await Wait(400);
		TextBox textBox2 = FindTextBox("Like Stripe or Meta Ads");
		if ((object)textBox2 != null)
		{
			TextBox textBox3 = FindTextBox("https://mcp.example.com/mcp");
			if ((object)textBox3 != null)
			{
				textBox2.Text = "E2E Test MCP";
				textBox3.Text = "https://mcp.example.com/mcp";
				await Wait(100);
				Click("Save");
				await Wait(800);
				Check("custom connector saved → AgentService", CustomConnectorStore.Load().Any((CustomConnector c) => c.Name == "E2E Test MCP"));
				await Snapshot.SaveAsync(View, "settings-integrations-custom");
				CustomConnectorStore.Remove(CustomConnectorStore.Load().First((CustomConnector c) => c.Name == "E2E Test MCP").Id);
				await Wait(300);
				goto IL_30e7;
			}
		}
		Check("custom connector editor opens", ok: false);
		goto IL_30e7;
		IL_30e7:
		if (View.Host.ShowsDeveloperSection)
		{
			await Page(SettingsWindow.Page.Developer, "developer");
			Click("Drawing sounds", trailingControl: true);
			await Wait(200);
			Check("drawing sounds off → canvas", !Saved.DrawingSoundsEnabled && !real.Canvas.PlaysSounds);
			Click("Drawing sounds", trailingControl: true);
			await Wait(200);
			Check("drawing sounds on → canvas", Saved.DrawingSoundsEnabled && real.Canvas.PlaysSounds);
			Click("Pen sounds", trailingControl: true);
			await Wait(200);
			Check("pen sounds → canvas", Saved.DrawingPenSoundsEnabled && real.Canvas.PlaysPenSounds);
			Click("Pen sounds", trailingControl: true);
			await Wait(200);
			PlanFixtures.Apply(host.Billing, "Pro cancelling");
			await Wait(300);
			await Page(SettingsWindow.Page.Account, "account-forced-pro-cancelling");
			Check("forced plan state shows Pro + cancellation", (object)FindText("You're on HeyClicky Pro.") != null && Descendants(View).OfType<TextBlock>().Any((TextBlock t) => t.Text.StartsWith("Cancels in")));
			PlanFixtures.Apply(host.Billing, null);
		}
		store.Update(delegate(AppSettings s)
		{
			s.SpeechSpeed = original.SpeechSpeed;
			s.VoiceIdentifier = original.VoiceIdentifier;
			s.AgentAnnouncementsVoiceEnabled = original.AgentAnnouncementsVoiceEnabled;
		});
		View.Show(SettingsWindow.Page.Account);
		(host.Services.Presenter as NotchPresenter)?.DismissNotchAgentPeek();
		say($"settings: {passed} passed, {failed} failed");
		return failed == 0;
		void OnShortcut(object? _, ShortcutAction a)
		{
			lock (fired)
			{
				fired.Add(a);
			}
		}
	}

	private static Pressable? FindAncestorPressable(DependencyObject node)
	{
		DependencyObject dependencyObject = node;
		while ((object)dependencyObject != null)
		{
			if (dependencyObject is Pressable result)
			{
				return result;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return null;
	}
}
