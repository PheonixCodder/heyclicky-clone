using System;

namespace HeyClicky.Core.Documents.Spreadsheet;

public readonly record struct SpreadsheetCellRange
{
	public int FirstRow { get; }

	public int FirstColumn { get; }

	public int LastRow { get; }

	public int LastColumn { get; }

	public bool IsSingleCell
	{
		get
		{
			if (FirstRow == LastRow)
			{
				return FirstColumn == LastColumn;
			}
			return false;
		}
	}

	public string Reference
	{
		get
		{
			string reference = new SpreadsheetCellAddress(FirstRow, FirstColumn).Reference;
			if (IsSingleCell)
			{
				return reference;
			}
			return reference + ":" + new SpreadsheetCellAddress(LastRow, LastColumn).Reference;
		}
	}

	public SpreadsheetCellRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
	{
		FirstRow = Math.Min(firstRow, lastRow);
		LastRow = Math.Max(firstRow, lastRow);
		FirstColumn = Math.Min(firstColumn, lastColumn);
		LastColumn = Math.Max(firstColumn, lastColumn);
	}

	public SpreadsheetCellRange(SpreadsheetCellAddress address)
		: this(address.Row, address.Column, address.Row, address.Column)
	{
	}

	public static SpreadsheetCellRange Spanning(SpreadsheetCellAddress first, SpreadsheetCellAddress second)
	{
		return new SpreadsheetCellRange(first.Row, first.Column, second.Row, second.Column);
	}

	public static SpreadsheetCellRange? Parse(string reference)
	{
		string[] array = reference.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			SpreadsheetCellAddress? spreadsheetCellAddress = SpreadsheetCellAddress.Parse(array[0]);
			if (spreadsheetCellAddress.HasValue)
			{
				SpreadsheetCellAddress valueOrDefault = spreadsheetCellAddress.GetValueOrDefault();
				SpreadsheetCellAddress? spreadsheetCellAddress2 = ((array.Length > 1) ? SpreadsheetCellAddress.Parse(array[1]) : new SpreadsheetCellAddress?(valueOrDefault));
				if (spreadsheetCellAddress2.HasValue)
				{
					SpreadsheetCellAddress valueOrDefault2 = spreadsheetCellAddress2.GetValueOrDefault();
					return Spanning(valueOrDefault, valueOrDefault2);
				}
				return null;
			}
		}
		return null;
	}

	public bool Contains(int row, int column)
	{
		if (row >= FirstRow && row <= LastRow && column >= FirstColumn)
		{
			return column <= LastColumn;
		}
		return false;
	}

	public SpreadsheetCellRange Union(SpreadsheetCellRange other)
	{
		return new SpreadsheetCellRange(Math.Min(FirstRow, other.FirstRow), Math.Min(FirstColumn, other.FirstColumn), Math.Max(LastRow, other.LastRow), Math.Max(LastColumn, other.LastColumn));
	}

	public bool Intersects(SpreadsheetCellRange other)
	{
		if (FirstRow <= other.LastRow && LastRow >= other.FirstRow && FirstColumn <= other.LastColumn)
		{
			return LastColumn >= other.FirstColumn;
		}
		return false;
	}
}
