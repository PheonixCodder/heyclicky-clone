using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;

namespace HeyClicky.App.Design;

public sealed class SpringValue(double initial, double epsilon)
{
	private double from = initial;

	private double velocity;

	private double to = initial;

	private double startsAt;

	private double endsAt;

	private SwiftSpring spring = new SwiftSpring(0.42, 0.86);

	public double Target => to;

	public double ValueAt(double time)
	{
		double num = time - startsAt;
		if (num <= 0.0)
		{
			return from;
		}
		if (time >= endsAt)
		{
			return to;
		}
		var (num2, num3, _, _) = SpringCurve.Basis(spring, num);
		return to + (from - to) * num2 + velocity * num3;
	}

	private double VelocityAt(double time)
	{
		double num = time - startsAt;
		if (num <= 0.0 || time >= endsAt)
		{
			return 0.0;
		}
		(double A, double B, double DA, double DB) tuple = SpringCurve.Basis(spring, num);
		double item = tuple.DA;
		double item2 = tuple.DB;
		return (from - to) * item + velocity * item2;
	}

	public KeyFrameAnimation? To(Compositor c, double target, SwiftSpring? next)
	{
		return Move(target, next)?.Animation(c);
	}

	public SpringMove? Move(double target, SwiftSpring? next)
	{
		double now = SpringCurve.Now;
		if (Math.Abs(target - to) < epsilon && (!next.HasValue || now >= endsAt || next == spring))
		{
			return null;
		}
		if (next.HasValue)
		{
			SwiftSpring valueOrDefault = next.GetValueOrDefault();
			double num = ValueAt(now);
			double speed = VelocityAt(now);
			from = num;
			velocity = speed;
			to = target;
			spring = valueOrDefault;
			startsAt = now;
			double num2 = Math.Max(0.016, SpringCurve.SettleSeconds(valueOrDefault, num - target, speed, epsilon));
			endsAt = now + num2;
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			foreach (var (num3, num4) in SpringCurve.Samples(num2))
			{
				list.Add(num3);
				list2.Add((num3 >= 1f) ? ((float)target) : ((float)ValueAt(now + num4)));
			}
			return new SpringMove(list.ToArray(), list2.ToArray(), TimeSpan.FromSeconds(num2));
		}
		from = (to = target);
		velocity = 0.0;
		startsAt = (endsAt = now);
		return new SpringMove(new float[2] { 0f, 1f }, new float[2]
		{
			(float)target,
			(float)target
		}, TimeSpan.FromMilliseconds(1L, 0L));
	}
}
