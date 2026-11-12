using System;
using System.Threading;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace HeyClicky.Platform.Input;

public sealed class ShortcutMonitor : IShortcutMonitor, IDisposable, ILowLevelInputSink
{
	[Flags]
	private enum ModifierSet : byte
	{
		None = 0,
		Control = 1,
		Alt = 2,
		Shift = 4,
		Win = 8
	}

	private const uint ControlTapMaxHoldMs = 400u;

	private const uint ControlTapMaxIntervalMs = 500u;

	private const uint VK_ESCAPE = 27u;

	private const uint VK_LSHIFT = 160u;

	private const uint VK_RSHIFT = 161u;

	private const uint VK_LCONTROL = 162u;

	private const uint VK_RCONTROL = 163u;

	private const uint VK_LMENU = 164u;

	private const uint VK_RMENU = 165u;

	private const uint VK_LWIN = 91u;

	private const uint VK_RWIN = 92u;

	private const uint VK_SHIFT = 16u;

	private const uint VK_CONTROL = 17u;

	private const uint VK_MENU = 18u;

	private const uint VK_F11 = 122u;

	private const uint VK_F12 = 123u;

	private const uint VK_S = 83u;

	private const uint VK_SNAPSHOT = 44u;

	private const uint VK_LEFT = 37u;

	private const uint VK_RIGHT = 39u;

	public const ushort StartMenuMaskVirtualKey = 255;

	private bool leftControlDown;

	private bool rightControlDown;

	private bool leftAltDown;

	private bool rightAltDown;

	private bool rightAltIsAltGr;

	private bool leftShiftDown;

	private bool rightShiftDown;

	private bool leftWinDown;

	private bool rightWinDown;

	private bool fakeAltGrControlDown;

	private bool leftControlBelongsToAltGr;

	private uint lastRealLeftControlDownTime;

	private bool leftControlWentDownWithLastEvent;

	private ModifierSet previousModifiers;

	private volatile bool isPushToTalkHeld;

	private volatile bool isDictationHeld;

	private bool dictationHoldCancelled;

	private uint keyDownCounter;

	private uint wheelCounter;

	private bool controlTapPending;

	private uint controlTapPressStartTime;

	private uint controlTapPressKeyDownSnapshot;

	private uint controlTapPressWheelSnapshot;

	private ModifierSet pressSessionUnion;

	private int completedControlTapCount;

	private ModifierSet completedTapMask;

	private uint lastCompletedControlTapReleaseTime;

	private volatile HotkeyBindings bindings = HotkeyBindings.Defaults;

	private volatile bool isRecording;

	private const int RingSize = 64;

	private const byte EntryAction = 0;

	private const byte EntryClick = 1;

	private const byte EntryRecordModifiers = 2;

	private const byte EntryRecordKey = 3;

	private const byte EntryRightClick = 4;

	private readonly ShortcutAction[] actionRing = new ShortcutAction[64];

	private readonly PointD[] clickRing = new PointD[64];

	private readonly byte[] ringEntryKind = new byte[64];

	private readonly HotkeyModifiers[] modifierRing = new HotkeyModifiers[64];

	private readonly uint[] timeRing = new uint[64];

	private int ringWriteIndex;

	private int ringReadIndex;

	private readonly AutoResetEvent dispatchSignal = new AutoResetEvent(initialState: false);

	private Thread? dispatcherThread;

	private Timer? holdWatchdog;

	private volatile bool running;

	private bool withoutHooks;

	private bool escapeSwallowed;

	private uint arrowSwallowed;

	internal Func<bool>? LayoutHasAltGrOverride;

	internal Func<uint, bool>? PhysicalKeyStateOverride;

	internal Action? StartMenuMaskOverride;

	private int startMenuMaskCount;

	public bool IsPushToTalkHeld => isPushToTalkHeld;

	public bool IsDictationHeld => isDictationHeld;

	public HotkeyBindings Bindings => bindings;

	public bool IsRecording
	{
		get
		{
			return isRecording;
		}
		set
		{
			if (isRecording != value)
			{
				isRecording = value;
				Log.Info("Shortcuts", value ? "recording a shortcut" : "recording ended");
			}
		}
	}

	public static int HookReinstallCount => LowLevelInputHub.Shared.ReinstallCount;

	public Func<bool>? ConsumesEscape { get; set; }

	public Func<int, bool>? ConsumesArrow { get; set; }

