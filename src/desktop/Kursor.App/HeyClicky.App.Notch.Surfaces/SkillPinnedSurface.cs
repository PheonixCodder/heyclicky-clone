using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class SkillPinnedSurface : NotchSurface
{
	private const double AvatarSize = 20.0;

	private readonly Grid view;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	public override FrameworkElement View => view;

	public SkillPinnedSurface(NotchLiveEvent.SkillPinned e)
	{
		int num = Math.Clamp(e.Count, 1, 3);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = -8.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		for (int i = 0; i < num; i++)
		{
			IReadOnlyList<string> names = e.Names;
			string obj = ((names != null && i < names.Count) ? names[i] : $"Skill {i + 1}");
			FrameworkElement frameworkElement = PowerUpAvatar(obj, obj, 20.0);
			Canvas.SetZIndex(frameworkElement, num - i);
			stackPanel.Children.Add(frameworkElement);
		}
		view = new Grid
		{
			Margin = new Thickness(12.0, 0.0, 6.0, 0.0),
			IsHitTestVisible = false,
			Children = { (UIElement)stackPanel }
		};
	}

	public static FrameworkElement PowerUpAvatar(string identityId, string displayName, double size)
	{
		(uint, uint)[] array = new(uint, uint)[6]
		{
			(6514417u, 9133302u),
			(15485081u, 16347926u),
			(440020u, 3900150u),
			(1096065u, 8702998u),
			(16096779u, 15680580u),
			(9133302u, 15485081u)
		};
		int num = 0;
		foreach (char c in identityId)
		{
			num = (num * 31 + c) & 0x7FFFFFFF;
		}
		(uint, uint) tuple = array[num % array.Length];
		uint item = tuple.Item1;
		uint item2 = tuple.Item2;
		double num2 = size * 0.24;
		Grid obj = new Grid
		{
			Width = size,
			Height = size,
			Children = { (UIElement)new ShadowLayer(num2, DS.Black(0.3), 2.0, 1.0) }
		};
		Border border = new Border
		{
			CornerRadius = new CornerRadius(num2),
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0f, 0f),
				EndPoint = new Point(1f, 1f),
				GradientStops = 
				{
					new GradientStop
					{
						Color = DS.Hex(item),
						Offset = 0.0
					},
					new GradientStop
					{
						Color = DS.Hex(item2),
						Offset = 1.0
					}
				}
			},
			BorderBrush = Ui.Brush(DS.White(0.9)),
			BorderThickness = new Thickness(Math.Max(0.5, size * 0.04))
		};
		string[] array2 = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string text = ((array2.Length == 0) ? "•" : ((array2.Length > 1) ? $"{array2[0][0]}{array2[1][0]}".ToUpperInvariant() : array2[0].Substring(0, 1).ToUpperInvariant()));
		border.Child = Ui.Text(text, size * 0.42, FontWeights.Bold, Colors.White, TextAlignment.Center).Center();
		obj.Children.Add(border);
		return obj;
	}
}
