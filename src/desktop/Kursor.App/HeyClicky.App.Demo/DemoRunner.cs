using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HeyClicky.Agents.Backend;
using HeyClicky.App.Billing;
using HeyClicky.App.DevFakes;
using HeyClicky.App.Drive;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.Onboarding;
using HeyClicky.App.Settings;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Demo;

public sealed class DemoRunner
{
	private sealed class DemoPaywallHost : IPaywallCardHost
	{
		public Task<string?> StartCheckoutAsync(string plan, string interval, string surface, PaywallSource source)
		{
			return Task.FromResult<string>(null);
		}

		public Task<string?> OpenPortalAsync()
		{
			return Task.FromResult<string>(null);
		}

		public void Close(PaywallCloseKind kind)
		{
		}

		public void ContentResized()
		{
		}
	}

	[CompilerGenerated]
	private sealed class _003CDemoAttachments_003Ed__11 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		string IEnumerator<string>.Current
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
		public _003CDemoAttachments_003Ed__11(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			string processPath;
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				string text = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.png", SearchOption.AllDirectories).FirstOrDefault();
				if (text != null)
				{
					_003C_003E2__current = text;
					_003C_003E1__state = 1;
					return true;
				}
				goto IL_0052;
			}
			case 1:
				_003C_003E1__state = -1;
				goto IL_0052;
			case 2:
				{
					_003C_003E1__state = -1;
					break;
				}
				IL_0052:
				processPath = Environment.ProcessPath;
				if (processPath != null)
				{
					_003C_003E2__current = processPath;
					_003C_003E1__state = 2;
					return true;
				}
				break;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003CDemoAttachments_003Ed__11(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private readonly AppHost host;

	private readonly List<(string name, Action show, double seconds, Func<UIElement?>? snap)> steps = new List<(string, Action, double, Func<UIElement>)>();

	private readonly Dictionary<string, (Func<nint> hwnd, double after)> windowSnaps = new Dictionary<string, (Func<nint>, double)>();

	public static double DwellSeconds = 1.5;

	private DispatcherQueueTimer? stepTimer;

	private PaywallWindow? demoPaywall;

	private INotchPresenter P => host.Services.Presenter;

	public IEnumerable<string> Names => steps.Select<(string, Action, double, Func<UIElement>), string>(((string name, Action show, double seconds, Func<UIElement> snap) s) => s.name);

	public DemoRunner(AppHost host)
	{
		this.host = host;
		if (double.TryParse(Environment.GetEnvironmentVariable("HEYCLICKY_DEMO_DWELL"), CultureInfo.InvariantCulture, out var result))
		{
			DwellSeconds = result;
		}
		BuildNotchSteps();
		BuildWindowSteps();
	}

	private void Live(NotchLiveEvent e)
	{
		P.ShowLiveEvent(e, null);
	}

	private void Add(string name, Action show, double seconds = 0.0, Func<UIElement?>? snap = null)
	{
		steps.Add((name, show, (seconds <= 0.0) ? DwellSeconds : seconds, snap));
	}

	private void BuildNotchSteps()
	{
		Add("idle", delegate
		{
			P.ReturnToIdle();
		});
		Add("listening", delegate
		{
			Live(new NotchLiveEvent.ActivityListening());
		});
		Add("thinking", delegate
		{
			Live(new NotchLiveEvent.ActivityThinking());
		});
		Add("speaking", delegate
		{
			Live(new NotchLiveEvent.ActivitySpeaking());
		});
		Add("custom", delegate
		{
			Live(new NotchLiveEvent.ActivityCustomStatus("Searching the web"));
		});
		Add("custom-long", delegate
		{
			Live(new NotchLiveEvent.ActivityCustomStatus("Pulling up places near Union Square, San Francisco"));
		});
		Add("agent-multi", delegate
		{
			SeedAgents();
			Live(new NotchLiveEvent.AgentMultiActivity("Research flights to Tokyo", 2));
		});
		Add("agent-task", delegate
		{
			Live(new NotchLiveEvent.AgentTask("demo-1", "Research flights to Tokyo", "running"));
		});
		Add("custom-agent", delegate
		{
			SeedAgents();
			Live(new NotchLiveEvent.ActivityCustomStatus("Searching the web"));
		});
		Add("dictation-recording", delegate
		{
			Live(new NotchLiveEvent.Dictation("Dictating", IsRecording: true));
		});
		Add("dictation-transcribing", delegate
		{
			Live(new NotchLiveEvent.Dictation("Transcribing", IsRecording: false));
		});
		Add("resting-mark", delegate
		{
			Live(new NotchLiveEvent.RestingMark());
		});
		Add("hatch-peek", delegate
		{
			Live(new NotchLiveEvent.IntroHatchPeek());
		});
		Add("integration-suggestion", delegate
		{
			Live(new NotchLiveEvent.IntegrationSuggestion("gmail", "Gmail"));
		});
		Add("integration-connected", delegate
		{
			Live(new NotchLiveEvent.IntegrationConnected("github"));
		});
		Add("meeting", delegate
		{
			Live(new NotchLiveEvent.MeetingCountdown("Design review", DateTimeOffset.Now.AddSeconds(59.0), new Uri("https://meet.google.com/abc-defg-hij")));
		});
		Add("mic-trouble", delegate
		{
			Live(new NotchLiveEvent.MicrophoneTrouble("USB Microphone isn't sending audio"));
		});
		Add("muted", delegate
		{
			Live(new NotchLiveEvent.VoiceReplyMutedCopiedToClipboard());
		});
		Add("muted-unmute", delegate
		{
			Live(new NotchLiveEvent.VoiceReplyMutedCopiedToClipboard("muted"));
		});
		Add("muted-came-back", delegate
		{
			Live(new NotchLiveEvent.VoiceReplyMutedCopiedToClipboard("sound_came_back"));
		});
		Add("dictation-clipboard", delegate
		{
			Live(new NotchLiveEvent.DictationCopiedToClipboard());
		});
		Add("app-updated", delegate
		{
			Live(new NotchLiveEvent.AppUpdated("0.1.0"));
		});
		Add("clicky-update", delegate
		{
			Live(new NotchLiveEvent.ClickyUpdate("research-buddy", "done"));
		});
		Add("tutorial-invite", delegate
		{
			Live(new NotchLiveEvent.OnboardingTutorialInvite());
		});
		Add("tutorial-resume", delegate
		{
			Live(new NotchLiveEvent.OnboardingTutorialResume());
		});
		Add("morning", delegate
		{
			Live(new NotchLiveEvent.MorningSuggestions(3));
		});
		Add("agent-peek", delegate
		{
			SeedAgents();
			Live(new NotchLiveEvent.AgentPeek());
		});
		Add("handoff-queued", delegate
		{
			Live(new NotchLiveEvent.HandoffQueued(2));
		});
		Add("handoff-sent", delegate
		{
			Live(new NotchLiveEvent.HandoffSent("Terminal"));
		});
		Add("sidecar-suggestion", delegate
		{
			Live(new NotchLiveEvent.SidecarLearningSuggestion("Blender"));
		});
		Add("spotify-tip", delegate
		{
			Live(new NotchLiveEvent.SpotifyVoiceTip());
		});
		Add("dictionary-learned", delegate
		{
			Live(new NotchLiveEvent.DictionaryWordLearned("HeyClicky"));
		});
		Add("document-access", delegate
		{
			Live(new NotchLiveEvent.DocumentFolderAccessDenied("Allow access to your Documents folder", ShowsDontShowAgain: false));
		});
		Add("document-access-repeat", delegate
		{
			Live(new NotchLiveEvent.DocumentFolderAccessDenied("Allow access to your Documents folder", ShowsDontShowAgain: true));
		});
		Add("skill-pinned", delegate
		{
			Live(new NotchLiveEvent.SkillPinned(3, new string[3] { "Figma Pro", "Notion", "Excel Wizard" }));
		});
		Add("intro-skill-ping", delegate
		{
			Live(new NotchLiveEvent.IntroSkillPing());
		});
		Add("intro-skill-ping-grown", delegate
		{
			int token = 0;
			Live(new NotchLiveEvent.IntroSkillPing(++token, LastToggleAdded: true, 0.09));
			DispatcherQueue forCurrentThread = DispatcherQueue.GetForCurrentThread();
			(double, double)[] array = new(double, double)[2]
			{
				(0.25, 0.18),
				(0.5, 0.27)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(double, double) tuple = array[i];
				double item = tuple.Item1;
				double item2 = tuple.Item2;
				double b = item2;
				Once.After(item, delegate
				{
					DemoRunner demoRunner = this;
					int num = token + 1;
					token = num;
					demoRunner.Live(new NotchLiveEvent.IntroSkillPing(num, LastToggleAdded: true, b));
				}, forCurrentThread);
			}
		});
		Add("text-input", delegate
		{
			P.Present(new NotchPresentation.TextInput(""));
		});
		Add("text-input-3-lines", delegate
		{
			P.Present(new NotchPresentation.TextInput("Can you look at the chart on my screen and tell me\nwhy revenue dipped in March?\nAlso suggest two fixes."));
		});
		Add("text-input-attachments", delegate
		{
			P.Present(new NotchPresentation.TextInput("Compare these"));
			Once.After(0.2, delegate
			{
				if (host.NotchUi.ActiveSurfaceForTest is TextInputSurface textInputSurface)
				{
					textInputSurface.Composer.AddAttachments(DemoAttachments());
				}
			}, DispatcherQueue.GetForCurrentThread());
		});
		Add("text-input-drop", delegate
		{
			P.Present(new NotchPresentation.TextInput(""));
			Once.After(0.2, delegate
			{
				host.NotchUi.ActiveSurfaceForTest?.SetDropTargeting(on: true);
			}, DispatcherQueue.GetForCurrentThread());
		});
		Add("text-thinking", delegate
		{
			P.Present(new NotchPresentation.TextResponse("why did revenue dip?", "", IsThinking: true));
		});
		Add("text-response", delegate
		{
			P.Present(new NotchPresentation.TextResponse("why did revenue dip?", "Here's what I see on your screen:\n\n1. The **notch** sits top-center and grows on a spring.\n2. Everything outside it stays *click-through*.\n\nTry `Ctrl+Alt` to talk, or double-tap **Ctrl** to type. More in the [docs](https://heyclicky.com).", IsThinking: false));
		});
		Add("onboarding-welcome", delegate
		{
			P.Present(new NotchPresentation.OnboardingPinned(0));
		});
		Add("onboarding-welcome-granted", delegate
		{
			if (host.Services.Permissions is FakePermissionService fakePermissionService)
			{
				fakePermissionService.Set(PermissionKind.Microphone, PermissionStatus.Granted);
				fakePermissionService.Set(PermissionKind.ScreenCapture, PermissionStatus.Granted);
			}
			P.Present(new NotchPresentation.OnboardingPinned(0));
		});
		Add("onboarding-welcome-micoff", delegate
		{
			if (host.Services.Permissions is FakePermissionService fakePermissionService2)
			{
				fakePermissionService2.MicrophoneBlockedBy = MicrophoneBlock.DesktopApps;
				fakePermissionService2.Set(PermissionKind.ScreenCapture, PermissionStatus.Granted);
				fakePermissionService2.Set(PermissionKind.Microphone, PermissionStatus.Denied);
			}
			P.Present(new NotchPresentation.OnboardingPinned(0));
		});
		Add("onboarding-mic", delegate
		{
			P.Present(new NotchPresentation.OnboardingPinned(1));
		});
		Add("onboarding-screen", delegate
		{
			P.Present(new NotchPresentation.OnboardingPinned(2));
		});
		Add("onboarding-allset", delegate
		{
			P.Present(new NotchPresentation.OnboardingPinned(3));
		});
		Add("onboarding-signedout", delegate
		{
			P.Present(new NotchPresentation.OnboardingPinned(-1));
		});
		Add("idle-end", delegate
		{
			P.ReturnToIdle();
		});
	}

	private void SeedAgents()
	{
		if (host.Services.Agents is FakeAgentService fakeAgentService && fakeAgentService.Tasks.Count == 0)
		{
			fakeAgentService.Seed(new AgentTaskSnapshot("demo-1", null, "Research flights to Tokyo", AgentTaskStatus.Running, "Comparing three airlines", null, DateTimeOffset.Now), new AgentTaskSnapshot("demo-2", null, "Draft the launch email", AgentTaskStatus.Running, "Writing the subject line", null, DateTimeOffset.Now), new AgentTaskSnapshot("demo-3", null, "Summarize Q3 metrics", AgentTaskStatus.Finished, "Done. Summary ready.", "## Q3\n\nRevenue grew **12%**.", DateTimeOffset.Now));
		}
	}

	[IteratorStateMachine(typeof(_003CDemoAttachments_003Ed__11))]
	private static IEnumerable<string> DemoAttachments()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDemoAttachments_003Ed__11(-2);
	}

