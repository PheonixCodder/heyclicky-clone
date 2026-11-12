using HeyClicky.Core.Ink;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkDashboardPalette
{
	public static readonly Color Up = ClickyPaperInk.DoneGreen;

	public static readonly Color Down = PaperColor.Hex("#C2410C");

	public static readonly Color Pencil = PaperColor.Hex("#3A3630");

	public static readonly Color Level = PaperColor.Hex("#6B7285");

	public static Color Lamp(ClickyInkStatusState state)
	{
		return PaperColor.Hex(state switch
		{
			ClickyInkStatusState.Ok => "#35A85E", 
			ClickyInkStatusState.Watch => "#F2A532", 
			ClickyInkStatusState.Bad => "#E5484D", 
			_ => "#A9AEBA", 
		});
	}

	public static Color Word(ClickyInkStatusState state)
	{
		return state switch
		{
			ClickyInkStatusState.Ok => ClickyPaperInk.DoneGreen, 
			ClickyInkStatusState.Watch => PaperColor.Hex("#B05F00"), 
			ClickyInkStatusState.Bad => PaperColor.Hex("#C2302B"), 
			_ => PaperColor.Hex("#6B7285"), 
		};
	}

	public static Color DeltaColor(double delta, ClickyInkDocument.Block block)
	{
		bool? flag = ClickyInkDashboardViewMath.DeltaIsGoodNews(delta, block);
		if (flag.HasValue)
		{
			if (flag == true)
			{
				return Up;
			}
			return Down;
		}
		return Level;
	}
}
