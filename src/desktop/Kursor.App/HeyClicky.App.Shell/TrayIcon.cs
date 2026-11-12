using System;
using System.Runtime.InteropServices;
using HeyClicky.App.Interop;

namespace HeyClicky.App.Shell;

public sealed class TrayIcon : IDisposable
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATAW
	{
		public int cbSize;

		public nint hWnd;

		public uint uID;

		public uint uFlags;

		public uint uCallbackMessage;

		public nint hIcon;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;

		public uint dwState;

		public uint dwStateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szInfo;

		public uint uVersion;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string szInfoTitle;

		public uint dwInfoFlags;

		public Guid guidItem;

		public nint hBalloonIcon;
	}

	private delegate nint WndProcDelegate(nint hwnd, uint msg, nint w, nint l);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSW
	{
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
	}

	private struct ICONINFO
	{
		public bool fIcon;

		public int xHotspot;

		public int yHotspot;

		public nint hbmMask;

		public nint hbmColor;
	}

	private struct BITMAPINFOHEADER
	{
		public int biSize;

		public int biWidth;

		public int biHeight;

		public short biPlanes;

		public short biBitCount;

		public int biCompression;

		public int biSizeImage;

		public int biXPelsPerMeter;

		public int biYPelsPerMeter;

		public int biClrUsed;

		public int biClrImportant;
	}

	private const uint WM_TRAY = 32769u;

	private const uint NIM_ADD = 0u;

	private const uint NIM_MODIFY = 1u;

	private const uint NIM_DELETE = 2u;

	private const uint NIM_SETVERSION = 4u;

	private const uint NIF_MESSAGE = 1u;

	private const uint NIF_ICON = 2u;

	private const uint NIF_TIP = 4u;

	private const uint NIF_SHOWTIP = 128u;

	private const int ID_SETTINGS = 1;

	private const int ID_PAUSE = 2;

	private const int ID_QUIT = 3;

	private readonly WndProcDelegate proc;

	private readonly nint hwnd;

	private nint icon;

	private readonly uint taskbarCreated;

	private NOTIFYICONDATAW data;

	private const int ID_REPORT_BUG = 10;

	private const int ID_REQUEST_FEATURE = 11;

	private const int ID_WHATS_NEW = 12;

	private const int ID_CHECK_UPDATES = 13;

	public Func<bool>? IsPaused { get; set; }

	public event Action? OpenSettings;

	public event Action? Quit;

	public event Action? ReportBug;

	public event Action? RequestFeature;

	public event Action? WhatsNew;

	public event Action? CheckForUpdates;

	public event Action? Clicked;

	public event Action<bool>? PauseToggled;

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern bool Shell_NotifyIconW(uint msg, ref NOTIFYICONDATAW data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern ushort RegisterClassW(ref WNDCLASSW wc);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateWindowExW(uint ex, string cls, string name, uint style, int x, int y, int w, int h, nint parent, nint menu, nint inst, nint param);

	[DllImport("user32.dll")]
	private static extern nint DefWindowProcW(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool AppendMenuW(nint menu, uint flags, nint id, string? text);

	[DllImport("user32.dll")]
	private static extern int TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint hwnd, nint tpm);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(nint menu);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out Native.POINT p);

	[DllImport("user32.dll")]
	private static extern nint CreateIconIndirect(ref ICONINFO info);

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(nint icon);

	[DllImport("gdi32.dll")]
	private static extern nint CreateBitmap(int w, int h, uint planes, uint bpp, byte[]? bits);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint hdc, ref BITMAPINFOHEADER bi, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint o);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandleW(string? name);

	[DllImport("user32.dll")]
	private static extern uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)] string name);

	public TrayIcon(string tooltip, uint colorArgb)
	{
		proc = WndProc;
		WNDCLASSW wc = new WNDCLASSW
		{
			lpfnWndProc = Marshal.GetFunctionPointerForDelegate(proc),
			hInstance = GetModuleHandleW(null),
			lpszClassName = "HeyClickyTray"
		};
		RegisterClassW(ref wc);
		hwnd = CreateWindowExW(0u, "HeyClickyTray", "HeyClickyTray", 0u, 0, 0, 0, 0, -3, 0, wc.hInstance, 0);
		taskbarCreated = RegisterWindowMessageW("TaskbarCreated");
		icon = MakeIcon(colorArgb);
		data = new NOTIFYICONDATAW
		{
			cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
			hWnd = hwnd,
			uID = 1u,
			uFlags = 135u,
			uCallbackMessage = 32769u,
			hIcon = icon,
			szTip = tooltip,
			szInfo = "",
			szInfoTitle = "",
			uVersion = 4u
		};
		Add();
	}

	private void Add()
	{
		Shell_NotifyIconW(0u, ref data);
		Shell_NotifyIconW(4u, ref data);
	}

	private nint WndProc(nint h, uint msg, nint w, nint l)
	{
		if (msg == 32769)
		{
			switch ((uint)(l & 0xFFFF))
			{
			case 514u:
			case 1024u:
			{
				Action action = this.Clicked;
				if (action != null)
				{
					action();
				}
				else
				{
					this.OpenSettings?.Invoke();
				}
				return 0;
			}
			case 123u:
			case 517u:
				ShowMenu();
				return 0;
			}
		}
		else if (msg == taskbarCreated)
		{
			Add();
		}
		return DefWindowProcW(h, msg, w, l);
	}

	private void ShowMenu()
	{
		nint menu = CreatePopupMenu();
		AppendMenuW(menu, 0u, 1, "Open Settings");
		bool flag = IsPaused?.Invoke() ?? false;
		AppendMenuW(menu, 0u, 2, flag ? "Resume Kursor" : "Pause Kursor");
		AppendMenuW(menu, 2048u, 0, null);
		AppendMenuW(menu, 0u, 10, "Report a Bug…");
		AppendMenuW(menu, 0u, 11, "Request a Feature…");
		AppendMenuW(menu, 0u, 12, "What's New");
		AppendMenuW(menu, 0u, 13, "Check for Updates…");
		AppendMenuW(menu, 2048u, 0, null);
		AppendMenuW(menu, 0u, 3, "Quit Kursor");
		GetCursorPos(out var p);
		SetForegroundWindow(hwnd);
		int num = TrackPopupMenuEx(menu, 288u, p.X, p.Y, hwnd, 0);
		DestroyMenu(menu);
		switch (num)
		{
		case 1:
			this.OpenSettings?.Invoke();
			break;
		case 2:
			this.PauseToggled?.Invoke(!flag);
			break;
		case 3:
			this.Quit?.Invoke();
			break;
		case 10:
			this.ReportBug?.Invoke();
			break;
		case 11:
			this.RequestFeature?.Invoke();
			break;
		case 12:
			this.WhatsNew?.Invoke();
			break;
		case 13:
			this.CheckForUpdates?.Invoke();
			break;
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			break;
		}
	}

	private static nint MakeIcon(uint argb)
	{
		BITMAPINFOHEADER bITMAPINFOHEADER = default(BITMAPINFOHEADER);
		bITMAPINFOHEADER.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
		bITMAPINFOHEADER.biWidth = 32;
		bITMAPINFOHEADER.biHeight = -32;
		bITMAPINFOHEADER.biPlanes = 1;
		bITMAPINFOHEADER.biBitCount = 32;
		BITMAPINFOHEADER bi = bITMAPINFOHEADER;
		nint bits;
		nint num = CreateDIBSection(0, ref bi, 0u, out bits, 0, 0u);
		int[] array = new int[1024];
		double num2 = 24.0;
		double num3 = num2 * Math.Sqrt(3.0) / 2.0;
		double num4 = 16.0;
		(double, double)[] array2 = new(double, double)[3]
		{
			(0.0, (0.0 - num3) / 1.5),
			(num2 / 2.0, num3 / 3.0),
			((0.0 - num2) / 2.0, num3 / 3.0)
		};
		double num5 = Math.PI * -7.0 / 36.0;
		for (int i = 0; i < 3; i++)
		{
			var (num6, num7) = array2[i];
			array2[i] = (num4 + num6 * Math.Cos(num5) - num7 * Math.Sin(num5), num4 + num6 * Math.Sin(num5) + num7 * Math.Cos(num5));
		}
		byte b = (byte)(argb >> 16);
		byte b2 = (byte)(argb >> 8);
		byte b3 = (byte)argb;
		for (int j = 0; j < 32; j++)
		{
			for (int k = 0; k < 32; k++)
			{
				int num8 = 0;
				for (int l = 0; l < 4; l++)
				{
					for (int m = 0; m < 4; m++)
					{
						if (Inside((double)k + ((double)m + 0.5) / 4.0, (double)j + ((double)l + 0.5) / 4.0, array2))
						{
							num8++;
						}
					}
				}
				int num9 = num8 * 255 / 16;
				array[j * 32 + k] = (num9 << 24) | (b * num9 / 255 << 16) | (b2 * num9 / 255 << 8) | (b3 * num9 / 255);
			}
		}
		Marshal.Copy(array, 0, bits, array.Length);
		nint num10 = CreateBitmap(32, 32, 1u, 1u, new byte[128]);
		ICONINFO iCONINFO = default(ICONINFO);
		iCONINFO.fIcon = true;
		iCONINFO.hbmColor = num;
		iCONINFO.hbmMask = num10;
		ICONINFO info = iCONINFO;
		nint result = CreateIconIndirect(ref info);
		DeleteObject(num);
		DeleteObject(num10);
		return result;
		static bool Inside(double x, double y, (double x, double y)[] t)
		{
			double num11 = (x - t[1].x) * (t[0].y - t[1].y) - (t[0].x - t[1].x) * (y - t[1].y);
			double num12 = (x - t[2].x) * (t[1].y - t[2].y) - (t[1].x - t[2].x) * (y - t[2].y);
			double num13 = (x - t[0].x) * (t[2].y - t[0].y) - (t[2].x - t[0].x) * (y - t[0].y);
			bool flag = num11 < 0.0 || num12 < 0.0 || num13 < 0.0;
			bool flag2 = num11 > 0.0 || num12 > 0.0 || num13 > 0.0;
			return !(flag && flag2);
		}
	}

	public void SetColor(uint colorArgb)
	{
		nint num = icon;
		icon = MakeIcon(colorArgb);
		data.hIcon = icon;
		Shell_NotifyIconW(1u, ref data);
		DestroyIcon(num);
	}

	public void SetTooltip(string tooltip)
	{
		if (tooltip.Length > 127)
		{
			tooltip = tooltip.Substring(0, 126) + "…";
		}
		if (!(tooltip == data.szTip))
		{
			data.szTip = tooltip;
			Shell_NotifyIconW(1u, ref data);
			Log.Info("tray", "tooltip: " + tooltip);
		}
	}

	public void Dispose()
	{
		Shell_NotifyIconW(2u, ref data);
		DestroyIcon(icon);
		DestroyWindow(hwnd);
	}
}
