using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.UI;

public static class Anim
{
	private const double ReducedFadeSeconds = 0.12;

	private static Compositor C => CompositionTarget.GetCompositorForCurrentThread();

	public static bool Instant { get; set; }

	public static bool ReduceMotion => MotionPreference.Reduced;

	private static bool Jump
	{
		get
		{
			if (!Instant)
			{
				return ReduceMotion;
			}
			return true;
		}
	}

	public static void ScaleSpring(UIElement e, float to, SwiftSpring spring, Vector3? center = null)
	{
		if (Jump)
		{
			if (center.HasValue)
			{
				Vector3 valueOrDefault = center.GetValueOrDefault();
				e.CenterPoint = valueOrDefault;
			}
			Stop(e, "Scale");
			e.Scale = new Vector3(to, to, 1f);
		}
		else
		{
			if (center.HasValue)
			{
				Vector3 valueOrDefault2 = center.GetValueOrDefault();
				e.CenterPoint = valueOrDefault2;
			}
			e.StartAnimation(Target(spring.Vector3(C, new Vector3(to, to, 1f)), "Scale"));
		}
	}

	public static void OpacitySpring(UIElement e, float to, SwiftSpring spring)
	{
		if (Instant)
		{
			Stop(e, "Opacity");
			e.Opacity = to;
		}
		else if (ReduceMotion)
		{
			Opacity(e, to, 0.12);
		}
		else
		{
			e.StartAnimation(Target(spring.Scalar(C, to), "Opacity"));
		}
	}

	public static void TranslationSpring(UIElement e, Vector3 to, SwiftSpring spring)
	{
		if (Jump)
		{
			Stop(e, "Translation");
			e.Translation = to;
		}
		else
		{
			e.StartAnimation(Target(spring.Vector3(C, to), "Translation"));
		}
	}

	public static void Scale(UIElement e, float to, double seconds, CompositionEasingFunction? easing = null, Vector3? center = null)
	{
		if (Jump)
		{
			if (center.HasValue)
			{
				Vector3 valueOrDefault = center.GetValueOrDefault();
				e.CenterPoint = valueOrDefault;
			}
			Stop(e, "Scale");
			e.Scale = new Vector3(to, to, 1f);
			return;
		}
		if (center.HasValue)
		{
			Vector3 valueOrDefault2 = center.GetValueOrDefault();
			e.CenterPoint = valueOrDefault2;
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(to, to, 1f), easing ?? Easing.EaseOut(C));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		vector3KeyFrameAnimation.Target = "Scale";
		e.StartAnimation(vector3KeyFrameAnimation);
	}

