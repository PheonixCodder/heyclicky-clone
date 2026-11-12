using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HeyClicky.Agents;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkSheetView : Panel, IClickyInkClocked
{
	private sealed record Row(string Id, ClickyInkSheetRowKind Kind, ClickyInkSheetRowInfo Info, bool HasNowLine = false, double? PageSettledAt = null);

	private readonly ClickyInkLiveNote note;

	private readonly ClickyHomeSpreadPaper paper;

	private readonly ClickyInkActions actions;

	private readonly bool isEmbedded;

	private readonly double? followsPenInHeight;

	private readonly double? scrollFollowsPenInHeight;

	private readonly bool? isWorking;

	private readonly double? jobFinishedAt;

	private readonly bool drawsCard;

	private readonly Func<DateTimeOffset, double>? clockAt;

	private readonly ClickyInkEnvironment environment;

	private static readonly HashSet<ClickyInkDocument.Kind> GlanceKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Answer,
		ClickyInkDocument.Kind.Tiles,
		ClickyInkDocument.Kind.Checklist,
		ClickyInkDocument.Kind.Question,
		ClickyInkDocument.Kind.Paragraph,
		ClickyInkDocument.Kind.Status,
		ClickyInkDocument.Kind.Progress,
		ClickyInkDocument.Kind.Run,
		ClickyInkDocument.Kind.Quiet,
		ClickyInkDocument.Kind.Countdown,
		ClickyInkDocument.Kind.Connect,
		ClickyInkDocument.Kind.Spark
	};

	private const int GlanceGroupLimit = 2;

	private readonly List<ClickyInkSheetRowInfo> infos = new List<ClickyInkSheetRowInfo>();

	private double topInset;

	private double bottomInset;

	private double horizontalInset;

	private double windowMargin;

	private double runwayFull = 1.0;

	private double clock;

	private double? cameraHeight;

	private List<ClickyInkLiveNote.PenFocus> focus = new List<ClickyInkLiveNote.PenFocus>();

	private bool isPenWriting;

	private FrameworkElement? background;

	private readonly Dictionary<string, FrameworkElement> keptRows = new Dictionary<string, FrameworkElement>();

	public static bool FreezesRowsAtRest { get; set; } = AgentDefaults.Get<bool?>("ClickyInkSheetFreezesRowsAtRest") ?? true;

	public ClickyInkPenAnchor? PenAnchor { get; private set; }

	public double? FollowOffset { get; private set; }

	public ClickyInkLiveNote Note => note;

	private double ContentWidth => environment.ContentWidth;

	private double SheetWidth => ContentWidth + 2.0 * horizontalInset;

	public event Action<ClickyInkPenAnchor?>? PenAnchorChanged;

	public event Action<double?>? FollowOffsetChanged;

	public ClickyInkSheetView(ClickyInkLiveNote note, double clock, ClickyHomeSpreadPaper? paper = null, ClickyInkActions? actions = null, bool isEmbedded = false, double? followsPenInHeight = null, double? scrollFollowsPenInHeight = null, bool? isWorking = null, double? jobFinishedAt = null, bool drawsCard = true, Func<DateTimeOffset, double>? clockAt = null)
	{
		this.note = note;
		this.paper = paper ?? ClickyHomeSpreadPaper.Note;
		this.actions = actions ?? new ClickyInkActions();
		this.isEmbedded = isEmbedded;
		this.followsPenInHeight = followsPenInHeight;
		this.scrollFollowsPenInHeight = scrollFollowsPenInHeight;
		this.isWorking = isWorking;
		this.jobFinishedAt = jobFinishedAt;
		this.drawsCard = drawsCard;
		this.clockAt = clockAt;
		environment = ClickyInkEnvironment.Current;
		Render(clock);
	}

	public void Render(DateTimeOffset now)
	{
		Render(clockAt?.Invoke(now) ?? clock);
	}

	public void Invalidate()
	{
		keptRows.Clear();
	}

	private new ClickyInkStyle Style(double at)
	{
		return new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(at)).Resting(note, isWorking);
	}

	public void Render(double clock)
	{
		this.clock = clock;
		ClickyInkStyle clickyInkStyle = Style(clock);
		bool isWindowPreview = environment.IsWindowPreview;
		if (!isEmbedded)
		{
			double num = (isWindowPreview ? 8.0 : 28.0);
			double num2 = 30.0;
			topInset = num;
			bottomInset = num2;
		}
		else
		{
			double num2 = 30.0;
			double num = 40.0;
			topInset = num2;
			bottomInset = num;
		}
		double? num3 = followsPenInHeight;
		windowMargin = ((num3.HasValue && !isEmbedded && drawsCard) ? 14 : 0);
		horizontalInset = (isWindowPreview ? 22.0 : 36.0);
		cameraHeight = followsPenInHeight ?? scrollFollowsPenInHeight;
		List<ClickyInkLiveNote.PenFocus> list = ClickyInkSheetCamera.FocusOf(note);
		num3 = cameraHeight;
		focus = ((!num3.HasValue) ? new List<ClickyInkLiveNote.PenFocus>() : list);
		num3 = cameraHeight;
		runwayFull = ((!num3.HasValue) ? 1.0 : RunwayFull(clickyInkStyle));
		ClickyInkRowMotion.Table table = (FreezesRowsAtRest ? ClickyInkRowMotion.TableFor(note) : null);
		List<Row> list2 = Rows(table);
		ClickyInkLiveNote.PenFocus penFocus = focus.LastOrDefault((ClickyInkLiveNote.PenFocus f) => f.At <= clock);
		isPenWriting = focus.Any((ClickyInkLiveNote.PenFocus f) => f.At <= clock) && clock < penFocus.Until;
		HashSet<string> hashSet = (from f in list
			where f.At <= clock + 0.25 && clock < f.Until + 0.4
			select f.PlaceID).ToHashSet();
		List<UIElement> list3 = new List<UIElement>();
		infos.Clear();
		using (ClickyInkEnvironment.Use(environment))
		{
			if (!isEmbedded && drawsCard)
			{
				if ((object)background == null)
				{
					background = CardBackground();
				}
				list3.Add(background);
				infos.Add(new ClickyInkSheetRowInfo.Background());
			}
			HashSet<string> shownKeys = new HashSet<string>();
			foreach (Row item2 in list2)
			{
				bool num4 = PlaceIDs(item2).Any(hashSet.Contains);
				double num5 = (num4 ? clock : (((object)table != null) ? RowClock(item2, table, clickyInkStyle) : clock));
				ClickyInkSheetRowKind kind = ((!item2.PageSettledAt.HasValue) ? item2.Kind.Resolved(num5, clock) : item2.Kind);
				double penClock = (num4 ? clock : 0.0);
				FrameworkElement item;
				if (num5 != clock)
				{
					string text = RowKey(item2, num5, penClock, clickyInkStyle, table);
					shownKeys.Add(text);
					if (!keptRows.TryGetValue(text, out FrameworkElement value))
					{
						value = ClickyInkSheetRowView.Make(kind, num5, clickyInkStyle, paper, actions);
						keptRows[text] = value;
					}
					item = value;
				}
				else
				{
					item = ClickyInkSheetRowView.Make(kind, num5, clickyInkStyle, paper, actions);
				}
				list3.Add(item);
				infos.Add(item2.Info);
			}
			foreach (string item3 in keptRows.Keys.Where((string k) => !shownKeys.Contains(k)).ToList())
			{
				keptRows.Remove(item3);
			}
			if (isWorking == true)
			{
				num3 = clickyInkStyle.RestingSince(clock);
				if (num3.HasValue)
				{
					double valueOrDefault = num3.GetValueOrDefault();
					if (!list2.Any((Row r) => r.HasNowLine))
					{
						list3.Add(ClickyInkRestingPencil.Make(clock, valueOrDefault, clickyInkStyle.Ink));
						infos.Add(new ClickyInkSheetRowInfo.Tail());
					}
				}
			}
		}
		Sync(list3);
		UpdateClip();
		InvalidateMeasure();
		InvalidateArrange();
	}

	private void Sync(List<UIElement> wanted)
	{
		for (int i = 0; i < wanted.Count; i++)
		{
			if (i >= base.Children.Count || (object)base.Children[i] != wanted[i])
			{
				int num = base.Children.IndexOf(wanted[i]);
				if (num >= 0)
				{
					base.Children.RemoveAt(num);
				}
				else if (wanted[i] is FrameworkElement { Parent: Panel parent } && parent != this)
				{
					parent.Children.Remove(wanted[i]);
				}
				base.Children.Insert(Math.Min(i, base.Children.Count), wanted[i]);
			}
		}
		while (base.Children.Count > wanted.Count)
		{
			base.Children.RemoveAt(base.Children.Count - 1);
		}
	}

	private void UpdateClip()
	{
		double? num = followsPenInHeight;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			double sheetWidth = SheetWidth;
			base.Clip = new RectangleGeometry
			{
				Rect = new Rect(-40.0, 0.0, sheetWidth + 80.0, valueOrDefault)
			};
		}
		else
		{
			base.Clip = null;
		}
	}

	private double RunwayFull(ClickyInkStyle style)
	{
		ClickyInkPenSchedule penSchedule = style.PenSchedule;
		if ((object)penSchedule != null)
		{
			double? num = penSchedule.IdleSince(clock);
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				num = jobFinishedAt;
				double num2;
				if (num.HasValue)
				{
					double valueOrDefault2 = num.GetValueOrDefault();
					num2 = valueOrDefault2;
				}
				else if (isWorking == false)
				{
					num2 = valueOrDefault;
				}
				else
				{
					bool? flag = isWorking;
					if (flag.HasValue || penSchedule.LooksWorking(clock))
					{
						return 1.0;
					}
					double? num3 = null;
					foreach (double arrival in penSchedule.Arrivals)
					{
						if (arrival <= clock)
						{
							num3 = arrival;
						}
					}
					num2 = (num3 ?? valueOrDefault) + 120.0;
				}
				if (clock < num2)
				{
					return 1.0;
				}
				double num4 = Math.Max(valueOrDefault, num2) + 0.4;
				return 1.0 - ClickyInkClock.EaseInOut(ClickyInkClock.Progress(clock, num4, num4 + 1.2));
			}
		}
		return 1.0;
	}

	private FrameworkElement CardBackground()
	{
		PaperSurface item = new PaperSurface(ClickyShape.RoundedRectangle(8.0), paper.Fill, 0.45).WithShadow(PaperColor.Black.Opacity(0.25), 10.0, 0.0, 4.0);
		Rectangle item2 = new Rectangle
		{
			Width = 74.0,
			Height = 18.0,
			Fill = Ui.Brush(PaperColor.White.Opacity(0.55)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false
		}.Transformed(0.0, -8.0, -2.0, 1.0, null);
		return new Grid
		{
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)item,
				(UIElement)item2
			}
		};
	}

	private static IReadOnlyList<string> PlaceIDs(Row row)
	{
		if (!(row.Info is ClickyInkSheetRowInfo.Row row2))
		{
			return Array.Empty<string>();
		}
		return row2.PlaceIDs;
	}

	private List<Row> Rows(ClickyInkRowMotion.Table? motion)
	{
		bool isWindowPreview = environment.IsWindowPreview;
		bool isGlancePreview = environment.IsGlancePreview;
		Dictionary<string, double> itemFractions = new Dictionary<string, double>();
		List<Row> list = new List<Row>();
		ClickyInkLiveNote.Words title = note.Title;
		if ((object)title != null && !isEmbedded)
		{
			list.Add(new Row("title", new ClickyInkSheetRowKind.Title(title), new ClickyInkSheetRowInfo.Row(new global::_003C_003Ez__ReadOnlySingleElementList<string>("title"), itemFractions), HasNowLine: false, null));
		}
		bool flag = false;
		for (int j = 0; j < note.Pages.Count; j++)
		{
			ClickyInkLiveNote.Page page = note.Pages[j];
			if (isWindowPreview && flag)
			{
				break;
			}
			double? pageSettledAt = (((object)motion == null) ? ((double?)null) : ClickyInkRowMotion.SettledClock(page, clock, motion));
			List<ClickyInkGroup> list2;
			if ((object)motion != null && pageSettledAt.HasValue)
			{
				double valueOrDefault = pageSettledAt.GetValueOrDefault();
				list2 = ClickyInkRowMotionGroups.Groups(page, valueOrDefault, motion);
			}
			else
			{
				list2 = ClickyInkGroup.Groups(page, clock);
			}
			List<ClickyInkGroup> list3 = list2;
			double num = page.Name.StartsAt ?? page.LandsAt;
			bool flag2 = page.Blocks.Any((ClickyInkLiveNote.Block b) => b.LandsAt > clock && !b.VanishesAt.HasValue);
			if (list3.Count == 0 && !(clock >= num && flag2))
			{
				continue;
			}
			flag = true;
			if (!isWindowPreview && (j > 0 || !ClickyInkLiveNote.IsSilentPageName(page.Name.Current, j) || isEmbedded))
			{
				bool flag3 = page.Blocks.Count > 0 && page.Blocks.All((ClickyInkLiveNote.Block b) => b.VanishesAt.HasValue);
				double landsAt = Math.Min(num, (list3.Count == 0) ? num : list3.Min((ClickyInkGroup g) => g.LandsAt));
				list.Add(new Row(ClickyInkLiveNote.PageFocusID(page.Id), new ClickyInkSheetRowKind.PageName(j, page.Id, page.Name, landsAt, flag3 ? new double?(page.Blocks.Max((ClickyInkLiveNote.Block b) => b.VanishesAt.Value)) : ((double?)null)), new ClickyInkSheetRowInfo.Row(new global::_003C_003Ez__ReadOnlySingleElementList<string>(ClickyInkLiveNote.PageFocusID(page.Id)), itemFractions), HasNowLine: false, null));
			}
			foreach (ClickyInkGroup item in isGlancePreview ? list3.Where((ClickyInkGroup g) => GlanceKinds.Contains(g.Kind)).Take(2).ToList() : list3)
			{
				if (isWindowPreview && item.Kind == ClickyInkDocument.Kind.File)
				{
					continue;
				}
				Dictionary<string, double> dictionary = new Dictionary<string, double>();
				if (item.Blocks.Count == 1 && item.Blocks[0].Items.Count > 0)
				{
					List<ClickyInkLiveNote.Item> items = item.Blocks[0].Items;
					List<double> list4 = items.Select(delegate(ClickyInkLiveNote.Item item)
					{
						double num2 = ClickyInkClock.EaseInOut(ClickyInkClock.Progress(clock, item.LandsAt, item.LandsAt + 0.4));
						double? vanishesAt = item.VanishesAt;
						double num3;
						if (vanishesAt.HasValue)
						{
							double valueOrDefault2 = vanishesAt.GetValueOrDefault();
							num3 = ClickyInkClock.EaseInOut(ClickyInkClock.Progress(clock, valueOrDefault2, valueOrDefault2 + 0.45));
						}
						else
						{
							num3 = 0.0;
						}
						double num4 = num3;
						return num2 * (1.0 - num4);
					}).ToList();
					double num5 = list4.Sum();
					double num6 = 0.0;
					for (int k = 0; k < items.Count; k++)
					{
						num6 += list4[k];
						dictionary[items[k].Id] = ((num5 > 0.0) ? (num6 / num5) : ((double)(k + 1) / (double)items.Count));
					}
				}
				bool hasNowLine = item.Blocks.Any((ClickyInkLiveNote.Block b) => b.Items.Any((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.Now));
				object obj;
				if (!pageSettledAt.HasValue)
				{
					(double, ClickyInkGroup)? tuple = Resized(item, page);
					if (tuple.HasValue)
					{
						(double, ClickyInkGroup) valueOrDefault3 = tuple.GetValueOrDefault();
						obj = new ClickyInkSheetRowKind.Resize(valueOrDefault3.Item1, valueOrDefault3.Item2);
						goto IL_041f;
					}
				}
				obj = null;
				goto IL_041f;
				IL_041f:
				ClickyInkSheetRowKind.Resize resized = (ClickyInkSheetRowKind.Resize)obj;
				list.Add(new Row(item.Id, new ClickyInkSheetRowKind.Group(item, resized), new ClickyInkSheetRowInfo.Row(item.Blocks.Select((ClickyInkLiveNote.Block b) => b.Id).ToList(), dictionary), hasNowLine, pageSettledAt));
			}
		}
		return WithCollapsedPlaces(list);
	}

	private List<Row> WithCollapsedPlaces(List<Row> rows)
	{
		List<Row> rows2 = rows;
		double? num = followsPenInHeight;
		if (!num.HasValue)
		{
			num = scrollFollowsPenInHeight;
			if (!num.HasValue)
			{
				goto IL_0035;
			}
		}
		Dictionary<string, int> rowOfPlace;
		List<string> pending;
		if (rows2.Count != 0)
		{
			rowOfPlace = new Dictionary<string, int>();
			for (int i = 0; i < rows2.Count; i++)
			{
				foreach (string item in PlaceIDs(rows2[i]))
				{
					rowOfPlace[item] = i;
				}
			}
			pending = new List<string>();
			foreach (ClickyInkLiveNote.Page page in note.Pages)
			{
				if (clock < page.LandsAt)
				{
					continue;
				}
				Attach(ClickyInkLiveNote.PageFocusID(page.Id));
				foreach (ClickyInkLiveNote.Block block in page.Blocks)
				{
					if (clock >= block.LandsAt)
					{
						Attach(block.Id);
					}
				}
			}
			if (pending.Count > 0)
			{
				List<Row> list = rows2;
				if (list[list.Count - 1].Info is ClickyInkSheetRowInfo.Row row)
				{
					List<Row> list2;
					int index = (list2 = rows2).Count - 1;
					List<Row> list3 = rows2;
					Row row2 = list3[list3.Count - 1]._003CClone_003E_0024();
					Row row3 = row2;
					ClickyInkSheetRowInfo.Row row4 = row._003CClone_003E_0024();
					ClickyInkSheetRowInfo.Row row5 = row4;
					IReadOnlyList<string> collapsedBelow = row.CollapsedBelow;
					List<string> list4 = pending;
					int num2 = 0;
					string[] array = new string[collapsedBelow.Count + list4.Count];
					foreach (string item2 in collapsedBelow)
					{
						array[num2] = item2;
						num2++;
					}
					Span<string> span = CollectionsMarshal.AsSpan(list4);
					span.CopyTo(new Span<string>(array).Slice(num2, span.Length));
					num2 += span.Length;
					row5.CollapsedBelow = new global::_003C_003Ez__ReadOnlyArray<string>(array);
					row3.Info = row4;
					list2[index] = row2;
				}
			}
			return rows2;
		}
		goto IL_0035;
		IL_0035:
		return rows2;
		void Attach(string placeID)
		{
			if (rowOfPlace.TryGetValue(placeID, out var value))
			{
				if (pending.Count > 0 && rows2[value].Info is ClickyInkSheetRowInfo.Row row6)
				{
					List<Row> list5 = rows2;
					int index2 = value;
					Row row7 = rows2[value]._003CClone_003E_0024();
					Row row8 = row7;
					ClickyInkSheetRowInfo.Row row9 = row6._003CClone_003E_0024();
					ClickyInkSheetRowInfo.Row row10 = row9;
					IReadOnlyList<string> collapsedAbove = row6.CollapsedAbove;
					List<string> list6 = pending;
					int num3 = 0;
					string[] array2 = new string[collapsedAbove.Count + list6.Count];
					foreach (string item3 in collapsedAbove)
					{
						array2[num3] = item3;
						num3++;
					}
					Span<string> span2 = CollectionsMarshal.AsSpan(list6);
					span2.CopyTo(new Span<string>(array2).Slice(num3, span2.Length));
					num3 += span2.Length;
					row10.CollapsedAbove = new global::_003C_003Ez__ReadOnlyArray<string>(array2);
					row8.Info = row9;
					list5[index2] = row7;
				}
				pending = new List<string>();
			}
			else
			{
				pending.Add(placeID);
			}
		}
	}

	private double RowClock(Row row, ClickyInkRowMotion.Table motion, ClickyInkStyle style)
	{
		if (ClickyHomeRendering.IsOffscreen)
		{
			return clock;
		}
		ClickyInkSheetRowKind kind = row.Kind;
		IReadOnlyList<string> readOnlyList;
		if (!(kind is ClickyInkSheetRowKind.Title))
		{
			if (!(kind is ClickyInkSheetRowKind.PageName pageName))
			{
				if (!(kind is ClickyInkSheetRowKind.Group group))
				{
					return clock;
				}
				if ((object)group.Resized != null || group.Value.Kind == ClickyInkDocument.Kind.Countdown)
				{
					return clock;
				}
				ClickyInkDocument.Kind kind2 = group.Value.Kind;
				bool flag = ((kind2 == ClickyInkDocument.Kind.Letter || (uint)(kind2 - 21) <= 1u) ? true : false);
				if (flag && (object)actions.Feedback.Pending != null)
				{
					return clock;
				}
				if (row.HasNowLine && style.RestingSince(clock).HasValue)
				{
					return clock;
				}
				readOnlyList = group.Value.Blocks.Select((ClickyInkLiveNote.Block b) => b.Id).ToList();
			}
			else
			{
				readOnlyList = new global::_003C_003Ez__ReadOnlySingleElementList<string>(ClickyInkLiveNote.PageFocusID(pageName.PageID));
			}
		}
		else
		{
			readOnlyList = new global::_003C_003Ez__ReadOnlySingleElementList<string>("title");
		}
		double? pageSettledAt = row.PageSettledAt;
		ClickyInkRowMotion.State state;
		if (pageSettledAt.HasValue)
		{
			double valueOrDefault = pageSettledAt.GetValueOrDefault();
			if (!row.HasNowLine)
			{
				return valueOrDefault;
			}
			state = motion.RestingSwitches.StateAt(clock);
			if (!(state is ClickyInkRowMotion.State.Moving))
			{
				if (state is ClickyInkRowMotion.State.Settled settled)
				{
					return Math.Max(valueOrDefault, settled.At);
				}
				return valueOrDefault;
			}
			return clock;
		}
		double num = double.NegativeInfinity;
		state = motion.Switches.StateAt(clock);
		if (!(state is ClickyInkRowMotion.State.Moving))
		{
			if (state is ClickyInkRowMotion.State.Settled settled2)
			{
				num = Math.Max(num, settled2.At);
			}
			if (row.HasNowLine)
			{
				state = motion.RestingSwitches.StateAt(clock);
				if (state is ClickyInkRowMotion.State.Moving)
				{
					return clock;
				}
				if (state is ClickyInkRowMotion.State.Settled settled3)
				{
					num = Math.Max(num, settled3.At);
				}
			}
			foreach (string item in readOnlyList)
			{
				if (!motion.Places.TryGetValue(item, out ClickyInkRowMotion value))
				{
					return clock;
				}
				state = value.StateAt(clock);
				if (!(state is ClickyInkRowMotion.State.Moving))
				{
					if (state is ClickyInkRowMotion.State.Settled settled4)
					{
						num = Math.Max(num, settled4.At);
					}
					continue;
				}
				return clock;
			}
			if (!double.IsFinite(num))
			{
				return clock;
			}
			return num;
		}
		return clock;
	}

	private string RowKey(Row row, double rowClock, double penClock, ClickyInkStyle style, ClickyInkRowMotion.Table motion)
	{
		return $"{motion.Key}|{row.Id}|{row.Kind.GetType().Name}|{InkText.DoubleDescription(rowClock)}|{InkText.DoubleDescription(penClock)}|{style.LatestEdit}|{style.IsWorking}|{actions.Feedback.Pending?.SentAt.ToUnixTimeMilliseconds()}|{actions.Feedback.Answered.Count}";
	}

	private (double At, ClickyInkGroup Before)? Resized(ClickyInkGroup group, ClickyInkLiveNote.Page page)
	{
		double? num = null;
		foreach (ClickyInkLiveNote.Block block in group.Blocks)
		{
			List<double> list = new List<double>();
			if (block.ShownSince() > block.LandsAt + 0.001)
			{
				list.Add(block.ShownSince());
			}
			list.AddRange(from i in block.Items
				where i.ShownSince > i.LandsAt + 0.001
				select i.ShownSince);
			foreach (double item in list)
			{
				if (clock >= item && clock < item + 0.4)
				{
					num = Math.Max(num ?? item, item);
				}
			}
		}
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (group.Blocks.Count != 0)
			{
				string firstID = group.Blocks[0].Id;
				ClickyInkGroup clickyInkGroup = ClickyInkGroup.Groups(page, valueOrDefault - 0.001).FirstOrDefault((ClickyInkGroup g) => g.Blocks.Count > 0 && g.Blocks[0].Id == firstID);
				if ((object)clickyInkGroup != null)
				{
					return (valueOrDefault, clickyInkGroup);
				}
				return null;
			}
		}
		return null;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double sheetWidth = SheetWidth;
		List<double> source = StackHeights();
		double num = topInset + source.Sum() + bottomInset;
		double? num2 = cameraHeight;
		double num3;
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			num3 = ClickyInkSheetCamera.Runway(num, valueOrDefault, runwayFull);
		}
		else
		{
			num3 = 0.0;
		}
		double num4 = num3;
		for (int i = 0; i < base.Children.Count && i < infos.Count; i++)
		{
			if (infos[i] is ClickyInkSheetRowInfo.Background)
			{
				base.Children[i].Measure(new Size(sheetWidth, num + num4));
			}
		}
		num2 = followsPenInHeight;
		if (num2.HasValue)
		{
			double valueOrDefault2 = num2.GetValueOrDefault();
			return new Size(sheetWidth, valueOrDefault2);
		}
		return new Size(sheetWidth, num + num4);
	}

	private List<double> StackHeights()
	{
		List<double> list = new List<double>(infos.Count);
		for (int i = 0; i < base.Children.Count && i < infos.Count; i++)
		{
			ClickyInkSheetRowInfo clickyInkSheetRowInfo = infos[i];
			if (!(clickyInkSheetRowInfo is ClickyInkSheetRowInfo.Background))
			{
				if (clickyInkSheetRowInfo is ClickyInkSheetRowInfo.Tail)
				{
					base.Children[i].Measure(new Size(ContentWidth, double.PositiveInfinity));
					list.Add(0.0);
				}
				else
				{
					base.Children[i].Measure(new Size(ContentWidth, double.PositiveInfinity));
					list.Add(base.Children[i].DesiredSize.Height);
				}
			}
			else
			{
				list.Add(0.0);
			}
		}
		return list;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		List<double> list = new List<double>(infos.Count);
		for (int i = 0; i < base.Children.Count && i < infos.Count; i++)
		{
			list.Add((infos[i] is ClickyInkSheetRowInfo.Row) ? base.Children[i].DesiredSize.Height : 0.0);
		}
		double num = topInset + list.Sum() + bottomInset;
		double num2 = 0.0;
		double num3 = 0.0;
		double? num4 = null;
		double? num5 = followsPenInHeight;
		bool flag = num5.HasValue;
		num5 = cameraHeight;
		if (num5.HasValue)
		{
			double valueOrDefault = num5.GetValueOrDefault();
			num3 = ClickyInkSheetCamera.Runway(num, valueOrDefault, runwayFull);
			Dictionary<string, ClickyInkSheetCamera.Place> dictionary = new Dictionary<string, ClickyInkSheetCamera.Place>();
			Dictionary<string, double> itemFractions = new Dictionary<string, double>();
			double num6 = topInset;
			for (int j = 0; j < list.Count; j++)
			{
				if (infos[j] is ClickyInkSheetRowInfo.Row row)
				{
					foreach (string placeID in row.PlaceIDs)
					{
						dictionary[placeID] = new ClickyInkSheetCamera.Place(num6, list[j], row.ItemFractions);
					}
					foreach (string item in row.CollapsedAbove)
					{
						dictionary[item] = new ClickyInkSheetCamera.Place(num6, 0.0, itemFractions);
					}
					foreach (string item2 in row.CollapsedBelow)
					{
						dictionary[item2] = new ClickyInkSheetCamera.Place(num6 + list[j], 0.0, itemFractions);
					}
				}
				num6 += list[j];
			}
			double num7 = (flag ? windowMargin : 0.0);
			num2 = ClickyInkSheetCamera.Offset(focus, dictionary, clock, valueOrDefault, Math.Max(0.0, num7 + num + num3 - valueOrDefault)) - num7;
			num4 = ClickyInkSheetCamera.PenLine(focus, dictionary, clock);
		}
		double num8 = (flag ? num2 : 0.0);
		double num9 = topInset - num8;
		double sheetWidth = SheetWidth;
		for (int k = 0; k < list.Count; k++)
		{
			UIElement uIElement = base.Children[k];
			ClickyInkSheetRowInfo clickyInkSheetRowInfo = infos[k];
			if (!(clickyInkSheetRowInfo is ClickyInkSheetRowInfo.Background))
			{
				if (clickyInkSheetRowInfo is ClickyInkSheetRowInfo.Tail)
				{
					uIElement.Arrange(new Rect(horizontalInset, num9, Math.Min(ContentWidth, uIElement.DesiredSize.Width), uIElement.DesiredSize.Height));
					continue;
				}
				uIElement.Arrange(new Rect(horizontalInset, num9, ContentWidth, list[k]));
				num9 += list[k];
			}
			else
			{
				uIElement.Arrange(new Rect(0.0, 0.0 - num8, sheetWidth, num + num3));
			}
		}
		num5 = cameraHeight;
		ClickyInkPenAnchor? clickyInkPenAnchor = ((!num5.HasValue) ? ((ClickyInkPenAnchor?)null) : new ClickyInkPenAnchor?(new ClickyInkPenAnchor(num4.GetValueOrDefault() - num8, isPenWriting)));
		num5 = scrollFollowsPenInHeight;
		double? num10 = ((!num5.HasValue) ? ((double?)null) : new double?(num2));
		if (clickyInkPenAnchor != PenAnchor)
		{
			PenAnchor = clickyInkPenAnchor;
			this.PenAnchorChanged?.Invoke(clickyInkPenAnchor);
		}
		if (num10 != FollowOffset)
		{
			FollowOffset = num10;
			this.FollowOffsetChanged?.Invoke(num10);
		}
		return finalSize;
	}
}
