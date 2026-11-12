using System;
using HeyClicky.App.Dock;
using HeyClicky.App.Notch.Peek;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;

namespace HeyClicky.App;

internal sealed class ArcNotchHomeDock : INotchHomeDock
{
	private readonly ArcDock dock;

	private readonly Func<NotchTab?> selectedTab;

	public (double W, double H)? FrontCardSize => dock.TopArcFrontCardSize;

	public string? StatusLine
	{
		get
		{
			if (!dock.IsRunning)
			{
				return null;
			}
			return dock.StatusLine();
		}
	}

	public event Action? FrontCardSizeChanged;

	public ArcNotchHomeDock(ArcDock dock, Func<NotchTab?> selectedTab)
	{
		this.dock = dock;
		this.selectedTab = selectedTab;
		dock.FrontCardSizeChanged += delegate
		{
			this.FrontCardSizeChanged?.Invoke();
		};
		string lastLine = null;
		dock.ContentChanged += delegate
		{
			string statusLine = StatusLine;
			if (!(statusLine == lastLine))
			{
				lastLine = statusLine;
				this.FrontCardSizeChanged?.Invoke();
			}
		};
	}

	public FrameworkElement? CreateHomeSlot()
	{
		return null;
	}

	public void NotchShowsHome(bool showsHome)
	{
		NotchTab? notchTab = selectedTab();
		if (!showsHome && notchTab == NotchTab.Home)
		{
			dock.CloseTopArc();
		}
		else
		{
			dock.NotchTabChanged(showsHome, showsHome ? null : notchTab?.RawValue());
		}
	}

	public void NotchFolded(string reason)
	{
		dock.CloseTopArc(reason);
	}
}
