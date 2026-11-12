using System;
using System.Globalization;

namespace HeyClicky.Core.Documents.Spreadsheet;

public readonly record struct SpreadsheetCellAddress(int Row, int Column)
{
	public string Reference => ColumnLetters(Column) + (Row + 1).ToString(CultureInfo.InvariantCulture);

	public static string ColumnLetters(int column)
	{
		int num = column + 1;
		string text = "";
		while (num > 0)
		{
			int num2 = (num - 1) % 26;
			text = (char)(65 + num2) + text;
			num = (num - 1) / 26;
		}
		return text;
	}

	public static SpreadsheetCellAddress? Parse(ReadOnlySpan<char> reference)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool flag2 = false;
		ReadOnlySpan<char> readOnlySpan = reference;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			switch (c)
			{
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				if (flag2)
				{
					return null;
				}
				num = num * 26 + (c - 64);
				flag = true;
				break;
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				if (flag2)
				{
					return null;
				}
				num = num * 26 + (c - 96);
				flag = true;
				break;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				num2 = num2 * 10 + (c - 48);
				flag2 = true;
				break;
			default:
				return null;
			case '$':
				break;
			}
		}
		if (!flag || !flag2 || num2 <= 0)
		{
			return null;
		}
		return new SpreadsheetCellAddress(num2 - 1, num - 1);
	}
}
