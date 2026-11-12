using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public static class ClickyInkRowMotionGroups
{
	private static readonly Dictionary<string, List<ClickyInkGroup>> RestingGroups = new Dictionary<string, List<ClickyInkGroup>>();

	public static List<ClickyInkGroup> Groups(ClickyInkLiveNote.Page page, double settled, ClickyInkRowMotion.Table table)
	{
		string key = $"{table.Key}|{page.Id}|{InkText.DoubleDescription(settled)}";
		lock (RestingGroups)
		{
			if (RestingGroups.TryGetValue(key, out List<ClickyInkGroup> value))
			{
				return value;
			}
			List<ClickyInkGroup> list = ClickyInkGroup.Groups(page, settled);
			if (RestingGroups.Count > 200)
			{
				RestingGroups.Clear();
			}
			RestingGroups[key] = list;
			return list;
		}
	}
}
