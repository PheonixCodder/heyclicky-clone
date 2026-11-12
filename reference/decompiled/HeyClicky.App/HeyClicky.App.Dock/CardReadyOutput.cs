using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public sealed record CardReadyOutput(Kinds Kind, string? Entry, int Count, string Title, string Detail, string ButtonTitle, string? ButtonSymbol, string Symbol, uint IconBackgroundRgb, string? ThreadId)
{
	public enum Kinds
	{
		File,
		Link,
		Several,
		NothingToOpen
	}

	public bool OpensFullReplyOnly => CardEntry == null;

	public string? CardEntry
	{
		get
		{
			Kinds kind = Kind;
			if ((uint)kind > 1u)
			{
				return null;
			}
			return Entry;
		}
	}

	public static CardReadyOutput Make(IReadOnlyList<string> entries, string? summary, string? threadId, Func<IReadOnlyList<string>, IReadOnlyList<string>> mainEntries, Func<string, string> resolve, Func<string, (string KindLabel, string Symbol, uint Tint)> kind)
	{
		HashSet<string> seen = new HashSet<string>();
		IReadOnlyList<string> readOnlyList = mainEntries(entries.Where((string e) => e.Length > 0 && seen.Add(e)).ToList());
		string text = summary?.Trim();
		string text2 = ((text != null && text.Length > 0) ? text : null);
		if (readOnlyList.Count == 1)
		{
			string text3 = readOnlyList[0];
			if (IsWebLink(text3))
			{
				ClickyLinkName.Name name = ClickyLinkName.For(text3);
				object obj;
				if ((object)name != null)
				{
					ClickyLinkName.Name name2 = name;
					string thing = name2.Thing;
					obj = ((thing != null) ? ("Your " + thing + " is ready") : ("Your " + name2.Site + " link is ready"));
				}
				else
				{
					obj = "Your link is ready";
				}
				string title = (string)obj;
				object obj2;
				if ((object)name != null)
				{
					ClickyLinkName.Name name3 = name;
					obj2 = "Open " + (name3.Thing ?? name3.Site);
				}
				else
				{
					obj2 = "Open link";
				}
				string buttonTitle = (string)obj2;
				return new CardReadyOutput(Kinds.Link, text3, 1, title, text2 ?? "Open it to see what it made.", buttonTitle, "arrow.up.right", "link", 3900150u, threadId);
			}
			string text4 = resolve(text3);
			(string, string, uint) tuple = kind(text4);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text4.TrimEnd('\\', '/'));
			return new CardReadyOutput(Kinds.File, text3, 1, ((fileNameWithoutExtension.Length == 0) ? "Your file" : fileNameWithoutExtension) + " is ready", text2 ?? ("Your " + KindNoun(tuple.Item1) + " is ready to open."), OpenTitle(tuple.Item1), null, tuple.Item2, tuple.Item3, threadId);
		}
		if (readOnlyList.Count > 1)
		{
			return new CardReadyOutput(Kinds.Several, null, readOnlyList.Count, $"{readOnlyList.Count} files are ready", text2 ?? "Open it to see everything it made.", "Open", null, "doc.on.doc.fill", 2269291u, threadId);
		}
		return new CardReadyOutput(Kinds.NothingToOpen, null, 0, "Done", text2 ?? "It finished. Open it to see what it did.", "Open", null, "checkmark", 2269291u, threadId);
	}

	public static string CardButtonTitle(CardReadyOutput output)
	{
		if (output.Kind != Kinds.Link || output.Entry == null)
		{
			return output.ButtonTitle;
		}
		string text = ClickyLinkName.For(output.Entry)?.Thing;
		if (text == null)
		{
			return "Open link";
		}
		string text2 = text.ToLowerInvariant();
		if (text2 == "pull request")
		{
			return "Open PR";
		}
		string text3 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? text2;
		return "Open " + text3;
	}

	public static bool IsWebLink(string entry)
	{
		if (!entry.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return entry.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string OpenTitle(string kindLabel)
	{
		switch (kindLabel)
		{
		case "PDF":
			return "Open PDF";
		case "Web page":
			return "Open page";
		case "Spreadsheet":
			return "Open sheet";
		case "Document":
		case "Markdown":
		case "Text":
			return "Open doc";
		case "Slides":
			return "Open slides";
		case "Image":
			return "Open image";
		case "Video":
			return "Play video";
		case "Audio":
			return "Play audio";
		case "Folder":
			return "Open folder";
		default:
			return "Open";
		}
	}

	private static string KindNoun(string kindLabel)
	{
		switch (kindLabel)
		{
		case "PDF":
			return "PDF";
		case "Web page":
			return "page";
		case "Spreadsheet":
			return "sheet";
		case "Document":
		case "Markdown":
		case "Text":
			return "doc";
		case "Folder":
			return "folder";
		default:
			return (kindLabel.Length == 0) ? "file" : kindLabel.ToLowerInvariant();
		}
	}
}
