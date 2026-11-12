using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkRoutineStatusLine
{
	public static FrameworkElement Create(ClickyInkRoutineControl routine, ClickyInkStyle style)
	{
		Color color = (routine.IsPaused ? PaperColor.Hex("#B4541A") : (routine.IsRunning ? style.Fresh : style.Quiet));
		return InkTextView.Plain(ClickyInkDashboardViewMath.RoutineStatusWords(routine, null), ClickyPaperFonts.Hand(15.0), color).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
	}
}
