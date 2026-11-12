using System.Collections.Generic;
using System.Numerics;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class Node
{
	public required NodeKind Kind;

	public required Visual Visual;

	public ContainerVisual? Content;

	public Node? Parent;

	public List<Node>? Children;

	public int Unit = -1;

	public string? MaskId;

	public PenMask? Mask;

	public float? GlowRadius;

	public List<RunningAnimation>? Animations;

	public bool HasTransformMotion;

	public Vector3 WrapperScale = Vector3.One;

	public Vector3 WrapperOffset = Vector3.Zero;

	public WindowChromeInfo? Window;

	public (double X, double Y, double Width, double Height) Extent;

	public object? Tag;

	public void AddAnimation(RunningAnimation animation)
	{
		(Animations ?? (Animations = new List<RunningAnimation>())).Add(animation);
	}
}
