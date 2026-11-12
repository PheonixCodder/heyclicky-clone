using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkReviewRules
{
	public static string Label(ClickyInkDocument.Block block)
	{
		return ClickyInkNoteActions.Plain(block.Text);
	}

	public static string GoInstruction(ClickyInkDocument.Block block)
	{
		string text = ClickyInkNoteActions.Plain(block["go"] ?? block["do"] ?? block["send"] ?? "").TrimSet(".:;!");
		if (text.Length != 0)
		{
			return text;
		}
		return "Go ahead with these";
	}

	public static string GoLabel(ClickyInkDocument.Block block, int count)
	{
		string text = ClickyInkNoteActions.Plain(block["go_label"] ?? block["label"] ?? "");
		if (count <= 0)
		{
			if (text.Length != 0)
			{
				return text;
			}
			return "Go ahead";
		}
		if (text.Length == 0)
		{
			return $"Go ahead with {count}";
		}
		return $"{text} {count}";
	}

	public static bool IsOpen(ClickyInkDocument.Item item)
	{
		if (item.Mark != ClickyInkDocument.Mark.Done)
		{
			return item.Mark != ClickyInkDocument.Mark.Gone;
		}
		return false;
	}

	public static string Action(string itemText)
	{
		string text = ClickyInkNoteActions.Plain(itemText);
		string[] array = new string[2] { " — ", " – " };
		foreach (string value in array)
		{
			int num = text.Find(value);
			if (num >= 0)
			{
				return text.Substring(0, num).TrimWs();
			}
		}
		return text;
	}

	public static List<string> ItemsToSend(ClickyInkDocument.Block block, IEnumerable<string> alreadySent)
	{
		HashSet<string> sent = alreadySent.Select(ClickyInkObjectKey.Normalized).ToHashSet();
		return (from i in block.Items
			where i.IsChecked == true && IsOpen(i)
			select Action(i.Text) into a
			where !sent.Contains(ClickyInkObjectKey.Normalized(a))
			select a).ToList();
	}

	public static bool WasSent(ClickyInkDocument.Item item, IEnumerable<string> alreadySent)
	{
		return alreadySent.Select(ClickyInkObjectKey.Normalized).ToHashSet().Contains(ClickyInkObjectKey.Normalized(Action(item.Text)));
	}
}
