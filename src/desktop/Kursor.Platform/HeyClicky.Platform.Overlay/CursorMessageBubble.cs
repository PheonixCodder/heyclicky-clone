using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime;
using Vortice;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.DirectComposition;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

internal sealed class CursorMessageBubble : IDisposable
{
	private enum Kind
	{
		Message,
		Welcome
	}

	private enum Phase
	{
		None,
		Revealing,
		Holding,
		FadingOut
	}

	private sealed class Word
	{
		public required string Text;

		public float Width;

		public float X;

		public float Y;

		public double RevealedAt;
	}

	private const float MaxTextWidth = 300f;

	private const float PaddingX = 8f;

	private const float PaddingY = 4f;

	private const float CornerRadius = 6f;

	private const float FontSize = 11f;

	private const float LineHeight = 15f;

	private const float LineSpacing = 1f;

	private const float WordSpacing = 3f;

	private const float HostMaxHeight = 180f;

	private const float Margin = 20f;

	private const float SurfaceWidth = 356f;

	private const float SurfaceHeight = 220f;

	private const float LeftFromAnchor = 10f;

	private const float TopFromAnchor = 18f;

	private const float AnchorFromPointerX = 35f;

	private const float AnchorFromPointerY = 25f;

	private const float GapFromPointer = 12f;

	private const string WelcomeMessage = "hi, im heyclicky";

	private readonly OverlayHost host;

	private readonly IDCompositionVisual2 visual;

	private readonly IDCompositionVisual2 plateGlow;

	private readonly IDCompositionVisual2 plate;

	private readonly IDCompositionVisual2 textHolder;

	private readonly IDCompositionVisual3? visual3;

	private readonly IDCompositionRectangleClip plateClip;

	private readonly IDCompositionDevice3? effects;

	private readonly IDCompositionShadowEffect? glowShadow;

	private readonly IDCompositionCompositeEffect? glowEffect;

	private readonly List<IDCompositionVisual2> wordVisuals = new List<IDCompositionVisual2>();

	private readonly List<IDCompositionVisual3?> wordVisuals3 = new List<IDCompositionVisual3>();

	private int wordVisualsShown;

	private IDCompositionSurface? tintSurface;

	private IDCompositionSurface? textSurface;

	private bool tintDrawn;

	private bool textDrawn;

	private int textMarginPx;

	private double textDrawnAt;

	private float[] welcomeCarets = Array.Empty<float>();

	private ID2D1Bitmap1? plateLayer;

	private ID2D1Bitmap1? compositeLayer;

	private ID2D1Effect? glow;

	private ID2D1SolidColorBrush? tintBrush;

	private ID2D1SolidColorBrush? textBrush;

	private IDWriteTextFormat? format;

	private readonly Dictionary<string, float> wordWidths = new Dictionary<string, float>();

	private double scale;

	private int pixelWidth;

	private int pixelHeight;

	private Color4 tint = new Color4(0.2f, 0.5019608f, 1f);

	private Kind kind;

	private Phase phase;

	private readonly List<Word> allWords = new List<Word>();

	private readonly List<Word> words = new List<Word>();

	private List<string> pendingChunks = new List<string>();

	private int nextChunk;

	private double nextChunkTime;

	private double holdSeconds;

	private double holdUntil;

	private double fadeStart;

	private bool timelineStarted;

	private double fadeInSeconds;

	private double fadeOutSeconds = 0.3;

	private string welcomeText = "";

	private double opacity;

	private double opacityFrom;

	private double opacityTo;

	private double opacityStart;

	private double opacityDuration;

	private Func<double, double> opacityCurve = Easing.EaseIn;

	private double width;

	private double height;

	private double targetWidth;

	private double targetHeight;

	private CursorAdjacentPlacement.Sides sides;

	private TaskCompletionSource? completion;

	public long RenderCount;

	public bool IsShowing => phase != Phase.None;

	public bool IsFadingOut => phase == Phase.FadingOut;

	public double Opacity
	{
		get
		{
			if (phase != 0)
			{
				return opacity;
			}
			return 0.0;
		}
	}

	public string Text
	{
		get
		{
			if (kind != Kind.Welcome)
			{
				return string.Join(' ', words.Select((Word w) => w.Text));
			}
			return welcomeText;
		}
	}

