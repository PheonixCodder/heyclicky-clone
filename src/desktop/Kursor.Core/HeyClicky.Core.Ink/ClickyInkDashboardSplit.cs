using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDashboardSplit
{
	public abstract record Row
	{
		public sealed record Columns(List<string> Left, List<string> Right, bool HasMove, int MoveOnRight = int.MaxValue) : Row()
		{
			public List<string> Left { get; set; } = Left;

			public List<string> Right { get; set; } = Right;

			internal Columns _003CClone_003E_0024() => this with { };
		}

		public sealed record Wide(string Id) : Row();
	}

	public abstract record Placed
	{
		public sealed record Columns(List<ClickyInkDashboardBoard.Section> Left, List<ClickyInkDashboardBoard.Section> Right, bool HasMove, int MoveOnRight) : Placed();

		public sealed record Wide(ClickyInkDashboardBoard.Section Section) : Placed();
	}

	public sealed record Rows(List<Row> Items)
	{
		public List<Placed> Placing(IReadOnlyList<ClickyInkDashboardBoard.Section> sections)
		{
			Dictionary<string, ClickyInkDashboardBoard.Section> byID = new Dictionary<string, ClickyInkDashboardBoard.Section>();
			foreach (ClickyInkDashboardBoard.Section section in sections)
			{
				byID.TryAdd(section.Id, section);
			}
			HashSet<string> known = new HashSet<string>(Items.SelectMany(delegate(Row row)
			{
				if (row is Row.Columns columns)
				{
					return columns.Left.Concat(columns.Right);
				}
				return (row is Row.Wide wide) ? ((IEnumerable<string>)new _003C_003Ez__ReadOnlySingleElementList<string>(wide.Id)) : ((IEnumerable<string>)Array.Empty<string>());
			}));
			List<ClickyInkDashboardBoard.Section> list = sections.Where((ClickyInkDashboardBoard.Section s) => !known.Contains(s.Id)).ToList();
			List<Placed> list2 = new List<Placed>();
			foreach (Row item in Items)
			{
				if (!(item is Row.Columns columns2))
				{
					if (item is Row.Wide wide2 && byID.TryGetValue(wide2.Id, out ClickyInkDashboardBoard.Section value))
					{
						list2.Add(new Placed.Wide(value));
					}
					continue;
				}
				List<ClickyInkDashboardBoard.Section> left = (from id in columns2.Left.Where(byID.ContainsKey)
					select byID[id]).Concat(list).ToList();
				list = new List<ClickyInkDashboardBoard.Section>();
				list2.Add(new Placed.Columns(left, (from id in columns2.Right.Where(byID.ContainsKey)
					select byID[id]).ToList(), columns2.HasMove, columns2.MoveOnRight));
			}
			if (list.Count > 0)
			{
				list2.Add(new Placed.Columns(list, new List<ClickyInkDashboardBoard.Section>(), HasMove: false, int.MaxValue));
			}
			return list2;
		}
	}

	public static bool IsWide(ClickyInkDashboardBoard.Section section)
	{
		return section.Groups.Any((ClickyInkGroup group) => group.Kind == ClickyInkDocument.Kind.Table && Math.Max(group.Blocks[0].Source.Columns.Count, group.Blocks[0].Source.Items.Select((ClickyInkDocument.Item i) => i.Cells.Count).DefaultIfEmpty(0).Max()) >= 4);
	}

	public static Rows Plan(ClickyInkLiveNote note, Func<ClickyInkGroup, double> groupHeight)
	{
		double clock = note.FinishedClock + 30.0;
		ClickyInkDashboardBoard board = new ClickyInkDashboardBoard(note, clock);
		ClickyInkDashboardBoard.Section move = board.Move;
		double moveHeight = (((object)move != null) ? Height(move.Groups) : 0.0);
		List<Row> rows = new List<Row>();
		List<ClickyInkDashboardBoard.Section> run = new List<ClickyInkDashboardBoard.Section>();
		bool moveIsPlaced = false;
		Dictionary<int, (double Left, double Right)> columnHeights = new Dictionary<int, (double, double)>();
		Dictionary<string, double> sectionHeights = new Dictionary<string, double>();
		foreach (ClickyInkDashboardBoard.Section section in board.Sections)
		{
			if (!sectionHeights.ContainsKey(section.Id))
			{
				sectionHeights[section.Id] = Height(section.Groups);
			}
		}
		double boardHeight = sectionHeights.Values.Sum() + moveHeight;
		foreach (ClickyInkDashboardBoard.Section section2 in board.Sections)
		{
			if (IsWide(section2) || IsTall(section2))
			{
				CloseRun();
				rows.Add(new Row.Wide(section2.Id));
			}
			else
			{
				run.Add(section2);
			}
		}
		CloseRun();
		return new Rows(rows);
		void CloseRun()
		{
			bool flag = !moveIsPlaced && (object)board.Move != null;
			if (run.Count != 0 || flag)
			{
				List<double> list = run.Select((ClickyInkDashboardBoard.Section s) => Height(s.Groups)).ToList();
				if (run.Count == 1 && !flag && list[0] < 420.0)
				{
					int num2 = rows.FindLastIndex((Row r) => r is Row.Columns);
					if (num2 >= 0 && rows[num2] is Row.Columns columns && columnHeights.TryGetValue(num2, out (double, double) value2))
					{
						List<Row> list2 = rows;
						Row.Columns columns2;
						if (!(value2.Item1 <= value2.Item2))
						{
							columns2 = columns._003CClone_003E_0024();
							Row.Columns obj = columns2;
							List<string> right = columns.Right;
							int num3 = 1 + right.Count;
							List<string> list3 = new List<string>(num3);
							CollectionsMarshal.SetCount(list3, num3);
							Span<string> span = CollectionsMarshal.AsSpan(list3);
							int num4 = 0;
							Span<string> span2 = CollectionsMarshal.AsSpan(right);
							span2.CopyTo(span.Slice(num4, span2.Length));
							num4 += span2.Length;
							span[num4] = run[0].Id;
							obj.Right = list3;
						}
						else
						{
							columns2 = columns._003CClone_003E_0024();
							Row.Columns obj2 = columns2;
							List<string> right = columns.Left;
							int num4 = 1 + right.Count;
							List<string> list4 = new List<string>(num4);
							CollectionsMarshal.SetCount(list4, num4);
							Span<string> span2 = CollectionsMarshal.AsSpan(list4);
							int num3 = 0;
							Span<string> span = CollectionsMarshal.AsSpan(right);
							span.CopyTo(span2.Slice(num3, span.Length));
							num3 += span.Length;
							span2[num3] = run[0].Id;
							obj2.Left = list4;
						}
						list2[num2] = columns2;
						columnHeights[num2] = ((value2.Item1 <= value2.Item2) ? (value2.Item1 + list[0], value2.Item2) : (value2.Item1, value2.Item2 + list[0]));
						run = new List<ClickyInkDashboardBoard.Section>();
						return;
					}
				}
				double num5 = list.Sum();
				double num6 = (flag ? moveHeight : 0.0);
				(int, double) tuple = ((run.Count != 0) ? 1 : 0, double.PositiveInfinity);
				for (int i = 0; i <= run.Count; i++)
				{
					if (i != 0 || flag || run.Count <= 0)
					{
						double num7 = list.Take(i).Sum();
						double num8 = Math.Abs(num7 - (num6 + num5 - num7));
						if (num8 < tuple.Item2)
						{
							tuple = (i, num8);
						}
					}
				}
				double num9 = list.Take(tuple.Item1).Sum();
				double num10 = num6 + num5 - num9;
				int moveOnRight = int.MaxValue;
				if (flag)
				{
					ClickyInkDashboardBoard.Section move2 = board.Move;
					if ((object)move2 != null && move2.Groups.Count > 1 && num10 > num9 * 1.5)
					{
						List<double> list5 = move2.Groups.Select((ClickyInkGroup g) => Height(new _003C_003Ez__ReadOnlySingleElementList<ClickyInkGroup>(g)) - 40.0).ToList();
						int num11 = move2.Groups.Count;
						while (num11 > 1 && num10 - list5[num11 - 1] >= num9 + list5[num11 - 1] - 60.0)
						{
							num11--;
							num10 -= list5[num11];
							num9 += list5[num11];
						}
						if (num11 < move2.Groups.Count)
						{
							moveOnRight = num11;
						}
					}
				}
				rows.Add(new Row.Columns((from s in run.Take(tuple.Item1)
					select s.Id).ToList(), (from s in run.Skip(tuple.Item1)
					select s.Id).ToList(), flag, moveOnRight));
				columnHeights[rows.Count - 1] = (num9, num10);
				if (flag)
				{
					moveIsPlaced = true;
				}
				run = new List<ClickyInkDashboardBoard.Section>();
			}
		}
		double Height(IEnumerable<ClickyInkGroup> groups)
		{
			return groups.Sum(groupHeight) + 40.0;
		}
		bool IsTall(ClickyInkDashboardBoard.Section section)
		{
			double value;
			double num = (sectionHeights.TryGetValue(section.Id, out value) ? value : 0.0);
			if (section.Groups.Count <= 1)
			{
				ClickyInkGroup clickyInkGroup = section.Groups.FirstOrDefault();
				if ((object)clickyInkGroup == null || !IsListKind(clickyInkGroup.Kind) || clickyInkGroup.Blocks[0].Items.Count < 4)
				{
					goto IL_0090;
				}
			}
			if (num > 480.0)
			{
				return num > 0.55 * boardHeight;
			}
			goto IL_0090;
			IL_0090:
			return false;
		}
	}

	public static string Key(ClickyInkLiveNote note)
	{
		return $"{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Id))}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}";
	}

	private static bool IsListKind(ClickyInkDocument.Kind kind)
	{
		if ((uint)(kind - 4) <= 2u)
		{
			return true;
		}
		return false;
	}

	public static List<ClickyInkGroup> SpreadingLongList(List<ClickyInkGroup> groups)
	{
		if (groups.Count == 1)
		{
			ClickyInkGroup clickyInkGroup = groups[0];
			if ((object)clickyInkGroup != null && IsListKind(clickyInkGroup.Kind) && clickyInkGroup.Blocks[0].Items.Count >= 4)
			{
				ClickyInkLiveNote.Block block = clickyInkGroup.Blocks[0];
				int count = (block.Items.Count + 1) / 2;
				ClickyInkLiveNote.Block block2 = block.Clone();
				block2.Items = block.Items.Take(count).ToList();
				ClickyInkLiveNote.Block block3 = block.Clone();
				block3.Items = block.Items.Skip(count).ToList();
				block3.Id = block.Id + "-second-half";
				block3.Words = ClickyInkLiveNote.Words.Empty;
				int num = 2;
				List<ClickyInkGroup> list = new List<ClickyInkGroup>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<ClickyInkGroup> span = CollectionsMarshal.AsSpan(list);
				int num2 = 0;
				ref ClickyInkGroup reference = ref span[num2];
				ClickyInkDocument.Kind kind = clickyInkGroup.Kind;
				int num3 = 1;
				List<ClickyInkLiveNote.Block> list2 = new List<ClickyInkLiveNote.Block>(num3);
				CollectionsMarshal.SetCount(list2, num3);
				Span<ClickyInkLiveNote.Block> span2 = CollectionsMarshal.AsSpan(list2);
				int index = 0;
				span2[index] = block2;
				reference = new ClickyInkGroup(kind, list2);
				num2++;
				ref ClickyInkGroup reference2 = ref span[num2];
				ClickyInkDocument.Kind kind2 = clickyInkGroup.Kind;
				index = 1;
				List<ClickyInkLiveNote.Block> list3 = new List<ClickyInkLiveNote.Block>(index);
				CollectionsMarshal.SetCount(list3, index);
				span2 = CollectionsMarshal.AsSpan(list3);
				num3 = 0;
				span2[num3] = block3;
				reference2 = new ClickyInkGroup(kind2, list3);
				return list;
			}
		}
		return groups;
	}
}
