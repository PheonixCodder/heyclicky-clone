using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTicket
{
	private static readonly Color Navy = PaperColor.Hex("#16213A");

	private static readonly Color Blue = PaperColor.Hex("#1F4FA8");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		string text = block.Source["from"];
		string text2 = block.Source["to"];
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.15, block.LandsAt + 0.9);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0,
			Width = 270.0,
			Padding = new Thickness(16.0, 12.0, 12.0, 12.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		string text3 = (block.Source["what"] ?? block.Source["carrier"] ?? block.Source["kind"] ?? "ticket").ToUpperInvariant();
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, InkTextView.Plain(text3, ClickyPaperFonts.Typed(10.0, FontWeights.Bold), Blue).With(delegate(InkTextView t)
		{
			t.Tracking = 1.2;
			t.LineLimit = 1;
		}), new ClickyInkSpacer());
		TextBlock textBlock = ClickyInkIcon.Glyph(ClickyInkTicketRules.Icon(block.Source["kind"], text != null), 12.0, Blue, bold: true);
		if ((object)textBlock != null)
		{
			clickyInkHStack.Children.Add(textBlock);
		}
		stackPanel.Children.Add(clickyInkHStack);
		if (text != null && text2 != null)
		{
			PaperFont font = ClickyInkObjectFonts.Rounded(28.0, FontWeights.ExtraBold);
			Grid grid = new Grid
			{
				Width = 70.0,
				Height = 1.5
			};
			grid.Children.Add(new Border
			{
				Background = Ui.Brush(Navy.Opacity(0.2))
			});
			grid.Children.Add(new Border
			{
				Width = 70.0 * num,
				HorizontalAlignment = HorizontalAlignment.Left,
				Background = Ui.Brush(Blue)
			});
			InkTextView inkTextView = InkTextView.Plain(ClickyInkTicketRules.Code(text2), font, Navy);
			inkTextView.Opacity = ((num > 0.95) ? 1.0 : 0.15);
			ClickyInkHStack item = new ClickyInkHStack(10.0, InkVerticalAlignment.FirstBaseline, InkTextView.Plain(ClickyInkTicketRules.Code(text), font, Navy), grid.BaselineFromBottom(0.0), inkTextView);
			stackPanel.Children.Add(item);
			ClickyInkHStack item2 = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, ClickyInkObjectKit.Line(ClickyInkTicketRules.Place(text), ClickyPaperFonts.Typed(10.5, null), Navy.Opacity(0.6), 1), new ClickyInkSpacer(), ClickyInkObjectKit.Line(ClickyInkTicketRules.Place(text2), ClickyPaperFonts.Typed(10.5, null), Navy.Opacity(0.6), 1))
			{
				Width = 190.0,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			stackPanel.Children.Add(item2);
		}
		else
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyInkObjectFonts.Rounded(19.0, FontWeights.ExtraBold), pencil: true, Navy, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 14.0
		};
		string text4 = block.Source["when"] ?? block.Source["date"];
		if (text4 != null)
		{
			stackPanel2.Children.Add(Field("WHEN", text4));
		}
		string text5 = block.Source["price"];
		if (text5 != null)
		{
			stackPanel2.Children.Add(Field("PRICE", text5));
		}
		stackPanel.Children.Add(stackPanel2);
		if (text != null && block.Words.Current.Length > 0)
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(15.0), pencil: false, Navy.Opacity(0.75), null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
		}
		PaperPath paperPath = new PaperPath();
		for (double num2 = 8.0; num2 < 126.0; num2 += 7.0)
		{
			paperPath.MoveTo(0.0, num2).LineTo(0.0, num2 + 3.5);
		}
		PaperDrawing paperDrawing = new PaperDrawing().Stroke(paperPath, new PaperStroke(Navy.Opacity(0.3), 1.2)).Sized(1.0, 130.0);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 8.0,
			Padding = new Thickness(14.0, 0.0, 14.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		string text6 = block.Source["seat"] ?? block.Source["gate"];
		if (text6 != null)
		{
			stackPanel3.Children.Add(Field((block.Source["seat"] != null) ? "SEAT" : "GATE", text6));
		}
		else
		{
			stackPanel3.Children.Add(Field("ADMIT", "1"));
		}
		stackPanel3.Children.Add(ClickyInkBarcode.Make(block.Id.CharacterCount(), 64.0, 30.0, Navy.Opacity(0.8)));
		Grid obj = new Grid
		{
			Width = 370.0,
			Height = 130.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(270.0)
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		Grid.SetColumn(paperDrawing, 1);
		Grid.SetColumn(stackPanel3, 2);
		obj.Children.Add(stackPanel);
		obj.Children.Add(paperDrawing);
		obj.Children.Add(stackPanel3);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(obj, ClickyShapeView.Stroke(new ClickyInkTicketShape(), Navy.Opacity(0.12), 1.0)).Behind(new ClickyInkShadowedShape(new ClickyInkTicketShape(), "ticket", new LinearGradientBrush
		{
			StartPoint = new Point(0.5, 0.0),
			EndPoint = new Point(0.5, 1.0),
			GradientStops = 
			{
				new GradientStop
				{
					Color = PaperColor.Hex("#EAF2FF"),
					Offset = 0.0
				},
				new GradientStop
				{
					Color = PaperColor.Hex("#DCE8FB"),
					Offset = 1.0
				}
			}
		}, new PaperShadow(PaperColor.Black.Opacity(0.13), 4.0, 0.0, 2.0)));
		clickyInkOverlay.Transformed(0.0, 0.0, -0.8, 1.0, null);
		clickyInkOverlay.Margin = new Thickness(0.0, 4.0, 0.0, 4.0);
		return ClickyInkObjectKit.Leading(clickyInkOverlay, contentWidth);
	}

	private static FrameworkElement Field(string label, string value)
	{
		return new StackPanel
		{
			Spacing = 1.0,
			Children = 
			{
				(UIElement)InkTextView.Plain(label, ClickyPaperFonts.Typed(8.5, FontWeights.Bold), Navy.Opacity(0.45)).With(delegate(InkTextView t)
				{
					t.Tracking = 1.0;
					t.LineLimit = 1;
				}),
				(UIElement)InkTextView.Plain(value, ClickyPaperFonts.Typed(12.5, FontWeights.SemiBold), Navy).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				})
			}
		};
	}
}