	private void BuildWindowSteps()
	{
		IntroWindow.Step[] values = Enum.GetValues<IntroWindow.Step>();
		foreach (IntroWindow.Step step in values)
		{
			IntroWindow.Step step2 = step;
			IntroWindow shownIntro = null;
			string text = "intro-" + Kebab(step2.ToString());
			Add(text, delegate
			{
				host.HideAuxWindows();
				P.ReturnToIdle();
				IntroWindow introWindow = (shownIntro = host.DemoIntro(step2));
				if (step2 == IntroWindow.Step.Hatched)
				{
					introWindow.SettleHatched();
				}
			}, (step2 == IntroWindow.Step.Hatched) ? 4.0 : 0.0, () => host.DemoIntroRoot());
			windowSnaps[text] = (() => shownIntro?.Window.Hwnd ?? 0, 1.2);
		}
		TutorialWindow.Step[] values2 = Enum.GetValues<TutorialWindow.Step>();
		foreach (TutorialWindow.Step step3 in values2)
		{
			TutorialWindow.Step step4 = step3;
			TutorialWindow shownTour = null;
			string text2 = "tutorial-" + Kebab(step4.ToString());
			Add(text2, delegate
			{
				host.HideAuxWindows();
				TutorialWindow tutorialWindow = (shownTour = host.DemoTutorial(step4));
				TutorialWindow.Step step5 = step4;
				if ((step5 == TutorialWindow.Step.VoiceHello || step5 == TutorialWindow.Step.DrawDemo) ? true : false)
				{
					tutorialWindow.MarkCompleted(step4);
				}
			}, (step4 == TutorialWindow.Step.DrawToAsk) ? 3.0 : 0.0, () => host.DemoTutorialRoot());
			windowSnaps[text2] = (() => shownTour?.Window.Hwnd ?? 0, (step4 == TutorialWindow.Step.DrawToAsk) ? 2.5 : 1.2);
		}
		AddTourPanelSteps();
		Add("cursor-video", delegate
		{
			host.HideAuxWindows();
			string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Video", "onboarding-intro-v2-windows.mp4");
			host.CursorCompanion.PlayVideo(path, Array.Empty<CursorCompanion.Cue>(), delegate
			{
			}, 2.0, 60.0, 5.0, onboardingAnalytics: false);
		}, 5.0, () => host.CursorCompanion.WindowForSnapshot.Root);
		windowSnaps["cursor-video"] = (() => host.CursorCompanion.WindowForSnapshot.Hwnd, 3.5);
		Add("cursor-video-end", delegate
		{
			host.CursorCompanion.DismissVideo();
		});
		AddPaywallSteps();
		Add("paywall-end", delegate
		{
			demoPaywall?.Dismiss();
		});
		SettingsContent.DemoCatalog = DemoIntegrationCatalog();
		SettingsWindow.Page[] values3 = Enum.GetValues<SettingsWindow.Page>();
		foreach (SettingsWindow.Page page in values3)
		{
			SettingsWindow.Page pg = page;
			Add("settings-" + Kebab(pg.ToString()), delegate
			{
				host.HideAuxWindows();
				SettingsWindow settingsWindow = host.DemoSettings(pg);
				if (pg == SettingsWindow.Page.Referral)
				{
					settingsWindow.Content.SetReferralForTests(ReferralFixture.ReferralBody(earned: false));
				}
			}, 0.0, () => host.DemoSettingsRoot());
		}
		Add("settings-integrations-connector", delegate
		{
			host.HideAuxWindows();
			host.DemoSettings(SettingsWindow.Page.Integrations).Content.DemoOpenConnectorEditor();
		}, 0.0, () => host.DemoSettingsRoot());
		Add("settings-referral-earned", delegate
		{
			host.HideAuxWindows();
			host.DemoSettings(SettingsWindow.Page.Referral).Content.SetReferralForTests(ReferralFixture.ReferralBody(earned: true));
		}, 0.0, () => host.DemoSettingsRoot());
		PortraitGallery gallery = null;
		Add("portraits", delegate
		{
			host.HideAuxWindows();
			if (gallery == null)
			{
				gallery = new PortraitGallery(!host.Options.AllowCapture);
			}
			gallery.Window.ShowCentered();
		}, 3.0, () => gallery?.Window.Root);
		Add("windows-end", delegate
		{
			host.HideAuxWindows();
			gallery?.Window.HideWindow();
		});
		DocumentGallery.AddSteps(host, delegate(string name, Action show, double seconds, Func<UIElement?> snap)
		{
			Add(name, show, seconds, snap);
		});
	}

