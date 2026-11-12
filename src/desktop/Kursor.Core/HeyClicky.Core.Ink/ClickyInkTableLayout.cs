using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkTableLayout(IReadOnlyList<double> Widths, IReadOnlyList<bool> Rigid, double FontScale)
{
	public delegate double Measurer(string text, ClickyInkTableFont font, double scale);

	private static readonly Dictionary<string, ClickyInkTableLayout> Made = new Dictionary<string, ClickyInkTableLayout>();

	public static bool IsShortValue(string cell)
	{
		string text = ClickyInkParser.Emphasis.Strip(cell).TrimWs();
		int num = text.CharacterCount();
		if (num > 10)
		{
			if (num <= 16)
			{
				return ClickyInkNumber.Parse(text).HasValue;
			}
			return false;
		}
		return true;
	}

	public static ClickyInkTableLayout Make(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, double width, Measurer measure)
	{
		string key = string.Join('\u001f', columns) + "\u001e" + string.Join('\u001e', rows.Select((IReadOnlyList<string> r) => string.Join('\u001f', r))) + "\u001d" + InkText.DoubleDescription(width);
		lock (Made)
		{
			if (Made.TryGetValue(key, out ClickyInkTableLayout value))
			{
				return value;
			}
		}
		ClickyInkTableLayout clickyInkTableLayout = Measured(columns, rows, width, measure);
		lock (Made)
		{
			if (Made.Count > 64)
			{
				Made.Clear();
			}
			Made[key] = clickyInkTableLayout;
			return clickyInkTableLayout;
		}
	}

	public bool Equals(ClickyInkTableLayout? other)
	{
		if ((object)other != null && Widths.SequenceEqual(other.Widths) && Rigid.SequenceEqual(other.Rigid))
		{
			return FontScale == other.FontScale;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Widths.Count, FontScale);
	}

	private static ClickyInkTableLayout Measured(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, double width, Measurer measureFont)
	{
		int num = Math.Max(Math.Max(columns.Count, (rows.Count == 0) ? 1 : rows.Max((IReadOnlyList<string> r) => r.Count)), 1);
		List<bool> rigid = Enumerable.Range(0, num).Select(delegate(int column)
		{
			if (column <= 0)
			{
				return false;
			}
			List<string> list = rows.Select((IReadOnlyList<string> r) => (column >= r.Count) ? "" : r[column]).ToList();
			return list.Count > 0 && list.All(IsShortValue);
		}).ToList();
		ClickyInkTableLayout clickyInkTableLayout = null;
		double[] array = new double[6] { 1.0, 0.94, 0.88, 0.82, 0.76, 0.7 };
		foreach (double scale2 in array)
		{
			List<double> list2 = new List<double>();
			List<double> minimum = new List<double>();
			for (int k = 0; k < num; k++)
			{
				ClickyInkTableFont font2 = ((k == 0) ? ClickyInkTableFont.Name : ClickyInkTableFont.Cell);
				List<string> list3 = rows.Select((IReadOnlyList<string> r) => (k >= r.Count) ? "" : r[k]).ToList();
				string obj = ((k < columns.Count) ? columns[k].ToUpperInvariant() : "");
				double val = ((list3.Count == 0) ? 0.0 : list3.Max((string c) => Measure(c, font2, scale2)));
				string[] array2 = obj.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				double val2 = ((array2.Length == 0) ? 0.0 : array2.Max((string w) => Measure(w, ClickyInkTableFont.Header, scale2, 0.4)));
				List<string> list4 = list3.SelectMany((string c) => c.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
				double val3 = ((list4.Count == 0) ? 0.0 : list4.Max((string w) => Measure(w, font2, scale2)));
				if (rigid[k])
				{
					list2.Add(Math.Max(val, val2) + 10.0);
					minimum.Add(list2[k]);
				}
				else
				{
					list2.Add(Math.Max(Math.Max(Math.Min(val, 200.0 * scale2), val2), 40.0) + 10.0);
					minimum.Add(Math.Max(val3, val2) + 10.0);
				}
			}
			double rigidTotal = list2.Where((double _, int i) => rigid[i]).Sum();
			double num2 = minimum.Where((double _, int i) => !rigid[i]).Sum();
			if (!(rigidTotal + num2 <= width))
			{
				double total = minimum.Sum();
				clickyInkTableLayout = new ClickyInkTableLayout(minimum.Select((double m) => m / Math.Max(total, 1.0) * width).ToList(), rigid, scale2);
				continue;
			}
			double available = width - rigidTotal;
			double flexibleNatural = list2.Where((double _, int i) => !rigid[i]).Sum();
			if (!(flexibleNatural > 0.0))
			{
				return new ClickyInkTableLayout(list2.Select((double n) => n / rigidTotal * width).ToList(), rigid, scale2);
			}
			if (rigidTotal + flexibleNatural <= width)
			{
				return new ClickyInkTableLayout(list2.Select((double n) => n / (rigidTotal + flexibleNatural) * width).ToList(), rigid, scale2);
			}
			List<double> widths = list2.Select((double columnWidth, int column) => (!rigid[column]) ? (columnWidth / flexibleNatural * available) : columnWidth).ToList();
			for (int l = 0; l < 4; l++)
			{
				List<int> shortColumns = (from i in Enumerable.Range(0, widths.Count)
					where !rigid[i] && widths[i] < minimum[i] - 0.5
					select i).ToList();
				if (shortColumns.Count == 0)
				{
					break;
				}
				double num3 = shortColumns.Sum((int i) => minimum[i] - widths[i]);
				foreach (int item in shortColumns)
				{
					widths[item] = minimum[item];
				}
				List<int> list5 = (from i in Enumerable.Range(0, widths.Count)
					where !rigid[i] && !shortColumns.Contains(i)
					select i).ToList();
				double num4 = list5.Sum((int i) => widths[i] - minimum[i]);
				if (!(num4 > 0.0))
				{
					break;
				}
				Dictionary<int, double> dictionary = list5.ToDictionary((int i) => i, (int i) => widths[i]);
				foreach (int item2 in list5)
				{
					widths[item2] -= num3 * (dictionary[item2] - minimum[item2]) / num4;
				}
			}
			return new ClickyInkTableLayout(widths, rigid, scale2);
		}
		return clickyInkTableLayout ?? new ClickyInkTableLayout(Enumerable.Repeat(width / (double)num, num).ToList(), rigid, 0.7);
		double Measure(string text, ClickyInkTableFont font, double scale, double tracking = 0.0)
		{
			string text2 = ClickyInkParser.Emphasis.Strip(text);
			return Math.Ceiling(measureFont(text2, font, scale) + tracking * (double)text2.CharacterCount());
		}
	}

	[CompilerGenerated]
	private ClickyInkTableLayout(ClickyInkTableLayout original)
	{
		Widths = original.Widths;
		Rigid = original.Rigid;
		FontScale = original.FontScale;
	}
}
