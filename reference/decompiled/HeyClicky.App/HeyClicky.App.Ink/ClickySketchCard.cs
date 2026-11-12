using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchCard : Grid
{
	public StackPanel Content { get; } = new StackPanel
	{
		Orientation = Orientation.Vertical,
		Spacing = 0.0
	};

	public ClickySketchTone Tone { get; }

	public ClickySketchCard(ClickySketchTone tone, string? label = null, long seed = 1L, bool isFocus = false)
	{
		base.Children.Add(new ClickySketchShapeView(tone.Fill(), tone.Pencil(), 3.0, seed, isFocus ? 2.0 : 1.3, isDoubled: true, hasShelf: true));
		Content.Margin = new Thickness(22.0, 18.0, 22.0, 18.0);
		Content.VerticalAlignment = VerticalAlignment.Top;
		Content.HorizontalAlignment = HorizontalAlignment.Stretch;
		if (label != null)
		{
			Content.Children.Add(new ClickySketchLabel(label, tone));
		}
		base.Children.Add(Content);
		Tone = tone;
	}

	public ClickySketchCard With(params UIElement[] content)
	{
		foreach (UIElement item in content)
		{
			Content.Children.Add(item);
		}
		return this;
	}
}
