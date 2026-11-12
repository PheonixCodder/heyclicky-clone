using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Notch.Window;

internal sealed class HeyClickyWindowUsageRing : Grid
{
	private static readonly Color SpentRed = DS.Rgb(0.96, 0.3, 0.28);

	private const double Diameter = 13.0;

	private const double Line = 2.5;

	private readonly BillingUsageRing usage;

	private readonly HeyClickyWindowPalette palette;

	private Popup? popup;

	private FrameworkElement? card;

	public HeyClickyWindowUsageRing(BillingUsageRing usage, HeyClickyWindowPalette palette)
	{
		this.usage = usage;
		this.palette = palette;
		base.Padding = new Thickness(3.0);
		base.Background = Ui.Transparent;
		base.VerticalAlignment = VerticalAlignment.Center;
		base.Children.Add(Glyph(usage.Progress, palette.InkAt(0.18), usage.IsSpent ? SpentRed : palette.InkAt(0.9)));
		base.PointerEntered += delegate
		{
			ShowCard();
		};
		base.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				HideCard();
			}
		};
		base.Unloaded += delegate
		{
			HideCard();
		};
		AutomationProperties.SetName(this, "Monthly usage");
		AutomationProperties.SetHelpText(this, usage.AccessibilityValue);
	}

	private static FrameworkElement Glyph(double progress, Color track, Color tint)
	{
		Grid grid = new Grid
		{
			Width = 13.0,
			Height = 13.0
		};
		grid.Children.Add(new Ellipse
		{
			Stroke = Ui.Brush(track),
			StrokeThickness = 2.5
		});
		double num = Math.Clamp(Math.Max(0.03, progress), 0.0, 1.0);
		if (num >= 0.999)
		{
			grid.Children.Add(new Ellipse
			{
				Stroke = Ui.Brush(tint),
				StrokeThickness = 2.5
			});
			return grid;
		}
		double num2 = 5.25;
		double num3 = 6.5;
		double num4 = num * 2.0 * Math.PI;
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = new Point(num3, num3 - num2),
			IsClosed = false,
			IsFilled = false
		};
		PathSegmentCollection segments = new PathSegmentCollection
		{
			new ArcSegment
			{
				Point = new Point(num3 + num2 * Math.Sin(num4), num3 - num2 * Math.Cos(num4)),
				Size = new Size(num2, num2),
				SweepDirection = SweepDirection.Clockwise,
				IsLargeArc = (num > 0.5)
			}
		};
		pathFigure.Segments = segments;
		PathGeometry pathGeometry = new PathGeometry();
		pathGeometry.Figures.Add(pathFigure);
		grid.Children.Add(new Path
		{
			Data = pathGeometry,
			Stroke = Ui.Brush(tint),
			StrokeThickness = 2.5,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round
		});
		return grid;
	}

	private void ShowCard()
	{
		if ((object)base.XamlRoot != null)
		{
			if ((object)card == null)
			{
				card = Card(usage, palette);
			}
			if ((object)popup == null)
			{
				popup = new Popup
				{
					Child = card,
					IsHitTestVisible = false,
					XamlRoot = base.XamlRoot
				};
			}
			card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Point point = TransformToVisual(null).TransformPoint(new Point(0.0, base.ActualHeight));
			popup.HorizontalOffset = Math.Max(4.0, point.X - 40.0);
			popup.VerticalOffset = point.Y - 26.0 - card.DesiredSize.Height;
			popup.IsOpen = true;
			card.Opacity = 0.0;
			Anim.Opacity(card, 1f, 0.14, Easing.EaseOut(Ui.Compositor));
		}
	}

	private void HideCard()
	{
		Popup popup = this.popup;
		if ((object)popup != null && popup.IsOpen)
		{
			this.popup.IsOpen = false;
		}
	}

	private static FrameworkElement Card(BillingUsageRing usage, HeyClickyWindowPalette palette)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0)
		};
		stackPanel.Children.Add(Section(usage.Agents, palette));
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Background = Ui.Brush(palette.InkAt(0.12))
		});
		stackPanel.Children.Add(Section(usage.Voice, palette));
		string resetsLine = usage.ResetsLine;
		if (resetsLine != null)
		{
			stackPanel.Children.Add(Ui.Text(resetsLine, 10.5, FontWeights.Medium, palette.SecondaryText));
		}
		Border item = new Border
		{
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(palette.CardPlate),
			BorderBrush = Ui.Brush(palette.InkAt(0.08)),
			BorderThickness = new Thickness(1.0),
			Child = stackPanel
		};
		return new Grid
		{
			Children = 
			{
				(UIElement)new ShadowLayer(10.0, DS.Black(0.3), 10.0, 4.0),
				(UIElement)item
			}
		};
	}

	private static FrameworkElement Section(BillingUsageSection section, HeyClickyWindowPalette palette)
	{
		StackPanel obj = new StackPanel
		{
			Spacing = 6.0
		};
		TextBlock textBlock = Ui.Text(section.Title, 9.5, FontWeights.Bold, palette.TertiaryText, TextAlignment.Left, 1);
		textBlock.CharacterSpacing = 50;
		obj.Children.Add(textBlock);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0
		};
		stackPanel.Children.Add(Ui.Text(section.CountText, 15.0, FontWeights.SemiBold, palette.PrimaryText, TextAlignment.Left, 1));
		string capText = section.CapText;
		if (capText != null)
		{
			TextBlock textBlock2 = Ui.Text(capText, 11.0, FontWeights.Medium, palette.SecondaryText, TextAlignment.Left, 1);
			textBlock2.VerticalAlignment = VerticalAlignment.Bottom;
			textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
			stackPanel.Children.Add(textBlock2);
		}
		obj.Children.Add(stackPanel);
		Color c = (section.IsSpent ? SpentRed : palette.InkAt(0.9));
		Grid grid = new Grid
		{
			Width = 150.0,
			Height = 4.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(2.0),
			Background = Ui.Brush(palette.InkAt(0.14))
		});
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(2.0),
			Background = Ui.Brush(c),
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = Math.Max(4.0, 150.0 * Math.Clamp(section.Progress, 0.0, 1.0))
		});
		obj.Children.Add(grid);
		return obj;
	}
}
