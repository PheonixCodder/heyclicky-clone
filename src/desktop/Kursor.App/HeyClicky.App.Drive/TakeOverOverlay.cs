using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Drive;

public sealed class TakeOverOverlay
{
	private OverlayWindow? window;

	private Grid? pill;

	private TextBlock? status;

	private TextBlock? statusOutgoing;

	private TextBlock? hint;

	private FrameworkElement? cursor;

	private FrameworkElement? keyCapElement;

	private string statusTextBehindHint = "";

	private bool isShowingMouseHint;

	private SpriteVisual? shimmer;

	private CompositionLinearGradientBrush? shimmerBrush;

	private ContainerVisual? glowHolder;

	private bool reduceMotion;

	private int generation;

	public const double Band = 54.0;

	private static readonly Color MouseHintColor = Color.FromArgb(byte.MaxValue, byte.MaxValue, 199, 107);

	private const string MouseHintLine = "You moved the mouse. I'm still driving.";

	private static readonly SwiftSpring GrowSpring = new SwiftSpring(Math.PI * 2.0 / Math.Sqrt(210.0), 16.0 / (2.0 * Math.Sqrt(210.0)));

	public static double PillWidth => Math.Max(360.0, 460.0);

	public static double PillHeight => 62.0;

	public bool IsShown => window?.IsShown ?? false;

	public void Show(Displays.Monitor display, Color accent, string goal)
	{
		Hide(animated: false);
		DriveNotchOwnership.SetDrivingShown(shown: true);
		generation++;
		reduceMotion = Anim.ReduceMotion;
		if ((object)window == null)
		{
			window = new OverlayWindow("Kursor drive", excludeFromCapture: true, clickThroughEverywhere: true, activatable: false, topmost: true, showInTaskbar: false, hiddenFromOwnCapture: true);
			NonRudeWindow.Mark(window.Hwnd);
		}
		Native.RECT bounds = display.Bounds;
		window.PlacePx(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
		double scale = display.Scale;
		double num = (double)bounds.Width / scale;
		double height = (double)bounds.Height / scale;
		Grid grid = new Grid
		{
			Width = num,
			Height = height,
			IsHitTestVisible = false
		};
		window.Root.Children.Clear();
		window.Root.Children.Add(grid);
		AddEdgeGlow(grid, num, height, accent, scale);
		AddPill(grid, num, accent, goal, scale);
		window.ShowNoActivate();
	}

	public void SetStatus(string text)
	{
		statusTextBehindHint = text;
		if (!isShowingMouseHint && (object)status != null && (object)statusOutgoing != null && !(status.Text == text))
		{
			statusOutgoing.Text = status.Text;
			statusOutgoing.Opacity = 1.0;
			status.Text = text;
			status.Opacity = 0.0;
			Anim.Opacity(statusOutgoing, 0f, 0.25, Easing.EaseInOut(Ui.Compositor));
			Anim.Opacity(status, 1f, 0.25, Easing.EaseInOut(Ui.Compositor));
		}
	}

	public void SetWaitingForUserMouse(bool waiting, bool nudge)
	{
		if ((object)status == null || (object)hint == null || (object)keyCapElement == null || (object)pill == null)
		{
			return;
		}
		if (waiting != isShowingMouseHint)
		{
			isShowingMouseHint = waiting;
			status.Text = (waiting ? "You moved the mouse. I'm still driving." : statusTextBehindHint);
			status.Foreground = Ui.Brush(waiting ? MouseHintColor : DS.White(0.62));
			status.FontWeight = (waiting ? FontWeights.SemiBold : FontWeights.Normal);
			hint.Text = (waiting ? "to take over" : "to stop");
			hint.Foreground = Ui.Brush(DS.White(waiting ? 0.9 : 0.5));
			hint.FontWeight = (waiting ? FontWeights.SemiBold : FontWeights.Normal);
			Anim.ScaleSpring(keyCapElement, waiting ? 1.12f : 1f, new SwiftSpring(0.3, 0.6), new Vector3((float)(keyCapElement.ActualWidth / 2.0), (float)(keyCapElement.ActualHeight / 2.0), 0f));
		}
		if (waiting && nudge && !reduceMotion)
		{
			Compositor compositor = Ui.Compositor;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			float[] array = new float[7] { 0f, 5f, -5f, 4f, -4f, 2f, 0f };
			for (int i = 0; i < array.Length; i++)
			{
				vector3KeyFrameAnimation.InsertKeyFrame((float)i / (float)(array.Length - 1), new Vector3(array[i], 0f, 0f), compositor.CreateLinearEasingFunction());
			}
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.42);
			vector3KeyFrameAnimation.Target = "Translation";
			pill.StartAnimation(vector3KeyFrameAnimation);
		}
	}

