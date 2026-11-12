using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Platform.Input;

public static class ClickyScreenActuator
{
	internal interface IInputSystem
	{
		uint Send(ReadOnlySpan<Input> inputs, out int error);

		bool IsKeyDown(ushort virtualKey);

		bool ForegroundIsElevated();

		void SetCursorPosition(int x, int y);
	}

	private readonly record struct Stroke(ushort VirtualKey, bool Shift);

	private sealed class User32InputSystem : IInputSystem
	{
		public unsafe uint Send(ReadOnlySpan<Input> inputs, out int error)
		{
			fixed (Input* inputs2 = inputs)
			{
				uint num = SendInput((uint)inputs.Length, inputs2, sizeof(Input));
				error = ((num != inputs.Length) ? Marshal.GetLastPInvokeError() : 0);
				return num;
			}
		}

		public bool IsKeyDown(ushort virtualKey)
		{
			return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
		}

		public bool ForegroundIsElevated()
		{
			return ForegroundIntegrity.IsAboveOurs();
		}

		public void SetCursorPosition(int x, int y)
		{
			SetCursorPos(x, y);
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct Input
	{
		[FieldOffset(0)]
		public uint Type;

		[FieldOffset(8)]
		public MouseInputData Mouse;

		[FieldOffset(8)]
		public KeyboardInputData Keyboard;

		public readonly bool IsVirtualKey
		{
			get
			{
				if (Type == 1 && (Keyboard.Flags & 4) == 0)
				{
					ushort virtualKey = Keyboard.VirtualKey;
					if (virtualKey > 0)
					{
						return virtualKey <= 255;
					}
					return false;
				}
				return false;
			}
		}

		public readonly bool IsKeyUp => (Keyboard.Flags & 2) != 0;
	}

	internal struct MouseInputData
	{
		public int Dx;

		public int Dy;

		public int MouseData;

		public uint Flags;

		public uint Time;

		public nuint ExtraInfo;
	}

	internal struct KeyboardInputData
	{
		public ushort VirtualKey;

		public ushort Scan;

		public uint Flags;

		public uint Time;

		public nuint ExtraInfo;
	}

	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	private static readonly (ClickyModifierKeys Flag, ushort VirtualKey)[] ModifierKeys = new(ClickyModifierKeys, ushort)[4]
	{
		(ClickyModifierKeys.Control, 162),
		(ClickyModifierKeys.Alt, 164),
		(ClickyModifierKeys.Shift, 160),
		(ClickyModifierKeys.Windows, 91)
	};

	private static readonly long[] heldKeys = new long[4];

	private const int ReleaseRetryMilliseconds = 250;

	private static readonly object actionStart = new object();

	private static int actionsStarted;

	private static int strandedWatchRunning;

	private const int StrandedWatchIntervalMilliseconds = 500;

	private const int StrandedWatchMilliseconds = 120000;

	private const uint MouseEventMove = 1u;

	private const uint MouseEventLeftDown = 2u;

	private const uint MouseEventLeftUp = 4u;

	private const uint MouseEventRightDown = 8u;

	private const uint MouseEventRightUp = 16u;

	private const uint MouseEventMiddleDown = 32u;

	private const uint MouseEventMiddleUp = 64u;

	private const uint MouseEventWheel = 2048u;

	private const uint MouseEventVirtualDesk = 16384u;

	private const uint MouseEventAbsolute = 32768u;

	private const uint KeyEventExtended = 1u;

	private const uint KeyEventKeyUp = 2u;

	private const uint KeyEventUnicode = 4u;

	internal static IInputSystem InputSystem { get; set; } = new User32InputSystem();

	public static PointD CurrentPointer
	{
		get
		{
			GetCursorPos(out var point);
			return new PointD(point.X, point.Y);
		}
	}

	public static bool IsLeftButtonHeld { get; private set; }

	private static bool HoldsAny => (Volatile.Read(in heldKeys[0]) | Volatile.Read(in heldKeys[1]) | Volatile.Read(in heldKeys[2]) | Volatile.Read(in heldKeys[3])) != 0;

	public static Task GlideAsync(PointD target, double seconds = 0.45, CancellationToken ct = default(CancellationToken))
	{
		return Task.Run(delegate
		{
			PointD currentPointer = CurrentPointer;
			int num = Math.Max(8, (int)(seconds * 60.0));
			Stopwatch clock = Stopwatch.StartNew();
			using PreciseTimer preciseTimer = new PreciseTimer();
			for (int i = 1; i <= num; i++)
			{
				if (ct.IsCancellationRequested)
				{
					break;
				}
				double num2 = Ease((double)i / (double)num);
				MoveTo(new PointD(currentPointer.X + (target.X - currentPointer.X) * num2, currentPointer.Y + (target.Y - currentPointer.Y) * num2));
				preciseTimer.WaitUntil(clock, seconds * (double)i / (double)num);
			}
		}, ct);
	}

	public static double Ease(double linear)
	{
		if (!(linear < 0.5))
		{
			return 1.0 - Math.Pow(-2.0 * linear + 2.0, 3.0) / 2.0;
		}
		return 4.0 * linear * linear * linear;
	}

	public static async Task<ClickyActuation> ClickAsync(PointD point, ClickyMouseButton button = ClickyMouseButton.Left, int count = 1, ClickyModifierKeys modifiers = ClickyModifierKeys.None)
	{
		var (down, up) = button switch
		{
			ClickyMouseButton.Right => (8u, 16u), 
			ClickyMouseButton.Middle => (32u, 64u), 
			_ => (2u, 4u), 
		};
		if (!ForegroundTakesInput())
		{
			return ClickyActuation.Refused;
		}
		return await HoldingModifiersAsync(modifiers, async delegate
		{
			MoveTo(point);
			for (int clickNumber = 1; clickNumber <= Math.Max(1, count); clickNumber++)
			{
				if (!SendMouse(down))
				{
					return ClickyActuation.Refused;
				}
				await SleepAsync(40).ConfigureAwait(continueOnCapturedContext: false);
				if (!SendMouse(up))
				{
					return ClickyActuation.Refused;
				}
				if (clickNumber < count)
				{
					await SleepAsync(70).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			return ClickyActuation.Done;
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static ClickyActuation SetLeftButton(bool down)
	{
		if (down == IsLeftButtonHeld)
		{
			return ClickyActuation.Done;
		}
		if (down && !ForegroundTakesInput())
		{
			return ClickyActuation.Refused;
		}
		if (!SendMouse(down ? 2u : 4u))
		{
			return ClickyActuation.Refused;
		}
		IsLeftButtonHeld = down;
		return ClickyActuation.Done;
	}

	public static void ReleaseHeldButton()
	{
		if (IsLeftButtonHeld)
		{
			SetLeftButton(down: false);
		}
	}

	public static async Task<ClickyActuation> HoldAsync(IEnumerable<string> keys, double seconds)
	{
		ClickyModifierKeys clickyModifierKeys = ClickyModifierKeys.None;
		ushort? num = null;
		foreach (string item in keys.Select((string k) => k.ToLowerInvariant()))
		{
			ClickyModifierKeys? clickyModifierKeys2 = ClickyKeyNames.Modifier(item);
			if (clickyModifierKeys2.HasValue)
			{
				ClickyModifierKeys valueOrDefault = clickyModifierKeys2.GetValueOrDefault();
				clickyModifierKeys |= valueOrDefault;
				continue;
			}
			ushort? num2 = ClickyKeyNames.VirtualKey(item);
			if (num2.HasValue)
			{
				ushort valueOrDefault2 = num2.GetValueOrDefault();
				if (!num.HasValue)
				{
					num = valueOrDefault2;
					continue;
				}
			}
			return ClickyActuation.UnknownKey;
		}
		if (!num.HasValue && clickyModifierKeys == ClickyModifierKeys.None)
		{
			return ClickyActuation.UnknownKey;
		}
		if (!ForegroundTakesInput())
		{
			return ClickyActuation.Refused;
		}
		Input[] downs = ModifierEvents(clickyModifierKeys);
		if (num.HasValue)
		{
			ushort valueOrDefault3 = num.GetValueOrDefault();
			Input[] array = downs;
			int num3 = 0;
			Input[] array2 = new Input[1 + array.Length];
			ReadOnlySpan<Input> readOnlySpan = new ReadOnlySpan<Input>(array);
			readOnlySpan.CopyTo(new Span<Input>(array2).Slice(num3, readOnlySpan.Length));
			num3 += readOnlySpan.Length;
			array2[num3] = KeyEvent(valueOrDefault3, keyUp: false);
			downs = array2;
		}
		ClickyActuation result;
		try
		{
			if (!SendKeys(downs))
			{
				result = ClickyActuation.Refused;
			}
			else
			{
				await SleepAsync((int)(Math.Min(10.0, Math.Max(0.1, seconds)) * 1000.0)).ConfigureAwait(continueOnCapturedContext: false);
				result = ClickyActuation.Done;
			}
		}
		finally
		{
			await ReleaseAsync(PressedKeys(downs).Reverse().ToArray()).ConfigureAwait(continueOnCapturedContext: false);
		}
		return result;
	}

	public static async Task<ClickyActuation> DragAsync(IReadOnlyList<PointD> path)
	{
		if (path.Count < 2)
		{
			return ClickyActuation.Done;
		}
		if (!ForegroundTakesInput())
		{
			return ClickyActuation.Refused;
		}
		MoveTo(path[0]);
		if (!SendMouse(2u))
		{
			return ClickyActuation.Refused;
		}
		await SleepAsync(80).ConfigureAwait(continueOnCapturedContext: false);
		PointD previous = path[0];
		foreach (PointD waypoint in path.Skip(1))
		{
			int steps = Math.Max(6, (int)(Math.Sqrt(Math.Pow(waypoint.X - previous.X, 2.0) + Math.Pow(waypoint.Y - previous.Y, 2.0)) / 12.0));
			for (int step = 1; step <= steps; step++)
			{
				double num = (double)step / (double)steps;
				MoveTo(new PointD(previous.X + (waypoint.X - previous.X) * num, previous.Y + (waypoint.Y - previous.Y) * num));
				await SleepAsync(14).ConfigureAwait(continueOnCapturedContext: false);
			}
			previous = waypoint;
		}
		await SleepAsync(60).ConfigureAwait(continueOnCapturedContext: false);
		MoveTo(path[path.Count - 1]);
		return (!SendMouse(4u)) ? ClickyActuation.Refused : ClickyActuation.Done;
	}

	public static async Task<ClickyActuation> ScrollAsync(int lines)
	{
		if (!ForegroundTakesInput())
		{
			return ClickyActuation.Refused;
		}
		int value = 3;
		bool flag = SystemParametersInfoW(104u, 0u, ref value, 0u);
		if (flag)
		{
			bool flag2 = ((value <= 0 || value > 100) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			value = 3;
		}
		int delta = Math.Max(1, 120 / Math.Max(1, value)) * ((lines <= 0) ? 1 : (-1));
		for (int i = 0; i < Math.Min(Math.Abs(lines), 40); i++)
		{
			if (!SendMouse(2048u, delta))
			{
				return ClickyActuation.Refused;
			}
			await SleepAsync(25).ConfigureAwait(continueOnCapturedContext: false);
		}
		return ClickyActuation.Done;
	}

	public static async Task<ClickyActuation> TypeAsync(string text, CancellationToken ct = default(CancellationToken))
	{
		nint layout = ForegroundKeyboardLayout();
		TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
		while (elements.MoveNext() && !ct.IsCancellationRequested)
		{
			if (!ForegroundTakesInput())
			{
				return ClickyActuation.Refused;
			}
			string text2 = (string)elements.Current;
			Stroke? stroke = ((text2.Length == 1) ? KeyStroke(text2[0], layout) : ((Stroke?)null));
			Input[] events;
			if (stroke.HasValue)
			{
				Stroke valueOrDefault = stroke.GetValueOrDefault();
				events = Chord(valueOrDefault.VirtualKey, valueOrDefault.Shift ? ClickyModifierKeys.Shift : ClickyModifierKeys.None);
			}
			else
			{
				events = UnicodeEvents(text2);
			}
			if (await StrokeAsync(events).ConfigureAwait(continueOnCapturedContext: false) == ClickyActuation.Refused)
			{
				return ClickyActuation.Refused;
			}
			await SleepAsync(14).ConfigureAwait(continueOnCapturedContext: false);
		}
		return ClickyActuation.Done;
	}

	public static async Task<ClickyActuation> PressAsync(string keys)
	{
		List<string> list = (from p in keys.ToLowerInvariant().Split('+')
			select p.Trim()).ToList();
		if (list.Count != 0)
		{
			ushort? num = ClickyKeyNames.VirtualKey(list[list.Count - 1]);
			if (num.HasValue)
			{
				ushort valueOrDefault = num.GetValueOrDefault();
				ClickyModifierKeys clickyModifierKeys = ClickyModifierKeys.None;
				foreach (string item in list.Take(list.Count - 1))
				{
					ClickyModifierKeys? clickyModifierKeys2 = ClickyKeyNames.Modifier(item);
					if (clickyModifierKeys2.HasValue)
					{
						ClickyModifierKeys valueOrDefault2 = clickyModifierKeys2.GetValueOrDefault();
						clickyModifierKeys |= valueOrDefault2;
						continue;
					}
					return ClickyActuation.UnknownKey;
				}
				if (!ForegroundTakesInput())
				{
					return ClickyActuation.Refused;
				}
				return await StrokeAsync(Chord(valueOrDefault, clickyModifierKeys)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		return ClickyActuation.UnknownKey;
	}

	private static async Task<ClickyActuation> HoldingModifiersAsync(ClickyModifierKeys modifiers, Func<Task<ClickyActuation>> body)
	{
		Input[] downs = ModifierEvents(modifiers);
		if (downs.Length == 0)
		{
			return await body().ConfigureAwait(continueOnCapturedContext: false);
		}
		ClickyActuation result;
		try
		{
			if (!SendKeys(downs))
			{
				result = ClickyActuation.Refused;
			}
			else
			{
				await SleepAsync(12).ConfigureAwait(continueOnCapturedContext: false);
				result = await body().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			await ReleaseAsync(PressedKeys(downs).Reverse().ToArray()).ConfigureAwait(continueOnCapturedContext: false);
		}
		return result;
	}

	private static async Task<ClickyActuation> StrokeAsync(Input[] events)
	{
		if (SendKeys(events))
		{
			return ClickyActuation.Done;
		}
		await ReleaseAsync(PressedKeys(events).Reverse().ToArray()).ConfigureAwait(continueOnCapturedContext: false);
		return ClickyActuation.Refused;
	}

	private static bool ForegroundTakesInput()
	{
		if (InputSystem.ForegroundIsElevated())
		{
			Log.Warn("Actuator", "the window in front runs elevated and refuses HeyClicky's input: the action stops before anything is sent");
			return false;
		}
		lock (actionStart)
		{
			actionsStarted++;
		}
		ReleaseStrandedKeys("before the next action");
		return true;
	}

	private static bool Holds(int virtualKey)
	{
		return (Volatile.Read(in heldKeys[virtualKey >> 6]) & (1L << virtualKey)) != 0;
	}

	private static void Hold(int virtualKey)
	{
		Interlocked.Or(ref heldKeys[virtualKey >> 6], 1L << (virtualKey & 0x3F));
	}

	private static bool Claim(int virtualKey)
	{
		long num = 1L << (virtualKey & 0x3F);
		return (Interlocked.And(ref heldKeys[virtualKey >> 6], ~num) & num) != 0;
	}

	private static bool SendKeys(ReadOnlySpan<Input> events)
	{
		int error;
		uint num = InputSystem.Send(events, out error);
		for (int i = 0; i < (int)num; i++)
		{
			if (events[i].IsVirtualKey)
			{
				if (events[i].IsKeyUp)
				{
					Claim(events[i].Keyboard.VirtualKey);
				}
				else
				{
					Hold(events[i].Keyboard.VirtualKey);
				}
			}
		}
		if (num == events.Length)
		{
			return true;
		}
		if (num == 0)
		{
			Log.Warn("Actuator", $"SendInput sent 0 of {events.Length} (error {error}): an elevated window in front refuses input");
		}
		else
		{
			Log.Error("Actuator", $"SendInput sent {num} of {events.Length} key events (error {error}): the rest were refused, so what went in is let go of again");
		}
		return false;
	}

	private static async Task ReleaseAsync(IReadOnlyList<ushort> keys)
	{
		if (!TryRelease(keys, "trying again shortly"))
		{
			await SleepAsync(250).ConfigureAwait(continueOnCapturedContext: false);
			ushort[] array = keys.Where((ushort key) => Holds(key)).ToArray();
			if (TryRelease(array, "it stays down until HeyClicky can let go of it (the drive's end tries again)") && array.Length != 0)
			{
				Log.Info("Actuator", "the retry let go of " + Names(array));
			}
		}
	}

	private static bool TryRelease(IReadOnlyList<ushort> keys, string then, bool logRefusal = true)
	{
		List<ushort> list = new List<ushort>(keys.Count);
		foreach (ushort key in keys)
		{
			if (Claim(key))
			{
				list.Add(key);
			}
		}
		if (list.Count == 0)
		{
			return true;
		}
		Input[] array = list.Select((ushort key) => KeyEvent(key, keyUp: true)).ToArray();
		int error;
		uint num = InputSystem.Send(array, out error);
		if (num == array.Length)
		{
			return true;
		}
		List<ushort> list2 = list.Skip((int)num).ToList();
		foreach (ushort item in list2)
		{
			Hold(item);
		}
		if (logRefusal)
		{
			Log.Error("Actuator", $"the key-up for {Names(list2)} was refused (sent {num} of {array.Length}, error {error}): {then}");
		}
		return false;
	}

	public static void ReleaseStrandedKeys(string when, bool logRefusal = true)
	{
		if (!HoldsAny)
		{
			return;
		}
		List<ushort> list = new List<ushort>();
		for (ushort num = 1; num <= 255; num++)
		{
			if (Holds(num))
			{
				if (InputSystem.IsKeyDown(num))
				{
					list.Add(num);
				}
				else
				{
					Claim(num);
				}
			}
		}
		list.Sort((ushort a, ushort b) => ReleaseRank(a).CompareTo(ReleaseRank(b)));
		if (list.Count > 0 && TryRelease(list, "it stays down until HeyClicky can let go of it", logRefusal))
		{
			Log.Info("Actuator", when + ": let go of " + Names(list) + ", which HeyClicky had left down");
		}
	}

	public static void ReleaseStrandedKeysAfterDrive(string when)
	{
		ReleaseStrandedKeys(when);
		if (!HoldsAny || Interlocked.Exchange(ref strandedWatchRunning, 1) == 1)
		{
			return;
		}
		int started;
		lock (actionStart)
		{
			started = actionsStarted;
		}
		Log.Warn("Actuator", $"{when}: a key HeyClicky pressed is still refused; trying again every {500} ms");
		Task.Run(async delegate
		{
			try
			{
				long giveUpAt = Environment.TickCount64 + 120000;
				while (Environment.TickCount64 < giveUpAt)
				{
					await Task.Delay(500).ConfigureAwait(continueOnCapturedContext: false);
					lock (actionStart)
					{
						if (actionsStarted != started || !HoldsAny)
						{
							return;
						}
						ReleaseStrandedKeys("after the drive", logRefusal: false);
						if (!HoldsAny)
						{
							return;
						}
					}
				}
				Log.Error("Actuator", "a key HeyClicky pressed is still refused two minutes after the drive: the user's own press of it lets it go");
			}
			finally
			{
				Volatile.Write(ref strandedWatchRunning, 0);
			}
		});
	}

	internal static void NoteOthersKey(in LowLevelKeyEvent keyEvent)
	{
		bool flag = keyEvent.ExtraInfo == ClickyInputProvenance.Tag;
		if (!flag)
		{
			uint virtualKey = keyEvent.VirtualKey;
			bool flag2 = ((virtualKey > 255 || virtualKey == 0) ? true : false);
			flag = flag2;
		}
		if (!flag && Holds((int)keyEvent.VirtualKey))
		{
			Claim((int)keyEvent.VirtualKey);
		}
	}

	private static int ReleaseRank(ushort key)
	{
		int num = Array.FindIndex(ModifierKeys, ((ClickyModifierKeys Flag, ushort VirtualKey) m) => m.VirtualKey == key);
		if (num < 0)
		{
			return 0;
		}
		return ModifierKeys.Length - num;
	}

	private static Input[] ModifierEvents(ClickyModifierKeys modifiers)
	{
		return (from m in ModifierKeys
			where modifiers.HasFlag(m.Flag)
			select KeyEvent(m.VirtualKey, keyUp: false)).ToArray();
	}

	private static Input[] Chord(ushort virtualKey, ClickyModifierKeys modifiers)
	{
		Input[] array = ModifierEvents(modifiers);
		List<Input> list = new List<Input>();
		list.AddRange(array);
		list.Add(KeyEvent(virtualKey, keyUp: false));
		list.Add(KeyEvent(virtualKey, keyUp: true));
		list.AddRange(from down in System.Linq.Enumerable.Reverse(array)
			select KeyEvent(down.Keyboard.VirtualKey, keyUp: true));
		return list.ToArray();
	}

	private static Input[] UnicodeEvents(string element)
	{
		return element.SelectMany((char unit) => new Input[2]
		{
			KeyInput(0, unit, 4u),
			KeyInput(0, unit, 6u)
		}).ToArray();
	}

	private static IEnumerable<ushort> PressedKeys(Input[] events)
	{
		return from e in events
			where e.IsVirtualKey && !e.IsKeyUp
			select e.Keyboard.VirtualKey;
	}

	private static string Names(IEnumerable<ushort> keys)
	{
		return string.Join("+", keys.Select((ushort key) => key switch
		{
			162 => "Ctrl", 
			164 => "Alt", 
			160 => "Shift", 
			91 => "Win", 
			_ => $"key 0x{key:X2}", 
		}));
	}

	private static Stroke? KeyStroke(char character, nint layout)
	{
		switch (character)
		{
		case ' ':
			return new Stroke(32, Shift: false);
		case '\n':
		case '\r':
			return new Stroke(13, Shift: false);
		case '\t':
			return new Stroke(9, Shift: false);
		default:
		{
			short num = VkKeyScanExW(character, layout);
			if (num == -1)
			{
				return null;
			}
			byte b = (byte)(num & 0xFF);
			byte b2 = (byte)((num >> 8) & 0xFF);
			if ((b2 & -2) != 0 || b == byte.MaxValue)
			{
				return null;
			}
			return new Stroke(b, (b2 & 1) != 0);
		}
		}
	}

	private static nint ForegroundKeyboardLayout()
	{
		nint foregroundWindow = GetForegroundWindow();
		uint processId;
		return GetKeyboardLayout((foregroundWindow != 0) ? GetWindowThreadProcessId(foregroundWindow, out processId) : 0u);
	}

	public static void MoveTo(PointD point)
	{
		int systemMetrics = GetSystemMetrics(76);
		int systemMetrics2 = GetSystemMetrics(77);
		int systemMetrics3 = GetSystemMetrics(78);
		int systemMetrics4 = GetSystemMetrics(79);
		int value = (int)Math.Round(point.X);
		int value2 = (int)Math.Round(point.Y);
		value = Math.Clamp(value, systemMetrics, systemMetrics + systemMetrics3 - 1);
		value2 = Math.Clamp(value2, systemMetrics2, systemMetrics2 + systemMetrics4 - 1);
		int dx = (int)(((long)(value - systemMetrics) * 65536L + systemMetrics3 / 2) / Math.Max(1, systemMetrics3));
		int dy = (int)(((long)(value2 - systemMetrics2) * 65536L + systemMetrics4 / 2) / Math.Max(1, systemMetrics4));
		Input reference = MouseInput(49153u, dx, dy, 0);
		SendInputs(new ReadOnlySpan<Input>(in reference));
		GetCursorPos(out var point2);
		if (point2.X != value || point2.Y != value2)
		{
			InputSystem.SetCursorPosition(value, value2);
		}
	}

	private static bool SendMouse(uint flags, int data = 0)
	{
		Input reference = MouseInput(flags, 0, 0, data);
		return SendInputs(new ReadOnlySpan<Input>(in reference));
	}

	private static Input KeyEvent(ushort virtualKey, bool keyUp)
	{
		return KeyInput(virtualKey, (ushort)MapVirtualKeyW(virtualKey, 0u), (uint)(keyUp ? 2 : 0) | (IsExtendedKey(virtualKey) ? 1u : 0u));
	}

	private static bool IsExtendedKey(ushort virtualKey)
	{
		switch (virtualKey)
		{
		case 33:
		case 34:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
		case 40:
		case 44:
		case 45:
		case 46:
		case 91:
		case 92:
		case 93:
		case 111:
		case 144:
		case 163:
		case 165:
			return true;
		default:
			return false;
		}
	}

	private static Input MouseInput(uint flags, int dx, int dy, int data)
	{
		Input result = default(Input);
		result.Type = 0u;
		result.Mouse = new MouseInputData
		{
			Dx = dx,
			Dy = dy,
			MouseData = data,
			Flags = flags,
			ExtraInfo = ClickyInputProvenance.Tag
		};
		return result;
	}

	private static Input KeyInput(ushort virtualKey, ushort scan, uint flags)
	{
		Input result = default(Input);
		result.Type = 1u;
		result.Keyboard = new KeyboardInputData
		{
			VirtualKey = virtualKey,
			Scan = scan,
			Flags = flags,
			ExtraInfo = ClickyInputProvenance.Tag
		};
		return result;
	}

	private static bool SendInputs(ReadOnlySpan<Input> inputs)
	{
		int error;
		uint num = InputSystem.Send(inputs, out error);
		if (num == inputs.Length)
		{
			return true;
		}
		Log.Warn("Actuator", $"SendInput sent {num} of {inputs.Length} (error {error}): an elevated window in front refuses input");
		return false;
	}

	private static Task SleepAsync(int milliseconds)
	{
		return PreciseTimer.DelayAsync(milliseconds);
	}

	[DllImport("user32.dll", SetLastError = true)]
	private unsafe static extern uint SendInput(uint count, Input* inputs, int size);

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int virtualKey);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out NativePoint point);

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	[DllImport("user32.dll")]
	private static extern uint MapVirtualKeyW(uint code, uint mapType);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern short VkKeyScanExW(char character, nint layout);

	[DllImport("user32.dll")]
	private static extern nint GetKeyboardLayout(uint threadId);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SystemParametersInfoW(uint action, uint param, ref int value, uint winIni);
}
