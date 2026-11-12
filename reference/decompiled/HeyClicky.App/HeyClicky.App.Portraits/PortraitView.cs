using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Portraits;

public sealed class PortraitView : Grid
{
	private sealed class MorphPath
	{
		public readonly Canvas Root = new Canvas
		{
			Width = 320.0,
			Height = 400.0
		};

		private readonly Path[] paths = new Path[2];

		private int active;

		private PPath? current;

		public MorphPath(Color? fill, Color? stroke, double lineWidth, PPath initial)
		{
			for (int i = 0; i < 2; i++)
			{
				paths[i] = Shape(initial, fill, stroke, lineWidth);
				paths[i].Opacity = ((i == 0) ? 1 : 0);
				Root.Children.Add(paths[i]);
			}
		}

		public void Set(PPath path, bool animated)
		{
			current = path;
			if (!animated)
			{
				paths[active].Data = path.ToXaml();
				paths[active].Opacity = 1.0;
				paths[1 - active].Opacity = 0.0;
			}
			else
			{
				Path obj = paths[1 - active];
				obj.Data = path.ToXaml();
				Fade(obj, 1.0, animated: true);
				Fade(paths[active], 0.0, animated: true);
				active = 1 - active;
			}
		}

		public void Pulse(PPath mood, double seconds)
		{
			Path e = paths[active];
			Path path = paths[1 - active];
			path.Data = mood.ToXaml();
			float[] times = new float[4] { 0f, 0.18f, 0.7f, 1f };
			Keys(e, "Opacity", new float[4] { 1f, 0f, 0f, 1f }, times, seconds);
			Keys(path, "Opacity", new float[4] { 0f, 1f, 1f, 0f }, times, seconds);
		}
	}

	private readonly PortraitAppearance a;

	private readonly PortraitPack pack;

	private readonly bool caretEyes;

	private readonly Canvas stage = new Canvas
	{
		Width = 320.0,
		Height = 400.0,
		IsHitTestVisible = false
	};

	private readonly Canvas breath = new Canvas
	{
		Width = 320.0,
		Height = 400.0
	};

	private readonly Canvas hairBack = new Canvas
	{
		Width = 320.0,
		Height = 400.0
	};

	private readonly Canvas head = new Canvas
	{
		Width = 320.0,
		Height = 400.0
	};

	private readonly Canvas hairFront = new Canvas
	{
		Width = 320.0,
		Height = 400.0
	};

	private readonly Grid art = new Grid();

	private readonly List<Canvas> eyes = new List<Canvas>();

	private readonly List<UIElement> pupils = new List<UIElement>();

	private readonly List<MorphPath> carets = new List<MorphPath>();

	private readonly List<MorphPath> lids = new List<MorphPath>();

	private readonly List<Path> hearts = new List<Path>();

	private readonly List<Path> cheeks = new List<Path>();

	private readonly List<Canvas> brows = new List<Canvas>();

	private readonly List<(double X, double Y)> eyeCenters = new List<(double, double)>();

	private MorphPath? mouth;

	private Path? paint;

	private Ellipse? plate;

	private PortraitExpression expression;

	private (double X, double Y) gaze;

	private (double X, double Y)? gazeTarget;

	private double rotationDeg;

	private double hairShiftX;

	private bool live;

	private bool loaded;

	private bool pointerInside;

	private bool breathing;

	private DispatcherQueueTimer? idleTimer;

	private readonly Random rng = new Random();

	private static readonly CanvasDevice Device = CanvasDevice.GetSharedDevice();

	private double scale;

	private static Compositor C => CompositionTarget.GetCompositorForCurrentThread();

	public bool AnimatesWhileIdle { get; init; }

	private bool IdleLifeEnabled => AnimatesWhileIdle;

	public bool TracksPointer { get; init; } = true;

	public bool BlinksOnPointerEntry { get; init; } = true;

	public PortraitAppearance Appearance => a;

	private (double X, double Y, double W, double H) Viewport
	{
		get
		{
			(int, int, int, int) tuple = pack switch
			{
				PortraitPack.ClassicClouds => (-32, -28, 384, 384), 
				PortraitPack.CloudClub => (0, 4, 320, 320), 
				_ => (0, -20, 320, 320), 
			};
			return (X: tuple.Item1, Y: tuple.Item2, W: tuple.Item3, H: tuple.Item4);
		}
	}

	private bool ClosedEyes
	{
		get
		{
			if (!expression.HasClosedEyes())
			{
				if (pack == PortraitPack.ClassicClouds)
				{
					return expression == PortraitExpression.Sad;
				}
				return false;
			}
			return true;
		}
	}

	public PortraitExpression Expression
	{
		get
		{
			return expression;
		}
		set
		{
			if (value != expression)
			{
				expression = value;
				ApplyExpression(CanAnimate);
				(double, double)? tuple = gazeTarget;
				if (!tuple.HasValue && !pointerInside)
				{
					Look(expression.RestingGaze(), CanAnimate);
				}
			}
		}
	}

	private bool CanAnimate
	{
		get
		{
			if (live && loaded)
			{
				return !Anim.Instant;
			}
			return false;
		}
	}