	public int StartMenuMaskCount => Volatile.Read(in startMenuMaskCount);

	public event EventHandler<ShortcutAction>? ShortcutTriggered;

	public event Action<PointD>? LeftMouseDown;

	public event Action<PointD>? MouseDown;

	public event Action<HotkeyModifiers, uint>? RecordingModifiersChanged;

	public event Action? RecordingKeyPressed;

	public void Configure(HotkeyBindings newBindings)
	{
		bindings = newBindings with
		{
			PushToTalk = newBindings.PushToTalk with
			{
				Trigger = HotkeyTrigger.Hold
			},
			Dictation = newBindings.Dictation with
			{
				Trigger = HotkeyTrigger.Hold
			}
		};
		Log.Info("Shortcuts", $"bindings: talk {bindings.PushToTalk.DisplayText}; text {bindings.TextMode.DisplayText}; dictate {bindings.Dictation.DisplayText}; hands-free {bindings.HandsFreeDictation.DisplayText}");
	}

	public void Start()
	{
		if (!running)
		{
			running = true;
			ResetAllState();
			dispatcherThread = new Thread(DispatcherMain)
			{
				IsBackground = true,
				Name = "HeyClicky shortcut dispatch",
				Priority = ThreadPriority.AboveNormal
			};
			dispatcherThread.Start();
			holdWatchdog = new Timer(delegate
			{
				LowLevelInputHub.Shared.RequestResync();
			}, null, -1, -1);
			LowLevelInputHub.Shared.AddSink(this);
			LowLevelInputHub.Shared.Acquire();
			Log.Info("Shortcuts", $"monitor started (talk {bindings.PushToTalk.DisplayText}; text {bindings.TextMode.DisplayText}; dictate {bindings.Dictation.DisplayText}; hands-free {bindings.HandsFreeDictation.DisplayText})");
		}
	}

	internal void StartWithoutHooks()
	{
		if (!running)
		{
			running = true;
			ResetAllState();
			dispatcherThread = new Thread(DispatcherMain)
			{
				IsBackground = true,
				Name = "HeyClicky shortcut dispatch (test)"
			};
			dispatcherThread.Start();
			holdWatchdog = new Timer(delegate
			{
			}, null, -1, -1);
			withoutHooks = true;
		}
	}

	public void Stop()
	{
		if (running)
		{
			running = false;
			if (!withoutHooks)
			{
				LowLevelInputHub.Shared.RemoveSink(this);
				LowLevelInputHub.Shared.Release();
			}
			withoutHooks = false;
			holdWatchdog?.Dispose();
			holdWatchdog = null;
			dispatchSignal.Set();
			dispatcherThread?.Join(1000);
			dispatcherThread = null;
			if (isPushToTalkHeld)
			{
				isPushToTalkHeld = false;
				this.ShortcutTriggered?.Invoke(this, ShortcutAction.PushToTalkUp);
			}
			if (isDictationHeld)
			{
				isDictationHeld = false;
				this.ShortcutTriggered?.Invoke(this, ShortcutAction.DictationUp);
			}
		}
	}

	public void Dispose()
	{
		Stop();
		dispatchSignal.Dispose();
	}

	public static void ForceHookReinstall()
	{
		LowLevelInputHub.Shared.RequestReinstall();
	}

	private void ResetAllState()
	{
		leftControlDown = (rightControlDown = (leftAltDown = (rightAltDown = (rightAltIsAltGr = false))));
		leftShiftDown = (rightShiftDown = (leftWinDown = (rightWinDown = (fakeAltGrControlDown = (leftControlBelongsToAltGr = false)))));
		previousModifiers = ModifierSet.None;
		isPushToTalkHeld = false;
		isDictationHeld = false;
		dictationHoldCancelled = false;
		ResetControlTapTracking();
	}

