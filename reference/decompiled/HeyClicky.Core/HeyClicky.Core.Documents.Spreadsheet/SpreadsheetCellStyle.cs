namespace HeyClicky.Core.Documents.Spreadsheet;

public struct SpreadsheetCellStyle
{
	public bool IsBold;

	public bool IsItalic;

	public bool IsUnderlined;

	public bool IsStruckThrough;

	public double FontSize;

	public SpreadsheetColor? TextColor;

	public SpreadsheetColor? FillColor;

	public SpreadsheetHorizontalAlignment HorizontalAlignment;

	public SpreadsheetVerticalAlignment VerticalAlignment;

	public bool WrapsText;

	public int IndentLevel;

	public SpreadsheetBorderEdge? LeftBorder;

	public SpreadsheetBorderEdge? RightBorder;

	public SpreadsheetBorderEdge? TopBorder;

	public SpreadsheetBorderEdge? BottomBorder;

	public string NumberFormatCode;

	public readonly bool HasBorders
	{
		get
		{
			SpreadsheetBorderEdge? leftBorder = LeftBorder;
			if (!leftBorder.HasValue)
			{
				leftBorder = RightBorder;
				if (!leftBorder.HasValue)
				{
					leftBorder = TopBorder;
					if (!leftBorder.HasValue)
					{
						leftBorder = BottomBorder;
						return leftBorder.HasValue;
					}
				}
			}
			return true;
		}
	}

	public SpreadsheetCellStyle()
	{
		IsBold = false;
		IsItalic = false;
		IsUnderlined = false;
		IsStruckThrough = false;
		FontSize = 12.5;
		TextColor = null;
		FillColor = null;
		HorizontalAlignment = SpreadsheetHorizontalAlignment.General;
		VerticalAlignment = SpreadsheetVerticalAlignment.Bottom;
		WrapsText = false;
		IndentLevel = 0;
		LeftBorder = null;
		RightBorder = null;
		TopBorder = null;
		BottomBorder = null;
		NumberFormatCode = "General";
	}
}
