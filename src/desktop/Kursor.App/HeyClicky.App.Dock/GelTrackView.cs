using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class GelTrackView : Grid
{
	private new sealed class Shadow : Grid
	{
		public Shadow(double radius, Color color, double blur)
		{
			base.Children.Add(new ShadowLayer(radius, color, blur, 0.0));
		}
	}

	private static readonly Color GelRim = DS.Hex(948782u);

	private readonly double height;

	private readonly Color ink;

	private int segments;

	private int done;

	private int? current;

	private readonly bool indeterminate;

	private readonly Canvas layer = new Canvas
	{
		IsHitTestVisible = false
	};

	private FrameworkElement? doneGel;

	private FrameworkElement? currentGel;

	private FrameworkElement? slider;

	private Grid? shimmerClip;

	private const double InsetPoints = 2.0;

	public GelTrackView(int segmentCount, int doneCount, int? currentIndex, double height, Color ink, bool isIndeterminate = false)
	{
		this.height = height;
		this.ink = ink;
		segments = segmentCount;
		done = doneCount;
		current = currentIndex;
		indeterminate = isIndeterminate;
		base.Height = height;
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(height / 2.0),
			Background = Ui.Brush(DS.White(0.8)),
			Margin = new Thickness(0.0, 1.0, 0.0, -1.0)
		});
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(height / 2.0),
			Background = Ui.Brush(BlendOver(ink, 0.07)),
			BorderBrush = Ui.Vertical((0.0, ink.WithAlpha(0.12)), (1.0, ink.WithAlpha(0.04))),
			BorderThickness = new Thickness(1.0)
		});
		base.Children.Add(layer);
		base.SizeChanged += delegate
		{
			Layout();
		};
	}

	private static Color BlendOver(Color ink, double alpha)
	{
		return ink.WithAlpha(alpha);
	}

	public void Update(int segmentCount, int doneCount, int? currentIndex)
	{
		if (segmentCount != segments || doneCount != done || currentIndex != current)
		{
			segments = segmentCount;
			done = doneCount;
			current = currentIndex;
			Layout(animated: true);
		}
	}

	private FrameworkElement Gel(double width, double gelHeight)
	{
		Color working = ArcColors.Working;
		Grid grid = new Grid
		{
			Width = width,
			Height = gelHeight
		};
		grid.Children.Add(new Shadow(gelHeight / 2.0, working.WithAlpha(0.45), 3.0));
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(gelHeight / 2.0),
			Background = Ui.Vertical((0.0, ArcColors.BlendWithWhite(working, 0.7)), (0.18, ArcColors.BlendWithWhite(working, 0.3)), (0.5, working), (0.82, working), (1.0, ArcColors.BlendWithWhite(working, 0.15))),
			BorderBrush = Ui.Brush(GelRim),
			BorderThickness = new Thickness(0.75)
		});
		double num = Math.Max(2.0, gelHeight * 0.42);
		grid.Children.Add(new Border
		{
			Height = num,
			VerticalAlignment = VerticalAlignment.Top,
			CornerRadius = new CornerRadius(num / 2.0),
			Margin = new Thickness(gelHeight * 0.45, Math.Max(1.0, gelHeight * 0.1), gelHeight * 0.45, 0.0),
			Background = Ui.Vertical((0.0, DS.White(0.85)), (1.0, DS.White(0.05)))
		});
		return grid;
	}

	private void Layout(bool animated = false)
	{
		double actualWidth = base.ActualWidth;
		if (actualWidth <= 0.0)
		{
			return;
		}
		layer.Children.Clear();
		doneGel = (currentGel = (slider = null));
		shimmerClip = null;
		double num = 2.0;
		double gelHeight = height - num * 2.0;
		double inner = Math.Max(0.0, actualWidth - num * 2.0);
		double segment = inner / (double)Math.Max(segments, 1);
		if (indeterminate)
		{
			slider = Gel(inner * 0.3, gelHeight);
			Canvas.SetLeft(slider, num);
			Canvas.SetTop(slider, num);
			layer.Children.Add(slider);
			if (Anim.ReduceMotion)
			{
				slider.Translation = new Vector3((float)(inner * 0.7 * 0.5), 0f, 0f);
				return;
			}
			Compositor compositor = Ui.Compositor;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			vector3KeyFrameAnimation.InsertKeyFrame(0f, Vector3.Zero);
			vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3((float)(inner * 0.7), 0f, 0f), Easing.EaseInOut(compositor));
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(1.4);
			vector3KeyFrameAnimation.Direction = AnimationDirection.Alternate;
			vector3KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			vector3KeyFrameAnimation.Target = "Translation";
			slider.StartAnimation(vector3KeyFrameAnimation);
			return;
		}
		int? num2 = current;
		if (num2.HasValue)
		{
			int valueOrDefault = num2.GetValueOrDefault();
			if (valueOrDefault >= done && valueOrDefault < segments)
			{
				double num3 = Fill(valueOrDefault + 1);
				double num4 = Math.Max(0.0, num3 - ((done > 0) ? Fill(done) : 0.0));
				currentGel = Gel(num3, gelHeight);
				Canvas.SetLeft(currentGel, num);
				Canvas.SetTop(currentGel, num);
				layer.Children.Add(currentGel);
				if (Anim.ReduceMotion)
				{
					currentGel.Opacity = 0.25;
				}
				else
				{
					Anim.LoopScalar(currentGel, "Opacity", 2.2, delegate(double t)
					{
						double num5 = t % 2.2 / 1.1;
						double num6 = ((num5 <= 1.0) ? num5 : (2.0 - num5));
						num6 = num6 * num6 * (3.0 - 2.0 * num6);
						return (float)(0.25 + 0.2 * num6);
					});
				}
				if (!Anim.ReduceMotion && num4 > 0.0)
				{
					shimmerClip = new Grid
					{
						Width = num3,
						Height = gelHeight
					};
					ShimmerSweep item = new ShimmerSweep(0.85, 1.3)
					{
						Width = num4,
						HorizontalAlignment = HorizontalAlignment.Right
					};
					shimmerClip.Children.Add(item);
					Ui.ClipRounded(shimmerClip, gelHeight / 2.0);
					Canvas.SetLeft(shimmerClip, num);
					Canvas.SetTop(shimmerClip, num);
					layer.Children.Add(shimmerClip);
				}
			}
		}
		if (done > 0)
		{
			doneGel = Gel(Fill(done), gelHeight);
			Canvas.SetLeft(doneGel, num);
			Canvas.SetTop(doneGel, num);
			layer.Children.Add(doneGel);
			if (animated && !Anim.ReduceMotion)
			{
				doneGel.CenterPoint = new Vector3(0f, (float)gelHeight / 2f, 0f);
				doneGel.Scale = new Vector3(Math.Max(0.1f, (float)(Fill(Math.Max(0, done - 1)) / Fill(done))), 1f, 1f);
				Anim.ScaleSpring(doneGel, 1f, new SwiftSpring(0.5, 0.85), new Vector3(0f, (float)gelHeight / 2f, 0f));
				doneGel.Scale = new Vector3(doneGel.Scale.X, 1f, 1f);
			}
		}
		if (segments > 1)
		{
			double num7 = Math.Max(1.1, height * 0.11);
			double num8 = ((done > 0) ? Fill(done) : 0.0);
			for (int i = 1; i < segments; i++)
			{
				double num9 = num + segment * (double)i;
				bool flag = num9 < num + num8 - num7;
				Ellipse ellipse = new Ellipse
				{
					Width = num7 * 2.0,
					Height = num7 * 2.0,
					Fill = Ui.Brush(flag ? GelRim.WithAlpha(0.45) : ink.WithAlpha(0.2))
				};
				Canvas.SetLeft(ellipse, num9 - num7);
				Canvas.SetTop(ellipse, height / 2.0 - num7);
				layer.Children.Add(ellipse);
			}
		}
		double Fill(int steps)
		{
			return GelTrack.FillWidth(steps, inner, segment, gelHeight);
		}
	}
}
