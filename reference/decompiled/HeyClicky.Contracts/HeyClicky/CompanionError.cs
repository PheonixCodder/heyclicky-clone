using System.Text.Json;

namespace HeyClicky;

public sealed record CompanionError(string Code, string Message, JsonElement? Paywall = null);
