using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public static class NotchTopFade
{
	public static Border Create(double bodyTopOffsetIntoRamp, double height = 52.0)
	{
		List<(double, Color)> list = new List<(double, Color)>();
		for (int i = 0; i <= 8; i++)
		{
			double num = (double)i / 8.0;
			double num2 = bodyTopOffsetIntoRamp + num * height;
			double num3 = Math.Min(1.0, num2 / 56.0) * 30.0 / 255.0;
			double a = 1.0 - num * num * (3.0 - 2.0 * num);
			list.Add((num, DS.Rgb(num3, num3, num3, a)));
		}
		return new Border
		{
			Height = height,
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false,
			Background = Ui.Vertical(list.ToArray())
		};
	}
}
