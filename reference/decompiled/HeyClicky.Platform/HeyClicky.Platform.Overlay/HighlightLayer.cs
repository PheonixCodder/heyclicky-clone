using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using HeyClicky.Platform.Win32;
using Vortice;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.DirectComposition;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

internal sealed class HighlightLayer : IOverlayLayer, IDisposable
{
	private sealed class InkPiece
	{
		public required IDCompositionVisual2 Visual;

		public IDCompositionVisual3? Visual3;

		public Vector2 From;

		public Vector2 To;
	}

	private const double SampleInterval = 11.0 / 750.0;

	private const double MovementActivationThreshold = 8.0;

	private const double MinimumPointSpacing = 1.5;

	private const double HighlighterLineWidth = 8.0;

	private const double StrokeFadeDurationSeconds = 1.0;

	private const double DwellRadius = 12.0;

	private const double DwellMinimumDuration = 0.35;

	private const float BaseAlpha = 0.55f;

	private const double SmoothingStep = 2.5;

	private const int MaximumPiecesPerSegment = 48;

	private const int OpacityLevels = 128;

	private readonly OverlayHost host;

	private readonly IDCompositionVisual2 visual;

	private readonly IDCompositionVisual3? visual3;

	private ID2D1SolidColorBrush? brush;

	private ID2D1StrokeStyle? roundStroke;

	private Color4 tint = new Color4(1f, 0.8f, 0f);

	private bool watching;

	private bool hasStartedStroke;

	private PointD holdStart;

	private double holdScale = 1.0;

	private int holdDisplayIndex;

	private double lastSampleTime = double.MinValue;

	private PointD lastDeliveredMouse;

	private PointD? lastSampledLocation;

	private PointD dwellAnchor;

	private double dwellAnchorStartTime;

	private readonly List<PointD> points = new List<PointD>(1024);

	private readonly List<double> pointTimes = new List<double>(1024);

	private readonly List<PointD> dwells = new List<PointD>(16);

	private bool layerVisible;

	private readonly List<Vector2> curvePoints = new List<Vector2>(4096);

	private readonly List<double> curveTimes = new List<double>(4096);

	public long RenderCount;

	private long rendersAtStart;

	private long gpuWaitsAtStart;

	private double strokeStartedAt;

	private IDCompositionSurface? barSurface;

	private IDCompositionSurface? capSurface;

	private const int BarWidthPx = 4;

	private int barHeightPx;

	private int capSizePx;

	private Color4 piecesTint;

	private double piecesScale = -1.0;

	private readonly List<InkPiece?> pieces = new List<InkPiece>(4096);

	private int firstLivePiece;

	private readonly Stack<InkPiece> sparePieces = new Stack<InkPiece>();

	private InkPiece? startCap;

	private InkPiece? endCap;

	public int PointCount => points.Count;

	public bool IsWatching => watching;

	public int IdleWakeIntervalMs => -1;

	public bool WantsMouseMoveWake => watching;

	public HighlightLayer(OverlayHost host)
	{
		this.host = host;
		visual = host.CompositionDevice.CreateVisual();
		visual3 = visual.QueryInterfaceOrNull<IDCompositionVisual3>();
		host.RootVisual.AddVisual(visual, false, null);
		visual3?.SetOpacity(0.55f);
		roundStroke = host.D2DFactory.CreateStrokeStyle(new StrokeStyleProperties
		{
			StartCap = CapStyle.Round,
			EndCap = CapStyle.Round,
			LineJoin = LineJoin.Round
		});
	}

	public void BeginWatching(Color4 color)
	{
		Clear();
		tint = color;
		watching = true;
		holdStart = DisplayEnumeratorFast.CursorPositionPx();
		lastDeliveredMouse = holdStart;
		DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(holdStart, host.Displays);
		holdScale = displayInfo.Scale;
		holdDisplayIndex = displayInfo.Index;
		rendersAtStart = RenderCount;
		gpuWaitsAtStart = host.GpuWaits;
		host.LongestGpuFrameMs = 0.0;
		strokeStartedAt = Now();
		EnsurePieceSurfaces();
		host.RequestFrames();
	}

