using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Notch.Window;
using HeyClicky.App.Proactive;
using HeyClicky.App.Shell;
using HeyClicky.Core;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Dictation;
using HeyClicky.Core.Net;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;
using HeyClicky.Drawing;
using HeyClicky.Platform.Audio;
using HeyClicky.Platform.Capture;
using HeyClicky.Platform.Input;
using HeyClicky.Platform.Overlay;
using HeyClicky.Platform.Permissions;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using Windows.System.UserProfile;

namespace HeyClicky.App.Wiring;

public sealed class RealStack : IAsyncDisposable
{
	private static (IReadOnlyList<string> Tags, long ReadAtTicks)? cachedEarsSystemLanguages;

	public CoreServices Core { get; private set; }

	public PermissionService Permissions { get; private set; }

	public IMicrophoneCapture Microphone { get; private set; }

	public WasapiAudioOutput Audio { get; private set; }

	public MicrophoneStartCue MicrophoneStartCue { get; private set; }

	public ScreenCaptureService Capture { get; private set; }

	public DeferredCursorOverlay Cursor { get; private set; }

	public DeferredHighlightOverlay Highlight { get; private set; }

	public ScreenDrawingCanvas Canvas { get; private set; }

	public AgentService Agents { get; private set; }

	public CompanionSession Companion { get; private set; }

	public DictationController Dictation { get; private set; }

	public ShortcutMonitor Shortcuts { get; private set; }

	public OnboardingTutorialViewModel Tutorial { get; private set; }

	public BrainContext BrainContext { get; private set; }

	public CompanionTools Tools { get; private set; }

	public AgentVoiceRelay AgentVoice { get; private set; }

	public OneBrainJobReports JobReports { get; private set; }

	public AgentToolHost ToolHost { get; private set; }

	public Func<string?, bool>? OpenHome { get; set; }

	public Func<bool>? CloseHome { get; set; }

	public bool IsComplete => Companion != null;

	private async Task<(int Status, string Body)?> PostToWorkerAsync(string path, string jsonBody, CancellationToken ct)
	{
		try
		{
			byte[] array = await Core.Worker.SendRawAsync(HttpMethod.Post, path, new StringContent(jsonBody, Encoding.UTF8, "application/json"), authenticated: true, ct, TimeSpan.FromSeconds(30L), "companion").ConfigureAwait(continueOnCapturedContext: false);
			return (array == null) ? (((int, string)?)null) : new(int, string)?((200, Encoding.UTF8.GetString(array)));
		}
		catch (WorkerHttpException ex)
		{
			return (ex.Status, ex.Body);
		}
		catch (PaywallReachedException)
		{
			return (402, "{\"error\":\"paywall\"}");
		}
	}