	public void Run(string? only)
	{
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		List<(string name, Action show, double seconds, Func<UIElement?>? snap)> list = (List<(string name, Action show, double seconds, Func<UIElement?>? snap)>)((only == null) ? ((IList)steps) : ((IList)steps.Where<(string, Action, double, Func<UIElement>)>(((string name, Action show, double seconds, Func<UIElement> snap) s) => s.name.Equals(only, StringComparison.OrdinalIgnoreCase) || (only.EndsWith('*') && s.name.StartsWith(only.TrimEnd('*'), StringComparison.OrdinalIgnoreCase))).ToList()));
		if (list.Count == 0)
		{
			Log.Warn("demo", "no demo state named '" + only + "'. Known: " + string.Join(", ", steps.Select<(string, Action, double, Func<UIElement>), string>(((string name, Action show, double seconds, Func<UIElement> snap) s) => s.name)));
			return;
		}
		int i = 0;
		DispatcherQueueTimer timer = (stepTimer = dq.CreateTimer());
		timer.IsRepeating = false;
		timer.Tick += delegate
		{
			Next();
		};
		Once.After(0.4, Next, dq);
		void Next()
		{
			if (i >= list.Count)
			{
				Log.Info("demo", "DEMO done");
			}
			else
			{
				var (name, action, num, func) = list[i++];
				try
				{
					action();
					Log.Info("demo", "DEMO " + name);
				}
				catch (Exception ex)
				{
					Log.Error("demo", "DEMO " + name + " failed", ex);
				}
				if (Snapshot.Directory != null)
				{
					Func<UIElement?> target = func;
					Once.After(Math.Min(1.1, Math.Max(0.6, num - 0.3)), async delegate
					{
						UIElement uIElement = target?.Invoke() ?? host.NotchUi.Window.Root;
						if ((object)uIElement != null)
						{
							await Snapshot.SaveAsync(uIElement, name);
						}
					}, dq);
					if (windowSnaps.TryGetValue(name, out var ws))
					{
						Once.After(ws.after, async delegate
						{
							await Snapshot.SaveWindowAsync(ws.hwnd(), name + "-window");
						}, dq);
					}
				}
				if (only == null || list.Count != 1)
				{
					timer.Interval = TimeSpan.FromSeconds(num);
					timer.Start();
				}
			}
		}
	}

