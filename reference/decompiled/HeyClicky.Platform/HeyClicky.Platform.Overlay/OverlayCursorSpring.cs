using System;

namespace HeyClicky.Platform.Overlay;

public readonly struct OverlayCursorSpring
{
	public readonly double Response;

	public readonly double DampingFraction;

	public static readonly OverlayCursorSpring CursorFollow = new OverlayCursorSpring(0.2, 0.6);

	public static readonly OverlayCursorSpring BubbleScale = new OverlayCursorSpring(0.4, 0.6);

	public double AngularFrequency => Math.PI * 2.0 / Response;

	public double Stiffness => AngularFrequency * AngularFrequency;

	private double DecayRate => DampingFraction * AngularFrequency;

	private double DampedFrequency => AngularFrequency * Math.Sqrt(1.0 - DampingFraction * DampingFraction);

	public OverlayCursorSpring(double response, double dampingFraction)
	{
		Response = response;
		DampingFraction = dampingFraction;
	}

	public (double Value, double Velocity) ReleasedDisplacement(double elapsedSeconds)
	{
		double num = Math.Max(0.0, elapsedSeconds);
		double decayRate = DecayRate;
		if (DampingFraction < 1.0)
		{
			double dampedFrequency = DampedFrequency;
			double num2 = Math.Exp((0.0 - decayRate) * num);
			double num3 = Math.Cos(dampedFrequency * num);
			double num4 = Math.Sin(dampedFrequency * num);
			return (Value: num2 * (num3 + decayRate / dampedFrequency * num4), Velocity: (0.0 - num2) * num4 * Stiffness / dampedFrequency);
		}
		if (DampingFraction == 1.0)
		{
			double num5 = Math.Exp((0.0 - decayRate) * num);
			return (Value: num5 * (1.0 + decayRate * num), Velocity: (0.0 - Stiffness) * num * num5);
		}
		double num6 = AngularFrequency * Math.Sqrt(DampingFraction * DampingFraction - 1.0);
		double num7 = 0.0 - decayRate - num6;
		double num8 = 0.0 - decayRate + num6;
		double num9 = num8 / (num8 - num7);
		double num10 = (0.0 - num7) / (num8 - num7);
		return (Value: num9 * Math.Exp(num7 * num) + num10 * Math.Exp(num8 * num), Velocity: num9 * num7 * Math.Exp(num7 * num) + num10 * num8 * Math.Exp(num8 * num));
	}

	public (double Value, double Velocity) ReleasedVelocity(double elapsedSeconds)
	{
		double num = Math.Max(0.0, elapsedSeconds);
		double decayRate = DecayRate;
		if (DampingFraction < 1.0)
		{
			double dampedFrequency = DampedFrequency;
			double num2 = Math.Exp((0.0 - decayRate) * num);
			double num3 = Math.Cos(dampedFrequency * num);
			double num4 = Math.Sin(dampedFrequency * num);
			return (Value: num2 * num4 / dampedFrequency, Velocity: num2 * (num3 - decayRate / dampedFrequency * num4));
		}
		if (DampingFraction == 1.0)
		{
			double num5 = Math.Exp((0.0 - decayRate) * num);
			return (Value: num * num5, Velocity: num5 * (1.0 - decayRate * num));
		}
		double num6 = AngularFrequency * Math.Sqrt(DampingFraction * DampingFraction - 1.0);
		double num7 = 0.0 - decayRate - num6;
		double num8 = 0.0 - decayRate + num6;
		double num9 = 1.0 / (num7 - num8);
		return (Value: num9 * (Math.Exp(num7 * num) - Math.Exp(num8 * num)), Velocity: num9 * (num7 * Math.Exp(num7 * num) - num8 * Math.Exp(num8 * num)));
	}
}
