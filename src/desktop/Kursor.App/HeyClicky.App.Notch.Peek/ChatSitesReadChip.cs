using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ChatSitesReadChip : StackPanel
{
	public ChatSitesReadChip(IReadOnlyList<string> hosts)
	{
		MessagesPalette current = MessagesPalette.Current;
		base.Spacing = 6.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		string text = ((hosts.Count == 1) ? ("Read " + hosts[0]) : $"Read {hosts.Count} sites");
		TextBlock chevron = Icons.Glyph("\ue70d", 8.0, current.TertiaryText);
		chevron.FontWeight = FontWeights.Bold;
		chevron.RenderTransformOrigin = new Point(0.5, 0.5);
		chevron.RenderTransform = new RotateTransform();
		TextBlock textBlock = Ui.Text(text, 11.0, FontWeights.Medium, current.SecondaryText, TextAlignment.Left, 1);
		LiveLineIconStack liveLineIconStack = new LiveLineIconStack(hosts.Take(4).Select(ClickyLiveLineIcon.Site).ToList(), Math.Max(0, hosts.Count - 4), 15.0, current.PrimaryText);
		StackPanel stackPanel = Ui.H(6.0, liveLineIconStack, textBlock, chevron);
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		Pressable button = new Pressable
		{
			CornerRadius = new CornerRadius(999.0),
			Padding = new Thickness(5.0, 4.0, 9.0, 4.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Ui.Brush(Color.FromArgb(17, byte.MaxValue, byte.MaxValue, byte.MaxValue))
		};
		button.Children.Add(stackPanel);
		button.PointerEntered += delegate
		{
			button.Background = Ui.Brush(Color.FromArgb(30, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		};
		button.PointerExited += delegate
		{
			button.Background = Ui.Brush(Color.FromArgb(17, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		};
		AutomationProperties.SetName(button, text);
		base.Children.Add(button);
		WrapPanel list = new WrapPanel
		{
			HorizontalSpacing = 5.0,
			VerticalSpacing = 5.0,
			MaxWidth = 420.0,
			Visibility = Visibility.Collapsed
		};
		foreach (string host in hosts)
		{
			FrameworkElement frameworkElement = ClickyInkObjectKit.Favicon("https://" + host, 12.0, Icons.Glyph("\ue774", 9.0, current.TertiaryText));
			TextBlock textBlock2 = Ui.Text(host, 10.5, FontWeights.Medium, current.SecondaryText, TextAlignment.Left, 1);
			list.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(999.0),
				Padding = new Thickness(7.0, 3.0, 7.0, 3.0),
				Background = Ui.Brush(Color.FromArgb(12, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
				Child = Ui.H(5.0, frameworkElement, textBlock2)
			});
		}
		base.Children.Add(list);
		button.Click += delegate
		{
			bool flag = list.Visibility != Visibility.Visible;
			list.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			((RotateTransform)chevron.RenderTransform).Angle = (flag ? 180 : 0);
		};
	}
}
