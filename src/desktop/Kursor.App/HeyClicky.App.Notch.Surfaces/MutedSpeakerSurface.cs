using System.Threading.Tasks;
using HeyClicky.App.UI;
using HeyClicky.Platform.Audio;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class MutedSpeakerSurface : NotchSurface
{
	private readonly Grid view;

	public override FrameworkElement View => view;

	public MutedSpeakerSurface(NotchLiveEvent.VoiceReplyMutedCopiedToClipboard e)
	{
		string kind = e.Kind;
		(string, string) obj = ((kind == "muted") ? ("Your PC is muted", "Unmute") : ((!(kind == "sound_came_back")) ? ("Your volume is at zero", "Set volume to 50%") : ("That reply wasn't heard", "")));
		string item = obj.Item1;
		string item2 = obj.Item2;
		StackPanel item3 = CardParts.CenteredTitle(item, "The answer is on your clipboard. Press Ctrl+V to paste");
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0,
			Margin = new Thickness(34.0, 34.0, 34.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top,
			Children = { (UIElement)item3 }
		};
		if (item2.Length > 0)
		{
			GelActionButton gelActionButton = new GelActionButton(item2, "\ue767", GelAppearance.PrimaryBlue, 32.0, 16.0, 0.0, 12.0, 11.5, 6.0).Tip("Turn your sound back on");
			gelActionButton.HorizontalAlignment = HorizontalAlignment.Center;
			gelActionButton.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
			gelActionButton.Click += delegate
			{
				Task.Run(() => SystemAudioOutputVolume.RestoreAudibleOutput());
				base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.VoiceReplyMutedCopiedToClipboard));
			};
			stackPanel.Children.Add(gelActionButton);
		}
		RingCloseButton ringCloseButton = new RingCloseButton(showRing: true).Tip("Dismiss");
		ringCloseButton.Click += delegate
		{
			base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.VoiceReplyMutedCopiedToClipboard));
		};
		ringCloseButton.HorizontalAlignment = HorizontalAlignment.Right;
		ringCloseButton.VerticalAlignment = VerticalAlignment.Top;
		ringCloseButton.Margin = new Thickness(0.0, NotchGeometry.HasHardwareNotchEar ? 2 : 8, 10.0, 0.0);
		ringCloseButton.StartCountdown(10.0);
		view = CardParts.Stack(stackPanel, ringCloseButton);
	}
}