	public IReadOnlyList<HighlightStroke> EndWatching()
	{
		if (watching)
		{
			watching = false;
			FinalizeOpenDwell(Now());
		}
		SetVisible(visible: false);
		if (!hasStartedStroke || points.Count < 2)
		{
			return Array.Empty<HighlightStroke>();
		}
		double num = Now() - strokeStartedAt;
		Log.Info("Highlight", $"stroke: {points.Count} points over {num:0.0} s, {RenderCount - rendersAtStart} frames drawn ({(double)(RenderCount - rendersAtStart) / Math.Max(0.1, num):0} a second), {host.GpuWaits - gpuWaitsAtStart} frames waited for the GPU, slowest GPU frame {host.LongestGpuFrameMs:0} ms");
		return new _003C_003Ez__ReadOnlySingleElementList<HighlightStroke>(new HighlightStroke(points.ToArray(), dwells.ToArray(), holdDisplayIndex));
	}

	public void Clear()
	{
		watching = false;
		hasStartedStroke = false;
		points.Clear();
		pointTimes.Clear();
		dwells.Clear();
		lastSampledLocation = null;
		SetVisible(visible: false);
	}

	public bool OnIdleWake(in OverlayFrame frame)
	{
		if (watching)
		{
			return frame.MousePx != lastDeliveredMouse;
		}
		return false;
	}

