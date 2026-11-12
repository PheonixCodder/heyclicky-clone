using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.App.Interop;
using HeyClicky.App.UI;
using HeyClicky.Platform.Win32;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Windows.Foundation;

namespace HeyClicky.App.Shell;

public class OverlayWindow : Window
{
	private bool noActivate = true;

	private readonly bool isTopmost;

	private readonly bool excludeFromCapture;

	private readonly bool hiddenFromOwnCapture;

	private readonly WindowSubclass subclass;

	private static readonly bool InputTrace = Environment.GetEnvironmentVariable("HEYCLICKY_INPUT_TRACE") == "1";

	private readonly List<WindowSubclass> traceSubclasses = new List<WindowSubclass>();

	private static readonly List<WeakReference<OverlayWindow>> Live = new List<WeakReference<OverlayWindow>>();

	private static bool showInScreenRecordings = true;

	private bool neverInCapture;

	private bool temporarilyHiddenFromCapture;

	private static int ownCapturesInProgress;

	public static readonly bool Offscreen = Environment.GetEnvironmentVariable("HEYCLICKY_OFFSCREEN") == "1";

	internal const int OffscreenShift = -24000;

	public static readonly bool OnscreenBesideReal = Environment.GetEnvironmentVariable("HEYCLICKY_ONSCREEN_BESIDE_REAL") == "1";

	private const int ParkedX = -32000;

	private const int ParkedY = -32000;

	private bool shown;

	private (int X, int Y, int W, int H) placement;

	private bool ignoresMouseEvents;

	private bool staysLayered;

	public nint Hwnd { get; }

	public AppWindow AppWin { get; }

	public Grid Root { get; }

	public double Scale { get; private set; } = 1.0;

	private nint ZOrder
	{
		get
		{
			if (!isTopmost)
			{
				return 0;
			}
			return FullscreenGuard.TopmostInsertAfter();
		}
	}

	public static bool ShowInScreenRecordings
	{
		get
		{
			return showInScreenRecordings;
		}
		set
		{
			showInScreenRecordings = value;
			List<OverlayWindow> list;
			lock (Live)
			{
				list = Live.Select((WeakReference<OverlayWindow> w) => (!w.TryGetTarget(out var target)) ? null : target).OfType<OverlayWindow>().ToList();
			}
			foreach (OverlayWindow item in list)
			{
				item.ApplyCapturePolicy();
			}
			Log.Info("capture", $"show in screen recordings: {value} ({list.Count} windows)");
		}
	}

