using System.Text.Json.Serialization;

namespace HeyClicky.Core.Worker;

public sealed record AppConfigResponse([property: JsonPropertyName("appConfig")] AppConfig AppConfig);
