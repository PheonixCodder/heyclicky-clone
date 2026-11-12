using System;
using System.Linq;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyCardView : Canvas
{
	public static readonly Size SheetSize = new Size(176f, 232f);

	public static readonly Point SheetOrigin = new Point(6f, 16f);

	public static readonly Size BoxSize = new Size(SheetSize.Width + 30.0, SheetSize.Height + 24.0);

	private static readonly IClickyShape CardShape = ClickyShape.UnevenRoundedRectangle(2.0, 2.0, 6.0, 6.0);

	private readonly ClickyCardFace face;

	private Color FootColor => face.FootTone switch
	{
		ClickyCardFace.CardFootTone.Deep => face.Stock.Deep(), 
		ClickyCardFace.CardFootTone.Good => PaperColor.Hex("#1F7A3E"), 
		_ => face.Stock.Ink().Opacity(0.62), 
	};

	public ClickyCardView(ClickyCardFace face, bool showsSticker = true)
	{
		this.face = face;
		base.Width = BoxSize.Width;
		base.Height = BoxSize.Height;
		PaperSurface e = new PaperSurface(CardShape, PaperFill.Of(face.Stock.Paper().Brightness(-0.07).Saturation(0.95)), null).WithShadow(PaperColor.Hex("#0A101E").Opacity(0.16), 1.0, 0.0, 1.0);
		Place(e, SheetOrigin.X + 2.5, SheetOrigin.Y + 2.0, SheetSize.Width, SheetSize.Height);
		Place(Front(), SheetOrigin.X, SheetOrigin.Y, SheetSize.Width, SheetSize.Height);
		if (showsSticker)
		{
			ClickyPaperStickerKind? sticker = face.Sticker;
			if (sticker.HasValue)
			{
				ClickyPaperStickerKind valueOrDefault = sticker.GetValueOrDefault();
				Place(new ClickyPaperSticker(valueOrDefault), SheetOrigin.X + SheetSize.Width - 20.0, SheetOrigin.Y - 15.0);
			}
		}
	}

	private void Place(FrameworkElement e, double x, double y, double w = double.NaN, double h = double.NaN)
	{
		if (!double.IsNaN(w))
		{
			e.Width = w;
		}
		if (!double.IsNaN(h))
		{
			e.Height = h;
		}
		Canvas.SetLeft(e, x);
		Canvas.SetTop(e, y);
		base.Children.Add(e);
	}

	private FrameworkElement Front()
	{
		Color color = face.Stock.Ink();
		Grid grid = new Grid();
		PaperSurface item = new PaperSurface(CardShape, PaperFill.Of(face.Stock.Paper()), 0.5).WithWash(PaperFill.Horizontal((0.0, PaperColor.Black.Opacity(0.14)), (0.38, PaperColor.Black.Opacity(0.04)), (0.68, PaperColor.White.Opacity(0.3)), (1.0, PaperColor.White.Opacity(0.0))), (Size s) => new Rect(0.0, 0.0, 15.0, s.Height), clipped: false).WithWash(new PaperFill.Linear(new(double, Color)[4]
		{
			(0.0, PaperColor.White.Opacity(0.22)),
			(0.45, PaperColor.White.Opacity(0.0)),
			(0.72, PaperColor.Black.Opacity(0.0)),
			(1.0, PaperColor.Black.Opacity(0.05))
		}, new Point(0.2, 0.0), new Point(0.8, 1.0))).WithOutline(new PaperStroke(PaperColor.Black.Opacity(0.07), 0.5))
			.WithShadow(PaperColor.Hex("#0A101E").Opacity(0.18), 1.0, 0.0, 1.0)
			.WithShadow(PaperColor.Hex("#0A1428").Opacity(0.18), 11.0, 0.0, 10.0);
		grid.Children.Add(item);
		grid.Children.Add(Content(color));
		ClickyBrandMark item2 = new ClickyBrandMark(21.0, color, 2.4)
		{
			Opacity = 0.5,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 12.0, 12.0)
		};
		grid.Children.Add(item2);
		return grid;
	}

	private Canvas Content(Color ink)
	{
		Canvas c = new Canvas
		{
			Width = SheetSize.Width,
			Height = SheetSize.Height
		};
		Grid obj = new Grid
		{
			Width = SheetSize.Width - 34.0,
			Height = 21.0,
			ColumnSpacing = 7.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				}
			},
			Children = { (UIElement)new ClickyPaperGlyphView(face.Glyph, ink)
			{
				VerticalAlignment = VerticalAlignment.Center
			} }
		};
		TextBlock textBlock = PaperText.Line(face.Title, ClickyPaperFonts.Hand(21.0), ink);
		textBlock.Margin = new Thickness(0.0, 1.0, 0.0, 0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetColumn(textBlock, 1);
		obj.Children.Add(textBlock);
		At(obj, 22.0, 18.0);
		bool flag = face.Headline is ClickyCardFace.CardHeadline.Message;
		string caps = face.Caps;
		if (caps != null)
		{
			TextBlock textBlock2 = PaperText.Line(caps.ToUpperInvariant(), ClickyPaperFonts.Typed(9.5, FontWeights.SemiBold), ink.Opacity(0.58));
			textBlock2.CharacterSpacing = (int)Math.Round(80.0);
			textBlock2.Width = SheetSize.Width - 34.0;
			At(textBlock2, 22.0, flag ? 52 : 50);
		}
		(FrameworkElement, double, double)? tuple = Headline(ink);
		if (tuple.HasValue)
		{
			(FrameworkElement, double, double) valueOrDefault = tuple.GetValueOrDefault();
			At(valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3);
		}
		tuple = Detail(ink);
		if (tuple.HasValue)
		{
			(FrameworkElement, double, double) valueOrDefault2 = tuple.GetValueOrDefault();
			At(valueOrDefault2.Item1, valueOrDefault2.Item2, valueOrDefault2.Item3);
		}
		string foot = face.Foot;
		if (foot != null)
		{
			PaperFont font = ClickyPaperFonts.Hand(16.0);
			TextBlock textBlock3 = PaperText.Line(foot, font, FootColor);
			textBlock3.MaxWidth = SheetSize.Width - 58.0;
			textBlock3.RenderTransformOrigin = new Point(0f, 1f);
			textBlock3.RenderTransform = new RotateTransform
			{
				Angle = -1.5
			};
			c.Children.Add(new ClickyBaselinePlaced(textBlock3, 22.0, SheetSize.Height - 17.0, ClickyBaselinePlaced.TextBaseline(font)));
		}
		return c;
		void At(FrameworkElement e, double x, double y)
		{
			Canvas.SetLeft(e, x);
			Canvas.SetTop(e, y);
			c.Children.Add(e);
		}
	}

	private static TextBlock Tabular(TextBlock t)
	{
		Typography.SetNumeralAlignment(t, FontNumeralAlignment.Tabular);
		return t;
	}

	private (FrameworkElement View, double X, double Y)? Headline(Color ink)
	{
		ClickyCardFace.CardHeadline headline = face.Headline;
		if (!(headline is ClickyCardFace.CardHeadline.Big big))
		{
			if (!(headline is ClickyCardFace.CardHeadline.Count count))
			{
				if (!(headline is ClickyCardFace.CardHeadline.Sentence sentence))
				{
					if (headline is ClickyCardFace.CardHeadline.Message message)
					{
						StackPanel obj = new StackPanel
						{
							Spacing = 0.0
						};
						PaperFont paperFont = ClickyPaperFonts.Typed(12.0, null);
						obj.Children.Add(paperFont.Apply(new TextBlock
						{
							Text = message.Text,
							Foreground = Ui.Brush(ink.Opacity(0.92)),
							TextWrapping = TextWrapping.WrapWholeWords,
							MaxLines = 2,
							TextTrimming = TextTrimming.CharacterEllipsis,
							Width = 138.0,
							LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
							LineHeight = ClickyHandLineBreaker.NaturalLineHeight(paperFont) + 2.0
						}));
						TextBlock textBlock = PaperText.Line("my draft", ClickyPaperFonts.Hand(14.0), ink.Opacity(0.6));
						textBlock.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
						obj.Children.Add(textBlock);
						PaperFont paperFont2 = ClickyPaperFonts.Hand(19.0);
						obj.Children.Add(paperFont2.Apply(new TextBlock
						{
							Text = message.Draft,
							Foreground = Ui.Brush(ink),
							TextWrapping = TextWrapping.WrapWholeWords,
							MaxLines = 3,
							TextTrimming = TextTrimming.CharacterEllipsis,
							Width = 142.0,
							Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
							LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
							LineHeight = ClickyHandLineBreaker.NaturalLineHeight(paperFont2) - 1.0
						}));
						AutomationProperties.SetName(obj, $"{message.From} wrote: {message.Text}. Draft: {message.Draft}");
						return (obj, 22.0, 70.0);
					}
					return null;
				}
				PaperFont paperFont3 = ClickyPaperFonts.Typed(15.0, FontWeights.SemiBold);
				return (paperFont3.Apply(new TextBlock
				{
					Text = sentence.Text,
					Foreground = Ui.Brush(ink),
					TextWrapping = TextWrapping.WrapWholeWords,
					MaxLines = 2,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Width = 140.0,
					LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
					LineHeight = ClickyHandLineBreaker.NaturalLineHeight(paperFont3) + 1.0
				}), 22.0, 68.0);
			}
			PaperFont font = ClickyPaperFonts.BigNumber(42.0);
			PaperFont font2 = ClickyPaperFonts.Hand(19.0);
			TextBlock textBlock2 = Tabular(PaperText.Line(count.Number, font, ink));
			TextBlock t = PaperText.Line(count.Word, font2, ink);
			double num = ClickyHandLineBreaker.Ascent(font);
			double num2 = ClickyHandLineBreaker.Ascent(font2);
			Grid obj2 = new Grid
			{
				Width = SheetSize.Width - 40.0,
				ColumnSpacing = 8.0,
				ColumnDefinitions = 
				{
					new ColumnDefinition
					{
						Width = GridLength.Auto
					},
					new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					}
				}
			};
			textBlock2.VerticalAlignment = VerticalAlignment.Top;
			obj2.Children.Add(textBlock2);
			FrameworkElement frameworkElement = Ui.Shrink(t, HorizontalAlignment.Left, 0.7);
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			frameworkElement.Margin = new Thickness(0.0, num - num2, 0.0, 0.0);
			Grid.SetColumn(frameworkElement, 1);
			obj2.Children.Add(frameworkElement);
			return (obj2, 20.0, 62.0);
		}
		TextBlock textBlock3 = Tabular(PaperText.Line(big.Text, ClickyPaperFonts.BigNumber(42.0), ink));
		textBlock3.CharacterSpacing = (int)Math.Round(-33.333333333333336);
		FrameworkElement frameworkElement2 = Ui.Shrink(textBlock3, HorizontalAlignment.Left, 0.6);
		frameworkElement2.Width = SheetSize.Width - 40.0;
		return (frameworkElement2, 20.0, 62.0);
	}

	private (FrameworkElement View, double X, double Y)? Detail(Color ink)
	{
		ClickyCardFace.CardDetail detail = face.Detail;
		if (!(detail is ClickyCardFace.CardDetail.Sparkline sparkline))
		{
			if (!(detail is ClickyCardFace.CardDetail.Rows rows))
			{
				if (!(detail is ClickyCardFace.CardDetail.Bullets bullets))
				{
					if (detail is ClickyCardFace.CardDetail.Waiting waiting)
					{
						StackPanel stackPanel = new StackPanel
						{
							Width = 140.0
						};
						foreach (string item3 in waiting.Names.Take(3))
						{
							StackPanel stackPanel2 = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								Spacing = 7.0,
								Height = 19.0
							};
							stackPanel2.Children.Add(new Ellipse
							{
								Width = 5.0,
								Height = 5.0,
								Fill = Ui.Brush(PaperColor.Hex("#E08A00")),
								VerticalAlignment = VerticalAlignment.Center
							});
							TextBlock textBlock = PaperText.Line(item3, ClickyPaperFonts.Typed(11.5, FontWeights.Medium), ink);
							textBlock.VerticalAlignment = VerticalAlignment.Center;
							stackPanel2.Children.Add(textBlock);
							stackPanel.Children.Add(stackPanel2);
						}
						return (stackPanel, 22.0, 122.0);
					}
					return null;
				}
				StackPanel stackPanel3 = new StackPanel
				{
					Width = 140.0
				};
				PaperFont font = ClickyPaperFonts.Hand(17.0);
				foreach (string item4 in bullets.Items.Take(3))
				{
					StackPanel stackPanel4 = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 7.0,
						Height = 24.0
					};
					TextBlock textBlock2 = PaperText.Line("·", font, face.Stock.Deep());
					textBlock2.VerticalAlignment = VerticalAlignment.Center;
					stackPanel4.Children.Add(textBlock2);
					TextBlock textBlock3 = PaperText.Line(item4, font, ink);
					textBlock3.VerticalAlignment = VerticalAlignment.Center;
					stackPanel4.Children.Add(textBlock3);
					stackPanel3.Children.Add(stackPanel4);
				}
				return (stackPanel3, 22.0, 112.0);
			}
			StackPanel stackPanel5 = new StackPanel
			{
				Width = 140.0
			};
			PaperFont font2 = ClickyPaperFonts.Hand(18.0);
			PaperFont font3 = ClickyPaperFonts.Rounded(12.0, FontWeights.SemiBold);
			double num = ClickyHandLineBreaker.Ascent(font2);
			double num2 = ClickyHandLineBreaker.Ascent(font3);
			double num3 = Math.Max(num, num2);
			foreach (var item5 in rows.Items.Take(4).Select((ClickyCardFace.CardRow r, int i) => (r: r, i: i)))
			{
				ClickyCardFace.CardRow item = item5.r;
				int item2 = item5.i;
				Grid grid = new Grid
				{
					Height = 27.0
				};
				Grid grid2 = new Grid
				{
					VerticalAlignment = VerticalAlignment.Center
				};
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(16.0)
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto,
					MinWidth = 6.0
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				TextBlock textBlock4 = PaperText.Line($"{item2 + 1}", font2, ink);
				textBlock4.Margin = new Thickness(0.0, num3 - num, 0.0, 0.0);
				TextBlock textBlock5 = PaperText.Line(item.Label, font2, ink);
				textBlock5.Margin = new Thickness(0.0, num3 - num, 0.0, 0.0);
				Grid.SetColumn(textBlock5, 1);
				TextBlock textBlock6 = Tabular(PaperText.Line(item.Value, font3, ink));
				textBlock6.Margin = new Thickness(6.0, num3 - num2, 0.0, 0.0);
				Grid.SetColumn(textBlock6, 3);
				grid2.Children.Add(textBlock4);
				grid2.Children.Add(textBlock5);
				grid2.Children.Add(textBlock6);
				grid.Children.Add(grid2);
				stackPanel5.Children.Add(grid);
			}
			return (stackPanel5, 20.0, 66.0);
		}
		return (new ClickyCardSparkline(sparkline.Values, ink, face.Stock.Deep(), sparkline.CircledEnd)
		{
			Width = 136.0,
			Height = 56.0
		}, 22.0, 122.0);
	}
}
