using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record RealtimeSessionRequest(string Voice, double Speed, RealtimeSessionPromptContext PromptContext, IReadOnlyList<string> ClientCapabilities, string? MouthProvider = null, string? Language = null, IReadOnlyList<string>? Languages = null);
