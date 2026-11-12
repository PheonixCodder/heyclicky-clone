using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Identity;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Telemetry;

public static class ClickyAnalytics
{
	public static class AppErrorKind
	{
		public const string CreditsExhausted = "credits_exhausted";

		public const string Generic = "generic";
	}

	public sealed record RecentAppError(string TimestampIso8601, string Stage, string Kind, string ErrorMessage, string? ErrorDomain, int ErrorCode, string UserFacingMessage);

	public static class ProductEvent
	{
		public const string DestinationViewed = "home_space_destination_viewed";

		public const string PresentationChanged = "home_space_presentation_changed";

		public const string ComposerModeChanged = "home_space_composer_mode_changed";

		public const string AttachmentsAdded = "home_space_attachments_added";

		public const string AttachmentRemoved = "home_space_attachment_removed";

		public const string ClickyCreated = "clicky_created";

		public const string ClickyArchived = "clicky_archived";

		public const string ClickyRestored = "clicky_restored";

		public const string ClickyPinned = "clicky_pin_changed";

		public const string ProfileViewed = "clicky_profile_viewed";

		public const string CharacterEditorOpened = "clicky_character_editor_opened";

		public const string CharacterEditorClosed = "clicky_character_editor_closed";

		public const string CharacterSaved = "clicky_character_saved";

		public const string CharacterRandomized = "clicky_character_randomized";

		public const string CharacterPackSelected = "clicky_character_pack_selected";

		public const string FirstAskSelected = "clicky_first_ask_selected";

		public const string ArtifactAction = "agent_artifact_action";

		public const string HudAction = "agent_hud_action";

		public const string ConversationAction = "home_space_conversation_action";

		public const string TaskControl = "agent_task_control_clicked";

		public const string RoutineCreated = "clicky_routine_created";

		public const string RoutineUpdated = "clicky_routine_updated";

		public const string RoutinePauseChanged = "clicky_routine_pause_changed";

		public const string RoutineDeleted = "clicky_routine_deleted";

		public const string RoutineRunRequested = "clicky_routine_run_requested";

		public const string RoutineRunDispatched = "clicky_routine_run_dispatched";

		public const string RoutineRunEnded = "clicky_routine_run_ended";

		public const string InterviewAnswerSubmitted = "clicky_interview_answer_submitted";

		public const string InterviewSkippedByVoice = "onboarding_interview_skipped_by_voice";

		public const string StarterPlanStarted = "onboarding_starter_plan_started";

		public const string StarterPlanFinished = "onboarding_starter_plan_finished";

		public const string OnboardingRevealStarted = "onboarding_reveal_started";

		public const string OnboardingRevealEnded = "onboarding_reveal_ended";

		public const string SuggestionViewed = "home_space_suggestion_viewed";

		public const string SuggestionAction = "home_space_suggestion_action";

		public const string MorningInvitationAction = "morning_suggestions_invitation_action";

		public const string MorningInvitationHeldBack = "morning_suggestions_invitation_held_back";

		public const string SettingChanged = "setting_changed";

		public const string ShortcutHelpViewed = "shortcut_help_viewed";

		public const string ConnectorAction = "custom_connector_action";

		public const string NotchPeekAction = "notch_peek_action";

		public const string DashboardAction = "clicky_dashboard_action";

		public const string JobAction = "job_action";

		public const string ArcDockAction = "arc_dock_action";

		public const string NotchUpdateCardAction = "notch_update_card_action";

		public const string ClickyDriveAction = "clicky_drive_action";

		public const string BrowserConnectionAction = "browser_connection_action";

		public const string OnboardingFinaleAction = "onboarding_finale_action";

		public const string OnboardingDriveStepAction = "onboarding_drive_step_action";

		public const string MorningBriefAction = "morning_brief_action";

		public const string NoteObjectAction = "note_object_action";

		public const string JobReportAction = "job_report_action";

		public const string NotchChatAction = "notch_chat_action";

		public const string AgentRequestAnswered = "agent_request_answered";
	}

	public sealed record SuggestionCardInfo(string SuggestionId, string Slot, bool CreatesClicky, bool HasRoutine, string? ClickySlug);

	public const string JobActionEvent = "job_action";

	private static readonly object Gate = new object();

	private static readonly JsonObject SuperProperties = new JsonObject((JsonNodeOptions?)null);

	private static IAnalyticsSink sink = NullAnalyticsSink.Instance;

	private static string anonymousId = Guid.NewGuid().ToString();

	private static string? identifiedId;

