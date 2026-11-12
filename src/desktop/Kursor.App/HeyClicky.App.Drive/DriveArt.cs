using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Drive;

internal static class DriveArt
{
	private struct SketchRandom
	{
		private ulong state;

		public SketchRandom(int seed)
		{
			state = (ulong)(seed + -7046029254386353131L);
		}

		public double Next()
		{
			state += 11400714819323198485uL;
			ulong num = state;
			long num2 = (long)(num ^ (num >> 30)) * -4658895280553007687L;
			long num3 = (num2 ^ (num2 >>> 27)) * -7723592293110705685L;
			return (double)((ulong)(num3 ^ (num3 >>> 31)) >> 11) / 9007199254740992.0;
		}

		public double Uniform(double low, double high)
		{
			return low + (high - low) * Next();
		}
	}

	internal struct PaperRandom
	{
		private ulong state;

		public PaperRandom(ulong seed)
		{
			state = (ulong)(((long)seed * -7046029254386353131L) | 1);
		}

		private ulong NextBits()
		{
			state ^= state >> 12;
			state ^= state << 25;
			state ^= state >> 27;
			return state * 2685821657736338717L;
		}

		public double NextUnit()
		{
			return (double)(NextBits() >> 11) / 9007199254740992.0;
		}

		public static ulong StableSeed(string text)
		{
			ulong num = 14695981039346656037uL;
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			foreach (byte b in bytes)
			{
				num = (num ^ b) * 1099511628211L;
			}
			return num;
		}
	}

	public static readonly Color SkyWash = DS.Hex(14149119u);

	public static readonly Color SkyDeep = DS.Hex(1784460u);

	public static readonly Color NeedsYouOnBlack = DS.Hex(16758087u);

	private const string DriveGlyphPath = "M5.59 10.49 L8.49 7.07 L14.17 16.81 M14.17 16.81 L19.45 16.81 M15.80 10.49 L19.14 7.07 L22.35 10.49 M1.2 11.2h2.4 M1.8 14.6h2";

	public const double ShoulderRadius = 10.0;

	public static WriteableBitmap? DriveChip(double chipSize, double frameSize, double scale, int seed = 11)
	{
		return HandoffChip("M5.59 10.49 L8.49 7.07 L14.17 16.81 M14.17 16.81 L19.45 16.81 M15.80 10.49 L19.14 7.07 L22.35 10.49 M1.2 11.2h2.4 M1.8 14.6h2", SkyWash, SkyDeep, chipSize, frameSize, scale, seed, null);
	}

	public static WriteableBitmap? YourTurnChip(double chipSize, double frameSize, double scale, int seed = 11)
	{
		return HandoffChip(HandoffGlyphs.Path(HandoffGlyph.YourTurn), SkyWash, SkyDeep, chipSize, frameSize, scale, seed, null);
	}