	void ILowLevelInputSink.OnKey(in LowLevelKeyEvent keyEvent)
	{
		if (!running || keyEvent.ExtraInfo == 1212369995)
		{
			return;
		}
		uint virtualKey = keyEvent.VirtualKey;
		bool isDown = keyEvent.IsDown;
		bool flag = leftControlWentDownWithLastEvent;
		leftControlWentDownWithLastEvent = false;
		if (IsModifierKey(virtualKey))
		{
			ResyncModifiersFromAsyncState(virtualKey);
		}
		switch (virtualKey)
		{
		case 162u:
			if ((keyEvent.ScanCode & 0x200) != 0)
			{
				fakeAltGrControlDown = isDown;
				return;
			}
			if (!isDown)
			{
				leftControlBelongsToAltGr = false;
			}
			else if (leftControlBelongsToAltGr)
			{
				return;
			}
			if (isDown && !leftControlDown)
			{
				lastRealLeftControlDownTime = keyEvent.TimeMs;
				leftControlWentDownWithLastEvent = true;
			}
			leftControlDown = isDown;
			break;
		case 163u:
			rightControlDown = isDown;
			break;
		case 17u:
			leftControlDown = isDown;
			break;
		case 164u:
			leftAltDown = isDown;
			break;
		case 18u:
			leftAltDown = isDown;
			break;
		case 165u:
			if (isDown && !rightAltDown)
			{
				bool flag3 = LayoutHasAltGrOverride?.Invoke() ?? KeyboardLayoutInfo.ForegroundLayoutHasAltGr();
				bool num = flag3 && flag && lastRealLeftControlDownTime == keyEvent.TimeMs;
				rightAltIsAltGr = fakeAltGrControlDown || flag3;
				if (num)
				{
					leftControlDown = false;
					leftControlBelongsToAltGr = true;
					ResetControlTapTracking();
				}
			}
			else if (!isDown)
			{
				rightAltIsAltGr = false;
			}
			rightAltDown = isDown;
			break;
		case 16u:
		case 160u:
			leftShiftDown = isDown;
			break;
		case 161u:
			rightShiftDown = isDown;
			break;
		case 91u:
			leftWinDown = isDown;
			break;
		case 92u:
			rightWinDown = isDown;
			break;
		default:
			if (isDown)
			{
				keyDownCounter++;
				ResetControlTapTracking();
				if (isRecording)
				{
					EnqueueRecording(3, HotkeyModifiers.None, keyEvent.TimeMs);
				}
				bool flag2;
				switch (virtualKey)
				{
				case 27u:
					Enqueue(ShortcutAction.Escape);
					return;
				case 122u:
				case 123u:
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				if (flag2 && CurrentModifiers() == (ModifierSet.Shift | ModifierSet.Win))
				{
					Enqueue((virtualKey == 123) ? ShortcutAction.ProactiveDebugSweep : ShortcutAction.MorningPreviewChord);
				}
				else if ((virtualKey == 83 && CurrentModifiers() == (ModifierSet.Shift | ModifierSet.Win)) || virtualKey == 44)
				{
					Enqueue(ShortcutAction.SystemScreenshotShortcut);
				}
				else if (isDictationHeld && !dictationHoldCancelled)
				{
					dictationHoldCancelled = true;
					Enqueue(ShortcutAction.DictationCancel);
				}
			}
			return;
		}
		ModifierSet modifierSet = CurrentModifiers();
		if (modifierSet != previousModifiers)
		{
			if (isRecording)
			{
				OnModifiersChangedWhileRecording(modifierSet, keyEvent.TimeMs);
			}
			else
			{
				OnModifiersChanged(modifierSet, keyEvent.TimeMs);
			}
			previousModifiers = modifierSet;
		}
	}

	private void OnModifiersChangedWhileRecording(ModifierSet modifiers, uint timeMs)
	{
		if (isPushToTalkHeld)
		{
			isPushToTalkHeld = false;
			Enqueue(ShortcutAction.PushToTalkUp);
		}
		if (isDictationHeld)
		{
			isDictationHeld = false;
			Enqueue(ShortcutAction.DictationUp);
		}
		ResetControlTapTracking();
		EnqueueRecording(2, (HotkeyModifiers)modifiers, timeMs);
	}

	bool ILowLevelInputSink.ShouldSwallowKey(in LowLevelKeyEvent keyEvent)
	{
		bool flag = running;
		if (flag)
		{
			uint virtualKey = keyEvent.VirtualKey;
			bool flag2 = ((virtualKey == 37 || virtualKey == 39) ? true : false);
			flag = flag2;
		}
		if (flag && keyEvent.ExtraInfo != 1212369995)
		{
			return ShouldSwallowArrow(in keyEvent);
		}
		if (!running || keyEvent.VirtualKey != 27 || keyEvent.ExtraInfo == 1212369995)
		{
			return false;
		}
		if (keyEvent.IsDown)
		{
			if (escapeSwallowed)
			{
				return true;
			}
			bool result;
			try
			{
				result = ConsumesEscape?.Invoke() ?? false;
			}
			catch
			{
				result = false;
			}
			escapeSwallowed = result;
			return result;
		}
		bool result2 = escapeSwallowed;
		escapeSwallowed = false;
		return result2;
	}

	private bool ShouldSwallowArrow(in LowLevelKeyEvent keyEvent)
	{
		if (!keyEvent.IsDown)
		{
			bool num = arrowSwallowed == keyEvent.VirtualKey;
			if (num)
			{
				arrowSwallowed = 0u;
			}
			return num;
		}
		if (arrowSwallowed == keyEvent.VirtualKey)
		{
			return true;
		}
		if (CurrentModifiers() != 0)
		{
			return false;
		}
		bool flag;
		try
		{
			flag = ConsumesArrow?.Invoke((keyEvent.VirtualKey == 39) ? 1 : (-1)) ?? false;
		}
		catch
		{
			flag = false;
		}
		arrowSwallowed = (flag ? keyEvent.VirtualKey : 0u);
		return flag;
	}

	private static bool IsModifierKey(uint virtualKey)
	{
		if (virtualKey - 16 <= 2 || virtualKey - 91 <= 1 || virtualKey - 160 <= 5)
		{
			return true;
		}
		return false;
	}

	private bool IsPhysicallyDown(uint virtualKey)
	{
		return PhysicalKeyStateOverride?.Invoke(virtualKey) ?? ((PInvoke.GetAsyncKeyState((int)virtualKey) & 0x8000) != 0);
	}

	private void ResyncModifiersFromAsyncState(uint exceptVirtualKey)
	{
		if ((exceptVirtualKey != 17 && exceptVirtualKey != 162) || 1 == 0)
		{
			leftControlDown = IsPhysicallyDown(162u) && !fakeAltGrControlDown && !leftControlBelongsToAltGr;
		}
		if (exceptVirtualKey != 163)
		{
			rightControlDown = IsPhysicallyDown(163u);
		}
		if ((exceptVirtualKey != 18 && exceptVirtualKey != 164) || 1 == 0)
		{
			leftAltDown = IsPhysicallyDown(164u);
		}
		if (exceptVirtualKey != 165)
		{
			rightAltDown = IsPhysicallyDown(165u);
			if (!rightAltDown)
			{
				rightAltIsAltGr = false;
				fakeAltGrControlDown = false;
			}
		}
		if ((exceptVirtualKey != 16 && exceptVirtualKey != 160) || 1 == 0)
		{
			leftShiftDown = IsPhysicallyDown(160u);
		}
		if (exceptVirtualKey != 161)
		{
			rightShiftDown = IsPhysicallyDown(161u);
		}
		if (exceptVirtualKey != 91)
		{
			leftWinDown = IsPhysicallyDown(91u);
		}
		if (exceptVirtualKey != 92)
		{
			rightWinDown = IsPhysicallyDown(92u);
		}
	}

	void ILowLevelInputSink.OnResyncRequested()
	{
		if (!running)
		{
			return;
		}
		ResyncModifiersFromAsyncState(0u);
		ModifierSet modifierSet = CurrentModifiers();
		if (modifierSet != previousModifiers)
		{
			if (isRecording)
			{
				OnModifiersChangedWhileRecording(modifierSet, (uint)Environment.TickCount);
			}
			else
			{
				OnModifiersChanged(modifierSet, (uint)Environment.TickCount);
			}
			previousModifiers = modifierSet;
		}
	}

	private ModifierSet CurrentModifiers()
	{
		ModifierSet modifierSet = ModifierSet.None;
		if (leftControlDown || rightControlDown)
		{
			modifierSet |= ModifierSet.Control;
		}
		if (leftAltDown || (rightAltDown && !rightAltIsAltGr))
		{
			modifierSet |= ModifierSet.Alt;
		}
		if (leftShiftDown || rightShiftDown)
		{
			modifierSet |= ModifierSet.Shift;
		}
		if (leftWinDown || rightWinDown)
		{
			modifierSet |= ModifierSet.Win;
		}
		return modifierSet;
	}

	private void OnModifiersChanged(ModifierSet modifiers, uint timeMs)
	{
		HotkeyBindings hotkeyBindings = bindings;
		ModifierSet modifiers2 = (ModifierSet)hotkeyBindings.PushToTalk.Modifiers;
		ModifierSet modifiers3 = (ModifierSet)hotkeyBindings.TextMode.Modifiers;
		ModifierSet modifiers4 = (ModifierSet)hotkeyBindings.Dictation.Modifiers;
		ModifierSet modifiers5 = (ModifierSet)hotkeyBindings.HandsFreeDictation.Modifiers;
		bool flag = modifiers == modifiers2;
		if (flag && !isPushToTalkHeld)
		{
			isPushToTalkHeld = true;
			Enqueue(ShortcutAction.PushToTalkDown);
		}
		else if (!flag && isPushToTalkHeld)
		{
			isPushToTalkHeld = false;
			Enqueue(ShortcutAction.PushToTalkUp);
		}
		bool flag2 = modifiers == modifiers4 && modifiers4 != modifiers2;
		if (flag2 && !isDictationHeld)
		{
			isDictationHeld = true;
			dictationHoldCancelled = false;
			Enqueue(ShortcutAction.DictationDown);
		}
		else if (!flag2 && isDictationHeld)
		{
			isDictationHeld = false;
			Enqueue(ShortcutAction.DictationUp);
		}
		if (modifiers != 0)
		{
			if (hotkeyBindings.TextMode.Trigger == HotkeyTrigger.Hold && modifiers == modifiers3)
			{
				Enqueue(ShortcutAction.TextComposerToggle);
			}
			if (hotkeyBindings.HandsFreeDictation.Trigger == HotkeyTrigger.Hold && modifiers == modifiers5 && modifiers5 != modifiers4)
			{
				Enqueue(ShortcutAction.HandsFreeDictationToggle);
			}
		}
		bool flag3 = modifiers == ModifierSet.None;
		if (previousModifiers == ModifierSet.None && !flag3)
		{
			controlTapPending = true;
			controlTapPressStartTime = timeMs;
			controlTapPressKeyDownSnapshot = keyDownCounter;
			controlTapPressWheelSnapshot = wheelCounter;
			pressSessionUnion = modifiers;
		}
		else if (!flag3 && controlTapPending)
		{
			pressSessionUnion |= modifiers;
		}
		else if (flag3 && controlTapPending)
		{
			controlTapPending = false;
			uint num = timeMs - controlTapPressStartTime;
			bool num2 = keyDownCounter != controlTapPressKeyDownSnapshot || wheelCounter != controlTapPressWheelSnapshot;
			bool flag4 = (hotkeyBindings.TextMode.Trigger == HotkeyTrigger.DoubleTap && pressSessionUnion == modifiers3) || (hotkeyBindings.HandsFreeDictation.Trigger == HotkeyTrigger.DoubleTap && pressSessionUnion == modifiers5 && modifiers5 != modifiers4);
			if (num2 || num > 400 || !flag4)
			{
				ResetControlTapTracking();
			}
			else
			{
				OnCompletedCleanTap(pressSessionUnion, controlTapPressStartTime, timeMs);
			}
		}
	}

	private void OnCompletedCleanTap(ModifierSet mask, uint pressStartTime, uint releaseTime)
	{
		if (completedControlTapCount > 0 && (mask != completedTapMask || pressStartTime - lastCompletedControlTapReleaseTime > 500))
		{
			completedControlTapCount = 0;
		}
		completedControlTapCount++;
		completedTapMask = mask;
		lastCompletedControlTapReleaseTime = releaseTime;
		if (completedControlTapCount >= 3)
		{
			completedControlTapCount = 0;
		}
		else if (completedControlTapCount == 2)
		{
			HotkeyBindings hotkeyBindings = bindings;
			if (hotkeyBindings.TextMode.Trigger == HotkeyTrigger.DoubleTap && (uint)mask == (uint)hotkeyBindings.TextMode.Modifiers)
			{
				Enqueue(ShortcutAction.TextComposerToggle);
			}
			if (hotkeyBindings.HandsFreeDictation.Trigger == HotkeyTrigger.DoubleTap && (uint)mask == (uint)hotkeyBindings.HandsFreeDictation.Modifiers && hotkeyBindings.HandsFreeDictation.Modifiers != hotkeyBindings.Dictation.Modifiers)
			{
				Enqueue(ShortcutAction.HandsFreeDictationToggle);
			}
		}
	}

	private void ResetControlTapTracking()
	{
		controlTapPending = false;
		completedControlTapCount = 0;
	}

	void ILowLevelInputSink.OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs)
	{
		if (!running)
		{
			return;
		}
		if (kind == LowLevelMouseButtonKind.Wheel)
		{
			wheelCounter++;
			return;
		}
		ResetControlTapTracking();
		if (kind == LowLevelMouseButtonKind.Left && (this.LeftMouseDown != null || this.MouseDown != null))
		{
			EnqueueClick(new PointD(x, y), 1);
		}
		else if (kind == LowLevelMouseButtonKind.Right && this.MouseDown != null)
		{
			EnqueueClick(new PointD(x, y), 4);
		}
	}

