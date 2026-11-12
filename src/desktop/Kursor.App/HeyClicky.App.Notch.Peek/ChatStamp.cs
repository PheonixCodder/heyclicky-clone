using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public static class ChatStamp
{
	public enum Status
	{
		NeedsYou,
		Working,
		Ready,
		Paused
	}

	public static Color OnBlack(Status status)
	{
		return status switch
		{
			Status.NeedsYou => DS.Hex(16758087u), 
			Status.Working => DS.Hex(9356799u), 
			Status.Ready => DS.Hex(6017162u), 
			_ => DS.Hex(9474976u), 
		};
	}

	public static FrameworkElement Create(Status status, string word, int seed)
	{
		Color c = OnBlack(status);
		TextBlock child = new TextBlock
		{
			Text = word.ToUpperInvariant(),
			FontSize = 7.5,
			FontWeight = FontWeights.Black,
			FontFamily = DS.Fonts.Small,
			Foreground = Ui.Brush(c),
			CharacterSpacing = 90,
			IsTextScaleFactorEnabled = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Border box = new Border
		{
			BorderBrush = Ui.Brush(c),
			BorderThickness = new Thickness(1.7),
			CornerRadius = new CornerRadius(3.5),
			Padding = new Thickness(7.0, 3.0, 7.0, 3.0),
			Child = child,
			Opacity = 0.92,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(1.0)
		};
		box.Rotation = seed % 5 - 6;
		box.Loaded += delegate
		{
			box.CenterPoint = new Vector3((float)box.ActualWidth / 2f, (float)box.ActualHeight / 2f, 0f);
		};
		AutomationProperties.SetName(box, word);
		return box;
	}

	public static int Seed(string key)
	{
		int num = 17;
		foreach (char c in key)
		{
			num = num * 31 + c;
		}
		return Math.Abs(num);
	}
}
