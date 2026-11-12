using System;

namespace HeyClicky.App.Onboarding;

public sealed class SpringAxis
{
	private readonly double omega;

	private readonly double zeta;

	public double Value { get; private set; }

	public double Velocity { get; private set; }

	public double Target { get; set; }

	public bool IsSettled
	{
		get
		{
			if (Math.Abs(Target - Value) < 0.05)
			{
				return Math.Abs(Velocity) < 0.5;
			}
			return false;
		}
	}

	public SpringAxis(double response, double dampingFraction, double value)
	{
		omega = Math.PI * 2.0 / response;
		zeta = dampingFraction;
		Value = (Target = value);
	}

	public void Step(double seconds)
	{
		if (!(seconds <= 0.0))
		{
			double num = Value - Target;
			double velocity = Velocity;
			double num6;
			double velocity2;
			if (zeta < 1.0)
			{
				double num2 = omega * Math.Sqrt(1.0 - zeta * zeta);
				double num3 = Math.Exp((0.0 - zeta) * omega * seconds);
				double num4 = Math.Cos(num2 * seconds);
				double num5 = Math.Sin(num2 * seconds);
				num6 = num3 * (num * num4 + (velocity + zeta * omega * num) / num2 * num5);
				velocity2 = num3 * (velocity * num4 - (zeta * omega * velocity + omega * omega * num) / num2 * num5);
			}
			else if (zeta == 1.0)
			{
				double num7 = Math.Exp((0.0 - omega) * seconds);
				double num8 = velocity + omega * num;
				num6 = num7 * (num + num8 * seconds);
				velocity2 = num7 * (velocity - omega * num8 * seconds);
			}
			else
			{
				double num9 = Math.Sqrt(zeta * zeta - 1.0);
				double num10 = (0.0 - omega) * (zeta - num9);
				double num11 = (0.0 - omega) * (zeta + num9);
				double num12 = (velocity - num10 * num) / (num11 - num10);
				double num13 = num - num12;
				num6 = num13 * Math.Exp(num10 * seconds) + num12 * Math.Exp(num11 * seconds);
				velocity2 = num13 * num10 * Math.Exp(num10 * seconds) + num12 * num11 * Math.Exp(num11 * seconds);
			}
			Value = Target + num6;
			Velocity = velocity2;
		}
	}

	public void Settle()
	{
		Value = Target;
		Velocity = 0.0;
	}

	public static double UnitStep(double response, double dampingFraction, double t)
	{
		if (t <= 0.0)
		{
			return 0.0;
		}
		double num = Math.PI * 2.0 / response;
		if (dampingFraction < 1.0)
		{
			double num2 = num * Math.Sqrt(1.0 - dampingFraction * dampingFraction);
			return 1.0 - Math.Exp((0.0 - dampingFraction) * num * t) * (Math.Cos(num2 * t) + dampingFraction * num / num2 * Math.Sin(num2 * t));
		}
		return 1.0 - Math.Exp((0.0 - num) * t) * (1.0 + num * t);
	}
}