	public static WriteableBitmap? HandoffChip(string? glyphPath, Color wash, Color deep, double chipSize, double frameSize, double scale, int seed, int? turnSeed = null, bool showsTape = true)
	{
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			int num = Math.Max(1, (int)Math.Ceiling(frameSize * scale));
			using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, num, num, 96f);
			using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
			{
				canvasDrawingSession.Clear(Color.FromArgb(0, 0, 0, 0));
				canvasDrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
				canvasDrawingSession.Transform = Matrix3x2.CreateScale((float)scale);
				float num2 = (float)((frameSize - chipSize) / 2.0);
				float num3 = (float)chipSize;
				Vector2 centerPoint = new Vector2(num2 + num3 / 2f, num2 + num3 / 2f);
				Matrix3x2 matrix3x2 = (canvasDrawingSession.Transform = Matrix3x2.CreateRotation((float)((double)((turnSeed ?? seed) % 7 - 3) * Math.PI / 180.0), centerPoint) * Matrix3x2.CreateScale((float)scale));
				Rect rect = new Rect(num2, num2, num3, num3);
				using (CanvasGeometry geometry = SketchRoundedRect(sharedDevice, rect, 5f * num3 / 24f, seed, 0.7))
				{
					canvasDrawingSession.FillGeometry(geometry, wash);
				}
				using (CanvasGeometry geometry2 = SketchRoundedRect(sharedDevice, rect, 5f * num3 / 24f, seed + 9, 0.6))
				{
					canvasDrawingSession.DrawGeometry(geometry2, deep.WithAlpha(0.55), 1.1f, new CanvasStrokeStyle
					{
						LineJoin = CanvasLineJoin.Round
					});
				}
				float num4 = num3 * 0.68f;
				float num5 = num4 / 24f;
				if (glyphPath != null)
				{
					using CanvasGeometry geometry3 = SvgStrokes(sharedDevice, glyphPath);
					Matrix3x2 transform = Matrix3x2.CreateScale(num5) * Matrix3x2.CreateTranslation(centerPoint.X - num4 / 2f, centerPoint.Y - num4 / 2f) * matrix3x2;
					canvasDrawingSession.Transform = transform;
					canvasDrawingSession.DrawGeometry(geometry3, deep, 1.9f / num5, new CanvasStrokeStyle
					{
						StartCap = CanvasCapStyle.Round,
						EndCap = CanvasCapStyle.Round,
						LineJoin = CanvasLineJoin.Round
					});
				}
				if (showsTape)
				{
					float num6 = num3 * 0.62f;
					float num7 = num3 * 0.24f;
					Vector2 centerPoint2 = new Vector2(num2 + num6 / 2f - num3 * 0.16f, num2 + num7 / 2f - num3 * 0.02f);
					canvasDrawingSession.Transform = Matrix3x2.CreateRotation((float)Math.PI * -8f / 45f, centerPoint2) * Matrix3x2.CreateScale((float)scale);
					Rect rect2 = new Rect(centerPoint2.X - num6 / 2f, centerPoint2.Y - num7 / 2f, num6, num7);
					using (CanvasCommandList canvasCommandList = new CanvasCommandList(sharedDevice))
					{
						using (CanvasDrawingSession canvasDrawingSession2 = canvasCommandList.CreateDrawingSession())
						{
							canvasDrawingSession2.FillRectangle(rect2, DS.Black(0.25));
						}
						using GaussianBlurEffect image = new GaussianBlurEffect
						{
							Source = canvasCommandList,
							BlurAmount = 0.3f * (float)scale,
							BorderMode = EffectBorderMode.Soft
						};
						canvasDrawingSession.DrawImage(image, new Vector2(0f, 0.4f));
					}
					canvasDrawingSession.FillRectangle(rect2, wash.WithAlpha(0.9));
				}
			}
			return Copy(canvasRenderTarget);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "chip not drawn: " + ex.Message);
			return null;
		}
	}

	private static CanvasGeometry SketchRoundedRect(CanvasDevice device, Rect rect, double radius, int seed, double jitter)
	{
		SketchRandom random = new SketchRandom(seed);
		double drift = 0.0;
		List<Vector2> list = RoundedRectPoints(rect, radius).Select(delegate(Vector2 point)
		{
			drift = drift * 0.6 + random.Uniform(0.0 - jitter, jitter) * 0.4;
			return new Vector2((float)((double)point.X + drift + random.Uniform(-0.25, 0.25)), (float)((double)point.Y + drift * 0.8 + random.Uniform(-0.25, 0.25)));
		}).ToList();
		list.AddRange(list.Take(2).ToList());
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		canvasPathBuilder.BeginFigure((list[0] + list[1]) / 2f);
		for (int i = 1; i < list.Count - 1; i++)
		{
			canvasPathBuilder.AddQuadraticBezier(list[i], (list[i] + list[i + 1]) / 2f);
		}
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	private static List<Vector2> RoundedRectPoints(Rect rect, double radius, double step = 7.0)
	{
		double r = Math.Min(radius, Math.Min(rect.Width / 2.0, rect.Height / 2.0));
		List<Vector2> points = new List<Vector2>();
		double x = rect.X;
		double y = rect.Y;
		double width = rect.Width;
		double height = rect.Height;
		Edge(x + r, y, x + width - r, y);
		Arc(x + width - r, y + r, -Math.PI / 2.0, 0.0);
		Edge(x + width, y + r, x + width, y + height - r);
		Arc(x + width - r, y + height - r, 0.0, Math.PI / 2.0);
		Edge(x + width - r, y + height, x + r, y + height);
		Arc(x + r, y + height - r, Math.PI / 2.0, Math.PI);
		Edge(x, y + height - r, x, y + r);
		Arc(x + r, y + r, Math.PI, 4.71238898038469);
		return points;
		void Arc(double cx, double cy, double start, double end)
		{
			int num3 = Math.Max(2, (int)(Math.Abs(end - start) * r / step) + 1);
			for (int j = 0; j < num3; j++)
			{
				double num4 = start + (end - start) * (double)j / (double)num3;
				points.Add(new Vector2((float)(cx + r * Math.Cos(num4)), (float)(cy + r * Math.Sin(num4))));
			}
		}
		void Edge(double fx, double fy, double tx, double ty)
		{
			int num = Math.Max(2, (int)(Math.Sqrt((tx - fx) * (tx - fx) + (ty - fy) * (ty - fy)) / step));
			for (int i = 0; i < num; i++)
			{
				double num2 = (double)i / (double)num;
				points.Add(new Vector2((float)(fx + (tx - fx) * num2), (float)(fy + (ty - fy) * num2)));
			}
		}
	}

	internal static CanvasGeometry SvgStrokes(CanvasDevice device, string data)
	{
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		List<string> tokens = (from m in Regex.Matches(data, "[MLHVCAZmlhvcaz]|-?\\d*\\.?\\d+")
			select m.Value).ToList();
		Vector2 vector = default(Vector2);
		Vector2 vector2 = default(Vector2);
		bool flag = false;
		char c = 'M';
		int i = 0;
		while (i < tokens.Count)
		{
			if (char.IsLetter(tokens[i][0]))
			{
				c = tokens[i++][0];
			}
			switch (c)
			{
			case 'M':
			case 'm':
			{
				Vector2 vector4 = new Vector2(Next(), Next());
				if (c == 'm')
				{
					vector4 += vector;
				}
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
				canvasPathBuilder.BeginFigure(vector4);
				flag = true;
				vector = (vector2 = vector4);
				c = ((c == 'M') ? 'L' : 'l');
				break;
			}
			case 'C':
			case 'c':
			{
				Vector2 controlPoint = new Vector2(Next(), Next());
				Vector2 controlPoint2 = new Vector2(Next(), Next());
				Vector2 vector6 = new Vector2(Next(), Next());
				if (c == 'c')
				{
					controlPoint += vector;
					controlPoint2 += vector;
					vector6 += vector;
				}
				canvasPathBuilder.AddCubicBezier(controlPoint, controlPoint2, vector6);
				vector = vector6;
				break;
			}
			case 'A':
			case 'a':
			{
				float radiusX = Next();
				float radiusY = Next();
				float num2 = Next();
				bool flag2 = Next() != 0f;
				bool flag3 = Next() != 0f;
				Vector2 vector5 = new Vector2(Next(), Next());
				if (c == 'a')
				{
					vector5 += vector;
				}
				canvasPathBuilder.AddArc(vector5, radiusX, radiusY, (float)((double)num2 * Math.PI / 180.0), flag3 ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise, flag2 ? CanvasArcSize.Large : CanvasArcSize.Small);
				vector = vector5;
				break;
			}
			case 'Z':
			case 'z':
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
				}
				flag = false;
				vector = vector2;
				c = 'M';
				break;
			case 'L':
			case 'l':
			{
				Vector2 vector3 = new Vector2(Next(), Next());
				vector = ((c == 'l') ? (vector + vector3) : vector3);
				canvasPathBuilder.AddLine(vector);
				break;
			}
			case 'H':
			case 'h':
			{
				float num3 = Next();
				vector = new Vector2((c == 'h') ? (vector.X + num3) : num3, vector.Y);
				canvasPathBuilder.AddLine(vector);
				break;
			}
			case 'V':
			case 'v':
			{
				float num = Next();
				vector = new Vector2(vector.X, (c == 'v') ? (vector.Y + num) : num);
				canvasPathBuilder.AddLine(vector);
				break;
			}
			default:
				i++;
				break;
			}
		}
		if (flag)
		{
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		}
		return CanvasGeometry.CreatePath(canvasPathBuilder);
		float Next()
		{
			return float.Parse(tokens[i++], CultureInfo.InvariantCulture);
		}
	}

	public static (WriteableBitmap? Image, double Width, double Height) Stamp(string word, Color color, double fontSize, double scale)
	{
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			double num = fontSize * 0.09;
			using CanvasTextFormat textFormat = new CanvasTextFormat
			{
				FontFamily = "Segoe UI Variable Display",
				FontSize = (float)fontSize,
				FontWeight = FontWeights.ExtraBold,
				WordWrapping = CanvasWordWrapping.NoWrap
			};
			using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(sharedDevice, word, textFormat, 1000f, 100f);
			canvasTextLayout.SetCharacterSpacing(0, word.Length, 0f, (float)num, 0f);
			double num2 = canvasTextLayout.LayoutBounds.Width - num;
			double num3 = Math.Ceiling(num2 + 16.0);
			double num4 = Math.Ceiling(fontSize + 10.0);
			float scale2 = (float)(scale * 3.0 / 3.0);
			using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, (float)Math.Ceiling(num3 * scale), (float)Math.Ceiling(num4 * scale), 96f);
			using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
			{
				canvasDrawingSession.Clear(Color.FromArgb(0, 0, 0, 0));
				canvasDrawingSession.Transform = Matrix3x2.CreateScale(scale2);
				canvasDrawingSession.DrawRoundedRectangle(new Rect(1.0, 1.0, num3 - 2.0, num4 - 2.0), 3.5f, 3.5f, color, 1.7f);
				Rect drawBounds = canvasTextLayout.DrawBounds;
				canvasDrawingSession.DrawTextLayout(canvasTextLayout, new Vector2((float)((num3 - num2) / 2.0 - canvasTextLayout.LayoutBounds.X), (float)(num4 / 2.0 - (drawBounds.Y + drawBounds.Height / 2.0))), color);
				canvasDrawingSession.Blend = CanvasBlend.Copy;
				PaperRandom paperRandom = new PaperRandom(PaperRandom.StableSeed(word) + 11);
				int num5 = (int)(num3 * num4 / 26.0);
				for (int i = 0; i < num5; i++)
				{
					double num6 = paperRandom.NextUnit() * num3;
					double num7 = num4 - paperRandom.NextUnit() * num4;
					double num8 = 0.18 + paperRandom.NextUnit() * 0.42;
					double num9 = num8 * (0.6 + paperRandom.NextUnit() * 0.8);
					canvasDrawingSession.FillEllipse((float)num6, (float)num7, (float)num8, (float)num9, Color.FromArgb(0, 0, 0, 0));
				}
			}
			return (Image: Copy(canvasRenderTarget), Width: num3, Height: num4);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "stamp not drawn: " + ex.Message);
			return (Image: null, Width: 0.0, Height: 0.0);
		}
	}

	public static CanvasGeometry NotchAttachedPath(CanvasDevice device, double width, double height, double bottomRadius)
	{
		float num = (float)width;
		float num2 = (float)height;
		float num3 = 10f;
		float num4 = (float)Math.Min(bottomRadius, (height - 10.0) / 2.0);
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		canvasPathBuilder.BeginFigure(0f, 0f);
		canvasPathBuilder.AddQuadraticBezier(new Vector2(num3, 0f), new Vector2(num3, num3));
		canvasPathBuilder.AddLine(num3, num2 - num4);
		canvasPathBuilder.AddQuadraticBezier(new Vector2(num3, num2), new Vector2(num3 + num4, num2));
		canvasPathBuilder.AddLine(num - num3 - num4, num2);
		canvasPathBuilder.AddQuadraticBezier(new Vector2(num - num3, num2), new Vector2(num - num3, num2 - num4));
		canvasPathBuilder.AddLine(num - num3, num3);
		canvasPathBuilder.AddQuadraticBezier(new Vector2(num - num3, 0f), new Vector2(num, 0f));
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	public static Color[] CloudColors(Color accent)
	{
		Color color = DS.Rgb(1.0, 0.62, 0.81);
		Color color2 = DS.Rgb(1.0, 0.86, 0.48);
		Color color3 = DS.Rgb(0.55, 0.79, 1.0);
		Color color4 = DS.Rgb(0.78, 0.66, 1.0);
		return new Color[7] { accent, color, color2, accent, color3, color4, accent };
	}

	public static (byte[] Bgra, int Width, int Height) EdgeGlowMask(double widthDips, double heightDips, double depth)
	{
		int num = Math.Max(2, (int)(widthDips * 0.5));
		int num2 = Math.Max(2, (int)(heightDips * 0.5));
		double sigma2 = Math.Max(0.5, depth * 0.5 * 0.95 / 2.0);
		double sigma3 = Math.Max(0.5, depth * 0.5 * 0.32 / 2.0);
		double[] array = Inside(num, sigma2);
		double[] array2 = Inside(num2, sigma2);
		double[] array3 = Inside(num, sigma3);
		double[] array4 = Inside(num2, sigma3);
		byte[] array5 = new byte[num * num2 * 4];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				double num3 = 1.0 - array[j] * array2[i];
				double num4 = 0.55 * (1.0 - array3[j] * array4[i]);
				byte b = (byte)Math.Round(Math.Clamp(num3 + num4 * (1.0 - num3), 0.0, 1.0) * 255.0);
				int num5 = (i * num + j) * 4;
				array5[num5] = b;
				array5[num5 + 1] = b;
				array5[num5 + 2] = b;
				array5[num5 + 3] = b;
			}
		}
		return (Bgra: array5, Width: num, Height: num2);
		static double[] Inside(int count, double sigma)
		{
			double[] array6 = new double[count];
			for (int k = 0; k < count; k++)
			{
				double num6 = (double)k + 0.5;
				array6[k] = Phi(num6 / sigma) + Phi(((double)count - num6) / sigma) - 1.0;
			}
			return array6;
		}
	}

	public static byte[] ConicGradient(int size, Color[] colors)
	{
		byte[] array = new byte[size * size * 4];
		double num = (double)size / 2.0;
		int num2 = colors.Length - 1;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				double num3 = Math.Atan2(num - ((double)j + 0.5), (double)i + 0.5 - num);
				double num4 = ((num3 < 0.0) ? (num3 + Math.PI * 2.0) : num3) / (Math.PI * 2.0) * (double)num2;
				int num5 = Math.Min(num2 - 1, (int)num4);
				double num6 = num4 - (double)num5;
				Color color = colors[num5];
				Color color2 = colors[num5 + 1];
				int num7 = (i * size + j) * 4;
				array[num7] = (byte)Math.Round((double)(int)color.B + (double)(color2.B - color.B) * num6);
				array[num7 + 1] = (byte)Math.Round((double)(int)color.G + (double)(color2.G - color.G) * num6);
				array[num7 + 2] = (byte)Math.Round((double)(int)color.R + (double)(color2.R - color.R) * num6);
				array[num7 + 3] = byte.MaxValue;
			}
		}
		return array;
	}

	private static double Phi(double x)
	{
		return 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));
	}

	private static double Erf(double x)
	{
		double num = Math.Sign(x);
		x = Math.Abs(x);
		double num2 = 1.0 / (1.0 + 0.3275911 * x);
		double num3 = 1.0 - ((((1.061405429 * num2 - 1.453152027) * num2 + 1.421413741) * num2 - 0.284496736) * num2 + 0.254829592) * num2 * Math.Exp((0.0 - x) * x);
		return num * num3;
	}

	internal static WriteableBitmap Copy(CanvasRenderTarget target)
	{
		byte[] pixelBytes = target.GetPixelBytes();
		WriteableBitmap writeableBitmap = new WriteableBitmap((int)target.SizeInPixels.Width, (int)target.SizeInPixels.Height);
		using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
		{
			stream.Write(pixelBytes, 0, pixelBytes.Length);
		}
		writeableBitmap.Invalidate();
		return writeableBitmap;
	}
}
