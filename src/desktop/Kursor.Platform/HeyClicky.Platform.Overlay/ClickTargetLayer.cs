using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using HeyClicky.Platform.Win32;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.DirectComposition;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

internal sealed class ClickTargetLayer : IOverlayLayer, IDisposable
{
	private const float OutlinePadding = 5f;

	private const float MinimumRingSize = 20f;

	private const float ControlCornerRadius = 12f;

	private const float StrokeWidth = 2.5f;

	private const float DashOn = 7f;

	private const float DashOff = 5f;

	private const float GlowRadius = 12f;

	private const float FillOpacity = 0.08f;

	private const float StrokeOpacity = 0.95f;

	private const float GlowOpacity = 0.65f;

	private const float PulseGap = 9f;

	private const float LabelGapAboveOutline = 24f;

	private const float PulseRestingOpacity = 0.72f;

	private const float PulsePeakOpacity = 0.28f;

	private const float PulsePeakScale = 1.16f;

	private const float PulseControlGrowthPerSide = 8f;

	private const double PulseHalfCycleSeconds = 0.9;

	private const float PulseLineWidth = 2f;

	private const float LabelFontSize = 12f;

	private const float LabelPaddingX = 11f;

	private const float LabelPaddingY = 5f;

	private const double SpringResponse = 0.42;

	private const double SpringDamping = 0.78;

	private const double LabelShowsAtProgress = 0.72;

	private const double PulseStartsAtProgress = 0.82;

	private readonly OverlayHost host;

	private readonly IDCompositionVisual2 container;

	private readonly IDCompositionVisual3? container3;

	private readonly IDCompositionVisual2 ringVisual;

	private readonly IDCompositionVisual2 pulseVisual;

	private readonly IDCompositionVisual2 labelVisual;

	private readonly IDCompositionScaleTransform ringScale;

	private readonly IDCompositionScaleTransform pulseScale;

	private readonly IDCompositionEffectGroup pulseEffect;

	private readonly IDCompositionEffectGroup labelEffect;

	private IDCompositionSurface? ringSurface;

	private IDCompositionSurface? pulseSurface;

	private IDCompositionSurface? labelSurface;

	private IDCompositionAnimation? pulseScaleXAnimation;

	private IDCompositionAnimation? pulseScaleYAnimation;

	private IDCompositionAnimation? pulseOpacityAnimation;

	private IDWriteTextFormat? labelFormat;

	private double labelFormatScale;

	private Color4 tint = new Color4(0.2f, 0.5019608f, 1f);

	private string tintHex = "#3380FF";

	private ClickTargetRing? ring;

	private double scale = 1.0;

	private Vector2 outlineSize;

	private bool isControlOutline;

	private double revealStartTime = -1.0;

	private double revealProgress;

	private bool pulseStarted;

	private bool labelShown;

	private bool layerVisible;

	private bool revealAwaitsGpu;

	public long RenderCount;

