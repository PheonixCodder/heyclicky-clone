using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Settings;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Shell;

public sealed class OneBrainStatsPanel
{
	private const double PanelWidth = 340.0;

	private const double PanelHeight = 380.0;

	private const double Inset = 12.0;

	private static readonly Color Green = DS.Hex(3458905u);

	private static readonly Color Orange = DS.Hex(16749824u);

	private static readonly Color Blue = DS.Hex(31487u);

	private readonly SettingsStore settings;

	private readonly DispatcherQueue dq;

	private OverlayWindow? window;

	private StackPanel? rows;

	private DispatcherQueueTimer? tick;

	private bool refreshQueued;

	private readonly bool keepOutOfCapture;

	public OneBrainStatsPanel(SettingsStore settings, bool keepOutOfCapture = true)
	{
		this.settings = settings;
		this.keepOutOfCapture = keepOutOfCapture;
		dq = DispatcherQueue.GetForCurrentThread();
		OneBrainStats.Shared.IsEnabledProvider = () => settings.Read((AppSettings s) => s.ShowOneBrainStats);
	}

	public void Start()
	{
		ApplyPreference();
		settings.Changed += delegate
		{
			dq.TryEnqueue(ApplyPreference);
		};
		OneBrainStats.Shared.Changed += delegate
		{
			if (!refreshQueued)
			{
				refreshQueued = true;
				dq.TryEnqueue(delegate
				{
					refreshQueued = false;
					Refresh();
				});
			}
		};
	}

	private void ApplyPreference()
	{
		bool isEnabled = OneBrainStats.Shared.IsEnabled;
		if (isEnabled && (object)window == null)
		{
			Show();
		}
		if (!isEnabled && (object)window != null)
		{
			tick?.Stop();
			tick = null;
			window.HideWindow();
			window.Close();
			window = null;
			rows = null;
		}
	}

	private void Show()
	{
		window = new OverlayWindow("Kursor one brain stats", keepOutOfCapture, clickThroughEverywhere: true);
		if (keepOutOfCapture)
		{
			window.KeepOutOfEveryCapture();
		}
		rows = new StackPanel
		{
			Spacing = 5.0
		};
		Border item = new Border
		{
			Width = 340.0,
			Padding = new Thickness(10.0),
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(DS.Black(0.78)),
			Child = rows,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		window.Root.Children.Add(item);
		Displays.Monitor monitor = Displays.Primary();
		double scale = monitor.Scale;
		window.PlacePx((int)((double)monitor.Work.Left + 12.0 * scale), (int)((double)monitor.Work.Top + 12.0 * scale), (int)(340.0 * scale), (int)(380.0 * scale));
		Refresh();
		window.ShowNoActivate();
		tick = dq.CreateTimer();
		tick.Interval = TimeSpan.FromSeconds(1L);
		tick.Tick += delegate
		{
			Refresh();
		};
		tick.Start();
	}

	private void Refresh()
	{
		if ((object)rows == null)
		{
			return;
		}
		var (list, fraction, pastThreshold, num) = OneBrainStats.Shared.Describe(DateTimeOffset.Now);
		rows.Children.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			if (i == num)
			{
				rows.Children.Add(ProgressBar(fraction, pastThreshold));
			}
			var (text, text2) = list[i];
			Color color;
			switch (text2)
			{
			case "divider":
				rows.Children.Add(new Border
				{
					Height = 1.0,
					Background = Ui.Brush(DS.White(0.15))
				});
				continue;
			case "dim":
				color = DS.White(0.7);
				break;
			case "faint":
			case "event":
				color = DS.White(0.6);
				break;
			case "hit":
				color = Green;
				break;
			case "miss":
				color = Orange;
				break;
			default:
				color = DS.White(0.9);
				break;
			}
			Color color2 = color;
			TextBlock item = Ui.Text(text, 10.5, (text2 == "bold") ? FontWeights.Bold : FontWeights.Normal, color2, TextAlignment.Left, 0, text2 != "event", mono: true);
			rows.Children.Add(item);
		}
	}

	private static FrameworkElement ProgressBar(double fraction, bool pastThreshold)
	{
		Grid obj = new Grid
		{
			Height = 4.0,
			CornerRadius = new CornerRadius(2.0),
			Background = Ui.Brush(DS.White(0.15)),
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(Math.Clamp(fraction, 0.0, 1.0), GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0 - Math.Clamp(fraction, 0.0, 1.0), GridUnitType.Star)
				}
			}
		};
		Border item = new Border
		{
			CornerRadius = new CornerRadius(2.0),
			Background = Ui.Brush(pastThreshold ? Orange : Blue)
		};
		obj.Children.Add(item);
		return obj;
	}
}
