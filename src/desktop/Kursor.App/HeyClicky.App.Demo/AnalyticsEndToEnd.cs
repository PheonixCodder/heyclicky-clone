using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Billing;
using HeyClicky.App.Design;
using HeyClicky.App.Onboarding;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Demo;

public sealed class AnalyticsEndToEnd(AppHost host, string outDir, Action<string> say)
{
	private sealed class ObStubPermissions : IPermissionService
	{
		private readonly Dictionary<PermissionKind, PermissionStatus> status = new Dictionary<PermissionKind, PermissionStatus>
		{
			[PermissionKind.Microphone] = PermissionStatus.NotDetermined,
			[PermissionKind.ScreenCapture] = PermissionStatus.NotDetermined
		};

		public PermissionStatus Next { get; set; } = PermissionStatus.Granted;

		public event EventHandler? Changed;

		public PermissionStatus GetStatus(PermissionKind kind)
		{
			return status[kind];
		}

		public Task<PermissionStatus> RequestAsync(PermissionKind kind)
		{
			Set(kind, Next);
			return Task.FromResult(status[kind]);
		}

		public void OpenSystemSettings(PermissionKind kind)
		{
		}

		public void StartPolling()
		{
		}

		public void StopPolling()
		{
		}

		public void Set(PermissionKind kind, PermissionStatus value)
		{
			status[kind] = value;
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	[CompilerGenerated]
	private sealed class _003CDescendants_003Ed__0 : IEnumerable<DependencyObject>, IEnumerable, IEnumerator<DependencyObject>, IEnumerator, IDisposable
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
		public _003CDescendants_003Ed__0(int _003C_003E1__state)
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
			_003CDescendants_003Ed__0 _003CDescendants_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CDescendants_003Ed__ = this;
			}
			else
			{
				_003CDescendants_003Ed__ = new _003CDescendants_003Ed__0(0);
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

	private DateTimeOffset mark = DateTimeOffset.MinValue;

	private LogAnalyticsSink? Sink => ClickyAnalytics.Sink as LogAnalyticsSink;

	[IteratorStateMachine(typeof(_003CDescendants_003Ed__0))]
	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDescendants_003Ed__0(-2)
		{
			_003C_003E3__root = root
		};
	}

	private bool ClickText(DependencyObject root, string text, bool trailingControl = false)
	{
		TextBlock textBlock = Descendants(root).OfType<TextBlock>().FirstOrDefault((TextBlock x) => x.Text == text);
		if ((object)textBlock == null)
		{
			say("analytics: no \"" + text + "\" on screen");
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
		return false;
	}

	private async Task CoreAsync()
	{
		_ = host.Real;
		NotchPresenter presenter = host.Services.Presenter as NotchPresenter;
		Mark();
		await OnUi(delegate
		{
			presenter?.SetOnboardingPin(0);
		});
		await Wait(700);
		await OnUi(delegate
		{
			presenter?.SetOnboardingPin(null);
		});
		await Expect("notch", new global::_003C_003Ez__ReadOnlyArray<(string, string[])>(new(string, string[])[2]
		{
			("notch_expanded", new string[1] { "trigger" }),
			("notch_collapsed", new string[1] { "dwell_ms" })
		}));
		Mark();
		await OnUi(delegate
		{
			host.OpenNotchHome();
		});
		await Wait(1200);
		await OnUi(delegate
		{
			presenter?.Tabs.Select(NotchTab.Chat);
		});
		await Wait(600);
		await Expect("notch tabs", new global::_003C_003Ez__ReadOnlySingleElementList<(string, string[])>(("notch_peek_action", new string[1] { "action" })));
		await OnUi(delegate
		{
			presenter?.DismissNotchAgentPeek();
		});
		await Wait(500);
		string dropped = Path.Combine(outDir, "analytics-drop.txt");
		File.WriteAllText(dropped, "dropped on the notch");
		Mark();
		await OnUi(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.TextInput(""));
		});
		await Wait(800);
		await OnUi(delegate
		{
			host.NotchUi.ActiveSurfaceForTest?.AcceptFiles(new global::_003C_003Ez__ReadOnlySingleElementList<string>(dropped));
		});
		await Expect("notch drop", "notch_dropped_files_attached", "file_count");
		await OnUi(delegate
		{
			host.Services.Presenter.Present(new NotchPresentation.Idle());
		});
		await Wait(500);
		Mark();
		await OnUi(delegate
		{
			host.OpenSettings(NotchSettingsSection.General);
		});
		await Wait(1500);
		await OnUi(delegate
		{
			ClickText(host.NotchUi.Window.Root, "Show in screen recordings", trailingControl: true);
		});
		await Wait(400);
		await OnUi(delegate
		{
			ClickText(host.NotchUi.Window.Root, "Show in screen recordings", trailingControl: true);
		});
		await Wait(400);
		await OnUi(delegate
		{
			host.OpenSettings(NotchSettingsSection.Referral);
		});
		await Wait(1200);
		await Expect("settings", new global::_003C_003Ez__ReadOnlyArray<(string, string[])>(new(string, string[])[2]
		{
			("setting_changed", new string[4] { "setting", "value", "surface", "product_event_version" }),
			("referral_page_opened", new string[1] { "entry_point" })
		}));
		await OnUi(delegate
		{
			presenter?.DismissNotchAgentPeek();
		});
		await Wait(800);
		int before = MainThreadStallMonitor.StallCount;
		await OnUi(delegate
		{
			Thread.Sleep(2500);
		});
		await Wait(400);
		bool flag = MainThreadStallMonitor.StallCount > before && MainThreadStallMonitor.LongestStallMs >= 2000;
		if (flag)
		{
			passed++;
		}
		else
		{
			failed++;
		}
		say($"analytics: {(flag ? "PASS" : "FAIL")} stall watchdog: {MainThreadStallMonitor.StallCount - before} stall(s), longest {MainThreadStallMonitor.LongestStallMs} ms");
	}

