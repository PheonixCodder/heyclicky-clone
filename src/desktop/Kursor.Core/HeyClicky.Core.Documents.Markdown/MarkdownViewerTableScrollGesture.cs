using System;

namespace HeyClicky.Core.Documents.Markdown;

public struct MarkdownViewerTableScrollGesture
{
	public enum Axis
	{
		Horizontal,
		Vertical
	}

	[Flags]
	public enum Phase
	{
		None = 0,
		Began = 1,
		Stationary = 2,
		Changed = 4,
		Ended = 8,
		Cancelled = 0x10,
		MayBegin = 0x20
	}

	private Axis? gestureAxis;

	public Axis AxisFor(double deltaX, double deltaY, Phase phase, Phase momentumPhase)
	{
		if (phase.HasFlag(Phase.Began) || phase.HasFlag(Phase.MayBegin))
		{
			gestureAxis = null;
		}
		Axis axis = ((!(Math.Abs(deltaX) > Math.Abs(deltaY))) ? Axis.Vertical : Axis.Horizontal);
		if (phase == Phase.None && momentumPhase == Phase.None)
		{
			return axis;
		}
		Axis? axis2 = gestureAxis;
		if (!axis2.HasValue && (deltaX != 0.0 || deltaY != 0.0))
		{
			gestureAxis = axis;
		}
		Axis result = gestureAxis ?? axis;
		if (phase.HasFlag(Phase.Cancelled) || momentumPhase.HasFlag(Phase.Ended))
		{
			gestureAxis = null;
		}
		return result;
	}
}
