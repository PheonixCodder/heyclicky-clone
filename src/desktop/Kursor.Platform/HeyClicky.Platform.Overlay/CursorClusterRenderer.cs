using System;
using System.Collections.Generic;
using System.Numerics;
using Vortice;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.DirectComposition;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

internal sealed class CursorClusterRenderer : IDisposable
{
	private sealed class LabelText(string text, IDCompositionSurface surface, float[] carets, float heightPoints, int heightPx) : IDisposable
	{
		public readonly string text = text;

		public readonly IDCompositionSurface surface = surface;

		public readonly float[] carets = carets;

		public readonly float heightPoints = heightPoints;

		public readonly int heightPx = heightPx;

		public void Dispose()
		{
			this.surface.Dispose();
		}
	}

	public const float ClusterWidth = 480f;

	public const float ClusterHeight = 160f;

	public static readonly Vector2 Anchor = new Vector2(64f, 56f);

	private const float TriangleSide = 16f;

	private const float TriangleCornerRadiusFraction = 0.09f;

	private const float DwellRingDiameter = 16f;

	private const float DwellRingLineWidth = 2.5f;

	private ID2D1StrokeStyle? dwellRingStroke;

	private const int BarCount = 5;

	private const float BarWidth = 2f;

	private const float BarSpacing = 2f;

	private const float BarCornerRadius = 1.5f;

	private const float BubbleFontSize = 11f;

	private const float BubblePaddingX = 8f;

	private const float BubblePaddingY = 4f;

	private const float BubbleCornerRadius = 6f;

	private const float BubbleLeftFromAnchor = 10f;

	private const float BubbleCenterYFromAnchor = 18f;

	private static readonly float LabelMaxWidth = 480f - Anchor.X - 10f - 16f - 12f;

	private readonly OverlayHost host;

	private readonly IDCompositionVisual2 visual;

	private ID2D1Bitmap1? bodyLayer;

	private ID2D1Bitmap1? bubbleLayer;

	private ID2D1Bitmap1? clusterLayer;

	private ID2D1Effect? bodyShadow;

	private ID2D1Effect? bubbleShadow;

	private ID2D1PathGeometry? trianglePath;

	private ID2D1SolidColorBrush? tintBrush;

	private ID2D1SolidColorBrush? whiteBrush;

	private IDWriteTextFormat? bubbleTextFormat;

	private IDWriteTextLayout? bubbleTextLayout;

	private string bubbleTextLayoutText = "";

	private double scale;

	private int pixelWidth;

	private int pixelHeight;

	private Color4 tint = new Color4(0.2f, 0.5019608f, 1f);

	public long RenderCount;

	private readonly IDCompositionVisual2 ringVisual;

	private readonly IDCompositionVisual2 triangleVisual;

	private readonly IDCompositionVisual3? ringVisual3;

	private readonly IDCompositionVisual3? triangleVisual3;

	private IDCompositionSurface? ringAtlas;

	private ID2D1Bitmap1? ringLayer;

	private int ringCellPx;

	private bool ringAtlasDrawn;

	private const int RingSteps = 32;

	private readonly IDCompositionVisual2[] glowVisuals = new IDCompositionVisual2[GlowLevels.Length];

	private readonly IDCompositionVisual3?[] glowVisuals3 = new IDCompositionVisual3[GlowLevels.Length];

	private readonly IDCompositionSurface?[] glowSurfaces = new IDCompositionSurface[GlowLevels.Length];

	private static readonly float[] GlowLevels = new float[4] { 4f, 8f, 12f, 18f };

	private readonly IDCompositionVisual2[] dotVisuals = new IDCompositionVisual2[3];

	private readonly IDCompositionVisual3?[] dotVisuals3 = new IDCompositionVisual3[3];

	private IDCompositionSurface? triangleSurface;

	private IDCompositionSurface? dotSurface;

	private ID2D1Bitmap1? triangleLayer;

	private ID2D1Bitmap1? dotLayer;

	private ID2D1Effect? pieceShadow;

	private int trianglePiecePx;

	private int dotPiecePx;

	private bool trianglePiecesDrawn;

	private bool dotPieceDrawn;

	private const float DotPieceRadius = 2.3f;

	private const float DotPieceGlow = 8f;

	private const float TrianglePieceMaximumGlow = 20f;

	private CursorClusterPose lastPose = CursorClusterPose.CreateDefault();

	private readonly IDCompositionDevice3? effects;

	private readonly IDCompositionVisual2 bubbleRoot;

	private readonly IDCompositionVisual2 bubbleGlow;

	private readonly IDCompositionVisual2 bubbleBody;

	private readonly IDCompositionVisual2 bubbleTextVisual;

	private readonly IDCompositionVisual3? bubbleRoot3;

	private readonly IDCompositionRectangleClip bubbleBodyClip;

	private readonly IDCompositionShadowEffect? bubbleShadowEffect;

	private readonly IDCompositionShadowEffect? waveShadowEffect;

	private readonly IDCompositionCompositeEffect? bubbleGlowEffect;

	private readonly IDCompositionCompositeEffect? waveGlowEffect;

	private readonly IDCompositionVisual2 waveRoot;

	private readonly IDCompositionVisual2 waveGlow;

	private readonly IDCompositionVisual3? waveRoot3;

	private readonly IDCompositionVisual2[] barVisuals = new IDCompositionVisual2[5];

	private readonly IDCompositionRectangleClip[] barClips = new IDCompositionRectangleClip[5];

	private IDCompositionSurface? tintSurface;

	private bool tintSurfaceDrawn;

	private readonly List<LabelText> labelTexts = new List<LabelText>();

	private LabelText? shownLabel;

	private int labelMarginPx;

	private PointD? clusterAnchorScreenPx;

	private RectD clusterWorkAreaPx;

	private CursorAdjacentPlacement.Sides bubbleSides;

	public double Scale => scale;

	public Vector2 AnchorPx => Anchor * (float)scale;

