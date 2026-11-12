using System.Collections.Generic;

namespace HeyClicky.Core.Notch;

public sealed record CachedConversation(IReadOnlyList<ConversationMessage> Messages, bool HasEarlierMessages);
