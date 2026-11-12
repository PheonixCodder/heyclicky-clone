using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal static class CompositionShadows
{
	public sealed record Caster(Visual Source, Vector2 SourceOffset, DropShadow Shadow);

	private static readonly ConditionalWeakTable<SpriteVisual, Caster> casters = new ConditionalWeakTable<SpriteVisual, Caster>();

	public static SpriteVisual Cast(Compositor compositor, Visual source, (double X, double Y, double Width, double Height) region, Color color, float blurRadius, Vector2 offset)
	{
		Vector2 vector = new Vector2((float)region.X, (float)region.Y);
		Vector2 vector2 = new Vector2((float)Math.Max(1.0, region.Width), (float)Math.Max(1.0, region.Height));
		CompositionVisualSurface compositionVisualSurface = compositor.CreateVisualSurface();
		compositionVisualSurface.SourceVisual = source;
		compositionVisualSurface.SourceOffset = vector;
		compositionVisualSurface.SourceSize = vector2;
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(compositionVisualSurface);
		compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.Color = color;
		dropShadow.BlurRadius = blurRadius;
		dropShadow.Offset = new Vector3(offset, 0f);
		dropShadow.Mask = compositionSurfaceBrush;
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Comment = "shadow";
		spriteVisual.Offset = new Vector3(vector, 0f);
		spriteVisual.Size = vector2;
		spriteVisual.Shadow = dropShadow;
		casters.AddOrUpdate(spriteVisual, new Caster(source, vector, dropShadow));
		return spriteVisual;
	}

	public static bool TryGetCaster(SpriteVisual sprite, out Caster caster)
	{
		return casters.TryGetValue(sprite, out caster);
	}

	public static (double X, double Y, double Width, double Height) Bounds((double X, double Y, double Width, double Height) box, Matrix3x2? transform, double margin)
	{
		double num = box.X;
		double num2 = box.Y;
		double num3 = box.X + box.Width;
		double num4 = box.Y + box.Height;
		if (transform.HasValue)
		{
			Matrix3x2 valueOrDefault = transform.GetValueOrDefault();
			Vector2[] source = new Vector2[4]
			{
				Vector2.Transform(new Vector2((float)num, (float)num2), valueOrDefault),
				Vector2.Transform(new Vector2((float)num3, (float)num2), valueOrDefault),
				Vector2.Transform(new Vector2((float)num, (float)num4), valueOrDefault),
				Vector2.Transform(new Vector2((float)num3, (float)num4), valueOrDefault)
			};
			num = source.Min((Vector2 c) => c.X);
			num3 = source.Max((Vector2 c) => c.X);
			num2 = source.Min((Vector2 c) => c.Y);
			num4 = source.Max((Vector2 c) => c.Y);
		}
		return (X: num - margin, Y: num2 - margin, Width: num3 - num + 2.0 * margin, Height: num4 - num2 + 2.0 * margin);
	}
}
