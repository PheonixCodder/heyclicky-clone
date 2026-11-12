using System;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetReadException(SpreadsheetReadErrorKind kind, string? part = null) : Exception((part == null) ? kind.ToString() : $"{kind}({part})")
{
	public SpreadsheetReadErrorKind Kind { get; } = kind;

	public string? Part { get; } = part;
}
