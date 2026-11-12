using System;
using HeyClicky.App.Dock;
using HeyClicky.App.Shell;
using HeyClicky.Core.Notch;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

internal sealed class ArcVoicePill
{
	private readonly NotchPresenter presenter;

	private readonly ArcDock dock;

	private readonly INotchHost host;

	private readonly bool excludeFromCapture;

	private OverlayWindow? window;

	private readonly Grid layer = new Grid
	{
		IsHitTestVisible = false,
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Center
	};

	private DispatcherQueueTimer? hideTimer;

	public nint Hwnd
	{
		get
		{
			OverlayWindow? overlayWindow = window;
			if ((object)overlayWindow == null || !overlayWindow.IsShown)
			{
				return 0;
			}
			return window.Hwnd;
		}
	}

	private (NotchActivityPhase Phase, string? Label)? Phase
	{
		get
		{
			if (!presenter.IsShowingNotchAgentPeek || !presenter.IsNotchHeldOpenByArc)
			{
				return null;
			}
			return presenter.VoicePill;
		}
	}

	public ArcVoicePill(NotchPresenter presenter, ArcDock dock, INotchHost host, bool excludeFromCapture)
	{
		this.presenter = presenter;
		this.dock = dock;
		this.host = host;
		this.excludeFromCapture = excludeFromCapture;
	}

	public void Sync()
	{
		(NotchActivityPhase, string)? phase = Phase;
		if (!phase.HasValue)
		{
			if ((object)window != null && window.IsShown)
			{
				NotchVoicePill.Update(layer, null, host);
				if ((object)hideTimer == null)
				{
					hideTimer = CreateHideTimer();
				}
				hideTimer.Stop();
				hideTimer.Start();
			}
		}
		else
		{
			hideTimer?.Stop();
			EnsureWindow();
			NotchVoicePill.Update(layer, phase, host);
			if (!window.IsShown)
			{
				window.ShowNoActivate();
			}
			window.Root.IsHitTestVisible = false;
			ArcDockView.KeepAboveDeck(window.Hwnd);
		}
	}

	private DispatcherQueueTimer CreateHideTimer()
	{
		DispatcherQueueTimer dispatcherQueueTimer = layer.DispatcherQueue.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.35);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (!Phase.HasValue)
			{
				window?.HideWindow();
			}
		};
		return dispatcherQueueTimer;
	}

	private void EnsureWindow()
	{
		if ((object)window == null)
		{
			window = new OverlayWindow("HeyClicky Arc Voice", excludeFromCapture, clickThroughEverywhere: true);
			window.Root.Children.Add(layer);
			window.Root.IsHitTestVisible = false;
		}
		Displays.Monitor monitor = dock.HostMonitor();
		ArcRect? arcRect = dock.Metrics?.HardwareNotch;
		double top = ((!arcRect.HasValue) ? 10.0 : (arcRect.GetValueOrDefault().MaxY + 2.0));
		window.Root.Width = (double)monitor.Bounds.Width / monitor.Scale;
		window.Root.Height = 80.0;
		layer.Margin = new Thickness(0.0, top, 0.0, 0.0);
		window.PlacePx(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, (int)Math.Ceiling(80.0 * monitor.Scale));
	}
}
