using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HeyClicky.Agents.Runtime;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Connectors;

namespace HeyClicky.App.Settings;

public static class CustomConnectorStore
{
	[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.15.2218")]
	private sealed class ConnectorJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
	{
		private JsonTypeInfo<bool>? _Boolean;

		private JsonTypeInfo<CustomConnector>? _CustomConnector;

		private JsonTypeInfo<Dictionary<string, string>>? _DictionaryStringString;

		private JsonTypeInfo<IReadOnlyList<string>>? _IReadOnlyListString;

		private JsonTypeInfo<List<CustomConnector>>? _ListCustomConnector;

		private JsonTypeInfo<DateTimeOffset>? _DateTimeOffset;

		private JsonTypeInfo<string>? _String;

		private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

		private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly JsonEncodedText PropName_Id = JsonEncodedText.Encode("Id");

		private static readonly JsonEncodedText PropName_Name = JsonEncodedText.Encode("Name");

		private static readonly JsonEncodedText PropName_Kind = JsonEncodedText.Encode("Kind");

		private static readonly JsonEncodedText PropName_Url = JsonEncodedText.Encode("Url");

		private static readonly JsonEncodedText PropName_Command = JsonEncodedText.Encode("Command");

		private static readonly JsonEncodedText PropName_Arguments = JsonEncodedText.Encode("Arguments");

		private static readonly JsonEncodedText PropName_EnvironmentNames = JsonEncodedText.Encode("EnvironmentNames");

		private static readonly JsonEncodedText PropName_HasApiKey = JsonEncodedText.Encode("HasApiKey");

		private static readonly JsonEncodedText PropName_OAuthClientId = JsonEncodedText.Encode("OAuthClientId");

		private static readonly JsonEncodedText PropName_HeaderNames = JsonEncodedText.Encode("HeaderNames");

		private static readonly JsonEncodedText PropName_TokenFromSignIn = JsonEncodedText.Encode("TokenFromSignIn");

		private static readonly JsonEncodedText PropName_DerivedCapabilityDescription = JsonEncodedText.Encode("DerivedCapabilityDescription");

		private static readonly JsonEncodedText PropName_BrowserSignInSupport = JsonEncodedText.Encode("BrowserSignInSupport");

		private static readonly JsonEncodedText PropName_CreatedAt = JsonEncodedText.Encode("CreatedAt");

		public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

		public JsonTypeInfo<CustomConnector> CustomConnector => _CustomConnector ?? (_CustomConnector = (JsonTypeInfo<CustomConnector>)base.Options.GetTypeInfo(typeof(CustomConnector)));

		public JsonTypeInfo<Dictionary<string, string>> DictionaryStringString => _DictionaryStringString ?? (_DictionaryStringString = (JsonTypeInfo<Dictionary<string, string>>)base.Options.GetTypeInfo(typeof(Dictionary<string, string>)));

		public JsonTypeInfo<IReadOnlyList<string>> IReadOnlyListString => _IReadOnlyListString ?? (_IReadOnlyListString = (JsonTypeInfo<IReadOnlyList<string>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<string>)));

		public JsonTypeInfo<List<CustomConnector>> ListCustomConnector => _ListCustomConnector ?? (_ListCustomConnector = (JsonTypeInfo<List<CustomConnector>>)base.Options.GetTypeInfo(typeof(List<CustomConnector>)));

		public JsonTypeInfo<DateTimeOffset> DateTimeOffset => _DateTimeOffset ?? (_DateTimeOffset = (JsonTypeInfo<DateTimeOffset>)base.Options.GetTypeInfo(typeof(DateTimeOffset)));

		public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

		public static ConnectorJsonContext Default { get; } = new ConnectorJsonContext(new JsonSerializerOptions(s_defaultOptions));

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

		private JsonTypeInfo<CustomConnector> Create_CustomConnector(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CustomConnector> jsonTypeInfo))
			{
				JsonObjectInfoValues<CustomConnector> objectInfo = new JsonObjectInfoValues<CustomConnector>
				{
					ObjectCreator = null,
					ObjectWithParameterizedConstructorCreator = (object[] args) => new CustomConnector((string)args[0], (string)args[1], (string)args[2], (string)args[3], (string)args[4], (IReadOnlyList<string>)args[5], (IReadOnlyList<string>)args[6], (bool)args[7])
					{
						OAuthClientId = (string)args[8],
						HeaderNames = (IReadOnlyList<string>)args[9],
						TokenFromSignIn = (bool)args[10],
						DerivedCapabilityDescription = (string)args[11],
						BrowserSignInSupport = (string)args[12],
						CreatedAt = (DateTimeOffset)args[13]
					},
					PropertyMetadataInitializer = (JsonSerializerContext _) => CustomConnectorPropInit(options),
					ConstructorParameterMetadataInitializer = CustomConnectorCtorParamInit,
					ConstructorAttributeProviderFactory = () => typeof(CustomConnector).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[8]
					{
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(IReadOnlyList<string>),
						typeof(IReadOnlyList<string>),
						typeof(bool)
					}, null),
					SerializeHandler = CustomConnectorSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private static JsonPropertyInfo[] CustomConnectorPropInit(JsonSerializerOptions options)
		{
			JsonPropertyInfo[] array = new JsonPropertyInfo[23];
			JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Id,
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
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			array[0].IsGetNullable = false;
			array[0].IsSetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Name,
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
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
			array[1].IsGetNullable = false;
			array[1].IsSetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Kind,
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
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
			array[2].IsGetNullable = false;
			array[2].IsSetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Url,
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
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Url", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
			JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Command,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "Command",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Command", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
			JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo6 = new JsonPropertyInfoValues<IReadOnlyList<string>>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).Arguments,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "Arguments",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Arguments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
			};
			array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
			array[5].IsGetNullable = false;
			array[5].IsSetNullable = false;
			JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo7 = new JsonPropertyInfoValues<IReadOnlyList<string>>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).EnvironmentNames,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "EnvironmentNames",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("EnvironmentNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
			};
			array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
			array[6].IsGetNullable = false;
			array[6].IsSetNullable = false;
			JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).HasApiKey,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "HasApiKey",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("HasApiKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
			JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).OAuthClientId,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "OAuthClientId",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("OAuthClientId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
			JsonPropertyInfoValues<IReadOnlyList<string>> propertyInfo10 = new JsonPropertyInfoValues<IReadOnlyList<string>>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).HeaderNames,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "HeaderNames",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("HeaderNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<string>), Array.Empty<Type>(), null)
			};
			array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
			array[9].IsGetNullable = false;
			array[9].IsSetNullable = false;
			JsonPropertyInfoValues<bool> propertyInfo11 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).TokenFromSignIn,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "TokenFromSignIn",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("TokenFromSignIn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
			JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).DerivedCapabilityDescription,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "DerivedCapabilityDescription",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("DerivedCapabilityDescription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
			JsonPropertyInfoValues<string> propertyInfo13 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).BrowserSignInSupport,
				Setter = delegate
				{
					throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "BrowserSignInSupport",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("BrowserSignInSupport", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
			JsonPropertyInfoValues<DateTimeOffset> propertyInfo14 = new JsonPropertyInfoValues<DateTimeOffset>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = (object obj) => ((CustomConnector)obj).CreatedAt,
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
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
			};
			array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
			JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "IsRemote",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("IsRemote", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
			JsonPropertyInfoValues<string> propertyInfo16 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "ServerKey",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("ServerKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
			array[15].IsGetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo17 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "IntegrationId",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("IntegrationId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
			array[16].IsGetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo18 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "TrimmedOAuthClientId",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("TrimmedOAuthClientId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
			array[17].IsGetNullable = false;
			JsonPropertyInfoValues<bool> propertyInfo19 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "UsesBrowserSignIn",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("UsesBrowserSignIn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[18] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo19);
			JsonPropertyInfoValues<bool> propertyInfo20 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "UsesPlainBearerToken",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("UsesPlainBearerToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
			};
			array[19] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo20);
			JsonPropertyInfoValues<string> propertyInfo21 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "LocationSummary",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("LocationSummary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[20] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo21);
			array[20].IsGetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo22 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "Summary",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[21] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo22);
			array[21].IsGetNullable = false;
			JsonPropertyInfoValues<string> propertyInfo23 = new JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(CustomConnector),
				Converter = null,
				Getter = null,
				Setter = null,
				IgnoreCondition = JsonIgnoreCondition.Always,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "EffectiveCapabilityDescription",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(CustomConnector).GetProperty("EffectiveCapabilityDescription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
			};
			array[22] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo23);
			array[22].IsGetNullable = false;
			return array;
		}

		private void CustomConnectorSerializeHandler(Utf8JsonWriter writer, CustomConnector? value)
		{
			if ((object)value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			writer.WriteString(PropName_Id, value.Id);
			writer.WriteString(PropName_Name, value.Name);
			writer.WriteString(PropName_Kind, value.Kind);
			writer.WriteString(PropName_Url, value.Url);
			writer.WriteString(PropName_Command, value.Command);
			writer.WritePropertyName(PropName_Arguments);
			IReadOnlyListStringSerializeHandler(writer, value.Arguments);
			writer.WritePropertyName(PropName_EnvironmentNames);
			IReadOnlyListStringSerializeHandler(writer, value.EnvironmentNames);
			writer.WriteBoolean(PropName_HasApiKey, value.HasApiKey);
			writer.WriteString(PropName_OAuthClientId, value.OAuthClientId);
			writer.WritePropertyName(PropName_HeaderNames);
			IReadOnlyListStringSerializeHandler(writer, value.HeaderNames);
			writer.WriteBoolean(PropName_TokenFromSignIn, value.TokenFromSignIn);
			writer.WriteString(PropName_DerivedCapabilityDescription, value.DerivedCapabilityDescription);
			writer.WriteString(PropName_BrowserSignInSupport, value.BrowserSignInSupport);
			writer.WriteString(PropName_CreatedAt, value.CreatedAt);
			writer.WriteEndObject();
		}

		private static JsonParameterInfoValues[] CustomConnectorCtorParamInit()
		{
			return new JsonParameterInfoValues[14]
			{
				new JsonParameterInfoValues
				{
					Name = "Id",
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
					Name = "Kind",
					ParameterType = typeof(string),
					Position = 2,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = false
				},
				new JsonParameterInfoValues
				{
					Name = "Url",
					ParameterType = typeof(string),
					Position = 3,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = true
				},
				new JsonParameterInfoValues
				{
					Name = "Command",
					ParameterType = typeof(string),
					Position = 4,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = true
				},
				new JsonParameterInfoValues
				{
					Name = "Arguments",
					ParameterType = typeof(IReadOnlyList<string>),
					Position = 5,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = false
				},
				new JsonParameterInfoValues
				{
					Name = "EnvironmentNames",
					ParameterType = typeof(IReadOnlyList<string>),
					Position = 6,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = false
				},
				new JsonParameterInfoValues
				{
					Name = "HasApiKey",
					ParameterType = typeof(bool),
					Position = 7,
					HasDefaultValue = false,
					DefaultValue = null,
					IsNullable = false
				},
				new JsonParameterInfoValues
				{
					Name = "OAuthClientId",
					ParameterType = typeof(string),
					Position = 8,
					IsNullable = true,
					IsMemberInitializer = true
				},
				new JsonParameterInfoValues
				{
					Name = "HeaderNames",
					ParameterType = typeof(IReadOnlyList<string>),
					Position = 9,
					IsNullable = false,
					IsMemberInitializer = true
				},
				new JsonParameterInfoValues
				{
					Name = "TokenFromSignIn",
					ParameterType = typeof(bool),
					Position = 10,
					IsNullable = false,
					IsMemberInitializer = true
				},
				new JsonParameterInfoValues
				{
					Name = "DerivedCapabilityDescription",
					ParameterType = typeof(string),
					Position = 11,
					IsNullable = true,
					IsMemberInitializer = true
				},
				new JsonParameterInfoValues
				{
					Name = "BrowserSignInSupport",
					ParameterType = typeof(string),
					Position = 12,
					IsNullable = true,
					IsMemberInitializer = true
				},
				new JsonParameterInfoValues
				{
					Name = "CreatedAt",
					ParameterType = typeof(DateTimeOffset),
					Position = 13,
					IsNullable = false,
					IsMemberInitializer = true
				}
			};
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

		private JsonTypeInfo<List<CustomConnector>> Create_ListCustomConnector(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<CustomConnector>> jsonTypeInfo))
			{
				JsonCollectionInfoValues<List<CustomConnector>> collectionInfo = new JsonCollectionInfoValues<List<CustomConnector>>
				{
					ObjectCreator = () => new List<CustomConnector>(),
					SerializeHandler = ListCustomConnectorSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<CustomConnector>, CustomConnector>(options, collectionInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private void ListCustomConnectorSerializeHandler(Utf8JsonWriter writer, List<CustomConnector>? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartArray();
			for (int i = 0; i < value.Count; i++)
			{
				CustomConnectorSerializeHandler(writer, value[i]);
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

		private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		public ConnectorJsonContext()
			: base(null)
		{
		}

		public ConnectorJsonContext(JsonSerializerOptions options)
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
			if (type == typeof(CustomConnector))
			{
				return Create_CustomConnector(options);
			}
			if (type == typeof(Dictionary<string, string>))
			{
				return Create_DictionaryStringString(options);
			}
			if (type == typeof(IReadOnlyList<string>))
			{
				return Create_IReadOnlyListString(options);
			}
			if (type == typeof(List<CustomConnector>))
			{
				return Create_ListCustomConnector(options);
			}
			if (type == typeof(DateTimeOffset))
			{
				return Create_DateTimeOffset(options);
			}
			if (type == typeof(string))
			{
				return Create_String(options);
			}
			return null;
		}
	}

	private static readonly byte[] Entropy = "HeyClicky.Connectors.v1"u8.ToArray();

	private static readonly object Gate = new object();

	private static string Dir => AppPaths.Root;

	private static string ListPath => Path.Combine(Dir, "connectors.json");

	private static string SecretsPath => Path.Combine(Dir, "connectors.secrets");

	public static event Action? Changed;

	public static event Action? Updated;

	public static List<CustomConnector> Load()
	{
		lock (Gate)
		{
			try
			{
				if (File.Exists(ListPath))
				{
					List<CustomConnector>? obj = JsonSerializer.Deserialize(File.ReadAllText(ListPath), ConnectorJsonContext.Default.ListCustomConnector) ?? new List<CustomConnector>();
					int num = obj.RemoveAll((CustomConnector c) => (object)c == null || string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Name));
					if (num > 0)
					{
						Log.Warn("connectors", $"connectors.json: skipped {num} entry(ies) without an id or a name");
					}
					return obj;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("connectors", "connectors.json unreadable: " + ex.Message);
			}
			return new List<CustomConnector>();
		}
	}

	public static CustomConnector? Get(string id)
	{
		return Load().FirstOrDefault((CustomConnector c) => c.Id == id);
	}

	private static void WriteList(List<CustomConnector> list)
	{
		Directory.CreateDirectory(Dir);
		AtomicFile.WriteAllBytes(ListPath, JsonSerializer.SerializeToUtf8Bytes(list, ConnectorJsonContext.Default.ListCustomConnector));
	}

	private static Dictionary<string, string> LoadSecrets()
	{
		try
		{
			if (!File.Exists(SecretsPath))
			{
				return new Dictionary<string, string>();
			}
			return JsonSerializer.Deserialize(ProtectedData.Unprotect(File.ReadAllBytes(SecretsPath), Entropy, DataProtectionScope.CurrentUser), ConnectorJsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>();
		}
		catch (Exception ex)
		{
			Log.Warn("connectors", "secrets unreadable: " + ex.Message);
			return new Dictionary<string, string>();
		}
	}

	private static void SaveSecrets(Dictionary<string, string> secrets)
	{
		Directory.CreateDirectory(Dir);
		byte[] array = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(secrets, ConnectorJsonContext.Default.DictionaryStringString), Entropy, DataProtectionScope.CurrentUser);
		AtomicFile.WriteAllBytes(SecretsPath, array);
	}

	public static Dictionary<string, string> SecretsFor(string id)
	{
		lock (Gate)
		{
			return (from kv in LoadSecrets()
				where kv.Key.StartsWith(id + "|", StringComparison.Ordinal)
				select kv).ToDictionary(delegate(KeyValuePair<string, string> kv)
			{
				string key = kv.Key;
				int num = id.Length + 1;
				return key.Substring(num, key.Length - num);
			}, (KeyValuePair<string, string> kv) => kv.Value);
		}
	}

	public static CustomConnectorSecrets ReadSecrets(string id)
	{
		Dictionary<string, string> dictionary = SecretsFor(id);
		DateTimeOffset result;
		CustomConnectorSecrets customConnectorSecrets = new CustomConnectorSecrets
		{
			BearerToken = dictionary.GetValueOrDefault("api_key"),
			RefreshToken = dictionary.GetValueOrDefault("oauth_refresh"),
			TokenEndpoint = dictionary.GetValueOrDefault("oauth_token_endpoint"),
			RegisteredClientId = dictionary.GetValueOrDefault("oauth_client_id"),
			RegisteredClientSecret = dictionary.GetValueOrDefault("oauth_client_secret"),
			RegisteredClientRedirectUri = dictionary.GetValueOrDefault("oauth_client_redirect"),
			ExpiresAt = (DateTimeOffset.TryParse(dictionary.GetValueOrDefault("oauth_expires"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result) ? new DateTimeOffset?(result) : ((DateTimeOffset?)null))
		};
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			item.Deconstruct(out var key, out var value);
			string text = key;
			string value2 = value;
			if (text.StartsWith("env:", StringComparison.Ordinal))
			{
				Dictionary<string, string> environment = customConnectorSecrets.Environment;
				value = text;
				environment[value.Substring(4, value.Length - 4)] = value2;
			}
			else if (text.StartsWith("header:", StringComparison.Ordinal))
			{
				Dictionary<string, string> headers = customConnectorSecrets.Headers;
				value = text;
				headers[value.Substring(7, value.Length - 7)] = value2;
			}
		}
		return customConnectorSecrets;
	}

	private static void WriteSecrets(Dictionary<string, string> all, string id, CustomConnectorSecrets s)
	{
		foreach (string item in all.Keys.Where((string k) => k.StartsWith(id + "|", StringComparison.Ordinal)).ToList())
		{
			all.Remove(item);
		}
		Put("api_key", s.BearerToken);
		Put("oauth_refresh", s.RefreshToken);
		Put("oauth_token_endpoint", s.TokenEndpoint);
		Put("oauth_client_id", s.RegisteredClientId);
		Put("oauth_client_secret", s.RegisteredClientSecret);
		Put("oauth_client_redirect", s.RegisteredClientRedirectUri);
		Put("oauth_expires", s.ExpiresAt?.ToString("O", CultureInfo.InvariantCulture));
		string value2;
		string key2;
		foreach (KeyValuePair<string, string> item2 in s.Environment)
		{
			item2.Deconstruct(out value2, out key2);
			string text = value2;
			string value3 = key2;
			Put("env:" + text, value3);
		}
		foreach (KeyValuePair<string, string> header in s.Headers)
		{
			header.Deconstruct(out key2, out value2);
			string text2 = key2;
			string value4 = value2;
			Put("header:" + text2, value4);
		}
		void Put(string key, string? value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				all[id + "|" + key] = value;
			}
		}
	}

	public static string? EnvironmentConflict(CustomConnector connector, IEnumerable<string> names, IReadOnlyList<CustomConnector> all)
	{
		if (connector.IsRemote)
		{
			return null;
		}
		foreach (CustomConnector other in all.Where((CustomConnector o) => o.Id != connector.Id && !o.IsRemote))
		{
			string text = names.FirstOrDefault((string n) => other.EnvironmentNames.Contains(n));
			if (text != null)
			{
				return other.Name + " already uses " + text + ". Use different environment variable names or remove that connector first.";
			}
		}
		return null;
	}

	public static string? Save(CustomConnector connector, CustomConnectorSecrets secrets, out bool isNew)
	{
		isNew = false;
		lock (Gate)
		{
			List<CustomConnector> list = Load();
			if (list.Any((CustomConnector c) => c.Id != connector.Id && string.Equals(c.Name, connector.Name, StringComparison.OrdinalIgnoreCase)))
			{
				return "You already have a connector with that name.";
			}
			string text = EnvironmentConflict(connector, secrets.Environment.Keys, list);
			if (text != null)
			{
				return text;
			}
			if (connector.IsRemote && Uri.TryCreate(connector.Url, UriKind.Absolute, out Uri result) && result.UserInfo.Length > 0)
			{
				return "Remove credentials from the URL and use the authentication fields.";
			}
			Dictionary<string, string> dictionary = LoadSecrets();
			CustomConnector customConnector = list.FirstOrDefault((CustomConnector c) => c.Id == connector.Id);
			isNew = (object)customConnector == null;
			if ((object)customConnector != null)
			{
				CustomConnectorSecrets customConnectorSecrets = ReadSecretsLocked(dictionary, connector.Id);
				if (!connector.HasSameAuthenticationTarget(customConnector))
				{
					secrets = new CustomConnectorSecrets
					{
						Environment = (connector.IsRemote ? new Dictionary<string, string>() : secrets.Environment)
					};
					connector = connector with
					{
						HasApiKey = false,
						TokenFromSignIn = false,
						HeaderNames = Array.Empty<string>(),
						BrowserSignInSupport = null,
						DerivedCapabilityDescription = null
					};
				}
				else
				{
					string bearerToken = secrets.BearerToken;
					if (bearerToken != null && bearerToken == customConnectorSecrets.BearerToken)
					{
						secrets.RefreshToken = customConnectorSecrets.RefreshToken;
						secrets.ExpiresAt = customConnectorSecrets.ExpiresAt;
						secrets.TokenEndpoint = customConnectorSecrets.TokenEndpoint;
						secrets.RegisteredClientId = customConnectorSecrets.RegisteredClientId;
						secrets.RegisteredClientSecret = customConnectorSecrets.RegisteredClientSecret;
						secrets.RegisteredClientRedirectUri = customConnectorSecrets.RegisteredClientRedirectUri;
						connector = connector with
						{
							TokenFromSignIn = customConnector.TokenFromSignIn
						};
					}
					if (customConnector.Url?.Trim() != connector.Url?.Trim())
					{
						connector = connector with
						{
							DerivedCapabilityDescription = null,
							BrowserSignInSupport = null
						};
					}
					else
					{
						connector = connector with
						{
							BrowserSignInSupport = customConnector.BrowserSignInSupport
						};
					}
				}
				connector = connector with
				{
					CreatedAt = customConnector.CreatedAt
				};
			}
			else if (connector.CreatedAt == default(DateTimeOffset))
			{
				connector = connector with
				{
					CreatedAt = DateTimeOffset.Now
				};
			}
			connector = connector with
			{
				HasApiKey = !string.IsNullOrEmpty(secrets.BearerToken),
				EnvironmentNames = (connector.IsRemote ? new List<string>() : secrets.Environment.Keys.ToList()),
				HeaderNames = (connector.IsRemote ? secrets.Headers.Keys.ToList() : new List<string>())
			};
			if (!connector.HasApiKey)
			{
				connector = connector with
				{
					TokenFromSignIn = false
				};
			}
			WriteSecrets(dictionary, connector.Id, secrets);
			list.RemoveAll((CustomConnector c) => c.Id == connector.Id);
			list.Add(connector);
			WriteList(list);
			SaveSecrets(dictionary);
			Log.Info("connectors", $"saved {connector.Name} ({connector.Kind}{(connector.HasApiKey ? ", bearer" : "")}{((connector.TrimmedOAuthClientId.Length > 0) ? ", client id" : "")}{((connector.HeaderNames.Count > 0) ? $", {connector.HeaderNames.Count} header(s)" : "")}{((connector.EnvironmentNames.Count > 0) ? $", {connector.EnvironmentNames.Count} env" : "")})");
		}
		CustomConnectorStore.Changed?.Invoke();
		return null;
	}

	private static CustomConnectorSecrets ReadSecretsLocked(Dictionary<string, string> all, string id)
	{
		CustomConnectorSecrets customConnectorSecrets = new CustomConnectorSecrets();
		foreach (KeyValuePair<string, string> item in all)
		{
			item.Deconstruct(out var key, out var value);
			string text = key;
			string text2 = value;
			if (!text.StartsWith(id + "|", StringComparison.Ordinal))
			{
				continue;
			}
			value = text;
			int num = id.Length + 1;
			string text3 = value.Substring(num, value.Length - num);
			switch (text3)
			{
			case "api_key":
				customConnectorSecrets.BearerToken = text2;
				continue;
			case "oauth_refresh":
				customConnectorSecrets.RefreshToken = text2;
				continue;
			case "oauth_token_endpoint":
				customConnectorSecrets.TokenEndpoint = text2;
				continue;
			case "oauth_client_id":
				customConnectorSecrets.RegisteredClientId = text2;
				continue;
			case "oauth_client_secret":
				customConnectorSecrets.RegisteredClientSecret = text2;
				continue;
			case "oauth_client_redirect":
				customConnectorSecrets.RegisteredClientRedirectUri = text2;
				continue;
			case "oauth_expires":
			{
				customConnectorSecrets.ExpiresAt = (DateTimeOffset.TryParse(text2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? new DateTimeOffset?(result) : ((DateTimeOffset?)null));
				continue;
			}
			}
			if (text3.StartsWith("env:", StringComparison.Ordinal))
			{
				Dictionary<string, string> environment = customConnectorSecrets.Environment;
				value = text3;
				environment[value.Substring(4, value.Length - 4)] = text2;
			}
			else if (text3.StartsWith("header:", StringComparison.Ordinal))
			{
				Dictionary<string, string> headers = customConnectorSecrets.Headers;
				value = text3;
				headers[value.Substring(7, value.Length - 7)] = text2;
			}
		}
		return customConnectorSecrets;
	}

	public static void StoreSignInTokens(string id, McpOAuthTokens tokens, Uri? tokenEndpoint, McpOAuthClientRegistration? registered, string? registeredRedirectUri = null)
	{
		lock (Gate)
		{
			List<CustomConnector> list = Load();
			int num = list.FindIndex((CustomConnector c) => c.Id == id);
			if (num < 0)
			{
				return;
			}
			Dictionary<string, string> dictionary = LoadSecrets();
			CustomConnectorSecrets customConnectorSecrets = ReadSecretsLocked(dictionary, id);
			customConnectorSecrets.BearerToken = tokens.AccessToken;
			customConnectorSecrets.RefreshToken = tokens.RefreshToken ?? customConnectorSecrets.RefreshToken;
			customConnectorSecrets.ExpiresAt = tokens.ExpiresAt;
			if ((object)tokenEndpoint != null)
			{
				customConnectorSecrets.TokenEndpoint = tokenEndpoint.AbsoluteUri;
			}
			if ((object)registered != null)
			{
				customConnectorSecrets.RegisteredClientId = registered.ClientId;
				customConnectorSecrets.RegisteredClientSecret = registered.ClientSecret;
				customConnectorSecrets.RegisteredClientRedirectUri = registeredRedirectUri;
			}
			WriteSecrets(dictionary, id, customConnectorSecrets);
			list[num] = list[num]with
			{
				HasApiKey = true,
				TokenFromSignIn = true,
				BrowserSignInSupport = "supported"
			};
			WriteList(list);
			SaveSecrets(dictionary);
		}
		string text = id;
		DateTimeOffset? expiresAt = tokens.ExpiresAt;
		object obj;
		if (expiresAt.HasValue)
		{
			DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
			obj = $" (expires {valueOrDefault:u})";
		}
		else
		{
			obj = "";
		}
		Log.Info("connectors", "sign-in token stored for " + text + (string?)obj);
		CustomConnectorStore.Changed?.Invoke();
	}

	public static void UpdateQuietly(string id, Func<CustomConnector, CustomConnector> mutate)
	{
		bool flag = false;
		lock (Gate)
		{
			List<CustomConnector> list = Load();
			int num = list.FindIndex((CustomConnector c) => c.Id == id);
			if (num < 0)
			{
				return;
			}
			CustomConnector customConnector = mutate(list[num]);
			if (customConnector != list[num])
			{
				list[num] = customConnector;
				WriteList(list);
				flag = true;
			}
		}
		if (flag)
		{
			CustomConnectorStore.Updated?.Invoke();
		}
	}

	public static void Remove(string id)
	{
		lock (Gate)
		{
			List<CustomConnector> list = Load();
			list.RemoveAll((CustomConnector c) => c.Id == id);
			WriteList(list);
			Dictionary<string, string> dictionary = LoadSecrets();
			foreach (string item in dictionary.Keys.Where((string k) => k.StartsWith(id + "|", StringComparison.Ordinal)).ToList())
			{
				dictionary.Remove(item);
			}
			SaveSecrets(dictionary);
		}
		Log.Info("connectors", "removed " + id);
		CustomConnectorStore.Changed?.Invoke();
	}

	public static IReadOnlyList<CustomConnectorRegistration> Registrations()
	{
		List<CustomConnector> list = Load();
		Dictionary<string, string> dictionary;
		lock (Gate)
		{
			dictionary = LoadSecrets();
		}
		List<CustomConnectorRegistration> list2 = new List<CustomConnectorRegistration>();
		foreach (CustomConnector item in list.OrderBy((CustomConnector c) => c.CreatedAt))
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			string serverKeyUpper = item.ServerKey.ToUpperInvariant();
			string text = "KURSOR_" + serverKeyUpper;
			string legacyPrefix = "HEYCLICKY_" + serverKeyUpper;
			CustomConnectorTransport transport;
			if (item.IsRemote)
			{
				Uri result;
				bool flag = !Uri.TryCreate(item.Url?.Trim(), UriKind.Absolute, out result);
				if (!flag)
				{
					string scheme = result.Scheme;
					bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
					flag = !flag2;
				}
				if (flag || result.UserInfo.Length > 0)
				{
					continue;
				}
				string text2 = null;
				if (item.HasApiKey && dictionary.TryGetValue(item.Id + "|api_key", out var value) && value.Length > 0)
				{
					text2 = text + "_TOKEN";
					dictionary2[text2] = value;
					dictionary2[legacyPrefix + "_TOKEN"] = value;
				}
				Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
				for (int i = 0; i < item.HeaderNames.Count; i++)
				{
					if (dictionary.TryGetValue(item.Id + "|header:" + item.HeaderNames[i], out var value2) && value2.Length != 0)
					{
						string text3 = $"{text}_HEADER_{i}";
						dictionary3[item.HeaderNames[i]] = text3;
						dictionary2[text3] = value2;
						dictionary2[$"{legacyPrefix}_HEADER_{i}"] = value2;
					}
				}
				transport = new CustomConnectorTransport.Remote(item.Url.Trim(), text2, dictionary3);
			}
			else
			{
				if (EnvironmentConflict(item, item.EnvironmentNames, list) != null)
				{
					continue;
				}
				List<string> list3 = new List<string>();
				foreach (string environmentName in item.EnvironmentNames)
				{
					if (dictionary.TryGetValue(item.Id + "|env:" + environmentName, out var value3) && value3.Length > 0)
					{
						dictionary2[environmentName] = value3;
						list3.Add(environmentName);
					}
				}
				transport = new CustomConnectorTransport.Stdio(CommandResolver.Resolve(item.Command ?? ""), item.Arguments, list3, CommandResolver.SearchPath());
			}
			string effectiveCapabilityDescription = item.EffectiveCapabilityDescription;
			list2.Add(new CustomConnectorRegistration(item.ServerKey, item.Name, effectiveCapabilityDescription, transport, dictionary2));
		}
		return list2;
	}
}
