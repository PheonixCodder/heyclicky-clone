using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace HeyClicky.App.Dock;

public sealed class ShimmerSweep : Grid
{
	private readonly double peak;

	private readonly double seconds;

	private SpriteVisual? sprite;

	public ShimmerSweep(double peakWhiteOpacity = 0.08, double sweepSeconds = 1.2)
	{
		peak = peakWhiteOpacity;
		seconds = sweepSeconds;
		base.IsHitTestVisible = false;
		base.SizeChanged += delegate
		{
			Rebuild();
		};
	}

	private void Rebuild()
	{
		float num = (float)base.ActualWidth;
		float num2 = (float)base.ActualHeight;
		if (!(num <= 0f) && !(num2 <= 0f) && !Anim.ReduceMotion)
		{
			Compositor compositor = Ui.Compositor;
			if ((object)sprite == null)
			{
				CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
				compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0.5f);
				compositionLinearGradientBrush.EndPoint = new Vector2(1f, 0.5f);
				compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, DS.White(0.0)));
				compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.5f, DS.White(peak)));
				compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(1f, DS.White(0.0)));
				sprite = compositor.CreateSpriteVisual();
				sprite.Brush = compositionLinearGradientBrush;
				ContainerVisual containerVisual = compositor.CreateContainerVisual();
				containerVisual.Clip = compositor.CreateInsetClip();
				containerVisual.Children.InsertAtTop(sprite);
				ElementCompositionPreview.SetElementChildVisual(this, containerVisual);
				ElementCompositionPreview.GetElementChildVisual(this).Size = new Vector2(num, num2);
			}
			Visual elementChildVisual = ElementCompositionPreview.GetElementChildVisual(this);
			if ((object)elementChildVisual != null)
			{
				elementChildVisual.Size = new Vector2(num, num2);
			}
			float num3 = num * 1.5f;
			sprite.Size = new Vector2(num3, num2);
			float num4 = num / 2f - num3 / 2f;
			float num5 = num * 1.5f;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			vector3KeyFrameAnimation.InsertKeyFrame(0f, new Vector3(num4 - num5, 0f, 0f));
			vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(num4 + num5, 0f, 0f), Easing.EaseInOut(compositor));
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
			vector3KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			sprite.StartAnimation("Offset", vector3KeyFrameAnimation);
		}
	}
}
