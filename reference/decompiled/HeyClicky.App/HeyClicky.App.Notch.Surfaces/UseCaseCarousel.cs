using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class UseCaseCarousel : Grid
{
	public UseCaseCarousel(IReadOnlyList<string> useCases)
	{
		UseCaseCarousel useCaseCarousel = this;
		base.Height = 26.0;
		StackPanel strip = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0
		};
		StackPanel first = Copy();
		strip.Children.Add(first);
		Grid grid = new Grid
		{
			Children = { (UIElement)strip }
		};
		Ui.ClipRounded(grid, 0.0);
		Grid grid2 = new Grid
		{
			IsHitTestVisible = false,
			Children = { (UIElement)grid }
		};
		Grid grid3 = new Grid
		{
			IsHitTestVisible = false
		};
		base.Children.Add(grid2);
		base.Children.Add(grid3);
		Compositor compositor = Ui.Compositor;
		CompositionVisualSurface surface = compositor.CreateVisualSurface();
		surface.SourceVisual = ElementCompositionPreview.GetElementVisual(grid);
		ElementCompositionPreview.GetElementVisual(grid2).Clip = compositor.CreateInsetClip(0f, 0f, 100000f, 100000f);
		CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
		compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0.5f);
		compositionLinearGradientBrush.EndPoint = new Vector2(1f, 0.5f);
		(float, int)[] array = new(float, int)[4]
		{
			(0f, 0),
			(0.055f, 255),
			(0.945f, 255),
			(1f, 0)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (offset, num) = array[i];
			compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(offset, Color.FromArgb((byte)num, byte.MaxValue, byte.MaxValue, byte.MaxValue)));
		}
		CompositionMaskBrush compositionMaskBrush = compositor.CreateMaskBrush();
		compositionMaskBrush.Source = compositor.CreateSurfaceBrush(surface);
		compositionMaskBrush.Mask = compositionLinearGradientBrush;
		SpriteVisual sprite = compositor.CreateSpriteVisual();
		sprite.Brush = compositionMaskBrush;
		ElementCompositionPreview.SetElementChildVisual(grid3, sprite);
		double laidOutSegment = -1.0;
		double laidOutWidth = -1.0;
		first.SizeChanged += delegate
		{
			Layout();
		};
		base.SizeChanged += delegate
		{
			Layout();
		};
		StackPanel Copy()
		{
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0
			};
			foreach (string useCase in useCases)
			{
				TextBlock child = Ui.Text(useCase, 11.2, FontWeights.Normal, DS.Colors.TextPrimary.WithAlpha(0.88), TextAlignment.Left, 1);
				stackPanel.Children.Add(new Border
				{
					Child = child,
					Padding = new Thickness(8.5, 4.0, 8.5, 2.0),
					VerticalAlignment = VerticalAlignment.Center,
					Background = Ui.Brush(DS.White(0.105)),
					BorderBrush = Ui.Brush(DS.White(0.16)),
					BorderThickness = new Thickness(0.6)
				}.Capsule());
			}
			return stackPanel;
		}
		void Layout()
		{
			Vector2 vector = new Vector2((float)useCaseCarousel.ActualWidth, (float)useCaseCarousel.ActualHeight);
			sprite.Size = vector;
			surface.SourceSize = vector;
			double segment = first.ActualWidth + 10.0;
			if (!(first.ActualWidth <= 1.0) && !(useCaseCarousel.ActualWidth <= 0.0) && (segment != laidOutSegment || useCaseCarousel.ActualWidth != laidOutWidth))
			{
				laidOutSegment = segment;
				laidOutWidth = useCaseCarousel.ActualWidth;
				int num2 = Math.Max(2, (int)Math.Ceiling((useCaseCarousel.ActualWidth + segment + 10.0) / segment));
				while (strip.Children.Count < num2)
				{
					strip.Children.Add(Copy());
				}
				while (strip.Children.Count > num2)
				{
					strip.Children.RemoveAt(strip.Children.Count - 1);
				}
				double duration = Math.Max(14.0, segment / 22.0);
				if (!Anim.Instant && !Anim.ReduceMotion)
				{
					Anim.LoopVector3(strip, "Translation", duration, (double t) => new Vector3((float)((0.0 - segment) * (t / duration)), 0f, 0f), 2);
				}
			}
		}
	}
}
