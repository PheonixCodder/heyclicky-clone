using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkRunStampMark
{
	public static FrameworkElement Create(string? number, ClickyInkDocument.Block block, double clock, double landsAt, double fontSize = 11.0)
	{
		double? num = ClickyInkChange.ChangedAt(block);
		double num2;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (ClickyInkChange.Previous(block, "n") != null || ClickyInkChange.Previous(block, "run") != null)
			{
				num2 = valueOrDefault;
				goto IL_0036;
			}
		}
		num2 = landsAt;
		goto IL_0036;
		IL_0036:
		double num3 = num2;
		double num4 = ClickyInkClock.Progress(clock, num3 + 0.1, num3 + 0.4);
		Grid obj = new Grid
		{
			Children = { (UIElement)new ClickyPaperStamp(ClickyThingStatus.Watching, (number == null) ? "KEPT" : ("RUN " + number), fontSize, -5.0) }
		};
		obj.Transformed(0.0, 0.0, 0.0, 1.0 + 0.35 * (1.0 - num4), null);
		obj.Opacity = ((num4 > 0.0) ? (0.3 + 0.7 * num4) : 0.0);
		return obj;
	}
}
