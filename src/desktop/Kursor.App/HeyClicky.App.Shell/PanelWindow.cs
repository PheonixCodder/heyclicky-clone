using System;
using HeyClicky.App.Design;
using HeyClicky.App.Onboarding;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Shell;

public class PanelWindow : OverlayWindow
{
	public const double ShadowMargin = 32.0;

	private double panelW;

	private double panelH;

	private Displays.Monitor monitor;

	private readonly ShadowLayer shadow;

	private (double W, double H)? envelope;

	private double shownW;

	private double shownH;

	private SpringAxis? springW;

	private SpringAxis? springH;

	private TimeSpan? lastFrame;

	private double easeStarted = -1.0;

	private double easeFromW;

	private double easeFromH;

	private bool animating;

	private int frames;

	private long firstFrameMs;

	private double longestFrameGap;

	private long animationStarted;

	private (double W, double H) animatedFrom;

	public Grid Panel { get; }

	public Border Chrome { get; }

	public double PanelWidth => panelW;

	public double PanelHeight => panelH;

	public double ShownWidth => shownW;

	public double ShownHeight => shownH;

	private (double W, double H) WindowDip
	{
		get
		{
			(double, double)? tuple = envelope;
			if (tuple.HasValue)
			{
				(double, double) valueOrDefault = tuple.GetValueOrDefault();
				return (W: valueOrDefault.Item1 + 64.0, H: valueOrDefault.Item2 + 64.0);
			}
			return (W: panelW + 64.0, H: panelH + 64.0);
		}
	}

	public event Action<double, double>? ShownSizeChanged;

	public PanelWindow(string title, bool excludeFromCapture, bool showInTaskbar = false, double cornerRadius = 14.0, Color? fill = null, double borderOpacity = 0.1)
		: base(title, excludeFromCapture, clickThroughEverywhere: false, activatable: true, topmost: false, showInTaskbar)
	{
		monitor = Displays.UnderCursor();
		shadow = new ShadowLayer(cornerRadius, DS.Black(0.45), 16.0, 10.0)
		{
			Margin = new Thickness(32.0)
		};
		Chrome = new Border
		{
			Margin = new Thickness(32.0),
			CornerRadius = new CornerRadius(cornerRadius),
			Background = Ui.Brush(fill ?? DS.Colors.Background)
		};
		Panel = new Grid();
		Ui.ClipRounded(Panel, cornerRadius);
		Border item = new Border
		{
			CornerRadius = new CornerRadius(cornerRadius),
			BorderBrush = Ui.Brush(DS.White(borderOpacity)),
			BorderThickness = new Thickness(0.5),
			IsHitTestVisible = false
		};
		Chrome.Child = new Grid
		{
			Children = 
			{
				(UIElement)Panel,
				(UIElement)item
			}
		};
		base.Root.Children.Add(shadow);
		base.Root.Children.Add(Chrome);
		base.Root.RequestedTheme = ElementTheme.Dark;
		MoveByBackground(Chrome);
	}

	public void UseFixedEnvelope(double maxWidth, double maxHeight)
	{
		envelope = (maxWidth, maxHeight);
		FrameworkElement[] array = new FrameworkElement[2] { Chrome, shadow };
		foreach (FrameworkElement obj in array)
		{
			obj.HorizontalAlignment = HorizontalAlignment.Center;
			obj.VerticalAlignment = VerticalAlignment.Center;
		}
		if (panelW > 0.0)
		{
			ApplyShownSize(panelW, panelH);
		}
	}

	public void SetPanelSize(double width, double height)
	{
		if (!(Math.Abs(width - panelW) < 0.5) || !(Math.Abs(height - panelH) < 0.5) || animating)
		{
			StopAnimating();
			panelW = width;
			panelH = height;
			ApplyShownSize(width, height);
			if (base.IsShown && !OverlayWindow.Offscreen)
			{
				monitor = Displays.ForWindow(base.Hwnd);
			}
			var (wDip, hDip) = WindowDip;
			var (x, y, w, h) = Displays.CenteredInWork(monitor, wDip, hDip);
			PlacePx(x, y, w, h);
			UpdateRegion(null, null);
		}
	}

	public void AnimatePanelSize(double width, double height, SwiftSpring spring)
	{
		(double, double)? tuple = envelope;
		if (!tuple.HasValue || !base.IsShown || Anim.Instant || shownW <= 0.0)
		{
			SetPanelSize(width, height);
		}
		else
		{
			if (Math.Abs(width - panelW) < 0.5 && Math.Abs(height - panelH) < 0.5)
			{
				return;
			}
			panelW = width;
			panelH = height;
			if (Anim.ReduceMotion)
			{
				springW = (springH = null);
				easeStarted = -1.0;
				easeFromW = shownW;
				easeFromH = shownH;
			}
			else
			{
				if (springW == null)
				{
					springW = new SpringAxis(spring.Response, spring.DampingFraction, shownW);
				}
				if (springH == null)
				{
					springH = new SpringAxis(spring.Response, spring.DampingFraction, shownH);
				}
				springW.Target = width;
				springH.Target = height;
			}
			if (!OverlayWindow.Offscreen)
			{
				monitor = Displays.ForWindow(base.Hwnd);
			}
			var (wDip, hDip) = WindowDip;
			var (x, y, w, h) = Displays.CenteredInWork(monitor, wDip, hDip);
			PlacePx(x, y, w, h);
			UpdateRegion(Math.Max(shownW, width), Math.Max(shownH, height));
			if (!animating)
			{
				animating = true;
				lastFrame = null;
				frames = 0;
				longestFrameGap = 0.0;
				animationStarted = Environment.TickCount64;
				animatedFrom = (W: shownW, H: shownH);
				CompositionTarget.Rendering += OnFrame;
			}
		}
	}

