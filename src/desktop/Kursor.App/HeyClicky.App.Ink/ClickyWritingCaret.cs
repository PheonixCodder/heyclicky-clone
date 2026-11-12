using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyWritingCaret : Grid
{
	public double Baseline => base.Height - base.Height * 0.18;

	public ClickyWritingCaret(double height = 18.0)
	{
		base.Width = Math.Max(1.6, height / 10.0);
		base.Height = height;
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(base.Width / 2.0),
			Background = Ui.Brush(ClickyPaperInk.ClickyBlue)
		});
		base.IsHitTestVisible = false;
		AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
	}
}
