using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Windows.Foundation;
using Windows.UI.Text;

namespace HeyClicky.Drawing.Engine;

internal sealed class TextShaper
{
	public sealed class ShapedLine
	{
		public required double Advance { get; init; }

		public required double LayoutAdvance { get; init; }

		public required Glyph[] Glyphs { get; init; }

		public bool HasOutlines { get; init; }
	}

	public sealed class Glyph
	{
		public CanvasGeometry? Outline { get; init; }

		public required double X { get; init; }

		public required double Advance { get; init; }

		public ColorGlyph? Color { get; init; }
	}

	public sealed record ColorGlyph(CanvasFontFace Face, float Size, ushort Index, float Advance, string Text);

	public readonly record struct FontMetrics(double Ascent, double Descent, double XHeight);

	public sealed class Font
	{
		public required CanvasFontFace Face { get; init; }

		public required float Size { get; init; }

		public required FontMetrics Metrics { get; init; }

		public required string Key { get; init; }

		public required CanvasFontFace[] Fallbacks { get; init; }
	}

	private sealed class BundledFace
	{
		public required int Weight;

		public required CanvasFontFace Face;

		public required double Ascent;

		public required double Descent;

		public required double XHeight;
	}

	[CompilerGenerated]
	private sealed class _003CFamilies_003Ed__13 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string cssFamilyList;

		public string _003C_003E3__cssFamilyList;

		private string[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		string IEnumerator<string>.Current
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
		public _003CFamilies_003Ed__13(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E7__wrap1 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				goto IL_0081;
			}
			_003C_003E1__state = -1;
			_003C_003E7__wrap1 = cssFamilyList.Split(',');
			_003C_003E7__wrap2 = 0;
			goto IL_008f;
			IL_0081:
			_003C_003E7__wrap2++;
			goto IL_008f;
			IL_008f:
			if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
			{
				string text = _003C_003E7__wrap1[_003C_003E7__wrap2].Trim().Trim('\'', '"').ToLowerInvariant();
				if (text.Length > 0)
				{
					_003C_003E2__current = text;
					_003C_003E1__state = 1;
					return true;
				}
				goto IL_0081;
			}
			_003C_003E7__wrap1 = null;
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CFamilies_003Ed__13 _003CFamilies_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CFamilies_003Ed__ = this;
			}
			else
			{
				_003CFamilies_003Ed__ = new _003CFamilies_003Ed__13(0);
			}
			_003CFamilies_003Ed__.cssFamilyList = _003C_003E3__cssFamilyList;
			return _003CFamilies_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private readonly CanvasDevice device;

	private readonly Dictionary<string, List<BundledFace>> bundled = new Dictionary<string, List<BundledFace>>(StringComparer.Ordinal);

	private readonly Dictionary<string, Font> fontCache = new Dictionary<string, Font>(StringComparer.Ordinal);

	private readonly Dictionary<string, ShapedLine> lineCache = new Dictionary<string, ShapedLine>(StringComparer.Ordinal);

	private readonly Dictionary<string, CanvasFontFace?> systemFaces = new Dictionary<string, CanvasFontFace>(StringComparer.OrdinalIgnoreCase);

	private CanvasFontFace[]? symbolFallbacks;

