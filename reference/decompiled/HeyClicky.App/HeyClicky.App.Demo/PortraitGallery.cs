using System;
using HeyClicky.App.Design;
using HeyClicky.App.Portraits;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Demo;

public sealed class PortraitGallery
{
	public PanelWindow Window { get; }

	public PortraitGallery(bool excludeFromCapture)
	{
		Window = new PanelWindow("Portraits", excludeFromCapture, showInTaskbar: true, 14.0, null);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Margin = new Thickness(18.0)
		};
		PortraitPack[] array = new PortraitPack[5]
		{
			PortraitPack.ClassicClouds,
			PortraitPack.PuffPals,
			PortraitPack.StorybookSpirits,
			PortraitPack.CloudClub,
			PortraitPack.ArcadeAllStars
		};
		foreach (PortraitPack portraitPack in array)
		{
			stackPanel.Children.Add(Ui.Text(portraitPack.Title(), 12.0, FontWeights.SemiBold, DS.Colors.TextSecondary));
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0
			};
			foreach (PortraitCharacter item in PortraitCharacter.CharactersOf(portraitPack))
			{
				stackPanel2.Children.Add(new PortraitView(item.Appearance, 64.0, 64.0, PortraitExpression.Happy, showsAvatarBackground: true, null)
				{
					AnimatesWhileIdle = false
				});
			}
			stackPanel.Children.Add(stackPanel2);
		}
		stackPanel.Children.Add(Ui.Text("Expressions (no plate)", 12.0, FontWeights.SemiBold, DS.Colors.TextSecondary));
		PortraitCharacter[] array2 = new PortraitCharacter[3]
		{
			PortraitCharacter.CharactersOf(PortraitPack.ClassicClouds)[0],
			PortraitCharacter.Sunny,
			PortraitCharacter.CharactersOf(PortraitPack.StorybookSpirits)[1]
		};
		foreach (PortraitCharacter portraitCharacter in array2)
		{
			StackPanel stackPanel3 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 2.0
			};
			PortraitExpression[] values = Enum.GetValues<PortraitExpression>();
			foreach (PortraitExpression expression in values)
			{
				stackPanel3.Children.Add(new PortraitView(portraitCharacter.Appearance, 56.0, 56.0, expression, showsAvatarBackground: false, null)
				{
					AnimatesWhileIdle = false
				});
			}
			stackPanel.Children.Add(stackPanel3);
		}
		stackPanel.Children.Add(Ui.Text("Sizes: notch badge 26, chat 20, peek 36, HUD 40, reveal 64, stage 195", 12.0, FontWeights.SemiBold, DS.Colors.TextSecondary));
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0
		};
		stackPanel4.Children.Add(ClickyFace.NotchBadge("demo-1"));
		stackPanel4.Children.Add(ClickyFace.Create("demo-2", 20.0));
		stackPanel4.Children.Add(ClickyFace.Create("demo-3", 36.0, PortraitMood.Running));
		stackPanel4.Children.Add(ClickyFace.Create("demo-1", 40.0, PortraitMood.Sad));
		stackPanel4.Children.Add(ClickyFace.Create("demo-2", 64.0, PortraitMood.Sleepy));
		PortraitAppearance portraitAppearance = PortraitAppearance.HomeMascot("#3380FF");
		stackPanel4.Children.Add(new PortraitView(portraitAppearance, 175.5, 144.3, PortraitExpression.Excited, showsAvatarBackground: false, portraitAppearance.HairColor.Hair(), 15.6));
		stackPanel4.Children.Add(new PortraitView(PortraitCharacter.Sunny.Appearance, 195.0, 195.0, PortraitExpression.Listening, showsAvatarBackground: false, PortraitCharacter.Sunny.Appearance.HairColor.Hair(), 15.6));
		stackPanel.Children.Add(stackPanel4);
		Window.Panel.Children.Add(stackPanel);
		Window.SetPanelSize(1180.0, 940.0);
	}
}
