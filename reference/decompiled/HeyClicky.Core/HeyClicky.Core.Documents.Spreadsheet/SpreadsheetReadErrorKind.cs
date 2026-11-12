namespace HeyClicky.Core.Documents.Spreadsheet;

public enum SpreadsheetReadErrorKind
{
	NotAZipArchive,
	UnsupportedEntry,
	MissingPart,
	TooLarge,
	Unreadable
}
