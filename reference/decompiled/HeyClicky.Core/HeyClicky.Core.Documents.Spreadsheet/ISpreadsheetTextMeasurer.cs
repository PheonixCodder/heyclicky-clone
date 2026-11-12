namespace HeyClicky.Core.Documents.Spreadsheet;

public interface ISpreadsheetTextMeasurer
{
	double SingleLineWidth(string text, SpreadsheetCellStyle style);

	double WrappedHeight(string text, SpreadsheetCellStyle style, double width);
}