	private double CheekRest
	{
		get
		{
			bool flag;
			switch (expression)
			{
			case PortraitExpression.Shy:
			case PortraitExpression.Laughing:
			case PortraitExpression.Loving:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			return Math.Max(flag ? 0.6 : ((expression == PortraitExpression.Happy) ? 0.22 : 0.0), (pack == PortraitPack.StorybookSpirits) ? 0.32 : 0.0);
		}
	}

	public PortraitView(PortraitAppearance appearance, double width, double height, PortraitExpression expression = PortraitExpression.Curious, bool showsAvatarBackground = true, Color? glow = null, double glowRadius = 0.0)
	{
		a = appearance;
		pack = appearance.Pack;
		caretEyes = pack == PortraitPack.ClassicClouds;
		this.expression = expression;
		base.Width = width;
		base.Height = height;
		base.IsHitTestVisible = true;
		AutomationProperties_SetName("HeyClicky");
		double num = Math.Min(width, height);
		if (showsAvatarBackground)
		{
			plate = new Ellipse
			{
				Width = num,
				Height = num,
				Fill = Ui.Brush(appearance.Background),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			art.Children.Add(plate);
		}
		if (glow.HasValue)
		{
			Color valueOrDefault = glow.GetValueOrDefault();
			if (glowRadius > 0.0)
			{
				base.Children.Add(GlowHost(valueOrDefault, glowRadius));
			}
		}
		art.Children.Add(stage);
		base.Children.Add(art);
		stage.Children.Add(breath);
		breath.Children.Add(hairBack);
		breath.Children.Add(head);
		UIElement[] array = new UIElement[2] { hairBack, head };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CenterPoint = new Vector3(160f, 192f, 0f);
		}
		BuildArtwork(showsAvatarBackground);
		Layout(width, height);
		ApplyExpression(animated: false);
		Look(expression.RestingGaze(), animated: false);
		if (showsAvatarBackground)
		{
			CompositionEllipseGeometry compositionEllipseGeometry = C.CreateEllipseGeometry();
			compositionEllipseGeometry.Center = new Vector2((float)width / 2f, (float)height / 2f);
			compositionEllipseGeometry.Radius = new Vector2((float)num / 2f);
			ElementCompositionPreview.GetElementVisual(art).Clip = C.CreateGeometricClip(compositionEllipseGeometry);
		}
		base.Background = Ui.Transparent;
		base.PointerEntered += OnPointerEntered;
		base.PointerMoved += OnPointerMoved;
		base.PointerExited += OnPointerExited;
		base.Loaded += delegate
		{
			loaded = true;
			live = true;
			base.IsHitTestVisible = TracksPointer;
			UpdateActivity();
		};
		base.Unloaded += delegate
		{
			loaded = false;
			UpdateActivity();
		};
	}

	private void AutomationProperties_SetName(string name)
	{
		AutomationProperties.SetName(this, name);
	}

	private void Layout(double w, double h)
	{
		(double, double, double, double) viewport = Viewport;
		scale = Math.Max(0.01, Math.Min(w / viewport.Item3, h / viewport.Item4));
		stage.RenderTransform = new MatrixTransform
		{
			Matrix = new Matrix(scale, 0.0, 0.0, scale, w / 2.0 - (viewport.Item1 + viewport.Item3 / 2.0) * scale, h / 2.0 - (viewport.Item2 + viewport.Item4 / 2.0) * scale)
		};
	}

	private (double X, double Y) DrawingPoint(Point p)
	{
		(double, double, double, double) viewport = Viewport;
		return (X: viewport.Item1 + (p.X - (base.Width - viewport.Item3 * scale) / 2.0) / scale, Y: viewport.Item2 + (p.Y - (base.Height - viewport.Item4 * scale) / 2.0) / scale);
	}

	private void BuildArtwork(bool avatarBackground)
	{
		if (pack == PortraitPack.ClassicClouds)
		{
			BuildClassicCloud(avatarBackground);
			return;
		}
		if (pack == PortraitPack.CloudClub)
		{
			BuildCloudClub();
			return;
		}
		foreach (HairPiece item in PortraitArt.Back(a.Hairstyle))
		{
			AddPiece(item, hairBack);
		}
		PortraitPack portraitPack = pack;
		bool flag = ((portraitPack == PortraitPack.ArcadeAllStars || portraitPack == PortraitPack.StorybookSpirits) ? true : false);
		double foreheadTop = (flag ? 116 : ((a.Hairstyle == Hairstyle.Topknot) ? 82 : ((a.Hairstyle == Hairstyle.TwinBuns) ? 72 : ((a.Hairstyle == Hairstyle.Braids) ? 68 : 58))));
		head.Children.Add(Shape(PortraitArt.FacePath(foreheadTop), a.Skin, null));
		head.Children.Add(Shape(PortraitArt.JawPath(), null, PortraitData.Ink));
		double[] array = new double[2] { 74.0, 225.0 };
		foreach (double num in array)
		{
			(double, int, int, int) tuple = ((pack == PortraitPack.StorybookSpirits) ? ((num == 74.0) ? 82 : 209, 191, 36, 20) : (num, 200, 28, 12));
			Path path = Shape(PPath.Oval(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4), PortraitData.Rgb(15172239u), null);
			path.Opacity = 0.0;
			head.Children.Add(path);
			cheeks.Add(path);
		}
		if (pack == PortraitPack.KiCrew)
		{
			head.Children.Add(Shape(new PPath().M(162.0, 185.0).L(157.0, 197.0).Q(158.0, 200.0, 164.0, 198.0), null, PortraitData.Ink));
		}
		else
		{
			Hairstyle hairstyle = a.Hairstyle;
			if ((hairstyle == Hairstyle.FoxHood || hairstyle == Hairstyle.BearHood) ? true : false)
			{
				head.Children.Add(Shape(PPath.Oval(125.0, 194.0, 76.0, 41.0), a.Background, null));
			}
		}
		MakeEye(108.0, 164.0);
		MakeEye(217.0, 164.0);
		Color value = ((pack == PortraitPack.StorybookSpirits) ? PortraitData.StorybookFaceInk : PortraitData.Ink);
		mouth = new MorphPath(value, value, 2.3, PortraitArt.MouthPath(expression));
		if (a.Hairstyle == Hairstyle.Robot)
		{
			mouth.Root.Translation = new Vector3(0f, -12f, 0f);
		}
		head.Children.Add(mouth.Root);
		head.Children.Add(hairFront);
		foreach (HairPiece item2 in PortraitArt.Front(a.Hairstyle))
		{
			AddPiece(item2, hairFront);
		}
	}

	private void BuildClassicCloud(bool avatarBackground)
	{
		PPath pPath = PortraitArt.ClassicSilhouette(a);
		(double, double, double, double) value = pPath.Bounds();
		if (avatarBackground)
		{
			head.Children.Add(HeadShadow(pPath));
		}
		paint = new Path
		{
			Data = pPath.ToXaml(),
			Fill = Ui.Brush(a.Hair)
		};
		WriteableBitmap writeableBitmap = PortraitWash.For(a, value);
		if ((object)writeableBitmap != null)
		{
			paint.Fill = new ImageBrush
			{
				ImageSource = writeableBitmap,
				Stretch = Stretch.Fill
			};
		}
		head.Children.Add(paint);
		for (int i = 0; i < 2; i++)
		{
			MakeCaretEye(PortraitArt.ClassicEyeCenter(i, a));
		}
	}

	private void BuildCloudClub()
	{
		PPath pPath = PortraitArt.CloudClubSilhouette(a.Hairstyle);
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(112.0, 52.0),
			EndPoint = new Point(208.0, 264.0)
		};
		linearGradientBrush.GradientStops.Add(new GradientStop
		{
			Color = a.HairColor.CloudFill(0.87),
			Offset = 0.0
		});
		linearGradientBrush.GradientStops.Add(new GradientStop
		{
			Color = a.HairColor.CloudFill(0.55),
			Offset = 0.5
		});
		linearGradientBrush.GradientStops.Add(new GradientStop
		{
			Color = a.HairColor.CloudFill(0.12),
			Offset = 1.0
		});
		head.Children.Add(new Path
		{
			Data = pPath.ToXaml(),
			Fill = linearGradientBrush,
			Stroke = Ui.Brush(PortraitData.Ink),
			StrokeThickness = 3.2,
			StrokeLineJoin = PenLineJoin.Round
		});
		double[] array = new double[2] { 74.0, 225.0 };
		for (int i = 0; i < array.Length; i++)
		{
			Path item = Shape(PPath.Oval(array[i], 204.0, 28.0, 12.0), PortraitData.Rgb(14650524u), null);
			head.Children.Add(item);
			cheeks.Add(item);
		}
		MakeEye(108.0, 164.0);
		MakeEye(217.0, 164.0);
		mouth = new MorphPath(PortraitData.Ink, PortraitData.Ink, 2.3, PortraitArt.MouthPath(expression));
		head.Children.Add(mouth.Root);
	}

