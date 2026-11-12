using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class DictationClipboardSurface : NotchSurface
{
	private readonly Grid view;

	public override FrameworkElement View => view;

	public DictationClipboardSurface()
	{
		StackPanel stackPanel = CardParts.CenteredTitle("Copied to your clipboard", "Press Ctrl+V to paste");
		stackPanel.Margin = new Thickness(34.0, 34.0, 34.0, 0.0);
		stackPanel.VerticalAlignment = VerticalAlignment.Top;
		RingCloseButton ringCloseButton = new RingCloseButton(showRing: true);
		ringCloseButton.Click += delegate
		{
			base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.DictationCopiedToClipboard));
		};
		ringCloseButton.HorizontalAlignment = HorizontalAlignment.Right;
		ringCloseButton.VerticalAlignment = VerticalAlignment.Top;
		ringCloseButton.Margin = new Thickness(0.0, NotchGeometry.HasHardwareNotchEar ? 2 : 8, 10.0, 0.0);
		ringCloseButton.StartCountdown(6.0);
		view = CardParts.Stack(stackPanel, ringCloseButton);
	}
}
