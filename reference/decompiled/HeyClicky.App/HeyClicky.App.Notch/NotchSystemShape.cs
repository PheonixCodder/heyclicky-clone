using System;
using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI;
using Windows.UI.Composition;

namespace HeyClicky.App.Notch;

public sealed class NotchSystemShape
{
	private readonly Compositor c;

	private readonly CompositionSpriteShape sliverBorder;

	private const float TopRadius = 10f;

	public CompositionPropertySet Props { get; }

	public ContainerVisual Root { get; }

	public CompositionRoundedRectangleGeometry ClipGeometry { get; }

	public float Hairline { get; private set; } = 0.5f;

	public event Action<float>? HairlineChanged;

	public NotchSystemShape(Compositor compositor, double envelopeWidth)
	{
		c = compositor;
		Props = c.CreatePropertySet();
		Props.InsertScalar("Width", 60f);
		Props.InsertScalar("Height", 8f);
		Props.InsertScalar("Radius", 14f);
		Props.InsertScalar("Gap", 4f);
		Props.InsertScalar("Concavity", 0f);
		Props.InsertScalar("Fill", 1f);
		Props.InsertScalar("Sliver", 1f);
		Props.InsertScalar("EnvW", (float)envelopeWidth);
		Props.InsertScalar("Px", 0.5f);
		Root = c.CreateContainerVisual();
		Root.RelativeSizeAdjustment = Vector2.One;
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.RelativeSizeAdjustment = Vector2.One;
		ShapeVisual shapeVisual = c.CreateShapeVisual();
		shapeVisual.RelativeSizeAdjustment = Vector2.One;
		containerVisual.Children.InsertAtTop(shapeVisual);
		CompositionColorBrush compositionColorBrush = c.CreateColorBrush(Colors.Black);
		containerVisual.Children.InsertAtTop(Shoulder(left: true, compositionColorBrush));
		containerVisual.Children.InsertAtTop(Shoulder(left: false, compositionColorBrush));
		CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = c.CreateRoundedRectangleGeometry();
		Bind(compositionRoundedRectangleGeometry, "Offset", "Vector2((p.EnvW - p.Width) / 2 + 10 * p.Concavity, p.Gap - p.Radius * p.Concavity)");
		Bind(compositionRoundedRectangleGeometry, "Size", "Vector2(p.Width - 20 * p.Concavity, p.Height + p.Radius * p.Concavity)");
		Bind(compositionRoundedRectangleGeometry, "CornerRadius", "Vector2((0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2), (0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2))");
		CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(compositionRoundedRectangleGeometry);
		compositionSpriteShape.FillBrush = compositionColorBrush;
		shapeVisual.Shapes.Add(compositionSpriteShape);
		Bind(containerVisual, "Opacity", "p.Fill");
		Root.Children.InsertAtTop(containerVisual);
		ShapeVisual shapeVisual2 = c.CreateShapeVisual();
		shapeVisual2.RelativeSizeAdjustment = Vector2.One;
		CompositionRoundedRectangleGeometry geometry = c.CreateRoundedRectangleGeometry();
		BindHairline(geometry);
		sliverBorder = c.CreateSpriteShape(geometry);
		sliverBorder.StrokeBrush = c.CreateColorBrush(DS.White(0.24));
		sliverBorder.StrokeThickness = 0.5f;
		shapeVisual2.Shapes.Add(sliverBorder);
		Bind(shapeVisual2, "Opacity", "p.Sliver * Clamp((16 - p.Height) / 8, 0, 1)");
		Root.Children.InsertAtTop(shapeVisual2);
		ClipGeometry = c.CreateRoundedRectangleGeometry();
		Bind(ClipGeometry, "Offset", "Vector2((p.EnvW - p.Width) / 2 + 10 * p.Concavity, p.Gap - p.Radius * p.Concavity)");
		Bind(ClipGeometry, "Size", "Vector2(p.Width - 20 * p.Concavity, p.Height + p.Radius * p.Concavity)");
		Bind(ClipGeometry, "CornerRadius", "Vector2((0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2), (0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2))");
	}

	public void BindHairline(CompositionRoundedRectangleGeometry geometry)
	{
		Bind(geometry, "Offset", "Vector2((p.EnvW - p.Width) / 2 + 10 * p.Concavity + p.Px / 2, p.Gap + p.Px / 2)");
		Bind(geometry, "Size", "Vector2(p.Width - 20 * p.Concavity - p.Px, p.Height - p.Px)");
		Bind(geometry, "CornerRadius", "Vector2((Min(p.Width - 20 * p.Concavity, p.Height) - p.Px) / 2, (Min(p.Width - 20 * p.Concavity, p.Height) - p.Px) / 2)");
	}

	public void SetHairline(float hairline)
	{
		Hairline = hairline;
		Props.InsertScalar("Px", hairline);
		sliverBorder.StrokeThickness = hairline;
		this.HairlineChanged?.Invoke(hairline);
	}

	public void Play(string knob, SpringMove move)
	{
		Props.StartAnimation(knob, move.Animation(c));
	}

	private ShapeVisual Shoulder(bool left, CompositionBrush fill)
	{
		ShapeVisual shapeVisual = c.CreateShapeVisual();
		shapeVisual.Size = new Vector2(10f, 10f);
		shapeVisual.Clip = c.CreateInsetClip();
		CompositionEllipseGeometry compositionEllipseGeometry = c.CreateEllipseGeometry();
		compositionEllipseGeometry.Center = new Vector2(left ? 0f : 10f, 10f);
		compositionEllipseGeometry.Radius = new Vector2(13f);
		CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(compositionEllipseGeometry);
		compositionSpriteShape.StrokeBrush = fill;
		compositionSpriteShape.StrokeThickness = 6f;
		shapeVisual.Shapes.Add(compositionSpriteShape);
		shapeVisual.CenterPoint = new Vector3(left ? 10f : 0f, 0f, 0f);
		Bind(shapeVisual, "Offset", left ? "Vector3((p.EnvW - p.Width) / 2, p.Gap, 0)" : "Vector3((p.EnvW + p.Width) / 2 - 10, p.Gap, 0)");
		Bind(shapeVisual, "Scale", "Vector3(p.Concavity, p.Concavity, 1)");
		return shapeVisual;
	}

	private void Bind(CompositionObject target, string property, string expression)
	{
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(expression);
		expressionAnimation.SetReferenceParameter("p", Props);
		target.StartAnimation(property, expressionAnimation);
	}
}
