using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Win32;

namespace HeyClicky.App.Services;

public sealed class JsonAppSettings : IAppSettings
{
	private sealed class Data
	{
		public string CursorColorHex { get; set; } = "#3380FF";

		public bool ScreenHighlightEnabled { get; set; } = true;

		public bool LibraryModeEnabled { get; set; }

		public bool HasCompletedIntro { get; set; } = true;

		public bool HasCompletedOnboarding { get; set; } = true;

		public bool HasCompletedTutorial { get; set; } = true;

		public string? SquadPack { get; set; }

		public string? DiscoveryChannel { get; set; }

		public bool CursorDocked { get; set; }

		public bool ShakeToDetachEnabled { get; set; }
	}

	[JsonSerializable(typeof(Data))]
	[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
	private sealed class Ctx : JsonSerializerContext, IJsonTypeInfoResolver
	{
		private JsonTypeInfo<bool>? _Boolean;

		private JsonTypeInfo<Data>? _Data;

		private JsonTypeInfo<string>? _String;

		private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

		private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly JsonEncodedText PropName_CursorColorHex = JsonEncodedText.Encode("CursorColorHex");

		private static readonly JsonEncodedText PropName_ScreenHighlightEnabled = JsonEncodedText.Encode("ScreenHighlightEnabled");

		private static readonly JsonEncodedText PropName_LibraryModeEnabled = JsonEncodedText.Encode("LibraryModeEnabled");

		private static readonly JsonEncodedText PropName_HasCompletedIntro = JsonEncodedText.Encode("HasCompletedIntro");

		private static readonly JsonEncodedText PropName_HasCompletedOnboarding = JsonEncodedText.Encode("HasCompletedOnboarding");

		private static readonly JsonEncodedText PropName_HasCompletedTutorial = JsonEncodedText.Encode("HasCompletedTutorial");

		private static readonly JsonEncodedText PropName_SquadPack = JsonEncodedText.Encode("SquadPack");

		private static readonly JsonEncodedText PropName_DiscoveryChannel = JsonEncodedText.Encode("DiscoveryChannel");

		private static readonly JsonEncodedText PropName_CursorDocked = JsonEncodedText.Encode("CursorDocked");

		private static readonly JsonEncodedText PropName_ShakeToDetachEnabled = JsonEncodedText.Encode("ShakeToDetachEnabled");

		public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

		public JsonTypeInfo<Data> Data => _Data ?? (_Data = (JsonTypeInfo<Data>)base.Options.GetTypeInfo(typeof(Data)));

		public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

		public static Ctx Default { get; } = new Ctx(new JsonSerializerOptions(s_defaultOptions));

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

		private JsonTypeInfo<Data> Create_Data(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Data> jsonTypeInfo))
			{
				JsonObjectInfoValues<Data> objectInfo = new JsonObjectInfoValues<Data>
				{
					ObjectCreator = () => new Data(),
					ObjectWithParameterizedConstructorCreator = null,
					PropertyMetadataInitializer = (JsonSerializerContext _) => DataPropInit(options),
					ConstructorParameterMetadataInitializer = null,
					ConstructorAttributeProviderFactory = () => typeof(Data).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
					SerializeHandler = DataSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private static JsonPropertyInfo[] DataPropInit(JsonSerializerOptions options)
		{
			JsonPropertyInfo[] array = new JsonPropertyInfo[10];
			JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).CursorColorHex,
				Setter = delegate(object obj, string? value)
				{
					((Data)obj).CursorColorHex = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "CursorColorHex",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("CursorColorHex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			array[0].IsGetNullable = false;
			array[0].IsSetNullable = false;
			JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).ScreenHighlightEnabled,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).ScreenHighlightEnabled = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "ScreenHighlightEnabled",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("ScreenHighlightEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
			JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).LibraryModeEnabled,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).LibraryModeEnabled = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "LibraryModeEnabled",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("LibraryModeEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
			JsonPropertyInfoValues<bool> propertyInfo4 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).HasCompletedIntro,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).HasCompletedIntro = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "HasCompletedIntro",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("HasCompletedIntro", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
			JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).HasCompletedOnboarding,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).HasCompletedOnboarding = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "HasCompletedOnboarding",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("HasCompletedOnboarding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
			JsonPropertyInfoValues<bool> propertyInfo6 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).HasCompletedTutorial,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).HasCompletedTutorial = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "HasCompletedTutorial",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("HasCompletedTutorial", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
			JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).SquadPack,
				Setter = delegate(object obj, string? value)
				{
					((Data)obj).SquadPack = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "SquadPack",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("SquadPack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
			JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).DiscoveryChannel,
				Setter = delegate(object obj, string? value)
				{
					((Data)obj).DiscoveryChannel = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "DiscoveryChannel",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("DiscoveryChannel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
			JsonPropertyInfoValues<bool> propertyInfo9 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).CursorDocked,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).CursorDocked = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "CursorDocked",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("CursorDocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
			JsonPropertyInfoValues<bool> propertyInfo10 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Data),
				Converter = null,
				Getter = (object obj) => ((Data)obj).ShakeToDetachEnabled,
				Setter = delegate(object obj, bool value)
				{
					((Data)obj).ShakeToDetachEnabled = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "ShakeToDetachEnabled",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Data).GetProperty("ShakeToDetachEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
			return array;
		}

		private void DataSerializeHandler(Utf8JsonWriter writer, Data? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			writer.WriteString(PropName_CursorColorHex, value.CursorColorHex);
			writer.WriteBoolean(PropName_ScreenHighlightEnabled, value.ScreenHighlightEnabled);
			writer.WriteBoolean(PropName_LibraryModeEnabled, value.LibraryModeEnabled);
			writer.WriteBoolean(PropName_HasCompletedIntro, value.HasCompletedIntro);
			writer.WriteBoolean(PropName_HasCompletedOnboarding, value.HasCompletedOnboarding);
			writer.WriteBoolean(PropName_HasCompletedTutorial, value.HasCompletedTutorial);
			writer.WriteString(PropName_SquadPack, value.SquadPack);
			writer.WriteString(PropName_DiscoveryChannel, value.DiscoveryChannel);
			writer.WriteBoolean(PropName_CursorDocked, value.CursorDocked);
			writer.WriteBoolean(PropName_ShakeToDetachEnabled, value.ShakeToDetachEnabled);
			writer.WriteEndObject();
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

		public Ctx()
			: base(null)
		{
		}

		public Ctx(JsonSerializerOptions options)
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
			if (type == typeof(Data))
			{
				return Create_Data(options);
			}
			if (type == typeof(string))
			{
				return Create_String(options);
			}
			return null;
		}
	}

	private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string RunValue = "HeyClicky";

	private readonly string path;

	private readonly Data data;

	private bool paused;

	public string CursorColorHex
	{
		get
		{
			return data.CursorColorHex;
		}
		set
		{
			data.CursorColorHex = value;
			Save();
		}
	}

	public bool ScreenHighlightEnabled
	{
		get
		{
			return data.ScreenHighlightEnabled;
		}
		set
		{
			data.ScreenHighlightEnabled = value;
			Save();
		}
	}

	public bool LibraryModeEnabled
	{
		get
		{
			return data.LibraryModeEnabled;
		}
		set
		{
			data.LibraryModeEnabled = value;
			Save();
		}
	}

	public bool HasCompletedIntro
	{
		get
		{
			return data.HasCompletedIntro;
		}
		set
		{
			data.HasCompletedIntro = value;
			Save();
		}
	}

	public bool HasCompletedOnboarding
	{
		get
		{
			return data.HasCompletedOnboarding;
		}
		set
		{
			data.HasCompletedOnboarding = value;
			Save();
		}
	}

	public bool HasCompletedTutorial
	{
		get
		{
			return data.HasCompletedTutorial;
		}
		set
		{
			data.HasCompletedTutorial = value;
			Save();
		}
	}

	public string? SquadPack
	{
		get
		{
			return data.SquadPack;
		}
		set
		{
			data.SquadPack = value;
			Save();
		}
	}

	public string? DiscoveryChannel
	{
		get
		{
			return data.DiscoveryChannel;
		}
		set
		{
			data.DiscoveryChannel = value;
			Save();
		}
	}

	public bool IsPaused
	{
		get
		{
			return paused;
		}
		set
		{
			paused = value;
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool CursorDocked
	{
		get
		{
			return data.CursorDocked;
		}
		set
		{
			data.CursorDocked = value;
			Save();
		}
	}

	public bool ShakeToDetachEnabled
	{
		get
		{
			return data.ShakeToDetachEnabled;
		}
		set
		{
			data.ShakeToDetachEnabled = value;
			Save();
		}
	}

	public string PushToTalkDisplay => "Ctrl + Alt";

	public bool DictationAutoDetect { get; set; } = true;

	public string DictationSpecificLanguageCode { get; set; } = "en";

	public string DictationCustomWordsText { get; set; } = "HeyClicky";

	public string TextModeDisplay => "Ctrl";

	public bool LaunchAtLogin
	{
		get
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			return registryKey?.GetValue("HeyClicky") is string;
		}
		set
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			if (value)
			{
				registryKey.SetValue("HeyClicky", "\"" + Environment.ProcessPath + "\" --background");
			}
			else
			{
				registryKey.DeleteValue("HeyClicky", throwOnMissingValue: false);
			}
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? Changed;

	public JsonAppSettings(string? directory = null)
	{
		string path = directory ?? AppPaths.Root;
		Directory.CreateDirectory(path);
		this.path = Path.Combine(path, "settings.json");
		try
		{
			data = (File.Exists(this.path) ? (JsonSerializer.Deserialize(File.ReadAllText(this.path), Ctx.Default.Data) ?? new Data()) : new Data());
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "unreadable settings, using defaults: " + ex.Message);
			data = new Data();
		}
	}

	private void Save()
	{
		try
		{
			File.WriteAllText(path, JsonSerializer.Serialize(data, Ctx.Default.Data));
		}
		catch (Exception ex)
		{
			Log.Error("settings", "save failed", ex);
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
	}
}
