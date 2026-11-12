using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Dock;

public sealed class DeckMoreButton : Pressable
{
	public DeckMoreButton(int count, bool pointsLeft, string help, Action action)
	{
		DeckMoreButton deckMoreButton = this;
		base.Height = 26.0;
		Border plate = new Border
		{
			Background = Ui.Vertical((0.0, DS.Rgb(0.19, 0.19, 0.19)), (1.0, DS.Rgb(0.1, 0.1, 0.1))),
			BorderBrush = Ui.Brush(DS.White(0.16)),
			BorderThickness = new Thickness(1.0)
		}.Capsule();
		base.Children.Add(new ShadowLayer(13.0, DS.Black(0.35), 8.0, 3.0));
		base.Children.Add(plate);
		TextBlock arrow = Icons.Glyph(pointsLeft ? "\ue76b" : "\ue76c", 10.0, DS.White(0.86));
		arrow.FontWeight = FontWeights.Bold;
		TextBlock words = Ui.Text($"{count} more", 12.0, FontWeights.SemiBold, DS.White(0.86));
		Typography.SetNumeralAlignment(words, FontNumeralAlignment.Tabular);
		StackPanel stackPanel = Ui.H(5.0, arrow, words);
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
		base.Children.Add(stackPanel);
		base.HoverChanged += delegate(bool h)
		{
			plate.Background = Ui.Vertical((0.0, DS.Rgb(h ? 0.24 : 0.19, h ? 0.24 : 0.19, h ? 0.24 : 0.19)), (1.0, DS.Rgb(0.1, 0.1, 0.1)));
			plate.BorderBrush = Ui.Brush(DS.White(h ? 0.28 : 0.16));
			TextBlock textBlock = arrow;
			Brush foreground = (words.Foreground = Ui.Brush(DS.White(h ? 1.0 : 0.86)));
			textBlock.Foreground = foreground;
			deckMoreButton.CenterPoint = new Vector3((float)deckMoreButton.ActualWidth / 2f, (float)deckMoreButton.ActualHeight / 2f, 0f);
			Anim.Scale(deckMoreButton, h ? 1.05f : 1f, 0.14, null, null);
		};
		WithPressScale();
		base.Click += action;
		ToolTipService.SetToolTip(this, help);
		AutomationProperties.SetName(this, help);
	}
}
