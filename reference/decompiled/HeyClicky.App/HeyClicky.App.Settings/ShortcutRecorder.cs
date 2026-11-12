using System;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Settings;

public sealed class ShortcutRecorder
{
	public enum Slot
	{
		PushToTalk,
		TextMode,
		Dictation,
		HandsFreeDictation
	}

	private const double TapMaxHoldSeconds = 0.4;

	private const double DoubleTapMaxIntervalSeconds = 0.5;

	private const double TimeoutSeconds = 7.0;

	private readonly DispatcherQueue dq;

	private readonly DispatcherQueueTimer timeout;

	private readonly DispatcherQueueTimer loneTapSettle;

	private HotkeyModifiers holdUnion;

	private bool holdSawOtherKey;

	private HotkeyModifiers tapUnion;

	private DateTime? tapPressStart;

	private HotkeyModifiers? pendingFirstTap;

	private DateTime pendingFirstTapRelease;

	public Slot? Mode { get; private set; }

	public HotkeyBinding? Captured { get; private set; }

	public bool IsLocked { get; private set; }

	public string? Hint { get; private set; }

	public Func<Slot, HotkeyBinding, string?>? Validate { get; set; }

	public event Action? Changed;

	public ShortcutRecorder(DispatcherQueue dq)
	{
		this.dq = dq;
		timeout = dq.CreateTimer();
		timeout.Interval = TimeSpan.FromSeconds(7.0);
		timeout.IsRepeating = false;
		timeout.Tick += delegate
		{
			if ((object)Captured == null)
			{
				Cancel();
			}
		};
		loneTapSettle = dq.CreateTimer();
		loneTapSettle.Interval = TimeSpan.FromSeconds(0.5);
		loneTapSettle.IsRepeating = false;
		loneTapSettle.Tick += delegate
		{
			HotkeyModifiers? hotkeyModifiers = pendingFirstTap;
			if (hotkeyModifiers.HasValue)
			{
				HotkeyModifiers valueOrDefault = hotkeyModifiers.GetValueOrDefault();
				if (!IsLocked)
				{
					DateTime? dateTime = tapPressStart;
					if (!dateTime.HasValue)
					{
						Commit(new HotkeyBinding(valueOrDefault));
					}
				}
			}
		};
	}

	private static bool IsHoldSlot(Slot slot)
	{
		if (slot == Slot.PushToTalk || slot == Slot.Dictation)
		{
			return true;
		}
		return false;
	}

	public void Begin(Slot slot)
	{
		ResetBuffers();
		Hint = null;
		Mode = slot;
		timeout.Stop();
		timeout.Start();
		Log.Info("settings", $"recording {slot}");
		this.Changed?.Invoke();
	}

	public void Cancel()
	{
		if (Mode.HasValue)
		{
			timeout.Stop();
			loneTapSettle.Stop();
			ResetBuffers();
			Hint = null;
			Mode = null;
			this.Changed?.Invoke();
		}
	}

	private void ResetBuffers()
	{
		Captured = null;
		IsLocked = false;
		holdUnion = HotkeyModifiers.None;
		holdSawOtherKey = false;
		tapUnion = HotkeyModifiers.None;
		tapPressStart = null;
		pendingFirstTap = null;
		loneTapSettle.Stop();
	}

	public void IngestKey()
	{
		Slot? mode = Mode;
		if (!mode.HasValue)
		{
			return;
		}
		Slot valueOrDefault = mode.GetValueOrDefault();
		if (IsLocked)
		{
			return;
		}
		if (IsHoldSlot(valueOrDefault))
		{
			if (holdUnion != 0)
			{
				holdSawOtherKey = true;
			}
			Hint = "Shortcuts use only Ctrl, Alt, Shift and Win. Hold two of them together, then let go.";
			timeout.Stop();
			timeout.Start();
			this.Changed?.Invoke();
		}
		else
		{
			pendingFirstTap = null;
			loneTapSettle.Stop();
		}
	}

