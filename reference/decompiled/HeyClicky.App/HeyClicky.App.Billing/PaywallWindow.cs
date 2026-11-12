using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Billing;

public sealed class PaywallWindow : OverlayWindow
{
	private const double ShadowMargin = 32.0;

	private readonly Grid stage;

	private readonly Border chrome;

	private readonly ShadowLayer shadow;

	private readonly ScrollViewer scroller;

	private UIElement? card;

	private double requestedCardWidth;

	private double cardWidth;

	private Displays.Monitor monitor;

	private int generation;

	public bool IsShowing { get; private set; }

	public UIElement? Card => card;

	public PaywallWindow(bool excludeFromCapture)
		: base("HeyClicky", excludeFromCapture)
	{
		stage = new Grid
		{
			Margin = new Thickness(32.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		shadow = new ShadowLayer(18.0, DS.Black(0.45), 16.0, 10.0);
		chrome = new Border
		{
			CornerRadius = new CornerRadius(18.0),
			BorderThickness = new Thickness(0.5),
			Padding = new Thickness(28.0, 26.0, 28.0, 26.0)
		};
		scroller = new ScrollViewer
		{
			HorizontalScrollMode = ScrollMode.Disabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollMode = ScrollMode.Enabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			IsTabStop = false
		};
		stage.Children.Add(shadow);
		stage.Children.Add(chrome);
		base.Root.Children.Add(stage);
		MoveByBackground(chrome);
	}

	public void Present(PaywallCard card, double width, PaywallPalette palette)
	{
		generation++;
		requestedCardWidth = width - 16.0;
		chrome.Background = Ui.Brush(palette.CardSurface);
		chrome.BorderBrush = Ui.Brush(palette.CardBorder);
		base.Root.RequestedTheme = ((!palette.IsDark) ? ElementTheme.Light : ElementTheme.Dark);
		this.card = card;
		scroller.Content = null;
		chrome.Child = card;
		monitor = Displays.UnderCursor();
		Fit();
		bool isShowing = IsShowing;
		IsShowing = true;
		Anim.Stop(base.Root, "Opacity");
		base.Root.Opacity = 1.0;
		ShowNoActivate();
		Vector3 vector = new Vector3((float)(cardWidth / 2.0), (float)(MeasuredHeight() / 2.0), 0f);
		SwiftSpring spring = new SwiftSpring(0.35, 0.82);
		int num;
		if (isShowing)
		{
			num = ((stage.Opacity < 1.0) ? 1 : 0);
			if (num == 0)
			{
				goto IL_013a;
			}
		}
		else
		{
			num = 1;
		}
		stage.CenterPoint = vector;
		goto IL_013a;
		IL_013a:
		if (num != 0 && !Anim.Instant)
		{
			stage.Opacity = 0.0;
			Anim.ScaleUpFrom(stage, 0.94f, spring, vector);
		}
		else
		{
			Anim.ScaleSpring(stage, 1f, spring, vector);
		}
		Anim.OpacitySpring(stage, 1f, spring);
	}

	private double MeasuredHeight()
	{
		chrome.Height = double.NaN;
		chrome.Measure(new Size(cardWidth, double.PositiveInfinity));
		return Math.Ceiling(chrome.DesiredSize.Height);
	}

	public void Fit()
	{
		double workWidthDip = (double)monitor.Work.Width / monitor.Scale;
		double workHeightDip = (double)monitor.Work.Height / monitor.Scale;
		cardWidth = PaywallFit.CardWidth(requestedCardWidth, workWidthDip, 32.0);
		chrome.Width = cardWidth;
		shadow.Width = cardWidth;
		double num = MeasuredHeight();
		double num2 = PaywallFit.MaxCardHeight(workHeightDip, 32.0);
		SetScrolling(num > num2);
		double num3 = Math.Min(num, num2);
		shadow.Height = num3;
		chrome.Height = num3;
		double num4 = cardWidth + 64.0;
		double num5 = num3 + 64.0;
		int num6 = (int)Math.Round(num4 * monitor.Scale);
		int num7 = (int)Math.Round(num5 * monitor.Scale);
		var (x, y) = PaywallFit.Origin(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height, monitor.Work.Left, monitor.Work.Top, monitor.Work.Right, monitor.Work.Bottom, num6, num7);
		PlacePx(x, y, num6, num7);
	}

	private void SetScrolling(bool scrolls)
	{
		if ((object)card != null && !(scrolls ? (chrome.Child == scroller) : (chrome.Child == card)))
		{
			chrome.Child = null;
			scroller.Content = null;
			if (scrolls)
			{
				scroller.Content = card;
				chrome.Child = scroller;
			}
			else
			{
				chrome.Child = card;
			}
		}
	}

	public void Dismiss()
	{
		if (!IsShowing)
		{
			return;
		}
		IsShowing = false;
		int dismissGeneration = ++generation;
		CubicBezierEasingFunction easing = Easing.EaseOut(Ui.Compositor);
		Anim.Opacity(stage, 0f, 0.4, easing);
		Anim.Scale(stage, 0.96f, 0.4, easing, new Vector3((float)(cardWidth / 2.0), (float)(chrome.ActualHeight / 2.0), 0f));
		Anim.Opacity(base.Root, 0f, 0.45, easing);
		DispatcherQueueTimer dispatcherQueueTimer = base.DispatcherQueue.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(Anim.Instant ? 0.0 : 0.48);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (generation == dismissGeneration && !IsShowing)
			{
				chrome.Child = null;
				scroller.Content = null;
				card = null;
				HideWindow();
				Anim.Stop(base.Root, "Opacity");
				base.Root.Opacity = 1.0;
			}
		};
		dispatcherQueueTimer.Start();
	}
}