	public CursorMessageBubble(OverlayHost host)
	{
		this.host = host;
		IDCompositionDesktopDevice compositionDevice = host.CompositionDevice;
		visual = compositionDevice.CreateVisual();
		visual3 = visual.QueryInterfaceOrNull<IDCompositionVisual3>();
		visual3?.SetOpacity(0f);
		plateGlow = compositionDevice.CreateVisual();
		plate = compositionDevice.CreateVisual();
		plateClip = compositionDevice.CreateRectangleClip();
		plate.SetClip(plateClip);
		plate.SetBorderMode(Vortice.DirectComposition.BorderMode.Soft);
		plateGlow.AddVisual(plate, false, null);
		textHolder = compositionDevice.CreateVisual();
		visual.AddVisual(plateGlow, false, null);
		visual.AddVisual(textHolder, true, plateGlow);
		effects = compositionDevice.QueryInterfaceOrNull<IDCompositionDevice3>();
		if ((object)effects != null)
		{
			effects.CreateShadowEffect(out glowShadow).CheckError();
			effects.CreateCompositeEffect(out glowEffect).CheckError();
			glowShadow.SetInput(0u, null, 0u);
			glowEffect.SetMode(Vortice.Direct2D1.CompositeMode.SourceOver);
			glowEffect.SetInput(0u, glowShadow, 0u);
			glowEffect.SetInput(1u, null, 0u);
			plateGlow.SetEffect(glowEffect);
		}
		host.RootVisual.AddVisual(visual, true, null);
	}

	public void SetTint(Color4 color)
	{
		tint = color;
		if ((object)tintBrush != null)
		{
			tintBrush.Color = color;
		}
		tintDrawn = false;
	}

	public void Show(string text, bool streamed, double hold, TaskCompletionSource done, double now, double fadeOut = 0.3)
	{
		Finish();
		completion = done;
		kind = Kind.Message;
		words.Clear();
		holdSeconds = Math.Max(0.0, hold);
		fadeOutSeconds = Math.Max(0.0, fadeOut);
		List<string> list;
		if (!streamed)
		{
			int num = 1;
			list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = text;
		}
		else
		{
			list = RevealChunks(text);
		}
		pendingChunks = list;
		LayOutAllWords();
		nextChunk = 0;
		phase = Phase.Revealing;
		fadeInSeconds = (streamed ? 0.4 : 0.25);
		BeginTimeline(now);
		width = (height = 0.0);
	}

	public void ShowWelcome(TaskCompletionSource done, double now)
	{
		Finish();
		completion = done;
		kind = Kind.Welcome;
		words.Clear();
		allWords.Clear();
		welcomeText = "";
		nextChunk = 0;
		holdSeconds = 2.0;
		phase = Phase.Revealing;
		fadeInSeconds = 0.4;
		BeginTimeline(now);
		width = (height = 0.0);
	}

	private void BeginTimeline(double now)
	{
		textDrawn = false;
		timelineStarted = false;
		nextChunkTime = now;
		StartOpacity(0.0, 0.0, Easing.EaseIn, now);
	}

	public void Hide(double now, bool animated = true)
	{
		if (phase != 0)
		{
			if (!animated)
			{
				Finish();
			}
			else if (phase != Phase.FadingOut)
			{
				BeginFadeOut(now);
			}
		}
	}

	private void BeginFadeOut(double now)
	{
		phase = Phase.FadingOut;
		fadeStart = now;
		StartOpacity(0.0, (kind == Kind.Welcome) ? 0.5 : fadeOutSeconds, Easing.EaseOut, now);
	}

	private void Finish()
	{
		phase = Phase.None;
		words.Clear();
		welcomeText = "";
		opacity = (opacityTo = 0.0);
		visual3?.SetOpacity(0f);
		completion?.TrySetResult();
		completion = null;
		host.RequestCommit();
	}

	private void StartOpacity(double target, double duration, Func<double, double> curve, double now)
	{
		opacityFrom = opacity;
		opacityTo = target;
		opacityStart = now;
		opacityDuration = duration;
		opacityCurve = curve;
	}

