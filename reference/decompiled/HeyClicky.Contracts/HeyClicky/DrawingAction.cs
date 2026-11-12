using System.Text.Json;

namespace HeyClicky;

public sealed record DrawingAction(string Kind, JsonElement Payload);
