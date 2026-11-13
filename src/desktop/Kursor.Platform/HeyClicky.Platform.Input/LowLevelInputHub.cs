using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Input;

internal sealed class LowLevelInputHub
{
	internal enum InstallReason
	{
		HookThreadStarted,
		MissedKeys,
		Watchdog,
		Requested
	}

	public static readonly LowLevelInputHub Shared = new LowLevelInputHub();

	public const nuint HeyClickyInjectedInputSignature = 1212369995u;

	private const uint WM_REINSTALL_HOOKS = 32769u;

	private const uint WM_STOP_HOOK_THREAD = 32770u;

	private const uint WM_RESYNC_SINKS = 32771u;

	private const uint WM_RAW_KEYBOARD = 32772u;

	private volatile bool rawKeyboardSuspended;

	private const uint WM_INPUT = 255u;

	private nuint checkTimerId;

	private int aliveCheckQueued;

	private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(3L);

	private readonly object lifecycleGate = new object();

	private int referenceCount;

	private Thread? hookThread;

	private uint hookThreadId;

	private nint keyboardHook;

	private nint rawInputWindow;

	private Timer? watchdogTimer;

	private readonly ManualResetEventSlim hookThreadReady = new ManualResetEventSlim(initialState: false);

	private ILowLevelInputSink?[] sinks = Array.Empty<ILowLevelInputSink>();

	private long hookKeyCount;

	private long rawKeyCount;

	private long hookKeyCountAtProbe;

	private long rawKeyCountAtProbe;

	private int maxHookLatencyMs;

	private TimeSpan gcPauseAtProbe;

	private int lastReinstallLogTick = Environment.TickCount - 120000;

	public long MouseMoveEventCount;

	private int acquiresAndReleases;

	private EventWaitHandle? mouseMoveWaker;

	private int mouseMoveWakerArmed;

	private const int RawHeaderSize = 24;

	private readonly byte[] rawBuffer = GC.AllocateUninitializedArray<byte>(1024, pinned: true);

	private long rawBaseline;

	private long hookBaseline;

	private int lastInlineReinstallTick = Environment.TickCount - 10000;

	private bool hookDead;

	private readonly LowLevelKeyEvent[] recentRawKeys = new LowLevelKeyEvent[32];

	private int recentRawKeyCount;

	public int ReinstallCount { get; private set; }

	public long HookKeyCount => Interlocked.Read(in hookKeyCount);

	public bool IsRunning => hookThread != null;

