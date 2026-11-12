using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Onboarding;

public sealed class MicMeter : Grid
{
	private readonly Rectangle[] bars = new Rectangle[16];

	private readonly Color tint;

	private double display;

	public MicMeter(Color tint)
	{
		this.tint = tint;
		base.Width = 234.0;
		base.Height = 132.0;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		for (int i = 0; i < 16; i++)
		{
			bars[i] = new Rectangle
			{
				Width = 9.0,
				Height = 26.0,
				RadiusX = 4.5,
				RadiusY = 4.5,
				Fill = Ui.Brush(DS.White(0.14)),
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel.Children.Add(bars[i]);
		}
		base.Children.Add(stackPanel);
	}

	public void SetLevel(double level)
	{
		display = Math.Max(level, display * 0.82);
		double num = Math.Max(0.0, (display - 0.14) / 0.86);
		for (int i = 0; i < 16; i++)
		{
			double num2 = Math.Abs((double)i - 7.5) / 7.5;
			bool flag = num > num2 * 0.85;
			bars[i].Height = (flag ? (30.0 + num * (24.0 + (1.0 - num2) * 56.0)) : 26.0);
			bars[i].Fill = Ui.Brush(flag ? tint : DS.White(0.14));
		}
	}
}
