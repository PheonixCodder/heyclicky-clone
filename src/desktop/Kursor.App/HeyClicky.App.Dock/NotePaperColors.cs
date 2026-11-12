using HeyClicky.Agents.Jobs;
using HeyClicky.App.Ink;

namespace HeyClicky.App.Dock;

public static class NotePaperColors
{
	public static PaperStock Stock(string key)
	{
		return ClickyNotePaperColors.Stock(key) switch
		{
			ClickyPaperStock.Sky => PaperStock.Sky, 
			ClickyPaperStock.Butter => PaperStock.Butter, 
			ClickyPaperStock.Mint => PaperStock.Mint, 
			ClickyPaperStock.Periwinkle => PaperStock.Periwinkle, 
			ClickyPaperStock.Peach => PaperStock.Peach, 
			ClickyPaperStock.Blush => PaperStock.Blush, 
			ClickyPaperStock.Kraft => PaperStock.Kraft, 
			_ => PaperStock.Cream, 
		};
	}

	public static string Key(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text == null)
		{
			return "clicky:" + railKey;
		}
		return "job:" + text;
	}

	public static int Seed(string key)
	{
		int num = 7;
		foreach (char c in key)
		{
			num = (num * 31 + c) & 0xFFFF;
		}
		return num;
	}
}
