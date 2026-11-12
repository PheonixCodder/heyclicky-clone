using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkMadeLink
{
	public sealed record Kind(string Name, string Symbol, string TintHex);

	public const string Attribute = "_made";

	public static bool IsMadeThingHost(string host)
	{
		host = host.ToLowerInvariant();
		HashSet<string> hashSet = new HashSet<string>
		{
			"mail.google.com", "docs.google.com", "sheets.google.com", "drive.google.com", "calendar.google.com", "notion.so", "www.notion.so", "figma.com", "www.figma.com", "canva.com",
			"www.canva.com", "airtable.com"
		};
		string[] source = new string[5] { ".notion.site", ".vercel.app", ".netlify.app", ".github.io", ".pages.dev" };
		if (!hashSet.Contains(host))
		{
			return source.Any((string s) => host.EndsWith(s, StringComparison.Ordinal));
		}
		return true;
	}

	public static bool LabelOpensSomething(string label)
	{
		string words = ClickyInkParser.Emphasis.Strip(label).ToLowerInvariant().TrimWs();
		if (!new string[6] { "open ", "open:", "view ", "edit ", "see the draft", "see the page" }.Any((string p) => words.StartsWith(p, StringComparison.Ordinal)))
		{
			return words == "open";
		}
		return true;
	}

	public static string? Host(string? url)
	{
		if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri result) || result.IsFile)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(result.Host))
		{
			return result.Host;
		}
		return null;
	}

	public static HashSet<string> Ids(ClickyInkLiveNote.Page page, IReadOnlyList<ClickyInkLiveNote.Block> present)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (!present.Any((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Link))
		{
			return hashSet;
		}
		bool flag = page.Blocks.Any((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.File);
		string text = null;
		for (int i = 0; i < present.Count; i++)
		{
			ClickyInkLiveNote.Block block = present[i];
			ClickyInkDocument.Block source = block.Source;
			if (source.Kind == ClickyInkDocument.Kind.Heading)
			{
				text = block.Words.Current;
			}
			else if (source.Kind == ClickyInkDocument.Kind.Link && hashSet.Count < 3 && !ClickyInkGroup.NamesSources(text ?? page.Name.Current))
			{
				string current = block.Words.Current;
				string host = Host(source["url"]) ?? "";
				ClickyInkDocument.Kind? kind = ((i > 0) ? new ClickyInkDocument.Kind?(present[i - 1].Source.Kind) : ((ClickyInkDocument.Kind?)null));
				bool flag2 = (i == 0 || present[i - 1].Source.Kind != ClickyInkDocument.Kind.Link) && (i + 1 >= present.Count || present[i + 1].Source.Kind != ClickyInkDocument.Kind.Link);
				if (LabelOpensSomething(current) || (flag2 && kind == ClickyInkDocument.Kind.Answer) || (flag2 && !flag && IsMadeThingHost(host)))
				{
					hashSet.Add(block.Id);
				}
			}
		}
		return hashSet;
	}

	public static Kind KindOf(string url, string label)
	{
		Uri.TryCreate(url, UriKind.Absolute, out Uri result);
		string host = (Host(url) ?? "").ToLowerInvariant();
		string text = (((object)result == null || result.IsFile) ? "" : result.AbsolutePath).ToLowerInvariant();
		string text2 = ClickyInkParser.Emphasis.Strip(label).ToLowerInvariant();
		if (host == "mail.google.com")
		{
			return new Kind((text2.Contains("draft") || url.Contains("#drafts", StringComparison.Ordinal)) ? "Gmail draft" : "Gmail", "envelope.fill", "#D93025");
		}
		if (host == "sheets.google.com" || (host == "docs.google.com" && text.StartsWith("/spreadsheets", StringComparison.Ordinal)))
		{
			return new Kind("Google Sheet", "tablecells.fill", "#188038");
		}
		if (host == "docs.google.com" && text.StartsWith("/presentation", StringComparison.Ordinal))
		{
			return new Kind("Google Slides", "rectangle.on.rectangle", "#E8A200");
		}
		if (host == "docs.google.com" && text.StartsWith("/forms", StringComparison.Ordinal))
		{
			return new Kind("Google Form", "list.bullet.rectangle", "#7248B9");
		}
		if (host == "docs.google.com")
		{
			return new Kind("Google Doc", "doc.text.fill", "#1A73E8");
		}
		if (host == "drive.google.com")
		{
			return new Kind("Google Drive", "folder.fill", "#1A73E8");
		}
		if (host == "calendar.google.com")
		{
			return new Kind("Calendar event", "calendar", "#1A73E8");
		}
		if (host.EndsWith("notion.so", StringComparison.Ordinal) || host.EndsWith(".notion.site", StringComparison.Ordinal))
		{
			return new Kind("Notion page", "doc.richtext", "#37352F");
		}
		if (host.EndsWith("figma.com", StringComparison.Ordinal))
		{
			return new Kind("Figma file", "paintpalette.fill", "#A259FF");
		}
		if (host.EndsWith("canva.com", StringComparison.Ordinal))
		{
			return new Kind("Canva design", "paintbrush.pointed.fill", "#00A3AD");
		}
		if (host.EndsWith("airtable.com", StringComparison.Ordinal))
		{
			return new Kind("Airtable", "square.grid.3x3.fill", "#D68A00");
		}
		if (new string[4] { ".vercel.app", ".netlify.app", ".github.io", ".pages.dev" }.Any((string s) => host.EndsWith(s, StringComparison.Ordinal)))
		{
			return new Kind("Live page", "globe", "#2F6FDB");
		}
		return new Kind((host.Length == 0) ? "Link" : Domain(url), "link", "#2F6FDB");
	}

	public static string Domain(string source)
	{
		string text = Host(source);
		if (text == null)
		{
			return source;
		}
		if (!text.StartsWith("www.", StringComparison.Ordinal))
		{
			return text;
		}
		string text2 = text;
		return text2.Substring(4, text2.Length - 4);
	}
}