	private static string identityFilePath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "analytics-identity.json");

	private static string appVersion = "unknown";

	private static string appBuild = "0";

	private static ClientIdentity? clientIdentity;

	private static readonly object RecentAppErrorsGate = new object();

	private static readonly List<RecentAppError> RecentAppErrorsStorage = new List<RecentAppError>();

	private const int MaxRecentAppErrorCount = 25;

	private static readonly TimeSpan ThrottleWindow = TimeSpan.FromSeconds(30L);

	private static readonly Dictionary<string, DateTimeOffset> LastCapturedAt = new Dictionary<string, DateTimeOffset>();

	private static string? lastSyncedDiscoveryChannel;

	private static (string Plan, string PlanName, bool IsPaid)? lastSyncedPlan;

	private static string? pendingReferralPageEntryPoint;

	private static readonly (PermissionKind Kind, string Name)[] PermissionSteps = new(PermissionKind, string)[2]
	{
		(PermissionKind.Microphone, "microphone"),
		(PermissionKind.ScreenCapture, "screen_recording")
	};

	internal static string CheckoutContextPath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "analytics-checkout-context.json");

	private static readonly TimeSpan CheckoutContextLifetime = TimeSpan.FromHours(48);

	private static readonly HashSet<string> GoogleIntegrationIds = new HashSet<string> { "gmail", "googlesheets", "googlecalendar", "googledrive", "googledocs" };

	public static IAnalyticsSink Sink => sink;

	public static bool IsSendingToPostHog => sink is PostHogAnalyticsSink;

	public static string DistinctId
	{
		get
		{
			lock (Gate)
			{
				return identifiedId ?? anonymousId;
			}
		}
	}

	public static string AppVersion => appVersion;

	public static Func<JsonObject?>? FrontmostApplicationProperties { get; set; }

	public static Func<(string? UserId, string? Email)>? CurrentAccount { get; set; }

	public static Action<ErrorReport>? ErrorRelay { get; set; }

	public static void TrackRealtimeAudioOutputRouteChange(string fromTransport, string toTransport, string fromDevice, string toDevice, int droppedBuffers, bool engineWasRunning, bool restartSucceeded)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["from_transport"] = fromTransport,
			["to_transport"] = toTransport,
			["transport_switched"] = fromTransport != toTransport,
			["from_device"] = fromDevice,
			["to_device"] = toDevice,
			["device_switched"] = fromDevice != toDevice,
			["dropped_buffers"] = droppedBuffers,
			["engine_was_running"] = engineWasRunning,
			["restart_succeeded"] = restartSucceeded
		};
		Breadcrumb("audio", "realtime_output_route_changed", restartSucceeded ? CrashReportLevel.Info : CrashReportLevel.Warning, (JsonObject)jsonObject.DeepClone());
		Capture("realtime_audio_route_changed", jsonObject);
	}

	public static void ReportRealtimeAudioRouteRebuildFailed(string transport, string device, int droppedBuffers)
	{
		Capture("realtime_audio_route_rebuild_failed", new JsonObject((JsonNodeOptions?)null)
		{
			["transport"] = transport,
			["device"] = device,
			["dropped_buffers"] = droppedBuffers
		});
		ICrashReporter current = CrashReporter.Current;
		if (!current.IsEnabled || !ShouldEmitCrashReporterEvent("realtime-audio-route-rebuild-failed"))
		{
			return;
		}
		try
		{
			current.CaptureMessage("Realtime voice engine failed to recover from an output route change", CrashReportLevel.Warning, new _003C_003Ez__ReadOnlySingleElementList<string>("realtime-audio-route-rebuild-failed"), new Dictionary<string, string>
			{
				["issue"] = "realtime_audio_rebuild_failed",
				["output_transport"] = transport,
				["output_device"] = device
			}, "realtime_audio_route", new JsonObject((JsonNodeOptions?)null)
			{
				["transport"] = transport,
				["device"] = device,
				["dropped_buffers"] = droppedBuffers
			});
		}
		catch (Exception ex)
		{
			Log.Warn("audio", "crash reporter capture failed: " + ex.Message);
		}
	}

	public static void TrackAuthCallbackRefused(string reason, bool wasSignedIn)
	{
		Capture("auth_callback_refused", new JsonObject((JsonNodeOptions?)null)
		{
			["reason"] = reason,
			["was_signed_in"] = wasSignedIn
		});
	}

	public static void TrackChromeJobAction(string action, string? threadId = null, JsonObject? properties = null)
	{
		JsonObject jsonObject = properties?.DeepClone().AsObject() ?? new JsonObject((JsonNodeOptions?)null);
		jsonObject["action"] = action;
		TrackProductEvent("job_action", null, threadId, jsonObject);
	}

	public static void Configure(TelemetryConfiguration config, ClickyEndpoints endpoints, ClientIdentity identity, HttpClient? http = null, IAnalyticsSink? overrideSink = null, string? identityFile = null)
	{
		if (identityFile != null)
		{
			identityFilePath = identityFile;
		}
		appVersion = identity.AppVersion;
		appBuild = identity.AppBuild;
		IAnalyticsSink analyticsSink;
		string value;
		if (overrideSink != null)
		{
			analyticsSink = overrideSink;
			value = "given";
		}
		else if (Environment.GetEnvironmentVariable("HEYCLICKY_ANALYTICS_DEBUG") == "1")
		{
			analyticsSink = new LogAnalyticsSink();
			value = "HEYCLICKY_ANALYTICS_DEBUG=1";
		}
		else if (!config.HasPostHogKey)
		{
			analyticsSink = NullAnalyticsSink.Instance;
			value = "no PostHog key configured";
		}
		else if (AppBundleConfiguration.IsLocalDevelopmentConfiguration(endpoints))
		{
			analyticsSink = NullAnalyticsSink.Instance;
			value = "loopback endpoints";
		}
		else
		{
			analyticsSink = new PostHogAnalyticsSink(http ?? new HttpClient(), config.PostHogHost, config.PostHogApiKey);
			value = config.PostHogHost;
		}
		lock (Gate)
		{
			if (sink is IDisposable disposable && disposable != analyticsSink)
			{
				disposable.Dispose();
			}
			sink = analyticsSink;
			LoadIdentity();
		}
		RegisterClientIdentitySuperProperties(identity);
		Log.Info("analytics", $"sink: {analyticsSink.Name} ({value})");
	}

	private static void RegisterClientIdentitySuperProperties(ClientIdentity? identity = null)
	{
		if ((object)identity == null)
		{
			identity = clientIdentity ?? ClickyClientIdentity.Current;
		}
		clientIdentity = identity;
		Register(new JsonObject((JsonNodeOptions?)null)
		{
			["clicky_device_key"] = identity.DeviceId,
			["clicky_platform"] = identity.Platform
		});
	}

	public static void Capture(string eventName, JsonObject? properties = null, JsonObject? set = null, JsonObject? setOnce = null)
	{
		try
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
			IAnalyticsSink analyticsSink;
			string distinctId;
			string key;
			JsonNode value;
			lock (Gate)
			{
				analyticsSink = sink;
				distinctId = identifiedId ?? anonymousId;
				foreach (KeyValuePair<string, JsonNode> superProperty in SuperProperties)
				{
					superProperty.Deconstruct(out key, out value);
					string propertyName = key;
					jsonObject[propertyName] = value?.DeepClone();
				}
			}
			if (analyticsSink is NullAnalyticsSink)
			{
				return;
			}
			jsonObject["$lib"] = "heyclicky-windows";
			jsonObject["$lib_version"] = appVersion;
			jsonObject["$app_version"] = appVersion;
			jsonObject["$app_build"] = appBuild;
			jsonObject["$os"] = "Windows";
			jsonObject["$os_version"] = ClickyClientIdentity.OsVersion();
			jsonObject["$device_type"] = "Desktop";
			if (properties != null)
			{
				foreach (KeyValuePair<string, JsonNode> property in properties)
				{
					property.Deconstruct(out key, out value);
					string propertyName2 = key;
					jsonObject[propertyName2] = value?.DeepClone();
				}
			}
			if (set != null)
			{
				jsonObject["$set"] = set.DeepClone();
			}
			if (setOnce != null)
			{
				jsonObject["$set_once"] = setOnce.DeepClone();
			}
			analyticsSink.Enqueue(new AnalyticsEvent(eventName, distinctId, jsonObject, DateTimeOffset.UtcNow, Guid.NewGuid()));
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "capture " + eventName + " failed: " + ex.Message);
		}
	}

	public static void Register(JsonObject properties)
	{
		lock (Gate)
		{
			foreach (var (propertyName, jsonNode2) in properties)
			{
				SuperProperties[propertyName] = jsonNode2?.DeepClone();
			}
		}
	}

	public static void Unregister(string key)
	{
		lock (Gate)
		{
			SuperProperties.Remove(key);
		}
	}

	public static void Identify(string userId, JsonObject? userProperties = null)
	{
		string text;
		lock (Gate)
		{
			text = identifiedId ?? anonymousId;
			if (identifiedId == userId && userProperties == null)
			{
				return;
			}
			identifiedId = userId;
			SaveIdentity();
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		if (text != userId)
		{
			jsonObject["$anon_distinct_id"] = text;
		}
		Capture("$identify", jsonObject, userProperties);
	}

	public static void Reset()
	{
		lock (Gate)
		{
			identifiedId = null;
			anonymousId = Guid.NewGuid().ToString();
			SuperProperties.Clear();
			SaveIdentity();
		}
	}

	public static Task FlushAsync()
	{
		return sink.FlushAsync();
	}

	private static void LoadIdentity()
	{
		try
		{
			if (!File.Exists(identityFilePath))
			{
				SaveIdentity();
				return;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllBytes(identityFilePath));
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.TryGetProperty("anonymous_id", out var value))
			{
				string @string = value.GetString();
				if (@string != null && @string.Length > 0)
				{
					anonymousId = @string;
				}
			}
			identifiedId = ((rootElement.TryGetProperty("distinct_id", out var value2) && value2.ValueKind == JsonValueKind.String) ? value2.GetString() : null);
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "identity file ignored: " + ex.Message);
		}
	}

	private static void SaveIdentity()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(identityFilePath));
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["anonymous_id"] = anonymousId,
				["distinct_id"] = identifiedId
			};
			File.WriteAllText(identityFilePath, jsonObject.ToJsonString());
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "identity file not saved: " + ex.Message);
		}
	}

	private static JsonObject WithFrontmostApplication(JsonObject properties)
	{
		try
		{
			JsonObject jsonObject = FrontmostApplicationProperties?.Invoke();
			if (jsonObject != null)
			{
				foreach (var (propertyName, jsonNode2) in jsonObject)
				{
					if (!properties.ContainsKey(propertyName))
					{
						properties[propertyName] = jsonNode2?.DeepClone();
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "foreground app: " + ex.Message);
		}
		return properties;
	}

	private static JsonObject Merge(JsonObject properties, JsonObject? extra)
	{
		if (extra != null)
		{
			foreach (KeyValuePair<string, JsonNode> item in extra)
			{
				item.Deconstruct(out var key, out var value);
				string propertyName = key;
				properties[propertyName] = value?.DeepClone();
			}
		}
		return properties;
	}

	private static void Breadcrumb(string category, string? message = null, CrashReportLevel level = CrashReportLevel.Info, JsonObject? data = null)
	{
		try
		{
			ICrashReporter current = CrashReporter.Current;
			if (current.IsEnabled)
			{
				current.AddBreadcrumb(category, message, level, data);
			}
		}
		catch
		{
		}
	}

	private static JsonArray Array(IEnumerable<string> items)
	{
		JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
		foreach (string item in items)
		{
			jsonArray.Add(item);
		}
		return jsonArray;
	}

	public static void TrackComputerUseConsent(bool approved, bool always)
	{
		Capture("computer_use_consent", new JsonObject((JsonNodeOptions?)null)
		{
			["approved"] = approved,
			["always"] = always
		});
	}

	public static void TrackOneBrainNothingHeard(double heldSeconds, bool earsFailed, bool showedMicrophoneTrouble)
	{
		Capture("one_brain_nothing_heard", new JsonObject((JsonNodeOptions?)null)
		{
			["held_seconds"] = Math.Round(heldSeconds, 1),
			["ears_failed"] = earsFailed,
			["showed_microphone_trouble"] = showedMicrophoneTrouble
		});
	}

	public static IReadOnlyList<RecentAppError> RecentAppErrorsSnapshot()
	{
		lock (RecentAppErrorsGate)
		{
			return RecentAppErrorsStorage.ToArray();
		}
	}

	public static void TrackAppError(string stage, string kind, string errorMessage, string? errorDomain, int errorCode, string userFacingMessage, bool relayToWorker = true)
	{
		Capture("app_error", new JsonObject((JsonNodeOptions?)null)
		{
			["stage"] = stage,
			["error_kind"] = kind,
			["error_message"] = errorMessage,
			["error_domain"] = errorDomain ?? "",
			["error_code"] = errorCode,
			["user_facing_message"] = userFacingMessage
		});
		Breadcrumb("app_error", stage + ": " + errorMessage, CrashReportLevel.Error, new JsonObject((JsonNodeOptions?)null)
		{
			["stage"] = stage,
			["error_kind"] = kind,
			["error_code"] = errorCode
		});
		lock (RecentAppErrorsGate)
		{
			RecentAppErrorsStorage.Add(new RecentAppError(DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), stage, kind, errorMessage, errorDomain, errorCode, userFacingMessage));
			if (RecentAppErrorsStorage.Count > 25)
			{
				RecentAppErrorsStorage.RemoveRange(0, RecentAppErrorsStorage.Count - 25);
			}
		}
		CaptureAppErrorToCrashReporter(stage, kind, errorMessage, errorDomain, errorCode, userFacingMessage);
		Log.Warn("app_error", $"{stage} ({kind}, {errorDomain ?? "none"}/{errorCode}): {errorMessage} | shown: \"{userFacingMessage}\"");
		if (!relayToWorker)
		{
			return;
		}
		try
		{
			var (supabaseUserId, userEmail) = CurrentAccount?.Invoke() ?? (null, null);
			ErrorRelay?.Invoke(new ErrorReport(stage, kind, errorMessage, userFacingMessage, appVersion, errorCode, errorDomain, supabaseUserId, userEmail));
		}
		catch (Exception ex)
		{
			Log.Warn("app_error", "relay failed: " + ex.Message);
		}
	}

	public static void TrackAppError(string stage, Exception error, string userFacingMessage, string kind = "generic")
	{
		TrackAppError(stage, kind, DiagnosticMessage(error), ErrorDomain(error), ErrorCode(error), userFacingMessage);
	}

	public static string DiagnosticMessage(Exception error)
	{
		List<string> list = new List<string> { Cap(error.Message) };
		if (error is WorkerHttpException ex)
		{
			list.Add("failing_url=" + ex.Path);
		}
		Exception innerException = error.InnerException;
		if (innerException != null)
		{
			list.Add("underlying=" + innerException.GetType().Name + ": " + Cap(innerException.Message));
		}
		return string.Join(" · ", list);
	}

	public static string ErrorDomain(Exception error)
	{
		if (error is HttpRequestException || error is TaskCanceledException || error is TimeoutException || error is SocketException || error is WebSocketException)
		{
			return "network";
		}
		return error.GetType().FullName ?? error.GetType().Name;
	}

	public static int ErrorCode(Exception error)
	{
		if (!(error is WorkerHttpException { Status: var status }))
		{
			if (error is HttpRequestException { StatusCode: { } statusCode })
			{
				return (int)statusCode;
			}
			return error.HResult;
		}
		return status;
	}

	private static string Cap(string text)
	{
		if (text.Length > 500)
		{
			return text.Substring(0, 500);
		}
		return text;
	}

	private static bool ShouldEmitCrashReporterEvent(string fingerprintKey)
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		lock (LastCapturedAt)
		{
			if (LastCapturedAt.TryGetValue(fingerprintKey, out var value) && now - value < ThrottleWindow)
			{
				return false;
			}
			LastCapturedAt[fingerprintKey] = now;
			foreach (string item in (from p in LastCapturedAt
				where now - p.Value >= ThrottleWindow
				select p.Key).ToList())
			{
				LastCapturedAt.Remove(item);
			}
			return true;
		}
	}

	private static void CaptureAppErrorToCrashReporter(string stage, string kind, string errorMessage, string? errorDomain, int errorCode, string userFacingMessage)
	{
		ICrashReporter current = CrashReporter.Current;
		if (!current.IsEnabled || kind == "credits_exhausted")
		{
			return;
		}
		string text = errorDomain ?? "none";
		if (!ShouldEmitCrashReporterEvent($"app_error|{stage}|{text}|{errorCode}"))
		{
			return;
		}
		try
		{
			current.CaptureMessage("App Error: " + stage, (text == "network") ? CrashReportLevel.Warning : CrashReportLevel.Error, new global::_003C_003Ez__ReadOnlyArray<string>(new string[4]
			{
				"app_error",
				stage,
				text,
				errorCode.ToString()
			}), new Dictionary<string, string>
			{
				["app_error_stage"] = stage,
				["app_error_kind"] = kind,
				["app_error_domain"] = text,
				["app_error_code"] = errorCode.ToString()
			}, "app_error", new JsonObject((JsonNodeOptions?)null)
			{
				["stage"] = stage,
				["kind"] = kind,
				["error_message"] = errorMessage,
				["error_domain"] = errorDomain ?? "",
				["error_code"] = errorCode,
				["user_facing_message"] = userFacingMessage
			});
		}
		catch (Exception ex)
		{
			Log.Warn("app_error", "crash reporter capture failed: " + ex.Message);
		}
	}

	public static void ReportInternalError(string subsystem, string stage, Exception? error = null, string? detail = null, CrashReportLevel severity = CrashReportLevel.Error, string? distinguishingKey = null, IReadOnlyDictionary<string, string>? extraTags = null)
	{
		string text = ((error == null) ? null : ErrorDomain(error));
		int num = ((error != null) ? ErrorCode(error) : 0);
		string text2 = ((error != null) ? DiagnosticMessage(error) : (detail ?? "no error object"));
		Breadcrumb("internal_error", $"{subsystem}/{stage}: {text2}", severity, new JsonObject((JsonNodeOptions?)null)
		{
			["subsystem"] = subsystem,
			["stage"] = stage,
			["error_domain"] = text ?? "",
			["error_code"] = num
		});
		ICrashReporter current = CrashReporter.Current;
		if (!current.IsEnabled)
		{
			return;
		}
		string text3 = text ?? "none";
		string value = ((distinguishingKey == null) ? "" : ("|" + distinguishingKey));
		if (!ShouldEmitCrashReporterEvent($"internal_error|{subsystem}|{stage}|{text3}|{num}{value}"))
		{
			return;
		}
		List<string> list = new List<string>
		{
			"internal_error",
			subsystem,
			stage,
			text3,
			num.ToString()
		};
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			["internal_error_subsystem"] = subsystem,
			["internal_error_stage"] = stage,
			["internal_error_domain"] = text3,
			["internal_error_code"] = num.ToString()
		};
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["subsystem"] = subsystem,
			["stage"] = stage,
			["error_message"] = text2,
			["error_domain"] = text ?? "",
			["error_code"] = num
		};
		if (detail != null)
		{
			jsonObject["detail"] = detail;
		}
		if (distinguishingKey != null)
		{
			list.Add(distinguishingKey);
			dictionary["internal_error_distinguisher"] = distinguishingKey;
			jsonObject["distinguisher"] = distinguishingKey;
		}
		if (extraTags != null)
		{
			foreach (var (key, value2) in extraTags)
			{
				dictionary.TryAdd(key, value2);
			}
		}
		try
		{
			current.CaptureMessage("Internal Error: " + subsystem + "/" + stage, severity, list, dictionary, "internal_error", jsonObject);
		}
		catch (Exception ex)
		{
			Log.Warn("internal_error", "crash reporter capture failed: " + ex.Message);
		}
	}

	public static void TrackAppOpened()
	{
		Capture("app_opened", new JsonObject((JsonNodeOptions?)null) { ["app_version"] = appVersion });
	}

	public static void TrackLocateClickyClicked()
	{
		Capture("locate_clicky_clicked");
	}

	public static void TrackLibraryModeToggled(bool isEnabled)
	{
		Capture(isEnabled ? "library_mode_enabled" : "library_mode_disabled");
	}

	public static void TrackPreSignInIntroDismissed()
	{
		Capture("pre_sign_in_intro_dismissed");
	}

	public static void TrackPreSignInIntroStepViewed(string step)
	{
		Capture("intro_viewed", new JsonObject((JsonNodeOptions?)null) { ["step"] = step });
	}

	public static void TrackPreSignInIntroButtonTapped(string step, string action, string? value = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["step"] = step,
			["action"] = action
		};
		if (value != null)
		{
			jsonObject["value"] = value;
		}
		Capture("intro_button_tapped", jsonObject);
	}

	public static void TrackPreSignInIntroAttentionRequested(string step)
	{
		Capture("intro_attention_requested", new JsonObject((JsonNodeOptions?)null) { ["step"] = step });
	}

	public static void TrackPreSignInIntroNotchWelcomeShown()
	{
		Capture("intro_notch_welcome_shown");
	}

	public static void TrackDockIconPanelOpened()
	{
		Capture("dock_reopen_panel_shown");
	}

	public static void TrackPreSignInIntroNotchFinalSetupTapped()
	{
		Capture("intro_notch_final_setup_tapped");
	}

	public static void TrackDiscoveryChannelSelected(string channel, string? otherDetail = null)
	{
		string text = (string.IsNullOrEmpty(otherDetail) ? null : otherDetail);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["channel"] = channel };
		if (text != null)
		{
			jsonObject["channel_other"] = text;
		}
		Capture("discovery_channel_selected", jsonObject);
		string text2 = ((text == null) ? channel : (channel + "|" + text));
		lock (Gate)
		{
			if (lastSyncedDiscoveryChannel == text2)
			{
				return;
			}
			lastSyncedDiscoveryChannel = text2;
		}
		Register(new JsonObject((JsonNodeOptions?)null) { ["discovery_channel"] = channel });
		if (text != null)
		{
			Capture("$set", null, new JsonObject((JsonNodeOptions?)null)
			{
				["discovery_channel"] = channel,
				["discovery_channel_other"] = text
			});
		}
		else
		{
			JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null);
			JsonNode reference = "discovery_channel_other";
			jsonObject2["$unset"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
			Capture("$set", jsonObject2, new JsonObject((JsonNodeOptions?)null) { ["discovery_channel"] = channel });
		}
	}

	public static void IdentifyUser(string supabaseUserId, string? email, string? name)
	{
		Identify(supabaseUserId, new JsonObject((JsonNodeOptions?)null)
		{
			["email"] = email ?? "",
			["name"] = name ?? ""
		});
		FlushAsync();
		try
		{
			if (CrashReporter.Current.IsEnabled)
			{
				CrashReporter.Current.SetUser(supabaseUserId, string.IsNullOrEmpty(email) ? null : email, string.IsNullOrEmpty(name) ? null : name);
			}
		}
		catch
		{
		}
	}

	public static void UpdateUserPlanProperties(string planName, string planDisplayName, bool isPaidUser)
	{
		Register(new JsonObject((JsonNodeOptions?)null)
		{
			["plan"] = planName,
			["plan_name"] = planDisplayName,
			["is_paid_user"] = isPaidUser
		});
		lock (Gate)
		{
			(string, string, bool)? tuple = lastSyncedPlan;
			bool flag = isPaidUser;
			bool hasValue = tuple.HasValue;
			if (hasValue)
			{
				if (!hasValue)
				{
					return;
				}
				(string, string, bool) valueOrDefault = tuple.GetValueOrDefault();
				if (valueOrDefault.Item1 == planName && valueOrDefault.Item2 == planDisplayName && valueOrDefault.Item3 == flag)
				{
					return;
				}
			}
			lastSyncedPlan = (planName, planDisplayName, isPaidUser);
		}
		Capture("$set", null, new JsonObject((JsonNodeOptions?)null)
		{
			["plan"] = planName,
			["plan_name"] = planDisplayName,
			["is_paid_user"] = isPaidUser
		});
	}

	public static void ResetIdentity()
	{
		Unregister("plan");
		Unregister("plan_name");
		Unregister("is_paid_user");
		lock (Gate)
		{
			lastSyncedPlan = null;
			lastSyncedDiscoveryChannel = null;
		}
		Reset();
		RegisterClientIdentitySuperProperties();
		try
		{
			if (CrashReporter.Current.IsEnabled)
			{
				CrashReporter.Current.SetUser(null, null, null);
				CrashReporter.Current.ClearBreadcrumbs();
			}
		}
		catch
		{
		}
		lock (RecentAppErrorsGate)
		{
			RecentAppErrorsStorage.Clear();
		}
	}

	public static void TrackGoogleSignUp(string email)
	{
		Capture("google_sign_up", new JsonObject((JsonNodeOptions?)null) { ["email"] = email });
	}

	public static void TrackGoogleSignIn(string email)
	{
		Capture("google_sign_in", new JsonObject((JsonNodeOptions?)null) { ["email"] = email });
	}

	public static void TrackAccountDeleted()
	{
		Capture("account_deleted");
	}

	public static void FlushPendingEvents()
	{
		FlushAsync();
	}

	public static void TrackOnboardingStarted()
	{
		Capture("onboarding_started");
	}

	public static void TrackOnboardingVideoStarted(JsonObject? properties = null)
	{
		Capture("onboarding_video_started", properties);
	}

	public static void TrackOnboardingVideoEscapePressed(string pressType, double secondsRemaining)
	{
		Capture("onboarding_video_escape_pressed", new JsonObject((JsonNodeOptions?)null)
		{
			["press_type"] = pressType,
			["seconds_remaining"] = Math.Max(0.0, secondsRemaining)
		});
	}

	public static void TrackOnboardingReplayed()
	{
		Capture("onboarding_replayed");
	}

	public static void TrackOnboardingVideoCompleted()
	{
		Capture("onboarding_video_completed");
	}

	public static void TrackHostedVideoFailure(JsonObject properties)
	{
		Capture("hosted_video_failed", properties);
	}

	public static void TrackOnboardingDemoTriggered()
	{
		Capture("onboarding_demo_triggered");
	}

	public static void TrackOnboardingTutorialInvitePresented()
	{
		Capture("onboarding_tutorial_invite_presented");
	}

	public static void TrackOnboardingTutorialStarted()
	{
		Capture("onboarding_tutorial_started");
	}

	public static void TrackOnboardingTutorialStepCompleted(string step)
	{
		Capture("onboarding_tutorial_step_completed", new JsonObject((JsonNodeOptions?)null) { ["step"] = step });
	}

	public static void TrackOnboardingTutorialResolved(string outcome, string? lastStep, int? durationSeconds = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["outcome"] = outcome };
		if (lastStep != null)
		{
			jsonObject["last_step"] = lastStep;
		}
		if (durationSeconds.HasValue)
		{
			int valueOrDefault = durationSeconds.GetValueOrDefault();
			jsonObject["duration_seconds"] = valueOrDefault;
		}
		Capture("onboarding_tutorial_resolved", jsonObject);
	}

	public static void TrackOnboardingTutorialStepViewed(string step, string navigation)
	{
		Capture("onboarding_tutorial_step_viewed", new JsonObject((JsonNodeOptions?)null)
		{
			["step"] = step,
			["navigation"] = navigation
		});
	}

	public static void TrackOnboardingTutorialMusicToggled(bool muted)
	{
		Capture("onboarding_tutorial_music_toggled", new JsonObject((JsonNodeOptions?)null) { ["muted"] = muted });
	}

	public static void TrackOnboardingTutorialResumedFromNotch()
	{
		Capture("onboarding_tutorial_resumed_from_notch");
	}

	public static void TrackOnboardingTutorialMicDeviceSwitched()
	{
		Capture("onboarding_tutorial_mic_device_switched");
	}

	public static void TrackOnboardingTutorialMicCheckPassedByFallback(double secondsOnStep)
	{
		Capture("onboarding_tutorial_mic_check_passed_by_fallback", new JsonObject((JsonNodeOptions?)null) { ["seconds_on_step"] = Math.Round(secondsOnStep, 1) });
	}

	public static void TrackOnboardingTutorialDrawToolRan(string outcome, int? visualCount = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["outcome"] = outcome };
		if (visualCount.HasValue)
		{
			int valueOrDefault = visualCount.GetValueOrDefault();
			jsonObject["visual_count"] = valueOrDefault;
		}
		Capture("onboarding_tutorial_draw_tool_ran", jsonObject);
	}

	public static void TrackOnboardingTutorialSpeakerVolumeFixed(string via)
	{
		Capture("onboarding_tutorial_speaker_volume_fixed", new JsonObject((JsonNodeOptions?)null) { ["via"] = via });
	}

	public static void TrackNotchFirstShown(string entryContext)
	{
		Capture("notch_first_shown", new JsonObject((JsonNodeOptions?)null) { ["entry_context"] = entryContext });
	}

	public static void TrackNotchExpanded(string trigger)
	{
		Capture("notch_expanded", new JsonObject((JsonNodeOptions?)null) { ["trigger"] = trigger });
	}

	public static void TrackNotchCollapsed(int dwellMs)
	{
		Capture("notch_collapsed", new JsonObject((JsonNodeOptions?)null) { ["dwell_ms"] = dwellMs });
	}

	public static void TrackPushToTalkStarted()
	{
		Breadcrumb("voice", "push_to_talk_started");
	}

	public static void TrackPushToTalkReleased()
	{
		Capture("push_to_talk_released");
		Breadcrumb("voice", "push_to_talk_released");
	}

	public static void TrackUserMessageSent(int characterCount, string source = "voice", string route = "assistant", bool isFollowUp = false, string aiPipeline = "legacy", bool libraryMode = false, JsonObject? interactionProperties = null)
	{
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["character_count"] = characterCount,
			["source"] = source,
			["route"] = route,
			["is_follow_up"] = isFollowUp,
			["ai_pipeline"] = aiPipeline,
			["library_mode"] = libraryMode
		};
		WithFrontmostApplication(properties);
		Merge(properties, interactionProperties);
		Capture("user_message_sent", properties);
		Breadcrumb("message", "user_message_sent", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["route"] = route,
			["is_follow_up"] = isFollowUp,
			["ai_pipeline"] = aiPipeline,
			["character_count"] = characterCount,
			["library_mode"] = libraryMode
		});
	}

	public static void TrackDictationCompleted(int wordCount, int characterCount, string outcome, string? clipboardFallbackReason, bool handsFree, bool usedBatchFallback, string engine, bool usedWarmConnection, bool usedLateFocusRecapture, int? releaseToInsertMilliseconds, int? transcriptWaitMilliseconds, int? cleanupMilliseconds)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["word_count"] = wordCount,
			["character_count"] = characterCount,
			["outcome"] = outcome,
			["hands_free"] = handsFree,
			["used_batch_fallback"] = usedBatchFallback,
			["engine"] = engine,
			["used_warm_connection"] = usedWarmConnection,
			["used_late_focus_recapture"] = usedLateFocusRecapture
		};
		if (clipboardFallbackReason != null)
		{
			jsonObject["clipboard_fallback_reason"] = clipboardFallbackReason;
		}
		if (releaseToInsertMilliseconds.HasValue)
		{
			int valueOrDefault = releaseToInsertMilliseconds.GetValueOrDefault();
			jsonObject["release_to_insert_ms"] = valueOrDefault;
		}
		if (transcriptWaitMilliseconds.HasValue)
		{
			int valueOrDefault2 = transcriptWaitMilliseconds.GetValueOrDefault();
			jsonObject["transcript_wait_ms"] = valueOrDefault2;
		}
		if (cleanupMilliseconds.HasValue)
		{
			int valueOrDefault3 = cleanupMilliseconds.GetValueOrDefault();
			jsonObject["cleanup_ms"] = valueOrDefault3;
		}
		WithFrontmostApplication(jsonObject);
		Capture("dictation_completed", jsonObject);
		Breadcrumb("dictation", "dictation_completed", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null)
		{
			["word_count"] = wordCount,
			["character_count"] = characterCount,
			["outcome"] = outcome,
			["clipboard_fallback_reason"] = clipboardFallbackReason ?? "none",
			["hands_free"] = handsFree,
			["used_batch_fallback"] = usedBatchFallback,
			["engine"] = engine
		});
	}

	public static void TrackWebSearchTriggered()
	{
		Capture("web_search_triggered");
	}

	public static void TrackImageSearchTriggered(string query)
	{
		Capture("image_search_triggered", new JsonObject((JsonNodeOptions?)null) { ["query"] = query });
	}

	public static void TrackPlacesSearchTriggered()
	{
		Capture("places_search_triggered");
	}

	public static void TrackStockQuoteTriggered()
	{
		Capture("stock_quote_triggered");
	}

	public static void TrackAIResponseReceived(int characterCount, string aiPipeline = "legacy")
	{
		Capture("ai_response_received", new JsonObject((JsonNodeOptions?)null)
		{
			["character_count"] = characterCount,
			["ai_pipeline"] = aiPipeline
		});
	}

	public static void TrackElementPointed(string? elementLabel)
	{
		Capture("element_pointed", new JsonObject((JsonNodeOptions?)null) { ["element_label"] = elementLabel ?? "unknown" });
	}

	public static void TrackRealtimeSessionOpened(int mintMs, int socketOpenMs, int totalMs, bool isFirstSessionOfProcess)
	{
		Capture("realtime_session_opened", new JsonObject((JsonNodeOptions?)null)
		{
			["mint_ms"] = mintMs,
			["socket_open_ms"] = socketOpenMs,
			["total_ms"] = totalMs,
			["is_first_session_of_process"] = isFirstSessionOfProcess
		});
	}

	public static void TrackResponseError(string error)
	{
		Capture("response_error", new JsonObject((JsonNodeOptions?)null) { ["error"] = error });
		Breadcrumb("response", "response_error", CrashReportLevel.Error, new JsonObject((JsonNodeOptions?)null) { ["error"] = error });
	}

	public static void TrackTTSError(string error)
	{
		Capture("tts_error", new JsonObject((JsonNodeOptions?)null) { ["error"] = error });
	}

	public static void TrackAnnotationWalkthroughCompleted(string source, int beatCount, int followUpRound)
	{
		Capture("annotation_walkthrough_completed", new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["beat_count"] = beatCount,
			["follow_up_round"] = followUpRound
		});
	}

	public static void TrackBugReportSubmitted(string reportIdentifier)
	{
		Capture("bug_report_submitted", new JsonObject((JsonNodeOptions?)null) { ["report_id"] = reportIdentifier });
		Breadcrumb("support", "bug_report_submitted");
	}

	public static void TrackFeatureRequestOpened()
	{
		Capture("feature_request_opened");
		Breadcrumb("support", "feature_request_opened");
	}

	public static void TrackChangelogOpened(string surface)
	{
		Capture("changelog_opened", new JsonObject((JsonNodeOptions?)null) { ["surface"] = surface });
		Breadcrumb("support", "changelog_opened", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null) { ["surface"] = surface });
	}

	public static void TrackCommunityLinkOpened(string platform)
	{
		Capture("community_link_opened", new JsonObject((JsonNodeOptions?)null) { ["platform"] = platform });
		Breadcrumb("support", "community_link_opened", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null) { ["platform"] = platform });
	}

	private static void AddAgentTranscriptProperties(JsonObject properties, string? transcript)
	{
		if (!string.IsNullOrWhiteSpace(transcript))
		{
			properties["transcript_character_count"] = transcript.Length;
		}
	}

	private static void AddAgentComputerUseProperties(JsonObject properties, bool usedComputerUse, IEnumerable<string>? toolNames)
	{
		List<string> list = (toolNames ?? System.Array.Empty<string>()).Order<string>(StringComparer.Ordinal).ToList();
		properties["used_computer_use"] = usedComputerUse;
		properties["computer_use_tool_names"] = Array(list);
		properties["computer_use_tool_count"] = list.Count;
	}

	private static void AddAgentIntegrationsUsedProperties(JsonObject properties, IEnumerable<string>? integrationsUsed)
	{
		List<string> list = (integrationsUsed ?? System.Array.Empty<string>()).Order<string>(StringComparer.Ordinal).ToList();
		properties["used_integrations"] = list.Count > 0;
		properties["integrations_used"] = Array(list);
		properties["integrations_count"] = list.Count;
	}

	public static void TrackAgentStarted(string transcript, string source, JsonObject? interactionProperties = null)
	{
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["character_count"] = transcript.Length,
			["source"] = source
		};
		WithFrontmostApplication(properties);
		Capture("agent_started", Merge(properties, interactionProperties));
	}

	public static void TrackAgentFollowedUp(string transcript, string source, string? threadIdentifier, JsonObject? interactionProperties = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["character_count"] = transcript.Length,
			["source"] = source
		};
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		WithFrontmostApplication(jsonObject);
		Capture("agent_followed_up", Merge(jsonObject, interactionProperties));
	}

	public static void TrackAgentUsedIntegration(string? threadIdentifier, string? transcript, string integrationProviderKey)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["integration"] = integrationProviderKey };
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		AddAgentTranscriptProperties(jsonObject, transcript);
		WithFrontmostApplication(jsonObject);
		Capture("agent_used_integration", jsonObject);
	}

	public static void TrackAgentFinished(string finalMessage, string? threadIdentifier, int? durationMilliseconds, int? tokenCount, string? transcript = null, bool usedComputerUse = false, IEnumerable<string>? computerUseToolNames = null, IEnumerable<string>? integrationsUsed = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["character_count"] = finalMessage.Length,
			["final_message_character_count"] = finalMessage.Length
		};
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		if (durationMilliseconds.HasValue)
		{
			int valueOrDefault = durationMilliseconds.GetValueOrDefault();
			jsonObject["duration_ms"] = valueOrDefault;
		}
		if (tokenCount.HasValue)
		{
			int valueOrDefault2 = tokenCount.GetValueOrDefault();
			jsonObject["token_count"] = valueOrDefault2;
		}
		AddAgentTranscriptProperties(jsonObject, transcript);
		AddAgentComputerUseProperties(jsonObject, usedComputerUse, computerUseToolNames);
		AddAgentIntegrationsUsedProperties(jsonObject, integrationsUsed);
		Capture("agent_finished", jsonObject);
		Breadcrumb("agent", "agent_finished", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null)
		{
			["thread_id"] = threadIdentifier ?? "",
			["duration_ms"] = durationMilliseconds ?? (-1)
		});
	}

	public static void TrackAgentFailed(string errorSummary, string phase, string? threadIdentifier, string? transcript, bool usedComputerUse = false, IEnumerable<string>? computerUseToolNames = null, IEnumerable<string>? integrationsUsed = null, bool isExpectedApprovalPause = false, string? diagnostic = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["error_summary"] = errorSummary,
			["phase"] = phase
		};
		if (diagnostic != null)
		{
			jsonObject["error_diagnostic"] = diagnostic;
		}
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		AddAgentTranscriptProperties(jsonObject, transcript);
		AddAgentComputerUseProperties(jsonObject, usedComputerUse, computerUseToolNames);
		AddAgentIntegrationsUsedProperties(jsonObject, integrationsUsed);
		Capture("agent_failed", jsonObject);
		Breadcrumb("agent", "agent_failed: " + errorSummary, CrashReportLevel.Error, new JsonObject((JsonNodeOptions?)null)
		{
			["phase"] = phase,
			["thread_id"] = threadIdentifier ?? ""
		});
		bool flag = ((phase == "launch" || phase == "run") ? true : false);
		if (flag && !isExpectedApprovalPause)
		{
			ReportInternalError("codex_agent", "agent_failed", null, diagnostic ?? errorSummary, CrashReportLevel.Error, phase + ":" + AgentFailureReason.Key(diagnostic ?? errorSummary));
		}
	}

	public static void TrackAgentInterrupted(string summary, string? threadIdentifier, string? transcript = null, bool usedComputerUse = false, IEnumerable<string>? computerUseToolNames = null, IEnumerable<string>? integrationsUsed = null, bool unexpected = false)
	{
		if (unexpected)
		{
			ReportInternalError("codex_agent", "agent_interrupted", null, summary, CrashReportLevel.Warning, AgentFailureReason.Key(summary));
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["summary_character_count"] = summary.Length };
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		AddAgentTranscriptProperties(jsonObject, transcript);
		AddAgentComputerUseProperties(jsonObject, usedComputerUse, computerUseToolNames);
		AddAgentIntegrationsUsedProperties(jsonObject, integrationsUsed);
		Capture("agent_interrupted", jsonObject);
	}

	public static void TrackCodexThreadArchived(string? threadIdentifier)
	{
		Capture("agent_thread_archived", ThreadOnly(threadIdentifier));
	}

	public static void TrackAgentExtraEffortApprovalShown(string? threadIdentifier)
	{
		Capture("agent_extra_effort_approval_shown", ThreadOnly(threadIdentifier));
	}

	public static void TrackAgentExtraEffortApproved(string? threadIdentifier)
	{
		Capture("agent_extra_effort_approved", ThreadOnly(threadIdentifier));
	}

	private static JsonObject ThreadOnly(string? threadIdentifier)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		if (threadIdentifier != null)
		{
			jsonObject["thread_id"] = threadIdentifier;
		}
		return jsonObject;
	}

	public static void TrackCheckoutReturned(bool isCanceled, bool isPortal, string? checkoutSessionIdentifier)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["is_canceled"] = isCanceled,
			["is_portal"] = isPortal
		};
		if (!string.IsNullOrEmpty(checkoutSessionIdentifier))
		{
			jsonObject["checkout_session_id"] = checkoutSessionIdentifier;
			jsonObject["$insert_id"] = "checkout_returned:" + checkoutSessionIdentifier;
		}
		Capture("checkout_returned", jsonObject);
	}

	public static void TrackCustomerPortalOpened(string source)
	{
		Capture("customer_portal_opened", new JsonObject((JsonNodeOptions?)null) { ["source"] = source });
		FlushPendingEvents();
	}

	public static void NoteReferralPageEntryPoint(string entryPoint)
	{
		lock (Gate)
		{
			pendingReferralPageEntryPoint = entryPoint;
		}
	}

	public static void TrackReferralPageOpened(string? entryPoint)
	{
		string text;
		lock (Gate)
		{
			text = entryPoint ?? pendingReferralPageEntryPoint ?? "unknown";
			pendingReferralPageEntryPoint = null;
		}
		Capture("referral_page_opened", new JsonObject((JsonNodeOptions?)null) { ["entry_point"] = text });
	}

	public static void TrackReferralShareCopied(string item)
	{
		Capture("referral_share_copied", new JsonObject((JsonNodeOptions?)null) { ["item"] = item });
	}

	public static void TrackReferralPayoutRequested(string surface, bool isAboveMinimum)
	{
		Capture("referral_payout_requested", new JsonObject((JsonNodeOptions?)null)
		{
			["surface"] = surface,
			["is_above_minimum"] = isAboveMinimum
		});
	}

	public static void TrackReferralUsernameChanged(string outcome)
	{
		Capture("referral_username_change_result", new JsonObject((JsonNodeOptions?)null) { ["outcome"] = outcome });
	}

	public static void TrackReferralFriendCodeClaimResult(string source, string outcome)
	{
		Capture("referral_friend_code_claim_result", new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["outcome"] = outcome
		});
	}

	public static void TrackReferralDeepLinkOpened(bool isSignedIn)
	{
		Capture("referral_deep_link_opened", new JsonObject((JsonNodeOptions?)null) { ["is_signed_in"] = isSignedIn });
	}

	public static void TrackUpdateFound(string fromVersion, string toVersion)
	{
		Capture("update_found", new JsonObject((JsonNodeOptions?)null)
		{
			["from_version"] = fromVersion,
			["to_version"] = toVersion
		});
	}

	public static void TrackCheckForUpdatesClicked(string surface)
	{
		Capture("check_for_updates_clicked", new JsonObject((JsonNodeOptions?)null) { ["surface"] = surface });
	}

	public static void TrackUpdateInstallClicked(string fromVersion, string toVersion, string surface)
	{
		Capture("update_install_clicked", new JsonObject((JsonNodeOptions?)null)
		{
			["from_version"] = fromVersion,
			["to_version"] = toVersion,
			["surface"] = surface
		});
	}

	public static void TrackUpdateInstallCompleted(string fromVersion, string toVersion)
	{
		Capture("update_install_completed", new JsonObject((JsonNodeOptions?)null)
		{
			["from_version"] = fromVersion,
			["to_version"] = toVersion
		});
	}

	public static void TrackAgentHarnessTurn(string harness, string place, string status, double durationSeconds, IReadOnlyDictionary<string, double> stats)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["harness"] = harness,
			["place"] = place,
			["status"] = status,
			["duration_seconds"] = Math.Round(durationSeconds * 10.0) / 10.0
		};
		foreach (var (text2, num2) in stats)
		{
			jsonObject["h1_" + text2] = num2;
		}
		Capture("agent_harness_turn", jsonObject);
	}

	public static string ProductClickyIdentifier(string slug)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(slug)));
	}

	public static void TrackProductEvent(string productEvent, string? clickySlug = null, string? threadId = null, JsonObject? properties = null)
	{
		JsonObject jsonObject = properties?.DeepClone().AsObject() ?? new JsonObject((JsonNodeOptions?)null);
		jsonObject["product_event_version"] = 1;
		jsonObject["app_version"] = appVersion;
		jsonObject["app_build"] = appBuild;
		jsonObject["is_debug_build"] = false;
		if (clickySlug != null)
		{
			jsonObject["clicky_id"] = ProductClickyIdentifier(clickySlug);
		}
		if (threadId != null)
		{
			jsonObject["thread_id"] = threadId;
		}
		Capture(productEvent, jsonObject);
	}

	public static void TrackOnboardingSkippedByShake()
	{
		Capture("onboarding_skipped_by_shake");
	}

	public static string PermissionAnalyticsName(PermissionKind kind)
	{
		if (kind != 0)
		{
			return "screen_recording";
		}
		return "microphone";
	}

	private static JsonObject PermissionProperties(PermissionKind kind, string? probeSource = null)
	{
		int num = System.Array.FindIndex(PermissionSteps, ((PermissionKind Kind, string Name) s) => s.Kind == kind);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["permission"] = PermissionAnalyticsName(kind),
			["permission_step_index"] = num + 1,
			["permission_step_count"] = PermissionSteps.Length,
			["permission_category"] = "system",
			["is_system_permission"] = true
		};
		if (probeSource != null)
		{
			jsonObject["probe_source"] = probeSource;
		}
		return jsonObject;
	}

	private static JsonObject PermissionFlowProperties(string? entryContext = null)
	{
		List<string> items = PermissionSteps.Select(((PermissionKind Kind, string Name) s) => s.Name).ToList();
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["required_permission_count"] = PermissionSteps.Length,
			["required_permissions"] = Array(items),
			["system_permission_count"] = PermissionSteps.Length,
			["system_permissions"] = Array(items),
			["includes_screen_content"] = false
		};
		if (entryContext != null)
		{
			jsonObject["entry_context"] = entryContext;
		}
		return jsonObject;
	}

	public static void TrackPermissionFlowShown(string entryContext)
	{
		Capture("permission_flow_shown", PermissionFlowProperties(entryContext));
	}

	public static void TrackPermissionFlowCompleted(string entryContext)
	{
		Capture("permission_flow_completed", PermissionFlowProperties(entryContext));
	}

	public static void TrackCorePermissionsGranted()
	{
		Capture("core_permissions_granted", PermissionFlowProperties());
	}

	public static void TrackAllPermissionsGranted()
	{
		Capture("all_permissions_granted", PermissionFlowProperties());
	}

	public static void TrackPermissionStepShown(PermissionKind kind)
	{
		Capture("permission_step_shown", PermissionProperties(kind));
	}

	public static void TrackPermissionSettingsOpened(PermissionKind kind)
	{
		Capture("permission_settings_opened", PermissionProperties(kind));
	}

	public static void TrackPermissionNativePromptTriggered(PermissionKind kind)
	{
		Capture("permission_native_prompt_triggered", PermissionProperties(kind));
	}

	public static void TrackPermissionGranted(PermissionKind kind, string? probeSource = null)
	{
		Capture("permission_granted", PermissionProperties(kind, probeSource));
	}

	public static void TrackPermissionDenied(PermissionKind kind, string? probeSource = null)
	{
		Capture("permission_denied", PermissionProperties(kind, probeSource));
	}

	public static void TrackPermissionRestartTapped(PermissionKind kind)
	{
		Capture("permission_restart_tapped", PermissionProperties(kind));
	}

	public static void TrackPaywallShown(string surface, string? reason, int currentUsage, int limit, string? priceDisplay, string presentationSource, bool offersFreeOption, IEnumerable<string> offeredPlans, bool offersYearly, string? defaultInterval)
	{
		List<string> list = offeredPlans.ToList();
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["surface"] = surface,
			["reason"] = reason ?? "unknown",
			["current_usage"] = currentUsage,
			["limit"] = limit,
			["price_display"] = priceDisplay ?? "",
			["presentation_source"] = presentationSource,
			["offers_free_option"] = offersFreeOption,
			["offered_plan_count"] = list.Count,
			["offered_plans"] = Array(list),
			["offers_yearly"] = offersYearly,
			["default_interval"] = defaultInterval ?? "month"
		};
		Capture("paywall_opened", properties);
		Capture("paywall_shown", properties);
	}

	public static void TrackPaywallIntervalToggled(string surface, string interval, string defaultInterval, string presentationSource = "limit_hit")
	{
		Capture("paywall_interval_toggled", new JsonObject((JsonNodeOptions?)null)
		{
			["surface"] = surface,
			["interval"] = interval,
			["default_interval"] = defaultInterval,
			["presentation_source"] = presentationSource
		});
	}

	public static void TrackAnnualBillingSwitchClicked(string plan)
	{
		Capture("annual_billing_switch_clicked", new JsonObject((JsonNodeOptions?)null) { ["plan"] = plan });
	}

	public static void TrackPaywallCTAClicked(string surface, string? plan = null, string? interval = null, string presentationSource = "limit_hit")
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["surface"] = surface,
			["presentation_source"] = presentationSource
		};
		if (plan != null)
		{
			jsonObject["plan"] = plan;
		}
		if (interval != null)
		{
			jsonObject["interval"] = interval;
		}
		Capture("paywall_cta_clicked", jsonObject);
	}

	public static void TrackPaywallFreeSelected(string presentationSource)
	{
		Capture("paywall_free_selected", new JsonObject((JsonNodeOptions?)null)
		{
			["presentation_source"] = presentationSource,
			["plan"] = "free"
		});
	}

	public static void TrackPaywallDismissed(string presentationSource)
	{
		Capture("paywall_dismissed", new JsonObject((JsonNodeOptions?)null)
		{
			["presentation_source"] = presentationSource,
			["dismiss_method"] = "close_button"
		});
	}

	public static void TrackCheckoutStarted(string source, string? surface, string? plan, string? interval, string? presentationSource)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["source"] = source };
		if (surface != null)
		{
			jsonObject["surface"] = surface;
		}
		if (plan != null)
		{
			jsonObject["plan"] = plan;
		}
		if (interval != null)
		{
			jsonObject["interval"] = interval;
		}
		if (presentationSource != null)
		{
			jsonObject["presentation_source"] = presentationSource;
		}
		Capture("checkout_started", jsonObject);
		FlushPendingEvents();
		try
		{
			JsonObject jsonObject2 = jsonObject.DeepClone().AsObject();
			jsonObject2["started_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			Directory.CreateDirectory(Path.GetDirectoryName(CheckoutContextPath));
			File.WriteAllText(CheckoutContextPath, jsonObject2.ToJsonString());
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "checkout context not saved: " + ex.Message);
		}
	}

	public static void TrackSubscriptionActivated(string? checkoutSessionId, string? tierDisplayName)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		if (!string.IsNullOrEmpty(checkoutSessionId))
		{
			jsonObject["checkout_session_id"] = checkoutSessionId;
			jsonObject["$insert_id"] = "subscription_activated:" + checkoutSessionId;
		}
		if (tierDisplayName != null)
		{
			jsonObject["tier"] = tierDisplayName;
		}
		string text = null;
		try
		{
			if (File.Exists(CheckoutContextPath) && JsonNode.Parse(File.ReadAllText(CheckoutContextPath), null) is JsonObject jsonObject2)
			{
				File.Delete(CheckoutContextPath);
				long value;
				DateTimeOffset? dateTimeOffset = ((jsonObject2["started_at"] is JsonValue jsonValue && jsonValue.TryGetValue<long>(out value)) ? new DateTimeOffset?(DateTimeOffset.FromUnixTimeSeconds(value)) : ((DateTimeOffset?)null));
				TimeSpan timeSpan;
				if (dateTimeOffset.HasValue)
				{
					DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
					timeSpan = DateTimeOffset.UtcNow - valueOrDefault;
				}
				else
				{
					timeSpan = TimeSpan.MaxValue;
				}
				TimeSpan timeSpan2 = timeSpan;
				if (timeSpan2 >= TimeSpan.Zero && timeSpan2 <= CheckoutContextLifetime)
				{
					foreach (var (text3, jsonNode2) in jsonObject2)
					{
						if (text3 != "started_at" && jsonNode2 is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value2))
						{
							jsonObject["checkout_" + text3] = value2;
						}
					}
					jsonObject["minutes_since_checkout_started"] = (int)timeSpan2.TotalMinutes;
					text = (jsonObject2["presentation_source"] ?? jsonObject2["source"])?.GetValue<string>();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("analytics", "checkout context: " + ex.Message);
		}
		Capture("subscription_activated", jsonObject, null, (text == null) ? null : new JsonObject((JsonNodeOptions?)null) { ["first_subscription_entry_point"] = text });
	}

	private static JsonObject IntegrationProperties(string integrationId, string integrationName, IReadOnlyList<string> toolkitSlugs, string source, string? status, int? connectedCount, int? totalCount, int? deletedAccountCount)
	{
		string text = toolkitSlugs.FirstOrDefault() ?? integrationId;
		bool flag = integrationId.StartsWith("custom:", StringComparison.Ordinal);
		bool flag2 = integrationId.Equals("obsidian", StringComparison.OrdinalIgnoreCase);
		JsonObject obj = new JsonObject((JsonNodeOptions?)null)
		{
			["integration_id"] = integrationId,
			["integration_name"] = integrationName,
			["toolkit_slug"] = text
		};
		IReadOnlyList<string> items;
		if (toolkitSlugs.Count != 0)
		{
			items = toolkitSlugs;
		}
		else
		{
			IReadOnlyList<string> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<string>(text);
			items = readOnlyList;
		}
		obj["toolkit_slugs"] = Array(items);
		obj["provider"] = (flag ? "custom_mcp" : (flag2 ? "local" : "composio"));
		obj["source"] = source;
		obj["is_google_integration"] = GoogleIntegrationIds.Contains(integrationId.ToLowerInvariant()) || GoogleIntegrationIds.Contains(text.ToLowerInvariant());
		obj["is_local_integration"] = flag || flag2;
		JsonObject jsonObject = obj;
		if (status != null)
		{
			jsonObject["integration_status"] = status;
		}
		if (connectedCount.HasValue)
		{
			int valueOrDefault = connectedCount.GetValueOrDefault();
			jsonObject["connected_count"] = valueOrDefault;
		}
		if (totalCount.HasValue)
		{
			int valueOrDefault2 = totalCount.GetValueOrDefault();
			jsonObject["total_count"] = valueOrDefault2;
		}
		if (deletedAccountCount.HasValue)
		{
			int valueOrDefault3 = deletedAccountCount.GetValueOrDefault();
			jsonObject["deleted_account_count"] = valueOrDefault3;
		}
		return WithFrontmostApplication(jsonObject);
	}

	public static void TrackIntegrationInitiated(string integrationId, string integrationName, IReadOnlyList<string> toolkitSlugs, string source = "settings", string? status = null, int? connectedCount = null, int? totalCount = null)
	{
		Capture("integration_initiated", IntegrationProperties(integrationId, integrationName, toolkitSlugs, source, status, connectedCount, totalCount, null));
	}

	public static void TrackIntegrationConnected(string integrationId, string integrationName, IReadOnlyList<string> toolkitSlugs, string source = "settings", string? status = null, int? connectedCount = null, int? totalCount = null)
	{
		Capture("integration_connected", IntegrationProperties(integrationId, integrationName, toolkitSlugs, source, status, connectedCount, totalCount, null));
	}

	public static void TrackIntegrationDisconnected(string integrationId, string integrationName, IReadOnlyList<string> toolkitSlugs, string source = "settings", string? status = null, int? connectedCount = null, int? totalCount = null, int? deletedAccountCount = null)
	{
		Capture("integration_disconnected", IntegrationProperties(integrationId, integrationName, toolkitSlugs, source, status, connectedCount, totalCount, deletedAccountCount));
	}

	private static JsonObject IntegrationSuggestionProperties(string toolkitSlug, string displayName, IReadOnlyList<string> useCases, string? logoUrl, string? sourceApplicationName, string? sourceHost)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["toolkit_slug"] = toolkitSlug,
			["integration_name"] = displayName,
			["source"] = "integration_suggestion",
			["use_case_count"] = useCases.Count,
			["use_cases"] = Array(useCases)
		};
		if (!string.IsNullOrEmpty(logoUrl))
		{
			jsonObject["logo_url"] = logoUrl;
		}
		if (!string.IsNullOrEmpty(sourceApplicationName))
		{
			jsonObject["source_application_name"] = sourceApplicationName;
		}
		if (!string.IsNullOrEmpty(sourceHost))
		{
			jsonObject["source_host"] = sourceHost;
		}
		return WithFrontmostApplication(jsonObject);
	}

	public static void TrackIntegrationSuggestion(string outcome, string toolkitSlug, string displayName, IReadOnlyList<string> useCases, string? logoUrl, string? sourceApplicationName, string? sourceHost)
	{
		Capture("integration_suggestion_" + outcome, IntegrationSuggestionProperties(toolkitSlug, displayName, useCases, logoUrl, sourceApplicationName, sourceHost));
	}

	public static void TrackAgentTabThreadHiddenFromHistory(string? threadIdentifier)
	{
		Capture("agent_tab_thread_hidden_from_history", WithFrontmostApplication(ThreadOnly(threadIdentifier)));
	}

	public static void TrackNotchFileDragOpenedComposer()
	{
		Capture("notch_file_drag_composer_opened");
	}

	public static void TrackNotchFilesDropped(int fileCount, string composerOpenSource)
	{
		Capture("notch_files_dropped", new JsonObject((JsonNodeOptions?)null)
		{
			["file_count"] = fileCount,
			["composer_open_source"] = composerOpenSource
		});
	}

	public static void TrackNotchFileDragAbandoned()
	{
		Capture("notch_file_drag_abandoned");
	}

	public static void TrackNotchDroppedFilesAttached(int fileCount)
	{
		Capture("notch_dropped_files_attached", new JsonObject((JsonNodeOptions?)null) { ["file_count"] = fileCount });
	}

	public static void TrackNotchDroppedAttachmentsDiscarded(int fileCount)
	{
		Capture("notch_dropped_attachments_discarded", new JsonObject((JsonNodeOptions?)null) { ["file_count"] = fileCount });
	}

	public static void TrackHomeSpaceEarlierAgentMovedIntoClicky(bool becameChat, bool foundedNewClicky = false)
	{
		Capture("agent_space_earlier_agent_moved_into_clicky", new JsonObject((JsonNodeOptions?)null)
		{
			["became_chat"] = becameChat,
			["founded_new_clicky"] = foundedNewClicky
		});
	}

	public static void TrackHomeSpaceNewClickyInterviewStarted(string mode, string interviewId)
	{
		Capture("home_space_new_clicky_interview_started", new JsonObject((JsonNodeOptions?)null)
		{
			["mode"] = mode,
			["interview_id"] = interviewId
		});
	}

	public static void TrackHomeSpaceNewClickyInterviewEnded(bool founded, string mode, int answerCount, bool routedToExisting, string interviewId, double durationSeconds)
	{
		Capture("home_space_new_clicky_interview_ended", new JsonObject((JsonNodeOptions?)null)
		{
			["founded"] = founded,
			["mode"] = mode,
			["answer_count"] = answerCount,
			["routed_to_existing"] = routedToExisting,
			["interview_id"] = interviewId,
			["duration_seconds"] = Math.Max(0.0, durationSeconds),
			["outcome"] = ((mode == "onboarding" && founded) ? "planning" : (routedToExisting ? "routed_to_existing" : (founded ? "founded" : "left")))
		});
	}

	public static string ProductArtifactKind(string artifact)
	{
		if (artifact.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || artifact.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return "web_link";
		}
		string text;
		try
		{
			text = Path.GetExtension(artifact).TrimStart('.').ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		switch (text)
		{
		case "md":
		case "markdown":
			return "markdown";
		case "pdf":
			return "pdf";
		case "png":
		case "jpg":
		case "gif":
		case "svg":
		case "jpeg":
		case "webp":
		case "heic":
			return "image";
		case "csv":
		case "tsv":
		case "xls":
		case "xlsx":
		case "numbers":
			return "spreadsheet";
		case "ppt":
		case "key":
		case "pptx":
			return "presentation";
		case "txt":
		case "doc":
		case "rtf":
		case "docx":
		case "pages":
			return "document";
		case "htm":
		case "html":
			return "html";
		case "mp4":
		case "mov":
		case "webm":
			return "video";
		case "mp3":
		case "m4a":
		case "wav":
			return "audio";
		default:
			return "other";
		}
	}

	public static void TrackArtifactAction(string action, string artifact, string? threadId = null, JsonObject? properties = null)
	{
		JsonObject jsonObject = properties?.DeepClone().AsObject() ?? new JsonObject((JsonNodeOptions?)null);
		jsonObject["action"] = action;
		jsonObject["artifact_kind"] = ProductArtifactKind(artifact);
		TrackProductEvent("agent_artifact_action", null, threadId, jsonObject);
	}

	public static JsonObject InteractionContext(string inputMethod, string inputSurface, string presentation = "unknown", bool homeWasOpen = false, bool homeWasVisible = false, bool notchWasExpanded = false)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["interaction_context_version"] = 1,
			["input_method"] = inputMethod,
			["input_surface"] = inputSurface,
			["home_space_presentation"] = presentation
		};
		if (presentation != "unknown")
		{
			jsonObject["home_space_was_open"] = homeWasOpen;
			jsonObject["home_space_was_visible"] = homeWasVisible;
			jsonObject["notch_was_expanded"] = notchWasExpanded;
		}
		return jsonObject;
	}

	public static void TrackSuggestionInteraction(string productEvent, SuggestionCardInfo card, string? action = null, bool hasFeedback = false, JsonObject? context = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["suggestion_id"] = card.SuggestionId,
			["slot"] = card.Slot,
			["creates_clicky"] = card.CreatesClicky,
			["has_routine"] = card.HasRoutine,
			["has_feedback"] = hasFeedback
		};
		if (action != null)
		{
			jsonObject["action"] = action;
		}
		Merge(jsonObject, context);
		TrackProductEvent(productEvent, card.ClickySlug, null, jsonObject);
	}

	public static JsonObject SuggestionInteractionProperties(SuggestionCardInfo card, JsonObject context)
	{
		JsonObject jsonObject = context.DeepClone().AsObject();
		jsonObject["suggestion_id"] = card.SuggestionId;
		jsonObject["has_routine"] = card.HasRoutine;
		jsonObject["creates_clicky"] = card.CreatesClicky;
		if (card.ClickySlug != null)
		{
			jsonObject["clicky_id"] = ProductClickyIdentifier(card.ClickySlug);
		}
		return jsonObject;
	}

	public static void TrackProactiveAgentApproved(string agentTitle, string agentDescription, double secondsSinceRecommended, bool dispatched, string slot, JsonObject? interactionProperties = null)
	{
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["agent_title"] = agentTitle,
			["agent_description"] = agentDescription,
			["seconds_since_recommended"] = (int)secondsSinceRecommended,
			["dispatched"] = dispatched,
			["slot"] = slot
		};
		Capture("proactive_agent_approved", Merge(properties, interactionProperties));
	}

	public static void TrackProactiveAgentDenied(string agentTitle, string agentDescription, double secondsSinceRecommended, string slot, string? denyReason = null, JsonObject? interactionProperties = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["agent_title"] = agentTitle,
			["agent_description"] = agentDescription,
			["seconds_since_recommended"] = (int)secondsSinceRecommended,
			["slot"] = slot,
			["provided_reason"] = denyReason != null
		};
		if (denyReason != null)
		{
			jsonObject["deny_reason"] = denyReason;
		}
		Capture("proactive_agent_denied", Merge(jsonObject, interactionProperties));
	}

	public static int RoundedSeconds(double seconds)
	{
		return (int)Math.Round(seconds, MidpointRounding.AwayFromZero);
	}

	public static JsonObject JobReportProperties(int jobCount, int attempts, int cardsShown, int blockCount, int cardErrors, int lineCount, bool raisedHeadsUp, bool playedChime, long durationMs)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["job_count"] = jobCount,
			["attempts"] = attempts,
			["has_card"] = cardsShown > 0,
			["cards_shown"] = cardsShown,
			["jobs_without_card"] = jobCount - cardsShown,
			["block_count"] = blockCount,
			["card_errors"] = cardErrors,
			["line_count"] = lineCount,
			["raised_heads_up"] = raisedHeadsUp,
			["played_chime"] = playedChime,
			["duration_ms"] = (int)durationMs
		};
	}

	public static void TrackCursorDetached()
	{
		Capture("cursor_detached");
	}

	public static void TrackCursorDetachedClickStart()
	{
		Capture("cursor_detached_click_start");
	}

	public static void TrackCursorDetachedClickStop()
	{
		Capture("cursor_detached_click_stop");
	}

	public static void TrackCursorDetachedAutoReattach()
	{
		Capture("cursor_detached_auto_reattach");
	}

	public static void TrackDictationPasteVerification(string result, int waitMilliseconds, string? targetAppName, string? targetAppBundleId)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["result"] = result,
			["wait_ms"] = waitMilliseconds
		};
		if (targetAppName != null)
		{
			jsonObject["target_app_name"] = targetAppName;
		}
		if (targetAppBundleId != null)
		{
			jsonObject["target_app_bundle_id"] = targetAppBundleId;
		}
		Capture("dictation_paste_verification", jsonObject);
	}

	public static void TrackMeetingCountdownPresented(string? actionKind, int secondsUntilStart)
	{
		Capture("meeting_countdown_presented", new JsonObject((JsonNodeOptions?)null)
		{
			["action_kind"] = actionKind ?? "none",
			["seconds_until_start"] = secondsUntilStart
		});
	}

	public static void TrackMeetingCountdownActionClicked(string? actionKind, int secondsUntilStart)
	{
		Capture("meeting_countdown_action_clicked", new JsonObject((JsonNodeOptions?)null)
		{
			["action_kind"] = actionKind ?? "none",
			["seconds_until_start"] = secondsUntilStart
		});
	}

	public static void TrackMeetingCountdownDismissed(string reason, string? actionKind, int secondsUntilStart)
	{
		Capture("meeting_countdown_dismissed", new JsonObject((JsonNodeOptions?)null)
		{
			["reason"] = reason,
			["action_kind"] = actionKind ?? "none",
			["seconds_until_start"] = secondsUntilStart
		});
	}

	public static void TrackProactiveAgentsRecommended(IReadOnlyList<string> agentTitles, string slot)
	{
		Capture("proactive_agent_recommended", new JsonObject((JsonNodeOptions?)null)
		{
			["agent_count"] = agentTitles.Count,
			["agent_titles"] = TitlesArray(agentTitles),
			["slot"] = slot
		});
	}

	public static void TrackProactiveAgentsUnactioned(IReadOnlyList<string> agentTitles, string reason, string slot)
	{
		Capture("proactive_agent_unactioned", new JsonObject((JsonNodeOptions?)null)
		{
			["agent_count"] = agentTitles.Count,
			["agent_titles"] = TitlesArray(agentTitles),
			["reason"] = reason,
			["slot"] = slot
		});
	}

	private static JsonArray TitlesArray(IReadOnlyList<string> titles)
	{
		JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
		foreach (string title in titles)
		{
			jsonArray.Add(title);
		}
		return jsonArray;
	}

	public static void TrackVoiceSettings(string voiceId, string? languageCode, double speechSpeed)
	{
		Capture("$set", null, new JsonObject((JsonNodeOptions?)null)
		{
			["voice_id"] = voiceId,
			["voice_provider"] = (VoiceCatalog.IsGemini(voiceId) ? "gemini" : "openai"),
			["voice_language"] = languageCode ?? "automatic",
			["speech_speed"] = speechSpeed
		});
	}

	public static void TrackScreenDrawingCompleted(string model, string outcome, int linesSpoken, int markCount, bool targetArmed, int followUpRound, int displayCount, bool drewOnOtherDisplay, bool continuedDrawing, bool startedFromTypedTurn, int textDeliveries, int? firstLineMilliseconds, int totalMilliseconds)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["model"] = model,
			["outcome"] = outcome,
			["lines_spoken"] = linesSpoken,
			["mark_count"] = markCount,
			["target_armed"] = targetArmed,
			["follow_up_round"] = followUpRound,
			["display_count"] = displayCount,
			["drew_on_other_display"] = drewOnOtherDisplay,
			["continued_drawing"] = continuedDrawing,
			["started_from_typed_turn"] = startedFromTypedTurn,
			["text_deliveries"] = textDeliveries,
			["total_ms"] = totalMilliseconds
		};
		if (firstLineMilliseconds.HasValue)
		{
			int valueOrDefault = firstLineMilliseconds.GetValueOrDefault();
			jsonObject["first_line_ms"] = valueOrDefault;
		}
		Capture("screen_drawing_completed", jsonObject);
	}

	public static void TrackAnnotationMarkShown(string source, string? label, bool isHover, int radius, int screen = 1)
	{
		Capture("annotation_mark_shown", new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["kind"] = (isHover ? "hover" : "target"),
			["label"] = label ?? "",
			["screen"] = screen,
			["is_hover"] = isHover,
			["radius"] = radius
		});
	}

	public static void TrackAnnotationTargetArmed(string? label, bool isHover, int followUpRound)
	{
		Capture("annotation_target_armed", new JsonObject((JsonNodeOptions?)null)
		{
			["label"] = label ?? "target",
			["is_hover"] = isHover,
			["follow_up_round"] = followUpRound
		});
	}

	public static void TrackAnnotationTargetCompleted(string? label, bool isHover, bool completedByHoverRest, int followUpRound)
	{
		Capture("annotation_target_completed", new JsonObject((JsonNodeOptions?)null)
		{
			["label"] = label ?? "target",
			["is_hover"] = isHover,
			["completed_by_hover_rest"] = completedByHoverRest,
			["follow_up_round"] = followUpRound
		});
	}

	public static void TrackAnnotationTargetMissed(string? label, int missCount, bool sameDisplay)
	{
		Capture("annotation_target_missed", new JsonObject((JsonNodeOptions?)null)
		{
			["label"] = label ?? "target",
			["miss_count"] = missCount,
			["same_display"] = sameDisplay
		});
	}

	public static void TrackAnnotationFollowUpRequested(bool clickedOnTarget, int followUpRound)
	{
		Capture("annotation_follow_up_requested", new JsonObject((JsonNodeOptions?)null)
		{
			["clicked_on_target"] = clickedOnTarget,
			["follow_up_round"] = followUpRound
		});
	}

	public static void TrackTextInjectionCompleted(string outcome, int characterCount, int wordCount, string source, string? targetAppName = null, string? failureReason = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["outcome"] = outcome,
			["character_count"] = characterCount,
			["word_count"] = wordCount,
			["source"] = source
		};
		if (!string.IsNullOrEmpty(targetAppName))
		{
			jsonObject["target_app_name"] = targetAppName;
		}
		if (!string.IsNullOrEmpty(failureReason))
		{
			jsonObject["failure_reason"] = failureReason;
		}
		WithFrontmostApplication(jsonObject);
		Capture("text_injection_completed", jsonObject);
		Breadcrumb("text_injection", "text_injection_completed", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null)
		{
			["outcome"] = outcome,
			["character_count"] = characterCount,
			["word_count"] = wordCount,
			["source"] = source
		});
	}

	public static string TextInjectionOutcomeName(string status, string? reason)
	{
		switch (status)
		{
		case "inserted":
			return "typed";
		case "copied_to_clipboard":
			return "copied_to_clipboard";
		case "unconfirmed":
			return "typed_unconfirmed";
		case "refused":
		{
			string result;
			switch (reason)
			{
			default:
				if (reason.Length <= 0)
				{
					goto case null;
				}
				result = "refused_" + reason;
				break;
			case "secure_field":
				result = "refused_secure_field";
				break;
			case "address_bar":
				result = "refused_address_bar";
				break;
			case "no_focused_text_field":
				result = "refused_no_text_input";
				break;
			case null:
				result = "refused_no_text_input";
				break;
			}
			return result;
		}
		default:
			return "failed";
		}
	}

	public static void TrackClipboardCopyCompleted(string source, int characterCount, int wordCount, bool succeeded)
	{
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["character_count"] = characterCount,
			["word_count"] = wordCount,
			["succeeded"] = succeeded
		};
		WithFrontmostApplication(properties);
		Capture("clipboard_copy_completed", properties);
		Breadcrumb("clipboard_copy", "clipboard_copy_completed", CrashReportLevel.Info, new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["character_count"] = characterCount,
			["word_count"] = wordCount,
			["succeeded"] = succeeded
		});
	}

	public static int WordCount(string text)
	{
		return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