	private void AddPiece(HairPiece piece, Canvas parent)
	{
		Color? fill = piece.Fill switch
		{
			PieceFill.Hair => a.Hair, 
			PieceFill.HairShadow => Tinted(a.Hair, 0.0, 0.23), 
			PieceFill.HairHighlight => Tinted(a.Hair, 1.0, 0.16), 
			PieceFill.Outfit => a.Outfit, 
			PieceFill.Accent => a.Accent, 
			_ => null, 
		};
		parent.Children.Add(Shape(piece.Path, fill, piece.Stroked ? new Color?(PortraitData.Ink) : ((Color?)null)));
	}

	private static Color Tinted(Color c, double toward, double amount)
	{
		return PortraitData.Rgb(F(c.R), F(c.G), F(c.B));
		double F(byte v)
		{
			double num = (double)(int)v / 255.0;
			return num + (toward - num) * amount;
		}
	}

	private static Path Shape(PPath path, Color? fill, Color? stroke, double lineWidth = 3.2)
	{
		Path obj = new Path
		{
			Data = path.ToXaml()
		};
		object fill2;
		if (fill.HasValue)
		{
			Color valueOrDefault = fill.GetValueOrDefault();
			fill2 = Ui.Brush(valueOrDefault);
		}
		else
		{
			fill2 = null;
		}
		obj.Fill = (Brush)fill2;
		object stroke2;
		if (stroke.HasValue)
		{
			Color valueOrDefault2 = stroke.GetValueOrDefault();
			stroke2 = Ui.Brush(valueOrDefault2);
		}
		else
		{
			stroke2 = null;
		}
		obj.Stroke = (Brush)stroke2;
		obj.StrokeThickness = lineWidth;
		obj.StrokeLineJoin = PenLineJoin.Round;
		obj.StrokeStartLineCap = PenLineCap.Round;
		obj.StrokeEndLineCap = PenLineCap.Round;
		return obj;
	}