	private static Task Wait(int ms)
	{
		return Task.Delay(ms);
	}

	private Task OnUi(Action action)
	{
		TaskCompletionSource done = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(delegate
		{
			try
			{
				action();
				done.TrySetResult();
			}
			catch (Exception exception)
			{
				done.TrySetException(exception);
			}
		});
		return done.Task;
	}

	private IReadOnlyList<AnalyticsEvent> Since()
	{
		return Sink?.Recent.Where((AnalyticsEvent e) => e.Timestamp >= mark).ToList() ?? new List<AnalyticsEvent>();
	}

	private void Mark()
	{
		mark = DateTimeOffset.UtcNow;
	}

	private async Task Expect(string label, IEnumerable<(string Event, string[] Keys)> expected, int waitMs = 1500)
	{
		List<(string Event, string[] Keys)> list = expected.ToList();
		DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
		IReadOnlyList<AnalyticsEvent> events;
		while (true)
		{
			events = Since();
			if (list.Where(((string Event, string[] Keys) e) => !events.Any((AnalyticsEvent x) => x.Event == e.Event && e.Keys.All((string k) => x.Properties.ContainsKey(k)))).ToList().Count == 0 || DateTime.UtcNow >= deadline)
			{
				break;
			}
			await Wait(100);
		}
		foreach (var e2 in list)
		{
			AnalyticsEvent hit = events.LastOrDefault((AnalyticsEvent x) => x.Event == e2.Event);
			bool flag = (object)hit != null && e2.Keys.All((string k) => hit.Properties.ContainsKey(k));
			if (flag)
			{
				passed++;
			}
			else
			{
				failed++;
			}
			string value = (((object)hit == null) ? "" : (" " + string.Join(", ", e2.Keys.Select((string k) => k + "=" + hit.Properties[k]?.ToJsonString()))));
			say($"analytics: {(flag ? "PASS" : "FAIL")} {label}: {e2.Event}{value}{(((object)hit == null) ? " (not fired)" : ((!flag) ? " (missing properties)" : ""))}");
		}
	}

	private Task Expect(string label, params string[] events)
	{
		return Expect(label, events.Select((string e) => (e: e, Array.Empty<string>())));
	}

	private Task Expect(string label, string evt, params string[] keys)
	{
		return Expect(label, new global::_003C_003Ez__ReadOnlySingleElementList<(string, string[])>((evt, keys)));
	}

