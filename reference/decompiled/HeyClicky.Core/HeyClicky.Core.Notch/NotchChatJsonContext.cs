using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HeyClicky.Core.Notch;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CachedConversation))]
[JsonSerializable(typeof(List<ChatHandOff>))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
internal sealed class NotchChatJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<bool?>? _NullableBoolean;

	private JsonTypeInfo<CachedConversation>? _CachedConversation;

	private JsonTypeInfo<ChatHandOff>? _ChatHandOff;

	private JsonTypeInfo<ChatHandOffKind>? _ChatHandOffKind;

	private JsonTypeInfo<ChatHandOffOutcome>? _ChatHandOffOutcome;

	private JsonTypeInfo<ChatHandOffOutcome?>? _NullableChatHandOffOutcome;

	private JsonTypeInfo<ConversationMessage>? _ConversationMessage;

	private JsonTypeInfo<IReadOnlyList<ConversationMessage>>? _IReadOnlyListConversationMessage;

	private JsonTypeInfo<IReadOnlyList<string>>? _IReadOnlyListString;

	private JsonTypeInfo<List<ChatHandOff>>? _ListChatHandOff;

	private JsonTypeInfo<DateTimeOffset>? _DateTimeOffset;

	private JsonTypeInfo<JsonObject>? _JsonObject;

	private JsonTypeInfo<long>? _Int64;

	private JsonTypeInfo<long?>? _NullableInt64;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_Messages = JsonEncodedText.Encode("Messages");

	private static readonly JsonEncodedText PropName_HasEarlierMessages = JsonEncodedText.Encode("HasEarlierMessages");

	private static readonly JsonEncodedText PropName_Id = JsonEncodedText.Encode("Id");

	private static readonly JsonEncodedText PropName_Kind = JsonEncodedText.Encode("Kind");

	private static readonly JsonEncodedText PropName_JobId = JsonEncodedText.Encode("JobId");

	private static readonly JsonEncodedText PropName_ClickySlug = JsonEncodedText.Encode("ClickySlug");

	private static readonly JsonEncodedText PropName_Title = JsonEncodedText.Encode("Title");

	private static readonly JsonEncodedText PropName_CreatedAt = JsonEncodedText.Encode("CreatedAt");

	private static readonly JsonEncodedText PropName_IsFollowUp = JsonEncodedText.Encode("IsFollowUp");

	private static readonly JsonEncodedText PropName_IsSentFromWindow = JsonEncodedText.Encode("IsSentFromWindow");

	private static readonly JsonEncodedText PropName_Outcome = JsonEncodedText.Encode("Outcome");

	private static readonly JsonEncodedText PropName_ReplyText = JsonEncodedText.Encode("ReplyText");

	private static readonly JsonEncodedText PropName_LessonId = JsonEncodedText.Encode("LessonId");

	private static readonly JsonEncodedText PropName_DeliveredText = JsonEncodedText.Encode("DeliveredText");

	private static readonly JsonEncodedText PropName_DeliveredToApp = JsonEncodedText.Encode("DeliveredToApp");

	private static readonly JsonEncodedText PropName_WasCopied = JsonEncodedText.Encode("WasCopied");

	private static readonly JsonEncodedText PropName_FullReply = JsonEncodedText.Encode("FullReply");

	private static readonly JsonEncodedText PropName_ArtifactEntries = JsonEncodedText.Encode("ArtifactEntries");

	private static readonly JsonEncodedText PropName_Answer = JsonEncodedText.Encode("Answer");

	private static readonly JsonEncodedText PropName_ThreadId = JsonEncodedText.Encode("ThreadId");

	private static readonly JsonEncodedText PropName_Card = JsonEncodedText.Encode("Card");

	private static readonly JsonEncodedText PropName_IsRoutineRun = JsonEncodedText.Encode("IsRoutineRun");

	private static readonly JsonEncodedText PropName_TaskTitle = JsonEncodedText.Encode("TaskTitle");

	private static readonly JsonEncodedText PropName_FollowUps = JsonEncodedText.Encode("FollowUps");

	private static readonly JsonEncodedText PropName_SentText = JsonEncodedText.Encode("SentText");

	private static readonly JsonEncodedText PropName_SentFiles = JsonEncodedText.Encode("SentFiles");

	private static readonly JsonEncodedText PropName_SitesRead = JsonEncodedText.Encode("SitesRead");

	private static readonly JsonEncodedText PropName_ServerId = JsonEncodedText.Encode("ServerId");

	private static readonly JsonEncodedText PropName_Role = JsonEncodedText.Encode("Role");

	private static readonly JsonEncodedText PropName_Content = JsonEncodedText.Encode("Content");

	private static readonly JsonEncodedText PropName_Source = JsonEncodedText.Encode("Source");

	private static readonly JsonEncodedText PropName_AttachmentPaths = JsonEncodedText.Encode("AttachmentPaths");

	private static readonly JsonEncodedText PropName_BubbleSegments = JsonEncodedText.Encode("BubbleSegments");

	private static readonly JsonEncodedText PropName_ReplacedLocalId = JsonEncodedText.Encode("ReplacedLocalId");

	private static readonly JsonEncodedText PropName_SentToAgentName = JsonEncodedText.Encode("SentToAgentName");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = (JsonTypeInfo<bool?>)base.Options.GetTypeInfo(typeof(bool?)));

	public JsonTypeInfo<CachedConversation> CachedConversation => _CachedConversation ?? (_CachedConversation = (JsonTypeInfo<CachedConversation>)base.Options.GetTypeInfo(typeof(CachedConversation)));

	public JsonTypeInfo<ChatHandOff> ChatHandOff => _ChatHandOff ?? (_ChatHandOff = (JsonTypeInfo<ChatHandOff>)base.Options.GetTypeInfo(typeof(ChatHandOff)));

	public JsonTypeInfo<ChatHandOffKind> ChatHandOffKind => _ChatHandOffKind ?? (_ChatHandOffKind = (JsonTypeInfo<ChatHandOffKind>)base.Options.GetTypeInfo(typeof(ChatHandOffKind)));

	public JsonTypeInfo<ChatHandOffOutcome> ChatHandOffOutcome => _ChatHandOffOutcome ?? (_ChatHandOffOutcome = (JsonTypeInfo<ChatHandOffOutcome>)base.Options.GetTypeInfo(typeof(ChatHandOffOutcome)));

	public JsonTypeInfo<ChatHandOffOutcome?> NullableChatHandOffOutcome => _NullableChatHandOffOutcome ?? (_NullableChatHandOffOutcome = (JsonTypeInfo<ChatHandOffOutcome?>)base.Options.GetTypeInfo(typeof(ChatHandOffOutcome?)));

	public JsonTypeInfo<ConversationMessage> ConversationMessage => _ConversationMessage ?? (_ConversationMessage = (JsonTypeInfo<ConversationMessage>)base.Options.GetTypeInfo(typeof(ConversationMessage)));

	public JsonTypeInfo<IReadOnlyList<ConversationMessage>> IReadOnlyListConversationMessage => _IReadOnlyListConversationMessage ?? (_IReadOnlyListConversationMessage = (JsonTypeInfo<IReadOnlyList<ConversationMessage>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<ConversationMessage>)));

	public JsonTypeInfo<IReadOnlyList<string>> IReadOnlyListString => _IReadOnlyListString ?? (_IReadOnlyListString = (JsonTypeInfo<IReadOnlyList<string>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<string>)));

	public JsonTypeInfo<List<ChatHandOff>> ListChatHandOff => _ListChatHandOff ?? (_ListChatHandOff = (JsonTypeInfo<List<ChatHandOff>>)base.Options.GetTypeInfo(typeof(List<ChatHandOff>)));

	public JsonTypeInfo<DateTimeOffset> DateTimeOffset => _DateTimeOffset ?? (_DateTimeOffset = (JsonTypeInfo<DateTimeOffset>)base.Options.GetTypeInfo(typeof(DateTimeOffset)));

	public JsonTypeInfo<JsonObject> JsonObject => _JsonObject ?? (_JsonObject = (JsonTypeInfo<JsonObject>)base.Options.GetTypeInfo(typeof(JsonObject)));

	public JsonTypeInfo<long> Int64 => _Int64 ?? (_Int64 = (JsonTypeInfo<long>)base.Options.GetTypeInfo(typeof(long)));

	public JsonTypeInfo<long?> NullableInt64 => _NullableInt64 ?? (_NullableInt64 = (JsonTypeInfo<long?>)base.Options.GetTypeInfo(typeof(long?)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static NotchChatJsonContext Default { get; } = new NotchChatJsonContext(new JsonSerializerOptions(s_defaultOptions));

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

	private JsonTypeInfo<CachedConversation> Create_CachedConversation(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CachedConversation> jsonTypeInfo))
		{
			JsonObjectInfoValues<CachedConversation> objectInfo = new JsonObjectInfoValues<CachedConversation>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new CachedConversation((IReadOnlyList<ConversationMessage>)args[0], (bool)args[1]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => CachedConversationPropInit(options),
				ConstructorParameterMetadataInitializer = CachedConversationCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(CachedConversation).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(IReadOnlyList<ConversationMessage>),
					typeof(bool)
				}, null),
				SerializeHandler = CachedConversationSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] CachedConversationPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<IReadOnlyList<ConversationMessage>> propertyInfo = new JsonPropertyInfoValues<IReadOnlyList<ConversationMessage>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CachedConversation),
			Converter = null,
			Getter = (object obj) => ((CachedConversation)obj).Messages,
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
			AttributeProviderFactory = () => typeof(CachedConversation).GetProperty("Messages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<ConversationMessage>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CachedConversation),
			Converter = null,
			Getter = (object obj) => ((CachedConversation)obj).HasEarlierMessages,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasEarlierMessages",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CachedConversation).GetProperty("HasEarlierMessages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void CachedConversationSerializeHandler(Utf8JsonWriter writer, CachedConversation? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		IReadOnlyList<ConversationMessage> messages = value.Messages;
		if (messages != null)
		{
			writer.WritePropertyName(PropName_Messages);
			IReadOnlyListConversationMessageSerializeHandler(writer, messages);
		}
		writer.WriteBoolean(PropName_HasEarlierMessages, value.HasEarlierMessages);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] CachedConversationCtorParamInit()
	{
		return new JsonParameterInfoValues[2]
		{
			new JsonParameterInfoValues
			{
				Name = "Messages",
				ParameterType = typeof(IReadOnlyList<ConversationMessage>),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "HasEarlierMessages",
				ParameterType = typeof(bool),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<ChatHandOff> Create_ChatHandOff(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ChatHandOff> jsonTypeInfo))
		{
			JsonObjectInfoValues<ChatHandOff> objectInfo = new JsonObjectInfoValues<ChatHandOff>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ChatHandOff
				{
					Id = (string)args[0],
					Kind = (ChatHandOffKind)args[1],
					JobId = (string)args[2],
					ClickySlug = (string)args[3],
					Title = (string)args[4],
					CreatedAt = (DateTimeOffset)args[5],
					IsFollowUp = (bool)args[6],
					IsSentFromWindow = (bool)args[7],
					Outcome = (ChatHandOffOutcome?)args[8],
					ReplyText = (string)args[9],
					LessonId = (string)args[10],
					DeliveredText = (string)args[11],
					DeliveredToApp = (string)args[12],
					WasCopied = (bool?)args[13],
					FullReply = (string)args[14],
					ArtifactEntries = (IReadOnlyList<string>)args[15],
					Answer = (JsonObject)args[16],
					ThreadId = (string)args[17],
					Card = (JsonObject)args[18],
					IsRoutineRun = (bool?)args[19],
					TaskTitle = (string)args[20],
					FollowUps = (IReadOnlyList<string>)args[21],
					SentText = (string)args[22],
					SentFiles = (IReadOnlyList<string>)args[23],
					SitesRead = (IReadOnlyList<string>)args[24]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => ChatHandOffPropInit(options),
				ConstructorParameterMetadataInitializer = ChatHandOffCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ChatHandOff).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ChatHandOffSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ChatHandOffPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[26];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Id,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsRequired = true;
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<ChatHandOffKind> propertyInfo2 = new JsonPropertyInfoValues<ChatHandOffKind>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Kind,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Kind",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ChatHandOffKind), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsRequired = true;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).JobId,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("JobId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).ClickySlug,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("ClickySlug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Title,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Title", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsRequired = true;
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo6 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).CreatedAt,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsRequired = true;
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).IsFollowUp,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsFollowUp",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("IsFollowUp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).IsSentFromWindow,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsSentFromWindow",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("IsSentFromWindow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<ChatHandOffOutcome?> propertyInfo9 = new JsonPropertyInfoValues<ChatHandOffOutcome?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Outcome,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Outcome", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ChatHandOffOutcome?), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<string> propertyInfo10 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).ReplyText,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReplyText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("ReplyText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<string> propertyInfo11 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).LessonId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LessonId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("LessonId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).DeliveredText,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DeliveredText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("DeliveredText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<string> propertyInfo13 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).DeliveredToApp,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DeliveredToApp",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("DeliveredToApp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<bool?> propertyInfo14 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).WasCopied,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WasCopied",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("WasCopied", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<string> propertyInfo15 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).FullReply,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FullReply",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("FullReply", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo16 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).ArtifactEntries,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ArtifactEntries",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("ArtifactEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		JsonPropertyInfoValues<JsonObject> propertyInfo17 = new JsonPropertyInfoValues<JsonObject>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Answer,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Answer",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Answer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(JsonObject), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		JsonPropertyInfoValues<string> propertyInfo18 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).ThreadId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ThreadId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("ThreadId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		JsonPropertyInfoValues<JsonObject> propertyInfo19 = new JsonPropertyInfoValues<JsonObject>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).Card,
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
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("Card", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(JsonObject), Array.Empty<Type>(), null)
		};
		array[18] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo19);
		JsonPropertyInfoValues<bool?> propertyInfo20 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).IsRoutineRun,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsRoutineRun",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("IsRoutineRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[19] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo20);
		JsonPropertyInfoValues<string> propertyInfo21 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).TaskTitle,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TaskTitle",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("TaskTitle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[20] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo21);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo22 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).FollowUps,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FollowUps",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("FollowUps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[21] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo22);
		JsonPropertyInfoValues<string> propertyInfo23 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).SentText,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SentText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("SentText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[22] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo23);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo24 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).SentFiles,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SentFiles",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("SentFiles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[23] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo24);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo25 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = (object obj) => ((ChatHandOff)obj).SitesRead,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SitesRead",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("SitesRead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[24] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo25);
		JsonPropertyInfoValues<string> propertyInfo26 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ChatHandOff),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TargetKey",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ChatHandOff).GetProperty("TargetKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[25] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo26);
		array[25].IsGetNullable = false;
		return array;
	}

	private void ChatHandOffSerializeHandler(Utf8JsonWriter writer, ChatHandOff? value)
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
			writer.WriteString(PropName_Id, id);
		}
		writer.WritePropertyName(PropName_Kind);
		JsonSerializer.Serialize(writer, value.Kind, ChatHandOffKind);
		string jobId = value.JobId;
		if (jobId != null)
		{
			writer.WriteString(PropName_JobId, jobId);
		}
		string clickySlug = value.ClickySlug;
		if (clickySlug != null)
		{
			writer.WriteString(PropName_ClickySlug, clickySlug);
		}
		string title = value.Title;
		if (title != null)
		{
			writer.WriteString(PropName_Title, title);
		}
		writer.WriteString(PropName_CreatedAt, value.CreatedAt);
		writer.WriteBoolean(PropName_IsFollowUp, value.IsFollowUp);
		writer.WriteBoolean(PropName_IsSentFromWindow, value.IsSentFromWindow);
		ChatHandOffOutcome? outcome = value.Outcome;
		if (outcome.HasValue)
		{
			writer.WritePropertyName(PropName_Outcome);
			JsonSerializer.Serialize(writer, outcome, NullableChatHandOffOutcome);
		}
		string replyText = value.ReplyText;
		if (replyText != null)
		{
			writer.WriteString(PropName_ReplyText, replyText);
		}
		string lessonId = value.LessonId;
		if (lessonId != null)
		{
			writer.WriteString(PropName_LessonId, lessonId);
		}
		string deliveredText = value.DeliveredText;
		if (deliveredText != null)
		{
			writer.WriteString(PropName_DeliveredText, deliveredText);
		}
		string deliveredToApp = value.DeliveredToApp;
		if (deliveredToApp != null)
		{
			writer.WriteString(PropName_DeliveredToApp, deliveredToApp);
		}
		bool? wasCopied = value.WasCopied;
		if (wasCopied.HasValue)
		{
			writer.WritePropertyName(PropName_WasCopied);
			JsonSerializer.Serialize(writer, wasCopied, NullableBoolean);
		}
		string fullReply = value.FullReply;
		if (fullReply != null)
		{
			writer.WriteString(PropName_FullReply, fullReply);
		}
		IReadOnlyList<string> artifactEntries = value.ArtifactEntries;
		if (artifactEntries != null)
		{
			writer.WritePropertyName(PropName_ArtifactEntries);
			IReadOnlyListStringSerializeHandler(writer, artifactEntries);
		}
		JsonObject answer = value.Answer;
		if (answer != null)
		{
			writer.WritePropertyName(PropName_Answer);
			JsonSerializer.Serialize(writer, answer, JsonObject);
		}
		string threadId = value.ThreadId;
		if (threadId != null)
		{
			writer.WriteString(PropName_ThreadId, threadId);
		}
		JsonObject card = value.Card;
		if (card != null)
		{
			writer.WritePropertyName(PropName_Card);
			JsonSerializer.Serialize(writer, card, JsonObject);
		}
		bool? isRoutineRun = value.IsRoutineRun;
		if (isRoutineRun.HasValue)
		{
			writer.WritePropertyName(PropName_IsRoutineRun);
			JsonSerializer.Serialize(writer, isRoutineRun, NullableBoolean);
		}
		string taskTitle = value.TaskTitle;
		if (taskTitle != null)
		{
			writer.WriteString(PropName_TaskTitle, taskTitle);
		}
		IReadOnlyList<string> followUps = value.FollowUps;
		if (followUps != null)
		{
			writer.WritePropertyName(PropName_FollowUps);
			IReadOnlyListStringSerializeHandler(writer, followUps);
		}
		string sentText = value.SentText;
		if (sentText != null)
		{
			writer.WriteString(PropName_SentText, sentText);
		}
		IReadOnlyList<string> sentFiles = value.SentFiles;
		if (sentFiles != null)
		{
			writer.WritePropertyName(PropName_SentFiles);
			IReadOnlyListStringSerializeHandler(writer, sentFiles);
		}
		IReadOnlyList<string> sitesRead = value.SitesRead;
		if (sitesRead != null)
		{
			writer.WritePropertyName(PropName_SitesRead);
			IReadOnlyListStringSerializeHandler(writer, sitesRead);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ChatHandOffCtorParamInit()
	{
		return new JsonParameterInfoValues[25]
		{
			new JsonParameterInfoValues
			{
				Name = "Id",
				ParameterType = typeof(string),
				Position = 0,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Kind",
				ParameterType = typeof(ChatHandOffKind),
				Position = 1,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "JobId",
				ParameterType = typeof(string),
				Position = 2,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClickySlug",
				ParameterType = typeof(string),
				Position = 3,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Title",
				ParameterType = typeof(string),
				Position = 4,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CreatedAt",
				ParameterType = typeof(DateTimeOffset),
				Position = 5,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsFollowUp",
				ParameterType = typeof(bool),
				Position = 6,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsSentFromWindow",
				ParameterType = typeof(bool),
				Position = 7,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Outcome",
				ParameterType = typeof(ChatHandOffOutcome?),
				Position = 8,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ReplyText",
				ParameterType = typeof(string),
				Position = 9,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "LessonId",
				ParameterType = typeof(string),
				Position = 10,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "DeliveredText",
				ParameterType = typeof(string),
				Position = 11,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "DeliveredToApp",
				ParameterType = typeof(string),
				Position = 12,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "WasCopied",
				ParameterType = typeof(bool?),
				Position = 13,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "FullReply",
				ParameterType = typeof(string),
				Position = 14,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ArtifactEntries",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 15,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Answer",
				ParameterType = typeof(JsonObject),
				Position = 16,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ThreadId",
				ParameterType = typeof(string),
				Position = 17,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Card",
				ParameterType = typeof(JsonObject),
				Position = 18,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "IsRoutineRun",
				ParameterType = typeof(bool?),
				Position = 19,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "TaskTitle",
				ParameterType = typeof(string),
				Position = 20,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "FollowUps",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 21,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "SentText",
				ParameterType = typeof(string),
				Position = 22,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "SentFiles",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 23,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "SitesRead",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 24,
				IsNullable = true,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<ChatHandOffKind> Create_ChatHandOffKind(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ChatHandOffKind> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(ChatHandOffKind), new JsonStringEnumConverter<ChatHandOffKind>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ChatHandOffKind>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ChatHandOffOutcome> Create_ChatHandOffOutcome(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ChatHandOffOutcome> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(ChatHandOffOutcome), new JsonStringEnumConverter<ChatHandOffOutcome>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ChatHandOffOutcome>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ChatHandOffOutcome?> Create_NullableChatHandOffOutcome(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ChatHandOffOutcome?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<ChatHandOffOutcome>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ChatHandOffOutcome?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ConversationMessage> Create_ConversationMessage(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ConversationMessage> jsonTypeInfo))
		{
			JsonObjectInfoValues<ConversationMessage> objectInfo = new JsonObjectInfoValues<ConversationMessage>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new ConversationMessage
				{
					Id = (string)args[0],
					ServerId = (long?)args[1],
					Role = (string)args[2],
					Content = (string)args[3],
					Source = (string)args[4],
					CreatedAt = (DateTimeOffset)args[5],
					AttachmentPaths = (IReadOnlyList<string>)args[6],
					BubbleSegments = (IReadOnlyList<string>)args[7],
					ReplacedLocalId = (string)args[8],
					SentToAgentName = (string)args[9]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => ConversationMessagePropInit(options),
				ConstructorParameterMetadataInitializer = ConversationMessageCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(ConversationMessage).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ConversationMessageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ConversationMessagePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[18];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).Id,
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
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsRequired = true;
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<long?> propertyInfo2 = new JsonPropertyInfoValues<long?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).ServerId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ServerId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("ServerId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(long?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).Role,
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
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
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
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).Content,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Content",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("Content", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsRequired = true;
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).Source,
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
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo6 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).CreatedAt,
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
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsRequired = true;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo7 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).AttachmentPaths,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AttachmentPaths",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("AttachmentPaths", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo8 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).BubbleSegments,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BubbleSegments",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("BubbleSegments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).ReplacedLocalId,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReplacedLocalId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("ReplacedLocalId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<string> propertyInfo10 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = (object obj) => ((ConversationMessage)obj).SentToAgentName,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SentToAgentName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("SentToAgentName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<bool> propertyInfo11 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsPending",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("IsPending", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<bool> propertyInfo12 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsUser",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("IsUser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<string> propertyInfo13 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RowId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("RowId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		array[12].IsGetNullable = false;
		JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo14 = new JsonPropertyInfoValues<IReadOnlyList<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BubbleTexts",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("BubbleTexts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		array[13].IsGetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BubbleIsOnlyAboutAttachments",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("BubbleIsOnlyAboutAttachments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<string> propertyInfo16 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TypedWords",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("TypedWords", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		array[15].IsGetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo17 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BubbleText",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("BubbleText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		array[16].IsGetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo18 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ConversationMessage),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ActionCaption",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ConversationMessage).GetProperty("ActionCaption", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		return array;
	}

	private void ConversationMessageSerializeHandler(Utf8JsonWriter writer, ConversationMessage? value)
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
			writer.WriteString(PropName_Id, id);
		}
		long? serverId = value.ServerId;
		if (serverId.HasValue)
		{
			writer.WritePropertyName(PropName_ServerId);
			JsonSerializer.Serialize(writer, serverId, NullableInt64);
		}
		string role = value.Role;
		if (role != null)
		{
			writer.WriteString(PropName_Role, role);
		}
		string content = value.Content;
		if (content != null)
		{
			writer.WriteString(PropName_Content, content);
		}
		string source = value.Source;
		if (source != null)
		{
			writer.WriteString(PropName_Source, source);
		}
		writer.WriteString(PropName_CreatedAt, value.CreatedAt);
		IReadOnlyList<string> attachmentPaths = value.AttachmentPaths;
		if (attachmentPaths != null)
		{
			writer.WritePropertyName(PropName_AttachmentPaths);
			IReadOnlyListStringSerializeHandler(writer, attachmentPaths);
		}
		IReadOnlyList<string> bubbleSegments = value.BubbleSegments;
		if (bubbleSegments != null)
		{
			writer.WritePropertyName(PropName_BubbleSegments);
			IReadOnlyListStringSerializeHandler(writer, bubbleSegments);
		}
		string replacedLocalId = value.ReplacedLocalId;
		if (replacedLocalId != null)
		{
			writer.WriteString(PropName_ReplacedLocalId, replacedLocalId);
		}
		string sentToAgentName = value.SentToAgentName;
		if (sentToAgentName != null)
		{
			writer.WriteString(PropName_SentToAgentName, sentToAgentName);
		}
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] ConversationMessageCtorParamInit()
	{
		return new JsonParameterInfoValues[10]
		{
			new JsonParameterInfoValues
			{
				Name = "Id",
				ParameterType = typeof(string),
				Position = 0,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ServerId",
				ParameterType = typeof(long?),
				Position = 1,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Role",
				ParameterType = typeof(string),
				Position = 2,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Content",
				ParameterType = typeof(string),
				Position = 3,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Source",
				ParameterType = typeof(string),
				Position = 4,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CreatedAt",
				ParameterType = typeof(DateTimeOffset),
				Position = 5,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "AttachmentPaths",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 6,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "BubbleSegments",
				ParameterType = typeof(IReadOnlyList<string>),
				Position = 7,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ReplacedLocalId",
				ParameterType = typeof(string),
				Position = 8,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "SentToAgentName",
				ParameterType = typeof(string),
				Position = 9,
				IsNullable = true,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<IReadOnlyList<ConversationMessage>> Create_IReadOnlyListConversationMessage(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<ConversationMessage>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<ConversationMessage>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<ConversationMessage>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListConversationMessageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<ConversationMessage>, ConversationMessage>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListConversationMessageSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<ConversationMessage>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (ConversationMessage item in value)
		{
			ConversationMessageSerializeHandler(writer, item);
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

	private JsonTypeInfo<List<ChatHandOff>> Create_ListChatHandOff(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<ChatHandOff>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<ChatHandOff>> collectionInfo = new JsonCollectionInfoValues<List<ChatHandOff>>
			{
				ObjectCreator = () => new List<ChatHandOff>(),
				SerializeHandler = ListChatHandOffSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<ChatHandOff>, ChatHandOff>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListChatHandOffSerializeHandler(Utf8JsonWriter writer, List<ChatHandOff>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			ChatHandOffSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
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

	private JsonTypeInfo<JsonObject> Create_JsonObject(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<JsonObject> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<JsonObject>(options, JsonMetadataServices.JsonObjectConverter);
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

	private JsonTypeInfo<long?> Create_NullableInt64(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<long?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<long>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<long?>(options, nullableConverter);
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

	public NotchChatJsonContext()
		: base(null)
	{
	}

	public NotchChatJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(CachedConversation))
		{
			return Create_CachedConversation(options);
		}
		if (type == typeof(ChatHandOff))
		{
			return Create_ChatHandOff(options);
		}
		if (type == typeof(ChatHandOffKind))
		{
			return Create_ChatHandOffKind(options);
		}
		if (type == typeof(ChatHandOffOutcome))
		{
			return Create_ChatHandOffOutcome(options);
		}
		if (type == typeof(ChatHandOffOutcome?))
		{
			return Create_NullableChatHandOffOutcome(options);
		}
		if (type == typeof(ConversationMessage))
		{
			return Create_ConversationMessage(options);
		}
		if (type == typeof(IReadOnlyList<ConversationMessage>))
		{
			return Create_IReadOnlyListConversationMessage(options);
		}
		if (type == typeof(IReadOnlyList<string>))
		{
			return Create_IReadOnlyListString(options);
		}
		if (type == typeof(List<ChatHandOff>))
		{
			return Create_ListChatHandOff(options);
		}
		if (type == typeof(DateTimeOffset))
		{
			return Create_DateTimeOffset(options);
		}
		if (type == typeof(JsonObject))
		{
			return Create_JsonObject(options);
		}
		if (type == typeof(long))
		{
			return Create_Int64(options);
		}
		if (type == typeof(long?))
		{
			return Create_NullableInt64(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
