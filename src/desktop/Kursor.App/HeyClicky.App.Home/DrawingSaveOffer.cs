using System;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Drawing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace HeyClicky.App.Home;

public sealed class DrawingSaveOffer
{
	private readonly OverlayWindow window;

	private readonly GelButton save;

	private readonly GelButton clear;

	private Action? onSave;

	private Action? onClear;

	private DisplayInfo? shownOn;

	private int placedY = int.MinValue;

	private DispatcherQueue? dq;

	public bool IsShown { get; private set; }

	public nint Hwnd => window.Hwnd;

	public DrawingSaveOffer(bool excludeFromCapture)
	{
		window = new OverlayWindow("HeyClicky save drawing", excludeFromCapture);
		save = new GelButton("Save drawing", GelAppearance.PrimaryBlue, GelButton.Sizing.Compact, 100.0, "\ue896", null, null);
		clear = new GelButton("Clear", GelAppearance.SecondaryWhite, GelButton.Sizing.Compact, 100.0, "\ue711", null, null);
		save.Click += delegate
		{
			onSave?.Invoke();
		};
		clear.Click += delegate
		{
			Hide();
			onClear?.Invoke();
		};
		StackPanel stackPanel = Ui.H(8.0, clear, save);
		stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		window.Root.Children.Add(stackPanel);
		dq = DispatcherQueue.GetForCurrentThread();
		NotchVisibleSurfaces.Changed += OnNotchChanged;
	}

	public void Show(DisplayInfo display, Action? save, Action clear)
	{
		onSave = save;
		onClear = clear;
		this.save.Visibility = ((save == null) ? Visibility.Collapsed : Visibility.Visible);
		shownOn = display;
		Place(display);
		window.ShowNoActivate();
		IsShown = true;
		Log.Info("lessons", (save == null) ? "clear offer shown (Clear only)" : "save offer shown");
	}

	private void Place(DisplayInfo display)
	{
		double scale = display.Scale;
		int num = (int)(300.0 * scale);
		int num2 = (int)(44.0 * scale);
		int num3 = (int)(display.BoundsPx.X + (display.BoundsPx.Width - (double)num) / 2.0);
		int num4 = (int)(display.BoundsPx.Y + 52.0 * scale);
		RectInt32 rectInt = new RectInt32(num3, num4, num, num2);
		RectInt32 b = new RectInt32(num3 - (int)(8.0 * scale), num4 - (int)(8.0 * scale), num + (int)(16.0 * scale), num2 + (int)(16.0 * scale));
		RectInt32? frame = NotchVisibleSurfaces.Frame;
		if (frame.HasValue)
		{
			RectInt32 valueOrDefault = frame.GetValueOrDefault();
			if (NotchVisibleSurfaces.Intersects(valueOrDefault, b))
			{
				num4 = Math.Min((int)(display.BoundsPx.Y + display.BoundsPx.Height - (double)num2 - 12.0 * scale), valueOrDefault.Y + valueOrDefault.Height + (int)(10.0 * scale));
			}
		}
		if (num4 != placedY && IsShown)
		{
			Log.Info("lessons", (num4 == rectInt.Y) ? "save offer back under the notch" : "save offer steps below the open notch surface");
		}
		placedY = num4;
		window.PlacePx(num3, num4, num, num2);
	}

	private void OnNotchChanged()
	{
		dq?.TryEnqueue(delegate
		{
			if (IsShown)
			{
				DisplayInfo displayInfo = shownOn;
				if ((object)displayInfo != null)
				{
					Place(displayInfo);
				}
			}
		});
	}

	public void Hide()
	{
		if (IsShown)
		{
			IsShown = false;
			window.HideWindow();
		}
	}
}
