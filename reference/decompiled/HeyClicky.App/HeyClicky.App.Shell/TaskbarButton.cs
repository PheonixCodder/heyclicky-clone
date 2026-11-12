using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HeyClicky.App.Shell;

public sealed class TaskbarButton : IDisposable
{
	private delegate nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam);

	[ComImport]
	[Guid("56FDF342-FD6D-11d0-958A-006097C9A090")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface ITaskbarList
	{
		[PreserveSig]
		int HrInit();

		[PreserveSig]
		int AddTab(nint hwnd);

		[PreserveSig]
		int DeleteTab(nint hwnd);

		[PreserveSig]
		int ActivateTab(nint hwnd);

		[PreserveSig]
		int SetActiveAlt(nint hwnd);
	}

	[ComImport]
	[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
	[ClassInterface(ClassInterfaceType.None)]
	private class TaskbarListClass
	{
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSEX
	{
		public int cbSize;

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

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct WINDOWPLACEMENT
	{
		public uint length;

		public uint flags;

		public uint showCmd;

		public POINT ptMinPosition;

		public POINT ptMaxPosition;

		public RECT rcNormalPosition;
	}

	private struct WINDOWPOS
	{
		public nint hwnd;

		public nint hwndInsertAfter;

		public int x;

		public int y;

		public int cx;

		public int cy;

		public uint flags;
	}

	private const string ClassName = "HeyClickyTaskbarButton";

	private const int WS_OVERLAPPEDWINDOW = 13565952;

	private const int WS_EX_APPWINDOW = 262144;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int SW_HIDE = 0;

	private const int SW_SHOWMINNOACTIVE = 7;

	private const uint WM_SYSCOMMAND = 274u;

	private const uint WM_CLOSE = 16u;

	private const uint WM_SETICON = 128u;

	private const uint WM_QUERYOPEN = 19u;

	private const uint WM_ACTIVATE = 6u;

	private const int SC_RESTORE = 61728;

	private const int SC_MAXIMIZE = 61488;

	private const int SC_CLOSE = 61536;

	private const uint IMAGE_ICON = 1u;

	private const uint LR_LOADFROMFILE = 16u;

	private const uint LR_DEFAULTSIZE = 64u;

	private const uint WM_WINDOWPOSCHANGING = 70u;

	private const uint SWP_NOMOVE = 2u;

	private const uint WPF_SETMINPOSITION = 1u;

	private const int ParkedX = -32000;

	private const int ParkedY = -32000;

	private static WndProc? wndProc;

	private static bool classRegistered;

	private static readonly uint TaskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");

	private nint hwnd;

	private bool shown;

	private readonly ITaskbarList? taskbarList;

	private static readonly Dictionary<nint, TaskbarButton> instances = new Dictionary<nint, TaskbarButton>();

	private long lastClick;

	public event Action? Clicked;

	public event Action? CloseRequested;

	public TaskbarButton(string title, string iconPath)
	{
		nint moduleHandle = GetModuleHandle(null);
		if (!classRegistered)
		{
			wndProc = Proc;
			WNDCLASSEX wc = new WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<WNDCLASSEX>(),
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
				hInstance = moduleHandle,
				lpszClassName = "HeyClickyTaskbarButton"
			};
			if (RegisterClassEx(ref wc) == 0)
			{
				Log.Warn("taskbar", $"register class failed ({Marshal.GetLastWin32Error()})");
				return;
			}
			classRegistered = true;
		}
		taskbarList = CreateTaskbarList();
		hwnd = CreateWindowEx((taskbarList != null) ? 128 : 262144, "HeyClickyTaskbarButton", title, 13565952, -32000, -32000, 1, 1, 0, 0, moduleHandle, 0);
		if (hwnd == 0)
		{
			Log.Warn("taskbar", $"create window failed ({Marshal.GetLastWin32Error()})");
			return;
		}
		instances[hwnd] = this;
		(int, uint)[] array = new(int, uint)[2]
		{
			(0, 1u),
			(16, 0u)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(int, uint) tuple = array[i];
			int item = tuple.Item1;
			uint item2 = tuple.Item2;
			nint num = LoadImage(0, iconPath, 1u, item, item, (uint)(0x10 | ((item == 0) ? 64 : 0)));
			if (num != 0)
			{
				SendMessage(hwnd, 128u, (nint)item2, num);
			}
		}
	}

	public void SetShown(bool show)
	{
		if (hwnd != 0 && show != shown)
		{
			shown = show;
			if (!show)
			{
				DeleteTab();
			}
			if (show)
			{
				ShowMinimizedOffDesktop();
			}
			else
			{
				ShowWindow(hwnd, 0);
			}
			if (show)
			{
				AddTab();
			}
			Log.Info("taskbar", show ? "button shown" : "button hidden");
		}
	}

	private void ShowMinimizedOffDesktop()
	{
		WINDOWPLACEMENT wINDOWPLACEMENT = default(WINDOWPLACEMENT);
		wINDOWPLACEMENT.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();
		wINDOWPLACEMENT.flags = 1u;
		wINDOWPLACEMENT.showCmd = 7u;
		wINDOWPLACEMENT.ptMinPosition = new POINT
		{
			X = -32000,
			Y = -32000
		};
		wINDOWPLACEMENT.ptMaxPosition = new POINT
		{
			X = -1,
			Y = -1
		};
		wINDOWPLACEMENT.rcNormalPosition = new RECT
		{
			Left = -32000,
			Top = -32000,
			Right = -31999,
			Bottom = -31999
		};
		WINDOWPLACEMENT placement = wINDOWPLACEMENT;
		if (!SetWindowPlacement(hwnd, ref placement))
		{
			Log.Warn("taskbar", $"SetWindowPlacement failed ({Marshal.GetLastWin32Error()}): minimizing in place");
			ShowWindow(hwnd, 7);
		}
	}

	private static ITaskbarList? CreateTaskbarList()
	{
		try
		{
			ITaskbarList taskbarList = (ITaskbarList)new TaskbarListClass();
			if (taskbarList.HrInit() >= 0)
			{
				return taskbarList;
			}
			Log.Warn("taskbar", "the taskbar list didn't start: the button is an app window (in Alt+Tab)");
		}
		catch (Exception ex)
		{
			Log.Warn("taskbar", "no taskbar list (" + ex.Message + "): the button is an app window (in Alt+Tab)");
		}
		return null;
	}

	private void AddTab()
	{
		if (taskbarList == null || hwnd == 0)
		{
			return;
		}
		try
		{
			int num = taskbarList.AddTab(hwnd);
			if (num < 0)
			{
				Log.Warn("taskbar", $"AddTab failed (0x{num:X8})");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("taskbar", "AddTab failed: " + ex.Message);
		}
	}

	private void DeleteTab()
	{
		if (taskbarList == null || hwnd == 0)
		{
			return;
		}
		try
		{
			taskbarList.DeleteTab(hwnd);
		}
		catch (Exception ex)
		{
			Log.Warn("taskbar", "DeleteTab failed: " + ex.Message);
		}
	}

	private void RaiseClicked()
	{
		long tickCount = Environment.TickCount64;
		if (tickCount - lastClick >= 400)
		{
			lastClick = tickCount;
			this.Clicked?.Invoke();
		}
	}

	private static nint Proc(nint h, uint msg, nint w, nint l)
	{
		if (instances.TryGetValue(h, out TaskbarButton value))
		{
			if (msg == 70 && l != 0 && IsIconic(h))
			{
				WINDOWPOS structure = Marshal.PtrToStructure<WINDOWPOS>(l);
				if ((structure.flags & 2) == 0 && (structure.x != -32000 || structure.y != -32000))
				{
					structure.x = -32000;
					structure.y = -32000;
					Marshal.StructureToPtr(structure, l, fDeleteOld: false);
				}
			}
			if (msg == TaskbarCreatedMessage && TaskbarCreatedMessage != 0)
			{
				if (value.shown)
				{
					value.AddTab();
				}
				return 0;
			}
			if (msg <= 16)
			{
				if (msg != 6)
				{
					if (msg == 16)
					{
						goto IL_00e9;
					}
				}
				else if ((w & 0xFFFF) != 0)
				{
					return 0;
				}
			}
			else
			{
				if (msg == 19)
				{
					value.RaiseClicked();
					return 0;
				}
				if (msg == 274)
				{
					int num = (int)w & 0xFFF0;
					if ((num == 61488 || num == 61728) ? true : false)
					{
						value.RaiseClicked();
						return 0;
					}
					if (((int)w & 0xFFF0) == 61536)
					{
						goto IL_00e9;
					}
				}
			}
		}
		return DefWindowProc(h, msg, w, l);
		IL_00e9:
		value.CloseRequested?.Invoke();
		return 0;
	}

	public void Dispose()
	{
		if (hwnd != 0)
		{
			DeleteTab();
			instances.Remove(hwnd);
			DestroyWindow(hwnd);
			hwnd = 0;
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern uint RegisterWindowMessage(string name);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWindowEx(int exStyle, string className, string title, int style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
	private static extern nint DefWindowProc(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hwnd, int cmd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPlacement(nint hwnd, ref WINDOWPLACEMENT placement);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern nint SendMessage(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint LoadImage(nint instance, string name, uint type, int cx, int cy, uint load);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint GetModuleHandle(string? name);
}
