using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using HeyClicky.Drawing.Painter;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal sealed class LiveCaption
{
	private const float BottomGap = 48f;

	private const float FontSize = 18f;

	private const float PaddingX = 22f;

	private const float PaddingY = 13f;

	private const float LineSpacing = 4f;

	private const float UnspokenAlpha = 0.34f;

	private const double WordFadeSeconds = 0.32;

	private const double ShowSeconds = 0.26;

	private const double WordsOutSeconds = 0.14;

	private const double WordsInSeconds = 0.22;

	private const double SpringStiffness = 280.0;

	private const double SpringDamping = 26.0;

	private static readonly Vector4 EaseOut = new Vector4(0f, 0f, 0.58f, 1f);

	private static readonly Vector4 EaseIn = new Vector4(0.42f, 0f, 1f, 1f);

	private readonly DrawingThread thread;

	private readonly Compositor compositor;

	private readonly OverlayWindow window;

	private readonly CompositionGraphicsDevice graphicsDevice;

	private readonly ContainerVisual anchor;

	private readonly ContainerVisual plateHolder;

	private readonly ShapeVisual plateShapes;

	private readonly CompositionRoundedRectangleGeometry plateGeometry;

	private readonly CompositionRoundedRectangleGeometry borderGeometry;

	private ContainerVisual? currentWords;

	private bool isShowing;

	private int hideGeneration;

	private Vector2 plateRoom;

	private Vector2 plateSize;

	private static readonly Regex WordPattern = new Regex("\\S+", RegexOptions.Compiled);

	public string ShownText { get; private set; } = "";

	public LiveCaption(DrawingThread thread, Compositor compositor, OverlayWindow window)
	{
		this.thread = thread;
		this.compositor = compositor;
		this.window = window;
		graphicsDevice = SystemComposition.GraphicsDevice(compositor, CanvasDevice.GetSharedDevice());
		anchor = compositor.CreateContainerVisual();
		anchor.Comment = "live caption";
		plateHolder = compositor.CreateContainerVisual();
		plateHolder.Opacity = 0f;
		plateShapes = compositor.CreateShapeVisual();
		plateGeometry = compositor.CreateRoundedRectangleGeometry();
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(plateGeometry);
		compositionSpriteShape.FillBrush = compositor.CreateColorBrush(Color.FromArgb((byte)Math.Round(209.1), 15, 16, 21));
		plateShapes.Shapes.Add(compositionSpriteShape);
		borderGeometry = compositor.CreateRoundedRectangleGeometry();
		CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(borderGeometry);
		compositionSpriteShape2.StrokeBrush = compositor.CreateColorBrush(Color.FromArgb((byte)Math.Round(28.05), byte.MaxValue, byte.MaxValue, byte.MaxValue));
		compositionSpriteShape2.StrokeThickness = 1f;
		plateShapes.Shapes.Add(compositionSpriteShape2);
		anchor.Children.InsertAtTop(plateHolder);
		window.Root.Children.InsertAtTop(anchor);
	}

	public void Show(string line, double revealSeconds, double holdSeconds)
	{
		string text = (line ?? "").Trim();
		float num = (float)Math.Max(1.0, window.RasterizationScale);
		Vector2 displaySize = new Vector2((float)window.BoundsPx.Width / num, (float)window.BoundsPx.Height / num);
		if (text.Length == 0 || displaySize.X <= 0f)
		{
			return;
		}
		ContainerVisual containerVisual;
		Vector2 vector;
		try
		{
			(containerVisual, vector) = BuildWords(text, Math.Min(780f, displaySize.X * 0.7f), num, Math.Max(0.3, revealSeconds), isShowing ? 0.084 : 0.0);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "live caption did not draw: " + ex.Message);
			return;
		}
		if ((object)containerVisual == null)
		{
			return;
		}
		hideGeneration++;
		float cornerRadius = Math.Min(18f, vector.Y / 2f);
		anchor.Offset = new Vector3(displaySize.X / 2f, displaySize.Y - 48f, 0f);
		EnsurePlateRoom(displaySize);
		containerVisual.Offset = new Vector3((0f - vector.X) / 2f, 0f - vector.Y, 0f);
		anchor.Children.InsertAtTop(containerVisual);
		if (isShowing)
		{
			SpringPlate(plateSize, vector, cornerRadius);
			ContainerVisual containerVisual2 = currentWords;
			if ((object)containerVisual2 != null)
			{
				FadeOut(containerVisual2, 0.14, 0.0, removesAfter: true);
			}
			Fade(containerVisual, 0f, 1f, 0.22, 0.084, EaseOut);
		}
		else
		{
			SetPlate(vector, cornerRadius);
			ContainerVisual containerVisual3 = currentWords;
			if ((object)containerVisual3 != null)
			{
				anchor.Children.Remove(containerVisual3);
			}
			Visual[] array = new Visual[2] { plateHolder, containerVisual };
			foreach (Visual visual in array)
			{
				Fade(visual, 0f, 1f, 0.26, 0.0, EaseOut);
				Rise(visual);
			}
		}
		plateSize = vector;
		currentWords = containerVisual;
		isShowing = true;
		ShownText = text;
		int generation = hideGeneration;
		thread.ScheduleTimer(DrawingThread.Now + Math.Max(1.5, holdSeconds), delegate
		{
			if (generation == hideGeneration)
			{
				Hide();
			}
		});
	}

	public void Hide()
	{
		hideGeneration++;
		if (isShowing)
		{
			isShowing = false;
			ShownText = "";
			FadeOut(plateHolder, 0.20800000000000002, 0.0, removesAfter: false);
			ContainerVisual containerVisual = currentWords;
			if ((object)containerVisual != null)
			{
				currentWords = null;
				FadeOut(containerVisual, 0.20800000000000002, 0.0, removesAfter: true);
			}
		}
	}

	private void EnsurePlateRoom(Vector2 displaySize)
	{
		Vector2 vector = new Vector2(displaySize.X, 400f);
		if (!(vector == plateRoom))
		{
			plateRoom = vector;
			plateHolder.Children.RemoveAll();
			plateShapes.Size = vector;
			plateHolder.Offset = new Vector3((0f - vector.X) / 2f, 0f - vector.Y, 0f);
			plateHolder.Size = vector;
			plateHolder.Children.InsertAtTop(CompositionShadows.Cast(compositor, plateShapes, (X: 0.0, Y: 0.0, Width: vector.X, Height: vector.Y), Color.FromArgb((byte)Math.Round(81.60000000000001), 0, 0, 0), 16f, new Vector2(0f, 6f)));
			plateHolder.Children.InsertAtTop(plateShapes);
		}
	}

	private Vector2 PlateOrigin(Vector2 size)
	{
		return new Vector2(plateRoom.X / 2f - size.X / 2f, plateRoom.Y - size.Y);
	}

	private void SetPlate(Vector2 size, float cornerRadius)
	{
		CompositionRoundedRectangleGeometry[] array = new CompositionRoundedRectangleGeometry[2] { plateGeometry, borderGeometry };
		foreach (CompositionRoundedRectangleGeometry obj in array)
		{
			obj.StopAnimation("Offset");
			obj.StopAnimation("Size");
			obj.StopAnimation("CornerRadius");
		}
		plateGeometry.Offset = PlateOrigin(size);
		plateGeometry.Size = size;
		plateGeometry.CornerRadius = new Vector2(cornerRadius);
		borderGeometry.Offset = PlateOrigin(size) + new Vector2(0.5f);
		borderGeometry.Size = size - new Vector2(1f);
		borderGeometry.CornerRadius = new Vector2(Math.Max(0f, cornerRadius - 0.5f));
	}

	private void SpringPlate(Vector2 from, Vector2 to, float cornerRadius)
	{
		float x = plateGeometry.CornerRadius.X;
		SetPlate(to, cornerRadius);
		Spring(plateGeometry, "Size", from, to);
		Spring(plateGeometry, "Offset", PlateOrigin(from), PlateOrigin(to));
		Spring(plateGeometry, "CornerRadius", new Vector2(x), new Vector2(cornerRadius));
		Spring(borderGeometry, "Size", from - new Vector2(1f), to - new Vector2(1f));
		Spring(borderGeometry, "Offset", PlateOrigin(from) + new Vector2(0.5f), PlateOrigin(to) + new Vector2(0.5f));
		Spring(borderGeometry, "CornerRadius", new Vector2(Math.Max(0f, x - 0.5f)), new Vector2(Math.Max(0f, cornerRadius - 0.5f)));
	}

	private void Spring(CompositionObject target, string property, Vector2 from, Vector2 to)
	{
		double num = Math.Sqrt(280.0);
		double dampingRatio = 26.0 / (2.0 * num);
		double response = Math.PI * 2.0 / num;
		double num2 = ScreenPenMath.SpringSettlingSeconds(response, dampingRatio);
		Vector2KeyFrameAnimation vector2KeyFrameAnimation = compositor.CreateVector2KeyFrameAnimation();
		LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
		vector2KeyFrameAnimation.InsertKeyFrame(0f, from);
		for (int i = 1; i <= 40; i++)
		{
			float num3 = (float)i / 40f;
			float amount = ((i == 40) ? 1f : ((float)ScreenPenMath.SpringProgress((double)num3 * num2, response, dampingRatio)));
			vector2KeyFrameAnimation.InsertKeyFrame(num3, Vector2.Lerp(from, to, amount), easingFunction);
		}
		vector2KeyFrameAnimation.Duration = TimeSpan.FromSeconds(num2);
		target.StartAnimation(property, vector2KeyFrameAnimation);
	}

	private void Fade(Visual visual, float from, float to, double duration, double delay, Vector4 curve)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, from);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, compositor.CreateCubicBezierEasingFunction(new Vector2(curve.X, curve.Y), new Vector2(curve.Z, curve.W)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.001, duration));
		if (delay > 0.0)
		{
			scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(delay);
		}
		scalarKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		visual.Opacity = to;
		visual.StartAnimation("Opacity", scalarKeyFrameAnimation);
	}

	private void FadeOut(Visual visual, double duration, double delay, bool removesAfter)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(EaseIn.X, EaseIn.Y), new Vector2(EaseIn.Z, EaseIn.W)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration);
		if (delay > 0.0)
		{
			scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(delay);
		}
		visual.StartAnimation("Opacity", scalarKeyFrameAnimation);
		if (!removesAfter)
		{
			return;
		}
		thread.ScheduleTimer(DrawingThread.Now + delay + duration + 0.02, delegate
		{
			try
			{
				anchor.Children.Remove(visual);
			}
			catch
			{
			}
		});
	}

	private void Rise(Visual visual)
	{
		Vector3 offset = visual.Offset;
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(0f, offset + new Vector3(0f, 8f, 0f));
		vector3KeyFrameAnimation.InsertKeyFrame(1f, offset, compositor.CreateCubicBezierEasingFunction(new Vector2(EaseOut.X, EaseOut.Y), new Vector2(EaseOut.Z, EaseOut.W)));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.26);
		visual.StartAnimation("Offset", vector3KeyFrameAnimation);
	}

	private (ContainerVisual? Words, Vector2 Size) BuildWords(string text, float maxWidth, float scale, double revealSeconds, double startsAfter)
	{
		CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
		CanvasTextLayout layout;
		Vector2 size;
		Vector2 textOrigin;
		using (CanvasTextFormat canvasTextFormat = new CanvasTextFormat
		{
			FontFamily = "Segoe UI Variable Text, Segoe UI",
			FontSize = 18f,
			FontWeight = FontWeights.Medium,
			HorizontalAlignment = CanvasHorizontalAlignment.Center,
			WordWrapping = CanvasWordWrapping.Wrap
		})
		{
			float requestedWidth = Math.Max(40f, maxWidth - 44f);
			using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(sharedDevice, "Ag", canvasTextFormat, requestedWidth, 10000f);
			CanvasLineMetrics canvasLineMetrics = canvasTextLayout.LineMetrics[0];
			canvasTextFormat.LineSpacingMode = CanvasLineSpacingMode.Uniform;
			canvasTextFormat.LineSpacing = canvasLineMetrics.Height + 4f;
			canvasTextFormat.LineSpacingBaseline = canvasLineMetrics.Baseline + 2f;
			layout = new CanvasTextLayout(sharedDevice, text, canvasTextFormat, requestedWidth, 10000f);
			try
			{
				Rect layoutBounds = layout.LayoutBounds;
				size = new Vector2(MathF.Ceiling((float)layoutBounds.Width) + 44f, MathF.Ceiling((float)layoutBounds.Height) + 26f);
				textOrigin = new Vector2(22f - (float)layoutBounds.X, 13f - (float)layoutBounds.Y);
				ContainerVisual containerVisual = compositor.CreateContainerVisual();
				containerVisual.Size = size;
				SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
				spriteVisual.Size = size;
				spriteVisual.Brush = Render(0.34f);
				containerVisual.Children.InsertAtTop(spriteVisual);
				CompositionSurfaceBrush brush = Render(1f);
				MatchCollection matchCollection = WordPattern.Matches(text);
				int num = Math.Max(1, matchCollection.Sum((Match match) => match.Length + 1));
				int num2 = 0;
				CubicBezierEasingFunction easingFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(EaseOut.X, EaseOut.Y), new Vector2(EaseOut.Z, EaseOut.W));
				foreach (Match item in matchCollection)
				{
					CanvasTextLayoutRegion[] characterRegions = layout.GetCharacterRegions(item.Index, item.Length);
					if (characterRegions.Length != 0)
					{
						double num3 = characterRegions.Min((CanvasTextLayoutRegion r) => r.LayoutBounds.Left);
						double num4 = characterRegions.Min((CanvasTextLayoutRegion r) => r.LayoutBounds.Top);
						double num5 = characterRegions.Max((CanvasTextLayoutRegion r) => r.LayoutBounds.Right);
						double num6 = characterRegions.Max((CanvasTextLayoutRegion r) => r.LayoutBounds.Bottom);
						Rect rect = new Rect(num3 + (double)textOrigin.X - 2.0, num4 + (double)textOrigin.Y - 3.0, num5 - num3 + 4.0, num6 - num4 + 6.0);
						SpriteVisual spriteVisual2 = compositor.CreateSpriteVisual();
						spriteVisual2.Size = size;
						spriteVisual2.Brush = brush;
						InsetClip insetClip = compositor.CreateInsetClip();
						insetClip.LeftInset = (float)Math.Max(0.0, rect.Left);
						insetClip.TopInset = (float)Math.Max(0.0, rect.Top);
						insetClip.RightInset = (float)Math.Max(0.0, (double)size.X - rect.Right);
						insetClip.BottomInset = (float)Math.Max(0.0, (double)size.Y - rect.Bottom);
						spriteVisual2.Clip = insetClip;
						double num7 = (double)num2 / (double)num * revealSeconds;
						num2 += item.Length + 1;
						spriteVisual2.Opacity = 1f;
						ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
						scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
						scalarKeyFrameAnimation.InsertKeyFrame(1f, 1f, easingFunction);
						scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.32);
						if (startsAfter + num7 > 0.0)
						{
							scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(startsAfter + num7);
						}
						scalarKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
						spriteVisual2.StartAnimation("Opacity", scalarKeyFrameAnimation);
						containerVisual.Children.InsertAtTop(spriteVisual2);
					}
				}
				return (Words: containerVisual, Size: size);
			}
			finally
			{
				if ((object)layout != null)
				{
					((IDisposable)layout).Dispose();
				}
			}
		}
		CompositionSurfaceBrush? Render(float alpha)
		{
			Size sizePixels = new Size(Math.Ceiling(size.X * scale), Math.Ceiling(size.Y * scale));
			CompositionDrawingSurface surface = graphicsDevice.CreateDrawingSurface(sizePixels, DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
			using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(surface))
			{
				CanvasDrawingSession session = surfaceDrawing.Session;
				session.Clear(Color.FromArgb(0, 0, 0, 0));
				session.Transform = Matrix3x2.CreateScale(scale);
				session.DrawTextLayout(layout, textOrigin, Color.FromArgb((byte)Math.Round(alpha * 255f), byte.MaxValue, byte.MaxValue, byte.MaxValue));
			}
			CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(surface);
			compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
			return compositionSurfaceBrush;
		}
	}
}
