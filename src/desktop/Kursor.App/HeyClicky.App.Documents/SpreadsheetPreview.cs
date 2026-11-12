using System;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Documents;

public sealed class SpreadsheetPreview : Grid
{
	private readonly TextBlock nameBox;

	private readonly TextBlock formula;

	private readonly Grid linkSlot = new Grid();

	private readonly StackPanel tabs = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 2.0,
		Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Border statisticsPill;

	private readonly TextBlock statistics;

	public SpreadsheetWorkbook Workbook { get; }

	public SpreadsheetGridView GridView { get; } = new SpreadsheetGridView();

	public int SelectedSheetIndex { get; private set; }

	private SpreadsheetSheet CurrentSheet => Workbook.Sheets[Math.Min(SelectedSheetIndex, Workbook.Sheets.Count - 1)];

	public SpreadsheetPreview(SpreadsheetWorkbook workbook)
	{
		Workbook = workbook;
		SelectedSheetIndex = workbook.InitialSheetIndex;
		base.RequestedTheme = ElementTheme.Light;
		base.Padding = new Thickness(10.0);
		Grid card = new Grid
		{
			Background = Ui.Brush(SpreadsheetChromePalette.Card)
		};
		GridLength[] array = new GridLength[5]
		{
			new GridLength(32.0),
			new GridLength(1.0),
			new GridLength(1.0, GridUnitType.Star),
			new GridLength(1.0),
			new GridLength(38.0)
		};
		foreach (GridLength height in array)
		{
			card.RowDefinitions.Add(new RowDefinition
			{
				Height = height
			});
		}
		nameBox = Ui.Text("", 12.0, FontWeights.Medium, SpreadsheetChromePalette.PrimaryText, TextAlignment.Left, 1);
		nameBox.Width = 64.0;
		nameBox.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		nameBox.VerticalAlignment = VerticalAlignment.Center;
		TextBlock element = new TextBlock
		{
			Text = "fx",
			FontSize = 12.5,
			FontWeight = FontWeights.Medium,
			FontStyle = FontStyle.Italic,
			FontFamily = new FontFamily("Cambria, Georgia, Times New Roman"),
			Foreground = Ui.Brush(SpreadsheetChromePalette.TertiaryText),
			Margin = new Thickness(10.0, 0.0, 10.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			IsTextScaleFactorEnabled = false
		};
		formula = Ui.Text("", 12.5, FontWeights.Normal, SpreadsheetChromePalette.PrimaryText, TextAlignment.Left, 1);
		formula.VerticalAlignment = VerticalAlignment.Center;
		formula.IsTextSelectionEnabled = true;
		Grid grid = new Grid
		{
			Height = 32.0,
			Background = Ui.Brush(SpreadsheetChromePalette.Card)
		};
		array = new GridLength[5]
		{
			GridLength.Auto,
			GridLength.Auto,
			GridLength.Auto,
			new GridLength(1.0, GridUnitType.Star),
			GridLength.Auto
		};
		foreach (GridLength width in array)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = width
			});
		}
		Border element2 = new Border
		{
			Width = 1.0,
			Height = 18.0,
			Background = Ui.Brush(SpreadsheetChromePalette.Line),
			VerticalAlignment = VerticalAlignment.Center
		};
		Place(grid, nameBox, 0);
		Place(grid, element2, 1);
		Place(grid, element, 2);
		Place(grid, formula, 3);
		linkSlot.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		linkSlot.VerticalAlignment = VerticalAlignment.Center;
		Place(grid, linkSlot, 4);
		Grid.SetRow(grid, 0);
		card.Children.Add(grid);
		card.Children.Add(Rule(1));
		Grid.SetRow(GridView, 2);
		card.Children.Add(GridView);
		card.Children.Add(Rule(3));
		Grid grid2 = new Grid
		{
			Height = 38.0,
			Background = Ui.Brush(SpreadsheetChromePalette.BarFill),
			ColumnSpacing = 8.0
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		ScrollViewer item = new ScrollViewer
		{
			Content = tabs,
			HorizontalScrollMode = ScrollMode.Enabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollMode = ScrollMode.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			IsTabStop = false,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		grid2.Children.Add(item);
		statistics = Ui.Text("", 11.5, FontWeights.Medium, SpreadsheetChromePalette.SecondaryText, TextAlignment.Left, 1);
		statistics.IsTextSelectionEnabled = true;
		statistics.VerticalAlignment = VerticalAlignment.Center;
		Typography.SetNumeralAlignment(statistics, FontNumeralAlignment.Tabular);
		statisticsPill = new Border
		{
			Child = statistics,
			Height = 24.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			CornerRadius = new CornerRadius(6.0),
			Background = Ui.Brush(Colors.White),
			BorderBrush = Ui.Brush(SpreadsheetChromePalette.Line),
			BorderThickness = new Thickness(1.0),
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		Grid.SetColumn(statisticsPill, 1);
		grid2.Children.Add(statisticsPill);
		Grid.SetRow(grid2, 4);
		card.Children.Add(grid2);
		Border border = new Border
		{
			CornerRadius = new CornerRadius(10.0),
			BorderBrush = Ui.Brush(DS.Black(0.08)),
			BorderThickness = new Thickness(1.0),
			IsHitTestVisible = false
		};
		Grid.SetRowSpan(border, 5);
		card.Children.Add(border);
		card.Loaded += delegate
		{
			Ui.ClipRounded(card, 10.0);
		};
		base.Children.Add(card);
		GridView.SelectionChanged += ShowSummary;
		GridView.OpenHyperlink += OpenLink;
		BuildTabs();
		GridView.Show(CurrentSheet);
	}

	private static void Place(Grid grid, FrameworkElement element, int column)
	{
		Grid.SetColumn(element, column);
		grid.Children.Add(element);
	}

	private static Border Rule(int row)
	{
		Border obj = new Border
		{
			Height = 1.0,
			Background = Ui.Brush(SpreadsheetChromePalette.Line)
		};
		Grid.SetRow(obj, row);
		return obj;
	}

	public void SelectSheet(int index)
	{
		if (index >= 0 && index < Workbook.Sheets.Count && index != SelectedSheetIndex)
		{
			SelectedSheetIndex = index;
			BuildTabs();
			GridView.Show(CurrentSheet);
		}
	}

	private void BuildTabs()
	{
		tabs.Children.Clear();
		for (int i = 0; i < Workbook.Sheets.Count; i++)
		{
			int i2 = i;
			tabs.Children.Add(SheetTab(Workbook.Sheets[i].Name, i == SelectedSheetIndex, delegate
			{
				SelectSheet(i2);
			}));
		}
	}

	private static Pressable SheetTab(string name, bool isSelected, Action action)
	{
		TextBlock textBlock = Ui.Text(name, 12.0, isSelected ? FontWeights.SemiBold : FontWeights.Medium, isSelected ? SpreadsheetChromePalette.SelectedTabText : SpreadsheetChromePalette.SecondaryText, TextAlignment.Left, 1);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.Margin = new Thickness(12.0, 0.0, 12.0, 0.0);
		Pressable tab = new Pressable
		{
			Height = 28.0,
			CornerRadius = new CornerRadius(7.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		tab.Background = Ui.Brush(isSelected ? SpreadsheetChromePalette.SelectedTabFill : DS.Black(0.0));
		tab.Children.Add(textBlock);
		tab.HoverChanged += delegate(bool hovering)
		{
			tab.Background = Ui.Brush(isSelected ? SpreadsheetChromePalette.SelectedTabFill : DS.Black(hovering ? 0.05 : 0.0));
		};
		tab.Click += action;
		AutomationProperties.SetName(tab, name);
		return tab;
	}

	private void ShowSummary(SpreadsheetSelectionSummary summary)
	{
		nameBox.Text = summary.NameBoxText;
		string text = summary.FormulaBarText.Replace("\n", " ");
		formula.Text = text;
		ToolTipService.SetToolTip(formula, string.IsNullOrEmpty(summary.FormulaBarText) ? null : summary.FormulaBarText);
		linkSlot.Children.Clear();
		string hyperlink = summary.Hyperlink;
		if (hyperlink != null && hyperlink.Length > 0 && Uri.TryCreate(hyperlink, UriKind.RelativeOrAbsolute, out Uri result))
		{
			linkSlot.Children.Add(LinkButton(result));
		}
		statistics.Text = summary.Statistics ?? "";
		statisticsPill.Visibility = ((summary.Statistics == null) ? Visibility.Collapsed : Visibility.Visible);
	}

	private Pressable LinkButton(Uri url)
	{
		TextBlock item = Icons.Glyph("\ue8a7", 11.0, SpreadsheetChromePalette.Link);
		TextBlock textBlock = Ui.Text((!url.IsAbsoluteUri || string.IsNullOrEmpty(url.Host)) ? "Open link" : url.Host, 11.5, FontWeights.Medium, SpreadsheetChromePalette.Link, TextAlignment.Left, 1);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		StackPanel item2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0,
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)item,
				(UIElement)textBlock
			}
		};
		Pressable button = new Pressable
		{
			Height = 22.0,
			CornerRadius = new CornerRadius(6.0)
		};
		Color fill = SpreadsheetChromePalette.SelectedTabFill;
		button.Background = Ui.Brush(Faded(0.6));
		button.HoverChanged += delegate(bool hovering)
		{
			button.Background = Ui.Brush(Faded(hovering ? 1.0 : 0.6));
		};
		button.Children.Add(item2);
		button.Click += delegate
		{
			OpenLink(url.ToString());
		};
		AutomationProperties.SetName(button, "Open link");
		return button.Tip(url.ToString());
		Color Faded(double opacity)
		{
			return Color.FromArgb((byte)Math.Round(255.0 * opacity), fill.R, fill.G, fill.B);
		}
	}

	private void OpenLink(string link)
	{
		if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
		{
			base.DispatcherQueue.TryEnqueue(delegate
			{
				ChatBlockViews.OpenPath(uri.ToString());
			});
		}
	}
}