	private void Enqueue(ShortcutAction action)
	{
		int num = ringWriteIndex % 64;
		actionRing[num] = action;
		ringEntryKind[num] = 0;
		Volatile.Write(ref ringWriteIndex, ringWriteIndex + 1);
		dispatchSignal.Set();
	}

	private void EnqueueClick(PointD point, byte entryKind)
	{
		int num = ringWriteIndex % 64;
		clickRing[num] = point;
		ringEntryKind[num] = entryKind;
		Volatile.Write(ref ringWriteIndex, ringWriteIndex + 1);
		dispatchSignal.Set();
	}

	private void EnqueueRecording(byte kind, HotkeyModifiers modifiers, uint timeMs)
	{
		int num = ringWriteIndex % 64;
		ringEntryKind[num] = kind;
		modifierRing[num] = modifiers;
		timeRing[num] = timeMs;
		Volatile.Write(ref ringWriteIndex, ringWriteIndex + 1);
		dispatchSignal.Set();
	}

	private void MaskStartMenu()
	{
		Interlocked.Increment(ref startMenuMaskCount);
		Action startMenuMaskOverride = StartMenuMaskOverride;
		if (startMenuMaskOverride != null)
		{
			startMenuMaskOverride();
		}
		else if (!withoutHooks)
		{
			SendMaskKey();
		}
	}

