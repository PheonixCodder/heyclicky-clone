using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public interface IClickyInkFrontPageMeasurer
{
	double ContentWidth { get; }

	double Group(ClickyInkGroup group);

	double Table(ClickyInkGroup group, double width);

	double Headline(ClickyInkDashboardBoard board, double width, bool isMission);

	double Tiles(IReadOnlyList<ClickyInkLiveNote.Block> blocks, double width);

	double MissionTileHeight(IReadOnlyList<ClickyInkDocument.Block> blocks);
}
