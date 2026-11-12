using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class HandoffSurface : NotchSurface
{
	private const string QueuedGlyph = "\ue710";

	private readonly StackPanel view;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	public override FrameworkElement View => view;

	public unsafe HandoffSurface(NotchLiveEvent e)
	{
		object obj = ((e is NotchLiveEvent.HandoffQueued handoffQueued) ? ("\ue710", (handoffQueued.Count == 1) ? "Region queued" : $"{handoffQueued.Count} regions queued", "Pick text or voice to send.") : ((!(e is NotchLiveEvent.HandoffSent handoffSent)) ? ((object)("\ue724", "", "")) : ((object)("\ue724", "Sent", "Pasted into " + handoffSent.TargetDisplayName + "."))));
		string item = ((ValueTuple<string, string, string>*)(&obj))->Item1;
		string item2 = ((ValueTuple<string, string, string>*)(&obj))->Item2;
		string item3 = ((ValueTuple<string, string, string>*)(&obj))->Item3;
		TextBlock textBlock = Icons.Glyph(item, 11.0, DS.Colors.Blue300);
		textBlock.FontWeight = FontWeights.Medium;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text(item2, 11.0, FontWeights.Medium, DS.Colors.TextPrimary),
				(UIElement)Ui.Text(item3, 10.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Left, 1)
			}
		};
		view = Ui.H(8.0, textBlock, stackPanel);
		view.Margin = new Thickness(12.0, 8.0, 12.0, 8.0);
		view.HorizontalAlignment = HorizontalAlignment.Left;
	}
}