	private void MakeEye(double cx, double cy)
	{
		bool flag = pack == PortraitPack.KiCrew;
		bool flag2 = pack == PortraitPack.ArcadeAllStars;
		bool flag3 = pack == PortraitPack.StorybookSpirits;
		bool num = pack.IsCloud();
		double num2 = (num ? 72 : (flag ? 86 : (flag3 ? 44 : (flag2 ? 82 : 90))));
		double num3 = (num ? 86 : (flag ? 64 : (flag3 ? 54 : (flag2 ? 92 : 112))));
		Canvas canvas = new Canvas
		{
			Width = num2,
			Height = num3,
			CenterPoint = new Vector3((float)num2 / 2f, (float)num3 / 2f, 0f)
		};
		Canvas.SetLeft(canvas, cx - num2 / 2.0);
		Canvas.SetTop(canvas, cy - num3 / 2.0);
		PPath path;
		if (flag3)
		{
			path = PortraitArt.StorybookEyePath();
		}
		else if (!flag)
		{
			path = ((a.Hairstyle != Hairstyle.Robot) ? PPath.Oval(0.0, 0.0, num2, num3) : PPath.Rounded(0.0, 0.0, num2, num3, 22.0));
		}
		else
		{
			PPath pPath = new PPath().M(2.0, 9.0).Q(43.0, 11.0, 84.0, 24.0).Q(83.0, 51.0, 72.0, 59.0)
				.Q(41.0, 65.0, 15.0, 58.0)
				.Q(4.0, 46.0, 2.0, 9.0)
				.Z();
			path = ((cx > 162.0) ? pPath.Transformed(new Matrix3x2(-1f, 0f, 0f, 1f, (float)num2, 0f)) : pPath);
		}
		Canvas canvas2 = new Canvas
		{
			Width = num2,
			Height = num3
		};
		Color value = (flag3 ? PortraitData.StorybookFaceInk : PortraitData.Rgb(0.99, 0.99, 0.98));
		canvas2.Children.Add(Shape(path, value, null));
		Canvas canvas3 = new Canvas
		{
			Width = num2,
			Height = num3
		};
		(double, double, double, double) valueTuple;
		if (!flag3)
		{
			(int, int, double, double) tuple = (flag ? (25, 9, 38.0, 54.0) : (6, 1, num2 - 12.0, num3 - 6.0));
			valueTuple = (tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
		}
		else
		{
			valueTuple = (3.0, 6.0, 38.0, 47.0);
		}
		(double, double, double, double) tuple2 = valueTuple;
		(double, double, double, double) valueTuple2;
		if (!flag3)
		{
			(int, int, double, double) tuple = (flag ? (35, 11, 22.0, 46.0) : (16, 4, num2 - 25.0, num3 - 16.0));
			valueTuple2 = (tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
		}
		else
		{
			valueTuple2 = (2.0, -3.0, 36.0, 49.0);
		}
		(double, double, double, double) tuple3 = valueTuple2;
		(double, double, double, double) valueTuple3;
		if (!num)
		{
			(int, int, int, int) tuple4 = (flag ? (46, 17, 8, 12) : (flag3 ? (25, 11, 6, 8) : (flag2 ? (44, 16, 18, 22) : (49, 18, 20, 24))));
			valueTuple3 = (tuple4.Item1, tuple4.Item2, tuple4.Item3, tuple4.Item4);
		}
		else
		{
			valueTuple3 = (38.0, 15.0, 15.0, 18.0);
		}
		(double, double, double, double) tuple5 = valueTuple3;
		canvas3.Children.Add(Shape(PPath.Oval(tuple2.Item1, tuple2.Item2, tuple2.Item3, tuple2.Item4), a.Eyes, null));
		canvas3.Children.Add(Shape(PPath.Oval(tuple3.Item1, tuple3.Item2, tuple3.Item3, tuple3.Item4), flag3 ? PortraitData.StorybookFaceInk : PortraitData.Ink, null));
		canvas3.Children.Add(Shape(PPath.Oval(tuple5.Item1, tuple5.Item2, tuple5.Item3, tuple5.Item4), PortraitData.Rgb(1.0, 1.0, 1.0), null));
		Path path2 = Shape(flag ? PortraitArt.HeartPath(0.65, 0.55, 17.0, 13.0) : PortraitArt.HeartPath(num2 / 78.0, num3 / 100.0), PortraitData.Rgb(14836871u), null);
		path2.Opacity = 0.0;
		canvas2.Children.Add(canvas3);
		canvas2.Children.Add(path2);
		ClipTo(canvas2, path);
		canvas.Children.Add(canvas2);
		if (!flag3)
		{
			canvas.Children.Add(Shape(path, null, PortraitData.Ink));
		}
		head.Children.Add(canvas);
		eyes.Add(canvas);
		pupils.Add(canvas3);
		hearts.Add(path2);
		eyeCenters.Add((cx, cy));
		double num4 = (flag3 ? 18 : 30);
		MorphPath morphPath = new MorphPath(null, PortraitData.Ink, 3.2, new PPath().M(cx - num4, cy + 3.0).Q(cx, cy + 17.0, cx + num4, cy + 3.0));
		morphPath.Root.Opacity = 0.0;
		head.Children.Add(morphPath.Root);
		lids.Add(morphPath);
		Canvas canvas4 = new Canvas
		{
			Width = 0.0,
			Height = 0.0,
			Opacity = 0.0
		};
		Canvas.SetLeft(canvas4, cx);
		Canvas.SetTop(canvas4, cy - num3 / 2.0 - 2.0);
		canvas4.Children.Add(Shape(new PPath().M(-17.0, 0.0).Q(0.0, -4.0, 17.0, 0.0), null, PortraitData.Ink, 3.4));
		head.Children.Add(canvas4);
		brows.Add(canvas4);
	}

	private void MakeCaretEye((double X, double Y) center)
	{
		Color value = ((pack == PortraitPack.ClassicClouds) ? PortraitData.ClassicCloudEyeColor : a.HairColor.CloudFeatureInk());
		Canvas canvas = new Canvas
		{
			Width = 72.0,
			Height = 86.0,
			CenterPoint = new Vector3(36f, 43f, 0f)
		};
		Canvas.SetLeft(canvas, center.X - 36.0);
		Canvas.SetTop(canvas, center.Y - 43.0);
		MorphPath morphPath = new MorphPath(null, value, 22.400000000000002, PortraitArt.CaretEyePath(expression, eyes.Count));
		canvas.Children.Add(morphPath.Root);
		head.Children.Add(canvas);
		eyes.Add(canvas);
		carets.Add(morphPath);
		eyeCenters.Add(center);
		MorphPath morphPath2 = new MorphPath(null, value, 22.400000000000002, LidPath(eyes.Count - 1, expression));
		morphPath2.Root.Opacity = 0.0;
		head.Children.Add(morphPath2.Root);
		lids.Add(morphPath2);
	}

	private PPath LidPath(int index, PortraitExpression e)
	{
		(double, double) c = eyeCenters[index];
		if (pack == PortraitPack.ClassicClouds)
		{
			return PortraitArt.ClassicClosedEyePath(e, c);
		}
		double num = ((pack == PortraitPack.StorybookSpirits) ? 18 : 30);
		double num2 = ((index == 0) ? 108 : 217);
		return new PPath().M(num2 - num, 167.0).Q(num2, (e == PortraitExpression.Laughing) ? 142 : 181, num2 + num, 167.0);
	}

	private static void ClipTo(UIElement element, PPath path)
	{
		try
		{
			CompositionPathGeometry geometry = C.CreatePathGeometry(new CompositionPath(path.ToWin2D(Device)));
			ElementCompositionPreview.GetElementVisual(element).Clip = C.CreateGeometricClip(geometry);
		}
		catch (Exception ex)
		{
			Log.Warn("portrait", "eye clip: " + ex.Message);
		}
	}

	private FrameworkElement HeadShadow(PPath silhouette)
	{
		Canvas canvas = new Canvas
		{
			Width = 320.0,
			Height = 400.0,
			IsHitTestVisible = false
		};
		try
		{
			CompositionPathGeometry geometry = C.CreatePathGeometry(new CompositionPath(silhouette.ToWin2D(Device)));
			CompositionSpriteShape compositionSpriteShape = C.CreateSpriteShape(geometry);
			compositionSpriteShape.FillBrush = C.CreateColorBrush(Colors.Black);
			ShapeVisual shapeVisual = C.CreateShapeVisual();
			shapeVisual.Size = new Vector2(320f, 400f);
			shapeVisual.Shapes.Add(compositionSpriteShape);
			CompositionVisualSurface compositionVisualSurface = C.CreateVisualSurface();
			compositionVisualSurface.SourceVisual = shapeVisual;
			compositionVisualSurface.SourceSize = new Vector2(320f, 400f);
			CompositionSurfaceBrush compositionSurfaceBrush = C.CreateSurfaceBrush(compositionVisualSurface);
			compositionSurfaceBrush.Stretch = CompositionStretch.None;
			DropShadow dropShadow = C.CreateDropShadow();
			dropShadow.Color = PortraitData.Rgb(0.16, 0.18, 0.23);
			dropShadow.Opacity = 0.2f;
			dropShadow.BlurRadius = 14f;
			dropShadow.Offset = new Vector3(0f, 4f, 0f);
			dropShadow.Mask = compositionSurfaceBrush;
			SpriteVisual spriteVisual = C.CreateSpriteVisual();
			spriteVisual.Size = new Vector2(320f, 400f);
			spriteVisual.Shadow = dropShadow;
			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);
		}
		catch (Exception ex)
		{
			Log.Warn("portrait", "head shadow: " + ex.Message);
		}
		return canvas;
	}

	private FrameworkElement GlowHost(Color color, double radius)
	{
		Grid grid = new Grid
		{
			IsHitTestVisible = false
		};
		try
		{
			CompositionVisualSurface compositionVisualSurface = C.CreateVisualSurface();
			compositionVisualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(art);
			Vector2 size = (compositionVisualSurface.SourceSize = new Vector2((float)base.Width, (float)base.Height));
			CompositionSurfaceBrush compositionSurfaceBrush = C.CreateSurfaceBrush(compositionVisualSurface);
			compositionSurfaceBrush.Stretch = CompositionStretch.None;
			DropShadow dropShadow = C.CreateDropShadow();
			dropShadow.Color = color;
			dropShadow.Opacity = 0.55f;
			dropShadow.BlurRadius = (float)(radius * 2.0);
			dropShadow.Offset = Vector3.Zero;
			dropShadow.Mask = compositionSurfaceBrush;
			SpriteVisual spriteVisual = C.CreateSpriteVisual();
			spriteVisual.Size = size;
			spriteVisual.Shadow = dropShadow;
			ElementCompositionPreview.SetElementChildVisual(grid, spriteVisual);
		}
		catch (Exception ex)
		{
			Log.Warn("portrait", "glow: " + ex.Message);
		}
		return grid;
	}

	private void ApplyExpression(bool animated)
	{
		bool closedEyes = ClosedEyes;
		for (int i = 0; i < eyes.Count; i++)
		{
			ScaleTo(eyes[i], new Vector3(1f, (float)expression.EyeOpenness(i), 1f), animated);
			Fade(eyes[i], (!closedEyes) ? 1 : 0, animated);
		}
		for (int j = 0; j < lids.Count; j++)
		{
			lids[j].Set(LidPath(j, expression), animated);
			Fade(lids[j].Root, closedEyes ? ((pack == PortraitPack.ClassicClouds && expression != PortraitExpression.Laughing) ? 0.58 : 1.0) : 0.0, animated);
		}
		foreach (Path heart in hearts)
		{
			Fade(heart, (expression == PortraitExpression.Loving) ? 1 : 0, animated);
		}
		if (caretEyes)
		{
			for (int k = 0; k < carets.Count; k++)
			{
				carets[k].Set(PortraitArt.CaretEyePath(expression, k), animated);
			}
		}
		else
		{
			foreach (UIElement pupil in pupils)
			{
				Fade(pupil, (expression != PortraitExpression.Loving) ? 1 : 0, animated);
			}
		}
		foreach (Path cheek in cheeks)
		{
			bool flag;
			switch (expression)
			{
			case PortraitExpression.Shy:
			case PortraitExpression.Laughing:
			case PortraitExpression.Loving:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			double val = (flag ? 0.6 : ((expression == PortraitExpression.Happy) ? 0.22 : 0.0));
			double val2 = ((pack == PortraitPack.StorybookSpirits) ? 0.32 : 0.0);
			Fade(cheek, Math.Max(val, val2), animated);
		}
		for (int l = 0; l < brows.Count; l++)
		{
			bool flag = pack == PortraitPack.KiCrew;
			if (!flag)
			{
				PortraitExpression portraitExpression = expression;
				bool flag2 = (((uint)(portraitExpression - 5) <= 1u || portraitExpression == PortraitExpression.Determined) ? true : false);
				flag = flag2;
			}
			bool flag3 = flag;
			Fade(brows[l], flag3 ? 1 : 0, animated);
			double radians = ((expression == PortraitExpression.Determined) ? ((l == 0) ? 0.28 : (-0.28)) : ((l == 0) ? (-0.12) : 0.08));
			Rotate(brows[l], Deg(radians), animated);
			TranslateTo(brows[l], new Vector3(0f, (expression == PortraitExpression.Surprised) ? (-8) : 5, 0f), animated);
		}
		mouth?.Set(PortraitArt.MouthPath(expression), animated);
		UpdateHead(animated);
	}

	public void Look((double X, double Y) target, bool animated = true)
	{
		if (!double.IsFinite(target.X) || !double.IsFinite(target.Y))
		{
			return;
		}
		double num = Math.Max(1.0, Math.Sqrt(target.X * target.X + target.Y * target.Y));
		(double, double) tuple = (target.X / num, target.Y / num);
		animated &= CanAnimate;
		if (animated && Math.Abs(tuple.Item1 - gaze.X) + Math.Abs(tuple.Item2 - gaze.Y) <= 0.008)
		{
			return;
		}
		gaze = tuple;
		double num2 = ((pack == PortraitPack.ClassicClouds) ? 19.2 : 6.0);
		double num3 = ((pack == PortraitPack.ClassicClouds) ? 11.52 : 4.5);
		Vector3 to = new Vector3((float)(tuple.Item1 * num2), (float)(tuple.Item2 * num3), 0f);
		if (caretEyes)
		{
			foreach (Canvas eye in eyes)
			{
				TranslateTo(eye, to, animated);
			}
			foreach (MorphPath lid in lids)
			{
				TranslateTo(lid.Root, to, animated);
			}
		}
		else
		{
			foreach (UIElement pupil in pupils)
			{
				TranslateTo(pupil, to, animated);
			}
		}
		hairShiftX = tuple.Item1 * 0.9;
		TranslateTo(hairBack, new Vector3((float)hairShiftX, 0f, 0f), animated, 0.28);
		TranslateTo(hairFront, new Vector3((float)hairShiftX, 0f, 0f), animated, 0.28);
		UpdateHead(animated);
	}

	public void SetGazeTarget((double X, double Y)? target)
	{
		gazeTarget = target;
		Look(target ?? expression.RestingGaze(), CanAnimate);
	}

	private void UpdateHead(bool animated)
	{
		rotationDeg = Deg(expression.HeadTilt() + gaze.X * 0.035);
		Rotate(head, rotationDeg, animated);
		Rotate(hairBack, rotationDeg, animated);
	}

	public void Blink()
	{
		if (ClosedEyes || !CanAnimate)
		{
			return;
		}
		for (int i = 0; i < eyes.Count; i++)
		{
			float num = (float)expression.EyeOpenness(i);
			KeysV3(eyes[i], "Scale", new float[4] { num, 0.025f, 0.025f, num }, new float[4] { 0f, 0.26f, 0.38f, 1f }, 0.19, (float v) => new Vector3(1f, v, 1f));
			Keys(eyes[i], "Opacity", new float[6] { 1f, 1f, 0f, 0f, 1f, 1f }, new float[6] { 0f, 0.16f, 0.26f, 0.38f, 0.65f, 1f }, 0.19);
			Keys(lids[i].Root, "Opacity", new float[6] { 0f, 0f, 1f, 1f, 0f, 0f }, new float[6] { 0f, 0.16f, 0.26f, 0.38f, 0.65f, 1f }, 0.19);
		}
	}

	public void Perform(PortraitReaction reaction)
	{
		if (!CanAnimate)
		{
			return;
		}
		float num = (float)Deg(expression.HeadTilt());
		switch (reaction)
		{
		case PortraitReaction.Wave:
			if (pack.IsCloud())
			{
				SmileBriefly(PortraitExpression.Happy);
				Keys(head, "Rotation", new float[5]
				{
					num,
					(float)Deg(-0.09),
					(float)Deg(0.08),
					(float)Deg(-0.04),
					num
				}, null, 1.15);
				KeysV3(head, "Translation", new float[4] { 0f, -6f, -3f, 0f }, null, 1.15, (float v) => new Vector3(0f, v, 0f));
			}
			else
			{
				Nod(-0.04);
			}
			break;
		case PortraitReaction.Wink:
			if (!ClosedEyes && eyes.Count > 0)
			{
				Keys(eyes[0], "Opacity", new float[4] { 1f, 0f, 0f, 1f }, new float[4] { 0f, 0.18f, 0.65f, 1f }, 0.6);
				Keys(lids[0].Root, "Opacity", new float[4] { 0f, 1f, 1f, 0f }, new float[4] { 0f, 0.18f, 0.65f, 1f }, 0.6);
			}
			Nod(-0.065);
			break;
		case PortraitReaction.Boop:
		{
			Blink();
			UIElement[] array = new UIElement[2] { head, hairBack };
			for (int i = 0; i < array.Length; i++)
			{
				KeysV3(array[i], "Scale", new float[3] { 1f, 0.96f, 1f }, null, 0.38, (float v) => new Vector3(v, v, 1f));
			}
			{
				foreach (Path cheek in cheeks)
				{
					Keys(cheek, "Opacity", new float[3]
					{
						(float)cheek.Opacity,
						0.8f,
						(float)CheekRest
					}, null, 0.65);
				}
				break;
			}
		}
		case PortraitReaction.Giggle:
		{
			SmileBriefly(PortraitExpression.Laughing);
			Blink();
			UIElement[] array = new UIElement[2] { head, hairBack };
			for (int i = 0; i < array.Length; i++)
			{
				Keys(array[i], "Rotation", new float[5]
				{
					num,
					(float)Deg(-0.055),
					(float)Deg(0.055),
					(float)Deg(-0.035),
					num
				}, null, 0.65);
			}
			break;
		}
		case PortraitReaction.Celebrate:
		{
			SmileBriefly(PortraitExpression.Excited);
			UIElement[] array = new UIElement[2] { head, hairBack };
			foreach (UIElement part in array)
			{
				KeysV3(part, "Translation", new float[4] { 0f, 1f, -6f, 0f }, new float[4] { 0f, 0.12f, 0.4f, 1f }, 0.92, (float v) => new Vector3((part == hairBack) ? ((float)hairShiftX) : 0f, v, 0f));
			}
			break;
		}
		case PortraitReaction.Dance:
		{
			UIElement[] array = new UIElement[2] { head, hairBack };
			for (int i = 0; i < array.Length; i++)
			{
				Keys(array[i], "Rotation", new float[6]
				{
					num,
					(float)Deg(-0.06),
					(float)Deg(0.06),
					(float)Deg(-0.06),
					(float)Deg(0.06),
					num
				}, null, 1.35);
			}
			break;
		}
		}
	}

	private void SmileBriefly(PortraitExpression mood)
	{
		mouth?.Pulse(PortraitArt.MouthPath(mood), 0.75);
	}

	private void Nod(double amount)
	{
		float num = (float)rotationDeg;
		Keys(head, "Rotation", new float[3]
		{
			num,
			num + (float)Deg(amount),
			num
		}, new float[3] { 0f, 0.38f, 1f }, 0.48);
		Keys(hairBack, "Rotation", new float[3]
		{
			num,
			num + (float)Deg(amount * 0.85),
			num
		}, new float[3] { 0f, 0.48f, 1f }, 0.56);
		Keys(hairFront, "Rotation", new float[3]
		{
			0f,
			(float)Deg(amount * 0.2),
			0f
		}, new float[3] { 0f, 0.48f, 1f }, 0.56);
	}

	private void UpdateActivity()
	{
		if (!loaded || !CanAnimate)
		{
			idleTimer?.Stop();
			pointerInside = false;
		}
		else
		{
			if (!IdleLifeEnabled)
			{
				return;
			}
			StartBreathing();
			if ((object)idleTimer == null)
			{
				idleTimer = base.DispatcherQueue.CreateTimer();
				idleTimer.IsRepeating = false;
				idleTimer.Tick += delegate
				{
					IdleBeat();
					ScheduleIdle(3.4, 6.5);
				};
			}
			ScheduleIdle(1.1, 2.8);
		}
	}

	private void ScheduleIdle(double min, double max)
	{
		if ((object)idleTimer != null && loaded)
		{
			idleTimer.Interval = TimeSpan.FromSeconds(min + rng.NextDouble() * (max - min));
			idleTimer.Start();
		}
	}

	private void IdleBeat()
	{
		if (!CanAnimate)
		{
			return;
		}
		if (!ClosedEyes)
		{
			Blink();
		}
		if (!pointerInside)
		{
			(double, double)? tuple = gazeTarget;
			if (!tuple.HasValue)
			{
				(double, double) tuple2 = expression.RestingGaze();
				Look((X: tuple2.Item1 + (rng.NextDouble() * 0.44 - 0.22), Y: tuple2.Item2 + (rng.NextDouble() * 0.24 - 0.12)));
			}
		}
	}

	private void StartBreathing()
	{
		if (!breathing)
		{
			breathing = true;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
			vector3KeyFrameAnimation.InsertKeyFrame(0f, Vector3.Zero);
			vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(0f, -1.4f, 0f), Easing.EaseInOut(C));
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(2.5);
			vector3KeyFrameAnimation.Direction = AnimationDirection.Alternate;
			vector3KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			vector3KeyFrameAnimation.Target = "Translation";
			breath.StartAnimation(vector3KeyFrameAnimation);
		}
	}

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		if (!TracksPointer || !CanAnimate)
		{
			return;
		}
		pointerInside = true;
		if (BlinksOnPointerEntry)
		{
			(double, double)? tuple = gazeTarget;
			if (!tuple.HasValue)
			{
				Blink();
			}
		}
		OnPointerMoved(sender, e);
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (TracksPointer && CanAnimate)
		{
			(double, double)? tuple = gazeTarget;
			if (!tuple.HasValue && expression != PortraitExpression.Sleepy)
			{
				pointerInside = true;
				(double X, double Y) tuple2 = DrawingPoint(e.GetCurrentPoint(this).Position);
				double num = (tuple2.X - 162.0) / 140.0;
				double num2 = (tuple2.Y - 164.0) / 145.0;
				double num3 = Math.Max(1.0, Math.Sqrt(num * num + num2 * num2));
				Look((X: num / num3, Y: num2 / num3));
			}
		}
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (TracksPointer && !PointerFrames.IsStale(e))
		{
			pointerInside = false;
			Look(gazeTarget ?? expression.RestingGaze(), CanAnimate);
		}
	}

