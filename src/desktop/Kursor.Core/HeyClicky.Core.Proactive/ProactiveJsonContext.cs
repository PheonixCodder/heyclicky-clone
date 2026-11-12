using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HeyClicky.Core.Proactive;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true, UseStringEnumConverter = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ProactiveStoreData))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
internal sealed class ProactiveJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<bool?>? _NullableBoolean;

	private JsonTypeInfo<MorningDismissalHistory>? _MorningDismissalHistory;

	private JsonTypeInfo<PendingSuggestionJob>? _PendingSuggestionJob;

	private JsonTypeInfo<ProactiveAccountState>? _ProactiveAccountState;

	private JsonTypeInfo<ProactiveCheckReason>? _ProactiveCheckReason;

	private JsonTypeInfo<ProactiveHistoryEntry>? _ProactiveHistoryEntry;

	private JsonTypeInfo<ProactiveNewClicky>? _ProactiveNewClicky;

	private JsonTypeInfo<ProactiveResolution>? _ProactiveResolution;

	private JsonTypeInfo<ProactiveRoutine>? _ProactiveRoutine;

	private JsonTypeInfo<ProactiveSlotDayState>? _ProactiveSlotDayState;

	private JsonTypeInfo<ProactiveStoreData>? _ProactiveStoreData;

	private JsonTypeInfo<ProactiveSuggestionCard>? _ProactiveSuggestionCard;

	private JsonTypeInfo<QueuedProactiveTask>? _QueuedProactiveTask;

	private JsonTypeInfo<Dictionary<string, ProactiveAccountState>>? _DictionaryStringProactiveAccountState;

	private JsonTypeInfo<Dictionary<string, DateTimeOffset>>? _DictionaryStringDateTimeOffset;

	private JsonTypeInfo<Dictionary<string, string>>? _DictionaryStringString;

	private JsonTypeInfo<List<ProactiveHistoryEntry>>? _ListProactiveHistoryEntry;

	private JsonTypeInfo<List<ProactiveSuggestionCard>>? _ListProactiveSuggestionCard;

	private JsonTypeInfo<List<QueuedProactiveTask>>? _ListQueuedProactiveTask;

	private JsonTypeInfo<List<DateTime>>? _ListDateTime;

	private JsonTypeInfo<List<DateTimeOffset>>? _ListDateTimeOffset;

	private JsonTypeInfo<List<string>>? _ListString;

	private JsonTypeInfo<DateTime>? _DateTime;

	private JsonTypeInfo<DateTimeOffset>? _DateTimeOffset;

	private JsonTypeInfo<DateTimeOffset?>? _NullableDateTimeOffset;

	private JsonTypeInfo<Guid>? _Guid;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<int?>? _NullableInt32;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		WriteIndented = true
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_dismissed_days = JsonEncodedText.Encode("dismissed_days");

	private static readonly JsonEncodedText PropName_resting_until = JsonEncodedText.Encode("resting_until");

	private static readonly JsonEncodedText PropName_job_id = JsonEncodedText.Encode("job_id");

	private static readonly JsonEncodedText PropName_started_at = JsonEncodedText.Encode("started_at");

	private static readonly JsonEncodedText PropName_check_reason = JsonEncodedText.Encode("check_reason");

	private static readonly JsonEncodedText PropName_cards = JsonEncodedText.Encode("cards");

	private static readonly JsonEncodedText PropName_queue = JsonEncodedText.Encode("queue");

	private static readonly JsonEncodedText PropName_pending_job = JsonEncodedText.Encode("pending_job");

	private static readonly JsonEncodedText PropName_slots = JsonEncodedText.Encode("slots");

	private static readonly JsonEncodedText PropName_ignored_suggestions_break_until = JsonEncodedText.Encode("ignored_suggestions_break_until");

	private static readonly JsonEncodedText PropName_suggestions_last_fetched_at = JsonEncodedText.Encode("suggestions_last_fetched_at");

	private static readonly JsonEncodedText PropName_suggestions_row_last_seen_at = JsonEncodedText.Encode("suggestions_row_last_seen_at");

	private static readonly JsonEncodedText PropName_title = JsonEncodedText.Encode("title");

	private static readonly JsonEncodedText PropName_description = JsonEncodedText.Encode("description");

	private static readonly JsonEncodedText PropName_resolution = JsonEncodedText.Encode("resolution");

	private static readonly JsonEncodedText PropName_decided_at = JsonEncodedText.Encode("decided_at");

	private static readonly JsonEncodedText PropName_deny_reason = JsonEncodedText.Encode("deny_reason");

	private static readonly JsonEncodedText PropName_was_viewed = JsonEncodedText.Encode("was_viewed");

	private static readonly JsonEncodedText PropName_wire_status = JsonEncodedText.Encode("wire_status");

	private static readonly JsonEncodedText PropName_name = JsonEncodedText.Encode("name");

	private static readonly JsonEncodedText PropName_role = JsonEncodedText.Encode("role");

	private static readonly JsonEncodedText PropName_every_minutes = JsonEncodedText.Encode("every_minutes");

	private static readonly JsonEncodedText PropName_clamped_interval_minutes = JsonEncodedText.Encode("clamped_interval_minutes");

	private static readonly JsonEncodedText PropName_cadence_label = JsonEncodedText.Encode("cadence_label");

	private static readonly JsonEncodedText PropName_cadence_spoken_label = JsonEncodedText.Encode("cadence_spoken_label");

	private static readonly JsonEncodedText PropName_morning_fired_at = JsonEncodedText.Encode("morning_fired_at");

	private static readonly JsonEncodedText PropName_retry_after = JsonEncodedText.Encode("retry_after");

	private static readonly JsonEncodedText PropName_consecutive_failed_attempts = JsonEncodedText.Encode("consecutive_failed_attempts");

	private static readonly JsonEncodedText PropName_accounts = JsonEncodedText.Encode("accounts");

	private static readonly JsonEncodedText PropName_history = JsonEncodedText.Encode("history");

	private static readonly JsonEncodedText PropName_morning_greeting_last_day = JsonEncodedText.Encode("morning_greeting_last_day");

	private static readonly JsonEncodedText PropName_morning_greeting_last_presented_at = JsonEncodedText.Encode("morning_greeting_last_presented_at");

	private static readonly JsonEncodedText PropName_morning_settled_at = JsonEncodedText.Encode("morning_settled_at");

	private static readonly JsonEncodedText PropName_morning_dismissals = JsonEncodedText.Encode("morning_dismissals");

	private static readonly JsonEncodedText PropName_onboarding_first_completed_at = JsonEncodedText.Encode("onboarding_first_completed_at");

	private static readonly JsonEncodedText PropName_integration_snoozed_until = JsonEncodedText.Encode("integration_snoozed_until");

	private static readonly JsonEncodedText PropName_integration_never_ask = JsonEncodedText.Encode("integration_never_ask");

	private static readonly JsonEncodedText PropName_integration_prompt_timestamps = JsonEncodedText.Encode("integration_prompt_timestamps");

	private static readonly JsonEncodedText PropName_spotify_voice_tip_shown = JsonEncodedText.Encode("spotify_voice_tip_shown");

	private static readonly JsonEncodedText PropName_id = JsonEncodedText.Encode("id");

	private static readonly JsonEncodedText PropName_agent_prompt = JsonEncodedText.Encode("agent_prompt");

	private static readonly JsonEncodedText PropName_server_suggestion_id = JsonEncodedText.Encode("server_suggestion_id");

	private static readonly JsonEncodedText PropName_clicky_slug = JsonEncodedText.Encode("clicky_slug");

	private static readonly JsonEncodedText PropName_new_clicky = JsonEncodedText.Encode("new_clicky");

	private static readonly JsonEncodedText PropName_routine = JsonEncodedText.Encode("routine");

	private static readonly JsonEncodedText PropName_integration_name = JsonEncodedText.Encode("integration_name");

	private static readonly JsonEncodedText PropName_created_at = JsonEncodedText.Encode("created_at");

	private static readonly JsonEncodedText PropName_viewed_at = JsonEncodedText.Encode("viewed_at");

	private static readonly JsonEncodedText PropName_expires_at = JsonEncodedText.Encode("expires_at");

	private static readonly JsonEncodedText PropName_card = JsonEncodedText.Encode("card");

	private static readonly JsonEncodedText PropName_adjustment_text = JsonEncodedText.Encode("adjustment_text");

	private static readonly JsonEncodedText PropName_was_founded = JsonEncodedText.Encode("was_founded");

	private static readonly JsonEncodedText PropName_was_restored = JsonEncodedText.Encode("was_restored");

	private static readonly JsonEncodedText PropName_is_paused = JsonEncodedText.Encode("is_paused");

	private static readonly JsonEncodedText PropName_is_launching = JsonEncodedText.Encode("is_launching");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = (JsonTypeInfo<bool?>)base.Options.GetTypeInfo(typeof(bool?)));

	public JsonTypeInfo<MorningDismissalHistory> MorningDismissalHistory => _MorningDismissalHistory ?? (_MorningDismissalHistory = (JsonTypeInfo<MorningDismissalHistory>)base.Options.GetTypeInfo(typeof(MorningDismissalHistory)));

	public JsonTypeInfo<PendingSuggestionJob> PendingSuggestionJob => _PendingSuggestionJob ?? (_PendingSuggestionJob = (JsonTypeInfo<PendingSuggestionJob>)base.Options.GetTypeInfo(typeof(PendingSuggestionJob)));

	public JsonTypeInfo<ProactiveAccountState> ProactiveAccountState => _ProactiveAccountState ?? (_ProactiveAccountState = (JsonTypeInfo<ProactiveAccountState>)base.Options.GetTypeInfo(typeof(ProactiveAccountState)));

	public JsonTypeInfo<ProactiveCheckReason> ProactiveCheckReason => _ProactiveCheckReason ?? (_ProactiveCheckReason = (JsonTypeInfo<ProactiveCheckReason>)base.Options.GetTypeInfo(typeof(ProactiveCheckReason)));

	public JsonTypeInfo<ProactiveHistoryEntry> ProactiveHistoryEntry => _ProactiveHistoryEntry ?? (_ProactiveHistoryEntry = (JsonTypeInfo<ProactiveHistoryEntry>)base.Options.GetTypeInfo(typeof(ProactiveHistoryEntry)));

	public JsonTypeInfo<ProactiveNewClicky> ProactiveNewClicky => _ProactiveNewClicky ?? (_ProactiveNewClicky = (JsonTypeInfo<ProactiveNewClicky>)base.Options.GetTypeInfo(typeof(ProactiveNewClicky)));

	public JsonTypeInfo<ProactiveResolution> ProactiveResolution => _ProactiveResolution ?? (_ProactiveResolution = (JsonTypeInfo<ProactiveResolution>)base.Options.GetTypeInfo(typeof(ProactiveResolution)));

	public JsonTypeInfo<ProactiveRoutine> ProactiveRoutine => _ProactiveRoutine ?? (_ProactiveRoutine = (JsonTypeInfo<ProactiveRoutine>)base.Options.GetTypeInfo(typeof(ProactiveRoutine)));

	public JsonTypeInfo<ProactiveSlotDayState> ProactiveSlotDayState => _ProactiveSlotDayState ?? (_ProactiveSlotDayState = (JsonTypeInfo<ProactiveSlotDayState>)base.Options.GetTypeInfo(typeof(ProactiveSlotDayState)));

	public JsonTypeInfo<ProactiveStoreData> ProactiveStoreData => _ProactiveStoreData ?? (_ProactiveStoreData = (JsonTypeInfo<ProactiveStoreData>)base.Options.GetTypeInfo(typeof(ProactiveStoreData)));

	public JsonTypeInfo<ProactiveSuggestionCard> ProactiveSuggestionCard => _ProactiveSuggestionCard ?? (_ProactiveSuggestionCard = (JsonTypeInfo<ProactiveSuggestionCard>)base.Options.GetTypeInfo(typeof(ProactiveSuggestionCard)));

	public JsonTypeInfo<QueuedProactiveTask> QueuedProactiveTask => _QueuedProactiveTask ?? (_QueuedProactiveTask = (JsonTypeInfo<QueuedProactiveTask>)base.Options.GetTypeInfo(typeof(QueuedProactiveTask)));

	public JsonTypeInfo<Dictionary<string, ProactiveAccountState>> DictionaryStringProactiveAccountState => _DictionaryStringProactiveAccountState ?? (_DictionaryStringProactiveAccountState = (JsonTypeInfo<Dictionary<string, ProactiveAccountState>>)base.Options.GetTypeInfo(typeof(Dictionary<string, ProactiveAccountState>)));

	public JsonTypeInfo<Dictionary<string, DateTimeOffset>> DictionaryStringDateTimeOffset => _DictionaryStringDateTimeOffset ?? (_DictionaryStringDateTimeOffset = (JsonTypeInfo<Dictionary<string, DateTimeOffset>>)base.Options.GetTypeInfo(typeof(Dictionary<string, DateTimeOffset>)));

	public JsonTypeInfo<Dictionary<string, string>> DictionaryStringString => _DictionaryStringString ?? (_DictionaryStringString = (JsonTypeInfo<Dictionary<string, string>>)base.Options.GetTypeInfo(typeof(Dictionary<string, string>)));

	public JsonTypeInfo<List<ProactiveHistoryEntry>> ListProactiveHistoryEntry => _ListProactiveHistoryEntry ?? (_ListProactiveHistoryEntry = (JsonTypeInfo<List<ProactiveHistoryEntry>>)base.Options.GetTypeInfo(typeof(List<ProactiveHistoryEntry>)));

	public JsonTypeInfo<List<ProactiveSuggestionCard>> ListProactiveSuggestionCard => _ListProactiveSuggestionCard ?? (_ListProactiveSuggestionCard = (JsonTypeInfo<List<ProactiveSuggestionCard>>)base.Options.GetTypeInfo(typeof(List<ProactiveSuggestionCard>)));

	public JsonTypeInfo<List<QueuedProactiveTask>> ListQueuedProactiveTask => _ListQueuedProactiveTask ?? (_ListQueuedProactiveTask = (JsonTypeInfo<List<QueuedProactiveTask>>)base.Options.GetTypeInfo(typeof(List<QueuedProactiveTask>)));

	public JsonTypeInfo<List<DateTime>> ListDateTime => _ListDateTime ?? (_ListDateTime = (JsonTypeInfo<List<DateTime>>)base.Options.GetTypeInfo(typeof(List<DateTime>)));

	public JsonTypeInfo<List<DateTimeOffset>> ListDateTimeOffset => _ListDateTimeOffset ?? (_ListDateTimeOffset = (JsonTypeInfo<List<DateTimeOffset>>)base.Options.GetTypeInfo(typeof(List<DateTimeOffset>)));

	public JsonTypeInfo<List<string>> ListString => _ListString ?? (_ListString = (JsonTypeInfo<List<string>>)base.Options.GetTypeInfo(typeof(List<string>)));

	public JsonTypeInfo<DateTime> DateTime => _DateTime ?? (_DateTime = (JsonTypeInfo<DateTime>)base.Options.GetTypeInfo(typeof(DateTime)));

	public JsonTypeInfo<DateTimeOffset> DateTimeOffset => _DateTimeOffset ?? (_DateTimeOffset = (JsonTypeInfo<DateTimeOffset>)base.Options.GetTypeInfo(typeof(DateTimeOffset)));

	public JsonTypeInfo<DateTimeOffset?> NullableDateTimeOffset => _NullableDateTimeOffset ?? (_NullableDateTimeOffset = (JsonTypeInfo<DateTimeOffset?>)base.Options.GetTypeInfo(typeof(DateTimeOffset?)));

	public JsonTypeInfo<Guid> Guid => _Guid ?? (_Guid = (JsonTypeInfo<Guid>)base.Options.GetTypeInfo(typeof(Guid)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<int?> NullableInt32 => _NullableInt32 ?? (_NullableInt32 = (JsonTypeInfo<int?>)base.Options.GetTypeInfo(typeof(int?)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static ProactiveJsonContext Default { get; } = new ProactiveJsonContext(new JsonSerializerOptions(s_defaultOptions));

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

	private JsonTypeInfo<MorningDismissalHistory> Create_MorningDismissalHistory(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<MorningDismissalHistory> jsonTypeInfo))
		{
			JsonObjectInfoValues<MorningDismissalHistory> objectInfo = new JsonObjectInfoValues<MorningDismissalHistory>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new MorningDismissalHistory((List<DateTime>)args[0], (DateTimeOffset?)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => MorningDismissalHistoryPropInit(options),
				ConstructorParameterMetadataInitializer = MorningDismissalHistoryCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(MorningDismissalHistory).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(List<DateTime>),
					typeof(DateTimeOffset?)
				}, null),
				SerializeHandler = MorningDismissalHistorySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] MorningDismissalHistoryPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<List<DateTime>> propertyInfo = new JsonPropertyInfoValues<List<DateTime>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(MorningDismissalHistory),
			Converter = null,
			Getter = (object obj) => ((MorningDismissalHistory)obj).DismissedDays,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DismissedDays",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(MorningDismissalHistory).GetProperty("DismissedDays", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<DateTime>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo2 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(MorningDismissalHistory),
			Converter = null,
			Getter = (object obj) => ((MorningDismissalHistory)obj).RestingUntil,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RestingUntil",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(MorningDismissalHistory).GetProperty("RestingUntil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void MorningDismissalHistorySerializeHandler(Utf8JsonWriter writer, MorningDismissalHistory? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		List<DateTime> dismissedDays = value.DismissedDays;
		if (dismissedDays != null)
		{
			writer.WritePropertyName(PropName_dismissed_days);
			ListDateTimeSerializeHandler(writer, dismissedDays);
		}
		DateTimeOffset? restingUntil = value.RestingUntil;
		if (restingUntil.HasValue)
		{
			writer.WritePropertyName(PropName_resting_until);
			JsonSerializer.Serialize(writer, restingUntil, NullableDateTimeOffset);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] MorningDismissalHistoryCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "DismissedDays",
				ParameterType = typeof(List<DateTime>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "RestingUntil",
				ParameterType = typeof(DateTimeOffset?),
				Position = 1,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<PendingSuggestionJob> Create_PendingSuggestionJob(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PendingSuggestionJob> jsonTypeInfo))
		{
			JsonObjectInfoValues<PendingSuggestionJob> objectInfo = new JsonObjectInfoValues<PendingSuggestionJob>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new PendingSuggestionJob((string)args[0], (DateTimeOffset)args[1], (ProactiveCheckReason)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => PendingSuggestionJobPropInit(options),
				ConstructorParameterMetadataInitializer = PendingSuggestionJobCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(PendingSuggestionJob).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(string),
					typeof(DateTimeOffset),
					typeof(ProactiveCheckReason)
				}, null),
				SerializeHandler = PendingSuggestionJobSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PendingSuggestionJobPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PendingSuggestionJob),
			Converter = null,
			Getter = (object obj) => ((PendingSuggestionJob)obj).JobId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "JobId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PendingSuggestionJob).GetProperty("JobId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo2 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PendingSuggestionJob),
			Converter = null,
			Getter = (object obj) => ((PendingSuggestionJob)obj).StartedAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StartedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PendingSuggestionJob).GetProperty("StartedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<ProactiveCheckReason> propertyInfo3 = new JsonPropertyInfoValues<ProactiveCheckReason>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PendingSuggestionJob),
			Converter = null,
			Getter = (object obj) => ((PendingSuggestionJob)obj).CheckReason,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CheckReason",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PendingSuggestionJob).GetProperty("CheckReason", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveCheckReason), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void PendingSuggestionJobSerializeHandler(Utf8JsonWriter writer, PendingSuggestionJob? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string jobId = value.JobId;
		if (jobId != null)
		{
			writer.WriteString(PropName_job_id, jobId);
		}
		writer.WriteString(PropName_started_at, value.StartedAt);
		writer.WritePropertyName(PropName_check_reason);
		JsonSerializer.Serialize(writer, value.CheckReason, ProactiveCheckReason);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] PendingSuggestionJobCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "JobId",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "StartedAt",
				ParameterType = typeof(DateTimeOffset),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CheckReason",
				ParameterType = typeof(ProactiveCheckReason),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ProactiveAccountState> Create_ProactiveAccountState(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveAccountState> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveAccountState> objectInfo = new JsonObjectInfoValues<ProactiveAccountState>
			{
				ObjectCreator = () => new ProactiveAccountState(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveAccountStatePropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveAccountState).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ProactiveAccountStateSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveAccountStatePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<List<ProactiveSuggestionCard>> propertyInfo = new JsonPropertyInfoValues<List<ProactiveSuggestionCard>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).Cards,
			Setter = delegate(object obj, List<ProactiveSuggestionCard>? value)
			{
				((ProactiveAccountState)obj).Cards = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Cards",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("Cards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<ProactiveSuggestionCard>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<List<QueuedProactiveTask>> propertyInfo2 = new JsonPropertyInfoValues<List<QueuedProactiveTask>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).Queue,
			Setter = delegate(object obj, List<QueuedProactiveTask>? value)
			{
				((ProactiveAccountState)obj).Queue = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Queue",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("Queue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<QueuedProactiveTask>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<PendingSuggestionJob> propertyInfo3 = new JsonPropertyInfoValues<PendingSuggestionJob>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).PendingJob,
			Setter = delegate(object obj, PendingSuggestionJob? value)
			{
				((ProactiveAccountState)obj).PendingJob = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PendingJob",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("PendingJob", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PendingSuggestionJob), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<ProactiveSlotDayState> propertyInfo4 = new JsonPropertyInfoValues<ProactiveSlotDayState>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).Slots,
			Setter = delegate(object obj, ProactiveSlotDayState? value)
			{
				((ProactiveAccountState)obj).Slots = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Slots",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("Slots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveSlotDayState), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo5 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).IgnoredSuggestionsBreakUntil,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveAccountState)obj).IgnoredSuggestionsBreakUntil = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IgnoredSuggestionsBreakUntil",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("IgnoredSuggestionsBreakUntil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo6 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).SuggestionsLastFetchedAt,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveAccountState)obj).SuggestionsLastFetchedAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SuggestionsLastFetchedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("SuggestionsLastFetchedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo7 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveAccountState),
			Converter = null,
			Getter = (object obj) => ((ProactiveAccountState)obj).SuggestionsRowLastSeenAt,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveAccountState)obj).SuggestionsRowLastSeenAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SuggestionsRowLastSeenAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveAccountState).GetProperty("SuggestionsRowLastSeenAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void ProactiveAccountStateSerializeHandler(Utf8JsonWriter writer, ProactiveAccountState? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		List<ProactiveSuggestionCard> cards = value.Cards;
		if (cards != null)
		{
			writer.WritePropertyName(PropName_cards);
			ListProactiveSuggestionCardSerializeHandler(writer, cards);
		}
		List<QueuedProactiveTask> queue = value.Queue;
		if (queue != null)
		{
			writer.WritePropertyName(PropName_queue);
			ListQueuedProactiveTaskSerializeHandler(writer, queue);
		}
		PendingSuggestionJob pendingJob = value.PendingJob;
		if ((object)pendingJob != null)
		{
			writer.WritePropertyName(PropName_pending_job);
			PendingSuggestionJobSerializeHandler(writer, pendingJob);
		}
		ProactiveSlotDayState slots = value.Slots;
		if ((object)slots != null)
		{
			writer.WritePropertyName(PropName_slots);
			ProactiveSlotDayStateSerializeHandler(writer, slots);
		}
		DateTimeOffset? ignoredSuggestionsBreakUntil = value.IgnoredSuggestionsBreakUntil;
		if (ignoredSuggestionsBreakUntil.HasValue)
		{
			writer.WritePropertyName(PropName_ignored_suggestions_break_until);
			JsonSerializer.Serialize(writer, ignoredSuggestionsBreakUntil, NullableDateTimeOffset);
		}
		DateTimeOffset? suggestionsLastFetchedAt = value.SuggestionsLastFetchedAt;
		if (suggestionsLastFetchedAt.HasValue)
		{
			writer.WritePropertyName(PropName_suggestions_last_fetched_at);
			JsonSerializer.Serialize(writer, suggestionsLastFetchedAt, NullableDateTimeOffset);
		}
		DateTimeOffset? suggestionsRowLastSeenAt = value.SuggestionsRowLastSeenAt;
		if (suggestionsRowLastSeenAt.HasValue)
		{
			writer.WritePropertyName(PropName_suggestions_row_last_seen_at);
			JsonSerializer.Serialize(writer, suggestionsRowLastSeenAt, NullableDateTimeOffset);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ProactiveCheckReason> Create_ProactiveCheckReason(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveCheckReason> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(ProactiveCheckReason), new JsonStringEnumConverter<ProactiveCheckReason>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ProactiveCheckReason>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ProactiveHistoryEntry> Create_ProactiveHistoryEntry(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveHistoryEntry> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveHistoryEntry> objectInfo = new JsonObjectInfoValues<ProactiveHistoryEntry>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ProactiveHistoryEntry((string)args[0], (string)args[1], (ProactiveResolution)args[2], (DateTimeOffset)args[3], (string)args[4], (bool?)args[5]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveHistoryEntryPropInit(options),
				ConstructorParameterMetadataInitializer = ProactiveHistoryEntryCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[6]
				{
					typeof(string),
					typeof(string),
					typeof(ProactiveResolution),
					typeof(DateTimeOffset),
					typeof(string),
					typeof(bool?)
				}, null),
				SerializeHandler = ProactiveHistoryEntrySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveHistoryEntryPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).Title,
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
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).Description,
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
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<ProactiveResolution> propertyInfo3 = new JsonPropertyInfoValues<ProactiveResolution>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).Resolution,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Resolution",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("Resolution", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveResolution), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo4 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).DecidedAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DecidedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("DecidedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).DenyReason,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DenyReason",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("DenyReason", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<bool?> propertyInfo6 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).WasViewed,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WasViewed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("WasViewed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveHistoryEntry),
			Converter = null,
			Getter = (object obj) => ((ProactiveHistoryEntry)obj).WireStatus,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WireStatus",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveHistoryEntry).GetProperty("WireStatus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].IsGetNullable = false;
		return array;
	}

	private void ProactiveHistoryEntrySerializeHandler(Utf8JsonWriter writer, ProactiveHistoryEntry? value)
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
		writer.WritePropertyName(PropName_resolution);
		JsonSerializer.Serialize(writer, value.Resolution, ProactiveResolution);
		writer.WriteString(PropName_decided_at, value.DecidedAt);
		string denyReason = value.DenyReason;
		if (denyReason != null)
		{
			writer.WriteString(PropName_deny_reason, denyReason);
		}
		bool? wasViewed = value.WasViewed;
		if (wasViewed.HasValue)
		{
			writer.WritePropertyName(PropName_was_viewed);
			JsonSerializer.Serialize(writer, wasViewed, NullableBoolean);
		}
		string wireStatus = value.WireStatus;
		if (wireStatus != null)
		{
			writer.WriteString(PropName_wire_status, wireStatus);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ProactiveHistoryEntryCtorParamInit()
	{
		return new JsonParameterInfoValues[6]
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
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Resolution",
				ParameterType = typeof(ProactiveResolution),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DecidedAt",
				ParameterType = typeof(DateTimeOffset),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "DenyReason",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "WasViewed",
				ParameterType = typeof(bool?),
				Position = 5,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ProactiveNewClicky> Create_ProactiveNewClicky(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveNewClicky> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveNewClicky> objectInfo = new JsonObjectInfoValues<ProactiveNewClicky>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ProactiveNewClicky((string)args[0], (string)args[1], (string)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveNewClickyPropInit(options),
				ConstructorParameterMetadataInitializer = ProactiveNewClickyCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveNewClicky).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = ProactiveNewClickySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveNewClickyPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveNewClicky),
			Converter = null,
			Getter = (object obj) => ((ProactiveNewClicky)obj).Name,
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
			AttributeProviderFactory = () => typeof(ProactiveNewClicky).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveNewClicky),
			Converter = null,
			Getter = (object obj) => ((ProactiveNewClicky)obj).Role,
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
			AttributeProviderFactory = () => typeof(ProactiveNewClicky).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveNewClicky),
			Converter = null,
			Getter = (object obj) => ((ProactiveNewClicky)obj).Description,
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
			AttributeProviderFactory = () => typeof(ProactiveNewClicky).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		return array;
	}

	private void ProactiveNewClickySerializeHandler(Utf8JsonWriter writer, ProactiveNewClicky? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
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
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ProactiveNewClickyCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "Name",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Role",
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
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ProactiveResolution> Create_ProactiveResolution(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveResolution> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(ProactiveResolution), new JsonStringEnumConverter<ProactiveResolution>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ProactiveResolution>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ProactiveRoutine> Create_ProactiveRoutine(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveRoutine> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveRoutine> objectInfo = new JsonObjectInfoValues<ProactiveRoutine>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ProactiveRoutine((int)args[0], (string)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveRoutinePropInit(options),
				ConstructorParameterMetadataInitializer = ProactiveRoutineCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveRoutine).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(int),
					typeof(string)
				}, null),
				SerializeHandler = ProactiveRoutineSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveRoutinePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveRoutine),
			Converter = null,
			Getter = (object obj) => ((ProactiveRoutine)obj).EveryMinutes,
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
			AttributeProviderFactory = () => typeof(ProactiveRoutine).GetProperty("EveryMinutes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveRoutine),
			Converter = null,
			Getter = (object obj) => ((ProactiveRoutine)obj).Title,
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
			AttributeProviderFactory = () => typeof(ProactiveRoutine).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveRoutine),
			Converter = null,
			Getter = (object obj) => ((ProactiveRoutine)obj).ClampedIntervalMinutes,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClampedIntervalMinutes",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveRoutine).GetProperty("ClampedIntervalMinutes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveRoutine),
			Converter = null,
			Getter = (object obj) => ((ProactiveRoutine)obj).CadenceLabel,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CadenceLabel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveRoutine).GetProperty("CadenceLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveRoutine),
			Converter = null,
			Getter = (object obj) => ((ProactiveRoutine)obj).CadenceSpokenLabel,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CadenceSpokenLabel",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveRoutine).GetProperty("CadenceSpokenLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		return array;
	}

	private void ProactiveRoutineSerializeHandler(Utf8JsonWriter writer, ProactiveRoutine? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_every_minutes, value.EveryMinutes);
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_title, title);
		}
		writer.WriteNumber(PropName_clamped_interval_minutes, value.ClampedIntervalMinutes);
		string cadenceLabel = value.CadenceLabel;
		if (cadenceLabel != null)
		{
			writer.WriteString(PropName_cadence_label, cadenceLabel);
		}
		string cadenceSpokenLabel = value.CadenceSpokenLabel;
		if (cadenceSpokenLabel != null)
		{
			writer.WriteString(PropName_cadence_spoken_label, cadenceSpokenLabel);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ProactiveRoutineCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "EveryMinutes",
				ParameterType = typeof(int),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ProactiveSlotDayState> Create_ProactiveSlotDayState(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveSlotDayState> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveSlotDayState> objectInfo = new JsonObjectInfoValues<ProactiveSlotDayState>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ProactiveSlotDayState((DateTimeOffset?)args[0], (DateTimeOffset?)args[1], (int?)args[2]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveSlotDayStatePropInit(options),
				ConstructorParameterMetadataInitializer = ProactiveSlotDayStateCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveSlotDayState).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(DateTimeOffset?),
					typeof(DateTimeOffset?),
					typeof(int?)
				}, null),
				SerializeHandler = ProactiveSlotDayStateSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveSlotDayStatePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSlotDayState),
			Converter = null,
			Getter = (object obj) => ((ProactiveSlotDayState)obj).MorningFiredAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningFiredAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSlotDayState).GetProperty("MorningFiredAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo2 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSlotDayState),
			Converter = null,
			Getter = (object obj) => ((ProactiveSlotDayState)obj).RetryAfter,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RetryAfter",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSlotDayState).GetProperty("RetryAfter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<int?> propertyInfo3 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSlotDayState),
			Converter = null,
			Getter = (object obj) => ((ProactiveSlotDayState)obj).ConsecutiveFailedAttempts,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ConsecutiveFailedAttempts",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSlotDayState).GetProperty("ConsecutiveFailedAttempts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void ProactiveSlotDayStateSerializeHandler(Utf8JsonWriter writer, ProactiveSlotDayState? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		DateTimeOffset? morningFiredAt = value.MorningFiredAt;
		if (morningFiredAt.HasValue)
		{
			writer.WritePropertyName(PropName_morning_fired_at);
			JsonSerializer.Serialize(writer, morningFiredAt, NullableDateTimeOffset);
		}
		DateTimeOffset? retryAfter = value.RetryAfter;
		if (retryAfter.HasValue)
		{
			writer.WritePropertyName(PropName_retry_after);
			JsonSerializer.Serialize(writer, retryAfter, NullableDateTimeOffset);
		}
		int? consecutiveFailedAttempts = value.ConsecutiveFailedAttempts;
		if (consecutiveFailedAttempts.HasValue)
		{
			writer.WritePropertyName(PropName_consecutive_failed_attempts);
			JsonSerializer.Serialize(writer, consecutiveFailedAttempts, NullableInt32);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ProactiveSlotDayStateCtorParamInit()
	{
		return new JsonParameterInfoValues[3]
		{
			new JsonParameterInfoValues
			{
				Name = "MorningFiredAt",
				ParameterType = typeof(DateTimeOffset?),
				Position = 0,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "RetryAfter",
				ParameterType = typeof(DateTimeOffset?),
				Position = 1,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ConsecutiveFailedAttempts",
				ParameterType = typeof(int?),
				Position = 2,
				HasDefaultValue = true,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<ProactiveStoreData> Create_ProactiveStoreData(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveStoreData> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveStoreData> objectInfo = new JsonObjectInfoValues<ProactiveStoreData>
			{
				ObjectCreator = () => new ProactiveStoreData(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveStoreDataPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveStoreData).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ProactiveStoreDataSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveStoreDataPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[11];
		JsonPropertyInfoValues<Dictionary<string, ProactiveAccountState>> propertyInfo = new JsonPropertyInfoValues<Dictionary<string, ProactiveAccountState>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).Accounts,
			Setter = delegate(object obj, Dictionary<string, ProactiveAccountState>? value)
			{
				((ProactiveStoreData)obj).Accounts = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Accounts",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("Accounts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, ProactiveAccountState>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<List<ProactiveHistoryEntry>> propertyInfo2 = new JsonPropertyInfoValues<List<ProactiveHistoryEntry>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).History,
			Setter = delegate(object obj, List<ProactiveHistoryEntry>? value)
			{
				((ProactiveStoreData)obj).History = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "History",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("History", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<ProactiveHistoryEntry>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).MorningGreetingLastDay,
			Setter = delegate(object obj, string? value)
			{
				((ProactiveStoreData)obj).MorningGreetingLastDay = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningGreetingLastDay",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("MorningGreetingLastDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo4 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).MorningGreetingLastPresentedAt,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveStoreData)obj).MorningGreetingLastPresentedAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningGreetingLastPresentedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("MorningGreetingLastPresentedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo5 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).MorningSettledAt,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveStoreData)obj).MorningSettledAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningSettledAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("MorningSettledAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<MorningDismissalHistory> propertyInfo6 = new JsonPropertyInfoValues<MorningDismissalHistory>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).MorningDismissals,
			Setter = delegate(object obj, MorningDismissalHistory? value)
			{
				((ProactiveStoreData)obj).MorningDismissals = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MorningDismissals",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("MorningDismissals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(MorningDismissalHistory), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo7 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).OnboardingFirstCompletedAt,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((ProactiveStoreData)obj).OnboardingFirstCompletedAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OnboardingFirstCompletedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("OnboardingFirstCompletedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<Dictionary<string, DateTimeOffset>> propertyInfo8 = new JsonPropertyInfoValues<Dictionary<string, DateTimeOffset>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).IntegrationSnoozedUntil,
			Setter = delegate(object obj, Dictionary<string, DateTimeOffset>? value)
			{
				((ProactiveStoreData)obj).IntegrationSnoozedUntil = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntegrationSnoozedUntil",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("IntegrationSnoozedUntil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, DateTimeOffset>), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		array[7].IsGetNullable = false;
		array[7].IsSetNullable = false;
		JsonPropertyInfoValues<List<string>> propertyInfo9 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).IntegrationNeverAsk,
			Setter = delegate(object obj, List<string>? value)
			{
				((ProactiveStoreData)obj).IntegrationNeverAsk = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntegrationNeverAsk",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("IntegrationNeverAsk", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		array[8].IsGetNullable = false;
		array[8].IsSetNullable = false;
		JsonPropertyInfoValues<List<DateTimeOffset>> propertyInfo10 = new JsonPropertyInfoValues<List<DateTimeOffset>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).IntegrationPromptTimestamps,
			Setter = delegate(object obj, List<DateTimeOffset>? value)
			{
				((ProactiveStoreData)obj).IntegrationPromptTimestamps = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntegrationPromptTimestamps",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("IntegrationPromptTimestamps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<DateTimeOffset>), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		array[9].IsGetNullable = false;
		array[9].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo11 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveStoreData),
			Converter = null,
			Getter = (object obj) => ((ProactiveStoreData)obj).SpotifyVoiceTipShown,
			Setter = delegate(object obj, bool value)
			{
				((ProactiveStoreData)obj).SpotifyVoiceTipShown = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SpotifyVoiceTipShown",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveStoreData).GetProperty("SpotifyVoiceTipShown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		return array;
	}

	private void ProactiveStoreDataSerializeHandler(Utf8JsonWriter writer, ProactiveStoreData? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		Dictionary<string, ProactiveAccountState> accounts = value.Accounts;
		if (accounts != null)
		{
			writer.WritePropertyName(PropName_accounts);
			DictionaryStringProactiveAccountStateSerializeHandler(writer, accounts);
		}
		List<ProactiveHistoryEntry> history = value.History;
		if (history != null)
		{
			writer.WritePropertyName(PropName_history);
			ListProactiveHistoryEntrySerializeHandler(writer, history);
		}
		string morningGreetingLastDay = value.MorningGreetingLastDay;
		if (morningGreetingLastDay != null)
		{
			writer.WriteString(PropName_morning_greeting_last_day, morningGreetingLastDay);
		}
		DateTimeOffset? morningGreetingLastPresentedAt = value.MorningGreetingLastPresentedAt;
		if (morningGreetingLastPresentedAt.HasValue)
		{
			writer.WritePropertyName(PropName_morning_greeting_last_presented_at);
			JsonSerializer.Serialize(writer, morningGreetingLastPresentedAt, NullableDateTimeOffset);
		}
		DateTimeOffset? morningSettledAt = value.MorningSettledAt;
		if (morningSettledAt.HasValue)
		{
			writer.WritePropertyName(PropName_morning_settled_at);
			JsonSerializer.Serialize(writer, morningSettledAt, NullableDateTimeOffset);
		}
		MorningDismissalHistory morningDismissals = value.MorningDismissals;
		if ((object)morningDismissals != null)
		{
			writer.WritePropertyName(PropName_morning_dismissals);
			MorningDismissalHistorySerializeHandler(writer, morningDismissals);
		}
		DateTimeOffset? onboardingFirstCompletedAt = value.OnboardingFirstCompletedAt;
		if (onboardingFirstCompletedAt.HasValue)
		{
			writer.WritePropertyName(PropName_onboarding_first_completed_at);
			JsonSerializer.Serialize(writer, onboardingFirstCompletedAt, NullableDateTimeOffset);
		}
		Dictionary<string, DateTimeOffset> integrationSnoozedUntil = value.IntegrationSnoozedUntil;
		if (integrationSnoozedUntil != null)
		{
			writer.WritePropertyName(PropName_integration_snoozed_until);
			DictionaryStringDateTimeOffsetSerializeHandler(writer, integrationSnoozedUntil);
		}
		List<string> integrationNeverAsk = value.IntegrationNeverAsk;
		if (integrationNeverAsk != null)
		{
			writer.WritePropertyName(PropName_integration_never_ask);
			ListStringSerializeHandler(writer, integrationNeverAsk);
		}
		List<DateTimeOffset> integrationPromptTimestamps = value.IntegrationPromptTimestamps;
		if (integrationPromptTimestamps != null)
		{
			writer.WritePropertyName(PropName_integration_prompt_timestamps);
			ListDateTimeOffsetSerializeHandler(writer, integrationPromptTimestamps);
		}
		writer.WriteBoolean(PropName_spotify_voice_tip_shown, value.SpotifyVoiceTipShown);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ProactiveSuggestionCard> Create_ProactiveSuggestionCard(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProactiveSuggestionCard> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProactiveSuggestionCard> objectInfo = new JsonObjectInfoValues<ProactiveSuggestionCard>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ProactiveSuggestionCard
				{
					Id = (Guid)args[0],
					Title = (string)args[1],
					Description = (string)args[2],
					AgentPrompt = (string)args[3],
					CheckReason = (ProactiveCheckReason)args[4],
					ServerSuggestionId = (int?)args[5],
					ClickySlug = (string)args[6],
					NewClicky = (ProactiveNewClicky)args[7],
					Routine = (ProactiveRoutine)args[8],
					IntegrationName = (string)args[9],
					CreatedAt = (DateTimeOffset)args[10],
					ViewedAt = (DateTimeOffset?)args[11]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProactiveSuggestionCardPropInit(options),
				ConstructorParameterMetadataInitializer = ProactiveSuggestionCardCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ProactiveSuggestionCardSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProactiveSuggestionCardPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[13];
		JsonPropertyInfoValues<Guid> propertyInfo = new JsonPropertyInfoValues<Guid>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).Id,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Guid), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).Title,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsRequired = true;
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).Description,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsRequired = true;
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).AgentPrompt,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("AgentPrompt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsRequired = true;
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<ProactiveCheckReason> propertyInfo5 = new JsonPropertyInfoValues<ProactiveCheckReason>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).CheckReason,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CheckReason",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("CheckReason", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveCheckReason), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<int?> propertyInfo6 = new JsonPropertyInfoValues<int?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).ServerSuggestionId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ServerSuggestionId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("ServerSuggestionId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int?), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).ClickySlug,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("ClickySlug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<ProactiveNewClicky> propertyInfo8 = new JsonPropertyInfoValues<ProactiveNewClicky>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).NewClicky,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NewClicky",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("NewClicky", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveNewClicky), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<ProactiveRoutine> propertyInfo9 = new JsonPropertyInfoValues<ProactiveRoutine>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).Routine,
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
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("Routine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveRoutine), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<string> propertyInfo10 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).IntegrationName,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IntegrationName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("IntegrationName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo11 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).CreatedAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CreatedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo12 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).ViewedAt,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ViewedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("ViewedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo13 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProactiveSuggestionCard),
			Converter = null,
			Getter = (object obj) => ((ProactiveSuggestionCard)obj).ExpiresAt,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ExpiresAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProactiveSuggestionCard).GetProperty("ExpiresAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		return array;
	}

	private void ProactiveSuggestionCardSerializeHandler(Utf8JsonWriter writer, ProactiveSuggestionCard? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_id, value.Id);
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
		writer.WritePropertyName(PropName_check_reason);
		JsonSerializer.Serialize(writer, value.CheckReason, ProactiveCheckReason);
		int? serverSuggestionId = value.ServerSuggestionId;
		if (serverSuggestionId.HasValue)
		{
			writer.WritePropertyName(PropName_server_suggestion_id);
			JsonSerializer.Serialize(writer, serverSuggestionId, NullableInt32);
		}
		string clickySlug = value.ClickySlug;
		if (clickySlug != null)
		{
			writer.WriteString(PropName_clicky_slug, clickySlug);
		}
		ProactiveNewClicky newClicky = value.NewClicky;
		if ((object)newClicky != null)
		{
			writer.WritePropertyName(PropName_new_clicky);
			ProactiveNewClickySerializeHandler(writer, newClicky);
		}
		ProactiveRoutine routine = value.Routine;
		if ((object)routine != null)
		{
			writer.WritePropertyName(PropName_routine);
			ProactiveRoutineSerializeHandler(writer, routine);
		}
		string integrationName = value.IntegrationName;
		if (integrationName != null)
		{
			writer.WriteString(PropName_integration_name, integrationName);
		}
		writer.WriteString(PropName_created_at, value.CreatedAt);
		DateTimeOffset? viewedAt = value.ViewedAt;
		if (viewedAt.HasValue)
		{
			writer.WritePropertyName(PropName_viewed_at);
			JsonSerializer.Serialize(writer, viewedAt, NullableDateTimeOffset);
		}
		DateTimeOffset? expiresAt = value.ExpiresAt;
		if (expiresAt.HasValue)
		{
			writer.WritePropertyName(PropName_expires_at);
			JsonSerializer.Serialize(writer, expiresAt, NullableDateTimeOffset);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ProactiveSuggestionCardCtorParamInit()
	{
		return new JsonParameterInfoValues[12]
		{
			new JsonParameterInfoValues
			{
				Name = "Id",
				ParameterType = typeof(Guid),
				Position = 0,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 1,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Description",
				ParameterType = typeof(string),
				Position = 2,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "AgentPrompt",
				ParameterType = typeof(string),
				Position = 3,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CheckReason",
				ParameterType = typeof(ProactiveCheckReason),
				Position = 4,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ServerSuggestionId",
				ParameterType = typeof(int?),
				Position = 5,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClickySlug",
				ParameterType = typeof(string),
				Position = 6,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "NewClicky",
				ParameterType = typeof(ProactiveNewClicky),
				Position = 7,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Routine",
				ParameterType = typeof(ProactiveRoutine),
				Position = 8,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "IntegrationName",
				ParameterType = typeof(string),
				Position = 9,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CreatedAt",
				ParameterType = typeof(DateTimeOffset),
				Position = 10,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ViewedAt",
				ParameterType = typeof(DateTimeOffset?),
				Position = 11,
				IsNullable = true,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<QueuedProactiveTask> Create_QueuedProactiveTask(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<QueuedProactiveTask> jsonTypeInfo))
		{
			JsonObjectInfoValues<QueuedProactiveTask> objectInfo = new JsonObjectInfoValues<QueuedProactiveTask>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new QueuedProactiveTask((ProactiveSuggestionCard)args[0], (string)args[1], (string)args[2], (bool)args[3], (bool)args[4])
				{
					IsPaused = (bool)args[5],
					IsLaunching = (bool)args[6]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => QueuedProactiveTaskPropInit(options),
				ConstructorParameterMetadataInitializer = QueuedProactiveTaskCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(QueuedProactiveTask).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[5]
				{
					typeof(ProactiveSuggestionCard),
					typeof(string),
					typeof(string),
					typeof(bool),
					typeof(bool)
				}, null),
				SerializeHandler = QueuedProactiveTaskSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] QueuedProactiveTaskPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<ProactiveSuggestionCard> propertyInfo = new JsonPropertyInfoValues<ProactiveSuggestionCard>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).Card,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Card",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("Card", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ProactiveSuggestionCard), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).ClickySlug,
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
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("ClickySlug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).AdjustmentText,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AdjustmentText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("AdjustmentText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<bool> propertyInfo4 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).WasFounded,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WasFounded",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("WasFounded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).WasRestored,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WasRestored",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("WasRestored", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<Guid> propertyInfo6 = new JsonPropertyInfoValues<Guid>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).Id,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Guid), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).IsPaused,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsPaused",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("IsPaused", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(QueuedProactiveTask),
			Converter = null,
			Getter = (object obj) => ((QueuedProactiveTask)obj).IsLaunching,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsLaunching",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(QueuedProactiveTask).GetProperty("IsLaunching", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void QueuedProactiveTaskSerializeHandler(Utf8JsonWriter writer, QueuedProactiveTask? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		ProactiveSuggestionCard card = value.Card;
		if ((object)card != null)
		{
			writer.WritePropertyName(PropName_card);
			ProactiveSuggestionCardSerializeHandler(writer, card);
		}
		string clickySlug = value.ClickySlug;
		if (clickySlug != null)
		{
			writer.WriteString(PropName_clicky_slug, clickySlug);
		}
		string adjustmentText = value.AdjustmentText;
		if (adjustmentText != null)
		{
			writer.WriteString(PropName_adjustment_text, adjustmentText);
		}
		writer.WriteBoolean(PropName_was_founded, value.WasFounded);
		writer.WriteBoolean(PropName_was_restored, value.WasRestored);
		writer.WriteString(PropName_id, value.Id);
		writer.WriteBoolean(PropName_is_paused, value.IsPaused);
		writer.WriteBoolean(PropName_is_launching, value.IsLaunching);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] QueuedProactiveTaskCtorParamInit()
	{
		return new JsonParameterInfoValues[7]
		{
			new JsonParameterInfoValues
			{
				Name = "Card",
				ParameterType = typeof(ProactiveSuggestionCard),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ClickySlug",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "AdjustmentText",
				ParameterType = typeof(string),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "WasFounded",
				ParameterType = typeof(bool),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "WasRestored",
				ParameterType = typeof(bool),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "IsPaused",
				ParameterType = typeof(bool),
				Position = 5,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsLaunching",
				ParameterType = typeof(bool),
				Position = 6,
				IsNullable = false,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<Dictionary<string, ProactiveAccountState>> Create_DictionaryStringProactiveAccountState(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Dictionary<string, ProactiveAccountState>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<Dictionary<string, ProactiveAccountState>> collectionInfo = new JsonCollectionInfoValues<Dictionary<string, ProactiveAccountState>>
			{
				ObjectCreator = () => new Dictionary<string, ProactiveAccountState>(),
				SerializeHandler = DictionaryStringProactiveAccountStateSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, ProactiveAccountState>, string, ProactiveAccountState>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void DictionaryStringProactiveAccountStateSerializeHandler(Utf8JsonWriter writer, Dictionary<string, ProactiveAccountState>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, ProactiveAccountState> item in value)
		{
			writer.WritePropertyName(item.Key);
			ProactiveAccountStateSerializeHandler(writer, item.Value);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<Dictionary<string, DateTimeOffset>> Create_DictionaryStringDateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Dictionary<string, DateTimeOffset>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<Dictionary<string, DateTimeOffset>> collectionInfo = new JsonCollectionInfoValues<Dictionary<string, DateTimeOffset>>
			{
				ObjectCreator = () => new Dictionary<string, DateTimeOffset>(),
				SerializeHandler = DictionaryStringDateTimeOffsetSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, DateTimeOffset>, string, DateTimeOffset>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void DictionaryStringDateTimeOffsetSerializeHandler(Utf8JsonWriter writer, Dictionary<string, DateTimeOffset>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, DateTimeOffset> item in value)
		{
			writer.WriteString(item.Key, item.Value);
		}
		writer.WriteEndObject();
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

	private JsonTypeInfo<List<ProactiveHistoryEntry>> Create_ListProactiveHistoryEntry(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<ProactiveHistoryEntry>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<ProactiveHistoryEntry>> collectionInfo = new JsonCollectionInfoValues<List<ProactiveHistoryEntry>>
			{
				ObjectCreator = () => new List<ProactiveHistoryEntry>(),
				SerializeHandler = ListProactiveHistoryEntrySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<ProactiveHistoryEntry>, ProactiveHistoryEntry>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListProactiveHistoryEntrySerializeHandler(Utf8JsonWriter writer, List<ProactiveHistoryEntry>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			ProactiveHistoryEntrySerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<ProactiveSuggestionCard>> Create_ListProactiveSuggestionCard(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<ProactiveSuggestionCard>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<ProactiveSuggestionCard>> collectionInfo = new JsonCollectionInfoValues<List<ProactiveSuggestionCard>>
			{
				ObjectCreator = () => new List<ProactiveSuggestionCard>(),
				SerializeHandler = ListProactiveSuggestionCardSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<ProactiveSuggestionCard>, ProactiveSuggestionCard>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListProactiveSuggestionCardSerializeHandler(Utf8JsonWriter writer, List<ProactiveSuggestionCard>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			ProactiveSuggestionCardSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<QueuedProactiveTask>> Create_ListQueuedProactiveTask(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<QueuedProactiveTask>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<QueuedProactiveTask>> collectionInfo = new JsonCollectionInfoValues<List<QueuedProactiveTask>>
			{
				ObjectCreator = () => new List<QueuedProactiveTask>(),
				SerializeHandler = ListQueuedProactiveTaskSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<QueuedProactiveTask>, QueuedProactiveTask>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListQueuedProactiveTaskSerializeHandler(Utf8JsonWriter writer, List<QueuedProactiveTask>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			QueuedProactiveTaskSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<DateTime>> Create_ListDateTime(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<DateTime>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<DateTime>> collectionInfo = new JsonCollectionInfoValues<List<DateTime>>
			{
				ObjectCreator = () => new List<DateTime>(),
				SerializeHandler = ListDateTimeSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<DateTime>, DateTime>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListDateTimeSerializeHandler(Utf8JsonWriter writer, List<DateTime>? value)
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

	private JsonTypeInfo<List<DateTimeOffset>> Create_ListDateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<DateTimeOffset>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<DateTimeOffset>> collectionInfo = new JsonCollectionInfoValues<List<DateTimeOffset>>
			{
				ObjectCreator = () => new List<DateTimeOffset>(),
				SerializeHandler = ListDateTimeOffsetSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<DateTimeOffset>, DateTimeOffset>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListDateTimeOffsetSerializeHandler(Utf8JsonWriter writer, List<DateTimeOffset>? value)
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

	private JsonTypeInfo<DateTime> Create_DateTime(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTime> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTime>(options, JsonMetadataServices.DateTimeConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<DateTimeOffset> Create_DateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTimeOffset> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTimeOffset>(options, JsonMetadataServices.DateTimeOffsetConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<DateTimeOffset?> Create_NullableDateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTimeOffset?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<DateTimeOffset>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTimeOffset?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<Guid> Create_Guid(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Guid> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Guid>(options, JsonMetadataServices.GuidConverter);
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

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public ProactiveJsonContext()
		: base(null)
	{
	}

	public ProactiveJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(MorningDismissalHistory))
		{
			return Create_MorningDismissalHistory(options);
		}
		if (type == typeof(PendingSuggestionJob))
		{
			return Create_PendingSuggestionJob(options);
		}
		if (type == typeof(ProactiveAccountState))
		{
			return Create_ProactiveAccountState(options);
		}
		if (type == typeof(ProactiveCheckReason))
		{
			return Create_ProactiveCheckReason(options);
		}
		if (type == typeof(ProactiveHistoryEntry))
		{
			return Create_ProactiveHistoryEntry(options);
		}
		if (type == typeof(ProactiveNewClicky))
		{
			return Create_ProactiveNewClicky(options);
		}
		if (type == typeof(ProactiveResolution))
		{
			return Create_ProactiveResolution(options);
		}
		if (type == typeof(ProactiveRoutine))
		{
			return Create_ProactiveRoutine(options);
		}
		if (type == typeof(ProactiveSlotDayState))
		{
			return Create_ProactiveSlotDayState(options);
		}
		if (type == typeof(ProactiveStoreData))
		{
			return Create_ProactiveStoreData(options);
		}
		if (type == typeof(ProactiveSuggestionCard))
		{
			return Create_ProactiveSuggestionCard(options);
		}
		if (type == typeof(QueuedProactiveTask))
		{
			return Create_QueuedProactiveTask(options);
		}
		if (type == typeof(Dictionary<string, ProactiveAccountState>))
		{
			return Create_DictionaryStringProactiveAccountState(options);
		}
		if (type == typeof(Dictionary<string, DateTimeOffset>))
		{
			return Create_DictionaryStringDateTimeOffset(options);
		}
		if (type == typeof(Dictionary<string, string>))
		{
			return Create_DictionaryStringString(options);
		}
		if (type == typeof(List<ProactiveHistoryEntry>))
		{
			return Create_ListProactiveHistoryEntry(options);
		}
		if (type == typeof(List<ProactiveSuggestionCard>))
		{
			return Create_ListProactiveSuggestionCard(options);
		}
		if (type == typeof(List<QueuedProactiveTask>))
		{
			return Create_ListQueuedProactiveTask(options);
		}
		if (type == typeof(List<DateTime>))
		{
			return Create_ListDateTime(options);
		}
		if (type == typeof(List<DateTimeOffset>))
		{
			return Create_ListDateTimeOffset(options);
		}
		if (type == typeof(List<string>))
		{
			return Create_ListString(options);
		}
		if (type == typeof(DateTime))
		{
			return Create_DateTime(options);
		}
		if (type == typeof(DateTimeOffset))
		{
			return Create_DateTimeOffset(options);
		}
		if (type == typeof(DateTimeOffset?))
		{
			return Create_NullableDateTimeOffset(options);
		}
		if (type == typeof(Guid))
		{
			return Create_Guid(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(int?))
		{
			return Create_NullableInt32(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
