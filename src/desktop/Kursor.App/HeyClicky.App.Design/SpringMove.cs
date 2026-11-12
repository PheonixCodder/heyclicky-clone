using System;
using Microsoft.UI.Composition;
using Windows.UI.Composition;

namespace HeyClicky.App.Design;

public sealed record SpringMove(float[] Progress, float[] Values, TimeSpan Duration)
{
	public Microsoft.UI.Composition.ScalarKeyFrameAnimation Animation(Microsoft.UI.Composition.Compositor c)
	{
		Microsoft.UI.Composition.LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		Microsoft.UI.Composition.ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		for (int i = 0; i < Progress.Length; i++)
		{
			scalarKeyFrameAnimation.InsertKeyFrame(Progress[i], Values[i], easingFunction);
		}
		scalarKeyFrameAnimation.Duration = Duration;
		return scalarKeyFrameAnimation;
	}

	public Windows.UI.Composition.ScalarKeyFrameAnimation Animation(Windows.UI.Composition.Compositor c)
	{
		Windows.UI.Composition.LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		Windows.UI.Composition.ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		for (int i = 0; i < Progress.Length; i++)
		{
			scalarKeyFrameAnimation.InsertKeyFrame(Progress[i], Values[i], easingFunction);
		}
		scalarKeyFrameAnimation.Duration = Duration;
		return scalarKeyFrameAnimation;
	}
}
