using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Drawing.Overlay;

public static class DrawingSuggestionCards
{
	private static volatile IReadOnlyDictionary<string, string> cardIdsByKey = new Dictionary<string, string>();

	public static IReadOnlyDictionary<string, string> CardIdsByKey => cardIdsByKey;

	public static void Set(IEnumerable<Guid> cardIds)
	{
		cardIdsByKey = cardIds.Select((Guid id, int index) => ($"c{index + 1}", id.ToString())).ToDictionary<(string, string), string, string>(((string, string) pair) => pair.Item1, ((string, string) pair) => pair.Item2, StringComparer.Ordinal);
	}
}