	public bool Update(double now, double dt)
	{
		if (phase == Phase.None)
		{
			return false;
		}
		if (!timelineStarted)
		{
			if (!textDrawn || (!host.GpuCaughtUp && now - textDrawnAt < 0.25))
			{
				return true;
			}
			timelineStarted = true;
			nextChunkTime = now;
			StartOpacity(1.0, fadeInSeconds, Easing.EaseIn, now);
		}
		if (phase == Phase.Revealing)
		{
			if (kind == Kind.Welcome)
			{
				while (now >= nextChunkTime && welcomeText.Length < "hi, im heyclicky".Length)
				{
					welcomeText = "hi, im heyclicky".Substring(0, welcomeText.Length + 1);
					nextChunkTime += 0.03;
				}
				if (welcomeText.Length == "hi, im heyclicky".Length)
				{
					phase = Phase.Holding;
					holdUntil = now + holdSeconds;
				}
			}
			else
			{
				while (now >= nextChunkTime && nextChunk < pendingChunks.Count)
				{
					string text = pendingChunks[nextChunk++];
					int num = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
					for (int i = 0; i < num; i++)
					{
						if (words.Count >= allWords.Count)
						{
							break;
						}
						Word word = allWords[words.Count];
						word.RevealedAt = now;
						words.Add(word);
					}
					nextChunkTime = now + DelayAfter(text);
				}
				if (nextChunk >= pendingChunks.Count && now >= nextChunkTime)
				{
					phase = Phase.Holding;
					holdUntil = now + holdSeconds;
				}
			}
		}
		if (phase == Phase.Holding && now >= holdUntil)
		{
			BeginFadeOut(now);
		}
		double num2 = ((opacityDuration <= 0.0) ? 1.0 : ((now - opacityStart) / opacityDuration));
		opacity = ((num2 >= 1.0) ? opacityTo : Easing.Lerp(opacityFrom, opacityTo, opacityCurve(Math.Clamp(num2, 0.0, 1.0))));
		visual3?.SetOpacity((float)opacity);
		if (phase == Phase.FadingOut && now - fadeStart >= ((kind == Kind.Welcome) ? 0.5 : (fadeOutSeconds + 0.05)))
		{
			Finish();
			return false;
		}
		Layout(out targetWidth, out targetHeight);
		if (width <= 0.0)
		{
			width = targetWidth;
			height = targetHeight;
		}
		else if (dt > 0.0)
		{
			double num3 = 1.0 - Math.Exp((0.0 - dt) / 0.07);
			width += (targetWidth - width) * num3;
			height += (targetHeight - height) * num3;
			if (Math.Abs(width - targetWidth) < 0.2)
			{
				width = targetWidth;
			}
			if (Math.Abs(height - targetHeight) < 0.2)
			{
				height = targetHeight;
			}
		}
		bool flag = width != targetWidth || height != targetHeight;
		int num4;
		if (words.Count > 0)
		{
			List<Word> list = words;
			num4 = ((now - list[list.Count - 1].RevealedAt < 0.5) ? 1 : 0);
		}
		else
		{
			num4 = 0;
		}
		bool flag2 = (byte)num4 != 0;
		if (!(phase != Phase.Holding || flag || flag2))
		{
			return opacity != opacityTo;
		}
		return true;
	}

	public bool NeedsFrame(double now)
	{
		Phase phase = this.phase;
		if ((phase != Phase.Revealing && phase != Phase.FadingOut) || 1 == 0)
		{
			if (this.phase == Phase.Holding)
			{
				return now >= holdUntil;
			}
			return false;
		}
		return true;
	}

	public void Present(PointD anchorPx, double displayScale, RectD virtualScreenPx, RectD workAreaPx, double now)
	{
		if (phase != 0)
		{
			EnsureScale(displayScale);
			if (!tintDrawn)
			{
				DrawTint();
			}
			if (!textDrawn)
			{
				DrawText(now);
			}
			float num = (float)scale;
			double num2;
			double num3;
			if (kind == Kind.Welcome)
			{
				num2 = anchorPx.X + (double)(10f * num);
				num3 = anchorPx.Y + (double)(18f * num) - (double)((float)height / 2f * num);
			}
			else
			{
				double num4 = anchorPx.X - (double)(35f * num);
				double num5 = anchorPx.Y - (double)(25f * num);
				CursorAdjacentPlacement.Placed placed = CursorAdjacentPlacement.Place(sides, anchorPx.X + (double)(10f * num), anchorPx.Y + (double)(18f * num), num4 - (double)(12f * num), num5 - (double)(12f * num), width * (double)num, height * (double)num, 316f * num, height * (double)num, workAreaPx, num);
				sides = placed.Sides;
				num2 = placed.Left;
				num3 = placed.Top;
			}
			visual.SetOffsetX((float)Math.Round(num2 - (double)(20f * num) - virtualScreenPx.X));
			visual.SetOffsetY((float)Math.Round(num3 - (double)(20f * num) - virtualScreenPx.Y));
			PlacePlateAndWords(now);
			host.RequestCommit();
		}
	}