	public void AddSink(ILowLevelInputSink sink)
	{
		lock (lifecycleGate)
		{
			ILowLevelInputSink[] array = sinks;
			int num = 0;
			ILowLevelInputSink[] array2 = new ILowLevelInputSink[1 + array.Length];
			ReadOnlySpan<ILowLevelInputSink> readOnlySpan = new ReadOnlySpan<ILowLevelInputSink>(array);
			readOnlySpan.CopyTo(new Span<ILowLevelInputSink>(array2).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			array2[num] = sink;
			sinks = array2;
		}
	}

	public void RemoveSink(ILowLevelInputSink sink)
	{
		lock (lifecycleGate)
		{
			sinks = Array.FindAll(sinks, (ILowLevelInputSink existing) => existing != sink);
		}
	}

	public void SetMouseMoveWaker(EventWaitHandle? waker)
	{
		mouseMoveWaker = waker;
	}

	public void ArmMouseMoveWaker()
	{
		Volatile.Write(ref mouseMoveWakerArmed, 1);
	}

	public void Acquire()
	{
		lock (lifecycleGate)
		{
			referenceCount++;
			Interlocked.Increment(ref acquiresAndReleases);
			Log.Info("InputHooks", $"acquired by {new StackTrace(1, fNeedFileInfo: false).GetFrame(0)?.GetMethod()?.DeclaringType?.Name} on thread {Environment.CurrentManagedThreadId} (refs {referenceCount}, hook thread {((hookThread == null) ? "starting" : "running")})");
			if (hookThread == null)
			{
				hookThreadReady.Reset();
				hookThread = new Thread(HookThreadMain)
				{
					IsBackground = true,
					Name = "HeyClicky input hooks",
					Priority = ThreadPriority.Highest
				};
				hookThread.Start();
				hookThreadReady.Wait(2000);
				watchdogTimer = new Timer(delegate
				{
					WatchdogProbe();
				}, null, WatchdogInterval, WatchdogInterval);
			}
		}
	}

	public void Release()
	{
		lock (lifecycleGate)
		{
			if (referenceCount > 0)
			{
				referenceCount--;
			}
			Interlocked.Increment(ref acquiresAndReleases);
		}
	}

	public void RequestResync()
	{
		uint num = hookThreadId;
		if (num != 0)
		{
			PInvoke.PostThreadMessage(num, 32771u, default(WPARAM), default(LPARAM));
		}
	}

	public void RequestReinstall(InstallReason reason = InstallReason.Requested)
	{
		uint num = hookThreadId;
		if (num != 0)
		{
			PInvoke.PostThreadMessage(num, 32769u, (nuint)reason, default(LPARAM));
		}
	}

	private void HookThreadMain()
	{
		hookThreadId = PInvoke.GetCurrentThreadId();
		PInvoke.PeekMessage(out var _, default(HWND), 0u, 0u, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
		WarmUpSinks();
		InstallHooks(InstallReason.HookThreadStarted);
		StartRawInput();
		hookThreadReady.Set();
		MSG lpMsg2;
		while ((bool)PInvoke.GetMessage(out lpMsg2, default(HWND), 0u, 0u))
		{
			if (lpMsg2.message == 255)
			{
				ReadRawInput((nint)lpMsg2.lParam);
			}
			if (lpMsg2.message == 32770)
			{
				break;
			}
			if (lpMsg2.message == 32771)
			{
				ILowLevelInputSink[] array = sinks;
				for (int i = 0; i < array.Length; i++)
				{
					array[i]?.OnResyncRequested();
				}
			}
			else if (lpMsg2.message == 32772)
			{
				ApplyRawKeyboard();
			}
			else if (lpMsg2.message == 275 && lpMsg2.wParam == checkTimerId && checkTimerId != 0)
			{
				NativeHooks.KillTimer(0, checkTimerId);
				checkTimerId = 0u;
				Volatile.Write(ref aliveCheckQueued, 0);
				CheckHookAlive();
			}
			else if (lpMsg2.message == 32769)
			{
				UninstallHooks();
				InstallHooks((InstallReason)lpMsg2.wParam.Value);
				ReinstallCount++;
				rawBaseline = Interlocked.Read(in rawKeyCount);
				hookBaseline = Interlocked.Read(in hookKeyCount);
			}
			else
			{
				PInvoke.TranslateMessage(in lpMsg2);
				PInvoke.DispatchMessage(in lpMsg2);
			}
		}
		StopRawInput();
		UninstallHooks();
	}

	private void WarmUpSinks()
	{
		LowLevelKeyEvent keyEvent = new LowLevelKeyEvent(255u, 0u, 16u, (uint)Environment.TickCount, isDown: false, 1212369995u);
		try
		{
			RuntimeHelpers.PrepareMethod(typeof(LowLevelInputHub).GetMethod("KeyboardHookProc", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);
			ClickyUserInputBlocker.ShouldHoldKey(in keyEvent);
			ClickyScreenActuator.NoteOthersKey(in keyEvent);
		}
		catch
		{
		}
		ILowLevelInputSink[] array = sinks;
		foreach (ILowLevelInputSink lowLevelInputSink in array)
		{
			try
			{
				lowLevelInputSink?.OnKey(in keyEvent);
				lowLevelInputSink?.ShouldSwallowKey(in keyEvent);
			}
			catch
			{
			}
		}
	}

	private unsafe void InstallHooks(InstallReason reason, string? detail = null)
	{
		nint moduleHandleW = NativeHooks.GetModuleHandleW(null);
		keyboardHook = NativeHooks.SetWindowsHookExW(13, (delegate* unmanaged<int, nuint, nint, nint>)(&KeyboardHookProc), moduleHandleW, 0u);
		if (keyboardHook == 0)
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			Log.Error("InputHooks", $"SetWindowsHookEx failed (error {lastPInvokeError}; {WhyInstalled(reason, detail)})");
		}
		else
		{
			Log.Info("InputHooks", "low-level keyboard hook installed (" + WhyInstalled(reason, detail) + ")");
		}
	}

	private string WhyInstalled(InstallReason reason, string? detail)
	{
		string value = reason switch
		{
			InstallReason.HookThreadStarted => "the hook thread started", 
			InstallReason.MissedKeys => "missed-key check", 
			InstallReason.Watchdog => "watchdog timeout: no key reached the hook in 3 s while Raw Input saw some", 
			_ => "asked for", 
		};
		int value2 = Interlocked.Exchange(ref acquiresAndReleases, 0);
		bool flag = ForegroundIntegrity.IsAboveOurs();
		return $"{value}{((detail == null) ? "" : (": " + detail))}; {value2} acquire/release since the last install{(flag ? "; the window in front runs elevated" : "")}";
	}

	private void UninstallHooks()
	{
		if (keyboardHook != 0)
		{
			NativeHooks.UnhookWindowsHookEx(keyboardHook);
			keyboardHook = 0;
		}
	}

	private unsafe void StartRawInput()
	{
		rawInputWindow = NativeHooks.CreateWindowExW(0u, "Message", "HeyClicky raw input", 0u, 0, 0, 0, 0, -3, 0, 0, 0);
		if (rawInputWindow == 0)
		{
			Log.Error("InputHooks", $"raw input window failed (error {Marshal.GetLastPInvokeError()})");
			return;
		}
		if (!NativeHooks.RegisterRawInputDevices(stackalloc RawInputDevice[2]
		{
			new RawInputDevice
			{
				UsagePage = 1,
				Usage = 2,
				Flags = 256u,
				Target = rawInputWindow
			},
			new RawInputDevice
			{
				UsagePage = 1,
				Usage = 6,
				Flags = 256u,
				Target = rawInputWindow
			}
		}, 2u, (uint)sizeof(RawInputDevice)))
		{
			Log.Error("InputHooks", $"RegisterRawInputDevices failed (error {Marshal.GetLastPInvokeError()})");
		}
		else
		{
			Log.Info("InputHooks", "raw mouse + keyboard input registered");
		}
		if (rawKeyboardSuspended)
		{
			ApplyRawKeyboard();
		}
		if (!NativeHooks.RegisterRawInputDevices(stackalloc RawInputDevice[1]
		{
			new RawInputDevice
			{
				UsagePage = 13,
				Usage = 5,
				Flags = 256u,
				Target = rawInputWindow
			}
		}, 1u, (uint)sizeof(RawInputDevice)))
		{
			Log.Warn("InputHooks", $"raw touchpad input failed (error {Marshal.GetLastPInvokeError()})");
		}
		else
		{
			PrecisionTouchpad.ReadDevicesNow();
		}
	}

	public void SetRawKeyboardSuspended(bool suspended)
	{
		rawKeyboardSuspended = suspended;
		uint num = hookThreadId;
		if (num != 0)
		{
			PInvoke.PostThreadMessage(num, 32772u, default(WPARAM), default(LPARAM));
		}
	}

	private unsafe void ApplyRawKeyboard()
	{
		if (rawInputWindow != 0)
		{
			if (!NativeHooks.RegisterRawInputDevices(stackalloc RawInputDevice[1] { rawKeyboardSuspended ? new RawInputDevice
			{
				UsagePage = 1,
				Usage = 6,
				Flags = 1u
			} : new RawInputDevice
			{
				UsagePage = 1,
				Usage = 6,
				Flags = 256u,
				Target = rawInputWindow
			} }, 1u, (uint)sizeof(RawInputDevice)))
			{
				Log.Warn("InputHooks", $"raw keyboard {(rawKeyboardSuspended ? "off" : "on")} failed (error {Marshal.GetLastPInvokeError()})");
			}
			rawBaseline = Interlocked.Read(in rawKeyCount);
			hookBaseline = Interlocked.Read(in hookKeyCount);
			rawKeyCountAtProbe = rawBaseline;
			hookKeyCountAtProbe = hookBaseline;
		}
	}

	private unsafe void StopRawInput()
	{
		if (rawInputWindow != 0)
		{
			NativeHooks.RegisterRawInputDevices(stackalloc RawInputDevice[3]
			{
				new RawInputDevice
				{
					UsagePage = 1,
					Usage = 2,
					Flags = 1u
				},
				new RawInputDevice
				{
					UsagePage = 1,
					Usage = 6,
					Flags = 1u
				},
				new RawInputDevice
				{
					UsagePage = 13,
					Usage = 5,
					Flags = 1u
				}
			}, 3u, (uint)sizeof(RawInputDevice));
			NativeHooks.DestroyWindow(rawInputWindow);
			rawInputWindow = 0;
		}
	}

	private unsafe void ReadRawInput(nint rawInputHandle)
	{
		uint size = (uint)rawBuffer.Length;
		ushort num;
		fixed (byte* ptr = rawBuffer)
		{
			if (NativeHooks.GetRawInputData(rawInputHandle, 268435459u, ptr, ref size, 24u) == uint.MaxValue)
			{
				return;
			}
			switch (*(uint*)ptr)
			{
			case 2u:
				PrecisionTouchpad.Read(*(nint*)(ptr + 8), ptr + 24, size - 24);
				return;
			case 1u:
			{
				if (*(uint*)(ptr + 24 + 12) == 1212369995)
				{
					return;
				}
				Interlocked.Increment(ref rawKeyCount);
				LowLevelKeyEvent? lowLevelKeyEvent = BuildRawKey(ptr + 24, *(nint*)(ptr + 8));
				if (lowLevelKeyEvent.HasValue)
				{
					LowLevelKeyEvent keyEvent = lowLevelKeyEvent.GetValueOrDefault();
					if (Volatile.Read(in hookDead))
					{
						if (!ClickyUserInputBlocker.ShouldHoldKey(in keyEvent))
						{
							DeliverKey(in keyEvent);
						}
					}
					else
					{
						recentRawKeys[recentRawKeyCount % recentRawKeys.Length] = keyEvent;
						recentRawKeyCount++;
					}
				}
				if (Interlocked.Exchange(ref aliveCheckQueued, 1) == 0)
				{
					checkTimerId = NativeHooks.SetTimer(0, 0u, 80u, 0);
				}
				return;
			}
			default:
				return;
			case 0u:
			{
				num = *(ushort*)(ptr + 24 + 4);
				bool flag = *(IntPtr*)(ptr + 8) != (IntPtr)0;
				nuint extraInfo = *(uint*)(ptr + 24 + 20);
				ILowLevelInputSink[] array = sinks;
				for (int i = 0; i < array.Length; i++)
				{
					try
					{
						array[i].OnRawMouse(num, flag, extraInfo);
					}
					catch
					{
					}
				}
				if ((num != 0 && flag && ClickyUserInputBlocker.HoldsPhysicalMouse) || (num != 0 && *(uint*)(ptr + 24 + 20) == 1212369995))
				{
					return;
				}
				break;
			}
			}
		}
		if (num == 0)
		{
			MouseMoveEventCount++;
			if (Interlocked.Exchange(ref mouseMoveWakerArmed, 0) == 1)
			{
				mouseMoveWaker?.Set();
			}
		}
		else
		{
			DeliverMouseButton(num, 1, LowLevelMouseButtonKind.Left);
			DeliverMouseButton(num, 4, LowLevelMouseButtonKind.Right);
			DeliverMouseButton(num, 16, LowLevelMouseButtonKind.Middle);
			DeliverMouseButton(num, 320, LowLevelMouseButtonKind.XButton);
			DeliverMouseButton(num, 3072, LowLevelMouseButtonKind.Wheel);
		}
	}

	private void DeliverKey(in LowLevelKeyEvent keyEvent)
	{
		ClickyScreenActuator.NoteOthersKey(in keyEvent);
		ILowLevelInputSink[] array = sinks;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				array[i].OnKey(in keyEvent);
			}
			catch
			{
			}
		}
	}

