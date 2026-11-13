using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HeyClicky.Drawing.Interop;
using Microsoft.UI.Input;
using Windows.Graphics;
using Windows.System;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

namespace HeyClicky.Drawing.Overlay;

internal sealed class OverlayWindow : IDisposable
{
	private struct CursorPoint
	{
		public int X;

		public int Y;
	}

	private const string ClassName = "HeyClickyDrawingWindow";

	private static NativeMethods.WndProc? classProcedure;

	private static bool classRegistered;

	private static readonly Dictionary<nint, OverlayWindow> windows = new Dictionary<nint, OverlayWindow>();

	internal static bool ExcludeFromCapture = true;

	private static readonly object capturePolicyGate = new object();

	private static readonly List<OverlayWindow> live = new List<OverlayWindow>();

	private static bool showInScreenRecordings;

	private static int hiddenFromOwnCaptureCount;

	private readonly DispatcherQueue? ownerQueue = DispatcherQueue.GetForCurrentThread();

	private static readonly int OffscreenShift = ((Environment.GetEnvironmentVariable("HEYCLICKY_OFFSCREEN") == "1") ? (-24000) : 0);

	private readonly DesktopWindowTarget target;

	private readonly ContainerVisual pixelRoot;

	private double scale = 1.0;

	private readonly bool interactive;

	private bool tracking;

	private const uint GW_HWNDPREV = 3u;

	private nint shapeCursor;

	private static bool ExcludedNow
	{
		get
		{
			if (ExcludeFromCapture)
			{
				if (showInScreenRecordings)
				{
					return hiddenFromOwnCaptureCount > 0;
				}
				return true;
			}
			return false;
		}
	}

	internal static bool ShowInScreenRecordings
	{
		get
		{
			lock (capturePolicyGate)
			{
				return showInScreenRecordings;
			}
		}
		set
		{
			lock (capturePolicyGate)
			{
				if (showInScreenRecordings == value)
				{
					return;
				}
				showInScreenRecordings = value;
			}
			_ = ApplyCapturePolicyToAllAsync();
		}
	}

	public nint Handle { get; }

	public ContainerVisual Root { get; }

	public RectInt32 BoundsPx { get; private set; }

	public bool IsShown { get; private set; }

	public double RasterizationScale => scale;

	public bool ShowsHandCursor { get; set; } = true;

	public nint CustomCursor { get; set; }

	public event Action<int, int>? PointerMoved;

	public event Action<int, int>? PointerPressed;

	public event Action<int, int>? PointerReleased;

	public event Action? PointerLeft;

	internal static async Task<bool> SetHiddenFromOwnCaptureAsync(bool hidden)
	{
		lock (capturePolicyGate)
		{
			bool excludedNow = ExcludedNow;
			hiddenFromOwnCaptureCount = Math.Max(0, hiddenFromOwnCaptureCount + (hidden ? 1 : (-1)));
			if (excludedNow == ExcludedNow || live.Count == 0)
			{
				return false;
			}
		}
		await ApplyCapturePolicyToAllAsync().ConfigureAwait(continueOnCapturedContext: false);
		return true;
	}

	internal static Task SetShowInScreenRecordingsAsync(bool value)
	{
		lock (capturePolicyGate)
		{
			if (showInScreenRecordings == value)
			{
				return Task.CompletedTask;
			}
			showInScreenRecordings = value;
		}
		return ApplyCapturePolicyToAllAsync();
	}

