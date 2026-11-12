using System;
using System.Runtime.CompilerServices;
using HeyClicky.Platform.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Overlay;

internal sealed class DetachedClickCatcher : IDisposable
{
	private const string ClassName = "HeyClickyDetachedCursorCatcher";

	private static WNDPROC? procedure;

	private static DetachedClickCatcher? current;

	private HWND hwnd;

	private bool hovered;

	private bool tracking;

	public bool IsHovered => hovered;

	public nint Handle => (nint)hwnd;

	public event Action? Clicked;

	public event Action<bool>? HoverChanged;

	public unsafe DetachedClickCatcher()
	{
		if (procedure == null)
		{
			procedure = WindowProcedure;
		}
		fixed (char* ptr = &"HeyClickyDetachedCursorCatcher".GetPinnableReference())
		{
			WNDCLASSEXW param = new WNDCLASSEXW
			{
				cbSize = (uint)Unsafe.SizeOf<WNDCLASSEXW>(),
				lpfnWndProc = procedure,
				hInstance = (HINSTANCE)PInvoke.GetModuleHandle((string)null).DangerousGetHandle(),
				lpszClassName = ptr,
				hCursor = NativeCursor.LoadArrow()
			};
			PInvoke.RegisterClassEx(in param);
		}
		hwnd = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOACTIVATE, "HeyClickyDetachedCursorCatcher", "HeyClicky detached cursor", WINDOW_STYLE.WS_POPUP, 0, 0, 1, 1, HWND.Null, null, null, null);
		PInvoke.SetLayeredWindowAttributes(hwnd, new COLORREF(0u), 1, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
		CaptureExclusion.Apply((nint)hwnd);
		current = this;
	}

	public void Show(RectD rectPx)
	{
		PInvoke.SetWindowPos(hwnd, new HWND(FullscreenGuard.TopmostInsertAfter()), (int)Math.Round(rectPx.X), (int)Math.Round(rectPx.Y), (int)Math.Round(rectPx.Width), (int)Math.Round(rectPx.Height), SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
	}

	public void Hide()
	{
		PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
		SetHovered(value: false);
	}

	private void SetHovered(bool value)
	{
		if (hovered != value)
		{
			hovered = value;
			this.HoverChanged?.Invoke(value);
		}
	}

	private static LRESULT WindowProcedure(HWND window, uint message, WPARAM wParam, LPARAM lParam)
	{
		DetachedClickCatcher detachedClickCatcher = current;
		if (detachedClickCatcher != null && detachedClickCatcher.hwnd == window)
		{
			switch (message)
			{
			case 33u:
				return new LRESULT(3);
			case 512u:
				if (!detachedClickCatcher.tracking)
				{
					detachedClickCatcher.tracking = NativeCursor.TrackLeave(window);
				}
				detachedClickCatcher.SetHovered(value: true);
				return new LRESULT(0);
			case 675u:
				detachedClickCatcher.tracking = false;
				detachedClickCatcher.SetHovered(value: false);
				return new LRESULT(0);
			case 513u:
				try
				{
					detachedClickCatcher.Clicked?.Invoke();
				}
				catch (Exception ex)
				{
					Log.Error("Overlay", "detached cursor click handler threw", ex);
				}
				return new LRESULT(0);
			}
		}
		return PInvoke.DefWindowProc(window, message, wParam, lParam);
	}

	public void Dispose()
	{
		if (current == this)
		{
			current = null;
		}
		if (!hwnd.IsNull)
		{
			PInvoke.DestroyWindow(hwnd);
		}
		hwnd = HWND.Null;
	}
}
