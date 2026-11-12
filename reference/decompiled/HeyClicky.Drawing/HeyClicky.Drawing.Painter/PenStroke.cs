using System;
using System.Numerics;

namespace HeyClicky.Drawing.Painter;

internal sealed class PenStroke
{
	public required Vector2[] Points;

	public required double BeginTime;

	public required double Duration;

	public required Vector4 Timing;

	public Func<Vector2[]>? LateResolver;

	public PenStroke Resolved()
	{
		if (LateResolver == null)
		{
			return this;
		}
		Vector2[] array = LateResolver();
		return new PenStroke
		{
			Points = ((array.Length != 0) ? array : Points),
			BeginTime = BeginTime,
			Duration = Duration,
			Timing = Timing
		};
	}
}
