using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkTableRules
{
	public static bool IsCrossingOut(ClickyInkLiveNote.Words words, double clock)
	{
		return words.Segments.Any(delegate(ClickyInkLiveNote.Segment segment)
		{
			InkRange? struck = segment.Struck;
			if (struck.HasValue)
			{
				InkRange valueOrDefault = struck.GetValueOrDefault();
				if (clock >= valueOrDefault.LowerBound)
				{
					double? vanishesAt = segment.VanishesAt;
					if (vanishesAt.HasValue)
					{
						double valueOrDefault2 = vanishesAt.GetValueOrDefault();
						return !(clock >= valueOrDefault2);
					}
					return true;
				}
			}
			return false;
		});
	}
}
