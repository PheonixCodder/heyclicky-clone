using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkPageView : Grid, IClickyInkClocked
{
	private readonly ClickyInkLiveNote note;

	private readonly int pageIndex;

	private readonly int part;

	private readonly ClickyHomeSpreadPaper paper;

	private readonly ClickyInkActions actions;

	private readonly Func<DateTimeOffset, double>? clockAt;

	private readonly Canvas sheet = new Canvas();

	private readonly StackPanel column = new StackPanel();

	private readonly Grid columnFrame = new Grid();

	private readonly ScaleTransform columnScale = new ScaleTransform();

	private readonly Dictionary<string, FrameworkElement> restingRows = new Dictionary<string, FrameworkElement>();

	private FrameworkElement? heading;

	private string headingKey = "";

	private readonly List<string> rowKeys = new List<string>();

	private ClickyInkLiveNote.Page Page => note.Pages[pageIndex];

	private bool CarriesTitle
	{
		get
		{
			if (pageIndex == 0 && part == 0)
			{
				return (object)note.Title != null;
			}
			return false;
		}
	}

	private double ContentTop => ClickyInkLayout.ContentTopFor(CarriesTitle, part > 0);

	public ClickyInkPageView(ClickyInkLiveNote note, int pageIndex, double clock, ClickyHomeSpreadPaper? paper = null, ClickyInkActions? actions = null, int part = 0, Func<DateTimeOffset, double>? clockAt = null)
	{
		this.note = note;
		this.pageIndex = pageIndex;
		this.part = part;
		this.paper = paper ?? ClickyHomeSpreadPaper.Note;
		this.actions = actions ?? new ClickyInkActions();
		this.clockAt = clockAt;
		base.Width = ClickyInkLayout.PageSize.Width;
		base.Height = ClickyInkLayout.PageSize.Height;
		columnFrame.Children.Add(column);
		columnFrame.RenderTransform = columnScale;
		base.Children.Add(sheet);
		Render(clock);
	}

	private ClickyInkStyle StyleAt(double clock)
	{
		return new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock)).Resting(note, null);
	}

	public void Render(DateTimeOffset now)
	{
		Render(clockAt?.Invoke(now) ?? 0.0);
	}

	public void Invalidate()
	{
		restingRows.Clear();
		headingKey = "";
		rowKeys.Clear();
	}

	public void Render(double clock)
	{
		List<ClickyInkGroup> list = Groups(clock);
		sheet.Children.Clear();
		if (part > 0 && list.Count == 0)
		{
			return;
		}
		ClickyInkStyle clickyInkStyle = StyleAt(clock);
		ClickyInkRowMotion.Table table = ClickyInkRowMotion.TableFor(note);
		double? num = RowClock(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
		{
			"title",
			ClickyInkLiveNote.PageFocusID(Page.Id)
		}), clock, table);
		object obj;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			obj = table.Key + "|" + InkText.DoubleDescription(valueOrDefault);
		}
		else
		{
			obj = "";
		}
		string text = (string)obj;
		if ((object)heading == null || text.Length == 0 || text != headingKey)
		{
			double clock2 = num ?? clock;
			heading = Heading(clock2, (!num.HasValue) ? clickyInkStyle : StyleAt(clock2));
			headingKey = text;
		}
		Canvas.SetLeft(heading, 36.0);
		Canvas.SetTop(heading, 26.0);
		sheet.Children.Add(heading);
		double scale = ClickyInkPagination.Parts(note, pageIndex, paper).Scale;
		double num2 = ClickyInkLayout.PageSize.Height - ContentTop - 26.0;
		columnFrame.Width = 378.0;
		columnFrame.Height = num2 / scale;
		ScaleTransform scaleTransform = columnScale;
		double scaleX = (columnScale.ScaleY = scale);
		scaleTransform.ScaleX = scaleX;
		Grid grid = new Grid
		{
			Width = 378.0,
			Height = num2,
			Clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, 378.0, num2)
			},
			Children = { (UIElement)columnFrame }
		};
		if (columnFrame.Parent is Grid grid2 && grid2 != grid)
		{
			grid2.Children.Clear();
		}
		List<string> keys = new List<string>();
		List<FrameworkElement> list2 = new List<FrameworkElement>();
		foreach (ClickyInkGroup item in list)
		{
			double? num4 = RowClock(item.Blocks.Select((ClickyInkLiveNote.Block b) => b.Id), clock, table);
			object obj2;
			if (num4.HasValue)
			{
				double valueOrDefault2 = num4.GetValueOrDefault();
				obj2 = $"{table.Key}|{item.Id}|{item.Blocks.Count}|{InkText.DoubleDescription(valueOrDefault2)}";
			}
			else
			{
				obj2 = "";
			}
			string text2 = (string)obj2;
			FrameworkElement frameworkElement;
			if (text2.Length > 0 && restingRows.TryGetValue(text2, out FrameworkElement value))
			{
				frameworkElement = value;
			}
			else
			{
				double clock3 = num4 ?? clock;
				ClickyInkGroup clickyInkGroup;
				if (num4.HasValue)
				{
					double valueOrDefault3 = num4.GetValueOrDefault();
					clickyInkGroup = Regrouped(item, valueOrDefault3);
				}
				else
				{
					clickyInkGroup = item;
				}
				ClickyInkGroup clickyInkGroup2 = clickyInkGroup;
				FrameworkElement frameworkElement2 = ClickyInkGroupView.Make(clickyInkGroup2, clock3, (!num4.HasValue) ? clickyInkStyle : StyleAt(clock3), paper, actions);
				frameworkElement2.Margin = new Thickness(0.0, 0.0, 0.0, 14.0);
				frameworkElement = ClickyInkGrow.Around(frameworkElement2, clock3, clickyInkGroup2.LandsAt, clickyInkGroup2.VanishesAt);
				if (text2.Length > 0)
				{
					restingRows[text2] = frameworkElement;
				}
			}
			keys.Add(text2);
			list2.Add(frameworkElement);
		}
		foreach (string item2 in restingRows.Keys.Where((string k) => !keys.Contains(k)).ToList())
		{
			restingRows.Remove(item2);
		}
		for (int i = 0; i < list2.Count; i++)
		{
			if (i < column.Children.Count)
			{
				if ((object)column.Children[i] != list2[i])
				{
					if (list2[i].Parent is Panel panel && panel != column)
					{
						panel.Children.Remove(list2[i]);
					}
					if (column.Children.Contains(list2[i]))
					{
						column.Children.Remove(list2[i]);
					}
					column.Children.Insert(i, list2[i]);
				}
			}
			else
			{
				if (list2[i].Parent is Panel panel2 && panel2 != column)
				{
					panel2.Children.Remove(list2[i]);
				}
				if (column.Children.Contains(list2[i]))
				{
					column.Children.Remove(list2[i]);
				}
				column.Children.Add(list2[i]);
			}
		}
		while (column.Children.Count > list2.Count)
		{
			column.Children.RemoveAt(column.Children.Count - 1);
		}
		rowKeys.Clear();
		rowKeys.AddRange(keys);
		Canvas.SetLeft(grid, 36.0);
		Canvas.SetTop(grid, ContentTop);
		sheet.Children.Add(grid);
	}

	public static double? RowClock(IEnumerable<string> placeIDs, double clock, ClickyInkRowMotion.Table table)
	{
		if (ClickyHomeRendering.IsOffscreen)
		{
			return null;
		}
		double num = double.NegativeInfinity;
		ClickyInkRowMotion value;
		foreach (ClickyInkRowMotion item in placeIDs.Select((string id) => (!table.Places.TryGetValue(id, out value)) ? null : value).Append(table.Switches).Append(table.RestingSwitches))
		{
			if ((object)item == null)
			{
				return null;
			}
			ClickyInkRowMotion.State state = item.StateAt(clock);
			if (!(state is ClickyInkRowMotion.State.Moving))
			{
				if (state is ClickyInkRowMotion.State.Settled settled)
				{
					num = Math.Max(num, settled.At);
				}
				continue;
			}
			return null;
		}
		if (!double.IsFinite(num))
		{
			return null;
		}
		return num;
	}

	private ClickyInkGroup Regrouped(ClickyInkGroup group, double clock)
	{
		return ClickyInkGroup.Groups(Page, clock).FirstOrDefault((ClickyInkGroup g) => g.Id == group.Id) ?? group;
	}

	private FrameworkElement Heading(double clock, ClickyInkStyle style)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			Width = 378.0
		};
		if (CarriesTitle)
		{
			ClickyInkLiveNote.Words title = note.Title;
			if ((object)title != null)
			{
				stackPanel.Children.Add(ClickyInkWordsView.Make(title, clock, style, ClickyPaperFonts.Hand(31.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.6;
				}));
				if (Page.Name.Current.ToLowerInvariant() != "in short")
				{
					stackPanel.Children.Add(ClickyInkWordsView.Make(Page.Name, clock, style, ClickyPaperFonts.Hand(18.0), pencil: true, style.Quiet, null));
				}
				goto IL_0288;
			}
		}
		if (part > 0)
		{
			stackPanel.Children.Add(InkTextView.Plain(Page.Name.Current + ", continued", ClickyPaperFonts.Hand(18.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
		}
		else
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(Page.Name, clock, style, ClickyPaperFonts.Hand(27.0), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkSquiggle(Page.Id.CharacterCount()).Trim(0.0, ClickyInkClock.Progress(clock, Page.Name.FinishedAt, Page.Name.FinishedAt + 0.3)), new PaperStroke(style.Ink.Opacity(0.35), 1.4, PenLineCap.Round));
			clickyShapeView.Width = 70.0;
			clickyShapeView.Height = 6.0;
			clickyShapeView.HorizontalAlignment = HorizontalAlignment.Left;
			stackPanel.Children.Add(clickyShapeView);
		}
		goto IL_0288;
		IL_0288:
		return stackPanel;
	}

	private List<ClickyInkGroup> Groups(double clock)
	{
		List<ClickyInkGroup> list = ClickyInkGroup.Groups(Page, clock);
		ClickyInkPagination.PartsInfo parts = ClickyInkPagination.Parts(note, pageIndex, paper);
		if (parts.Count <= 1)
		{
			return list;
		}
		List<ClickyInkGroup> list2 = new List<ClickyInkGroup>();
		foreach (ClickyInkGroup group in list)
		{
			int value2;
			if (group.Blocks.Count == 1 && parts.ItemPartOf.Keys.Any((string k) => k.StartsWith(group.Blocks[0].Id + "|", StringComparison.Ordinal)))
			{
				ClickyInkLiveNote.Block block = group.Blocks[0].Clone();
				block.Items = group.Blocks[0].Items.Where((ClickyInkLiveNote.Item item) => (parts.ItemPartOf.TryGetValue(ClickyInkPagination.ItemKey(block.Id, item.Id), out var value) ? value : 0) == part).ToList();
				if (block.Items.Count > 0)
				{
					ClickyInkGroup clickyInkGroup = group with
					{
						Blocks = new List<ClickyInkLiveNote.Block> { block }
					};
					list2.Add(clickyInkGroup);
				}
			}
			else if ((parts.PartOf.TryGetValue(group.Blocks[0].Id, out value2) ? value2 : 0) == part)
			{
				list2.Add(group);
			}
		}
		return list2;
	}
}
