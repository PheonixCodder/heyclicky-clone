using System.Text.Json.Serialization;

namespace HeyClicky.Core.Worker;

public sealed record AppConfigLinks([property: JsonPropertyName("featureRequest")] string? FeatureRequest, [property: JsonPropertyName("communityReddit")] string? CommunityReddit, [property: JsonPropertyName("communityWhatsApp")] string? CommunityWhatsApp, [property: JsonPropertyName("changelog")] string? Changelog);
