using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class PlanTickBox : Grid
{
	private readonly double size;

	private readonly Color ink;

	private readonly FrameworkElement current;

	private readonly FrameworkElement todo;

	private readonly Border doneBox;

	private readonly Path check;

	private readonly PathGeometry checkGeometry;

	private bool isDone;

	public PlanTickBox(bool isDone, bool isCurrent, double size, double cornerRadius, Color ink, bool todoIsCircle = true)
	{
		this.size = size;
		this.ink = ink;
		base.Width = (base.Height = size);
		current = ActivityIndicators.ThinkingDots(ink.WithAlpha(0.6));
		current.HorizontalAlignment = HorizontalAlignment.Center;
		double num2 = size * 0.95 / 18.0;
		current.RenderTransform = new ScaleTransform
		{
			ScaleX = num2,
			ScaleY = num2,
			CenterX = 9.0,
			CenterY = 7.0
		};
		todo = (todoIsCircle ? ((FrameworkElement)new Ellipse
		{
			Width = size * 0.8,
			Height = size * 0.8,
			Stroke = Ui.Brush(ink.WithAlpha(0.25)),
			StrokeThickness = 1.4
		}) : ((FrameworkElement)new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(cornerRadius),
			BorderBrush = Ui.Brush(ink.WithAlpha(0.28)),
			BorderThickness = new Thickness(2.0)
		}));
		todo.HorizontalAlignment = HorizontalAlignment.Center;
		todo.VerticalAlignment = VerticalAlignment.Center;
		doneBox = new Border
		{
			CornerRadius = new CornerRadius(cornerRadius),
			Background = Ui.Brush(ink.WithAlpha(0.3)),
			CenterPoint = new Vector3((float)size / 2f, (float)size / 2f, 0f)
		};
		double num3 = size * 0.24;
		double inner = size - num3 * 2.0;
		checkGeometry = CheckGeometry(num3, inner);
		check = new Path
		{
			Data = checkGeometry,
			Stroke = Ui.Brush(Colors.White),
			StrokeThickness = Math.Max(1.6, size * 0.12),
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round
		};
		base.Children.Add(current);
		base.Children.Add(todo);
		base.Children.Add(doneBox);
		base.Children.Add(check);
		Set(isDone, isCurrent, animated: false);
	}

	private static PathGeometry CheckGeometry(double inset, double inner)
	{
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = new Point(inset + inner * 0.08, inset + inner * 0.55),
			IsClosed = false
		};
		pathFigure.Segments.Add(new LineSegment
		{
			Point = new Point(inset + inner * 0.4, inset + inner * 0.85)
		});
		pathFigure.Segments.Add(new LineSegment
		{
			Point = new Point(inset + inner * 0.94, inset + inner * 0.18)
		});
		return new PathGeometry
		{
			Figures = { pathFigure }
		};
	}

	public void Set(bool done, bool isCurrent, bool animated = true)
	{
		current.Visibility = ((!isCurrent || done) ? Visibility.Collapsed : Visibility.Visible);
		todo.Visibility = ((isCurrent || done) ? Visibility.Collapsed : Visibility.Visible);
		bool flag = done != isDone;
		isDone = done;
		if (!animated || !flag || Anim.ReduceMotion)
		{
			doneBox.Opacity = (done ? 1 : 0);
			doneBox.Scale = (done ? Vector3.One : new Vector3(0.55f, 0.55f, 1f));
			check.Opacity = (done ? 1 : 0);
			check.StrokeDashArray = null;
			return;
		}
		SwiftSpring spring = new SwiftSpring(0.28, 0.55);
		Anim.ScaleSpring(doneBox, done ? 1f : 0.55f, spring, null);
		Anim.OpacitySpring(doneBox, done ? 1 : 0, spring);
		if (!done)
		{
			check.Opacity = 0.0;
			return;
		}
		double length = size * 1.25;
		double thickness = check.StrokeThickness;
		check.StrokeDashArray = new DoubleCollection
		{
			length / thickness,
			length / thickness
		};
		check.StrokeDashOffset = length / thickness;
		check.Opacity = 1.0;
		DateTimeOffset started = DateTimeOffset.Now;
		DispatcherQueueTimer timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(16L, 0L);
		timer.Tick += delegate
		{
			double num = Math.Clamp(((DateTimeOffset.Now - started).TotalSeconds - 0.06) / 0.22, 0.0, 1.0);
			num = 1.0 - (1.0 - num) * (1.0 - num);
			check.StrokeDashOffset = length / thickness * (1.0 - num);
			if (num >= 1.0)
			{
				timer.Stop();
				check.StrokeDashArray = null;
			}
		};
		timer.Start();
	}
}
