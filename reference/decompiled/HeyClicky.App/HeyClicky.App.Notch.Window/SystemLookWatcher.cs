using System;
using Windows.UI.ViewManagement;

namespace HeyClicky.App.Notch.Window;

internal sealed class SystemLookWatcher : IDisposable
{
	private readonly UISettings settings = new UISettings();

	private readonly Action changed;

	public SystemLookWatcher(Action changed)
	{
		this.changed = changed;
		settings.ColorValuesChanged += OnChanged;
	}

	private void OnChanged(UISettings sender, object args)
	{
		changed();
	}

	public void Dispose()
	{
		settings.ColorValuesChanged -= OnChanged;
	}
}