	private void OnFrame(object? sender, object e)
	{
		TimeSpan timeSpan = ((e is RenderingEventArgs renderingEventArgs) ? renderingEventArgs.RenderingTime : TimeSpan.FromMilliseconds(Environment.TickCount64, 0L));
		TimeSpan? timeSpan2 = lastFrame;
		double num;
		if (timeSpan2.HasValue)
		{
			TimeSpan valueOrDefault = timeSpan2.GetValueOrDefault();
			num = (timeSpan - valueOrDefault).TotalSeconds;
		}
		else
		{
			num = 0.0;
		}
		double num2 = num;
		double seconds = Math.Clamp(num2, 0.0, 0.05);
		lastFrame = timeSpan;
		if (frames++ == 0)
		{
			firstFrameMs = Environment.TickCount64 - animationStarted;
		}
		longestFrameGap = Math.Max(longestFrameGap, num2);
		SpringAxis springAxis = springW;
		bool flag;
		double width;
		double height;
		if (springAxis != null)
		{
			SpringAxis springAxis2 = springH;
			if (springAxis2 != null)
			{
				springAxis.Step(seconds);
				springAxis2.Step(seconds);
				flag = springAxis.IsSettled && springAxis2.IsSettled;
				if (flag)
				{
					springAxis.Settle();
					springAxis2.Settle();
				}
				width = springAxis.Value;
				height = springAxis2.Value;
				goto IL_01ac;
			}
		}
		if (easeStarted < 0.0)
		{
			easeStarted = timeSpan.TotalSeconds;
		}
		double num3 = Math.Min(1.0, (timeSpan.TotalSeconds - easeStarted) / 0.12);
		double num4 = TourMotion.EaseOut(num3);
		width = easeFromW + (panelW - easeFromW) * num4;
		height = easeFromH + (panelH - easeFromH) * num4;
		flag = num3 >= 1.0;
		goto IL_01ac;
		IL_01ac:
		ApplyShownSize(width, height);
		if (flag)
		{
			StopAnimating();
			ApplyShownSize(panelW, panelH);
			UpdateRegion(null, null);
			double num5 = (double)(Environment.TickCount64 - animationStarted) / 1000.0;
			Log.Info("panel", $"{base.Title}: {animatedFrom.W:0}x{animatedFrom.H:0} → {panelW:0}x{panelH:0} in {num5:0.00} s, {frames} frames ({((num5 > 0.0) ? ((double)frames / num5) : 0.0):0} fps, first frame {firstFrameMs} ms after the change, longest frame {longestFrameGap * 1000.0:0} ms)");
		}
	}

	private void StopAnimating()
	{
		if (animating)
		{
			CompositionTarget.Rendering -= OnFrame;
		}
		animating = false;
		springW = (springH = null);
		easeStarted = -1.0;
	}

	private void ApplyShownSize(double width, double height)
	{
		shownW = width;
		shownH = height;
		Chrome.Width = width;
		Chrome.Height = height;
		(double, double)? tuple = envelope;
		if (tuple.HasValue)
		{
			shadow.Width = width;
			shadow.Height = height;
		}
		this.ShownSizeChanged?.Invoke(width, height);
	}

	private void UpdateRegion(double? width = null, double? height = null)
	{
		(double, double)? tuple = envelope;
		if (tuple.HasValue && !OverlayWindow.Offscreen)
		{
			(int X, int Y, int W, int H) rectPx = base.RectPx;
			int item = rectPx.W;
			int item2 = rectPx.H;
			var (x, y, w, h) = TourMotion.CenteredRegionPx(item, item2, base.Scale, width ?? panelW, height ?? panelH, 32.0);
			SetRegionPx(x, y, w, h);
		}
	}

	public void BringToFrontRecentered()
	{
		monitor = Displays.ForWindow(this);
		var (wDip, hDip) = WindowDip;
		var (x, y, w, h) = Displays.CenteredInWork(monitor, wDip, hDip);
		PlacePx(x, y, w, h);
		ShowActivated();
		UpdateRegion(null, null);
	}

	public void ShowCentered()
	{
		monitor = Displays.UnderCursor();
		var (wDip, hDip) = WindowDip;
		var (x, y, w, h) = Displays.CenteredInWork(monitor, wDip, hDip);
		PlacePx(x, y, w, h);
		ShowActivated();
		UpdateRegion(null, null);
	}
}
