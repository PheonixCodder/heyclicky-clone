using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkLint
{
	public const string FileName = "note.problems";

	public const int MaximumProblems = 8;

	public static List<string> Problems(string text)
	{
		ClickyInkDocument clickyInkDocument = ClickyInkParser.Parse(text);
		List<string> problems = new List<string>();
		if (string.IsNullOrEmpty(clickyInkDocument.Title))
		{
			Add("The note has no # title: start it with a two-to-four word title.");
		}
		if (clickyInkDocument.Pages.Count > 4)
		{
			Add($"The note has {clickyInkDocument.Pages.Count} pages; keep it to four at most (merge or cut the thinnest).");
		}
		int num = text.Components("{now}").Length - 1;
		if (num > 1)
		{
			Add($"{num} lines are marked {{now}}: keep {{now}} on the one step you're on, and tick or drop the others.");
		}
		for (int j = 0; j < clickyInkDocument.Pages.Count; j++)
		{
			ClickyInkDocument.Page page = clickyInkDocument.Pages[j];
			string value = ((page.Name.Length == 0) ? $"page {j + 1}" : ("“" + page.Name + "”"));
			foreach (ClickyInkDocument.Block block in page.Blocks)
			{
				List<string> list = new List<string> { block.Text };
				foreach (ClickyInkDocument.Item item in block.Items)
				{
					list.Add(item.Text);
					list.Add(item.Detail ?? "");
					list.AddRange(item.Cells);
				}
				list.AddRange(block.Columns);
				foreach (string item2 in list)
				{
					string text2 = RawMarkup(item2);
					if (text2 != null)
					{
						Add($"On {value}, “{Short(item2)}” shows {text2} as raw text: write it as an object on its own line, or drop it.");
					}
				}
				switch (block.Kind)
				{
				case ClickyInkDocument.Kind.Unknown:
					Add($"On {value}, “{Short(block.Text)}” isn't a paper object the page knows: use one from the brief (figure, sticky, ticket, receipt, photo, place, day, letter, clip, chart, file, link, ask, question, note).");
					break;
				case ClickyInkDocument.Kind.List:
				case ClickyInkDocument.Kind.Checklist:
				case ClickyInkDocument.Kind.Numbered:
					if (block.Items.Count > 6)
					{
						Add($"On {value}, a list has {block.Items.Count} items: keep the five that matter, or make it a table or a chart.");
					}
					break;
				case ClickyInkDocument.Kind.Table:
				{
					if (block.Columns.Count > 4)
					{
						Add($"On {value}, a table has {block.Columns.Count} columns: four at most fit the page (put the rest under it).");
					}
					string text3 = block.Items.SelectMany((ClickyInkDocument.Item i) => i.Cells).FirstOrDefault((string c) => c.CharacterCount() > 34);
					if (text3 != null)
					{
						Add($"On {value}, the table cell “{Short(text3)}” is too long: a number or two or three words; the why goes under the table.");
					}
					break;
				}
				case ClickyInkDocument.Kind.Paragraph:
					if (block.Text.CharacterCount() > 260)
					{
						Add($"On {value}, a paragraph runs {block.Text.CharacterCount()} characters: two short sentences at most.");
					}
					break;
				case ClickyInkDocument.Kind.Answer:
				{
					string text4 = block["big"];
					if (text4 != null && text4.CharacterCount() > 12)
					{
						Add("The answer's big=\"" + text4 + "\" is too long for the circle: use the number that matters (a price, a count) or the short name people already call it, under 12 characters. Never squash words together.");
					}
					break;
				}
				case ClickyInkDocument.Kind.Ticket:
					if (block["from"] == null || block["to"] == null)
					{
						Add("The ticket “" + Short(block.Text) + "” needs from= and to=, or it isn't a ticket.");
					}
					break;
				case ClickyInkDocument.Kind.Photo:
					if (block["query"] == null && block["path"] == null)
					{
						Add("The photo “" + Short(block.Text) + "” needs query= (what to search a photo for) or path=.");
					}
					break;
				}
			}
		}
		return problems.Take(8).ToList();
		void Add(string problem)
		{
			if (!problems.Contains(problem))
			{
				problems.Add(problem);
			}
		}
	}

	public static string? FileText(IReadOnlyList<string> problems)
	{
		if (problems.Count == 0)
		{
			return null;
		}
		return "What will look broken on the page (fix these lines in your next save of note.md):\n" + string.Join("\n", problems.Select((string p) => "- " + p)) + "\n";
	}

	private static string? RawMarkup(string text)
	{
		(string, string)[] array = new(string, string)[7]
		{
			("::", "“::”"),
			("](", "a markdown link"),
			("**", "“**”"),
			("`", "a backtick"),
			("{", "“{”"),
			("}", "“}”"),
			("<", "“<”")
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (value, result) = array[i];
			if (text.Contains(value, StringComparison.Ordinal))
			{
				return result;
			}
		}
		return null;
	}

	private static string Short(string text)
	{
		string text2 = text.Replace("\n", " ");
		if (text2.CharacterCount() <= 48)
		{
			return text2;
		}
		return text2.PrefixCharacters(47) + "…";
	}
}
