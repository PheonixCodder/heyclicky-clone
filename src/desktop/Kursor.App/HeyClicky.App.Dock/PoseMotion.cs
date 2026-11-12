using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Dock;

internal sealed class PoseMotion
{
	private readonly UIElement element;

	private readonly Visual? scaler;

	private readonly Vector2 anchor;

	private double raster = 1.0;

	private DeckPose from;

	private DeckPose velocity;

	private DeckPose to;

	private SwiftSpring spring = new SwiftSpring(0.36, 0.86);

	private double startsAt;

	private double endsAt;

	private bool hasPose;

	private static readonly DeckPose Epsilon = new DeckPose(0.02, 0.02, 0.0002, 0.005, 0.001);

	private DispatcherQueueTimer? settleTimer;

	private static CompositionEasingFunction? linear;

	public static double Now => PointerEvaluationDriver.Now;

	public DeckPose Target => to;

	public bool HasPose => hasPose;

	public bool IsMoving
	{
		get
		{
			if (hasPose)
			{
				return Now < endsAt;
			}
			return false;
		}
	}

	public PoseMotion(UIElement element, Visual? scaler, Vector2 anchor)
	{
		this.element = element;
		this.scaler = scaler;
		this.anchor = anchor;
	}

	public DeckPose PoseAt(double time)
	{
		double num = time - startsAt;
		if (!hasPose || num <= 0.0)
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

	private DeckPose VelocityAt(double time)
	{
		double num = time - startsAt;
		if (!hasPose || num <= 0.0 || time >= endsAt)
		{
			return DeckPose.Zero;
		}
		(double A, double B, double DA, double DB) tuple = SpringCurve.Basis(spring, num);
		double item = tuple.DA;
		double item2 = tuple.DB;
		return (from - to) * item + velocity * item2;
	}

	public void SetRaster(double scale)
	{
		if (scale <= 0.0 || Math.Abs(scale - raster) < 1E-09)
		{
			return;
		}
		raster = scale;
		element.Scale = new Vector3((float)scale, (float)scale, 1f);
		if (hasPose)
		{
			double now = Now;
			if (now < endsAt)
			{
				Play(PoseAt(now), VelocityAt(now), to, spring, Math.Max(0.0, startsAt - now), jump: false, rescale: true);
			}
			else
			{
				Play(to, DeckPose.Zero, to, spring, 0.0, jump: true);
			}
		}
	}

	public void Jump(DeckPose target)
	{
		Play(target, DeckPose.Zero, target, spring, 0.0, jump: true);
	}

	public void SpringTo(DeckPose target, SwiftSpring next, double delay = 0.0)
	{
		if (!hasPose)
		{
			Jump(target);
			return;
		}
		double now = Now;
		DeckPose deckPose = PoseAt(now);
		DeckPose deckPose2 = VelocityAt(now);
		if (!(target == to) || !(deckPose2 == DeckPose.Zero) || !(deckPose == to))
		{
			Play(deckPose, deckPose2, target, next, delay);
		}
	}

	private void Play(DeckPose start, DeckPose speed, DeckPose target, SwiftSpring next, double delay, bool jump = false, bool rescale = false)
	{
		double now = Now;
		DeckPose previous = (hasPose ? PoseAt(now) : start);
		from = start;
		velocity = speed;
		to = target;
		spring = next;
		hasPose = true;
		double num = (jump ? 0.0 : Duration(start - target, speed, next));
		startsAt = now + delay;
		endsAt = startsAt + num;
		if (to.Opacity > 0.0 && element.Opacity <= 0.0)
		{
			element.Opacity = 1.0;
		}
		Emit(previous, num, delay, rescale);
		if ((object)settleTimer == null)
		{
			settleTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			settleTimer.IsRepeating = false;
			settleTimer.Tick += delegate
			{
				if (!(Now < endsAt))
				{
					element.Translation = Translation(to);
					element.Rotation = (float)to.Degrees;
					element.Opacity = Math.Clamp(to.Opacity, 0.0, 1.0);
				}
			};
		}
		settleTimer.Stop();
		settleTimer.Interval = TimeSpan.FromSeconds(delay + num + 0.05);
		settleTimer.Start();
	}

	private static double Duration(DeckPose offset, DeckPose speed, SwiftSpring spring)
	{
		double longest = 0.0;
		Take(offset.X, speed.X, Epsilon.X);
		Take(offset.Y, speed.Y, Epsilon.Y);
		Take(offset.Scale, speed.Scale, Epsilon.Scale);
		Take(offset.Degrees, speed.Degrees, Epsilon.Degrees);
		Take(offset.Opacity, speed.Opacity, Epsilon.Opacity);
		return longest;
		void Take(double distance, double rate, double epsilon)
		{
			longest = Math.Max(longest, SpringCurve.SettleSeconds(spring, distance, rate, epsilon));
		}
	}

	private Vector3 Translation(DeckPose p)
	{
		return new Vector3((float)(p.X - (double)anchor.X), (float)(p.Y - (double)anchor.Y), 0f);
	}

	private void Emit(DeckPose previous, double duration, double delay, bool rescale)
	{
		Compositor compositor = Ui.Compositor;
		if ((object)linear == null)
		{
			linear = compositor.CreateLinearEasingFunction();
		}
		bool flag = duration <= 0.0;
		int num = (flag ? 1 : Math.Clamp((int)Math.Ceiling(duration * 70.0), 6, 48));
		double[] array = new double[num + 1];
		DeckPose[] array2 = new DeckPose[num + 1];
		for (int i = 0; i <= num; i++)
		{
			double num2 = (double)i / (double)num;
			array[i] = (flag ? num2 : Math.Pow(num2, 1.6));
			array2[i] = ((flag || i == num) ? to : PoseAt(startsAt + array[i] * duration));
		}
		if (flag)
		{
			array2[0] = to;
		}
		TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(duration, 0.001));
		TimeSpan timeSpan2 = TimeSpan.FromSeconds(Math.Max(0.0, delay));
		if (flag || Moves((DeckPose p) => p.X, Epsilon.X) || Moves((DeckPose p) => p.Y, Epsilon.Y))
		{
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			for (int j = 0; j <= num; j++)
			{
				vector3KeyFrameAnimation.InsertKeyFrame((float)array[j], Translation(array2[j]), linear);
			}
			Start(vector3KeyFrameAnimation, "Translation", timeSpan, timeSpan2);
		}
		if (flag || Moves((DeckPose p) => p.Degrees, Epsilon.Degrees))
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
			for (int k = 0; k <= num; k++)
			{
				scalarKeyFrameAnimation.InsertKeyFrame((float)array[k], (float)array2[k].Degrees, linear);
			}
			Start(scalarKeyFrameAnimation, "Rotation", timeSpan, timeSpan2);
		}
		if (flag || Moves((DeckPose p) => p.Opacity, Epsilon.Opacity))
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = compositor.CreateScalarKeyFrameAnimation();
			for (int l = 0; l <= num; l++)
			{
				scalarKeyFrameAnimation2.InsertKeyFrame((float)array[l], (float)Math.Clamp(array2[l].Opacity, 0.0, 1.0), linear);
			}
			Start(scalarKeyFrameAnimation2, "Opacity", timeSpan, timeSpan2);
		}
		if ((object)scaler != null && (flag || rescale || Moves((DeckPose p) => p.Scale, Epsilon.Scale)))
		{
			Vector3KeyFrameAnimation vector3KeyFrameAnimation2 = compositor.CreateVector3KeyFrameAnimation();
			for (int m = 0; m <= num; m++)
			{
				float num3 = (float)(array2[m].Scale / raster);
				vector3KeyFrameAnimation2.InsertKeyFrame((float)array[m], new Vector3(num3, num3, 1f), linear);
			}
			vector3KeyFrameAnimation2.Duration = timeSpan;
			vector3KeyFrameAnimation2.DelayTime = timeSpan2;
			vector3KeyFrameAnimation2.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
			scaler.StartAnimation("Scale", vector3KeyFrameAnimation2);
		}
		bool Moves(Func<DeckPose, double> channel, double epsilon)
		{
			if (!(Math.Abs(channel(previous) - channel(to)) > epsilon) && !(Math.Abs(channel(from) - channel(to)) > epsilon))
			{
				return channel(velocity) != 0.0;
			}
			return true;
		}
	}

	private void Start(KeyFrameAnimation a, string target, TimeSpan span, TimeSpan wait)
	{
		a.Duration = span;
		a.DelayTime = wait;
		a.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		a.Target = target;
		element.StartAnimation(a);
	}
}