	private void AddPaywallSteps()
	{
		string resetAt = DateTimeOffset.UtcNow.AddDays(12.0).ToString("O");
		Paywall("paywall-limit", PaywallPayloads.LocalMessagesLimit(25, 25, resetAt, 150, 1000, proYearlyAvailable: true, maxYearlyAvailable: true), PaywallSource.LimitHit, "free", null);
		Paywall("paywall-upgrade", PaywallPayloads.ManualUpgrade("free", 150, 1000, proYearlyAvailable: true, maxYearlyAvailable: true), PaywallSource.SettingsUpgradeButton, "free", null);
		Paywall("paywall-upgrade-max", PaywallPayloads.ManualUpgrade("pro", 150, 1000, proYearlyAvailable: true, maxYearlyAvailable: true), PaywallSource.SettingsUpgradeButton, "pro", null);
		Paywall("paywall-onboarding", PaywallPayloads.ManualUpgrade("free", 150, 1000, proYearlyAvailable: true, maxYearlyAvailable: true), PaywallSource.OnboardingCompleted, "free", null);
		Paywall("paywall-referral", PaywallPayloads.ManualUpgrade("free", 150, 1000, proYearlyAvailable: true, maxYearlyAvailable: true), PaywallSource.SettingsUpgradeButton, "free", new ReferralPaywallDiscount(50.0, 3, AppliesToYearlyPlans: false, "Sam", null));
	}

