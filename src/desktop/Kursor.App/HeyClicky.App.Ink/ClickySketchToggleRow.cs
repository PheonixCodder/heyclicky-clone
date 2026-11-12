using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchToggleRow : Grid
{
	public ClickySketchToggle Toggle { get; }

	public ClickySketchToggleRow(string title, bool isOn, ClickySketchTone tone = ClickySketchTone.Blue2)
	{
		Toggle = new ClickySketchToggle(isOn, PaperText.CharacterCount(title), title);
		base.ColumnSpacing = 10.0;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.Children.Add(Toggle);
		Pressable pressable = new Pressable(handCursor: false)
		{
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		pressable.Children.Add(new ClickyHandText(title, 19.0, 1.15, null).WithForeground(tone.Words()));
		pressable.Click += delegate
		{
			Toggle.IsOn = !Toggle.IsOn;
		};
		AutomationProperties.SetAccessibilityView(pressable, AccessibilityView.Raw);
		Grid.SetColumn(pressable, 1);
		base.Children.Add(pressable);
	}
}
