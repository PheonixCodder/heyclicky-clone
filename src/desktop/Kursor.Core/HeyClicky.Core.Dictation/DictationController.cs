using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Net;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Dictation;

public sealed class DictationController : IAsyncDisposable
{
	private sealed record WarmConnection(GeminiTranscriber Transcriber, string Language, string Words, string UsageContext, IDisposable Expiry);

	private sealed class ActorContext : SynchronizationContext, IDisposable
	{
		private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new BlockingCollection<(SendOrPostCallback, object)>();

		private readonly Thread thread;

		public ActorContext(string name)
		{
			thread = new Thread(Run)
			{
				IsBackground = true,
				Name = name
			};
			thread.Start();
		}

		private void Run()
		{
			SynchronizationContext.SetSynchronizationContext(this);
			foreach (var (sendOrPostCallback, state) in queue.GetConsumingEnumerable())
			{
				try
				{
					sendOrPostCallback(state);
				}
				catch (Exception ex)
				{
					Log.Error("Dictation", "dictation work item threw", ex);
				}
			}
		}

		public override void Post(SendOrPostCallback d, object? state)
		{
			try
			{
				queue.Add((d, state));
			}
			catch (InvalidOperationException)
			{
			}
		}

		public void Post(Action action)
		{
			Post(delegate
			{
				action();
			}, null);
		}

		public override void Send(SendOrPostCallback d, object? state)
		{
			throw new NotSupportedException();
		}

		public void Dispose()
		{
			queue.CompleteAdding();
		}
	}

	private sealed class ActorScheduler(ActorContext actor) : IDictationScheduler
	{
		private sealed class Handle : IDisposable
		{
			public Timer? Timer;

			public volatile bool Cancelled;

			public void Dispose()
			{
				Cancelled = true;
				Timer?.Dispose();
			}
		}

		public DateTimeOffset Now => DateTimeOffset.UtcNow;

		public IDisposable Schedule(TimeSpan delay, Action action)
		{
			Handle handle = new Handle();
			handle.Timer = new Timer(delegate
			{
				if (!handle.Cancelled)
				{
					actor.Post(delegate
					{
						if (!handle.Cancelled)
						{
							action();
						}
					});
				}
			}, null, delay, Timeout.InfiniteTimeSpan);
			return handle;
		}
	}

	private const string Area = "Dictation";

	private static readonly TimeSpan MicrophoneTail = TimeSpan.FromSeconds(0.15);

	private static readonly TimeSpan WarmConnectionWindow = TimeSpan.FromSeconds(120L);

	private static readonly TimeSpan FocusCaptureBudget = TimeSpan.FromMilliseconds(600L, 0L);

	private const string HandsFreeLabel = "Dictating (hands-free)";

	private readonly ClickyHttp http;

	private readonly SupabaseAuthService auth;

	private readonly SettingsStore settings;

	private readonly IMicrophoneCapture microphone;

	private readonly IDictationTextTarget target;

	private readonly NotchPresenter notch;

	private readonly DictationControllerOptions options;

	private readonly ActorContext actor;

	private readonly DictationHotkeyStateMachine hotkeys;

	private readonly object frameGate = new object();

	private bool isSessionActive;

	private int sessionToken;

	private DictationTranscriptionClient? client;

	private GeminiTranscriber? streamer;

	private Task setupTask = Task.CompletedTask;

	private DictationFocus? capturedFocus;

	private DictationWorkApp? keyDownWorkAppWithoutFocus;

	private string? streamingPartialBeforeFailure;

	private bool usedWarmConnection;

	private bool startedOnTutorialDictationStep;

	private long sessionStartedTicks;

	private long releaseTicks;

	private IDisposable? tailTimer;

	private string? currentLabel;

	private WarmConnection? warm;

	private CancellationTokenSource? correctionWatch;

	private volatile bool capturing;

	private volatile bool activeFlag;

	private int framesThisSession;

	private readonly Timer warmupTimer;

	public bool IsSessionActive => activeFlag;

	public bool IsCapturingAudio => capturing;

	public DictationEngagement Engagement => hotkeys.Engagement;

	public bool HasDedicatedHandsFreeShortcut
	{
		set
		{
			actor.Post(delegate
			{
				hotkeys.IsDedicatedHandsFreeShortcutConfigured = value;
			});
		}
	}