	private void PlacePlateAndWords(double now)
	{
		float num = (float)scale;
		float num2 = (float)width * num;
		float num3 = (float)height * num;
		float num4 = 20f * num;
		plate.SetOffsetX(num4);
		plate.SetOffsetY(num4);
		plateClip.SetLeft(0f);
		plateClip.SetTop(0f);
		plateClip.SetRight(num2);
		plateClip.SetBottom(num3);
		float num5 = 6f * num;
		plateClip.SetTopLeftRadiusX(num5);
		plateClip.SetTopLeftRadiusY(num5);
		plateClip.SetTopRightRadiusX(num5);
		plateClip.SetTopRightRadiusY(num5);
		plateClip.SetBottomLeftRadiusX(num5);
		plateClip.SetBottomLeftRadiusY(num5);
		plateClip.SetBottomRightRadiusX(num5);
		plateClip.SetBottomRightRadiusY(num5);
		glowShadow?.SetStandardDeviation((float)Math.Max(0.01, 6.0 * scale / 2.0));
		glowShadow?.SetColor(new Vector4(tint.R, tint.G, tint.B, 0.5f));
		textHolder.SetClip(new RawRectF(num4, num4, num4 + num2, num4 + num3));
		float num6 = 28f * num - (float)textMarginPx;
		float offsetY = 24f * num - (float)textMarginPx;
		int num7 = 0;
		if (kind == Kind.Welcome)
		{
			if (welcomeText.Length > 0 && welcomeCarets.Length > welcomeText.Length)
			{
				IDCompositionVisual2 iDCompositionVisual = WordVisual(num7++);
				iDCompositionVisual.SetOffsetX(num6);
				iDCompositionVisual.SetOffsetY(offsetY);
				iDCompositionVisual.SetClip(new RawRectF(0f, 0f, (float)textMarginPx + welcomeCarets[welcomeText.Length] * num, (float)(textMarginPx * 2) + 15f * num));
				wordVisuals3[num7 - 1]?.SetOpacity(1f);
			}
		}
		else
		{
			foreach (Word word in words)
			{
				double num8 = Easing.EaseOut(Math.Clamp((now - word.RevealedAt) / 0.46, 0.0, 1.0));
				IDCompositionVisual2 iDCompositionVisual2 = WordVisual(num7++);
				iDCompositionVisual2.SetOffsetX(num6 + (float)((1.0 - num8) * -1.5) * num);
				iDCompositionVisual2.SetOffsetY(offsetY);
				iDCompositionVisual2.SetClip(new RawRectF((float)textMarginPx + word.X * num - 1f, (float)textMarginPx + (word.Y - 0.5f) * num, (float)textMarginPx + (word.X + word.Width) * num + 1f, (float)textMarginPx + (word.Y + 15f + 0.5f) * num));
				wordVisuals3[num7 - 1]?.SetOpacity((float)num8);
			}
		}
		for (int i = num7; i < wordVisualsShown; i++)
		{
			wordVisuals3[i]?.SetVisible(false);
		}
		wordVisualsShown = num7;
	}

