using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeyClicky.Platform.Input;

public sealed class ClickyUserInputBlocker
{
	private struct MsllHookStruct
	{
		public int X;

		public int Y;

		public uint MouseData;

		public uint Flags;

		public uint Time;

		public nuint DwExtraInfo;
	}

	private struct KbdllHookStruct
	{
		public uint VkCode;

		public uint ScanCode;

		public uint Flags;

		public uint Time;

		public nuint DwExtraInfo;
	}

	private struct Msg
	{
		public nint Hwnd;

		public uint Message;

		public nuint WParam;

		public nint LParam;

		public uint Time;

		public int PtX;

		public int PtY;

		public uint Private;
	}

	public static readonly ClickyUserInputBlocker Shared = new ClickyUserInputBlocker();

	private const int WhMouseLl = 14;

	private const int WhKeyboardLl = 13;

	private const uint WmStopHookThread = 32849u;

	private const uint VkEscape = 27u;

	private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(3L);

	private readonly object gate = new object();

	private int generation;

	private Action? onEscape;

	private Timer? safetyTimer;

	private Timer? watchdogTimer;

	private Action<Action>? postToOwner;

	private long lastHeartbeatTicks;

	private Thread? mouseHookThread;

	private uint mouseHookThreadId;

	private bool acquiredKeyboardHook;

	private nint mouseHook;

	private static volatile bool holding;

	private static long droppedMouse;

	private static long droppedKeys;

	public bool IsHolding => holding;

	public long DroppedMouseEvents => Interlocked.Read(in droppedMouse);

	public long DroppedKeyEvents => Interlocked.Read(in droppedKeys);

	internal static bool HoldsPhysicalMouse => holding;

	public void Start(double maximumSeconds, Action onEscape, Action<Action>? postToOwner = null)
	{
		Stop();
		int holdGeneration;
		lock (gate)
		{
			holdGeneration = ++generation;
			this.onEscape = onEscape;
			this.postToOwner = postToOwner;
			Interlocked.Exchange(ref droppedMouse, 0L);
			Interlocked.Exchange(ref droppedKeys, 0L);
		}
		LowLevelInputHub.Shared.Acquire();
		LowLevelInputHub.Shared.SetRawKeyboardSuspended(suspended: true);
		lock (gate)
		{
			acquiredKeyboardHook = true;
		}
		ManualResetEventSlim ready = new ManualResetEventSlim(initialState: false);
		try
		{
			Thread thread = new Thread((ThreadStart)delegate
			{
				MouseHookThreadMain(holdGeneration, ready);
			})
			{
				IsBackground = true,
				Name = "Clicky drive input hold",
				Priority = ThreadPriority.Highest
			};
			thread.Start();
			ready.Wait(1000);
			bool flag;
			lock (gate)
			{
				flag = generation == holdGeneration && mouseHook != 0;
				if (flag)
				{
					mouseHookThread = thread;
					holding = true;
					lastHeartbeatTicks = Environment.TickCount64;
					safetyTimer = new Timer(delegate
					{
						StopIfCurrent(holdGeneration, "time limit");
					}, null, TimeSpan.FromSeconds(Math.Max(1.0, maximumSeconds)), Timeout.InfiniteTimeSpan);
					if (postToOwner != null)
					{
						watchdogTimer = new Timer(delegate
						{
							Watchdog(holdGeneration);
						}, null, 500, 500);
					}
				}
			}
			if (flag)
			{
				Log.Info("InputHold", $"holding the user's mouse and keys (at most {maximumSeconds:0} s)");
			}
			else
			{
				Log.Warn("InputHold", "couldn't hold the user's input (no mouse hook), so their mouse and keys stay free");
				StopIfCurrent(holdGeneration, "no mouse hook");
			}
		}
		finally
		{
			if (ready != null)
			{
				((IDisposable)ready).Dispose();
			}
		}
	}

	public void Stop()
	{
		StopCore(null, "stopped");
	}

	private void StopIfCurrent(int holdGeneration, string reason)
	{
		StopCore(holdGeneration, reason);
	}

