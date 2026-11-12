using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public sealed record CompanionTurnFinish(bool WordedTurn, IReadOnlyList<string> HeardLines, bool DrewMarks, string? UserWords = null)
{
	public string SpokenText => string.Join(" ", HeardLines).Trim();
}
