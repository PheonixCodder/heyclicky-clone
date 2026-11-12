using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace HeyClicky.Drawing3D;

internal sealed class LabelWords : IDisposable
{
	private sealed class Face
	{
		public required IDWriteFontFace Native;

		public float UnitsPerEm;

		public float Ascent;

		public float Descent;

		public bool IsColor;
	}

	private sealed class ShapedGlyph
	{
		public required Face Face;

		public ushort Index;

		public float X;

		public float Y;

		public float Advance;

		public float InkLeft;

		public float InkTop;

		public float InkRight;

		public float InkBottom;

		public bool HasInk;

		public ColorLayer[]? Layers;
	}

	private readonly record struct ColorLayer(Face Face, ushort Index, float X, float Y, Color4? Color);

	private sealed class ShapedLine
	{
		public required ShapedGlyph[] Glyphs;

		public float Advance;

		public float LayoutAdvance;
	}

	private sealed class RunCollector : TextRendererBase
	{
		public readonly List<ShapedGlyph> Glyphs = new List<ShapedGlyph>();

		public readonly List<int> StringIndices = new List<int>();

		public readonly List<(int Start, int Length, float Width, Face Face)> Runs = new List<(int, int, float, Face)>();

		public void Clear()
		{
			Glyphs.Clear();
			StringIndices.Clear();
			Runs.Clear();
		}

		public unsafe override void DrawGlyphRun(nint clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, GlyphRun glyphRun, GlyphRunDescription glyphRunDescription, IUnknown clientDrawingEffect)
		{
			if ((object)glyphRun.FontFace == null)
			{
				return;
			}
			ushort[] indices = glyphRun.Indices;
			if (indices == null || indices.Length <= 0)
			{
				return;
			}
			Face face = FaceFor(glyphRun.FontFace.NativePointer);
			float[] advances = glyphRun.Advances;
			GlyphOffset[] offsets = glyphRun.Offsets;
			int num = indices.Length;
			int textPosition = (int)glyphRunDescription.TextPosition;
			int num2 = glyphRunDescription.Text?.Length ?? 0;
			int[] array = new int[num];
			Array.Fill(array, -1);
			ushort* clusterMap = (ushort*)glyphRunDescription.ClusterMap;
			if (clusterMap != null)
			{
				for (int i = 0; i < num2; i++)
				{
					int num3 = clusterMap[i];
					if (num3 < num && array[num3] < 0)
					{
						array[num3] = i;
					}
				}
			}
			int num4 = 0;
			for (int j = 0; j < num; j++)
			{
				if (array[j] < 0)
				{
					array[j] = num4;
				}
				else
				{
					num4 = array[j];
				}
			}
			bool flag = (glyphRun.BidiLevel & 1) != 0;
			float num5 = baselineOriginX;
			float num6 = 0f;
			for (int k = 0; k < num; k++)
			{
				float num7 = ((advances != null && k < advances.Length) ? advances[k] : 0f);
				GlyphOffset glyphOffset = ((offsets != null && k < offsets.Length) ? offsets[k] : default(GlyphOffset));
				float num8 = (flag ? (num5 - num7 - glyphOffset.AdvanceOffset) : (num5 + glyphOffset.AdvanceOffset));
				num5 += (flag ? (0f - num7) : num7);
				num6 += num7;
				Glyphs.Add(new ShapedGlyph
				{
					Face = face,
					Index = indices[k],
					X = num8 / 64f,
					Y = (0f - glyphOffset.AscenderOffset) / 64f,
					Advance = num7 / 64f
				});
				StringIndices.Add(textPosition + array[k]);
			}
			Runs.Add((textPosition, num2, num6 / 64f, face));
		}

		public override RawBool IsPixelSnappingDisabled(nint clientDrawingContext)
		{
			return true;
		}

		public override float GetPixelsPerDip(nint clientDrawingContext)
		{
			return 1f;
		}

		public override Matrix3x2 GetCurrentTransform(nint clientDrawingContext)
		{
			return Matrix3x2.Identity;
		}
	}

	private sealed class LayerRun
	{
		public required Face Face;

		public required ushort[] Glyph;

		public required float[] Advance;

		public Vector2 Origin;

		public Color4? Color;
	}

	private sealed class Letter
	{
		public required ShapedGlyph Glyph;

		public float X;

		public int Line;

		public Vector2 Pivot;

		public double Begin;

		public GlyphKey Key;

		public bool InkReady;

		public ID2D1GeometryRealization? Ink;

		public Vector2 InkOffset;

		public LayerRun[]? Layers;

		public Vector2 PixelPivot;
	}

	private sealed class Unit
	{
		public readonly List<float> Baselines = new List<float>();

		public readonly List<Letter> Letters = new List<Letter>();

		public float Size;

		public double Start;

		public double Fade;

		public double End;

		public Vector2 Offset;

		public Color4 Ink;

		public WordsGlow? Glow;

		public bool HasPixels;

		public Matrix3x2 PixelsFor;

		public float Dilation;

		public int BoxX;

		public int BoxY;

		public int BoxWidth;

		public int BoxHeight;

		public bool HasColor;

		public ID2D1Bitmap1? Picture;

		public bool PictureSettled;

		public ID2D1CommandList? GlowInput;

		public ID2D1Bitmap1? Settled;

		public int SettledMargin;
	}