	private void Paywall(string name, PaywallPayload payload, PaywallSource source, string tier, ReferralPaywallDiscount? referral)
	{
		Add(name, delegate
		{
			host.HideAuxWindows();
			PaywallPalette palette = PaywallPalette.Current();
			PaywallCard card = new PaywallCard(payload, source, palette, tier, referral, new DemoPaywallHost());
			if ((object)demoPaywall == null)
			{
				demoPaywall = new PaywallWindow(!host.Options.AllowCapture);
			}
			double width = (source.OffersFreeOption() ? 800 : ((payload.UpgradeOptions.Count >= 2) ? 560 : 400));
			demoPaywall.Present(card, width, palette);
		}, 0.0, () => demoPaywall?.Root);
	}

	private void AddTourPanelSteps()
	{
		if (host.Options.DemoOnly != "tutorial-panel")
		{
			return;
		}
		Add("tutorial-panel", delegate
		{
			host.HideAuxWindows();
			if (TourDrive.StepContent == null)
			{
				TourDrive.StepContent = delegate(Color accent, FrameworkElement askRow, Func<bool> _)
				{
					FrameworkElement frameworkElement = OnboardingDriveDemo.Shared.CreateWindowView(accent);
					frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
					return new StackPanel
					{
						Spacing = 12.0,
						Children = 
						{
							(UIElement)frameworkElement,
							(UIElement)askRow,
							(UIElement)new Grid
							{
								Height = 14.0
							}
						}
					};
				};
			}
			host.DemoTutorial(TutorialWindow.Step.VoiceHello);
			RunTourPanelAsync(DispatcherQueue.GetForCurrentThread());
		}, 40.0, () => host.DemoTutorialRoot());
	}

