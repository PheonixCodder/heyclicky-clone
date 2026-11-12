using System;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch.Peek;

public interface INotchHomeDock
{
	(double W, double H)? FrontCardSize { get; }

	string? StatusLine { get; }

	event Action? FrontCardSizeChanged;

	FrameworkElement? CreateHomeSlot();

	void NotchShowsHome(bool showsHome);

	void NotchFolded(string reason);
}