	public static void Opacity(UIElement e, float to, double seconds, CompositionEasingFunction? easing = null, double delay = 0.0)
	{
		if (Instant)
		{
			Stop(e, "Opacity");
			e.Opacity = to;
			return;
		}
		if (ReduceMotion)
		{
			seconds = Math.Min(seconds, 0.12);
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, easing ?? Easing.EaseOut(C));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(seconds, 0.001));
		if (delay > 0.0)
		{
			scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(delay);
			scalarKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		}
		scalarKeyFrameAnimation.Target = "Opacity";
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	public static void ReducedMotionFade(UIElement e, float to, double seconds)
	{
		if (Instant)
		{
			Stop(e, "Opacity");
			e.Opacity = to;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, Easing.EaseOut(C));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(seconds, 0.001));
		scalarKeyFrameAnimation.Target = "Opacity";
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	public static void Translate(UIElement e, Vector3 to, double seconds, CompositionEasingFunction? easing = null)
	{
		if (Jump)
		{
			Stop(e, "Translation");
			e.Translation = to;
			return;
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(1f, to, easing ?? Easing.EaseOut(C));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		vector3KeyFrameAnimation.Target = "Translation";
		e.StartAnimation(vector3KeyFrameAnimation);
	}

	public static void Rotate(UIElement e, float degrees, double seconds, CompositionEasingFunction? easing = null)
	{
		if (Jump)
		{
			Stop(e, "Rotation");
			e.Rotation = degrees;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, degrees, easing ?? Easing.EaseOut(C));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		scalarKeyFrameAnimation.Target = "Rotation";
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	public static void LoopScalar(UIElement e, string target, double period, Func<double, float> f, int samples = 48, double phaseOffset = 0.0)
	{
		if (ReduceMotion && !Instant)
		{
			float num = f(phaseOffset);
			Stop(e, target);
			if (target == "Opacity")
			{
				e.Opacity = num;
			}
			else if (target == "Rotation")
			{
				e.Rotation = num;
			}
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		LinearEasingFunction easingFunction = C.CreateLinearEasingFunction();
		for (int i = 0; i <= samples; i++)
		{
			double num2 = (double)i / (double)samples;
			scalarKeyFrameAnimation.InsertKeyFrame((float)num2, f(num2 * period + phaseOffset), easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(period);
		scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
		scalarKeyFrameAnimation.Target = target;
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	public static void LoopVector3(UIElement e, string target, double period, Func<double, Vector3> f, int samples = 48, double phaseOffset = 0.0)
	{
		if (ReduceMotion && !Instant)
		{
			Vector3 vector = f(phaseOffset);
			Stop(e, target);
			if (target == "Translation")
			{
				e.Translation = vector;
			}
			else if (target == "Scale")
			{
				e.Scale = vector;
			}
			return;
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		LinearEasingFunction easingFunction = C.CreateLinearEasingFunction();
		for (int i = 0; i <= samples; i++)
		{
			double num = (double)i / (double)samples;
			vector3KeyFrameAnimation.InsertKeyFrame((float)num, f(num * period + phaseOffset), easingFunction);
		}
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(period);
		vector3KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
		vector3KeyFrameAnimation.Target = target;
		e.StartAnimation(vector3KeyFrameAnimation);
	}

	public static void KeyframesScalar(UIElement e, string target, IReadOnlyList<(float value, double seconds)> frames, Func<float, float>? map = null)
	{
		if (Jump && frames.Count > 0)
		{
			float item = frames[frames.Count - 1].value;
			float num = map?.Invoke(item) ?? item;
			Stop(e, target);
			if (target == "Opacity")
			{
				e.Opacity = num;
			}
			else if (target == "Rotation")
			{
				e.Rotation = num;
			}
			return;
		}
		double num2 = frames.Sum(((float value, double seconds) f) => f.seconds);
		if (num2 <= 0.0)
		{
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		CubicBezierEasingFunction easingFunction = Easing.EaseInOut(C);
		double num3 = 0.0;
		foreach (var frame in frames)
		{
			float item2 = frame.value;
			double item3 = frame.seconds;
			num3 += item3;
			scalarKeyFrameAnimation.InsertKeyFrame((float)(num3 / num2), map?.Invoke(item2) ?? item2, easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(num2);
		scalarKeyFrameAnimation.Target = target;
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	public static void Stop(UIElement e, string target)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.Target = target;
		try
		{
			e.StopAnimation(scalarKeyFrameAnimation);
		}
		catch
		{
		}
	}

	private static T Target<T>(T a, string target) where T : CompositionAnimation
	{
		a.Target = target;
		return a;
	}

	public static void ScaleUpFrom(UIElement e, float from, SwiftSpring spring, Vector3? center = null)
	{
		if (center.HasValue)
		{
			Vector3 valueOrDefault = center.GetValueOrDefault();
			e.CenterPoint = valueOrDefault;
		}
		if (Jump)
		{
			Stop(e, "Scale");
			e.Scale = Vector3.One;
		}
		else
		{
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = SpringCurve.Scale(C, spring, from, 1f);
			vector3KeyFrameAnimation.Target = "Scale";
			e.StartAnimation(vector3KeyFrameAnimation);
		}
	}

	public static void ContentIn(FrameworkElement e, SwiftSpring spring)
	{
		e.Opacity = 0.0;
		if (e.ActualWidth > 0.0)
		{
			Run();
		}
		else
		{
			e.SizeChanged += OnSize;
		}
		void OnSize(object s, SizeChangedEventArgs a)
		{
			e.SizeChanged -= OnSize;
			Run();
		}
		void Run()
		{
			e.CenterPoint = new Vector3((float)(e.ActualWidth / 2.0), 0f, 0f);
			ScaleUpFrom(e, 0.92f, spring, null);
			OpacitySpring(e, 1f, spring);
		}
	}

	public static void ContentOut(FrameworkElement e, SwiftSpring spring)
	{
		e.CenterPoint = new Vector3((float)(e.ActualWidth / 2.0), 0f, 0f);
		ScaleSpring(e, 0.92f, spring, null);
		OpacitySpring(e, 0f, spring);
	}

	public static void ContentBack(FrameworkElement e, SwiftSpring spring)
	{
		e.CenterPoint = new Vector3((float)(e.ActualWidth / 2.0), 0f, 0f);
		ScaleSpring(e, 1f, spring, null);
		OpacitySpring(e, 1f, spring);
	}
}
