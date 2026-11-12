using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;
using Windows.UI;

namespace HeyClicky.App.Settings;

public sealed class GelSlider : Grid
{
	private const double KnobDiameter = 30.0;

	private const double TroughHeight = 26.0;

	private const double FillInset = 3.0;

	private readonly double min;

	private readonly double max;

	private readonly Grid fillHost;

	private readonly Canvas dots = new Canvas
	{
		IsHitTestVisible = false,
		Height = 26.0
	};

	private readonly Grid knob;

	private readonly GelAppearance gel;

	private double value;

	private bool dragging;

	private int moves;

	public double Value => value;

	private double Progress
	{
		get
		{
			if (!(max > min))
			{
				return 0.0;
			}
			return (value - min) / (max - min);
		}
	}

	public event Action<double>? ValueChanged;

	public GelSlider(double value, double min, double max, GelAppearance gel, Color glow, string accessibleName)
	{
		GelSlider gelSlider = this;
		this.min = min;
		this.max = max;
		this.gel = gel;
		this.value = Math.Clamp(value, min, max);
		base.Height = 30.0;
		base.Background = Ui.Transparent;
		base.IsTabStop = true;
		base.UseSystemFocusVisuals = true;
		AutomationProperties.SetName(this, accessibleName);
		base.ProtectedCursor = Pressable.ClickableCursor;
		base.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX;
		Border item = new Border
		{
			Height = 26.0,
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(DS.Black(0.42)),
			BorderBrush = Ui.Brush(SK.Ink.WithAlpha(0.05)),
			BorderThickness = new Thickness(1.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		base.Children.Add(item);
		fillHost = new Grid
		{
			Height = 20.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(3.0, 0.0, 0.0, 0.0),
			IsHitTestVisible = false
		};
		fillHost.Children.Add(new ShadowLayer(6.0, glow.WithAlpha(0.4), 6.0, 0.0));
		fillHost.Children.Add(new GelSurface(gel, 6.0));
		base.Children.Add(fillHost);
		dots.VerticalAlignment = VerticalAlignment.Center;
		base.Children.Add(dots);
		knob = new Grid
		{
			Width = 30.0,
			Height = 30.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false
		};
		knob.Children.Add(new ShadowLayer(15.0, DS.Black(0.35), 4.0, 2.0));
		knob.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(Colors.White)
		});
		TextBlock item2 = new TextBlock
		{
			Text = "‹›",
			FontSize = 13.0,
			FontWeight = FontWeights.Black,
			Foreground = Ui.Brush(DS.Black(0.8)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, -3.0, 0.0, 0.0),
			IsTextScaleFactorEnabled = false
		};
		knob.Children.Add(item2);
		base.Children.Add(knob);
		base.SizeChanged += delegate
		{
			gelSlider.Layout();
		};
		base.PointerEntered += delegate
		{
			Anim.Scale(gelSlider.knob, 1.06f, 0.15, null, new Vector3(15f, 15f, 0f));
		};
		base.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e) && !gelSlider.dragging)
			{
				Anim.Scale(gelSlider.knob, 1f, 0.15, null, new Vector3(15f, 15f, 0f));
			}
		};
		base.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(gelSlider).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
			{
				gelSlider.dragging = gelSlider.CapturePointer(e.Pointer);
				gelSlider.moves = 0;
				gelSlider.SetFromX(e.GetCurrentPoint(gelSlider).Position.X);
				e.Handled = true;
			}
		};
		base.PointerMoved += delegate(object _, PointerRoutedEventArgs e)
		{
			if (gelSlider.dragging)
			{
				gelSlider.moves++;
				gelSlider.SetFromX(e.GetCurrentPoint(gelSlider).Position.X);
				e.Handled = true;
			}
		};
		base.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
		{
			if (gelSlider.dragging)
			{
				gelSlider.dragging = false;
				gelSlider.ReleasePointerCapture(e.Pointer);
				Log.Info("settings", $"gel slider \"{accessibleName}\": {gelSlider.value} after a drag of {gelSlider.moves} moves");
				e.Handled = true;
			}
		};
		base.PointerCaptureLost += delegate
		{
			if (gelSlider.dragging)
			{
				Log.Info("settings", $"gel slider \"{accessibleName}\": capture lost at {gelSlider.value}");
			}
			gelSlider.dragging = false;
		};
		base.KeyDown += delegate(object _, KeyRoutedEventArgs e)
		{
			VirtualKey key = e.Key;
			if ((uint)(key - 38) <= 1u)
			{
				gelSlider.Set(gelSlider.value + 1.0);
				e.Handled = true;
			}
			else
			{
				key = e.Key;
				if ((key == VirtualKey.Left || key == VirtualKey.Down) ? true : false)
				{
					gelSlider.Set(gelSlider.value - 1.0);
					e.Handled = true;
				}
			}
		};
	}

	private void SetFromX(double x)
	{
		double num = Math.Max(1.0, base.ActualWidth - 30.0);
		double num2 = Math.Clamp((x - 15.0) / num, 0.0, 1.0);
		Set(Math.Round(min + num2 * (max - min)));
	}

	public void Set(double v)
	{
		v = Math.Clamp(Math.Round(v), min, max);
		if (v != value)
		{
			value = v;
			Layout();
			this.ValueChanged?.Invoke(v);
		}
	}

	private void Layout()
	{
		double actualWidth = base.ActualWidth;
		if (!(actualWidth <= 0.0))
		{
			double num = Math.Max(1.0, actualWidth - 30.0);
			double num2 = num * Progress;
			double num3 = num2 + 15.0;
			knob.Margin = new Thickness(num2, 0.0, 0.0, 0.0);
			fillHost.Width = Math.Max(0.0, num3 - 3.0);
			fillHost.Opacity = ((Progress > 0.0) ? 1 : 0);
			int num4 = (int)Math.Round(max - min) + 1;
			int num5 = ((num4 <= 24) ? Math.Max(2, num4) : 16);
			dots.Children.Clear();
			dots.Width = actualWidth;
			for (int i = 0; i < num5; i++)
			{
				double num6 = 15.0 + num * (double)i / (double)(num5 - 1);
				Ellipse ellipse = new Ellipse
				{
					Width = 3.2,
					Height = 3.2,
					Fill = Ui.Brush((num6 < num3) ? DS.Black(0.22) : DS.White(0.2))
				};
				Canvas.SetLeft(ellipse, num6 - 1.6);
				Canvas.SetTop(ellipse, 11.4);
				dots.Children.Add(ellipse);
			}
		}
	}
}
