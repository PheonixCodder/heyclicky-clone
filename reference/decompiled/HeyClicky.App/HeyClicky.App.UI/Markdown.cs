using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.UI;

public static class Markdown
{
	public sealed record Options(double FontSize, Color Text, Color Link, Color Secondary)
	{
		public double BlockSpacing { get; init; } = 10.0;

		public double[] HeadingSizes { get; init; } = new double[4] { 20.0, 17.0, 15.0, 14.0 };

		public double LineSpacing { get; init; }

		public double ListRowSpacing { get; init; } = 4.0;

		public double BulletIndent { get; init; }

		public double NumberIndent { get; init; }

		public Color CodeFill { get; init; } = DS.White(0.06);

		public Color CodeStroke { get; init; } = DS.White(0.08);

		public Thickness CodePadding { get; init; } = new Thickness(10.0);

		public bool CodeCopyButton { get; init; }

		public FontWeight StrongWeight { get; init; } = FontWeights.SemiBold;
	}

	public static readonly Options NotchResponse = new Options(13.0, DS.Colors.TextPrimary, DS.Colors.Blue300, DS.White(0.55))
	{
		StrongWeight = FontWeights.Bold
	};

	public static event Action<XamlRoot?, Uri>? LinkOpened;

	internal static void RaiseLinkOpened(XamlRoot? root, Uri uri)
	{
		Markdown.LinkOpened?.Invoke(root, uri);
	}

	public static StackPanel Render(string source, Options o, out List<(string thumb, string link)> images)
	{
		images = new List<(string, string)>();
		StackPanel stackPanel = new StackPanel
		{
			Spacing = o.BlockSpacing
		};
		string[] array = source.Replace("\r\n", "\n").Split('\n');
		int i = 0;
		while (i < array.Length)
		{
			string text = array[i];
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				i++;
				continue;
			}
			Match match = ImageRegex().Match(text2);
			if (match.Success && match.Value.Length == text2.Length)
			{
				images.Add((match.Groups[2].Value, match.Groups[2].Value));
				i++;
				continue;
			}
			if (text2.StartsWith("```"))
			{
				List<string> list = new List<string>();
				i++;
				while (i < array.Length && !array[i].Trim().StartsWith("```"))
				{
					list.Add(array[i++]);
				}
				i++;
				stackPanel.Children.Add(CodeBlock(string.Join("\n", list), o));
				continue;
			}
			Match match2 = HeadingRegex().Match(text2);
			if (match2.Success)
			{
				int length = match2.Groups[1].Value.Length;
				double size = o.HeadingSizes[Math.Min(length, o.HeadingSizes.Length) - 1];
				TextBlock textBlock = Inline(match2.Groups[2].Value, o, size, FontWeights.SemiBold, null);
				if (length <= 2)
				{
					textBlock.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
				}
				stackPanel.Children.Add(textBlock);
				i++;
				continue;
			}
			if (RuleRegex().IsMatch(text2))
			{
				stackPanel.Children.Add(new Border
				{
					Height = 1.0,
					Background = Ui.Brush(o.Secondary.WithAlpha(0.2)),
					Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
				});
				i++;
				continue;
			}
			if (text2.StartsWith('>'))
			{
				List<string> list2 = new List<string>();
				while (i < array.Length && array[i].Trim().StartsWith('>'))
				{
					list2.Add(array[i++].Trim().TrimStart('>').Trim());
				}
				Border item = new Border
				{
					Width = 2.0,
					Background = Ui.Brush(o.Secondary.WithAlpha(0.35))
				};
				TextBlock textBlock2 = Inline(string.Join(" ", list2), o, o.FontSize, FontWeights.Normal, o.Secondary);
				Grid grid = new Grid
				{
					ColumnSpacing = 8.0
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				Grid.SetColumn(textBlock2, 1);
				grid.Children.Add(item);
				grid.Children.Add(textBlock2);
				stackPanel.Children.Add(grid);
				continue;
			}
			if (text2.StartsWith('|') && i + 1 < array.Length && TableRuleRegex().IsMatch(array[i + 1].Trim()))
			{
				List<string[]> list3 = new List<string[]> { Cells(text2) };
				i += 2;
				while (i < array.Length && array[i].Trim().StartsWith('|'))
				{
					list3.Add(Cells(array[i++].Trim()));
				}
				stackPanel.Children.Add(Table(list3, o));
				continue;
			}
			if (BulletRegex().IsMatch(text) || NumberRegex().IsMatch(text))
			{
				bool flag = NumberRegex().IsMatch(text);
				StackPanel stackPanel2 = new StackPanel
				{
					Spacing = o.ListRowSpacing
				};
				int num = 0;
				for (; i < array.Length && (flag ? NumberRegex().IsMatch(array[i]) : BulletRegex().IsMatch(array[i])); i++)
				{
					string text3 = (flag ? NumberRegex().Replace(array[i], "", 1) : BulletRegex().Replace(array[i], "", 1));
					TextBlock textBlock3 = Ui.Text(flag ? $"{++num}." : "•", o.FontSize, FontWeights.Normal, o.Secondary);
					if (flag)
					{
						Typography.SetNumeralAlignment(textBlock3, FontNumeralAlignment.Tabular);
					}
					TextBlock textBlock4 = Inline(text3, o, o.FontSize, FontWeights.Normal, null);
					double num2 = (flag ? o.NumberIndent : o.BulletIndent);
					Grid grid2 = new Grid
					{
						ColumnSpacing = ((!(num2 > 0.0)) ? 8 : 0)
					};
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = ((num2 > 0.0) ? new GridLength(num2) : GridLength.Auto)
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					Grid.SetColumn(textBlock4, 1);
					ApplyLineSpacing(textBlock3, o, o.FontSize);
					grid2.Children.Add(textBlock3);
					grid2.Children.Add(textBlock4);
					stackPanel2.Children.Add(grid2);
				}
				stackPanel.Children.Add(stackPanel2);
				continue;
			}
			List<string> list4 = new List<string>();
			for (; i < array.Length; i++)
			{
				string text4 = array[i].Trim();
				if (text4.Length == 0 || text4.StartsWith("```") || HeadingRegex().IsMatch(text4) || text4.StartsWith('>') || RuleRegex().IsMatch(text4) || BulletRegex().IsMatch(array[i]) || NumberRegex().IsMatch(array[i]) || (text4.StartsWith('|') && i + 1 < array.Length && TableRuleRegex().IsMatch(array[i + 1].Trim())))
				{
					break;
				}
				list4.Add(array[i].TrimEnd());
			}
			stackPanel.Children.Add(Inline(string.Join("\n", list4), o, o.FontSize, FontWeights.Normal, null));
		}
		return stackPanel;
	}

