using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperProgressBar : Grid
{
	public ClickyPaperProgressBar(double fraction, double width = 74.0, double height = 4.0, Color? color = null, Color? track = null)
	{
		Color c = color ?? PaperColor.Hex("#2F7CF6");
		base.Width = width;
		base.Height = height;
		base.IsHitTestVisible = false;
		base.Children.Add(new Border
		{
			Background = Ui.Brush(track ?? PaperColor.Hex("#2F7CF6").Opacity(0.18)),
			CornerRadius = new CornerRadius(height / 2.0)
		});
		base.Children.Add(new Border
		{
			Background = Ui.Brush(c),
			CornerRadius = new CornerRadius(height / 2.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = Math.Max(height, width * Math.Min(1.0, Math.Max(0.0, fraction)))
		});
		AutomationProperties.SetItemStatus(this, $"{(int)(fraction * 100.0)} percent");
	}
}
