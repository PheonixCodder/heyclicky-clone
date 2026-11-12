using System;
using System.Globalization;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Notch.Peek;

public static class ChatBubbles
{
	private static readonly Markdown.Options IncomingDark = Incoming(MessagesPalette.Dark);

	private static readonly Markdown.Options IncomingLight = Incoming(MessagesPalette.Light);

	private static readonly Markdown.Options Outgoing = new Markdown.Options(13.0, DS.Black(0.86), DS.Black(0.86), DS.Black(0.6))
	{
		LineSpacing = 2.0
	};

	private static Markdown.Options Incoming(MessagesPalette p)
	{
		return new Markdown.Options(13.0, p.IncomingBubbleText, p.SelectionBlue, p.SecondaryText)
		{
			LineSpacing = 2.0
		};
	}

	public static FrameworkElement Bubble(string text, bool outgoing, bool tail, MessagesPalette? look = null)
	{
		MessagesPalette messagesPalette = look ?? MessagesPalette.Current;
		TextBlock child = Markdown.Inline(text, outgoing ? Outgoing : (messagesPalette.IsDark ? IncomingDark : IncomingLight), 13.0, FontWeights.Normal, null);
		return new Grid
		{
			HorizontalAlignment = (outgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left),
			Children = 
			{
				(UIElement)(outgoing ? ChatBlockViews.SentBubblePlate(tail) : ChatBlockViews.BubblePlate(Ui.Brush(messagesPalette.IncomingBubble), null, tail, mirrored: false)),
				(UIElement)new Border
				{
					Padding = new Thickness(12.0, 7.0, 12.0, 7.0 + (tail ? 7.0 : 0.0)),
					Child = child
				}
			}
		};
	}

	public static FrameworkElement TypingBubble()
	{
		MessagesPalette current = MessagesPalette.Current;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		for (int i = 0; i < 3; i++)
		{
			Ellipse d = new Ellipse
			{
				Width = 8.0,
				Height = 8.0,
				Fill = Ui.Brush(current.IsDark ? DS.White(0.72) : DS.Black(0.42))
			};
			double phase = (double)i / 6.0 * 1.1;
			d.Loaded += delegate
			{
				if (!Anim.ReduceMotion)
				{
					d.CenterPoint = new Vector3(4f, 4f, 0f);
					Anim.LoopScalar(d, "Opacity", 1.1, (double x) => (float)(0.38 + 0.62 * Wave(x / 1.1)), 24, phase);
					Anim.LoopVector3(d, "Scale", 1.1, delegate(double x)
					{
						float num = (float)(0.86 + 0.22 * Wave(x / 1.1));
						return new Vector3(num, num, 1f);
					}, 24, phase);
				}
			};
			stackPanel.Children.Add(d);
		}
		Grid obj = new Grid
		{
			Height = 30.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(8.0, 0.0, 0.0, 8.0),
			Children = 
			{
				(UIElement)new Ellipse
				{
					Width = 10.0,
					Height = 10.0,
					Fill = Ui.Brush(current.IncomingBubble),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Bottom,
					Margin = new Thickness(-2.0, 0.0, 0.0, -2.0)
				},
				(UIElement)new Ellipse
				{
					Width = 5.0,
					Height = 5.0,
					Fill = Ui.Brush(current.IncomingBubble),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Bottom,
					Margin = new Thickness(-7.0, 0.0, 0.0, -8.0)
				},
				(UIElement)new Border
				{
					CornerRadius = new CornerRadius(15.0),
					Background = Ui.Brush(current.IncomingBubble),
					Padding = new Thickness(13.0, 0.0, 13.0, 0.0),
					Child = stackPanel
				}
			}
		};
		AutomationProperties.SetName(obj, "Working");
		return obj;
	}

	private static double Wave(double u)
	{
		double num = (u % 1.0 + 1.0) % 1.0;
		if (!(num < 0.5))
		{
			return Smooth(2.0 - num * 2.0);
		}
		return Smooth(num * 2.0);
	}

	private static double Smooth(double t)
	{
		return t * t * (3.0 - 2.0 * t);
	}

	public static FrameworkElement DateSeparator(DateTimeOffset date)
	{
		DateTime dateTime = date.ToLocalTime().DateTime;
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
		string text = dateTime.ToString("h:mm tt", cultureInfo);
		bool flag = dateTime.Date == DateTime.Today || dateTime.Date == DateTime.Today.AddDays(-1.0);
		string text2 = ((dateTime.Date == DateTime.Today) ? "Today" : ((dateTime.Date == DateTime.Today.AddDays(-1.0)) ? "Yesterday" : dateTime.ToString("ddd, MMM d", cultureInfo)));
		TextBlock textBlock = new TextBlock
		{
			FontSize = 11.0,
			FontFamily = DS.Fonts.Small,
			Foreground = Ui.Brush(MessagesPalette.Current.TertiaryText),
			HorizontalAlignment = HorizontalAlignment.Center,
			IsTextScaleFactorEnabled = false
		};
		textBlock.Inlines.Add(new Run
		{
			Text = text2,
			FontWeight = FontWeights.SemiBold
		});
		textBlock.Inlines.Add(new Run
		{
			Text = (flag ? " " : " at ") + text
		});
		return new Border
		{
			Padding = new Thickness(0.0, 6.0, 0.0, 6.0),
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			Child = textBlock
		};
	}

	public static string ShortTime(DateTimeOffset date)
	{
		return date.ToLocalTime().DateTime.ToString("h:mm tt", CultureInfo.GetCultureInfo("en-US"));
	}

	public static string Glyph(string sfSymbol)
	{
		switch (sfSymbol)
		{
		case "repeat":
			return "\ue8ee";
		case "magnifyingglass":
			return "\ue721";
		case "clock.arrow.circlepath":
			return "\ue81c";
		case "bookmark.fill":
			return "\ue8a4";
		case "eraser":
			return "\ue75c";
		case "safari":
			return "\ue774";
		case "play.circle.fill":
			return "\ue768";
		case "doc.on.clipboard":
			return "\ue77f";
		case "keyboard":
			return "\ue765";
		case "paperplane.fill":
			return "\ue724";
		case "arrowshape.turn.up.right.fill":
			return "\ue72a";
		case "stop.circle":
			return "\ue71a";
		case "checkmark.bubble":
			return "\ue8bd";
		case "hourglass":
			return "\ue823";
		case "person.text.rectangle":
			return "\ue77b";
		case "doc.on.doc":
			return "\ue8c8";
		case "doc.text":
		case "doc.text.magnifyingglass":
			return "\ue8a5";
		case "folder":
			return "\ue8b7";
		case "house":
			return "\ue80f";
		case "terminal":
			return "\ue756";
		case "camera.viewfinder":
			return "\ue722";
		case "calendar":
		case "calendar.badge.plus":
			return "\ue787";
		case "square.and.arrow.down":
			return "\ue896";
		case "brain":
			return "\ue82f";
		case "photo":
			return "\ue91b";
		case "forward":
			return "\ue893";
		case "tray.full":
			return "\ue8f1";
		case "link":
			return "\ue71b";
		default:
			return "\ue90f";
		}
	}
}
