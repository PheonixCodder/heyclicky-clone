using System;
using System.Numerics;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Onboarding;

public sealed class HatchFlightOverlay
{
	private readonly OverlayWindow window;

	private readonly DispatcherQueue dq;

	public event Action? NotchShouldExpand;

	public event Action? Landed;

	public event Action? PeekFinishing;

	public event Action? Completed;

	public HatchFlightOverlay(bool excludeFromCapture)
	{
		dq = DispatcherQueue.GetForCurrentThread();
		window = new OverlayWindow("HeyClicky Flight", excludeFromCapture, clickThroughEverywhere: true);
	}

	public void Fly(double startPxX, double startPxY, Color color)
	{
		Native.POINT p = default(Native.POINT);
		p.X = (int)startPxX;
		p.Y = (int)startPxY;
		Displays.Monitor monitor = Displays.From(Native.MonitorFromPoint(p, 2));
		window.PlacePx(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height);
		double scale = monitor.Scale;
		double num = (double)monitor.Bounds.Width / scale;
		Vector2 vector = new Vector2((float)((startPxX - (double)monitor.Bounds.Left) / scale), (float)((startPxY - (double)monitor.Bounds.Top) / scale));
		double num2 = Math.Max(240.0, 130.0);
		Vector2 vector2 = new Vector2((float)(num / 2.0 + num2 / 2.0 - 26.0), 16f);
		Vector2 vector3 = new Vector2(vector2.X - 44f, vector2.Y);
		Vector2 vector4 = new Vector2((vector.X + vector3.X) / 2f, Math.Min(vector.Y, vector3.Y) - 80f);
		Canvas canvas = new Canvas();
		window.Root.Children.Clear();
		window.Root.Children.Add(canvas);
		Grid grid = new Grid
		{
			Width = 44.0,
			Height = 44.0
		};
		Grid grid2 = Ui.GlowSpot(color, 0.32, 118.0, 0.3).Pad(-37.0);
		grid2.Opacity = 0.558;
		grid.Children.Add(grid2);
		grid.Children.Add(IntroMark.Create(44.0, color));
		grid.CenterPoint = new Vector3(22f, 22f, 0f);
		canvas.Children.Add(grid);
		grid.Translation = new Vector3(vector.X - 22f, vector.Y - 22f, 0f);
		window.ShowNoActivate();
		Compositor compositor = Ui.Compositor;
		CubicBezierEasingFunction easingFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.58f, 1f));
		CubicBezierEasingFunction easingFunction2 = compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(1f, 1f));
		float normalizedProgressKey = 7f / 12f;
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(0f, new Vector3(vector.X - 22f, vector.Y - 22f, 0f));
		vector3KeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, new Vector3(vector4.X - 22f, vector4.Y - 22f, 0f), easingFunction);
		vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(vector3.X - 22f, vector3.Y - 22f, 0f), easingFunction2);
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.72);
		vector3KeyFrameAnimation.Target = "Translation";
		grid.StartAnimation(vector3KeyFrameAnimation);
		Vector3KeyFrameAnimation vector3KeyFrameAnimation2 = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation2.InsertKeyFrame(0f, Vector3.One);
		vector3KeyFrameAnimation2.InsertKeyFrame(normalizedProgressKey, new Vector3(0.78f, 0.78f, 1f), easingFunction);
		vector3KeyFrameAnimation2.InsertKeyFrame(1f, new Vector3(0.26f, 0.26f, 1f), easingFunction2);
		vector3KeyFrameAnimation2.Duration = TimeSpan.FromSeconds(0.72);
		vector3KeyFrameAnimation2.Target = "Scale";
		grid.StartAnimation(vector3KeyFrameAnimation2);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, -14f, easingFunction);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, -32f, easingFunction2);
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.72);
		scalarKeyFrameAnimation.Target = "Rotation";
		grid.StartAnimation(scalarKeyFrameAnimation);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation2.InsertKeyFrame(0f, 1f);
		scalarKeyFrameAnimation2.InsertKeyFrame(normalizedProgressKey, 1f);
		scalarKeyFrameAnimation2.InsertKeyFrame(1f, 0f, easingFunction2);
		scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(0.72);
		scalarKeyFrameAnimation2.Target = "Opacity";
		grid.StartAnimation(scalarKeyFrameAnimation2);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation3 = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation3.InsertKeyFrame(0f, 0.55799997f);
		scalarKeyFrameAnimation3.InsertKeyFrame(normalizedProgressKey, 0.4836f, easingFunction);
		scalarKeyFrameAnimation3.InsertKeyFrame(1f, 0.124000005f, easingFunction2);
		scalarKeyFrameAnimation3.Duration = TimeSpan.FromSeconds(0.72);
		scalarKeyFrameAnimation3.Target = "Opacity";
		grid2.StartAnimation(scalarKeyFrameAnimation3);
		After(0.72, delegate
		{
			this.NotchShouldExpand?.Invoke();
		});
		After(0.96, delegate
		{
			this.Landed?.Invoke();
		});
		After(3.46, delegate
		{
			this.PeekFinishing?.Invoke();
		});
		After(3.68, delegate
		{
			window.HideWindow();
			this.Completed?.Invoke();
		});
	}

	private void After(double seconds, Action a)
	{
		DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(seconds);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			a();
		};
		dispatcherQueueTimer.Start();
	}
}