	public async Task RunAsync()
	{
		if (Sink == null)
		{
			say("analytics: FAIL the debug sink is off (set HEYCLICKY_ANALYTICS_DEBUG=1)");
			return;
		}
		string only = Environment.GetEnvironmentVariable("HEYCLICKY_E2E_ONLY");
		if ((only == null || only == "core") ? true : false)
		{
			await RunCoreAsync();
		}
		if ((only == null || only == "onboarding") ? true : false)
		{
			await RunOnboardingAsync();
		}
		say($"analytics: {passed} passed, {failed} failed");
	}

	private void RunCore(ref Task task)
	{
		task = CoreAsync();
	}

	private async Task RunCoreAsync()
	{
		Task task = Task.CompletedTask;
		RunCore(ref task);
		await task;
	}

	private void RunOnboarding(ref Task task)
	{
		task = OnboardingAsync();
	}

	private async Task RunOnboardingAsync()
	{
		Task task = Task.CompletedTask;
		RunOnboarding(ref task);
		await task;
	}

	private static (string Event, string[] Keys) ObE(string evt, params string[] keys)
	{
		return (Event: evt, Keys: keys);
	}

	private async Task ObExpectValue(string label, string evt, string key, string value, int waitMs = 1500)
	{
		DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
		bool flag;
		while (true)
		{
			flag = Since().Any(delegate(AnalyticsEvent x)
			{
				if (x.Event == evt)
				{
					JsonNode jsonNode = x.Properties[key];
					if (jsonNode != null)
					{
						return jsonNode.ToJsonString().Trim('"') == value;
					}
				}
				return false;
			});
			if (flag || DateTime.UtcNow >= deadline)
			{
				break;
			}
			await Wait(100);
		}
		if (flag)
		{
			passed++;
		}
		else
		{
			failed++;
		}
		say($"analytics: {(flag ? "PASS" : "FAIL")} {label}: {evt} {key}={value}{(flag ? "" : " (not seen)")}");
	}

	private Task ObOnUiAsync(Func<Task> action)
	{
		TaskCompletionSource done = new TaskCompletionSource();
		host.DispatcherQueue.TryEnqueue(async delegate
		{
			try
			{
				await action();
				done.TrySetResult();
			}
			catch (Exception exception)
			{
				done.TrySetException(exception);
			}
		});
		return done.Task;
	}