	public void IngestModifiers(HotkeyModifiers held)
	{
		Slot? mode = Mode;
		if (!mode.HasValue)
		{
			return;
		}
		Slot valueOrDefault = mode.GetValueOrDefault();
		if (IsLocked)
		{
			return;
		}
		if (IsHoldSlot(valueOrDefault))
		{
			if (held == HotkeyModifiers.None)
			{
				if (holdUnion != 0)
				{
					string text = ShortcutCaptureRules.HoldCaptureProblem(holdUnion, holdSawOtherKey);
					if (text != null)
					{
						Reject(text);
					}
					else
					{
						Commit(new HotkeyBinding(holdUnion));
					}
				}
			}
			else
			{
				holdUnion |= held;
				Captured = new HotkeyBinding(holdUnion);
				timeout.Stop();
				timeout.Start();
				this.Changed?.Invoke();
			}
			return;
		}
		if (held == HotkeyModifiers.None)
		{
			ReleaseToEmpty();
			return;
		}
		DateTime valueOrDefault2 = tapPressStart.GetValueOrDefault();
		if (!tapPressStart.HasValue)
		{
			valueOrDefault2 = DateTime.UtcNow;
			tapPressStart = valueOrDefault2;
		}
		tapUnion |= held;
		HotkeyModifiers? hotkeyModifiers = pendingFirstTap;
		if (hotkeyModifiers.HasValue)
		{
			HotkeyModifiers valueOrDefault3 = hotkeyModifiers.GetValueOrDefault();
			if ((valueOrDefault3 | tapUnion) != valueOrDefault3)
			{
				pendingFirstTap = null;
				loneTapSettle.Stop();
			}
		}
		timeout.Stop();
		timeout.Start();
		Captured = new HotkeyBinding(tapUnion);
		this.Changed?.Invoke();
	}

	private void ReleaseToEmpty()
	{
		if (tapUnion == HotkeyModifiers.None)
		{
			tapPressStart = null;
			return;
		}
		HotkeyModifiers hotkeyModifiers = tapUnion;
		DateTime dateTime = tapPressStart ?? DateTime.UtcNow;
		tapUnion = HotkeyModifiers.None;
		tapPressStart = null;
		if ((DateTime.UtcNow - dateTime).TotalSeconds <= 0.4)
		{
			if (pendingFirstTap == hotkeyModifiers && (dateTime - pendingFirstTapRelease).TotalSeconds <= 0.5)
			{
				Commit(new HotkeyBinding(hotkeyModifiers, HotkeyTrigger.DoubleTap));
				return;
			}
			pendingFirstTap = hotkeyModifiers;
			pendingFirstTapRelease = DateTime.UtcNow;
			Captured = new HotkeyBinding(hotkeyModifiers, HotkeyTrigger.DoubleTap);
			timeout.Stop();
			timeout.Start();
			loneTapSettle.Stop();
			loneTapSettle.Start();
			this.Changed?.Invoke();
		}
		else
		{
			pendingFirstTap = null;
			loneTapSettle.Stop();
			Commit(new HotkeyBinding(hotkeyModifiers));
		}
	}

	private void Reject(string problem)
	{
		Log.Info("settings", $"recording {Mode}: not captured ({problem})");
		ResetBuffers();
		Hint = problem;
		timeout.Stop();
		timeout.Start();
		this.Changed?.Invoke();
	}

	private void Commit(HotkeyBinding binding)
	{
		Slot? mode = Mode;
		if (mode.HasValue)
		{
			Slot valueOrDefault = mode.GetValueOrDefault();
			string text = Validate?.Invoke(valueOrDefault, binding);
			if (text != null)
			{
				Reject(text);
				return;
			}
		}
		Hint = null;
		timeout.Stop();
		loneTapSettle.Stop();
		pendingFirstTap = null;
		Captured = binding;
		IsLocked = true;
		Log.Info("settings", $"recorded {Mode}: {binding.DisplayText}");
		this.Changed?.Invoke();
	}
}
