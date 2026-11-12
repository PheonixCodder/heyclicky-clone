using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Markdown;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerTableView : Grid
{
	private static readonly Color BestRowFill = DS.Rgb(0.3, 0.78, 0.48, 0.14);

	private const double ScrollerHeight = 15.0;

	private readonly MarkdownViewerTableContent content;

	private readonly MessagesPalette look;

	private readonly ScrollViewer scroller;

	private double lastWidth = -1.0;

	private Size measuredSize;

	private static readonly Dictionary<string, double> CellWidthCache = new Dictionary<string, double>();

	private static TextBlock? measuringBlock;

	private Color Plate => look.ComposerFieldFill;

	private Color PlateStroke => look.ComposerFieldStroke;

	private Color HeaderFill => look.RowDivider.WithAlpha(0.55);

	private Color InlineCodeFill => look.RowDivider.WithAlpha(0.75);

	public MarkdownViewerTableView(MarkdownViewerTableContent content, MessagesPalette look)
	{
		this.content = content;
		this.look = look;
		base.Background = Ui.Brush(Plate);
		base.CornerRadius = new CornerRadius(8.0);
		scroller = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled,
			IsVerticalScrollChainingEnabled = true,
			ZoomMode = ZoomMode.Disabled
		};
		base.Children.Add(scroller);
		base.Children.Add(new Border
		{
			BorderBrush = Ui.Brush(PlateStroke),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			IsHitTestVisible = false
		});
		Ui.ClipRounded(this, 8.0);
		AutomationProperties.SetName(this, "Markdown table");
	}

	public Size LayoutTable(double availableWidth)
	{
		double num = Math.Max(1.0, Math.Floor(availableWidth));
		if (num == lastWidth)
		{
			return measuredSize;
		}
		lastWidth = num;
		MarkdownViewerTableLayout markdownViewerTableLayout = MarkdownViewerTableLayout.Compute(content.Headers, content.Rows, Math.Max(1.0, num - 2.0), MeasureCell);
		Grid grid = BuildGrid(content.Headers, content.Rows, markdownViewerTableLayout.ColumnWidths);
		double width = Math.Ceiling(markdownViewerTableLayout.ColumnWidths.Sum());
		grid.Measure(new Size(width, double.PositiveInfinity));
		double num2 = Math.Ceiling(grid.DesiredSize.Height);
		grid.Width = width;
		grid.Height = num2;
		double num3 = (markdownViewerTableLayout.ScrollsHorizontally ? 15.0 : 0.0);
		grid.Margin = new Thickness(0.0, 0.0, 0.0, num3);
		scroller.HorizontalScrollBarVisibility = (markdownViewerTableLayout.ScrollsHorizontally ? ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled);
		scroller.Content = grid;
		measuredSize = new Size(num, num2 + num3 + 2.0);
		base.Width = measuredSize.Width;
		base.Height = measuredSize.Height;
		return measuredSize;
	}

	private static double MeasureCell(string plainText)
	{
		if (CellWidthCache.TryGetValue(plainText, out var value))
		{
			return value;
		}
		if ((object)measuringBlock == null)
		{
			measuringBlock = new TextBlock
			{
				FontSize = 13.0,
				FontFamily = MarkdownViewerFonts.System(13.0),
				IsTextScaleFactorEnabled = false
			};
		}
		measuringBlock.Text = plainText;
		measuringBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		double width = measuringBlock.DesiredSize.Width;
		if (CellWidthCache.Count >= 2000)
		{
			CellWidthCache.Clear();
		}
		CellWidthCache[plainText] = width;
		return width;
	}

	private Grid BuildGrid(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, IReadOnlyList<double> widths)
	{
		Grid grid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		foreach (double width in widths)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(width)
			});
		}
		int gridRow = 0;
		AddRow(headers, isHeader: true);
		AddHairline();
		for (int i = 0; i < rows.Count; i++)
		{
			AddRow(rows[i], isHeader: false);
			if (i < rows.Count - 1)
			{
				AddHairline();
			}
		}
		return grid;
		void AddHairline()
		{
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = new GridLength(1.0)
			});
			Border border3 = new Border
			{
				Background = Ui.Brush(PlateStroke),
				Height = 1.0
			};
			Grid.SetRow(border3, gridRow);
			Grid.SetColumnSpan(border3, Math.Max(1, headers.Count));
			grid.Children.Add(border3);
			gridRow++;
		}
		void AddRow(IReadOnlyList<string> cells, bool isHeader)
		{
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			Color? color = (isHeader ? new Color?(HeaderFill) : (MarkdownViewerTableLayout.IsBestRow(cells) ? new Color?(BestRowFill) : ((Color?)null)));
			if (color.HasValue)
			{
				Color valueOrDefault = color.GetValueOrDefault();
				Border border = new Border
				{
					Background = Ui.Brush(valueOrDefault)
				};
				Grid.SetRow(border, gridRow);
				Grid.SetColumnSpan(border, Math.Max(1, headers.Count));
				grid.Children.Add(border);
			}
			for (int j = 0; j < headers.Count; j++)
			{
				Border border2 = Cell((j < cells.Count) ? cells[j] : "", isHeader);
				if (j < headers.Count - 1)
				{
					border2.BorderBrush = Ui.Brush(PlateStroke);
				}
				border2.BorderThickness = new Thickness(0.0, 0.0, (j < headers.Count - 1) ? 1 : 0, 0.0);
				Grid.SetRow(border2, gridRow);
				Grid.SetColumn(border2, j);
				grid.Children.Add(border2);
			}
			gridRow++;
		}
	}

	private Border Cell(string text, bool isHeader)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = 13.0,
			FontFamily = MarkdownViewerFonts.System(13.0),
			FontWeight = (isHeader ? FontWeights.SemiBold : FontWeights.Normal),
			Foreground = Ui.Brush(isHeader ? look.SecondaryText : look.PrimaryText),
			TextWrapping = TextWrapping.Wrap,
			IsTextSelectionEnabled = true,
			IsTextScaleFactorEnabled = false,
			LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
			LineHeight = Math.Round(18.509, 1)
		};
		List<(int, int)> list = new List<(int, int)>();
		int num = 0;
		foreach (MarkdownInlineRun item2 in MarkdownInlineParser.Parse(text))
		{
			Run run = new Run
			{
				Text = item2.Text
			};
			if (item2.IsCode)
			{
				run.FontFamily = MarkdownViewerFonts.Mono;
				run.FontSize = 12.0;
				list.Add((num, item2.Text.Length));
			}
			if (item2.IsStrong)
			{
				run.FontWeight = FontWeights.SemiBold;
			}
			if (item2.IsEmphasized)
			{
				run.FontStyle = FontStyle.Italic;
			}
			if (item2.IsStrikethrough)
			{
				run.TextDecorations = TextDecorations.Strikethrough;
			}
			string link = item2.Link;
			Uri result;
			Inline item = ((link == null || !Uri.TryCreate(link, UriKind.Absolute, out result)) ? ((Inline)run) : ((Inline)new Hyperlink
			{
				NavigateUri = result,
				Foreground = Ui.Brush(MarkdownViewerPalette.For(look).LinkText),
				Inlines = { (Inline)run }
			}));
			textBlock.Inlines.Add(item);
			num += item2.Text.Length;
		}
		if (list.Count > 0)
		{
			TextHighlighter textHighlighter = new TextHighlighter
			{
				Background = Ui.Brush(InlineCodeFill),
				Foreground = Ui.Brush(look.PrimaryText)
			};
			foreach (var (startIndex, length) in list)
			{
				textHighlighter.Ranges.Add(new TextRange
				{
					StartIndex = startIndex,
					Length = length
				});
			}
			textBlock.TextHighlighters.Add(textHighlighter);
		}
		return new Border
		{
			Padding = new Thickness(12.0, 8.0, 12.0, 8.0),
			Child = textBlock
		};
	}
}
