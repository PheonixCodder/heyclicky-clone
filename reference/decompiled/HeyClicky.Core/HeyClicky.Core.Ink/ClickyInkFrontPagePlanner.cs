using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace HeyClicky.Core.Ink;

public static class ClickyInkFrontPagePlanner
{
	public sealed record Piece(string GroupID, double Height)
	{
		public List<string>? ItemIDs { get; init; }

		public bool TakesOtherItems { get; init; }

		public HashSet<string> OtherClaimedItemIDs { get; init; } = new HashSet<string>();

		public int Part { get; init; }
	}

	public sealed class Chunk
	{
		public string StoryID { get; }

		public bool IsLead { get; }

		public bool IsContinuation { get; }

		public bool IsLastOfStory { get; set; }

		public List<Piece> Pieces { get; }

		public Chunk(string storyID, bool isLead, bool isContinuation, List<Piece> pieces, bool isLastOfStory = false)
		{
			StoryID = storyID;
			IsLead = isLead;
			IsContinuation = isContinuation;
			IsLastOfStory = isLastOfStory;
			Pieces = pieces;
			base._002Ector();
		}
	}

	public sealed class Column
	{
		public double X { get; }

		public double Top { get; }

		public List<Chunk> Chunks { get; }

		public double Bottom { get; set; }

		public double ExtraGap { get; set; }

		public Column(double x, double top, double bottom)
		{
			X = x;
			Top = top;
			Chunks = new List<Chunk>();
			Bottom = bottom;
			base._002Ector();
		}
	}

	public sealed record Band(string StoryID, string Name, string GroupID, Rect Frame);

	public sealed record Plan
	{
		public ClickyInkFrontPageVariant Variant { get; init; }

		public Size Canvas { get; init; }

		public int Columns { get; init; }

		public double ColumnWidth { get; init; }

		public double InnerWidth { get; init; }

		public Rect Masthead { get; init; }

		public Rect Headline { get; init; }

		public Rect? Stats { get; init; }

		public List<Band> Bands { get; init; } = new List<Band>();

		public Rect? Move { get; init; }

		public List<Column> Flow { get; init; } = new List<Column>();

		public List<Rect> Rules { get; init; } = new List<Rect>();

		public Rect Footer { get; init; }

		public double StoryGap { get; init; }

		public HashSet<string> PlacedGroupIDs { get; init; } = new HashSet<string>();

		public HashSet<string> StatGroupIDs { get; init; } = new HashSet<string>();

		public Dictionary<string, string> StoryOfPage { get; init; } = new Dictionary<string, string>();

		public string LastStoryID { get; init; } = "";

		public List<string> OverflowNames { get; init; } = new List<string>();

		public bool Fits { get; init; } = true;

		public string? LeadFallbackTitle { get; init; }
	}

	private sealed record Story(string Id, string Name, bool IsLead, List<Piece> Pieces);

	public const int MaximumStats = 8;

	public const double PieceGap = 12.0;

	public const double Kicker = 44.0;

	public const double ContinuedLine = 24.0;

	public const double PanelHeader = 30.0;

	public const double BoxInset = 16.0;

	public static readonly Size SpreadSize = new Size(900f, 560f);

	public static int MissionTilesPerRow(int count, double width)
	{
		if (count <= 0)
		{
			return 1;
		}
		int num = Math.Max(1, Math.Min((int)(width / 190.0), 6));
		int num2 = (count + num - 1) / num;
		return Math.Max(1, (count + num2 - 1) / num2);
	}

	public static string Key(ClickyInkLiveNote note, ClickyInkFrontPageVariant variant)
	{
		return $"{variant.RawValue()}|{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Id))}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}";
	}

	private static bool IsListKind(ClickyInkDocument.Kind kind)
	{
		if ((uint)(kind - 4) <= 2u)
		{
			return true;
		}
		return false;
	}