	private async Task OnboardingAsync()
	{
		RealStack real = host.Real;
		if (real == null)
		{
			failed++;
			say("analytics: FAIL onboarding: needs the real stack");
			return;
		}
		SettingsStore store = real.Core.Settings;
		(bool PreSignInIntroDismissed, bool HasAcknowledgedPermissionIntro, bool HasCompletedOnboarding, double? TutorialCompletedAtEpochSeconds, string DiscoveryChannel, string DiscoveryChannelOther, bool DiscoveryChannelSynced, string SquadPackId, string SquadMemberId, string VoiceIdentifier, string CursorColorHex) saved = store.Read((AppSettings s) => (PreSignInIntroDismissed: s.Onboarding.PreSignInIntroDismissed, HasAcknowledgedPermissionIntro: s.Onboarding.HasAcknowledgedPermissionIntro, HasCompletedOnboarding: s.Onboarding.HasCompletedOnboarding, TutorialCompletedAtEpochSeconds: s.Onboarding.TutorialCompletedAtEpochSeconds, DiscoveryChannel: s.Onboarding.DiscoveryChannel, DiscoveryChannelOther: s.Onboarding.DiscoveryChannelOther, DiscoveryChannelSynced: s.Onboarding.DiscoveryChannelSynced, SquadPackId: s.Onboarding.SquadPackId, SquadMemberId: s.SquadMemberId, VoiceIdentifier: s.VoiceIdentifier, CursorColorHex: s.CursorColorHex));
		string noCues = Environment.GetEnvironmentVariable("HEYCLICKY_NO_VIDEO_CUES");
		try
		{
			_ = 6;
			try
			{
				mark = DateTimeOffset.MinValue;
				await Expect("launch", new(string, string[])[1] { ObE("notch_first_shown", "entry_context") }, 0);
				await ObIntroAsync(saved.SquadPackId, saved.CursorColorHex);
				await ObPermissionsAsync(store);
				await ObVideoAndInviteAsync(store);
				await ObTutorialAsync();
				await ObInterviewAsync();
				await ObRevealAsync();
			}
			catch (Exception ex)
			{
				failed++;
				say("analytics: FAIL onboarding: " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		finally
		{
			Environment.SetEnvironmentVariable("HEYCLICKY_NO_VIDEO_CUES", noCues);
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.PreSignInIntroDismissed = saved.PreSignInIntroDismissed;
				s.Onboarding.HasAcknowledgedPermissionIntro = saved.HasAcknowledgedPermissionIntro;
				s.Onboarding.HasCompletedOnboarding = saved.HasCompletedOnboarding;
				s.Onboarding.TutorialCompletedAtEpochSeconds = saved.TutorialCompletedAtEpochSeconds;
				s.Onboarding.DiscoveryChannel = saved.DiscoveryChannel;
				s.Onboarding.DiscoveryChannelOther = saved.DiscoveryChannelOther;
				s.Onboarding.DiscoveryChannelSynced = saved.DiscoveryChannelSynced;
				s.Onboarding.SquadPackId = saved.SquadPackId;
				s.SquadMemberId = saved.SquadMemberId;
				s.VoiceIdentifier = saved.VoiceIdentifier;
				s.CursorColorHex = saved.CursorColorHex;
			});
			string item = saved.DiscoveryChannel;
			if (item != null)
			{
				try
				{
					await real.Core.Worker.SetDiscoveryChannelAsync(item, saved.DiscoveryChannelOther);
				}
				catch (Exception ex2)
				{
					say("analytics: onboarding: discovery channel not restored: " + ex2.Message);
				}
			}
			await OnUi(delegate
			{
				if (real.Tutorial.Phase == OnboardingTutorialPhase.InvitePresented)
				{
					real.Tutorial.BypassInvite();
				}
				BillingUi billing = host.Billing;
				if (billing != null && billing.IsPaywallVisible)
				{
					billing.DismissPaywall();
				}
				host.HideAuxWindows();
				NotchOnboardingCardViewModel cardVm = host.CardVm;
				if (cardVm != null)
				{
					cardVm.IsIntroHomeWelcomeActive = false;
					cardVm.Refresh();
				}
			});
		}
	}

	private async Task ObIntroAsync(string? savedSquad, string savedColorHex)
	{
		SettingsStore settingsStore = host.Real.Core.Settings;
		bool hadLanguageStep = settingsStore.Read((AppSettings x) => x.OnboardingLanguageStepEnabled);
		settingsStore.Update(delegate(AppSettings x)
		{
			x.OnboardingLanguageStepEnabled = true;
		});
		try
		{
			await ObIntroStepsAsync(savedSquad, savedColorHex);
		}
		finally
		{
			settingsStore.Update(delegate(AppSettings x)
			{
				x.OnboardingLanguageStepEnabled = hadLanguageStep;
			});
		}
	}

	private async Task ObIntroStepsAsync(string? savedSquad, string savedColorHex)
	{
		IntroWindow intro = null;
		Mark();
		await OnUi(delegate
		{
			intro = host.DemoIntro(IntroWindow.Step.WhatIsClicky);
		});
		await Expect("intro open", new(string, string[])[1] { ObE("intro_viewed", "step") });
		await ObExpectValue("intro open", "intro_viewed", "step", "what_is_clicky");
		IntroWindow w = intro;
		Mark();
		await OnUi(w.ContinueFromWelcome);
		await Expect("intro continue", new(string, string[])[2]
		{
			ObE("intro_button_tapped", "step", "action"),
			ObE("intro_viewed", "step")
		});
		await ObExpectValue("intro continue", "intro_button_tapped", "action", "continue");
		await ObExpectValue("intro continue", "intro_viewed", "step", "where_heard_about");
		Mark();
		await OnUi(delegate
		{
			w.PickChannel("Google");
		});
		await Expect("intro channel", new(string, string[])[2]
		{
			ObE("intro_button_tapped", "step", "action", "value"),
			ObE("discovery_channel_selected", "channel")
		});
		await ObExpectValue("intro channel", "intro_button_tapped", "value", "google");
		await ObExpectValue("intro channel", "intro_viewed", "step", "pick_language");
		Mark();
		await OnUi(delegate
		{
			w.PickLanguage(null);
			w.ContinueLanguage();
		});
		await ObExpectValue("intro language", "intro_button_tapped", "value", "automatic");
		await ObExpectValue("intro language", "intro_viewed", "step", "open_clicky");
		(string, string) tuple = host.Real.Core.Settings.Read((AppSettings s) => (SquadPackId: s.Onboarding.SquadPackId, VoiceIdentifier: s.VoiceIdentifier));
		if (tuple.Item1 == "classicClouds" && tuple.Item2 == "cedar")
		{
			passed++;
			say("analytics: PASS intro look: classicClouds with cedar");
		}
		else
		{
			failed++;
			say("analytics: FAIL intro look: " + tuple.Item1 + " with " + tuple.Item2);
		}
		await OnUi(delegate
		{
			w.Go(IntroWindow.Step.WhereHeardAbout);
		});
		Mark();
		await OnUi(delegate
		{
			w.PickChannel("Other");
			w.CommitOther();
		});
		await Expect("intro other", new(string, string[])[2]
		{
			ObE("intro_button_tapped", "step", "action", "value"),
			ObE("discovery_channel_selected", "channel")
		});
		await ObExpectValue("intro other", "intro_button_tapped", "action", "submit_discovery_channel_other");
		await ObExpectValue("intro other", "intro_button_tapped", "value", "skipped");
		await ObExpectValue("intro other", "discovery_channel_selected", "channel", "other");
		await ObExpectValue("intro other", "intro_viewed", "step", "pick_language");
		await OnUi(w.ContinueLanguage);
		CursorColorOption option = OnboardingCatalog.CursorColors.FirstOrDefault((CursorColorOption c) => string.Equals(c.Hex, savedColorHex, StringComparison.OrdinalIgnoreCase)) ?? OnboardingCatalog.CursorColors[1];
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(option.Id[0]));
		string id = option.Id;
		string swatch = string.Concat(readOnlySpan, id.Substring(1, id.Length - 1));
		Mark();
		await OnUi(delegate
		{
			w.PickColor(swatch, DS.CursorColor(option.Hex));
		});
		await ObExpectValue("intro color", "intro_button_tapped", "value", option.Id);
		await ObExpectValue("intro color", "intro_viewed", "step", "hatched");
		await Wait(600);
		Mark();
		await OnUi(w.GiveHome);
		await ObExpectValue("intro home", "intro_button_tapped", "action", "give_clicky_a_home");
		await Expect("intro home", new(string, string[])[1] { ObE("intro_notch_welcome_shown") }, 12000);
	}

