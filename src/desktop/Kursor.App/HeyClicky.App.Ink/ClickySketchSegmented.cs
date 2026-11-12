using System;
using System.Collections.Generic;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchSegmented<TOption> : StackPanel where TOption : notnull
{
	public static double ChoiceWidth(string title, double fontSize)
	{
		return Math.Max(Math.Floor((double)PaperText.CharacterCount(title) * fontSize * 0.48 + 22.0), Math.Ceiling(ClickyHandLineBreaker.Width(title, ClickyPaperFonts.Hand(fontSize)) + 22.0));
	}

	public ClickySketchSegmented(IReadOnlyList<TOption> options, TOption? selection, Func<TOption, string> title, Action<TOption> onSelect, double fontSize = 17.0, long seed = 9L)
	{
		base.Orientation = Orientation.Horizontal;
		base.Spacing = 6.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		for (int i = 0; i < options.Count; i++)
		{
			TOption option = options[i];
			bool flag = EqualityComparer<TOption>.Default.Equals(option, selection);
			Pressable pressable = new Pressable
			{
				Width = ChoiceWidth(title(option), fontSize),
				Height = 30.0
			};
			pressable.Children.Add(new ClickySketchShapeView(flag ? ClickySketchInk.Teal : PaperColor.White.Opacity(0.55), flag ? ClickySketchInk.TealLine : ClickySketchInk.TealLine.Opacity(0.4), 3.0, seed + i, 1.1, isDoubled: false));
			pressable.Children.Add(new ClickyHandText(title(option), fontSize, 1.0, 1)
			{
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}.WithForeground(flag ? ClickySketchInk.TealWords : PaperColor.Hex("#4A6488")));
			pressable.Click += delegate
			{
				onSelect(option);
			};
			ClickySketchPressStyle.Apply(pressable);
			AutomationProperties.SetName(pressable, title(option));
			base.Children.Add(pressable);
		}
	}
}