	private IDCompositionVisual2 WordVisual(int i)
	{
		while (wordVisuals.Count <= i)
		{
			IDCompositionVisual2 iDCompositionVisual = host.CompositionDevice.CreateVisual();
			iDCompositionVisual.SetBitmapInterpolationMode(Vortice.DirectComposition.BitmapInterpolationMode.Linear);
			if (wordVisuals.Count == 0)
			{
				textHolder.AddVisual(iDCompositionVisual, false, null);
			}
			else
			{
				IDCompositionVisual2 iDCompositionVisual2 = textHolder;
				RawBool insertAbove = true;
				List<IDCompositionVisual2> list = wordVisuals;
				iDCompositionVisual2.AddVisual(iDCompositionVisual, insertAbove, list[list.Count - 1]);
			}
			wordVisuals.Add(iDCompositionVisual);
			wordVisuals3.Add(iDCompositionVisual.QueryInterfaceOrNull<IDCompositionVisual3>());
		}
		wordVisuals[i].SetContent(textSurface);
		wordVisuals3[i]?.SetVisible(true);
		return wordVisuals[i];
	}

	private static List<string> RevealChunks(string text)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in text)
		{
			stringBuilder.Append(c);
			if (char.IsWhiteSpace(c))
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	private static double DelayAfter(string chunk)
	{
		string text = chunk.Trim();
		if (text.Length == 0)
		{
			return 0.06;
		}
		char c = text[text.Length - 1];
		if ((c == '!' || c == '.' || c == '?') ? true : false)
		{
			return 0.32;
		}
		if ((c == ',' || c == ':' || c == ';') ? true : false)
		{
			return 0.205;
		}
		return 0.112 + (double)Math.Min(text.Length, 10) * 0.01;
	}

	private float MeasureWord(string word)
	{
		if (wordWidths.TryGetValue(word, out var value))
		{
			return value;
		}
		EnsureFormat();
		using IDWriteTextLayout iDWriteTextLayout = host.DWriteFactory.CreateTextLayout(word, format, 10000f, 100f);
		value = MathF.Ceiling(iDWriteTextLayout.Metrics.Width);
		if (wordWidths.Count > 512)
		{
			wordWidths.Clear();
		}
		wordWidths[word] = value;
		return value;
	}

	private void LayOutAllWords()
	{
		allWords.Clear();
		float num = 0f;
		int num2 = 0;
		foreach (string pendingChunk in pendingChunks)
		{
			string[] array = pendingChunk.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				float num3 = MeasureWord(text);
				float num4 = ((num == 0f) ? num3 : (num + 3f + num3));
				if (num > 0f && num4 > 300f)
				{
					num2++;
					num = 0f;
					num4 = num3;
				}
				allWords.Add(new Word
				{
					Text = text,
					Width = num3,
					X = ((num == 0f) ? 0f : (num + 3f)),
					Y = (float)num2 * 16f
				});
				num = num4;
			}
		}
	}

	private void Layout(out double w, out double h)
	{
		if (kind == Kind.Welcome)
		{
			float num = ((welcomeText.Length == 0) ? 1f : MeasureWord(welcomeText));
			w = num + 16f;
			h = 23.0;
			return;
		}
		float num2 = 0f;
		int num3 = 1;
		foreach (Word word in words)
		{
			num2 = Math.Max(num2, Math.Min(300f, word.X + word.Width));
			num3 = (int)Math.Round(word.Y / 16f) + 1;
		}
		w = Math.Max(1f, num2) + 16f;
		h = Math.Min(180f, (float)num3 * 15f + (float)(num3 - 1) * 1f + 8f);
	}

	private void EnsureFormat()
	{
		if ((object)format != null)
		{
			return;
		}
		string fontFamilyName = "Segoe UI";
		using (IDWriteFontCollection iDWriteFontCollection = host.DWriteFactory.GetSystemFontCollection(false))
		{
			if ((bool)iDWriteFontCollection.FindFamilyName("Segoe UI Variable Text", out var _))
			{
				fontFamilyName = "Segoe UI Variable Text";
			}
		}
		format = host.DWriteFactory.CreateTextFormat(fontFamilyName, null, FontWeight.Medium, FontStyle.Normal, FontStretch.Normal, 11f, "en-us");
		format.WordWrapping = WordWrapping.NoWrap;
		format.ParagraphAlignment = ParagraphAlignment.Center;
	}

	private void EnsureScale(double newScale)
	{
		if ((object)tintSurface == null || !(Math.Abs(newScale - scale) < 0.001))
		{
			scale = newScale;
			ReleaseScaled();
			ID2D1DeviceContext d2DContext = host.D2DContext;
			pixelWidth = (int)Math.Ceiling(356.0 * scale);
			pixelHeight = (int)Math.Ceiling(220.0 * scale);
			tintSurface = host.CompositionDevice.CreateSurface((uint)Math.Ceiling(316.0 * scale), (uint)Math.Ceiling(180.0 * scale), Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
			plate.SetContent(tintSurface);
			textMarginPx = (int)Math.Ceiling(2.0 * scale);
			tintDrawn = (textDrawn = false);
			if ((object)tintBrush == null)
			{
				tintBrush = d2DContext.CreateSolidColorBrush(tint, null);
			}
			if ((object)textBrush == null)
			{
				textBrush = d2DContext.CreateSolidColorBrush(new Color4(1f, 1f, 1f), null);
			}
			EnsureFormat();
		}
	}

	private void DrawTint()
	{
		Int2 offset;
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
		tintDrawn = true;
	}

	private void DrawText(double now)
	{
		RenderCount++;
		float num = (float)scale;
		float num2;
		if (kind != Kind.Welcome)
		{
			float val;
			if (allWords.Count != 0)
			{
				List<Word> list = allWords;
				val = list[list.Count - 1].Y + 15f;
			}
			else
			{
				val = 15f;
			}
			num2 = Math.Min(180f, val);
		}
		else
		{
			num2 = 15f;
		}
		float num3 = num2;
		float num4 = ((kind == Kind.Welcome) ? (MeasureWord("hi, im heyclicky") + 4f) : 300f);
		textSurface?.Dispose();
		textSurface = host.CompositionDevice.CreateSurface((uint)Math.Ceiling(num4 * num) + (uint)(2 * textMarginPx), (uint)Math.Ceiling(num3 * num) + (uint)(2 * textMarginPx), Format.B8G8R8A8_UNorm, Vortice.DXGI.AlphaMode.Premultiplied);
		Int2 offset;
		ID2D1DeviceContext iD2D1DeviceContext = OverlayHost.BeginDraw(textSurface, null, out offset);
		try
		{
			iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
			iD2D1DeviceContext.Transform = Matrix3x2.CreateScale(num) * Matrix3x2.CreateTranslation(offset.X + textMarginPx, offset.Y + textMarginPx);
			textBrush.Opacity = 1f;
			if (kind == Kind.Welcome)
			{
				iD2D1DeviceContext.DrawText("hi, im heyclicky", format, new RawRectF(0f, 0f, 1000f, 15f), textBrush, DrawTextOptions.None);
				using IDWriteTextLayout iDWriteTextLayout = host.DWriteFactory.CreateTextLayout("hi, im heyclicky", format, 1000f, 15f);
				welcomeCarets = new float["hi, im heyclicky".Length + 1];
				for (int i = 0; i < "hi, im heyclicky".Length; i++)
				{
					iDWriteTextLayout.HitTestTextPosition((uint)i, false, out var pointX, out var _, out var _);
					welcomeCarets[i] = pointX;
				}
				welcomeCarets["hi, im heyclicky".Length] = iDWriteTextLayout.Metrics.WidthIncludingTrailingWhitespace;
			}
			else
			{
				foreach (Word allWord in allWords)
				{
					if (allWord.Y >= 180f)
					{
						break;
					}
					iD2D1DeviceContext.DrawText(allWord.Text, format, new RawRectF(allWord.X, allWord.Y, allWord.X + allWord.Width + 40f, allWord.Y + 15f), textBrush, DrawTextOptions.None);
				}
			}
			iD2D1DeviceContext.Transform = Matrix3x2.Identity;
		}
		finally
		{
			iD2D1DeviceContext.Dispose();
			textSurface.EndDraw();
		}
		foreach (IDCompositionVisual2 wordVisual in wordVisuals)
		{
			wordVisual.SetContent(textSurface);
		}
		textDrawn = true;
		textDrawnAt = now;
	}

	public unsafe void SnapshotPng(string path, Color4 background)
	{
		if ((object)tintSurface == null)
		{
			return;
		}
		ID2D1DeviceContext d2DContext = host.D2DContext;
		BitmapProperties1 bitmapProperties = new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target);
		SizeI size = new SizeI(pixelWidth, pixelHeight);
		if ((object)plateLayer == null)
		{
			plateLayer = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, bitmapProperties);
		}
		if ((object)compositeLayer == null)
		{
			compositeLayer = d2DContext.CreateBitmap(size, IntPtr.Zero, 0u, bitmapProperties);
		}
		if ((object)glow == null)
		{
			glow = new ID2D1Effect(d2DContext.CreateEffect(EffectGuids.Shadow));
			glow.SetInput(0u, plateLayer, true);
		}
		float num = (float)scale;
		Matrix3x2 transform = Matrix3x2.CreateScale(num) * Matrix3x2.CreateTranslation(20f * num, 20f * num);
		float right = (float)width;
		float bottom = (float)height;
		double num2 = (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
		d2DContext.Target = plateLayer;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		d2DContext.Transform = transform;
		d2DContext.FillRoundedRectangle(new RoundedRectangle(new RawRectF(0f, 0f, right, bottom), 6f, 6f), tintBrush);
		d2DContext.Transform = Matrix3x2.Identity;
		d2DContext.EndDraw();
		d2DContext.Target = compositeLayer;
		d2DContext.BeginDraw();
		d2DContext.Clear(new Color4(0f, 0f, 0f, 0f));
		float num3 = (float)Math.Max(0.01, 6.0 * scale / 2.0);
		glow.SetValue(0u, PropertyType.Float, &num3, 4u);
		Vector4 vector = new Vector4(tint.R, tint.G, tint.B, 0.5f);
		glow.SetValue(1u, PropertyType.Vector4, &vector, (uint)sizeof(Vector4));
		d2DContext.DrawImage(glow.Output, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.DrawImage(plateLayer, Vector2.Zero, null, InterpolationMode.Linear, Vortice.Direct2D1.CompositeMode.SourceOver);
		d2DContext.Transform = transform;
		d2DContext.PushAxisAlignedClip(new RawRectF(0f, 0f, right, bottom), AntialiasMode.PerPrimitive);
		if (kind == Kind.Welcome)
		{
			textBrush.Opacity = 1f;
			if (welcomeText.Length > 0)
			{
				d2DContext.DrawText(welcomeText, format, new RawRectF(8f, 4f, 1008f, 19f), textBrush, DrawTextOptions.None);
			}
		}
		else
		{
			foreach (Word word in words)
			{
				double num4 = Easing.EaseOut(Math.Clamp((num2 - word.RevealedAt) / 0.46, 0.0, 1.0));
				textBrush.Opacity = (float)num4;
				float num5 = (float)((1.0 - num4) * -1.5);
				d2DContext.DrawText(word.Text, format, new RawRectF(8f + word.X + num5, 4f + word.Y, 8f + word.X + num5 + word.Width + 40f, 4f + word.Y + 15f), textBrush, DrawTextOptions.None);
			}
			textBrush.Opacity = 1f;
		}
		d2DContext.PopAxisAlignedClip();
		d2DContext.Transform = Matrix3x2.Identity;
		d2DContext.EndDraw();
		d2DContext.Target = null;
		OverlayDebug.SavePng(host, compositeLayer, path, background);
	}

	private void ReleaseScaled()
	{
		glow?.Dispose();
		glow = null;
		plateLayer?.Dispose();
		plateLayer = null;
		compositeLayer?.Dispose();
		compositeLayer = null;
		foreach (IDCompositionVisual2 wordVisual in wordVisuals)
		{
			wordVisual.SetContent(null);
		}
		plate.SetContent(null);
		textSurface?.Dispose();
		textSurface = null;
		tintSurface?.Dispose();
		tintSurface = null;
	}

	public void Dispose()
	{
		completion?.TrySetResult();
		ReleaseScaled();
		tintBrush?.Dispose();
		textBrush?.Dispose();
		format?.Dispose();
		host.RootVisual.RemoveVisual(visual);
		plateGlow.SetEffect(null);
		glowEffect?.Dispose();
		glowShadow?.Dispose();
		effects?.Dispose();
		foreach (IDCompositionVisual3 item in wordVisuals3)
		{
			item?.Dispose();
		}
		foreach (IDCompositionVisual2 wordVisual in wordVisuals)
		{
			wordVisual.Dispose();
		}
		textHolder.Dispose();
		plateClip.Dispose();
		plate.Dispose();
		plateGlow.Dispose();
		visual3?.Dispose();
		visual.Dispose();
	}
}
