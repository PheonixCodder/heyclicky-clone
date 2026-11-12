using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;

namespace HeyClicky.App.Design;

public static class SpringCurve
{
	[CompilerGenerated]
	private sealed class _003CSamples_003Ed__5 : IEnumerable<(float Progress, double Seconds)>, IEnumerable, IEnumerator<(float Progress, double Seconds)>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private (float Progress, double Seconds) _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private double duration;

		public double _003C_003E3__duration;

		private int _003Ccount_003E5__2;

		private int _003Ck_003E5__3;

		(float Progress, double Seconds) IEnumerator<(float Progress, double Seconds)>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CSamples_003Ed__5(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Ccount_003E5__2 = Math.Clamp((int)Math.Ceiling(duration * 70.0), 6, 48);
				_003Ck_003E5__3 = 0;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Ck_003E5__3++;
				break;
			}
			if (_003Ck_003E5__3 <= _003Ccount_003E5__2)
			{
				double num = Math.Pow((double)_003Ck_003E5__3 / (double)_003Ccount_003E5__2, 1.6);
				_003C_003E2__current = (Progress: (float)num, Seconds: num * duration);
				_003C_003E1__state = 1;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<(float Progress, double Seconds)> IEnumerable<(float Progress, double Seconds)>.GetEnumerator()
		{
			_003CSamples_003Ed__5 _003CSamples_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CSamples_003Ed__ = this;
			}
			else
			{
				_003CSamples_003Ed__ = new _003CSamples_003Ed__5(0);
			}
			_003CSamples_003Ed__.duration = _003C_003E3__duration;
			return _003CSamples_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<(float, double)>)this).GetEnumerator();
		}
	}

	public static double Now => (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

	public static (double A, double B, double DA, double DB) Basis(SwiftSpring spring, double t)
	{
		if (t <= 0.0)
		{
			return (A: 1.0, B: 0.0, DA: 0.0, DB: 1.0);
		}
		double num = Math.PI * 2.0 / spring.Response;
		double num2 = Math.Max(0.01, spring.DampingFraction);
		if (num2 < 0.999)
		{
			double num3 = num * Math.Sqrt(1.0 - num2 * num2);
			double num4 = Math.Exp((0.0 - num2) * num * t);
			double num5 = Math.Cos(num3 * t);
			double num6 = Math.Sin(num3 * t);
			return (A: num4 * (num5 + num2 * num / num3 * num6), B: num4 * num6 / num3, DA: (0.0 - num4) * num6 * num * num / num3, DB: num4 * (num5 - num2 * num / num3 * num6));
		}
		if (num2 <= 1.001)
		{
			double num7 = Math.Exp((0.0 - num) * t);
			return (A: num7 * (1.0 + num * t), B: num7 * t, DA: (0.0 - num) * num * t * num7, DB: num7 * (1.0 - num * t));
		}
		double num8 = Math.Sqrt(num2 * num2 - 1.0);
		double num9 = (0.0 - num) * (num2 - num8);
		double num10 = (0.0 - num) * (num2 + num8);
		double num11 = Math.Exp(num9 * t);
		double num12 = Math.Exp(num10 * t);
		double num13 = num10 - num9;
		double num14 = (0.0 - num9) / num13;
		double num15 = 1.0 - num14;
		double num16 = 1.0 / num13;
		double num17 = 0.0 - num16;
		return (A: num15 * num11 + num14 * num12, B: num17 * num11 + num16 * num12, DA: num15 * num9 * num11 + num14 * num10 * num12, DB: num17 * num9 * num11 + num16 * num10 * num12);
	}

	private static double DecayRate(SwiftSpring spring)
	{
		double num = Math.PI * 2.0 / spring.Response;
		double num2 = Math.Max(0.01, spring.DampingFraction);
		if (!(num2 < 1.0))
		{
			return num * (num2 - Math.Sqrt(num2 * num2 - 1.0));
		}
		return num2 * num;
	}

	public static double SettleSeconds(SwiftSpring spring, double distance, double speed, double epsilon)
	{
		double num = DecayRate(spring);
		double num2 = Math.PI * 2.0 / spring.Response;
		double num3 = Math.Max(0.01, spring.DampingFraction);
		double num4 = ((num3 < 1.0) ? (num2 * Math.Sqrt(1.0 - num3 * num3)) : num);
		double num5 = Math.Abs(distance) * (1.0 + num / num4) + Math.Abs(speed) / num4;
		if (num5 <= epsilon)
		{
			return 0.0;
		}
		return Math.Min(3.0, Math.Log(num5 / epsilon) / num);
	}

	[IteratorStateMachine(typeof(_003CSamples_003Ed__5))]
	public static IEnumerable<(float Progress, double Seconds)> Samples(double duration)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CSamples_003Ed__5(-2)
		{
			_003C_003E3__duration = duration
		};
	}

	public static ScalarKeyFrameAnimation Scalar(Compositor c, SwiftSpring spring, float from, float to)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		double num = Math.Max(0.016, SettleSeconds(spring, from - to, 0.0, 0.0005));
		foreach (var (num2, t) in Samples(num))
		{
			scalarKeyFrameAnimation.InsertKeyFrame(num2, (num2 >= 1f) ? to : ((float)((double)to + (double)(from - to) * Basis(spring, t).A)), easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(num);
		return scalarKeyFrameAnimation;
	}

	public static Vector3KeyFrameAnimation Scale(Compositor c, SwiftSpring spring, float from, float to)
	{
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = c.CreateVector3KeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		double num = Math.Max(0.016, SettleSeconds(spring, from - to, 0.0, 0.0005));
		foreach (var item3 in Samples(num))
		{
			float item = item3.Progress;
			double item2 = item3.Seconds;
			float num2 = ((item >= 1f) ? to : ((float)((double)to + (double)(from - to) * Basis(spring, item2).A)));
			vector3KeyFrameAnimation.InsertKeyFrame(item, new Vector3(num2, num2, 1f), easingFunction);
		}
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(num);
		return vector3KeyFrameAnimation;
	}

	public static Vector3KeyFrameAnimation Scale(Compositor c, SwiftSpring spring, Vector3 from, Vector3 to)
	{
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = c.CreateVector3KeyFrameAnimation();
		LinearEasingFunction easingFunction = c.CreateLinearEasingFunction();
		float num = Math.Max(Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)), Math.Abs(from.Z - to.Z));
		double num2 = Math.Max(0.016, SettleSeconds(spring, num, 0.0, 0.0005));
		foreach (var (num3, t) in Samples(num2))
		{
			vector3KeyFrameAnimation.InsertKeyFrame(num3, (num3 >= 1f) ? to : (to + (from - to) * (float)Basis(spring, t).A), easingFunction);
		}
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(num2);
		return vector3KeyFrameAnimation;
	}
}
