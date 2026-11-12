using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkPlanReading
{
	public static IReadOnlyList<(string Text, bool IsDone)>? ChecklistSteps(string noteText)
	{
		ClickyInkDocument.Block block = ClickyInkParser.Parse(noteText).AllBlocks.Select<(int, ClickyInkDocument.Block), ClickyInkDocument.Block>(((int Page, ClickyInkDocument.Block Block) b) => b.Block).FirstOrDefault((ClickyInkDocument.Block b) => b.Kind == ClickyInkDocument.Kind.Checklist);
		if ((object)block == null)
		{
			return null;
		}
		List<(string, bool)> list = (from item in block.Items
			select (Text: item.Text.TrimWsNl(), IsDone: item.IsChecked == true) into i
			where i.Text.Length > 0
			select i).ToList();
		if (list.Count != 0)
		{
			return list;
		}
		return null;
	}

	public static string? LatestText(string? notePath)
	{
		if (notePath != null && ClickyLiveNotePreferences.IsEnabled)
		{
			IReadOnlyList<ClickyInkNoteVersion> readOnlyList = ClickyInkNoteStore.Shared.Versions(notePath);
			if (readOnlyList != null && readOnlyList.Count > 0)
			{
				return readOnlyList[readOnlyList.Count - 1].Text;
			}
		}
		return null;
	}

	public static string? NoteTitle(string? notePath)
	{
		string text = LatestText(notePath);
		if (text == null)
		{
			return null;
		}
		string text2 = (ClickyInkParser.Parse(text).Title ?? "").TrimWsNl();
		if (text2.Length != 0)
		{
			return text2;
		}
		return null;
	}
}