	private unsafe static void SendMaskKey()
	{
		Span<INPUT> span = stackalloc INPUT[2];
		for (int i = 0; i < 2; i++)
		{
			span[i] = new INPUT
			{
				type = INPUT_TYPE.INPUT_KEYBOARD,
				Anonymous = new INPUT._Anonymous_e__Union
				{
					ki = new KEYBDINPUT
					{
						wVk = VIRTUAL_KEY.VK__none_,
						dwFlags = ((i == 1) ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : ((KEYBD_EVENT_FLAGS)0u)),
						dwExtraInfo = 1212369995u
					}
				}
			};
		}
		uint num = PInvoke.SendInput(span, sizeof(INPUT));
		if (num != 2)
		{
			Log.Warn("Shortcuts", $"Start menu mask: SendInput sent {num} of 2");
		}
	}

	private void DispatcherMain()
	{
		do
		{
			dispatchSignal.WaitOne();
			int num = Volatile.Read(in ringWriteIndex);
			while (ringReadIndex != num)
			{
				int num2 = ringReadIndex % 64;
				byte b = ringEntryKind[num2];
				ShortcutAction shortcutAction = actionRing[num2];
				PointD obj = clickRing[num2];
				HotkeyModifiers arg = modifierRing[num2];
				uint arg2 = timeRing[num2];
				ringReadIndex++;
				try
				{
					switch (b)
					{
					case 1:
						this.LeftMouseDown?.Invoke(obj);
						this.MouseDown?.Invoke(obj);
						continue;
					case 4:
						this.MouseDown?.Invoke(obj);
						continue;
					case 2:
						this.RecordingModifiersChanged?.Invoke(arg, arg2);
						continue;
					case 3:
						this.RecordingKeyPressed?.Invoke();
						continue;
					}
					if ((shortcutAction == ShortcutAction.PushToTalkDown || shortcutAction == ShortcutAction.DictationDown) ? true : false)
					{
						holdWatchdog?.Change(250, 250);
					}
					else
					{
						bool flag = ((shortcutAction == ShortcutAction.PushToTalkUp || shortcutAction == ShortcutAction.DictationUp) ? true : false);
						if (flag && !isPushToTalkHeld && !isDictationHeld)
						{
							holdWatchdog?.Change(-1, -1);
						}
					}
					if (shortcutAction == ShortcutAction.DictationDown && bindings.Dictation.Modifiers.HasFlag(HotkeyModifiers.Win))
					{
						MaskStartMenu();
					}
					this.ShortcutTriggered?.Invoke(this, shortcutAction);
				}
				catch (Exception ex)
				{
					Log.Error("Shortcuts", $"handler for {shortcutAction} threw", ex);
				}
			}
		}
		while (running);
	}
}