	private readonly record struct GlyphKey(nint Face, ushort Glyph, float Em, float FractionX, float FractionY);

	[Flags]
	private enum Parts
	{
		Outlines = 1,
		Colour = 2,
		All = 3
	}

	private struct Point2F
	{
		public float X;

		public float Y;
	}

	private struct NativeGlyphRun
	{
		public nint FontFace;

		public float FontEmSize;

		public uint GlyphCount;

		public unsafe ushort* GlyphIndices;

		public unsafe float* GlyphAdvances;

		public nint GlyphOffsets;

		public int IsSideways;

		public uint BidiLevel;
	}

	private const float ShapingEm = 64f;

	private static readonly object fontLock = new object();

	private static bool fontsTried;

	private static IDWriteTextFormat? textFormat;

	private static IDWriteFactory2? factory2;

	private static RunCollector? collector;

	private static readonly Dictionary<nint, Face> faces = new Dictionary<nint, Face>();

	private static readonly Dictionary<string, ShapedLine> lineCache = new Dictionary<string, ShapedLine>();

	private const float Tolerance = 0.25f;

	private readonly Dictionary<GlyphKey, ID2D1GeometryRealization?> glyphInk = new Dictionary<GlyphKey, ID2D1GeometryRealization>();

	private readonly Dictionary<int, Unit> units = new Dictionary<int, Unit>();

	private ID2D1SolidColorBrush? brush;

	private ID2D1DeviceContext? effectsContext;

	private Shadow? wideGlow;

	private Shadow? tightGlow;

	private ID2D1DeviceContext1? context1;

	private nint context1For;

	private static ID2D1StrokeStyle? roundJoin;

	public static string? FontPath { get; set; }

	public bool IsEmpty => units.Count == 0;