	private bool ExcludesNow
	{
		get
		{
			if (!neverInCapture && !temporarilyHiddenFromCapture)
			{
				if (!hiddenFromOwnCapture)
				{
					if (excludeFromCapture)
					{
						return !showInScreenRecordings;
					}
					return false;
				}
				if (CaptureExclusion.Enabled)
				{
					if (showInScreenRecordings)
					{
						return ownCapturesInProgress > 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public (int X, int Y, int W, int H) RectPx
	{
		get
		{
			if (!shown)
			{
				return placement;
			}
			Native.GetWindowRectPublic(Hwnd, out var rc);
			return (X: rc.Left, Y: rc.Top, W: rc.Width, H: rc.Height);
		}
	}

	public bool IsShown => shown;

	public Native.RECT WindowRect
	{
		get
		{
			(int X, int Y, int W, int H) rectPx = RectPx;
			int item = rectPx.X;
			int item2 = rectPx.Y;
			int item3 = rectPx.W;
			int item4 = rectPx.H;
			Native.RECT result = default(Native.RECT);
			result.Left = item;
			result.Top = item2;
			result.Right = item + item3;
			result.Bottom = item2 + item4;
			return result;
		}
	}

	public bool CanActivate
	{
		get
		{
			return !noActivate;
		}
		set
		{
			if (noActivate != !value)
			{
				noActivate = !value;
				if (value)
				{
					Native.AddExStyle(Hwnd, 0u, 134217728u);
				}
				else
				{
					Native.AddExStyle(Hwnd, 134217728u);
				}
			}
		}
	}

	public event Action<uint, nint, nint>? RawMessage;

	public OverlayWindow(string title, bool excludeFromCapture, bool clickThroughEverywhere = false, bool activatable = false, bool topmost = true, bool showInTaskbar = false, bool hiddenFromOwnCapture = false)
	{
		this.excludeFromCapture = excludeFromCapture;
		this.hiddenFromOwnCapture = hiddenFromOwnCapture;
		noActivate = !activatable;
		isTopmost = topmost;
		base.Title = title;
		Root = new AccessibleGrid
		{
			Background = null,
			ControlType = AutomationControlType.Pane
		};
		AutomationProperties.SetName(Root, title);
		base.Content = Root;
		base.SystemBackdrop = new TransparentBackdrop();
		Hwnd = WindowNative.GetWindowHandle(this);
		AppWin = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Hwnd));
		try
		{
			AppWin.IsShownInSwitchers = showInTaskbar;
		}
		catch (Exception ex)
		{
			Log.Warn("shell", "IsShownInSwitchers: " + ex.Message);
		}
		if (showInTaskbar)
		{
			try
			{
				AppWin.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
			}
			catch (Exception ex2)
			{
				Log.Warn("shell", "window icon: " + ex2.Message);
			}
		}
		if (AppWin.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
			overlappedPresenter.IsResizable = false;
			overlappedPresenter.IsMaximizable = false;
			overlappedPresenter.IsMinimizable = false;
			overlappedPresenter.IsAlwaysOnTop = topmost;
		}
		Native.SetStyle(Hwnd, 2147483648u, 13565952u);
		Native.AddExStyle(Hwnd, (uint)((showInTaskbar ? 262144 : 128) | ((!activatable) ? 134217728 : 0) | (topmost ? 8 : 0) | (clickThroughEverywhere ? 524320 : 0)), showInTaskbar ? 128u : 262144u);
		if (clickThroughEverywhere)
		{
			Native.SetLayeredWindowAttributes(Hwnd, 0u, byte.MaxValue, 2u);
		}
		int value = 1;
		Native.DwmSetWindowAttribute(Hwnd, 33, ref value, 4);
		int value2 = -2;
		Native.DwmSetWindowAttribute(Hwnd, 34, ref value2, 4);
		int value3 = 1;
		Native.DwmSetWindowAttribute(Hwnd, 3, ref value3, 4);
		Native.MARGINS m = new Native.MARGINS
		{
			L = -1,
			R = -1,
			T = -1,
			B = -1
		};
		Native.DwmExtendFrameIntoClientArea(Hwnd, ref m);
		subclass = new WindowSubclass(Hwnd, WndProc);
		Native.SetWindowPos(Hwnd, ZOrder, 0, 0, 0, 0, (uint)(0x33 | ((!isTopmost) ? 4 : 0)));
		ApplyCapturePolicy();
		Scale = (double)Native.GetDpiForWindow(Hwnd) / 96.0;
		Native.GetWindowRectPublic(Hwnd, out var rc);
		placement = (X: rc.Left, Y: rc.Top, W: rc.Width, H: rc.Height);
		Park();
		Root.IsHitTestVisible = false;
		PointerEventHandler handler = delegate(object _, PointerRoutedEventArgs e)
		{
			PointerFrames.Note(e);
		};
		Root.AddHandler(UIElement.PointerMovedEvent, handler, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerPressedEvent, handler, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerReleasedEvent, handler, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerEnteredEvent, handler, handledEventsToo: true);
		lock (Live)
		{
			Live.Add(new WeakReference<OverlayWindow>(this));
		}
		if (InputTrace)
		{
			StartInputTrace(title);
		}
		base.Closed += delegate
		{
			subclass.Dispose();
			lock (Live)
			{
				Live.RemoveAll((WeakReference<OverlayWindow> w) => !w.TryGetTarget(out OverlayWindow target) || target == this);
			}
		};
	}

	private void StartInputTrace(string title)
	{
		Hook(Hwnd, "top");
		HashSet<nint> hooked = new HashSet<nint> { Hwnd };
		HookChildren();
		DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(4L);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			HookChildren();
		};
		dispatcherQueueTimer.Start();
		Root.AddHandler(UIElement.PointerPressedEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			Log.Info("inputtrace", $"{title} xaml PointerPressed on {(e.OriginalSource as FrameworkElement)?.GetType().Name} handled={e.Handled}");
		}, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerExitedEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			Log.Info("inputtrace", title + " xaml PointerExited " + Where(e));
		}, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerEnteredEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			Log.Info("inputtrace", title + " xaml PointerEntered " + Where(e));
		}, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerReleasedEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			Log.Info("inputtrace", $"{title} xaml PointerReleased {Where(e)} handled={e.Handled}");
		}, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerCaptureLostEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			Log.Info("inputtrace", title + " xaml PointerCaptureLost " + (e.OriginalSource as FrameworkElement)?.GetType().Name);
		}, handledEventsToo: true);
		Root.AddHandler(UIElement.PointerMovedEvent, (PointerEventHandler)delegate(object _, PointerRoutedEventArgs e)
		{
			if (Environment.TickCount % 8 == 0)
			{
				Log.Info("inputtrace", title + " xaml PointerMoved on " + (e.OriginalSource as FrameworkElement)?.GetType().Name);
			}
		}, handledEventsToo: true);
		void Hook(nint h, string label)
		{
			traceSubclasses.Add(new WindowSubclass(h, delegate(nint _, uint msg, nint w, nint l)
			{
				string text = msg switch
				{
					512u => "WM_MOUSEMOVE", 
					513u => "WM_LBUTTONDOWN", 
					514u => "WM_LBUTTONUP", 
					33u => "WM_MOUSEACTIVATE", 
					132u => "WM_NCHITTEST", 
					581u => "WM_POINTERUPDATE", 
					582u => "WM_POINTERDOWN", 
					583u => "WM_POINTERUP", 
					585u => "WM_POINTERENTER", 
					586u => "WM_POINTERLEAVE", 
					587u => "WM_POINTERACTIVATE", 
					588u => "WM_POINTERCAPTURECHANGED", 
					533u => "WM_CAPTURECHANGED", 
					161u => "WM_NCLBUTTONDOWN", 
					675u => "WM_MOUSELEAVE", 
					71u => "WM_WINDOWPOSCHANGED", 
					5u => "WM_SIZE", 
					3u => "WM_MOVE", 
					_ => null, 
				};
				if (text != null && ((msg != 512 && msg != 132 && msg != 581) || Environment.TickCount % 4 == 0))
				{
					Log.Info("inputtrace", $"{title}/{label} {text} w={w} l=0x{l:X}");
				}
				if (msg == 513)
				{
					Point intersectingPoint = new Point((double)(short)(l & 0xFFFF) / Scale, (double)(short)((l >> 16) & 0xFFFF) / Scale);
					IEnumerable<string> values = (from e in VisualTreeHelper.FindElementsInHostCoordinates(intersectingPoint, Root)
						select e.GetType().Name).Take(6);
					Log.Info("inputtrace", $"{title} root {Root.ActualWidth:F0}x{Root.ActualHeight:F0} xamlroot {Root.XamlRoot?.Size.Width:F0}x{Root.XamlRoot?.Size.Height:F0} scale {Root.XamlRoot?.RasterizationScale} at dip {intersectingPoint.X:F0},{intersectingPoint.Y:F0}: {string.Join(" > ", values)}");
				}
				return (nint?)null;
			}));
		}
		void HookChildren()
		{
			foreach (nint item in Native.DirectChildren(Hwnd))
			{
				if (hooked.Add(item))
				{
					Hook(item, $"child {item}");
				}
				foreach (nint item2 in Native.DirectChildren(item))
				{
					if (hooked.Add(item2))
					{
						Hook(item2, $"grandchild {item2}");
					}
				}
			}
		}
		string Where(PointerRoutedEventArgs e)
		{
			Native.GetCursorPos(out var p);
			PointerPoint currentPoint = e.GetCurrentPoint(Root);
			return $"{(e.OriginalSource as FrameworkElement)?.GetType().Name} at {currentPoint.Position} id={e.Pointer.PointerId} {e.Pointer.PointerDeviceType} inContact={currentPoint.IsInContact} frame={currentPoint.FrameId} cursor={p.X},{p.Y}";
		}
	}

	public static bool SetHiddenFromOwnCapture(bool hidden)
	{
		bool num = ownCapturesInProgress > 0;
		ownCapturesInProgress = Math.Max(0, ownCapturesInProgress + (hidden ? 1 : (-1)));
		if (num == ownCapturesInProgress > 0 || !showInScreenRecordings)
		{
			return false;
		}
		List<OverlayWindow> list;
		lock (Live)
		{
			list = (from o in Live.Select((WeakReference<OverlayWindow> w) => (!w.TryGetTarget(out var target)) ? null : target).OfType<OverlayWindow>()
				where o.hiddenFromOwnCapture
				select o).ToList();
		}
		foreach (OverlayWindow item in list)
		{
			item.ApplyCapturePolicy();
		}
		return list.Count > 0;
	}

	public void SetTemporarilyHiddenFromOwnCapture(bool hidden)
	{
		if (temporarilyHiddenFromCapture != hidden)
		{
			temporarilyHiddenFromCapture = hidden;
			ApplyCapturePolicy();
		}
	}

	public void KeepOutOfEveryCapture()
	{
		neverInCapture = true;
		ApplyCapturePolicy();
	}

	private void ApplyCapturePolicy()
	{
		Native.SetWindowDisplayAffinity(Hwnd, ExcludesNow ? 17u : 0u);
	}

	private nint? WndProc(nint h, uint msg, nint w, nint l)
	{
		switch (msg)
		{
		case 20u:
		{
			Native.GetClientRect(h, out var rc);
			Native.FillRect(w, ref rc, Native.GetStockObject(4));
			return 1;
		}
		case 33u:
			if (noActivate)
			{
				return 3;
			}
			break;
		case 6u:
			if ((((IntPtr)w).ToInt64() & 0xFFFF) != 0L)
			{
				FocusWatch.NoteActivated(base.Title);
			}
			break;
		}
		this.RawMessage?.Invoke(msg, w, l);
		return null;
	}

	private void Park()
	{
		if (!Offscreen)
		{
			Native.SetWindowPos(Hwnd, 0, -32000, -32000, 0, 0, 21u);
		}
	}

	private void Unpark()
	{
		if (!shown)
		{
			shown = true;
			Root.IsHitTestVisible = !ignoresMouseEvents;
			var (x, y, cx, cy) = placement;
			Native.SetWindowPos(Hwnd, ZOrder, x, y, cx, cy, (uint)(0x10 | ((!isTopmost) ? 4 : 0)));
			Scale = (double)Native.GetDpiForWindow(Hwnd) / 96.0;
		}
	}

	public void PlacePx(int x, int y, int w, int h)
	{
		if (Offscreen)
		{
			x += -24000;
			y += -24000;
		}
		placement = (X: x, Y: y, W: w, H: h);
		if (!shown)
		{
			Native.SetWindowPos(Hwnd, ZOrder, Offscreen ? x : (-32000), Offscreen ? y : (-32000), w, h, (uint)(0x10 | ((!isTopmost) ? 4 : 0)));
			Scale = Displays.DpiAt(x + w / 2, y + h / 2) / 96.0;
		}
		else
		{
			Native.SetWindowPos(Hwnd, ZOrder, x, y, w, h, (uint)(0x10 | ((!isTopmost) ? 4 : 0)));
			Scale = (double)Native.GetDpiForWindow(Hwnd) / 96.0;
		}
	}

	public void ShowNoActivate()
	{
		Unpark();
		Native.ShowWindow(Hwnd, 4);
		Native.SetWindowPos(Hwnd, ZOrder, 0, 0, 0, 0, 83u);
		ApplyCapturePolicy();
	}

	public void ShowActivated()
	{
		if (Offscreen)
		{
			ShowNoActivate();
			return;
		}
		Unpark();
		Native.ShowWindow(Hwnd, 5);
		Native.ForceForeground(Hwnd);
		ApplyCapturePolicy();
	}

	public void MoveByBackground(UIElement background)
	{
		(Native.POINT Pointer, int Left, int Top)? drag = null;
		background.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(background).Properties.IsLeftButtonPressed && !IsPressOnControl(e.OriginalSource, background) && Native.IsPrimaryButtonDown() && Native.GetCursorPos(out var p) && Native.GetWindowRectPublic(Hwnd, out var rc) && background.CapturePointer(e.Pointer))
			{
				drag = (p, rc.Left, rc.Top);
				e.Handled = true;
			}
		};
		background.PointerMoved += delegate(object _, PointerRoutedEventArgs e)
		{
			if (drag.HasValue)
			{
				(Native.POINT, int, int) valueOrDefault = drag.GetValueOrDefault();
				if (!Native.IsPrimaryButtonDown() || !Native.GetCursorPos(out var p2))
				{
					End(e.Pointer);
				}
				else
				{
					Native.SetWindowPos(Hwnd, 0, valueOrDefault.Item2 + p2.X - valueOrDefault.Item1.X, valueOrDefault.Item3 + p2.Y - valueOrDefault.Item1.Y, 0, 0, 21u);
					e.Handled = true;
				}
			}
		};
		background.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
		{
			End(e.Pointer);
		};
		background.PointerCanceled += delegate(object _, PointerRoutedEventArgs e)
		{
			End(e.Pointer);
		};
		background.PointerCaptureLost += delegate
		{
			drag = null;
		};
		void End(Pointer? pointer)
		{
			if (drag.HasValue)
			{
				drag = null;
				if ((object)pointer != null)
				{
					background.ReleasePointerCapture(pointer);
				}
			}
		}
	}

	public static bool IsPressOnControl(object? source, DependencyObject background)
	{
		DependencyObject dependencyObject = source as DependencyObject;
		while ((object)dependencyObject != null && (object)dependencyObject != background)
		{
			if ((dependencyObject is Pressable || dependencyObject is Control) ? true : false)
			{
				return true;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return false;
	}

	public void HideWindow()
	{
		if (!shown)
		{
			Native.ShowWindow(Hwnd, 0);
			return;
		}
		if (!Offscreen)
		{
			Native.GetWindowRectPublic(Hwnd, out var rc);
			placement = (X: rc.Left, Y: rc.Top, W: rc.Width, H: rc.Height);
		}
		shown = false;
		Park();
		Native.ShowWindow(Hwnd, 0);
		Root.IsHitTestVisible = false;
	}

	public void SetRegionPx(int x, int y, int w, int h)
	{
		SetRegion(Native.CreateRectRgn(x, y, x + Math.Max(0, w), y + Math.Max(0, h)));
	}

	public void SetRegion(nint region)
	{
		Native.GetWindowRectPublic(Hwnd, out var rc);
		foreach (nint item in Native.DirectChildren(Hwnd))
		{
			if (Native.GetWindowRectPublic(item, out var rc2) && rc2.Width > 0 && rc2.Height > 0)
			{
				nint num = Native.CreateRectRgn(0, 0, 0, 0);
				Native.CombineRgn(num, region, 0, 5);
				Native.OffsetRgn(num, rc.Left - rc2.Left, rc.Top - rc2.Top);
				if (HasRegion(item, num))
				{
					Native.DeleteObject(num);
				}
				else if (Native.SetWindowRgn(item, num, redraw: true) == 0)
				{
					Native.DeleteObject(num);
				}
			}
		}
		if (HasRegion(Hwnd, region))
		{
			Native.DeleteObject(region);
		}
		else if (Native.SetWindowRgn(Hwnd, region, redraw: true) == 0)
		{
			Native.DeleteObject(region);
		}
	}

	private static bool HasRegion(nint hwnd, nint region)
	{
		nint num = Native.CreateRectRgn(0, 0, 0, 0);
		bool result = Native.GetWindowRgn(hwnd, num) != 0 && Native.EqualRgn(num, region);
		Native.DeleteObject(num);
		return result;
	}

	public void SetIgnoresMouseEvents(bool ignores)
	{
		if (ignores != ignoresMouseEvents)
		{
			ignoresMouseEvents = ignores;
			if (ignores)
			{
				Native.AddExStyle(Hwnd, 524320u);
				Native.SetLayeredWindowAttributes(Hwnd, 0u, byte.MaxValue, 2u);
			}
			else
			{
				Native.AddExStyle(Hwnd, 0u, staysLayered ? 32u : 524320u);
			}
			Root.IsHitTestVisible = !ignores && shown;
		}
	}

	public void StayLayered()
	{
		staysLayered = true;
		Native.AddExStyle(Hwnd, 524288u);
		Native.SetLayeredWindowAttributes(Hwnd, 0u, byte.MaxValue, 2u);
	}

	public void ClearRegion()
	{
		foreach (nint item in Native.DirectChildren(Hwnd))
		{
			Native.SetWindowRgn(item, 0, redraw: true);
		}
		Native.SetWindowRgn(Hwnd, 0, redraw: true);
	}
}
