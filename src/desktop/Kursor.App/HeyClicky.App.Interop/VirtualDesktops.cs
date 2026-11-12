using System;
using System.Runtime.InteropServices;

namespace HeyClicky.App.Interop;

internal static class VirtualDesktops
{
	[ComImport]
	[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IVirtualDesktopManager
	{
		[PreserveSig]
		int IsWindowOnCurrentVirtualDesktop(nint topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);

		[PreserveSig]
		int GetWindowDesktopId(nint topLevelWindow, out Guid desktopId);

		[PreserveSig]
		int MoveWindowToDesktop(nint topLevelWindow, ref Guid desktopId);
	}

	[ComImport]
	[Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
	private class VirtualDesktopManager
	{
	}

	private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

	private static IVirtualDesktopManager? manager;

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

	private static IVirtualDesktopManager? Manager()
	{
		try
		{
			return manager ?? (manager = (IVirtualDesktopManager)new VirtualDesktopManager());
		}
		catch (Exception ex)
		{
			Log.Info("window", "virtual desktops unavailable: " + ex.Message);
			return null;
		}
	}

	public static bool IsOnCurrentDesktop(nint hwnd)
	{
		if (hwnd != 0)
		{
			IVirtualDesktopManager virtualDesktopManager = Manager();
			if (virtualDesktopManager != null)
			{
				try
				{
					bool onCurrentDesktop;
					return virtualDesktopManager.IsWindowOnCurrentVirtualDesktop(hwnd, out onCurrentDesktop) != 0 || onCurrentDesktop;
				}
				catch
				{
					return true;
				}
			}
		}
		return true;
	}

	public static bool MoveToCurrentDesktop(nint hwnd)
	{
		if (hwnd != 0)
		{
			IVirtualDesktopManager virtualDesktopManager = Manager();
			if (virtualDesktopManager != null)
			{
				try
				{
					if (virtualDesktopManager.IsWindowOnCurrentVirtualDesktop(hwnd, out var onCurrentDesktop) != 0 || onCurrentDesktop)
					{
						return false;
					}
					Guid? guid = CurrentDesktopId(virtualDesktopManager, hwnd);
					if (guid.HasValue)
					{
						Guid desktopId = guid.GetValueOrDefault();
						int num = virtualDesktopManager.MoveWindowToDesktop(hwnd, ref desktopId);
						if (num != 0)
						{
							Log.Info("window", $"couldn't move the window to this desktop (0x{num:X8})");
							return false;
						}
						Log.Info("window", "moved the HeyClicky window to the desktop the user is on");
						return true;
					}
					return false;
				}
				catch (Exception ex)
				{
					Log.Info("window", "couldn't move the window to this desktop: " + ex.Message);
					return false;
				}
			}
		}
		return false;
	}

	private static Guid? CurrentDesktopId(IVirtualDesktopManager desktops, nint ours)
	{
		Guid? guid = IdOf(GetForegroundWindow());
		if (guid.HasValue)
		{
			return guid.GetValueOrDefault();
		}
		GetWindowThreadProcessId(ours, out var ourProcess);
		Guid? found = null;
		EnumWindows(delegate(nint candidate, nint _)
		{
			GetWindowThreadProcessId(candidate, out var processId);
			if (processId != ourProcess)
			{
				found = IdOf(candidate);
			}
			return !found.HasValue;
		}, 0);
		return found;
		Guid? IdOf(nint candidate)
		{
			if (candidate == 0 || candidate == ours || !IsWindowVisible(candidate))
			{
				return null;
			}
			if (desktops.IsWindowOnCurrentVirtualDesktop(candidate, out var onCurrentDesktop) != 0 || !onCurrentDesktop)
			{
				return null;
			}
			if (desktops.GetWindowDesktopId(candidate, out var desktopId) != 0 || !(desktopId != Guid.Empty))
			{
				return null;
			}
			return desktopId;
		}
	}
}