	private unsafe static LowLevelKeyEvent? BuildRawKey(byte* keyboard, nint device)
	{
		ushort num = *(ushort*)keyboard;
		ushort num2 = *(ushort*)(keyboard + 2);
		ushort num3 = *(ushort*)(keyboard + 6);
		uint num4 = *(uint*)(keyboard + 12);
		if ((num3 == 0 || num3 == 255) ? true : false)
		{
			return null;
		}
		bool flag = (num2 & 2) != 0;
		int virtualKey = num3 switch
		{
			17 => flag ? 163 : 162, 
			18 => flag ? 165 : 164, 
			16 => (num == 54) ? 161 : 160, 
			_ => num3, 
		};
		bool flag2 = (num2 & 1) == 0;
		uint flags = (flag ? 1u : 0u) | (uint)((device == 0) ? 16 : 0) | (uint)((!flag2) ? 128 : 0);
		return new LowLevelKeyEvent((uint)virtualKey, num, flags, (uint)PInvoke.GetMessageTime(), flag2, num4);
	}

	private void CheckHookAlive()
	{
		long num = Interlocked.Read(in rawKeyCount);
		long num2 = Interlocked.Read(in hookKeyCount);
		long num3 = num - rawBaseline;
		long num4 = num2 - hookBaseline;
		rawBaseline = num;
		hookBaseline = num2;
		if (num3 == 0L || num4 > 0 || ForegroundIntegrity.IsForegroundElevated())
		{
			return;
		}
		if (!Volatile.Read(in hookDead))
		{
			Volatile.Write(ref hookDead, value: true);
			int num5 = (int)Math.Min(num3, Math.Min(recentRawKeys.Length, recentRawKeyCount));
			for (int i = recentRawKeyCount - num5; i < recentRawKeyCount; i++)
			{
				ref LowLevelKeyEvent keyEvent = ref recentRawKeys[i % recentRawKeys.Length];
				if (!ClickyUserInputBlocker.ShouldHoldKey(in keyEvent))
				{
					DeliverKey(in keyEvent);
				}
			}
		}
		if (Environment.TickCount - lastInlineReinstallTick >= 1000)
		{
			lastInlineReinstallTick = Environment.TickCount;
			UninstallHooks();
			InstallHooks(InstallReason.MissedKeys, $"the hook missed {num3} key(s) since the last check, and Raw Input stands in");
			ReinstallCount++;
		}
	}

