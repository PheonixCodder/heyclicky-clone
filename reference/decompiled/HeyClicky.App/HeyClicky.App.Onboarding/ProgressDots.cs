using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Onboarding;

public sealed class ProgressDots : StackPanel
{
	public ProgressDots(int count, int current, Action<int>? jump, int furthest)
	{
		base.Orientation = Orientation.Horizontal;
		base.Spacing = 7.0;
		base.HorizontalAlignment = HorizontalAlignment.Center;
		for (int i = 0; i < count; i++)
		{
			bool flag = i <= current;
			GelSurface gelSurface = new GelSurface((!flag) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, 3.0)
			{
				Width = ((i == current) ? 22 : 6),
				Height = 6.0,
				Opacity = (flag ? 1.0 : 0.3)
			};
			if (i > furthest)
			{
				gelSurface.Enabled = false;
			}
			Pressable pressable = new Pressable
			{
				Padding = new Thickness(2.0, 5.0, 2.0, 5.0),
				Enabled = (i <= furthest)
			};
			pressable.Children.Add(gelSurface);
			int idx = i;
			if (jump != null && i <= furthest)
			{
				pressable.Click += delegate
				{
					jump(idx);
				};
			}
			AutomationProperties.SetName(pressable, (i <= furthest) ? $"Go to step {i + 1}" : $"Step {i + 1}, not reached yet");
			base.Children.Add(pressable);
		}
	}
}