	private static async Task ApplyCapturePolicyToAllAsync()
	{
		try
		{
			List<OverlayWindow> list;
			lock (capturePolicyGate)
			{
				list = live.ToList();
			}
			List<Task> list2 = new List<Task>();
			foreach (OverlayWindow window in list)
			{
				DispatcherQueue dispatcherQueue = window.ownerQueue;
				if ((object)dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
				{
					window.ApplyCapturePolicy();
					continue;
				}
				TaskCompletionSource done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				if (dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, delegate
				{
					window.ApplyCapturePolicy();
					done.TrySetResult();
				}))
				{
					list2.Add(done.Task);
				}
			}
			if (list2.Count > 0)
			{
				await Task.WhenAny(Task.WhenAll(list2), Task.Delay(250)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
		}
	}

	private void ApplyCapturePolicy()
	{
		bool excludedNow;
		lock (capturePolicyGate)
		{
			if (!live.Contains(this))
			{
				return;
			}
			excludedNow = ExcludedNow;
		}
		if (!NativeMethods.SetWindowDisplayAffinity(Handle, excludedNow ? 17u : 0u) && excludedNow)
		{
			Log.Warn("Drawing", $"SetWindowDisplayAffinity failed ({Marshal.GetLastWin32Error()}); the canvas may show in our own screenshots");
		}
	}

	public OverlayWindow(Compositor compositor, string title, bool interactive = false, bool win32Input = false)
	{
		this.interactive = interactive;
		RegisterClass();
		int num = 136315016;
		if (!interactive)
		{
			num |= 0x80020;
		}
		Handle = NativeMethods.CreateWindowExW(num, "HeyClickyDrawingWindow", title, int.MinValue, 0, 0, 16, 16, IntPtr.Zero, IntPtr.Zero, NativeMethods.GetModuleHandleW(IntPtr.Zero), IntPtr.Zero);
		if (Handle == IntPtr.Zero)
		{
			throw new InvalidOperationException($"CreateWindowEx failed ({Marshal.GetLastWin32Error()})");
		}
		NonRudeWindow.Mark(Handle);
		windows[Handle] = this;
		if (!interactive)
		{
			NativeMethods.SetLayeredWindowAttributes(Handle, 0u, byte.MaxValue, 2u);
		}
		lock (capturePolicyGate)
		{
			live.Add(this);
		}
		ApplyCapturePolicy();
		target = SystemComposition.CreateWindowTarget(compositor, Handle);
		pixelRoot = compositor.CreateContainerVisual();
		pixelRoot.RelativeSizeAdjustment = Vector2.One;
		target.Root = pixelRoot;
		Root = compositor.CreateContainerVisual();
		pixelRoot.Children.InsertAtTop(Root);
		Fit(16, 16);
	}

	private void Fit(int widthPx, int heightPx)
	{
		uint dpiForWindow = GetDpiForWindow(Handle);
		scale = ((dpiForWindow != 0) ? ((double)dpiForWindow / 96.0) : 1.0);
		Root.Scale = new Vector3((float)scale, (float)scale, 1f);
		Root.Size = new Vector2((float)((double)widthPx / scale), (float)((double)heightPx / scale));
	}

	public void ApplyCustomCursor(nint cursor)
	{
		CustomCursor = cursor;
		if (cursor != IntPtr.Zero)
		{
			ShowCursorIfOver(cursor);
		}
	}

	private void ShowCursorIfOver(nint cursor)
	{
		if (GetCursorPos(out var point) && WindowFromPoint(point) == Handle)
		{
			NativeMethods.SetCursor(cursor);
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out CursorPoint point);

	[DllImport("user32.dll")]
	private static extern nint WindowFromPoint(CursorPoint point);

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint window, uint command);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint window);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowTextW(nint window, char[] text, int maxCount);

	public static nint TopOfRunAbove(nint window, string title)
	{
		if (window == IntPtr.Zero)
		{
			return window;
		}
		char[] array = new char[title.Length + 2];
		nint num = window;
		for (int i = 0; i < 256; i++)
		{
			nint window2 = GetWindow(num, 3u);
			if (window2 == IntPtr.Zero)
			{
				break;
			}
			int windowTextW = GetWindowTextW(window2, array, array.Length);
			if (windowTextW != title.Length || !title.AsSpan().SequenceEqual(array.AsSpan(0, windowTextW)))
			{
				break;
			}
			num = window2;
		}
		return num;
	}

	public void SetHandCursor(bool hand)
	{
		ShowsHandCursor = hand;
		shapeCursor = IntPtr.Zero;
		if (interactive && CustomCursor == IntPtr.Zero)
		{
			ShowCursorIfOver(NativeMethods.LoadCursorW(IntPtr.Zero, hand ? NativeMethods.IDC_HAND : NativeMethods.IDC_ARROW));
		}
	}

	public void SetCursor(InputSystemCursorShape shape)
	{
		if (interactive)
		{
			shapeCursor = NativeMethods.LoadCursorW(name: shape switch
			{
				InputSystemCursorShape.Hand => NativeMethods.IDC_HAND, 
				InputSystemCursorShape.SizeAll => 32646, 
				InputSystemCursorShape.SizeWestEast => 32644, 
				InputSystemCursorShape.SizeNorthSouth => 32645, 
				InputSystemCursorShape.SizeNorthwestSoutheast => 32642, 
				InputSystemCursorShape.SizeNortheastSouthwest => 32643, 
				InputSystemCursorShape.IBeam => 32513, 
				InputSystemCursorShape.Cross => 32515, 
				_ => NativeMethods.IDC_ARROW, 
			}, instance: IntPtr.Zero);
			if (CustomCursor == IntPtr.Zero)
			{
				ShowCursorIfOver(shapeCursor);
			}
		}
	}

	private static void RegisterClass()
	{
		if (!classRegistered)
		{
			classProcedure = WindowProcedure;
			NativeMethods.WNDCLASSEXW wNDCLASSEXW = default(NativeMethods.WNDCLASSEXW);
			wNDCLASSEXW.cbSize = Marshal.SizeOf<NativeMethods.WNDCLASSEXW>();
			wNDCLASSEXW.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(classProcedure);
			wNDCLASSEXW.hInstance = NativeMethods.GetModuleHandleW(IntPtr.Zero);
			wNDCLASSEXW.lpszClassName = Marshal.StringToHGlobalUni("HeyClickyDrawingWindow");
			wNDCLASSEXW.hCursor = NativeMethods.LoadCursorW(IntPtr.Zero, NativeMethods.IDC_ARROW);
			NativeMethods.WNDCLASSEXW windowClass = wNDCLASSEXW;
			if (NativeMethods.RegisterClassExW(ref windowClass) == 0)
			{
				throw new InvalidOperationException($"RegisterClassEx failed ({Marshal.GetLastWin32Error()})");
			}
			classRegistered = true;
		}
	}

	private static nint WindowProcedure(nint hwnd, uint message, nint wParam, nint lParam)
	{
		try
		{
			windows.TryGetValue(hwnd, out OverlayWindow value);
			switch (message)
			{
			case 33u:
				return 3;
			case 736u:
				if (value != null)
				{
					value.Fit(value.BoundsPx.Width, value.BoundsPx.Height);
				}
				return IntPtr.Zero;
			case 132u:
				if (value == null || !value.interactive)
				{
					return -1;
				}
				return 1;
			case 32u:
				if (value != null && value.interactive)
				{
					NativeMethods.SetCursor((value.CustomCursor != IntPtr.Zero) ? value.CustomCursor : ((value.shapeCursor != IntPtr.Zero) ? value.shapeCursor : NativeMethods.LoadCursorW(IntPtr.Zero, value.ShowsHandCursor ? NativeMethods.IDC_HAND : NativeMethods.IDC_ARROW)));
					return 1;
				}
				break;
			case 512u:
				if (value != null)
				{
					if (!value.tracking)
					{
						NativeMethods.TRACKMOUSEEVENT tRACKMOUSEEVENT = default(NativeMethods.TRACKMOUSEEVENT);
						tRACKMOUSEEVENT.cbSize = Marshal.SizeOf<NativeMethods.TRACKMOUSEEVENT>();
						tRACKMOUSEEVENT.dwFlags = 2u;
						tRACKMOUSEEVENT.hwndTrack = hwnd;
						NativeMethods.TRACKMOUSEEVENT track = tRACKMOUSEEVENT;
						value.tracking = NativeMethods.TrackMouseEvent(ref track);
					}
					value.PointerMoved?.Invoke(NativeMethods.LowWord(lParam), NativeMethods.HighWord(lParam));
					return IntPtr.Zero;
				}
				break;
			case 675u:
				if (value != null)
				{
					value.tracking = false;
					value.PointerLeft?.Invoke();
					return IntPtr.Zero;
				}
				break;
			case 513u:
				if (value != null)
				{
					NativeMethods.SetCapture(hwnd);
					value.PointerPressed?.Invoke(NativeMethods.LowWord(lParam), NativeMethods.HighWord(lParam));
					return IntPtr.Zero;
				}
				break;
			case 514u:
				if (value != null)
				{
					NativeMethods.ReleaseCapture();
					value.PointerReleased?.Invoke(NativeMethods.LowWord(lParam), NativeMethods.HighWord(lParam));
					return IntPtr.Zero;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Drawing", "window message failed", ex);
		}
		return NativeMethods.DefWindowProcW(hwnd, message, wParam, lParam);
	}

	public void ShowOver(RectInt32 boundsPx, nint justAbove = 0)
	{
		BoundsPx = boundsPx;
		NativeMethods.SetWindowPos(Handle, InsertAfterToBeAbove(justAbove), boundsPx.X + OffscreenShift, boundsPx.Y, boundsPx.Width, boundsPx.Height, 592u);
		Fit(boundsPx.Width, boundsPx.Height);
		if (!IsShown)
		{
			NativeMethods.ShowWindow(Handle, 8);
			IsShown = true;
			if (justAbove != IntPtr.Zero || FullscreenGuard.GameWindow() != IntPtr.Zero)
			{
				StackJustAbove(justAbove);
			}
		}
	}

	public void Reframe(RectInt32 boundsPx)
	{
		BoundsPx = boundsPx;
		NativeMethods.SetWindowPos(Handle, IntPtr.Zero, boundsPx.X + OffscreenShift, boundsPx.Y, boundsPx.Width, boundsPx.Height, 532u);
		Fit(boundsPx.Width, boundsPx.Height);
	}

	public void StackJustAbove(nint window)
	{
		NativeMethods.SetWindowPos(Handle, InsertAfterToBeAbove(window), 0, 0, 0, 0, 531u);
	}

	private nint InsertAfterToBeAbove(nint window)
	{
		nint num = ((window == IntPtr.Zero) ? IntPtr.Zero : GetWindow(window, 3u));
		if (num == IntPtr.Zero)
		{
			return FullscreenGuard.TopmostInsertAfter();
		}
		if (num != Handle)
		{
			return num;
		}
		nint window2 = GetWindow(Handle, 3u);
		if (window2 == IntPtr.Zero)
		{
			return FullscreenGuard.TopmostInsertAfter();
		}
		return window2;
	}

	public void SetBelow(nint window, bool isBelow)
	{
		nint insertAfter = ((!isBelow) ? FullscreenGuard.TopmostInsertAfter() : ((window != IntPtr.Zero) ? window : NativeMethods.HWND_NOTTOPMOST));
		NativeMethods.SetWindowPos(Handle, insertAfter, 0, 0, 0, 0, 531u);
	}

	public void MoveTo(int x, int y)
	{
		BoundsPx = new RectInt32(x, y, BoundsPx.Width, BoundsPx.Height);
		NativeMethods.SetWindowPos(Handle, IntPtr.Zero, x + OffscreenShift, y, 0, 0, 533u);
		Fit(BoundsPx.Width, BoundsPx.Height);
	}

	public void Hide()
	{
		if (IsShown)
		{
			NativeMethods.ShowWindow(Handle, 0);
			IsShown = false;
		}
	}

	public void Dispose()
	{
		windows.Remove(Handle);
		lock (capturePolicyGate)
		{
			live.Remove(this);
		}
		try
		{
			target.Dispose();
		}
		catch
		{
		}
		NativeMethods.DestroyWindow(Handle);
	}
}