	public void SetActing(bool acting)
	{
		if ((object)shimmer == null || (object)shimmerBrush == null || (object)cursor == null || (object)glowHolder == null)
		{
			return;
		}
		Compositor compositor = Ui.Compositor;
		if (acting)
		{
			if (!reduceMotion)
			{
				Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
				vector3KeyFrameAnimation.InsertKeyFrame(0f, Vector3.One);
				vector3KeyFrameAnimation.InsertKeyFrame(0.35f, new Vector3(1.22f, 1.22f, 1f));
				vector3KeyFrameAnimation.InsertKeyFrame(0.7f, new Vector3(0.94f, 0.94f, 1f));
				vector3KeyFrameAnimation.InsertKeyFrame(1f, Vector3.One);
				vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.45);
				vector3KeyFrameAnimation.Target = "Scale";
				cursor.StartAnimation(vector3KeyFrameAnimation);
				CompositionColorGradientStopCollection colorStops = shimmerBrush.ColorStops;
				(float, float)[] array = new(float, float)[3]
				{
					(-0.4f, 1f),
					(-0.2f, 1.2f),
					(0f, 1.4f)
				};
				for (int i = 0; i < colorStops.Count && i < array.Length; i++)
				{
					ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
					scalarKeyFrameAnimation.InsertKeyFrame(0f, array[i].Item1, compositor.CreateLinearEasingFunction());
					scalarKeyFrameAnimation.InsertKeyFrame(1f, array[i].Item2, compositor.CreateLinearEasingFunction());
					scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(1.1);
					scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
					colorStops[i].StartAnimation("Offset", scalarKeyFrameAnimation);
				}
			}
			FadeVisual(shimmer, 1f, 0.25);
			FadeVisual(glowHolder, 1f, 0.25);
		}
		else
		{
			CompositionColorGradientStopCollection colorStops2 = shimmerBrush.ColorStops;
			float[] array2 = new float[3] { -0.4f, -0.2f, 0f };
			for (int j = 0; j < colorStops2.Count && j < array2.Length; j++)
			{
				colorStops2[j].StopAnimation("Offset");
				colorStops2[j].Offset = array2[j];
			}
			FadeVisual(shimmer, 0f, 0.25);
			FadeVisual(glowHolder, 0.82f, 0.25);
		}
	}

	public void Hide(bool animated = true)
	{
		DriveNotchOwnership.SetDrivingShown(shown: false);
		if ((object)window == null || !window.IsShown)
		{
			return;
		}
		int shownGeneration = ++generation;
		OverlayWindow closing = window;
		Grid grid = pill;
		ContainerVisual containerVisual = glowHolder;
		isShowingMouseHint = false;
		pill = null;
		status = null;
		statusOutgoing = null;
		hint = null;
		keyCapElement = null;
		cursor = null;
		shimmer = null;
		shimmerBrush = null;
		glowHolder = null;
		if (!animated || (object)grid == null)
		{
			closing.HideWindow();
			closing.Root.Children.Clear();
			return;
		}
		Compositor compositor = Ui.Compositor;
		if ((object)containerVisual != null)
		{
			FadeVisual(containerVisual, 0f, 0.3);
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(0.35f, 0.35f, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(1f, 1f)));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.28);
		vector3KeyFrameAnimation.Target = "Scale";
		grid.StartAnimation(vector3KeyFrameAnimation);
		Anim.Opacity(grid, 0f, 0.3);
		DispatcherQueueTimer dispatcherQueueTimer = closing.DispatcherQueue.CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.3);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (generation == shownGeneration)
			{
				closing.HideWindow();
				closing.Root.Children.Clear();
			}
		};
		dispatcherQueueTimer.Start();
	}

	private void AddEdgeGlow(Grid root, double width, double height, Color accent, double scale)
	{
		Compositor compositor = Ui.Compositor;
		Grid grid = new Grid
		{
			Width = width,
			Height = height,
			IsHitTestVisible = false
		};
		root.Children.Add(grid);
		Vector2 vector = new Vector2((float)width, (float)height);
		CompositionGraphicsDevice graphics = CanvasComposition.CreateCompositionGraphicsDevice(compositor, CanvasDevice.GetSharedDevice());
		double depth = Math.Min(130.0, height * 0.12);
		(byte[] Bgra, int Width, int Height) tuple = DriveArt.EdgeGlowMask(width, height, depth);
		byte[] item = tuple.Bgra;
		int item2 = tuple.Width;
		int item3 = tuple.Height;
		CompositionDrawingSurface surface = BitmapSurface(graphics, item, item2, item3);
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(surface);
		compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
		double num = Math.Sqrt(width * width + height * height);
		CompositionDrawingSurface surface2 = BitmapSurface(graphics, DriveArt.ConicGradient(512, DriveArt.CloudColors(accent)), 512, 512);
		CompositionSurfaceBrush compositionSurfaceBrush2 = compositor.CreateSurfaceBrush(surface2);
		compositionSurfaceBrush2.Stretch = CompositionStretch.Fill;
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Size = vector;
		ContainerVisual containerVisual2 = compositor.CreateContainerVisual();
		containerVisual2.Size = vector;
		containerVisual2.CenterPoint = new Vector3(vector / 2f, 0f);
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2((float)num);
		spriteVisual.Offset = new Vector3((float)((width - num) / 2.0), (float)((height - num) / 2.0), 0f);
		spriteVisual.CenterPoint = new Vector3((float)num / 2f, (float)num / 2f, 0f);
		spriteVisual.Brush = compositionSurfaceBrush2;
		containerVisual2.Children.InsertAtTop(spriteVisual);
		containerVisual.Children.InsertAtTop(containerVisual2);
		CompositionVisualSurface compositionVisualSurface = compositor.CreateVisualSurface();
		compositionVisualSurface.SourceVisual = containerVisual;
		compositionVisualSurface.SourceSize = vector;
		CompositionSurfaceBrush compositionSurfaceBrush3 = compositor.CreateSurfaceBrush(compositionVisualSurface);
		compositionSurfaceBrush3.Stretch = CompositionStretch.None;
		CompositionMaskBrush compositionMaskBrush = compositor.CreateMaskBrush();
		compositionMaskBrush.Source = compositionSurfaceBrush3;
		compositionMaskBrush.Mask = compositionSurfaceBrush;
		SpriteVisual spriteVisual2 = compositor.CreateSpriteVisual();
		spriteVisual2.Size = vector;
		spriteVisual2.Brush = compositionMaskBrush;
		ContainerVisual containerVisual3 = compositor.CreateContainerVisual();
		containerVisual3.Size = vector;
		containerVisual3.Children.InsertAtTop(spriteVisual2);
		ContainerVisual containerVisual4 = compositor.CreateContainerVisual();
		containerVisual4.Size = vector;
		containerVisual4.Opacity = 0f;
		containerVisual4.Children.InsertAtTop(containerVisual3);
		ElementCompositionPreview.SetElementChildVisual(grid, containerVisual4);
		glowHolder = containerVisual4;
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 0.82f, compositor.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.58f, 1f)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.7);
		containerVisual4.StartAnimation("Opacity", scalarKeyFrameAnimation);
		if (!reduceMotion)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = compositor.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation2.InsertKeyFrame(0f, -180f);
			scalarKeyFrameAnimation2.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.7f), new Vector2(0.3f, 1f)));
			scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(1.4);
			containerVisual2.StartAnimation("RotationAngleInDegrees", scalarKeyFrameAnimation2);
			ScalarKeyFrameAnimation scalarKeyFrameAnimation3 = compositor.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation3.InsertKeyFrame(0f, 0f, compositor.CreateLinearEasingFunction());
			scalarKeyFrameAnimation3.InsertKeyFrame(1f, 360f, compositor.CreateLinearEasingFunction());
			scalarKeyFrameAnimation3.Duration = TimeSpan.FromSeconds(16L);
			scalarKeyFrameAnimation3.DelayTime = TimeSpan.FromSeconds(1.4);
			scalarKeyFrameAnimation3.IterationBehavior = AnimationIterationBehavior.Forever;
			spriteVisual.StartAnimation("RotationAngleInDegrees", scalarKeyFrameAnimation3);
			ScalarKeyFrameAnimation scalarKeyFrameAnimation4 = compositor.CreateScalarKeyFrameAnimation();
			CubicBezierEasingFunction easingFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));
			scalarKeyFrameAnimation4.InsertKeyFrame(0f, 0.72f);
			scalarKeyFrameAnimation4.InsertKeyFrame(0.5f, 1f, easingFunction);
			scalarKeyFrameAnimation4.InsertKeyFrame(1f, 0.72f, easingFunction);
			scalarKeyFrameAnimation4.Duration = TimeSpan.FromSeconds(5.2);
			scalarKeyFrameAnimation4.IterationBehavior = AnimationIterationBehavior.Forever;
			containerVisual3.StartAnimation("Opacity", scalarKeyFrameAnimation4);
		}
	}

	private static CompositionDrawingSurface BitmapSurface(CompositionGraphicsDevice graphics, byte[] bgra, int width, int height)
	{
		CompositionDrawingSurface compositionDrawingSurface = graphics.CreateDrawingSurface(new Size(width, height), Microsoft.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
		using CanvasBitmap bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), bgra, width, height, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 96f, CanvasAlphaMode.Premultiplied);
		using (CanvasDrawingSession canvasDrawingSession = CanvasComposition.CreateDrawingSession(compositionDrawingSurface))
		{
			canvasDrawingSession.Clear(Color.FromArgb(0, 0, 0, 0));
			canvasDrawingSession.DrawImage(bitmap, new Rect(0f, 0f, width, height));
		}
		return compositionDrawingSurface;
	}

	private void AddPill(Grid root, double screenWidth, Color accent, string statusText, double scale)
	{
		Compositor compositor = Ui.Compositor;
		double pillWidth = PillWidth;
		double pillHeight = PillHeight;
		Grid grid = new Grid
		{
			Width = pillWidth,
			Height = pillHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness((screenWidth - pillWidth) / 2.0, 0.0, 0.0, 0.0),
			IsHitTestVisible = false
		};
		Canvas canvas = new Canvas
		{
			Width = pillWidth,
			Height = pillHeight
		};
		grid.Children.Add(canvas);
		root.Children.Add(grid);
		CompositionPath path = new CompositionPath(DriveArt.NotchAttachedPath(CanvasDevice.GetSharedDevice(), pillWidth, pillHeight, 24.0));
		CompositionPathGeometry geometry = compositor.CreatePathGeometry(path);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(geometry);
		compositionSpriteShape.FillBrush = compositor.CreateColorBrush(Colors.Black);
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2((float)pillWidth, (float)pillHeight);
		shapeVisual.Shapes.Add(compositionSpriteShape);
		CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(compositor.CreatePathGeometry(path));
		compositionSpriteShape2.FillBrush = compositor.CreateColorBrush(Colors.Black);
		ShapeVisual shapeVisual2 = compositor.CreateShapeVisual();
		shapeVisual2.Size = shapeVisual.Size;
		shapeVisual2.Shapes.Add(compositionSpriteShape2);
		CompositionVisualSurface compositionVisualSurface = compositor.CreateVisualSurface();
		compositionVisualSurface.SourceVisual = shapeVisual2;
		compositionVisualSurface.SourceSize = shapeVisual.Size;
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(compositionVisualSurface);
		compositionSurfaceBrush.Stretch = CompositionStretch.None;
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.Color = DS.Black(0.4);
		dropShadow.BlurRadius = 36f;
		dropShadow.Offset = new Vector3(0f, 6f, 0f);
		dropShadow.Mask = compositionSurfaceBrush;
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = shapeVisual.Size;
		spriteVisual.Shadow = dropShadow;
		CompositionSpriteShape compositionSpriteShape3 = compositor.CreateSpriteShape(compositor.CreatePathGeometry(path));
		compositionSpriteShape3.StrokeBrush = compositor.CreateColorBrush(accent.WithAlpha(0.35));
		compositionSpriteShape3.StrokeThickness = 1f;
		ShapeVisual shapeVisual3 = compositor.CreateShapeVisual();
		shapeVisual3.Size = shapeVisual.Size;
		shapeVisual3.Shapes.Add(compositionSpriteShape3);
		shapeVisual3.Clip = compositor.CreateInsetClip(0f, (float)(pillHeight - 37.8), 0f, 0f);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Size = shapeVisual.Size;
		containerVisual.Children.InsertAtTop(spriteVisual);
		containerVisual.Children.InsertAtTop(shapeVisual);
		containerVisual.Children.InsertAtTop(shapeVisual3);
		Grid grid2 = new Grid
		{
			Width = pillWidth,
			Height = pillHeight
		};
		ElementCompositionPreview.SetElementChildVisual(grid2, containerVisual);
		canvas.Children.Add(grid2);
		double num = pillHeight - 27.0;
		double num2 = 10.0 + 22.0;
		Image image = new Image
		{
			Width = 44.0,
			Height = 44.0,
			Margin = new Thickness(-4.0),
			Source = DriveArt.DriveChip(28.0, 44.0, scale),
			Stretch = Stretch.Fill
		};
		Grid grid3 = new Grid
		{
			Width = 36.0,
			Height = 36.0
		};
		grid3.Children.Add(image);
		Canvas.SetLeft(grid3, num2 + 14.0 - 18.0);
		Canvas.SetTop(grid3, num - 18.0);
		image.CenterPoint = new Vector3(22f, 22f, 0f);
		canvas.Children.Add(grid3);
		if (!reduceMotion)
		{
			Anim.LoopVector3(grid3, "Translation", 2.8, (double t) => new Vector3(0f, (float)(1.5 * Math.Cos(Math.PI * t / 1.4)), 0f), 64);
		}
		double num3 = num2 + 28.0 + 12.0;
		double width = pillWidth - num3 - num2 - 30.0 - 92.0 - 14.0;
		TextBlock textBlock = Text("Kursor is driving", 13.0, FontWeights.SemiBold, Colors.White, width);
		Place(canvas, textBlock, num3, pillHeight - 28.0 - 18.0);
		Grid grid4 = new Grid
		{
			Width = width,
			Height = 16.0
		};
		TextBlock textBlock2 = Text("", 11.0, FontWeights.Normal, DS.White(0.62), width);
		textBlock2.Opacity = 0.0;
		TextBlock item = Text(statusText, 11.0, FontWeights.Normal, DS.White(0.62), width);
		grid4.Children.Add(textBlock2);
		grid4.Children.Add(item);
		Place(canvas, grid4, num3, pillHeight - 10.0 - 16.0);
		Border border = new Border
		{
			Width = 30.0,
			Height = 20.0,
			CornerRadius = new CornerRadius(5.0),
			Background = Ui.Brush(DS.White(0.9)),
			Child = new TextBlock
			{
				Text = "esc",
				FontSize = 10.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = Ui.Brush(DS.Black(0.8)),
				FontFamily = DS.Fonts.For(10.5),
				IsTextScaleFactorEnabled = false,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
				Height = 14.0
			}
		};
		Place(canvas, border, pillWidth - num2 - 92.0 - 30.0, pillHeight - 17.0 - 20.0);
		TextBlock textBlock3 = Text("to stop", 11.0, FontWeights.Normal, DS.White(0.5), 92.0);
		Place(canvas, textBlock3, pillWidth - num2 - 92.0 + 6.0, pillHeight - 19.0 - 16.0);
		SpriteVisual spriteVisual2 = compositor.CreateSpriteVisual();
		spriteVisual2.Size = new Vector2((float)(pillWidth - num2 * 2.0), 1.5f);
		spriteVisual2.Offset = new Vector3((float)num2, (float)(pillHeight - 5.0 - 1.5), 0f);
		CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
		compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0.5f);
		compositionLinearGradientBrush.EndPoint = new Vector2(1f, 0.5f);
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(-0.4f, accent.WithAlpha(0.0)));
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(-0.2f, DS.White(0.95)));
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, accent.WithAlpha(0.0)));
		spriteVisual2.Brush = compositionLinearGradientBrush;
		spriteVisual2.Opacity = 0f;
		Grid grid5 = new Grid
		{
			Width = pillWidth,
			Height = pillHeight,
			IsHitTestVisible = false
		};
		ElementCompositionPreview.SetElementChildVisual(grid5, spriteVisual2);
		canvas.Children.Add(grid5);
		pill = grid;
		status = item;
		statusOutgoing = textBlock2;
		hint = textBlock3;
		keyCapElement = border;
		statusTextBehindHint = statusText;
		isShowingMouseHint = false;
		cursor = image;
		shimmer = spriteVisual2;
		shimmerBrush = compositionLinearGradientBrush;
		grid.CenterPoint = new Vector3((float)pillWidth / 2f, 0f, 0f);
		if (reduceMotion)
		{
			grid.Opacity = 0.0;
			Anim.Opacity(grid, 1f, 0.25);
			return;
		}
		grid.Scale = Vector3.One;
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = SpringCurve.Scale(compositor, GrowSpring, new Vector3((float)(60.0 / pillWidth), (float)(8.0 / pillHeight), 1f), Vector3.One);
		vector3KeyFrameAnimation.Target = "Scale";
		grid.StartAnimation(vector3KeyFrameAnimation);
		UIElement[] array = new UIElement[5] { grid3, textBlock, grid4, border, textBlock3 };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Opacity = 0.0;
			Anim.Opacity(array[i], 1f, 0.22, compositor.CreateLinearEasingFunction(), 0.16 + (double)i * 0.035);
		}
	}

	private static TextBlock Text(string text, double size, FontWeight weight, Color color, double width)
	{
		TextBlock textBlock = Ui.Text(text, size, weight, color);
		textBlock.Width = width;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock.TextWrapping = TextWrapping.NoWrap;
		return textBlock;
	}

	private static void Place(Canvas canvas, FrameworkElement element, double x, double y)
	{
		Canvas.SetLeft(element, x);
		Canvas.SetTop(element, y);
		canvas.Children.Add(element);
	}

	private static void FadeVisual(Visual visual, float to, double seconds)
	{
		Compositor compositor = visual.Compositor;
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		visual.StartAnimation("Opacity", scalarKeyFrameAnimation);
	}
}
