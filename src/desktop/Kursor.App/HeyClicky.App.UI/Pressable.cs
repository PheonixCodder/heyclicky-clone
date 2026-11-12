using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.UI;

public class Pressable : Grid
{
	internal const bool UsesHandCursor = false;

	private bool enabled = true;

	private bool pressArmed;

	private readonly bool looksClickable;

	private Border? hoverFill;

	private GelSurface? hoverGel;

	private const double DefaultHoverMaxWidth = 640.0;

	private const double DefaultHoverMaxHeight = 360.0;

	private CornerRadius requestedCorners;

	private bool fittingCorners;

	private static InputCursor? sharedHand;

	private static InputCursor? sharedArrow;

	public bool IsHovered { get; private set; }

	public bool IsPressed { get; private set; }

	public bool ShowsDefaultHover { get; set; } = true;

	public bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			bool num = enabled != value;
			enabled = value;
			if (num)
			{
				ShowDefaultHover();
			}
		}
	}

	internal static InputCursor? ClickableCursor => null;

	internal static InputCursor HandCursor => sharedHand ?? (sharedHand = InputSystemCursor.Create(InputSystemCursorShape.Hand));

	internal static InputCursor ArrowCursor => sharedArrow ?? (sharedArrow = InputSystemCursor.Create(InputSystemCursorShape.Arrow));

	public bool? AccessibleToggleState { get; set; }

	public event Action? Click;

	public event Action<bool>? HoverChanged;

	public event Action<bool>? PressedChanged;

	private void SetPressedLook(bool pressed)
	{
		if (IsPressed != pressed)
		{
			IsPressed = pressed;
			this.PressedChanged?.Invoke(pressed);
			ShowDefaultHover();
		}
	}

	private void ShowDefaultHover()
	{
		bool flag = IsHovered && enabled && looksClickable && ShowsDefaultHover && this.HoverChanged == null && base.ActualWidth <= 640.0 && base.ActualHeight <= 360.0;
		if (!flag && (object)hoverFill == null && (object)hoverGel == null)
		{
			return;
		}
		if ((object)hoverGel != null && (object)hoverGel.Parent != this)
		{
			hoverGel = null;
		}
		if ((object)hoverGel == null)
		{
			hoverGel = (flag ? base.Children.OfType<GelSurface>().FirstOrDefault() : null);
		}
		if ((object)hoverGel != null)
		{
			hoverGel.Hovered = flag;
			return;
		}
		if ((object)hoverFill != null && (object)hoverFill.Parent != this)
		{
			hoverFill = null;
		}
		if ((object)hoverFill == null)
		{
			(bool OnDark, CornerRadius Corners) tuple = HoverSurface();
			bool item = tuple.OnDark;
			CornerRadius item2 = tuple.Corners;
			hoverFill = new Border
			{
				IsHitTestVisible = false,
				Opacity = 0.0,
				CornerRadius = item2,
				Background = Ui.Brush(item ? DS.White(0.06) : DS.Black(0.035))
			};
			base.Children.Add(hoverFill);
		}
		Anim.Opacity(hoverFill, (!flag) ? 0f : (IsPressed ? 0.67f : 1f), 0.083, Easing.EaseOut(Ui.Compositor));
	}

	private (bool OnDark, CornerRadius Corners) HoverSurface()
	{
		bool item = base.ActualTheme != ElementTheme.Light;
		CornerRadius cornerRadius = base.CornerRadius;
		Queue<(DependencyObject, int)> queue = new Queue<(DependencyObject, int)>();
		for (int i = 0; i < base.Children.Count; i++)
		{
			queue.Enqueue((base.Children[i], 1));
		}
		while (queue.Count > 0)
		{
			var (dependencyObject, num) = queue.Dequeue();
			if (dependencyObject is FrameworkElement { Visibility: Visibility.Visible } frameworkElement && frameworkElement.ActualWidth >= base.ActualWidth * 0.7 && frameworkElement.ActualHeight >= base.ActualHeight * 0.7)
			{
				(Brush, CornerRadius) tuple2 = ((frameworkElement is Border border) ? (border.Background, border.CornerRadius) : ((frameworkElement is Panel panel) ? (panel.Background, (panel is Grid grid) ? grid.CornerRadius : default(CornerRadius)) : ((!(frameworkElement is Control control)) ? (null, default(CornerRadius)) : (control.Background, control.CornerRadius))));
				(Brush, CornerRadius) tuple3 = tuple2;
				Brush item2 = tuple3.Item1;
				CornerRadius item3 = tuple3.Item2;
				Color? color = ((item2 is SolidColorBrush solidColorBrush) ? new Color?(solidColorBrush.Color) : ((!(item2 is GradientBrush { GradientStops: { Count: >0 } } gradientBrush)) ? ((Color?)null) : new Color?(gradientBrush.GradientStops[0].Color)));
				Color? color2 = color;
				if (color2.HasValue)
				{
					Color valueOrDefault = color2.GetValueOrDefault();
					if (valueOrDefault.A > 200)
					{
						item = 0.2126 * (double)(int)valueOrDefault.R + 0.7152 * (double)(int)valueOrDefault.G + 0.0722 * (double)(int)valueOrDefault.B < 140.0;
						if (cornerRadius == default(CornerRadius))
						{
							cornerRadius = item3;
						}
						break;
					}
				}
			}
			if (num < 3)
			{
				for (int j = 0; j < VisualTreeHelper.GetChildrenCount(dependencyObject); j++)
				{
					queue.Enqueue((VisualTreeHelper.GetChild(dependencyObject, j), num + 1));
				}
			}
		}
		if (cornerRadius == default(CornerRadius))
		{
			cornerRadius = new CornerRadius(Math.Min(6.0, Math.Min(base.ActualWidth, base.ActualHeight) / 2.0));
		}
		return (OnDark: item, Corners: cornerRadius);
	}

	public Pressable(bool handCursor = true)
	{
		base.Background = Ui.Transparent;
		looksClickable = handCursor;
		base.PointerEntered += delegate
		{
			IsHovered = true;
			this.HoverChanged?.Invoke(obj: true);
			ShowDefaultHover();
		};
		base.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				IsHovered = false;
				this.HoverChanged?.Invoke(obj: false);
				SetPressedLook(pressed: false);
				ShowDefaultHover();
			}
		};
		base.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
		{
			if (enabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			{
				CapturePointer(e.Pointer);
				pressArmed = true;
				SetPressedLook(pressed: true);
				e.Handled = true;
			}
		};
		base.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
		{
			if (pressArmed)
			{
				pressArmed = false;
				ReleasePointerCapture(e.Pointer);
				SetPressedLook(pressed: false);
				Point position = e.GetCurrentPoint(this).Position;
				if (enabled && position.X >= 0.0 && position.Y >= 0.0 && position.X <= base.ActualWidth && position.Y <= base.ActualHeight)
				{
					this.Click?.Invoke();
				}
				e.Handled = true;
			}
		};
		base.PointerCaptureLost += delegate
		{
			SetPressedLook(pressed: false);
		};
		RegisterPropertyChangedCallback(Grid.CornerRadiusProperty, delegate
		{
			if (!fittingCorners)
			{
				requestedCorners = base.CornerRadius;
				FitCorners();
			}
		});
		base.SizeChanged += delegate
		{
			FitCorners();
		};
	}

	private void FitCorners()
	{
		double num = Math.Min(base.ActualWidth, base.ActualHeight) / 2.0;
		if (!(num <= 0.0))
		{
			CornerRadius cornerRadius = requestedCorners;
			CornerRadius cornerRadius2 = new CornerRadius(Math.Min(cornerRadius.TopLeft, num), Math.Min(cornerRadius.TopRight, num), Math.Min(cornerRadius.BottomRight, num), Math.Min(cornerRadius.BottomLeft, num));
			if (!(cornerRadius2 == base.CornerRadius))
			{
				fittingCorners = true;
				base.CornerRadius = cornerRadius2;
				fittingCorners = false;
			}
		}
	}

	public void RaiseClick()
	{
		this.Click?.Invoke();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new PressableAutomationPeer(this);
	}

	public Pressable WithPressScale(float scale = 0.97f, double seconds = 0.14, bool easeOut = false)
	{
		PressedChanged += delegate(bool p)
		{
			base.CenterPoint = new Vector3((float)base.ActualWidth / 2f, (float)base.ActualHeight / 2f, 0f);
			Anim.Scale(this, p ? scale : 1f, seconds, easeOut ? Easing.EaseOut(Ui.Compositor) : Easing.Expressive(Ui.Compositor), null);
		};
		return this;
	}

	public Pressable WithIconHover(double diameter)
	{
		ShadowLayer shadow = new ShadowLayer(diameter / 2.0, DS.Black(0.18), 5.0, 1.0)
		{
			Width = diameter,
			Height = diameter,
			Opacity = 0.0
		};
		Ellipse plate = new Ellipse
		{
			Width = diameter,
			Height = diameter,
			Fill = Ui.Brush(DS.White(0.06)),
			Stroke = Ui.Brush(DS.White(0.14)),
			StrokeThickness = 0.8,
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		FrameworkElement[] array = new FrameworkElement[2] { shadow, plate };
		foreach (FrameworkElement obj in array)
		{
			obj.HorizontalAlignment = HorizontalAlignment.Center;
			obj.VerticalAlignment = VerticalAlignment.Center;
		}
		base.Children.Insert(0, plate);
		base.Children.Insert(0, shadow);
		HoverChanged += delegate(bool h)
		{
			Anim.Opacity(shadow, h ? 1 : 0, 0.15, Easing.EaseOut(Ui.Compositor));
			Anim.Opacity(plate, h ? 1 : 0, 0.15, Easing.EaseOut(Ui.Compositor));
			Anim.Scale(this, (h && !Anim.Instant) ? 1.025f : 1f, 0.15, Easing.EaseOut(Ui.Compositor), new Vector3((float)base.ActualWidth / 2f, (float)base.ActualHeight / 2f, 0f));
		};
		return this;
	}

	public Pressable WithHoverScale(float scale, double seconds = 0.15, bool bottomAnchor = false)
	{
		HoverChanged += delegate(bool h)
		{
			if (enabled)
			{
				base.CenterPoint = new Vector3((float)base.ActualWidth / 2f, bottomAnchor ? ((float)base.ActualHeight) : ((float)base.ActualHeight / 2f), 0f);
				Anim.Scale(this, h ? scale : 1f, seconds, Easing.EaseOut(Ui.Compositor), null);
			}
		};
		return this;
	}
}
