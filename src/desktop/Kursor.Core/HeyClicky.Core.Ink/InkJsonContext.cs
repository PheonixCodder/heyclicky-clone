using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HeyClicky.Core.Ink;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(InkNoteSnapshot))]
[JsonSerializable(typeof(InkActionsSnapshot))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
public class InkJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<bool?>? _NullableBoolean;

	private JsonTypeInfo<double>? _Double;

	private JsonTypeInfo<InkActionsSnapshot>? _InkActionsSnapshot;

	private JsonTypeInfo<InkNoteSnapshot>? _InkNoteSnapshot;

	private JsonTypeInfo<InkNoteVersionFile>? _InkNoteVersionFile;

	private JsonTypeInfo<InkSentActionFile>? _InkSentActionFile;

	private JsonTypeInfo<Dictionary<string, List<InkSentActionFile>>>? _DictionaryStringListInkSentActionFile;

	private JsonTypeInfo<List<InkNoteVersionFile>>? _ListInkNoteVersionFile;

	private JsonTypeInfo<List<InkSentActionFile>>? _ListInkSentActionFile;

	private JsonTypeInfo<List<string>>? _ListString;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_version = JsonEncodedText.Encode("version");

	private static readonly JsonEncodedText PropName_actionsByThingID = JsonEncodedText.Encode("actionsByThingID");

	private static readonly JsonEncodedText PropName_versions = JsonEncodedText.Encode("versions");

	private static readonly JsonEncodedText PropName_at = JsonEncodedText.Encode("at");

	private static readonly JsonEncodedText PropName_text = JsonEncodedText.Encode("text");

	private static readonly JsonEncodedText PropName_isEdit = JsonEncodedText.Encode("isEdit");

	private static readonly JsonEncodedText PropName_isPartial = JsonEncodedText.Encode("isPartial");

	private static readonly JsonEncodedText PropName_startsNewEdit = JsonEncodedText.Encode("startsNewEdit");

	private static readonly JsonEncodedText PropName_kind = JsonEncodedText.Encode("kind");

	private static readonly JsonEncodedText PropName_objectKey = JsonEncodedText.Encode("objectKey");

	private static readonly JsonEncodedText PropName_marginWords = JsonEncodedText.Encode("marginWords");

	private static readonly JsonEncodedText PropName_sentAt = JsonEncodedText.Encode("sentAt");

	private static readonly JsonEncodedText PropName_wasSentWhileRunning = JsonEncodedText.Encode("wasSentWhileRunning");

	private static readonly JsonEncodedText PropName_reviewedItems = JsonEncodedText.Encode("reviewedItems");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = (JsonTypeInfo<bool?>)base.Options.GetTypeInfo(typeof(bool?)));

	public JsonTypeInfo<double> Double => _Double ?? (_Double = (JsonTypeInfo<double>)base.Options.GetTypeInfo(typeof(double)));

	public JsonTypeInfo<InkActionsSnapshot> InkActionsSnapshot => _InkActionsSnapshot ?? (_InkActionsSnapshot = (JsonTypeInfo<InkActionsSnapshot>)base.Options.GetTypeInfo(typeof(InkActionsSnapshot)));

	public JsonTypeInfo<InkNoteSnapshot> InkNoteSnapshot => _InkNoteSnapshot ?? (_InkNoteSnapshot = (JsonTypeInfo<InkNoteSnapshot>)base.Options.GetTypeInfo(typeof(InkNoteSnapshot)));

	public JsonTypeInfo<InkNoteVersionFile> InkNoteVersionFile => _InkNoteVersionFile ?? (_InkNoteVersionFile = (JsonTypeInfo<InkNoteVersionFile>)base.Options.GetTypeInfo(typeof(InkNoteVersionFile)));

	public JsonTypeInfo<InkSentActionFile> InkSentActionFile => _InkSentActionFile ?? (_InkSentActionFile = (JsonTypeInfo<InkSentActionFile>)base.Options.GetTypeInfo(typeof(InkSentActionFile)));

	public JsonTypeInfo<Dictionary<string, List<InkSentActionFile>>> DictionaryStringListInkSentActionFile => _DictionaryStringListInkSentActionFile ?? (_DictionaryStringListInkSentActionFile = (JsonTypeInfo<Dictionary<string, List<InkSentActionFile>>>)base.Options.GetTypeInfo(typeof(Dictionary<string, List<InkSentActionFile>>)));

	public JsonTypeInfo<List<InkNoteVersionFile>> ListInkNoteVersionFile => _ListInkNoteVersionFile ?? (_ListInkNoteVersionFile = (JsonTypeInfo<List<InkNoteVersionFile>>)base.Options.GetTypeInfo(typeof(List<InkNoteVersionFile>)));

	public JsonTypeInfo<List<InkSentActionFile>> ListInkSentActionFile => _ListInkSentActionFile ?? (_ListInkSentActionFile = (JsonTypeInfo<List<InkSentActionFile>>)base.Options.GetTypeInfo(typeof(List<InkSentActionFile>)));

	public JsonTypeInfo<List<string>> ListString => _ListString ?? (_ListString = (JsonTypeInfo<List<string>>)base.Options.GetTypeInfo(typeof(List<string>)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static InkJsonContext Default { get; } = new InkJsonContext(new JsonSerializerOptions(s_defaultOptions));

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

	private JsonTypeInfo<InkActionsSnapshot> Create_InkActionsSnapshot(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InkActionsSnapshot> jsonTypeInfo))
		{
			JsonObjectInfoValues<InkActionsSnapshot> objectInfo = new JsonObjectInfoValues<InkActionsSnapshot>
			{
				ObjectCreator = () => new InkActionsSnapshot(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => InkActionsSnapshotPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(InkActionsSnapshot).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = InkActionsSnapshotSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] InkActionsSnapshotPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkActionsSnapshot),
			Converter = null,
			Getter = (object obj) => ((InkActionsSnapshot)obj).Version,
			Setter = delegate(object obj, int value)
			{
				((InkActionsSnapshot)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = "version",
			AttributeProviderFactory = () => typeof(InkActionsSnapshot).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<Dictionary<string, List<InkSentActionFile>>> propertyInfo2 = new JsonPropertyInfoValues<Dictionary<string, List<InkSentActionFile>>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkActionsSnapshot),
			Converter = null,
			Getter = (object obj) => ((InkActionsSnapshot)obj).ActionsByThingId,
			Setter = delegate(object obj, Dictionary<string, List<InkSentActionFile>>? value)
			{
				((InkActionsSnapshot)obj).ActionsByThingId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ActionsByThingId",
			JsonPropertyName = "actionsByThingID",
			AttributeProviderFactory = () => typeof(InkActionsSnapshot).GetProperty("ActionsByThingId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, List<InkSentActionFile>>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void InkActionsSnapshotSerializeHandler(Utf8JsonWriter writer, InkActionsSnapshot? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_version, value.Version);
		Dictionary<string, List<InkSentActionFile>> actionsByThingId = value.ActionsByThingId;
		if (actionsByThingId != null)
		{
			writer.WritePropertyName(PropName_actionsByThingID);
			DictionaryStringListInkSentActionFileSerializeHandler(writer, actionsByThingId);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<InkNoteSnapshot> Create_InkNoteSnapshot(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InkNoteSnapshot> jsonTypeInfo))
		{
			JsonObjectInfoValues<InkNoteSnapshot> objectInfo = new JsonObjectInfoValues<InkNoteSnapshot>
			{
				ObjectCreator = () => new InkNoteSnapshot(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => InkNoteSnapshotPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(InkNoteSnapshot).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = InkNoteSnapshotSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] InkNoteSnapshotPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteSnapshot),
			Converter = null,
			Getter = (object obj) => ((InkNoteSnapshot)obj).Version,
			Setter = delegate(object obj, int value)
			{
				((InkNoteSnapshot)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = "version",
			AttributeProviderFactory = () => typeof(InkNoteSnapshot).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<List<InkNoteVersionFile>> propertyInfo2 = new JsonPropertyInfoValues<List<InkNoteVersionFile>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteSnapshot),
			Converter = null,
			Getter = (object obj) => ((InkNoteSnapshot)obj).Versions,
			Setter = delegate(object obj, List<InkNoteVersionFile>? value)
			{
				((InkNoteSnapshot)obj).Versions = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Versions",
			JsonPropertyName = "versions",
			AttributeProviderFactory = () => typeof(InkNoteSnapshot).GetProperty("Versions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<InkNoteVersionFile>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void InkNoteSnapshotSerializeHandler(Utf8JsonWriter writer, InkNoteSnapshot? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_version, value.Version);
		List<InkNoteVersionFile> versions = value.Versions;
		if (versions != null)
		{
			writer.WritePropertyName(PropName_versions);
			ListInkNoteVersionFileSerializeHandler(writer, versions);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<InkNoteVersionFile> Create_InkNoteVersionFile(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InkNoteVersionFile> jsonTypeInfo))
		{
			JsonObjectInfoValues<InkNoteVersionFile> objectInfo = new JsonObjectInfoValues<InkNoteVersionFile>
			{
				ObjectCreator = () => new InkNoteVersionFile(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => InkNoteVersionFilePropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(InkNoteVersionFile).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = InkNoteVersionFileSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] InkNoteVersionFilePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<double> propertyInfo = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteVersionFile),
			Converter = null,
			Getter = (object obj) => ((InkNoteVersionFile)obj).At,
			Setter = delegate(object obj, double value)
			{
				((InkNoteVersionFile)obj).At = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "At",
			JsonPropertyName = "at",
			AttributeProviderFactory = () => typeof(InkNoteVersionFile).GetProperty("At", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteVersionFile),
			Converter = null,
			Getter = (object obj) => ((InkNoteVersionFile)obj).Text,
			Setter = delegate(object obj, string? value)
			{
				((InkNoteVersionFile)obj).Text = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Text",
			JsonPropertyName = "text",
			AttributeProviderFactory = () => typeof(InkNoteVersionFile).GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteVersionFile),
			Converter = null,
			Getter = (object obj) => ((InkNoteVersionFile)obj).IsEdit,
			Setter = delegate(object obj, bool value)
			{
				((InkNoteVersionFile)obj).IsEdit = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsEdit",
			JsonPropertyName = "isEdit",
			AttributeProviderFactory = () => typeof(InkNoteVersionFile).GetProperty("IsEdit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<bool?> propertyInfo4 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteVersionFile),
			Converter = null,
			Getter = (object obj) => ((InkNoteVersionFile)obj).IsPartial,
			Setter = delegate(object obj, bool? value)
			{
				((InkNoteVersionFile)obj).IsPartial = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsPartial",
			JsonPropertyName = "isPartial",
			AttributeProviderFactory = () => typeof(InkNoteVersionFile).GetProperty("IsPartial", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool?> propertyInfo5 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkNoteVersionFile),
			Converter = null,
			Getter = (object obj) => ((InkNoteVersionFile)obj).StartsNewEdit,
			Setter = delegate(object obj, bool? value)
			{
				((InkNoteVersionFile)obj).StartsNewEdit = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StartsNewEdit",
			JsonPropertyName = "startsNewEdit",
			AttributeProviderFactory = () => typeof(InkNoteVersionFile).GetProperty("StartsNewEdit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		return array;
	}

	private void InkNoteVersionFileSerializeHandler(Utf8JsonWriter writer, InkNoteVersionFile? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_at, value.At);
		string text = value.Text;
		if (text != null)
		{
			writer.WriteString(PropName_text, text);
		}
		writer.WriteBoolean(PropName_isEdit, value.IsEdit);
		bool? isPartial = value.IsPartial;
		if (isPartial.HasValue)
		{
			writer.WritePropertyName(PropName_isPartial);
			JsonSerializer.Serialize(writer, isPartial, NullableBoolean);
		}
		bool? startsNewEdit = value.StartsNewEdit;
		if (startsNewEdit.HasValue)
		{
			writer.WritePropertyName(PropName_startsNewEdit);
			JsonSerializer.Serialize(writer, startsNewEdit, NullableBoolean);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<InkSentActionFile> Create_InkSentActionFile(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InkSentActionFile> jsonTypeInfo))
		{
			JsonObjectInfoValues<InkSentActionFile> objectInfo = new JsonObjectInfoValues<InkSentActionFile>
			{
				ObjectCreator = () => new InkSentActionFile(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => InkSentActionFilePropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(InkSentActionFile).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = InkSentActionFileSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] InkSentActionFilePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[6];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).Kind,
			Setter = delegate(object obj, string? value)
			{
				((InkSentActionFile)obj).Kind = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Kind",
			JsonPropertyName = "kind",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).ObjectKey,
			Setter = delegate(object obj, string? value)
			{
				((InkSentActionFile)obj).ObjectKey = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ObjectKey",
			JsonPropertyName = "objectKey",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("ObjectKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).MarginWords,
			Setter = delegate(object obj, string? value)
			{
				((InkSentActionFile)obj).MarginWords = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MarginWords",
			JsonPropertyName = "marginWords",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("MarginWords", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).SentAt,
			Setter = delegate(object obj, double value)
			{
				((InkSentActionFile)obj).SentAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SentAt",
			JsonPropertyName = "sentAt",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("SentAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).WasSentWhileRunning,
			Setter = delegate(object obj, bool value)
			{
				((InkSentActionFile)obj).WasSentWhileRunning = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WasSentWhileRunning",
			JsonPropertyName = "wasSentWhileRunning",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("WasSentWhileRunning", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<List<string>> propertyInfo6 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(InkSentActionFile),
			Converter = null,
			Getter = (object obj) => ((InkSentActionFile)obj).ReviewedItems,
			Setter = delegate(object obj, List<string>? value)
			{
				((InkSentActionFile)obj).ReviewedItems = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReviewedItems",
			JsonPropertyName = "reviewedItems",
			AttributeProviderFactory = () => typeof(InkSentActionFile).GetProperty("ReviewedItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		return array;
	}

	private void InkSentActionFileSerializeHandler(Utf8JsonWriter writer, InkSentActionFile? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string kind = value.Kind;
		if (kind != null)
		{
			writer.WriteString(PropName_kind, kind);
		}
		string objectKey = value.ObjectKey;
		if (objectKey != null)
		{
			writer.WriteString(PropName_objectKey, objectKey);
		}
		string marginWords = value.MarginWords;
		if (marginWords != null)
		{
			writer.WriteString(PropName_marginWords, marginWords);
		}
		writer.WriteNumber(PropName_sentAt, value.SentAt);
		writer.WriteBoolean(PropName_wasSentWhileRunning, value.WasSentWhileRunning);
		List<string> reviewedItems = value.ReviewedItems;
		if (reviewedItems != null)
		{
			writer.WritePropertyName(PropName_reviewedItems);
			ListStringSerializeHandler(writer, reviewedItems);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<Dictionary<string, List<InkSentActionFile>>> Create_DictionaryStringListInkSentActionFile(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Dictionary<string, List<InkSentActionFile>>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<Dictionary<string, List<InkSentActionFile>>> collectionInfo = new JsonCollectionInfoValues<Dictionary<string, List<InkSentActionFile>>>
			{
				ObjectCreator = () => new Dictionary<string, List<InkSentActionFile>>(),
				SerializeHandler = DictionaryStringListInkSentActionFileSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, List<InkSentActionFile>>, string, List<InkSentActionFile>>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void DictionaryStringListInkSentActionFileSerializeHandler(Utf8JsonWriter writer, Dictionary<string, List<InkSentActionFile>>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, List<InkSentActionFile>> item in value)
		{
			writer.WritePropertyName(item.Key);
			ListInkSentActionFileSerializeHandler(writer, item.Value);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<List<InkNoteVersionFile>> Create_ListInkNoteVersionFile(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<InkNoteVersionFile>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<InkNoteVersionFile>> collectionInfo = new JsonCollectionInfoValues<List<InkNoteVersionFile>>
			{
				ObjectCreator = () => new List<InkNoteVersionFile>(),
				SerializeHandler = ListInkNoteVersionFileSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<InkNoteVersionFile>, InkNoteVersionFile>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListInkNoteVersionFileSerializeHandler(Utf8JsonWriter writer, List<InkNoteVersionFile>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			InkNoteVersionFileSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<InkSentActionFile>> Create_ListInkSentActionFile(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<InkSentActionFile>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<InkSentActionFile>> collectionInfo = new JsonCollectionInfoValues<List<InkSentActionFile>>
			{
				ObjectCreator = () => new List<InkSentActionFile>(),
				SerializeHandler = ListInkSentActionFileSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<InkSentActionFile>, InkSentActionFile>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListInkSentActionFileSerializeHandler(Utf8JsonWriter writer, List<InkSentActionFile>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			InkSentActionFileSerializeHandler(writer, value[i]);
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

	private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<int> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
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

	public InkJsonContext()
		: base(null)
	{
	}

	public InkJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(InkActionsSnapshot))
		{
			return Create_InkActionsSnapshot(options);
		}
		if (type == typeof(InkNoteSnapshot))
		{
			return Create_InkNoteSnapshot(options);
		}
		if (type == typeof(InkNoteVersionFile))
		{
			return Create_InkNoteVersionFile(options);
		}
		if (type == typeof(InkSentActionFile))
		{
			return Create_InkSentActionFile(options);
		}
		if (type == typeof(Dictionary<string, List<InkSentActionFile>>))
		{
			return Create_DictionaryStringListInkSentActionFile(options);
		}
		if (type == typeof(List<InkNoteVersionFile>))
		{
			return Create_ListInkNoteVersionFile(options);
		}
		if (type == typeof(List<InkSentActionFile>))
		{
			return Create_ListInkSentActionFile(options);
		}
		if (type == typeof(List<string>))
		{
			return Create_ListString(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