	private async Task ObPermissionsAsync(SettingsStore store)
	{
		store.Update(delegate(AppSettings s)
		{
			s.Onboarding.HasCompletedOnboarding = false;
			s.Onboarding.HasAcknowledgedPermissionIntro = false;
			s.Onboarding.PreSignInIntroDismissed = false;
		});
		ObStubPermissions stub = new ObStubPermissions();
		NotchOnboardingCardViewModel vm = null;
		await OnUi(delegate
		{
			vm = new NotchOnboardingCardViewModel(store, stub)
			{
				IsSignedIn = true,
				GrantedSuccessHold = TimeSpan.Zero
			};
		});
		NotchOnboardingCardViewModel card = vm;
		Mark();
		await OnUi(delegate
		{
			card.IsIntroHomeWelcomeActive = true;
		});
		await ObOnUiAsync(card.PrimaryActionAsync);
		await Expect("card welcome", new(string, string[])[5]
		{
			ObE("intro_notch_welcome_shown"),
			ObE("intro_notch_final_setup_tapped"),
			ObE("pre_sign_in_intro_dismissed"),
			ObE("permission_flow_shown", "entry_context", "required_permission_count"),
			ObE("permission_step_shown", "permission", "permission_step_index")
		});
		await ObExpectValue("card welcome", "permission_flow_shown", "entry_context", "firstRun");
		await ObExpectValue("card welcome", "permission_step_shown", "permission", "microphone");
		Mark();
		stub.Next = PermissionStatus.Denied;
		await ObOnUiAsync(card.PrimaryActionAsync);
		await ObOnUiAsync(card.PrimaryActionAsync);
		await Expect("card microphone", new(string, string[])[2]
		{
			ObE("permission_denied", "permission"),
			ObE("permission_settings_opened", "permission")
		});
		Mark();
		await OnUi(delegate
		{
			stub.Set(PermissionKind.Microphone, PermissionStatus.Granted);
		});
		stub.Next = PermissionStatus.WaitingInSettings;
		await ObOnUiAsync(card.PrimaryActionAsync);
		await OnUi(delegate
		{
			stub.Set(PermissionKind.ScreenCapture, PermissionStatus.Granted);
		});
		await Expect("card granted", new(string, string[])[5]
		{
			ObE("permission_granted", "permission"),
			ObE("permission_settings_opened", "permission"),
			ObE("core_permissions_granted"),
			ObE("all_permissions_granted"),
			ObE("permission_flow_completed", "entry_context")
		});
		await ObExpectValue("card granted", "permission_step_shown", "permission", "screen_recording");
		await ObExpectValue("card granted", "permission_granted", "permission", "microphone");
		await ObExpectValue("card granted", "permission_granted", "permission", "screen_recording");
		Mark();
		await ObOnUiAsync(card.PrimaryActionAsync);
		await Expect("card meet", new(string, string[])[1] { ObE("onboarding_started") });
	}