	private async Task RunTourPanelAsync(DispatcherQueue dq)
	{
		_ = 15;
		try
		{
			TutorialWindow tour = host.DemoTutorialWindow();
			Grid root = tour.Window.Root;
			await Task.Delay(1500);
			await FramesAsync("tutorial-panel-grow-timing", delegate
			{
				tour.Go(TutorialWindow.Step.ClickyInterview);
			}, tour, snapshots: false);
			await Task.Delay(600);
			await FramesAsync("tutorial-panel-shrink-timing", delegate
			{
				tour.Go(TutorialWindow.Step.DrawDemo);
			}, tour, snapshots: false);
			await Task.Delay(600);
			tour.Go(TutorialWindow.Step.VoiceHello);
			await Task.Delay(1500);
			await FramesAsync("tutorial-panel-grow", delegate
			{
				tour.Go(TutorialWindow.Step.ClickyInterview);
			}, tour);
			await Task.Delay(800);
			Log.Info("demo", "tutorial-panel: interview step open, focus: " + Focused(root));
			for (int i = 0; i < 40; i++)
			{
				TextBox textBox = FindElement<TextBox>(root);
				if ((object)textBox == null || textBox.IsEnabled)
				{
					break;
				}
				await Task.Delay(250);
			}
			TextBox box = FindElement<TextBox>(root);
			if ((object)box != null)
			{
				Log.Info("demo", "tutorial-panel: the field is " + (box.IsEnabled ? "enabled" : "disabled") + ", focus: " + Focused(root));
				await Attempt("a programmatic focus on the field", () => box.Focus(FocusState.Programmatic));
				await Attempt("a keyboard-state focus with no direction (an activation restore)", () => box.Focus(FocusState.Keyboard));
				Log.Info("demo", "tutorial-panel: XAML's first focusable element: " + (FocusManager.FindFirstFocusableElement(root)?.GetType().Name ?? "none"));
				await Attempt("a pointer focus on the field (a click)", () => box.Focus(FocusState.Pointer));
				await Snapshot.SaveAsync(root, "tutorial-panel-field-clicked");
			}
			await FramesAsync("tutorial-panel-shrink", delegate
			{
				tour.Go(TutorialWindow.Step.DrawDemo);
			}, tour);
			tour.Go(TutorialWindow.Step.ClickyDrive);
			await Task.Delay(1200);
			await Snapshot.SaveAsync(root, "tutorial-panel-drive");
			OnboardingDriveDemo shared = OnboardingDriveDemo.Shared;
			string[] array = new string[5] { "row0.field", "row0.check", "row1.field", "row1.check", "row2.field" };
			foreach (string text in array)
			{
				Log.Info("demo", $"tutorial-panel: drive target {text} at {shared.ScreenCenter(text)} (window {tour.Window.RectPx})");
			}
			TextBlock textBlock = FindElement(root, (TextBlock t) => t.Text == "Your list");
			if ((object)textBlock != null)
			{
				Point point = textBlock.TransformToVisual(root).TransformPoint(new Point(0f, 0f));
				Log.Info("demo", $"tutorial-panel: \"Your list\" title at y {point.Y:0.0} (panel top {(root.ActualHeight - tour.Window.PanelHeight) / 2.0:0.0})");
			}
			async Task Attempt(string what, Func<bool> focus)
			{
				bool result = focus();
				await Task.Delay(150);
				Log.Info("demo", $"tutorial-panel: {what}: {result}, focus a moment later: {Focused(root)}");
			}
		}
		catch (Exception ex)
		{
			Log.Error("demo", "tutorial-panel", ex);
		}
	}

