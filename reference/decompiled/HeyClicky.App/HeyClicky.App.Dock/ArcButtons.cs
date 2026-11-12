using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Dock;

public static class ArcButtons
{
	public static GelButton Gel(string title, string? symbol, GelAppearance appearance, double? minimumHeight = 32.0, double? labelFontSize = 13.5, GelButton.Sizing size = GelButton.Sizing.Normal, Action? click = null)
	{
		GelButton gelButton = new GelButton(title, appearance, size, 100.0, null, labelFontSize, minimumHeight);
		if (symbol != null)
		{
			StackPanel stackPanel = FindRow(gelButton);
			if ((object)stackPanel != null)
			{
				double size2 = (labelFontSize ?? 12.5) * 0.93;
				stackPanel.Children.Insert(0, ArcSymbols.Make(symbol, size2, (appearance == GelAppearance.SecondaryOutline) ? DS.White(0.86) : Colors.Black));
				stackPanel.Spacing = 6.0;
			}
		}
		if (click != null)
		{
			gelButton.Click += click;
		}
		return gelButton;
	}

	private static StackPanel? FindRow(Grid button)
	{
		foreach (UIElement child in button.Children)
		{
			if (child is Grid grid && grid.Children.Count > 0 && grid.Children[0] is StackPanel result)
			{
				return result;
			}
		}
		return null;
	}
}