	private async Task ObVideoAndInviteAsync(SettingsStore store)
	{
		store.Update(delegate(AppSettings s)
		{
			s.Onboarding.TutorialCompletedAtEpochSeconds = null;
		});
		Environment.SetEnvironmentVariable("HEYCLICKY_NO_VIDEO_CUES", "1");
		CursorCompanion companion = host.CursorCompanion;
		Mark();
		await OnUi(delegate
		{
			host.DemoIntroSequence(isReplay: true);
		});
		await Expect("video", new(string, string[])[1] { ObE("onboarding_video_started", "video_kind", "phase", "asset_url", "player_status", "item_status", "time_control_status", "source") }, 30000);
		if (!companion.IsPlayingVideo)
		{
			failed++;
			say("analytics: FAIL video: no video playing (missing asset?)");
			return;
		}
		Mark();
		await OnUi(companion.OnEscapePressed);
		await ObExpectValue("video escape", "onboarding_video_escape_pressed", "press_type", "armed_skip");
		await OnUi(companion.OnEscapePressed);
		await ObExpectValue("video escape", "onboarding_video_escape_pressed", "press_type", "confirmed_skip");
		await Expect("video escape", new(string, string[])[2]
		{
			ObE("onboarding_video_escape_pressed", "press_type", "seconds_remaining"),
			ObE("onboarding_skipped_by_shake")
		});
		await Expect("invite", new(string, string[])[1] { ObE("onboarding_tutorial_invite_presented") }, 8000);
		Mark();
		host.Hotkeys?.AllowsTextMode();
		await ObExpectValue("invite bypass", "onboarding_tutorial_resolved", "outcome", "invite_bypassed_by_text");
		await Wait(500);
		Mark();
		await OnUi(delegate
		{
			host.DemoIntroSequence(isReplay: true);
		});
		await Expect("video end", new(string, string[])[1] { ObE("onboarding_video_started", "video_kind") }, 30000);
		Mark();
		await OnUi(delegate
		{
			companion.SeekNearEnd(1.0);
		});
		await Expect("video end", new(string, string[])[1] { ObE("onboarding_video_completed") }, 8000);
		await Expect("invite", new(string, string[])[1] { ObE("onboarding_tutorial_invite_presented") }, 8000);
		Mark();
		host.Hotkeys?.AllowsPushToTalk();
		await ObExpectValue("invite bypass", "onboarding_tutorial_resolved", "outcome", "invite_bypassed_by_ptt");
	}

