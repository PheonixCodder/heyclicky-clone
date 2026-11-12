using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace HeyClicky.App.Design;

public readonly record struct SwiftSpring(double Response, double DampingFraction)
{
	public TimeSpan Period => TimeSpan.FromSeconds(Response / (Math.PI * 2.0));

	public float DampingRatio => (float)DampingFraction;

	public TimeSpan SettleTime
	{
		get
		{
			double num = Math.PI * 2.0 / Response;
			double num2 = Math.Max(0.05, Math.Min(1.0, DampingFraction));
			return TimeSpan.FromSeconds(Math.Min(2.0, 6.9 / (num2 * num)));
		}
	}

	public SpringScalarNaturalMotionAnimation Scalar(Compositor c, float to)
	{
		SpringScalarNaturalMotionAnimation springScalarNaturalMotionAnimation = c.CreateSpringScalarAnimation();
		springScalarNaturalMotionAnimation.DampingRatio = DampingRatio;
		springScalarNaturalMotionAnimation.Period = Period;
		springScalarNaturalMotionAnimation.FinalValue = to;
		springScalarNaturalMotionAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
		return springScalarNaturalMotionAnimation;
	}

	public SpringVector2NaturalMotionAnimation Vector2(Compositor c, Vector2 to)
	{
		SpringVector2NaturalMotionAnimation springVector2NaturalMotionAnimation = c.CreateSpringVector2Animation();
		springVector2NaturalMotionAnimation.DampingRatio = DampingRatio;
		springVector2NaturalMotionAnimation.Period = Period;
		springVector2NaturalMotionAnimation.FinalValue = to;
		springVector2NaturalMotionAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
		return springVector2NaturalMotionAnimation;
	}

	public SpringVector3NaturalMotionAnimation Vector3(Compositor c, Vector3 to)
	{
		SpringVector3NaturalMotionAnimation springVector3NaturalMotionAnimation = c.CreateSpringVector3Animation();
		springVector3NaturalMotionAnimation.DampingRatio = DampingRatio;
		springVector3NaturalMotionAnimation.Period = Period;
		springVector3NaturalMotionAnimation.FinalValue = to;
		springVector3NaturalMotionAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
		return springVector3NaturalMotionAnimation;
	}
}
