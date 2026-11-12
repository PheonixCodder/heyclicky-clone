using System.Numerics;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class PenMask
{
	public required CompositionPathGeometry Geometry;

	public required ShapeVisual MaskVisual;

	public required ContainerVisual MaskHolder;

	public required SpriteVisual Sprite;

	public required CompositionVisualSurface MaskSurface;

	public required CompositionSurfaceBrush Source;

	public CompositionDrawingSurface? Ink;

	public bool IsInkStale;

	public ContainerVisual? ContentHolder;

	public CompositionVisualSurface? ContentSurface;

	public required CompositionMaskBrush Brush;

	public Vector2 PixelOrigin;

	public Vector2 PixelSize;

	public bool IsApplied;

	public double PenEndTime;

	public bool EditRunning;

	public double EditEndTime;

	public float ModelTrimEnd = 1f;
}
