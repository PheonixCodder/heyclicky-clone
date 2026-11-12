using System;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkFigureRules
{
	public sealed record Delta(string Text, bool IsCaption, bool IsDown);

	public static Delta? Read(string? delta)
	{
		if (string.IsNullOrEmpty(delta))
		{
			return null;
		}
		string lower = delta.ToLowerInvariant();
		if (!delta.Any(char.IsDigit) && !new string[4] { "up", "down", "fell", "rose" }.Any((string w) => lower.Contains(w, StringComparison.Ordinal)))
		{
			return new Delta(delta, IsCaption: true, IsDown: false);
		}
		bool flag = delta.HasPrefix("-") || delta.HasPrefix("−") || lower.Contains("down", StringComparison.Ordinal);
		return new Delta((flag ? "↓ " : "↑ ") + delta.TrimSet("+-−"), IsCaption: false, flag);
	}
}
