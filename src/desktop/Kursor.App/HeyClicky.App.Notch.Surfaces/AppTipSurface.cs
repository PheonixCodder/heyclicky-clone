using System;
using System.IO;
using HeyClicky.App.Design;
using HeyClicky.App.Proactive;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class AppTipSurface : NotchSurface
{
	private readonly Grid view;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	public override FrameworkElement View => view;

	public AppTipSurface(NotchLiveEvent e)
	{
		AppTipSurface appTipSurface = this;
		Type dismissType = e.GetType();
		string text;
		string text2;
		int num;
		string text3;
		string iconGlyph;
		FrameworkElement content;
		if (e is NotchLiveEvent.SidecarLearningSuggestion sidecarLearningSuggestion)
		{
			text = "I can help you with " + sidecarLearningSuggestion.AppDisplayName;
			text2 = "I have a special skill file for " + sidecarLearningSuggestion.AppDisplayName + " so my AI can better answer questions.";
			num = 2;
			text3 = "Activate Skill";
			iconGlyph = "\uf4a5";
			content = AppIcon(sidecarLearningSuggestion.AppIconPath);
		}
		else
		{
			text = "I can play songs on Spotify";
			text2 = "Try saying \"Play Good Morning by Kanye West on Spotify\"";
			num = 1;
			text3 = "Got it";
			iconGlyph = "\ue73e";
			content = IntegrationLogos.Image("spotify-logo", 24.0, null);
		}
		Canvas canvas = new Canvas
		{
			Width = 58.0,
			Height = 38.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		FrameworkElement frameworkElement = IntegrationLogos.Image("heyclicky-logo", 24.0, null);
		Ui.ClipRounded(frameworkElement, 5.5);
		Grid grid = CardParts.Tile(frameworkElement);
		Canvas.SetLeft(grid, -10.0);
		Canvas.SetTop(grid, 2.0);
		Grid grid2 = CardParts.Tile(content);
		Canvas.SetLeft(grid2, 34.0);
		Canvas.SetTop(grid2, 2.0);
		canvas.Children.Add(grid);
		canvas.Children.Add(grid2);
		TextBlock textBlock = Ui.Text(text, 12.5, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1);
		TextBlock textBlock2 = Ui.Text(text2, 10.5, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Left, num, num > 1);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)((num > 1) ? textBlock : Ui.Shrink(textBlock)),
				(UIElement)((num > 1) ? textBlock2 : Ui.Shrink(textBlock2))
			}
		};
		GelActionButton gelActionButton = new GelActionButton(text3, iconGlyph, GelAppearance.PrimaryBlue, 28.0, 12.0, 86.0, 10.5).Tip(text3);
		gelActionButton.VerticalAlignment = VerticalAlignment.Center;
		gelActionButton.Click += delegate
		{
			if (dismissType == typeof(NotchLiveEvent.SpotifyVoiceTip))
			{
				ProactiveCoordinator current = ProactiveCoordinator.Current;
				if (current != null)
				{
					current.AcknowledgeSpotifyTip();
					return;
				}
			}
			appTipSurface.Host.Services.Presenter.DismissLiveEvent(dismissType);
		};
		view = new Grid
		{
			ColumnSpacing = 20.0,
			Margin = new Thickness(16.0, 4.0, 16.0, 0.0),
			RenderTransform = new TranslateTransform
			{
				Y = 6.0
			}
		};
		view.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		view.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		view.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(gelActionButton, 2);
		gelActionButton.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
		view.Children.Add(canvas);
		view.Children.Add(stackPanel);
		view.Children.Add(gelActionButton);
	}

	private static FrameworkElement AppIcon(string? path)
	{
		bool flag = path != null && path.Length > 0 && File.Exists(path);
		if (flag)
		{
			bool flag2;
			switch (Path.GetExtension(path).ToLowerInvariant())
			{
			case ".png":
			case ".jpg":
			case ".jpeg":
			case ".ico":
			case ".bmp":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			return new Image
			{
				Source = new BitmapImage(new Uri(path)),
				Width = 24.0,
				Height = 24.0,
				Stretch = Stretch.Uniform
			};
		}
		TextBlock textBlock = Icons.Glyph("\ue9ce", 16.0, DS.Colors.TextSecondary);
		textBlock.FontWeight = FontWeights.SemiBold;
		return textBlock;
	}
}
