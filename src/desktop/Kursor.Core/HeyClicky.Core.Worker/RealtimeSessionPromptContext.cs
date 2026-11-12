using System.Collections.Generic;
using System.Text.Json;

namespace HeyClicky.Core.Worker;

public sealed record RealtimeSessionPromptContext(IReadOnlyList<string> ConnectedIntegrations, IReadOnlyList<string> NeedsReconnectIntegrations, int PriorMessageCount, string TimeZone, string CurrentLocalDatetime, string VoiceGender, IReadOnlyList<JsonElement> ActiveSkills, string? DictationHoldShortcut = null);
