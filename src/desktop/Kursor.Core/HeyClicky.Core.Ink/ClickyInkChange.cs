using System;

namespace HeyClicky.Core.Ink;

public static class ClickyInkChange
{
	public static string? Previous(ClickyInkDocument.Block block, string key)
	{
		return block["_prev_" + key];
	}

	public static double? ChangedAt(ClickyInkDocument.Block block)
	{
		return InkText.ParseDouble(block["_changed_at"]);
	}

	public static int? ChangedEdit(ClickyInkDocument.Block block)
	{
		long? num = InkText.ParseInt(block["_changed_edit"]);
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			return (int)valueOrDefault;
		}
		return null;
	}

	public static bool IsLatest(ClickyInkDocument.Block block, int? latestEdit)
	{
		int? num = ChangedEdit(block);
		if (num.HasValue)
		{
			return num.GetValueOrDefault() == latestEdit;
		}
		return false;
	}

	public static bool Animates(ClickyInkDocument.Block block, int? latestEdit)
	{
		if (ChangedAt(block).HasValue)
		{
			if (ChangedEdit(block).HasValue)
			{
				return IsLatest(block, latestEdit);
			}
			return true;
		}
		return false;
	}

	public static double Progress(ClickyInkDocument.Block block, double clock, double delay = 0.0, double seconds = 0.9)
	{
		double? num = ChangedAt(block);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			double num2 = Math.Min(1.0, Math.Max(0.0, (clock - valueOrDefault - delay) / seconds));
			return 1.0 - Math.Pow(1.0 - num2, 3.0);
		}
		return 1.0;
	}
}
