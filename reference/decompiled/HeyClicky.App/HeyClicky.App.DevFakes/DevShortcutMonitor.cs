using System;
using System.Runtime.InteropServices;
using HeyClicky.App.Shell;

namespace HeyClicky.App.DevFakes;

public sealed class DevShortcutMonitor : IShortcutMonitor, IDisposable
{
	private delegate nint HookProc(int code, nint wParam, nint lParam);

	private struct KBDLLHOOKSTRUCT
	{
		public uint vkCode;

		public uint scanCode;

		public uint flags;

		public uint time;

		public nuint extra;
	}

	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 256;

	private const int WM_KEYUP = 257;

	private const int WM_SYSKEYDOWN = 260;

	private const int WM_SYSKEYUP = 261;

	private const uint VK_LCONTROL = 162u;

	private const uint VK_RCONTROL = 163u;

	private const uint VK_LMENU = 164u;

	private const uint VK_RMENU = 165u;

	private const uint VK_ESCAPE = 27u;

	private readonly HookProc proc;

	private nint hook;

	private bool lctrl;

	private bool rctrl;

	private bool lalt;

	private bool ralt;

	private bool ptt;

	private bool otherKeySinceCtrl;

	private long lastCtrlTap;

	private const int DoubleTapMs = 350;

	public bool IsPushToTalkHeld => ptt;

	public event EventHandler<ShortcutAction>? ShortcutTriggered;

	[DllImport("user32.dll")]
	private static extern nint SetWindowsHookExW(int id, HookProc proc, nint mod, uint thread);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(nint hook);

	[DllImport("user32.dll")]
	private static extern nint CallNextHookEx(nint hook, int code, nint w, nint l);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandleW(string? name);

	public DevShortcutMonitor()
	{
		proc = Hook;
	}

	public void Start()
	{
		if (hook == 0)
		{
			if (OverlayWindow.Offscreen || OverlayWindow.OnscreenBesideReal)
			{
				Log.Info("hotkeys", "offscreen run: dev keyboard hook skipped");
				return;
			}
			hook = SetWindowsHookExW(13, proc, GetModuleHandleW(null), 0u);
			Log.Info("hotkeys", (hook != 0) ? "dev keyboard hook installed" : "dev keyboard hook failed");
		}
	}

	public void Stop()
	{
		if (hook != 0)
		{
			UnhookWindowsHookEx(hook);
		}
		hook = 0;
	}

	private nint Hook(int code, nint wParam, nint lParam)
	{
		if (code >= 0)
		{
			KBDLLHOOKSTRUCT kBDLLHOOKSTRUCT = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
			bool flag = wParam == 256 || wParam == 260;
			bool flag2 = wParam == 257 || wParam == 261;
			bool flag3 = kBDLLHOOKSTRUCT.vkCode == 162 && (kBDLLHOOKSTRUCT.scanCode & 0x200) != 0;
			switch (kBDLLHOOKSTRUCT.vkCode)
			{
			case 162u:
				if (!flag3)
				{
					OnCtrl(ref lctrl, flag, flag2);
					break;
				}
				goto default;
			case 163u:
				OnCtrl(ref rctrl, flag, flag2);
				break;
			case 164u:
				if (flag)
				{
					lalt = true;
				}
				if (flag2)
				{
					lalt = false;
				}
				otherKeySinceCtrl = true;
				break;
			case 165u:
				if (flag)
				{
					ralt = true;
				}
				if (flag2)
				{
					ralt = false;
				}
				otherKeySinceCtrl = true;
				break;
			case 27u:
				if (flag)
				{
					Raise(ShortcutAction.Escape);
				}
				otherKeySinceCtrl = true;
				break;
			default:
				if (flag)
				{
					otherKeySinceCtrl = true;
				}
				break;
			}
			bool flag4 = (lalt && (lctrl || rctrl)) || (rctrl && ralt);
			if (flag4 && !ptt)
			{
				ptt = true;
				Raise(ShortcutAction.PushToTalkDown);
			}
			else if (!flag4 && ptt)
			{
				ptt = false;
				Raise(ShortcutAction.PushToTalkUp);
			}
		}
		return CallNextHookEx(hook, code, wParam, lParam);
	}

	private void OnCtrl(ref bool state, bool down, bool up)
	{
		if (down && !state)
		{
			state = true;
			otherKeySinceCtrl = false;
		}
		else
		{
			if (!(up & state))
			{
				return;
			}
			state = false;
			if (otherKeySinceCtrl || ptt)
			{
				lastCtrlTap = 0L;
				return;
			}
			long tickCount = Environment.TickCount64;
			if (lastCtrlTap != 0L && tickCount - lastCtrlTap <= 350)
			{
				lastCtrlTap = 0L;
				Raise(ShortcutAction.TextComposerToggle);
			}
			else
			{
				lastCtrlTap = tickCount;
			}
		}
	}

	private void Raise(ShortcutAction a)
	{
		try
		{
			this.ShortcutTriggered?.Invoke(this, a);
		}
		catch (Exception ex)
		{
			Log.Error("hotkeys", $"handler for {a}", ex);
		}
	}

	public void Dispose()
	{
		Stop();
	}
}
