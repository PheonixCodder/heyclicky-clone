using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record WebSearchResult(string Title, string Url, string? Description, string? Age, IReadOnlyList<string>? Highlights);