	public event Action<DictationResult>? Completed;

	public DictationController(ClickyHttp http, SupabaseAuthService auth, SettingsStore settings, IMicrophoneCapture microphone, IDictationTextTarget target, NotchPresenter notch, DictationControllerOptions? options = null)
	{
		DictationController dictationController = this;
		this.http = http;
		this.auth = auth;
		this.settings = settings;
		this.microphone = microphone;
		this.target = target;
		this.notch = notch;
		this.options = options ?? new DictationControllerOptions();
		actor = new ActorContext("HeyClicky dictation");
		hotkeys = new DictationHotkeyStateMachine(new ActorScheduler(actor), () => dictationController.isSessionActive, Begin, End, OnLockedHandsFree)
		{
			IsOnboardingTutorialActive = this.options.IsOnboardingTutorialRestricting
		};
		microphone.Frame += OnMicrophoneFrame;
		auth.Changed += delegate
		{
			if (!auth.IsSignedIn)
			{
				dictationController.actor.Post(dictationController.DiscardWarmConnection);
			}
		};
		warmupTimer = new Timer(delegate
		{
			dictationController.WarmUpNetworkConnections();
		}, null, TimeSpan.FromSeconds(5L), TimeSpan.FromSeconds(55L));
	}

	private void WarmUpNetworkConnections()
	{
		double? num = settings.Read((AppSettings s) => s.DictationLastCompletedAtEpochSeconds);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (!((double)DateTimeOffset.UtcNow.ToUnixTimeSeconds() - valueOrDefault > 604800.0) && auth.IsSignedIn)
			{
				http.PrewarmWorker();
				http.PrewarmGemini();
			}
		}
	}

	public void HandleShortcut(ShortcutAction action)
	{
		switch (action)
		{
		case ShortcutAction.DictationDown:
			actor.Post(delegate
			{
				if (!options.OnboardingTutorialAllowsPress())
				{
					Log.Info("Dictation", "press swallowed by the tour");
				}
				else
				{
					hotkeys.Pressed();
				}
			});
			break;
		case ShortcutAction.DictationUp:
			actor.Post(hotkeys.Released);
			break;
		case ShortcutAction.DictationCancel:
			actor.Post(delegate
			{
				if (isSessionActive && hotkeys.Engagement == DictationEngagement.Holding)
				{
					Log.Info("Dictation", "Ctrl+Win+key is a Windows shortcut: dictation cancelled");
					Cancel();
				}
			});
			break;
		case ShortcutAction.HandsFreeDictationToggle:
			actor.Post(hotkeys.ToggleHandsFree);
			break;
		case ShortcutAction.Escape:
			break;
		}
	}

	public bool CancelActiveSession()
	{
		if (!activeFlag)
		{
			return false;
		}
		actor.Post(delegate
		{
			if (isSessionActive)
			{
				options.PlayCue("textClose", 0.5f);
				Cancel();
			}
		});
		return true;
	}

	public void RemoveLearnedWord(string word)
	{
		DictationVocabulary.RemoveTerm(settings, word);
		notch.DismissLiveEvent(typeof(NotchLiveEvent.DictionaryWordLearned));
		Log.Info("Dictation", "learned word removed: " + word);
	}

	private void Begin()
	{
		if (!isSessionActive)
		{
			if (!auth.IsSignedIn)
			{
				PresentUserVisibleError("Please sign in to use dictation.");
			}
			else if (options.IsMicrophoneDenied())
			{
				PresentUserVisibleError("Microphone access is needed for dictation.");
			}
			else if (!options.CanStartRecording())
			{
				Log.Info("Dictation", "press ignored: the microphone is busy with push-to-talk");
			}
			else
			{
				StartSession();
			}
		}
	}

	private void StartSession()
	{
		correctionWatch?.Cancel();
		correctionWatch = null;
		isSessionActive = (activeFlag = true);
		int token = ++sessionToken;
		streamingPartialBeforeFailure = null;
		usedWarmConnection = false;
		startedOnTutorialDictationStep = options.IsOnboardingTutorialDictationStepActive();
		capturedFocus = null;
		keyDownWorkAppWithoutFocus = null;
		framesThisSession = 0;
		sessionStartedTicks = Environment.TickCount64;
		releaseTicks = 0L;
		(options.PlayMicrophoneStartCue ?? options.PlayCue)("textOpen", 0.35f);
		SetLabel("Dictating", recording: true);
		http.PrewarmWorker();
		AppSettings current = settings.Current;
		string text = (string.IsNullOrWhiteSpace(current.DictationLanguageCode) ? "multi" : current.DictationLanguageCode.Trim());
		IReadOnlyList<string> readOnlyList = DictationVocabulary.Terms(current.DictationCustomWords);
		string usageContext = options.UsageContext();
		DictationTranscriptionClient newClient = new DictationTranscriptionClient(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), usageContext, text, microphone.SampleRate)
		{
			KnownTerms = readOnlyList
		};
		GeminiTranscriber geminiTranscriber = TakeWarmConnection(text, readOnlyList, usageContext);
		lock (frameGate)
		{
			client = newClient;
			streamer = geminiTranscriber;
		}
		usedWarmConnection = geminiTranscriber != null;
		try
		{
			capturing = true;
			microphone.Start();
		}
		catch (Exception ex)
		{
			Log.Error("Dictation", "microphone did not start", ex);
			HandleSessionError("Couldn't start the microphone.");
			return;
		}
		Log.Info("Dictation", $"session started ({((geminiTranscriber == null) ? "cold" : "warm socket")}, language {text}, {readOnlyList.Count} custom words)");
		setupTask = SetUpSessionAsync(token, newClient, geminiTranscriber, text, readOnlyList, usageContext);
	}

	private async Task SetUpSessionAsync(int token, DictationTranscriptionClient newClient, GeminiTranscriber? adopted, string language, IReadOnlyList<string> customWords, string usageContext)
	{
		Task<DictationFocus?> focusTask = target.CaptureFocusAsync();
		DictationFocus dictationFocus = ((await Task.WhenAny(focusTask, Task.Delay(FocusCaptureBudget)) != focusTask) ? null : (await focusTask));
		DictationFocus dictationFocus2 = dictationFocus;
		if (token != sessionToken)
		{
			return;
		}
		capturedFocus = dictationFocus2;
		keyDownWorkAppWithoutFocus = (((object)dictationFocus2 == null) ? target.ForegroundWorkApp() : null);
		newClient.Context = (((object)dictationFocus2 != null) ? new DictationCleanupContext(dictationFocus2.AppName, dictationFocus2.SiteHost, dictationFocus2.BeforeText, dictationFocus2.SelectedText, dictationFocus2.AfterText, dictationFocus2.IsSingleLine) : new DictationCleanupContext(keyDownWorkAppWithoutFocus?.AppName ?? "", keyDownWorkAppWithoutFocus?.SiteHost, "", "", "", SingleLine: false));
		IReadOnlyList<string> readOnlyList;
		if ((object)dictationFocus2 != null)
		{
			global::_003C_003Ey__InlineArray3<string> buffer = default(global::_003C_003Ey__InlineArray3<string>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 0) = dictationFocus2.BeforeText;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 1) = dictationFocus2.SelectedText;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 2) = dictationFocus2.AfterText;
			readOnlyList = DictationVocabulary.ContextKeyterms(string.Join(" ", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<string>, string>(in buffer, 3)));
		}
		else
		{
			IReadOnlyList<string> readOnlyList2 = Array.Empty<string>();
			readOnlyList = readOnlyList2;
		}
		IReadOnlyList<string> readOnlyList3 = readOnlyList;
		List<string> list = customWords.Concat(readOnlyList3).Distinct<string>(StringComparer.OrdinalIgnoreCase).Take(50)
			.ToList();
		Log.Info("Dictation", ((object)dictationFocus2 == null) ? ("focus: no editable field (app " + (keyDownWorkAppWithoutFocus?.AppName ?? "?") + ((keyDownWorkAppWithoutFocus?.AllowsBlindPaste ?? false) ? ", blind paste allowed" : "") + ")") : $"focus: {dictationFocus2.Role} in {dictationFocus2.AppName}{(dictationFocus2.IsSecure ? " (password field)" : "")}{(dictationFocus2.IsSingleLine ? ", single line" : "")}, {dictationFocus2.BeforeText.Length} chars before, {readOnlyList3.Count} context keyterms");
		newClient.KnownTerms = list;
		GeminiTranscriber transcriber;
		if (adopted != null)
		{
			transcriber = adopted;
		}
		else
		{
			newClient.KnownTerms = list;
			transcriber = NewStreamingTranscriber(language, list, usageContext);
			transcriber.Start();
			lock (frameGate)
			{
				transcriber.PushAudio(newClient.AccumulatedAudio());
				streamer = transcriber;
			}
		}
		transcriber.Opened.ContinueWith(delegate
		{
			if (transcriber.PaywallReached)
			{
				actor.Post(delegate
				{
					if (token == sessionToken)
					{
						HandlePaywallReached();
					}
				});
			}
		}, TaskScheduler.Default);
	}

	private void OnLockedHandsFree()
	{
		SetLabel("Dictating (hands-free)", recording: true);
		options.PlayCue("textOpen", 0.22f);
		Log.Info("Dictation", "hands-free (double-tap)");
	}

	private void End()
	{
		if (!isSessionActive)
		{
			return;
		}
		options.PlayCue("textReceive", 0.6f);
		SetLabel("Transcribing");
		releaseTicks = Environment.TickCount64;
		int token = sessionToken;
		tailTimer?.Dispose();
		tailTimer = new ActorScheduler(actor).Schedule(MicrophoneTail, delegate
		{
			if (token == sessionToken)
			{
				StopMicrophone();
				if (releaseTicks - sessionStartedTicks > 1500 && Volatile.Read(in framesThisSession) == 0)
				{
					notch.ShowLiveEvent(new NotchLiveEvent.MicrophoneTrouble("I can't hear your microphone. Is it muted or unplugged?"), null);
				}
				FinishAsync(token);
			}
		});
	}

	private async Task FinishAsync(int token)
	{
		_ = 6;
		try
		{
			await setupTask;
			if (token != sessionToken)
			{
				return;
			}
			GeminiTranscriber transcriber = streamer;
			DictationTranscriptionClient batchClient = client;
			if (batchClient == null)
			{
				return;
			}
			if (transcriber != null && transcriber.PaywallReached)
			{
				HandlePaywallReached();
				return;
			}
			if (transcriber == null || transcriber.DidFail)
			{
				streamingPartialBeforeFailure = transcriber?.PartialTranscriptSoFar();
				await RunBatchAsync(token, batchClient, transcriber?.FailureMessage);
				return;
			}
			string text = await transcriber.FinishAsync(null);
			if (token != sessionToken)
			{
				return;
			}
			if (transcriber.PaywallReached)
			{
				HandlePaywallReached();
				return;
			}
			if (transcriber.DidFail)
			{
				streamingPartialBeforeFailure = transcriber.PartialTranscriptSoFar();
				await RunBatchAsync(token, batchClient, transcriber.FailureMessage);
				return;
			}
			int transcriptWaitMs = (int)(Environment.TickCount64 - releaseTicks);
			string text2 = (text ?? "").Trim();
			double sentAudioSeconds = transcriber.SentAudioSeconds;
			batchClient.Cancel();
			if (text2.Length == 0)
			{
				await DeliverAsync(token, "", usedBatch: false, transcriptWaitMs, null);
				return;
			}
			string finalText = text2;
			int? cleanupMs = null;
			long cleanupStarted = Environment.TickCount64;
			try
			{
				finalText = await batchClient.CleanUpAsync(text2, sentAudioSeconds, transcriber.Receipt, useFastCleanupLane: true, transcriptWaitMs, "gemini");
				cleanupMs = (int)(Environment.TickCount64 - cleanupStarted);
			}
			catch (DictationPaywallReachedException)
			{
				if (token == sessionToken)
				{
					HandlePaywallReached();
				}
				return;
			}
			catch (Exception ex2)
			{
				Log.Warn("Dictation", "cleanup failed, inserting the raw transcript: " + ex2.Message);
			}
			await DeliverAsync(token, finalText, usedBatch: false, transcriptWaitMs, cleanupMs);
		}
		catch (Exception ex3)
		{
			Log.Error("Dictation", "finish failed", ex3);
			if (token == sessionToken)
			{
				HandleSessionError("Dictation failed. Try again");
			}
		}
	}

	private async Task RunBatchAsync(int token, DictationTranscriptionClient batchClient, string? why)
	{
		Log.Info("Dictation", "streaming unavailable (" + (why ?? "no socket") + "): batch transcription");
		try
		{
			string rawText = (await batchClient.TranscribeAsync()).Item1;
			if (token == sessionToken)
			{
				int transcriptWaitMs = (int)(Environment.TickCount64 - releaseTicks);
				string trimmedRaw = (rawText ?? "").Trim();
				if (trimmedRaw.Length == 0)
				{
					await DeliverAsync(token, "", usedBatch: true, transcriptWaitMs, null);
					return;
				}
				string finalText = trimmedRaw;
				int? cleanupMs = null;
				long cleanupStarted = Environment.TickCount64;
				try
				{
					DictationCleanupContext focusContext = batchClient.Context;
					string cleaned = await batchClient.CleanUpAsync(trimmedRaw, focusContext, null, default(CancellationToken));
					if (!string.IsNullOrWhiteSpace(cleaned))
					{
						finalText = cleaned;
					}
					cleanupMs = (int)(Environment.TickCount64 - cleanupStarted);
				}
				catch (DictationPaywallReachedException)
				{
					if (token == sessionToken)
					{
						HandlePaywallReached();
					}
					return;
				}
				catch (Exception exClean)
				{
					Log.Warn("Dictation", "batch cleanup failed, inserting the raw transcript: " + exClean.Message);
				}
				await DeliverAsync(token, finalText, usedBatch: true, transcriptWaitMs, cleanupMs);
			}
		}
		catch (DictationPaywallReachedException)
		{
			if (token == sessionToken)
			{
				HandlePaywallReached();
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("Dictation", "batch transcription failed: " + ex2.Message);
			if (token == sessionToken)
			{
				bool flag = ((ex2 is HttpRequestException || ex2 is TaskCanceledException) ? true : false);
				HandleSessionError(flag ? "Couldn't reach HeyClicky's servers." : ex2.Message);
			}
		}
	}

	private async Task DeliverAsync(int token, string text, bool usedBatch, int transcriptWaitMs, int? cleanupMs)
	{
		if (token != sessionToken)
		{
			return;
		}
		DictationFocus focus = capturedFocus;
		DictationWorkApp workApp = keyDownWorkAppWithoutFocus;
		bool handsFree = hotkeys.SessionUsedHandsFree;
		long releasedAt = releaseTicks;
		bool flag = startedOnTutorialDictationStep;
		startedOnTutorialDictationStep = false;
		FinishSessionState();
		string transcript = text.Trim();
		if (transcript.Length == 0)
		{
			Log.Info("Dictation", "nothing dictated");
			SetLabel(null);
			this.Completed?.Invoke(new DictationResult("", DictationDeliveryOutcome.Nothing, null, usedBatch, handsFree, usedWarmConnection, 0));
			return;
		}
		if ((object)focus == null || !focus.IsSecure)
		{
			DictationTranscriptArchive.AppendFinishedTranscript(transcript, focus?.AppName ?? workApp?.AppName);
		}
		settings.Update(delegate(AppSettings s)
		{
			s.DictationLastCompletedAtEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		});
		string clipboardReason = null;
		bool lateRecapture = false;
		if (options.ConsumeOnboardingTutorialTranscript(transcript))
		{
			Log.Info("Dictation", $"transcript delivered to the tour's box ({transcript.Length} chars)");
			options.PlayCue("textSend", 0.5f);
			SetLabel(null);
			this.Completed?.Invoke(new DictationResult(transcript, DictationDeliveryOutcome.Inserted, null, usedBatch, handsFree, usedWarmConnection, (int)((releasedAt > 0) ? (Environment.TickCount64 - releasedAt) : 0)));
			if (!usedBatch)
			{
				PrepareWarmConnection();
			}
			return;
		}
		if (flag)
		{
			Log.Info("Dictation", "dropping a tour-step transcript delivered after the step was left");
			SetLabel(null);
			return;
		}
		if ((object)focus == null)
		{
			DictationFocus dictationFocus = await target.CaptureFocusAsync();
			if ((object)dictationFocus != null && (object)workApp != null && dictationFocus.ProcessId != workApp.ProcessId)
			{
				Log.Info("Dictation", "late re-capture found a field in a different app; keeping the clipboard fallback");
			}
			else
			{
				focus = dictationFocus;
			}
			lateRecapture = (object)focus != null;
		}
		DictationDeliveryOutcome dictationDeliveryOutcome;
		if ((object)focus != null && focus.IsEditableInsertionTarget)
		{
			string verificationAppName = focus.AppName;
			string verificationProcessName = ProcessNameOf(focus.ProcessId);
			string text2;
			switch (await target.InsertAsync(transcript, focus, delegate(string verification, int waitMs)
			{
				ClickyAnalytics.TrackDictationPasteVerification(verification, waitMs, verificationAppName, verificationProcessName);
			}))
			{
			case DictationInsertOutcome.Inserted:
				options.PlayCue("textSend", 0.5f);
				SetLabel(null);
				dictationDeliveryOutcome = DictationDeliveryOutcome.Inserted;
				StartCorrectionLearning(focus, transcript);
				break;
			case DictationInsertOutcome.RefusedUnsafeTarget:
				text2 = "secure_field";
				goto IL_0467;
			case DictationInsertOutcome.FocusLost:
				text2 = "focus_lost";
				goto IL_0467;
			case DictationInsertOutcome.ModifiersHeld:
				text2 = "modifiers_held";
				goto IL_0467;
			case DictationInsertOutcome.ClipboardWriteFailed:
				text2 = "pasteboard_write_failed";
				goto IL_0467;
			default:
				{
					text2 = "empty_transcript";
					goto IL_0467;
				}
				IL_0467:
				clipboardReason = text2;
				await CopyTranscriptToClipboardAsync(transcript);
				dictationDeliveryOutcome = DictationDeliveryOutcome.Clipboard;
				break;
			}
		}
		else
		{
			bool flag2 = (object)focus == null && (workApp?.AllowsBlindPaste ?? false);
			if (flag2)
			{
				flag2 = await target.BlindPasteAsync(transcript, workApp);
			}
			if (flag2)
			{
				options.PlayCue("textSend", 0.5f);
				SetLabel(null);
				dictationDeliveryOutcome = DictationDeliveryOutcome.PastedBlind;
			}
			else
			{
				clipboardReason = (((object)focus == null) ? "no_focus_target" : "secure_field");
				await CopyTranscriptToClipboardAsync(transcript);
				dictationDeliveryOutcome = DictationDeliveryOutcome.Clipboard;
			}
		}
		int num = (int)((releasedAt > 0) ? (Environment.TickCount64 - releasedAt) : 0);
		Log.Info("Dictation", $"dictation completed: {dictationDeliveryOutcome}{((clipboardReason == null) ? "" : (" (" + clipboardReason + ")"))}, {transcript.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length} words, engine {(usedBatch ? "batch" : "gemini")}{(usedWarmConnection ? " (warm)" : "")}, hands-free {handsFree}, late re-capture {lateRecapture}, transcript {transcriptWaitMs} ms, cleanup {cleanupMs?.ToString() ?? "-"} ms, release to insert {num} ms");
		this.Completed?.Invoke(new DictationResult(transcript, dictationDeliveryOutcome, clipboardReason, usedBatch, handsFree, usedWarmConnection, num));
		if (!usedBatch)
		{
			PrepareWarmConnection();
		}
	}

	private static string? ProcessNameOf(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			return process.ProcessName;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private async Task CopyTranscriptToClipboardAsync(string text)
	{
		if (!(await target.CopyToClipboardAsync(text)))
		{
			Log.Warn("Dictation", "clipboard write failed");
		}
		notch.ShowLiveEvent(new NotchLiveEvent.DictationCopiedToClipboard(), null);
		SetLabel(null);
	}

	private void HandlePaywallReached()
	{
		if (isSessionActive)
		{
			Log.Info("Dictation", "session ended quietly: dictation word limit (402)");
			options.PlayCue("textClose", 0.5f);
			FinishSessionState();
			SetLabel(null);
		}
	}

	private void HandleSessionError(string message)
	{
		string text = (streamer?.PartialTranscriptSoFar() ?? streamingPartialBeforeFailure ?? "").Trim();
		string targetAppName = capturedFocus?.AppName ?? keyDownWorkAppWithoutFocus?.AppName;
		FinishSessionState();
		if (text.Length == 0)
		{
			PresentUserVisibleError(message);
			return;
		}
		Log.Info("Dictation", $"session failed ({message}); salvaged {text.Length} chars to the clipboard");
		DictationTranscriptArchive.AppendFinishedTranscript(text, targetAppName);
		CopyTranscriptToClipboardAsync(text);
	}

	private void PresentUserVisibleError(string message)
	{
		Log.Warn("Dictation", "error: " + message);
		ClickyAnalytics.TrackAppError("dictation", "generic", message, null, 0, message, relayToWorker: false);
		string label = ((message.Length <= 64) ? message : "Dictation failed. Try again");
		SetLabel(label);
		new ActorScheduler(actor).Schedule(TimeSpan.FromSeconds(2.5), delegate
		{
			if (currentLabel == label)
			{
				SetLabel(null);
			}
		});
	}

	private void Cancel()
	{
		sessionToken++;
		tailTimer?.Dispose();
		tailTimer = null;
		StopMicrophone();
		DisposeSessionTranscribers();
		capturedFocus = null;
		keyDownWorkAppWithoutFocus = null;
		isSessionActive = (activeFlag = false);
		hotkeys.Reset();
		SetLabel(null);
		Log.Info("Dictation", "session cancelled");
	}

	private void FinishSessionState()
	{
		sessionToken++;
		tailTimer?.Dispose();
		tailTimer = null;
		StopMicrophone();
		DisposeSessionTranscribers();
		streamingPartialBeforeFailure = null;
		capturedFocus = null;
		keyDownWorkAppWithoutFocus = null;
		isSessionActive = (activeFlag = false);
		hotkeys.Reset();
	}

	private void DisposeSessionTranscribers()
	{
		GeminiTranscriber geminiTranscriber;
		DictationTranscriptionClient dictationTranscriptionClient;
		lock (frameGate)
		{
			geminiTranscriber = streamer;
			dictationTranscriptionClient = client;
			streamer = null;
			client = null;
		}
		dictationTranscriptionClient?.Cancel();
		geminiTranscriber?.DisposeAsync().AsTask();
	}

	private void StopMicrophone()
	{
		if (!capturing)
		{
			return;
		}
		capturing = false;
		try
		{
			microphone.Stop();
		}
		catch (Exception ex)
		{
			Log.Warn("Dictation", "microphone stop: " + ex.Message);
		}
	}

	private void SetLabel(string? label, bool recording = false)
	{
		currentLabel = label;
		if (label == null)
		{
			notch.DismissLiveEvent(typeof(NotchLiveEvent.Dictation));
			return;
		}
		if (recording)
		{
			notch.DismissLiveEvent(typeof(NotchLiveEvent.DictationCopiedToClipboard));
		}
		notch.ShowLiveEvent(new NotchLiveEvent.Dictation(label, recording), null);
	}

	private void OnMicrophoneFrame(ReadOnlyMemory<byte> frame)
	{
		if (!capturing)
		{
			return;
		}
		Interlocked.Increment(ref framesThisSession);
		lock (frameGate)
		{
			client?.AppendAudio(frame.Span);
			streamer?.PushAudio(frame.Span);
		}
	}

	private static string WordsKey(IReadOnlyList<string> words)
	{
		return string.Join("\n", words);
	}

	private GeminiTranscriber NewStreamingTranscriber(string language, IReadOnlyList<string> keyterms, string usageContext)
	{
		return new GeminiTranscriber(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), language, keyterms, usageContext, microphone.SampleRate, "/v2/dictation/gemini-token");
	}

	private GeminiTranscriber? TakeWarmConnection(string language, IReadOnlyList<string> customWords, string usageContext)
	{
		WarmConnection warmConnection = warm;
		if ((object)warmConnection == null)
		{
			return null;
		}
		warm = null;
		warmConnection.Expiry.Dispose();
		if (warmConnection.Language == language && warmConnection.Words == WordsKey(customWords) && warmConnection.UsageContext == usageContext && warmConnection.Transcriber.SampleRate == microphone.SampleRate && warmConnection.Transcriber.Adopt())
		{
			return warmConnection.Transcriber;
		}
		warmConnection.Transcriber.DisposeAsync().AsTask();
		return null;
	}

	private void PrepareWarmConnection()
	{
		DiscardWarmConnection();
		if (!auth.IsSignedIn)
		{
			return;
		}
		AppSettings current = settings.Current;
		string language = (string.IsNullOrWhiteSpace(current.DictationLanguageCode) ? "multi" : current.DictationLanguageCode.Trim());
		IReadOnlyList<string> readOnlyList = DictationVocabulary.Terms(current.DictationCustomWords);
		string usageContext = options.UsageContext();
		GeminiTranscriber geminiTranscriber = NewStreamingTranscriber(language, readOnlyList, usageContext);
		WarmConnection connection = null;
		IDisposable expiry = new ActorScheduler(actor).Schedule(WarmConnectionWindow, delegate
		{
			if (warm == connection)
			{
				DiscardWarmConnection();
			}
		});
		connection = new WarmConnection(geminiTranscriber, language, WordsKey(readOnlyList), usageContext, expiry);
		warm = connection;
		geminiTranscriber.Start(asWarmSocket: true);
	}

	private void DiscardWarmConnection()
	{
		WarmConnection warmConnection = warm;
		if ((object)warmConnection != null)
		{
			warm = null;
			warmConnection.Expiry.Dispose();
			warmConnection.Transcriber.DisposeAsync().AsTask();
		}
	}

	private void StartCorrectionLearning(DictationFocus focus, string insertedText)
	{
		correctionWatch?.Cancel();
		WatchForCorrectionAsync(focus, insertedText, (correctionWatch = new CancellationTokenSource()).Token);
	}

	private async Task WatchForCorrectionAsync(DictationFocus focus, string insertedText, CancellationToken ct)
	{
		_ = 3;
		try
		{
			await Task.Delay(DictationCorrectionDetector.BaselineDelay, ct);
			string baseline = await target.ReadFieldValueAsync(focus);
			if (baseline == null || baseline.Length > 20000)
			{
				return;
			}
			long deadline = Environment.TickCount64 + (long)DictationCorrectionDetector.ObservationWindow.TotalMilliseconds;
			string lastSeen = baseline;
			while (Environment.TickCount64 < deadline)
			{
				await Task.Delay(DictationCorrectionDetector.PollInterval, ct);
				string text = await target.ReadFieldValueAsync(focus);
				if (ct.IsCancellationRequested || text == null)
				{
					break;
				}
				if (text == lastSeen)
				{
					continue;
				}
				lastSeen = text;
				DictationLearnedCorrection dictationLearnedCorrection = DictationCorrectionDetector.DetectSingleWordCorrection(baseline, text, insertedText);
				if ((object)dictationLearnedCorrection == null)
				{
					continue;
				}
				if (DictationVocabulary.AddTerm(settings, dictationLearnedCorrection.Corrected))
				{
					Log.Info("Dictation", $"learned \"{dictationLearnedCorrection.Corrected}\" (was \"{dictationLearnedCorrection.Original}\")");
					if ((object)warm != null)
					{
						PrepareWarmConnection();
					}
					options.PlayCue("textReceive", 0.35f);
					notch.ShowLiveEvent(new NotchLiveEvent.DictionaryWordLearned(dictationLearnedCorrection.Corrected), null);
				}
				break;
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Log.Warn("Dictation", "correction watch: " + ex2.Message);
		}
	}

	public async ValueTask DisposeAsync()
	{
		microphone.Frame -= OnMicrophoneFrame;
		await warmupTimer.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		TaskCompletionSource done = new TaskCompletionSource();
		actor.Post(delegate
		{
			if (isSessionActive)
			{
				Cancel();
			}
			DiscardWarmConnection();
			correctionWatch?.Cancel();
			done.TrySetResult();
		});
		await Task.WhenAny(done.Task, Task.Delay(1000)).ConfigureAwait(continueOnCapturedContext: false);
		actor.Dispose();
	}
}