	private static string IanaTimeZone()
	{
		if (!TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out string ianaId))
		{
			return TimeZoneInfo.Local.Id;
		}
		return ianaId;
	}

	private static IReadOnlyList<string> SystemLanguageTagsForEars()
	{
		(IReadOnlyList<string>, long)? tuple = cachedEarsSystemLanguages;
		if (tuple.HasValue)
		{
			(IReadOnlyList<string>, long) valueOrDefault = tuple.GetValueOrDefault();
			if (Environment.TickCount64 - valueOrDefault.Item2 < 600000)
			{
				return valueOrDefault.Item1;
			}
		}
		List<string> list = new List<string>();
		try
		{
			list.AddRange(GlobalizationPreferences.Languages);
		}
		catch
		{
			list.Add(CultureInfo.CurrentUICulture.Name);
		}
		list.AddRange(KeyboardLayoutInfo.InstalledLayoutLanguageTags());
		IReadOnlyList<string> readOnlyList = list.Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
		cachedEarsSystemLanguages = (readOnlyList, Environment.TickCount64);
		return readOnlyList;
	}

	private static bool OpenUri(string uri)
	{
		try
		{
			ShellOpen.Start(uri);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "open " + uri + ": " + ex.Message);
			return false;
		}
	}

	private static string? TestSpeakerSilence()
	{
		return null;
	}

	private static bool SpotifyAppInstalled()
	{
		using RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("spotify\\shell\\open\\command");
		return registryKey != null;
	}

	private bool ShowDocumentAccessCard(CoreServices core, string path)
	{
		if (core.Settings.Read((AppSettings s) => s.DocumentAccessNoticeSuppressed))
		{
			return false;
		}
		if (core.Notch.HasLiveEvent(typeof(NotchLiveEvent.DocumentFolderAccessDenied)))
		{
			return true;
		}
		int count = 0;
		core.Settings.Update(delegate(AppSettings s)
		{
			count = ++s.DocumentAccessNoticeShowCount;
		});
		string text = ProtectedFolderName(path);
		core.Notch.ShowLiveEvent(new NotchLiveEvent.DocumentFolderAccessDenied((text == null) ? "Allow your account to open this file" : ("Allow your account to open this file in " + text), count >= 2, path), null);
		Log.Info("PageRead", $"document access card ({text ?? "unrecognized folder"}, show #{count})");
		return true;
	}

	internal bool RaiseDocumentAccessCardForTests(string path)
	{
		return ShowDocumentAccessCard(Core, path);
	}

	private static string? ProtectedFolderName(string path)
	{
		(Environment.SpecialFolder, string)[] array = new(Environment.SpecialFolder, string)[5]
		{
			(Environment.SpecialFolder.Personal, "Documents"),
			(Environment.SpecialFolder.Desktop, "Desktop"),
			(Environment.SpecialFolder.MyPictures, "Pictures"),
			(Environment.SpecialFolder.MyMusic, "Music"),
			(Environment.SpecialFolder.MyVideos, "Videos")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(Environment.SpecialFolder, string) tuple = array[i];
			Environment.SpecialFolder item = tuple.Item1;
			string item2 = tuple.Item2;
			string folderPath = Environment.GetFolderPath(item);
			if (folderPath.Length > 0 && path.StartsWith(folderPath + "\\", StringComparison.OrdinalIgnoreCase))
			{
				return item2;
			}
		}
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
		if (!path.StartsWith(text + "\\", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return "Downloads";
	}

	public static RealStack CreateCore()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		CoreServices core = CoreServices.Create();
		PermissionService permissionService = new PermissionService();
		permissionService.StartPolling();
		VoicePreviewStore.Shared = new VoicePreviewStore(core.Http, (CancellationToken ct) => core.Auth.GetValidAccessTokenAsync(ct));
		Log.Info("startup", $"core services in {stopwatch.Elapsed.TotalMilliseconds:0} ms");
		return new RealStack
		{
			Core = core,
			Permissions = permissionService,
			Tutorial = new OnboardingTutorialViewModel(core.Settings, () => SystemVolume.EffectiveLevel())
		};
	}

	public void Complete(DispatcherQueue ui, SynchronizationContext? uiContext)
	{
		Stopwatch sw = Stopwatch.StartNew();
		CoreServices core = Core;
		Environment.GetEnvironmentVariable("HEYCLICKY_TEST_MIC_WAV");
		WasapiMicrophoneCapture wasapiMicrophoneCapture = new WasapiMicrophoneCapture();
		wasapiMicrophoneCapture.Prewarm();
		IMicrophoneCapture microphone = wasapiMicrophoneCapture;
		_ = AudioTopologyMonitor.Shared;
		AudioOutputRoute.Changed += delegate(AudioOutputRouteChange change)
		{
			if (!(change.StreamName != "voice"))
			{
				ClickyAnalytics.TrackRealtimeAudioOutputRouteChange(change.FromTransport, change.ToTransport, change.FromDevice, change.ToDevice, change.DroppedBuffers, change.EngineWasRunning, change.RestartSucceeded);
			}
		};
		AudioOutputRoute.OutputUnavailable += delegate(string stream, string transport, string device)
		{
			if (stream == "voice")
			{
				ClickyAnalytics.ReportRealtimeAudioRouteRebuildFailed(transport, device, 0);
			}
		};
		WasapiAudioOutput audio = new WasapiAudioOutput();
		Task.Run((Action)audio.PreloadEarcons);
		MicrophoneStartCue microphoneStartCue = new MicrophoneStartCue(microphone, delegate(string name, float? volume)
		{
			if (volume.HasValue)
			{
				float valueOrDefault = volume.GetValueOrDefault();
				audio.PlayEarcon(name, valueOrDefault);
			}
			else
			{
				audio.PlayEarcon(name);
			}
		});
		ScreenCaptureService capture = new ScreenCaptureService();
		double value = Lap();
		DeferredCursorOverlay cursor = new DeferredCursorOverlay();
		DeferredHighlightOverlay highlight = new DeferredHighlightOverlay();
		ScreenDrawingCanvas screenDrawingCanvas = new ScreenDrawingCanvas((CancellationToken ct) => core.Auth.GetValidAccessTokenAsync(ct), core.Endpoints.WorkerBaseUrl, ui, delegate(HttpRequestMessage request)
		{
			ClickyClientHeadersHandler.Apply(request.Headers, core.Identity);
		});
		screenDrawingCanvas.PenStrokes += delegate(object? _, IReadOnlyList<PenStrokePath> strokes)
		{
			cursor.RidePen(strokes);
		};
		screenDrawingCanvas.Cleared += delegate
		{
			cursor.EndPenRide();
		};
		screenDrawingCanvas.PenHandedBack += delegate
		{
			cursor.EndPenRide();
		};
		screenDrawingCanvas.ScreenChangeWatchFactory = (DisplayInfo display, Action<string> onChanged) => new ScreenDrawingStaleWatch(display, onChanged);
		double value2 = Lap();
		AgentRuntimeOptions agentRuntimeOptions = new AgentRuntimeOptions();
		AppHost.PrepareStoredSettings(core.Settings, agentRuntimeOptions);
		AgentService agents = new AgentService(core.Endpoints, (CancellationToken ct) => core.Auth.GetValidAccessTokenAsync(ct), core.Http.Client, core.Identity, agentRuntimeOptions);
		agents.CurrentUserId = () => (!core.Auth.IsSignedIn) ? null : core.Auth.UserId;
		agents.Routines.AgentAllowanceResetsAt = () => core.Billing.UsageResetsDate;
		AgentToolHost agentToolHost2 = (ToolHost = new AgentToolHost(agents, (string? threadTaskId) => OpenHome?.Invoke(threadTaskId) ?? false, () => CloseHome?.Invoke() ?? false));
		AgentToolHost agentToolHost3 = agentToolHost2;
		BrainContext brainContext = new BrainContext(core, agentToolHost3);
		AgentTools agents2 = new AgentTools(agentToolHost3);
		CompanionTools companionTools = new CompanionTools(new LocalRootTools(() => agents.ProjectsRoot), new WorkerTools(PostToWorkerAsync, IanaTimeZone, OpenUri, SpotifyAppInstalled), agents2);
		OnboardingTutorialViewModel tutorial = Tutorial;
		IAudioOutput audioOutput2;
		if (!(Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1"))
		{
			IAudioOutput audioOutput = audio;
			audioOutput2 = audioOutput;
		}
		else
		{
			IAudioOutput audioOutput = new MutedAudioOutput(audio);
			audioOutput2 = audioOutput;
		}
		IAudioOutput audio2 = audioOutput2;
		CoreServices coreServices = core;
		ScreenCaptureService screens = capture;
		DeferredCursorOverlay cursor2 = cursor;
		CompanionSessionOptions obj = new CompanionSessionOptions
		{
			UiContext = uiContext,
			DesktopActions = new PlatformDesktopActions(),
			ToolCallHandler = companionTools.HandleAsync,
			UserContext = brainContext.UserContext,
			RosterNote = brainContext.RosterNote,
			PlanNote = brainContext.PlanNote,
			PageReader = new WindowsPageReader(),
			DocumentAccessDenied = (string path) => ShowDocumentAccessCard(core, path)
		};
		string silence = TestSpeakerSilence();
		obj.OutputSilenceKind = ((silence != null) ? ((Func<string>)(() => silence)) : new Func<string>(SystemAudioOutputVolume.CurrentSilenceKind));
		obj.IsOnboardingTutorialRestrictingNormalUse = () => tutorial.IsRestrictingNormalUse;
		obj.UsageContext = () => (!tutorial.IsEngaged) ? "companion" : "onboarding";
		obj.LiveStateNote = delegate
		{
			Func<bool>? isInterviewSendOffInProgress = FlowHooks.IsInterviewSendOffInProgress;
			object obj2;
			if (isInterviewSendOffInProgress == null || !isInterviewSendOffInProgress())
			{
				obj2 = FlowHooks.LiveStateNote?.Invoke();
				if (obj2 == null)
				{
					obj2 = ProactiveHooks.LiveStateNote?.Invoke();
					if (obj2 == null)
					{
						obj2 = ((!tutorial.IsRestrictingNormalUse) ? null : FlowHooks.TourStepNote?.Invoke());
						if (obj2 == null)
						{
							return FlowHooks.NotchNote?.Invoke();
						}
					}
				}
			}
			else
			{
				obj2 = null;
			}
			return (string?)obj2;
		};
		obj.TurnFlow = delegate
		{
			object obj3 = FlowHooks.TurnFlow?.Invoke();
			if (obj3 == null)
			{
				obj3 = ((ProactiveCoordinator.Current?.AdjustingCardId).HasValue ? "suggestion_adjust" : null);
				if (obj3 == null)
				{
					if (!tutorial.IsEngaged)
					{
						Func<bool>? isInterviewSendOffInProgress2 = FlowHooks.IsInterviewSendOffInProgress;
						if (isInterviewSendOffInProgress2 == null || !isInterviewSendOffInProgress2())
						{
							return (string?)null;
						}
					}
					obj3 = "onboarding_tour";
				}
			}
			return (string?)obj3;
		};
		obj.SnapTargetToControl = (RectD modelBox, DisplayInfo display, CancellationToken ct) => UIAutomationHelpers.ControlFrameNearAsync(modelBox, display.BoundsPx, display.Scale, ct);
		obj.PointerInput = new PointerInput();
		obj.RefuseTurnOverMessageCap = delegate
		{
			if (!core.Billing.ShouldBlockFreeUserMessageTurnLocally())
			{
				return false;
			}
			PaywallPayload paywallPayload = core.Billing.MakeLocalMessagesLimitPaywallPayload();
			if ((object)paywallPayload != null)
			{
				BillingPaywallDetector.Present(paywallPayload);
			}
			return true;
		};
		obj.MessageCounted = delegate(bool counted)
		{
			if (counted)
			{
				core.Billing.RegisterLocalMessageTurnConsumed();
			}
			else
			{
				core.Billing.RollbackLocalMessageTurnConsumed();
			}
		};
		obj.KeepMouthWarm = () => agents.Tasks.Any(delegate(AgentTaskSnapshot t)
		{
			AgentTaskStatus status = t.Status;
			return (uint)status <= 1u;
		});
		obj.HighlightText = async delegate(IReadOnlyList<HighlightStroke> strokes, CancellationToken ct)
		{
			if (QuietContext.FullscreenForegroundMonitor() == 0)
			{
				return await HighlightTextHarvester.SummarizeAsync(strokes, capture.GetDisplays(), ct);
			}
			using CancellationTokenSource quick = CancellationTokenSource.CreateLinkedTokenSource(ct);
			quick.CancelAfter(300);
			try
			{
				return await HighlightTextHarvester.SummarizeAsync(strokes, capture.GetDisplays(), quick.Token).WaitAsync(quick.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				return (string?)null;
			}
		};
		obj.MutedSpeakerReplyExempt = () => FlowHooks.MutedSpeakerReplyExempt?.Invoke() ?? false;
		obj.TakeFlowBrief = () => FlowHooks.TakeFlowBrief?.Invoke();
		obj.TakePillPlanNote = () => FlowHooks.TakePillPlanNote?.Invoke();
		obj.EdgeDockFocusNote = (bool typed) => FlowHooks.EdgeDockFocusNote?.Invoke(typed);
		obj.TakeSpokenWordsDirectly = (string words) => FlowHooks.TakeSpokenWordsDirectly?.Invoke(words) ?? Task.FromResult(result: false);
		obj.BrowserStateNote = (bool canDrive) => FlowHooks.BrowserStateNote?.Invoke(canDrive);
		obj.IsOnboardingTurn = delegate
		{
			if (!tutorial.IsEngaged)
			{
				Func<bool>? isInterviewSendOffInProgress3 = FlowHooks.IsInterviewSendOffInProgress;
				if (isInterviewSendOffInProgress3 == null || !isInterviewSendOffInProgress3())
				{
					return FlowHooks.IsDrawnTourRunning?.Invoke() ?? false;
				}
			}
			return true;
		};
		obj.CaptureSettleSeconds = () => FlowHooks.CaptureSettleSeconds?.Invoke() ?? 0.0;
		obj.RendersTextInNotchChat = () => FlowHooks.RendersTextInNotchChat?.Invoke() ?? false;
		obj.StepNotchChatAsideForDrawing = () => FlowHooks.StepNotchChatAsideForDrawing?.Invoke() ?? false;
		obj.ChatHistory = () => NotchChat.History;
		obj.SystemLanguageTags = SystemLanguageTagsForEars;
		CompanionSession companion = coreServices.CreateCompanionSession(microphone, audio2, screens, screenDrawingCanvas, cursor2, highlight, obj);
		companion.Prewarm();
		PermissionService permissions = Permissions;
		DictationController dictationController = new DictationController(core.Http, core.Auth, core.Settings, microphone, new DictationTextTarget(), core.Notch, new DictationControllerOptions
		{
			UsageContext = () => (!tutorial.IsEngaged) ? "companion" : "onboarding",
			IsOnboardingTutorialRestricting = () => tutorial.IsRestrictingNormalUse,
			OnboardingTutorialAllowsPress = () => DictationHooks.TutorialAllowsPress?.Invoke() ?? tutorial.AllowsDictationPress,
			IsOnboardingTutorialDictationStepActive = () => tutorial.IsOnDictationStep,
			ConsumeOnboardingTutorialTranscript = (string transcript) => DictationHooks.ConsumeTutorialTranscript?.Invoke(transcript) ?? false,
			CanStartRecording = () => companion.State != VoiceState.Listening,
			IsMicrophoneDenied = () => permissions.GetStatus(PermissionKind.Microphone) == PermissionStatus.Denied,
			PlayCue = ((Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1") ? ((Action<string, float>)delegate
			{
			}) : ((Action<string, float>)delegate(string name, float volume)
			{
				audio.PlayEarcon(name, volume);
			})),
			PlayMicrophoneStartCue = ((Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1") ? ((Action<string, float>)delegate
			{
			}) : ((Action<string, float>)delegate(string name, float volume)
			{
				microphoneStartCue.Play(name, volume);
			}))
		});
		DictationHooks.RemoveLearnedWord = dictationController.RemoveLearnedWord;
		double value3 = Lap();
		Microphone = microphone;
		Audio = audio;
		MicrophoneStartCue = microphoneStartCue;
		Capture = capture;
		Cursor = cursor;
		Highlight = highlight;
		Canvas = screenDrawingCanvas;
		Agents = agents;
		Companion = companion;
		Dictation = dictationController;
		Shortcuts = new ShortcutMonitor();
		companion.StateChanged += delegate
		{
			VoiceState state = companion.State;
			if ((uint)(state - 1) <= 1u)
			{
				ScreenDrawingCanvas.WarmUpFigures();
			}
		};
		BrainContext = brainContext;
		Tools = companionTools;
		AgentVoice = new AgentVoiceRelay(agents, core, companion, brainContext);
		WireJobsAndHarness(core, agents, companion, brainContext, audio);
		agents.TaskEvent += delegate(object? _, AgentTaskEvent e)
		{
			AgentTaskEventKind kind = e.Kind;
			if ((uint)(kind - 8) <= 1u)
			{
				brainContext.Refresh();
			}
		};
		FlowHooks.SuppressDocumentAccessNotice = delegate
		{
			core.Settings.Update(delegate(AppSettings s)
			{
				s.DocumentAccessNoticeSuppressed = true;
			});
		};
		ForegroundTracker.Start();
		ForegroundTracker.ExternalWindowCameForward += ScreenCaptureYield.NoteForegroundWindow;
		if (core.Auth.IsSignedIn)
		{
			brainContext.Refresh();
		}
		Log.Info("startup", $"real services: audio+capture {value:0} ms, canvas {value2:0} ms, agents+companion {value3:0} ms");
		double Lap()
		{
			double totalMilliseconds = sw.Elapsed.TotalMilliseconds;
			sw.Restart();
			return totalMilliseconds;
		}
	}

	public void StartBackground(string cursorColorHex, bool showCursor = true)
	{
		if (!AppPaths.IsOverridden)
		{
			Task.Run(delegate
			{
				try
				{
					ProtocolRegistration.Register();
				}
				catch (Exception ex)
				{
					Log.Warn("startup", "protocol registration: " + ex.Message);
				}
			});
		}
		else
		{
			Log.Info("startup", "clicky:// and Open-with registration skipped: the data folder is moved (a test run)");
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			if (OverlayWindow.Offscreen)
			{
				Log.Info("startup", "offscreen run: cursor and highlight overlays skipped");
				return;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				CursorOverlay cursorOverlay = new CursorOverlay();
				cursorOverlay.SetColor(cursorColorHex);
				Cursor.Attach(cursorOverlay);
				if (showCursor)
				{
					Cursor.Show();
				}
				Highlight.Attach(new HighlightOverlay());
				Log.Info("startup", $"overlays ready in {stopwatch.ElapsedMilliseconds} ms");
			}
			catch (Exception ex2)
			{
				Log.Error("startup", "overlays failed", ex2);
			}
		});
		thread.IsBackground = true;
		thread.Name = "OverlayStartup";
		thread.Start();
		Core.StartAsync();
		if (Core.Auth.IsSignedIn)
		{
			Agents.PrewarmAsync();
		}
		if (!OverlayWindow.Offscreen)
		{
			Shortcuts.Start();
		}
	}

	public async ValueTask DisposeAsync()
	{
		Try(delegate
		{
			Shortcuts?.Dispose();
		}, "shortcuts");
		await TryAsync(Dictation, "dictation");
		await TryAsync(Companion, "companion");
		await TryAsync(Agents, "agents");
		Try(delegate
		{
			Canvas?.Dispose();
		}, "canvas");
		Try(delegate
		{
			Highlight?.Dispose();
		}, "highlight");
		Try(delegate
		{
			Cursor?.Dispose();
		}, "cursor");
		Try(delegate
		{
			Capture?.Dispose();
		}, "capture");
		Try(delegate
		{
			MicrophoneStartCue?.Dispose();
		}, "microphone start cue");
		Try(delegate
		{
			Audio?.Dispose();
		}, "audio");
		Try(delegate
		{
			Microphone?.Dispose();
		}, "microphone");
		Try(delegate
		{
			Permissions?.Dispose();
		}, "permissions");
		await TryAsync(Core, "core");
		static void Try(Action a, string what)
		{
			Log.Info("shutdown", "stopping " + what);
			try
			{
				a();
			}
			catch (Exception ex)
			{
				Log.Warn("shutdown", what + ": " + ex.Message);
			}
		}
		static async Task TryAsync(IAsyncDisposable? service, string what)
		{
			Log.Info("shutdown", "stopping " + what);
			try
			{
				if (service != null)
				{
					await service.DisposeAsync();
				}
			}
			catch (Exception ex2)
			{
				Log.Warn("shutdown", what + ": " + ex2.Message);
			}
		}
	}

	private void WireJobsAndHarness(CoreServices core, AgentService agents, CompanionSession companion, BrainContext brainContext, WasapiAudioOutput audio)
	{
		bool muted = Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1";
		JobReports = new OneBrainJobReports(agents, companion)
		{
			Speak = (string line) => companion.AnnounceAsync(line, core.Settings.Read((AppSettings s) => s.AgentCursorTextAnnouncementsEnabled) && !HeyClickyWindow.KeepsAgentLinesOffCursor),
			PlayDoneChime = delegate
			{
				if (!muted)
				{
					audio.PlayEarcon("agentDone", 0.7f);
				}
			}
		};
		AgentVoice.JobReports = JobReports;
		AgentToolHooks.ShowResult = JobReports.ShowResult;
		AgentToolHooks.Routines = async (string argumentsJson) => (await agents.RoutinesTool.RunAsync(argumentsJson).ConfigureAwait(continueOnCapturedContext: false)).ToJsonString();
		AgentToolHooks.JobIdForThread = agents.OneBrainJobId;
		agents.StepWatcher.FrontmostBrowserAddress = delegate
		{
			try
			{
				ForegroundApp result = ForegroundAppInfo.CaptureUserAppAsync().WaitAsync(TimeSpan.FromSeconds(2L)).GetAwaiter()
					.GetResult();
				return ((object)result != null && ForegroundAppInfo.IsBrowser(result.ProcessName)) ? result.BrowserUrl : null;
			}
			catch (Exception)
			{
				return (string?)null;
			}
		};
		brainContext.JobsNote = () => agents.WorkContextNoteForBrainTurn(ProactiveHooks.LiveCardIds());
		agents.HarnessTurnEnded += delegate(string harness, string place, string status, double seconds, IReadOnlyDictionary<string, double> stats)
		{
			ClickyAnalytics.TrackAgentHarnessTurn(harness, place, status, seconds, stats);
		};
		agents.InternalError += delegate(string subsystem, string stage, string detail, string key)
		{
			ClickyAnalytics.ReportInternalError(subsystem, stage, null, detail, CrashReportLevel.Error, key);
		};
		agents.JobAction += delegate(string _, string? threadId, IReadOnlyDictionary<string, object?> properties)
		{
			ClickyAnalytics.TrackProductEvent("job_action", null, threadId, Json(properties));
		};
	}

	private static JsonObject Json(IReadOnlyDictionary<string, object?> properties)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		foreach (KeyValuePair<string, object> property in properties)
		{
			property.Deconstruct(out var key, out var value);
			string text = key;
			object obj = value;
			JsonObject jsonObject2 = jsonObject;
			key = text;
			JsonValue value7 = ((obj == null) ? null : ((obj is bool value2) ? JsonValue.Create(value2, (JsonNodeOptions?)null) : ((obj is int value3) ? JsonValue.Create(value3, (JsonNodeOptions?)null) : ((obj is long value4) ? JsonValue.Create(value4, (JsonNodeOptions?)null) : ((obj is double value5) ? JsonValue.Create(value5, (JsonNodeOptions?)null) : ((!(obj is string value6)) ? JsonValue.Create(obj.ToString(), (JsonNodeOptions?)null) : JsonValue.Create(value6, (JsonNodeOptions?)null)))))));
			jsonObject2[key] = value7;
		}
		return jsonObject;
	}
}
