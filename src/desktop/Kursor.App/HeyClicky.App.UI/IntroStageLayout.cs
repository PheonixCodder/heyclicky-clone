using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.UI;

public sealed class IntroStageLayout : Panel
{
	private const double FooterBottomInset = 16.0;

	public UIElement? Header { get; set; }

	public UIElement? Body { get; set; }

	public UIElement? Footer { get; set; }

	public bool HasFooter { get; set; } = true;

	public double MinimumHeight { get; set; }

	private double ContentOffset => (!HasFooter) ? 24 : 0;

	public void SetParts(UIElement header, UIElement body, UIElement? footer, bool hasFooter)
	{
		base.Children.Clear();
		Header = header;
		Body = body;
		Footer = footer;
		HasFooter = hasFooter;
		base.Children.Add(header);
		base.Children.Add(body);
		if ((object)footer != null)
		{
			base.Children.Add(footer);
		}
		InvalidateMeasure();
	}

	protected override Size MeasureOverride(Size available)
	{
		double width = (double.IsInfinity(available.Width) ? 392.0 : available.Width);
		Size availableSize = new Size(width, double.PositiveInfinity);
		Header?.Measure(availableSize);
		Body?.Measure(availableSize);
		Footer?.Measure(availableSize);
		double num = Header?.DesiredSize.Height ?? 0.0;
		double num2 = ((HasFooter && (object)Footer != null) ? (Footer.DesiredSize.Height + 16.0) : 0.0);
		double val = num - ContentOffset;
		double val2 = num2 + ContentOffset;
		double num3 = Body?.DesiredSize.Height ?? 0.0;
		double height = Math.Max(MinimumHeight, num3 + 2.0 * Math.Max(val, val2));
		return new Size(width, height);
	}

	protected override Size ArrangeOverride(Size final)
	{
		Header?.Arrange(new Rect(0.0, 0.0, final.Width, Header.DesiredSize.Height));
		if ((object)Body != null)
		{
			Size desiredSize = Body.DesiredSize;
			double num = final.Height / 2.0 + ContentOffset;
			Body.Arrange(new Rect(0.0, num - desiredSize.Height / 2.0, final.Width, desiredSize.Height));
		}
		if ((object)Footer != null)
		{
			Size desiredSize2 = Footer.DesiredSize;
			Footer.Arrange(new Rect(0.0, final.Height - 16.0 - desiredSize2.Height, final.Width, desiredSize2.Height));
		}
		return final;
	}
}
