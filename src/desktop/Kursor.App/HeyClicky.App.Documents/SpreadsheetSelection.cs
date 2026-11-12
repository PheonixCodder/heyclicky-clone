using HeyClicky.Core.Documents.Spreadsheet;

namespace HeyClicky.App.Documents;

public readonly record struct SpreadsheetSelection(SpreadsheetCellAddress Anchor, SpreadsheetCellAddress Extent, SpreadsheetCellRange Range);
