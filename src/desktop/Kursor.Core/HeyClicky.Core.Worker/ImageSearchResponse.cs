using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record ImageSearchResponse(IReadOnlyList<ImageSearchResult>? Images);