	private void DeliverMouseButton(ushort buttonFlags, int mask, LowLevelMouseButtonKind kind)
	{
		if ((buttonFlags & mask) == 0)
		{
			return;
		}
		PInvoke.GetCursorPos(out var lpPoint);
		uint messageTime = (uint)PInvoke.GetMessageTime();
		ILowLevelInputSink[] array = sinks;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				array[i].OnMouseButton(kind, lpPoint.X, lpPoint.Y, messageTime);
			}
			catch
			{
			}
		}
	}

	private void WatchdogProbe()
	{
		long num = Interlocked.Read(in rawKeyCount);
		long num2 = Interlocked.Read(in hookKeyCount);
		bool flag = num > rawKeyCountAtProbe && num2 == hookKeyCountAtProbe && !ForegroundIntegrity.IsForegroundElevated();
		rawKeyCountAtProbe = num;
		hookKeyCountAtProbe = num2;
		int num3 = Interlocked.Exchange(ref maxHookLatencyMs, 0);
		TimeSpan totalPauseDuration = GC.GetTotalPauseDuration();
		TimeSpan timeSpan = totalPauseDuration - gcPauseAtProbe;
		gcPauseAtProbe = totalPauseDuration;
		if (num3 >= 150 || flag)
		{
			Log.Info("InputHooks", $"hook timing: slowest key callback {num3} ms after the key, GC paused {timeSpan.TotalMilliseconds:0} ms in the last 3 s (gen2 {GC.CollectionCount(2)}){(flag ? ", hook dropped" : "")}");
		}
		if (flag)
		{
			if (Environment.TickCount - lastReinstallLogTick > 60000)
			{
				lastReinstallLogTick = Environment.TickCount;
				Log.Warn("InputHooks", $"keyboard hook looks removed (raw input saw {num} keys, the hook {num2}); reinstalling");
			}
			RequestReinstall(InstallReason.Watchdog);
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static nint KeyboardHookProc(int code, nuint wParam, nint lParam)
	{
		if (code >= 0)
		{
			LowLevelInputHub shared = Shared;
			if (Interlocked.Increment(ref shared.hookKeyCount) == 1)
			{
				Log.Info("InputHooks", $"first key through the hook (vk {((KBDLLHOOKSTRUCT*)lParam)->vkCode})");
			}
			if (shared.hookDead)
			{
				shared.hookDead = false;
			}
			int num = Environment.TickCount - (int)((KBDLLHOOKSTRUCT*)lParam)->time;
			if (num > shared.maxHookLatencyMs)
			{
				shared.maxHookLatencyMs = num;
			}
			uint num2 = (uint)wParam;
			bool isDown = num2 == 256 || num2 == 260;
			LowLevelKeyEvent keyEvent = new LowLevelKeyEvent(((KBDLLHOOKSTRUCT*)lParam)->vkCode, ((KBDLLHOOKSTRUCT*)lParam)->scanCode, (uint)((KBDLLHOOKSTRUCT*)lParam)->flags, ((KBDLLHOOKSTRUCT*)lParam)->time, isDown, ((KBDLLHOOKSTRUCT*)lParam)->dwExtraInfo);
			if (ClickyUserInputBlocker.ShouldHoldKey(in keyEvent))
			{
				return 1;
			}
			ClickyScreenActuator.NoteOthersKey(in keyEvent);
			ILowLevelInputSink[] array = shared.sinks;
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					array[i].OnKey(in keyEvent);
					flag |= array[i].ShouldSwallowKey(in keyEvent);
				}
				catch
				{
				}
			}
			if (flag)
			{
				return 1;
			}
		}
		return NativeHooks.CallNextHookEx(0, code, wParam, lParam);
	}
}