	private void StopCore(int? onlyGeneration, string reason)
	{
		bool flag;
		bool flag2;
		Timer timer;
		Timer timer2;
		uint num;
		lock (gate)
		{
			if (onlyGeneration.HasValue)
			{
				int valueOrDefault = onlyGeneration.GetValueOrDefault();
				if (valueOrDefault != generation)
				{
					return;
				}
			}
			flag = holding;
			flag2 = acquiredKeyboardHook;
			acquiredKeyboardHook = false;
			holding = false;
			generation++;
			timer = safetyTimer;
			timer2 = watchdogTimer;
			safetyTimer = null;
			watchdogTimer = null;
			onEscape = null;
			postToOwner = null;
			num = mouseHookThreadId;
			mouseHookThread = null;
		}
		timer?.Dispose();
		timer2?.Dispose();
		if (num != 0)
		{
			PostThreadMessageW(num, 32849u, 0u, 0);
		}
		if (flag2)
		{
			LowLevelInputHub.Shared.SetRawKeyboardSuspended(suspended: false);
			LowLevelInputHub.Shared.Release();
		}
		if (flag)
		{
			Log.Info("InputHold", $"gave the user's mouse and keys back ({reason}; held {Interlocked.Read(in droppedMouse)} mouse and {Interlocked.Read(in droppedKeys)} key events)");
		}
		ClickyScreenActuator.ReleaseStrandedKeysAfterDrive("the input hold ended");
	}

	private void Watchdog(int holdGeneration)
	{
		Action<Action> action;
		lock (gate)
		{
			if (generation != holdGeneration)
			{
				return;
			}
			action = postToOwner;
		}
		if ((double)(Environment.TickCount64 - Interlocked.Read(in lastHeartbeatTicks)) > HeartbeatTimeout.TotalMilliseconds)
		{
			Log.Warn("InputHold", "HeyClicky stopped answering while holding the user's input: giving it back");
			StopIfCurrent(holdGeneration, "watchdog");
			return;
		}
		try
		{
			action?.Invoke(delegate
			{
				Interlocked.Exchange(ref lastHeartbeatTicks, Environment.TickCount64);
			});
		}
		catch
		{
		}
	}

