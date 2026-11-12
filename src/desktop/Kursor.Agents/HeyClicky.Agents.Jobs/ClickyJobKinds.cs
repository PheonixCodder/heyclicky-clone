using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky.Agents.Jobs;

public static class ClickyJobKinds
{
	public static ClickyJobKind Infer(IReadOnlyList<string> artifactEntries, string title, string prompt)
	{
		foreach (string artifactEntry in artifactEntries)
		{
			string text = artifactEntry.ToLowerInvariant();
			if (text.StartsWith("http://", StringComparison.Ordinal) || text.StartsWith("https://", StringComparison.Ordinal))
			{
				return ClickyJobKind.Page;
			}
			switch (Path.GetExtension(text).TrimStart('.'))
			{
			case "pdf":
				return ClickyJobKind.Pdf;
			case "doc":
			case "txt":
			case "rtf":
			case "docx":
			case "md":
			case "pages":
				return ClickyJobKind.Document;
			case "tsv":
			case "xls":
			case "csv":
			case "xlsx":
			case "numbers":
				return ClickyJobKind.Sheet;
			case "ppt":
			case "key":
			case "pptx":
				return ClickyJobKind.Slides;
			case "png":
			case "jpg":
			case "gif":
			case "svg":
			case "jpeg":
			case "heic":
			case "webp":
				return ClickyJobKind.Image;
			case "mp4":
			case "mov":
			case "m4v":
			case "mkv":
			case "avi":
			case "webm":
				return ClickyJobKind.Video;
			case "htm":
			case "html":
				return ClickyJobKind.Page;
			case "tsx":
			case "app":
			case "json":
			case "py":
			case "js":
			case "ts":
			case "sh":
			case "rb":
			case "rs":
			case "go":
			case "swift":
				return ClickyJobKind.Code;
			}
		}
		string @object = (title + " " + ((prompt.Length > 200) ? prompt.Substring(0, 200) : prompt)).ToLowerInvariant();
		(string[], ClickyJobKind)[] array = new(string[], ClickyJobKind)[9]
		{
			(new string[1] { "pdf" }, ClickyJobKind.Pdf),
			(new string[4] { "spreadsheet", "sheet", "csv", "table" }, ClickyJobKind.Sheet),
			(new string[3] { "slide", "deck", "presentation" }, ClickyJobKind.Slides),
			(new string[4] { "website", "landing page", "web page", "site" }, ClickyJobKind.Page),
			(new string[4] { "video", "clip", "reel", "movie" }, ClickyJobKind.Video),
			(new string[5] { "image", "logo", "photo", "picture", "poster" }, ClickyJobKind.Image),
			(new string[5] { "code", "script", "app", "bug", "repo" }, ClickyJobKind.Code),
			(new string[6] { "message", "email", "text ", "whatsapp", "reply", "send" }, ClickyJobKind.Message),
			(new string[6] { "doc", "essay", "report", "write", "draft", "notes" }, ClickyJobKind.Document)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (source, result) = array[i];
			if (source.Any(@object.Contains))
			{
				return result;
			}
		}
		return ClickyJobKind.Research;
	}
}
