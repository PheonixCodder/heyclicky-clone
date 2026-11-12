using System;

namespace HeyClicky.Core.Drive;

public sealed class ClickyDriveUserMouse
{
	public readonly record struct Verdict(bool TakeOver, bool Nudge);

	public static readonly TimeSpan RestTime = TimeSpan.FromSeconds(1L);

	public static readonly TimeSpan RunGap = TimeSpan.FromSeconds(0.5);

	public static readonly TimeSpan SustainedTime = TimeSpan.FromSeconds(3L);

	public static readonly TimeSpan WaitCap = TimeSpan.FromSeconds(8L);

	public static readonly TimeSpan HintLinger = TimeSpan.FromSeconds(1.2);

	public const double SustainedDistance = 150.0;

	public const double HintDistance = 24.0;

	private DateTimeOffset? lastMoveAt;

	private (double X, double Y)? lastPoint;

	private DateTimeOffset? runStartedAt;

	private double runDistance;

	private bool nudgedThisRun;

	public Verdict NoteMove(double x, double y, DateTimeOffset now, bool clickyHasActed)
	{
		DateTimeOffset? dateTimeOffset = lastMoveAt;
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			if (now - valueOrDefault <= RunGap)
			{
				(double, double)? tuple = lastPoint;
				if (tuple.HasValue)
				{
					(double, double) valueOrDefault2 = tuple.GetValueOrDefault();
					dateTimeOffset = runStartedAt;
					if (dateTimeOffset.HasValue)
					{
						runDistance += Math.Sqrt(Math.Pow(x - valueOrDefault2.Item1, 2.0) + Math.Pow(y - valueOrDefault2.Item2, 2.0));
						goto IL_00bc;
					}
				}
			}
		}
		runStartedAt = now;
		runDistance = 0.0;
		nudgedThisRun = false;
		goto IL_00bc;
		IL_00bc:
		lastMoveAt = now;
		lastPoint = (x, y);
		if (clickyHasActed)
		{
			dateTimeOffset = runStartedAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault3 = dateTimeOffset.GetValueOrDefault();
				if (now - valueOrDefault3 >= SustainedTime && runDistance >= 150.0)
				{
					return new Verdict(TakeOver: true, Nudge: false);
				}
			}
		}
		bool flag = !nudgedThisRun && runDistance >= 24.0;
		if (flag)
		{
			nudgedThisRun = true;
		}
		return new Verdict(TakeOver: false, flag);
	}

	public bool IsResting(DateTimeOffset now)
	{
		DateTimeOffset? dateTimeOffset = lastMoveAt;
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			return now - valueOrDefault >= RestTime;
		}
		return true;
	}

	public void ForgetRun()
	{
		lastPoint = null;
		runStartedAt = null;
		runDistance = 0.0;
		nudgedThisRun = false;
	}

	public void Reset()
	{
		ForgetRun();
		lastMoveAt = null;
	}
}
