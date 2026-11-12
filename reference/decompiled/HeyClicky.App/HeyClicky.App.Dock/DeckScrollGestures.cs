using System;
using HeyClicky.Platform.Input;

namespace HeyClicky.App.Dock;

public sealed class DeckScrollGestures
{
	public enum Kind
	{
		None,
		Swipe,
		Coast,
		Wheel,
		Guess
	}

	private sealed class GuessedSwipes
	{
		private const double LiftGap = 0.06;

		private const double TurnDistance = 78.0;

		private const double SwipeSpeed = 1500.0;

		private double lastAt = double.NegativeInfinity;

		private double accumulated;

		private double speed;

		private double peak;

		private double trough;

		private bool turned;

		private int turnSign;

		public int Feed(double time, double signedDelta)
		{
			double num = time - lastAt;
			lastAt = time;
			double num2 = Math.Abs(signedDelta) / Math.Clamp(num, 0.004, 0.05);
			speed = ((num > 0.22) ? num2 : (speed + (num2 - speed) * 0.4));
			if (num > 0.22 || (turned && (num >= 0.06 || (signedDelta != 0.0 && Math.Sign(signedDelta) != turnSign) || (trough < 0.35 * peak && speed > 3.0 * trough && speed > 1500.0))))
			{
				accumulated = 0.0;
				turned = false;
			}
			if (turned)
			{
				if (speed > peak)
				{
					peak = (trough = speed);
				}
				else
				{
					trough = Math.Min(trough, speed);
				}
				return 0;
			}
			accumulated += signedDelta;
			if (Math.Abs(accumulated) < 78.0)
			{
				return 0;
			}
			turnSign = Math.Sign(accumulated);
			turned = true;
			accumulated = 0.0;
			peak = (trough = speed);
			if (turnSign <= 0)
			{
				return 1;
			}
			return -1;
		}
	}

	public const double QuietGap = 0.22;

	public const double SwipeTurnDistance = 40.0;

	public const double NotchDistance = 120.0;

	private const double Slack = 0.03;

	private readonly GuessedSwipes guess = new GuessedSwipes();

	private double lastAt = double.NegativeInfinity;

	private double accumulated;

	private long touch = -1L;

	private bool swipeTurned;

	public Kind Last { get; private set; }

	public long Touch => touch;

	public int Feed(double time, double signedDelta, TouchpadSnapshot pad)
	{
		double num = time - lastAt;
		lastAt = time;
		Kind last = Last;
		Kind last2;
		if (pad.Present && pad.Touch > 0 && time >= pad.DownAt - 0.03 && (pad.IsTouching || time <= pad.LiftedAt + 0.03))
		{
			last2 = Kind.Swipe;
		}
		else
		{
			bool flag = pad.Present;
			if (flag)
			{
				bool flag2 = (uint)(last - 1) <= 1u;
				flag = flag2;
			}
			last2 = ((flag && num <= 0.22) ? Kind.Coast : ((!pad.Present && signedDelta % 120.0 != 0.0) ? Kind.Guess : Kind.Wheel));
		}
		Last = last2;
		if (Last != last)
		{
			accumulated = 0.0;
		}
		switch (Last)
		{
		case Kind.Swipe:
			if (pad.Touch != touch)
			{
				touch = pad.Touch;
				accumulated = 0.0;
				swipeTurned = false;
			}
			if (swipeTurned)
			{
				return 0;
			}
			accumulated += signedDelta;
			if (Math.Abs(accumulated) < 40.0)
			{
				return 0;
			}
			swipeTurned = true;
			return Step();
		case Kind.Wheel:
			if (num > 0.22)
			{
				accumulated = 0.0;
			}
			accumulated += signedDelta;
			if (!(Math.Abs(accumulated) < 120.0))
			{
				return Step();
			}
			return 0;
		case Kind.Guess:
			return guess.Feed(time, signedDelta);
		default:
			return 0;
		}
	}

	private int Step()
	{
		int result = ((!(accumulated > 0.0)) ? 1 : (-1));
		accumulated = 0.0;
		return result;
	}
}
