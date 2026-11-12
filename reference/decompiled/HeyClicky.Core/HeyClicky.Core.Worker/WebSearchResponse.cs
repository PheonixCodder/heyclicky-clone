using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record WebSearchResponse(string? Query, IReadOnlyList<WebSearchResult>? Results);
