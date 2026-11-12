using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkPencilLayers : Grid
{
	public abstract record Motion
	{
		public sealed record Tapping : Motion;

		public sealed record Resting(double Elapsed) : Motion();
	}

	public ClickyInkPencilLayers(Motion motion, Color tint, double size)
	{
		ClickyInkPencilLayers clickyInkPencilLayers = this;
		base.IsHitTestVisible = false;
		FrameworkElement pencil = ClickyInkPencilGlyph.Make(size, tint, bold: true);
		ElementCompositionPreview.SetIsTranslationEnabled(pencil, value: true);
		Grid holder = new Grid
		{
			Width = size,
			Height = size,
			IsHitTestVisible = false,
			Children = { (UIElement)pencil }
		};
		ElementCompositionPreview.SetIsTranslationEnabled(holder, value: true);
		Canvas canvas = new Canvas
		{
			Children = { (UIElement)holder }
		};
		base.Children.Add(canvas);
		List<FrameworkElement> dots = new List<FrameworkElement>();
		if (motion is Motion.Resting)
		{
			for (int i = 0; i < 3; i++)
			{
				Ellipse item = new Ellipse
				{
					Width = 2.8,
					Height = 2.8,
					Fill = Ui.Brush(tint),
					Opacity = 0.0
				};
				canvas.Children.Add(item);
				dots.Add(item);
			}
		}
		base.SizeChanged += delegate
		{
			if (motion is Motion.Tapping)
			{
				Canvas.SetLeft(holder, (clickyInkPencilLayers.ActualWidth - size) / 2.0);
				Canvas.SetTop(holder, (clickyInkPencilLayers.ActualHeight - size) / 2.0);
				pencil.Opacity = 0.8;
			}
			else
			{
				double num = clickyInkPencilLayers.ActualHeight * 0.78;
				Canvas.SetLeft(holder, 0.0);
				Canvas.SetTop(holder, num - size - 1.0);
				for (int j = 0; j < dots.Count; j++)
				{
					Canvas.SetLeft(dots[j], size + 4.0 + (double)j * 5.5);
					Canvas.SetTop(dots[j], num - 2.8);
				}
			}
		};
		base.Loaded += delegate
		{
			pencil.CenterPoint = new Vector3((float)(size / 2.0), (float)(size / 2.0), 0f);
			if (motion is Motion.Tapping)
			{
				Anim.LoopScalar(pencil, "Rotation", 0.64, delegate(double t)
				{
					return (float)(-6.0 - 14.0 * Ease(t));
					static double Ease(double t)
					{
						return (1.0 - Math.Cos(t * Math.PI / 0.32)) / 2.0;
					}
				});
				Anim.LoopVector3(pencil, "Translation", 0.64, (double t) => new Vector3(0f, (float)(1.0 - 3.0 * ClickyInkPencilLayers._003C_002Ector_003Eg__Ease_007C1_2(t)), 0f));
			}
			else
			{
				double elapsed = ((Motion.Resting)motion).Elapsed;
				if (elapsed < 0.5)
				{
					pencil.Opacity = 0.6 * ClickyInkClock.EaseInOut(elapsed / 0.5);
					Anim.Opacity(pencil, 0.6f, 0.5 - elapsed);
					holder.Translation = new Vector3(0f, (float)(-4.0 * (1.0 - ClickyInkClock.EaseInOut(elapsed / 0.5))), 0f);
					Anim.Translate(holder, Vector3.Zero, 0.5 - elapsed);
				}
				else
				{
					pencil.Opacity = 0.6;
				}
				if (elapsed < 2.4)
				{
					DispatcherQueue forCurrentThread = DispatcherQueue.GetForCurrentThread();
					Once.After(2.4 - elapsed, delegate
					{
						Anim.LoopVector3(pencil, "Translation", 2.4, delegate(double t)
						{
							return new Vector3(0f, (float)Tap(t), 0f);
							static double Tap(double cycle)
							{
								if (!(cycle < 0.5))
								{
									return 0.0;
								}
								return Math.Abs(Math.Sin(cycle / 0.25 * Math.PI)) * 2.2;
							}
						}, 96);
					}, forCurrentThread);
				}
				else
				{
					Anim.LoopVector3(pencil, "Translation", 2.4, (double t) => new Vector3(0f, (float)ClickyInkPencilLayers._003C_002Ector_003Eg__Tap_007C1_5(t), 0f), 96, (elapsed - 2.4) % 2.4);
				}
				for (int k = 0; k < dots.Count; k++)
				{
					int i2 = k;
					Anim.LoopScalar(dots[i2], "Opacity", 2.0, delegate(double t)
					{
						double num2 = (t * 0.9 - (double)i2 * 0.22) % 1.8;
						return (float)((num2 < 0.0) ? 0.0 : (Math.Max(0.0, Math.Sin(Math.Min(1.0, num2 / 1.2) * Math.PI)) * 0.55));
					}, 48, elapsed % 2.0);
				}
			}
		};
	}
}
