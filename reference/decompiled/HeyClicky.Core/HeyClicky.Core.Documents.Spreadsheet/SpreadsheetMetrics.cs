using System;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetMetrics
{
	public const double DefaultFontSize = 12.5;

	public const double FontPointScale = 1.17;

	public const double RowHeightScale = 1.3333333333333333;

	public const double DefaultRowHeight = 21.0;

	public const double MinimumRowHeight = 4.0;

	public const double DefaultColumnWidth = 100.0;

	public const double ColumnHeaderHeight = 22.0;

	public const double CellHorizontalPadding = 4.0;

	public const double IndentWidth = 9.0;

	public static double Points(double excelColumnWidthCharacters)
	{
		if (excelColumnWidthCharacters <= 0.0)
		{
			return 0.0;
		}
		return Math.Round(excelColumnWidthCharacters * 7.4 + 5.0, MidpointRounding.AwayFromZero);
	}
}
