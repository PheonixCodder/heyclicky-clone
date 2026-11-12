using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchLabel : Grid
{
	public ClickySketchLabel(string text, ClickySketchTone tone = ClickySketchTone.Blue1)
	{
		base.Children.Add(new ClickyHandText(text, 18.0, 1.2, 1).WithForeground(tone.SoftWords()));
	}
}