	private unsafe void MouseHookThreadMain(int holdGeneration, ManualResetEventSlim ready)
	{
		uint currentThreadId = GetCurrentThreadId();
		PeekMessageW(out var _, 0, 0u, 0u, 0u);
		try
		{
			string[] array = new string[2] { "MouseHookProc", "KeyboardHookProc" };
			foreach (string name in array)
			{
				RuntimeHelpers.PrepareMethod(typeof(ClickyUserInputBlocker).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);
			}
			ShouldHoldKey(new LowLevelKeyEvent(255u, 0u, 16u, 0u, isDown: false, 1212369995u));
		}
		catch
		{
		}
		nint num = SetWindowsHookExW(14, (delegate* unmanaged<int, nuint, nint, nint>)(&MouseHookProc), GetModuleHandleW(null), 0u);
		nint num2 = SetWindowsHookExW(13, (delegate* unmanaged<int, nuint, nint, nint>)(&KeyboardHookProc), GetModuleHandleW(null), 0u);
		if (num2 == 0)
		{
			Log.Warn("InputHold", $"SetWindowsHookEx(WH_KEYBOARD_LL) failed (error {Marshal.GetLastPInvokeError()}); the shared hook holds the keys");
		}
		bool flag;
		lock (gate)
		{
			flag = generation == holdGeneration && num != 0;
			if (flag)
			{
				mouseHook = num;
				mouseHookThreadId = currentThreadId;
			}
		}
		if (num == 0)
		{
			Log.Warn("InputHold", $"SetWindowsHookEx(WH_MOUSE_LL) failed (error {Marshal.GetLastPInvokeError()})");
		}
		ready.Set();
		if (!flag)
		{
			if (num != 0)
			{
				UnhookWindowsHookEx(num);
			}
			if (num2 != 0)
			{
				UnhookWindowsHookEx(num2);
			}
			return;
		}
		long tickCount = Environment.TickCount64;
		while (true)
		{
			bool flag2;
			lock (gate)
			{
				flag2 = generation == holdGeneration;
			}
			if (!flag2)
			{
				break;
			}
			if (Environment.TickCount64 - tickCount >= 1000)
			{
				tickCount = Environment.TickCount64;
				nint num3 = SetWindowsHookExW(14, (delegate* unmanaged<int, nuint, nint, nint>)(&MouseHookProc), GetModuleHandleW(null), 0u);
				nint num4 = SetWindowsHookExW(13, (delegate* unmanaged<int, nuint, nint, nint>)(&KeyboardHookProc), GetModuleHandleW(null), 0u);
				if (num3 != 0)
				{
					UnhookWindowsHookEx(num);
					num = num3;
				}
				if (num4 != 0)
				{
					if (num2 != 0)
					{
						UnhookWindowsHookEx(num2);
					}
					num2 = num4;
				}
				lock (gate)
				{
					if (generation == holdGeneration)
					{
						mouseHook = num;
					}
				}
			}
			if (MsgWaitForMultipleObjectsEx(0u, null, 250u, 1279u, 4u) != 0)
			{
				continue;
			}
			bool flag3 = false;
			Msg message2;
			while (PeekMessageW(out message2, 0, 0u, 0u, 1u))
			{
				if (message2.Message == 32849)
				{
					flag3 = true;
					break;
				}
				TranslateMessage(in message2);
				DispatchMessageW(in message2);
			}
			if (flag3)
			{
				break;
			}
		}
		UnhookWindowsHookEx(num);
		if (num2 != 0)
		{
			UnhookWindowsHookEx(num2);
		}
		lock (gate)
		{
			if (mouseHook == num)
			{
				mouseHook = 0;
				mouseHookThreadId = 0u;
			}
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static nint KeyboardHookProc(int code, nuint wParam, nint lParam)
	{
		if (code >= 0 && holding)
		{
			uint num = (uint)wParam;
			bool flag = ((num == 256 || num == 260) ? true : false);
			bool isDown = flag;
			if (ShouldHoldKey(new LowLevelKeyEvent(((KbdllHookStruct*)lParam)->VkCode, ((KbdllHookStruct*)lParam)->ScanCode, ((KbdllHookStruct*)lParam)->Flags, ((KbdllHookStruct*)lParam)->Time, isDown, ((KbdllHookStruct*)lParam)->DwExtraInfo)))
			{
				return 1;
			}
		}
		return CallNextHookEx(0, code, wParam, lParam);
	}

	[UnmanagedCallersOnly]
	private unsafe static nint MouseHookProc(int code, nuint wParam, nint lParam)
	{
		if (code >= 0 && holding)
		{
			bool flag;
			switch ((uint)wParam)
			{
			case 514u:
			case 517u:
			case 520u:
			case 524u:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool flag2 = flag;
			if (((MsllHookStruct*)lParam)->DwExtraInfo != 1212369995 && !flag2)
			{
				Interlocked.Increment(ref droppedMouse);
				return 1;
			}
		}
		return CallNextHookEx(0, code, wParam, lParam);
	}

	internal static bool ShouldHoldKey(in LowLevelKeyEvent keyEvent)
	{
		if (!holding || keyEvent.ExtraInfo == 1212369995 || !keyEvent.IsDown)
		{
			return false;
		}
		if (keyEvent.VirtualKey == 27)
		{
			Shared.EscapePressed();
			return false;
		}
		Interlocked.Increment(ref droppedKeys);
		return true;
	}

	private void EscapePressed()
	{
		Action handler;
		lock (gate)
		{
			handler = onEscape;
			onEscape = null;
		}
		if (handler == null)
		{
			return;
		}
		ThreadPool.UnsafeQueueUserWorkItem(delegate
		{
			Stop();
			try
			{
				handler();
			}
			catch (Exception ex)
			{
				Log.Warn("InputHold", "Escape handler failed: " + ex.Message);
			}
		}, null);
	}

	[DllImport("user32.dll", SetLastError = true)]
	private unsafe static extern nint SetWindowsHookExW(int idHook, delegate* unmanaged<int, nuint, nint, nint> lpfn, nint hmod, uint threadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(nint hook);

	[DllImport("user32.dll")]
	private static extern nint CallNextHookEx(nint hook, int code, nuint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint GetModuleHandleW(string? name);

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	private static extern bool PostThreadMessageW(uint threadId, uint message, nuint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool PeekMessageW(out Msg message, nint hwnd, uint filterMin, uint filterMax, uint remove);

	[DllImport("user32.dll")]
	private static extern bool TranslateMessage(in Msg message);

	[DllImport("user32.dll")]
	private static extern nint DispatchMessageW(in Msg message);

	[DllImport("user32.dll")]
	private unsafe static extern uint MsgWaitForMultipleObjectsEx(uint count, nint* handles, uint milliseconds, uint wakeMask, uint flags);
}