	public static Plan Make(ClickyInkLiveNote note, ClickyInkFrontPageVariant variant, IClickyInkFrontPageMeasurer measure)
	{
		double clock = note.FinishedClock + 30.0;
		ClickyInkDashboardBoard board = new ClickyInkDashboardBoard(note, clock);
		bool isMission = variant == ClickyInkFrontPageVariant.MissionControl;
		HashSet<string> statGroupIDs = new HashSet<string>();
		int statCount = board.Strip.Count;
		List<ClickyInkDashboardBoard.Section> list = board.Sections.Select((ClickyInkDashboardBoard.Section section) => section with
		{
			Groups = section.Groups.Where(delegate(ClickyInkGroup group)
			{
				if (group.Kind != ClickyInkDocument.Kind.Figure || statCount + group.Blocks.Count > 8 || group.Blocks.Any((ClickyInkLiveNote.Block b) => b.IsStruck(clock)))
				{
					return true;
				}
				statCount += group.Blocks.Count;
				statGroupIDs.Add(group.Id);
				return false;
			}).ToList()
		}).ToList();
		List<Story> list2 = new List<Story>();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		List<(string, string, ClickyInkGroup)> list3 = new List<(string, string, ClickyInkGroup)>();
		for (int j = 0; j < list.Count; j++)
		{
			ClickyInkDashboardBoard.Section section2 = list[j];
			if (section2.Groups.Count == 0)
			{
				continue;
			}
			List<Piece> list4 = new List<Piece>();
			foreach (ClickyInkGroup group in section2.Groups)
			{
				ClickyInkLiveNote.Block block = group.Blocks[0];
				if (group.Kind == ClickyInkDocument.Kind.Table && group.Blocks.Count == 1 && (Math.Max(block.Source.Columns.Count, block.Source.Items.Select((ClickyInkDocument.Item i) => i.Cells.Count).DefaultIfEmpty(0).Max()) >= 4 || block.Items.Count >= 6))
				{
					list3.Add((section2.Id, ClickyInkParser.Emphasis.Strip(section2.Name.Current), group));
				}
				else if (group.Blocks.Count == 1 && IsListKind(group.Kind) && block.Items.Count >= 4)
				{
					List<List<ClickyInkLiveNote.Item>> list5 = new List<List<ClickyInkLiveNote.Item>>();
					for (int k = 0; k < block.Items.Count; k += 2)
					{
						list5.Add(block.Items.Skip(k).Take(2).ToList());
					}
					HashSet<string> first = block.Items.Select((ClickyInkLiveNote.Item i) => i.Id).ToHashSet();
					for (int l = 0; l < list5.Count; l++)
					{
						List<ClickyInkLiveNote.Item> list6 = list5[l];
						ClickyInkLiveNote.Block block2 = block.Clone();
						block2.Items = list6;
						if (l > 0)
						{
							block2.Words = ClickyInkLiveNote.Words.Empty;
						}
						bool flag = l == list5.Count - 1;
						HashSet<string> otherClaimedItemIDs = (flag ? first.Except(list6.Select((ClickyInkLiveNote.Item i) => i.Id)).ToHashSet() : new HashSet<string>());
						string id = group.Id;
						ClickyInkGroup clickyInkGroup = group._003CClone_003E_0024();
						int num = 1;
						List<ClickyInkLiveNote.Block> list7 = new List<ClickyInkLiveNote.Block>(num);
						CollectionsMarshal.SetCount(list7, num);
						Span<ClickyInkLiveNote.Block> span = CollectionsMarshal.AsSpan(list7);
						int index = 0;
						span[index] = block2;
						clickyInkGroup.Blocks = list7;
						list4.Add(new Piece(id, Height(clickyInkGroup))
						{
							ItemIDs = list6.Select((ClickyInkLiveNote.Item i) => i.Id).ToList(),
							TakesOtherItems = flag,
							OtherClaimedItemIDs = otherClaimedItemIDs,
							Part = l
						});
					}
				}
				else
				{
					list4.Add(new Piece(group.Id, Height(group)));
				}
			}
			dictionary[section2.Id] = section2.Id;
			if (list4.Count != 0)
			{
				list2.Add(new Story(section2.Id, ClickyInkParser.Emphasis.Strip(section2.Name.Current), j == 0 && list2.Count == 0, list4));
			}
		}
		List<ClickyInkGroup> list8 = board.Move?.Groups ?? new List<ClickyInkGroup>();
		double moveHeight = ((list8.Count == 0) ? 0.0 : (((IEnumerable<ClickyInkGroup>)list8).Sum((Func<ClickyInkGroup, double>)Height) + 44.0 + 32.0 - 12.0));
		Plan result = null;
		List<ClickyInkLiveNote.Block> statBlocks = StatBlocks(board, board.Sections, statGroupIDs);
		for (int m = 2; m <= 5; m++)
		{
			List<(string, string, string, Func<double, double>)> bandTables = list3.Select<(string, string, ClickyInkGroup), (string, string, string, Func<double, double>)>(((string StoryID, string Name, ClickyInkGroup Group) table) => ((string StoryID, string Name, string GroupID, Func<double, double> Height))(StoryID: table.StoryID, Name: table.Name, GroupID: table.Group.Id, Height: (double width) => measure.Table(table.Group, width) + 44.0 + 12.0)).ToList();
			int count = statCount;
			int columns = m;
			ClickyInkDashboardBoard board2 = board;
			int statCount2 = statCount;
			double contentWidth = measure.ContentWidth;
			Func<double, double> measureHeadline = (double width) => measure.Headline(board, width, isMission);
			if ((result = Layout(columns, variant, list2, bandTables, board2, statCount2, moveHeight, contentWidth, measureHeadline, (double width) => (!isMission) ? (measure.Tiles(statBlocks, width) + 18.0) : ((double)((count + MissionTilesPerRow(count, width) - 1) / MissionTilesPerRow(count, width)) * (measure.MissionTileHeight(statBlocks.Select((ClickyInkLiveNote.Block b) => b.Source).ToList()) + 14.0) - 14.0))with
			{
				StatGroupIDs = statGroupIDs,
				StoryOfPage = dictionary
			}).Fits)
			{
				break;
			}
		}
		return result;
		double Height(ClickyInkGroup group)
		{
			return measure.Group(group) + 12.0;
		}
	}

