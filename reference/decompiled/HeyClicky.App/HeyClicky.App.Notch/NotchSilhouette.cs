using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Composition;

namespace HeyClicky.App.Notch;

public sealed class NotchSilhouette
{
	private readonly Compositor c;

	private readonly float envelopeWidth;

	private const float TopRadius = 10f;

	public const double SliverFill = 1.0;

	internal const double SliverBorderAlpha = 0.24;

	private NotchSystemShape? twin;

	private readonly CompositionSpriteShape sliverBorder;

	internal const string RoundedCorners = "Vector2((0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2), (0.854 + 0.146 * p.Sliver) * Min(p.Radius, Min(p.Width - 20 * p.Concavity, p.Height) / 2))";

	public const string OnBar = "p.Sliver * Clamp((16 - p.Height) / 8, 0, 1)";

	private readonly Dictionary<string, SpringValue> knobs = new Dictionary<string, SpringValue>
	{
		["Width"] = new SpringValue(60.0, 0.05),
		["Height"] = new SpringValue(8.0, 0.05),
		["Radius"] = new SpringValue(14.0, 0.05),
		["Gap"] = new SpringValue(4.0, 0.05),
		["Concavity"] = new SpringValue(0.0, 0.001),
		["Fill"] = new SpringValue(1.0, 0.001),
		["Sliver"] = new SpringValue(1.0, 0.001)
	};

	public CompositionPropertySet Props { get; }

	public ContainerVisual Root { get; }

	public CompositionRoundedRectangleGeometry ClipGeometry { get; }

	public float Hairline { get; private set; } = 0.5f;

	public event Action<float>? HairlineChanged;

	public NotchSilhouette(Compositor compositor, double envelopeWidth)
	{
		c = compositor;
		this.envelopeWidth = (float)envelopeWidth;
		Props = c.CreatePropertySet();
		Props.InsertScalar("Width", 60f);
		Props.InsertScalar("Height", 8f);
		Props.InsertScalar("Radius", 14f);
		Props.InsertScalar("Gap", 4f);
		Props.InsertScalar("Concavity", 0f);
		Props.InsertScalar("Fill", 1f);
		Props.InsertScalar("Sliver", 1f);
		Props.InsertScalar("EnvW", this.envelopeWidth);
		Props.InsertScalar("Px", Hairline);
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
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation("p.Fill");
		expressionAnimation.SetReferenceParameter("p", Props);
		containerVisual.StartAnimation("Opacity", expressionAnimation);
		Root.Children.InsertAtTop(containerVisual);
		ShapeVisual shapeVisual2 = c.CreateShapeVisual();
		shapeVisual2.RelativeSizeAdjustment = Vector2.One;
		CompositionRoundedRectangleGeometry geometry = c.CreateRoundedRectangleGeometry();
		BindHairline(geometry);
		sliverBorder = c.CreateSpriteShape(geometry);
		sliverBorder.StrokeBrush = c.CreateColorBrush(DS.White(0.24));
		sliverBorder.StrokeThickness = Hairline;
		shapeVisual2.Shapes.Add(sliverBorder);
		ExpressionAnimation expressionAnimation2 = c.CreateExpressionAnimation("p.Sliver * Clamp((16 - p.Height) / 8, 0, 1)");
		expressionAnimation2.SetReferenceParameter("p", Props);
		shapeVisual2.StartAnimation("Opacity", expressionAnimation2);
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

	public void SetDeviceScale(double scale)
	{
		Hairline = (float)(1.0 / Math.Max(1.0, scale));
		Props.InsertScalar("Px", Hairline);
		sliverBorder.StrokeThickness = Hairline;
		this.HairlineChanged?.Invoke(Hairline);
		twin?.SetHairline(Hairline);
	}

	public void AttachTwin(NotchSystemShape shape)
	{
		twin = shape;
		foreach (var (propertyName, springValue2) in knobs)
		{
			shape.Props.InsertScalar(propertyName, (float)springValue2.Target);
		}
		shape.SetHairline(Hairline);
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

	public void AnimateTo(double width, double height, double radius, double gap, double concavity, double fill, double sliver, SwiftSpring? spring)
	{
		if (Anim.Instant || Anim.ReduceMotion)
		{
			spring = null;
		}
		if (concavity > 0.0)
		{
			width = Math.Round(width / (double)(2f * Hairline)) * 2.0 * (double)Hairline;
		}
		Go("Width", width);
		Go("Height", height);
		Go("Radius", radius);
		Go("Gap", gap);
		Go("Concavity", concavity);
		Go("Fill", fill);
		Go("Sliver", sliver);
		void Go(string name, double v)
		{
			SpringMove springMove = knobs[name].Move(v, spring);
			if ((object)springMove != null)
			{
				Props.StartAnimation(name, springMove.Animation(c));
				twin?.Play(name, springMove);
			}
		}
	}
}
