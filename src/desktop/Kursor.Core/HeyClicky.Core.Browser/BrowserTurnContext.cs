using System;

namespace HeyClicky.Core.Browser;

public sealed record BrowserTurnContext(string Title, string GroupTitle, string? ProgressLine, Func<bool> IsActive);
