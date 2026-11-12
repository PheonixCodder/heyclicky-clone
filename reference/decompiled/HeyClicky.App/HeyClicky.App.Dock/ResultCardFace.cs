using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Dock;

public sealed class ResultCardFace : Grid
{
	public static readonly Color Ink = DS.Rgb(0.11, 0.12, 0.14);

	public static readonly Color Green = DS.Rgb(0.12, 0.62, 0.29);

	public static readonly Color Blue = DS.Rgb(0.18, 0.36, 0.89);

	private readonly string title;

	private readonly ClickyAnswer answer;

	private readonly Color ink;

	private readonly Color wash;

	private readonly string? whenLine;

	private readonly Action<ClickyAnswer.Choice> onChoose;

	private readonly Action<string> onOpen;

	private bool isDraftCopied;

	private string? sentChoice;

	private readonly FirstThatFits pages = new FirstThatFits();

	public ResultCardFace(string title, ClickyAnswer answer, Color ink, Color wash, string? whenLine, bool isDraftCopied, string? sentChoice, Action<ClickyAnswer.Choice> onChoose, Action<string> onOpen)
	{
		this.title = title;
		this.answer = answer;
		this.ink = ink;
		this.wash = wash;
		this.whenLine = whenLine;
		this.isDraftCopied = isDraftCopied;
		this.sentChoice = sentChoice;
		this.onChoose = onChoose;
		this.onOpen = onOpen;
		base.Padding = new Thickness(18.0, 12.0, 18.0, 8.0);
		base.Children.Add(pages);
		Build();
	}

	public void Update(bool draftCopied, string? sent)
	{
		if (draftCopied != isDraftCopied || !(sent == sentChoice))
		{
			isDraftCopied = draftCopied;
			sentChoice = sent;
			Build();
		}
	}

	private void Build()
	{
		pages.Children.Clear();
		foreach (ResultCardLayouts.Arrangement item in ResultCardLayouts.Layouts(answer))
		{
			pages.Children.Add(Page(item));
		}
		if (pages.Children.Count == 0)
		{
			pages.Children.Add(Page(new ResultCardLayouts.Arrangement(Array.Empty<ClickyAnswer.Block>(), ShowsNote: false)));
		}
	}

