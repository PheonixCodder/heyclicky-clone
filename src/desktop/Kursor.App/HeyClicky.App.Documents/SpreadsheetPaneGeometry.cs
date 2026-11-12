namespace HeyClicky.App.Documents;

public readonly record struct SpreadsheetPaneGeometry(int RowStart, int RowEnd, int ColumnStart, int ColumnEnd, double OffsetX, double OffsetY, double ColumnHeaderHeight, double RowHeaderWidth)
{
	public bool RowsEmpty => RowEnd <= RowStart;

	public bool ColumnsEmpty => ColumnEnd <= ColumnStart;
}