	private static bool EnsureFonts()
	{
		if (fontsTried)
		{
			return (object)textFormat != null;
		}
		fontsTried = true;
		try
		{
			object obj = FontPath;
			if (obj == null)
			{
				string[] obj2 = new string[2]
				{
					Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "ClickyDraw-PatrickHand.ttf"),
					null
				};
				global::_003C_003Ey__InlineArray5<string> buffer = default(global::_003C_003Ey__InlineArray5<string>);
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 0) = AppContext.BaseDirectory;
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 1) = "HeyClicky.Drawing";
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 2) = "Assets";
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 3) = "Fonts";
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 4) = "ClickyDraw-PatrickHand.ttf";
				obj2[1] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray5<string>, string>(in buffer, 5));
				obj = obj2.FirstOrDefault(File.Exists) ?? Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "ClickyDraw-PatrickHand.ttf");
			}
			string filePath = (string)obj;
			IDWriteFactory dWriteFactory = GpuDevice.Shared.DWriteFactory;
			using IDWriteFactory5 iDWriteFactory = dWriteFactory.QueryInterface<IDWriteFactory5>();
			using IDWriteFontFile fontFile = dWriteFactory.CreateFontFileReference(filePath, null);
			using IDWriteFontSetBuilder1 iDWriteFontSetBuilder = iDWriteFactory.CreateFontSetBuilder();
			iDWriteFontSetBuilder.AddFontFile(fontFile);
			using IDWriteFontSet fontSet = iDWriteFontSetBuilder.CreateFontSet();
			using IDWriteFontCollection1 iDWriteFontCollection = iDWriteFactory.CreateFontCollectionFromFontSet(fontSet);
			string @string;
			using (IDWriteFontFamily1 iDWriteFontFamily = iDWriteFontCollection.GetFontFamily(0u))
			{
				using IDWriteLocalizedStrings iDWriteLocalizedStrings = iDWriteFontFamily.FamilyNames;
				@string = iDWriteLocalizedStrings.GetString(0u);
			}
			textFormat = dWriteFactory.CreateTextFormat(@string, iDWriteFontCollection, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 64f, "en-us");
			textFormat.WordWrapping = WordWrapping.NoWrap;
			factory2 = dWriteFactory.QueryInterface<IDWriteFactory2>();
			collector = new RunCollector();
		}
		catch (Exception ex)
		{
			Log.Error("3D", "label font failed to load", ex);
			textFormat?.Dispose();
			textFormat = null;
		}
		return (object)textFormat != null;
	}

	private static ShapedLine ShapeLine(string text)
	{
		lock (fontLock)
		{
			if (lineCache.TryGetValue(text, out ShapedLine value))
			{
				return value;
			}
			ShapedLine shapedLine = ShapeUncached(text);
			if (lineCache.Count > 2000)
			{
				lineCache.Clear();
			}
			lineCache[text] = shapedLine;
			return shapedLine;
		}
	}

	private static ShapedLine ShapeUncached(string text)
	{
		if (text.Length == 0 || !EnsureFonts())
		{
			return new ShapedLine
			{
				Glyphs = Array.Empty<ShapedGlyph>()
			};
		}
		RunCollector runCollector = collector;
		runCollector.Clear();
		try
		{
			using IDWriteTextLayout iDWriteTextLayout = GpuDevice.Shared.DWriteFactory.CreateTextLayout(text, textFormat, 1000000f, 256f);
			iDWriteTextLayout.Draw(IntPtr.Zero, runCollector, 0f, 0f);
		}
		catch (Exception ex)
		{
			Log.Error("3D", "label words failed to shape", ex);
			return new ShapedLine
			{
				Glyphs = Array.Empty<ShapedGlyph>()
			};
		}
		ShapedGlyph[] array = runCollector.Glyphs.ToArray();
		List<int> stringIndices = runCollector.StringIndices;
		float num = 0f;
		ShapedGlyph[] array2 = array;
		foreach (ShapedGlyph shapedGlyph in array2)
		{
			num += shapedGlyph.Advance;
		}
		bool num2 = !NeedsWholeRunShaping(text);
		float num3 = num;
		if (num2)
		{
			num3 = 0f;
			foreach (var run in runCollector.Runs)
			{
				num3 += CharacterAdvanceSum(text, run.Start, run.Length, run.Width, run.Face);
			}
			float num4 = 0f;
			for (int j = 0; j < array.Length; j++)
			{
				ShapedGlyph shapedGlyph2 = array[j];
				shapedGlyph2.X += num4;
				bool flag = j + 1 < array.Length && IsSpace(stringIndices[j + 1]);
				if (IsSpace(stringIndices[j]) || flag)
				{
					float num5 = NominalAdvance(shapedGlyph2.Face, shapedGlyph2.Index);
					num4 += num5 - shapedGlyph2.Advance;
					shapedGlyph2.Advance = num5;
				}
			}
			num += num4;
		}
		array2 = array;
		foreach (ShapedGlyph shapedGlyph3 in array2)
		{
			GlyphMetrics glyphMetrics = shapedGlyph3.Face.Native.GetDesignGlyphMetrics(new ushort[1] { shapedGlyph3.Index }, isSideways: false)[0];
			float num6 = 1f / shapedGlyph3.Face.UnitsPerEm;
			shapedGlyph3.InkLeft = (float)glyphMetrics.LeftSideBearing * num6;
			shapedGlyph3.InkRight = (float)((int)glyphMetrics.AdvanceWidth - glyphMetrics.RightSideBearing) * num6;
			shapedGlyph3.InkTop = (float)(-(glyphMetrics.VerticalOriginY - glyphMetrics.TopSideBearing)) * num6;
			shapedGlyph3.InkBottom = (float)(-(glyphMetrics.VerticalOriginY - (int)glyphMetrics.AdvanceHeight + glyphMetrics.BottomSideBearing)) * num6;
			shapedGlyph3.HasInk = shapedGlyph3.InkRight > shapedGlyph3.InkLeft && shapedGlyph3.InkBottom > shapedGlyph3.InkTop;
			if (shapedGlyph3.Face.IsColor)
			{
				shapedGlyph3.Layers = ColorLayers(shapedGlyph3.Face, shapedGlyph3.Index, shapedGlyph3.Advance);
			}
		}
		return new ShapedLine
		{
			Glyphs = array,
			Advance = num,
			LayoutAdvance = num3
		};
		bool IsSpace(int index)
		{
			bool flag2 = index >= 0 && index < text.Length;
			if (flag2)
			{
				char c = text[index];
				bool flag3 = ((c == '\t' || c == ' ' || c == '\u00a0') ? true : false);
				flag2 = flag3;
			}
			return flag2;
		}
	}

	private static float NominalAdvance(Face face, ushort glyph)
	{
		return (float)face.Native.GetDesignGlyphMetrics(new ushort[1] { glyph }, isSideways: false)[0].AdvanceWidth / face.UnitsPerEm;
	}

	private static float CharacterAdvanceSum(string text, int start, int length, float shapedWidth, Face face)
	{
		if (length <= 0 || start < 0 || start + length > text.Length)
		{
			return 0f;
		}
		uint[] array = new uint[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = text[start + i];
		}
		ushort[] glyphIndices = face.Native.GetGlyphIndices(array);
		if (Array.IndexOf(glyphIndices, (ushort)0) >= 0)
		{
			return shapedWidth;
		}
		GlyphMetrics[] designGlyphMetrics = face.Native.GetDesignGlyphMetrics(glyphIndices, isSideways: false);
		float num = 0f;
		GlyphMetrics[] array2 = designGlyphMetrics;
		for (int j = 0; j < array2.Length; j++)
		{
			GlyphMetrics glyphMetrics = array2[j];
			num += (float)glyphMetrics.AdvanceWidth;
		}
		return num / face.UnitsPerEm;
	}

	private static bool NeedsWholeRunShaping(string text)
	{
		foreach (char c in text)
		{
			if (c <= '\udfff')
			{
				if (c <= '\u200f')
				{
					if (c <= '\u1dff')
					{
						if (c <= '\u036f')
						{
							if (c >= '\u0300')
							{
								goto IL_00a9;
							}
						}
						else if (c >= '\u0591')
						{
							goto IL_00a9;
						}
					}
					else if (c >= '\u200c')
					{
						goto IL_00a9;
					}
				}
				else if (c <= '\u20ff')
				{
					if (c >= '\u20d0')
					{
						goto IL_00a9;
					}
				}
				else if (c >= '\ud800')
				{
					goto IL_00a9;
				}
			}
			else if (c >= '\ufe20')
			{
				if (c >= 'ﹰ')
				{
					if (c <= '\ufeff')
					{
						goto IL_00a9;
					}
				}
				else if (c <= '\ufe2f')
				{
					goto IL_00a9;
				}
			}
			else if (c <= '﷿')
			{
				if (c >= 'יִ')
				{
					goto IL_00a9;
				}
			}
			else if (c <= '\ufe0f')
			{
				goto IL_00a9;
			}
			bool flag = false;
			goto IL_00af;
			IL_00a9:
			flag = true;
			goto IL_00af;
			IL_00af:
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private static Face FaceFor(nint native)
	{
		if (faces.TryGetValue(native, out Face value))
		{
			return value;
		}
		Marshal.AddRef(native);
		IDWriteFontFace iDWriteFontFace = new IDWriteFontFace(native);
		FontMetrics metrics = iDWriteFontFace.Metrics;
		float num = Math.Max(1, (int)metrics.DesignUnitsPerEm);
		bool isColor = false;
		using (IDWriteFontFace2 iDWriteFontFace2 = iDWriteFontFace.QueryInterfaceOrNull<IDWriteFontFace2>())
		{
			isColor = iDWriteFontFace2?.IsColorFont ?? ((RawBool)false);
		}
		Face face = new Face
		{
			Native = iDWriteFontFace,
			UnitsPerEm = num,
			Ascent = (float)(int)metrics.Ascent / num,
			Descent = (float)(int)metrics.Descent / num,
			IsColor = isColor
		};
		faces[native] = face;
		return face;
	}

	private static ColorLayer[]? ColorLayers(Face face, ushort glyph, float advance)
	{
		if ((object)factory2 == null)
		{
			return null;
		}
		try
		{
			GlyphRun glyphRun = new GlyphRun();
			glyphRun.FontFace = face.Native;
			glyphRun.FontEmSize = 64f;
			glyphRun.Indices = new ushort[1] { glyph };
			glyphRun.Advances = new float[1] { advance * 64f };
			GlyphRun glyphRun2 = glyphRun;
			if (factory2.TranslateColorGlyphRun(0f, 0f, glyphRun2, null, MeasuringMode.Natural, null, 0u, out var colorLayers).Failure || (object)colorLayers == null)
			{
				return null;
			}
			using (colorLayers)
			{
				List<ColorLayer> list = new List<ColorLayer>();
				while ((bool)colorLayers.MoveNext())
				{
					ColorGlyphRun currentRun = colorLayers.CurrentRun;
					GlyphRun glyphRun3 = currentRun.GlyphRun;
					if ((object)glyphRun3?.FontFace != null && glyphRun3.Indices != null)
					{
						Face face2 = FaceFor(glyphRun3.FontFace.NativePointer);
						float num = currentRun.BaselineOriginX;
						for (int i = 0; i < glyphRun3.Indices.Length; i++)
						{
							GlyphOffset[] offsets = glyphRun3.Offsets;
							GlyphOffset glyphOffset = ((offsets != null && i < offsets.Length) ? offsets[i] : default(GlyphOffset));
							list.Add(new ColorLayer(face2, glyphRun3.Indices[i], (num + glyphOffset.AdvanceOffset) / 64f, (currentRun.BaselineOriginY - glyphOffset.AscenderOffset) / 64f, (currentRun.PaletteIndex == ushort.MaxValue) ? ((Color4?)null) : new Color4?(currentRun.RunColor)));
							float num2 = num;
							float[] advances = glyphRun3.Advances;
							num = num2 + ((advances != null && i < advances.Length) ? advances[i] : 0f);
						}
					}
				}
				return (list.Count > 0) ? list.ToArray() : null;
			}
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static float Measure(string text, float size)
	{
		return ShapeLine(text).LayoutAdvance * size;
	}

	public void Write(int unit, IReadOnlyList<string> lines, float size, WordsAnchor anchor, double originX, double originY, double lineAdvance, double start, double duration, Vector4 inkPremultiplied, WordsGlow? glow = null)
	{
		lock (fontLock)
		{
			if (!EnsureFonts())
			{
				return;
			}
		}
		if (units.Remove(unit, out Unit value))
		{
			Release(value);
		}
		Unit unit2 = new Unit
		{
			Size = size,
			Start = start,
			Glow = glow
		};
		float w = inkPremultiplied.W;
		unit2.Ink = ((w > 0f) ? new Color4(inkPremultiplied.X / w, inkPremultiplied.Y / w, inkPremultiplied.Z / w, w) : new Color4(0f, 0f, 0f, 0f));
		double num = 0.0;
		List<double> list = new List<double>();
		double num2 = originY;
		for (int i = 0; i < lines.Count; i++)
		{
			if (i > 0)
			{
				num2 += lineAdvance;
			}
			ShapedLine shapedLine = ShapeLine(lines[i]);
			double num3 = shapedLine.LayoutAdvance * size;
			double num4 = anchor switch
			{
				WordsAnchor.Start => originX, 
				WordsAnchor.End => originX - num3, 
				_ => originX - num3 / 2.0, 
			};
			unit2.Baselines.Add((float)num2);
			ShapedGlyph[] glyphs = shapedLine.Glyphs;
			foreach (ShapedGlyph shapedGlyph in glyphs)
			{
				float num5 = shapedGlyph.Advance * size;
				unit2.Letters.Add(new Letter
				{
					Glyph = shapedGlyph,
					X = (float)(num4 + (double)(shapedGlyph.X * size)),
					Line = i,
					Pivot = new Vector2((float)(num4 + (double)(shapedGlyph.X * size)) + num5 / 2f, (float)num2 - size * 0.32f)
				});
				list.Add(num + (double)(num5 / 2f));
				num += (double)num5;
			}
			num += (double)size * 0.8;
		}
		double num6 = Math.Max(1.0, num - (double)size * 0.8);
		unit2.Fade = Math.Min(0.16, Math.Max(0.07, duration * 0.22));
		double num7 = Math.Max(0.01, duration - unit2.Fade);
		for (int k = 0; k < unit2.Letters.Count; k++)
		{
			unit2.Letters[k].Begin = start + num7 * Math.Clamp(list[k] / num6, 0.0, 1.0);
		}
		unit2.End = start + num7 + unit2.Fade;
		units[unit] = unit2;
	}

	public void SetOffset(int unit, double dx, double dy)
	{
		if (units.TryGetValue(unit, out Unit value))
		{
			value.Offset = new Vector2((float)dx, (float)dy);
		}
	}

	public void Remove(int unit)
	{
		if (units.Remove(unit, out Unit value))
		{
			Release(value);
		}
	}

	public bool IsWriting(double time)
	{
		foreach (Unit value in units.Values)
		{
			if (time < value.End)
			{
				return true;
			}
		}
		return false;
	}

	private static float SettleCurve(float x)
	{
		float num = x;
		for (int i = 0; i < 6; i++)
		{
			float num2 = Bezier(num, 0.2f, 0.3f) - x;
			float num3 = Slope(num, 0.2f, 0.3f);
			if (MathF.Abs(num2) < 1E-05f || MathF.Abs(num3) < 1E-06f)
			{
				break;
			}
			num -= num2 / num3;
		}
		num = Math.Clamp(num, 0f, 1f);
		return Bezier(num, 0.7f, 1f);
		static float Bezier(float t, float p1, float p2)
		{
			float num4 = 1f - t;
			return 3f * num4 * num4 * t * p1 + 3f * num4 * t * t * p2 + t * t * t;
		}
		static float Slope(float t, float p1, float p2)
		{
			float num5 = 1f - t;
			return 3f * num5 * num5 * p1 + 6f * num5 * t * (p2 - p1) + 3f * t * t * (1f - p2);
		}
	}

	public void Draw(ID2D1DeviceContext context, double time, Matrix3x2 toPixels)
	{
		if (units.Count == 0)
		{
			return;
		}
		if ((object)brush == null)
		{
			brush = context.CreateSolidColorBrush(new Color4(1f, 1f, 1f), null);
		}
		context.TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode.Grayscale;
		ID2D1DeviceContext1 dc = DeviceContext1(context);
		float m = toPixels.M11;
		foreach (Unit value in units.Values)
		{
			if (time < value.Start)
			{
				continue;
			}
			if (!value.HasPixels || value.PixelsFor != toPixels)
			{
				BuildPixels(dc, value, toPixels);
			}
			if (value.BoxWidth <= 0 || value.BoxHeight <= 0)
			{
				continue;
			}
			Vector2 position = new Vector2(MathF.Round(value.Offset.X * m), MathF.Round(value.Offset.Y * m));
			Vector2 vector = new Vector2((float)value.BoxX + position.X, (float)value.BoxY + position.Y);
			bool flag = time >= value.End;
			WordsGlow? glow = value.Glow;
			if (glow.HasValue)
			{
				WordsGlow valueOrDefault = glow.GetValueOrDefault();
				if (!flag || !value.PictureSettled)
				{
					PaintPicture(context, dc, value, time, Parts.Outlines);
				}
				if (flag && value.PictureSettled)
				{
					if ((object)value.Settled == null)
					{
						PaintSettled(context, dc, value, time, valueOrDefault, m);
					}
					context.Transform = Matrix3x2.Identity;
					context.DrawImage(value.Settled, vector - new Vector2(value.SettledMargin), null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
					continue;
				}
				ID2D1CommandList iD2D1CommandList;
				if (value.PictureSettled && (object)value.GlowInput != null)
				{
					iD2D1CommandList = value.GlowInput;
				}
				else
				{
					iD2D1CommandList = WrapPicture(context, value.Picture);
					if (value.PictureSettled)
					{
						value.GlowInput = iD2D1CommandList;
					}
				}
				EnsureGlowEffects(context);
				context.Transform = Matrix3x2.Identity;
				DrawGlow(context, wideGlow, iD2D1CommandList, valueOrDefault.WideColor, valueOrDefault.WideRadius * m, vector + new Vector2(0f, (0f - valueOrDefault.WideOffsetY) * m));
				DrawGlow(context, tightGlow, iD2D1CommandList, valueOrDefault.TightColor, valueOrDefault.TightRadius * m, vector);
				if (!value.PictureSettled)
				{
					iD2D1CommandList.Dispose();
				}
				context.DrawImage(value.Picture, vector, null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
				if (value.HasColor)
				{
					DrawLetters(context, dc, value, time, Matrix3x2.CreateTranslation(position), Parts.Colour);
				}
			}
			else if (!flag)
			{
				DrawLetters(context, dc, value, time, Matrix3x2.CreateTranslation(position), Parts.All);
			}
			else
			{
				if (!value.PictureSettled)
				{
					PaintPicture(context, dc, value, time, Parts.All);
				}
				context.Transform = Matrix3x2.Identity;
				context.DrawImage(value.Picture, vector, null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
			}
		}
		context.Transform = Matrix3x2.Identity;
	}

	private ID2D1DeviceContext1 DeviceContext1(ID2D1DeviceContext context)
	{
		if ((object)context1 == null || context1For != context.NativePointer)
		{
			context1?.Dispose();
			context1 = context.QueryInterface<ID2D1DeviceContext1>();
			context1For = context.NativePointer;
		}
		return context1;
	}

	private void PaintSettled(ID2D1DeviceContext context, ID2D1DeviceContext1 dc1, Unit u, double time, WordsGlow glow, float pixelsPerUnit)
	{
		float num = glow.WideRadius * pixelsPerUnit;
		float num2 = glow.TightRadius * pixelsPerUnit;
		float num3 = glow.WideOffsetY * pixelsPerUnit;
		int num4 = (u.SettledMargin = (int)MathF.Ceiling(3f * MathF.Max(num, num2) + MathF.Abs(num3)) + 2);
		u.Settled = context.CreateBitmap(new SizeI(u.BoxWidth + 2 * num4, u.BoxHeight + 2 * num4), IntPtr.Zero, 0u, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target));
		if ((object)u.GlowInput == null)
		{
			u.GlowInput = WrapPicture(context, u.Picture);
		}
		EnsureGlowEffects(context);
		ID2D1Image target = context.Target;
		context.Target = u.Settled;
		context.Transform = Matrix3x2.Identity;
		context.Clear(new Color4(0f, 0f, 0f, 0f));
		Vector2 vector = new Vector2(num4, num4);
		DrawGlow(context, wideGlow, u.GlowInput, glow.WideColor, num, vector + new Vector2(0f, 0f - num3));
		DrawGlow(context, tightGlow, u.GlowInput, glow.TightColor, num2, vector);
		context.DrawImage(u.Picture, vector, null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
		if (u.HasColor)
		{
			DrawLetters(context, dc1, u, time, Matrix3x2.CreateTranslation(num4 - u.BoxX, num4 - u.BoxY), Parts.Colour);
		}
		context.Transform = Matrix3x2.Identity;
		context.Target = target;
		target?.Dispose();
	}

	private static ID2D1CommandList WrapPicture(ID2D1DeviceContext context, ID2D1Bitmap1 picture)
	{
		ID2D1Image target = context.Target;
		ID2D1CommandList iD2D1CommandList = (ID2D1CommandList)(context.Target = context.CreateCommandList());
		context.Transform = Matrix3x2.Identity;
		context.DrawImage(picture, Vector2.Zero, null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
		context.Target = target;
		target?.Dispose();
		iD2D1CommandList.Close();
		return iD2D1CommandList;
	}

	private void PaintPicture(ID2D1DeviceContext context, ID2D1DeviceContext1 dc1, Unit u, double time, Parts parts)
	{
		u.GlowInput?.Dispose();
		u.GlowInput = null;
		if ((object)u.Picture == null)
		{
			u.Picture = context.CreateBitmap(new SizeI(u.BoxWidth, u.BoxHeight), IntPtr.Zero, 0u, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target));
		}
		ID2D1Image target = context.Target;
		context.Target = u.Picture;
		context.Transform = Matrix3x2.Identity;
		context.Clear(new Color4(0f, 0f, 0f, 0f));
		DrawLetters(context, dc1, u, time, Matrix3x2.CreateTranslation(-u.BoxX, -u.BoxY), parts);
		context.Target = target;
		target?.Dispose();
		u.PictureSettled = time >= u.End;
	}

	private void DrawLetters(ID2D1DeviceContext context, ID2D1DeviceContext1 dc1, Unit u, double time, Matrix3x2 place, Parts parts)
	{
		bool flag = time >= u.End;
		float num = u.Size * 0.06f * u.PixelsFor.M11;
		foreach (Letter letter in u.Letters)
		{
			bool flag2 = letter.Layers != null;
			bool num2;
			if (!flag2)
			{
				if (!letter.Glyph.HasInk)
				{
					continue;
				}
				num2 = (parts & Parts.Outlines) == 0;
			}
			else
			{
				num2 = (parts & Parts.Colour) == 0;
			}
			if (num2)
			{
				continue;
			}
			float opacity = 1f;
			Matrix3x2 matrix3x = place;
			if (!flag)
			{
				if (time < letter.Begin)
				{
					continue;
				}
				float num3 = SettleCurve((float)Math.Clamp((time - letter.Begin) / u.Fade, 0.0, 1.0));
				float scale = 0.86f + 0.14f * num3;
				float yPosition = num * (1f - num3);
				matrix3x = Matrix3x2.CreateScale(scale, letter.PixelPivot) * Matrix3x2.CreateTranslation(0f, yPosition) * place;
				opacity = num3;
			}
			if (!flag2)
			{
				if (!letter.InkReady)
				{
					PrepareInk(dc1, u, letter);
				}
				ID2D1GeometryRealization ink = letter.Ink;
				if ((object)ink != null)
				{
					context.Transform = Matrix3x2.CreateTranslation(letter.InkOffset) * matrix3x;
					brush.Color = u.Ink;
					brush.Opacity = opacity;
					dc1.DrawGeometryRealization(ink, brush);
				}
			}
			else
			{
				context.Transform = matrix3x;
				LayerRun[] layers = letter.Layers;
				foreach (LayerRun layerRun in layers)
				{
					brush.Color = layerRun.Color ?? u.Ink;
					brush.Opacity = opacity;
					DrawRun(context, layerRun.Origin, layerRun.Face.Native.NativePointer, u.Size * u.PixelsFor.M11, layerRun.Glyph, layerRun.Advance, brush);
				}
			}
		}
	}

	private void PrepareInk(ID2D1DeviceContext1 dc1, Unit u, Letter letter)
	{
		letter.InkReady = true;
		if (!glyphInk.TryGetValue(letter.Key, out ID2D1GeometryRealization value))
		{
			ID2D1Factory1 d2DFactory = GpuDevice.Shared.D2DFactory;
			lock (fontLock)
			{
				if ((object)roundJoin == null)
				{
					roundJoin = d2DFactory.CreateStrokeStyle(new StrokeStyleProperties
					{
						StartCap = CapStyle.Flat,
						EndCap = CapStyle.Flat,
						DashCap = CapStyle.Flat,
						LineJoin = LineJoin.Round,
						MiterLimit = 10f,
						DashStyle = DashStyle.Solid
					});
				}
			}
			try
			{
				value = Outline(dc1, d2DFactory, letter.Glyph, letter.Key.Em, new Vector2(letter.Key.FractionX, letter.Key.FractionY), u.Dilation);
			}
			catch (Exception ex)
			{
				Log.Error("3D", "label glyph outline failed", ex);
			}
			glyphInk[letter.Key] = value;
		}
		letter.Ink = value;
	}

	private void BuildPixels(ID2D1DeviceContext1 dc1, Unit u, Matrix3x2 toPixels)
	{
		ReleasePixels(u);
		u.HasPixels = true;
		u.PixelsFor = toPixels;
		float m = toPixels.M11;
		float num = u.Size * m;
		float num2 = MathF.Min(0.0128f * num, 0.7f);
		if (num2 <= 0.01f)
		{
			num2 = 0f;
		}
		u.Dilation = num2;
		float num3 = ((num < 33.5f) ? 0.5f : 1f);
		Span<float> span = ((u.Baselines.Count > 16) ? ((Span<float>)new float[u.Baselines.Count]) : stackalloc float[u.Baselines.Count]);
		Span<float> span2 = span;
		for (int i = 0; i < span2.Length; i++)
		{
			float num4 = u.Baselines[i] * toPixels.M22 + toPixels.M32;
			span2[i] = MathF.Ceiling(num4 - 0.001f) - num2 / 2f;
		}
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		u.HasColor = false;
		foreach (Letter letter in u.Letters)
		{
			float x = MathF.Floor((letter.X * m + toPixels.M31 + 0.001f) / num3) * num3;
			Vector2 vector = new Vector2(x, span2[letter.Line] + letter.Glyph.Y * num);
			letter.PixelPivot = Vector2.Transform(letter.Pivot, toPixels);
			ColorLayer[] layers = letter.Glyph.Layers;
			if (layers != null)
			{
				LayerRun[] array = new LayerRun[layers.Length];
				for (int j = 0; j < layers.Length; j++)
				{
					array[j] = new LayerRun
					{
						Face = layers[j].Face,
						Glyph = new ushort[1] { layers[j].Index },
						Advance = new float[1],
						Origin = vector + new Vector2(layers[j].X * num, layers[j].Y * num),
						Color = layers[j].Color
					};
				}
				letter.Layers = array;
				Face face = letter.Glyph.Face;
				Include(vector.X, vector.Y - face.Ascent * num, vector.X + MathF.Max(letter.Glyph.Advance, face.Ascent) * num, vector.Y + face.Descent * num);
				u.HasColor = true;
			}
			else if (letter.Glyph.HasInk)
			{
				Vector2 inkOffset = new Vector2(MathF.Floor(vector.X), MathF.Floor(vector.Y));
				letter.Key = new GlyphKey(letter.Glyph.Face.Native.NativePointer, letter.Glyph.Index, num, vector.X - inkOffset.X, vector.Y - inkOffset.Y);
				letter.InkOffset = inkOffset;
				ShapedGlyph glyph = letter.Glyph;
				Include(vector.X + glyph.InkLeft * num, vector.Y + glyph.InkTop * num, vector.X + glyph.InkRight * num, vector.Y + glyph.InkBottom * num);
			}
		}
		if (minX > maxX)
		{
			u.BoxWidth = (u.BoxHeight = 0);
			return;
		}
		float num5 = MathF.Ceiling(0.07f * num + num2) + 2f;
		u.BoxX = (int)MathF.Floor(minX - num5);
		u.BoxY = (int)MathF.Floor(minY - num5);
		u.BoxWidth = (int)MathF.Ceiling(maxX + num5) - u.BoxX;
		u.BoxHeight = (int)MathF.Ceiling(maxY + num5) - u.BoxY;
		void Include(float left, float top, float right, float bottom)
		{
			minX = MathF.Min(minX, left);
			minY = MathF.Min(minY, top);
			maxX = MathF.Max(maxX, right);
			maxY = MathF.Max(maxY, bottom);
		}
	}

	private static ID2D1GeometryRealization? Outline(ID2D1DeviceContext1 dc1, ID2D1Factory1 d2d, ShapedGlyph glyph, float em, Vector2 origin, float dilation)
	{
		using ID2D1PathGeometry1 iD2D1PathGeometry = d2d.CreatePathGeometry();
		using (ID2D1GeometrySink iD2D1GeometrySink = iD2D1PathGeometry.Open())
		{
			glyph.Face.Native.GetGlyphRunOutline(em, new ushort[1] { glyph.Index }, new float[1], new GlyphOffset[1]
			{
				new GlyphOffset
				{
					AdvanceOffset = origin.X,
					AscenderOffset = 0f - origin.Y
				}
			}, false, false, iD2D1GeometrySink);
			iD2D1GeometrySink.Close();
		}
		RawRectF bounds = iD2D1PathGeometry.GetBounds();
		if (!(bounds.Right > bounds.Left) || !(bounds.Bottom > bounds.Top))
		{
			return null;
		}
		if (dilation <= 0f)
		{
			return dc1.CreateFilledGeometryRealization(iD2D1PathGeometry, 0.25f);
		}
		using ID2D1PathGeometry1 iD2D1PathGeometry2 = d2d.CreatePathGeometry();
		using (ID2D1GeometrySink iD2D1GeometrySink2 = iD2D1PathGeometry2.Open())
		{
			iD2D1PathGeometry.Widen(dilation, roundJoin, null, 0.25f, iD2D1GeometrySink2);
			iD2D1GeometrySink2.Close();
		}
		using ID2D1PathGeometry1 iD2D1PathGeometry3 = d2d.CreatePathGeometry();
		using (ID2D1GeometrySink iD2D1GeometrySink3 = iD2D1PathGeometry3.Open())
		{
			iD2D1PathGeometry.CombineWithGeometry(iD2D1PathGeometry2, CombineMode.Union, null, 0.25f, iD2D1GeometrySink3);
			iD2D1GeometrySink3.Close();
		}
		return dc1.CreateFilledGeometryRealization(iD2D1PathGeometry3, 0.25f);
	}

	private static void ReleasePixels(Unit u)
	{
		foreach (Letter letter in u.Letters)
		{
			letter.Ink = null;
			letter.InkReady = false;
			letter.Layers = null;
		}
		u.GlowInput?.Dispose();
		u.GlowInput = null;
		u.Settled?.Dispose();
		u.Settled = null;
		u.Picture?.Dispose();
		u.Picture = null;
		u.PictureSettled = false;
		u.HasPixels = false;
	}

	private static void Release(Unit u)
	{
		ReleasePixels(u);
	}

	private void EnsureGlowEffects(ID2D1DeviceContext context)
	{
		if (!(effectsContext == context) || (object)wideGlow == null || (object)tightGlow == null)
		{
			wideGlow?.Dispose();
			tightGlow?.Dispose();
			wideGlow = new Shadow(context);
			tightGlow = new Shadow(context);
			effectsContext = context;
		}
	}

	private static void DrawGlow(ID2D1DeviceContext context, Shadow shadow, ID2D1Image words, Color4 color, float sigma, Vector2 at)
	{
		shadow.SetInput(0u, words, true);
		shadow.BlurStandardDeviation = Math.Max(0f, sigma);
		shadow.Color = new Vector4(color.R, color.G, color.B, color.A);
		using ID2D1Image image = shadow.Output;
		context.DrawImage(image, at, null, InterpolationMode.Linear, CompositeMode.SourceOver);
	}

	private unsafe static void DrawRun(ID2D1DeviceContext context, Vector2 origin, nint fontFace, float emSize, ushort[] glyphs, float[] advances, ID2D1SolidColorBrush brush)
	{
		fixed (ushort* glyphIndices = glyphs)
		{
			fixed (float* glyphAdvances = advances)
			{
				NativeGlyphRun nativeGlyphRun = default(NativeGlyphRun);
				nativeGlyphRun.FontFace = fontFace;
				nativeGlyphRun.FontEmSize = emSize;
				nativeGlyphRun.GlyphCount = (uint)glyphs.Length;
				nativeGlyphRun.GlyphIndices = glyphIndices;
				nativeGlyphRun.GlyphAdvances = glyphAdvances;
				NativeGlyphRun nativeGlyphRun2 = nativeGlyphRun;
				nint nativePointer = context.NativePointer;
				((delegate* unmanaged[Stdcall]<nint, Point2F, NativeGlyphRun*, nint, int, void>)(*(IntPtr*)((nint)(*(IntPtr*)nativePointer) + (nint)29 * (nint)sizeof(void*))))(nativePointer, new Point2F
				{
					X = origin.X,
					Y = origin.Y
				}, &nativeGlyphRun2, brush.NativePointer, 0);
			}
		}
	}

	public void Dispose()
	{
		brush?.Dispose();
		brush = null;
		wideGlow?.Dispose();
		tightGlow?.Dispose();
		wideGlow = (tightGlow = null);
		effectsContext = null;
		foreach (Unit value in units.Values)
		{
			Release(value);
		}
		units.Clear();
		context1?.Dispose();
		context1 = null;
		context1For = IntPtr.Zero;
		foreach (ID2D1GeometryRealization value2 in glyphInk.Values)
		{
			value2?.Dispose();
		}
		glyphInk.Clear();
	}
}
