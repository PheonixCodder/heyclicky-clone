using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDashboardKinds
{
	public static readonly IReadOnlySet<ClickyInkDocument.Kind> TileKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Figure,
		ClickyInkDocument.Kind.Spark,
		ClickyInkDocument.Kind.Status,
		ClickyInkDocument.Kind.Progress,
		ClickyInkDocument.Kind.Tally,
		ClickyInkDocument.Kind.Streak,
		ClickyInkDocument.Kind.Countdown
	};

	public static readonly IReadOnlySet<ClickyInkDocument.Kind> DashboardKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Spark,
		ClickyInkDocument.Kind.Status,
		ClickyInkDocument.Kind.Progress,
		ClickyInkDocument.Kind.Tally,
		ClickyInkDocument.Kind.Streak,
		ClickyInkDocument.Kind.Countdown,
		ClickyInkDocument.Kind.Pipeline,
		ClickyInkDocument.Kind.Run,
		ClickyInkDocument.Kind.Quiet
	};
}