	public bool OnFrame(in OverlayFrame frame)
	{
		double timeSeconds = frame.TimeSeconds;
		if (watching && frame.MousePx != lastDeliveredMouse && timeSeconds - lastSampleTime >= 11.0 / 750.0)
		{
			lastDeliveredMouse = frame.MousePx;
			lastSampleTime = timeSeconds;
			ExtendStroke(frame.MousePx, timeSeconds);
		}
		int num;
		if (hasStartedStroke && pointTimes.Count > 0)
		{
			List<double> list = pointTimes;
			num = ((timeSeconds - list[list.Count - 1] < 1.0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (layerVisible && flag)
		{
			Render(timeSeconds);
		}
		if (watching)
		{
			if (!flag)
			{
				return frame.MousePx != lastDeliveredMouse;
			}
			return true;
		}
		return false;
	}

	private void ExtendStroke(PointD location, double now)
	{
		if (!hasStartedStroke)
		{
			if (!(Distance(location, holdStart) / holdScale < 8.0))
			{
				hasStartedStroke = true;
				points.Add(holdStart);
				pointTimes.Add(now);
				points.Add(location);
				pointTimes.Add(now);
				lastSampledLocation = location;
				dwellAnchor = location;
				dwellAnchorStartTime = now;
				SetVisible(visible: true);
			}
			return;
		}
		PointD? pointD = lastSampledLocation;
		if (pointD.HasValue)
		{
			PointD valueOrDefault = pointD.GetValueOrDefault();
			if (!(Distance(location, valueOrDefault) / holdScale >= 1.5))
			{
				goto IL_00da;
			}
		}
		points.Add(location);
		pointTimes.Add(now);
		lastSampledLocation = location;
		goto IL_00da;
		IL_00da:
		if (Distance(location, dwellAnchor) / holdScale > 12.0)
		{
			FinalizeOpenDwell(now);
			dwellAnchor = location;
			dwellAnchorStartTime = now;
		}
	}

	private void FinalizeOpenDwell(double now)
	{
		if (hasStartedStroke && now - dwellAnchorStartTime >= 0.35)
		{
			dwells.Add(dwellAnchor);
		}
	}

	private void SetVisible(bool visible)
	{
		if (visible != layerVisible)
		{
			layerVisible = visible;
			if (visible)
			{
				visual3?.SetVisible(true);
			}
			else
			{
				ReleaseAllPieces();
				visual3?.SetVisible(false);
			}
			host.SetLayerVisible(visible);
			host.RequestCommit();
			host.RequestFrames();
		}
	}

	private bool EnsurePieceSurfaces()
	{
		if ((object)barSurface != null && piecesScale == holdScale && piecesTint == tint)
		{
			return true;
		}
		if (!host.GpuCaughtUp)
		{
			return false;
		}
		float lineWidthPx = (float)(8.0 * holdScale);
		if (piecesScale != holdScale)
		{
			barSurface?.Dispose();
			capSurface?.Dispose();
			barHeightPx = (int)Math.Ceiling(lineWidthPx) + 4;
			capSizePx = barHeightPx;
			barSurface = host.CompositionDevice.CreateSurface(4u, (uint)barHeightPx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			capSurface = host.CompositionDevice.CreateSurface((uint)capSizePx, (uint)capSizePx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			ReleaseAllPieces(dropSpare: true);
		}
		DrawPieceSurface(barSurface, delegate(ID2D1DeviceContext context)
		{
			float num = ((float)barHeightPx - lineWidthPx) / 2f;
			context.FillRectangle(new RawRectF(-1f, num, 5f, num + lineWidthPx), brush);
		});
		DrawPieceSurface(capSurface, delegate(ID2D1DeviceContext context)
		{
			context.FillEllipse(new Ellipse(new Vector2((float)capSizePx / 2f, (float)capSizePx / 2f), lineWidthPx / 2f, lineWidthPx / 2f), brush);
		});
		piecesScale = holdScale;
		piecesTint = tint;
		return true;
	}

	private void DrawPieceSurface(IDCompositionSurface pieceSurface, Action<ID2D1DeviceContext> draw)
	{
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(pieceSurface, null, out offset);
		try
		{
			if ((object)brush == null)
			{
				brush = iD2D1DeviceContext.CreateSolidColorBrush(tint, null);
			}
			brush.Color = tint;
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			iD2D1DeviceContext.Transform = Matrix3x2.CreateTranslation(offset.X, offset.Y);
			draw(iD2D1DeviceContext);
			iD2D1DeviceContext.Transform = Matrix3x2.Identity;
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			pieceSurface.EndDraw();
		}
	}

	private InkPiece TakePiece(IDCompositionSurface content)
	{
		if (!sparePieces.TryPop(out InkPiece result))
		{
			IDCompositionVisual2 iDCompositionVisual = host.CompositionDevice.CreateVisual();
			iDCompositionVisual.SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
			iDCompositionVisual.SetBorderMode(Vortice.DirectComposition.BorderMode.Hard);
			visual.AddVisual(iDCompositionVisual, true, null);
			result = new InkPiece
			{
				Visual = iDCompositionVisual,
				Visual3 = iDCompositionVisual.QueryInterfaceOrNull<IDCompositionVisual3>()
			};
		}
		result.Visual.SetContent(content);
		result.From = (result.To = new Vector2(float.NaN));
		return result;
	}

	private void ReleasePiece(InkPiece? piece)
	{
		if (piece != null)
		{
			piece.Visual3?.SetOpacity(0f);
			sparePieces.Push(piece);
		}
	}

	private void ReleaseAllPieces(bool dropSpare = false)
	{
		foreach (InkPiece piece in pieces)
		{
			ReleasePiece(piece);
		}
		pieces.Clear();
		firstLivePiece = 0;
		ReleasePiece(startCap);
		startCap = null;
		ReleasePiece(endCap);
		endCap = null;
		if (dropSpare)
		{
			InkPiece result;
			while (sparePieces.TryPop(out result))
			{
				visual.RemoveVisual(result.Visual);
				result.Visual3?.Dispose();
				result.Visual.Dispose();
			}
		}
	}

	private void PlaceBar(InkPiece piece, Vector2 from, Vector2 to)
	{
		piece.From = from;
		piece.To = to;
		Vector2 vector = to - from;
		float num = vector.Length();
		piece.Visual.SetTransform(Matrix3x2.CreateScale(num / 4f, 1f) * Matrix3x2.CreateTranslation(0f, (float)(-barHeightPx) / 2f) * Matrix3x2.CreateRotation(MathF.Atan2(vector.Y, vector.X)) * Matrix3x2.CreateTranslation(from));
	}

	private void FadePiece(InkPiece piece, double pieceTime, double now)
	{
		double num = Opacity(pieceTime, now);
		if ((object)piece.Visual3 == null)
		{
			return;
		}
		using IDCompositionAnimation iDCompositionAnimation = host.CompositionDevice.CreateAnimation();
		iDCompositionAnimation.AddCubic(0.0, (float)num, -1f, 0f, 0f);
		iDCompositionAnimation.End(num * 1.0, 0f);
		piece.Visual3.SetOpacity(iDCompositionAnimation);
	}

	private void PlaceCap(ref InkPiece? cap, Vector2 at, double pieceTime, double now, bool fresh)
	{
		if (cap == null)
		{
			cap = TakePiece(capSurface);
			fresh = true;
		}
		if (cap.From != at)
		{
			cap.From = at;
			cap.Visual.SetTransform(Matrix3x2.CreateTranslation(at - new Vector2((float)capSizePx / 2f)));
		}
		if (fresh)
		{
			FadePiece(cap, pieceTime, now);
		}
	}

	private void Render(double now)
	{
		if (!EnsurePieceSurfaces())
		{
			return;
		}
		BuildSmoothedCurve();
		if (curvePoints.Count == 0)
		{
			return;
		}
		RenderCount++;
		Vector2 vector = new Vector2((float)host.VirtualScreenPx.X, (float)host.VirtualScreenPx.Y);
		int num = curvePoints.Count - 1;
		for (int num2 = pieces.Count - 1; num2 >= num; num2--)
		{
			ReleasePiece(pieces[num2]);
			pieces.RemoveAt(num2);
		}
		while (firstLivePiece < num && Opacity(curveTimes[firstLivePiece + 1], now) <= 0.0)
		{
			if (firstLivePiece < pieces.Count)
			{
				ReleasePiece(pieces[firstLivePiece]);
				pieces[firstLivePiece] = null;
			}
			firstLivePiece++;
		}
		for (int i = firstLivePiece; i < num; i++)
		{
			while (pieces.Count <= i)
			{
				pieces.Add(null);
			}
			Vector2 vector2 = curvePoints[i] - vector;
			Vector2 vector3 = curvePoints[i + 1] - vector;
			InkPiece inkPiece = pieces[i];
			if (inkPiece == null)
			{
				InkPiece inkPiece3 = (pieces[i] = TakePiece(barSurface));
				inkPiece = inkPiece3;
				PlaceBar(inkPiece, vector2, vector3);
				FadePiece(inkPiece, (curveTimes[i] + curveTimes[i + 1]) / 2.0, now);
			}
			else if (inkPiece.From != vector2 || inkPiece.To != vector3)
			{
				PlaceBar(inkPiece, vector2, vector3);
			}
		}
		int num3;
		if (endCap != null)
		{
			Vector2 from = endCap.From;
			List<Vector2> list = curvePoints;
			num3 = ((from != list[list.Count - 1] - vector) ? 1 : 0);
		}
		else
		{
			num3 = 1;
		}
		bool fresh = (byte)num3 != 0;
		ref InkPiece? cap = ref endCap;
		List<Vector2> list2 = curvePoints;
		Vector2 at = list2[list2.Count - 1] - vector;
		List<double> list3 = curveTimes;
		PlaceCap(ref cap, at, list3[list3.Count - 1], now, fresh);
		if (firstLivePiece == 0)
		{
			PlaceCap(ref startCap, curvePoints[0] - vector, curveTimes[0], now, fresh: false);
		}
		else
		{
			ReleasePiece(startCap);
			startCap = null;
		}
		host.RequestCommit();
	}

	private void DrawInk(ID2D1DeviceContext context, Matrix3x2 transform, int startIndex, double now, float lineWidthPx)
	{
		if ((object)brush == null)
		{
			brush = context.CreateSolidColorBrush(tint, null);
		}
		context.PrimitiveBlend = PrimitiveBlend.Max;
		context.Transform = transform;
		for (int i = startIndex; i < curvePoints.Count - 1; i++)
		{
			int num = (int)Math.Round((Opacity(curveTimes[i], now) + Opacity(curveTimes[i + 1], now)) / 2.0 * 128.0);
			if (num > 0)
			{
				brush.Color = new Color4(tint.R, tint.G, tint.B, 0.55f * (float)num / 128f);
				context.DrawLine(curvePoints[i], curvePoints[i + 1], brush, lineWidthPx, roundStroke);
			}
		}
		context.PrimitiveBlend = PrimitiveBlend.SourceOver;
		context.Transform = Matrix3x2.Identity;
	}

	public void SnapshotPng(string path, Color4 background, double now)
	{
		BuildSmoothedCurve();
		if (curvePoints.Count < 2)
		{
			return;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		foreach (Vector2 curvePoint in curvePoints)
		{
			num = Math.Min(num, curvePoint.X);
			num2 = Math.Min(num2, curvePoint.Y);
			num3 = Math.Max(num3, curvePoint.X);
			num4 = Math.Max(num4, curvePoint.Y);
		}
		float num5 = (float)(8.0 * holdScale);
		float num6 = num5 + 20f;
		SizeI size = new SizeI((int)(num3 - num + 2f * num6), (int)(num4 - num2 + 2f * num6));
		ID2D1DeviceContext d2DContext = host.D2DContext;
		using ID2D1Bitmap1 iD2D1Bitmap = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target));
		d2DContext.Target = iD2D1Bitmap;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		DrawInk(d2DContext, Matrix3x2.CreateTranslation(num6 - num, num6 - num2), 0, now, num5);
		d2DContext.EndDraw();
		d2DContext.Target = null;
		OverlayDebug.SavePng(host, iD2D1Bitmap, path, background);
	}

	private static double Opacity(double pointTime, double now)
	{
		return Math.Max(0.0, 1.0 - (now - pointTime) / 1.0);
	}

	private void BuildSmoothedCurve()
	{
		curvePoints.Clear();
		curveTimes.Clear();
		int count = points.Count;
		switch (count)
		{
		case 0:
			return;
		case 1:
			curvePoints.Add(ToVector(points[0]));
			curveTimes.Add(pointTimes[0]);
			return;
		}
		double num = 2.5 * holdScale;
		for (int i = 0; i < count - 1; i++)
		{
			Vector2 vector = ToVector(points[i]);
			Vector2 vector2 = ToVector(points[i + 1]);
			Vector2 a = ((i > 0) ? ToVector(points[i - 1]) : (2f * vector - vector2));
			Vector2 b = ((i + 2 < count) ? ToVector(points[i + 2]) : (2f * vector2 - vector));
			float num2 = 0f;
			float num3 = num2 + KnotDistance(a, vector);
			float num4 = num3 + KnotDistance(vector, vector2);
			float tb = num4 + KnotDistance(vector2, b);
			float num5 = Vector2.Distance(vector, vector2);
			int num6 = Math.Min(48, Math.Max(1, (int)Math.Ceiling((double)num5 / num)));
			for (int j = 0; j < num6; j++)
			{
				float num7 = (float)j / (float)num6;
				if (j == 0)
				{
					curvePoints.Add(vector);
				}
				else
				{
					float t = num3 + num7 * (num4 - num3);
					Vector2 a2 = Lerp(a, vector, num2, num3, t);
					Vector2 vector3 = Lerp(vector, vector2, num3, num4, t);
					Vector2 b2 = Lerp(vector2, b, num4, tb, t);
					Vector2 a3 = Lerp(a2, vector3, num2, num4, t);
					Vector2 b3 = Lerp(vector3, b2, num3, tb, t);
					curvePoints.Add(Lerp(a3, b3, num3, num4, t));
				}
				curveTimes.Add(pointTimes[i] + (double)num7 * (pointTimes[i + 1] - pointTimes[i]));
			}
		}
		curvePoints.Add(ToVector(points[count - 1]));
		curveTimes.Add(pointTimes[count - 1]);
	}

	private static float KnotDistance(Vector2 a, Vector2 b)
	{
		return Math.Max(MathF.Sqrt(Vector2.Distance(a, b)), 0.0001f);
	}

	private static Vector2 Lerp(Vector2 a, Vector2 b, float ta, float tb, float t)
	{
		float num = ((tb - ta == 0f) ? 0f : ((t - ta) / (tb - ta)));
		return a + (b - a) * num;
	}

	private static Vector2 ToVector(PointD p)
	{
		return new Vector2((float)p.X, (float)p.Y);
	}

	private static double Distance(PointD a, PointD b)
	{
		return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
	}

	private static double Now()
	{
		return (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
	}

	public void OnDisplaysChanged(IReadOnlyList<DisplayInfo> displays, RectD virtualScreenPx)
	{
	}

	public void Dispose()
	{
		SetVisible(visible: false);
		host.RootVisual.RemoveVisual(visual);
		brush?.Dispose();
		roundStroke?.Dispose();
		ReleaseAllPieces(dropSpare: true);
		barSurface?.Dispose();
		capSurface?.Dispose();
		visual3?.Dispose();
		visual.Dispose();
		host.RequestCommit();
	}

	bool IOverlayLayer.OnFrame(in OverlayFrame frame)
	{
		return OnFrame(in frame);
	}

	bool IOverlayLayer.OnIdleWake(in OverlayFrame frame)
	{
		return OnIdleWake(in frame);
	}
}
