using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyHandText : Panel
{
	private readonly string text;

	private readonly PaperFont font;

	private readonly ClickyHandTextMetrics metrics;

	private readonly int? lineLimit;

	private readonly double minimumScale;

	private readonly TextAlignment alignment;

	private readonly TextBlock? single;

	private IReadOnlyList<string> lines = Array.Empty<string>();

	private Color foreground = PaperColor.Black;

	private readonly bool cssLineLimitOne;

	public string Text => text;

	public PaperFont Font => font;

	public ClickyHandTextMetrics Metrics => metrics;

	public double FirstBaseline => metrics.HalfLeading + ClickyHandLineBreaker.Ascent(font);

	public double LastBaseline => (double)Math.Max(1, ((object)single != null) ? 1 : lines.Count) * metrics.Pitch - metrics.Pitch + FirstBaseline;

	public Color Foreground
	{
		get
		{
			return foreground;
		}
		set
		{
			foreground = value;
			SolidColorBrush solidColorBrush = Ui.Brush(value);
			if ((object)single != null)
			{
				single.Foreground = solidColorBrush;
			}
			foreach (UIElement child in base.Children)
			{
				if (child is TextBlock textBlock)
				{
					textBlock.Foreground = solidColorBrush;
				}
			}
		}
	}

	public ClickyHandText(string text, double size, double lineHeight = 1.2, int? lineLimit = null, double minimumScale = 1.0, TextAlignment alignment = TextAlignment.Left)
		: this(text, ClickyPaperFonts.Hand(size), ClickyHandTextMetrics.For(ClickyPaperFonts.Hand(size), size, lineHeight), lineLimit, minimumScale, alignment, paragraphOnly: false)
	{
	}

	private ClickyHandText(string text, PaperFont font, ClickyHandTextMetrics metrics, int? lineLimit, double minimumScale, TextAlignment alignment, bool paragraphOnly)
	{
		cssLineLimitOne = paragraphOnly && lineLimit == 1;
		this.text = text;
		this.font = font;
		this.metrics = metrics;
		this.lineLimit = lineLimit;
		this.minimumScale = minimumScale;
		this.alignment = alignment;
		AutomationProperties.SetName(this, text);
		if (lineLimit == 1 && !paragraphOnly)
		{
			single = font.Apply(new TextBlock
			{
				Text = text,
				TextWrapping = TextWrapping.NoWrap,
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextAlignment = alignment,
				Margin = new Thickness(0.0, metrics.HalfLeading, 0.0, metrics.HalfLeading),
				Foreground = Ui.Brush(foreground)
			});
			UIElementCollection children = base.Children;
			FrameworkElement item;
			if (minimumScale < 1.0)
			{
				TextBlock t = single;
				item = Ui.Shrink(t, alignment switch
				{
					TextAlignment.Center => HorizontalAlignment.Center, 
					TextAlignment.Right => HorizontalAlignment.Right, 
					_ => HorizontalAlignment.Left, 
				}, minimumScale);
			}
			else
			{
				item = single;
			}
			children.Add(item);
		}
	}

	public static ClickyHandText CssText(string text, PaperFont font, double lineHeight, TextAlignment alignment = TextAlignment.Left, int? lineLimit = null)
	{
		return new ClickyHandText(text, font, ClickyHandTextMetrics.For(font, font.Size, lineHeight / font.Size), lineLimit, 1.0, alignment, paragraphOnly: true);
	}

	public ClickyHandText WithForeground(Color color)
	{
		Foreground = color;
		return this;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if ((object)single != null)
		{
			FrameworkElement obj = (FrameworkElement)base.Children[0];
			obj.Measure(availableSize);
			return obj.DesiredSize;
		}
		double width = availableSize.Width;
		IReadOnlyList<string> readOnlyList = ClickyHandLineBreaker.Lines(text, font, width, cssLineLimitOne ? new int?(1) : lineLimit);
		SetLines(readOnlyList);
		double num = Math.Ceiling(readOnlyList.Select((string l) => ClickyHandLineBreaker.Width(l, font)).DefaultIfEmpty(0.0).Max());
		double width2 = (double.IsFinite(width) ? Math.Min(num, width) : num);
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		}
		return new Size(width2, (double)readOnlyList.Count * metrics.Pitch);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if ((object)single != null)
		{
			base.Children[0].Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
			return finalSize;
		}
		for (int i = 0; i < base.Children.Count; i++)
		{
			TextBlock textBlock = (TextBlock)base.Children[i];
			textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			double width = textBlock.DesiredSize.Width;
			double x = alignment switch
			{
				TextAlignment.Center => (finalSize.Width - width) / 2.0, 
				TextAlignment.Right => finalSize.Width - width, 
				_ => 0.0, 
			};
			double y = (double)i * metrics.Pitch + metrics.HalfLeading;
			textBlock.Arrange(new Rect(x, y, width, textBlock.DesiredSize.Height));
		}
		return finalSize;
	}

	private void SetLines(IReadOnlyList<string> broken)
	{
		if (lines.SequenceEqual(broken))
		{
			return;
		}
		lines = broken;
		SolidColorBrush solidColorBrush = Ui.Brush(foreground);
		while (base.Children.Count > broken.Count)
		{
			base.Children.RemoveAt(base.Children.Count - 1);
		}
		for (int i = 0; i < broken.Count; i++)
		{
			if (i < base.Children.Count)
			{
				((TextBlock)base.Children[i]).Text = broken[i];
				continue;
			}
			TextBlock textBlock = font.Apply(new TextBlock
			{
				Text = broken[i],
				TextWrapping = TextWrapping.NoWrap,
				Foreground = solidColorBrush
			});
			AutomationProperties.SetAccessibilityView(textBlock, AccessibilityView.Raw);
			base.Children.Add(textBlock);
		}
	}
}
