using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class DockLaunchFlight
{
	private const double TriangleSize = 26.0;

	private const double StartRotation = -32.0;

	private const double EndRotation = -140.0;

	private const double StartScale = 1.0;

	private const double PopScale = 1.25;

	private const double EndScale = 0.35;

	private const double PopDuration = 0.12;

	private const double TravelDelay = 0.05;

	private const double TravelDuration = 0.7;

	private const double FadeDelay = 0.62;

	private const double FadeDuration = 0.25;

	private static readonly double Visible = Math.Max(0.75, 0.87);

	private readonly bool excludeFromCapture;

	private OverlayWindow? window;

	private Canvas? layer;

	private int inFlight;

	private static readonly Func<double, double> EaseOut = Bezier(0.0, 0.0, 0.58, 1.0);

	private static readonly Func<double, double> EaseInOut = Bezier(0.42, 0.0, 0.58, 1.0);

	private static readonly Func<double, double> EaseIn = Bezier(0.42, 0.0, 1.0, 1.0);

	public DockLaunchFlight(bool excludeFromCapture)
	{
		this.excludeFromCapture = excludeFromCapture;
	}

	public void Fly(PointD endPx, Color color, Action onFirstFrame, PointD? startPx = null)
	{
		if (Anim.Instant || Anim.ReduceMotion)
		{
			onFirstFrame();
			return;
		}
		try
		{
			Native.GetCursorPos(out var p);
			Displays.Monitor monitor;
			if (startPx.HasValue)
			{
				PointD valueOrDefault = startPx.GetValueOrDefault();
				monitor = Displays.At((int)valueOrDefault.X, (int)valueOrDefault.Y);
			}
			else
			{
				monitor = Displays.UnderCursor();
			}
			Displays.Monitor m = monitor;
			EnsureWindow(m);
			double scale = m.Scale;
			Vector2 vector;
			if (startPx.HasValue)
			{
				PointD valueOrDefault2 = startPx.GetValueOrDefault();
				vector = new Vector2((float)((valueOrDefault2.X - (double)m.Bounds.Left) / scale), (float)((valueOrDefault2.Y - (double)m.Bounds.Top) / scale));
			}
			else
			{
				vector = new Vector2((float)((double)(p.X - m.Bounds.Left) / scale + 35.0), (float)((double)(p.Y - m.Bounds.Top) / scale + 25.0));
			}
			Vector2 vector2 = vector;
			Vector2 vector3 = new Vector2((float)((endPx.X - (double)m.Bounds.Left) / scale), (float)((endPx.Y - (double)m.Bounds.Top) / scale));
			Grid outer = new Grid
			{
				Width = 26.0,
				Height = 26.0,
				IsHitTestVisible = false
			};
			outer.Children.Add(Ui.GlowSpot(color, 0.8, 54.0, 0.2));
			Grid grid = new Grid
			{
				Width = 26.0,
				Height = 26.0,
				CenterPoint = new Vector3(13f, 13f, 0f)
			};
			grid.Children.Add(new ShadowLayer(3.0, color.WithAlpha(0.4), 3.0, 1.0)
			{
				Margin = new Thickness(6.0)
			});
			grid.Children.Add(IntroMark.Cursor(26.0, color, 0.0));
			outer.Children.Add(grid);
			layer.Children.Add(outer);
			Compositor compositor = Ui.Compositor;
			LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
			Vector3KeyFrameAnimation vector3KeyFrameAnimation2 = compositor.CreateVector3KeyFrameAnimation();
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = compositor.CreateScalarKeyFrameAnimation();
			int num = (int)Math.Ceiling(Visible * 240.0);
			for (int i = 0; i <= num; i++)
			{
				double t = (double)i / (double)num * Visible;
				float normalizedProgressKey = (float)((double)i / (double)num);
				double num2 = Progress(t, 0.05, 0.7, EaseInOut);
				Vector2 vector4 = vector2 + (vector3 - vector2) * (float)num2;
				vector3KeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, new Vector3(vector4.X - 13f, vector4.Y - 13f, 0f), easingFunction);
				scalarKeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, (float)Opacity(t), easingFunction);
				float num3 = (float)Scale(t);
				vector3KeyFrameAnimation2.InsertKeyFrame(normalizedProgressKey, new Vector3(num3, num3, 1f), easingFunction);
				scalarKeyFrameAnimation2.InsertKeyFrame(normalizedProgressKey, (float)(-140.0 + 108.0 * (1.0 - num2)), easingFunction);
			}
			KeyFrameAnimation[] array = new KeyFrameAnimation[4] { vector3KeyFrameAnimation, scalarKeyFrameAnimation, vector3KeyFrameAnimation2, scalarKeyFrameAnimation2 };
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Duration = TimeSpan.FromSeconds(Visible);
			}
			vector3KeyFrameAnimation.Target = "Translation";
			scalarKeyFrameAnimation.Target = "Opacity";
			vector3KeyFrameAnimation2.Target = "Scale";
			scalarKeyFrameAnimation2.Target = "Rotation";
			outer.Opacity = 0.0;
			outer.StartAnimation(vector3KeyFrameAnimation);
			outer.StartAnimation(scalarKeyFrameAnimation);
			grid.StartAnimation(vector3KeyFrameAnimation2);
			grid.StartAnimation(scalarKeyFrameAnimation2);
			inFlight++;
			window.ShowNoActivate();
			DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Low, delegate
			{
				onFirstFrame();
			});
			Canvas host = layer;
			DockTimer.After(1.1, delegate
			{
				host.Children.Remove(outer);
				if (--inFlight == 0)
				{
					window.HideWindow();
				}
			});
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "launch flight: " + ex.Message);
			onFirstFrame();
		}
	}

	private void EnsureWindow(Displays.Monitor m)
	{
		if ((object)window == null)
		{
			window = new OverlayWindow("HeyClicky Launch Flight", excludeFromCapture, clickThroughEverywhere: true);
		}
		if ((object)layer == null)
		{
			layer = new Canvas();
			window.Root.Children.Add(layer);
		}
		window.Root.Width = (double)m.Bounds.Width / m.Scale;
		window.Root.Height = (double)m.Bounds.Height / m.Scale;
		window.PlacePx(m.Bounds.Left, m.Bounds.Top, m.Bounds.Width, m.Bounds.Height);
	}

	private static double Scale(double t)
	{
		return 0.35 + -0.25 * (1.0 - Progress(t, 0.0, 0.12, EaseOut)) + 0.9 * (1.0 - Progress(t, 0.05, 0.7, EaseInOut));
	}

	private static double Opacity(double t)
	{
		return Math.Clamp(0.0 + -1.0 * (1.0 - Progress(t, 0.0, 0.12, EaseOut)) + 1.0 * (1.0 - Progress(t, 0.62, 0.25, EaseIn)), 0.0, 1.0);
	}

	private static double Progress(double t, double delay, double duration, Func<double, double> curve)
	{
		return curve(Math.Clamp((t - delay) / duration, 0.0, 1.0));
	}

	private static Func<double, double> Bezier(double x1, double y1, double x2, double y2)
	{
		double cx = 3.0 * x1;
		double bx = 3.0 * (x2 - x1) - cx;
		double ax = 1.0 - cx - bx;
		double cy = 3.0 * y1;
		double by = 3.0 * (y2 - y1) - cy;
		double ay = 1.0 - cy - by;
		return delegate(double x)
		{
			if (x <= 0.0)
			{
				return 0.0;
			}
			if (x >= 1.0)
			{
				return 1.0;
			}
			double num = x;
			for (int i = 0; i < 8; i++)
			{
				double num2 = X(num) - x;
				if (Math.Abs(num2) < 1E-07)
				{
					return Y(num);
				}
				double num3 = dX(num);
				if (Math.Abs(num3) < 1E-06)
				{
					break;
				}
				num -= num2 / num3;
			}
			double num4 = 0.0;
			double num5 = 1.0;
			num = x;
			for (int j = 0; j < 64; j++)
			{
				if (!(num4 < num5))
				{
					break;
				}
				double num6 = X(num);
				if (Math.Abs(num6 - x) < 1E-07)
				{
					break;
				}
				if (x > num6)
				{
					num4 = num;
				}
				else
				{
					num5 = num;
				}
				num = (num4 + num5) / 2.0;
			}
			return Y(num);
		};
		double X(double u)
		{
			return ((ax * u + bx) * u + cx) * u;
		}
		double Y(double u)
		{
			return ((ay * u + by) * u + cy) * u;
		}
		double dX(double u)
		{
			return (3.0 * ax * u + 2.0 * bx) * u + cx;
		}
	}
}
