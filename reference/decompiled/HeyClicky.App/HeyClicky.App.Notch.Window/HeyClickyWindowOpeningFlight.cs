using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using WinRT;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

namespace HeyClicky.App.Notch.Window;

public sealed class HeyClickyWindowOpeningFlight : IDisposable
{
	private delegate nint WndProcDelegate(nint hwnd, uint message, nint wParam, nint lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSEXW
	{
		public uint cbSize;

		public uint style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		public string? lpszMenuName;

		public string lpszClassName;

		public nint hIconSm;
	}

	private const double NotchBottomRadius = 22.0;

	private const double WindowCornerRadius = 8.0;

	private const double DropletWidth = 224.0;

	private const double DropletHeight = 46.0;

	private const double CoverDuration = 0.09;

	private const double FoldDelay = 0.06;

	private const double FoldDuration = 0.25;

	private const double RevealFadeDuration = 0.3;

	private const int ShadowRings = 14;

	private const double ShadowStep = 2.2;

	private const double ShadowDrop = 7.0;

	private readonly Compositor c;

	private readonly nint hwnd;

	private readonly DesktopWindowTarget target;

	private readonly ContainerVisual blob;

	private readonly CompositionPropertySet p;

	private readonly CompositionColorBrush fill;

	private readonly ShapeVisual shadowShapes;

	private readonly CompositionRoundedRectangleGeometry shape;

	private readonly int originX;

	private readonly int originY;

	private readonly double scale;

	private readonly nint notchWindow;

	private bool notchHidden;

	private readonly Color windowGround;

	private readonly DateTime startedAt = DateTime.UtcNow;

	private volatile bool disposed;

	private const double CoverTimeout = 0.3;

	private readonly List<Action> afterCover = new List<Action>();

	private bool covered;

	private readonly object notchGate = new object();

	private Timer? watchdog;

	private volatile bool revealing;

	private const string ClassName = "HeyClickyWindowFlight";

	private static WndProcDelegate? procedure;

	private static bool registered;

	public static bool IsFlying { get; private set; }

	internal static nint CurrentCarrier { get; private set; }

	internal static DateTime LastUncoveredAt { get; set; }

	internal static DateTime LastNotchHiddenAt { get; private set; }

	internal static DateTime LastNotchShownAt { get; private set; }

	public static HeyClickyWindowOpeningFlight? Begin((int X, int Y, int W, int H) openNotchPx, Displays.Monitor monitor, nint notchWindow, Color windowGround, bool excludeFromCapture)
	{
		if (Anim.Instant || Anim.ReduceMotion || openNotchPx.W <= 0 || openNotchPx.H <= 0)
		{
			return null;
		}
		try
		{
			return new HeyClickyWindowOpeningFlight(openNotchPx, monitor, notchWindow, windowGround, excludeFromCapture);
		}
		catch (Exception ex)
		{
			Log.Warn("window", "no flight out of the notch: " + ex.Message);
			return null;
		}
	}

	private HeyClickyWindowOpeningFlight((int X, int Y, int W, int H) openNotchPx, Displays.Monitor monitor, nint notchWindow, Color windowGround, bool excludeFromCapture)
	{
		this.windowGround = windowGround;
		this.notchWindow = notchWindow;
		scale = monitor.Scale;
		int num = (OverlayWindow.Offscreen ? (-24000) : 0);
		originX = monitor.Bounds.Left + num;
		originY = monitor.Bounds.Top + num;
		hwnd = CreateCarrier(originX, originY, monitor.Bounds.Width, monitor.Bounds.Height, excludeFromCapture);
		c = TransparentBackdrop.SystemCompositor;
		target = CreateTarget(c, hwnd);
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.RelativeSizeAdjustment = Vector2.One;
		target.Root = containerVisual;
		p = c.CreatePropertySet();
		float num2 = (float)(22.0 * scale);
		Vector2 value = new Vector2(openNotchPx.X + num - originX, (float)(openNotchPx.Y + num - originY) - num2);
		Vector2 value2 = new Vector2(openNotchPx.W, (float)openNotchPx.H + num2);
		float num3 = (float)Math.Min(openNotchPx.W, 224.0 * scale);
		float num4 = (float)(46.0 * scale);
		float num5 = num4 / 2f;
		Vector2 value3 = new Vector2(value.X + ((float)openNotchPx.W - num3) / 2f, (float)(openNotchPx.Y + num - originY) - num5);
		Vector2 value4 = new Vector2(num3, num4 + num5);
		p.InsertVector2("OpenOff", value);
		p.InsertVector2("OpenSize", value2);
		p.InsertVector2("OpenR", new Vector2(num2));
		p.InsertVector2("DropOff", value3);
		p.InsertVector2("DropSize", value4);
		p.InsertVector2("DropR", new Vector2(num5));
		p.InsertVector2("WinOff", value3);
		p.InsertVector2("WinSize", value4);
		p.InsertVector2("WinR", new Vector2(num5));
		p.InsertScalar("F", 0f);
		p.InsertScalar("B", 0f);
		shape = c.CreateRoundedRectangleGeometry();
		Bind(shape, "Offset", "Off");
		Bind(shape, "Size", "Size");
		Bind(shape, "CornerRadius", "R");
		fill = c.CreateColorBrush(Colors.Black);
		CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(shape);
		compositionSpriteShape.FillBrush = fill;
		ShapeVisual shapeVisual = c.CreateShapeVisual();
		shapeVisual.RelativeSizeAdjustment = Vector2.One;
		shadowShapes = c.CreateShapeVisual();
		shadowShapes.RelativeSizeAdjustment = Vector2.One;
		shadowShapes.Opacity = 0f;
		CompositionColorBrush fillBrush = c.CreateColorBrush(Color.FromArgb(5, 0, 0, 0));
		for (int num6 = 14; num6 >= 1; num6--)
		{
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = c.CreateRoundedRectangleGeometry();
			float num7 = (float)((double)num6 * 2.2 * scale);
			float num8 = (float)(7.0 * scale);
			Bind(compositionRoundedRectangleGeometry, "Offset", "Off", $"Vector2({0f - num7}, {num8 - num7})");
			Bind(compositionRoundedRectangleGeometry, "Size", "Size", $"Vector2({2f * num7}, {2f * num7})");
			Bind(compositionRoundedRectangleGeometry, "CornerRadius", "R", $"Vector2({num7}, {num7})");
			CompositionSpriteShape compositionSpriteShape2 = c.CreateSpriteShape(compositionRoundedRectangleGeometry);
			compositionSpriteShape2.FillBrush = fillBrush;
			shadowShapes.Shapes.Add(compositionSpriteShape2);
		}
		shapeVisual.Shapes.Add(compositionSpriteShape);
		blob = c.CreateContainerVisual();
		blob.RelativeSizeAdjustment = Vector2.One;
		blob.Children.InsertAtTop(shadowShapes);
		blob.Children.InsertAtTop(shapeVisual);
		blob.Opacity = 0f;
		containerVisual.Children.InsertAtTop(blob);
		Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0, 83u);
		IsFlying = true;
		CurrentCarrier = hwnd;
		Log.Info("window", $"flight: out of the notch {openNotchPx.W}x{openNotchPx.H} at {openNotchPx.X},{openNotchPx.Y}");
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 1f, c.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.58f, 1f)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.09);
		CompositionScopedBatch compositionScopedBatch = c.CreateScopedBatch(CompositionBatchTypes.Animation);
		blob.StartAnimation("Opacity", scalarKeyFrameAnimation);
		compositionScopedBatch.End();
		DispatcherQueue ui = DispatcherQueue.GetForCurrentThread();
		compositionScopedBatch.Completed += delegate
		{
			ui.TryEnqueue(Covered);
		};
		DispatcherQueueTimer coverTimeout = ui.CreateTimer();
		coverTimeout.Interval = TimeSpan.FromSeconds(0.3);
		coverTimeout.IsRepeating = false;
		coverTimeout.Tick += delegate
		{
			coverTimeout.Stop();
			Covered();
		};
		coverTimeout.Start();
		watchdog = new Timer(delegate
		{
			ui.TryEnqueue(delegate
			{
				if (!revealing && !disposed)
				{
					Log.Warn("window", "flight: never landed; the shape fades");
					Reveal();
				}
			});
		}, null, TimeSpan.FromSeconds(3L), Timeout.InfiniteTimeSpan);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation2.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation2.InsertKeyFrame(1f, 1f, c.CreateCubicBezierEasingFunction(new Vector2(0.55f, 0f), new Vector2(0.3f, 1f)));
		scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(0.25);
		scalarKeyFrameAnimation2.DelayTime = TimeSpan.FromSeconds(0.06);
		scalarKeyFrameAnimation2.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		p.StartAnimation("F", scalarKeyFrameAnimation2);
	}

	public void AfterCover(Action action)
	{
		if (covered || disposed)
		{
			action();
		}
		else
		{
			afterCover.Add(action);
		}
	}

	private void Covered()
	{
		if (covered)
		{
			return;
		}
		covered = true;
		HideNotch(hide: true);
		foreach (Action item in afterCover)
		{
			item();
		}
		afterCover.Clear();
	}

	private void Bind(CompositionObject o, string property, string name, string? extra = null)
	{
		string value = $"(p.Open{name} + (p.Drop{name} - p.Open{name}) * p.F)";
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation($"{value} + (p.Win{name} - {value}) * p.B" + ((extra == null) ? "" : (" + " + extra)));
		expressionAnimation.SetReferenceParameter("p", p);
		o.StartAnimation(property, expressionAnimation);
	}

	public void Bloom(Native.RECT windowPx, Action spans)
	{
		if (disposed)
		{
			spans();
			return;
		}
		double totalSeconds = (DateTime.UtcNow - startedAt).TotalSeconds;
		double num = Math.Max(0.0, 0.31 - totalSeconds);
		float value = (float)(8.0 * scale);
		p.InsertVector2("WinOff", new Vector2(windowPx.Left - originX, windowPx.Top - originY));
		p.InsertVector2("WinSize", new Vector2(windowPx.Width, windowPx.Height));
		p.InsertVector2("WinR", new Vector2(value));
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		for (int i = 1; i <= 60; i++)
		{
			double t = 0.95 * (double)i / 60.0;
			scalarKeyFrameAnimation.InsertKeyFrame((float)i / 60f, (i == 60) ? 1f : ((float)HeyClickyWindowFlightSpring.Progress(t)), easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.95);
		scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num);
		scalarKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		p.StartAnimation("B", scalarKeyFrameAnimation);
		Log.Info("window", $"flight: blooms into {windowPx.Width}x{windowPx.Height} at {windowPx.Left},{windowPx.Top} in {num:0.000} s ({totalSeconds:0.000} s in)");
		ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation2.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation2.InsertKeyFrame(1f, 1f);
		scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(0.4);
		scalarKeyFrameAnimation2.DelayTime = TimeSpan.FromSeconds(num);
		shadowShapes.StartAnimation("Opacity", scalarKeyFrameAnimation2);
		ColorKeyFrameAnimation colorKeyFrameAnimation = c.CreateColorKeyFrameAnimation();
		colorKeyFrameAnimation.InsertKeyFrame(0f, Colors.Black);
		colorKeyFrameAnimation.InsertKeyFrame(1f, windowGround, c.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f)));
		colorKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.36);
		colorKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num + 0.06);
		colorKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		fill.StartAnimation("Color", colorKeyFrameAnimation);
		DispatcherQueue queue = DispatcherQueue.GetForCurrentThread();
		CompositionScopedBatch compositionScopedBatch = c.CreateScopedBatch(CompositionBatchTypes.Animation);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation3 = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation3.InsertKeyFrame(1f, 1f, easingFunction);
		scalarKeyFrameAnimation3.Duration = TimeSpan.FromSeconds(num + HeyClickyWindowFlightSpring.FirstCrossing() + 0.02);
		p.InsertScalar("Clock", 0f);
		p.StartAnimation("Clock", scalarKeyFrameAnimation3);
		compositionScopedBatch.End();
		compositionScopedBatch.Completed += delegate
		{
			queue.TryEnqueue(delegate
			{
				Log.Info("window", $"flight: spans the window {(DateTime.UtcNow - startedAt).TotalSeconds:0.000} s in");
				spans();
			});
		};
	}

	public void Reveal(Action? done = null)
	{
		if (disposed)
		{
			done?.Invoke();
			return;
		}
		revealing = true;
		HideNotch(hide: false);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, c.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.58f, 1f)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.3);
		blob.StartAnimation("Opacity", scalarKeyFrameAnimation);
		DispatcherQueue forCurrentThread = DispatcherQueue.GetForCurrentThread();
		DispatcherQueueTimer timer = forCurrentThread.CreateTimer();
		timer.Interval = TimeSpan.FromSeconds(0.35);
		timer.IsRepeating = false;
		timer.Tick += delegate
		{
			timer.Stop();
			Dispose();
			done?.Invoke();
		};
		timer.Start();
	}

	public void Abandon()
	{
		Reveal();
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			watchdog?.Dispose();
			IsFlying = false;
			HideNotch(hide: false);
			if (CurrentCarrier == hwnd)
			{
				CurrentCarrier = 0;
			}
			try
			{
				target.Root = null;
				target.Dispose();
			}
			catch
			{
			}
			if (hwnd != 0)
			{
				DestroyWindow(hwnd);
			}
		}
	}

	private void HideNotch(bool hide)
	{
		lock (notchGate)
		{
			if (notchWindow != 0 && hide != notchHidden && (!hide || (!revealing && !disposed)))
			{
				notchHidden = hide;
				if (hide)
				{
					LastNotchHiddenAt = DateTime.UtcNow;
				}
				else
				{
					LastNotchShownAt = DateTime.UtcNow;
				}
				int value = (hide ? 1 : 0);
				Native.DwmSetWindowAttribute(notchWindow, 13, ref value, 4);
			}
		}
	}

	public static void Cloak(nint window, bool cloaked)
	{
		int value = (cloaked ? 1 : 0);
		Native.DwmSetWindowAttribute(window, 3, ref value, 4);
		Native.DwmSetWindowAttribute(window, 13, ref value, 4);
	}

	public static Native.RECT VisibleFrame(nint window)
	{
		if (DwmGetWindowAttribute(window, 9, out var value, Marshal.SizeOf<Native.RECT>()) >= 0 && value.Width > 0)
		{
			return value;
		}
		Native.GetWindowRectPublic(window, out var rc);
		return rc;
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out Native.RECT value, int size);

	private static nint CreateCarrier(int x, int y, int w, int h, bool excludeFromCapture)
	{
		nint moduleHandleW = GetModuleHandleW(0);
		if (!registered)
		{
			procedure = (nint window, uint message, nint wParam, nint lParam) => Native.DefWindowProc(window, message, wParam, lParam);
			WNDCLASSEXW wNDCLASSEXW = default(WNDCLASSEXW);
			wNDCLASSEXW.cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>();
			wNDCLASSEXW.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(procedure);
			wNDCLASSEXW.hInstance = moduleHandleW;
			wNDCLASSEXW.lpszClassName = "HeyClickyWindowFlight";
			WNDCLASSEXW wc = wNDCLASSEXW;
			if (RegisterClassExW(ref wc) == 0)
			{
				throw new InvalidOperationException($"RegisterClassEx failed ({Marshal.GetLastWin32Error()})");
			}
			registered = true;
		}
		nint num = CreateWindowExW(136839336u, "HeyClickyWindowFlight", "HeyClicky", 2147483648u, x, y, w, h, 0, 0, moduleHandleW, 0);
		if (num == 0)
		{
			throw new InvalidOperationException($"CreateWindowEx failed ({Marshal.GetLastWin32Error()})");
		}
		SetLayeredWindowAttributes(num, 0u, byte.MaxValue, 2u);
		if (excludeFromCapture && !OverlayWindow.ShowInScreenRecordings)
		{
			SetWindowDisplayAffinity(num, 17u);
		}
		return num;
	}

	private unsafe static DesktopWindowTarget CreateTarget(Compositor compositor, nint window)
	{
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(((IWinRTObject)compositor).NativeObject.ThisPtr, new Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"), out var ppv));
		nint num = default(nint);
		try
		{
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)3 * (nint)sizeof(nint))))(ppv, window, 1, &num));
		}
		finally
		{
			Marshal.Release(ppv);
		}
		try
		{
			return DesktopWindowTarget.FromAbi(num);
		}
		finally
		{
			Marshal.Release(num);
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWindowExW(uint exStyle, string className, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool SetLayeredWindowAttributes(nint hwnd, uint key, byte alpha, uint flags);

	[DllImport("user32.dll")]
	private static extern bool SetWindowDisplayAffinity(nint hwnd, uint affinity);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint GetModuleHandleW(nint name);
}
