using System;
using System.Collections.Generic;
using System.IO;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Documents;

public static class DocumentViewers
{
	private static readonly HashSet<string> MarkdownExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "md", "markdown", "mdown", "mkd", "mkdn" };

	public static bool IsMarkdown(string path)
	{
		return MarkdownExtensions.Contains(Path.GetExtension(path).TrimStart('.'));
	}

	public static bool IsSpreadsheet(string path)
	{
		return SpreadsheetWorkbook.CanRead(path);
	}

	public static bool HasViewer(string path)
	{
		if (!IsMarkdown(path))
		{
			return IsSpreadsheet(path);
		}
		return true;
	}

	public static FrameworkElement? CreateViewer(string path, Func<FrameworkElement>? fallback = null)
	{
		FrameworkElement viewer = null;
		if (IsMarkdown(path))
		{
			CreateMarkdownViewer(path, ref viewer);
		}
		else if (IsSpreadsheet(path))
		{
			CreateSpreadsheetViewer(path, fallback, ref viewer);
		}
		return viewer;
	}

	private static void CreateMarkdownViewer(string path, ref FrameworkElement? viewer)
	{
		viewer = new MarkdownViewerContent(path);
	}

	private static void CreateSpreadsheetViewer(string path, Func<FrameworkElement>? fallback, ref FrameworkElement? viewer)
	{
		viewer = new SpreadsheetDocumentView(path, fallback);
	}
}
