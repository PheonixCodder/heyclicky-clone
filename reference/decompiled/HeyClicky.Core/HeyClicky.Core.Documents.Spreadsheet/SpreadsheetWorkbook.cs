using System;
using System.Collections.Generic;
using System.IO;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetWorkbook
{
	public static readonly IReadOnlySet<string> SupportedPathExtensions = new HashSet<string> { "xlsx", "xlsm", "csv", "tsv" };

	public IReadOnlyList<SpreadsheetSheet> Sheets { get; }

	public int InitialSheetIndex { get; }

	public SpreadsheetWorkbook(IReadOnlyList<SpreadsheetSheet> sheets, int initialSheetIndex)
	{
		Sheets = sheets;
		InitialSheetIndex = ((initialSheetIndex >= 0 && initialSheetIndex < sheets.Count) ? initialSheetIndex : 0);
	}

	public static bool CanRead(string path)
	{
		return SupportedPathExtensions.Contains(Path.GetExtension(path).TrimStart('.').ToLowerInvariant());
	}

	public static SpreadsheetWorkbook? Read(string path)
	{
		string text = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
		try
		{
			SpreadsheetWorkbook result;
			switch (text)
			{
			case "xlsx":
			case "xlsm":
				result = SpreadsheetXlsxReader.Read(path);
				break;
			case "csv":
			case "tsv":
				result = SpreadsheetDelimitedTextReader.Read(path, text == "tsv");
				break;
			default:
				result = null;
				break;
			}
			return result;
		}
		catch (Exception ex)
		{
			Log.Info("spreadsheet", "couldn't read " + Path.GetFileName(path) + ": " + ex.Message);
			return null;
		}
	}
}