	private FrameworkElement Page(ResultCardLayouts.Arrangement layout)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Top
		};
		TextBlock textBlock = Ui.Text(title, 22.0, FontWeights.Bold, ink, TextAlignment.Left, 2);
		textBlock.FontFamily = DS.Fonts.Display;
		textBlock.CharacterSpacing = -14;
		stackPanel.Children.Add(Ui.Shrink(textBlock, HorizontalAlignment.Left, 0.8));
		stackPanel.Children.Add(Outcome());
		foreach (ClickyAnswer.Block block in layout.Blocks)
		{
			FrameworkElement frameworkElement = BlockView(block, layout.IsCompact);
			if ((object)frameworkElement != null)
			{
				stackPanel.Children.Add(frameworkElement);
			}
		}
		if (answer.IsQuiet || answer.NumbersLook == ClickyAnswer.NumbersLookKind.Changes)
		{
			string text = whenLine;
			if (text != null)
			{
				TextBlock textBlock2 = Ui.Text(text, 12.0, FontWeights.Normal, ink.WithAlpha(0.5), TextAlignment.Left, 0, wrap: true);
				textBlock2.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
				stackPanel.Children.Add(textBlock2);
			}
		}
		if (layout.ShowsNote)
		{
			string note = answer.Note;
			if (note != null)
			{
				stackPanel.Children.Add(Sticky(note));
			}
		}
		return stackPanel;
	}

	private FrameworkElement Outcome()
	{
		TextBlock textBlock = Ui.Text(answer.Line, 14.5, answer.IsQuiet ? FontWeights.Medium : FontWeights.SemiBold, ink.WithAlpha(answer.IsQuiet ? 0.75 : 1.0), TextAlignment.Left, 0, wrap: true);
		textBlock.LineHeight = 21.285;
		return new Border
		{
			CornerRadius = new CornerRadius(11.0),
			Padding = new Thickness(12.0, 9.0, 12.0, 9.0),
			Background = Ui.Brush(answer.IsQuiet ? ink.WithAlpha(0.06) : wash.WithAlpha(0.72)),
			Child = textBlock
		};
	}

	private FrameworkElement? BlockView(ClickyAnswer.Block block, bool isCompact)
	{
		switch (block)
		{
		case ClickyAnswer.Block.Numbers:
		{
			IReadOnlyList<ClickyAnswer.Number> numbers = answer.Numbers;
			if (numbers != null)
			{
				ClickyAnswer.NumbersLookKind? numbersLook = answer.NumbersLook;
				if (numbersLook.HasValue)
				{
					ClickyAnswer.NumbersLookKind valueOrDefault = numbersLook.GetValueOrDefault();
					return Numbers(numbers, valueOrDefault, isCompact);
				}
			}
			break;
		}
		case ClickyAnswer.Block.List:
		{
			IReadOnlyList<ClickyAnswer.Row> list = answer.List;
			if (list != null)
			{
				return List(list, answer.Ranked == true);
			}
			break;
		}
		case ClickyAnswer.Block.Done:
		{
			IReadOnlyList<ClickyAnswer.DoneRow> done = answer.Done;
			if (done != null)
			{
				return Receipt(done);
			}
			break;
		}
		case ClickyAnswer.Block.Draft:
		{
			ClickyAnswer.Draft draftText = answer.DraftText;
			if ((object)draftText != null)
			{
				return DraftSlip(draftText);
			}
			break;
		}
		case ClickyAnswer.Block.Ask:
		{
			IReadOnlyList<ClickyAnswer.Choice> ask = answer.Ask;
			if (ask != null)
			{
				return Choices(ask, isCompact);
			}
			break;
		}
		}
		return null;
	}

	private FrameworkElement Numbers(IReadOnlyList<ClickyAnswer.Number> numbers, ClickyAnswer.NumbersLookKind look, bool isCompact)
	{
		Color c = ink.WithAlpha(0.55);
		switch (look)
		{
		case ClickyAnswer.NumbersLookKind.Hero:
		{
			ClickyAnswer.Number number2 = numbers[0];
			TextBlock textBlock6 = Rounded(number2.Value, isCompact ? 32 : 46, FontWeights.ExtraBold, ink);
			textBlock6.CharacterSpacing = -20;
			StackPanel stackPanel5 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10.0,
				Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
			};
			stackPanel5.Children.Add(Ui.Shrink(textBlock6, HorizontalAlignment.Left, 0.6));
			string label = number2.Label;
			if (label != null)
			{
				TextBlock textBlock7 = Ui.Text(label, 14.0, FontWeights.SemiBold, ink.WithAlpha(0.6), TextAlignment.Left, 0, wrap: true);
				textBlock7.VerticalAlignment = VerticalAlignment.Bottom;
				textBlock7.Margin = new Thickness(0.0, 0.0, 0.0, isCompact ? 5 : 9);
				stackPanel5.Children.Add(textBlock7);
			}
			return stackPanel5;
		}
		case ClickyAnswer.NumbersLookKind.Tiles:
		{
			Grid grid3 = new Grid
			{
				Margin = new Thickness(4.0, isCompact ? 6 : 8, 4.0, isCompact ? 6 : 8)
			};
			Grid grid4 = new Grid();
			for (int k = 0; k < numbers.Count; k++)
			{
				if (k > 0)
				{
					grid4.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.5)
					});
				}
				grid4.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
			}
			int num2 = 0;
			for (int l = 0; l < numbers.Count; l++)
			{
				if (l > 0)
				{
					Rectangle rectangle = new Rectangle
					{
						Fill = Ui.Brush(c)
					};
					Grid.SetColumn(rectangle, num2++);
					grid4.Children.Add(rectangle);
				}
				TextBlock textBlock4 = Ui.Text((numbers[l].Label ?? "").ToUpperInvariant(), 9.5, FontWeights.Bold, ink.WithAlpha(0.55));
				textBlock4.CharacterSpacing = 89;
				TextBlock textBlock5 = Rounded(numbers[l].Value, 24.0, FontWeights.ExtraBold, ink);
				textBlock5.CharacterSpacing = -10;
				StackPanel stackPanel4 = new StackPanel
				{
					Spacing = 1.0,
					Padding = new Thickness(12.0, isCompact ? 4 : 7, 12.0, isCompact ? 5 : 8)
				};
				stackPanel4.Children.Add(Ui.Shrink(textBlock4, HorizontalAlignment.Left, 0.8));
				stackPanel4.Children.Add(Ui.Shrink(textBlock5, HorizontalAlignment.Left, 0.6));
				Grid.SetColumn(stackPanel4, num2++);
				grid4.Children.Add(stackPanel4);
			}
			grid3.Children.Add(grid4);
			grid3.Children.Add(new Rectangle
			{
				Height = 1.5,
				Fill = Ui.Brush(c),
				VerticalAlignment = VerticalAlignment.Top
			});
			grid3.Children.Add(new Rectangle
			{
				Height = 1.5,
				Fill = Ui.Brush(c),
				VerticalAlignment = VerticalAlignment.Bottom
			});
			grid3.Children.Add(new Rectangle
			{
				Width = 1.5,
				Fill = Ui.Brush(c),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0.0, -6.0, 0.0, -6.0)
			});
			grid3.Children.Add(new Rectangle
			{
				Width = 1.5,
				Fill = Ui.Brush(c),
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0.0, -6.0, 0.0, -6.0)
			});
			return grid3;
		}
		case ClickyAnswer.NumbersLookKind.Changes:
		{
			Grid grid2 = new Grid
			{
				ColumnSpacing = 8.0,
				RowSpacing = 6.0,
				Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
			};
			for (int i = 0; i < 5; i++)
			{
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
			}
			for (int j = 0; j < numbers.Count; j++)
			{
				grid2.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLength.Auto
				});
				ClickyAnswer.Number number = numbers[j];
				ResultCardLayouts.MoveKind moveKind = ResultCardLayouts.Move(number);
				Color c2 = DS.Rgb(0.17, 0.15, 0.13);
				(string, Color, Color, Color) tuple = moveKind switch
				{
					ResultCardLayouts.MoveKind.Same => ("same", c2.WithAlpha(0.4), c2.WithAlpha(0.07), c2.WithAlpha(0.55)), 
					ResultCardLayouts.MoveKind.Better => ("better", Green, DS.Rgb(0.84, 0.94, 0.86), DS.Rgb(0.08, 0.48, 0.22)), 
					ResultCardLayouts.MoveKind.Worse => ("worse", DS.Rgb(0.76, 0.25, 0.05), DS.Rgb(0.99, 0.89, 0.83), DS.Rgb(0.71, 0.25, 0.05)), 
					ResultCardLayouts.MoveKind.Plain => (null, c2.WithAlpha(0.55), Colors.Transparent, Colors.Transparent), 
					_ => (null, Colors.Transparent, Colors.Transparent, Colors.Transparent), 
				};
				TextBlock textBlock2 = Ui.Text(number.Label ?? "", 13.0, FontWeights.Normal, ink.WithAlpha(0.6));
				textBlock2.MaxWidth = 130.0;
				Place(grid2, textBlock2, j, 0);
				string was = number.Was;
				if (was != null)
				{
					TextBlock textBlock3 = Ui.Text(was, 16.0, FontWeights.SemiBold, ink.WithAlpha(0.45));
					textBlock3.TextDecorations = TextDecorations.Strikethrough;
					Place(grid2, textBlock3, j, 1);
					Place(grid2, Ui.Text("→", 13.0, FontWeights.Bold, tuple.Item2), j, 2);
				}
				Place(grid2, Rounded(number.Value, 22.0, FontWeights.ExtraBold, ink), j, 3);
				var (text, _, _, _) = tuple;
				if (text != null)
				{
					Border e2 = new Border
					{
						Background = Ui.Brush(tuple.Item3),
						Padding = new Thickness(7.0, 2.0, 7.0, 2.0),
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Left,
						Child = Ui.Text(text, 11.5, FontWeights.Bold, tuple.Item4)
					}.Capsule();
					Place(grid2, e2, j, 4);
				}
			}
			return grid2;
		}
		default:
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 10.0,
				Margin = new Thickness(4.0, 2.0, 4.0, 0.0)
			};
			{
				foreach (ClickyAnswer.Number number3 in numbers)
				{
					StackPanel stackPanel2 = new StackPanel
					{
						Spacing = 6.0
					};
					StackPanel stackPanel3 = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 8.0
					};
					stackPanel3.Children.Add(Rounded(number3.Value, 24.0, FontWeights.ExtraBold, ink));
					string[] array = new string[2];
					string of = number3.Of;
					array[0] = ((of != null) ? ("of " + of) : null);
					array[1] = number3.Label;
					TextBlock textBlock = Ui.Text(string.Join(" ", array.OfType<string>()), 13.0, FontWeights.Normal, ink.WithAlpha(0.6));
					textBlock.VerticalAlignment = VerticalAlignment.Bottom;
					textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
					stackPanel3.Children.Add(textBlock);
					stackPanel2.Children.Add(stackPanel3);
					double? num = ResultCardLayouts.Fraction(number3);
					if (num.HasValue)
					{
						double fraction = num.GetValueOrDefault();
						Grid grid = new Grid
						{
							Height = 8.0
						};
						grid.Children.Add(new Border
						{
							Background = Ui.Brush(ink.WithAlpha(0.12))
						}.Capsule());
						Border fill = new Border
						{
							Background = Ui.Brush(Green),
							HorizontalAlignment = HorizontalAlignment.Left
						}.Capsule();
						grid.SizeChanged += delegate(object _, SizeChangedEventArgs e)
						{
							fill.Width = Math.Max(8.0, e.NewSize.Width * fraction);
						};
						grid.Children.Add(fill);
						stackPanel2.Children.Add(grid);
					}
					stackPanel.Children.Add(stackPanel2);
				}
				return stackPanel;
			}
		}
		}
	}

	private static void Place(Grid grid, FrameworkElement e, int row, int column)
	{
		e.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetRow(e, row);
		Grid.SetColumn(e, column);
		grid.Children.Add(e);
	}

	private static TextBlock Rounded(string text, double size, FontWeight weight, Color color)
	{
		TextBlock textBlock = Ui.Text(text, size, weight, color);
		textBlock.FontFamily = DS.Fonts.Display;
		return textBlock;
	}

	private FrameworkElement List(IReadOnlyList<ClickyAnswer.Row> rows, bool isRanked)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0,
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
		};
		for (int i = 0; i < rows.Count; i++)
		{
			Grid grid = new Grid
			{
				ColumnSpacing = 9.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			FrameworkElement item = (isRanked ? ((FrameworkElement)new Border
			{
				Width = 16.0,
				Height = 16.0,
				CornerRadius = new CornerRadius(8.0),
				Background = Ui.Brush(ink.WithAlpha(0.1)),
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 1.0, 0.0, 0.0),
				Child = Ui.Text($"{i + 1}", 11.0, FontWeights.Bold, ink.WithAlpha(0.85), TextAlignment.Center)
			}) : ((FrameworkElement)Ui.Text("–", 13.5, FontWeights.Normal, ink.WithAlpha(0.45))));
			grid.Children.Add(item);
			TextBlock textBlock = Ui.Text(rows[i].Text, 13.5, FontWeights.Normal, ink, TextAlignment.Left, 0, wrap: true);
			Grid.SetColumn(textBlock, 1);
			grid.Children.Add(textBlock);
			string detail = rows[i].Detail;
			if (detail != null)
			{
				TextBlock textBlock2 = Ui.Text(detail, 12.5, FontWeights.Normal, ink.WithAlpha(0.55));
				textBlock2.Margin = new Thickness(8.0, 1.0, 0.0, 0.0);
				Grid.SetColumn(textBlock2, 2);
				grid.Children.Add(textBlock2);
			}
			stackPanel.Children.Add(grid);
		}
		return stackPanel;
	}

	private static FrameworkElement FileIcon(string path)
	{
		DockFileStyle dockFileStyle = DockFileStyle.For(path);
		Border host = new Border
		{
			Width = 16.0,
			Height = 16.0,
			Child = Icons.Glyph(dockFileStyle.Glyph, 14.0, dockFileStyle.Tint)
		};
		LoadAsync();
		return host;
		async Task LoadAsync()
		{
			_ = 3;
			try
			{
				IStorageItemProperties storageItemProperties;
				if (Directory.Exists(path))
				{
					storageItemProperties = await StorageFolder.GetFolderFromPathAsync(path);
				}
				else
				{
					if (!File.Exists(path))
					{
						throw new FileNotFoundException();
					}
					storageItemProperties = await StorageFile.GetFileFromPathAsync(path);
				}
				using StorageItemThumbnail thumb = await storageItemProperties.GetThumbnailAsync(ThumbnailMode.ListView, 32u);
				if ((object)thumb == null)
				{
					return;
				}
				BitmapImage bitmap = new BitmapImage();
				await bitmap.SetSourceAsync(thumb);
				host.Child = new Image
				{
					Source = bitmap,
					Width = 16.0,
					Height = 16.0,
					Stretch = Stretch.Uniform
				};
			}
			catch
			{
			}
		}
	}

	private FrameworkElement Receipt(IReadOnlyList<ClickyAnswer.DoneRow> rows)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0,
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
		};
		foreach (ClickyAnswer.DoneRow row in rows)
		{
			Grid grid = new Grid
			{
				ColumnSpacing = 9.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			TextBlock textBlock = Icons.Glyph("\ue73e", 8.5, Colors.White);
			textBlock.FontWeight = FontWeights.ExtraBold;
			grid.Children.Add(new Border
			{
				Width = 16.0,
				Height = 16.0,
				CornerRadius = new CornerRadius(4.0),
				Background = Ui.Brush(Green),
				Child = textBlock,
				VerticalAlignment = VerticalAlignment.Center
			});
			TextBlock textBlock2 = new TextBlock
			{
				FontSize = 13.5,
				FontFamily = DS.Fonts.For(13.5),
				TextWrapping = TextWrapping.WrapWholeWords,
				IsTextScaleFactorEnabled = false,
				VerticalAlignment = VerticalAlignment.Center
			};
			textBlock2.Inlines.Add(new Run
			{
				Text = row.Did,
				Foreground = Ui.Brush(ink)
			});
			string what = row.What;
			if (what != null)
			{
				textBlock2.Inlines.Add(new Run
				{
					Text = " " + what,
					Foreground = Ui.Brush(Blue),
					FontWeight = FontWeights.SemiBold
				});
			}
			Grid.SetColumn(textBlock2, 1);
			grid.Children.Add(textBlock2);
			string link = row.Link;
			if (link != null)
			{
				FrameworkElement frameworkElement;
				if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					FrameworkElement item = ArcSymbols.Make("arrow.up.right.square", 13.0, ink.WithAlpha(0.4), bold: false);
					frameworkElement = ClickyInkObjectKit.Favicon(link, 16.0, new Grid
					{
						Width = 16.0,
						Height = 16.0,
						Children = { (UIElement)item }
					});
					FrameworkElement frameworkElement2 = frameworkElement;
					double width = (frameworkElement.Height = 16.0);
					frameworkElement2.Width = width;
					Ui.ClipRounded(frameworkElement, 4.0);
				}
				else
				{
					frameworkElement = FileIcon(link);
				}
				frameworkElement.VerticalAlignment = VerticalAlignment.Center;
				frameworkElement.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
				Grid.SetColumn(frameworkElement, 2);
				grid.Children.Add(frameworkElement);
				Pressable pressable = new Pressable
				{
					Children = { (UIElement)grid }
				};
				pressable.Click += delegate
				{
					onOpen(link);
				};
				ToolTipService.SetToolTip(pressable, link);
				stackPanel.Children.Add(pressable);
			}
			else
			{
				stackPanel.Children.Add(grid);
			}
		}
		return stackPanel;
	}

	private FrameworkElement DraftSlip(ClickyAnswer.Draft draft)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0
		};
		string to = draft.To;
		if (to != null)
		{
			stackPanel.Children.Add(Ui.Text("To " + to, 10.5, FontWeights.SemiBold, DS.Black(0.45)));
		}
		TextBlock textBlock = Ui.Text(draft.Text, 12.0, FontWeights.Medium, DS.Rgb(0.17, 0.15, 0.13), TextAlignment.Left, 3, wrap: false, mono: true);
		textBlock.LineHeight = 18.6;
		stackPanel.Children.Add(textBlock);
		Grid slip = new Grid
		{
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0),
			RenderTransform = new RotateTransform
			{
				Angle = -0.6
			}
		};
		slip.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			((RotateTransform)slip.RenderTransform).CenterX = e.NewSize.Width / 2.0;
		};
		slip.Children.Add(new ShadowLayer(6.0, DS.Black(0.12), 6.0, 4.0));
		slip.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(6.0),
			Background = Ui.Brush(Colors.White),
			BorderBrush = Ui.Brush(DS.Black(0.05)),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			Child = stackPanel
		});
		if (isDraftCopied)
		{
			Border border = new Border
			{
				CornerRadius = new CornerRadius(6.0),
				Padding = new Thickness(7.0, 3.0, 7.0, 3.0),
				Background = Ui.Brush(DS.Rgb(0.92, 0.94, 0.99)),
				BorderBrush = Ui.Brush(Blue.WithAlpha(0.25)),
				BorderThickness = new Thickness(1.0),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, -10.0, 10.0, 0.0),
				Child = Ui.Text("Copied", 10.5, FontWeights.Bold, Blue)
			};
			slip.Children.Add(border);
			if (!Anim.ReduceMotion)
			{
				border.Opacity = 0.0;
				border.Scale = new Vector3(0.9f, 0.9f, 1f);
				Anim.Opacity(border, 1f, 0.2);
				Anim.Scale(border, 1f, 0.2, null, null);
			}
		}
		return slip;
	}

	private FrameworkElement Choices(IReadOnlyList<ClickyAnswer.Choice> choices, bool isCompact)
	{
		Panel panel;
		if (choices.All((ClickyAnswer.Choice c) => c.File != null))
		{
			Grid grid = new Grid
			{
				ColumnSpacing = 10.0
			};
			for (int i = 0; i < choices.Count; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				FrameworkElement frameworkElement = Button(choices[i], FileChoice(choices[i], IsSent(choices[i]), isCompact));
				Grid.SetColumn(frameworkElement, i);
				grid.Children.Add(frameworkElement);
			}
			panel = grid;
		}
		else if (isCompact)
		{
			DockFlow dockFlow = new DockFlow
			{
				HorizontalSpacing = 6.0,
				VerticalSpacing = 6.0
			};
			foreach (ClickyAnswer.Choice choice3 in choices)
			{
				StackPanel stackPanel = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 5.0,
					VerticalAlignment = VerticalAlignment.Center
				};
				if (IsSent(choice3))
				{
					TextBlock textBlock = Icons.Glyph("\ue73e", 9.0, Blue);
					textBlock.FontWeight = FontWeights.ExtraBold;
					stackPanel.Children.Add(textBlock);
				}
				stackPanel.Children.Add(Ui.Text(choice3.Answer ?? "", 13.0, FontWeights.SemiBold, ink));
				Border label2 = new Border
				{
					Padding = new Thickness(11.0, 6.0, 11.0, 6.0),
					Background = Ui.Brush(DS.White(0.8)),
					BorderBrush = Ui.Brush(IsSent(choice3) ? Blue.WithAlpha(0.7) : ink.WithAlpha(0.16)),
					BorderThickness = new Thickness(IsSent(choice3) ? 1.5 : 1.0),
					Child = stackPanel
				}.Capsule();
				dockFlow.Children.Add(Button(choice3, label2));
			}
			panel = dockFlow;
		}
		else
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 5.0
			};
			for (int j = 0; j < choices.Count; j++)
			{
				ClickyAnswer.Choice choice2 = choices[j];
				Grid grid2 = new Grid
				{
					ColumnSpacing = 10.0,
					Padding = new Thickness(11.0, 7.0, 11.0, 7.0)
				};
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				FrameworkElement child = (IsSent(choice2) ? Icons.Glyph("\ue73e", 9.0, Colors.White) : Ui.Text($"{j + 1}", 10.0, FontWeights.Bold, ink.WithAlpha(0.6), TextAlignment.Center, 0, wrap: false, mono: true));
				grid2.Children.Add(new Border
				{
					Width = 18.0,
					Height = 18.0,
					CornerRadius = new CornerRadius(5.0),
					VerticalAlignment = VerticalAlignment.Center,
					Background = Ui.Brush(IsSent(choice2) ? Blue : ink.WithAlpha(0.08)),
					Child = child
				});
				TextBlock textBlock2 = Ui.Text(choice2.Answer ?? "", 13.5, FontWeights.SemiBold, ink);
				textBlock2.VerticalAlignment = VerticalAlignment.Center;
				Grid.SetColumn(textBlock2, 1);
				grid2.Children.Add(textBlock2);
				string detail = choice2.Detail;
				if (detail != null)
				{
					TextBlock textBlock3 = Ui.Text(detail, 12.0, FontWeights.Normal, ink.WithAlpha(0.55));
					textBlock3.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(textBlock3, 3);
					grid2.Children.Add(textBlock3);
				}
				Grid grid3 = new Grid();
				grid3.Children.Add(new Border
				{
					CornerRadius = new CornerRadius(10.0),
					Background = Ui.Brush(DS.White(0.75)),
					BorderBrush = Ui.Brush(IsSent(choice2) ? Blue.WithAlpha(0.7) : ink.WithAlpha(0.14)),
					BorderThickness = new Thickness(IsSent(choice2) ? 1.5 : 1.0)
				});
				grid3.Children.Add(grid2);
				stackPanel2.Children.Add(Button(choice2, grid3));
			}
			panel = stackPanel2;
		}
		panel.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
		return panel;
		FrameworkElement Button(ClickyAnswer.Choice choice, FrameworkElement label)
		{
			Pressable pressable = new Pressable
			{
				Children = { (UIElement)label }
			};
			pressable.Enabled = sentChoice == null;
			pressable.WithPressScale(0.97f, 0.12, easeOut: true);
			pressable.Click += delegate
			{
				if (sentChoice == null)
				{
					onChoose(choice);
				}
			};
			string file = choice.File;
			if (file != null)
			{
				ToolTipService.SetToolTip(pressable, file);
			}
			pressable.Opacity = (IsDimmed(choice) ? 0.45 : 1.0);
			return pressable;
		}
		bool IsDimmed(ClickyAnswer.Choice c)
		{
			if (sentChoice != null)
			{
				return !IsSent(c);
			}
			return false;
		}
		bool IsSent(ClickyAnswer.Choice c)
		{
			return sentChoice == c.Reply;
		}
	}

	private static FrameworkElement FileChoice(ClickyAnswer.Choice choice, bool isSent, bool isCompact)
	{
		string path = choice.File ?? "";
		Grid grid = new Grid
		{
			Height = (isCompact ? 34 : 56)
		};
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(4.0),
			Background = Ui.Brush(DS.Rgb(0.95, 0.95, 0.96))
		});
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0,
			Padding = new Thickness(7.0)
		};
		stackPanel.Children.Add(new Border
		{
			Width = 34.0,
			Height = 4.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Ui.Brush(DS.Rgb(0.11, 0.12, 0.14))
		}.Capsule());
		for (int i = 0; i < 3; i++)
		{
			stackPanel.Children.Add(new Border
			{
				Height = 3.0,
				MaxWidth = ((i == 1) ? 50.0 : double.PositiveInfinity),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Background = Ui.Brush(DS.Rgb(0.85, 0.86, 0.88))
			}.Capsule());
		}
		grid.Children.Add(stackPanel);
		Ui.ClipRounded(grid, 4.0);
		LoadThumbnailAsync(grid, stackPanel, path);
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 4.0,
			Padding = new Thickness(7.0)
		};
		stackPanel2.Children.Add(grid);
		TextBlock textBlock = Ui.Text(choice.Answer ?? System.IO.Path.GetFileName(path), 9.5, FontWeights.Bold, Ink);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		stackPanel2.Children.Add(textBlock);
		stackPanel2.Children.Add(Ui.Text(choice.Detail ?? FileDetail(path), 9.0, FontWeights.Normal, DS.Rgb(0.54, 0.56, 0.62)));
		return new Grid
		{
			Children = 
			{
				(UIElement)new ShadowLayer(7.0, DS.Black(0.14), 4.5, 3.0),
				(UIElement)new Border
				{
					CornerRadius = new CornerRadius(7.0),
					Background = Ui.Brush(Colors.White),
					BorderBrush = Ui.Brush(isSent ? Blue.WithAlpha(0.8) : DS.Black(0.05)),
					BorderThickness = new Thickness((!isSent) ? 1 : 2)
				},
				(UIElement)stackPanel2
			}
		};
	}

	private static async Task LoadThumbnailAsync(Grid picture, FrameworkElement placeholder, string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			ImageSource imageSource = await ArtifactThumbnails.GetAsync(path, 360);
			if ((object)imageSource != null)
			{
				picture.Children.Remove(placeholder);
				picture.Children.Add(new Image
				{
					Source = imageSource,
					Stretch = Stretch.UniformToFill,
					VerticalAlignment = VerticalAlignment.Top
				});
			}
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "answer file picture: " + ex.Message);
		}
	}

	public static string FileDetail(string path)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			if (!fileInfo.Exists)
			{
				return "";
			}
			return fileInfo.LastWriteTime.ToString("MMM d", CultureInfo.CurrentCulture) + " · " + DockFiles.FormatBytes(fileInfo.Length);
		}
		catch
		{
			return "";
		}
	}

	private static FrameworkElement Sticky(string text)
	{
		TextBlock textBlock = Ui.Text(text, 12.5, FontWeights.Normal, DS.Rgb(0.23, 0.2, 0.0), TextAlignment.Left, 0, wrap: true);
		textBlock.LineHeight = 17.75;
		Grid obj = new Grid
		{
			MaxWidth = 300.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(4.0, 2.0, 4.0, 0.0),
			Children = 
			{
				(UIElement)new ShadowLayer(0.0, DS.Black(0.12), 4.0, 3.0),
				(UIElement)new Border
				{
					Background = Ui.Brush(DS.Rgb(1.0, 0.957, 0.69)),
					Padding = new Thickness(11.0, 7.0, 11.0, 7.0),
					Child = textBlock
				}
			}
		};
		RotateTransform rotate = new RotateTransform
		{
			Angle = -1.0
		};
		obj.RenderTransform = rotate;
		obj.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			rotate.CenterX = e.NewSize.Width / 2.0;
			rotate.CenterY = e.NewSize.Height / 2.0;
		};
		return obj;
	}
}