	private static FrameworkElement CodeBlock(string code, Options o)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = code,
			FontFamily = DS.Fonts.Mono,
			FontSize = 12.0,
			Foreground = Ui.Brush(o.Text),
			IsTextSelectionEnabled = true,
			IsTextScaleFactorEnabled = false
		};
		ScrollViewer child = new ScrollViewer
		{
			Content = textBlock,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled
		};
		if (!o.CodeCopyButton)
		{
			textBlock.Margin = o.CodePadding;
		}
		Border border = new Border
		{
			Child = child,
			Padding = (o.CodeCopyButton ? o.CodePadding : new Thickness(0.0)),
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(o.CodeFill),
			BorderBrush = Ui.Brush(o.CodeStroke),
			BorderThickness = new Thickness(o.CodeCopyButton ? 1.0 : 0.5)
		};
		if (!o.CodeCopyButton)
		{
			return border;
		}
		Grid obj = new Grid
		{
			Children = { (UIElement)border }
		};
		Pressable copy = new Pressable
		{
			Width = 22.0,
			Height = 22.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 6.0, 6.0, 0.0),
			Opacity = 0.0
		};
		TextBlock glyph = Icons.Glyph("\ue8c8", 11.0, o.Secondary);
		copy.Children.Add(glyph);
		copy.Click += delegate
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(code);
			Clipboard.SetContent(dataPackage);
			glyph.Text = "\ue73e";
		};
		ToolTipService.SetToolTip(copy, "Copy");
		obj.PointerEntered += delegate
		{
			copy.Opacity = 1.0;
		};
		obj.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				copy.Opacity = 0.0;
				glyph.Text = "\ue8c8";
			}
		};
		obj.Children.Add(copy);
		return obj;
	}

	private static string[] Cells(string row)
	{
		return (from c in row.Trim().Trim('|').Split('|')
			select c.Trim()).ToArray();
	}

	private static FrameworkElement Table(List<string[]> rows, Options o)
	{
		int num = rows.Max((string[] r) => r.Length);
		Grid grid = new Grid();
		for (int i = 0; i < num; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
		}
		for (int j = 0; j < rows.Count; j++)
		{
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			for (int k = 0; k < num; k++)
			{
				TextBlock textBlock = Inline((k < rows[j].Length) ? rows[j][k] : "", o, o.FontSize - 1.0, (j == 0) ? FontWeights.SemiBold : FontWeights.Normal, null);
				textBlock.TextWrapping = TextWrapping.NoWrap;
				Border border = new Border
				{
					Child = textBlock,
					Padding = new Thickness(10.0, 6.0, 10.0, 6.0),
					BorderBrush = Ui.Brush(DS.White(0.08)),
					BorderThickness = new Thickness(0.0, 0.0, (k < num - 1) ? 1 : 0, (j < rows.Count - 1) ? 1 : 0),
					Background = ((j == 0) ? Ui.Brush(DS.White(0.04)) : null)
				};
				Grid.SetRow(border, j);
				Grid.SetColumn(border, k);
				grid.Children.Add(border);
			}
		}
		Border content = new Border
		{
			Child = grid,
			CornerRadius = new CornerRadius(8.0),
			BorderBrush = Ui.Brush(DS.White(0.1)),
			BorderThickness = new Thickness(1.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		return new ScrollViewer
		{
			Content = content,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled
		};
	}

	private static void ApplyLineSpacing(TextBlock tb, Options o, double size)
	{
		if (!(o.LineSpacing <= 0.0))
		{
			tb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
			tb.LineHeight = Math.Ceiling(size * 1.33) + o.LineSpacing;
		}
	}

	public static TextBlock Inline(string text, Options o, double size, FontWeight weight, Color? color = null)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = size,
			FontWeight = weight,
			FontFamily = DS.Fonts.For(size),
			Foreground = Ui.Brush(color ?? o.Text),
			TextWrapping = TextWrapping.Wrap,
			IsTextSelectionEnabled = true,
			IsTextScaleFactorEnabled = false,
			SelectionHighlightColor = Ui.Brush(DS.Colors.CursorBlue)
		};
		ApplyLineSpacing(textBlock, o, size);
		int num = 0;
		foreach (Match item in InlineRegex().Matches(text))
		{
			if (item.Index > num)
			{
				int num2 = num;
				AddRun(textBlock, text.Substring(num2, item.Index - num2));
			}
			if (item.Groups["bold"].Success)
			{
				textBlock.Inlines.Add(new Run
				{
					Text = item.Groups["bold"].Value,
					FontWeight = o.StrongWeight
				});
			}
			else if (item.Groups["ital"].Success)
			{
				textBlock.Inlines.Add(new Run
				{
					Text = item.Groups["ital"].Value,
					FontStyle = FontStyle.Italic
				});
			}
			else if (item.Groups["code"].Success)
			{
				textBlock.Inlines.Add(new Run
				{
					Text = item.Groups["code"].Value,
					FontFamily = DS.Fonts.Mono
				});
			}
			else if (item.Groups["ltext"].Success)
			{
				Hyperlink hyperlink = new Hyperlink
				{
					Foreground = Ui.Brush(o.Link),
					UnderlineStyle = UnderlineStyle.None
				};
				if (Uri.TryCreate(item.Groups["lurl"].Value, UriKind.Absolute, out Uri uri))
				{
					hyperlink.NavigateUri = uri;
					hyperlink.Click += delegate(Hyperlink s, HyperlinkClickEventArgs _)
					{
						Markdown.LinkOpened?.Invoke(s.XamlRoot, uri);
					};
				}
				hyperlink.Inlines.Add(new Run
				{
					Text = item.Groups["ltext"].Value
				});
				textBlock.Inlines.Add(hyperlink);
			}
			num = item.Index + item.Length;
		}
		if (num < text.Length)
		{
			int num2 = num;
			AddRun(textBlock, text.Substring(num2, text.Length - num2));
		}
		return textBlock;
	}

	private static void AddRun(TextBlock tb, string s)
	{
		string[] array = s.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			if (i > 0)
			{
				tb.Inlines.Add(new LineBreak());
			}
			if (array[i].Length > 0)
			{
				tb.Inlines.Add(new Run
				{
					Text = array[i]
				});
			}
		}
	}

	[GeneratedRegex("^(#{1,6})\\s+(.*)$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex HeadingRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__HeadingRegex_1.Instance;
	}

	[GeneratedRegex("^\\s*([-*_])(\\s*\\1){2,}\\s*$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex RuleRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__RuleRegex_2.Instance;
	}

	[GeneratedRegex("^\\s*[-*]\\s+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BulletRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__BulletRegex_3.Instance;
	}

	[GeneratedRegex("^\\s*\\d+\\.\\s+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex NumberRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__NumberRegex_4.Instance;
	}

	[GeneratedRegex("!\\[([^\\]]*)\\]\\(([^)]+)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex ImageRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__ImageRegex_5.Instance;
	}

	[GeneratedRegex("^\\|?\\s*:?-{2,}:?\\s*(\\|\\s*:?-{2,}:?\\s*)*\\|?$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex TableRuleRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__TableRuleRegex_6.Instance;
	}

	[GeneratedRegex("\\*\\*(?<bold>[^*]+)\\*\\*|__(?<bold>[^_]+)__|\\*(?<ital>[^*\\s][^*]*)\\*|`(?<code>[^`]+)`|\\[(?<ltext>[^\\]]+)\\]\\((?<lurl>[^)\\s]+)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex InlineRegex()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__InlineRegex_7.Instance;
	}
}
