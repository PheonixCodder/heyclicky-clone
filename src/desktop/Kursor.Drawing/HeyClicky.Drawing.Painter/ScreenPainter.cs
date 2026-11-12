using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Overlay;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class ScreenPainter
{
	private sealed record WindowParts(CompositionRoundedRectangleGeometry Clip, CompositionRoundedRectangleGeometry Border, CompositionContainerShape TitleBar, CompositionContainerShape? Title, double LineWidth);

	private sealed record MaskDefinition(SvgPath Centreline, Vector2[] Points, float LineWidth, AnimationSpec? Animation, CanvasGeometry Geometry);

	private struct UnitResult
	{
		public List<PenStroke> PenStrokes;

		public PenStroke? FallbackVisit;

		public (double Begin, double End)? Window;

		public void Include(double begin, double end)
		{
			(double, double)? window = Window;
			(double, double) value;
			if (window.HasValue)
			{
				(double, double) valueOrDefault = window.GetValueOrDefault();
				value = (Math.Min(valueOrDefault.Item1, begin), Math.Max(valueOrDefault.Item2, end));
			}
			else
			{
				value = (begin, end);
			}
			Window = value;
		}
	}

	private readonly record struct AnimationRecord(string Property, Vector4 From, Vector4 To, double BeginTime, double Duration, Vector4 Timing, bool FillBackwards, Func<double, double>? Curve = null);

	private readonly record struct TextGlow(Color TightColor, float TightBlur, Color WideColor, float WideBlur, float WideOffsetY)
	{
		public static TextGlow? ForFill(string fill)
		{
			if (!(fill == "#FFFFFF"))
			{
				if (fill == "#15171C")
				{
					return new TextGlow(Color.FromArgb(242, byte.MaxValue, byte.MaxValue, byte.MaxValue), 1.4f, Color.FromArgb(153, byte.MaxValue, byte.MaxValue, byte.MaxValue), 4f, 0f);
				}
				return null;
			}
			return new TextGlow(Color.FromArgb(242, 8, 14, 30), 1.2f, Color.FromArgb(140, 8, 14, 30), 3f, 1f);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003C_003Ec__DisplayClass102_0
	{
		public HashSet<Node> holdingKept;

		public HashSet<Node> keptSet;
	}

	[CompilerGenerated]
	private sealed class _003COtherNodes_003Ed__102 : IEnumerable<Node>, IEnumerable, IEnumerator<Node>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private Node _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public ScreenPainter _003C_003E4__this;

		private IEnumerable<int> keptUnits;

		public IEnumerable<int> _003C_003E3__keptUnits;

		private _003C_003Ec__DisplayClass102_0 _003C_003E8__1;

		private HashSet<Node> _003Cseen_003E5__2;

		private Dictionary<int, Node>.ValueCollection.Enumerator _003C_003E7__wrap2;

		Node IEnumerator<Node>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003COtherNodes_003Ed__102(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 1)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E8__1 = default(_003C_003Ec__DisplayClass102_0);
			_003Cseen_003E5__2 = null;
			_003C_003E7__wrap2 = default(Dictionary<int, Node>.ValueCollection.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				ScreenPainter screenPainter = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
				{
					_003C_003E1__state = -1;
					List<Node> list = screenPainter.NodesFor(keptUnits);
					_003C_003E8__1.keptSet = new HashSet<Node>(list);
					_003C_003E8__1.holdingKept = new HashSet<Node>();
					foreach (Node item in list)
					{
						for (Node parent = item.Parent; parent != null; parent = parent.Parent)
						{
							_003C_003E8__1.holdingKept.Add(parent);
						}
					}
					_003Cseen_003E5__2 = new HashSet<Node>();
					_003C_003E7__wrap2 = screenPainter.unitNodes.Values.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				}
				case 1:
					_003C_003E1__state = -3;
					break;
				}
				while (_003C_003E7__wrap2.MoveNext())
				{
					Node current = _003C_003E7__wrap2.Current;
					if (current.Visual.IsVisible && current.Parent != null && !_003COtherNodes_003Eg__IsInsideKept_007C102_0(current, ref _003C_003E8__1) && _003Cseen_003E5__2.Add(current))
					{
						_003C_003E2__current = current;
						_003C_003E1__state = 1;
						return true;
					}
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap2 = default(Dictionary<int, Node>.ValueCollection.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap2/*cast due to .constrained prefix*/).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
		{
			_003COtherNodes_003Ed__102 _003COtherNodes_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003COtherNodes_003Ed__ = this;
			}
			else
			{
				_003COtherNodes_003Ed__ = new _003COtherNodes_003Ed__102(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003COtherNodes_003Ed__.keptUnits = _003C_003E3__keptUnits;
			return _003COtherNodes_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Node>)this).GetEnumerator();
		}
	}

	public const double TitleBarHeightPoints = 28.0;

	public const double CornerRadiusPoints = 10.0;

	public const double ButtonDiameterPoints = 12.0;

	public const double FirstButtonCenterXPoints = 16.0;

	public const double ButtonSpacingPoints = 20.0;

	private readonly Compositor compositor;

	private readonly DrawingThread thread;

	private readonly CanvasDevice canvasDevice;

	private readonly CompositionGraphicsDevice graphicsDevice;

	private readonly TextShaper textShaper;

	private readonly ContainerVisual canvasVisual;

	private readonly Dictionary<string, Node> groups = new Dictionary<string, Node>(StringComparer.Ordinal);

	private Vector2 canvasSize;

	private double rasterizationScale = 1.0;

	private readonly Dictionary<string, MaskDefinition> maskDefinitions = new Dictionary<string, MaskDefinition>(StringComparer.Ordinal);

	private readonly Dictionary<int, Node> unitNodes = new Dictionary<int, Node>();

	private readonly Dictionary<string, (string MaskId, Node Container)> lastMaskedContainerByGroup = new Dictionary<string, (string, Node)>(StringComparer.Ordinal);

	private readonly Dictionary<string, List<Node>> containersByMask = new Dictionary<string, List<Node>>(StringComparer.Ordinal);

	private int generation;

	private readonly Dictionary<Color, CompositionColorBrush> colorBrushes = new Dictionary<Color, CompositionColorBrush>();

	private readonly Dictionary<Vector4, CompositionEasingFunction> easings = new Dictionary<Vector4, CompositionEasingFunction>();

	private readonly Dictionary<CanvasGeometry, CompositionPath> pathCache = new Dictionary<CanvasGeometry, CompositionPath>(ReferenceEqualityComparer.Instance);

	public Func<string, CancellationToken, Task<byte[]?>>? PhotoProvider;

	public bool RemovesFinishedPenMasks = true;

	public bool ShowsOnScreen = true;

	public Drawing3DAdapter? Figures;

	private readonly List<(Node Node, Vector3 Offset)> cardDragNodes = new List<(Node, Vector3)>();

	private readonly List<Node> isolationHiddenNodes = new List<Node>();

	private SpriteVisual? fullScreenBackdrop;

	private const double MaximumSurfacePixels = 12000000.0;

	public const string SourceIconQueryPrefix = "favicon:";

	private static readonly Vector4 PhotoArrivalTiming = new Vector4(0.22f, 1f, 0.36f, 1f);

	private readonly Dictionary<int, Node> glowNodesByUnit = new Dictionary<int, Node>();

	private const float GlowMaskWidening = 3f;

	private static readonly Vector4 EaseOut = new Vector4(0f, 0f, 0.58f, 1f);

	private readonly Dictionary<CompositionObject, List<AnimationRecord>> animationRecords = new Dictionary<CompositionObject, List<AnimationRecord>>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<CompositionPath, CanvasGeometry> pathSources = new Dictionary<CompositionPath, CanvasGeometry>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<SpriteVisual, Action<CanvasDrawingSession, Vector2>> spriteContents = new Dictionary<SpriteVisual, Action<CanvasDrawingSession, Vector2>>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<SpriteVisual, Node> maskedSprites = new Dictionary<SpriteVisual, Node>(ReferenceEqualityComparer.Instance);

	public ContainerVisual StageVisual { get; }

	public double PointsPerUnit { get; private set; } = 1.0;

	public double DevicePixelsPerUnit => PointsPerUnit * rasterizationScale;

	public static double Now => DrawingThread.Now;

	public TextShaper TextShaper => textShaper;

	public Compositor Compositor => compositor;

	public Vector2 CanvasSize => canvasSize;

	public bool IsIsolatingACard => (object)fullScreenBackdrop != null;

	public int UnitCount => unitNodes.Count;

	public int AppliedMaskCount => containersByMask.Values.Sum((List<Node> list) => list.Count((Node node) => node.Mask?.IsApplied ?? false));

	public int RecordedAnimationCount => animationRecords.Values.Sum((List<AnimationRecord> list) => list.Count((AnimationRecord r) => r.BeginTime + r.Duration > Now));

	public event Action<IReadOnlyList<MarkTiming>>? MarkTimingsReady;

	public event Action<IReadOnlyList<PenStroke>>? PenStrokesReady;

	public event Action? PhotoShown;

	private Node MakeWindowNode(JsonElement attributes, AnimationSpec? animation)
	{
		WindowChromeInfo windowChromeInfo = WindowInfo(attributes);
		Node node = BuildWindowChrome(windowChromeInfo);
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault = animation.GetValueOrDefault();
			if (valueOrDefault.Name == "fadein")
			{
				Visual visual = node.Visual;
				visual.CenterPoint = new Vector3((float)(windowChromeInfo.X + windowChromeInfo.Width / 2.0), (float)(windowChromeInfo.Y + windowChromeInfo.Height / 2.0), 0f);
				AnimateScalar(visual, "Opacity", 0f, 1f, valueOrDefault, fillBackwards: true, node);
				AnimateVector3(visual, "Scale", new Vector3(0.96f, 0.96f, 1f), Vector3.One, valueOrDefault, fillBackwards: true, node);
			}
		}
		return node;
	}

	private WindowChromeInfo WindowInfo(JsonElement attributes)
	{
		double num = 1.0 / Math.Max(PointsPerUnit, 0.01);
		double number;
		double num2 = (SvgValues.TryNumber(Attr(attributes, "data-tint-alpha"), out number) ? number : 0.15);
		double x = SvgValues.Number(Attr(attributes, "x"));
		double y = SvgValues.Number(Attr(attributes, "y"));
		double width = Math.Max(16.0, SvgValues.Number(Attr(attributes, "width")));
		double height = Math.Max(16.0, SvgValues.Number(Attr(attributes, "height")));
		string text = Attr(attributes, "data-header");
		return new WindowChromeInfo(x, y, width, height, (text != null) ? SvgValues.Number(text) : (28.0 * num), Attr(attributes, "data-title") ?? "", Attr(attributes, "data-board") ?? "plain", CssColor.Paint(Attr(attributes, "data-bg") ?? "#FAF5EA", null) ?? Color.FromArgb(byte.MaxValue, 247, 247, 247), CssColor.Paint(Attr(attributes, "data-tint") ?? "#0F7FFF", num2.ToString(CultureInfo.InvariantCulture)) ?? Color.FromArgb(38, 128, 128, 128), num);
	}

	private CompositionSpriteShape RectShape(double x, double y, double width, double height, CompositionBrush brush)
	{
		CompositionRectangleGeometry compositionRectangleGeometry = compositor.CreateRectangleGeometry();
		compositionRectangleGeometry.Offset = new Vector2((float)x, (float)y);
		compositionRectangleGeometry.Size = new Vector2((float)width, (float)height);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositionRectangleGeometry);
		compositionSpriteShape.FillBrush = brush;
		return compositionSpriteShape;
	}

	private Node BuildWindowChrome(WindowChromeInfo info)
	{
		double unitsPerPoint = info.UnitsPerPoint;
		double num = Math.Min(10.0 * unitsPerPoint, Math.Min(info.Width / 2.0, info.Height / 2.0));
		double num2 = Math.Clamp(info.TitleBarHeight, 0.0, info.Height);
		(double, double, double, double) tuple = (info.X, info.Y, info.Width, info.Height);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		ShapeVisual shapeVisual = MakeShapeVisual();
		CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = compositor.CreateRoundedRectangleGeometry();
		compositionRoundedRectangleGeometry.Offset = new Vector2((float)info.X, (float)info.Y);
		compositionRoundedRectangleGeometry.Size = new Vector2((float)info.Width, (float)info.Height);
		compositionRoundedRectangleGeometry.CornerRadius = new Vector2((float)num);
		shapeVisual.Clip = compositor.CreateGeometricClip(compositionRoundedRectangleGeometry);
		shapeVisual.Shapes.Add(RectShape(info.X, info.Y, info.Width, info.Height, Brush(info.BoardColor)));
		CompositionSpriteShape compositionSpriteShape = BoardPattern(rect: (X: info.X, Y: info.Y + num2, Width: info.Width, Height: info.Height - num2), board: info.Board, pt: unitsPerPoint);
		if ((object)compositionSpriteShape != null)
		{
			shapeVisual.Shapes.Add(compositionSpriteShape);
		}
		CompositionContainerShape compositionContainerShape = compositor.CreateContainerShape();
		compositionContainerShape.Comment = "titleBar";
		CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
		compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0.5f);
		compositionLinearGradientBrush.EndPoint = new Vector2(1f, 0.5f);
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, Color.FromArgb(byte.MaxValue, 214, 213, 215)));
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.5f, Color.FromArgb(byte.MaxValue, 240, 240, 240)));
		compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(1f, Color.FromArgb(byte.MaxValue, 214, 213, 215)));
		compositionContainerShape.Shapes.Add(RectShape(info.X, info.Y, info.Width, num2, compositionLinearGradientBrush));
		compositionContainerShape.Shapes.Add(RectShape(info.X, info.Y, info.Width, num2, Brush(info.TintColor)));
		compositionContainerShape.Shapes.Add(RectShape(info.X, info.Y + num2 - 0.5 * unitsPerPoint, info.Width, 0.5 * unitsPerPoint, Brush(Color.FromArgb(23, 0, 0, 0))));
		compositionContainerShape.Shapes.Add(RectShape(info.X, info.Y + 1.0 * unitsPerPoint, info.Width, 1.0 * unitsPerPoint, Brush(Color.FromArgb(179, byte.MaxValue, byte.MaxValue, byte.MaxValue))));
		shapeVisual.Shapes.Add(compositionContainerShape);
		containerVisual.Children.InsertAtTop(CompositionShadows.Cast(compositor, shapeVisual, CompositionShadows.Bounds(tuple, null, 2.0 * unitsPerPoint), Color.FromArgb(20, 0, 0, 0), (float)(12.0 * unitsPerPoint), new Vector2(0f, (float)(3.0 * unitsPerPoint))));
		containerVisual.Children.InsertAtTop(shapeVisual);
		ShapeVisual shapeVisual2 = MakeShapeVisual();
		CompositionContainerShape compositionContainerShape2 = TitleShape(info.Title, (X: info.X, Y: info.Y, Width: info.Width, Height: num2), unitsPerPoint);
		if ((object)compositionContainerShape2 != null)
		{
			shapeVisual2.Shapes.Add(compositionContainerShape2);
		}
		double num3 = 1.0 * unitsPerPoint;
		CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry2 = compositor.CreateRoundedRectangleGeometry();
		compositionRoundedRectangleGeometry2.Offset = new Vector2((float)(info.X + num3 / 2.0), (float)(info.Y + num3 / 2.0));
		compositionRoundedRectangleGeometry2.Size = new Vector2((float)(info.Width - num3), (float)(info.Height - num3));
		compositionRoundedRectangleGeometry2.CornerRadius = new Vector2((float)Math.Max(0.0, num - num3 / 2.0));
		CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(compositionRoundedRectangleGeometry2);
		CompositionLinearGradientBrush compositionLinearGradientBrush2 = compositor.CreateLinearGradientBrush();
		compositionLinearGradientBrush2.StartPoint = new Vector2(0.5f, 0f);
		compositionLinearGradientBrush2.EndPoint = new Vector2(0.5f, 1f);
		compositionLinearGradientBrush2.ColorStops.Add(compositor.CreateColorGradientStop(0f, Color.FromArgb(byte.MaxValue, 0, 0, 0)));
		compositionLinearGradientBrush2.ColorStops.Add(compositor.CreateColorGradientStop(1f, Color.FromArgb(26, 0, 0, 0)));
		compositionSpriteShape2.StrokeBrush = compositionLinearGradientBrush2;
		compositionSpriteShape2.StrokeThickness = (float)num3;
		shapeVisual2.Shapes.Add(compositionSpriteShape2);
		containerVisual.Children.InsertAtTop(shapeVisual2);
		info.Inside = shapeVisual;
		return new Node
		{
			Kind = NodeKind.Window,
			Visual = containerVisual,
			Extent = tuple,
			Window = info,
			Tag = new WindowParts(compositionRoundedRectangleGeometry, compositionRoundedRectangleGeometry2, compositionContainerShape, compositionContainerShape2, num3)
		};
	}

	private CompositionContainerShape? TitleShape(string title, (double X, double Y, double Width, double Height) titleBar, double pt)
	{
		string text = title.Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return null;
		}
		double num = 13.0 * pt;
		TextShaper.Font font = textShaper.GetFont("system-ui", num, 600);
		double num2 = (double)WindowButtonShapes.CaptionsFromRight.Length * 40.0 + 8.0;
		double num3 = titleBar.Width - 2.0 * num2 * pt;
		if (num3 <= num * 2.0)
		{
			return null;
		}
		string text2 = text;
		TextShaper.ShapedLine shapedLine = textShaper.Shape(text2, font);
		while (shapedLine.Advance > num3 && text2.Length > 1)
		{
			string text3 = text2;
			text2 = text3.Substring(0, text3.Length - 1).TrimEnd();
			shapedLine = textShaper.Shape(text2 + "…", font);
		}
		double num4 = titleBar.Y + titleBar.Height / 2.0 + (font.Metrics.Ascent - font.Metrics.Descent) / 2.0;
		Vector2 offset = new Vector2((float)(titleBar.X + titleBar.Width / 2.0 - shapedLine.Advance / 2.0), (float)num4);
		CompositionContainerShape compositionContainerShape = compositor.CreateContainerShape();
		CompositionColorBrush fillBrush = Brush(Color.FromArgb(102, 0, 0, 0));
		TextShaper.Glyph[] glyphs = shapedLine.Glyphs;
		foreach (TextShaper.Glyph glyph in glyphs)
		{
			if ((object)glyph.Outline != null)
			{
				CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(CompositionPathFor(glyph.Outline)));
				compositionSpriteShape.FillBrush = fillBrush;
				compositionSpriteShape.Offset = offset;
				compositionContainerShape.Shapes.Add(compositionSpriteShape);
			}
		}
		return compositionContainerShape;
	}

	private CompositionSpriteShape? BoardPattern(string board, (double X, double Y, double Width, double Height) rect, double pt)
	{
		bool flag = rect.Width <= 0.0 || rect.Height <= 0.0;
		if (!flag)
		{
			bool flag2 = ((board == "dots" || board == "grid") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			return null;
		}
		double num = (double)((board == "grid") ? 20 : 16) * pt;
		double num2 = Math.Ceiling(rect.X / num) * num;
		double num3 = Math.Ceiling(rect.Y / num) * num;
		int val = (int)Math.Floor((rect.X + rect.Width - num2) / num);
		int val2 = (int)Math.Floor((rect.Y + rect.Height - num3) / num);
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(canvasDevice);
		if (board == "dots")
		{
			float num4 = (float)(1.1 * pt);
			double num5 = num * 0.4;
			for (int i = 0; i <= Math.Max(0, val2); i++)
			{
				for (int j = 0; j <= Math.Max(0, val); j++)
				{
					double num6 = num2 + (double)j * num;
					double num7 = num3 + (double)i * num;
					if (!(num6 < rect.X + num5) && !(num6 > rect.X + rect.Width - num5) && !(num7 < rect.Y + num5) && !(num7 > rect.Y + rect.Height - num5))
					{
						canvasPathBuilder.BeginFigure((float)num6 + num4, (float)num7);
						canvasPathBuilder.AddArc(new Vector2((float)num6, (float)num7), num4, num4, 0f, (float)Math.PI * 2f);
						canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
					}
				}
			}
		}
		else
		{
			for (int k = 0; k <= Math.Max(0, val); k++)
			{
				double num8 = num2 + (double)k * num;
				if (!(num8 <= rect.X) && !(num8 >= rect.X + rect.Width))
				{
					canvasPathBuilder.BeginFigure((float)num8, (float)rect.Y);
					canvasPathBuilder.AddLine((float)num8, (float)(rect.Y + rect.Height));
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
			}
			for (int l = 0; l <= Math.Max(0, val2); l++)
			{
				double num9 = num3 + (double)l * num;
				if (!(num9 <= rect.Y) && !(num9 >= rect.Y + rect.Height))
				{
					canvasPathBuilder.BeginFigure((float)rect.X, (float)num9);
					canvasPathBuilder.AddLine((float)(rect.X + rect.Width), (float)num9);
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
			}
		}
		CanvasGeometry geometry = CanvasGeometry.CreatePath(canvasPathBuilder);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(CompositionPathFor(geometry)));
		if (board == "dots")
		{
			compositionSpriteShape.FillBrush = Brush(Color.FromArgb(51, 0, 0, 0));
		}
		else
		{
			compositionSpriteShape.StrokeBrush = Brush(Color.FromArgb(14, 0, 0, 0));
			compositionSpriteShape.StrokeThickness = (float)(0.6 * pt);
		}
		return compositionSpriteShape;
	}

	private void ResizeWindow(Node previous, int unit, JsonElement attributes)
	{
		WindowChromeInfo windowChromeInfo = WindowInfo(attributes);
		WindowChromeInfo window = previous.Window;
		Node node = BuildWindowChrome(windowChromeInfo);
		node.Unit = unit;
		node.Visual.Opacity = previous.Visual.Opacity;
		node.Visual.IsVisible = previous.Visual.IsVisible;
		node.Visual.Offset = previous.Visual.Offset;
		Node parent = previous.Parent;
		int index = parent.Children.IndexOf(previous);
		Detach(previous);
		InsertAt(node, parent, index);
		unitNodes[unit] = node;
		for (int i = 0; i < isolationHiddenNodes.Count; i++)
		{
			if (isolationHiddenNodes[i] == previous)
			{
				isolationHiddenNodes[i] = node;
			}
		}
		for (int j = 0; j < cardDragNodes.Count; j++)
		{
			if (cardDragNodes[j].Node == previous)
			{
				cardDragNodes[j] = (node, cardDragNodes[j].Offset);
			}
		}
		if ((window.X != windowChromeInfo.X || window.Y != windowChromeInfo.Y || window.Width != windowChromeInfo.Width || window.Height != windowChromeInfo.Height) && node.Tag is WindowParts windowParts)
		{
			AnimationSpec spec = new AnimationSpec("grow", 0.5, Now, new Vector4(0.22f, 1f, 0.36f, 1f));
			AnimateVector2(windowParts.Clip, "Offset", new Vector2((float)window.X, (float)window.Y), new Vector2((float)windowChromeInfo.X, (float)windowChromeInfo.Y), spec, fillBackwards: true, node);
			AnimateVector2(windowParts.Clip, "Size", new Vector2((float)window.Width, (float)window.Height), new Vector2((float)windowChromeInfo.Width, (float)windowChromeInfo.Height), spec, fillBackwards: true, node);
			float num = (float)(windowParts.LineWidth / 2.0);
			AnimateVector2(windowParts.Border, "Offset", new Vector2((float)window.X + num, (float)window.Y + num), windowParts.Border.Offset, spec, fillBackwards: true, node);
			AnimateVector2(windowParts.Border, "Size", new Vector2((float)window.Width - 2f * num, (float)window.Height - 2f * num), windowParts.Border.Size, spec, fillBackwards: true, node);
			Vector2 from = new Vector2((float)(window.X + window.Width / 2.0 - (windowChromeInfo.X + windowChromeInfo.Width / 2.0)), (float)(window.Y - windowChromeInfo.Y));
			AnimateVector2(windowParts.TitleBar, "Offset", new Vector2(0f, from.Y), Vector2.Zero, spec, fillBackwards: true, node);
			CompositionContainerShape title = windowParts.Title;
			if ((object)title != null)
			{
				AnimateVector2(title, "Offset", from, Vector2.Zero, spec, fillBackwards: true, node);
			}
		}
	}

	public ScreenPainter(Compositor compositor, DrawingThread thread, CanvasDevice canvasDevice, TextShaper textShaper)
	{
		this.compositor = compositor;
		this.thread = thread;
		this.canvasDevice = canvasDevice;
		this.textShaper = textShaper;
		graphicsDevice = SystemComposition.GraphicsDevice(compositor, canvasDevice);
		StageVisual = compositor.CreateContainerVisual();
		StageVisual.Comment = "drawing stage";
		canvasVisual = compositor.CreateContainerVisual();
		StageVisual.Children.InsertAtTop(canvasVisual);
		string[] array = new string[4] { "under", "main", "hl", "words" };
		foreach (string text in array)
		{
			ContainerVisual containerVisual = compositor.CreateContainerVisual();
			containerVisual.Comment = text;
			canvasVisual.Children.InsertAtTop(containerVisual);
			groups[text] = new Node
			{
				Kind = NodeKind.Group,
				Visual = containerVisual,
				Content = containerVisual,
				Children = new List<Node>()
			};
		}
	}

	public void Configure(Vector2 size, double pointWidth, double rasterization)
	{
		canvasSize = size;
		PointsPerUnit = ((size.X > 0f) ? (pointWidth / (double)size.X) : 1.0);
		rasterizationScale = Math.Max(1.0, rasterization);
		canvasVisual.Scale = new Vector3((float)PointsPerUnit, (float)PointsPerUnit, 1f);
		StageVisual.Size = new Vector2((float)pointWidth, (float)((double)size.Y * PointsPerUnit));
		Figures?.SetDevicePixelsPerUnit(DevicePixelsPerUnit);
	}

	public void ClearAll()
	{
		generation++;
		foreach (Node value in groups.Values)
		{
			value.Content.Children.RemoveAll();
			value.Children.Clear();
		}
		StageVisual.StopAnimation("Opacity");
		ForgetRecords(StageVisual, "Opacity");
		StageVisual.Opacity = 1f;
		foreach (List<Node> value2 in containersByMask.Values)
		{
			foreach (Node item in value2)
			{
				DisposeMask(item);
			}
		}
		maskDefinitions.Clear();
		unitNodes.Clear();
		glowNodesByUnit.Clear();
		cardDragNodes.Clear();
		isolationHiddenNodes.Clear();
		if ((object)fullScreenBackdrop != null)
		{
			StageVisual.Children.Remove(fullScreenBackdrop);
			fullScreenBackdrop = null;
		}
		lastMaskedContainerByGroup.Clear();
		containersByMask.Clear();
		ClearSnapshotRecords();
		pathSources.Clear();
		pathCache.Clear();
		Figures?.ClearAll();
	}

	public void Apply(string opsJson)
	{
		if (string.IsNullOrEmpty(opsJson))
		{
			return;
		}
		JsonDocument jsonDocument;
		try
		{
			jsonDocument = JsonDocument.Parse(opsJson);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "render ops did not parse: " + ex.Message);
			return;
		}
		using (jsonDocument)
		{
			if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
			{
				return;
			}
			Dictionary<int, List<PenStroke>> dictionary = new Dictionary<int, List<PenStroke>>();
			Dictionary<int, List<PenStroke>> dictionary2 = new Dictionary<int, List<PenStroke>>();
			Dictionary<int, (double, double)> dictionary3 = new Dictionary<int, (double, double)>();
			List<int> list = new List<int>();
			foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
			{
				try
				{
					JsonElement value;
					switch (item.TryGetProperty("op", out value) ? value.GetString() : null)
					{
					case "remove":
					{
						if (item.TryGetProperty("n", out var value10) && value10.ValueKind == JsonValueKind.Number)
						{
							RemoveUnit(value10.GetInt32());
						}
						break;
					}
					case "wrap":
						ApplyWrap(item);
						break;
					case "prop":
						ApplyProperty(item);
						break;
					case "stop":
						ApplyStop(item);
						break;
					case "motion":
						ApplyMotion(item);
						break;
					case "set":
					{
						if (!item.TryGetProperty("group", out var value2) || value2.ValueKind != JsonValueKind.String || !item.TryGetProperty("node", out var value3) || value3.ValueKind != JsonValueKind.Object)
						{
							break;
						}
						string @string = value2.GetString();
						if (@string == "defs")
						{
							RegisterDefinition(value3);
						}
						else
						{
							if (!groups.TryGetValue(@string, out Node value4))
							{
								break;
							}
							JsonElement value5;
							int? beforeUnit = ((item.TryGetProperty("before", out value5) && value5.ValueKind == JsonValueKind.Number) ? new int?(value5.GetInt32()) : ((int?)null));
							UnitResult unitResult = BuildUnit(value3, @string, value4, beforeUnit);
							if (!value3.TryGetProperty("k", out var value6) || value6.ValueKind != JsonValueKind.Number)
							{
								break;
							}
							int @int = value6.GetInt32();
							if (!list.Contains(@int))
							{
								list.Add(@int);
							}
							if (!dictionary.TryGetValue(@int, out var value7))
							{
								value7 = (dictionary[@int] = new List<PenStroke>());
							}
							value7.AddRange(unitResult.PenStrokes);
							PenStroke fallbackVisit = unitResult.FallbackVisit;
							if (fallbackVisit != null)
							{
								if (!dictionary2.TryGetValue(@int, out var value8))
								{
									value8 = (dictionary2[@int] = new List<PenStroke>());
								}
								value8.Add(fallbackVisit);
							}
							(double, double)? window = unitResult.Window;
							if (window.HasValue)
							{
								(double, double) valueOrDefault = window.GetValueOrDefault();
								dictionary3[@int] = (dictionary3.TryGetValue(@int, out var value9) ? (Math.Min(value9.Item1, valueOrDefault.Item1), Math.Max(value9.Item2, valueOrDefault.Item2)) : valueOrDefault);
							}
						}
						break;
					}
					}
				}
				catch (Exception ex2)
				{
					Log.Warn("Drawing", "render op failed: " + ex2.Message);
				}
			}
			DrawStaleInk();
			Figures?.SyncPlacements(this);
			Action<IReadOnlyList<MarkTiming>> action = this.MarkTimingsReady;
			if (action != null)
			{
				List<MarkTiming> list4 = new List<MarkTiming>();
				foreach (int item2 in list)
				{
					if (dictionary3.TryGetValue(item2, out var value11))
					{
						List<PenStroke> value12;
						List<PenStroke> penStrokes = (dictionary.TryGetValue(item2, out value12) ? value12.ConvertAll((PenStroke s) => s.Resolved()) : new List<PenStroke>());
						list4.Add(new MarkTiming(item2, value11.Item1, value11.Item2, penStrokes));
					}
				}
				if (list4.Count > 0)
				{
					action(list4);
				}
			}
			Action<IReadOnlyList<PenStroke>> action2 = this.PenStrokesReady;
			if (action2 == null)
			{
				return;
			}
			List<PenStroke> list5 = new List<PenStroke>();
			foreach (int item3 in list)
			{
				List<PenStroke> value14;
				if (dictionary.TryGetValue(item3, out var value13) && value13.Count > 0)
				{
					list5.AddRange(value13.ConvertAll((PenStroke s) => s.Resolved()));
				}
				else if (dictionary2.TryGetValue(item3, out value14) && value14.Count > 0)
				{
					list5.Add(value14[0].Resolved());
				}
			}
			if (list5.Count > 0)
			{
				list5.Sort((PenStroke a, PenStroke b) => a.BeginTime.CompareTo(b.BeginTime));
				action2(list5);
			}
		}
	}

	private void RegisterDefinition(JsonElement node)
	{
		if (Str(node, "tag") != "mask" || !node.TryGetProperty("a", out var value))
		{
			return;
		}
		string text = Attr(value, "id");
		if (text == null || !node.TryGetProperty("ch", out var value2) || value2.ValueKind != JsonValueKind.Array)
		{
			return;
		}
		foreach (JsonElement item in value2.EnumerateArray())
		{
			if (!(Str(item, "tag") != "path") && item.TryGetProperty("a", out var value3))
			{
				string text2 = Attr(value3, "d");
				if (text2 != null)
				{
					SvgPath svgPath = new SvgPath(text2);
					List<List<Vector2>> list = svgPath.FlattenedSubpaths();
					SvgValues.TryNumber(Attr(value3, "stroke-width"), out var number);
					maskDefinitions[text] = new MaskDefinition(svgPath, (list.Count > 0) ? list[0].ToArray() : Array.Empty<Vector2>(), (float)((number > 0.0) ? number : 1.0), item.TryGetProperty("anim", out var value4) ? AnimationSpec.From(value4, Now) : ((AnimationSpec?)null), svgPath.ToGeometry(canvasDevice, null));
					break;
				}
			}
		}
	}

	private UnitResult BuildUnit(JsonElement node, string groupName, Node groupNode, int? beforeUnit)
	{
		UnitResult unitResult = default(UnitResult);
		unitResult.PenStrokes = new List<PenStroke>();
		UnitResult result = unitResult;
		if (node.TryGetProperty("n", out var value) && value.ValueKind == JsonValueKind.Number)
		{
			string text = Str(node, "tag");
			if (text != null)
			{
				int @int = value.GetInt32();
				JsonElement value2;
				JsonElement attributes = ((node.TryGetProperty("a", out value2) && value2.ValueKind == JsonValueKind.Object) ? value2 : default(JsonElement));
				if (text == "window" && unitNodes.TryGetValue(@int, out Node value3) && (object)value3.Window != null && value3.Parent != null)
				{
					ResizeWindow(value3, @int, attributes);
					return result;
				}
				RemoveUnit(@int);
				JsonElement value4;
				JsonElement jsonElement = ((node.TryGetProperty("st", out value4) && value4.ValueKind == JsonValueKind.Object) ? value4 : default(JsonElement));
				JsonElement value5;
				AnimationSpec? animation = (node.TryGetProperty("anim", out value5) ? AnimationSpec.From(value5, Now) : ((AnimationSpec?)null));
				switch (text)
				{
				case "path":
				{
					string text2 = Attr(attributes, "d");
					if (text2 == null)
					{
						break;
					}
					SvgPath svgPath = new SvgPath(text2);
					string text3 = SvgValues.UrlReference(Attr(attributes, "mask"));
					if (text3 != null && maskDefinitions.TryGetValue(text3, out MaskDefinition value6))
					{
						Node node3;
						if (lastMaskedContainerByGroup.TryGetValue(groupName, out (string, Node) value7) && value7.Item1 == text3 && value7.Item2.Parent == groupNode && NodeBefore(beforeUnit, groupNode) == value7.Item2)
						{
							node3 = value7.Item2;
						}
						else
						{
							node3 = MakeMaskedContainer(value6, text3, null);
							Insert(node3, groupNode, beforeUnit);
							lastMaskedContainerByGroup[groupName] = (text3, node3);
							AnimationSpec? animation2 = value6.Animation;
							if (animation2.HasValue)
							{
								AnimationSpec valueOrDefault = animation2.GetValueOrDefault();
								result.PenStrokes.Add(new PenStroke
								{
									Points = value6.Points,
									BeginTime = valueOrDefault.BeginTime,
									Duration = valueOrDefault.Duration,
									Timing = valueOrDefault.Timing
								});
							}
						}
						Node node4 = ((SvgValues.UrlReference(Attr(attributes, "filter")) == "grain") ? MakeGrainNode(svgPath, attributes) : MakeShapeNode(svgPath, attributes));
						node4.Unit = @int;
						AppendChild(node3, node4);
						unitNodes[@int] = node4;
						(float, float)? tuple = ScreenPenMath.GlowSpec(Attr(attributes, "data-glow"));
						if (tuple.HasValue)
						{
							(float, float) valueOrDefault2 = tuple.GetValueOrDefault();
							Node parent = node3.Parent;
							if (parent != null)
							{
								Node node5 = MakeGlowContainer(svgPath, value6, text3, CssColor.Paint(Attr(attributes, "fill") ?? "#000000", null) ?? Colors.Black, valueOrDefault2);
								InsertBelow(node5, parent, node3);
								glowNodesByUnit[@int] = node5;
							}
						}
						break;
					}
					ScreenPenMath.MarqueeBox? marqueeBox = ScreenPenMath.ParseMarquee(Attr(attributes, "data-marquee"));
					if (marqueeBox.HasValue)
					{
						ScreenPenMath.MarqueeBox valueOrDefault3 = marqueeBox.GetValueOrDefault();
						(Node Node, PenStroke? CursorStroke) tuple2 = MakeMarqueeNode(valueOrDefault3, attributes, animation);
						Node item = tuple2.Node;
						PenStroke item2 = tuple2.CursorStroke;
						item.Unit = @int;
						Insert(item, groupNode, beforeUnit);
						unitNodes[@int] = item;
						if (item2 != null)
						{
							result.PenStrokes.Add(item2);
						}
						break;
					}
					if (SvgValues.UrlReference(Attr(attributes, "filter")) == "wash")
					{
						Node node6 = MakeWashNode(svgPath, attributes, animation);
						node6.Unit = @int;
						Insert(node6, groupNode, beforeUnit);
						unitNodes[@int] = node6;
						if (animation.HasValue)
						{
							AnimationSpec valueOrDefault4 = animation.GetValueOrDefault();
							(double, double, double, double) tuple3 = svgPath.BoundingBox();
							result.FallbackVisit = Visit(new Vector2((float)(tuple3.Item1 + tuple3.Item3 / 2.0), (float)(tuple3.Item2 + tuple3.Item4 / 2.0)), valueOrDefault4);
						}
						break;
					}
					Node node7 = MakePlainPathNode(svgPath, attributes, jsonElement, animation);
					node7.Unit = @int;
					Insert(node7, groupNode, beforeUnit);
					unitNodes[@int] = node7;
					if (animation.HasValue)
					{
						AnimationSpec valueOrDefault5 = animation.GetValueOrDefault();
						(double, double, double, double) tuple4 = svgPath.BoundingBox();
						result.FallbackVisit = ((valueOrDefault5.Name == "swipe") ? new PenStroke
						{
							Points = new Vector2[2]
							{
								new Vector2((float)tuple4.Item1, (float)(tuple4.Item2 + tuple4.Item4 / 2.0)),
								new Vector2((float)(tuple4.Item1 + tuple4.Item3), (float)(tuple4.Item2 + tuple4.Item4 / 2.0))
							},
							BeginTime = valueOrDefault5.BeginTime,
							Duration = valueOrDefault5.Duration,
							Timing = valueOrDefault5.Timing
						} : Visit(new Vector2((float)(tuple4.Item1 + tuple4.Item3 / 2.0), (float)(tuple4.Item2 + tuple4.Item4 / 2.0)), valueOrDefault5));
					}
					break;
				}
				case "text":
				{
					if (node.TryGetProperty("text", out var value8) && value8.ValueKind == JsonValueKind.Object)
					{
						(Node Node, List<PenStroke> PenStrokes) tuple5 = MakeTextNode(value8, attributes, animation);
						Node item3 = tuple5.Node;
						List<PenStroke> item4 = tuple5.PenStrokes;
						item3.Unit = @int;
						Insert(item3, groupNode, beforeUnit);
						unitNodes[@int] = item3;
						result.PenStrokes = item4;
					}
					break;
				}
				case "circle":
				{
					Node node8 = MakeDotNode(attributes, animation);
					node8.Unit = @int;
					Insert(node8, groupNode, beforeUnit);
					unitNodes[@int] = node8;
					if (animation.HasValue)
					{
						AnimationSpec valueOrDefault6 = animation.GetValueOrDefault();
						if (SvgValues.Number(Attr(attributes, "r")) >= 1.0)
						{
							result.FallbackVisit = Visit(new Vector2((float)SvgValues.Number(Attr(attributes, "cx")), (float)SvgValues.Number(Attr(attributes, "cy"))), valueOrDefault6);
						}
					}
					break;
				}
				case "g":
					result = BuildGroupUnit(node, @int, attributes, groupNode, beforeUnit);
					break;
				case "photo":
				{
					Node node9 = MakePhotoNode(attributes, animation);
					node9.Unit = @int;
					Insert(node9, groupNode, beforeUnit);
					unitNodes[@int] = node9;
					break;
				}
				case "window":
				{
					Node node10 = MakeWindowNode(attributes, animation);
					node10.Unit = @int;
					Insert(node10, groupNode, beforeUnit);
					unitNodes[@int] = node10;
					if (animation.HasValue)
					{
						AnimationSpec valueOrDefault7 = animation.GetValueOrDefault();
						WindowChromeInfo window = node10.Window;
						if ((object)window != null)
						{
							result.FallbackVisit = Visit(new Vector2((float)(window.X + window.Width / 2.0), (float)window.Y), valueOrDefault7);
						}
					}
					break;
				}
				case "tick3d":
				case "part3d":
				case "label3d":
				case "scene3d":
				case "camera3d":
				case "motion3d":
					if (Figures != null)
					{
						Node node2 = new Node
						{
							Kind = ((text == "scene3d") ? NodeKind.Stage3D : NodeKind.Placeholder),
							Visual = compositor.CreateContainerVisual(),
							Unit = @int
						};
						PenStroke fallbackVisit = Figures.Add(text, @int, attributes, animation, node2, this);
						Insert(node2, groupNode, beforeUnit);
						unitNodes[@int] = node2;
						result.FallbackVisit = fallbackVisit;
					}
					break;
				}
				if (unitNodes.TryGetValue(@int, out Node value9))
				{
					string text4 = Attr(jsonElement, "opacity");
					if (text4 != null && SvgValues.TryNumber(text4, out var number))
					{
						value9.Visual.Opacity = (float)number;
					}
				}
				if (animation.HasValue)
				{
					AnimationSpec valueOrDefault8 = animation.GetValueOrDefault();
					result.Include(valueOrDefault8.BeginTime, valueOrDefault8.EndTime);
				}
				{
					foreach (PenStroke penStroke in result.PenStrokes)
					{
						result.Include(penStroke.BeginTime, penStroke.BeginTime + penStroke.Duration);
					}
					return result;
				}
			}
		}
		return result;
	}

	internal static PenStroke Visit(Vector2 point, AnimationSpec animation)
	{
		PenStroke penStroke = new PenStroke();
		penStroke.Points = new Vector2[1] { point };
		penStroke.BeginTime = animation.BeginTime;
		penStroke.Duration = animation.Duration;
		penStroke.Timing = animation.Timing;
		return penStroke;
	}

	private void RemoveUnit(int unit)
	{
		RemoveGlow(unit);
		if (unitNodes.Remove(unit, out Node value))
		{
			Figures?.RemoveUnit(unit, value);
			if (value.Kind == NodeKind.Wrapper)
			{
				ForgetNodes(value);
			}
			Node parent = value.Parent;
			Detach(value);
			if (parent != null && parent.Kind == NodeKind.MaskedContainer && parent.Children.Count == 0)
			{
				ForgetMaskedContainer(parent);
				Detach(parent);
				DisposeMask(parent);
			}
		}
	}

	private Node? NodeBefore(int? beforeUnit, Node group)
	{
		List<Node> children = group.Children;
		if (beforeUnit.HasValue)
		{
			int valueOrDefault = beforeUnit.GetValueOrDefault();
			if (unitNodes.TryGetValue(valueOrDefault, out Node value))
			{
				Node node = ((value.Parent == group) ? value : value.Parent);
				if (node != null)
				{
					int num = children.IndexOf(node);
					if (num >= 0)
					{
						if (num <= 0)
						{
							return null;
						}
						return children[num - 1];
					}
				}
			}
		}
		if (children.Count <= 0)
		{
			return null;
		}
		return children[children.Count - 1];
	}

	private void Insert(Node node, Node group, int? beforeUnit)
	{
		if (beforeUnit.HasValue)
		{
			int valueOrDefault = beforeUnit.GetValueOrDefault();
			if (unitNodes.TryGetValue(valueOrDefault, out Node value))
			{
				Node node2 = ((value.Parent == group) ? value : value.Parent);
				if (node2 != null && node2.Parent == group)
				{
					InsertBelow(node, group, node2);
					return;
				}
			}
		}
		AppendChild(group, node);
	}

	private static void AppendChild(Node parent, Node node)
	{
		node.Parent = parent;
		parent.Children.Add(node);
		parent.Content.Children.InsertAtTop(node.Visual);
		MarkInkStale(parent);
	}

	private static void InsertBelow(Node node, Node parent, Node sibling)
	{
		node.Parent = parent;
		int val = parent.Children.IndexOf(sibling);
		parent.Children.Insert(Math.Max(0, val), node);
		parent.Content.Children.InsertBelow(node.Visual, sibling.Visual);
		MarkInkStale(parent);
	}

	private static void InsertAt(Node node, Node parent, int index)
	{
		node.Parent = parent;
		List<Node> children = parent.Children;
		index = Math.Clamp(index, 0, children.Count);
		if (index < children.Count)
		{
			children.Insert(index, node);
			parent.Content.Children.InsertBelow(node.Visual, children[index + 1].Visual);
		}
		else
		{
			children.Add(node);
			parent.Content.Children.InsertAtTop(node.Visual);
		}
		MarkInkStale(parent);
	}

	private static void Detach(Node node)
	{
		Node parent = node.Parent;
		if (parent != null)
		{
			parent.Children.Remove(node);
			try
			{
				parent.Content.Children.Remove(node.Visual);
			}
			catch
			{
			}
			node.Parent = null;
			MarkInkStale(parent);
		}
	}

	private static void MarkInkStale(Node container)
	{
		PenMask mask = container.Mask;
		if (mask != null && mask.IsApplied && (object)mask.Ink != null)
		{
			mask.IsInkStale = true;
		}
	}

	private static void ReplaceVisual(Node node, Visual replacement)
	{
		Node parent = node.Parent;
		if (parent != null)
		{
			parent.Content.Children.InsertAbove(replacement, node.Visual);
			parent.Content.Children.Remove(node.Visual);
		}
		node.Visual = replacement;
	}

	private CompositionColorBrush Brush(Color color)
	{
		if (!colorBrushes.TryGetValue(color, out CompositionColorBrush value))
		{
			value = (colorBrushes[color] = compositor.CreateColorBrush(color));
		}
		return value;
	}

	internal CompositionEasingFunction Easing(Vector4 timing)
	{
		if (!easings.TryGetValue(timing, out CompositionEasingFunction value))
		{
			value = (easings[timing] = ((timing == new Vector4(0f, 0f, 1f, 1f)) ? ((CompositionEasingFunction)compositor.CreateLinearEasingFunction()) : ((CompositionEasingFunction)compositor.CreateCubicBezierEasingFunction(new Vector2(timing.X, timing.Y), new Vector2(timing.Z, timing.W)))));
		}
		return value;
	}

	internal CompositionPath CompositionPathFor(CanvasGeometry geometry)
	{
		if (!pathCache.TryGetValue(geometry, out CompositionPath value))
		{
			value = new CompositionPath(geometry);
			pathCache[geometry] = value;
			pathSources[value] = geometry;
		}
		return value;
	}

	internal void AnimateScalar(CompositionObject target, string property, float from, float to, AnimationSpec spec, bool fillBackwards = true, Node? owner = null)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, from);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, Easing(spec.Timing));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.001, spec.Duration));
		double num = spec.BeginTime - Now;
		if (num > 0.0)
		{
			scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num);
		}
		scalarKeyFrameAnimation.DelayBehavior = (fillBackwards ? AnimationDelayBehavior.SetInitialValueBeforeDelay : AnimationDelayBehavior.SetInitialValueAfterDelay);
		target.StartAnimation(property, scalarKeyFrameAnimation);
		Record(target, property, new Vector4(from, 0f, 0f, 0f), new Vector4(to, 0f, 0f, 0f), spec, fillBackwards);
		owner?.AddAnimation(new RunningAnimation(target, property, new Vector3(to, 0f, 0f), 1));
	}

	internal void AnimateVector3(CompositionObject target, string property, Vector3 from, Vector3 to, AnimationSpec spec, bool fillBackwards = true, Node? owner = null)
	{
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(0f, from);
		vector3KeyFrameAnimation.InsertKeyFrame(1f, to, Easing(spec.Timing));
		vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.001, spec.Duration));
		double num = spec.BeginTime - Now;
		if (num > 0.0)
		{
			vector3KeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num);
		}
		vector3KeyFrameAnimation.DelayBehavior = (fillBackwards ? AnimationDelayBehavior.SetInitialValueBeforeDelay : AnimationDelayBehavior.SetInitialValueAfterDelay);
		target.StartAnimation(property, vector3KeyFrameAnimation);
		Record(target, property, new Vector4(from, 0f), new Vector4(to, 0f), spec, fillBackwards);
		owner?.AddAnimation(new RunningAnimation(target, property, to, 3));
	}

	internal void AnimateVector2(CompositionObject target, string property, Vector2 from, Vector2 to, AnimationSpec spec, bool fillBackwards = true, Node? owner = null)
	{
		Vector2KeyFrameAnimation vector2KeyFrameAnimation = compositor.CreateVector2KeyFrameAnimation();
		vector2KeyFrameAnimation.InsertKeyFrame(0f, from);
		vector2KeyFrameAnimation.InsertKeyFrame(1f, to, Easing(spec.Timing));
		vector2KeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.001, spec.Duration));
		double num = spec.BeginTime - Now;
		if (num > 0.0)
		{
			vector2KeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num);
		}
		vector2KeyFrameAnimation.DelayBehavior = (fillBackwards ? AnimationDelayBehavior.SetInitialValueBeforeDelay : AnimationDelayBehavior.SetInitialValueAfterDelay);
		target.StartAnimation(property, vector2KeyFrameAnimation);
		Record(target, property, new Vector4(from, 0f, 0f), new Vector4(to, 0f, 0f), spec, fillBackwards);
		owner?.AddAnimation(new RunningAnimation(target, property, new Vector3(to, 0f), 2));
	}

	internal void FinishAnimations(Node node)
	{
		List<RunningAnimation> animations = node.Animations;
		if (animations != null)
		{
			foreach (RunningAnimation item in animations)
			{
				try
				{
					item.Target.StopAnimation(item.Property);
					ForgetRecords(item.Target, item.Property);
					switch (item.Components)
					{
					case 1:
						SetScalar(item.Target, item.Property, item.FinalValue.X);
						break;
					case 2:
						SetVector2(item.Target, item.Property, new Vector2(item.FinalValue.X, item.FinalValue.Y));
						break;
					case 3:
						SetVector3(item.Target, item.Property, item.FinalValue);
						break;
					}
				}
				catch
				{
				}
			}
			animations.Clear();
		}
		if (node.Tag is List<(CompositionColorBrush, Color)> list)
		{
			foreach (var (compositionColorBrush, color) in list)
			{
				compositionColorBrush.StopAnimation("Color");
				ForgetRecords(compositionColorBrush, "Color");
				compositionColorBrush.Color = color;
			}
			list.Clear();
		}
		PenMask mask = node.Mask;
		if (mask != null)
		{
			mask.Geometry.StopAnimation("TrimEnd");
			mask.Geometry.TrimEnd = mask.ModelTrimEnd;
			mask.EditRunning = false;
			ForgetRecords(mask.Geometry, "TrimEnd");
		}
		List<Node> children = node.Children;
		if (children == null)
		{
			return;
		}
		foreach (Node item2 in children)
		{
			FinishAnimations(item2);
		}
	}

	private static void SetScalar(CompositionObject target, string property, float value)
	{
		if (!(target is Visual visual))
		{
			if (!(target is CompositionGeometry compositionGeometry))
			{
				if (target is CompositionShape compositionShape && property == "RotationAngleInDegrees")
				{
					compositionShape.RotationAngleInDegrees = value;
				}
			}
			else if (property == "TrimEnd")
			{
				compositionGeometry.TrimEnd = value;
			}
		}
		else if (property == "Opacity")
		{
			visual.Opacity = value;
		}
		else
		{
			Visual visual2 = visual;
			if (property == "RotationAngleInDegrees")
			{
				visual2.RotationAngleInDegrees = value;
			}
		}
	}

	private static void SetVector2(CompositionObject target, string property, Vector2 value)
	{
		if (!(target is CompositionShape compositionShape))
		{
			if (!(target is CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry))
			{
				if (target is Visual visual && property == "Size")
				{
					visual.Size = value;
				}
				return;
			}
			if (property == "Size")
			{
				compositionRoundedRectangleGeometry.Size = value;
				return;
			}
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry2 = compositionRoundedRectangleGeometry;
			if (property == "Offset")
			{
				compositionRoundedRectangleGeometry2.Offset = value;
			}
		}
		else if (property == "Scale")
		{
			compositionShape.Scale = value;
		}
		else
		{
			CompositionShape compositionShape2 = compositionShape;
			if (property == "Offset")
			{
				compositionShape2.Offset = value;
			}
		}
	}

	private static void SetVector3(CompositionObject target, string property, Vector3 value)
	{
		if (!(target is Visual visual))
		{
			return;
		}
		if (property == "Scale")
		{
			visual.Scale = value;
			return;
		}
		Visual visual2 = visual;
		if (property == "Offset")
		{
			visual2.Offset = value;
		}
	}

	internal static string? Str(JsonElement element, string name)
	{
		if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		return value.GetString();
	}

	internal static string? Attr(JsonElement attributes, string name)
	{
		if (attributes.ValueKind != JsonValueKind.Object || !attributes.TryGetProperty(name, out var value))
		{
			return null;
		}
		return value.ValueKind switch
		{
			JsonValueKind.String => value.GetString(), 
			JsonValueKind.Number => value.GetRawText(), 
			JsonValueKind.True => "true", 
			JsonValueKind.False => "false", 
			_ => null, 
		};
	}

	private void ApplyWrap(JsonElement operation)
	{
		if (!operation.TryGetProperty("g", out var value) || !operation.TryGetProperty("child", out var value2) || value.ValueKind != JsonValueKind.Number || value2.ValueKind != JsonValueKind.Number || !unitNodes.TryGetValue(value2.GetInt32(), out Node value3))
		{
			return;
		}
		int @int = value.GetInt32();
		Node parent = value3.Parent;
		Node node = ((parent != null && parent.Kind == NodeKind.MaskedContainer) ? parent : value3);
		Node parent2 = node.Parent;
		if (parent2 != null && parent2.Kind == NodeKind.Wrapper)
		{
			unitNodes[@int] = parent2;
			return;
		}
		Node parent3 = node.Parent;
		if (parent3 != null)
		{
			Node node2 = MakeWrapper();
			node2.Unit = @int;
			int num = parent3.Children.IndexOf(node);
			Detach(node);
			InsertAt(node2, parent3, (num < 0) ? parent3.Children.Count : num);
			AppendChild(node2, node);
			unitNodes[@int] = node2;
		}
	}

	private void ApplyProperty(JsonElement operation)
	{
		JsonElement value;
		double? num = ((operation.TryGetProperty("value", out value) && value.ValueKind == JsonValueKind.Number) ? new double?(value.GetDouble()) : ((double?)null));
		JsonElement value2;
		int? num2 = ((operation.TryGetProperty("n", out value2) && value2.ValueKind == JsonValueKind.Number) ? new int?(value2.GetInt32()) : ((int?)null));
		switch (Str(operation, "key"))
		{
		case "opacity":
		{
			if (!num2.HasValue)
			{
				break;
			}
			int valueOrDefault2 = num2.GetValueOrDefault();
			if (unitNodes.TryGetValue(valueOrDefault2, out Node value4) && num.HasValue)
			{
				double valueOrDefault3 = num.GetValueOrDefault();
				ShowLiveIfInked(value4);
				value4.Visual.StopAnimation("Opacity");
				ForgetRecords(value4.Visual, "Opacity");
				value4.Visual.Opacity = (float)valueOrDefault3;
				Node node = GlowNode(valueOrDefault2);
				if (node != null)
				{
					node.Visual.StopAnimation("Opacity");
					ForgetRecords(node.Visual, "Opacity");
					node.Visual.Opacity = (float)valueOrDefault3;
				}
			}
			break;
		}
		case "transform":
		{
			if (!num2.HasValue)
			{
				break;
			}
			int valueOrDefault4 = num2.GetValueOrDefault();
			if (!unitNodes.TryGetValue(valueOrDefault4, out Node value5))
			{
				break;
			}
			ShowLiveIfInked(value5);
			Matrix3x2? transform = SvgValues.Transform(Str(operation, "value"));
			if (value5.Kind == NodeKind.Wrapper)
			{
				SetWrapperTransform(value5, transform);
			}
			else
			{
				Visual visual = value5.Visual;
				Matrix4x4 transformMatrix;
				if (transform.HasValue)
				{
					Matrix3x2 valueOrDefault5 = transform.GetValueOrDefault();
					transformMatrix = SvgValues.To4x4(valueOrDefault5);
				}
				else
				{
					transformMatrix = Matrix4x4.Identity;
				}
				visual.TransformMatrix = transformMatrix;
			}
			Node node2 = GlowNode(valueOrDefault4);
			if (node2 != null)
			{
				Visual visual2 = node2.Visual;
				Matrix4x4 transformMatrix2;
				if (transform.HasValue)
				{
					Matrix3x2 valueOrDefault6 = transform.GetValueOrDefault();
					transformMatrix2 = SvgValues.To4x4(valueOrDefault6);
				}
				else
				{
					transformMatrix2 = Matrix4x4.Identity;
				}
				visual2.TransformMatrix = transformMatrix2;
			}
			break;
		}
		case "dash":
		{
			string text = Str(operation, "mask");
			if (text == null || !num.HasValue)
			{
				break;
			}
			double valueOrDefault = num.GetValueOrDefault();
			if (!containersByMask.TryGetValue(text, out List<Node> value3))
			{
				break;
			}
			{
				foreach (Node item in value3)
				{
					PenMask penMask = EnsurePenMask(item);
					penMask.Geometry.StopAnimation("TrimEnd");
					ForgetRecords(penMask.Geometry, "TrimEnd");
					ForgetRecords(penMask.Geometry, "TrimEnd");
					penMask.ModelTrimEnd = (float)(1.0 - valueOrDefault);
					penMask.Geometry.TrimEnd = penMask.ModelTrimEnd;
					penMask.EditRunning = false;
					if (penMask.ModelTrimEnd >= 0.999f)
					{
						ScheduleMaskRemoval(item, Now + 0.05);
					}
				}
				break;
			}
		}
		}
	}

	private void ApplyStop(JsonElement operation)
	{
		if (operation.TryGetProperty("n", out var value) && value.ValueKind == JsonValueKind.Number && unitNodes.TryGetValue(value.GetInt32(), out Node value2))
		{
			FinishAnimations(value2);
		}
		string text = Str(operation, "mask");
		if (text == null || !containersByMask.TryGetValue(text, out List<Node> value3))
		{
			return;
		}
		foreach (Node item in value3)
		{
			PenMask mask = item.Mask;
			if (mask != null)
			{
				mask.Geometry.StopAnimation("TrimEnd");
				mask.Geometry.TrimEnd = mask.ModelTrimEnd;
				mask.PenEndTime = Math.Min(mask.PenEndTime, Now);
				if (!mask.EditRunning)
				{
					ScheduleMaskRemoval(item, Now + 0.05);
				}
			}
		}
	}

	private void ApplyMotion(JsonElement operation)
	{
		if (!operation.TryGetProperty("items", out var value) || value.ValueKind != JsonValueKind.Array)
		{
			return;
		}
		JsonElement value2;
		double duration = Math.Max(0.001, (operation.TryGetProperty("dur", out value2) && value2.ValueKind == JsonValueKind.Number) ? value2.GetDouble() : 0.001);
		JsonElement value3;
		double beginTime = Math.Max((operation.TryGetProperty("begin", out value3) && value3.ValueKind == JsonValueKind.Number) ? value3.GetDouble() : Now, Now);
		Vector4 timing = new Vector4(0.42f, 0f, 0.58f, 1f);
		if (operation.TryGetProperty("fn", out var value4) && value4.ValueKind == JsonValueKind.Array && value4.GetArrayLength() == 4)
		{
			float[] array = (from e in value4.EnumerateArray()
				select (e.ValueKind != JsonValueKind.Number) ? 0f : ((float)e.GetDouble())).ToArray();
			timing = new Vector4(array[0], array[1], array[2], array[3]);
		}
		AnimationSpec spec = new AnimationSpec("motion", duration, beginTime, timing);
		foreach (JsonElement item in value.EnumerateArray())
		{
			JsonElement value5;
			double num = ((item.TryGetProperty("from", out value5) && value5.ValueKind == JsonValueKind.Number) ? value5.GetDouble() : 0.0);
			JsonElement value6;
			double num2 = ((item.TryGetProperty("to", out value6) && value6.ValueKind == JsonValueKind.Number) ? value6.GetDouble() : 0.0);
			switch (Str(item, "key"))
			{
			case "opacity":
			{
				if (item.TryGetProperty("n", out var value9) && value9.ValueKind == JsonValueKind.Number && unitNodes.TryGetValue(value9.GetInt32(), out Node value10))
				{
					ShowLiveIfInked(value10);
					AnimateScalar(value10.Visual, "Opacity", (float)num, (float)num2, spec, fillBackwards: false);
					Node node = GlowNode(value9.GetInt32());
					if (node != null)
					{
						AnimateScalar(node.Visual, "Opacity", (float)num, (float)num2, spec, fillBackwards: false);
					}
				}
				break;
			}
			case "dash":
			{
				string text = Str(item, "mask");
				if (text == null || !containersByMask.TryGetValue(text, out List<Node> value11))
				{
					break;
				}
				foreach (Node item2 in value11)
				{
					PenMask penMask = EnsurePenMask(item2);
					penMask.EditRunning = true;
					penMask.EditEndTime = Math.Max(penMask.EditEndTime, spec.EndTime);
					penMask.ModelTrimEnd = (float)(1.0 - num2);
					AnimateScalar(penMask.Geometry, "TrimEnd", (float)(1.0 - num), (float)(1.0 - num2), spec, fillBackwards: false);
				}
				break;
			}
			case "transform":
			{
				if (!item.TryGetProperty("n", out var value7) || value7.ValueKind != JsonValueKind.Number || !unitNodes.TryGetValue(value7.GetInt32(), out Node value8))
				{
					break;
				}
				(float, float, float)? tuple = EditTransform(item, "from");
				if (!tuple.HasValue)
				{
					break;
				}
				(float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				tuple = EditTransform(item, "to");
				if (tuple.HasValue)
				{
					(float, float, float) valueOrDefault2 = tuple.GetValueOrDefault();
					ShowLiveIfInked(value8);
					bool fillBackwards = !value8.HasTransformMotion;
					value8.HasTransformMotion = true;
					Visual visual = value8.Visual;
					visual.TransformMatrix = Matrix4x4.Identity;
					value8.WrapperScale = new Vector3(valueOrDefault2.Item1, valueOrDefault2.Item1, 1f);
					value8.WrapperOffset = new Vector3(valueOrDefault2.Item2, valueOrDefault2.Item3, 0f);
					visual.Scale = value8.WrapperScale;
					visual.Offset = value8.WrapperOffset;
					if (spec.Duration <= 0.002 && spec.BeginTime <= Now + 0.05)
					{
						visual.StopAnimation("Scale");
						visual.StopAnimation("Offset");
						visual.Scale = value8.WrapperScale;
						visual.Offset = value8.WrapperOffset;
					}
					else
					{
						AnimateVector3(visual, "Scale", new Vector3(valueOrDefault.Item1, valueOrDefault.Item1, 1f), value8.WrapperScale, spec, fillBackwards);
						AnimateVector3(visual, "Offset", new Vector3(valueOrDefault.Item2, valueOrDefault.Item3, 0f), value8.WrapperOffset, spec, fillBackwards);
					}
				}
				break;
			}
			}
		}
	}

	private static (float Scale, float X, float Y)? EditTransform(JsonElement item, string key)
	{
		if (!item.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
		{
			return null;
		}
		float[] array = (from e in value.EnumerateArray()
			select (e.ValueKind != JsonValueKind.Number) ? 0f : ((float)e.GetDouble())).ToArray();
		return (array[0], array[1], array[2]);
	}

	public void FadeOut(double duration = 0.45)
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, Easing(AnimationSpec.CssEase));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration);
		StageVisual.StartAnimation("Opacity", scalarKeyFrameAnimation);
		Record(StageVisual, "Opacity", new Vector4(StageVisual.Opacity, 0f, 0f, 0f), Vector4.Zero, new AnimationSpec("fade", duration, Now, AnimationSpec.CssEase), fillBackwards: true);
	}

	private List<Node> NodesFor(IEnumerable<int> unitsToFind)
	{
		IReadOnlyCollection<int> readOnlyCollection = (unitsToFind as IReadOnlyCollection<int>) ?? unitsToFind.ToList();
		HashSet<Node> hashSet = new HashSet<Node>();
		List<Node> list = new List<Node>();
		foreach (int item in readOnlyCollection)
		{
			if (unitNodes.TryGetValue(item, out Node value))
			{
				Node parent = value.Parent;
				if (parent != null && parent.Kind == NodeKind.MaskedContainer)
				{
					value = parent;
				}
				if (hashSet.Add(value))
				{
					list.Add(value);
				}
			}
		}
		foreach (int item2 in readOnlyCollection)
		{
			Node node = GlowNode(item2);
			if (node != null && hashSet.Add(node))
			{
				list.Add(node);
			}
		}
		return list;
	}

	public void BeginCardDrag(IEnumerable<int> units)
	{
		EndCardDrag();
		foreach (Node item in NodesFor(units))
		{
			cardDragNodes.Add((item, item.Visual.Offset));
		}
	}

	public void MoveCardDrag(Vector2 offset)
	{
		foreach (var (node, vector) in cardDragNodes)
		{
			node.Visual.Offset = vector + new Vector3(offset, 0f);
		}
	}

	public void EndCardDrag(bool keepWhereDropped = false)
	{
		foreach (var (node, offset) in cardDragNodes)
		{
			node.Visual.Offset = offset;
		}
		cardDragNodes.Clear();
	}

	[IteratorStateMachine(typeof(_003COtherNodes_003Ed__102))]
	private IEnumerable<Node> OtherNodes(IEnumerable<int> keptUnits)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003COtherNodes_003Ed__102(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__keptUnits = keptUnits
		};
	}

	public void BeginIsolation(IEnumerable<int> keptUnits)
	{
		EndIsolation(animated: false);
		foreach (Node item in OtherNodes(keptUnits).ToList())
		{
			item.Visual.IsVisible = false;
			isolationHiddenNodes.Add(item);
		}
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.RelativeSizeAdjustment = Vector2.One;
		spriteVisual.Size = StageVisual.Size;
		spriteVisual.Brush = Brush(Color.FromArgb(107, 0, 0, 0));
		StageVisual.Children.InsertAtBottom(spriteVisual);
		fullScreenBackdrop = spriteVisual;
		AnimateScalar(spriteVisual, "Opacity", 0f, 1f, new AnimationSpec("backdrop", 0.3, Now, AnimationSpec.CssEase));
	}

	public void EndIsolation(bool animated = true)
	{
		foreach (Node isolationHiddenNode in isolationHiddenNodes)
		{
			isolationHiddenNode.Visual.IsVisible = true;
		}
		isolationHiddenNodes.Clear();
		SpriteVisual backdrop = fullScreenBackdrop;
		if ((object)backdrop == null)
		{
			return;
		}
		fullScreenBackdrop = null;
		if (!animated)
		{
			StageVisual.Children.Remove(backdrop);
			return;
		}
		CompositionScopedBatch compositionScopedBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
		AnimateScalar(backdrop, "Opacity", backdrop.Opacity, 0f, new AnimationSpec("backdrop", 0.25, Now, AnimationSpec.CssEase));
		compositionScopedBatch.End();
		compositionScopedBatch.Completed += delegate
		{
			try
			{
				StageVisual.Children.Remove(backdrop);
			}
			catch
			{
			}
		};
	}

	public CanvasRenderTarget RenderUnitsSnapshot(IEnumerable<int> keptUnits, double time, float pixelsPerUnit)
	{
		List<Node> list = (from n in OtherNodes(keptUnits)
			where n.Visual.IsVisible
			select n).ToList();
		SpriteVisual spriteVisual = fullScreenBackdrop;
		try
		{
			foreach (Node item in list)
			{
				item.Visual.IsVisible = false;
			}
			fullScreenBackdrop = null;
			return RenderSnapshot(time, pixelsPerUnit);
		}
		finally
		{
			foreach (Node item2 in list)
			{
				item2.Visual.IsVisible = true;
			}
			fullScreenBackdrop = spriteVisual;
		}
	}

	public void HideUnits(IEnumerable<int> units)
	{
		foreach (Node item in NodesFor(units))
		{
			item.Visual.IsVisible = false;
		}
	}

	public (double X, double Y, double Width, double Height)? ExtentOf(int unit)
	{
		if (!unitNodes.TryGetValue(unit, out Node value))
		{
			return null;
		}
		return value.Extent;
	}

	private CompositionDrawingSurface? CreateSurface(Vector2 pixelSize)
	{
		if (pixelSize.X < 1f || pixelSize.Y < 1f || (double)(pixelSize.X * pixelSize.Y) > 12000000.0)
		{
			return null;
		}
		return graphicsDevice.CreateDrawingSurface(new Size(pixelSize.X, pixelSize.Y), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
	}

	private Node MakeWashNode(SvgPath path, JsonElement attributes, AnimationSpec? animation)
	{
		Color color = CssColor.Paint(Attr(attributes, "fill") ?? "#000000", Attr(attributes, "fill-opacity")) ?? Colors.Black;
		(double, double, double, double) tuple = path.BoundingBox();
		(double, double, double, double) tuple2 = (tuple.Item1 - tuple.Item3 * 0.1, tuple.Item2 - tuple.Item4 * 0.1, tuple.Item3 * 1.2, tuple.Item4 * 1.2);
		float scale = (float)DevicePixelsPerUnit;
		Vector2 vector = new Vector2(MathF.Ceiling((float)tuple2.Item3 * scale), MathF.Ceiling((float)tuple2.Item4 * scale));
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2((float)tuple2.Item3, (float)tuple2.Item4);
		spriteVisual.Offset = new Vector3((float)tuple2.Item1, (float)tuple2.Item2, 0f);
		CompositionDrawingSurface compositionDrawingSurface = CreateSurface(vector);
		if ((object)compositionDrawingSurface != null)
		{
			try
			{
				CanvasGeometry geometry = path.ToGeometry(canvasDevice, null);
				CanvasCommandList canvasCommandList = new CanvasCommandList(canvasDevice);
				using (CanvasDrawingSession canvasDrawingSession = canvasCommandList.CreateDrawingSession())
				{
					canvasDrawingSession.Transform = Matrix3x2.CreateTranslation(0f - (float)tuple2.Item1, 0f - (float)tuple2.Item2) * Matrix3x2.CreateScale(scale);
					canvasDrawingSession.FillGeometry(geometry, color);
				}
				TurbulenceEffect source = new TurbulenceEffect
				{
					Frequency = new Vector2(0.035f / scale),
					Octaves = 2,
					Seed = 3,
					Noise = TurbulenceEffectNoise.FractalSum,
					Offset = new Vector2((float)tuple2.Item1 * scale, (float)tuple2.Item2 * scale),
					Size = vector
				};
				Transform2DEffect displacement = new Transform2DEffect
				{
					Source = source,
					TransformMatrix = Matrix3x2.CreateTranslation((0f - (float)tuple2.Item1) * scale, (0f - (float)tuple2.Item2) * scale)
				};
				DisplacementMapEffect displaced = new DisplacementMapEffect
				{
					Source = canvasCommandList,
					Displacement = displacement,
					Amount = 3.5f * scale,
					XChannelSelect = EffectChannelSelect.Alpha,
					YChannelSelect = EffectChannelSelect.Alpha
				};
				using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(compositionDrawingSurface))
				{
					CanvasDrawingSession session = surfaceDrawing.Session;
					session.Clear(Colors.Transparent);
					session.DrawImage(displaced);
				}
				spriteContents[spriteVisual] = delegate(CanvasDrawingSession s, Vector2 size)
				{
					Matrix3x2 transform = s.Transform;
					s.Transform = Matrix3x2.CreateScale(1f / scale) * transform;
					s.DrawImage(displaced);
					s.Transform = transform;
				};
				CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(compositionDrawingSurface);
				compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
				spriteVisual.Brush = compositionSurfaceBrush;
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "wash failed: " + ex.Message);
			}
		}
		Visual visual = spriteVisual;
		Matrix3x2? matrix3x = SvgValues.Transform(Attr(attributes, "transform"));
		if (matrix3x.HasValue)
		{
			Matrix3x2 valueOrDefault = matrix3x.GetValueOrDefault();
			ContainerVisual containerVisual = compositor.CreateContainerVisual();
			containerVisual.TransformMatrix = SvgValues.To4x4(valueOrDefault);
			containerVisual.Children.InsertAtTop(spriteVisual);
			visual = containerVisual;
		}
		Node node = new Node
		{
			Kind = NodeKind.Wash,
			Visual = visual,
			Extent = (X: tuple2.Item1, Y: tuple2.Item2, Width: tuple2.Item3, Height: tuple2.Item4)
		};
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault2 = animation.GetValueOrDefault();
			if (valueOrDefault2.Name == "fadein")
			{
				AnimateScalar(visual, "Opacity", 0f, 1f, valueOrDefault2, fillBackwards: true, node);
			}
		}
		return node;
	}

	private Node MakeGrainNode(SvgPath path, JsonElement attributes)
	{
		Color color = CssColor.Paint(Attr(attributes, "fill") ?? "#000000", Attr(attributes, "fill-opacity")) ?? Colors.Black;
		(double, double, double, double) tuple = path.BoundingBox();
		(double, double, double, double) extent = (tuple.Item1 - 2.0, tuple.Item2 - 2.0, tuple.Item3 + 4.0, tuple.Item4 + 4.0);
		float scale = (float)DevicePixelsPerUnit;
		Vector2 vector = new Vector2(MathF.Ceiling((float)extent.Item3 * scale), MathF.Ceiling((float)extent.Item4 * scale));
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2((float)extent.Item3, (float)extent.Item4);
		spriteVisual.Offset = new Vector3((float)extent.Item1, (float)extent.Item2, 0f);
		CompositionDrawingSurface compositionDrawingSurface = CreateSurface(vector);
		if ((object)compositionDrawingSurface != null)
		{
			try
			{
				Matrix3x2 matrix3x = SvgValues.Transform(Attr(attributes, "transform")) ?? Matrix3x2.Identity;
				CanvasGeometry geometry = path.ToGeometry(canvasDevice, null);
				CanvasCommandList canvasCommandList = new CanvasCommandList(canvasDevice);
				using (CanvasDrawingSession canvasDrawingSession = canvasCommandList.CreateDrawingSession())
				{
					canvasDrawingSession.Transform = matrix3x * Matrix3x2.CreateTranslation(0f - (float)extent.Item1, 0f - (float)extent.Item2) * Matrix3x2.CreateScale(scale);
					canvasDrawingSession.FillGeometry(geometry, color);
				}
				TurbulenceEffect source = new TurbulenceEffect
				{
					Frequency = new Vector2(0.9f / scale),
					Octaves = 1,
					Seed = 7,
					Noise = TurbulenceEffectNoise.FractalSum,
					Offset = new Vector2((float)extent.Item1 * scale, (float)extent.Item2 * scale),
					Size = vector
				};
				Transform2DEffect source2 = new Transform2DEffect
				{
					Source = source,
					TransformMatrix = Matrix3x2.CreateTranslation((0f - (float)extent.Item1) * scale, (0f - (float)extent.Item2) * scale)
				};
				ColorMatrixEffect alphaMask = new ColorMatrixEffect
				{
					Source = source2,
					ClampOutput = true,
					ColorMatrix = new Matrix5x4
					{
						M44 = -1.2f,
						M54 = 1.1f
					}
				};
				AlphaMaskEffect masked = new AlphaMaskEffect
				{
					Source = canvasCommandList,
					AlphaMask = alphaMask
				};
				using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(compositionDrawingSurface))
				{
					CanvasDrawingSession session = surfaceDrawing.Session;
					session.Clear(Colors.Transparent);
					session.DrawImage(masked);
				}
				spriteContents[spriteVisual] = delegate(CanvasDrawingSession s, Vector2 size)
				{
					Matrix3x2 transform = s.Transform;
					s.Transform = Matrix3x2.CreateScale(1f / scale) * transform;
					s.DrawImage(masked);
					s.Transform = transform;
				};
				spriteVisual.Brush = compositor.CreateSurfaceBrush(compositionDrawingSurface);
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "grain failed: " + ex.Message);
			}
		}
		return new Node
		{
			Kind = NodeKind.Shape,
			Visual = spriteVisual,
			Extent = extent
		};
	}

	private Node MakePhotoNode(JsonElement attributes, AnimationSpec? animation)
	{
		(double, double, double, double) extent = (SvgValues.Number(Attr(attributes, "x")), SvgValues.Number(Attr(attributes, "y")), Math.Max(8.0, SvgValues.Number(Attr(attributes, "width"))), Math.Max(8.0, SvgValues.Number(Attr(attributes, "height"))));
		Vector2 vector = new Vector2((float)extent.Item3, (float)extent.Item4);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Size = vector;
		containerVisual.Offset = new Vector3((float)extent.Item1, (float)extent.Item2, 0f);
		containerVisual.CenterPoint = new Vector3(vector / 2f, 0f);
		containerVisual.RotationAngleInDegrees = (float)SvgValues.Number(Attr(attributes, "data-rotate"));
		float value = (float)SvgValues.Number(Attr(attributes, "data-radius"));
		CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = compositor.CreateRoundedRectangleGeometry();
		compositionRoundedRectangleGeometry.Size = vector;
		compositionRoundedRectangleGeometry.CornerRadius = new Vector2(value);
		containerVisual.Clip = compositor.CreateGeometricClip(compositionRoundedRectangleGeometry);
		string text = Attr(attributes, "data-query") ?? "";
		bool flag = text.StartsWith("favicon:", StringComparison.Ordinal);
		if (!flag)
		{
			SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.RelativeSizeAdjustment = Vector2.One;
			spriteVisual.Brush = Brush(Color.FromArgb(byte.MaxValue, 224, 227, 232));
			containerVisual.Children.InsertAtTop(spriteVisual);
		}
		else
		{
			containerVisual.Opacity = 0.7f;
		}
		Node node = new Node
		{
			Kind = NodeKind.Photo,
			Visual = containerVisual,
			Extent = extent
		};
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault = animation.GetValueOrDefault();
			if (valueOrDefault.Name == "fadein")
			{
				AnimateScalar(containerVisual, "Opacity", 0f, flag ? 0.7f : 1f, valueOrDefault, fillBackwards: true, node);
			}
		}
		double printShowsAt = animation?.BeginTime ?? Now;
		Func<string, CancellationToken, Task<byte[]>> photoProvider = PhotoProvider;
		if (photoProvider != null && text.Length > 0)
		{
			LoadPhotoAsync(photoProvider, text, containerVisual, vector, flag, printShowsAt);
		}
		return node;
	}

	private async Task LoadPhotoAsync(Func<string, CancellationToken, Task<byte[]?>> provider, string query, ContainerVisual holder, Vector2 size, bool isSourceIcon, double printShowsAt)
	{
		int generationAtStart = generation;
		try
		{
			byte[] array = await provider(query, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			if (array == null || array.Length == 0)
			{
				return;
			}
			using IRandomAccessStream stream = new MemoryStream(array).AsRandomAccessStream();
			CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(canvasDevice, stream).AsTask().ConfigureAwait(continueOnCapturedContext: false);
			thread.Post(delegate
			{
				if (generation == generationAtStart)
				{
					float num = (float)DevicePixelsPerUnit;
					Vector2 vector = size * num;
					Vector2 vector2 = new Vector2(bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height);
					float num2 = (isSourceIcon ? Math.Min(vector.X / vector2.X, vector.Y / vector2.Y) : Math.Max(vector.X / vector2.X, vector.Y / vector2.Y));
					Vector2 value = Vector2.Min(vector2, vector2 * Math.Min(1f, num2 * 1f + 0.0001f));
					value = Vector2.Max(value, Vector2.One);
					CompositionDrawingSurface compositionDrawingSurface = CreateSurface(new Vector2(MathF.Ceiling(value.X), MathF.Ceiling(value.Y)));
					if ((object)compositionDrawingSurface != null)
					{
						using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(compositionDrawingSurface))
						{
							CanvasDrawingSession session = surfaceDrawing.Session;
							session.Clear(Colors.Transparent);
							session.DrawImage(bitmap, new Rect(0.0, 0.0, Math.Ceiling(value.X), Math.Ceiling(value.Y)), bitmap.Bounds, 1f, CanvasImageInterpolation.HighQualityCubic);
						}
						CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(compositionDrawingSurface);
						compositionSurfaceBrush.Stretch = (isSourceIcon ? CompositionStretch.Uniform : CompositionStretch.UniformToFill);
						compositionSurfaceBrush.HorizontalAlignmentRatio = 0.5f;
						compositionSurfaceBrush.VerticalAlignmentRatio = 0.5f;
						SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
						spriteVisual.RelativeSizeAdjustment = Vector2.One;
						spriteVisual.Brush = compositionSurfaceBrush;
						holder.Children.InsertAtTop(spriteVisual);
						spriteContents[spriteVisual] = delegate(CanvasDrawingSession s, Vector2 box)
						{
							Size size2 = bitmap.Size;
							double num3 = (isSourceIcon ? Math.Min((double)box.X / size2.Width, (double)box.Y / size2.Height) : Math.Max((double)box.X / size2.Width, (double)box.Y / size2.Height));
							double num4 = size2.Width * num3;
							double num5 = size2.Height * num3;
							double num6 = Math.Max(0.0, (num4 - (double)box.X) / 2.0 / num3);
							double num7 = Math.Max(0.0, (num5 - (double)box.Y) / 2.0 / num3);
							s.DrawImage(destinationRectangle: new Rect(Math.Max(0.0, ((double)box.X - num4) / 2.0), Math.Max(0.0, ((double)box.Y - num5) / 2.0), Math.Min(num4, box.X), Math.Min(num5, box.Y)), image: bitmap, sourceRectangle: new Rect(num6, num7, size2.Width - 2.0 * num6, size2.Height - 2.0 * num7));
						};
						double now = Now;
						if (!isSourceIcon && !thread.IsClockPaused && now > printShowsAt)
						{
							AnimationSpec spec = new AnimationSpec("photo", 0.45, now, PhotoArrivalTiming);
							spriteVisual.CenterPoint = new Vector3(size / 2f, 0f);
							AnimateScalar(spriteVisual, "Opacity", 0f, 1f, spec);
							AnimateVector3(spriteVisual, "Scale", new Vector3(1.03f, 1.03f, 1f), Vector3.One, spec);
						}
						this.PhotoShown?.Invoke();
					}
				}
			});
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "photo \"" + query + "\" did not load: " + ex.Message);
		}
	}

	private static float MaskLineWidth(MaskDefinition definition, float? glowRadius)
	{
		float lineWidth = definition.LineWidth;
		float num;
		if (glowRadius.HasValue)
		{
			float valueOrDefault = glowRadius.GetValueOrDefault();
			num = 3f * valueOrDefault;
		}
		else
		{
			num = 0f;
		}
		return lineWidth + num;
	}

	private Node MakeGlowContainer(SvgPath outline, MaskDefinition mask, string maskId, Color color, (float Opacity, float Radius) glow)
	{
		Node node = MakeMaskedContainer(mask, maskId, glow.Radius);
		ShapeVisual shapeVisual = MakeShapeVisual();
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(CompositionPathFor(outline.ToGeometry(canvasDevice, null))));
		compositionSpriteShape.FillBrush = Brush(Color.FromArgb(byte.MaxValue, color.R, color.G, color.B));
		shapeVisual.Shapes.Add(compositionSpriteShape);
		(double, double, double, double) extent = outline.BoundingBox();
		SpriteVisual spriteVisual = CompositionShadows.Cast(compositor, shapeVisual, (X: extent.Item1 - 1.0, Y: extent.Item2 - 1.0, Width: extent.Item3 + 2.0, Height: extent.Item4 + 2.0), Color.FromArgb((byte)Math.Round(glow.Opacity * 255f), color.R, color.G, color.B), glow.Radius, Vector2.Zero);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Comment = "glow";
		containerVisual.Children.InsertAtTop(spriteVisual);
		AppendChild(node, new Node
		{
			Kind = NodeKind.Shape,
			Visual = containerVisual,
			Extent = extent,
			Tag = spriteVisual
		});
		return node;
	}

	private Node? GlowNode(int unit)
	{
		if (!glowNodesByUnit.TryGetValue(unit, out Node value))
		{
			return null;
		}
		return value;
	}

	private void RemoveGlow(int unit)
	{
		if (glowNodesByUnit.Remove(unit, out Node value))
		{
			ForgetMaskedContainer(value);
			Detach(value);
			DisposeMask(value);
		}
	}

	private void AnimateSpringVector2(CompositionObject target, string property, Vector2 from, Vector2 to, double beginTime, double response, Node owner)
	{
		double duration = ScreenPenMath.SpringSettlingSeconds(response);
		Vector2KeyFrameAnimation vector2KeyFrameAnimation = compositor.CreateVector2KeyFrameAnimation();
		LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
		for (int i = 1; i <= 48; i++)
		{
			float num = (float)i / 48f;
			float amount = ((i == 48) ? 1f : ((float)ScreenPenMath.SpringProgress((double)num * duration, response)));
			vector2KeyFrameAnimation.InsertKeyFrame(num, Vector2.Lerp(from, to, amount), easingFunction);
		}
		vector2KeyFrameAnimation.InsertKeyFrame(0f, from);
		vector2KeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration);
		double num2 = beginTime - Now;
		if (num2 > 0.0)
		{
			vector2KeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num2);
		}
		vector2KeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		target.StartAnimation(property, vector2KeyFrameAnimation);
		Record(target, property, new Vector4(from, 0f, 0f), new Vector4(to, 0f, 0f), new AnimationSpec("spring", duration, Math.Max(beginTime, Now), new Vector4(0f, 0f, 1f, 1f)), fillBackwards: true, (double progress) => (!(progress >= 1.0)) ? ScreenPenMath.SpringProgress(progress * duration, response) : 1.0);
		owner.AddAnimation(new RunningAnimation(target, property, new Vector3(to, 0f), 2));
	}

	private void FadeAwayAt(Visual visual, double beginTime, double duration, Node owner)
	{
		AnimateScalar(visual, "Opacity", 1f, 0f, new AnimationSpec("fade", duration, Math.Max(beginTime, Now), EaseOut), fillBackwards: true, owner);
	}

	private (Node Node, PenStroke? CursorStroke) MakeMarqueeNode(ScreenPenMath.MarqueeBox marquee, JsonElement attributes, AnimationSpec? animation)
	{
		double pointInUnits = 1.0 / Math.Max(PointsPerUnit, 0.01);
		bool hasCorners = marquee.Radius > 0.0;
		Vector2 finalSize = new Vector2((float)marquee.Width, (float)marquee.Height);
		Vector2 startSize = new Vector2((float)Math.Max(1.0, marquee.Width * 0.02), (float)Math.Max(1.0, marquee.Height * 0.02));
		float finalCorner = ScreenPenMath.MarqueeCorner(marquee.Width, marquee.Height, marquee.Radius, hasCorners);
		float startCorner = ScreenPenMath.MarqueeCorner(startSize.X, startSize.Y, marquee.Radius * 0.02, hasCorners);
		Vector2 origin = new Vector2((float)marquee.X, (float)marquee.Y);
		bool flag = Attr(attributes, "data-marquee-handle") != null;
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Comment = (flag ? "marquee" : "marquee tint");
		double num = 20.0 * pointInUnits;
		Node node = new Node
		{
			Kind = NodeKind.Shape,
			Visual = containerVisual,
			Extent = (X: marquee.X - num, Y: marquee.Y - num, Width: marquee.Width + 2.0 * num, Height: marquee.Height + 2.0 * num)
		};
		double response = Math.Max(0.15, (animation?.Duration ?? 0.55) * 0.87);
		if (!flag)
		{
			CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(Box());
			Color? color2 = CssColor.Paint(Attr(attributes, "fill") ?? "#000000", Attr(attributes, "fill-opacity"));
			if (color2.HasValue)
			{
				Color valueOrDefault = color2.GetValueOrDefault();
				if (valueOrDefault.A > 0)
				{
					compositionSpriteShape.FillBrush = Brush(valueOrDefault);
				}
			}
			ShapeVisual shapeVisual = MakeShapeVisual();
			shapeVisual.Shapes.Add(compositionSpriteShape);
			containerVisual.Children.InsertAtTop(shapeVisual);
			return (Node: node, CursorStroke: null);
		}
		Color ink = CssColor.Paint(Attr(attributes, "stroke") ?? "#3380FF", Attr(attributes, "stroke-opacity")) ?? Color.FromArgb(byte.MaxValue, 51, 128, byte.MaxValue);
		float num2 = (float)SvgValues.Number(Attr(attributes, "stroke-width"));
		float result;
		float[] dashes2 = (from part in (Attr(attributes, "stroke-dasharray") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
			select (!float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) ? float.NaN : result into n
			where !float.IsNaN(n)
			select n).ToArray();
		ShapeVisual shapeVisual2 = MakeShapeVisual();
		shapeVisual2.Shapes.Add(Dashed(Box(), num2, ink, dashes2));
		SpriteVisual spriteVisual = CompositionShadows.Cast(region: (X: marquee.X - (double)num2, Y: marquee.Y - (double)num2, Width: marquee.Width * 1.1 + (double)(2f * num2), Height: marquee.Height * 1.1 + (double)(2f * num2)), compositor: compositor, source: shapeVisual2, color: Color.FromArgb((byte)Math.Round(140.25), ink.R, ink.G, ink.B), blurRadius: (float)(12.0 * pointInUnits), offset: Vector2.Zero);
		Node node2 = node;
		node2.Tag = spriteVisual;
		Vector2 guideStart;
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault2 = animation.GetValueOrDefault();
			ShapeVisual shapeVisual3 = MakeShapeVisual();
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = compositor.CreateRoundedRectangleGeometry();
			compositionRoundedRectangleGeometry.Offset = origin;
			compositionRoundedRectangleGeometry.Size = finalSize;
			compositionRoundedRectangleGeometry.CornerRadius = new Vector2(finalCorner);
			shapeVisual3.Shapes.Add(Dashed(compositionRoundedRectangleGeometry, (float)pointInUnits, WithAlpha(0.18), new float[2]
			{
				(float)(5.0 * pointInUnits),
				(float)(6.0 * pointInUnits)
			}));
			containerVisual.Children.InsertAtTop(shapeVisual3);
			FadeAwayAt(shapeVisual3, valueOrDefault2.BeginTime, valueOrDefault2.Duration * 1.2, node2);
			containerVisual.Children.InsertAtTop(spriteVisual);
			containerVisual.Children.InsertAtTop(shapeVisual2);
			guideStart = new Vector2((float)(marquee.X + 3.0 * pointInUnits), (float)(marquee.Y + 3.0 * pointInUnits));
			Vector2 vector = origin + startSize;
			Vector2 vector2 = origin + finalSize;
			CompositionLineGeometry compositionLineGeometry = compositor.CreateLineGeometry();
			compositionLineGeometry.Start = guideStart;
			compositionLineGeometry.End = GuideEnd(vector2);
			ShapeVisual shapeVisual4 = MakeShapeVisual();
			shapeVisual4.Shapes.Add(Dashed(compositionLineGeometry, (float)(1.5 * pointInUnits), WithAlpha(0.45), new float[2]
			{
				(float)(4.0 * pointInUnits),
				(float)(4.0 * pointInUnits)
			}));
			containerVisual.Children.InsertAtTop(shapeVisual4);
			AnimateSpringVector2(compositionLineGeometry, "End", GuideEnd(vector), GuideEnd(vector2), valueOrDefault2.BeginTime, response, node2);
			FadeAwayAt(shapeVisual4, valueOrDefault2.BeginTime + valueOrDefault2.Duration * 0.4, valueOrDefault2.Duration * 0.8, node2);
			float value = (float)(3.5 * pointInUnits);
			CompositionEllipseGeometry compositionEllipseGeometry = compositor.CreateEllipseGeometry();
			compositionEllipseGeometry.Radius = new Vector2(value);
			CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(compositionEllipseGeometry);
			compositionSpriteShape2.FillBrush = Brush(Colors.White);
			compositionSpriteShape2.StrokeBrush = Brush(ink);
			compositionSpriteShape2.StrokeThickness = (float)(2.0 * pointInUnits);
			compositionSpriteShape2.Offset = vector2;
			ShapeVisual shapeVisual5 = MakeShapeVisual();
			shapeVisual5.Shapes.Add(compositionSpriteShape2);
			containerVisual.Children.InsertAtTop(shapeVisual5);
			AnimateSpringVector2(compositionSpriteShape2, "Offset", vector, vector2, valueOrDefault2.BeginTime, response, node2);
			FadeAwayAt(shapeVisual5, valueOrDefault2.BeginTime + valueOrDefault2.Duration + 0.25, 0.3, node2);
			PenStroke penStroke = new PenStroke();
			penStroke.Points = new Vector2[2] { vector, vector2 };
			penStroke.BeginTime = valueOrDefault2.BeginTime;
			penStroke.Duration = valueOrDefault2.Duration;
			penStroke.Timing = new Vector4(0.2f, 0.85f, 0.3f, 1f);
			PenStroke item = penStroke;
			return (Node: node2, CursorStroke: item);
		}
		containerVisual.Children.InsertAtTop(spriteVisual);
		containerVisual.Children.InsertAtTop(shapeVisual2);
		return (Node: node2, CursorStroke: null);
		CompositionRoundedRectangleGeometry Box()
		{
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry2 = compositor.CreateRoundedRectangleGeometry();
			compositionRoundedRectangleGeometry2.Offset = origin;
			compositionRoundedRectangleGeometry2.Size = finalSize;
			compositionRoundedRectangleGeometry2.CornerRadius = new Vector2(finalCorner);
			if (animation.HasValue)
			{
				AnimationSpec valueOrDefault3 = animation.GetValueOrDefault();
				AnimateSpringVector2(compositionRoundedRectangleGeometry2, "Size", startSize, finalSize, valueOrDefault3.BeginTime, response, node);
				AnimateSpringVector2(compositionRoundedRectangleGeometry2, "CornerRadius", new Vector2(startCorner), new Vector2(finalCorner), valueOrDefault3.BeginTime, response, node);
			}
			return compositionRoundedRectangleGeometry2;
		}
		CompositionSpriteShape Dashed(CompositionGeometry geometry, float width, Color color, float[] dashes)
		{
			CompositionSpriteShape compositionSpriteShape3 = compositor.CreateSpriteShape(geometry);
			compositionSpriteShape3.StrokeBrush = Brush(color);
			compositionSpriteShape3.StrokeThickness = width;
			CompositionStrokeCap compositionStrokeCap2 = (compositionSpriteShape3.StrokeDashCap = CompositionStrokeCap.Round);
			CompositionStrokeCap strokeStartCap = (compositionSpriteShape3.StrokeEndCap = compositionStrokeCap2);
			compositionSpriteShape3.StrokeStartCap = strokeStartCap;
			compositionSpriteShape3.StrokeLineJoin = CompositionStrokeLineJoin.Round;
			foreach (float item2 in dashes)
			{
				compositionSpriteShape3.StrokeDashArray.Add(item2);
			}
			return compositionSpriteShape3;
		}
		Vector2 GuideEnd(Vector2 corner)
		{
			return new Vector2(Math.Max(guideStart.X, corner.X - (float)(3.0 * pointInUnits)), Math.Max(guideStart.Y, corner.Y - (float)(3.0 * pointInUnits)));
		}
		Color WithAlpha(double alpha)
		{
			return Color.FromArgb((byte)Math.Round((double)(int)ink.A * alpha), ink.R, ink.G, ink.B);
		}
	}

	public void FadeTo(float opacity, double duration)
	{
		float x = Scalar(StageVisual, "Opacity", StageVisual.Opacity, Now);
		ForgetRecords(StageVisual, "Opacity");
		if (duration <= 0.0)
		{
			StageVisual.StopAnimation("Opacity");
			StageVisual.Opacity = opacity;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		scalarKeyFrameAnimation.InsertKeyFrame(1f, opacity, Easing(AnimationSpec.CssEase));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration);
		StageVisual.StartAnimation("Opacity", scalarKeyFrameAnimation);
		Record(StageVisual, "Opacity", new Vector4(x, 0f, 0f, 0f), new Vector4(opacity, 0f, 0f, 0f), new AnimationSpec("fade", duration, Now, AnimationSpec.CssEase), fillBackwards: true);
	}

	private ShapeVisual MakeShapeVisual()
	{
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = canvasSize;
		return shapeVisual;
	}

	private CompositionSpriteShape MakeSpriteShape(SvgPath path, JsonElement attributes)
	{
		CompositionPathGeometry geometry = compositor.CreatePathGeometry(CompositionPathFor(path.ToGeometry(canvasDevice, null)));
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(geometry);
		Color? color = CssColor.Paint(Attr(attributes, "fill") ?? "#000000", Attr(attributes, "fill-opacity"));
		if (color.HasValue)
		{
			Color valueOrDefault = color.GetValueOrDefault();
			if (valueOrDefault.A > 0)
			{
				compositionSpriteShape.FillBrush = Brush(valueOrDefault);
			}
		}
		string text = Attr(attributes, "stroke");
		if (text != null && text != "none")
		{
			color = CssColor.Paint(text, Attr(attributes, "stroke-opacity"));
			if (color.HasValue)
			{
				Color valueOrDefault2 = color.GetValueOrDefault();
				SvgValues.TryNumber(Attr(attributes, "stroke-width"), out var number);
				compositionSpriteShape.StrokeBrush = Brush(valueOrDefault2);
				compositionSpriteShape.StrokeThickness = (float)((Attr(attributes, "stroke-width") == null) ? 1.0 : number);
				CompositionSpriteShape compositionSpriteShape2 = compositionSpriteShape;
				string text2 = Attr(attributes, "stroke-linejoin");
				CompositionStrokeLineJoin strokeLineJoin = ((text2 == "round") ? CompositionStrokeLineJoin.Round : ((text2 == "bevel") ? CompositionStrokeLineJoin.Bevel : CompositionStrokeLineJoin.Miter));
				compositionSpriteShape2.StrokeLineJoin = strokeLineJoin;
				text2 = Attr(attributes, "stroke-linecap");
				CompositionStrokeCap compositionStrokeCap = ((text2 == "round") ? CompositionStrokeCap.Round : ((text2 == "square") ? CompositionStrokeCap.Square : CompositionStrokeCap.Flat));
				CompositionStrokeCap compositionStrokeCap2 = compositionStrokeCap;
				CompositionStrokeCap compositionStrokeCap4 = (compositionSpriteShape.StrokeDashCap = compositionStrokeCap2);
				compositionStrokeCap = (compositionSpriteShape.StrokeEndCap = compositionStrokeCap4);
				compositionSpriteShape.StrokeStartCap = compositionStrokeCap;
				compositionSpriteShape.StrokeMiterLimit = 4f;
			}
		}
		Matrix3x2? matrix3x = SvgValues.Transform(Attr(attributes, "transform"));
		if (matrix3x.HasValue)
		{
			Matrix3x2 valueOrDefault3 = matrix3x.GetValueOrDefault();
			compositionSpriteShape.TransformMatrix = valueOrDefault3;
		}
		return compositionSpriteShape;
	}

	private Node MakeShapeNode(SvgPath path, JsonElement attributes)
	{
		ShapeVisual shapeVisual = MakeShapeVisual();
		shapeVisual.Shapes.Add(MakeSpriteShape(path, attributes));
		(double, double, double, double) extent = path.BoundingBox();
		return new Node
		{
			Kind = NodeKind.Shape,
			Visual = shapeVisual,
			Extent = extent
		};
	}

	private Node MakePlainPathNode(SvgPath path, JsonElement attributes, JsonElement style, AnimationSpec? animation)
	{
		Node node = MakeShapeNode(path, attributes);
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault = animation.GetValueOrDefault();
			Visual visual = node.Visual;
			(double, double, double, double) extent = node.Extent;
			switch (valueOrDefault.Name)
			{
			case "swipe":
			{
				Vector2 value = SvgValues.TransformOrigin(Attr(style, "transformOrigin")) ?? new Vector2((float)extent.Item1, (float)extent.Item2);
				visual.CenterPoint = new Vector3(value, 0f);
				visual.Scale = Vector3.One;
				AnimateVector3(visual, "Scale", new Vector3(0f, 1f, 1f), Vector3.One, valueOrDefault, fillBackwards: true, node);
				break;
			}
			case "fadein":
				visual.Opacity = 1f;
				AnimateScalar(visual, "Opacity", 0f, 1f, valueOrDefault, fillBackwards: true, node);
				break;
			case "pop":
				visual.CenterPoint = new Vector3((float)(extent.Item1 + extent.Item3 / 2.0), (float)(extent.Item2 + extent.Item4 / 2.0), 0f);
				AnimateVector3(visual, "Scale", Vector3.Zero, Vector3.One, valueOrDefault, fillBackwards: true, node);
				break;
			}
			return node;
		}
		return node;
	}

	private Node MakeDotNode(JsonElement attributes, AnimationSpec? animation)
	{
		Vector2 vector = new Vector2((float)SvgValues.Number(Attr(attributes, "cx")), (float)SvgValues.Number(Attr(attributes, "cy")));
		float num = (float)SvgValues.Number(Attr(attributes, "r"));
		CompositionEllipseGeometry compositionEllipseGeometry = compositor.CreateEllipseGeometry();
		compositionEllipseGeometry.Center = vector;
		compositionEllipseGeometry.Radius = new Vector2(num);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositionEllipseGeometry);
		Color? color = CssColor.Paint(Attr(attributes, "fill") ?? "#000000", Attr(attributes, "fill-opacity"));
		if (color.HasValue)
		{
			Color valueOrDefault = color.GetValueOrDefault();
			if (valueOrDefault.A > 0)
			{
				compositionSpriteShape.FillBrush = Brush(valueOrDefault);
			}
		}
		ShapeVisual shapeVisual = MakeShapeVisual();
		shapeVisual.Shapes.Add(compositionSpriteShape);
		shapeVisual.CenterPoint = new Vector3(vector, 0f);
		Node node = new Node
		{
			Kind = NodeKind.Dot,
			Visual = shapeVisual,
			Extent = (X: vector.X - num, Y: vector.Y - num, Width: num * 2f, Height: num * 2f)
		};
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault2 = animation.GetValueOrDefault();
			if (valueOrDefault2.Name == "pop")
			{
				AnimateVector3(shapeVisual, "Scale", Vector3.Zero, Vector3.One, valueOrDefault2, fillBackwards: true, node);
			}
		}
		return node;
	}

	private Node MakeMaskedContainer(MaskDefinition mask, string maskId, float? glowRadius = null)
	{
		Rect rect = mask.Geometry.ComputeBounds();
		double num = MaskLineWidth(mask, glowRadius) / 2f + 2f;
		(double, double, double, double) extent = (rect.X - num, rect.Y - num, rect.Width + 2.0 * num, rect.Height + 2.0 * num);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Comment = ((!glowRadius.HasValue) ? "pen " : "glow ") + maskId;
		Node node = new Node
		{
			Kind = NodeKind.MaskedContainer,
			Visual = containerVisual,
			Content = containerVisual,
			Children = new List<Node>(),
			MaskId = maskId,
			Extent = extent,
			GlowRadius = glowRadius
		};
		if (!containersByMask.TryGetValue(maskId, out List<Node> value))
		{
			value = (containersByMask[maskId] = new List<Node>());
		}
		value.Add(node);
		AnimationSpec? animation = mask.Animation;
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault = animation.GetValueOrDefault();
			PenMask penMask = EnsurePenMask(node, mask);
			AnimateScalar(penMask.Geometry, "TrimEnd", 0f, 1f, valueOrDefault);
			penMask.PenEndTime = valueOrDefault.EndTime;
			penMask.ModelTrimEnd = 1f;
			ScheduleMaskRemoval(node, valueOrDefault.EndTime + 0.05);
		}
		return node;
	}

	private PenMask EnsurePenMask(Node node, MaskDefinition? definition = null)
	{
		PenMask mask = node.Mask;
		if (mask != null && mask.IsApplied)
		{
			return mask;
		}
		if ((object)definition == null)
		{
			definition = maskDefinitions[node.MaskId];
		}
		float num = (float)DevicePixelsPerUnit;
		(double X, double Y, double Width, double Height) extent = node.Extent;
		double item = extent.X;
		double item2 = extent.Y;
		double item3 = extent.Width;
		double item4 = extent.Height;
		Vector2 vector = new Vector2(MathF.Floor((float)item * num), MathF.Floor((float)item2 * num));
		Vector2 vector2 = new Vector2(MathF.Ceiling((float)(item + item3) * num), MathF.Ceiling((float)(item2 + item4) * num)) - vector;
		PenMask penMask = node.Mask;
		if (penMask == null)
		{
			CompositionPathGeometry geometry = compositor.CreatePathGeometry(CompositionPathFor(definition.Geometry));
			CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(geometry);
			compositionSpriteShape.StrokeBrush = Brush(Colors.White);
			compositionSpriteShape.StrokeThickness = MaskLineWidth(definition, node.GlowRadius);
			CompositionStrokeCap strokeStartCap = (compositionSpriteShape.StrokeEndCap = CompositionStrokeCap.Round);
			compositionSpriteShape.StrokeStartCap = strokeStartCap;
			compositionSpriteShape.StrokeLineJoin = CompositionStrokeLineJoin.Round;
			ShapeVisual shapeVisual = MakeShapeVisual();
			shapeVisual.Shapes.Add(compositionSpriteShape);
			ContainerVisual containerVisual = compositor.CreateContainerVisual();
			containerVisual.Children.InsertAtTop(shapeVisual);
			CompositionVisualSurface compositionVisualSurface = compositor.CreateVisualSurface();
			CompositionSurfaceBrush source = compositor.CreateSurfaceBrush();
			CompositionMaskBrush compositionMaskBrush = compositor.CreateMaskBrush();
			compositionMaskBrush.Source = source;
			compositionMaskBrush.Mask = compositor.CreateSurfaceBrush(compositionVisualSurface);
			SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = compositionMaskBrush;
			PenMask obj = new PenMask
			{
				Geometry = geometry,
				MaskVisual = shapeVisual,
				MaskHolder = containerVisual,
				Sprite = spriteVisual,
				MaskSurface = compositionVisualSurface,
				Source = source,
				Brush = compositionMaskBrush
			};
			PenMask penMask2 = obj;
			node.Mask = obj;
			penMask = penMask2;
			maskedSprites[spriteVisual] = node;
		}
		penMask.PixelOrigin = vector;
		penMask.PixelSize = vector2;
		ReplaceVisual(node, penMask.Sprite);
		penMask.MaskVisual.Scale = new Vector3(num, num, 1f);
		penMask.MaskVisual.Offset = new Vector3(-vector, 0f);
		penMask.MaskVisual.Size = canvasSize + new Vector2((float)(item + item3), (float)(item2 + item4));
		penMask.MaskSurface.SourceVisual = penMask.MaskHolder;
		penMask.MaskSurface.SourceSize = vector2;
		penMask.Sprite.Size = vector2;
		penMask.Sprite.Scale = new Vector3(1f / num, 1f / num, 1f);
		penMask.Sprite.Offset = new Vector3(vector / num, 0f);
		penMask.IsApplied = true;
		if (!ShowsOnScreen)
		{
			return penMask;
		}
		float? glowRadius = node.GlowRadius;
		if (!glowRadius.HasValue)
		{
			DrawInk(node, penMask);
		}
		else
		{
			ShowLiveContent(node, penMask);
		}
		return penMask;
	}

	private void DrawInk(Node node, PenMask mask)
	{
		mask.IsInkStale = false;
		ContainerVisual content = node.Content;
		CompositionVisualSurface contentSurface = mask.ContentSurface;
		if ((object)contentSurface != null)
		{
			contentSurface.SourceVisual = null;
			if (content.Parent == mask.ContentHolder)
			{
				mask.ContentHolder.Children.Remove(content);
			}
			content.Scale = Vector3.One;
			content.Offset = Vector3.Zero;
		}
		CompositionDrawingSurface ink = mask.Ink;
		if ((object)ink != null && (ink.Size.Width != (double)mask.PixelSize.X || ink.Size.Height != (double)mask.PixelSize.Y))
		{
			ink.Dispose();
			mask.Ink = null;
		}
		if ((object)mask.Ink == null)
		{
			mask.Ink = CreateSurface(mask.PixelSize);
		}
		CompositionDrawingSurface ink2 = mask.Ink;
		if ((object)ink2 == null)
		{
			ShowLiveContent(node, mask);
			return;
		}
		mask.Source.Surface = ink2;
		try
		{
			using SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(ink2);
			CanvasDrawingSession session = surfaceDrawing.Session;
			session.Clear(Colors.Transparent);
			Matrix3x2 parent = Matrix3x2.CreateScale((float)DevicePixelsPerUnit) * Matrix3x2.CreateTranslation(-mask.PixelOrigin);
			foreach (Node child in node.Children)
			{
				DrawNode(session, child, parent, Now);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "pen ink failed: " + ex.Message);
			ShowLiveContent(node, mask);
		}
	}

	private void ShowLiveContent(Node node, PenMask mask)
	{
		float num = (float)DevicePixelsPerUnit;
		ContainerVisual content = node.Content;
		PenMask penMask = mask;
		ContainerVisual containerVisual = penMask.ContentHolder ?? (penMask.ContentHolder = compositor.CreateContainerVisual());
		penMask = mask;
		CompositionVisualSurface compositionVisualSurface = penMask.ContentSurface ?? (penMask.ContentSurface = compositor.CreateVisualSurface());
		if (content.Parent != containerVisual)
		{
			containerVisual.Children.InsertAtTop(content);
		}
		content.Scale = new Vector3(num, num, 1f);
		content.Offset = new Vector3(-mask.PixelOrigin, 0f);
		compositionVisualSurface.SourceVisual = containerVisual;
		compositionVisualSurface.SourceSize = mask.PixelSize;
		mask.Source.Surface = compositionVisualSurface;
		mask.Ink?.Dispose();
		mask.Ink = null;
		mask.IsInkStale = false;
	}

	private void ShowLiveIfInked(Node piece)
	{
		Node parent = piece.Parent;
		if (parent != null && parent.Kind == NodeKind.MaskedContainer)
		{
			PenMask mask = parent.Mask;
			if (mask != null && mask.IsApplied && (object)mask.Ink != null)
			{
				ShowLiveContent(parent, mask);
			}
		}
	}

	private void DrawStaleInk()
	{
		foreach (List<Node> value in containersByMask.Values)
		{
			foreach (Node item in value)
			{
				PenMask mask = item.Mask;
				if (mask != null && mask.IsApplied && mask.IsInkStale)
				{
					DrawInk(item, mask);
				}
			}
		}
	}

	private void RemovePenMask(Node node)
	{
		PenMask mask = node.Mask;
		if (mask == null || !mask.IsApplied)
		{
			return;
		}
		ContainerVisual content = node.Content;
		mask.MaskSurface.SourceVisual = null;
		CompositionVisualSurface contentSurface = mask.ContentSurface;
		if ((object)contentSurface != null)
		{
			contentSurface.SourceVisual = null;
			if (content.Parent == mask.ContentHolder)
			{
				mask.ContentHolder.Children.Remove(content);
			}
		}
		mask.Source.Surface = null;
		mask.Ink?.Dispose();
		mask.Ink = null;
		content.Scale = Vector3.One;
		content.Offset = Vector3.Zero;
		ReplaceVisual(node, content);
		mask.IsApplied = false;
	}

	private void DisposeMask(Node node)
	{
		PenMask mask = node.Mask;
		if (mask == null)
		{
			return;
		}
		try
		{
			mask.MaskSurface.SourceVisual = null;
			CompositionVisualSurface contentSurface = mask.ContentSurface;
			if ((object)contentSurface != null)
			{
				contentSurface.SourceVisual = null;
			}
			mask.ContentHolder?.Children.RemoveAll();
			mask.Sprite.Dispose();
			mask.Brush.Dispose();
			mask.Source.Dispose();
			mask.Ink?.Dispose();
			mask.ContentSurface?.Dispose();
			mask.ContentHolder?.Dispose();
			mask.MaskSurface.Dispose();
			mask.MaskVisual.Dispose();
			mask.MaskHolder.Dispose();
		}
		catch
		{
		}
		node.Mask = null;
	}

	private void ScheduleMaskRemoval(Node node, double time)
	{
		if (!RemovesFinishedPenMasks)
		{
			return;
		}
		int generationAtStart = generation;
		thread.ScheduleTimer(time, delegate
		{
			if (generation == generationAtStart)
			{
				PenMask mask = node.Mask;
				if (mask != null && mask.IsApplied)
				{
					if (mask.EditRunning && Now < mask.EditEndTime + 0.05)
					{
						ScheduleMaskRemoval(node, mask.EditEndTime + 0.05);
					}
					else if (!(mask.ModelTrimEnd < 0.999f) && !(Now < mask.PenEndTime))
					{
						RemovePenMask(node);
					}
				}
			}
		});
	}

	private void ForgetMaskedContainer(Node container)
	{
		string maskId = container.MaskId;
		if (maskId != null && containersByMask.TryGetValue(maskId, out List<Node> value))
		{
			value.Remove(container);
			if (value.Count == 0)
			{
				containersByMask.Remove(maskId);
			}
		}
	}

	private void ForgetNodes(Node inside)
	{
		foreach (int item in (from pair in unitNodes
			where pair.Value != inside && IsInside(pair.Value)
			select pair.Key).ToList())
		{
			unitNodes.Remove(item);
		}
		foreach (var (key, list2) in containersByMask.ToList())
		{
			list2.RemoveAll(delegate(Node container)
			{
				bool num = IsInside(container);
				if (num)
				{
					DisposeMask(container);
				}
				return num;
			});
			if (list2.Count == 0)
			{
				containersByMask.Remove(key);
			}
		}
		bool IsInside(Node node)
		{
			for (Node node2 = node; node2 != null; node2 = node2.Parent)
			{
				if (node2 == inside)
				{
					return true;
				}
			}
			return false;
		}
	}

	private Node MakeWrapper()
	{
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Comment = "wrapper";
		return new Node
		{
			Kind = NodeKind.Wrapper,
			Visual = containerVisual,
			Content = containerVisual,
			Children = new List<Node>()
		};
	}

	private UnitResult BuildGroupUnit(JsonElement node, int unit, JsonElement attributes, Node groupNode, int? beforeUnit)
	{
		Node node2 = MakeWrapper();
		node2.Unit = unit;
		SetWrapperTransform(node2, SvgValues.Transform(Attr(attributes, "transform")));
		Insert(node2, groupNode, beforeUnit);
		unitNodes[unit] = node2;
		UnitResult unitResult = default(UnitResult);
		unitResult.PenStrokes = new List<PenStroke>();
		UnitResult result = unitResult;
		if (node.TryGetProperty("ch", out var value) && value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				UnitResult unitResult2 = BuildUnit(item, "g" + unit, node2, null);
				result.PenStrokes.AddRange(unitResult2.PenStrokes);
				ref PenStroke fallbackVisit = ref result.FallbackVisit;
				if (fallbackVisit == null)
				{
					fallbackVisit = unitResult2.FallbackVisit;
				}
				(double, double)? window = unitResult2.Window;
				if (window.HasValue)
				{
					(double, double) valueOrDefault = window.GetValueOrDefault();
					result.Include(valueOrDefault.Item1, valueOrDefault.Item2);
				}
			}
		}
		return result;
	}

	private static void SetWrapperTransform(Node wrapper, Matrix3x2? transform)
	{
		Visual visual = wrapper.Visual;
		Matrix3x2 m = transform ?? Matrix3x2.Identity;
		if (m.M12 == 0f && m.M21 == 0f && Math.Abs(m.M11 - m.M22) < 1E-06f)
		{
			visual.TransformMatrix = Matrix4x4.Identity;
			wrapper.WrapperScale = new Vector3(m.M11, m.M11, 1f);
			wrapper.WrapperOffset = new Vector3(m.M31, m.M32, 0f);
			visual.Scale = wrapper.WrapperScale;
			visual.Offset = wrapper.WrapperOffset;
		}
		else
		{
			visual.Scale = Vector3.One;
			visual.Offset = Vector3.Zero;
			wrapper.WrapperScale = Vector3.One;
			wrapper.WrapperOffset = Vector3.Zero;
			visual.TransformMatrix = SvgValues.To4x4(m);
		}
	}

	private void ClearSnapshotRecords()
	{
		animationRecords.Clear();
		spriteContents.Clear();
		maskedSprites.Clear();
	}

	private void Record(CompositionObject target, string property, Vector4 from, Vector4 to, AnimationSpec spec, bool fillBackwards, Func<double, double>? curve = null)
	{
		if (!animationRecords.TryGetValue(target, out List<AnimationRecord> value))
		{
			value = (animationRecords[target] = new List<AnimationRecord>());
		}
		value.RemoveAll((AnimationRecord r) => r.Property == property && r.BeginTime + r.Duration <= spec.BeginTime && !r.FillBackwards);
		value.Add(new AnimationRecord(property, from, to, spec.BeginTime, spec.Duration, spec.Timing, fillBackwards, curve));
	}

	private void ForgetRecords(CompositionObject target, string property)
	{
		if (animationRecords.TryGetValue(target, out List<AnimationRecord> value))
		{
			value.RemoveAll((AnimationRecord r) => r.Property == property);
		}
	}

	private Vector4 ValueAt(CompositionObject target, string property, Vector4 current, double time)
	{
		if (!animationRecords.TryGetValue(target, out List<AnimationRecord> value))
		{
			return current;
		}
		Vector4 result = current;
		bool flag = false;
		foreach (AnimationRecord item in value)
		{
			if (item.Property != property)
			{
				continue;
			}
			if (time < item.BeginTime)
			{
				if (item.FillBackwards && !flag)
				{
					result = item.From;
					flag = true;
				}
			}
			else
			{
				flag = true;
				double num = ((item.Duration <= 0.0) ? 1.0 : Math.Clamp((time - item.BeginTime) / item.Duration, 0.0, 1.0));
				float amount = (float)(item.Curve?.Invoke(num) ?? CubicBezier(num, item.Timing));
				result = Vector4.Lerp(item.From, item.To, amount);
			}
		}
		return result;
	}

	private float Scalar(CompositionObject target, string property, float current, double time)
	{
		return ValueAt(target, property, new Vector4(current, 0f, 0f, 0f), time).X;
	}

	private Vector2 Vector2At(CompositionObject target, string property, Vector2 current, double time)
	{
		Vector4 vector = ValueAt(target, property, new Vector4(current, 0f, 0f), time);
		return new Vector2(vector.X, vector.Y);
	}

	private Vector3 Vector3At(CompositionObject target, string property, Vector3 current, double time)
	{
		Vector4 vector = ValueAt(target, property, new Vector4(current, 0f), time);
		return new Vector3(vector.X, vector.Y, vector.Z);
	}

	private Color ColorAt(CompositionColorBrush brush, double time)
	{
		Color color = brush.Color;
		Vector4 vector = ValueAt(brush, "Color", new Vector4((int)color.A, (int)color.R, (int)color.G, (int)color.B), time);
		return Color.FromArgb(B(vector.X), B(vector.Y), B(vector.Z), B(vector.W));
		static byte B(float x)
		{
			return (byte)Math.Clamp(MathF.Round(x), 0f, 255f);
		}
	}

	private static double CubicBezier(double x, Vector4 c)
	{
		if (c == new Vector4(0f, 0f, 1f, 1f))
		{
			return x;
		}
		double num = x;
		for (int i = 0; i < 8; i++)
		{
			double num2 = 3.0 * (1.0 - num) * (1.0 - num) * num * (double)c.X + 3.0 * (1.0 - num) * num * num * (double)c.Z + num * num * num - x;
			double num3 = 3.0 * (1.0 - num) * (1.0 - num) * (double)c.X + 6.0 * (1.0 - num) * num * (double)(c.Z - c.X) + 3.0 * num * num * (double)(1f - c.Z);
			if (Math.Abs(num2) < 1E-06 || Math.Abs(num3) < 1E-06)
			{
				break;
			}
			num = Math.Clamp(num - num2 / num3, 0.0, 1.0);
		}
		return 3.0 * (1.0 - num) * (1.0 - num) * num * (double)c.Y + 3.0 * (1.0 - num) * num * num * (double)c.W + num * num * num;
	}

	public CanvasRenderTarget RenderSnapshot(double time, float pixelsPerUnit, CanvasBitmap? background = null)
	{
		Vector2 vector = canvasSize * pixelsPerUnit;
		CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(canvasDevice, Math.Max(1f, vector.X), Math.Max(1f, vector.Y), 96f);
		using CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession();
		canvasDrawingSession.Clear(Colors.Transparent);
		if ((object)background != null)
		{
			canvasDrawingSession.DrawImage(background, new Rect(0f, 0f, vector.X, vector.Y), background.Bounds);
		}
		if (Scalar(StageVisual, "Opacity", StageVisual.Opacity, time) <= 0.001f)
		{
			return canvasRenderTarget;
		}
		Matrix3x2 matrix3x = Matrix3x2.CreateScale(pixelsPerUnit);
		SpriteVisual spriteVisual = fullScreenBackdrop;
		if ((object)spriteVisual != null)
		{
			canvasDrawingSession.Transform = Matrix3x2.Identity;
			float num = Scalar(spriteVisual, "Opacity", spriteVisual.Opacity, time);
			canvasDrawingSession.FillRectangle(0f, 0f, vector.X, vector.Y, Color.FromArgb((byte)(107.1 * (double)num), 0, 0, 0));
		}
		string[] array = new string[4] { "under", "main", "hl", "words" };
		foreach (string key in array)
		{
			foreach (Node child in groups[key].Children)
			{
				DrawNode(canvasDrawingSession, child, matrix3x, time);
			}
		}
		foreach (var (bytes, widthInPixels, heightInPixels, tuple2, node) in Figures?.Snapshots(time) ?? Enumerable.Empty<(byte[], int, int, (double, double, double, double), Node)>())
		{
			if (node.Visual.IsVisible || !(node.Visual.Opacity <= 0f))
			{
				using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromBytes(canvasDevice, bytes, widthInPixels, heightInPixels, DirectXPixelFormat.B8G8R8A8UIntNormalized);
				canvasDrawingSession.Transform = matrix3x * ParentTransform(node, time);
				canvasDrawingSession.DrawImage(canvasBitmap, new Rect(tuple2.Item1, tuple2.Item2, tuple2.Item3, tuple2.Item4), canvasBitmap.Bounds, Scalar(node.Visual, "Opacity", node.Visual.Opacity, time));
			}
		}
		return canvasRenderTarget;
	}

	private Matrix3x2 ParentTransform(Node node, double time)
	{
		Matrix3x2 identity = Matrix3x2.Identity;
		for (Node parent = node.Parent; parent != null; parent = parent.Parent)
		{
			if (parent.Kind == NodeKind.Wrapper)
			{
				identity *= VisualTransform(parent.Visual, time);
			}
		}
		return identity;
	}

	private void DrawNode(CanvasDrawingSession session, Node node, Matrix3x2 parent, double time)
	{
		if (node.Kind == NodeKind.MaskedContainer)
		{
			PenMask mask = node.Mask;
			if (mask != null && mask.IsApplied)
			{
				DrawMaskedContainer(session, node, mask, parent, time);
				return;
			}
		}
		if (node.Kind == NodeKind.MaskedContainer)
		{
			foreach (Node child in node.Children)
			{
				DrawNode(session, child, parent, time);
			}
			return;
		}
		DrawVisual(session, node.Visual, parent, time, Vector2.Zero, node);
	}

	private Matrix3x2 VisualTransform(Visual visual, double time)
	{
		Vector3 vector = Vector3At(visual, "Offset", visual.Offset, time);
		Vector3 vector2 = Vector3At(visual, "Scale", visual.Scale, time);
		float num = Scalar(visual, "RotationAngleInDegrees", visual.RotationAngleInDegrees, time);
		Vector2 vector3 = new Vector2(visual.CenterPoint.X, visual.CenterPoint.Y);
		Matrix4x4 transformMatrix = visual.TransformMatrix;
		Matrix3x2 matrix3x = new Matrix3x2(transformMatrix.M11, transformMatrix.M12, transformMatrix.M21, transformMatrix.M22, transformMatrix.M41, transformMatrix.M42);
		return Matrix3x2.CreateTranslation(-vector3) * Matrix3x2.CreateScale(vector2.X, vector2.Y) * Matrix3x2.CreateRotation(num * (float)Math.PI / 180f) * Matrix3x2.CreateTranslation(vector3) * matrix3x * Matrix3x2.CreateTranslation(vector.X, vector.Y);
	}

	private void DrawVisual(CanvasDrawingSession session, Visual visual, Matrix3x2 parent, double time, Vector2 parentSize, Node? node = null)
	{
		if (!visual.IsVisible)
		{
			return;
		}
		float num = Scalar(visual, "Opacity", visual.Opacity, time);
		if (num <= 0.001f)
		{
			return;
		}
		Matrix3x2 transform = VisualTransform(visual, time) * parent;
		Vector2 size = visual.Size + visual.RelativeSizeAdjustment * parentSize;
		CanvasGeometry canvasGeometry = null;
		if (visual.Clip is CompositionGeometricClip { Geometry: CompositionRoundedRectangleGeometry geometry })
		{
			Vector2 vector = Vector2At(geometry, "Offset", geometry.Offset, time);
			Vector2 vector2 = Vector2At(geometry, "Size", geometry.Size, time);
			using CanvasGeometry canvasGeometry2 = CanvasGeometry.CreateRoundedRectangle(canvasDevice, vector.X, vector.Y, vector2.X, vector2.Y, geometry.CornerRadius.X, geometry.CornerRadius.Y);
			canvasGeometry = canvasGeometry2.Transform(transform);
		}
		if (num < 0.999f || (object)canvasGeometry != null)
		{
			using (CanvasCommandList canvasCommandList = new CanvasCommandList(canvasDevice))
			{
				using (CanvasDrawingSession session2 = canvasCommandList.CreateDrawingSession())
				{
					DrawVisualContent(session2, visual, transform, time, size);
				}
				ICanvasImage source = canvasCommandList;
				CanvasCommandList canvasCommandList2 = null;
				AlphaMaskEffect alphaMaskEffect = null;
				if ((object)canvasGeometry != null)
				{
					canvasCommandList2 = new CanvasCommandList(canvasDevice);
					using (CanvasDrawingSession canvasDrawingSession = canvasCommandList2.CreateDrawingSession())
					{
						canvasDrawingSession.FillGeometry(canvasGeometry, Colors.White);
					}
					alphaMaskEffect = new AlphaMaskEffect
					{
						Source = canvasCommandList,
						AlphaMask = canvasCommandList2
					};
					source = alphaMaskEffect;
				}
				using OpacityEffect image = new OpacityEffect
				{
					Source = source,
					Opacity = num
				};
				session.Transform = Matrix3x2.Identity;
				session.DrawImage(image);
				alphaMaskEffect?.Dispose();
				canvasCommandList2?.Dispose();
				canvasGeometry?.Dispose();
				return;
			}
		}
		DrawVisualContent(session, visual, transform, time, size);
	}

	private void DrawVisualContent(CanvasDrawingSession session, Visual visual, Matrix3x2 transform, double time, Vector2 size)
	{
		session.Transform = transform;
		if (!(visual is SpriteVisual spriteVisual))
		{
			if (visual is ShapeVisual shapeVisual)
			{
				{
					foreach (CompositionShape shape in shapeVisual.Shapes)
					{
						DrawShape(session, shape, transform, time);
					}
					return;
				}
			}
			if (!(visual is ContainerVisual containerVisual))
			{
				return;
			}
			{
				foreach (Visual child in containerVisual.Children)
				{
					DrawVisual(session, child, transform, time, size);
				}
				return;
			}
		}
		SpriteVisual spriteVisual2 = spriteVisual;
		if (CompositionShadows.TryGetCaster(spriteVisual2, out CompositionShadows.Caster caster))
		{
			DrawCastShadow(session, spriteVisual2, caster, transform, time);
			return;
		}
		SpriteVisual sprite = spriteVisual;
		DrawSprite(session, sprite, transform, time, size);
	}

	private void DrawSprite(CanvasDrawingSession session, SpriteVisual sprite, Matrix3x2 transform, double time, Vector2 size)
	{
		session.Transform = transform;
		if (maskedSprites.TryGetValue(sprite, out Node value))
		{
			PenMask mask = value.Mask;
			if (mask != null)
			{
				_ = Matrix3x2.CreateScale(sprite.Scale.X, sprite.Scale.Y) * Matrix3x2.CreateTranslation(sprite.Offset.X, sprite.Offset.Y);
				Matrix3x2.Invert(VisualTransform(sprite, time), out var result);
				DrawMaskedContainer(session, value, mask, result * transform, time);
				return;
			}
		}
		if (spriteContents.TryGetValue(sprite, out Action<CanvasDrawingSession, Vector2> value2))
		{
			value2(session, size);
		}
		else if (sprite.Brush is CompositionColorBrush brush)
		{
			session.FillRectangle(0f, 0f, size.X, size.Y, ColorAt(brush, time));
		}
	}

	private void DrawMaskedContainer(CanvasDrawingSession session, Node node, PenMask mask, Matrix3x2 canvasTransform, double time)
	{
		float num = Scalar(mask.Geometry, "TrimEnd", mask.ModelTrimEnd, time);
		if (num <= 0.0005f)
		{
			return;
		}
		using CanvasCommandList canvasCommandList = new CanvasCommandList(canvasDevice);
		using (CanvasDrawingSession session2 = canvasCommandList.CreateDrawingSession())
		{
			foreach (Node child in node.Children)
			{
				DrawNode(session2, child, canvasTransform, time);
			}
		}
		if (num >= 0.9995f)
		{
			session.Transform = Matrix3x2.Identity;
			session.DrawImage(canvasCommandList);
			return;
		}
		using CanvasCommandList canvasCommandList2 = new CanvasCommandList(canvasDevice);
		using (CanvasDrawingSession canvasDrawingSession = canvasCommandList2.CreateDrawingSession())
		{
			canvasDrawingSession.Transform = canvasTransform;
			MaskDefinition maskDefinition = maskDefinitions[node.MaskId];
			using CanvasGeometry canvasGeometry = TrimmedGeometry(maskDefinition.Geometry, num);
			using CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle
			{
				StartCap = CanvasCapStyle.Round,
				EndCap = CanvasCapStyle.Round,
				LineJoin = CanvasLineJoin.Round
			};
			if ((object)canvasGeometry != null)
			{
				canvasDrawingSession.DrawGeometry(canvasGeometry, Colors.White, MaskLineWidth(maskDefinition, node.GlowRadius), strokeStyle);
			}
		}
		session.Transform = Matrix3x2.Identity;
		using AlphaMaskEffect image = new AlphaMaskEffect
		{
			Source = canvasCommandList,
			AlphaMask = canvasCommandList2
		};
		session.DrawImage(image);
	}

	private CanvasGeometry? TrimmedGeometry(CanvasGeometry geometry, float fraction)
	{
		float num = geometry.ComputePathLength();
		if (num <= 0f)
		{
			return null;
		}
		float num2 = num * fraction;
		int num3 = Math.Clamp((int)(num2 / 1.5f), 2, 2000);
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(canvasDevice);
		canvasPathBuilder.BeginFigure(geometry.ComputePointOnPath(0f));
		for (int i = 1; i <= num3; i++)
		{
			canvasPathBuilder.AddLine(geometry.ComputePointOnPath(num2 * (float)i / (float)num3));
		}
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	private void DrawCastShadow(CanvasDrawingSession session, SpriteVisual caster, CompositionShadows.Caster cast, Matrix3x2 transform, double time)
	{
		Matrix3x2 transform2 = Matrix3x2.CreateTranslation(-cast.SourceOffset) * transform;
		using CanvasCommandList canvasCommandList = new CanvasCommandList(canvasDevice);
		using (CanvasDrawingSession canvasDrawingSession = canvasCommandList.CreateDrawingSession())
		{
			canvasDrawingSession.Transform = transform2;
			DrawVisualContent(canvasDrawingSession, cast.Source, transform2, time, cast.Source.Size);
		}
		float num = new Vector2(transform.M11, transform.M12).Length();
		Vector2 offset = Vector2.TransformNormal(new Vector2(cast.Shadow.Offset.X, cast.Shadow.Offset.Y), transform);
		session.Transform = Matrix3x2.Identity;
		using ShadowEffect image = new ShadowEffect
		{
			Source = canvasCommandList,
			BlurAmount = Math.Max(0.01f, cast.Shadow.BlurRadius * num / 2f),
			ShadowColor = cast.Shadow.Color
		};
		session.DrawImage(image, offset);
	}

	private Matrix3x2 ShapeTransform(CompositionShape shape, double time)
	{
		Vector2 position = Vector2At(shape, "Offset", shape.Offset, time);
		Vector2 scales = Vector2At(shape, "Scale", shape.Scale, time);
		return Matrix3x2.CreateTranslation(-shape.CenterPoint) * Matrix3x2.CreateScale(scales) * Matrix3x2.CreateRotation(shape.RotationAngleInDegrees * (float)Math.PI / 180f) * Matrix3x2.CreateTranslation(shape.CenterPoint) * shape.TransformMatrix * Matrix3x2.CreateTranslation(position);
	}

	private void DrawShape(CanvasDrawingSession session, CompositionShape shape, Matrix3x2 parent, double time)
	{
		Matrix3x2 matrix3x = ShapeTransform(shape, time) * parent;
		if (shape is CompositionContainerShape compositionContainerShape)
		{
			{
				foreach (CompositionShape shape2 in compositionContainerShape.Shapes)
				{
					DrawShape(session, shape2, matrix3x, time);
				}
				return;
			}
		}
		CompositionSpriteShape sprite = shape as CompositionSpriteShape;
		if ((object)sprite == null)
		{
			return;
		}
		CompositionGeometry geometry = sprite.Geometry;
		if ((object)geometry == null)
		{
			return;
		}
		CompositionGeometry compositionGeometry = geometry;
		CanvasGeometry canvasGeometry;
		if (compositionGeometry is CompositionPathGeometry { Path: var path })
		{
			if ((object)path != null)
			{
				CompositionPath key = path;
				if (pathSources.TryGetValue(key, out CanvasGeometry value))
				{
					canvasGeometry = value;
					goto IL_019e;
				}
			}
			goto IL_019b;
		}
		if (!(compositionGeometry is CompositionEllipseGeometry compositionEllipseGeometry))
		{
			if (!(compositionGeometry is CompositionRectangleGeometry compositionRectangleGeometry))
			{
				if (!(compositionGeometry is CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry))
				{
					if (!(compositionGeometry is CompositionLineGeometry compositionLineGeometry))
					{
						goto IL_019b;
					}
					CompositionLineGeometry line = compositionLineGeometry;
					canvasGeometry = LineAt(line, time);
				}
				else
				{
					CompositionRoundedRectangleGeometry rounded = compositionRoundedRectangleGeometry;
					canvasGeometry = RoundedAt(rounded, time);
				}
			}
			else
			{
				CompositionRectangleGeometry compositionRectangleGeometry2 = compositionRectangleGeometry;
				canvasGeometry = CanvasGeometry.CreateRectangle(canvasDevice, compositionRectangleGeometry2.Offset.X, compositionRectangleGeometry2.Offset.Y, compositionRectangleGeometry2.Size.X, compositionRectangleGeometry2.Size.Y);
			}
		}
		else
		{
			CompositionEllipseGeometry compositionEllipseGeometry2 = compositionEllipseGeometry;
			canvasGeometry = CanvasGeometry.CreateEllipse(canvasDevice, compositionEllipseGeometry2.Center, compositionEllipseGeometry2.Radius.X, compositionEllipseGeometry2.Radius.Y);
		}
		goto IL_019e;
		IL_019b:
		canvasGeometry = null;
		goto IL_019e;
		IL_019e:
		CanvasGeometry canvasGeometry2 = canvasGeometry;
		if ((object)canvasGeometry2 == null)
		{
			return;
		}
		session.Transform = matrix3x;
		float num = Scalar(geometry, "TrimEnd", geometry.TrimEnd, time);
		CompositionBrush fillBrush = sprite.FillBrush;
		if ((object)fillBrush != null)
		{
			ICanvasBrush canvasBrush = BrushFor(session, fillBrush, canvasGeometry2, time);
			if (canvasBrush != null)
			{
				session.FillGeometry(canvasGeometry2, canvasBrush);
				canvasBrush.Dispose();
			}
		}
		CompositionBrush strokeBrush = sprite.StrokeBrush;
		if ((object)strokeBrush == null || !(sprite.StrokeThickness > 0f))
		{
			return;
		}
		ICanvasBrush canvasBrush2 = BrushFor(session, strokeBrush, canvasGeometry2, time);
		if (canvasBrush2 == null)
		{
			return;
		}
		CanvasStrokeStyle canvasStrokeStyle = new CanvasStrokeStyle();
		canvasStrokeStyle.StartCap = Cap(sprite.StrokeStartCap);
		canvasStrokeStyle.EndCap = Cap(sprite.StrokeEndCap);
		CanvasStrokeStyle canvasStrokeStyle2 = canvasStrokeStyle;
		canvasStrokeStyle2.LineJoin = sprite.StrokeLineJoin switch
		{
			CompositionStrokeLineJoin.Round => CanvasLineJoin.Round, 
			CompositionStrokeLineJoin.Bevel => CanvasLineJoin.Bevel, 
			_ => CanvasLineJoin.Miter, 
		};
		canvasStrokeStyle.MiterLimit = sprite.StrokeMiterLimit;
		using CanvasStrokeStyle canvasStrokeStyle3 = canvasStrokeStyle;
		if (sprite.StrokeDashArray.Count > 0)
		{
			canvasStrokeStyle3.CustomDashStyle = sprite.StrokeDashArray.Select((float dash) => dash / Math.Max(sprite.StrokeThickness, 0.001f)).ToArray();
			canvasStrokeStyle3.DashCap = Cap(sprite.StrokeDashCap);
		}
		CanvasGeometry canvasGeometry3 = ((num < 0.9995f) ? TrimmedGeometry(canvasGeometry2, num) : canvasGeometry2);
		if ((object)canvasGeometry3 != null)
		{
			session.DrawGeometry(canvasGeometry3, canvasBrush2, sprite.StrokeThickness, canvasStrokeStyle3);
		}
		if ((object)canvasGeometry3 != canvasGeometry2)
		{
			canvasGeometry3?.Dispose();
		}
		canvasBrush2.Dispose();
	}

	private CanvasGeometry RoundedAt(CompositionRoundedRectangleGeometry rounded, double time)
	{
		Vector2 vector = Vector2At(rounded, "Offset", rounded.Offset, time);
		Vector2 vector2 = Vector2At(rounded, "Size", rounded.Size, time);
		Vector2 vector3 = Vector2At(rounded, "CornerRadius", rounded.CornerRadius, time);
		return CanvasGeometry.CreateRoundedRectangle(canvasDevice, vector.X, vector.Y, vector2.X, vector2.Y, vector3.X, vector3.Y);
	}

	private CanvasGeometry LineAt(CompositionLineGeometry line, double time)
	{
		Vector2 startPoint = Vector2At(line, "Start", line.Start, time);
		Vector2 endPoint = Vector2At(line, "End", line.End, time);
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(canvasDevice);
		canvasPathBuilder.BeginFigure(startPoint);
		canvasPathBuilder.AddLine(endPoint);
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	private static CanvasCapStyle Cap(CompositionStrokeCap cap)
	{
		return cap switch
		{
			CompositionStrokeCap.Round => CanvasCapStyle.Round, 
			CompositionStrokeCap.Square => CanvasCapStyle.Square, 
			_ => CanvasCapStyle.Flat, 
		};
	}

	private ICanvasBrush? BrushFor(CanvasDrawingSession session, CompositionBrush brush, CanvasGeometry geometry, double time)
	{
		if (!(brush is CompositionColorBrush brush2))
		{
			if (brush is CompositionLinearGradientBrush compositionLinearGradientBrush)
			{
				Rect rect = geometry.ComputeBounds();
				CanvasGradientStop[] gradientStops = compositionLinearGradientBrush.ColorStops.Select(delegate(CompositionColorGradientStop s)
				{
					CanvasGradientStop result = default(CanvasGradientStop);
					result.Position = s.Offset;
					result.Color = s.Color;
					return result;
				}).ToArray();
				return new CanvasLinearGradientBrush(session, gradientStops)
				{
					StartPoint = new Vector2((float)(rect.X + (double)compositionLinearGradientBrush.StartPoint.X * rect.Width), (float)(rect.Y + (double)compositionLinearGradientBrush.StartPoint.Y * rect.Height)),
					EndPoint = new Vector2((float)(rect.X + (double)compositionLinearGradientBrush.EndPoint.X * rect.Width), (float)(rect.Y + (double)compositionLinearGradientBrush.EndPoint.Y * rect.Height))
				};
			}
			return null;
		}
		return new CanvasSolidColorBrush(session, ColorAt(brush2, time));
	}

	public void DrawForeignVisual(CanvasDrawingSession session, Visual visual, Matrix3x2 transform, Vector2 size, double time)
	{
		DrawVisual(session, visual, transform, time, size);
	}

	public string DumpTree(double time)
	{
		StringBuilder text = new StringBuilder();
		string[] array = new string[4] { "under", "main", "hl", "words" };
		foreach (string text2 in array)
		{
			text.AppendLine(text2);
			foreach (Node child in groups[text2].Children)
			{
				Visit(child, 1);
			}
		}
		return text.ToString();
		void Visit(Node node, int depth)
		{
			Visual visual = node.Visual;
			StringBuilder stringBuilder = text.Append(' ', depth * 2);
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(26, 9, stringBuilder);
			handler.AppendFormatted(node.Kind);
			handler.AppendLiteral(" unit=");
			handler.AppendFormatted(node.Unit);
			handler.AppendLiteral(" ");
			handler.AppendFormatted(visual.GetType().Name);
			handler.AppendLiteral(" op=");
			handler.AppendFormatted(Scalar(visual, "Opacity", visual.Opacity, time), "F2");
			handler.AppendLiteral(" vis=");
			handler.AppendFormatted(visual.IsVisible);
			handler.AppendLiteral(" ext=(");
			handler.AppendFormatted(node.Extent.X, "F0");
			handler.AppendLiteral(",");
			handler.AppendFormatted(node.Extent.Y, "F0");
			handler.AppendLiteral(",");
			handler.AppendFormatted(node.Extent.Width, "F0");
			handler.AppendLiteral(",");
			handler.AppendFormatted(node.Extent.Height, "F0");
			handler.AppendLiteral(")");
			stringBuilder2.Append(ref handler);
			if (visual is SpriteVisual spriteVisual)
			{
				stringBuilder = text;
				StringBuilder stringBuilder3 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(16, 2, stringBuilder);
				handler.AppendLiteral(" brush=");
				handler.AppendFormatted(spriteVisual.Brush?.GetType().Name);
				handler.AppendLiteral(" content=");
				handler.AppendFormatted(spriteContents.TryGetValue(spriteVisual, out Action<CanvasDrawingSession, Vector2> _));
				stringBuilder3.Append(ref handler);
			}
			if (visual is ShapeVisual shapeVisual)
			{
				stringBuilder = text;
				StringBuilder stringBuilder4 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder);
				handler.AppendLiteral(" shapes=");
				handler.AppendFormatted(shapeVisual.Shapes.Count);
				stringBuilder4.Append(ref handler);
			}
			if (node.Kind == NodeKind.Wrapper)
			{
				Matrix3x2 matrix3x = VisualTransform(visual, time);
				stringBuilder = text;
				StringBuilder stringBuilder5 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(24, 5, stringBuilder);
				handler.AppendLiteral(" offset=");
				handler.AppendFormatted(visual.Offset);
				handler.AppendLiteral(" scale=");
				handler.AppendFormatted(visual.Scale);
				handler.AppendLiteral(" now=[");
				handler.AppendFormatted(matrix3x.M11, "F2");
				handler.AppendLiteral(" ");
				handler.AppendFormatted(matrix3x.M31, "F1");
				handler.AppendLiteral(" ");
				handler.AppendFormatted(matrix3x.M32, "F1");
				handler.AppendLiteral("]");
				stringBuilder5.Append(ref handler);
			}
			if (visual is ContainerVisual containerVisual && !(visual is ShapeVisual))
			{
				stringBuilder = text;
				StringBuilder stringBuilder6 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder);
				handler.AppendLiteral(" children=");
				handler.AppendFormatted(containerVisual.Children.Count);
				stringBuilder6.Append(ref handler);
			}
			text.AppendLine();
			List<Node> children = node.Children;
			if (children != null)
			{
				foreach (Node item in children)
				{
					Visit(item, depth + 1);
				}
			}
		}
	}

	public (double X, double Y, double Width, double Height) TextBox(JsonElement description)
	{
		return TextLayout.Make(textShaper, description).Box;
	}

	public double TextWidth(string family, double size, int weight, string text)
	{
		return textShaper.LayoutWidth(text, textShaper.GetFont(family, size, weight));
	}

	private (Node Node, List<PenStroke> PenStrokes) MakeTextNode(JsonElement description, JsonElement attributes, AnimationSpec? animation)
	{
		TextLayout textLayout = TextLayout.Make(textShaper, description);
		string text = Attr(attributes, "fill") ?? "#000000";
		Color fillColor = CssColor.Paint(text, Attr(attributes, "fill-opacity")) ?? Colors.Black;
		TextGlow? textGlow = TextGlow.ForFill(text);
		Matrix3x2? transform = SvgValues.Transform(Attr(attributes, "transform"));
		string text2 = Attr(attributes, "data-knockout");
		Color? knockoutColor = ((text2 != null) ? CssColor.Paint(text2, null) : ((Color?)null));
		double knockoutWidth = ((!knockoutColor.HasValue) ? 0.0 : Math.Max(0.0, SvgValues.Number(Attr(attributes, "data-knockout-width"))));
		double devicePixelsPerUnit = DevicePixelsPerUnit;
		double num = Math.Min(0.0128 * textLayout.Size * devicePixelsPerUnit, 0.7);
		double num2 = ((num > 0.01) ? (num / devicePixelsPerUnit) : 0.0);
		float inkStrokeWidth = (float)(num2 + textLayout.SyntheticBoldStrokeWidth);
		int num3;
		if (transform.HasValue)
		{
			Matrix3x2 valueOrDefault = transform.GetValueOrDefault();
			num3 = ((valueOrDefault.M11 == 1f && valueOrDefault.M12 == 0f && valueOrDefault.M21 == 0f && valueOrDefault.M22 == 1f) ? 1 : 0);
		}
		else
		{
			num3 = 1;
		}
		bool flag = (byte)num3 != 0;
		bool snapsToPixels = flag && devicePixelsPerUnit > 0.0;
		double glyphStepPixels = ((textLayout.Size * devicePixelsPerUnit < 33.5) ? 0.5 : 1.0);
		Vector2 vector;
		if (transform.HasValue)
		{
			Matrix3x2 valueOrDefault2 = transform.GetValueOrDefault();
			vector = new Vector2(valueOrDefault2.M31, valueOrDefault2.M32);
		}
		else
		{
			vector = Vector2.Zero;
		}
		Vector2 translation = vector;
		Vector2[] array = new Vector2[textLayout.Lines.Count];
		for (int i = 0; i < textLayout.Lines.Count; i++)
		{
			TextLayout.Line line = textLayout.Lines[i];
			double num4 = line.OriginY;
			if (snapsToPixels)
			{
				double num5 = (num4 + (double)translation.Y) * devicePixelsPerUnit;
				num4 += (Math.Ceiling(num5 - 0.001) - num5) / devicePixelsPerUnit;
			}
			array[i] = new Vector2((float)line.OriginX, (float)(num4 - num2 / 2.0));
		}
		ShapeVisual shapeVisual = MakeShapeVisual();
		CompositionContainerShape compositionContainerShape = compositor.CreateContainerShape();
		if (transform.HasValue)
		{
			Matrix3x2 valueOrDefault3 = transform.GetValueOrDefault();
			compositionContainerShape.TransformMatrix = valueOrDefault3;
		}
		shapeVisual.Shapes.Add(compositionContainerShape);
		CompositionContainerShape haloLayer = null;
		if (knockoutColor.HasValue && knockoutWidth > 0.0)
		{
			haloLayer = compositor.CreateContainerShape();
			compositionContainerShape.Shapes.Add(haloLayer);
		}
		CompositionContainerShape inkLayer = compositor.CreateContainerShape();
		compositionContainerShape.Shapes.Add(inkLayer);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		Visual visual = containerVisual;
		containerVisual.Children.InsertAtTop(shapeVisual);
		if (textGlow.HasValue)
		{
			TextGlow valueOrDefault4 = textGlow.GetValueOrDefault();
			ContainerVisual containerVisual2 = compositor.CreateContainerVisual();
			(double, double, double, double) region = CompositionShadows.Bounds(textLayout.Box, transform, textLayout.Size * 0.5 + 2.0);
			containerVisual2.Children.InsertAtTop(CompositionShadows.Cast(compositor, containerVisual, region, valueOrDefault4.WideColor, valueOrDefault4.WideBlur, new Vector2(0f, valueOrDefault4.WideOffsetY)));
			containerVisual2.Children.InsertAtTop(CompositionShadows.Cast(compositor, containerVisual, region, valueOrDefault4.TightColor, valueOrDefault4.TightBlur, Vector2.Zero));
			containerVisual2.Children.InsertAtTop(containerVisual);
			visual = containerVisual2;
		}
		Node node = new Node
		{
			Kind = NodeKind.Text,
			Visual = visual,
			Extent = textLayout.Box
		};
		AnimationSpec write;
		double totalAlong;
		double spread;
		if (animation.HasValue)
		{
			write = animation.GetValueOrDefault();
			if (write.Name == "write")
			{
				List<(int, TextShaper.Glyph, Vector2, Vector2, double)> list = new List<(int, TextShaper.Glyph, Vector2, Vector2, double)>();
				double num6 = 0.0;
				List<(double, double, double, double, double)> list2 = new List<(double, double, double, double, double)>();
				for (int j = 0; j < textLayout.Lines.Count; j++)
				{
					TextLayout.Line line2 = textLayout.Lines[j];
					double item = num6;
					TextShaper.Glyph[] glyphs = line2.Shaped.Glyphs;
					foreach (TextShaper.Glyph glyph2 in glyphs)
					{
						double num7 = line2.OriginX + glyph2.X + glyph2.Advance / 2.0;
						list.Add((j, glyph2, GlyphPlacement(glyph2, array[j]), new Vector2((float)num7, (float)(line2.OriginY - textLayout.Size * 0.32)), num6 + glyph2.Advance / 2.0));
						num6 += glyph2.Advance;
					}
					list2.Add((item, num6, line2.OriginY - textLayout.Size * 0.3, line2.OriginX, line2.OriginX + line2.Shaped.Advance));
					num6 += textLayout.Size * 0.8;
				}
				totalAlong = Math.Max(1.0, num6 - textLayout.Size * 0.8);
				double num8 = Math.Min(0.16, Math.Max(0.07, write.Duration * 0.22));
				spread = Math.Max(0.01, write.Duration - num8);
				float y = (float)(textLayout.Size * 0.06);
				Vector4 timing = new Vector4(0.2f, 0.7f, 0.3f, 1f);
				Color from = Color.FromArgb(0, fillColor.R, fillColor.G, fillColor.B);
				foreach (var item3 in list)
				{
					AnimationSpec spec = new AnimationSpec("write", num8, StartTime(item3.Item5), timing);
					if ((object)item3.Item2.Outline != null)
					{
						(CompositionContainerShape, CompositionContainerShape, CompositionColorBrush, CompositionColorBrush) tuple = AddPiece(item3.Item2, item3.Item3, ownBrushes: true);
						CompositionContainerShape[] array2 = new CompositionContainerShape[2] { tuple.Item1, tuple.Item2 };
						foreach (CompositionContainerShape compositionContainerShape2 in array2)
						{
							if ((object)compositionContainerShape2 != null)
							{
								compositionContainerShape2.CenterPoint = item3.Item4;
								AnimateVector2(compositionContainerShape2, "Scale", new Vector2(0.86f), Vector2.One, spec, fillBackwards: true, node);
								AnimateVector2(compositionContainerShape2, "Offset", new Vector2(0f, y), Vector2.Zero, spec, fillBackwards: true, node);
							}
						}
						AnimateColor(tuple.Item4, from, fillColor, spec, node);
						CompositionColorBrush item2 = tuple.Item3;
						if ((object)item2 != null && knockoutColor.HasValue)
						{
							Color valueOrDefault5 = knockoutColor.GetValueOrDefault();
							AnimateColor(item2, Color.FromArgb(0, valueOrDefault5.R, valueOrDefault5.G, valueOrDefault5.B), valueOrDefault5, spec, node);
						}
						continue;
					}
					TextShaper.ColorGlyph color = item3.Item2.Color;
					if ((object)color != null)
					{
						SpriteVisual spriteVisual = MakeColorGlyphVisual(color, item3.Item3 + new Vector2((float)item3.Item2.X, 0f), transform);
						if ((object)spriteVisual != null)
						{
							containerVisual.Children.InsertAtTop(spriteVisual);
							AnimateScalar(spriteVisual, "Opacity", 0f, 1f, spec, fillBackwards: true, node);
						}
					}
				}
				List<PenStroke> list3 = new List<PenStroke>();
				foreach (var item4 in list2)
				{
					if (item4.Item2 <= item4.Item1)
					{
						continue;
					}
					Vector2[] array3 = new Vector2[2]
					{
						new Vector2((float)item4.Item4, (float)item4.Item3),
						new Vector2((float)item4.Item5, (float)item4.Item3)
					};
					if (transform.HasValue)
					{
						Matrix3x2 valueOrDefault6 = transform.GetValueOrDefault();
						for (int l = 0; l < array3.Length; l++)
						{
							array3[l] = Vector2.Transform(array3[l], valueOrDefault6);
						}
					}
					double num9 = StartTime(item4.Item1);
					double num10 = StartTime(item4.Item2) + num8 * 0.5;
					list3.Add(new PenStroke
					{
						Points = array3,
						BeginTime = num9,
						Duration = Math.Max(0.05, num10 - num9),
						Timing = new Vector4(0f, 0f, 1f, 1f)
					});
				}
				return (Node: node, PenStrokes: list3);
			}
		}
		for (int m = 0; m < textLayout.Lines.Count; m++)
		{
			TextShaper.Glyph[] glyphs = textLayout.Lines[m].Shaped.Glyphs;
			foreach (TextShaper.Glyph glyph3 in glyphs)
			{
				Vector2 vector2 = GlyphPlacement(glyph3, array[m]);
				if ((object)glyph3.Outline != null)
				{
					AddPiece(glyph3, vector2, ownBrushes: false);
					continue;
				}
				TextShaper.ColorGlyph color2 = glyph3.Color;
				if ((object)color2 != null)
				{
					SpriteVisual spriteVisual2 = MakeColorGlyphVisual(color2, vector2 + new Vector2((float)glyph3.X, 0f), transform);
					if ((object)spriteVisual2 != null)
					{
						containerVisual.Children.InsertAtTop(spriteVisual2);
					}
				}
			}
		}
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault7 = animation.GetValueOrDefault();
			if (valueOrDefault7.Name == "fadein")
			{
				AnimateScalar(visual, "Opacity", 0f, 1f, valueOrDefault7, fillBackwards: true, node);
			}
		}
		return (Node: node, PenStrokes: new List<PenStroke>());
		(CompositionContainerShape? Halo, CompositionContainerShape Ink, CompositionColorBrush? HaloBrush, CompositionColorBrush InkBrush) AddPiece(TextShaper.Glyph glyph, Vector2 placement, bool ownBrushes)
		{
			CompositionPathGeometry geometry = compositor.CreatePathGeometry(CompositionPathFor(glyph.Outline));
			CompositionContainerShape compositionContainerShape3 = null;
			CompositionColorBrush compositionColorBrush = null;
			if ((object)haloLayer != null && knockoutColor.HasValue)
			{
				Color valueOrDefault8 = knockoutColor.GetValueOrDefault();
				compositionColorBrush = (ownBrushes ? compositor.CreateColorBrush(valueOrDefault8) : Brush(valueOrDefault8));
				CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(geometry);
				compositionSpriteShape.FillBrush = compositionColorBrush;
				compositionSpriteShape.StrokeBrush = compositionColorBrush;
				compositionSpriteShape.StrokeThickness = (float)knockoutWidth;
				compositionSpriteShape.StrokeLineJoin = CompositionStrokeLineJoin.Round;
				compositionSpriteShape.Offset = placement;
				compositionContainerShape3 = compositor.CreateContainerShape();
				compositionContainerShape3.Shapes.Add(compositionSpriteShape);
				haloLayer.Shapes.Add(compositionContainerShape3);
			}
			CompositionColorBrush compositionColorBrush2 = (ownBrushes ? compositor.CreateColorBrush(fillColor) : Brush(fillColor));
			CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(geometry);
			compositionSpriteShape2.FillBrush = compositionColorBrush2;
			if (inkStrokeWidth > 0f)
			{
				compositionSpriteShape2.StrokeBrush = compositionColorBrush2;
				compositionSpriteShape2.StrokeThickness = inkStrokeWidth;
				compositionSpriteShape2.StrokeLineJoin = CompositionStrokeLineJoin.Round;
			}
			compositionSpriteShape2.Offset = placement;
			CompositionContainerShape compositionContainerShape4 = compositor.CreateContainerShape();
			compositionContainerShape4.Shapes.Add(compositionSpriteShape2);
			inkLayer.Shapes.Add(compositionContainerShape4);
			return (Halo: compositionContainerShape3, Ink: compositionContainerShape4, HaloBrush: compositionColorBrush, InkBrush: compositionColorBrush2);
		}
		Vector2 GlyphPlacement(TextShaper.Glyph glyph, Vector2 lineOrigin)
		{
			double num11 = lineOrigin.X;
			if (snapsToPixels)
			{
				double num12 = (num11 + glyph.X + (double)translation.X) * devicePixelsPerUnit;
				double num13 = Math.Floor((num12 + 0.001) / glyphStepPixels) * glyphStepPixels;
				num11 += (num13 - num12) / devicePixelsPerUnit;
			}
			return new Vector2((float)num11, lineOrigin.Y);
		}
		double StartTime(double value)
		{
			return write.BeginTime + spread * Math.Clamp(value / totalAlong, 0.0, 1.0);
		}
	}

	private void AnimateColor(CompositionColorBrush brush, Color from, Color to, AnimationSpec spec, Node owner)
	{
		ColorKeyFrameAnimation colorKeyFrameAnimation = compositor.CreateColorKeyFrameAnimation();
		colorKeyFrameAnimation.InterpolationColorSpace = CompositionColorSpace.Rgb;
		colorKeyFrameAnimation.InsertKeyFrame(0f, from);
		colorKeyFrameAnimation.InsertKeyFrame(1f, to, Easing(spec.Timing));
		colorKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.001, spec.Duration));
		double num = spec.BeginTime - Now;
		if (num > 0.0)
		{
			colorKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(num);
		}
		colorKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		brush.StartAnimation("Color", colorKeyFrameAnimation);
		Record(brush, "Color", new Vector4((int)from.A, (int)from.R, (int)from.G, (int)from.B), new Vector4((int)to.A, (int)to.R, (int)to.G, (int)to.B), spec, fillBackwards: true);
		if (owner.Tag == null)
		{
			owner.Tag = new List<(CompositionColorBrush, Color)>();
		}
		((List<(CompositionColorBrush, Color)>)owner.Tag).Add((brush, to));
	}

	private SpriteVisual? MakeColorGlyphVisual(TextShaper.ColorGlyph glyph, Vector2 origin, Matrix3x2? transform)
	{
		try
		{
			float num = (float)DevicePixelsPerUnit;
			float num2 = glyph.Face.Ascent * glyph.Size;
			float num3 = glyph.Face.Descent * glyph.Size;
			Vector2 size = new Vector2(Math.Max(1f, glyph.Advance), num2 + num3);
			Size sizePixels = new Size(Math.Ceiling(size.X * num), Math.Ceiling(size.Y * num));
			CompositionDrawingSurface surface = graphicsDevice.CreateDrawingSurface(sizePixels, DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
			using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(surface))
			{
				CanvasDrawingSession session = surfaceDrawing.Session;
				session.Clear(Colors.Transparent);
				session.Transform = Matrix3x2.CreateScale(num);
				using CanvasTextFormat format = new CanvasTextFormat
				{
					FontFamily = "Segoe UI Emoji",
					FontSize = glyph.Size,
					Options = CanvasDrawTextOptions.EnableColorFont,
					WordWrapping = CanvasWordWrapping.NoWrap
				};
				session.DrawText(glyph.Text, new Vector2(0f, 0f), Colors.Black, format);
			}
			SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = compositor.CreateSurfaceBrush(surface);
			spriteVisual.Size = size;
			spriteVisual.Offset = new Vector3(origin.X, origin.Y - num2, 0f);
			string text = glyph.Text;
			float fontSize = glyph.Size;
			spriteContents[spriteVisual] = delegate(CanvasDrawingSession s, Vector2 box)
			{
				using CanvasTextFormat format2 = new CanvasTextFormat
				{
					FontFamily = "Segoe UI Emoji",
					FontSize = fontSize,
					Options = CanvasDrawTextOptions.EnableColorFont,
					WordWrapping = CanvasWordWrapping.NoWrap
				};
				s.DrawText(text, Vector2.Zero, Colors.Black, format2);
			};
			if (transform.HasValue)
			{
				Matrix3x2 valueOrDefault = transform.GetValueOrDefault();
				spriteVisual.TransformMatrix = SvgValues.To4x4(valueOrDefault);
			}
			return spriteVisual;
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "colour glyph failed: " + ex.Message);
			return null;
		}
	}

	[CompilerGenerated]
	private static bool _003COtherNodes_003Eg__IsInsideKept_007C102_0(Node node, ref _003C_003Ec__DisplayClass102_0 P_1)
	{
		if (P_1.holdingKept.Contains(node))
		{
			return true;
		}
		for (Node node2 = node; node2 != null; node2 = node2.Parent)
		{
			if (P_1.keptSet.Contains(node2))
			{
				return true;
			}
		}
		return false;
	}
}
