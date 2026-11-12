using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchSwatch : Pressable
{
	public ClickySketchSwatch(Color color, bool isSelected, Action action, double diameter = 24.0, long seed = 13L, string accessibilityTitle = "")
	{
		base.Width = (base.Height = diameter + 10.0);
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		double num2 = diameter + 12.0;
		double num3 = (diameter + 10.0) / num2;
		Grid grid = new Grid
		{
			Width = num2,
			Height = num2,
			RenderTransform = new ScaleTransform
			{
				ScaleX = num3,
				ScaleY = num3
			}
		};
		PaperDrawing paperDrawing = new PaperDrawing();
		double num4 = num2 / 2.0;
		if (isSelected)
		{
			PaperPath p = new ClickySketchRect((diameter + 9.0) / 2.0, seed + 4, 0.45).PathIn(new Rect(num4 - (diameter + 9.0) / 2.0, num4 - (diameter + 9.0) / 2.0, diameter + 9.0, diameter + 9.0));
			paperDrawing.Stroke(p, new PaperStroke(PaperColor.Hex("#2C3A68"), 1.6));
		}
		PaperPath p2 = new ClickySketchRect(diameter / 2.0, seed, 0.35).PathIn(new Rect(num4 - diameter / 2.0, num4 - diameter / 2.0, diameter, diameter));
		paperDrawing.Fill(p2, color);
		paperDrawing.Stroke(p2, new PaperStroke(PaperColor.Black.Opacity(0.25)));
		grid.Children.Add(paperDrawing);
		base.Children.Add(grid);
		base.Click += action;
		ClickySketchPressStyle.Apply(this);
		AutomationProperties.SetName(this, accessibilityTitle);
	}
}