	public TextShaper(CanvasDevice device, string fontsDirectory)
	{
		this.device = device;
		(string, int, string)[] array = new(string, int, string)[4]
		{
			("patrick hand", 400, "ClickyDraw-PatrickHand.ttf"),
			("barlow", 400, "ClickyDraw-Barlow-Regular.ttf"),
			("barlow", 500, "ClickyDraw-Barlow-Medium.ttf"),
			("barlow", 600, "ClickyDraw-Barlow-SemiBold.ttf")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, int, string) tuple = array[i];
			string text = Path.Combine(fontsDirectory, tuple.Item3);
			try
			{
				CanvasFontSet canvasFontSet = new CanvasFontSet(new Uri(text));
				if (canvasFontSet.Fonts.Count == 0)
				{
					Log.Warn("Drawing", "font " + tuple.Item3 + " has no faces");
					continue;
				}
				CanvasFontFace face = canvasFontSet.Fonts[0];
				var (ascent, descent, xHeight) = ReadCoreTextMetrics(File.ReadAllBytes(text));
				if (!bundled.TryGetValue(tuple.Item1, out List<BundledFace> value))
				{
					value = (bundled[tuple.Item1] = new List<BundledFace>());
				}
				value.Add(new BundledFace
				{
					Weight = tuple.Item2,
					Face = face,
					Ascent = ascent,
					Descent = descent,
					XHeight = xHeight
				});
			}
			catch (Exception ex)
			{
				Log.Error("Drawing", "font " + tuple.Item3 + " did not load", ex);
			}
		}
	}

	[IteratorStateMachine(typeof(_003CFamilies_003Ed__13))]
	private static IEnumerable<string> Families(string cssFamilyList)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CFamilies_003Ed__13(-2)
		{
			_003C_003E3__cssFamilyList = cssFamilyList
		};
	}

	public Font GetFont(string cssFamilyList, double size, int weight)
	{
		global::_003C_003Ey__InlineArray5<string> buffer = default(global::_003C_003Ey__InlineArray5<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 0) = cssFamilyList;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 1) = "|";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 2) = size.ToString("R", CultureInfo.InvariantCulture);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 3) = "|";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray5<string>, string>(ref buffer, 4) = weight.ToString();
		string key = string.Concat(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray5<string>, string>(in buffer, 5));
		if (fontCache.TryGetValue(key, out Font value))
		{
			return value;
		}
		Font font = ResolveFont(cssFamilyList, (float)size, weight, key);
		fontCache[key] = font;
		return font;
	}

	private Font ResolveFont(string cssFamilyList, float size, int weight, string key)
	{
		foreach (string item in Families(cssFamilyList))
		{
			if (item != "system-ui" && bundled.TryGetValue(item, out List<BundledFace> value))
			{
				BundledFace bundledFace = value.MinBy((BundledFace w) => Math.Abs(w.Weight - weight));
				return new Font
				{
					Face = bundledFace.Face,
					Size = size,
					Key = key,
					Fallbacks = SymbolFallbacks(),
					Metrics = new FontMetrics(bundledFace.Ascent * (double)size, bundledFace.Descent * (double)size, bundledFace.XHeight * (double)size)
				};
			}
			string text;
			switch (item)
			{
			case "system-ui":
			case "-apple-system":
				text = "Segoe UI Variable Text";
				break;
			case "sans-serif":
				text = "Arial";
				break;
			case "serif":
				text = "Times New Roman";
				break;
			case "monospace":
				text = "Courier New";
				break;
			case "cursive":
				text = "Comic Sans MS";
				break;
			case "fantasy":
				text = "Impact";
				break;
			default:
				text = item;
				break;
			}
			string family = text;
			CanvasFontFace canvasFontFace = SystemFace(family, weight);
			if ((object)canvasFontFace != null)
			{
				return new Font
				{
					Face = canvasFontFace,
					Size = size,
					Key = key,
					Fallbacks = SymbolFallbacks(),
					Metrics = new FontMetrics(canvasFontFace.Ascent * size, canvasFontFace.Descent * size, canvasFontFace.LowercaseLetterHeight * size)
				};
			}
		}
		CanvasFontFace canvasFontFace2 = SystemFace("Arial", weight) ?? SymbolFallbacks()[0];
		return new Font
		{
			Face = canvasFontFace2,
			Size = size,
			Key = key,
			Fallbacks = SymbolFallbacks(),
			Metrics = new FontMetrics(canvasFontFace2.Ascent * size, canvasFontFace2.Descent * size, canvasFontFace2.LowercaseLetterHeight * size)
		};
	}

	private CanvasFontFace? SystemFace(string family, int weight)
	{
		string key = family + "|" + weight;
		if (systemFaces.TryGetValue(key, out CanvasFontFace value))
		{
			return value;
		}
		CanvasFontFace canvasFontFace = null;
		try
		{
			CanvasFontSet matchingFonts = CanvasFontSet.GetSystemFontSet().GetMatchingFonts(family, new FontWeight((ushort)Math.Clamp(weight, 1, 999)), FontStretch.Normal, FontStyle.Normal);
			if (matchingFonts.Fonts.Count > 0)
			{
				canvasFontFace = matchingFonts.Fonts[0];
			}
		}
		catch
		{
		}
		systemFaces[key] = canvasFontFace;
		return canvasFontFace;
	}

	private CanvasFontFace[] SymbolFallbacks()
	{
		if (symbolFallbacks != null)
		{
			return symbolFallbacks;
		}
		List<CanvasFontFace> list = new List<CanvasFontFace>();
		string[] array = new string[5] { "Segoe UI Variable Text", "Segoe UI", "Segoe UI Symbol", "Segoe UI Emoji", "Arial" };
		foreach (string family in array)
		{
			CanvasFontFace canvasFontFace = SystemFace(family, 400);
			if ((object)canvasFontFace != null)
			{
				list.Add(canvasFontFace);
			}
		}
		symbolFallbacks = list.ToArray();
		return symbolFallbacks;
	}

	public double SyntheticBoldStrokeWidth(string cssFamilyList, double size, int weight)
	{
		if (weight < 600)
		{
			return 0.0;
		}
		foreach (string item in Families(cssFamilyList))
		{
			if (item == "system-ui")
			{
				return 0.0;
			}
			if (bundled.TryGetValue(item, out List<BundledFace> value))
			{
				if (value.Count != 1)
				{
					return 0.0;
				}
				double num = Math.Clamp((size - 9.0) / 27.0, 0.0, 1.0);
				double num2 = 1.0 / 24.0 + -0.010416666666666664 * num;
				return size * num2;
			}
		}
		return 0.0;
	}

	public ShapedLine Shape(string text, Font font)
	{
		string key = font.Key + "\u0001" + text;
		if (lineCache.TryGetValue(key, out ShapedLine value))
		{
			return value;
		}
		ShapedLine shapedLine = ShapeUncached(text, font);
		if (lineCache.Count > 2000)
		{
			lineCache.Clear();
		}
		lineCache[key] = shapedLine;
		return shapedLine;
	}

	public double LayoutWidth(string text, Font font)
	{
		return Shape(text, font).LayoutAdvance;
	}

	private ShapedLine ShapeUncached(string text, Font font)
	{
		if (text.Length == 0)
		{
			return new ShapedLine
			{
				Advance = 0.0,
				LayoutAdvance = 0.0,
				Glyphs = Array.Empty<Glyph>()
			};
		}
		bool flag = !NeedsWholeRunShaping(text);
		List<(int, int, CanvasFontFace)> list = new List<(int, int, CanvasFontFace)>();
		int units;
		for (int i = 0; i < text.Length; i += units)
		{
			int codepoint = CodepointAt(text, i, out units);
			CanvasFontFace canvasFontFace = FaceFor(codepoint, font);
			if (list.Count > 0)
			{
				if ((object)list[list.Count - 1].Item3 == canvasFontFace)
				{
					if (list[list.Count - 1].Item1 + list[list.Count - 1].Item2 == i)
					{
						list[list.Count - 1] = (list[list.Count - 1].Item1, list[list.Count - 1].Item2 + units, canvasFontFace);
						continue;
					}
				}
			}
			list.Add((i, units, canvasFontFace));
		}
		List<(CanvasFontFace, ushort, float, float, Vector2, int, bool)> list2 = new List<(CanvasFontFace, ushort, float, float, Vector2, int, bool)>();
		float num = 0f;
		double num2 = 0.0;
		foreach (var item2 in list)
		{
			string text2 = text.Substring(item2.Item1, item2.Item2);
			CanvasGlyph[] array;
			int[] clusterMapIndicesElements;
			try
			{
				CanvasTextAnalyzer canvasTextAnalyzer = new CanvasTextAnalyzer(text2, CanvasTextDirection.LeftToRightThenTopToBottom);
				IReadOnlyList<KeyValuePair<CanvasCharacterRange, CanvasAnalyzedScript>> script = canvasTextAnalyzer.GetScript();
				array = canvasTextAnalyzer.GetGlyphs(script: (script.Count > 0) ? script[0].Value : default(CanvasAnalyzedScript), characterRange: new CanvasCharacterRange
				{
					CharacterIndex = 0,
					CharacterCount = text2.Length
				}, fontFace: item2.Item3, fontSize: font.Size, isSideways: false, isRightToLeft: false, locale: "en-us", numberSubstitution: null, typographyRanges: null, clusterMapIndicesElements: out clusterMapIndicesElements, isShapedAloneGlyphsElements: out var _, glyphShapingResultsElements: out var _);
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "text shaping failed for a run: " + ex.Message);
				continue;
			}
			bool item = IsColorFace(item2.Item3);
			int[] array2 = new int[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				array2[j] = -1;
			}
			for (int num3 = clusterMapIndicesElements.Length - 1; num3 >= 0; num3--)
			{
				if (clusterMapIndicesElements[num3] < array.Length)
				{
					array2[clusterMapIndicesElements[num3]] = item2.Item1 + num3;
				}
			}
			for (int k = 0; k < array.Length; k++)
			{
				if (array2[k] < 0)
				{
					int num4 = k;
					int num5;
					if (k <= 0)
					{
						(num5, _, _) = item2;
					}
					else
					{
						num5 = array2[k - 1];
					}
					array2[num4] = num5;
				}
				list2.Add((item2.Item3, (ushort)array[k].Index, num, array[k].Advance, new Vector2(array[k].AdvanceOffset, 0f - array[k].AscenderOffset), array2[k], item));
				num += array[k].Advance;
			}
			if (flag)
			{
				num2 += CharacterAdvanceSum(text2, item2.Item3, font.Size, array);
			}
		}
		double num6 = num;
		if (flag)
		{
			float num7 = 0f;
			for (int l = 0; l < list2.Count; l++)
			{
				(CanvasFontFace, ushort, float, float, Vector2, int, bool) value = list2[l];
				value.Item3 += num7;
				bool flag2 = l + 1 < list2.Count && IsSpace(list2[l + 1].Item6);
				if (IsSpace(value.Item6) || flag2)
				{
					float num8 = NominalAdvance(value.Item1, value.Item2, font.Size);
					num7 += num8 - value.Item4;
					value.Item4 = num8;
				}
				list2[l] = value;
			}
			num6 += (double)num7;
		}
		Glyph[] array3 = new Glyph[list2.Count];
		bool hasOutlines = false;
		for (int m = 0; m < list2.Count; m++)
		{
			(CanvasFontFace, ushort, float, float, Vector2, int, bool) tuple2 = list2[m];
			CanvasGeometry outline = null;
			ColorGlyph color = null;
			if (tuple2.Item7)
			{
				color = new ColorGlyph(tuple2.Item1, font.Size, tuple2.Item2, tuple2.Item4, ClusterText(text, list2, m));
			}
			else if (!IsWhiteSpace(text, tuple2.Item6))
			{
				try
				{
					CanvasGlyph[] glyphs = new CanvasGlyph[1]
					{
						new CanvasGlyph
						{
							Index = tuple2.Item2,
							Advance = tuple2.Item4,
							AdvanceOffset = tuple2.Item5.X,
							AscenderOffset = 0f - tuple2.Item5.Y
						}
					};
					CanvasGeometry canvasGeometry = CanvasGeometry.CreateGlyphRun(device, new Vector2(tuple2.Item3, 0f), tuple2.Item1, font.Size, glyphs, isSideways: false, 0u, CanvasTextMeasuringMode.Natural, CanvasGlyphOrientation.Upright);
					Rect rect = canvasGeometry.ComputeBounds();
					if (rect.Width > 0.0 || rect.Height > 0.0)
					{
						outline = canvasGeometry;
						hasOutlines = true;
					}
					else
					{
						canvasGeometry.Dispose();
					}
				}
				catch
				{
				}
			}
			array3[m] = new Glyph
			{
				Outline = outline,
				X = tuple2.Item3,
				Advance = tuple2.Item4,
				Color = color
			};
		}
		return new ShapedLine
		{
			Advance = num6,
			LayoutAdvance = (flag ? num2 : num6),
			Glyphs = array3,
			HasOutlines = hasOutlines
		};
		bool IsSpace(int stringIndex)
		{
			bool flag3 = stringIndex >= 0 && stringIndex < text.Length;
			if (flag3)
			{
				char c = text[stringIndex];
				bool flag4 = ((c == '\t' || c == ' ' || c == '\u00a0') ? true : false);
				flag3 = flag4;
			}
			return flag3;
		}
	}

	private static bool IsColorFace(CanvasFontFace face)
	{
		foreach (string value in face.FamilyNames.Values)
		{
			if (value.Contains("Emoji", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static int CodepointAt(string text, int index, out int units)
	{
		if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
		{
			units = 2;
			return char.ConvertToUtf32(text[index], text[index + 1]);
		}
		units = 1;
		return text[index];
	}

	private static string ClusterText(string text, List<(CanvasFontFace Face, ushort Index, float X, float Advance, Vector2 Offset, int StringIndex, bool IsColor)> records, int g)
	{
		int num = Math.Clamp(records[g].StringIndex, 0, text.Length);
		int val = text.Length;
		for (int i = g + 1; i < records.Count; i++)
		{
			if (records[i].StringIndex > num)
			{
				val = records[i].StringIndex;
				break;
			}
		}
		int num2 = num;
		return text.Substring(num2, Math.Max(num, val) - num2);
	}

	private static bool IsWhiteSpace(string text, int index)
	{
		if (index >= 0 && index < text.Length)
		{
			return char.IsWhiteSpace(text[index]);
		}
		return false;
	}

	private CanvasFontFace FaceFor(int codepoint, Font font)
	{
		if (codepoint < 32 || font.Face.HasCharacter((uint)codepoint))
		{
			return font.Face;
		}
		CanvasFontFace[] fallbacks = font.Fallbacks;
		foreach (CanvasFontFace canvasFontFace in fallbacks)
		{
			if (canvasFontFace.HasCharacter((uint)codepoint))
			{
				return canvasFontFace;
			}
		}
		return font.Face;
	}

	private static float NominalAdvance(CanvasFontFace face, ushort glyphIndex, float size)
	{
		CanvasGlyphMetrics[] glyphMetrics = face.GetGlyphMetrics(new int[1] { glyphIndex }, isSideways: false);
		if (glyphMetrics.Length == 0)
		{
			return 0f;
		}
		return glyphMetrics[0].AdvanceWidth * size;
	}

	private static double CharacterAdvanceSum(string runText, CanvasFontFace face, float size, CanvasGlyph[] shapedGlyphs)
	{
		List<uint> list = new List<uint>(runText.Length);
		int num;
		for (num = 0; num < runText.Length; num++)
		{
			int units;
			int item = CodepointAt(runText, num, out units);
			list.Add((uint)item);
			num += units - 1;
		}
		int[] glyphIndices = face.GetGlyphIndices(list.ToArray());
		if (glyphIndices.Contains(0))
		{
			double num2 = 0.0;
			for (int i = 0; i < shapedGlyphs.Length; i++)
			{
				CanvasGlyph canvasGlyph = shapedGlyphs[i];
				num2 += (double)canvasGlyph.Advance;
			}
			return num2;
		}
		CanvasGlyphMetrics[] glyphMetrics = face.GetGlyphMetrics(glyphIndices, isSideways: false);
		double num3 = 0.0;
		CanvasGlyphMetrics[] array = glyphMetrics;
		for (int i = 0; i < array.Length; i++)
		{
			CanvasGlyphMetrics canvasGlyphMetrics = array[i];
			num3 += (double)canvasGlyphMetrics.AdvanceWidth;
		}
		return num3 * (double)size;
	}

	private static bool NeedsWholeRunShaping(string text)
	{
		foreach (int num2 in text)
		{
			if (num2 <= 57343)
			{
				if (num2 <= 8207)
				{
					if (num2 <= 7679)
					{
						if (num2 <= 879)
						{
							if (num2 >= 768)
							{
								goto IL_00a9;
							}
						}
						else if (num2 >= 1425)
						{
							goto IL_00a9;
						}
					}
					else if (num2 >= 8204)
					{
						goto IL_00a9;
					}
				}
				else if (num2 <= 8447)
				{
					if (num2 >= 8400)
					{
						goto IL_00a9;
					}
				}
				else if (num2 >= 55296)
				{
					goto IL_00a9;
				}
			}
			else if (num2 >= 65056)
			{
				if (num2 >= 65136)
				{
					if (num2 <= 65279)
					{
						goto IL_00a9;
					}
				}
				else if (num2 <= 65071)
				{
					goto IL_00a9;
				}
			}
			else if (num2 <= 65023)
			{
				if (num2 >= 64285)
				{
					goto IL_00a9;
				}
			}
			else if (num2 <= 65039)
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

	private static (double Ascent, double Descent, double XHeight) ReadCoreTextMetrics(byte[] font)
	{
		int num = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
		int num2 = -1;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < num; i++)
		{
			int num5 = 12 + i * 16;
			string @string = Encoding.ASCII.GetString(font, num5, 4);
			int num6 = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(num5 + 8));
			switch (@string)
			{
			case "head":
				num2 = num6;
				break;
			case "hhea":
				num3 = num6;
				break;
			case "OS/2":
				num4 = num6;
				break;
			}
		}
		double num7 = ((num2 >= 0) ? BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(num2 + 18)) : 1000);
		double num8 = ((num3 >= 0) ? BinaryPrimitives.ReadInt16BigEndian(font.AsSpan(num3 + 4)) : 800);
		double value = ((num3 >= 0) ? (-BinaryPrimitives.ReadInt16BigEndian(font.AsSpan(num3 + 6))) : 200);
		double num9 = 0.5 * num7;
		if (num4 >= 0 && BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(num4)) >= 2)
		{
			num9 = BinaryPrimitives.ReadInt16BigEndian(font.AsSpan(num4 + 86));
		}
		return (Ascent: num8 / num7, Descent: Math.Abs(value) / num7, XHeight: num9 / num7);
	}
}