	private static async Task FramesAsync(string name, Action change, TutorialWindow tour, bool snapshots = true)
	{
		bool instant = Anim.Instant;
		Anim.Instant = false;
		Stopwatch clock = Stopwatch.StartNew();
		try
		{
			change();
		}
		finally
		{
			Anim.Instant = instant;
		}
		List<string> sizes = new List<string>();
		while (clock.Elapsed.TotalSeconds < 1.1)
		{
			double totalMilliseconds = clock.Elapsed.TotalMilliseconds;
			sizes.Add($"{totalMilliseconds:0}ms {tour.Window.ShownWidth:0.0}x{tour.Window.ShownHeight:0.0}");
			if (snapshots)
			{
				await Snapshot.SaveAsync(tour.Window.Root, $"{name}-{totalMilliseconds:0000}ms");
			}
			else
			{
				await Task.Delay(50);
			}
		}
		Log.Info("demo", name + ": " + string.Join(", ", sizes));
	}

	private static string Focused(FrameworkElement root)
	{
		object focusedElement = FocusManager.GetFocusedElement(root.XamlRoot);
		if (focusedElement != null)
		{
			if (focusedElement is TextBox textBox)
			{
				return "the text box \"" + textBox.PlaceholderText + "\"";
			}
			return focusedElement.GetType().Name;
		}
		return "nothing";
	}

	private static T? FindElement<T>(DependencyObject root, Func<T, bool>? match = null) where T : class
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is T val && (match == null || match(val)))
			{
				return val;
			}
			T val2 = FindElement(child, match);
			if (val2 != null)
			{
				return val2;
			}
		}
		return null;
	}

	private static AgentIntegrationCatalog DemoIntegrationCatalog()
	{
		return new AgentIntegrationCatalog(ComposioConfigured: true, new global::_003C_003Ez__ReadOnlyArray<AgentIntegration>(new AgentIntegration[6]
		{
			new AgentIntegration("gmail", "Gmail", "Read, search and draft email.", "connected", 1, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("gmail", "Gmail", on: true)), new global::_003C_003Ez__ReadOnlyArray<AgentIntegrationAccount>(new AgentIntegrationAccount[2]
			{
				Account("ca_1", "maya@acme.com", "Work"),
				Account("ca_2", "maya.lee@gmail.com", null)
			})),
			new AgentIntegration("github", "GitHub", "Issues, pull requests and code.", "connected", 1, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("github", "GitHub", on: true)), Array.Empty<AgentIntegrationAccount>()),
			new AgentIntegration("googlecalendar", "Google Calendar", "See and schedule events.", "needs_reconnect", 0, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("googlecalendar", "Google Calendar", on: false)), new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationAccount>(Account("ca_3", "maya@acme.com", null, "expired"))),
			new AgentIntegration("slack", "Slack", "Read channels and send messages.", "not_connected", 0, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("slack", "Slack", on: false)), Array.Empty<AgentIntegrationAccount>()),
			new AgentIntegration("notion", "Notion", "Pages, databases and docs.", "not_connected", 0, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("notion", "Notion", on: false)), Array.Empty<AgentIntegrationAccount>()),
			new AgentIntegration("linear", "Linear", "Track issues and projects.", "not_connected", 0, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(Kit("linear", "Linear", on: false)), Array.Empty<AgentIntegrationAccount>())
		}));
		static AgentIntegrationAccount Account(string id, string? name, string? label, string status = "connected")
		{
			return new AgentIntegrationAccount(id, name, label, null, null, status, DateTimeOffset.Now.AddDays(-12.0).ToString("O"));
		}
		static AgentIntegrationToolkit Kit(string slug, string name, bool on)
		{
			return new AgentIntegrationToolkit(slug, name, null, on);
		}
	}

	private static string Kebab(string s)
	{
		return string.Concat(s.Select((char ch, int i) => (!char.IsUpper(ch) || i <= 0) ? char.ToLowerInvariant(ch).ToString() : ("-" + char.ToLowerInvariant(ch))));
	}
}
