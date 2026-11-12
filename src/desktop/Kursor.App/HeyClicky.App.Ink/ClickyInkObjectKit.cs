using System;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkObjectKit
{
	public static FrameworkElement Leading(FrameworkElement content, double width)
	{
		content.HorizontalAlignment = HorizontalAlignment.Left;
		return new Grid
		{
			Width = Math.Max(0.0, width),
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)content }
		};
	}

	public static InkTextView Line(string text, PaperFont font, Color color, int? lineLimit = 1)
	{
		return InkTextView.Plain(text, font, color).With(delegate(InkTextView t)
		{
			t.LineLimit = lineLimit;
		});
	}

	public static void FileMenu(FrameworkElement target, string rawPath, string resolvedPath, bool isWebLink, Action open)
	{
		MenuFlyout menuFlyout = new MenuFlyout();
		MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
		{
			Text = "Open"
		};
		menuFlyoutItem.Click += delegate
		{
			open();
		};
		menuFlyout.Items.Add(menuFlyoutItem);
		if (!isWebLink)
		{
			MenuFlyoutItem menuFlyoutItem2 = new MenuFlyoutItem
			{
				Text = "Show in Explorer"
			};
			menuFlyoutItem2.Click += delegate
			{
				ArtifactPaths.Reveal(resolvedPath);
			};
			menuFlyout.Items.Add(menuFlyoutItem2);
		}
		target.ContextFlyout = menuFlyout;
	}

	public static FrameworkElement Favicon(string urlString, double size, FrameworkElement fallback)
	{
		if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri result) || string.IsNullOrEmpty(result.Host))
		{
			return new Grid
			{
				Width = size,
				Height = size,
				Children = { (UIElement)fallback }
			};
		}
		return IntegrationLogos.Remote(ClickyInkImages.WorkerBaseUrl.TrimEnd('/') + "/favicon?host=" + Uri.EscapeDataString(result.Host.ToLowerInvariant()) + "&sz=128", size, fallback);
	}
}