	private static List<ClickyInkLiveNote.Block> StatBlocks(ClickyInkDashboardBoard board, IEnumerable<ClickyInkDashboardBoard.Section> sections, HashSet<string> ids)
	{
		return board.Strip.Concat((from g in sections.SelectMany((ClickyInkDashboardBoard.Section s) => s.Groups)
			where ids.Contains(g.Id)
			select g).SelectMany((ClickyInkGroup g) => g.Blocks)).Take(8).ToList();
	}

	private static double MaxY(Rect rect)
	{
		return rect.Y + rect.Height;
	}

	private static Plan Layout(int columns, ClickyInkFrontPageVariant variant, List<Story> stories, List<(string StoryID, string Name, string GroupID, Func<double, double> Height)> bandTables, ClickyInkDashboardBoard board, int statCount, double moveHeight, double contentWidth, Func<double, double> measureHeadline, Func<double, double> measureStats)
	{
		bool isMission = variant == ClickyInkFrontPageVariant.MissionControl;
		double columnWidth = contentWidth + (isMission ? 32.0 : 0.0);
		double gutter = (isMission ? 18 : 30);
		double margin = (isMission ? 24 : 34);
		double num = (double)columns * columnWidth + (double)(columns - 1) * gutter;
		double num2 = num + 2.0 * margin;
		double num3 = num2 * SpreadSize.Height / SpreadSize.Width;
		double storyGap = (isMission ? 16 : 14);
		double height = (isMission ? 62 : 120);
		Rect rect = new Rect(margin, isMission ? 20 : 22, num, height);
		double y = MaxY(rect) + 16.0;
		bool flag = moveHeight > 0.0;
		bool flag2 = flag && columns == 2;
		int num4 = ((flag && !flag2) ? (columns - 1) : columns);
		double num5 = (double)num4 * columnWidth + (double)(num4 - 1) * gutter;
		double num6 = (((object)board.Answer != null || (object)board.Quiet != null) ? measureHeadline(num5) : 0.0);
		Rect rect2 = new Rect(margin, y, num5, Math.Max(0.0, num6));
		y = MaxY(rect2) + (double)((num6 > 0.0) ? (isMission ? 16 : 18) : 0);
		Rect? stats = null;
		if (statCount > 0)
		{
			double num7 = measureStats(num5);
			stats = new Rect(margin, y, num5, Math.Max(0.0, num7));
			y += num7 + (double)(isMission ? 16 : 14);
		}
		List<Band> list = new List<Band>();
		foreach (var bandTable in bandTables)
		{
			double num8 = bandTable.Height(num5);
			list.Add(new Band(bandTable.StoryID, bandTable.Name, bandTable.GroupID, new Rect(margin, y, num5, Math.Max(0.0, num8))));
			y += num8 + (double)(isMission ? 16 : 10);
		}
		Rect? move = (flag ? new Rect?(new Rect(ColumnX(columns - 1), flag2 ? (y + 6.0) : rect2.Y, columnWidth, moveHeight)) : ((Rect?)null));
		double val = ((board.Sources.Count != 0) ? 30 : 0);
		Rect footer = new Rect(margin, num3 - 18.0 - Math.Max(val, 22.0), num, Math.Max(val, 22.0));
		double limit = footer.Y - 10.0;
		double[] tops = (from _ in Enumerable.Range(0, columns)
			select y + (double)((!isMission) ? 6 : 0)).ToArray();
		if (move.HasValue)
		{
			Rect valueOrDefault = move.GetValueOrDefault();
			tops[columns - 1] = MaxY(valueOrDefault) + storyGap + 4.0;
		}
		double columnHeight = limit - tops.Min();
		Dictionary<string, Story> storyByID = new Dictionary<string, Story>();
		foreach (Story story2 in stories)
		{
			storyByID.TryAdd(story2.Id, story2);
		}
		List<Story> lead = ((stories.Count > 0 && stories[0].IsLead) ? new List<Story> { stories[0] } : new List<Story>());
		List<Story> list2 = stories.Skip(lead.Count).ToList();
		List<List<Story>> orders = new List<List<Story>>();
		if (list2.Count <= 6)
		{
			Permute(new List<Story>(), list2);
		}
		else
		{
			int num9 = 2;
			List<List<Story>> list3 = new List<List<Story>>(num9);
			CollectionsMarshal.SetCount(list3, num9);
			Span<List<Story>> span = CollectionsMarshal.AsSpan(list3);
			int num10 = 0;
			span[num10] = stories;
			num10++;
			ref List<Story> reference = ref span[num10];
			List<Story> list4 = new List<Story>();
			list4.AddRange(lead);
			list4.AddRange(list2.OrderByDescending((Story s) => s.Pieces.Count));
			reference = list4;
			orders = list3;
		}
		((List<Column>, List<Story>), double)? tuple = null;
		foreach (List<Story> item in orders)
		{
			((List<Column>, List<Story>), double)? tuple2 = Best(item);
			if (tuple2.HasValue)
			{
				((List<Column>, List<Story>), double) valueOrDefault2 = tuple2.GetValueOrDefault();
				if (valueOrDefault2.Item2 < (tuple?.Item2 ?? double.PositiveInfinity))
				{
					tuple = valueOrDefault2;
				}
			}
		}
		(List<Column>, List<Story>) tuple3 = tuple?.Item1 ?? Pour(stories, limit, allowOverflow: true) ?? (new List<Column>(), stories);
		var (list5, _) = tuple3;
		foreach (Column item2 in list5)
		{
			int num11 = item2.Chunks.Sum((Chunk c) => c.Pieces.Count) + item2.Chunks.Count;
			if (num11 > 0 && tuple.HasValue)
			{
				item2.ExtraGap = Math.Max(0.0, Math.Min(36.0, (limit - item2.Bottom) / (double)num11));
				item2.Bottom += item2.ExtraGap * (double)num11;
			}
		}
		List<Rect> list6 = new List<Rect>();
		for (int j = 0; j < Math.Max(0, columns - 1); j++)
		{
			Column column2 = ((j < list5.Count) ? list5[j] : null);
			Column column3 = ((j + 1 < list5.Count) ? list5[j + 1] : null);
			double num12 = Math.Min(column2?.Top ?? limit, column3?.Top ?? limit) - 2.0;
			double num13 = Math.Min(column2?.Bottom ?? num12, column3?.Bottom ?? num12);
			if (j == columns - 2 && move.HasValue)
			{
				Rect valueOrDefault3 = move.GetValueOrDefault();
				num13 = Math.Max(num13, Math.Min(MaxY(valueOrDefault3), column2?.Bottom ?? MaxY(valueOrDefault3)));
			}
			if (num13 - num12 > 40.0)
			{
				list6.Add(new Rect(ColumnX(j + 1) - gutter / 2.0, num12, 1.0, num13 - num12));
			}
		}
		HashSet<string> placedGroupIDs = (from p in list5.SelectMany((Column c) => c.Chunks).SelectMany((Chunk c) => c.Pieces)
			select p.GroupID).Concat(list.Select((Band b) => b.GroupID)).ToHashSet();
		Plan obj = new Plan
		{
			Variant = variant,
			Canvas = new Size(num2, num3),
			Columns = columns,
			ColumnWidth = columnWidth,
			InnerWidth = num,
			Masthead = rect,
			Headline = rect2,
			Stats = stats,
			Bands = list,
			Move = move,
			Flow = list5,
			Rules = list6,
			Footer = footer,
			StoryGap = storyGap,
			PlacedGroupIDs = placedGroupIDs
		};
		object lastStoryID;
		if (stories.Count <= 0)
		{
			lastStoryID = "";
		}
		else
		{
			List<Story> list7 = stories;
			lastStoryID = list7[list7.Count - 1].Id;
		}
		obj.LastStoryID = (string)lastStoryID;
		obj.OverflowNames = tuple3.Item2.Select((Story s) => s.Name).ToList();
		int fits;
		if (tuple.HasValue)
		{
			if (move.HasValue)
			{
				Rect valueOrDefault4 = move.GetValueOrDefault();
				fits = ((MaxY(valueOrDefault4) <= limit) ? 1 : 0);
			}
			else
			{
				fits = 1;
			}
		}
		else
		{
			fits = 0;
		}
		obj.Fits = (byte)fits != 0;
		obj.LeadFallbackTitle = null;
		return obj;
		((List<Column> Columns, List<Story> Overflow) Poured, double Score)? Best(List<Story> order)
		{
			if (!Pour(order, limit, allowOverflow: false).HasValue)
			{
				return null;
			}
			double num16 = tops.Min();
			double num17 = limit;
			for (int l = 0; l < 14; l++)
			{
				double num18 = (num16 + num17) / 2.0;
				if (Pour(order, num18, allowOverflow: false).HasValue)
				{
					num17 = num18;
				}
				else
				{
					num16 = num18;
				}
			}
			((List<Column>, List<Story>), double)? tuple5 = null;
			double[] array = new double[5] { 0.0, 0.25, 0.5, 0.75, 1.0 };
			foreach (double num19 in array)
			{
				double num20 = num17 + (limit - num17) * num19;
				(List<Column>, List<Story>)? tuple6 = Pour(order, num20, allowOverflow: false);
				if (tuple6.HasValue)
				{
					(List<Column>, List<Story>) valueOrDefault5 = tuple6.GetValueOrDefault();
					Story value;
					double num21 = (from c in valueOrDefault5.Item1.SelectMany((Column c) => c.Chunks)
						where c.IsContinuation
						select c).Sum((Chunk chunk) => (((storyByID.TryGetValue(chunk.StoryID, out value) ? value : null)?.Pieces.Count ?? 0) > 2 && chunk.Pieces.Count != 1) ? 70.0 : 220.0);
					double num23 = valueOrDefault5.Item1.Sum(delegate(Column column)
					{
						double num22 = column.Chunks.Sum((Chunk c) => c.Pieces.Count) + column.Chunks.Count;
						return Math.Pow(Math.Max(0.0, limit - column.Bottom - Math.Min(36.0 * num22, Math.Max(0.0, limit - column.Bottom))), 2.0);
					});
					int num24 = order.Zip(stories).Count(((Story First, Story Second) pair) => pair.First.Id != pair.Second.Id);
					double num25 = 0.2 * num20 + num21 + 0.0008 * num23 + (double)(6 * num24);
					if (num25 < (tuple5?.Item2 ?? double.PositiveInfinity))
					{
						tuple5 = (valueOrDefault5, num25);
					}
				}
			}
			return tuple5;
		}
		double Chrome(bool isLead, bool isContinuation)
		{
			if (isMission)
			{
				return 48.0;
			}
			if (!isContinuation)
			{
				if (!isLead)
				{
					return 44.0;
				}
				return 0.0;
			}
			return 24.0;
		}
		double ColumnX(int index)
		{
			return margin + (double)index * (columnWidth + gutter);
		}
		void Permute(List<Story> prefix, List<Story> rest)
		{
			if (orders.Count < 720)
			{
				if (rest.Count == 0)
				{
					List<List<Story>> list8 = orders;
					List<Story> list9 = lead;
					int num14 = list9.Count + prefix.Count;
					List<Story> list10 = new List<Story>(num14);
					CollectionsMarshal.SetCount(list10, num14);
					Span<Story> span2 = CollectionsMarshal.AsSpan(list10);
					int num15 = 0;
					Span<Story> span3 = CollectionsMarshal.AsSpan(list9);
					span3.CopyTo(span2.Slice(num15, span3.Length));
					num15 += span3.Length;
					Span<Story> span4 = CollectionsMarshal.AsSpan(prefix);
					span4.CopyTo(span2.Slice(num15, span4.Length));
					num15 += span4.Length;
					list8.Add(list10);
				}
				else
				{
					for (int k = 0; k < rest.Count; k++)
					{
						List<Story> list11 = new List<Story>(rest);
						Story story = list11[k];
						list11.RemoveAt(k);
						List<Story> list10 = prefix;
						int num15 = 1 + list10.Count;
						List<Story> list12 = new List<Story>(num15);
						CollectionsMarshal.SetCount(list12, num15);
						Span<Story> span4 = CollectionsMarshal.AsSpan(list12);
						int num14 = 0;
						Span<Story> span3 = CollectionsMarshal.AsSpan(list10);
						span3.CopyTo(span4.Slice(num14, span3.Length));
						num14 += span3.Length;
						span4[num14] = story;
						Permute(list12, list11);
					}
				}
			}
		}
		(List<Column> Columns, List<Story> Overflow)? Pour(List<Story> order, double bottom, bool allowOverflow)
		{
			List<Column> result = (from i in Enumerable.Range(0, columns)
				select new Column(ColumnX(i), tops[i], tops[i])).ToList();
			int num26 = 0;
			double num27 = tops[0];
			List<Story> list13 = new List<Story>();
			foreach (Story item3 in order)
			{
				bool flag3 = false;
				double total = item3.Pieces.Sum((Piece p) => p.Height) + Chrome(item3.IsLead, isContinuation: false) + storyGap;
				if (!item3.IsLead)
				{
					int num28 = Enumerable.Range(0, num26).FirstOrDefault((int i) => result[i].Bottom + total <= bottom, -1);
					if (num28 >= 0)
					{
						result[num28].Chunks.Add(new Chunk(item3.Id, isLead: false, isContinuation: false, item3.Pieces.ToList(), isLastOfStory: true));
						result[num28].Bottom += total;
						continue;
					}
				}
				if (num27 > tops[num26] + 0.5 && num26 + 1 < columns)
				{
					double num29 = 0.0;
					double num30 = num27 + Chrome(item3.IsLead, isContinuation: false) + storyGap;
					foreach (Piece piece2 in item3.Pieces)
					{
						if (num30 + piece2.Height <= bottom)
						{
							num30 += piece2.Height;
							num29 += piece2.Height;
						}
					}
					if (((total < 0.45 * columnHeight || item3.Pieces.Count <= 2) && num27 + total > bottom) || (num29 < Math.Min(total * 0.35, 180.0) && num27 + total > bottom))
					{
						num26++;
						num27 = tops[num26];
					}
				}
				bool flag4 = false;
				for (int n = 0; n < item3.Pieces.Count; n++)
				{
					Piece piece = item3.Pieces[n];
					bool isContinuation2 = n > 0;
					double num31 = piece.Height + (flag3 ? 0.0 : (Chrome(item3.IsLead, isContinuation2) + storyGap));
					if (num27 + num31 > bottom)
					{
						if (flag3 || num27 > tops[num26] + 0.5)
						{
							if (num26 + 1 >= columns)
							{
								if (allowOverflow)
								{
									list13.Add(item3);
									if (flag3)
									{
										List<Chunk> chunks = result[num26].Chunks;
										chunks[chunks.Count - 1].IsLastOfStory = true;
									}
									flag4 = true;
									break;
								}
								return null;
							}
							num26++;
							num27 = tops[num26];
							flag3 = false;
							num31 = piece.Height + Chrome(item3.IsLead, isContinuation2) + storyGap;
						}
						if (num27 + num31 > bottom && !allowOverflow)
						{
							return null;
						}
					}
					if (!flag3)
					{
						result[num26].Chunks.Add(new Chunk(item3.Id, item3.IsLead, isContinuation2, new List<Piece>()));
						flag3 = true;
					}
					List<Chunk> chunks2 = result[num26].Chunks;
					chunks2[chunks2.Count - 1].Pieces.Add(piece);
					num27 += num31;
					result[num26].Bottom = num27;
				}
				if (!flag4 && flag3)
				{
					List<Chunk> chunks3 = result[num26].Chunks;
					chunks3[chunks3.Count - 1].IsLastOfStory = true;
				}
			}
			return (result, list13);
		}
	}
}
