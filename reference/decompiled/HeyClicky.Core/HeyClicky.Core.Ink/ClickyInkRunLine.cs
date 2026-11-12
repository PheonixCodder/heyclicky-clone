using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkRunLine
{
	public string? Number { get; }

	public string? RanAt { get; }

	public string? Next { get; }

	public string? Extra { get; }

	public ClickyInkRunLine(ClickyInkDocument.Block block)
	{
		Dictionary<string, string> a = block.Attributes;
		string text = (Get("n") ?? Get("run") ?? Get("number") ?? Get("edition"))?.TrimWs();
		Number = (string.IsNullOrEmpty(text) ? null : text);
		string text2 = Get("at") ?? Get("date") ?? Get("when") ?? Get("ran");
		RanAt = (string.IsNullOrEmpty(text2) ? null : text2);
		string text3 = Get("next");
		Next = (string.IsNullOrEmpty(text3) ? null : text3);
		string text4 = block.Text.TrimWs();
		if (Number == null && text4.Lowered().HasPrefix("run"))
		{
			string text5 = text4;
			string text6 = text5.Substring(3, text5.Length - 3).SplitCharacters((string c) => !InkText.IsNumber(c)).FirstOrDefault();
			if (text6 != null)
			{
				Number = text6;
				List<string> source = (from p in text4.Split('·')
					select p.TrimWs()).ToList();
				text4 = string.Join(" · ", source.Skip(1));
				if (RanAt == null && text4.Length > 0)
				{
					RanAt = text4;
					text4 = "";
				}
			}
		}
		string text7 = Get("note")?.TrimWs();
		string text8 = string.Join(" · ", new string[2]
		{
			(text4.Length == 0) ? null : text4,
			string.IsNullOrEmpty(text7) ? null : text7
		}.Where((string s) => s != null));
		Extra = ((text8.Length == 0) ? null : text8);
		string? Get(string key)
		{
			if (!a.TryGetValue(key, out string value))
			{
				return null;
			}
			return value;
		}
	}
}
