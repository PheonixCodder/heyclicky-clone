namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed record SpreadsheetCell(string DisplayText, string FormulaBarText, SpreadsheetCellKind Kind, double? NumericValue, int StyleIndex);
