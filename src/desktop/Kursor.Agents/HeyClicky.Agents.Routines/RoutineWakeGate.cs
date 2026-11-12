using System;
using System.Threading;

namespace HeyClicky.Agents.Routines;

public sealed class RoutineWakeGate
{
	private readonly Lock gate = new Lock();

	private bool awaitingDisplaySinceSleep;

	private bool awaitingFullWakeSinceSleep;

	private DateTimeOffset? displayOnFirstObservedAt;

	public bool IsDisplayOn { get; private set; } = true;

	public bool DisplayOffMeansStandby { get; set; }

	public bool IsInDarkWake
	{
		get
		{
			using (gate.EnterScope())
			{
				return awaitingDisplaySinceSleep && !IsDisplayOn;
			}
		}
	}

	public void NoteSleeping()
	{
		using (gate.EnterScope())
		{
			awaitingDisplaySinceSleep = true;
			awaitingFullWakeSinceSleep = true;
			displayOnFirstObservedAt = null;
		}
	}

	public bool NoteDisplay(bool isOn)
	{
		bool isDisplayOn;
		using (gate.EnterScope())
		{
			isDisplayOn = IsDisplayOn;
			IsDisplayOn = isOn;
		}
		if (!isOn)
		{
			if (isDisplayOn && DisplayOffMeansStandby)
			{
				NoteSleeping();
			}
			return false;
		}
		using (gate.EnterScope())
		{
			bool result = awaitingDisplaySinceSleep;
			awaitingDisplaySinceSleep = false;
			return result;
		}
	}

	public bool ConsumeFullWake()
	{
		using (gate.EnterScope())
		{
			if (!awaitingFullWakeSinceSleep)
			{
				return false;
			}
			awaitingFullWakeSinceSleep = false;
			displayOnFirstObservedAt = null;
			return true;
		}
	}

	public bool ShouldHoldForFullWake(DateTimeOffset now, TimeSpan failOpenAfter)
	{
		using (gate.EnterScope())
		{
			if (!awaitingFullWakeSinceSleep)
			{
				return false;
			}
			if (!IsDisplayOn)
			{
				return true;
			}
			DateTimeOffset? dateTimeOffset = displayOnFirstObservedAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (now - valueOrDefault < failOpenAfter)
				{
					return true;
				}
				awaitingFullWakeSinceSleep = false;
				displayOnFirstObservedAt = null;
				return false;
			}
			displayOnFirstObservedAt = now;
			return true;
		}
	}
}
