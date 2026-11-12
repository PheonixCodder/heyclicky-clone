using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using HeyClicky.Platform.Overlay;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace HeyClicky.App.Onboarding;

internal sealed class CompositorSpringMotion
{
	private const int Groups = 8;

	private const int PerGroup = 8;

	private const int Slots = 64;

	private const int Samples = 48;

	private readonly Compositor compositor;

	private readonly CompositionPropertySet props;

	private readonly CompositionPropertySet sums;

	private readonly double[] slotFreeAt = new double[64];

	private readonly Dictionary<(double, double), (ScalarKeyFrameAnimation Curve, double Seconds)> curves = new Dictionary<(double, double), (ScalarKeyFrameAnimation, double)>();

	private int nextSlot;

	private Vector3 rest;

	public int Moves { get; private set; }

	public int SlotWaits { get; private set; }

	public void ResetCounts()
	{
		Moves = 0;
		SlotWaits = 0;
	}

	public CompositorSpringMotion(UIElement element)
	{
		ElementCompositionPreview.SetIsTranslationEnabled(element, value: true);
		Visual elementVisual = ElementCompositionPreview.GetElementVisual(element);
		compositor = elementVisual.Compositor;
		props = compositor.CreatePropertySet();
		sums = compositor.CreatePropertySet();
		props.InsertVector3("Rest", Vector3.Zero);
		for (int i = 0; i < 64; i++)
		{
			props.InsertVector3("D" + i, Vector3.Zero);
			props.InsertScalar("G" + i, 0f);
		}
		StringBuilder stringBuilder = new StringBuilder("p.Rest");
		for (int j = 0; j < 8; j++)
		{
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int k = 0; k < 8; k++)
			{
				int value = j * 8 + k;
				if (k > 0)
				{
					stringBuilder2.Append('+');
				}
				stringBuilder2.Append("p.D").Append(value).Append("*p.G")
					.Append(value);
			}
			sums.InsertVector3("S" + j, Vector3.Zero);
			ExpressionAnimation expressionAnimation = compositor.CreateExpressionAnimation(stringBuilder2.ToString());
			expressionAnimation.SetReferenceParameter("p", props);
			sums.StartAnimation("S" + j, expressionAnimation);
			stringBuilder.Append("+s.S").Append(j);
		}
		ExpressionAnimation expressionAnimation2 = compositor.CreateExpressionAnimation(stringBuilder.ToString());
		expressionAnimation2.SetReferenceParameter("p", props);
		expressionAnimation2.SetReferenceParameter("s", sums);
		elementVisual.StartAnimation("Translation", expressionAnimation2);
	}

	public void Snap(Vector3 to)
	{
		for (int i = 0; i < 64; i++)
		{
			props.StopAnimation("G" + i);
			props.InsertScalar("G" + i, 0f);
			props.InsertVector3("D" + i, Vector3.Zero);
			slotFreeAt[i] = 0.0;
		}
		rest = to;
		props.InsertVector3("Rest", rest);
	}

	public void Jump(Vector3 delta)
	{
		rest += delta;
		props.InsertVector3("Rest", rest);
	}

	public bool Move(Vector3 delta, OverlayCursorSpring spring, double now)
	{
		if (delta.LengthSquared() < 1E-06f)
		{
			return true;
		}
		int num = -1;
		for (int i = 0; i < 64; i++)
		{
			int num2 = (nextSlot + i) % 64;
			if (slotFreeAt[num2] <= now)
			{
				num = num2;
				break;
			}
		}
		if (num < 0)
		{
			SlotWaits++;
			return false;
		}
		nextSlot = (num + 1) % 64;
		var (animation, num3) = Curve(spring);
		props.InsertVector3("D" + num, -delta);
		props.InsertScalar("G" + num, 1f);
		props.StartAnimation("G" + num, animation);
		slotFreeAt[num] = now + num3 + 0.03;
		rest += delta;
		props.InsertVector3("Rest", rest);
		Moves++;
		return true;
	}

	private (ScalarKeyFrameAnimation, double) Curve(OverlayCursorSpring spring)
	{
		(double, double) key = (spring.Response, spring.DampingFraction);
		if (curves.TryGetValue(key, out (ScalarKeyFrameAnimation, double) value))
		{
			return value;
		}
		double num = Math.PI * 2.0 / spring.Response;
		double dampingFraction = spring.DampingFraction;
		double num2 = ((dampingFraction < 1.0) ? (dampingFraction * num) : (num * (dampingFraction - Math.Sqrt(dampingFraction * dampingFraction - 1.0))));
		double num3 = Math.Clamp(Math.Log(40000.0) / num2, 0.2, 1.2);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(num3);
		LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
		for (int i = 0; i <= 48; i++)
		{
			float num4 = (float)i / 48f;
			float value2 = ((i == 48) ? 0f : ((float)spring.ReleasedDisplacement((double)num4 * num3).Value));
			scalarKeyFrameAnimation.InsertKeyFrame(num4, value2, easingFunction);
		}
		curves[key] = (scalarKeyFrameAnimation, num3);
		return (scalarKeyFrameAnimation, num3);
	}
}
