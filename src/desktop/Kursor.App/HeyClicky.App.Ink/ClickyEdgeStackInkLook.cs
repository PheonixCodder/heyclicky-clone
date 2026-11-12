using HeyClicky.Core.Ink;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackInkLook
{
	public static readonly Color WaitingAmber = PaperColor.Hex("#8A5300");

	public static readonly Color Highlighter = PaperColor.Hex("#FFE27A").Opacity(0.62);

	public static Color TapeColor(ClickyEdgeStackInkReading.State state)
	{
		return PaperColor.Hex(state switch
		{
			ClickyEdgeStackInkReading.State.Writing => "#3A7BF0", 
			ClickyEdgeStackInkReading.State.NeedsYou => "#F2A21E", 
			ClickyEdgeStackInkReading.State.Ready => "#1F9D57", 
			_ => "#8E93A3", 
		});
	}

	public static ClickyInkStyle Style(ClickyInkLiveNote note, ClickyHomeSpreadPaper paper, double clock, ClickyEdgeStackInkReading reading)
	{
		int? latestEdit = note.LatestEdit(clock);
		if (latestEdit.HasValue)
		{
			int valueOrDefault = latestEdit.GetValueOrDefault();
			if (reading.ChangeCounts.IsEmpty && !reading.LeadChanged && !reading.TitleChanged && valueOrDefault - 1 >= 0 && valueOrDefault - 1 < note.EditStarts.Count && !reading.IsPenBusy)
			{
				latestEdit = null;
			}
		}
		return new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, latestEdit).Resting(note, null);
	}
}