	private static double Deg(double radians)
	{
		return radians * 180.0 / Math.PI;
	}

	private static void Fade(UIElement e, double to, bool animated, double seconds = 0.2)
	{
		if (!animated)
		{
			e.Opacity = to;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, (float)to, Easing.EaseOut(C));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		scalarKeyFrameAnimation.Target = "Opacity";
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	private static void Rotate(UIElement e, double degrees, bool animated, double seconds = 0.2)
	{
		if (!animated)
		{
			e.Rotation = (float)degrees;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, (float)degrees, Easing.EaseOut(C));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		scalarKeyFrameAnimation.Target = "Rotation";
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	private static void TranslateTo(UIElement e, Vector3 to, bool animated, double seconds = 0.2)
	{
		if (!animated)
		{
			e.Translation = to;
			return;
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(1f, to, Easing.EaseOut(C));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		vector3KeyFrameAnimation.Target = "Translation";
		e.StartAnimation(vector3KeyFrameAnimation);
	}

	private static void ScaleTo(UIElement e, Vector3 to, bool animated, double seconds = 0.2)
	{
		if (!animated)
		{
			e.Scale = to;
			return;
		}
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(1f, to, Easing.EaseOut(C));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		vector3KeyFrameAnimation.Target = "Scale";
		e.StartAnimation(vector3KeyFrameAnimation);
	}

	private static void Keys(UIElement e, string target, float[] values, float[]? times, double seconds)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = C.CreateScalarKeyFrameAnimation();
		CubicBezierEasingFunction easingFunction = Easing.EaseInOut(C);
		scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		for (int i = 1; i < values.Length; i++)
		{
			scalarKeyFrameAnimation.InsertKeyFrame((times != null) ? times[i] : ((float)i / (float)(values.Length - 1)), values[i], easingFunction);
		}
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		scalarKeyFrameAnimation.Target = target;
		e.StartAnimation(scalarKeyFrameAnimation);
	}

	private static void KeysV3(UIElement e, string target, float[] values, float[]? times, double seconds, Func<float, Vector3> map)
	{
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = C.CreateVector3KeyFrameAnimation();
		CubicBezierEasingFunction easingFunction = Easing.EaseInOut(C);
		vector3KeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		for (int i = 1; i < values.Length; i++)
		{
			vector3KeyFrameAnimation.InsertKeyFrame((times != null) ? times[i] : ((float)i / (float)(values.Length - 1)), map(values[i]), easingFunction);
		}
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		vector3KeyFrameAnimation.Target = target;
		e.StartAnimation(vector3KeyFrameAnimation);
	}
}
