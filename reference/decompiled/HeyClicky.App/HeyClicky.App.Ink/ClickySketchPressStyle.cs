using HeyClicky.App.UI;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickySketchPressStyle
{
	public static Pressable Apply(Pressable pressable)
	{
		Border veil = new Border
		{
			Background = Ui.Brush(PaperColor.Black.Opacity(0.04)),
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		pressable.Children.Add(veil);
		pressable.WithPressScale(0.96f, 0.12, easeOut: true);
		pressable.PressedChanged += delegate(bool pressed)
		{
			veil.Opacity = (pressed ? 1 : 0);
		};
		return pressable;
	}
}
