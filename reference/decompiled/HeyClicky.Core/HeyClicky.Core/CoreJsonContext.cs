using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, UseStringEnumConverter = true)]
[JsonSerializable(typeof(PaywallPayload))]
[JsonSerializable(typeof(StoredAuthSession))]
[JsonSerializable(typeof(StoredPendingSignIn))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(PlanSnapshot))]
[JsonSerializable(typeof(AppConfigResponse))]
[JsonSerializable(typeof(DeviceRegistrationRequest))]
[JsonSerializable(typeof(DeviceResponse))]
[JsonSerializable(typeof(DevicesResponse))]
[JsonSerializable(typeof(DiscoveryChannelRequest))]
[JsonSerializable(typeof(DiscoveryChannelResponse))]
[JsonSerializable(typeof(UserMemoryResponse))]
[JsonSerializable(typeof(SaveMemoryRequest))]
[JsonSerializable(typeof(SaveMemoryResponse))]
[JsonSerializable(typeof(ErrorReport))]
[JsonSerializable(typeof(StarterPlanRequest))]
[JsonSerializable(typeof(StarterPlanResponse))]
[JsonSerializable(typeof(ImageSearchRequest))]
[JsonSerializable(typeof(ImageSearchResponse))]
[JsonSerializable(typeof(WebSearchRequest))]
[JsonSerializable(typeof(WebSearchResponse))]
[JsonSerializable(typeof(CheckoutRequest))]
[JsonSerializable(typeof(CheckoutResponse))]
[JsonSerializable(typeof(PortalResponse))]
[JsonSerializable(typeof(ReferralClaimRequest))]
[JsonSerializable(typeof(ReferralCodeRequest))]
[JsonSerializable(typeof(ReferralOutcomeResponse))]
[JsonSerializable(typeof(RealtimeSessionRequest))]
[JsonSerializable(typeof(DeepgramTokenResponse))]
[JsonSerializable(typeof(EarsTranscribeRequest))]
[JsonSerializable(typeof(EarsTranscribeResponse))]
[JsonSerializable(typeof(OnboardingProgress))]
[JsonSerializable(typeof(DeviceRegistrationStamp))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
internal sealed class CoreJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<bool?>? _NullableBoolean;

	private JsonTypeInfo<double>? _Double;

	private JsonTypeInfo<double?>? _NullableDouble;

	private JsonTypeInfo<StoredAuthSession>? _StoredAuthSession;

	private JsonTypeInfo<StoredPendingSignIn>? _StoredPendingSignIn;

	private JsonTypeInfo<PaywallPayload>? _PaywallPayload;

	private JsonTypeInfo<PaywallPlanOption>? _PaywallPlanOption;

	private JsonTypeInfo<OnboardingProgress>? _OnboardingProgress;

	private JsonTypeInfo<AppSettings>? _AppSettings;

	private JsonTypeInfo<AppConfig>? _AppConfig;

	private JsonTypeInfo<AppConfigLinks>? _AppConfigLinks;

	private JsonTypeInfo<AppConfigResponse>? _AppConfigResponse;

	private JsonTypeInfo<CheckoutRequest>? _CheckoutRequest;

	private JsonTypeInfo<CheckoutResponse>? _CheckoutResponse;

	private JsonTypeInfo<DeepgramTokenResponse>? _DeepgramTokenResponse;

	private JsonTypeInfo<DeviceRegistrationRequest>? _DeviceRegistrationRequest;

	private JsonTypeInfo<DeviceRegistrationStamp>? _DeviceRegistrationStamp;

	private JsonTypeInfo<DeviceResponse>? _DeviceResponse;

	private JsonTypeInfo<DevicesResponse>? _DevicesResponse;

	private JsonTypeInfo<DiscoveryChannelRequest>? _DiscoveryChannelRequest;

	private JsonTypeInfo<DiscoveryChannelResponse>? _DiscoveryChannelResponse;

	private JsonTypeInfo<EarsTranscribeRequest>? _EarsTranscribeRequest;

	private JsonTypeInfo<EarsTranscribeResponse>? _EarsTranscribeResponse;

	private JsonTypeInfo<ErrorReport>? _ErrorReport;

	private JsonTypeInfo<ImageSearchRequest>? _ImageSearchRequest;

	private JsonTypeInfo<ImageSearchResponse>? _ImageSearchResponse;

	private JsonTypeInfo<ImageSearchResult>? _ImageSearchResult;

	private JsonTypeInfo<InterviewLine>? _InterviewLine;

	private JsonTypeInfo<PlanSnapshot>? _PlanSnapshot;

	private JsonTypeInfo<PlanSubscription>? _PlanSubscription;

	private JsonTypeInfo<PlanUsage>? _PlanUsage;

	private JsonTypeInfo<PlanUsageBucket>? _PlanUsageBucket;

	private JsonTypeInfo<PortalResponse>? _PortalResponse;

	private JsonTypeInfo<RealtimeSessionPromptContext>? _RealtimeSessionPromptContext;

	private JsonTypeInfo<RealtimeSessionRequest>? _RealtimeSessionRequest;

	private JsonTypeInfo<ReferralClaimRequest>? _ReferralClaimRequest;

	private JsonTypeInfo<ReferralCodeRequest>? _ReferralCodeRequest;

	private JsonTypeInfo<ReferralOutcomeResponse>? _ReferralOutcomeResponse;

	private JsonTypeInfo<RegisteredDevice>? _RegisteredDevice;

	private JsonTypeInfo<SaveMemoryRequest>? _SaveMemoryRequest;

	private JsonTypeInfo<SaveMemoryResponse>? _SaveMemoryResponse;

	private JsonTypeInfo<StarterPlanClicky>? _StarterPlanClicky;

	private JsonTypeInfo<StarterPlanClickyInput>? _StarterPlanClickyInput;

	private JsonTypeInfo<StarterPlanClickyRoutine>? _StarterPlanClickyRoutine;

	private JsonTypeInfo<StarterPlanRequest>? _StarterPlanRequest;

	private JsonTypeInfo<StarterPlanResponse>? _StarterPlanResponse;

	private JsonTypeInfo<StarterPlanRoutine>? _StarterPlanRoutine;

	private JsonTypeInfo<StarterPlanSuggestion>? _StarterPlanSuggestion;

	private JsonTypeInfo<UserMemoryResponse>? _UserMemoryResponse;

	private JsonTypeInfo<WebSearchRequest>? _WebSearchRequest;

	private JsonTypeInfo<WebSearchResponse>? _WebSearchResponse;

	private JsonTypeInfo<WebSearchResult>? _WebSearchResult;

	private JsonTypeInfo<HotkeyBinding>? _HotkeyBinding;

	private JsonTypeInfo<HotkeyBindings>? _HotkeyBindings;

	private JsonTypeInfo<HotkeyModifiers>? _HotkeyModifiers;

	private JsonTypeInfo<HotkeyTrigger>? _HotkeyTrigger;

	private JsonTypeInfo<Dictionary<string, string>>? _DictionaryStringString;

	private JsonTypeInfo<IReadOnlyList<PaywallPlanOption>>? _IReadOnlyListPaywallPlanOption;

	private JsonTypeInfo<IReadOnlyList<ImageSearchResult>>? _IReadOnlyListImageSearchResult;

	private JsonTypeInfo<IReadOnlyList<InterviewLine>>? _IReadOnlyListInterviewLine;

	private JsonTypeInfo<IReadOnlyList<RegisteredDevice>>? _IReadOnlyListRegisteredDevice;

	private JsonTypeInfo<IReadOnlyList<StarterPlanClicky>>? _IReadOnlyListStarterPlanClicky;

	private JsonTypeInfo<IReadOnlyList<StarterPlanClickyInput>>? _IReadOnlyListStarterPlanClickyInput;

	private JsonTypeInfo<IReadOnlyList<StarterPlanRoutine>>? _IReadOnlyListStarterPlanRoutine;

	private JsonTypeInfo<IReadOnlyList<StarterPlanSuggestion>>? _IReadOnlyListStarterPlanSuggestion;

	private JsonTypeInfo<IReadOnlyList<WebSearchResult>>? _IReadOnlyListWebSearchResult;

	private JsonTypeInfo<IReadOnlyList<JsonElement>>? _IReadOnlyListJsonElement;

	private JsonTypeInfo<IReadOnlyList<string>>? _IReadOnlyListString;

	private JsonTypeInfo<List<string>>? _ListString;

	private JsonTypeInfo<JsonElement>? _JsonElement;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<int?>? _NullableInt32;

	private JsonTypeInfo<long>? _Int64;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_access_token = JsonEncodedText.Encode("access_token");

	private static readonly JsonEncodedText PropName_refresh_token = JsonEncodedText.Encode("refresh_token");

	private static readonly JsonEncodedText PropName_user_id = JsonEncodedText.Encode("user_id");

	private static readonly JsonEncodedText PropName_email = JsonEncodedText.Encode("email");

	private static readonly JsonEncodedText PropName_display_name = JsonEncodedText.Encode("display_name");

	private static readonly JsonEncodedText PropName_avatar_url = JsonEncodedText.Encode("avatar_url");

	private static readonly JsonEncodedText PropName_signup_date_epoch_seconds = JsonEncodedText.Encode("signup_date_epoch_seconds");

	private static readonly JsonEncodedText PropName_state = JsonEncodedText.Encode("state");

	private static readonly JsonEncodedText PropName_started_at_epoch_seconds = JsonEncodedText.Encode("started_at_epoch_seconds");

	private static readonly JsonEncodedText PropName_reason = JsonEncodedText.Encode("reason");

	private static readonly JsonEncodedText PropName_surface = JsonEncodedText.Encode("surface");

	private static readonly JsonEncodedText PropName_title = JsonEncodedText.Encode("title");

	private static readonly JsonEncodedText PropName_body = JsonEncodedText.Encode("body");

	private static readonly JsonEncodedText PropName_cta_label = JsonEncodedText.Encode("cta_label");

	private static readonly JsonEncodedText PropName_price_display = JsonEncodedText.Encode("price_display");

	private static readonly JsonEncodedText PropName_current_usage = JsonEncodedText.Encode("current_usage");

	private static readonly JsonEncodedText PropName_limit = JsonEncodedText.Encode("limit");

	private static readonly JsonEncodedText PropName_reset_at = JsonEncodedText.Encode("reset_at");

	private static readonly JsonEncodedText PropName_plans = JsonEncodedText.Encode("plans");

	private static readonly JsonEncodedText PropName_yearly_savings_label = JsonEncodedText.Encode("yearly_savings_label");

	private static readonly JsonEncodedText PropName_default_interval = JsonEncodedText.Encode("default_interval");

	private static readonly JsonEncodedText PropName_key = JsonEncodedText.Encode("key");

	private static readonly JsonEncodedText PropName_name = JsonEncodedText.Encode("name");

	private static readonly JsonEncodedText PropName_agents_cap = JsonEncodedText.Encode("agents_cap");

	private static readonly JsonEncodedText PropName_tagline = JsonEncodedText.Encode("tagline");

	private static readonly JsonEncodedText PropName_recommended = JsonEncodedText.Encode("recommended");

	private static readonly JsonEncodedText PropName_yearly_price_display = JsonEncodedText.Encode("yearly_price_display");

	private static readonly JsonEncodedText PropName_yearly_billed_summary = JsonEncodedText.Encode("yearly_billed_summary");

	private static readonly JsonEncodedText PropName_pre_sign_in_intro_dismissed = JsonEncodedText.Encode("pre_sign_in_intro_dismissed");

	private static readonly JsonEncodedText PropName_has_acknowledged_permission_intro = JsonEncodedText.Encode("has_acknowledged_permission_intro");

	private static readonly JsonEncodedText PropName_has_completed_onboarding = JsonEncodedText.Encode("has_completed_onboarding");

	private static readonly JsonEncodedText PropName_tutorial_completed_at_epoch_seconds = JsonEncodedText.Encode("tutorial_completed_at_epoch_seconds");

	private static readonly JsonEncodedText PropName_discovery_channel = JsonEncodedText.Encode("discovery_channel");

	private static readonly JsonEncodedText PropName_discovery_channel_other = JsonEncodedText.Encode("discovery_channel_other");

	private static readonly JsonEncodedText PropName_discovery_channel_synced = JsonEncodedText.Encode("discovery_channel_synced");

	private static readonly JsonEncodedText PropName_squad_pack_id = JsonEncodedText.Encode("squad_pack_id");

	private static readonly JsonEncodedText PropName_cursor_color_hex = JsonEncodedText.Encode("cursor_color_hex");

	private static readonly JsonEncodedText PropName_squad_member_id = JsonEncodedText.Encode("squad_member_id");

	private static readonly JsonEncodedText PropName_voice_identifier = JsonEncodedText.Encode("voice_identifier");

	private static readonly JsonEncodedText PropName_speech_speed = JsonEncodedText.Encode("speech_speed");

	private static readonly JsonEncodedText PropName_voice_language_code = JsonEncodedText.Encode("voice_language_code");

	private static readonly JsonEncodedText PropName_screen_highlight_enabled = JsonEncodedText.Encode("screen_highlight_enabled");

	private static readonly JsonEncodedText PropName_cursor_docked = JsonEncodedText.Encode("cursor_docked");

	private static readonly JsonEncodedText PropName_shake_to_detach_enabled = JsonEncodedText.Encode("shake_to_detach_enabled");

	private static readonly JsonEncodedText PropName_launch_at_login = JsonEncodedText.Encode("launch_at_login");

	private static readonly JsonEncodedText PropName_show_in_taskbar = JsonEncodedText.Encode("show_in_taskbar");

	private static readonly JsonEncodedText PropName_typed_turns_respond_with_voice = JsonEncodedText.Encode("typed_turns_respond_with_voice");

	private static readonly JsonEncodedText PropName_dictation_language_code = JsonEncodedText.Encode("dictation_language_code");

	private static readonly JsonEncodedText PropName_dictation_specific_language_code = JsonEncodedText.Encode("dictation_specific_language_code");

	private static readonly JsonEncodedText PropName_dictation_last_completed_at_epoch_seconds = JsonEncodedText.Encode("dictation_last_completed_at_epoch_seconds");

	private static readonly JsonEncodedText PropName_dictation_custom_words = JsonEncodedText.Encode("dictation_custom_words");

	private static readonly JsonEncodedText PropName_dictation_claude_seeded = JsonEncodedText.Encode("dictation_claude_seeded");

	private static readonly JsonEncodedText PropName_dictation_account_name_seeded = JsonEncodedText.Encode("dictation_account_name_seeded");

	private static readonly JsonEncodedText PropName_discovery_channel_pending_sync = JsonEncodedText.Encode("discovery_channel_pending_sync");

	private static readonly JsonEncodedText PropName_microphone_device_name = JsonEncodedText.Encode("microphone_device_name");

	private static readonly JsonEncodedText PropName_agent_announcements_voice_enabled = JsonEncodedText.Encode("agent_announcements_voice_enabled");

	private static readonly JsonEncodedText PropName_document_access_notice_show_count = JsonEncodedText.Encode("document_access_notice_show_count");

	private static readonly JsonEncodedText PropName_document_access_notice_suppressed = JsonEncodedText.Encode("document_access_notice_suppressed");

	private static readonly JsonEncodedText PropName_onboarding = JsonEncodedText.Encode("onboarding");

	private static readonly JsonEncodedText PropName_hotkeys = JsonEncodedText.Encode("hotkeys");

	private static readonly JsonEncodedText PropName_microphone_device_id = JsonEncodedText.Encode("microphone_device_id");

	private static readonly JsonEncodedText PropName_output_device_id = JsonEncodedText.Encode("output_device_id");

	private static readonly JsonEncodedText PropName_drawing_sounds_enabled = JsonEncodedText.Encode("drawing_sounds_enabled");

	private static readonly JsonEncodedText PropName_drawing_voice_captions = JsonEncodedText.Encode("drawing_voice_captions");

	private static readonly JsonEncodedText PropName_keep_drawings_in_chat = JsonEncodedText.Encode("keep_drawings_in_chat");

	private static readonly JsonEncodedText PropName_drawing_pen_sounds_enabled = JsonEncodedText.Encode("drawing_pen_sounds_enabled");

	private static readonly JsonEncodedText PropName_drawing_java_script_jit = JsonEncodedText.Encode("drawing_java_script_jit");

	private static readonly JsonEncodedText PropName_drawing_subtitles = JsonEncodedText.Encode("drawing_subtitles");

	private static readonly JsonEncodedText PropName_save_drawings = JsonEncodedText.Encode("save_drawings");

	private static readonly JsonEncodedText PropName_show_agent_labels_in_activity_strip = JsonEncodedText.Encode("show_agent_labels_in_activity_strip");

	private static readonly JsonEncodedText PropName_show_in_screen_recordings = JsonEncodedText.Encode("show_in_screen_recordings");

	private static readonly JsonEncodedText PropName_agent_folder_path = JsonEncodedText.Encode("agent_folder_path");

	private static readonly JsonEncodedText PropName_obsidian_vault_path = JsonEncodedText.Encode("obsidian_vault_path");

	private static readonly JsonEncodedText PropName_agent_auto_approve_extra_usage = JsonEncodedText.Encode("agent_auto_approve_extra_usage");

	private static readonly JsonEncodedText PropName_agent_always_approve_file_uploads = JsonEncodedText.Encode("agent_always_approve_file_uploads");

	private static readonly JsonEncodedText PropName_agent_cursor_text_announcements_enabled = JsonEncodedText.Encode("agent_cursor_text_announcements_enabled");

	private static readonly JsonEncodedText PropName_one_brain_model = JsonEncodedText.Encode("one_brain_model");

	private static readonly JsonEncodedText PropName_one_brain_reads_apps = JsonEncodedText.Encode("one_brain_reads_apps");

	private static readonly JsonEncodedText PropName_one_brain_keeps_screens_in_history = JsonEncodedText.Encode("one_brain_keeps_screens_in_history");

	private static readonly JsonEncodedText PropName_show_one_brain_stats = JsonEncodedText.Encode("show_one_brain_stats");

	private static readonly JsonEncodedText PropName_morning_paper_enabled = JsonEncodedText.Encode("morning_paper_enabled");

	private static readonly JsonEncodedText PropName_stream_mode = JsonEncodedText.Encode("stream_mode");

	private static readonly JsonEncodedText PropName_developer_chat_models = JsonEncodedText.Encode("developer_chat_models");

	private static readonly JsonEncodedText PropName_has_shown_paywall_intro_video = JsonEncodedText.Encode("has_shown_paywall_intro_video");

	private static readonly JsonEncodedText PropName_proactive_agent_suggestions_enabled = JsonEncodedText.Encode("proactive_agent_suggestions_enabled");

	private static readonly JsonEncodedText PropName_upcoming_meetings_enabled = JsonEncodedText.Encode("upcoming_meetings_enabled");

	private static readonly JsonEncodedText PropName_onboarding_language_step_enabled = JsonEncodedText.Encode("onboarding_language_step_enabled");

	private static readonly JsonEncodedText PropName_remote_tasks_enabled = JsonEncodedText.Encode("remote_tasks_enabled");

	private static readonly JsonEncodedText PropName_proactive_debug_chord_enabled = JsonEncodedText.Encode("proactive_debug_chord_enabled");

	private static readonly JsonEncodedText PropName_proactive_debug_cadence = JsonEncodedText.Encode("proactive_debug_cadence");

	private static readonly JsonEncodedText PropName_live_notes_enabled = JsonEncodedText.Encode("live_notes_enabled");

	private static readonly JsonEncodedText PropName_live_notes_stream_patches = JsonEncodedText.Encode("live_notes_stream_patches");

	private static readonly JsonEncodedText PropName_links = JsonEncodedText.Encode("links");

	private static readonly JsonEncodedText PropName_featureRequest = JsonEncodedText.Encode("featureRequest");

	private static readonly JsonEncodedText PropName_communityReddit = JsonEncodedText.Encode("communityReddit");

	private static readonly JsonEncodedText PropName_communityWhatsApp = JsonEncodedText.Encode("communityWhatsApp");

	private static readonly JsonEncodedText PropName_changelog = JsonEncodedText.Encode("changelog");

	private static readonly JsonEncodedText PropName_appConfig = JsonEncodedText.Encode("appConfig");

	private static readonly JsonEncodedText PropName_plan = JsonEncodedText.Encode("plan");

	private static readonly JsonEncodedText PropName_interval = JsonEncodedText.Encode("interval");

	private static readonly JsonEncodedText PropName_checkout_ui = JsonEncodedText.Encode("checkout_ui");

	private static readonly JsonEncodedText PropName_discount_code = JsonEncodedText.Encode("discount_code");

	private static readonly JsonEncodedText PropName_url = JsonEncodedText.Encode("url");

	private static readonly JsonEncodedText PropName_discount_code_applied = JsonEncodedText.Encode("discount_code_applied");

	private static readonly JsonEncodedText PropName_model = JsonEncodedText.Encode("model");

	private static readonly JsonEncodedText PropName_receipt = JsonEncodedText.Encode("receipt");

	private static readonly JsonEncodedText PropName_capabilities = JsonEncodedText.Encode("capabilities");

	private static readonly JsonEncodedText PropName_fingerprint = JsonEncodedText.Encode("fingerprint");

	private static readonly JsonEncodedText PropName_registered_at_epoch_seconds = JsonEncodedText.Encode("registered_at_epoch_seconds");

	private static readonly JsonEncodedText PropName_device = JsonEncodedText.Encode("device");

	private static readonly JsonEncodedText PropName_devices = JsonEncodedText.Encode("devices");

	private static readonly JsonEncodedText PropName_audio_wav_base64 = JsonEncodedText.Encode("audio_wav_base64");

	private static readonly JsonEncodedText PropName_language = JsonEncodedText.Encode("language");

	private static readonly JsonEncodedText PropName_text = JsonEncodedText.Encode("text");

	private static readonly JsonEncodedText PropName_stage = JsonEncodedText.Encode("stage");

	private static readonly JsonEncodedText PropName_error_kind = JsonEncodedText.Encode("error_kind");

	private static readonly JsonEncodedText PropName_error_message = JsonEncodedText.Encode("error_message");

	private static readonly JsonEncodedText PropName_user_facing_message = JsonEncodedText.Encode("user_facing_message");

	private static readonly JsonEncodedText PropName_app_version = JsonEncodedText.Encode("app_version");

	private static readonly JsonEncodedText PropName_error_code = JsonEncodedText.Encode("error_code");

	private static readonly JsonEncodedText PropName_error_domain = JsonEncodedText.Encode("error_domain");

	private static readonly JsonEncodedText PropName_supabase_user_id = JsonEncodedText.Encode("supabase_user_id");

	private static readonly JsonEncodedText PropName_user_email = JsonEncodedText.Encode("user_email");

	private static readonly JsonEncodedText PropName_query = JsonEncodedText.Encode("query");

	private static readonly JsonEncodedText PropName_subject_photo = JsonEncodedText.Encode("subject_photo");

	private static readonly JsonEncodedText PropName_page_url = JsonEncodedText.Encode("page_url");

	private static readonly JsonEncodedText PropName_images = JsonEncodedText.Encode("images");

	private static readonly JsonEncodedText PropName_thumbnail_url = JsonEncodedText.Encode("thumbnail_url");

	private static readonly JsonEncodedText PropName_full_url = JsonEncodedText.Encode("full_url");

	private static readonly JsonEncodedText PropName_source_url = JsonEncodedText.Encode("source_url");

	private static readonly JsonEncodedText PropName_role = JsonEncodedText.Encode("role");

	private static readonly JsonEncodedText PropName_usage = JsonEncodedText.Encode("usage");

	private static readonly JsonEncodedText PropName_billing_enforcement = JsonEncodedText.Encode("billing_enforcement");

	private static readonly JsonEncodedText PropName_tier = JsonEncodedText.Encode("tier");

	private static readonly JsonEncodedText PropName_tier_source = JsonEncodedText.Encode("tier_source");

	private static readonly JsonEncodedText PropName_team_name = JsonEncodedText.Encode("team_name");

	private static readonly JsonEncodedText PropName_subscription = JsonEncodedText.Encode("subscription");

	private static readonly JsonEncodedText PropName_unlimited_agents_cap = JsonEncodedText.Encode("unlimited_agents_cap");

	private static readonly JsonEncodedText PropName_pro_agents_cap = JsonEncodedText.Encode("pro_agents_cap");

	private static readonly JsonEncodedText PropName_max_agents_cap = JsonEncodedText.Encode("max_agents_cap");

	private static readonly JsonEncodedText PropName_pro_yearly_available = JsonEncodedText.Encode("pro_yearly_available");

	private static readonly JsonEncodedText PropName_max_yearly_available = JsonEncodedText.Encode("max_yearly_available");

	private static readonly JsonEncodedText PropName_is_fail_open_fallback = JsonEncodedText.Encode("is_fail_open_fallback");

	private static readonly JsonEncodedText PropName_status = JsonEncodedText.Encode("status");

	private static readonly JsonEncodedText PropName_cancel_at_period_end = JsonEncodedText.Encode("cancel_at_period_end");

	private static readonly JsonEncodedText PropName_current_period_start = JsonEncodedText.Encode("current_period_start");

	private static readonly JsonEncodedText PropName_current_period_end = JsonEncodedText.Encode("current_period_end");

	private static readonly JsonEncodedText PropName_product_id = JsonEncodedText.Encode("product_id");

	private static readonly JsonEncodedText PropName_cancel_at = JsonEncodedText.Encode("cancel_at");

	private static readonly JsonEncodedText PropName_billing_interval = JsonEncodedText.Encode("billing_interval");

	private static readonly JsonEncodedText PropName_messages = JsonEncodedText.Encode("messages");

	private static readonly JsonEncodedText PropName_agents = JsonEncodedText.Encode("agents");

	private static readonly JsonEncodedText PropName_dictation = JsonEncodedText.Encode("dictation");

	private static readonly JsonEncodedText PropName_window_resets_at = JsonEncodedText.Encode("window_resets_at");

	private static readonly JsonEncodedText PropName_connected_integrations = JsonEncodedText.Encode("connected_integrations");

	private static readonly JsonEncodedText PropName_needs_reconnect_integrations = JsonEncodedText.Encode("needs_reconnect_integrations");

	private static readonly JsonEncodedText PropName_prior_message_count = JsonEncodedText.Encode("prior_message_count");

	private static readonly JsonEncodedText PropName_time_zone = JsonEncodedText.Encode("time_zone");

	private static readonly JsonEncodedText PropName_current_local_datetime = JsonEncodedText.Encode("current_local_datetime");

	private static readonly JsonEncodedText PropName_voice_gender = JsonEncodedText.Encode("voice_gender");

	private static readonly JsonEncodedText PropName_active_skills = JsonEncodedText.Encode("active_skills");

	private static readonly JsonEncodedText PropName_dictation_hold_shortcut = JsonEncodedText.Encode("dictation_hold_shortcut");

	private static readonly JsonEncodedText PropName_voice = JsonEncodedText.Encode("voice");

	private static readonly JsonEncodedText PropName_speed = JsonEncodedText.Encode("speed");

	private static readonly JsonEncodedText PropName_prompt_context = JsonEncodedText.Encode("prompt_context");

	private static readonly JsonEncodedText PropName_client_capabilities = JsonEncodedText.Encode("client_capabilities");

	private static readonly JsonEncodedText PropName_mouth_provider = JsonEncodedText.Encode("mouth_provider");

	private static readonly JsonEncodedText PropName_languages = JsonEncodedText.Encode("languages");

	private static readonly JsonEncodedText PropName_code = JsonEncodedText.Encode("code");

	private static readonly JsonEncodedText PropName_source = JsonEncodedText.Encode("source");

	private static readonly JsonEncodedText PropName_outcome = JsonEncodedText.Encode("outcome");

	private static readonly JsonEncodedText PropName_message = JsonEncodedText.Encode("message");

	private static readonly JsonEncodedText PropName_discount_percent = JsonEncodedText.Encode("discount_percent");

	private static readonly JsonEncodedText PropName_share_url = JsonEncodedText.Encode("share_url");

	private static readonly JsonEncodedText PropName_share_display_text = JsonEncodedText.Encode("share_display_text");

	private static readonly JsonEncodedText PropName_id = JsonEncodedText.Encode("id");

	private static readonly JsonEncodedText PropName_is_this_device = JsonEncodedText.Encode("is_this_device");

	private static readonly JsonEncodedText PropName_platform = JsonEncodedText.Encode("platform");

	private static readonly JsonEncodedText PropName_os_version = JsonEncodedText.Encode("os_version");

	private static readonly JsonEncodedText PropName_app_build = JsonEncodedText.Encode("app_build");

	private static readonly JsonEncodedText PropName_first_seen_at = JsonEncodedText.Encode("first_seen_at");

	private static readonly JsonEncodedText PropName_last_seen_at = JsonEncodedText.Encode("last_seen_at");

	private static readonly JsonEncodedText PropName_last_active_at = JsonEncodedText.Encode("last_active_at");

	private static readonly JsonEncodedText PropName_messages_last14_days = JsonEncodedText.Encode("messages_last14_days");

	private static readonly JsonEncodedText PropName_memory = JsonEncodedText.Encode("memory");

	private static readonly JsonEncodedText PropName_saved = JsonEncodedText.Encode("saved");

	private static readonly JsonEncodedText PropName_error = JsonEncodedText.Encode("error");

	private static readonly JsonEncodedText PropName_description = JsonEncodedText.Encode("description");

	private static readonly JsonEncodedText PropName_intro_messages = JsonEncodedText.Encode("intro_messages");

	private static readonly JsonEncodedText PropName_reveal_line = JsonEncodedText.Encode("reveal_line");

	private static readonly JsonEncodedText PropName_first_asks = JsonEncodedText.Encode("first_asks");

	private static readonly JsonEncodedText PropName_routine = JsonEncodedText.Encode("routine");

	private static readonly JsonEncodedText PropName_slug = JsonEncodedText.Encode("slug");

	private static readonly JsonEncodedText PropName_routines = JsonEncodedText.Encode("routines");

	private static readonly JsonEncodedText PropName_thread_ids = JsonEncodedText.Encode("thread_ids");

	private static readonly JsonEncodedText PropName_task_prompt = JsonEncodedText.Encode("task_prompt");

	private static readonly JsonEncodedText PropName_interval_minutes = JsonEncodedText.Encode("interval_minutes");

	private static readonly JsonEncodedText PropName_interview = JsonEncodedText.Encode("interview");

	private static readonly JsonEncodedText PropName_available_integrations = JsonEncodedText.Encode("available_integrations");

	private static readonly JsonEncodedText PropName_clickys = JsonEncodedText.Encode("clickys");

	private static readonly JsonEncodedText PropName_first_asks_for_clicky = JsonEncodedText.Encode("first_asks_for_clicky");

	private static readonly JsonEncodedText PropName_interview_saved = JsonEncodedText.Encode("interview_saved");

	private static readonly JsonEncodedText PropName_jobs = JsonEncodedText.Encode("jobs");

	private static readonly JsonEncodedText PropName_suggested_agents = JsonEncodedText.Encode("suggested_agents");

	private static readonly JsonEncodedText PropName_agent_familiarity = JsonEncodedText.Encode("agent_familiarity");

	private static readonly JsonEncodedText PropName_goal_line = JsonEncodedText.Encode("goal_line");

	private static readonly JsonEncodedText PropName_every_minutes = JsonEncodedText.Encode("every_minutes");

	private static readonly JsonEncodedText PropName_agent_prompt = JsonEncodedText.Encode("agent_prompt");

	private static readonly JsonEncodedText PropName_deliverable = JsonEncodedText.Encode("deliverable");

	private static readonly JsonEncodedText PropName_clicky_slug = JsonEncodedText.Encode("clicky_slug");

	private static readonly JsonEncodedText PropName_clicky_key = JsonEncodedText.Encode("clicky_key");

	private static readonly JsonEncodedText PropName_suggestion_id = JsonEncodedText.Encode("suggestion_id");

	private static readonly JsonEncodedText PropName_markdown = JsonEncodedText.Encode("markdown");

	private static readonly JsonEncodedText PropName_profile_markdown = JsonEncodedText.Encode("profile_markdown");

	private static readonly JsonEncodedText PropName_context_markdown = JsonEncodedText.Encode("context_markdown");

	private static readonly JsonEncodedText PropName_has_profile = JsonEncodedText.Encode("has_profile");

	private static readonly JsonEncodedText PropName_updated_at = JsonEncodedText.Encode("updated_at");

	private static readonly JsonEncodedText PropName_count = JsonEncodedText.Encode("count");

	private static readonly JsonEncodedText PropName_freshness = JsonEncodedText.Encode("freshness");

	private static readonly JsonEncodedText PropName_results = JsonEncodedText.Encode("results");

	private static readonly JsonEncodedText PropName_age = JsonEncodedText.Encode("age");

	private static readonly JsonEncodedText PropName_highlights = JsonEncodedText.Encode("highlights");

	private static readonly JsonEncodedText PropName_modifiers = JsonEncodedText.Encode("modifiers");

	private static readonly JsonEncodedText PropName_trigger = JsonEncodedText.Encode("trigger");

	private static readonly JsonEncodedText PropName_push_to_talk = JsonEncodedText.Encode("push_to_talk");

	private static readonly JsonEncodedText PropName_text_mode = JsonEncodedText.Encode("text_mode");

	private static readonly JsonEncodedText PropName_hands_free_dictation = JsonEncodedText.Encode("hands_free_dictation");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = (JsonTypeInfo<bool?>)base.Options.GetTypeInfo(typeof(bool?)));

	public JsonTypeInfo<double> Double => _Double ?? (_Double = (JsonTypeInfo<double>)base.Options.GetTypeInfo(typeof(double)));

	public JsonTypeInfo<double?> NullableDouble => _NullableDouble ?? (_NullableDouble = (JsonTypeInfo<double?>)base.Options.GetTypeInfo(typeof(double?)));

	public JsonTypeInfo<StoredAuthSession> StoredAuthSession => _StoredAuthSession ?? (_StoredAuthSession = (JsonTypeInfo<StoredAuthSession>)base.Options.GetTypeInfo(typeof(StoredAuthSession)));

	public JsonTypeInfo<StoredPendingSignIn> StoredPendingSignIn => _StoredPendingSignIn ?? (_StoredPendingSignIn = (JsonTypeInfo<StoredPendingSignIn>)base.Options.GetTypeInfo(typeof(StoredPendingSignIn)));

	public JsonTypeInfo<PaywallPayload> PaywallPayload => _PaywallPayload ?? (_PaywallPayload = (JsonTypeInfo<PaywallPayload>)base.Options.GetTypeInfo(typeof(PaywallPayload)));

	public JsonTypeInfo<PaywallPlanOption> PaywallPlanOption => _PaywallPlanOption ?? (_PaywallPlanOption = (JsonTypeInfo<PaywallPlanOption>)base.Options.GetTypeInfo(typeof(PaywallPlanOption)));

	public JsonTypeInfo<OnboardingProgress> OnboardingProgress => _OnboardingProgress ?? (_OnboardingProgress = (JsonTypeInfo<OnboardingProgress>)base.Options.GetTypeInfo(typeof(OnboardingProgress)));

	public JsonTypeInfo<AppSettings> AppSettings => _AppSettings ?? (_AppSettings = (JsonTypeInfo<AppSettings>)base.Options.GetTypeInfo(typeof(AppSettings)));

	public JsonTypeInfo<AppConfig> AppConfig => _AppConfig ?? (_AppConfig = (JsonTypeInfo<AppConfig>)base.Options.GetTypeInfo(typeof(AppConfig)));

	public JsonTypeInfo<AppConfigLinks> AppConfigLinks => _AppConfigLinks ?? (_AppConfigLinks = (JsonTypeInfo<AppConfigLinks>)base.Options.GetTypeInfo(typeof(AppConfigLinks)));

	public JsonTypeInfo<AppConfigResponse> AppConfigResponse => _AppConfigResponse ?? (_AppConfigResponse = (JsonTypeInfo<AppConfigResponse>)base.Options.GetTypeInfo(typeof(AppConfigResponse)));

	public JsonTypeInfo<CheckoutRequest> CheckoutRequest => _CheckoutRequest ?? (_CheckoutRequest = (JsonTypeInfo<CheckoutRequest>)base.Options.GetTypeInfo(typeof(CheckoutRequest)));

	public JsonTypeInfo<CheckoutResponse> CheckoutResponse => _CheckoutResponse ?? (_CheckoutResponse = (JsonTypeInfo<CheckoutResponse>)base.Options.GetTypeInfo(typeof(CheckoutResponse)));

	public JsonTypeInfo<DeepgramTokenResponse> DeepgramTokenResponse => _DeepgramTokenResponse ?? (_DeepgramTokenResponse = (JsonTypeInfo<DeepgramTokenResponse>)base.Options.GetTypeInfo(typeof(DeepgramTokenResponse)));

	public JsonTypeInfo<DeviceRegistrationRequest> DeviceRegistrationRequest => _DeviceRegistrationRequest ?? (_DeviceRegistrationRequest = (JsonTypeInfo<DeviceRegistrationRequest>)base.Options.GetTypeInfo(typeof(DeviceRegistrationRequest)));

	public JsonTypeInfo<DeviceRegistrationStamp> DeviceRegistrationStamp => _DeviceRegistrationStamp ?? (_DeviceRegistrationStamp = (JsonTypeInfo<DeviceRegistrationStamp>)base.Options.GetTypeInfo(typeof(DeviceRegistrationStamp)));

	public JsonTypeInfo<DeviceResponse> DeviceResponse => _DeviceResponse ?? (_DeviceResponse = (JsonTypeInfo<DeviceResponse>)base.Options.GetTypeInfo(typeof(DeviceResponse)));

	public JsonTypeInfo<DevicesResponse> DevicesResponse => _DevicesResponse ?? (_DevicesResponse = (JsonTypeInfo<DevicesResponse>)base.Options.GetTypeInfo(typeof(DevicesResponse)));

	public JsonTypeInfo<DiscoveryChannelRequest> DiscoveryChannelRequest => _DiscoveryChannelRequest ?? (_DiscoveryChannelRequest = (JsonTypeInfo<DiscoveryChannelRequest>)base.Options.GetTypeInfo(typeof(DiscoveryChannelRequest)));

	public JsonTypeInfo<DiscoveryChannelResponse> DiscoveryChannelResponse => _DiscoveryChannelResponse ?? (_DiscoveryChannelResponse = (JsonTypeInfo<DiscoveryChannelResponse>)base.Options.GetTypeInfo(typeof(DiscoveryChannelResponse)));

	public JsonTypeInfo<EarsTranscribeRequest> EarsTranscribeRequest => _EarsTranscribeRequest ?? (_EarsTranscribeRequest = (JsonTypeInfo<EarsTranscribeRequest>)base.Options.GetTypeInfo(typeof(EarsTranscribeRequest)));

	public JsonTypeInfo<EarsTranscribeResponse> EarsTranscribeResponse => _EarsTranscribeResponse ?? (_EarsTranscribeResponse = (JsonTypeInfo<EarsTranscribeResponse>)base.Options.GetTypeInfo(typeof(EarsTranscribeResponse)));

	public JsonTypeInfo<ErrorReport> ErrorReport => _ErrorReport ?? (_ErrorReport = (JsonTypeInfo<ErrorReport>)base.Options.GetTypeInfo(typeof(ErrorReport)));

	public JsonTypeInfo<ImageSearchRequest> ImageSearchRequest => _ImageSearchRequest ?? (_ImageSearchRequest = (JsonTypeInfo<ImageSearchRequest>)base.Options.GetTypeInfo(typeof(ImageSearchRequest)));

	public JsonTypeInfo<ImageSearchResponse> ImageSearchResponse => _ImageSearchResponse ?? (_ImageSearchResponse = (JsonTypeInfo<ImageSearchResponse>)base.Options.GetTypeInfo(typeof(ImageSearchResponse)));

	public JsonTypeInfo<ImageSearchResult> ImageSearchResult => _ImageSearchResult ?? (_ImageSearchResult = (JsonTypeInfo<ImageSearchResult>)base.Options.GetTypeInfo(typeof(ImageSearchResult)));

	public JsonTypeInfo<InterviewLine> InterviewLine => _InterviewLine ?? (_InterviewLine = (JsonTypeInfo<InterviewLine>)base.Options.GetTypeInfo(typeof(InterviewLine)));

	public JsonTypeInfo<PlanSnapshot> PlanSnapshot => _PlanSnapshot ?? (_PlanSnapshot = (JsonTypeInfo<PlanSnapshot>)base.Options.GetTypeInfo(typeof(PlanSnapshot)));

	public JsonTypeInfo<PlanSubscription> PlanSubscription => _PlanSubscription ?? (_PlanSubscription = (JsonTypeInfo<PlanSubscription>)base.Options.GetTypeInfo(typeof(PlanSubscription)));

	public JsonTypeInfo<PlanUsage> PlanUsage => _PlanUsage ?? (_PlanUsage = (JsonTypeInfo<PlanUsage>)base.Options.GetTypeInfo(typeof(PlanUsage)));

	public JsonTypeInfo<PlanUsageBucket> PlanUsageBucket => _PlanUsageBucket ?? (_PlanUsageBucket = (JsonTypeInfo<PlanUsageBucket>)base.Options.GetTypeInfo(typeof(PlanUsageBucket)));

	public JsonTypeInfo<PortalResponse> PortalResponse => _PortalResponse ?? (_PortalResponse = (JsonTypeInfo<PortalResponse>)base.Options.GetTypeInfo(typeof(PortalResponse)));

	public JsonTypeInfo<RealtimeSessionPromptContext> RealtimeSessionPromptContext => _RealtimeSessionPromptContext ?? (_RealtimeSessionPromptContext = (JsonTypeInfo<RealtimeSessionPromptContext>)base.Options.GetTypeInfo(typeof(RealtimeSessionPromptContext)));

	public JsonTypeInfo<RealtimeSessionRequest> RealtimeSessionRequest => _RealtimeSessionRequest ?? (_RealtimeSessionRequest = (JsonTypeInfo<RealtimeSessionRequest>)base.Options.GetTypeInfo(typeof(RealtimeSessionRequest)));

	public JsonTypeInfo<ReferralClaimRequest> ReferralClaimRequest => _ReferralClaimRequest ?? (_ReferralClaimRequest = (JsonTypeInfo<ReferralClaimRequest>)base.Options.GetTypeInfo(typeof(ReferralClaimRequest)));

	public JsonTypeInfo<ReferralCodeRequest> ReferralCodeRequest => _ReferralCodeRequest ?? (_ReferralCodeRequest = (JsonTypeInfo<ReferralCodeRequest>)base.Options.GetTypeInfo(typeof(ReferralCodeRequest)));

	public JsonTypeInfo<ReferralOutcomeResponse> ReferralOutcomeResponse => _ReferralOutcomeResponse ?? (_ReferralOutcomeResponse = (JsonTypeInfo<ReferralOutcomeResponse>)base.Options.GetTypeInfo(typeof(ReferralOutcomeResponse)));

	public JsonTypeInfo<RegisteredDevice> RegisteredDevice => _RegisteredDevice ?? (_RegisteredDevice = (JsonTypeInfo<RegisteredDevice>)base.Options.GetTypeInfo(typeof(RegisteredDevice)));

	public JsonTypeInfo<SaveMemoryRequest> SaveMemoryRequest => _SaveMemoryRequest ?? (_SaveMemoryRequest = (JsonTypeInfo<SaveMemoryRequest>)base.Options.GetTypeInfo(typeof(SaveMemoryRequest)));

	public JsonTypeInfo<SaveMemoryResponse> SaveMemoryResponse => _SaveMemoryResponse ?? (_SaveMemoryResponse = (JsonTypeInfo<SaveMemoryResponse>)base.Options.GetTypeInfo(typeof(SaveMemoryResponse)));

	public JsonTypeInfo<StarterPlanClicky> StarterPlanClicky => _StarterPlanClicky ?? (_StarterPlanClicky = (JsonTypeInfo<StarterPlanClicky>)base.Options.GetTypeInfo(typeof(StarterPlanClicky)));

	public JsonTypeInfo<StarterPlanClickyInput> StarterPlanClickyInput => _StarterPlanClickyInput ?? (_StarterPlanClickyInput = (JsonTypeInfo<StarterPlanClickyInput>)base.Options.GetTypeInfo(typeof(StarterPlanClickyInput)));

	public JsonTypeInfo<StarterPlanClickyRoutine> StarterPlanClickyRoutine => _StarterPlanClickyRoutine ?? (_StarterPlanClickyRoutine = (JsonTypeInfo<StarterPlanClickyRoutine>)base.Options.GetTypeInfo(typeof(StarterPlanClickyRoutine)));

	public JsonTypeInfo<StarterPlanRequest> StarterPlanRequest => _StarterPlanRequest ?? (_StarterPlanRequest = (JsonTypeInfo<StarterPlanRequest>)base.Options.GetTypeInfo(typeof(StarterPlanRequest)));

	public JsonTypeInfo<StarterPlanResponse> StarterPlanResponse => _StarterPlanResponse ?? (_StarterPlanResponse = (JsonTypeInfo<StarterPlanResponse>)base.Options.GetTypeInfo(typeof(StarterPlanResponse)));

	public JsonTypeInfo<StarterPlanRoutine> StarterPlanRoutine => _StarterPlanRoutine ?? (_StarterPlanRoutine = (JsonTypeInfo<StarterPlanRoutine>)base.Options.GetTypeInfo(typeof(StarterPlanRoutine)));

	public JsonTypeInfo<StarterPlanSuggestion> StarterPlanSuggestion => _StarterPlanSuggestion ?? (_StarterPlanSuggestion = (JsonTypeInfo<StarterPlanSuggestion>)base.Options.GetTypeInfo(typeof(StarterPlanSuggestion)));

	public JsonTypeInfo<UserMemoryResponse> UserMemoryResponse => _UserMemoryResponse ?? (_UserMemoryResponse = (JsonTypeInfo<UserMemoryResponse>)base.Options.GetTypeInfo(typeof(UserMemoryResponse)));

	public JsonTypeInfo<WebSearchRequest> WebSearchRequest => _WebSearchRequest ?? (_WebSearchRequest = (JsonTypeInfo<WebSearchRequest>)base.Options.GetTypeInfo(typeof(WebSearchRequest)));

	public JsonTypeInfo<WebSearchResponse> WebSearchResponse => _WebSearchResponse ?? (_WebSearchResponse = (JsonTypeInfo<WebSearchResponse>)base.Options.GetTypeInfo(typeof(WebSearchResponse)));

	public JsonTypeInfo<WebSearchResult> WebSearchResult => _WebSearchResult ?? (_WebSearchResult = (JsonTypeInfo<WebSearchResult>)base.Options.GetTypeInfo(typeof(WebSearchResult)));

	public JsonTypeInfo<HotkeyBinding> HotkeyBinding => _HotkeyBinding ?? (_HotkeyBinding = (JsonTypeInfo<HotkeyBinding>)base.Options.GetTypeInfo(typeof(HotkeyBinding)));

	public JsonTypeInfo<HotkeyBindings> HotkeyBindings => _HotkeyBindings ?? (_HotkeyBindings = (JsonTypeInfo<HotkeyBindings>)base.Options.GetTypeInfo(typeof(HotkeyBindings)));

	public JsonTypeInfo<HotkeyModifiers> HotkeyModifiers => _HotkeyModifiers ?? (_HotkeyModifiers = (JsonTypeInfo<HotkeyModifiers>)base.Options.GetTypeInfo(typeof(HotkeyModifiers)));

	public JsonTypeInfo<HotkeyTrigger> HotkeyTrigger => _HotkeyTrigger ?? (_HotkeyTrigger = (JsonTypeInfo<HotkeyTrigger>)base.Options.GetTypeInfo(typeof(HotkeyTrigger)));

	public JsonTypeInfo<Dictionary<string, string>> DictionaryStringString => _DictionaryStringString ?? (_DictionaryStringString = (JsonTypeInfo<Dictionary<string, string>>)base.Options.GetTypeInfo(typeof(Dictionary<string, string>)));

	public JsonTypeInfo<IReadOnlyList<PaywallPlanOption>> IReadOnlyListPaywallPlanOption => _IReadOnlyListPaywallPlanOption ?? (_IReadOnlyListPaywallPlanOption = (JsonTypeInfo<IReadOnlyList<PaywallPlanOption>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<PaywallPlanOption>)));

	public JsonTypeInfo<IReadOnlyList<ImageSearchResult>> IReadOnlyListImageSearchResult => _IReadOnlyListImageSearchResult ?? (_IReadOnlyListImageSearchResult = (JsonTypeInfo<IReadOnlyList<ImageSearchResult>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<ImageSearchResult>)));

	public JsonTypeInfo<IReadOnlyList<InterviewLine>> IReadOnlyListInterviewLine => _IReadOnlyListInterviewLine ?? (_IReadOnlyListInterviewLine = (JsonTypeInfo<IReadOnlyList<InterviewLine>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<InterviewLine>)));

	public JsonTypeInfo<IReadOnlyList<RegisteredDevice>> IReadOnlyListRegisteredDevice => _IReadOnlyListRegisteredDevice ?? (_IReadOnlyListRegisteredDevice = (JsonTypeInfo<IReadOnlyList<RegisteredDevice>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<RegisteredDevice>)));

	public JsonTypeInfo<IReadOnlyList<StarterPlanClicky>> IReadOnlyListStarterPlanClicky => _IReadOnlyListStarterPlanClicky ?? (_IReadOnlyListStarterPlanClicky = (JsonTypeInfo<IReadOnlyList<StarterPlanClicky>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<StarterPlanClicky>)));

	public JsonTypeInfo<IReadOnlyList<StarterPlanClickyInput>> IReadOnlyListStarterPlanClickyInput => _IReadOnlyListStarterPlanClickyInput ?? (_IReadOnlyListStarterPlanClickyInput = (JsonTypeInfo<IReadOnlyList<StarterPlanClickyInput>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<StarterPlanClickyInput>)));

	public JsonTypeInfo<IReadOnlyList<StarterPlanRoutine>> IReadOnlyListStarterPlanRoutine => _IReadOnlyListStarterPlanRoutine ?? (_IReadOnlyListStarterPlanRoutine = (JsonTypeInfo<IReadOnlyList<StarterPlanRoutine>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<StarterPlanRoutine>)));

	public JsonTypeInfo<IReadOnlyList<StarterPlanSuggestion>> IReadOnlyListStarterPlanSuggestion => _IReadOnlyListStarterPlanSuggestion ?? (_IReadOnlyListStarterPlanSuggestion = (JsonTypeInfo<IReadOnlyList<StarterPlanSuggestion>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<StarterPlanSuggestion>)));

	public JsonTypeInfo<IReadOnlyList<WebSearchResult>> IReadOnlyListWebSearchResult => _IReadOnlyListWebSearchResult ?? (_IReadOnlyListWebSearchResult = (JsonTypeInfo<IReadOnlyList<WebSearchResult>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<WebSearchResult>)));

	public JsonTypeInfo<IReadOnlyList<JsonElement>> IReadOnlyListJsonElement => _IReadOnlyListJsonElement ?? (_IReadOnlyListJsonElement = (JsonTypeInfo<IReadOnlyList<JsonElement>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<JsonElement>)));

	public JsonTypeInfo<IReadOnlyList<string>> IReadOnlyListString => _IReadOnlyListString ?? (_IReadOnlyListString = (JsonTypeInfo<IReadOnlyList<string>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<string>)));

	public JsonTypeInfo<List<string>> ListString => _ListString ?? (_ListString = (JsonTypeInfo<List<string>>)base.Options.GetTypeInfo(typeof(List<string>)));

	public JsonTypeInfo<JsonElement> JsonElement => _JsonElement ?? (_JsonElement = (JsonTypeInfo<JsonElement>)base.Options.GetTypeInfo(typeof(JsonElement)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<int?> NullableInt32 => _NullableInt32 ?? (_NullableInt32 = (JsonTypeInfo<int?>)base.Options.GetTypeInfo(typeof(int?)));

	public JsonTypeInfo<long> Int64 => _Int64 ?? (_Int64 = (JsonTypeInfo<long>)base.Options.GetTypeInfo(typeof(long)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static CoreJsonContext Default { get; } = new CoreJsonContext(new JsonSerializerOptions(s_defaultOptions));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<bool?> Create_NullableBoolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<bool>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<double> Create_Double(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<double> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<double>(options, JsonMetadataServices.DoubleConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<double?> Create_NullableDouble(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<double?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<double>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<double?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<StoredAuthSession> Create_StoredAuthSession(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StoredAuthSession> jsonTypeInfo))
		{
			JsonObjectInfoValues<StoredAuthSession> objectInfo = new JsonObjectInfoValues<StoredAuthSession>
			{
				ObjectCreator = () => new StoredAuthSession(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => StoredAuthSessionPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(StoredAuthSession).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = StoredAuthSessionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StoredAuthSessionPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).AccessToken,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).AccessToken = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AccessToken",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("AccessToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).RefreshToken,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).RefreshToken = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RefreshToken",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("RefreshToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).UserId,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).UserId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UserId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("UserId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).Email,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).Email = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Email",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("Email", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).DisplayName,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).DisplayName = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DisplayName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).AvatarUrl,
			Setter = delegate(object obj, string? value)
			{
				((StoredAuthSession)obj).AvatarUrl = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AvatarUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("AvatarUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<double?> propertyInfo7 = new JsonPropertyInfoValues<double?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredAuthSession),
			Converter = null,
			Getter = (object obj) => ((StoredAuthSession)obj).SignupDateEpochSeconds,
			Setter = delegate(object obj, double? value)
			{
				((StoredAuthSession)obj).SignupDateEpochSeconds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SignupDateEpochSeconds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredAuthSession).GetProperty("SignupDateEpochSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double?), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void StoredAuthSessionSerializeHandler(Utf8JsonWriter writer, StoredAuthSession? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string accessToken = value.AccessToken;
		if (accessToken != null)
		{
			writer.WriteString(PropName_access_token, accessToken);
		}
		string refreshToken = value.RefreshToken;
		if (refreshToken != null)
		{
			writer.WriteString(PropName_refresh_token, refreshToken);
		}
		string userId = value.UserId;
		if (userId != null)
		{
			writer.WriteString(PropName_user_id, userId);
		}
		string email = value.Email;
		if (email != null)
		{
			writer.WriteString(PropName_email, email);
		}
		string displayName = value.DisplayName;
		if (displayName != null)
		{
			writer.WriteString(PropName_display_name, displayName);
		}
		string avatarUrl = value.AvatarUrl;
		if (avatarUrl != null)
		{
			writer.WriteString(PropName_avatar_url, avatarUrl);
		}
		double? signupDateEpochSeconds = value.SignupDateEpochSeconds;
		if (signupDateEpochSeconds.HasValue)
		{
			writer.WritePropertyName(PropName_signup_date_epoch_seconds);
			JsonSerializer.Serialize(writer, signupDateEpochSeconds, NullableDouble);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<StoredPendingSignIn> Create_StoredPendingSignIn(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StoredPendingSignIn> jsonTypeInfo))
		{
			JsonObjectInfoValues<StoredPendingSignIn> objectInfo = new JsonObjectInfoValues<StoredPendingSignIn>
			{
				ObjectCreator = () => new StoredPendingSignIn(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => StoredPendingSignInPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(StoredPendingSignIn).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = StoredPendingSignInSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StoredPendingSignInPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredPendingSignIn),
			Converter = null,
			Getter = (object obj) => ((StoredPendingSignIn)obj).State,
			Setter = delegate(object obj, string? value)
			{
				((StoredPendingSignIn)obj).State = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "State",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredPendingSignIn).GetProperty("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<double?> propertyInfo2 = new JsonPropertyInfoValues<double?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StoredPendingSignIn),
			Converter = null,
			Getter = (object obj) => ((StoredPendingSignIn)obj).StartedAtEpochSeconds,
			Setter = delegate(object obj, double? value)
			{
				((StoredPendingSignIn)obj).StartedAtEpochSeconds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StartedAtEpochSeconds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StoredPendingSignIn).GetProperty("StartedAtEpochSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void StoredPendingSignInSerializeHandler(Utf8JsonWriter writer, StoredPendingSignIn? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string state = value.State;
		if (state != null)
		{
			writer.WriteString(PropName_state, state);
		}
		double? startedAtEpochSeconds = value.StartedAtEpochSeconds;
		if (startedAtEpochSeconds.HasValue)
		{
			writer.WritePropertyName(PropName_started_at_epoch_seconds);
			JsonSerializer.Serialize(writer, startedAtEpochSeconds, NullableDouble);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<PaywallPayload> Create_PaywallPayload(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PaywallPayload> jsonTypeInfo))
		{
			JsonObjectInfoValues<PaywallPayload> objectInfo = new JsonObjectInfoValues<PaywallPayload>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PaywallPayload((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (string)args[5], (int)args[6], (int)args[7], (string)args[8], (IReadOnlyList<PaywallPlanOption>)args[9], (string)args[10], (string)args[11]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PaywallPayloadPropInit(options),
				ConstructorParameterMetadataInitializer = PaywallPayloadCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PaywallPayload).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[12]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(int),
					typeof(int),
					typeof(string),
					typeof(IReadOnlyList<PaywallPlanOption>),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = PaywallPayloadSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PaywallPayloadPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[15];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Reason,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Reason",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Reason", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Surface,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Surface",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Surface", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Body,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Body",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Body", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).CtaLabel,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CtaLabel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("CtaLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).PriceDisplay,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PriceDisplay",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("PriceDisplay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo7 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).CurrentUsage,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CurrentUsage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("CurrentUsage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<int> propertyInfo8 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Limit,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Limit",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Limit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).ResetAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ResetAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("ResetAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<IReadOnlyList<PaywallPlanOption>> propertyInfo10 = new JsonPropertyInfoValues<IReadOnlyList<PaywallPlanOption>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).Plans,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Plans",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("Plans", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<PaywallPlanOption>), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<string> propertyInfo11 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).YearlySavingsLabel,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "YearlySavingsLabel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("YearlySavingsLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = (object obj) => ((PaywallPayload)obj).DefaultInterval,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DefaultInterval",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("DefaultInterval", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<bool> propertyInfo13 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsPaymentProblem",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("IsPaymentProblem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<IReadOnlyList<PaywallPlanOption>> propertyInfo14 = new JsonPropertyInfoValues<IReadOnlyList<PaywallPlanOption>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UpgradeOptions",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("UpgradeOptions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<PaywallPlanOption>), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		array[13].IsGetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPayload),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SupportsYearly",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPayload).GetProperty("SupportsYearly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		return array;
	}

	private void PaywallPayloadSerializeHandler(Utf8JsonWriter writer, PaywallPayload? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string reason = value.Reason;
		if (reason != null)
		{
			writer.WriteString(PropName_reason, reason);
		}
		string surface = value.Surface;
		if (surface != null)
		{
			writer.WriteString(PropName_surface, surface);
		}
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		string body = value.Body;
		if (body != null)
		{
			writer.WriteString(PropName_body, body);
		}
		string ctaLabel = value.CtaLabel;
		if (ctaLabel != null)
		{
			writer.WriteString(PropName_cta_label, ctaLabel);
		}
		string priceDisplay = value.PriceDisplay;
		if (priceDisplay != null)
		{
			writer.WriteString(PropName_price_display, priceDisplay);
		}
		writer.WriteNumber(PropName_current_usage, value.CurrentUsage);
		writer.WriteNumber(PropName_limit, value.Limit);
		string resetAt = value.ResetAt;
		if (resetAt != null)
		{
			writer.WriteString(PropName_reset_at, resetAt);
		}
		IReadOnlyList<PaywallPlanOption> plans = value.Plans;
		if (plans != null)
		{
			writer.WritePropertyName(PropName_plans);
			IReadOnlyListPaywallPlanOptionSerializeHandler(writer, plans);
		}
		string yearlySavingsLabel = value.YearlySavingsLabel;
		if (yearlySavingsLabel != null)
		{
			writer.WriteString(PropName_yearly_savings_label, yearlySavingsLabel);
		}
		string defaultInterval = value.DefaultInterval;
		if (defaultInterval != null)
		{
			writer.WriteString(PropName_default_interval, defaultInterval);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PaywallPayloadCtorParamInit()
	{
		return new JsonParameterInfoValues[12]
		{
			new JsonParameterInfoValues
			{
				Name = "Reason",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Surface",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Body",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CtaLabel",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "PriceDisplay",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CurrentUsage",
				ParameterType = typeof(int),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Limit",
				ParameterType = typeof(int),
				Position = 7,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ResetAt",
				ParameterType = typeof(string),
				Position = 8,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Plans",
				ParameterType = typeof(IReadOnlyList<PaywallPlanOption>),
				Position = 9,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "YearlySavingsLabel",
				ParameterType = typeof(string),
				Position = 10,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "DefaultInterval",
				ParameterType = typeof(string),
				Position = 11,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<PaywallPlanOption> Create_PaywallPlanOption(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PaywallPlanOption> jsonTypeInfo))
		{
			JsonObjectInfoValues<PaywallPlanOption> objectInfo = new JsonObjectInfoValues<PaywallPlanOption>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PaywallPlanOption((string)args[0], (string)args[1], (string)args[2], (int)args[3], (string)args[4], (string)args[5], (bool)args[6], (string)args[7], (string)args[8]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PaywallPlanOptionPropInit(options),
				ConstructorParameterMetadataInitializer = PaywallPlanOptionCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PaywallPlanOption).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[9]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(int),
					typeof(string),
					typeof(string),
					typeof(bool),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = PaywallPlanOptionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PaywallPlanOptionPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[10];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).Key,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Key",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).Name,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).PriceDisplay,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PriceDisplay",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("PriceDisplay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo4 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).AgentsCap,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentsCap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("AgentsCap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).Tagline,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Tagline",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("Tagline", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).CtaLabel,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CtaLabel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("CtaLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).Recommended,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Recommended",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("Recommended", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).YearlyPriceDisplay,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "YearlyPriceDisplay",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("YearlyPriceDisplay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = (object obj) => ((PaywallPlanOption)obj).YearlyBilledSummary,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "YearlyBilledSummary",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("YearlyBilledSummary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<bool> propertyInfo10 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PaywallPlanOption),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SupportsYearly",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PaywallPlanOption).GetProperty("SupportsYearly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		return array;
	}

	private void PaywallPlanOptionSerializeHandler(Utf8JsonWriter writer, PaywallPlanOption? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string key = value.Key;
		if (key != null)
		{
			writer.WriteString(PropName_key, key);
		}
		string name = value.Name;
		if (name != null)
		{
			writer.WriteString(PropName_name, name);
		}
		string priceDisplay = value.PriceDisplay;
		if (priceDisplay != null)
		{
			writer.WriteString(PropName_price_display, priceDisplay);
		}
		writer.WriteNumber(PropName_agents_cap, value.AgentsCap);
		string tagline = value.Tagline;
		if (tagline != null)
		{
			writer.WriteString(PropName_tagline, tagline);
		}
		string ctaLabel = value.CtaLabel;
		if (ctaLabel != null)
		{
			writer.WriteString(PropName_cta_label, ctaLabel);
		}
		writer.WriteBoolean(PropName_recommended, value.Recommended);
		string yearlyPriceDisplay = value.YearlyPriceDisplay;
		if (yearlyPriceDisplay != null)
		{
			writer.WriteString(PropName_yearly_price_display, yearlyPriceDisplay);
		}
		string yearlyBilledSummary = value.YearlyBilledSummary;
		if (yearlyBilledSummary != null)
		{
			writer.WriteString(PropName_yearly_billed_summary, yearlyBilledSummary);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PaywallPlanOptionCtorParamInit()
	{
		return new JsonParameterInfoValues[9]
		{
			new JsonParameterInfoValues
			{
				Name = "Key",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Name",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "PriceDisplay",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "AgentsCap",
				ParameterType = typeof(int),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Tagline",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CtaLabel",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Recommended",
				ParameterType = typeof(bool),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "YearlyPriceDisplay",
				ParameterType = typeof(string),
				Position = 7,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "YearlyBilledSummary",
				ParameterType = typeof(string),
				Position = 8,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<OnboardingProgress> Create_OnboardingProgress(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<OnboardingProgress> jsonTypeInfo))
		{
			JsonObjectInfoValues<OnboardingProgress> objectInfo = new JsonObjectInfoValues<OnboardingProgress>
			{
				ObjectCreator = () => new OnboardingProgress(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => OnboardingProgressPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(OnboardingProgress).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = OnboardingProgressSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] OnboardingProgressPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).PreSignInIntroDismissed,
			Setter = delegate(object obj, bool value)
			{
				((OnboardingProgress)obj).PreSignInIntroDismissed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PreSignInIntroDismissed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("PreSignInIntroDismissed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).HasAcknowledgedPermissionIntro,
			Setter = delegate(object obj, bool value)
			{
				((OnboardingProgress)obj).HasAcknowledgedPermissionIntro = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasAcknowledgedPermissionIntro",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("HasAcknowledgedPermissionIntro", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).HasCompletedOnboarding,
			Setter = delegate(object obj, bool value)
			{
				((OnboardingProgress)obj).HasCompletedOnboarding = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasCompletedOnboarding",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("HasCompletedOnboarding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<double?> propertyInfo4 = new JsonPropertyInfoValues<double?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).TutorialCompletedAtEpochSeconds,
			Setter = delegate(object obj, double? value)
			{
				((OnboardingProgress)obj).TutorialCompletedAtEpochSeconds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TutorialCompletedAtEpochSeconds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("TutorialCompletedAtEpochSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double?), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).DiscoveryChannel,
			Setter = delegate(object obj, string? value)
			{
				((OnboardingProgress)obj).DiscoveryChannel = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("DiscoveryChannel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).DiscoveryChannelOther,
			Setter = delegate(object obj, string? value)
			{
				((OnboardingProgress)obj).DiscoveryChannelOther = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannelOther",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("DiscoveryChannelOther", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).DiscoveryChannelSynced,
			Setter = delegate(object obj, bool value)
			{
				((OnboardingProgress)obj).DiscoveryChannelSynced = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannelSynced",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("DiscoveryChannelSynced", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(OnboardingProgress),
			Converter = null,
			Getter = (object obj) => ((OnboardingProgress)obj).SquadPackId,
			Setter = delegate(object obj, string? value)
			{
				((OnboardingProgress)obj).SquadPackId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SquadPackId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(OnboardingProgress).GetProperty("SquadPackId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void OnboardingProgressSerializeHandler(Utf8JsonWriter writer, OnboardingProgress? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_pre_sign_in_intro_dismissed, value.PreSignInIntroDismissed);
		writer.WriteBoolean(PropName_has_acknowledged_permission_intro, value.HasAcknowledgedPermissionIntro);
		writer.WriteBoolean(PropName_has_completed_onboarding, value.HasCompletedOnboarding);
		double? tutorialCompletedAtEpochSeconds = value.TutorialCompletedAtEpochSeconds;
		if (tutorialCompletedAtEpochSeconds.HasValue)
		{
			writer.WritePropertyName(PropName_tutorial_completed_at_epoch_seconds);
			JsonSerializer.Serialize(writer, tutorialCompletedAtEpochSeconds, NullableDouble);
		}
		string discoveryChannel = value.DiscoveryChannel;
		if (discoveryChannel != null)
		{
			writer.WriteString(PropName_discovery_channel, discoveryChannel);
		}
		string discoveryChannelOther = value.DiscoveryChannelOther;
		if (discoveryChannelOther != null)
		{
			writer.WriteString(PropName_discovery_channel_other, discoveryChannelOther);
		}
		writer.WriteBoolean(PropName_discovery_channel_synced, value.DiscoveryChannelSynced);
		string squadPackId = value.SquadPackId;
		if (squadPackId != null)
		{
			writer.WriteString(PropName_squad_pack_id, squadPackId);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<AppSettings> Create_AppSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppSettings> objectInfo = new JsonObjectInfoValues<AppSettings>
			{
				ObjectCreator = () => new AppSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(AppSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = AppSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[60];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).CursorColorHex,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).CursorColorHex = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CursorColorHex",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("CursorColorHex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).SquadMemberId,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).SquadMemberId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SquadMemberId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("SquadMemberId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).VoiceIdentifier,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).VoiceIdentifier = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "VoiceIdentifier",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("VoiceIdentifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).SpeechSpeed,
			Setter = delegate(object obj, double value)
			{
				((AppSettings)obj).SpeechSpeed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SpeechSpeed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("SpeechSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).VoiceLanguageCode,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).VoiceLanguageCode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "VoiceLanguageCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("VoiceLanguageCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<bool> propertyInfo6 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ScreenHighlightEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ScreenHighlightEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ScreenHighlightEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ScreenHighlightEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).CursorDocked,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).CursorDocked = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CursorDocked",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("CursorDocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ShakeToDetachEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ShakeToDetachEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShakeToDetachEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ShakeToDetachEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<bool> propertyInfo9 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).LaunchAtLogin,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).LaunchAtLogin = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LaunchAtLogin",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("LaunchAtLogin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<bool> propertyInfo10 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ShowInTaskbar,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ShowInTaskbar = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowInTaskbar",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ShowInTaskbar", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<bool> propertyInfo11 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).TypedTurnsRespondWithVoice,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).TypedTurnsRespondWithVoice = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TypedTurnsRespondWithVoice",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("TypedTurnsRespondWithVoice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationLanguageCode,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).DictationLanguageCode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationLanguageCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationLanguageCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		array[11].IsGetNullable = false;
		array[11].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo13 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationSpecificLanguageCode,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).DictationSpecificLanguageCode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationSpecificLanguageCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationSpecificLanguageCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		array[12].IsGetNullable = false;
		array[12].IsSetNullable = false;
		JsonPropertyInfoValues<double?> propertyInfo14 = new JsonPropertyInfoValues<double?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationLastCompletedAtEpochSeconds,
			Setter = delegate(object obj, double? value)
			{
				((AppSettings)obj).DictationLastCompletedAtEpochSeconds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationLastCompletedAtEpochSeconds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationLastCompletedAtEpochSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double?), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<List<string>> propertyInfo15 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationCustomWords,
			Setter = delegate(object obj, List<string>? value)
			{
				((AppSettings)obj).DictationCustomWords = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationCustomWords",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationCustomWords", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		array[14].IsGetNullable = false;
		array[14].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo16 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationClaudeSeeded,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DictationClaudeSeeded = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationClaudeSeeded",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationClaudeSeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		JsonPropertyInfoValues<string> propertyInfo17 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DictationAccountNameSeeded,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).DictationAccountNameSeeded = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationAccountNameSeeded",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DictationAccountNameSeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		JsonPropertyInfoValues<string> propertyInfo18 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DiscoveryChannelPendingSync,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).DiscoveryChannelPendingSync = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannelPendingSync",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DiscoveryChannelPendingSync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		JsonPropertyInfoValues<string> propertyInfo19 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).MicrophoneDeviceName,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).MicrophoneDeviceName = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MicrophoneDeviceName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("MicrophoneDeviceName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[18] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo19);
		JsonPropertyInfoValues<bool> propertyInfo20 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).AgentAnnouncementsVoiceEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).AgentAnnouncementsVoiceEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentAnnouncementsVoiceEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("AgentAnnouncementsVoiceEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[19] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo20);
		JsonPropertyInfoValues<int> propertyInfo21 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DocumentAccessNoticeShowCount,
			Setter = delegate(object obj, int value)
			{
				((AppSettings)obj).DocumentAccessNoticeShowCount = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DocumentAccessNoticeShowCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DocumentAccessNoticeShowCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[20] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo21);
		JsonPropertyInfoValues<bool> propertyInfo22 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DocumentAccessNoticeSuppressed,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DocumentAccessNoticeSuppressed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DocumentAccessNoticeSuppressed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DocumentAccessNoticeSuppressed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[21] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo22);
		JsonPropertyInfoValues<OnboardingProgress> propertyInfo23 = new JsonPropertyInfoValues<OnboardingProgress>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).Onboarding,
			Setter = delegate(object obj, OnboardingProgress? value)
			{
				((AppSettings)obj).Onboarding = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Onboarding",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("Onboarding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(OnboardingProgress), Array.Empty<Type>(), null)
		};
		array[22] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo23);
		array[22].IsGetNullable = false;
		array[22].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyBindings> propertyInfo24 = new JsonPropertyInfoValues<HotkeyBindings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).Hotkeys,
			Setter = delegate(object obj, HotkeyBindings? value)
			{
				((AppSettings)obj).Hotkeys = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Hotkeys",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("Hotkeys", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBindings), Array.Empty<Type>(), null)
		};
		array[23] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo24);
		JsonPropertyInfoValues<string> propertyInfo25 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).MicrophoneDeviceId,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).MicrophoneDeviceId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MicrophoneDeviceId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("MicrophoneDeviceId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[24] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo25);
		JsonPropertyInfoValues<string> propertyInfo26 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).OutputDeviceId,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).OutputDeviceId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OutputDeviceId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("OutputDeviceId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[25] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo26);
		JsonPropertyInfoValues<bool> propertyInfo27 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DrawingSoundsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DrawingSoundsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingSoundsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingSoundsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[26] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo27);
		JsonPropertyInfoValues<bool> propertyInfo28 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DrawingVoiceCaptions,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DrawingVoiceCaptions = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingVoiceCaptions",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingVoiceCaptions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[27] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo28);
		JsonPropertyInfoValues<bool?> propertyInfo29 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).KeepDrawingsInChat,
			Setter = delegate(object obj, bool? value)
			{
				((AppSettings)obj).KeepDrawingsInChat = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "KeepDrawingsInChat",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("KeepDrawingsInChat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[28] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo29);
		JsonPropertyInfoValues<bool> propertyInfo30 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "KeepDrawingsInChatEffective",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("KeepDrawingsInChatEffective", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[29] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo30);
		JsonPropertyInfoValues<bool> propertyInfo31 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DrawingPenSoundsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DrawingPenSoundsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingPenSoundsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingPenSoundsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[30] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo31);
		JsonPropertyInfoValues<bool> propertyInfo32 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DrawingJavaScriptJit,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).DrawingJavaScriptJit = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingJavaScriptJit",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingJavaScriptJit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[31] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo32);
		JsonPropertyInfoValues<bool?> propertyInfo33 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DrawingSubtitles,
			Setter = delegate(object obj, bool? value)
			{
				((AppSettings)obj).DrawingSubtitles = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingSubtitles",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingSubtitles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[32] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo33);
		JsonPropertyInfoValues<bool> propertyInfo34 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DrawingSubtitlesEffective",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DrawingSubtitlesEffective", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[33] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo34);
		JsonPropertyInfoValues<bool> propertyInfo35 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).SaveDrawings,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).SaveDrawings = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SaveDrawings",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("SaveDrawings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[34] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo35);
		JsonPropertyInfoValues<bool> propertyInfo36 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SaveDrawingsEffective",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("SaveDrawingsEffective", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[35] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo36);
		JsonPropertyInfoValues<bool> propertyInfo37 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ShowAgentLabelsInActivityStrip,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ShowAgentLabelsInActivityStrip = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowAgentLabelsInActivityStrip",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ShowAgentLabelsInActivityStrip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[36] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo37);
		JsonPropertyInfoValues<bool> propertyInfo38 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ShowInScreenRecordings,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ShowInScreenRecordings = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowInScreenRecordings",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ShowInScreenRecordings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[37] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo38);
		JsonPropertyInfoValues<string> propertyInfo39 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).AgentFolderPath,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).AgentFolderPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentFolderPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("AgentFolderPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[38] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo39);
		JsonPropertyInfoValues<string> propertyInfo40 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ObsidianVaultPath,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).ObsidianVaultPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ObsidianVaultPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ObsidianVaultPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[39] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo40);
		JsonPropertyInfoValues<bool> propertyInfo41 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).AgentAutoApproveExtraUsage,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).AgentAutoApproveExtraUsage = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentAutoApproveExtraUsage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("AgentAutoApproveExtraUsage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[40] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo41);
		JsonPropertyInfoValues<bool> propertyInfo42 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).AgentAlwaysApproveFileUploads,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).AgentAlwaysApproveFileUploads = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentAlwaysApproveFileUploads",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("AgentAlwaysApproveFileUploads", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[41] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo42);
		JsonPropertyInfoValues<bool> propertyInfo43 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).AgentCursorTextAnnouncementsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).AgentCursorTextAnnouncementsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentCursorTextAnnouncementsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("AgentCursorTextAnnouncementsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[42] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo43);
		JsonPropertyInfoValues<string> propertyInfo44 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).OneBrainModel,
			Setter = delegate(object obj, string? value)
			{
				((AppSettings)obj).OneBrainModel = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OneBrainModel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("OneBrainModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[43] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo44);
		JsonPropertyInfoValues<bool> propertyInfo45 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).OneBrainReadsApps,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).OneBrainReadsApps = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OneBrainReadsApps",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("OneBrainReadsApps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[44] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo45);
		JsonPropertyInfoValues<bool> propertyInfo46 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).OneBrainKeepsScreensInHistory,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).OneBrainKeepsScreensInHistory = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OneBrainKeepsScreensInHistory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("OneBrainKeepsScreensInHistory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[45] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo46);
		JsonPropertyInfoValues<bool> propertyInfo47 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ShowOneBrainStats,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ShowOneBrainStats = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowOneBrainStats",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ShowOneBrainStats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[46] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo47);
		JsonPropertyInfoValues<bool?> propertyInfo48 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).MorningPaperEnabled,
			Setter = delegate(object obj, bool? value)
			{
				((AppSettings)obj).MorningPaperEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningPaperEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("MorningPaperEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[47] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo48);
		JsonPropertyInfoValues<bool> propertyInfo49 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).StreamMode,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).StreamMode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StreamMode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("StreamMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[48] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo49);
		JsonPropertyInfoValues<Dictionary<string, string>> propertyInfo50 = new JsonPropertyInfoValues<Dictionary<string, string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).DeveloperChatModels,
			Setter = delegate(object obj, Dictionary<string, string>? value)
			{
				((AppSettings)obj).DeveloperChatModels = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DeveloperChatModels",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("DeveloperChatModels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, string>), Array.Empty<Type>(), null)
		};
		array[49] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo50);
		JsonPropertyInfoValues<bool> propertyInfo51 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).HasShownPaywallIntroVideo,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).HasShownPaywallIntroVideo = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasShownPaywallIntroVideo",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("HasShownPaywallIntroVideo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[50] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo51);
		JsonPropertyInfoValues<HotkeyBindings> propertyInfo52 = new JsonPropertyInfoValues<HotkeyBindings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "EffectiveHotkeys",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("EffectiveHotkeys", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBindings), Array.Empty<Type>(), null)
		};
		array[51] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo52);
		array[51].IsGetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo53 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ProactiveAgentSuggestionsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ProactiveAgentSuggestionsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProactiveAgentSuggestionsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ProactiveAgentSuggestionsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[52] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo53);
		JsonPropertyInfoValues<bool> propertyInfo54 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).UpcomingMeetingsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).UpcomingMeetingsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UpcomingMeetingsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("UpcomingMeetingsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[53] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo54);
		JsonPropertyInfoValues<bool> propertyInfo55 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).OnboardingLanguageStepEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).OnboardingLanguageStepEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OnboardingLanguageStepEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("OnboardingLanguageStepEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[54] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo55);
		JsonPropertyInfoValues<bool> propertyInfo56 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).RemoteTasksEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).RemoteTasksEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RemoteTasksEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("RemoteTasksEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[55] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo56);
		JsonPropertyInfoValues<bool> propertyInfo57 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ProactiveDebugChordEnabled,
			Setter = delegate(object obj, bool value)
			{
				((AppSettings)obj).ProactiveDebugChordEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProactiveDebugChordEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ProactiveDebugChordEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[56] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo57);
		JsonPropertyInfoValues<int> propertyInfo58 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).ProactiveDebugCadence,
			Setter = delegate(object obj, int value)
			{
				((AppSettings)obj).ProactiveDebugCadence = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProactiveDebugCadence",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("ProactiveDebugCadence", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[57] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo58);
		JsonPropertyInfoValues<bool?> propertyInfo59 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).LiveNotesEnabled,
			Setter = delegate(object obj, bool? value)
			{
				((AppSettings)obj).LiveNotesEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LiveNotesEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("LiveNotesEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[58] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo59);
		JsonPropertyInfoValues<bool?> propertyInfo60 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppSettings),
			Converter = null,
			Getter = (object obj) => ((AppSettings)obj).LiveNotesStreamPatches,
			Setter = delegate(object obj, bool? value)
			{
				((AppSettings)obj).LiveNotesStreamPatches = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LiveNotesStreamPatches",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppSettings).GetProperty("LiveNotesStreamPatches", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[59] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo60);
		return array;
	}

	private void AppSettingsSerializeHandler(Utf8JsonWriter writer, AppSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string cursorColorHex = value.CursorColorHex;
		if (cursorColorHex != null)
		{
			writer.WriteString(PropName_cursor_color_hex, cursorColorHex);
		}
		string squadMemberId = value.SquadMemberId;
		if (squadMemberId != null)
		{
			writer.WriteString(PropName_squad_member_id, squadMemberId);
		}
		string voiceIdentifier = value.VoiceIdentifier;
		if (voiceIdentifier != null)
		{
			writer.WriteString(PropName_voice_identifier, voiceIdentifier);
		}
		writer.WriteNumber(PropName_speech_speed, value.SpeechSpeed);
		string voiceLanguageCode = value.VoiceLanguageCode;
		if (voiceLanguageCode != null)
		{
			writer.WriteString(PropName_voice_language_code, voiceLanguageCode);
		}
		writer.WriteBoolean(PropName_screen_highlight_enabled, value.ScreenHighlightEnabled);
		writer.WriteBoolean(PropName_cursor_docked, value.CursorDocked);
		writer.WriteBoolean(PropName_shake_to_detach_enabled, value.ShakeToDetachEnabled);
		writer.WriteBoolean(PropName_launch_at_login, value.LaunchAtLogin);
		writer.WriteBoolean(PropName_show_in_taskbar, value.ShowInTaskbar);
		writer.WriteBoolean(PropName_typed_turns_respond_with_voice, value.TypedTurnsRespondWithVoice);
		string dictationLanguageCode = value.DictationLanguageCode;
		if (dictationLanguageCode != null)
		{
			writer.WriteString(PropName_dictation_language_code, dictationLanguageCode);
		}
		string dictationSpecificLanguageCode = value.DictationSpecificLanguageCode;
		if (dictationSpecificLanguageCode != null)
		{
			writer.WriteString(PropName_dictation_specific_language_code, dictationSpecificLanguageCode);
		}
		double? dictationLastCompletedAtEpochSeconds = value.DictationLastCompletedAtEpochSeconds;
		if (dictationLastCompletedAtEpochSeconds.HasValue)
		{
			writer.WritePropertyName(PropName_dictation_last_completed_at_epoch_seconds);
			JsonSerializer.Serialize(writer, dictationLastCompletedAtEpochSeconds, NullableDouble);
		}
		List<string> dictationCustomWords = value.DictationCustomWords;
		if (dictationCustomWords != null)
		{
			writer.WritePropertyName(PropName_dictation_custom_words);
			ListStringSerializeHandler(writer, dictationCustomWords);
		}
		writer.WriteBoolean(PropName_dictation_claude_seeded, value.DictationClaudeSeeded);
		string dictationAccountNameSeeded = value.DictationAccountNameSeeded;
		if (dictationAccountNameSeeded != null)
		{
			writer.WriteString(PropName_dictation_account_name_seeded, dictationAccountNameSeeded);
		}
		string discoveryChannelPendingSync = value.DiscoveryChannelPendingSync;
		if (discoveryChannelPendingSync != null)
		{
			writer.WriteString(PropName_discovery_channel_pending_sync, discoveryChannelPendingSync);
		}
		string microphoneDeviceName = value.MicrophoneDeviceName;
		if (microphoneDeviceName != null)
		{
			writer.WriteString(PropName_microphone_device_name, microphoneDeviceName);
		}
		writer.WriteBoolean(PropName_agent_announcements_voice_enabled, value.AgentAnnouncementsVoiceEnabled);
		writer.WriteNumber(PropName_document_access_notice_show_count, value.DocumentAccessNoticeShowCount);
		writer.WriteBoolean(PropName_document_access_notice_suppressed, value.DocumentAccessNoticeSuppressed);
		OnboardingProgress onboarding = value.Onboarding;
		if (onboarding != null)
		{
			writer.WritePropertyName(PropName_onboarding);
			OnboardingProgressSerializeHandler(writer, onboarding);
		}
		HotkeyBindings hotkeys = value.Hotkeys;
		if ((object)hotkeys != null)
		{
			writer.WritePropertyName(PropName_hotkeys);
			HotkeyBindingsSerializeHandler(writer, hotkeys);
		}
		string microphoneDeviceId = value.MicrophoneDeviceId;
		if (microphoneDeviceId != null)
		{
			writer.WriteString(PropName_microphone_device_id, microphoneDeviceId);
		}
		string outputDeviceId = value.OutputDeviceId;
		if (outputDeviceId != null)
		{
			writer.WriteString(PropName_output_device_id, outputDeviceId);
		}
		writer.WriteBoolean(PropName_drawing_sounds_enabled, value.DrawingSoundsEnabled);
		writer.WriteBoolean(PropName_drawing_voice_captions, value.DrawingVoiceCaptions);
		bool? keepDrawingsInChat = value.KeepDrawingsInChat;
		if (keepDrawingsInChat.HasValue)
		{
			writer.WritePropertyName(PropName_keep_drawings_in_chat);
			JsonSerializer.Serialize(writer, keepDrawingsInChat, NullableBoolean);
		}
		writer.WriteBoolean(PropName_drawing_pen_sounds_enabled, value.DrawingPenSoundsEnabled);
		writer.WriteBoolean(PropName_drawing_java_script_jit, value.DrawingJavaScriptJit);
		bool? drawingSubtitles = value.DrawingSubtitles;
		if (drawingSubtitles.HasValue)
		{
			writer.WritePropertyName(PropName_drawing_subtitles);
			JsonSerializer.Serialize(writer, drawingSubtitles, NullableBoolean);
		}
		writer.WriteBoolean(PropName_save_drawings, value.SaveDrawings);
		writer.WriteBoolean(PropName_show_agent_labels_in_activity_strip, value.ShowAgentLabelsInActivityStrip);
		writer.WriteBoolean(PropName_show_in_screen_recordings, value.ShowInScreenRecordings);
		string agentFolderPath = value.AgentFolderPath;
		if (agentFolderPath != null)
		{
			writer.WriteString(PropName_agent_folder_path, agentFolderPath);
		}
		string obsidianVaultPath = value.ObsidianVaultPath;
		if (obsidianVaultPath != null)
		{
			writer.WriteString(PropName_obsidian_vault_path, obsidianVaultPath);
		}
		writer.WriteBoolean(PropName_agent_auto_approve_extra_usage, value.AgentAutoApproveExtraUsage);
		writer.WriteBoolean(PropName_agent_always_approve_file_uploads, value.AgentAlwaysApproveFileUploads);
		writer.WriteBoolean(PropName_agent_cursor_text_announcements_enabled, value.AgentCursorTextAnnouncementsEnabled);
		string oneBrainModel = value.OneBrainModel;
		if (oneBrainModel != null)
		{
			writer.WriteString(PropName_one_brain_model, oneBrainModel);
		}
		writer.WriteBoolean(PropName_one_brain_reads_apps, value.OneBrainReadsApps);
		writer.WriteBoolean(PropName_one_brain_keeps_screens_in_history, value.OneBrainKeepsScreensInHistory);
		writer.WriteBoolean(PropName_show_one_brain_stats, value.ShowOneBrainStats);
		bool? morningPaperEnabled = value.MorningPaperEnabled;
		if (morningPaperEnabled.HasValue)
		{
			writer.WritePropertyName(PropName_morning_paper_enabled);
			JsonSerializer.Serialize(writer, morningPaperEnabled, NullableBoolean);
		}
		writer.WriteBoolean(PropName_stream_mode, value.StreamMode);
		Dictionary<string, string> developerChatModels = value.DeveloperChatModels;
		if (developerChatModels != null)
		{
			writer.WritePropertyName(PropName_developer_chat_models);
			DictionaryStringStringSerializeHandler(writer, developerChatModels);
		}
		writer.WriteBoolean(PropName_has_shown_paywall_intro_video, value.HasShownPaywallIntroVideo);
		writer.WriteBoolean(PropName_proactive_agent_suggestions_enabled, value.ProactiveAgentSuggestionsEnabled);
		writer.WriteBoolean(PropName_upcoming_meetings_enabled, value.UpcomingMeetingsEnabled);
		writer.WriteBoolean(PropName_onboarding_language_step_enabled, value.OnboardingLanguageStepEnabled);
		writer.WriteBoolean(PropName_remote_tasks_enabled, value.RemoteTasksEnabled);
		writer.WriteBoolean(PropName_proactive_debug_chord_enabled, value.ProactiveDebugChordEnabled);
		writer.WriteNumber(PropName_proactive_debug_cadence, value.ProactiveDebugCadence);
		bool? liveNotesEnabled = value.LiveNotesEnabled;
		if (liveNotesEnabled.HasValue)
		{
			writer.WritePropertyName(PropName_live_notes_enabled);
			JsonSerializer.Serialize(writer, liveNotesEnabled, NullableBoolean);
		}
		bool? liveNotesStreamPatches = value.LiveNotesStreamPatches;
		if (liveNotesStreamPatches.HasValue)
		{
			writer.WritePropertyName(PropName_live_notes_stream_patches);
			JsonSerializer.Serialize(writer, liveNotesStreamPatches, NullableBoolean);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<AppConfig> Create_AppConfig(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppConfig> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppConfig> objectInfo = new JsonObjectInfoValues<AppConfig>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AppConfig((AppConfigLinks)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppConfigPropInit(options),
				ConstructorParameterMetadataInitializer = AppConfigCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AppConfig).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(AppConfigLinks) }, null),
				SerializeHandler = AppConfigSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppConfigPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<AppConfigLinks> propertyInfo = new JsonPropertyInfoValues<AppConfigLinks>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfig),
			Converter = null,
			Getter = (object obj) => ((AppConfig)obj).Links,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Links",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppConfig).GetProperty("Links", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(AppConfigLinks), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void AppConfigSerializeHandler(Utf8JsonWriter writer, AppConfig? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		AppConfigLinks links = value.Links;
		if ((object)links != null)
		{
			writer.WritePropertyName(PropName_links);
			AppConfigLinksSerializeHandler(writer, links);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AppConfigCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Links",
				ParameterType = typeof(AppConfigLinks),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<AppConfigLinks> Create_AppConfigLinks(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppConfigLinks> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppConfigLinks> objectInfo = new JsonObjectInfoValues<AppConfigLinks>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AppConfigLinks((string)args[0], (string)args[1], (string)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppConfigLinksPropInit(options),
				ConstructorParameterMetadataInitializer = AppConfigLinksCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AppConfigLinks).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = AppConfigLinksSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppConfigLinksPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfigLinks),
			Converter = null,
			Getter = (object obj) => ((AppConfigLinks)obj).FeatureRequest,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FeatureRequest",
			JsonPropertyName = "featureRequest",
			AttributeProviderFactory = () => typeof(AppConfigLinks).GetProperty("FeatureRequest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfigLinks),
			Converter = null,
			Getter = (object obj) => ((AppConfigLinks)obj).CommunityReddit,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CommunityReddit",
			JsonPropertyName = "communityReddit",
			AttributeProviderFactory = () => typeof(AppConfigLinks).GetProperty("CommunityReddit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfigLinks),
			Converter = null,
			Getter = (object obj) => ((AppConfigLinks)obj).CommunityWhatsApp,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CommunityWhatsApp",
			JsonPropertyName = "communityWhatsApp",
			AttributeProviderFactory = () => typeof(AppConfigLinks).GetProperty("CommunityWhatsApp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfigLinks),
			Converter = null,
			Getter = (object obj) => ((AppConfigLinks)obj).Changelog,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Changelog",
			JsonPropertyName = "changelog",
			AttributeProviderFactory = () => typeof(AppConfigLinks).GetProperty("Changelog", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void AppConfigLinksSerializeHandler(Utf8JsonWriter writer, AppConfigLinks? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string featureRequest = value.FeatureRequest;
		if (featureRequest != null)
		{
			writer.WriteString(PropName_featureRequest, featureRequest);
		}
		string communityReddit = value.CommunityReddit;
		if (communityReddit != null)
		{
			writer.WriteString(PropName_communityReddit, communityReddit);
		}
		string communityWhatsApp = value.CommunityWhatsApp;
		if (communityWhatsApp != null)
		{
			writer.WriteString(PropName_communityWhatsApp, communityWhatsApp);
		}
		string changelog = value.Changelog;
		if (changelog != null)
		{
			writer.WriteString(PropName_changelog, changelog);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AppConfigLinksCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "FeatureRequest",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "CommunityReddit",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "CommunityWhatsApp",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Changelog",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<AppConfigResponse> Create_AppConfigResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppConfigResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppConfigResponse> objectInfo = new JsonObjectInfoValues<AppConfigResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AppConfigResponse((AppConfig)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppConfigResponsePropInit(options),
				ConstructorParameterMetadataInitializer = AppConfigResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AppConfigResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(AppConfig) }, null),
				SerializeHandler = AppConfigResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppConfigResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<AppConfig> propertyInfo = new JsonPropertyInfoValues<AppConfig>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppConfigResponse),
			Converter = null,
			Getter = (object obj) => ((AppConfigResponse)obj).AppConfig,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AppConfig",
			JsonPropertyName = "appConfig",
			AttributeProviderFactory = () => typeof(AppConfigResponse).GetProperty("AppConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(AppConfig), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void AppConfigResponseSerializeHandler(Utf8JsonWriter writer, AppConfigResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		AppConfig appConfig = value.AppConfig;
		if ((object)appConfig != null)
		{
			writer.WritePropertyName(PropName_appConfig);
			AppConfigSerializeHandler(writer, appConfig);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AppConfigResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "AppConfig",
				ParameterType = typeof(AppConfig),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<CheckoutRequest> Create_CheckoutRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CheckoutRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<CheckoutRequest> objectInfo = new JsonObjectInfoValues<CheckoutRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new CheckoutRequest((string)args[0], (string)args[1], (string)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => CheckoutRequestPropInit(options),
				ConstructorParameterMetadataInitializer = CheckoutRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(CheckoutRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = CheckoutRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] CheckoutRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutRequest),
			Converter = null,
			Getter = (object obj) => ((CheckoutRequest)obj).Plan,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Plan",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutRequest).GetProperty("Plan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutRequest),
			Converter = null,
			Getter = (object obj) => ((CheckoutRequest)obj).Interval,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Interval",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutRequest).GetProperty("Interval", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutRequest),
			Converter = null,
			Getter = (object obj) => ((CheckoutRequest)obj).CheckoutUi,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CheckoutUi",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutRequest).GetProperty("CheckoutUi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutRequest),
			Converter = null,
			Getter = (object obj) => ((CheckoutRequest)obj).DiscountCode,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscountCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutRequest).GetProperty("DiscountCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void CheckoutRequestSerializeHandler(Utf8JsonWriter writer, CheckoutRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string plan = value.Plan;
		if (plan != null)
		{
			writer.WriteString(PropName_plan, plan);
		}
		string interval = value.Interval;
		if (interval != null)
		{
			writer.WriteString(PropName_interval, interval);
		}
		string checkoutUi = value.CheckoutUi;
		if (checkoutUi != null)
		{
			writer.WriteString(PropName_checkout_ui, checkoutUi);
		}
		string discountCode = value.DiscountCode;
		if (discountCode != null)
		{
			writer.WriteString(PropName_discount_code, discountCode);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] CheckoutRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "Plan",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Interval",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CheckoutUi",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = true,
				DefaultValue = "web",
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DiscountCode",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<CheckoutResponse> Create_CheckoutResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CheckoutResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<CheckoutResponse> objectInfo = new JsonObjectInfoValues<CheckoutResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new CheckoutResponse((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => CheckoutResponsePropInit(options),
				ConstructorParameterMetadataInitializer = CheckoutResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(CheckoutResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = CheckoutResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] CheckoutResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutResponse),
			Converter = null,
			Getter = (object obj) => ((CheckoutResponse)obj).Url,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Url",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutResponse).GetProperty("Url", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CheckoutResponse),
			Converter = null,
			Getter = (object obj) => ((CheckoutResponse)obj).DiscountCodeApplied,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscountCodeApplied",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CheckoutResponse).GetProperty("DiscountCodeApplied", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void CheckoutResponseSerializeHandler(Utf8JsonWriter writer, CheckoutResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string url = value.Url;
		if (url != null)
		{
			writer.WriteString(PropName_url, url);
		}
		string discountCodeApplied = value.DiscountCodeApplied;
		if (discountCodeApplied != null)
		{
			writer.WriteString(PropName_discount_code_applied, discountCodeApplied);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] CheckoutResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Url",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DiscountCodeApplied",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DeepgramTokenResponse> Create_DeepgramTokenResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DeepgramTokenResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<DeepgramTokenResponse> objectInfo = new JsonObjectInfoValues<DeepgramTokenResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DeepgramTokenResponse((string)args[0], (string)args[1], (string)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DeepgramTokenResponsePropInit(options),
				ConstructorParameterMetadataInitializer = DeepgramTokenResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DeepgramTokenResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = DeepgramTokenResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DeepgramTokenResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeepgramTokenResponse),
			Converter = null,
			Getter = (object obj) => ((DeepgramTokenResponse)obj).AccessToken,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AccessToken",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeepgramTokenResponse).GetProperty("AccessToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeepgramTokenResponse),
			Converter = null,
			Getter = (object obj) => ((DeepgramTokenResponse)obj).Model,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Model",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeepgramTokenResponse).GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeepgramTokenResponse),
			Converter = null,
			Getter = (object obj) => ((DeepgramTokenResponse)obj).Receipt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Receipt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeepgramTokenResponse).GetProperty("Receipt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void DeepgramTokenResponseSerializeHandler(Utf8JsonWriter writer, DeepgramTokenResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string accessToken = value.AccessToken;
		if (accessToken != null)
		{
			writer.WriteString(PropName_access_token, accessToken);
		}
		string model = value.Model;
		if (model != null)
		{
			writer.WriteString(PropName_model, model);
		}
		string receipt = value.Receipt;
		if (receipt != null)
		{
			writer.WriteString(PropName_receipt, receipt);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DeepgramTokenResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "AccessToken",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Model",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Receipt",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DeviceRegistrationRequest> Create_DeviceRegistrationRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DeviceRegistrationRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<DeviceRegistrationRequest> objectInfo = new JsonObjectInfoValues<DeviceRegistrationRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DeviceRegistrationRequest((IReadOnlyList<string>)args[0], (string)args[1], (string)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DeviceRegistrationRequestPropInit(options),
				ConstructorParameterMetadataInitializer = DeviceRegistrationRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DeviceRegistrationRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(IReadOnlyList<string>),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = DeviceRegistrationRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DeviceRegistrationRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceRegistrationRequest),
			Converter = null,
			Getter = (object obj) => ((DeviceRegistrationRequest)obj).Capabilities,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Capabilities",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceRegistrationRequest).GetProperty("Capabilities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceRegistrationRequest),
			Converter = null,
			Getter = (object obj) => ((DeviceRegistrationRequest)obj).DisplayName,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DisplayName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceRegistrationRequest).GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceRegistrationRequest),
			Converter = null,
			Getter = (object obj) => ((DeviceRegistrationRequest)obj).Model,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Model",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceRegistrationRequest).GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void DeviceRegistrationRequestSerializeHandler(Utf8JsonWriter writer, DeviceRegistrationRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<string> capabilities = value.Capabilities;
		if (capabilities != null)
		{
			writer.WritePropertyName(PropName_capabilities);
			IReadOnlyListStringSerializeHandler(writer, capabilities);
		}
		string displayName = value.DisplayName;
		if (displayName != null)
		{
			writer.WriteString(PropName_display_name, displayName);
		}
		string model = value.Model;
		if (model != null)
		{
			writer.WriteString(PropName_model, model);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DeviceRegistrationRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "Capabilities",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "DisplayName",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Model",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DeviceRegistrationStamp> Create_DeviceRegistrationStamp(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DeviceRegistrationStamp> jsonTypeInfo))
		{
			JsonObjectInfoValues<DeviceRegistrationStamp> objectInfo = new JsonObjectInfoValues<DeviceRegistrationStamp>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DeviceRegistrationStamp((string)args[0], (long)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DeviceRegistrationStampPropInit(options),
				ConstructorParameterMetadataInitializer = DeviceRegistrationStampCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DeviceRegistrationStamp).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(long)
				}, null),
				SerializeHandler = DeviceRegistrationStampSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DeviceRegistrationStampPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceRegistrationStamp),
			Converter = null,
			Getter = (object obj) => ((DeviceRegistrationStamp)obj).Fingerprint,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Fingerprint",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceRegistrationStamp).GetProperty("Fingerprint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<long> propertyInfo2 = new JsonPropertyInfoValues<long>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceRegistrationStamp),
			Converter = null,
			Getter = (object obj) => ((DeviceRegistrationStamp)obj).RegisteredAtEpochSeconds,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RegisteredAtEpochSeconds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceRegistrationStamp).GetProperty("RegisteredAtEpochSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(long), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void DeviceRegistrationStampSerializeHandler(Utf8JsonWriter writer, DeviceRegistrationStamp? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string fingerprint = value.Fingerprint;
		if (fingerprint != null)
		{
			writer.WriteString(PropName_fingerprint, fingerprint);
		}
		writer.WriteNumber(PropName_registered_at_epoch_seconds, value.RegisteredAtEpochSeconds);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DeviceRegistrationStampCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Fingerprint",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "RegisteredAtEpochSeconds",
				ParameterType = typeof(long),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<DeviceResponse> Create_DeviceResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DeviceResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<DeviceResponse> objectInfo = new JsonObjectInfoValues<DeviceResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DeviceResponse((RegisteredDevice)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DeviceResponsePropInit(options),
				ConstructorParameterMetadataInitializer = DeviceResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DeviceResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(RegisteredDevice) }, null),
				SerializeHandler = DeviceResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DeviceResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<RegisteredDevice> propertyInfo = new JsonPropertyInfoValues<RegisteredDevice>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DeviceResponse),
			Converter = null,
			Getter = (object obj) => ((DeviceResponse)obj).Device,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Device",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DeviceResponse).GetProperty("Device", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(RegisteredDevice), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void DeviceResponseSerializeHandler(Utf8JsonWriter writer, DeviceResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		RegisteredDevice device = value.Device;
		if ((object)device != null)
		{
			writer.WritePropertyName(PropName_device);
			RegisteredDeviceSerializeHandler(writer, device);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DeviceResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Device",
				ParameterType = typeof(RegisteredDevice),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DevicesResponse> Create_DevicesResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DevicesResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<DevicesResponse> objectInfo = new JsonObjectInfoValues<DevicesResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DevicesResponse((IReadOnlyList<RegisteredDevice>)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DevicesResponsePropInit(options),
				ConstructorParameterMetadataInitializer = DevicesResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DevicesResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(IReadOnlyList<RegisteredDevice>) }, null),
				SerializeHandler = DevicesResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DevicesResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<IReadOnlyList<RegisteredDevice>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<RegisteredDevice>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DevicesResponse),
			Converter = null,
			Getter = (object obj) => ((DevicesResponse)obj).Devices,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Devices",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DevicesResponse).GetProperty("Devices", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<RegisteredDevice>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void DevicesResponseSerializeHandler(Utf8JsonWriter writer, DevicesResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<RegisteredDevice> devices = value.Devices;
		if (devices != null)
		{
			writer.WritePropertyName(PropName_devices);
			IReadOnlyListRegisteredDeviceSerializeHandler(writer, devices);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DevicesResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Devices",
				ParameterType = typeof(IReadOnlyList<RegisteredDevice>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DiscoveryChannelRequest> Create_DiscoveryChannelRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DiscoveryChannelRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<DiscoveryChannelRequest> objectInfo = new JsonObjectInfoValues<DiscoveryChannelRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DiscoveryChannelRequest((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DiscoveryChannelRequestPropInit(options),
				ConstructorParameterMetadataInitializer = DiscoveryChannelRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DiscoveryChannelRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = DiscoveryChannelRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DiscoveryChannelRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DiscoveryChannelRequest),
			Converter = null,
			Getter = (object obj) => ((DiscoveryChannelRequest)obj).DiscoveryChannel,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DiscoveryChannelRequest).GetProperty("DiscoveryChannel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DiscoveryChannelRequest),
			Converter = null,
			Getter = (object obj) => ((DiscoveryChannelRequest)obj).DiscoveryChannelOther,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannelOther",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DiscoveryChannelRequest).GetProperty("DiscoveryChannelOther", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void DiscoveryChannelRequestSerializeHandler(Utf8JsonWriter writer, DiscoveryChannelRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string discoveryChannel = value.DiscoveryChannel;
		if (discoveryChannel != null)
		{
			writer.WriteString(PropName_discovery_channel, discoveryChannel);
		}
		string discoveryChannelOther = value.DiscoveryChannelOther;
		if (discoveryChannelOther != null)
		{
			writer.WriteString(PropName_discovery_channel_other, discoveryChannelOther);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DiscoveryChannelRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "DiscoveryChannel",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DiscoveryChannelOther",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<DiscoveryChannelResponse> Create_DiscoveryChannelResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DiscoveryChannelResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<DiscoveryChannelResponse> objectInfo = new JsonObjectInfoValues<DiscoveryChannelResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new DiscoveryChannelResponse((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => DiscoveryChannelResponsePropInit(options),
				ConstructorParameterMetadataInitializer = DiscoveryChannelResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(DiscoveryChannelResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = DiscoveryChannelResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DiscoveryChannelResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DiscoveryChannelResponse),
			Converter = null,
			Getter = (object obj) => ((DiscoveryChannelResponse)obj).DiscoveryChannel,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DiscoveryChannelResponse).GetProperty("DiscoveryChannel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DiscoveryChannelResponse),
			Converter = null,
			Getter = (object obj) => ((DiscoveryChannelResponse)obj).DiscoveryChannelOther,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscoveryChannelOther",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DiscoveryChannelResponse).GetProperty("DiscoveryChannelOther", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void DiscoveryChannelResponseSerializeHandler(Utf8JsonWriter writer, DiscoveryChannelResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string discoveryChannel = value.DiscoveryChannel;
		if (discoveryChannel != null)
		{
			writer.WriteString(PropName_discovery_channel, discoveryChannel);
		}
		string discoveryChannelOther = value.DiscoveryChannelOther;
		if (discoveryChannelOther != null)
		{
			writer.WriteString(PropName_discovery_channel_other, discoveryChannelOther);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] DiscoveryChannelResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "DiscoveryChannel",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "DiscoveryChannelOther",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<EarsTranscribeRequest> Create_EarsTranscribeRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<EarsTranscribeRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<EarsTranscribeRequest> objectInfo = new JsonObjectInfoValues<EarsTranscribeRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new EarsTranscribeRequest((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => EarsTranscribeRequestPropInit(options),
				ConstructorParameterMetadataInitializer = EarsTranscribeRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(EarsTranscribeRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = EarsTranscribeRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] EarsTranscribeRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(EarsTranscribeRequest),
			Converter = null,
			Getter = (object obj) => ((EarsTranscribeRequest)obj).AudioWavBase64,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AudioWavBase64",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(EarsTranscribeRequest).GetProperty("AudioWavBase64", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(EarsTranscribeRequest),
			Converter = null,
			Getter = (object obj) => ((EarsTranscribeRequest)obj).Language,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Language",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(EarsTranscribeRequest).GetProperty("Language", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		return array;
	}

	private void EarsTranscribeRequestSerializeHandler(Utf8JsonWriter writer, EarsTranscribeRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string audioWavBase = value.AudioWavBase64;
		if (audioWavBase != null)
		{
			writer.WriteString(PropName_audio_wav_base64, audioWavBase);
		}
		string language = value.Language;
		if (language != null)
		{
			writer.WriteString(PropName_language, language);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] EarsTranscribeRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "AudioWavBase64",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Language",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<EarsTranscribeResponse> Create_EarsTranscribeResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<EarsTranscribeResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<EarsTranscribeResponse> objectInfo = new JsonObjectInfoValues<EarsTranscribeResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new EarsTranscribeResponse((string)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => EarsTranscribeResponsePropInit(options),
				ConstructorParameterMetadataInitializer = EarsTranscribeResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(EarsTranscribeResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null),
				SerializeHandler = EarsTranscribeResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] EarsTranscribeResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(EarsTranscribeResponse),
			Converter = null,
			Getter = (object obj) => ((EarsTranscribeResponse)obj).Text,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Text",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(EarsTranscribeResponse).GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void EarsTranscribeResponseSerializeHandler(Utf8JsonWriter writer, EarsTranscribeResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string text = value.Text;
		if (text != null)
		{
			writer.WriteString(PropName_text, text);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] EarsTranscribeResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Text",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ErrorReport> Create_ErrorReport(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ErrorReport> jsonTypeInfo))
		{
			JsonObjectInfoValues<ErrorReport> objectInfo = new JsonObjectInfoValues<ErrorReport>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ErrorReport((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (int)args[5], (string)args[6], (string)args[7], (string)args[8]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ErrorReportPropInit(options),
				ConstructorParameterMetadataInitializer = ErrorReportCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ErrorReport).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[9]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(int),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = ErrorReportSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ErrorReportPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[9];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).Stage,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Stage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("Stage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).ErrorKind,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ErrorKind",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("ErrorKind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).ErrorMessage,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ErrorMessage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("ErrorMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).UserFacingMessage,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UserFacingMessage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("UserFacingMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).AppVersion,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AppVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("AppVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo6 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).ErrorCode,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ErrorCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("ErrorCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).ErrorDomain,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ErrorDomain",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("ErrorDomain", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).SupabaseUserId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SupabaseUserId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("SupabaseUserId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ErrorReport),
			Converter = null,
			Getter = (object obj) => ((ErrorReport)obj).UserEmail,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UserEmail",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ErrorReport).GetProperty("UserEmail", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		return array;
	}

	private void ErrorReportSerializeHandler(Utf8JsonWriter writer, ErrorReport? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string stage = value.Stage;
		if (stage != null)
		{
			writer.WriteString(PropName_stage, stage);
		}
		string errorKind = value.ErrorKind;
		if (errorKind != null)
		{
			writer.WriteString(PropName_error_kind, errorKind);
		}
		string errorMessage = value.ErrorMessage;
		if (errorMessage != null)
		{
			writer.WriteString(PropName_error_message, errorMessage);
		}
		string userFacingMessage = value.UserFacingMessage;
		if (userFacingMessage != null)
		{
			writer.WriteString(PropName_user_facing_message, userFacingMessage);
		}
		string appVersion = value.AppVersion;
		if (appVersion != null)
		{
			writer.WriteString(PropName_app_version, appVersion);
		}
		writer.WriteNumber(PropName_error_code, value.ErrorCode);
		string errorDomain = value.ErrorDomain;
		if (errorDomain != null)
		{
			writer.WriteString(PropName_error_domain, errorDomain);
		}
		string supabaseUserId = value.SupabaseUserId;
		if (supabaseUserId != null)
		{
			writer.WriteString(PropName_supabase_user_id, supabaseUserId);
		}
		string userEmail = value.UserEmail;
		if (userEmail != null)
		{
			writer.WriteString(PropName_user_email, userEmail);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ErrorReportCtorParamInit()
	{
		return new JsonParameterInfoValues[9]
		{
			new JsonParameterInfoValues
			{
				Name = "Stage",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ErrorKind",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ErrorMessage",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "UserFacingMessage",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "AppVersion",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ErrorCode",
				ParameterType = typeof(int),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ErrorDomain",
				ParameterType = typeof(string),
				Position = 6,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "SupabaseUserId",
				ParameterType = typeof(string),
				Position = 7,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "UserEmail",
				ParameterType = typeof(string),
				Position = 8,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ImageSearchRequest> Create_ImageSearchRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ImageSearchRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<ImageSearchRequest> objectInfo = new JsonObjectInfoValues<ImageSearchRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ImageSearchRequest((string)args[0], (int)args[1], (bool?)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ImageSearchRequestPropInit(options),
				ConstructorParameterMetadataInitializer = ImageSearchRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ImageSearchRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(string),
					typeof(int),
					typeof(bool?),
					typeof(string)
				}, null),
				SerializeHandler = ImageSearchRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ImageSearchRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchRequest),
			Converter = null,
			Getter = (object obj) => ((ImageSearchRequest)obj).Query,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Query",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchRequest).GetProperty("Query", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchRequest),
			Converter = null,
			Getter = (object obj) => ((ImageSearchRequest)obj).Limit,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Limit",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchRequest).GetProperty("Limit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool?> propertyInfo3 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchRequest),
			Converter = null,
			Getter = (object obj) => ((ImageSearchRequest)obj).SubjectPhoto,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SubjectPhoto",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchRequest).GetProperty("SubjectPhoto", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchRequest),
			Converter = null,
			Getter = (object obj) => ((ImageSearchRequest)obj).PageUrl,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PageUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchRequest).GetProperty("PageUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void ImageSearchRequestSerializeHandler(Utf8JsonWriter writer, ImageSearchRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string query = value.Query;
		if (query != null)
		{
			writer.WriteString(PropName_query, query);
		}
		writer.WriteNumber(PropName_limit, value.Limit);
		bool? subjectPhoto = value.SubjectPhoto;
		if (subjectPhoto.HasValue)
		{
			writer.WritePropertyName(PropName_subject_photo);
			JsonSerializer.Serialize(writer, subjectPhoto, NullableBoolean);
		}
		string pageUrl = value.PageUrl;
		if (pageUrl != null)
		{
			writer.WriteString(PropName_page_url, pageUrl);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ImageSearchRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "Query",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Limit",
				ParameterType = typeof(int),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "SubjectPhoto",
				ParameterType = typeof(bool?),
				Position = 2,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "PageUrl",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ImageSearchResponse> Create_ImageSearchResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ImageSearchResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<ImageSearchResponse> objectInfo = new JsonObjectInfoValues<ImageSearchResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ImageSearchResponse((IReadOnlyList<ImageSearchResult>)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ImageSearchResponsePropInit(options),
				ConstructorParameterMetadataInitializer = ImageSearchResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ImageSearchResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(IReadOnlyList<ImageSearchResult>) }, null),
				SerializeHandler = ImageSearchResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ImageSearchResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<IReadOnlyList<ImageSearchResult>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<ImageSearchResult>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchResponse),
			Converter = null,
			Getter = (object obj) => ((ImageSearchResponse)obj).Images,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Images",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchResponse).GetProperty("Images", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<ImageSearchResult>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void ImageSearchResponseSerializeHandler(Utf8JsonWriter writer, ImageSearchResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<ImageSearchResult> images = value.Images;
		if (images != null)
		{
			writer.WritePropertyName(PropName_images);
			IReadOnlyListImageSearchResultSerializeHandler(writer, images);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ImageSearchResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Images",
				ParameterType = typeof(IReadOnlyList<ImageSearchResult>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ImageSearchResult> Create_ImageSearchResult(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ImageSearchResult> jsonTypeInfo))
		{
			JsonObjectInfoValues<ImageSearchResult> objectInfo = new JsonObjectInfoValues<ImageSearchResult>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ImageSearchResult((string)args[0], (string)args[1], (string)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ImageSearchResultPropInit(options),
				ConstructorParameterMetadataInitializer = ImageSearchResultCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ImageSearchResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = ImageSearchResultSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ImageSearchResultPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchResult),
			Converter = null,
			Getter = (object obj) => ((ImageSearchResult)obj).ThumbnailUrl,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ThumbnailUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchResult).GetProperty("ThumbnailUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchResult),
			Converter = null,
			Getter = (object obj) => ((ImageSearchResult)obj).FullUrl,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FullUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchResult).GetProperty("FullUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchResult),
			Converter = null,
			Getter = (object obj) => ((ImageSearchResult)obj).SourceUrl,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SourceUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchResult).GetProperty("SourceUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ImageSearchResult),
			Converter = null,
			Getter = (object obj) => ((ImageSearchResult)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ImageSearchResult).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		return array;
	}

	private void ImageSearchResultSerializeHandler(Utf8JsonWriter writer, ImageSearchResult? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string thumbnailUrl = value.ThumbnailUrl;
		if (thumbnailUrl != null)
		{
			writer.WriteString(PropName_thumbnail_url, thumbnailUrl);
		}
		string fullUrl = value.FullUrl;
		if (fullUrl != null)
		{
			writer.WriteString(PropName_full_url, fullUrl);
		}
		string sourceUrl = value.SourceUrl;
		if (sourceUrl != null)
		{
			writer.WriteString(PropName_source_url, sourceUrl);
		}
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ImageSearchResultCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "ThumbnailUrl",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "FullUrl",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "SourceUrl",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<InterviewLine> Create_InterviewLine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InterviewLine> jsonTypeInfo))
		{
			JsonObjectInfoValues<InterviewLine> objectInfo = new JsonObjectInfoValues<InterviewLine>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new InterviewLine((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => InterviewLinePropInit(options),
				ConstructorParameterMetadataInitializer = InterviewLineCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(InterviewLine).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = InterviewLineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] InterviewLinePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InterviewLine),
			Converter = null,
			Getter = (object obj) => ((InterviewLine)obj).Role,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Role",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(InterviewLine).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InterviewLine),
			Converter = null,
			Getter = (object obj) => ((InterviewLine)obj).Text,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Text",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(InterviewLine).GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		return array;
	}

	private void InterviewLineSerializeHandler(Utf8JsonWriter writer, InterviewLine? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string role = value.Role;
		if (role != null)
		{
			writer.WriteString(PropName_role, role);
		}
		string text = value.Text;
		if (text != null)
		{
			writer.WriteString(PropName_text, text);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] InterviewLineCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Role",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Text",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<PlanSnapshot> Create_PlanSnapshot(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PlanSnapshot> jsonTypeInfo))
		{
			JsonObjectInfoValues<PlanSnapshot> objectInfo = new JsonObjectInfoValues<PlanSnapshot>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PlanSnapshot((string)args[0], (PlanUsage)args[1], (string)args[2], (string)args[3], (string)args[4], (string)args[5], (PlanSubscription)args[6], (int?)args[7], (int?)args[8], (int?)args[9], (bool?)args[10], (bool?)args[11], (bool?)args[12]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PlanSnapshotPropInit(options),
				ConstructorParameterMetadataInitializer = PlanSnapshotCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PlanSnapshot).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[13]
				{
					typeof(string),
					typeof(PlanUsage),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(PlanSubscription),
					typeof(int?),
					typeof(int?),
					typeof(int?),
					typeof(bool?),
					typeof(bool?),
					typeof(bool?)
				}, null),
				SerializeHandler = PlanSnapshotSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PlanSnapshotPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[15];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).Plan,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Plan",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("Plan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<PlanUsage> propertyInfo2 = new JsonPropertyInfoValues<PlanUsage>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).Usage,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Usage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("Usage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PlanUsage), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).BillingEnforcement,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BillingEnforcement",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("BillingEnforcement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).Tier,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Tier",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("Tier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).TierSource,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TierSource",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("TierSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).TeamName,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TeamName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("TeamName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<PlanSubscription> propertyInfo7 = new JsonPropertyInfoValues<PlanSubscription>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).Subscription,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Subscription",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("Subscription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PlanSubscription), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<int?> propertyInfo8 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).UnlimitedAgentsCap,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UnlimitedAgentsCap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("UnlimitedAgentsCap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<int?> propertyInfo9 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).ProAgentsCap,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProAgentsCap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("ProAgentsCap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<int?> propertyInfo10 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).MaxAgentsCap,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MaxAgentsCap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("MaxAgentsCap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<bool?> propertyInfo11 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).ProYearlyAvailable,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProYearlyAvailable",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("ProYearlyAvailable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<bool?> propertyInfo12 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).MaxYearlyAvailable,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MaxYearlyAvailable",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("MaxYearlyAvailable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<bool?> propertyInfo13 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = (object obj) => ((PlanSnapshot)obj).IsFailOpenFallback,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsFailOpenFallback",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("IsFailOpenFallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<string> propertyInfo14 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "EffectiveTier",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("EffectiveTier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		array[13].IsGetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSnapshot),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsFreeUserOverMessageCap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSnapshot).GetProperty("IsFreeUserOverMessageCap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		return array;
	}

	private void PlanSnapshotSerializeHandler(Utf8JsonWriter writer, PlanSnapshot? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string plan = value.Plan;
		if (plan != null)
		{
			writer.WriteString(PropName_plan, plan);
		}
		PlanUsage usage = value.Usage;
		if ((object)usage != null)
		{
			writer.WritePropertyName(PropName_usage);
			PlanUsageSerializeHandler(writer, usage);
		}
		string billingEnforcement = value.BillingEnforcement;
		if (billingEnforcement != null)
		{
			writer.WriteString(PropName_billing_enforcement, billingEnforcement);
		}
		string tier = value.Tier;
		if (tier != null)
		{
			writer.WriteString(PropName_tier, tier);
		}
		string tierSource = value.TierSource;
		if (tierSource != null)
		{
			writer.WriteString(PropName_tier_source, tierSource);
		}
		string teamName = value.TeamName;
		if (teamName != null)
		{
			writer.WriteString(PropName_team_name, teamName);
		}
		PlanSubscription subscription = value.Subscription;
		if ((object)subscription != null)
		{
			writer.WritePropertyName(PropName_subscription);
			PlanSubscriptionSerializeHandler(writer, subscription);
		}
		int? unlimitedAgentsCap = value.UnlimitedAgentsCap;
		if (unlimitedAgentsCap.HasValue)
		{
			writer.WritePropertyName(PropName_unlimited_agents_cap);
			JsonSerializer.Serialize(writer, unlimitedAgentsCap, NullableInt32);
		}
		int? proAgentsCap = value.ProAgentsCap;
		if (proAgentsCap.HasValue)
		{
			writer.WritePropertyName(PropName_pro_agents_cap);
			JsonSerializer.Serialize(writer, proAgentsCap, NullableInt32);
		}
		int? maxAgentsCap = value.MaxAgentsCap;
		if (maxAgentsCap.HasValue)
		{
			writer.WritePropertyName(PropName_max_agents_cap);
			JsonSerializer.Serialize(writer, maxAgentsCap, NullableInt32);
		}
		bool? proYearlyAvailable = value.ProYearlyAvailable;
		if (proYearlyAvailable.HasValue)
		{
			writer.WritePropertyName(PropName_pro_yearly_available);
			JsonSerializer.Serialize(writer, proYearlyAvailable, NullableBoolean);
		}
		bool? maxYearlyAvailable = value.MaxYearlyAvailable;
		if (maxYearlyAvailable.HasValue)
		{
			writer.WritePropertyName(PropName_max_yearly_available);
			JsonSerializer.Serialize(writer, maxYearlyAvailable, NullableBoolean);
		}
		bool? isFailOpenFallback = value.IsFailOpenFallback;
		if (isFailOpenFallback.HasValue)
		{
			writer.WritePropertyName(PropName_is_fail_open_fallback);
			JsonSerializer.Serialize(writer, isFailOpenFallback, NullableBoolean);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PlanSnapshotCtorParamInit()
	{
		return new JsonParameterInfoValues[13]
		{
			new JsonParameterInfoValues
			{
				Name = "Plan",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Usage",
				ParameterType = typeof(PlanUsage),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "BillingEnforcement",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Tier",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "TierSource",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "TeamName",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Subscription",
				ParameterType = typeof(PlanSubscription),
				Position = 6,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "UnlimitedAgentsCap",
				ParameterType = typeof(int?),
				Position = 7,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ProAgentsCap",
				ParameterType = typeof(int?),
				Position = 8,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "MaxAgentsCap",
				ParameterType = typeof(int?),
				Position = 9,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ProYearlyAvailable",
				ParameterType = typeof(bool?),
				Position = 10,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "MaxYearlyAvailable",
				ParameterType = typeof(bool?),
				Position = 11,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsFailOpenFallback",
				ParameterType = typeof(bool?),
				Position = 12,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<PlanSubscription> Create_PlanSubscription(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PlanSubscription> jsonTypeInfo))
		{
			JsonObjectInfoValues<PlanSubscription> objectInfo = new JsonObjectInfoValues<PlanSubscription>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PlanSubscription((string)args[0], (bool)args[1], (string)args[2], (string)args[3], (string)args[4], (string)args[5], (string)args[6]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PlanSubscriptionPropInit(options),
				ConstructorParameterMetadataInitializer = PlanSubscriptionCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PlanSubscription).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[7]
				{
					typeof(string),
					typeof(bool),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = PlanSubscriptionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PlanSubscriptionPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).Status,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Status",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("Status", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).CancelAtPeriodEnd,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CancelAtPeriodEnd",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("CancelAtPeriodEnd", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).CurrentPeriodStart,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CurrentPeriodStart",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("CurrentPeriodStart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).CurrentPeriodEnd,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CurrentPeriodEnd",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("CurrentPeriodEnd", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).ProductId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProductId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("ProductId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).CancelAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CancelAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("CancelAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanSubscription),
			Converter = null,
			Getter = (object obj) => ((PlanSubscription)obj).BillingInterval,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BillingInterval",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanSubscription).GetProperty("BillingInterval", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void PlanSubscriptionSerializeHandler(Utf8JsonWriter writer, PlanSubscription? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string status = value.Status;
		if (status != null)
		{
			writer.WriteString(PropName_status, status);
		}
		writer.WriteBoolean(PropName_cancel_at_period_end, value.CancelAtPeriodEnd);
		string currentPeriodStart = value.CurrentPeriodStart;
		if (currentPeriodStart != null)
		{
			writer.WriteString(PropName_current_period_start, currentPeriodStart);
		}
		string currentPeriodEnd = value.CurrentPeriodEnd;
		if (currentPeriodEnd != null)
		{
			writer.WriteString(PropName_current_period_end, currentPeriodEnd);
		}
		string productId = value.ProductId;
		if (productId != null)
		{
			writer.WriteString(PropName_product_id, productId);
		}
		string cancelAt = value.CancelAt;
		if (cancelAt != null)
		{
			writer.WriteString(PropName_cancel_at, cancelAt);
		}
		string billingInterval = value.BillingInterval;
		if (billingInterval != null)
		{
			writer.WriteString(PropName_billing_interval, billingInterval);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PlanSubscriptionCtorParamInit()
	{
		return new JsonParameterInfoValues[7]
		{
			new JsonParameterInfoValues
			{
				Name = "Status",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CancelAtPeriodEnd",
				ParameterType = typeof(bool),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CurrentPeriodStart",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "CurrentPeriodEnd",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ProductId",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "CancelAt",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "BillingInterval",
				ParameterType = typeof(string),
				Position = 6,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<PlanUsage> Create_PlanUsage(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PlanUsage> jsonTypeInfo))
		{
			JsonObjectInfoValues<PlanUsage> objectInfo = new JsonObjectInfoValues<PlanUsage>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PlanUsage((PlanUsageBucket)args[0], (PlanUsageBucket)args[1], (PlanUsageBucket)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PlanUsagePropInit(options),
				ConstructorParameterMetadataInitializer = PlanUsageCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PlanUsage).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(PlanUsageBucket),
					typeof(PlanUsageBucket),
					typeof(PlanUsageBucket),
					typeof(string)
				}, null),
				SerializeHandler = PlanUsageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PlanUsagePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<PlanUsageBucket> propertyInfo = new JsonPropertyInfoValues<PlanUsageBucket>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsage),
			Converter = null,
			Getter = (object obj) => ((PlanUsage)obj).Messages,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Messages",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsage).GetProperty("Messages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PlanUsageBucket), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<PlanUsageBucket> propertyInfo2 = new JsonPropertyInfoValues<PlanUsageBucket>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsage),
			Converter = null,
			Getter = (object obj) => ((PlanUsage)obj).Agents,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Agents",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsage).GetProperty("Agents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PlanUsageBucket), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<PlanUsageBucket> propertyInfo3 = new JsonPropertyInfoValues<PlanUsageBucket>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsage),
			Converter = null,
			Getter = (object obj) => ((PlanUsage)obj).Dictation,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Dictation",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsage).GetProperty("Dictation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PlanUsageBucket), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsage),
			Converter = null,
			Getter = (object obj) => ((PlanUsage)obj).WindowResetsAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WindowResetsAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsage).GetProperty("WindowResetsAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void PlanUsageSerializeHandler(Utf8JsonWriter writer, PlanUsage? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		PlanUsageBucket messages = value.Messages;
		if ((object)messages != null)
		{
			writer.WritePropertyName(PropName_messages);
			JsonSerializer.Serialize(writer, messages, PlanUsageBucket);
		}
		PlanUsageBucket agents = value.Agents;
		if ((object)agents != null)
		{
			writer.WritePropertyName(PropName_agents);
			JsonSerializer.Serialize(writer, agents, PlanUsageBucket);
		}
		PlanUsageBucket dictation = value.Dictation;
		if ((object)dictation != null)
		{
			writer.WritePropertyName(PropName_dictation);
			JsonSerializer.Serialize(writer, dictation, PlanUsageBucket);
		}
		string windowResetsAt = value.WindowResetsAt;
		if (windowResetsAt != null)
		{
			writer.WriteString(PropName_window_resets_at, windowResetsAt);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PlanUsageCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "Messages",
				ParameterType = typeof(PlanUsageBucket),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Agents",
				ParameterType = typeof(PlanUsageBucket),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Dictation",
				ParameterType = typeof(PlanUsageBucket),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "WindowResetsAt",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<PlanUsageBucket> Create_PlanUsageBucket(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PlanUsageBucket> jsonTypeInfo))
		{
			JsonObjectInfoValues<PlanUsageBucket> objectInfo = new JsonObjectInfoValues<PlanUsageBucket>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PlanUsageBucket((int)args[0], (int)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PlanUsageBucketPropInit(options),
				ConstructorParameterMetadataInitializer = PlanUsageBucketCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PlanUsageBucket).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(int),
					typeof(int)
				}, null),
				SerializeHandler = null
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PlanUsageBucketPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsageBucket),
			Converter = (JsonConverter<int>)ExpandConverter(typeof(int), new SaturatingInt32Converter(), options),
			Getter = (object obj) => ((PlanUsageBucket)obj).Used,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Used",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsageBucket).GetProperty("Used", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PlanUsageBucket),
			Converter = (JsonConverter<int>)ExpandConverter(typeof(int), new SaturatingInt32Converter(), options),
			Getter = (object obj) => ((PlanUsageBucket)obj).Cap,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Cap",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PlanUsageBucket).GetProperty("Cap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private static JsonParameterInfoValues[] PlanUsageBucketCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Used",
				ParameterType = typeof(int),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Cap",
				ParameterType = typeof(int),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<PortalResponse> Create_PortalResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PortalResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<PortalResponse> objectInfo = new JsonObjectInfoValues<PortalResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PortalResponse((string)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PortalResponsePropInit(options),
				ConstructorParameterMetadataInitializer = PortalResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PortalResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null),
				SerializeHandler = PortalResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PortalResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PortalResponse),
			Converter = null,
			Getter = (object obj) => ((PortalResponse)obj).Url,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Url",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PortalResponse).GetProperty("Url", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void PortalResponseSerializeHandler(Utf8JsonWriter writer, PortalResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string url = value.Url;
		if (url != null)
		{
			writer.WriteString(PropName_url, url);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PortalResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Url",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<RealtimeSessionPromptContext> Create_RealtimeSessionPromptContext(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<RealtimeSessionPromptContext> jsonTypeInfo))
		{
			JsonObjectInfoValues<RealtimeSessionPromptContext> objectInfo = new JsonObjectInfoValues<RealtimeSessionPromptContext>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new RealtimeSessionPromptContext((IReadOnlyList<string>)args[0], (IReadOnlyList<string>)args[1], (int)args[2], (string)args[3], (string)args[4], (string)args[5], (IReadOnlyList<JsonElement>)args[6], (string)args[7]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => RealtimeSessionPromptContextPropInit(options),
				ConstructorParameterMetadataInitializer = RealtimeSessionPromptContextCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[8]
				{
					typeof(IReadOnlyList<string>),
					typeof(IReadOnlyList<string>),
					typeof(int),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<JsonElement>),
					typeof(string)
				}, null),
				SerializeHandler = RealtimeSessionPromptContextSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] RealtimeSessionPromptContextPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).ConnectedIntegrations,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ConnectedIntegrations",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("ConnectedIntegrations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo2 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).NeedsReconnectIntegrations,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NeedsReconnectIntegrations",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("NeedsReconnectIntegrations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).PriorMessageCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PriorMessageCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("PriorMessageCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).TimeZone,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TimeZone",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("TimeZone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).CurrentLocalDatetime,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CurrentLocalDatetime",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("CurrentLocalDatetime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).VoiceGender,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "VoiceGender",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("VoiceGender", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<JsonElement>> propertyInfo7 = new JsonPropertyInfoValues<IReadOnlyList<JsonElement>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).ActiveSkills,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ActiveSkills",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("ActiveSkills", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<JsonElement>), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].IsGetNullable = false;
		array[6].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionPromptContext),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionPromptContext)obj).DictationHoldShortcut,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DictationHoldShortcut",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionPromptContext).GetProperty("DictationHoldShortcut", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void RealtimeSessionPromptContextSerializeHandler(Utf8JsonWriter writer, RealtimeSessionPromptContext? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<string> connectedIntegrations = value.ConnectedIntegrations;
		if (connectedIntegrations != null)
		{
			writer.WritePropertyName(PropName_connected_integrations);
			IReadOnlyListStringSerializeHandler(writer, connectedIntegrations);
		}
		IReadOnlyList<string> needsReconnectIntegrations = value.NeedsReconnectIntegrations;
		if (needsReconnectIntegrations != null)
		{
			writer.WritePropertyName(PropName_needs_reconnect_integrations);
			IReadOnlyListStringSerializeHandler(writer, needsReconnectIntegrations);
		}
		writer.WriteNumber(PropName_prior_message_count, value.PriorMessageCount);
		string timeZone = value.TimeZone;
		if (timeZone != null)
		{
			writer.WriteString(PropName_time_zone, timeZone);
		}
		string currentLocalDatetime = value.CurrentLocalDatetime;
		if (currentLocalDatetime != null)
		{
			writer.WriteString(PropName_current_local_datetime, currentLocalDatetime);
		}
		string voiceGender = value.VoiceGender;
		if (voiceGender != null)
		{
			writer.WriteString(PropName_voice_gender, voiceGender);
		}
		IReadOnlyList<JsonElement> activeSkills = value.ActiveSkills;
		if (activeSkills != null)
		{
			writer.WritePropertyName(PropName_active_skills);
			IReadOnlyListJsonElementSerializeHandler(writer, activeSkills);
		}
		string dictationHoldShortcut = value.DictationHoldShortcut;
		if (dictationHoldShortcut != null)
		{
			writer.WriteString(PropName_dictation_hold_shortcut, dictationHoldShortcut);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] RealtimeSessionPromptContextCtorParamInit()
	{
		return new JsonParameterInfoValues[8]
		{
			new JsonParameterInfoValues
			{
				Name = "ConnectedIntegrations",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "NeedsReconnectIntegrations",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "PriorMessageCount",
				ParameterType = typeof(int),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "TimeZone",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CurrentLocalDatetime",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "VoiceGender",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ActiveSkills",
				ParameterType = typeof(IReadOnlyList<JsonElement>),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DictationHoldShortcut",
				ParameterType = typeof(string),
				Position = 7,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<RealtimeSessionRequest> Create_RealtimeSessionRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<RealtimeSessionRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<RealtimeSessionRequest> objectInfo = new JsonObjectInfoValues<RealtimeSessionRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new RealtimeSessionRequest((string)args[0], (double)args[1], (RealtimeSessionPromptContext)args[2], (IReadOnlyList<string>)args[3], (string)args[4], (string)args[5], (IReadOnlyList<string>)args[6]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => RealtimeSessionRequestPropInit(options),
				ConstructorParameterMetadataInitializer = RealtimeSessionRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[7]
				{
					typeof(string),
					typeof(double),
					typeof(RealtimeSessionPromptContext),
					typeof(IReadOnlyList<string>),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<string>)
				}, null),
				SerializeHandler = RealtimeSessionRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] RealtimeSessionRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).Voice,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Voice",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("Voice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<double> propertyInfo2 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).Speed,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Speed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("Speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<RealtimeSessionPromptContext> propertyInfo3 = new JsonPropertyInfoValues<RealtimeSessionPromptContext>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).PromptContext,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PromptContext",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("PromptContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(RealtimeSessionPromptContext), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo4 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).ClientCapabilities,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClientCapabilities",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("ClientCapabilities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).MouthProvider,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouthProvider",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("MouthProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).Language,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Language",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("Language", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo7 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RealtimeSessionRequest),
			Converter = null,
			Getter = (object obj) => ((RealtimeSessionRequest)obj).Languages,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Languages",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RealtimeSessionRequest).GetProperty("Languages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void RealtimeSessionRequestSerializeHandler(Utf8JsonWriter writer, RealtimeSessionRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string voice = value.Voice;
		if (voice != null)
		{
			writer.WriteString(PropName_voice, voice);
		}
		writer.WriteNumber(PropName_speed, value.Speed);
		RealtimeSessionPromptContext promptContext = value.PromptContext;
		if ((object)promptContext != null)
		{
			writer.WritePropertyName(PropName_prompt_context);
			RealtimeSessionPromptContextSerializeHandler(writer, promptContext);
		}
		IReadOnlyList<string> clientCapabilities = value.ClientCapabilities;
		if (clientCapabilities != null)
		{
			writer.WritePropertyName(PropName_client_capabilities);
			IReadOnlyListStringSerializeHandler(writer, clientCapabilities);
		}
		string mouthProvider = value.MouthProvider;
		if (mouthProvider != null)
		{
			writer.WriteString(PropName_mouth_provider, mouthProvider);
		}
		string language = value.Language;
		if (language != null)
		{
			writer.WriteString(PropName_language, language);
		}
		IReadOnlyList<string> languages = value.Languages;
		if (languages != null)
		{
			writer.WritePropertyName(PropName_languages);
			IReadOnlyListStringSerializeHandler(writer, languages);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] RealtimeSessionRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[7]
		{
			new JsonParameterInfoValues
			{
				Name = "Voice",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Speed",
				ParameterType = typeof(double),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "PromptContext",
				ParameterType = typeof(RealtimeSessionPromptContext),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ClientCapabilities",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "MouthProvider",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Language",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Languages",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 6,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ReferralClaimRequest> Create_ReferralClaimRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ReferralClaimRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<ReferralClaimRequest> objectInfo = new JsonObjectInfoValues<ReferralClaimRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ReferralClaimRequest((string)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ReferralClaimRequestPropInit(options),
				ConstructorParameterMetadataInitializer = ReferralClaimRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ReferralClaimRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = ReferralClaimRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ReferralClaimRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralClaimRequest),
			Converter = null,
			Getter = (object obj) => ((ReferralClaimRequest)obj).Code,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Code",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralClaimRequest).GetProperty("Code", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralClaimRequest),
			Converter = null,
			Getter = (object obj) => ((ReferralClaimRequest)obj).Source,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Source",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralClaimRequest).GetProperty("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		return array;
	}

	private void ReferralClaimRequestSerializeHandler(Utf8JsonWriter writer, ReferralClaimRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string code = value.Code;
		if (code != null)
		{
			writer.WriteString(PropName_code, code);
		}
		string source = value.Source;
		if (source != null)
		{
			writer.WriteString(PropName_source, source);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ReferralClaimRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Code",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Source",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ReferralCodeRequest> Create_ReferralCodeRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ReferralCodeRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<ReferralCodeRequest> objectInfo = new JsonObjectInfoValues<ReferralCodeRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ReferralCodeRequest((string)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ReferralCodeRequestPropInit(options),
				ConstructorParameterMetadataInitializer = ReferralCodeRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ReferralCodeRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null),
				SerializeHandler = ReferralCodeRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ReferralCodeRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralCodeRequest),
			Converter = null,
			Getter = (object obj) => ((ReferralCodeRequest)obj).Code,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Code",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralCodeRequest).GetProperty("Code", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void ReferralCodeRequestSerializeHandler(Utf8JsonWriter writer, ReferralCodeRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string code = value.Code;
		if (code != null)
		{
			writer.WriteString(PropName_code, code);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ReferralCodeRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Code",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ReferralOutcomeResponse> Create_ReferralOutcomeResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ReferralOutcomeResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<ReferralOutcomeResponse> objectInfo = new JsonObjectInfoValues<ReferralOutcomeResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ReferralOutcomeResponse((string)args[0], (string)args[1], (double?)args[2], (string)args[3], (string)args[4], (string)args[5]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ReferralOutcomeResponsePropInit(options),
				ConstructorParameterMetadataInitializer = ReferralOutcomeResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[6]
				{
					typeof(string),
					typeof(string),
					typeof(double?),
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = ReferralOutcomeResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ReferralOutcomeResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[6];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).Outcome,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Outcome",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("Outcome", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).Message,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Message",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("Message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<double?> propertyInfo3 = new JsonPropertyInfoValues<double?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).DiscountPercent,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DiscountPercent",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("DiscountPercent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double?), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).Code,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Code",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("Code", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).ShareUrl,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShareUrl",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("ShareUrl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ReferralOutcomeResponse),
			Converter = null,
			Getter = (object obj) => ((ReferralOutcomeResponse)obj).ShareDisplayText,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShareDisplayText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ReferralOutcomeResponse).GetProperty("ShareDisplayText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		return array;
	}

	private void ReferralOutcomeResponseSerializeHandler(Utf8JsonWriter writer, ReferralOutcomeResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string outcome = value.Outcome;
		if (outcome != null)
		{
			writer.WriteString(PropName_outcome, outcome);
		}
		string message = value.Message;
		if (message != null)
		{
			writer.WriteString(PropName_message, message);
		}
		double? discountPercent = value.DiscountPercent;
		if (discountPercent.HasValue)
		{
			writer.WritePropertyName(PropName_discount_percent);
			JsonSerializer.Serialize(writer, discountPercent, NullableDouble);
		}
		string code = value.Code;
		if (code != null)
		{
			writer.WriteString(PropName_code, code);
		}
		string shareUrl = value.ShareUrl;
		if (shareUrl != null)
		{
			writer.WriteString(PropName_share_url, shareUrl);
		}
		string shareDisplayText = value.ShareDisplayText;
		if (shareDisplayText != null)
		{
			writer.WriteString(PropName_share_display_text, shareDisplayText);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ReferralOutcomeResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[6]
		{
			new JsonParameterInfoValues
			{
				Name = "Outcome",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Message",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "DiscountPercent",
				ParameterType = typeof(double?),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Code",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ShareUrl",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ShareDisplayText",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<RegisteredDevice> Create_RegisteredDevice(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<RegisteredDevice> jsonTypeInfo))
		{
			JsonObjectInfoValues<RegisteredDevice> objectInfo = new JsonObjectInfoValues<RegisteredDevice>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new RegisteredDevice((string)args[0], (bool)args[1], (string)args[2], (string)args[3], (string)args[4], (string)args[5], (string)args[6], (string)args[7], (IReadOnlyList<string>)args[8], (string)args[9], (string)args[10], (string)args[11], (int?)args[12]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => RegisteredDevicePropInit(options),
				ConstructorParameterMetadataInitializer = RegisteredDeviceCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(RegisteredDevice).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[13]
				{
					typeof(string),
					typeof(bool),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<string>),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(int?)
				}, null),
				SerializeHandler = RegisteredDeviceSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] RegisteredDevicePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[13];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).Id,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).IsThisDevice,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsThisDevice",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("IsThisDevice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).Platform,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Platform",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("Platform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).DisplayName,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DisplayName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).Model,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Model",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).OsVersion,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OsVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("OsVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).AppVersion,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AppVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("AppVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).AppBuild,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AppBuild",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("AppBuild", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo9 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).Capabilities,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Capabilities",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("Capabilities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<string> propertyInfo10 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).FirstSeenAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FirstSeenAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("FirstSeenAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<string> propertyInfo11 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).LastSeenAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LastSeenAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("LastSeenAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).LastActiveAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LastActiveAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("LastActiveAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<int?> propertyInfo13 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(RegisteredDevice),
			Converter = null,
			Getter = (object obj) => ((RegisteredDevice)obj).MessagesLast14Days,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MessagesLast14Days",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(RegisteredDevice).GetProperty("MessagesLast14Days", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		return array;
	}

	private void RegisteredDeviceSerializeHandler(Utf8JsonWriter writer, RegisteredDevice? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string id = value.Id;
		if (id != null)
		{
			writer.WriteString(PropName_id, id);
		}
		writer.WriteBoolean(PropName_is_this_device, value.IsThisDevice);
		string platform = value.Platform;
		if (platform != null)
		{
			writer.WriteString(PropName_platform, platform);
		}
		string displayName = value.DisplayName;
		if (displayName != null)
		{
			writer.WriteString(PropName_display_name, displayName);
		}
		string model = value.Model;
		if (model != null)
		{
			writer.WriteString(PropName_model, model);
		}
		string osVersion = value.OsVersion;
		if (osVersion != null)
		{
			writer.WriteString(PropName_os_version, osVersion);
		}
		string appVersion = value.AppVersion;
		if (appVersion != null)
		{
			writer.WriteString(PropName_app_version, appVersion);
		}
		string appBuild = value.AppBuild;
		if (appBuild != null)
		{
			writer.WriteString(PropName_app_build, appBuild);
		}
		IReadOnlyList<string> capabilities = value.Capabilities;
		if (capabilities != null)
		{
			writer.WritePropertyName(PropName_capabilities);
			IReadOnlyListStringSerializeHandler(writer, capabilities);
		}
		string firstSeenAt = value.FirstSeenAt;
		if (firstSeenAt != null)
		{
			writer.WriteString(PropName_first_seen_at, firstSeenAt);
		}
		string lastSeenAt = value.LastSeenAt;
		if (lastSeenAt != null)
		{
			writer.WriteString(PropName_last_seen_at, lastSeenAt);
		}
		string lastActiveAt = value.LastActiveAt;
		if (lastActiveAt != null)
		{
			writer.WriteString(PropName_last_active_at, lastActiveAt);
		}
		int? messagesLast14Days = value.MessagesLast14Days;
		if (messagesLast14Days.HasValue)
		{
			writer.WritePropertyName(PropName_messages_last14_days);
			JsonSerializer.Serialize(writer, messagesLast14Days, NullableInt32);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] RegisteredDeviceCtorParamInit()
	{
		return new JsonParameterInfoValues[13]
		{
			new JsonParameterInfoValues
			{
				Name = "Id",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsThisDevice",
				ParameterType = typeof(bool),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Platform",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "DisplayName",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Model",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "OsVersion",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "AppVersion",
				ParameterType = typeof(string),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "AppBuild",
				ParameterType = typeof(string),
				Position = 7,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Capabilities",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 8,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "FirstSeenAt",
				ParameterType = typeof(string),
				Position = 9,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "LastSeenAt",
				ParameterType = typeof(string),
				Position = 10,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "LastActiveAt",
				ParameterType = typeof(string),
				Position = 11,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "MessagesLast14Days",
				ParameterType = typeof(int?),
				Position = 12,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<SaveMemoryRequest> Create_SaveMemoryRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<SaveMemoryRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<SaveMemoryRequest> objectInfo = new JsonObjectInfoValues<SaveMemoryRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new SaveMemoryRequest((string)args[0]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => SaveMemoryRequestPropInit(options),
				ConstructorParameterMetadataInitializer = SaveMemoryRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(SaveMemoryRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null),
				SerializeHandler = SaveMemoryRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] SaveMemoryRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(SaveMemoryRequest),
			Converter = null,
			Getter = (object obj) => ((SaveMemoryRequest)obj).Memory,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Memory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(SaveMemoryRequest).GetProperty("Memory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void SaveMemoryRequestSerializeHandler(Utf8JsonWriter writer, SaveMemoryRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string memory = value.Memory;
		if (memory != null)
		{
			writer.WriteString(PropName_memory, memory);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] SaveMemoryRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[1]
		{
			new JsonParameterInfoValues
			{
				Name = "Memory",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<SaveMemoryResponse> Create_SaveMemoryResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<SaveMemoryResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<SaveMemoryResponse> objectInfo = new JsonObjectInfoValues<SaveMemoryResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new SaveMemoryResponse((bool)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => SaveMemoryResponsePropInit(options),
				ConstructorParameterMetadataInitializer = SaveMemoryResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(SaveMemoryResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(bool),
					typeof(string)
				}, null),
				SerializeHandler = SaveMemoryResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] SaveMemoryResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(SaveMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((SaveMemoryResponse)obj).Saved,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Saved",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(SaveMemoryResponse).GetProperty("Saved", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(SaveMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((SaveMemoryResponse)obj).Error,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Error",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(SaveMemoryResponse).GetProperty("Error", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void SaveMemoryResponseSerializeHandler(Utf8JsonWriter writer, SaveMemoryResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_saved, value.Saved);
		string error = value.Error;
		if (error != null)
		{
			writer.WriteString(PropName_error, error);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] SaveMemoryResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Saved",
				ParameterType = typeof(bool),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Error",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<StarterPlanClicky> Create_StarterPlanClicky(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanClicky> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanClicky> objectInfo = new JsonObjectInfoValues<StarterPlanClicky>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanClicky((string)args[0], (string)args[1], (string)args[2], (string)args[3], (IReadOnlyList<string>)args[4], (string)args[5], (IReadOnlyList<string>)args[6], (StarterPlanClickyRoutine)args[7]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanClickyPropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanClickyCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanClicky).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[8]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<string>),
					typeof(string),
					typeof(IReadOnlyList<string>),
					typeof(StarterPlanClickyRoutine)
				}, null),
				SerializeHandler = StarterPlanClickySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanClickyPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).Key,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Key",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).Name,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).Role,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Role",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).Description,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo5 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).IntroMessages,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntroMessages",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("IntroMessages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).RevealLine,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RevealLine",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("RevealLine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo7 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).FirstAsks,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FirstAsks",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("FirstAsks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<StarterPlanClickyRoutine> propertyInfo8 = new JsonPropertyInfoValues<StarterPlanClickyRoutine>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClicky),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClicky)obj).Routine,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Routine",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClicky).GetProperty("Routine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(StarterPlanClickyRoutine), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void StarterPlanClickySerializeHandler(Utf8JsonWriter writer, StarterPlanClicky? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string key = value.Key;
		if (key != null)
		{
			writer.WriteString(PropName_key, key);
		}
		string name = value.Name;
		if (name != null)
		{
			writer.WriteString(PropName_name, name);
		}
		string role = value.Role;
		if (role != null)
		{
			writer.WriteString(PropName_role, role);
		}
		string description = value.Description;
		if (description != null)
		{
			writer.WriteString(PropName_description, description);
		}
		IReadOnlyList<string> introMessages = value.IntroMessages;
		if (introMessages != null)
		{
			writer.WritePropertyName(PropName_intro_messages);
			IReadOnlyListStringSerializeHandler(writer, introMessages);
		}
		string revealLine = value.RevealLine;
		if (revealLine != null)
		{
			writer.WriteString(PropName_reveal_line, revealLine);
		}
		IReadOnlyList<string> firstAsks = value.FirstAsks;
		if (firstAsks != null)
		{
			writer.WritePropertyName(PropName_first_asks);
			IReadOnlyListStringSerializeHandler(writer, firstAsks);
		}
		StarterPlanClickyRoutine routine = value.Routine;
		if ((object)routine != null)
		{
			writer.WritePropertyName(PropName_routine);
			StarterPlanClickyRoutineSerializeHandler(writer, routine);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanClickyCtorParamInit()
	{
		return new JsonParameterInfoValues[8]
		{
			new JsonParameterInfoValues
			{
				Name = "Key",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Name",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Role",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Description",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "IntroMessages",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "RevealLine",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "FirstAsks",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Routine",
				ParameterType = typeof(StarterPlanClickyRoutine),
				Position = 7,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<StarterPlanClickyInput> Create_StarterPlanClickyInput(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanClickyInput> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanClickyInput> objectInfo = new JsonObjectInfoValues<StarterPlanClickyInput>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanClickyInput((string)args[0], (string)args[1], (string)args[2], (string)args[3], (IReadOnlyList<StarterPlanRoutine>)args[4], (IReadOnlyList<string>)args[5]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanClickyInputPropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanClickyInputCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[6]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<StarterPlanRoutine>),
					typeof(IReadOnlyList<string>)
				}, null),
				SerializeHandler = StarterPlanClickyInputSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanClickyInputPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[6];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).Slug,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Slug",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("Slug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).Name,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).Role,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Role",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).Description,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<StarterPlanRoutine>> propertyInfo5 = new JsonPropertyInfoValues<IReadOnlyList<StarterPlanRoutine>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).Routines,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Routines",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("Routines", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<StarterPlanRoutine>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo6 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyInput),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyInput)obj).ThreadIds,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ThreadIds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyInput).GetProperty("ThreadIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		return array;
	}

	private void StarterPlanClickyInputSerializeHandler(Utf8JsonWriter writer, StarterPlanClickyInput? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string slug = value.Slug;
		if (slug != null)
		{
			writer.WriteString(PropName_slug, slug);
		}
		string name = value.Name;
		if (name != null)
		{
			writer.WriteString(PropName_name, name);
		}
		string role = value.Role;
		if (role != null)
		{
			writer.WriteString(PropName_role, role);
		}
		string description = value.Description;
		if (description != null)
		{
			writer.WriteString(PropName_description, description);
		}
		IReadOnlyList<StarterPlanRoutine> routines = value.Routines;
		if (routines != null)
		{
			writer.WritePropertyName(PropName_routines);
			IReadOnlyListStarterPlanRoutineSerializeHandler(writer, routines);
		}
		IReadOnlyList<string> threadIds = value.ThreadIds;
		if (threadIds != null)
		{
			writer.WritePropertyName(PropName_thread_ids);
			IReadOnlyListStringSerializeHandler(writer, threadIds);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanClickyInputCtorParamInit()
	{
		return new JsonParameterInfoValues[6]
		{
			new JsonParameterInfoValues
			{
				Name = "Slug",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Name",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Role",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Description",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Routines",
				ParameterType = typeof(IReadOnlyList<StarterPlanRoutine>),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ThreadIds",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<StarterPlanClickyRoutine> Create_StarterPlanClickyRoutine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanClickyRoutine> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanClickyRoutine> objectInfo = new JsonObjectInfoValues<StarterPlanClickyRoutine>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanClickyRoutine((string)args[0], (string)args[1], (int?)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanClickyRoutinePropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanClickyRoutineCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanClickyRoutine).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(int?)
				}, null),
				SerializeHandler = StarterPlanClickyRoutineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanClickyRoutinePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyRoutine),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyRoutine)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyRoutine).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyRoutine),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyRoutine)obj).TaskPrompt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TaskPrompt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyRoutine).GetProperty("TaskPrompt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<int?> propertyInfo3 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanClickyRoutine),
			Converter = null,
			Getter = (object obj) => ((StarterPlanClickyRoutine)obj).IntervalMinutes,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntervalMinutes",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanClickyRoutine).GetProperty("IntervalMinutes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void StarterPlanClickyRoutineSerializeHandler(Utf8JsonWriter writer, StarterPlanClickyRoutine? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		string taskPrompt = value.TaskPrompt;
		if (taskPrompt != null)
		{
			writer.WriteString(PropName_task_prompt, taskPrompt);
		}
		int? intervalMinutes = value.IntervalMinutes;
		if (intervalMinutes.HasValue)
		{
			writer.WritePropertyName(PropName_interval_minutes);
			JsonSerializer.Serialize(writer, intervalMinutes, NullableInt32);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanClickyRoutineCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "TaskPrompt",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "IntervalMinutes",
				ParameterType = typeof(int?),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<StarterPlanRequest> Create_StarterPlanRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanRequest> objectInfo = new JsonObjectInfoValues<StarterPlanRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanRequest((IReadOnlyList<InterviewLine>)args[0], (IReadOnlyList<string>)args[1], (IReadOnlyList<string>)args[2], (IReadOnlyList<StarterPlanClickyInput>)args[3], (string)args[4], (bool?)args[5])
				{
					Jobs = (bool)args[6]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanRequestPropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[6]
				{
					typeof(IReadOnlyList<InterviewLine>),
					typeof(IReadOnlyList<string>),
					typeof(IReadOnlyList<string>),
					typeof(IReadOnlyList<StarterPlanClickyInput>),
					typeof(string),
					typeof(bool?)
				}, null),
				SerializeHandler = StarterPlanRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<IReadOnlyList<InterviewLine>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<InterviewLine>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).Interview,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Interview",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("Interview", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<InterviewLine>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo2 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).ConnectedIntegrations,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ConnectedIntegrations",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("ConnectedIntegrations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo3 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).AvailableIntegrations,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AvailableIntegrations",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("AvailableIntegrations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<StarterPlanClickyInput>> propertyInfo4 = new JsonPropertyInfoValues<IReadOnlyList<StarterPlanClickyInput>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).Clickys,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Clickys",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("Clickys", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<StarterPlanClickyInput>), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).FirstAsksForClicky,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FirstAsksForClicky",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("FirstAsksForClicky", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<bool?> propertyInfo6 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).InterviewSaved,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "InterviewSaved",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("InterviewSaved", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRequest),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRequest)obj).Jobs,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Jobs",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRequest).GetProperty("Jobs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void StarterPlanRequestSerializeHandler(Utf8JsonWriter writer, StarterPlanRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<InterviewLine> interview = value.Interview;
		if (interview != null)
		{
			writer.WritePropertyName(PropName_interview);
			IReadOnlyListInterviewLineSerializeHandler(writer, interview);
		}
		IReadOnlyList<string> connectedIntegrations = value.ConnectedIntegrations;
		if (connectedIntegrations != null)
		{
			writer.WritePropertyName(PropName_connected_integrations);
			IReadOnlyListStringSerializeHandler(writer, connectedIntegrations);
		}
		IReadOnlyList<string> availableIntegrations = value.AvailableIntegrations;
		if (availableIntegrations != null)
		{
			writer.WritePropertyName(PropName_available_integrations);
			IReadOnlyListStringSerializeHandler(writer, availableIntegrations);
		}
		IReadOnlyList<StarterPlanClickyInput> clickys = value.Clickys;
		if (clickys != null)
		{
			writer.WritePropertyName(PropName_clickys);
			IReadOnlyListStarterPlanClickyInputSerializeHandler(writer, clickys);
		}
		string firstAsksForClicky = value.FirstAsksForClicky;
		if (firstAsksForClicky != null)
		{
			writer.WriteString(PropName_first_asks_for_clicky, firstAsksForClicky);
		}
		bool? interviewSaved = value.InterviewSaved;
		if (interviewSaved.HasValue)
		{
			writer.WritePropertyName(PropName_interview_saved);
			JsonSerializer.Serialize(writer, interviewSaved, NullableBoolean);
		}
		writer.WriteBoolean(PropName_jobs, value.Jobs);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[7]
		{
			new JsonParameterInfoValues
			{
				Name = "Interview",
				ParameterType = typeof(IReadOnlyList<InterviewLine>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ConnectedIntegrations",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "AvailableIntegrations",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Clickys",
				ParameterType = typeof(IReadOnlyList<StarterPlanClickyInput>),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "FirstAsksForClicky",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "InterviewSaved",
				ParameterType = typeof(bool?),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Jobs",
				ParameterType = typeof(bool),
				Position = 6,
				IsNullable = false,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<StarterPlanResponse> Create_StarterPlanResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanResponse> objectInfo = new JsonObjectInfoValues<StarterPlanResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanResponse((IReadOnlyList<StarterPlanClicky>)args[0], (IReadOnlyList<StarterPlanSuggestion>)args[1], (string)args[2], (string)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanResponsePropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(IReadOnlyList<StarterPlanClicky>),
					typeof(IReadOnlyList<StarterPlanSuggestion>),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = StarterPlanResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<IReadOnlyList<StarterPlanClicky>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<StarterPlanClicky>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanResponse),
			Converter = null,
			Getter = (object obj) => ((StarterPlanResponse)obj).Clickys,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Clickys",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanResponse).GetProperty("Clickys", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<StarterPlanClicky>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<IReadOnlyList<StarterPlanSuggestion>> propertyInfo2 = new JsonPropertyInfoValues<IReadOnlyList<StarterPlanSuggestion>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanResponse),
			Converter = null,
			Getter = (object obj) => ((StarterPlanResponse)obj).SuggestedAgents,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SuggestedAgents",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanResponse).GetProperty("SuggestedAgents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<StarterPlanSuggestion>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanResponse),
			Converter = null,
			Getter = (object obj) => ((StarterPlanResponse)obj).AgentFamiliarity,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentFamiliarity",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanResponse).GetProperty("AgentFamiliarity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanResponse),
			Converter = null,
			Getter = (object obj) => ((StarterPlanResponse)obj).GoalLine,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GoalLine",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanResponse).GetProperty("GoalLine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void StarterPlanResponseSerializeHandler(Utf8JsonWriter writer, StarterPlanResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<StarterPlanClicky> clickys = value.Clickys;
		if (clickys != null)
		{
			writer.WritePropertyName(PropName_clickys);
			IReadOnlyListStarterPlanClickySerializeHandler(writer, clickys);
		}
		IReadOnlyList<StarterPlanSuggestion> suggestedAgents = value.SuggestedAgents;
		if (suggestedAgents != null)
		{
			writer.WritePropertyName(PropName_suggested_agents);
			IReadOnlyListStarterPlanSuggestionSerializeHandler(writer, suggestedAgents);
		}
		string agentFamiliarity = value.AgentFamiliarity;
		if (agentFamiliarity != null)
		{
			writer.WriteString(PropName_agent_familiarity, agentFamiliarity);
		}
		string goalLine = value.GoalLine;
		if (goalLine != null)
		{
			writer.WriteString(PropName_goal_line, goalLine);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "Clickys",
				ParameterType = typeof(IReadOnlyList<StarterPlanClicky>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "SuggestedAgents",
				ParameterType = typeof(IReadOnlyList<StarterPlanSuggestion>),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "AgentFamiliarity",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "GoalLine",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<StarterPlanRoutine> Create_StarterPlanRoutine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanRoutine> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanRoutine> objectInfo = new JsonObjectInfoValues<StarterPlanRoutine>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanRoutine((string)args[0], (int)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanRoutinePropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanRoutineCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanRoutine).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(int)
				}, null),
				SerializeHandler = StarterPlanRoutineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanRoutinePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRoutine),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRoutine)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRoutine).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanRoutine),
			Converter = null,
			Getter = (object obj) => ((StarterPlanRoutine)obj).EveryMinutes,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "EveryMinutes",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanRoutine).GetProperty("EveryMinutes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void StarterPlanRoutineSerializeHandler(Utf8JsonWriter writer, StarterPlanRoutine? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		writer.WriteNumber(PropName_every_minutes, value.EveryMinutes);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanRoutineCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "EveryMinutes",
				ParameterType = typeof(int),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<StarterPlanSuggestion> Create_StarterPlanSuggestion(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StarterPlanSuggestion> jsonTypeInfo))
		{
			JsonObjectInfoValues<StarterPlanSuggestion> objectInfo = new JsonObjectInfoValues<StarterPlanSuggestion>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new StarterPlanSuggestion((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (string)args[5], (StarterPlanRoutine)args[6], (int?)args[7]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => StarterPlanSuggestionPropInit(options),
				ConstructorParameterMetadataInitializer = StarterPlanSuggestionCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[8]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(StarterPlanRoutine),
					typeof(int?)
				}, null),
				SerializeHandler = StarterPlanSuggestionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] StarterPlanSuggestionPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).Description,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).AgentPrompt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AgentPrompt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("AgentPrompt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).Deliverable,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Deliverable",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("Deliverable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).ClickySlug,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClickySlug",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("ClickySlug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).ClickyKey,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClickyKey",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("ClickyKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<StarterPlanRoutine> propertyInfo7 = new JsonPropertyInfoValues<StarterPlanRoutine>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).Routine,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Routine",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("Routine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(StarterPlanRoutine), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<int?> propertyInfo8 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(StarterPlanSuggestion),
			Converter = null,
			Getter = (object obj) => ((StarterPlanSuggestion)obj).SuggestionId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SuggestionId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(StarterPlanSuggestion).GetProperty("SuggestionId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void StarterPlanSuggestionSerializeHandler(Utf8JsonWriter writer, StarterPlanSuggestion? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		string description = value.Description;
		if (description != null)
		{
			writer.WriteString(PropName_description, description);
		}
		string agentPrompt = value.AgentPrompt;
		if (agentPrompt != null)
		{
			writer.WriteString(PropName_agent_prompt, agentPrompt);
		}
		string deliverable = value.Deliverable;
		if (deliverable != null)
		{
			writer.WriteString(PropName_deliverable, deliverable);
		}
		string clickySlug = value.ClickySlug;
		if (clickySlug != null)
		{
			writer.WriteString(PropName_clicky_slug, clickySlug);
		}
		string clickyKey = value.ClickyKey;
		if (clickyKey != null)
		{
			writer.WriteString(PropName_clicky_key, clickyKey);
		}
		StarterPlanRoutine routine = value.Routine;
		if ((object)routine != null)
		{
			writer.WritePropertyName(PropName_routine);
			StarterPlanRoutineSerializeHandler(writer, routine);
		}
		int? suggestionId = value.SuggestionId;
		if (suggestionId.HasValue)
		{
			writer.WritePropertyName(PropName_suggestion_id);
			JsonSerializer.Serialize(writer, suggestionId, NullableInt32);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] StarterPlanSuggestionCtorParamInit()
	{
		return new JsonParameterInfoValues[8]
		{
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Description",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "AgentPrompt",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Deliverable",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClickySlug",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClickyKey",
				ParameterType = typeof(string),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Routine",
				ParameterType = typeof(StarterPlanRoutine),
				Position = 6,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "SuggestionId",
				ParameterType = typeof(int?),
				Position = 7,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<UserMemoryResponse> Create_UserMemoryResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<UserMemoryResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<UserMemoryResponse> objectInfo = new JsonObjectInfoValues<UserMemoryResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new UserMemoryResponse((string)args[0], (string)args[1], (string)args[2], (bool?)args[3], (string)args[4]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => UserMemoryResponsePropInit(options),
				ConstructorParameterMetadataInitializer = UserMemoryResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(UserMemoryResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[5]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(bool?),
					typeof(string)
				}, null),
				SerializeHandler = UserMemoryResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] UserMemoryResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((UserMemoryResponse)obj).Markdown,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Markdown",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserMemoryResponse).GetProperty("Markdown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((UserMemoryResponse)obj).ProfileMarkdown,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProfileMarkdown",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserMemoryResponse).GetProperty("ProfileMarkdown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((UserMemoryResponse)obj).ContextMarkdown,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ContextMarkdown",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserMemoryResponse).GetProperty("ContextMarkdown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<bool?> propertyInfo4 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((UserMemoryResponse)obj).HasProfile,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasProfile",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserMemoryResponse).GetProperty("HasProfile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserMemoryResponse),
			Converter = null,
			Getter = (object obj) => ((UserMemoryResponse)obj).UpdatedAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UpdatedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserMemoryResponse).GetProperty("UpdatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		return array;
	}

	private void UserMemoryResponseSerializeHandler(Utf8JsonWriter writer, UserMemoryResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string markdown = value.Markdown;
		if (markdown != null)
		{
			writer.WriteString(PropName_markdown, markdown);
		}
		string profileMarkdown = value.ProfileMarkdown;
		if (profileMarkdown != null)
		{
			writer.WriteString(PropName_profile_markdown, profileMarkdown);
		}
		string contextMarkdown = value.ContextMarkdown;
		if (contextMarkdown != null)
		{
			writer.WriteString(PropName_context_markdown, contextMarkdown);
		}
		bool? hasProfile = value.HasProfile;
		if (hasProfile.HasValue)
		{
			writer.WritePropertyName(PropName_has_profile);
			JsonSerializer.Serialize(writer, hasProfile, NullableBoolean);
		}
		string updatedAt = value.UpdatedAt;
		if (updatedAt != null)
		{
			writer.WriteString(PropName_updated_at, updatedAt);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] UserMemoryResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[5]
		{
			new JsonParameterInfoValues
			{
				Name = "Markdown",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ProfileMarkdown",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ContextMarkdown",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "HasProfile",
				ParameterType = typeof(bool?),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "UpdatedAt",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<WebSearchRequest> Create_WebSearchRequest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<WebSearchRequest> jsonTypeInfo))
		{
			JsonObjectInfoValues<WebSearchRequest> objectInfo = new JsonObjectInfoValues<WebSearchRequest>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new WebSearchRequest((string)args[0], (int?)args[1], (string)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => WebSearchRequestPropInit(options),
				ConstructorParameterMetadataInitializer = WebSearchRequestCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(WebSearchRequest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(string),
					typeof(int?),
					typeof(string)
				}, null),
				SerializeHandler = WebSearchRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] WebSearchRequestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchRequest),
			Converter = null,
			Getter = (object obj) => ((WebSearchRequest)obj).Query,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Query",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchRequest).GetProperty("Query", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<int?> propertyInfo2 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchRequest),
			Converter = null,
			Getter = (object obj) => ((WebSearchRequest)obj).Count,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Count",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchRequest).GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchRequest),
			Converter = null,
			Getter = (object obj) => ((WebSearchRequest)obj).Freshness,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Freshness",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchRequest).GetProperty("Freshness", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void WebSearchRequestSerializeHandler(Utf8JsonWriter writer, WebSearchRequest? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string query = value.Query;
		if (query != null)
		{
			writer.WriteString(PropName_query, query);
		}
		int? count = value.Count;
		if (count.HasValue)
		{
			writer.WritePropertyName(PropName_count);
			JsonSerializer.Serialize(writer, count, NullableInt32);
		}
		string freshness = value.Freshness;
		if (freshness != null)
		{
			writer.WriteString(PropName_freshness, freshness);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] WebSearchRequestCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "Query",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Count",
				ParameterType = typeof(int?),
				Position = 1,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Freshness",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<WebSearchResponse> Create_WebSearchResponse(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<WebSearchResponse> jsonTypeInfo))
		{
			JsonObjectInfoValues<WebSearchResponse> objectInfo = new JsonObjectInfoValues<WebSearchResponse>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new WebSearchResponse((string)args[0], (IReadOnlyList<WebSearchResult>)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => WebSearchResponsePropInit(options),
				ConstructorParameterMetadataInitializer = WebSearchResponseCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(WebSearchResponse).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(string),
					typeof(IReadOnlyList<WebSearchResult>)
				}, null),
				SerializeHandler = WebSearchResponseSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] WebSearchResponsePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResponse),
			Converter = null,
			Getter = (object obj) => ((WebSearchResponse)obj).Query,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Query",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResponse).GetProperty("Query", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<IReadOnlyList<WebSearchResult>> propertyInfo2 = new JsonPropertyInfoValues<IReadOnlyList<WebSearchResult>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResponse),
			Converter = null,
			Getter = (object obj) => ((WebSearchResponse)obj).Results,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Results",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResponse).GetProperty("Results", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<WebSearchResult>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void WebSearchResponseSerializeHandler(Utf8JsonWriter writer, WebSearchResponse? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string query = value.Query;
		if (query != null)
		{
			writer.WriteString(PropName_query, query);
		}
		IReadOnlyList<WebSearchResult> results = value.Results;
		if (results != null)
		{
			writer.WritePropertyName(PropName_results);
			IReadOnlyListWebSearchResultSerializeHandler(writer, results);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] WebSearchResponseCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Query",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Results",
				ParameterType = typeof(IReadOnlyList<WebSearchResult>),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<WebSearchResult> Create_WebSearchResult(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<WebSearchResult> jsonTypeInfo))
		{
			JsonObjectInfoValues<WebSearchResult> objectInfo = new JsonObjectInfoValues<WebSearchResult>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new WebSearchResult((string)args[0], (string)args[1], (string)args[2], (string)args[3], (IReadOnlyList<string>)args[4]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => WebSearchResultPropInit(options),
				ConstructorParameterMetadataInitializer = WebSearchResultCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(WebSearchResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[5]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(IReadOnlyList<string>)
				}, null),
				SerializeHandler = WebSearchResultSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] WebSearchResultPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResult),
			Converter = null,
			Getter = (object obj) => ((WebSearchResult)obj).Title,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Title",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResult).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResult),
			Converter = null,
			Getter = (object obj) => ((WebSearchResult)obj).Url,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Url",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResult).GetProperty("Url", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResult),
			Converter = null,
			Getter = (object obj) => ((WebSearchResult)obj).Description,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResult).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResult),
			Converter = null,
			Getter = (object obj) => ((WebSearchResult)obj).Age,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Age",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResult).GetProperty("Age", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo5 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(WebSearchResult),
			Converter = null,
			Getter = (object obj) => ((WebSearchResult)obj).Highlights,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Highlights",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(WebSearchResult).GetProperty("Highlights", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		return array;
	}

	private void WebSearchResultSerializeHandler(Utf8JsonWriter writer, WebSearchResult? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		string url = value.Url;
		if (url != null)
		{
			writer.WriteString(PropName_url, url);
		}
		string description = value.Description;
		if (description != null)
		{
			writer.WriteString(PropName_description, description);
		}
		string age = value.Age;
		if (age != null)
		{
			writer.WriteString(PropName_age, age);
		}
		IReadOnlyList<string> highlights = value.Highlights;
		if (highlights != null)
		{
			writer.WritePropertyName(PropName_highlights);
			IReadOnlyListStringSerializeHandler(writer, highlights);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] WebSearchResultCtorParamInit()
	{
		return new JsonParameterInfoValues[5]
		{
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Url",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Description",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Age",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Highlights",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<HotkeyBinding> Create_HotkeyBinding(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotkeyBinding> jsonTypeInfo))
		{
			JsonObjectInfoValues<HotkeyBinding> objectInfo = new JsonObjectInfoValues<HotkeyBinding>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new HotkeyBinding((HotkeyModifiers)args[0], (HotkeyTrigger)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => HotkeyBindingPropInit(options),
				ConstructorParameterMetadataInitializer = HotkeyBindingCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(HotkeyBinding).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(HotkeyModifiers),
					typeof(HotkeyTrigger)
				}, null),
				SerializeHandler = HotkeyBindingSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] HotkeyBindingPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<HotkeyModifiers> propertyInfo = new JsonPropertyInfoValues<HotkeyModifiers>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBinding),
			Converter = null,
			Getter = (object obj) => ((HotkeyBinding)obj).Modifiers,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Modifiers",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBinding).GetProperty("Modifiers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyModifiers), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<HotkeyTrigger> propertyInfo2 = new JsonPropertyInfoValues<HotkeyTrigger>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBinding),
			Converter = null,
			Getter = (object obj) => ((HotkeyBinding)obj).Trigger,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Trigger",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBinding).GetProperty("Trigger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyTrigger), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo3 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBinding),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "KeyNames",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBinding).GetProperty("KeyNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBinding),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DisplayText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBinding).GetProperty("DisplayText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		return array;
	}

	private void HotkeyBindingSerializeHandler(Utf8JsonWriter writer, HotkeyBinding? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WritePropertyName(PropName_modifiers);
		JsonSerializer.Serialize(writer, value.Modifiers, HotkeyModifiers);
		writer.WritePropertyName(PropName_trigger);
		JsonSerializer.Serialize(writer, value.Trigger, HotkeyTrigger);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] HotkeyBindingCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Modifiers",
				ParameterType = typeof(HotkeyModifiers),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Trigger",
				ParameterType = typeof(HotkeyTrigger),
				Position = 1,
				HasDefaultValue = true,
				DefaultValue = HeyClicky.HotkeyTrigger.Hold,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<HotkeyBindings> Create_HotkeyBindings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotkeyBindings> jsonTypeInfo))
		{
			JsonObjectInfoValues<HotkeyBindings> objectInfo = new JsonObjectInfoValues<HotkeyBindings>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new HotkeyBindings((HotkeyBinding)args[0], (HotkeyBinding)args[1], (HotkeyBinding)args[2], (HotkeyBinding)args[3]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => HotkeyBindingsPropInit(options),
				ConstructorParameterMetadataInitializer = HotkeyBindingsCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(HotkeyBindings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[4]
				{
					typeof(HotkeyBinding),
					typeof(HotkeyBinding),
					typeof(HotkeyBinding),
					typeof(HotkeyBinding)
				}, null),
				SerializeHandler = HotkeyBindingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] HotkeyBindingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<HotkeyBinding> propertyInfo = new JsonPropertyInfoValues<HotkeyBinding>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBindings),
			Converter = null,
			Getter = (object obj) => ((HotkeyBindings)obj).PushToTalk,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PushToTalk",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBindings).GetProperty("PushToTalk", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBinding), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyBinding> propertyInfo2 = new JsonPropertyInfoValues<HotkeyBinding>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBindings),
			Converter = null,
			Getter = (object obj) => ((HotkeyBindings)obj).TextMode,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TextMode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBindings).GetProperty("TextMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBinding), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyBinding> propertyInfo3 = new JsonPropertyInfoValues<HotkeyBinding>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBindings),
			Converter = null,
			Getter = (object obj) => ((HotkeyBindings)obj).Dictation,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Dictation",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBindings).GetProperty("Dictation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBinding), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyBinding> propertyInfo4 = new JsonPropertyInfoValues<HotkeyBinding>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyBindings),
			Converter = null,
			Getter = (object obj) => ((HotkeyBindings)obj).HandsFreeDictation,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HandsFreeDictation",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyBindings).GetProperty("HandsFreeDictation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyBinding), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		return array;
	}

	private void HotkeyBindingsSerializeHandler(Utf8JsonWriter writer, HotkeyBindings? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		HotkeyBinding pushToTalk = value.PushToTalk;
		if ((object)pushToTalk != null)
		{
			writer.WritePropertyName(PropName_push_to_talk);
			HotkeyBindingSerializeHandler(writer, pushToTalk);
		}
		HotkeyBinding textMode = value.TextMode;
		if ((object)textMode != null)
		{
			writer.WritePropertyName(PropName_text_mode);
			HotkeyBindingSerializeHandler(writer, textMode);
		}
		HotkeyBinding dictation = value.Dictation;
		if ((object)dictation != null)
		{
			writer.WritePropertyName(PropName_dictation);
			HotkeyBindingSerializeHandler(writer, dictation);
		}
		HotkeyBinding handsFreeDictation = value.HandsFreeDictation;
		if ((object)handsFreeDictation != null)
		{
			writer.WritePropertyName(PropName_hands_free_dictation);
			HotkeyBindingSerializeHandler(writer, handsFreeDictation);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] HotkeyBindingsCtorParamInit()
	{
		return new JsonParameterInfoValues[4]
		{
			new JsonParameterInfoValues
			{
				Name = "PushToTalk",
				ParameterType = typeof(HotkeyBinding),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "TextMode",
				ParameterType = typeof(HotkeyBinding),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Dictation",
				ParameterType = typeof(HotkeyBinding),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "HandsFreeDictation",
				ParameterType = typeof(HotkeyBinding),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<HotkeyModifiers> Create_HotkeyModifiers(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotkeyModifiers> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(HotkeyModifiers), new JsonStringEnumConverter<HotkeyModifiers>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<HotkeyModifiers>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<HotkeyTrigger> Create_HotkeyTrigger(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotkeyTrigger> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(HotkeyTrigger), new JsonStringEnumConverter<HotkeyTrigger>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<HotkeyTrigger>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<Dictionary<string, string>> Create_DictionaryStringString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Dictionary<string, string>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<Dictionary<string, string>> collectionInfo = new JsonCollectionInfoValues<Dictionary<string, string>>
			{
				ObjectCreator = () => new Dictionary<string, string>(),
				SerializeHandler = DictionaryStringStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, string>, string, string>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void DictionaryStringStringSerializeHandler(Utf8JsonWriter writer, Dictionary<string, string>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, string> item in value)
		{
			writer.WriteString(item.Key, item.Value);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<IReadOnlyList<PaywallPlanOption>> Create_IReadOnlyListPaywallPlanOption(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<PaywallPlanOption>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<PaywallPlanOption>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<PaywallPlanOption>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListPaywallPlanOptionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<PaywallPlanOption>, PaywallPlanOption>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListPaywallPlanOptionSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<PaywallPlanOption>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (PaywallPlanOption item in value)
		{
			PaywallPlanOptionSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<ImageSearchResult>> Create_IReadOnlyListImageSearchResult(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<ImageSearchResult>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<ImageSearchResult>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<ImageSearchResult>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListImageSearchResultSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<ImageSearchResult>, ImageSearchResult>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListImageSearchResultSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<ImageSearchResult>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (ImageSearchResult item in value)
		{
			ImageSearchResultSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<InterviewLine>> Create_IReadOnlyListInterviewLine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<InterviewLine>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<InterviewLine>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<InterviewLine>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListInterviewLineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<InterviewLine>, InterviewLine>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListInterviewLineSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<InterviewLine>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (InterviewLine item in value)
		{
			InterviewLineSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<RegisteredDevice>> Create_IReadOnlyListRegisteredDevice(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<RegisteredDevice>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<RegisteredDevice>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<RegisteredDevice>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListRegisteredDeviceSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<RegisteredDevice>, RegisteredDevice>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListRegisteredDeviceSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<RegisteredDevice>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (RegisteredDevice item in value)
		{
			RegisteredDeviceSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<StarterPlanClicky>> Create_IReadOnlyListStarterPlanClicky(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<StarterPlanClicky>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<StarterPlanClicky>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<StarterPlanClicky>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListStarterPlanClickySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<StarterPlanClicky>, StarterPlanClicky>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListStarterPlanClickySerializeHandler(Utf8JsonWriter writer, IReadOnlyList<StarterPlanClicky>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (StarterPlanClicky item in value)
		{
			StarterPlanClickySerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<StarterPlanClickyInput>> Create_IReadOnlyListStarterPlanClickyInput(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<StarterPlanClickyInput>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<StarterPlanClickyInput>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<StarterPlanClickyInput>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListStarterPlanClickyInputSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<StarterPlanClickyInput>, StarterPlanClickyInput>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListStarterPlanClickyInputSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<StarterPlanClickyInput>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (StarterPlanClickyInput item in value)
		{
			StarterPlanClickyInputSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<StarterPlanRoutine>> Create_IReadOnlyListStarterPlanRoutine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<StarterPlanRoutine>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<StarterPlanRoutine>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<StarterPlanRoutine>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListStarterPlanRoutineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<StarterPlanRoutine>, StarterPlanRoutine>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListStarterPlanRoutineSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<StarterPlanRoutine>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (StarterPlanRoutine item in value)
		{
			StarterPlanRoutineSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<StarterPlanSuggestion>> Create_IReadOnlyListStarterPlanSuggestion(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<StarterPlanSuggestion>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<StarterPlanSuggestion>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<StarterPlanSuggestion>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListStarterPlanSuggestionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<StarterPlanSuggestion>, StarterPlanSuggestion>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListStarterPlanSuggestionSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<StarterPlanSuggestion>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (StarterPlanSuggestion item in value)
		{
			StarterPlanSuggestionSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<WebSearchResult>> Create_IReadOnlyListWebSearchResult(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<WebSearchResult>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<WebSearchResult>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<WebSearchResult>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListWebSearchResultSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<WebSearchResult>, WebSearchResult>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListWebSearchResultSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<WebSearchResult>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (WebSearchResult item in value)
		{
			WebSearchResultSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<JsonElement>> Create_IReadOnlyListJsonElement(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<JsonElement>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<JsonElement>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<JsonElement>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListJsonElementSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<JsonElement>, JsonElement>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListJsonElementSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<JsonElement>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (JsonElement item in value)
		{
			JsonSerializer.Serialize(writer, item, JsonElement);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<string>> Create_IReadOnlyListString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<string>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<string>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<string>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<string>, string>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListStringSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<string>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (string item in value)
		{
			writer.WriteStringValue(item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<string>> Create_ListString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<string>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<string>> collectionInfo = new JsonCollectionInfoValues<List<string>>
			{
				ObjectCreator = () => new List<string>(),
				SerializeHandler = ListStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<string>, string>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListStringSerializeHandler(Utf8JsonWriter writer, List<string>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			writer.WriteStringValue(value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<JsonElement> Create_JsonElement(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<JsonElement> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<JsonElement>(options, JsonMetadataServices.JsonElementConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<int> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<int?> Create_NullableInt32(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<int?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<int>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<int?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<long> Create_Int64(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<long> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<long>(options, JsonMetadataServices.Int64Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public CoreJsonContext()
		: base(null)
	{
	}

	public CoreJsonContext(JsonSerializerOptions options)
		: base(options)
	{
	}

	private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
	{
		JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
		if (runtimeConverterForType != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
			return true;
		}
		jsonTypeInfo = null;
		return false;
	}

	private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
	{
		for (int i = 0; i < options.Converters.Count; i++)
		{
			JsonConverter jsonConverter = options.Converters[i];
			if (jsonConverter != null && jsonConverter.CanConvert(type))
			{
				return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
			}
		}
		return null;
	}

	private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
	{
		if (validateCanConvert && !converter.CanConvert(type))
		{
			throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
		}
		if (converter is JsonConverterFactory jsonConverterFactory)
		{
			converter = jsonConverterFactory.CreateConverter(type, options);
			if (converter == null || converter is JsonConverterFactory)
			{
				throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
			}
		}
		return converter;
	}

	public override JsonTypeInfo? GetTypeInfo(Type type)
	{
		base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
		return typeInfo;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(bool))
		{
			return Create_Boolean(options);
		}
		if (type == typeof(bool?))
		{
			return Create_NullableBoolean(options);
		}
		if (type == typeof(double))
		{
			return Create_Double(options);
		}
		if (type == typeof(double?))
		{
			return Create_NullableDouble(options);
		}
		if (type == typeof(StoredAuthSession))
		{
			return Create_StoredAuthSession(options);
		}
		if (type == typeof(StoredPendingSignIn))
		{
			return Create_StoredPendingSignIn(options);
		}
		if (type == typeof(PaywallPayload))
		{
			return Create_PaywallPayload(options);
		}
		if (type == typeof(PaywallPlanOption))
		{
			return Create_PaywallPlanOption(options);
		}
		if (type == typeof(OnboardingProgress))
		{
			return Create_OnboardingProgress(options);
		}
		if (type == typeof(AppSettings))
		{
			return Create_AppSettings(options);
		}
		if (type == typeof(AppConfig))
		{
			return Create_AppConfig(options);
		}
		if (type == typeof(AppConfigLinks))
		{
			return Create_AppConfigLinks(options);
		}
		if (type == typeof(AppConfigResponse))
		{
			return Create_AppConfigResponse(options);
		}
		if (type == typeof(CheckoutRequest))
		{
			return Create_CheckoutRequest(options);
		}
		if (type == typeof(CheckoutResponse))
		{
			return Create_CheckoutResponse(options);
		}
		if (type == typeof(DeepgramTokenResponse))
		{
			return Create_DeepgramTokenResponse(options);
		}
		if (type == typeof(DeviceRegistrationRequest))
		{
			return Create_DeviceRegistrationRequest(options);
		}
		if (type == typeof(DeviceRegistrationStamp))
		{
			return Create_DeviceRegistrationStamp(options);
		}
		if (type == typeof(DeviceResponse))
		{
			return Create_DeviceResponse(options);
		}
		if (type == typeof(DevicesResponse))
		{
			return Create_DevicesResponse(options);
		}
		if (type == typeof(DiscoveryChannelRequest))
		{
			return Create_DiscoveryChannelRequest(options);
		}
		if (type == typeof(DiscoveryChannelResponse))
		{
			return Create_DiscoveryChannelResponse(options);
		}
		if (type == typeof(EarsTranscribeRequest))
		{
			return Create_EarsTranscribeRequest(options);
		}
		if (type == typeof(EarsTranscribeResponse))
		{
			return Create_EarsTranscribeResponse(options);
		}
		if (type == typeof(ErrorReport))
		{
			return Create_ErrorReport(options);
		}
		if (type == typeof(ImageSearchRequest))
		{
			return Create_ImageSearchRequest(options);
		}
		if (type == typeof(ImageSearchResponse))
		{
			return Create_ImageSearchResponse(options);
		}
		if (type == typeof(ImageSearchResult))
		{
			return Create_ImageSearchResult(options);
		}
		if (type == typeof(InterviewLine))
		{
			return Create_InterviewLine(options);
		}
		if (type == typeof(PlanSnapshot))
		{
			return Create_PlanSnapshot(options);
		}
		if (type == typeof(PlanSubscription))
		{
			return Create_PlanSubscription(options);
		}
		if (type == typeof(PlanUsage))
		{
			return Create_PlanUsage(options);
		}
		if (type == typeof(PlanUsageBucket))
		{
			return Create_PlanUsageBucket(options);
		}
		if (type == typeof(PortalResponse))
		{
			return Create_PortalResponse(options);
		}
		if (type == typeof(RealtimeSessionPromptContext))
		{
			return Create_RealtimeSessionPromptContext(options);
		}
		if (type == typeof(RealtimeSessionRequest))
		{
			return Create_RealtimeSessionRequest(options);
		}
		if (type == typeof(ReferralClaimRequest))
		{
			return Create_ReferralClaimRequest(options);
		}
		if (type == typeof(ReferralCodeRequest))
		{
			return Create_ReferralCodeRequest(options);
		}
		if (type == typeof(ReferralOutcomeResponse))
		{
			return Create_ReferralOutcomeResponse(options);
		}
		if (type == typeof(RegisteredDevice))
		{
			return Create_RegisteredDevice(options);
		}
		if (type == typeof(SaveMemoryRequest))
		{
			return Create_SaveMemoryRequest(options);
		}
		if (type == typeof(SaveMemoryResponse))
		{
			return Create_SaveMemoryResponse(options);
		}
		if (type == typeof(StarterPlanClicky))
		{
			return Create_StarterPlanClicky(options);
		}
		if (type == typeof(StarterPlanClickyInput))
		{
			return Create_StarterPlanClickyInput(options);
		}
		if (type == typeof(StarterPlanClickyRoutine))
		{
			return Create_StarterPlanClickyRoutine(options);
		}
		if (type == typeof(StarterPlanRequest))
		{
			return Create_StarterPlanRequest(options);
		}
		if (type == typeof(StarterPlanResponse))
		{
			return Create_StarterPlanResponse(options);
		}
		if (type == typeof(StarterPlanRoutine))
		{
			return Create_StarterPlanRoutine(options);
		}
		if (type == typeof(StarterPlanSuggestion))
		{
			return Create_StarterPlanSuggestion(options);
		}
		if (type == typeof(UserMemoryResponse))
		{
			return Create_UserMemoryResponse(options);
		}
		if (type == typeof(WebSearchRequest))
		{
			return Create_WebSearchRequest(options);
		}
		if (type == typeof(WebSearchResponse))
		{
			return Create_WebSearchResponse(options);
		}
		if (type == typeof(WebSearchResult))
		{
			return Create_WebSearchResult(options);
		}
		if (type == typeof(HotkeyBinding))
		{
			return Create_HotkeyBinding(options);
		}
		if (type == typeof(HotkeyBindings))
		{
			return Create_HotkeyBindings(options);
		}
		if (type == typeof(HotkeyModifiers))
		{
			return Create_HotkeyModifiers(options);
		}
		if (type == typeof(HotkeyTrigger))
		{
			return Create_HotkeyTrigger(options);
		}
		if (type == typeof(Dictionary<string, string>))
		{
			return Create_DictionaryStringString(options);
		}
		if (type == typeof(IReadOnlyList<PaywallPlanOption>))
		{
			return Create_IReadOnlyListPaywallPlanOption(options);
		}
		if (type == typeof(IReadOnlyList<ImageSearchResult>))
		{
			return Create_IReadOnlyListImageSearchResult(options);
		}
		if (type == typeof(IReadOnlyList<InterviewLine>))
		{
			return Create_IReadOnlyListInterviewLine(options);
		}
		if (type == typeof(IReadOnlyList<RegisteredDevice>))
		{
			return Create_IReadOnlyListRegisteredDevice(options);
		}
		if (type == typeof(IReadOnlyList<StarterPlanClicky>))
		{
			return Create_IReadOnlyListStarterPlanClicky(options);
		}
		if (type == typeof(IReadOnlyList<StarterPlanClickyInput>))
		{
			return Create_IReadOnlyListStarterPlanClickyInput(options);
		}
		if (type == typeof(IReadOnlyList<StarterPlanRoutine>))
		{
			return Create_IReadOnlyListStarterPlanRoutine(options);
		}
		if (type == typeof(IReadOnlyList<StarterPlanSuggestion>))
		{
			return Create_IReadOnlyListStarterPlanSuggestion(options);
		}
		if (type == typeof(IReadOnlyList<WebSearchResult>))
		{
			return Create_IReadOnlyListWebSearchResult(options);
		}
		if (type == typeof(IReadOnlyList<JsonElement>))
		{
			return Create_IReadOnlyListJsonElement(options);
		}
		if (type == typeof(IReadOnlyList<string>))
		{
			return Create_IReadOnlyListString(options);
		}
		if (type == typeof(List<string>))
		{
			return Create_ListString(options);
		}
		if (type == typeof(JsonElement))
		{
			return Create_JsonElement(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(int?))
		{
			return Create_NullableInt32(options);
		}
		if (type == typeof(long))
		{
			return Create_Int64(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