	private static readonly BitmapProperties1 TargetBitmap = new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target);

	public int IdleWakeIntervalMs => -1;

	public bool WantsMouseMoveWake => false;

	public bool IsShown => (object)ring != null;

	public double RevealProgress => revealProgress;

	public bool IsPulsing => pulseStarted;

	public ClickTargetRing? Ring => ring;

	private float OutlineCornerRadius
	{
		get
		{
			if (!isControlOutline)
			{
				return outlineSize.Y / 2f;
			}
			return Math.Min(12f, outlineSize.Y / 2f);
		}
	}

	public ClickTargetLayer(OverlayHost host)
	{
		this.host = host;
		IDCompositionDesktopDevice compositionDevice = host.CompositionDevice;
		container = compositionDevice.CreateVisual();
		container3 = container.QueryInterfaceOrNull<IDCompositionVisual3>();
		ringVisual = compositionDevice.CreateVisual();
		pulseVisual = compositionDevice.CreateVisual();
		labelVisual = compositionDevice.CreateVisual();
		ringScale = compositionDevice.CreateScaleTransform();
		pulseScale = compositionDevice.CreateScaleTransform();
		pulseEffect = compositionDevice.CreateEffectGroup();
		labelEffect = compositionDevice.CreateEffectGroup();
		ringVisual.SetTransform(ringScale);
		pulseVisual.SetTransform(pulseScale);
		pulseVisual.SetEffect(pulseEffect);
		labelVisual.SetEffect(labelEffect);
		container.AddVisual(pulseVisual, false, null);
		container.AddVisual(ringVisual, true, pulseVisual);
		container.AddVisual(labelVisual, true, ringVisual);
		host.RootVisual.AddVisual(container, false, null);
		container3?.SetVisible(false);
	}

	public void SetTint(Color4 color, string hex)
	{
		tint = color;
		tintHex = hex.ToUpperInvariant();
		ClickTargetRing clickTargetRing = ring;
		if ((object)clickTargetRing != null)
		{
			Show(clickTargetRing, restartReveal: false);
		}
	}

	public void Show(ClickTargetRing target, bool restartReveal = true)
	{
		ring = target;
		DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(target.CenterPx, host.Displays);
		scale = Math.Max(0.5, displayInfo.Scale);
		isControlOutline = target.ControlFramePx.HasValue;
		RectD? controlFramePx = target.ControlFramePx;
		Vector2 vector;
		if (controlFramePx.HasValue)
		{
			RectD valueOrDefault = controlFramePx.GetValueOrDefault();
			vector = new Vector2((float)(valueOrDefault.Width / scale) + 10f, (float)(valueOrDefault.Height / scale) + 10f);
		}
		else
		{
			vector = new Vector2(Math.Max(20f, (float)(target.RadiusPx / scale) * 2f), Math.Max(20f, (float)(target.RadiusPx / scale) * 2f));
		}
		outlineSize = vector;
		Vector2 centerPx = new Vector2((float)(target.CenterPx.X - host.VirtualScreenPx.X), (float)(target.CenterPx.Y - host.VirtualScreenPx.Y));
		RenderRing(centerPx);
		RenderPulse(centerPx);
		RenderLabel(centerPx, target.Label);
		if (restartReveal)
		{
			StopPulse();
			revealStartTime = Now();
			revealAwaitsGpu = true;
			revealProgress = 0.001;
			labelShown = false;
			labelEffect.SetOpacity(0f);
			ApplyRevealScale();
		}
		if (!layerVisible)
		{
			layerVisible = true;
			container3?.SetVisible(true);
			host.SetLayerVisible(visible: true);
			host.BringToTop();
		}
		host.RequestCommit();
		host.RequestFrames();
	}

	public void Hide()
	{
		ring = null;
		revealStartTime = -1.0;
		StopPulse();
		if (layerVisible)
		{
			layerVisible = false;
			container3?.SetVisible(false);
			host.SetLayerVisible(visible: false);
			host.RequestCommit();
		}
	}

	public bool OnIdleWake(in OverlayFrame frame)
	{
		if ((object)ring != null)
		{
			return revealStartTime >= 0.0;
		}
		return false;
	}

	public bool OnFrame(in OverlayFrame frame)
	{
		if ((object)ring == null || revealStartTime < 0.0)
		{
			return false;
		}
		if (revealAwaitsGpu)
		{
			if (!host.GpuCaughtUp && frame.TimeSeconds - revealStartTime < 0.25)
			{
				return true;
			}
			revealAwaitsGpu = false;
			revealStartTime = frame.TimeSeconds;
		}
		double num = frame.TimeSeconds - revealStartTime;
		revealProgress = Math.Max(0.001, Spring(num));
		ApplyRevealScale();
		if (!labelShown && revealProgress > 0.72)
		{
			labelShown = true;
			labelEffect.SetOpacity(1f);
		}
		if (!pulseStarted && revealProgress > 0.82)
		{
			StartPulse();
		}
		host.RequestCommit();
		int num2;
		if (num > 0.3)
		{
			num2 = ((Math.Abs(1.0 - revealProgress) < 0.002) ? 1 : 0);
			if (num2 != 0)
			{
				revealProgress = 1.0;
				ApplyRevealScale();
				revealStartTime = -1.0;
				if (!labelShown)
				{
					labelShown = true;
					labelEffect.SetOpacity(1f);
				}
				if (!pulseStarted)
				{
					StartPulse();
				}
			}
		}
		else
		{
			num2 = 0;
		}
		return num2 == 0;
	}

	private static double Spring(double t)
	{
		if (t <= 0.0)
		{
			return 0.0;
		}
		double num = 14.959965017094254;
		double num2 = 0.78;
		double num3 = num * Math.Sqrt(1.0 - num2 * num2);
		return 1.0 - Math.Exp((0.0 - num2) * num * t) * (Math.Cos(num3 * t) + num2 * num / num3 * Math.Sin(num3 * t));
	}

	private void ApplyRevealScale()
	{
		float num = (float)revealProgress;
		ringScale.SetScaleX(num);
		ringScale.SetScaleY(num);
	}

	private void StartPulse()
	{
		pulseStarted = true;
		float to;
		float to2;
		if (isControlOutline)
		{
			Vector2 vector = outlineSize + new Vector2(18f);
			to = (vector.X + 16f) / vector.X;
			to2 = (vector.Y + 16f) / vector.Y;
		}
		else
		{
			to = (to2 = 1.16f);
		}
		DisposePulseAnimations();
		pulseScaleXAnimation = Breathing(1f, to);
		pulseScaleYAnimation = Breathing(1f, to2);
		pulseOpacityAnimation = Breathing(0.72f, 0.28f);
		pulseScale.SetScaleX(pulseScaleXAnimation);
		pulseScale.SetScaleY(pulseScaleYAnimation);
		pulseEffect.SetOpacity(pulseOpacityAnimation);
		host.RequestCommit();
	}

	private void StopPulse()
	{
		pulseStarted = false;
		pulseScale.SetScaleX(1f);
		pulseScale.SetScaleY(1f);
		pulseEffect.SetOpacity(0f);
		DisposePulseAnimations();
	}

	private IDCompositionAnimation Breathing(float from, float to)
	{
		IDCompositionAnimation iDCompositionAnimation = host.CompositionDevice.CreateAnimation();
		double num = 0.9;
		float num2 = to - from;
		iDCompositionAnimation.AddCubic(0.0, from, 0f, (float)((double)(3f * num2) / (num * num)), (float)((double)(-2f * num2) / (num * num * num)));
		iDCompositionAnimation.AddCubic(num, to, 0f, (float)((double)(-3f * num2) / (num * num)), (float)((double)(2f * num2) / (num * num * num)));
		iDCompositionAnimation.AddRepeat(2.0 * num, 2.0 * num);
		return iDCompositionAnimation;
	}

	private void DisposePulseAnimations()
	{
		pulseScaleXAnimation?.Dispose();
		pulseScaleXAnimation = null;
		pulseScaleYAnimation?.Dispose();
		pulseScaleYAnimation = null;
		pulseOpacityAnimation?.Dispose();
		pulseOpacityAnimation = null;
	}

	private void RenderRing(Vector2 centerPx)
	{
		float num = (float)scale;
		float num2 = 26.5f * num;
		SizeI size = new SizeI((int)Math.Ceiling(outlineSize.X * num + num2 * 2f), (int)Math.Ceiling(outlineSize.Y * num + num2 * 2f));
		ringSurface = Resized(ringSurface, ringVisual, size);
		using ID2D1Bitmap1 image = DrawRingBitmap(size);
		CopyToSurface(ringSurface, image);
		Place(ringVisual, ringScale, centerPx, size);
	}

	private ID2D1Bitmap1 DrawRingBitmap(SizeI size)
	{
		ID2D1DeviceContext d2DContext = host.D2DContext;
		float num = (float)scale;
		RectangleF rect = new RectangleF((float)size.Width / 2f - outlineSize.X * num / 2f, (float)size.Height / 2f - outlineSize.Y * num / 2f, outlineSize.X * num, outlineSize.Y * num);
		RoundedRectangle roundedRect = new RoundedRectangle(rect, OutlineCornerRadius * num, OutlineCornerRadius * num);
		using ID2D1Bitmap1 iD2D1Bitmap = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, TargetBitmap);
		using ID2D1StrokeStyle strokeStyle = host.D2DFactory.CreateStrokeStyle(new StrokeStyleProperties
		{
			DashStyle = DashStyle.Custom,
			StartCap = CapStyle.Flat,
			EndCap = CapStyle.Flat,
			DashCap = CapStyle.Flat,
			LineJoin = LineJoin.Round
		}, new float[2] { 2.8f, 2f });
		using ID2D1SolidColorBrush iD2D1SolidColorBrush = d2DContext.CreateSolidColorBrush(WithAlpha(tint, 0.95f), null);
		d2DContext.Target = iD2D1Bitmap;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		d2DContext.DrawRoundedRectangle(roundedRect, iD2D1SolidColorBrush, 2.5f * num, strokeStyle);
		d2DContext.EndDraw();
		ID2D1Bitmap1 iD2D1Bitmap2 = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, TargetBitmap);
		using ID2D1Effect iD2D1Effect = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
		iD2D1Effect.SetInput(0u, iD2D1Bitmap, true);
		ConfigureShadow(iD2D1Effect, 12f * num, WithAlpha(tint, 0.65f));
		d2DContext.Target = iD2D1Bitmap2;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		iD2D1SolidColorBrush.Color = WithAlpha(tint, 0.08f);
		d2DContext.FillRoundedRectangle(roundedRect, iD2D1SolidColorBrush);
		d2DContext.DrawImage(iD2D1Effect.Output, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.DrawImage(iD2D1Bitmap, Vector2.Zero, null, InterpolationMode.NearestNeighbor, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.EndDraw();
		d2DContext.Target = null;
		RenderCount++;
		return iD2D1Bitmap2;
	}

	private void RenderPulse(Vector2 centerPx)
	{
		float num = (float)scale;
		Vector2 vector = outlineSize + new Vector2(18f);
		float num2 = (float)Math.Ceiling(Math.Max(vector.X, vector.Y) * 0.15999997f / 2f + 8f + 4f) * num;
		SizeI size = new SizeI((int)Math.Ceiling(vector.X * num + num2 * 2f), (int)Math.Ceiling(vector.Y * num + num2 * 2f));
		pulseSurface = Resized(pulseSurface, pulseVisual, size);
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(pulseSurface, null, out offset);
		try
		{
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			RectangleF rect = new RectangleF((float)offset.X + (float)size.Width / 2f - vector.X * num / 2f, (float)offset.Y + (float)size.Height / 2f - vector.Y * num / 2f, vector.X * num, vector.Y * num);
			float num3 = (OutlineCornerRadius + 9f) * num;
			using ID2D1SolidColorBrush brush = iD2D1DeviceContext.CreateSolidColorBrush(tint, null);
			iD2D1DeviceContext.DrawRoundedRectangle(new RoundedRectangle(rect, num3, num3), brush, 2f * num);
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			pulseSurface.EndDraw();
		}
		Place(pulseVisual, pulseScale, centerPx, size);
	}

	private void RenderLabel(Vector2 centerPx, string? label)
	{
		if (string.IsNullOrWhiteSpace(label))
		{
			labelVisual.SetContent(null);
			labelSurface?.Dispose();
			labelSurface = null;
			return;
		}
		float num = (float)scale;
		Vector2 pillSize;
		float shadowMargin;
		using ID2D1Bitmap1 iD2D1Bitmap = DrawLabelBitmap(label, out pillSize, out shadowMargin);
		SizeI pixelSize = iD2D1Bitmap.PixelSize;
		labelSurface = Resized(labelSurface, labelVisual, pixelSize);
		CopyToSurface(labelSurface, iD2D1Bitmap);
		float num2 = centerPx.Y - (outlineSize.Y / 2f + 24f) * num;
		labelVisual.SetOffsetX((float)Math.Round(centerPx.X - (float)pixelSize.Width / 2f));
		labelVisual.SetOffsetY((float)Math.Round(num2 - pillSize.Y / 2f - shadowMargin));
	}

	private ID2D1Bitmap1 DrawLabelBitmap(string label, out Vector2 pillSize, out float shadowMargin)
	{
		ID2D1DeviceContext d2DContext = host.D2DContext;
		float num = (float)scale;
		if ((object)labelFormat == null || Math.Abs(labelFormatScale - scale) > 0.001)
		{
			labelFormat?.Dispose();
			labelFormat = CreateLabelFormat(host.DWriteFactory, 12f * num);
			labelFormatScale = scale;
		}
		using IDWriteTextLayout iDWriteTextLayout = host.DWriteFactory.CreateTextLayout(label, labelFormat, 4000f, 400f);
		TextMetrics metrics = iDWriteTextLayout.Metrics;
		pillSize = new Vector2((float)Math.Ceiling(metrics.WidthIncludingTrailingWhitespace + 22f * num), (float)Math.Ceiling(metrics.Height + 10f * num));
		shadowMargin = (float)Math.Ceiling(12f * num);
		SizeI size = new SizeI((int)(pillSize.X + shadowMargin * 2f), (int)(pillSize.Y + shadowMargin * 2f));
		RoundedRectangle roundedRect = new RoundedRectangle(new RectangleF(shadowMargin, shadowMargin, pillSize.X, pillSize.Y), pillSize.Y / 2f, pillSize.Y / 2f);
		using ID2D1Bitmap1 iD2D1Bitmap = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, TargetBitmap);
		using (ID2D1LinearGradientBrush brush = CreateGelGradient(d2DContext, roundedRect.Rect))
		{
			using ID2D1SolidColorBrush brush2 = d2DContext.CreateSolidColorBrush(GelStrokeColor(), null);
			using ID2D1SolidColorBrush defaultFillBrush2 = d2DContext.CreateSolidColorBrush(new Color4(0f, 0f, 0f), null);
			using ID2D1SolidColorBrush defaultFillBrush = d2DContext.CreateSolidColorBrush(new Color4(1f, 1f, 1f, 0.8f), null);
			d2DContext.Target = iD2D1Bitmap;
			d2DContext.BeginDraw();
			d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
			d2DContext.FillRoundedRectangle(roundedRect, brush);
			d2DContext.DrawRoundedRectangle(roundedRect, brush2, 1f * num);
			Vector2 vector = new Vector2(shadowMargin + 11f * num, shadowMargin + 5f * num);
			d2DContext.DrawTextLayout(vector + new Vector2(0f, 1.5f * num), iDWriteTextLayout, defaultFillBrush, DrawTextOptions.None);
			d2DContext.DrawTextLayout(vector, iDWriteTextLayout, defaultFillBrush2, DrawTextOptions.None);
			d2DContext.EndDraw();
		}
		ID2D1Bitmap1 iD2D1Bitmap2 = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, TargetBitmap);
		using ID2D1Effect iD2D1Effect = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
		iD2D1Effect.SetInput(0u, iD2D1Bitmap, true);
		ConfigureShadow(iD2D1Effect, 5f * num, new Color4(0f, 0f, 0f, 0.22f));
		d2DContext.Target = iD2D1Bitmap2;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		d2DContext.DrawImage(iD2D1Effect.Output, new Vector2(0f, 2f * num), null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.DrawImage(iD2D1Bitmap, Vector2.Zero, null, InterpolationMode.NearestNeighbor, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.EndDraw();
		d2DContext.Target = null;
		return iD2D1Bitmap2;
	}

	private ID2D1LinearGradientBrush CreateGelGradient(ID2D1DeviceContext context, RectangleF pill)
	{
		Color4? color = PastelTint();
		object obj;
		if (!color.HasValue)
		{
			obj = new GradientStop[5]
			{
				new GradientStop(0f, Hex(15594751u)),
				new GradientStop(0.18f, Hex(14084095u)),
				new GradientStop(0.5f, Hex(10668031u)),
				new GradientStop(0.8f, Hex(10079224u)),
				new GradientStop(1f, Hex(10670839u))
			};
		}
		else
		{
			Color4 color2 = color.GetValueOrDefault();
			obj = new GradientStop[5]
			{
				new GradientStop(0f, Blend(color2, 0.7f)),
				new GradientStop(0.18f, Blend(color2, 0.3f)),
				new GradientStop(0.5f, in color2),
				new GradientStop(0.82f, in color2),
				new GradientStop(1f, Blend(color2, 0.1f))
			};
		}
		GradientStop[] gradientStops = (GradientStop[])obj;
		using ID2D1GradientStopCollection gradientStopCollection = context.CreateGradientStopCollection(gradientStops);
		return context.CreateLinearGradientBrush(new LinearGradientBrushProperties(new Vector2(0f, pill.Top), new Vector2(0f, pill.Bottom)), gradientStopCollection);
	}

	private Color4 GelStrokeColor()
	{
		if (PastelTint().HasValue)
		{
			return new Color4(0f, 0f, 0f, 0.22f);
		}
		return Hex(2234520u);
	}

	private Color4? PastelTint()
	{
		return tintHex switch
		{
			"#FF4C4C" => Hex(16758960u), 
			"#D4A017" => Hex(16768906u), 
			"#26C281" => Hex(13234383u), 
			_ => null, 
		};
	}

	private static Color4 Blend(Color4 color, float whiteFraction)
	{
		return new Color4(color.R + (1f - color.R) * whiteFraction, color.G + (1f - color.G) * whiteFraction, color.B + (1f - color.B) * whiteFraction);
	}

	private static Color4 Hex(uint rgb)
	{
		return new Color4((float)((rgb >> 16) & 0xFF) / 255f, (float)((rgb >> 8) & 0xFF) / 255f, (float)(rgb & 0xFF) / 255f);
	}

	private static Color4 WithAlpha(Color4 color, float alpha)
	{
		return new Color4(color.R, color.G, color.B, alpha);
	}

	private static IDWriteTextFormat CreateLabelFormat(IDWriteFactory factory, float sizePx)
	{
		string fontFamilyName = "Segoe UI";
		using (IDWriteFontCollection iDWriteFontCollection = factory.GetSystemFontCollection(false))
		{
			if ((bool)iDWriteFontCollection.FindFamilyName("Segoe UI Variable Text", out var _))
			{
				fontFamilyName = "Segoe UI Variable Text";
			}
		}
		IDWriteTextFormat iDWriteTextFormat = factory.CreateTextFormat(fontFamilyName, null, FontWeight.DemiBold, FontStyle.Normal, FontStretch.Normal, sizePx, "en-us");
		iDWriteTextFormat.WordWrapping = WordWrapping.NoWrap;
		return iDWriteTextFormat;
	}

	private unsafe static void ConfigureShadow(ID2D1Effect shadow, double radiusPx, Color4 color)
	{
		float num = (float)Math.Max(0.01, radiusPx / 2.0);
		shadow.SetValue(0u, PropertyType.Float, &num, 4u);
		Vector4 vector = new Vector4(color.R, color.G, color.B, color.A);
		shadow.SetValue(1u, PropertyType.Vector4, &vector, (uint)sizeof(Vector4));
	}

	private IDCompositionSurface Resized(IDCompositionSurface? surface, IDCompositionVisual2 visual, SizeI size)
	{
		surface?.Dispose();
		IDCompositionSurface iDCompositionSurface = host.CompositionDevice.CreateSurface((uint)Math.Max(1, size.Width), (uint)Math.Max(1, size.Height), Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
		visual.SetContent(iDCompositionSurface);
		return iDCompositionSurface;
	}

	private static void CopyToSurface(IDCompositionSurface surface, ID2D1Bitmap1 image)
	{
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(surface, null, out offset);
		try
		{
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			iD2D1DeviceContext.DrawImage(image, new Vector2(offset.X, offset.Y), null, InterpolationMode.NearestNeighbor, Vortice.Direct2D1.CompositeMode.SourceCopy);
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			surface.EndDraw();
		}
	}

	private static void Place(IDCompositionVisual2 visual, IDCompositionScaleTransform transform, Vector2 centerPx, SizeI size)
	{
		float offsetX = (float)Math.Round(centerPx.X - (float)size.Width / 2f);
		float offsetY = (float)Math.Round(centerPx.Y - (float)size.Height / 2f);
		visual.SetOffsetX(offsetX);
		visual.SetOffsetY(offsetY);
		transform.SetCenterX((float)size.Width / 2f);
		transform.SetCenterY((float)size.Height / 2f);
	}

	public void SnapshotPng(string path, Color4 background)
	{
		ClickTargetRing clickTargetRing = ring;
		if ((object)clickTargetRing == null)
		{
			return;
		}
		float num = (float)scale;
		float num2 = 26.5f * num;
		SizeI size = new SizeI((int)Math.Ceiling(outlineSize.X * num + num2 * 2f), (int)Math.Ceiling(outlineSize.Y * num + num2 * 2f));
		using ID2D1Bitmap1 image = DrawRingBitmap(size);
		ID2D1Bitmap1 iD2D1Bitmap = null;
		Vector2 pillSize = default(Vector2);
		float shadowMargin = 0f;
		if (!string.IsNullOrWhiteSpace(clickTargetRing.Label))
		{
			iD2D1Bitmap = DrawLabelBitmap(clickTargetRing.Label, out pillSize, out shadowMargin);
		}
		float num3 = iD2D1Bitmap?.PixelSize.Height ?? 0;
		float num4 = iD2D1Bitmap?.PixelSize.Width ?? 0;
		float num5 = (outlineSize.Y / 2f + 24f) * num;
		float num6 = Math.Max(0f, num5 + num3 / 2f - (float)size.Height / 2f);
		SizeI size2 = new SizeI((int)Math.Max(size.Width, num4), (int)((float)size.Height + num6));
		ID2D1DeviceContext d2DContext = host.D2DContext;
		using ID2D1Bitmap1 iD2D1Bitmap2 = d2DContext.CreateBitmap(size2, IntPtr.Zero, 0u, TargetBitmap);
		d2DContext.Target = iD2D1Bitmap2;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		Vector2 vector = new Vector2((float)size2.Width / 2f, num6 + (float)size.Height / 2f);
		d2DContext.DrawImage(image, vector - new Vector2((float)size.Width / 2f, (float)size.Height / 2f), null, InterpolationMode.NearestNeighbor, Vortice.Direct2D1.CompositeMode.SourceOver);
		if ((object)iD2D1Bitmap != null)
		{
			d2DContext.DrawImage(iD2D1Bitmap, new Vector2((float)size2.Width / 2f - num4 / 2f, vector.Y - num5 - pillSize.Y / 2f - shadowMargin), null, InterpolationMode.NearestNeighbor, Vortice.Direct2D1.CompositeMode.SourceOver);
		}
		d2DContext.EndDraw();
		d2DContext.Target = null;
		iD2D1Bitmap?.Dispose();
		OverlayDebug.SavePng(host, iD2D1Bitmap2, path, background);
	}

	private static double Now()
	{
		return (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
	}

	public void OnDisplaysChanged(IReadOnlyList<DisplayInfo> displays, RectD virtualScreenPx)
	{
		ClickTargetRing clickTargetRing = ring;
		if ((object)clickTargetRing != null)
		{
			Show(clickTargetRing, restartReveal: false);
		}
	}

	public void Dispose()
	{
		Hide();
		host.RootVisual.RemoveVisual(container);
		DisposePulseAnimations();
		ringSurface?.Dispose();
		pulseSurface?.Dispose();
		labelSurface?.Dispose();
		labelFormat?.Dispose();
		ringScale.Dispose();
		pulseScale.Dispose();
		pulseEffect.Dispose();
		labelEffect.Dispose();
		ringVisual.Dispose();
		pulseVisual.Dispose();
		labelVisual.Dispose();
		container3?.Dispose();
		container.Dispose();
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