	private async Task ObTutorialAsync()
	{
		Mark();
		await OnUi(host.StartTutorial);
		await Expect("tour start", new(string, string[])[2]
		{
			ObE("onboarding_tutorial_started"),
			ObE("onboarding_tutorial_step_viewed", "step", "navigation")
		});
		await ObExpectValue("tour start", "onboarding_tutorial_step_viewed", "navigation", "start");
		TutorialWindow tour = host.DemoTutorialWindow();
		if (tour == null)
		{
			failed++;
			say("analytics: FAIL tour: no tour window");
			return;
		}
		Mark();
		TutorialWindow.Step reached = TutorialWindow.Step.MicCheck;
		await OnUi(delegate
		{
			tour.MarkCompleted(TutorialWindow.Step.MicCheck);
			tour.Advance();
			reached = tour.CurrentStep;
		});
		await OnUi(tour.ToggleMusic);
		await OnUi(tour.Back);
		await OnUi(delegate
		{
			tour.Go(reached);
		});
		await Expect("tour steps", new(string, string[])[3]
		{
			ObE("onboarding_tutorial_step_completed", "step"),
			ObE("onboarding_tutorial_step_viewed", "step", "navigation"),
			ObE("onboarding_tutorial_music_toggled", "muted")
		});
		await ObExpectValue("tour steps", "onboarding_tutorial_step_completed", "step", "mic_check");
		await ObExpectValue("tour steps", "onboarding_tutorial_step_viewed", "navigation", "advance");
		await ObExpectValue("tour steps", "onboarding_tutorial_step_viewed", "navigation", "back");
		await ObExpectValue("tour steps", "onboarding_tutorial_step_viewed", "navigation", "jump");
		Mark();
		await OnUi(host.ResumeTutorial);
		await Expect("tour resume", new(string, string[])[1] { ObE("onboarding_tutorial_resumed_from_notch") });
		Mark();
		await OnUi(delegate
		{
			tour.End("skipped");
		});
		await Expect("tour skip", new(string, string[])[1] { ObE("onboarding_tutorial_resolved", "outcome", "last_step", "duration_seconds") });
		await ObExpectValue("tour skip", "onboarding_tutorial_resolved", "outcome", "skipped");
		for (int i = 0; i < 30; i++)
		{
			BillingUi billing = host.Billing;
			if (billing == null || billing.IsPaywallVisible)
			{
				break;
			}
			await Wait(100);
		}
		await OnUi(delegate
		{
			BillingUi billing2 = host.Billing;
			if (billing2 != null && billing2.IsPaywallVisible)
			{
				billing2.DismissPaywall();
			}
		});
	}

	private async Task ObInterviewAsync()
	{
		InterviewController interview = host.Interview;
		if (interview == null)
		{
			failed++;
			say("analytics: FAIL interview: no interview controller");
			return;
		}
		Mark();
		await OnUi(delegate
		{
			interview.Start();
			FlowHooks.LiveStateNote?.Invoke();
			FlowHooks.SkipInterview?.Invoke();
		});
		await OnUi(interview.Stop);
		await Expect("interview", new(string, string[])[4]
		{
			ObE("home_space_new_clicky_interview_started", "mode", "interview_id"),
			ObE("clicky_interview_answer_submitted", "mode", "source", "answer_number", "interview_id"),
			ObE("onboarding_interview_skipped_by_voice", "interview_id", "answer_count"),
			ObE("home_space_new_clicky_interview_ended", "founded", "mode", "answer_count", "interview_id", "duration_seconds")
		});
		await ObExpectValue("interview", "clicky_interview_answer_submitted", "source", "voice");
		await ObExpectValue("interview", "clicky_interview_answer_submitted", "interview_id", interview.InterviewId);
	}

	private async Task ObRevealAsync()
	{
		HomeTour reveal = null;
		Task<bool> run = null;
		Mark();
		await OnUi(delegate
		{
			StarterCast.Unlock();
			reveal = new HomeTour(host, host.Interview);
			run = reveal.RunAsync();
		});
		await Expect("reveal", new(string, string[])[1] { ObE("onboarding_reveal_started", "clicky_count", "suggestion_count", "interview_id", "is_debug_preview") });
		await OnUi(delegate
		{
			reveal.AbortForEscape();
		});
		await Task.WhenAny(run, Wait(20000));
		await Expect("reveal", new(string, string[])[1] { ObE("onboarding_reveal_ended", "interview_id", "outcome", "duration_seconds") }, 3000);
		await ObExpectValue("reveal", "onboarding_reveal_ended", "outcome", "aborted");
	}
}
