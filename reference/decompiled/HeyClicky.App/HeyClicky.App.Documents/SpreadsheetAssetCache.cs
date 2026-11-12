using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.Core.Documents.Spreadsheet;

namespace HeyClicky.App.Documents;

public static class SpreadsheetAssetCache
{
	private sealed record Entry(string Path, string Revision, SpreadsheetWorkbook? Workbook);

	private static readonly List<Entry> entries = new List<Entry>();

	private static readonly object gate = new object();

	public static async Task<SpreadsheetWorkbook?> LoadAsync(string path)
	{
		string revision = await Task.Run(delegate
		{
			try
			{
				FileInfo fileInfo = new FileInfo(path);
				return fileInfo.Exists ? $"{fileInfo.LastWriteTimeUtc.Ticks}|{fileInfo.Length}" : null;
			}
			catch (Exception)
			{
				return (string)null;
			}
		});
		if (revision == null)
		{
			return null;
		}
		lock (gate)
		{
			Entry entry = entries.FirstOrDefault((Entry e) => e.Path == path);
			if ((object)entry != null && entry.Revision == revision)
			{
				entries.Remove(entry);
				entries.Insert(0, entry);
				return entry.Workbook;
			}
		}
		SpreadsheetWorkbook spreadsheetWorkbook = await Task.Run(() => SpreadsheetWorkbook.Read(path));
		lock (gate)
		{
			entries.RemoveAll((Entry e) => e.Path == path);
			entries.Insert(0, new Entry(path, revision, spreadsheetWorkbook));
			while (entries.Count > 3)
			{
				entries.RemoveAt(entries.Count - 1);
			}
		}
		return spreadsheetWorkbook;
	}
}
