using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class MicrophoneTroubleSurface : NotchSurface
{
	private readonly Grid view;

	private readonly RingCloseButton close;

	public override FrameworkElement View => view;

	public MicrophoneTroubleSurface(NotchLiveEvent.MicrophoneTrouble e)
	{
		Grid tile = CardParts.IconTile(e.ShowsMicrophoneSettingsAction ? "\uf781" : "\uec71", 14.0, DS.Hex(2450411u));
		StackPanel lines = CardParts.TwoLines(e.Message, e.Subtitle ?? "HeyClicky can't hear your microphone", smallShrinks: true);
		GelActionButton gelActionButton = null;
		if (e.ShowsMicrophoneSettingsAction)
		{
			gelActionButton = new GelActionButton("Mic settings", null, GelAppearance.PrimaryBlue, 30.0, 14.0).Tip("Open microphone settings to pick a working mic");
			gelActionButton.Click += delegate
			{
				base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MicrophoneTrouble));
				if (base.Host.Actions is AppHost appHost)
				{
					appHost.OpenMicrophoneSettings();
				}
				else
				{
					base.Host.Services.Permissions.OpenSystemSettings(PermissionKind.Microphone);
				}
			};
		}
		close = new RingCloseButton(showRing: true);
		close.Click += delegate
		{
			base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MicrophoneTrouble));
		};
		view = CardParts.HeadsUpRow(tile, lines, gelActionButton, close);
	}

	protected override void OnAttached()
	{
		if (base.Host.Services.Presenter is NotchPresenter notchPresenter)
		{
			(TimeSpan, TimeSpan)? tuple = notchPresenter.LiveEventCountdown(typeof(NotchLiveEvent.MicrophoneTrouble));
			if (tuple.HasValue)
			{
				var (timeSpan, timeSpan2) = tuple.GetValueOrDefault();
				close.StartCountdown(timeSpan.TotalSeconds, timeSpan / timeSpan2);
				return;
			}
		}
		close.StartCountdown(NotchPresenter.MicrophoneTroubleAutoHide.TotalSeconds);
	}
}