	public bool PiecesNeedDrawing
	{
		get
		{
			if (trianglePiecesDrawn && dotPieceDrawn && tintSurfaceDrawn)
			{
				return !ringAtlasDrawn;
			}
			return true;
		}
	}

	private static ID2D1PathGeometry CreateArc(ID2D1Factory1 factory, float radius, double fraction)
	{
		ID2D1PathGeometry1 iD2D1PathGeometry = factory.CreatePathGeometry();
		using ID2D1GeometrySink iD2D1GeometrySink = iD2D1PathGeometry.Open();
		double num = Math.Min(fraction, 0.9999) * 2.0 * Math.PI;
		iD2D1GeometrySink.BeginFigure(new Vector2(0f, 0f - radius), FigureBegin.Hollow);
		iD2D1GeometrySink.AddArc(new ArcSegment(new Vector2((float)((double)radius * Math.Sin(num)), (float)((double)(0f - radius) * Math.Cos(num))), new Size(radius, radius), 0f, SweepDirection.Clockwise, (fraction > 0.5) ? ArcSize.Large : ArcSize.Small));
		iD2D1GeometrySink.EndFigure(FigureEnd.Open);
		iD2D1GeometrySink.Close();
		return iD2D1PathGeometry;
	}

	public CursorClusterRenderer(OverlayHost host, IDCompositionVisual2 visual)
	{
		this.host = host;
		this.visual = visual;
		IDCompositionDesktopDevice compositionDevice = host.CompositionDevice;
		IDCompositionVisual2 iDCompositionVisual = null;
		for (int i = 0; i < GlowLevels.Length; i++)
		{
			glowVisuals[i] = compositionDevice.CreateVisual();
			glowVisuals3[i] = glowVisuals[i].QueryInterfaceOrNull<IDCompositionVisual3>();
			glowVisuals[i].SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
			if ((object)iDCompositionVisual == null)
			{
				visual.AddVisual(glowVisuals[i], false, null);
			}
			else
			{
				visual.AddVisual(glowVisuals[i], true, iDCompositionVisual);
			}
			iDCompositionVisual = glowVisuals[i];
			glowVisuals3[i]?.SetOpacity(0f);
		}
		triangleVisual = compositionDevice.CreateVisual();
		triangleVisual3 = triangleVisual.QueryInterfaceOrNull<IDCompositionVisual3>();
		triangleVisual.SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
		visual.AddVisual(triangleVisual, true, iDCompositionVisual);
		IDCompositionVisual2 referenceVisual = triangleVisual;
		for (int j = 0; j < 3; j++)
		{
			dotVisuals[j] = compositionDevice.CreateVisual();
			dotVisuals3[j] = dotVisuals[j].QueryInterfaceOrNull<IDCompositionVisual3>();
			dotVisuals[j].SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
			visual.AddVisual(dotVisuals[j], true, referenceVisual);
			referenceVisual = dotVisuals[j];
		}
		ringVisual = compositionDevice.CreateVisual();
		ringVisual3 = ringVisual.QueryInterfaceOrNull<IDCompositionVisual3>();
		ringVisual.SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.NearestNeighbor);
		visual.AddVisual(ringVisual, true, referenceVisual);
		ringVisual3?.SetOpacity(0f);
		triangleVisual3?.SetOpacity(0f);
		IDCompositionVisual3[] array = dotVisuals3;
		for (int k = 0; k < array.Length; k++)
		{
			array[k]?.SetOpacity(0f);
		}
		effects = compositionDevice.QueryInterfaceOrNull<IDCompositionDevice3>();
		waveRoot = compositionDevice.CreateVisual();
		waveRoot3 = waveRoot.QueryInterfaceOrNull<IDCompositionVisual3>();
		waveGlow = compositionDevice.CreateVisual();
		waveRoot.AddVisual(waveGlow, false, null);
		IDCompositionVisual2 iDCompositionVisual2 = null;
		for (int l = 0; l < 5; l++)
		{
			barVisuals[l] = compositionDevice.CreateVisual();
			barClips[l] = compositionDevice.CreateRectangleClip();
			barVisuals[l].SetClip(barClips[l]);
			barVisuals[l].SetBorderMode(Vortice.DirectComposition.BorderMode.Soft);
			if ((object)iDCompositionVisual2 == null)
			{
				waveGlow.AddVisual(barVisuals[l], false, null);
			}
			else
			{
				waveGlow.AddVisual(barVisuals[l], true, iDCompositionVisual2);
			}
			iDCompositionVisual2 = barVisuals[l];
		}
		visual.AddVisual(waveRoot, true, ringVisual);
		bubbleRoot = compositionDevice.CreateVisual();
		bubbleRoot3 = bubbleRoot.QueryInterfaceOrNull<IDCompositionVisual3>();
		bubbleGlow = compositionDevice.CreateVisual();
		bubbleBody = compositionDevice.CreateVisual();
		bubbleBodyClip = compositionDevice.CreateRectangleClip();
		bubbleBody.SetClip(bubbleBodyClip);
		bubbleBody.SetBorderMode(Vortice.DirectComposition.BorderMode.Soft);
		bubbleGlow.AddVisual(bubbleBody, false, null);
		bubbleTextVisual = compositionDevice.CreateVisual();
		bubbleTextVisual.SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
		bubbleRoot.AddVisual(bubbleGlow, false, null);
		bubbleRoot.AddVisual(bubbleTextVisual, true, bubbleGlow);
		visual.AddVisual(bubbleRoot, true, waveRoot);
		if ((object)effects != null)
		{
			(bubbleShadowEffect, bubbleGlowEffect) = CreateGlowEffect(effects);
			bubbleGlow.SetEffect(bubbleGlowEffect);
			(waveShadowEffect, waveGlowEffect) = CreateGlowEffect(effects);
			waveGlow.SetEffect(waveGlowEffect);
		}
		waveRoot3?.SetVisible(false);
		bubbleRoot3?.SetVisible(false);
	}

	private static (IDCompositionShadowEffect Shadow, IDCompositionCompositeEffect Glow) CreateGlowEffect(IDCompositionDevice3 effects)
	{
		effects.CreateShadowEffect(out var shadowEffect).CheckError();
		effects.CreateCompositeEffect(out var compositeEffect).CheckError();
		shadowEffect.SetInput(0u, null, 0u);
		compositeEffect.SetMode(Vortice.Direct2D1.CompositeMode.SourceOver);
		compositeEffect.SetInput(0u, shadowEffect, 0u);
		compositeEffect.SetInput(1u, null, 0u);
		return (Shadow: shadowEffect, Glow: compositeEffect);
	}

	public void SetTint(Color4 color)
	{
		tint = color;
		if ((object)tintBrush != null)
		{
			tintBrush.Color = color;
		}
		trianglePiecesDrawn = false;
		dotPieceDrawn = false;
		tintSurfaceDrawn = false;
		ringAtlasDrawn = false;
	}

	public void SetScreenPlacement(PointD anchorScreenPx, RectD workAreaPx)
	{
		clusterAnchorScreenPx = anchorScreenPx;
		clusterWorkAreaPx = workAreaPx;
	}

	public void PlacePieces(in CursorClusterPose pose)
	{
		if ((object)triangleSurface == null)
		{
			return;
		}
		PlaceWaveform(in pose);
		PlaceBubble(in pose);
		PlaceRing(in pose);
		float num = (float)scale;
		Vector2 position = Anchor * num;
		if (pose.TriangleOpacity > 0.001 && trianglePiecesDrawn)
		{
			float num2 = (float)trianglePiecePx / 2f;
			Matrix3x2 transform = Matrix3x2.CreateTranslation(0f - num2, 0f - num2) * Matrix3x2.CreateScale((float)pose.TriangleScale) * Matrix3x2.CreateRotation((float)(pose.TriangleRotationDegrees * Math.PI / 180.0)) * Matrix3x2.CreateTranslation(position);
			triangleVisual.SetTransform(transform);
			triangleVisual3?.SetOpacity((float)pose.TriangleOpacity);
			double num3 = Math.Clamp(pose.TriangleGlowRadius, GlowLevels[0], GlowLevels[^1]);
			int i;
			for (i = 0; i < GlowLevels.Length - 2 && num3 > (double)GlowLevels[i + 1]; i++)
			{
			}
			double num4 = (num3 - (double)GlowLevels[i]) / (double)(GlowLevels[i + 1] - GlowLevels[i]);
			for (int j = 0; j < GlowLevels.Length; j++)
			{
				double num5 = ((j == i) ? (1.0 - num4) : ((j == i + 1) ? num4 : 0.0));
				glowVisuals[j].SetTransform(transform);
				glowVisuals3[j]?.SetOpacity((float)(pose.TriangleOpacity * num5));
			}
		}
		else
		{
			triangleVisual3?.SetOpacity(0f);
			IDCompositionVisual3[] array = glowVisuals3;
			for (int k = 0; k < array.Length; k++)
			{
				array[k]?.SetOpacity(0f);
			}
		}
		for (int l = 0; l < 3; l++)
		{
			if (pose.DotsOpacity > 0.001 && l < pose.DotCount && dotPieceDrawn)
			{
				float num6 = (float)dotPiecePx / 2f;
				float num7 = (float)(pose.DotRadii[l] / 2.299999952316284);
				dotVisuals[l].SetTransform(Matrix3x2.CreateTranslation(0f - num6, 0f - num6) * Matrix3x2.CreateScale(num7, num7 * (float)pose.DotAcrossScale[l]) * Matrix3x2.CreateRotation((float)pose.DotAngles[l]) * Matrix3x2.CreateTranslation((pose.DotCenters[l] + Anchor) * num));
				dotVisuals3[l]?.SetOpacity((float)pose.DotsOpacity);
			}
			else
			{
				dotVisuals3[l]?.SetOpacity(0f);
			}
		}
		host.RequestCommit();
	}

	private void PlaceRing(in CursorClusterPose pose)
	{
		bool flag = pose.StopSquareOpacity > 0.001;
		bool flag2 = !flag && pose.DwellRingOpacity > 0.001;
		if (!(flag || flag2) || !ringAtlasDrawn)
		{
			ringVisual3?.SetOpacity(0f);
			return;
		}
		int num = (flag ? 32 : ((int)Math.Round(Math.Clamp(pose.DwellRingProgress, 0.0, 1.0) * 31.0)));
		Vector2 vector = Anchor * (float)scale;
		ringVisual.SetOffsetX(vector.X - ((float)num + 0.5f) * (float)ringCellPx);
		ringVisual.SetOffsetY(vector.Y - (float)ringCellPx / 2f);
		ringVisual.SetClip(new RawRectF(num * ringCellPx, 0f, (num + 1) * ringCellPx, ringCellPx));
		ringVisual3?.SetOpacity((float)(flag ? pose.StopSquareOpacity : pose.DwellRingOpacity));
	}

	private void PlaceWaveform(in CursorClusterPose pose)
	{
		bool flag = pose.WaveformOpacity > 0.001 && pose.StopSquareOpacity <= 0.001 && tintSurfaceDrawn;
		waveRoot3?.SetVisible(flag);
		if (flag)
		{
			float num = (float)scale;
			float num2 = (float)Math.Max(0.1, pose.WaveformScale);
			Vector2 vector = Anchor * num;
			waveRoot.SetTransform(Matrix3x2.CreateTranslation(-vector) * Matrix3x2.CreateScale(num2) * Matrix3x2.CreateTranslation(vector));
			float num3 = 18f;
			float radius = Math.Min(1.5f, 1f) * num;
			for (int i = 0; i < 5; i++)
			{
				float num4 = pose.WaveformBarHeights[i];
				float num5 = (0f - num3) / 2f + (float)i * 4f;
				barVisuals[i].SetOffsetX(vector.X + num5 * num);
				barVisuals[i].SetOffsetY(vector.Y - num4 / 2f * num);
				SetRoundedClip(barClips[i], 2f * num, num4 * num, radius);
			}
			SetGlow(waveShadowEffect, 6f * num / num2, 0.6);
			waveRoot3?.SetOpacity((float)pose.WaveformOpacity);
		}
	}

	private void PlaceBubble(in CursorClusterPose pose)
	{
		LabelText labelText = ((pose.BubbleOpacity > 0.001 && pose.BubbleText.Length > 0 && tintSurfaceDrawn) ? LabelFor(in pose) : null);
		bubbleRoot3?.SetVisible(labelText != null);
		if (labelText != null)
		{
			float num = (float)scale;
			Vector2 vector = Anchor * num;
			float num2 = labelText.carets[Math.Min(pose.BubbleText.Length, labelText.carets.Length - 1)];
			float num3 = (num2 + 16f) * num;
			float num4 = labelText.heightPoints * num;
			float num5 = vector.X + 10f * num;
			float num6 = vector.Y + 18f * num - num4 / 2f;
			PointD? pointD = clusterAnchorScreenPx;
			if (pointD.HasValue)
			{
				PointD valueOrDefault = pointD.GetValueOrDefault();
				double num7 = valueOrDefault.X - (double)vector.X;
				double num8 = valueOrDefault.Y - (double)vector.Y;
				float num9 = (labelText.carets[^1] + 16f) * num;
				CursorAdjacentPlacement.Placed placed = CursorAdjacentPlacement.Place(bubbleSides, (double)num5 + num7, (double)num6 + num8, valueOrDefault.X - (double)(10f * num), valueOrDefault.Y - (double)(18f * num) + (double)(num4 / 2f), num3, num4, num9, num4, clusterWorkAreaPx, num);
				bubbleSides = placed.Sides;
				num5 = (float)(placed.Left - num7);
				num6 = (float)(placed.Top - num8);
			}
			num5 = MathF.Round(num5);
			num6 = MathF.Round(num6);
			float num10 = (float)pose.BubbleScale;
			bubbleRoot.SetTransform(Matrix3x2.CreateTranslation((0f - num3) / 2f, (0f - num4) / 2f) * Matrix3x2.CreateScale(num10) * Matrix3x2.CreateTranslation(num5 + num3 / 2f, num6 + num4 / 2f));
			SetRoundedClip(bubbleBodyClip, num3, num4, 6f * num);
			if (shownLabel != labelText)
			{
				bubbleTextVisual.SetContent(labelText.surface);
				bubbleTextVisual.SetOffsetX(8f * num - (float)labelMarginPx);
				bubbleTextVisual.SetOffsetY(4f * num - (float)labelMarginPx);
				shownLabel = labelText;
			}
			bubbleTextVisual.SetClip(new RawRectF(0f, 0f, (float)labelMarginPx + num2 * num, labelText.heightPx));
			double num11 = 1.0 - pose.BubbleScale;
			SetGlow(bubbleShadowEffect, (6.0 + num11 * 16.0) * (double)num / Math.Max(0.05, num10), Math.Min(1.0, 0.5 + num11));
			bubbleRoot3?.SetOpacity((float)pose.BubbleOpacity);
		}
	}

	private static void SetRoundedClip(IDCompositionRectangleClip clip, float width, float height, float radius)
	{
		clip.SetLeft(0f);
		clip.SetTop(0f);
		clip.SetRight(width);
		clip.SetBottom(height);
		clip.SetTopLeftRadiusX(radius);
		clip.SetTopLeftRadiusY(radius);
		clip.SetTopRightRadiusX(radius);
		clip.SetTopRightRadiusY(radius);
		clip.SetBottomLeftRadiusX(radius);
		clip.SetBottomLeftRadiusY(radius);
		clip.SetBottomRightRadiusX(radius);
		clip.SetBottomRightRadiusY(radius);
	}

	private void SetGlow(IDCompositionShadowEffect? shadow, double radiusPx, double opacity)
	{
		if ((object)shadow != null)
		{
			shadow.SetStandardDeviation((float)Math.Max(0.01, radiusPx / 2.0));
			shadow.SetColor(new Vector4(tint.R, tint.G, tint.B, (float)Math.Clamp(opacity, 0.0, 1.0)));
		}
	}

	private LabelText? LabelFor(in CursorClusterPose pose)
	{
		string whole = ((pose.BubbleLabel.Length >= pose.BubbleText.Length && pose.BubbleLabel.StartsWith(pose.BubbleText, StringComparison.Ordinal)) ? pose.BubbleLabel : pose.BubbleText);
		PrepareLabel(whole);
		return labelTexts.Find((LabelText label) => label.text == whole);
	}

	public bool PrepareLabel(string text)
	{
		if ((object)tintSurface == null || (object)bubbleTextFormat == null || text.Length == 0 || labelTexts.Exists((LabelText label) => label.text == text))
		{
			return false;
		}
		float num = (float)scale;
		using IDWriteTextLayout iDWriteTextLayout = host.DWriteFactory.CreateTextLayout(text, bubbleTextFormat, LabelMaxWidth, 100f);
		TextMetrics metrics = iDWriteTextLayout.Metrics;
		float[] array = new float[text.Length + 1];
		for (int i = 0; i < text.Length; i++)
		{
			iDWriteTextLayout.HitTestTextPosition((uint)i, false, out var pointX, out var _, out var _);
			array[i] = Math.Min(pointX, LabelMaxWidth);
		}
		array[text.Length] = Math.Min(metrics.WidthIncludingTrailingWhitespace, LabelMaxWidth);
		int width = (int)Math.Ceiling(array[text.Length] * num) + 2 * labelMarginPx;
		int num2 = (int)Math.Ceiling(metrics.Height * num) + 2 * labelMarginPx;
		IDCompositionSurface iDCompositionSurface = host.CompositionDevice.CreateSurface((uint)width, (uint)num2, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(iDCompositionSurface, null, out offset);
		try
		{
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			iD2D1DeviceContext.Transform = Matrix3x2.CreateScale(num) * Matrix3x2.CreateTranslation(offset.X + labelMarginPx, offset.Y + labelMarginPx);
			iD2D1DeviceContext.DrawTextLayout(Vector2.Zero, iDWriteTextLayout, whiteBrush, DrawTextOptions.None);
			iD2D1DeviceContext.Transform = Matrix3x2.Identity;
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			iDCompositionSurface.EndDraw();
		}
		if (labelTexts.Count >= 4)
		{
			LabelText labelText = labelTexts.Find((LabelText label) => label != shownLabel);
			if (labelText != null)
			{
				labelTexts.Remove(labelText);
				labelText.Dispose();
			}
		}
		labelTexts.Add(new LabelText(text, iDCompositionSurface, array, metrics.Height + 8f, num2));
		RenderCount++;
		return true;
	}

	private void ReleaseLabels()
	{
		bubbleTextVisual.SetContent(null);
		shownLabel = null;
		foreach (LabelText labelText in labelTexts)
		{
			labelText.Dispose();
		}
		labelTexts.Clear();
	}

	private void DrawPieces()
	{
		ID2D1DeviceContext d2DContext = host.D2DContext;
		float s = (float)scale;
		Int2 offset;
		if (!tintSurfaceDrawn)
		{
			ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(tintSurface, null, out offset);
			try
			{
				iD2D1DeviceContext.Clear(tint);
			}
			finally
			{
				iD2D1DeviceContext.Dispose();
				tintSurface.EndDraw();
			}
			tintSurfaceDrawn = true;
		}
		float half;
		if (!trianglePiecesDrawn)
		{
			half = (float)trianglePiecePx / 2f;
			DrawPiece(d2DContext, triangleLayer, triangleSurface, 0f, Triangle, withShape: true, withGlow: false, 1.0, null);
			for (int i = 0; i < GlowLevels.Length; i++)
			{
				DrawPiece(d2DContext, triangleLayer, glowSurfaces[i], GlowLevels[i] * s, Triangle, withShape: false, withGlow: true, 1.0, null);
			}
			trianglePiecesDrawn = true;
		}
		if (!dotPieceDrawn)
		{
			float half2 = (float)dotPiecePx / 2f;
			DrawPiece(d2DContext, dotLayer, dotSurface, 8f * s, delegate(ID2D1DeviceContext piece)
			{
				piece.Transform = Matrix3x2.Identity;
				piece.FillEllipse(new Ellipse(new Vector2(half2, half2), 2.3f * s, 2.3f * s), tintBrush);
			}, withShape: true, withGlow: true, 1.0, null);
			dotPieceDrawn = true;
		}
		if (ringAtlasDrawn)
		{
			return;
		}
		float half3 = (float)ringCellPx / 2f;
		float radius = 6.75f;
		if ((object)dwellRingStroke == null)
		{
			dwellRingStroke = host.D2DFactory.CreateStrokeStyle(new StrokeStyleProperties
			{
				StartCap = CapStyle.Round,
				EndCap = CapStyle.Round
			});
		}
		ID2D1DeviceContext iD2D1DeviceContext2 = OverlayHost.BeginDraw(ringAtlas, null, out offset);
		try
		{
			iD2D1DeviceContext2.Clear(new Color4(0f, 0f, 0f, 0f));
		}
		finally
		{
			iD2D1DeviceContext2.Dispose();
			ringAtlas.EndDraw();
		}
		for (int j = 0; j <= 32; j++)
		{
			int step = j;
			DrawPiece(d2DContext, ringLayer, ringAtlas, 6f * s, delegate(ID2D1DeviceContext piece)
			{
				piece.Transform = Matrix3x2.CreateScale(s) * Matrix3x2.CreateTranslation(half3, half3);
				if (step == 32)
				{
					piece.FillRoundedRectangle(new RoundedRectangle(new RawRectF(-3.5f, -3.5f, 3.5f, 3.5f), 1.5f, 1.5f), tintBrush);
				}
				else
				{
					tintBrush.Opacity = 0.22f;
					piece.DrawEllipse(new Ellipse(Vector2.Zero, radius, radius), tintBrush, 2.5f);
					tintBrush.Opacity = 1f;
					double num = (double)step / 31.0;
					if (!(num <= 0.001))
					{
						using (ID2D1PathGeometry geometry = CreateArc(host.D2DFactory, radius, num))
						{
							piece.DrawGeometry(geometry, tintBrush, 2.5f, dwellRingStroke);
						}
					}
				}
			}, withShape: true, withGlow: true, 0.6, new RawRect(j * ringCellPx, 0, (j + 1) * ringCellPx, ringCellPx));
		}
		ringAtlasDrawn = true;
		void Triangle(ID2D1DeviceContext piece)
		{
			piece.Transform = Matrix3x2.CreateScale(s) * Matrix3x2.CreateTranslation(half, half);
			piece.FillGeometry(trianglePath, tintBrush);
		}
	}

	private void DrawPiece(ID2D1DeviceContext context, ID2D1Bitmap1 layer, IDCompositionSurface pieceSurface, float glowPx, Action<ID2D1DeviceContext> draw, bool withShape = true, bool withGlow = true, double glowOpacity = 1.0, RawRect? updateRect = null)
	{
		context.Target = layer;
		context.BeginDraw();
		context.Clear(new Color4(0f, 0f, 0f, 0f));
		tintBrush.Opacity = 1f;
		draw(context);
		tintBrush.Opacity = 1f;
		context.Transform = Matrix3x2.Identity;
		context.EndDraw();
		context.Target = null;
		pieceShadow.SetInput(0u, layer, true);
		ConfigureShadow(pieceShadow, glowPx, glowOpacity);
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(pieceSurface, updateRect, out offset);
		try
		{
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			Vector2 value = new Vector2(offset.X, offset.Y);
			if (withGlow)
			{
				iD2D1DeviceContext.DrawImage(pieceShadow.Output, value, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
			}
			if (withShape)
			{
				iD2D1DeviceContext.DrawImage(layer, value, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
			}
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			pieceSurface.EndDraw();
		}
	}

	public void EnsureScale(double newScale)
	{
		if ((object)ringAtlas == null || !(Math.Abs(newScale - scale) < 0.001))
		{
			scale = newScale;
			ReleaseScaledResources();
			pixelWidth = (int)Math.Ceiling(480.0 * scale);
			pixelHeight = (int)Math.Ceiling(160.0 * scale);
			ID2D1DeviceContext d2DContext = host.D2DContext;
			BitmapProperties1 bitmapProperties = new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target);
			ringCellPx = (int)Math.Ceiling(38.0 * scale / 2.0) * 2;
			ringAtlas = host.CompositionDevice.CreateSurface((uint)(ringCellPx * 33), (uint)ringCellPx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			ringVisual.SetContent(ringAtlas);
			ringLayer = d2DContext.CreateBitmap(new SizeI(ringCellPx, ringCellPx), IntPtr.Zero, 0u, bitmapProperties);
			SizeI size = new SizeI(pixelWidth, pixelHeight);
			bodyLayer = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, bitmapProperties);
			bubbleLayer = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, bitmapProperties);
			clusterLayer = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, bitmapProperties);
			bodyShadow = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
			bodyShadow.SetInput(0u, bodyLayer, true);
			bubbleShadow = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
			bubbleShadow.SetInput(0u, bubbleLayer, true);
			trianglePiecePx = (int)Math.Ceiling(80.0 * scale) / 2 * 2;
			dotPiecePx = (int)Math.Ceiling(32.599998474121094 * scale) / 2 * 2;
			triangleSurface = host.CompositionDevice.CreateSurface((uint)trianglePiecePx, (uint)trianglePiecePx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			dotSurface = host.CompositionDevice.CreateSurface((uint)dotPiecePx, (uint)dotPiecePx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			triangleVisual.SetContent(triangleSurface);
			for (int i = 0; i < GlowLevels.Length; i++)
			{
				glowSurfaces[i] = host.CompositionDevice.CreateSurface((uint)trianglePiecePx, (uint)trianglePiecePx, Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
				glowVisuals[i].SetContent(glowSurfaces[i]);
			}
			IDCompositionVisual2[] array = dotVisuals;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetContent(dotSurface);
			}
			triangleLayer = d2DContext.CreateBitmap(new SizeI(trianglePiecePx, trianglePiecePx), IntPtr.Zero, 0u, bitmapProperties);
			dotLayer = d2DContext.CreateBitmap(new SizeI(dotPiecePx, dotPiecePx), IntPtr.Zero, 0u, bitmapProperties);
			pieceShadow = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
			tintSurface = host.CompositionDevice.CreateSurface((uint)Math.Ceiling((double)(LabelMaxWidth + 16f + 4f) * scale), (uint)Math.Ceiling(40.0 * scale), Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			bubbleBody.SetContent(tintSurface);
			array = barVisuals;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetContent(tintSurface);
			}
			labelMarginPx = (int)Math.Ceiling(2.0 * scale);
			trianglePiecesDrawn = false;
			dotPieceDrawn = false;
			tintSurfaceDrawn = false;
			ringAtlasDrawn = false;
			if ((object)tintBrush == null)
			{
				tintBrush = d2DContext.CreateSolidColorBrush(tint, null);
			}
			if ((object)whiteBrush == null)
			{
				whiteBrush = d2DContext.CreateSolidColorBrush(new Color4(1f, 1f, 1f), null);
			}
			if ((object)trianglePath == null)
			{
				trianglePath = CreateRoundedTrianglePath(host.D2DFactory);
			}
			if ((object)bubbleTextFormat == null)
			{
				bubbleTextFormat = CreateBubbleTextFormat(host.DWriteFactory);
			}
		}
	}

	public void Render(in CursorClusterPose pose)
	{
		if ((object)ringAtlas != null)
		{
			RenderCount++;
			if (PiecesNeedDrawing)
			{
				DrawPieces();
			}
			pose.CopyTo(ref lastPose);
			host.RequestCommit();
		}
	}

	private void DrawCluster(in CursorClusterPose pose)
	{
		ID2D1DeviceContext d2DContext = host.D2DContext;
		float num = (float)scale;
		Matrix3x2 matrix3x = Matrix3x2.CreateScale(num) * Matrix3x2.CreateTranslation(Anchor * num);
		d2DContext.Target = bodyLayer;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		double num2 = 0.0;
		double num3 = 8.0;
		if (pose.TriangleOpacity > 0.001)
		{
			d2DContext.Transform = Matrix3x2.CreateScale((float)pose.TriangleScale) * Matrix3x2.CreateRotation((float)(pose.TriangleRotationDegrees * Math.PI / 180.0)) * matrix3x;
			tintBrush.Opacity = (float)pose.TriangleOpacity;
			d2DContext.FillGeometry(trianglePath, tintBrush);
			num2 = 1.0;
			num3 = pose.TriangleGlowRadius * pose.TriangleScale;
		}
		if (pose.DwellRingOpacity > 0.001)
		{
			d2DContext.Transform = matrix3x;
			float num4 = 6.75f;
			tintBrush.Opacity = (float)(0.22 * pose.DwellRingOpacity);
			d2DContext.DrawEllipse(new Ellipse(Vector2.Zero, num4, num4), tintBrush, 2.5f);
			double num5 = Math.Clamp(pose.DwellRingProgress, 0.0, 1.0);
			if (num5 > 0.001)
			{
				tintBrush.Opacity = (float)pose.DwellRingOpacity;
				using ID2D1PathGeometry geometry = CreateArc(host.D2DFactory, num4, num5);
				if ((object)dwellRingStroke == null)
				{
					dwellRingStroke = host.D2DFactory.CreateStrokeStyle(new StrokeStyleProperties
					{
						StartCap = CapStyle.Round,
						EndCap = CapStyle.Round
					});
				}
				d2DContext.DrawGeometry(geometry, tintBrush, 2.5f, dwellRingStroke);
			}
			num2 = Math.Max(num2, 0.6);
			num3 = 6.0;
		}
		if (pose.StopSquareOpacity > 0.001)
		{
			d2DContext.Transform = matrix3x;
			tintBrush.Opacity = (float)pose.StopSquareOpacity;
			d2DContext.FillRoundedRectangle(new RoundedRectangle(new RawRectF(-3.5f, -3.5f, 3.5f, 3.5f), 1.5f, 1.5f), tintBrush);
			num2 = Math.Max(num2, 0.6);
			num3 = 6.0;
		}
		else if (pose.WaveformOpacity > 0.001)
		{
			d2DContext.Transform = Matrix3x2.CreateScale((float)Math.Max(0.1, pose.WaveformScale)) * matrix3x;
			tintBrush.Opacity = (float)pose.WaveformOpacity;
			float num6 = 18f;
			for (int i = 0; i < 5; i++)
			{
				float num7 = pose.WaveformBarHeights[i];
				float num8 = (0f - num6) / 2f + (float)i * 4f;
				RawRectF rawRectF = new RawRectF(num8, (0f - num7) / 2f, num8 + 2f, num7 / 2f);
				float num9 = Math.Min(1.5f, 1f);
				d2DContext.FillRoundedRectangle(new RoundedRectangle(rawRectF, num9, num9), tintBrush);
			}
			num2 = Math.Max(num2, 0.6);
			num3 = ((pose.TriangleOpacity > 0.001) ? num3 : 6.0);
		}
		if (pose.DotsOpacity > 0.001 && pose.DotCount > 0)
		{
			tintBrush.Opacity = (float)pose.DotsOpacity;
			for (int j = 0; j < pose.DotCount; j++)
			{
				d2DContext.Transform = Matrix3x2.CreateScale(1f, (float)pose.DotAcrossScale[j]) * Matrix3x2.CreateRotation((float)pose.DotAngles[j]) * Matrix3x2.CreateTranslation(pose.DotCenters[j]) * matrix3x;
				float num10 = (float)pose.DotRadii[j];
				d2DContext.FillEllipse(new Ellipse(Vector2.Zero, num10, num10), tintBrush);
			}
			num2 = 1.0;
		}
		tintBrush.Opacity = 1f;
		d2DContext.Transform = Matrix3x2.Identity;
		d2DContext.EndDraw();
		bool num11 = pose.BubbleOpacity > 0.001 && pose.BubbleText.Length > 0;
		Vector2 vector = default(Vector2);
		Matrix3x2 transform = default(Matrix3x2);
		if (num11)
		{
			TextMetrics metrics = GetBubbleLayout(pose.BubbleText).Metrics;
			vector = new Vector2(metrics.WidthIncludingTrailingWhitespace + 16f, metrics.Height + 8f);
			Vector2 position = new Vector2(10f + vector.X / 2f, 18f);
			transform = Matrix3x2.CreateScale((float)pose.BubbleScale, (float)pose.BubbleScale) * Matrix3x2.CreateTranslation(position) * matrix3x;
			d2DContext.Target = bubbleLayer;
			d2DContext.BeginDraw();
			d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
			d2DContext.Transform = transform;
			RawRectF rawRectF2 = new RawRectF((0f - vector.X) / 2f, (0f - vector.Y) / 2f, vector.X / 2f, vector.Y / 2f);
			d2DContext.FillRoundedRectangle(new RoundedRectangle(rawRectF2, 6f, 6f), tintBrush);
			d2DContext.Transform = Matrix3x2.Identity;
			d2DContext.EndDraw();
		}
		d2DContext.Target = clusterLayer;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		if (num2 > 0.0)
		{
			ConfigureShadow(bodyShadow, num3 * scale, num2);
			d2DContext.DrawImage(bodyShadow.Output, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
			d2DContext.DrawImage(bodyLayer, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
		}
		if (num11)
		{
			double num12 = 1.0 - pose.BubbleScale;
			ConfigureShadow(bubbleShadow, (6.0 + num12 * 16.0) * scale, Math.Min(1.0, 0.5 + num12 * 1.0));
			d2DContext.PushLayer(new LayerParameters1
			{
				ContentBounds = new RawRectF(0f, 0f, pixelWidth, pixelHeight),
				Opacity = (float)pose.BubbleOpacity,
				MaskTransform = Matrix3x2.Identity
			}, null);
			d2DContext.DrawImage(bubbleShadow.Output, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
			d2DContext.DrawImage(bubbleLayer, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
			d2DContext.Transform = transform;
			d2DContext.DrawTextLayout(new Vector2((0f - vector.X) / 2f + 8f, (0f - vector.Y) / 2f + 4f), bubbleTextLayout, whiteBrush, DrawTextOptions.None);
			d2DContext.Transform = Matrix3x2.Identity;
			d2DContext.PopLayer();
		}
		d2DContext.EndDraw();
		d2DContext.Target = null;
	}

	public void SnapshotPng(string path, Color4 background)
	{
		if ((object)clusterLayer != null)
		{
			DrawCluster(in lastPose);
			OverlayDebug.SavePng(host, clusterLayer, path, background);
		}
	}

	private unsafe void ConfigureShadow(ID2D1Effect shadow, double radiusPx, double opacity)
	{
		float num = (float)Math.Max(0.01, radiusPx / 2.0);
		shadow.SetValue(0u, PropertyType.Float, &num, 4u);
		Vector4 vector = new Vector4(tint.R, tint.G, tint.B, (float)Math.Clamp(opacity, 0.0, 1.0));
		shadow.SetValue(1u, PropertyType.Vector4, &vector, (uint)sizeof(Vector4));
	}

	private IDWriteTextLayout GetBubbleLayout(string text)
	{
		if ((object)bubbleTextLayout != null && bubbleTextLayoutText == text)
		{
			return bubbleTextLayout;
		}
		bubbleTextLayout?.Dispose();
		bubbleTextLayout = host.DWriteFactory.CreateTextLayout(text, bubbleTextFormat, 480f - Anchor.X - 10f - 16f - 12f, 100f);
		bubbleTextLayoutText = text;
		return bubbleTextLayout;
	}

	private static IDWriteTextFormat CreateBubbleTextFormat(IDWriteFactory factory)
	{
		string fontFamilyName = "Segoe UI";
		using (IDWriteFontCollection iDWriteFontCollection = factory.GetSystemFontCollection(false))
		{
			if ((bool)iDWriteFontCollection.FindFamilyName("Segoe UI Variable Text", out var _))
			{
				fontFamilyName = "Segoe UI Variable Text";
			}
		}
		IDWriteTextFormat iDWriteTextFormat = factory.CreateTextFormat(fontFamilyName, null, FontWeight.Medium, FontStyle.Normal, FontStretch.Normal, 11f, "en-us");
		iDWriteTextFormat.WordWrapping = WordWrapping.NoWrap;
		iDWriteTextFormat.SetTrimming(new Trimming
		{
			Granularity = TrimmingGranularity.Character
		}, null);
		return iDWriteTextFormat;
	}

	private static ID2D1PathGeometry CreateRoundedTrianglePath(ID2D1Factory1 factory)
	{
		float num = 16f;
		float num2 = num * MathF.Sqrt(3f) / 2f;
		Vector2[] array = new Vector2[3]
		{
			new Vector2(0f, (0f - num2) / 1.5f),
			new Vector2((0f - num) / 2f, num2 / 3f),
			new Vector2(num / 2f, num2 / 3f)
		};
		float num3 = num * 0.09f;
		float num4 = num3 * MathF.Sqrt(3f);
		ID2D1PathGeometry1 iD2D1PathGeometry = factory.CreatePathGeometry();
		using ID2D1GeometrySink iD2D1GeometrySink = iD2D1PathGeometry.Open();
		Vector2 startPoint = (array[0] + array[1]) / 2f;
		iD2D1GeometrySink.BeginFigure(startPoint, FigureBegin.Filled);
		for (int i = 1; i <= 3; i++)
		{
			Vector2 vector = array[i % 3];
			Vector2 vector2 = array[(i - 1) % 3];
			Vector2 vector3 = array[(i + 1) % 3];
			Vector2 point = vector + Vector2.Normalize(vector2 - vector) * num4;
			Vector2 point2 = vector + Vector2.Normalize(vector3 - vector) * num4;
			iD2D1GeometrySink.AddLine(point);
			iD2D1GeometrySink.AddArc(new ArcSegment(point2, new Size(num3, num3), 0f, SweepDirection.CounterClockwise, ArcSize.Small));
		}
		iD2D1GeometrySink.EndFigure(FigureEnd.Closed);
		iD2D1GeometrySink.Close();
		return iD2D1PathGeometry;
	}

	private void ReleaseScaledResources()
	{
		bodyShadow?.Dispose();
		bodyShadow = null;
		bubbleShadow?.Dispose();
		bubbleShadow = null;
		bodyLayer?.Dispose();
		bodyLayer = null;
		bubbleLayer?.Dispose();
		bubbleLayer = null;
		clusterLayer?.Dispose();
		clusterLayer = null;
		ringVisual.SetContent(null);
		ringAtlas?.Dispose();
		ringAtlas = null;
		ringLayer?.Dispose();
		ringLayer = null;
		pieceShadow?.Dispose();
		pieceShadow = null;
		triangleLayer?.Dispose();
		triangleLayer = null;
		dotLayer?.Dispose();
		dotLayer = null;
		triangleSurface?.Dispose();
		triangleSurface = null;
		for (int i = 0; i < GlowLevels.Length; i++)
		{
			glowSurfaces[i]?.Dispose();
			glowSurfaces[i] = null;
		}
		dotSurface?.Dispose();
		dotSurface = null;
		ReleaseLabels();
		bubbleBody.SetContent(null);
		IDCompositionVisual2[] array = barVisuals;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].SetContent(null);
		}
		tintSurface?.Dispose();
		tintSurface = null;
	}

	public void Dispose()
	{
		ReleaseScaledResources();
		bubbleGlow.SetEffect(null);
		waveGlow.SetEffect(null);
		bubbleGlowEffect?.Dispose();
		bubbleShadowEffect?.Dispose();
		waveGlowEffect?.Dispose();
		waveShadowEffect?.Dispose();
		effects?.Dispose();
		bubbleTextVisual.Dispose();
		bubbleBodyClip.Dispose();
		bubbleBody.Dispose();
		bubbleGlow.Dispose();
		bubbleRoot3?.Dispose();
		bubbleRoot.Dispose();
		IDCompositionRectangleClip[] array = barClips;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose();
		}
		IDCompositionVisual2[] array2 = barVisuals;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Dispose();
		}
		waveGlow.Dispose();
		waveRoot3?.Dispose();
		waveRoot.Dispose();
		IDCompositionVisual3[] array3 = dotVisuals3;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i]?.Dispose();
		}
		array2 = dotVisuals;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Dispose();
		}
		triangleVisual3?.Dispose();
		triangleVisual.Dispose();
		array3 = glowVisuals3;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i]?.Dispose();
		}
		array2 = glowVisuals;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Dispose();
		}
		ringVisual3?.Dispose();
		ringVisual.Dispose();
		trianglePath?.Dispose();
		dwellRingStroke?.Dispose();
		tintBrush?.Dispose();
		whiteBrush?.Dispose();
		bubbleTextLayout?.Dispose();
		bubbleTextFormat?.Dispose();
	}
}
