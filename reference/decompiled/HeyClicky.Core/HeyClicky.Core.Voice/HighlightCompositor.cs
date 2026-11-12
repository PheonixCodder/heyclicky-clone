using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.Core.Voice;

public static class HighlightCompositor
{
	private enum StrokeShape
	{
		Circle,
		Line,
		Scribble
	}

	private const double HighlighterLineWidthPoints = 8.0;

	public static bool StrokesTouch(DisplayInfo display, IReadOnlyList<HighlightStroke> strokes)
	{
		double num = 8.0 * display.Scale;
		RectD area = new RectD(display.BoundsPx.X - num, display.BoundsPx.Y - num, display.BoundsPx.Width + 2.0 * num, display.BoundsPx.Height + 2.0 * num);
		return strokes.Any((HighlightStroke s) => s.PointsPx.Any(((RectD)area).Contains));
	}

	public static async Task<ScreenCaptureFrame> CompositeAsync(ScreenCaptureFrame frame, IReadOnlyList<HighlightStroke> strokes, string colorHex)
	{
		List<HighlightStroke> drawn = strokes.Where((HighlightStroke s) => s.PointsPx.Count >= 2).ToList();
		if (drawn.Count == 0 || !StrokesTouch(frame.Display, drawn))
		{
			return frame;
		}
		try
		{
			using InMemoryRandomAccessStream input = new InMemoryRandomAccessStream();
			await input.WriteAsync(frame.Jpeg.AsBuffer());
			input.Seek(0uL);
			BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(input);
			int width = (int)bitmapDecoder.PixelWidth;
			int height = (int)bitmapDecoder.PixelHeight;
			byte[] pixels = (await bitmapDecoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
			(byte R, byte G, byte B) tuple = ParseHex(colorHex);
			byte item = tuple.R;
			byte item2 = tuple.G;
			byte item3 = tuple.B;
			double scaleX = (double)width / Math.Max(1.0, frame.Display.BoundsPx.Width);
			double scaleY = (double)height / Math.Max(1.0, frame.Display.BoundsPx.Height);
			double num = 8.0 * frame.Display.Scale * scaleX;
			List<(double, double)[]> paths = drawn.Select((HighlightStroke s) => s.PointsPx.Select((PointD p) => ((p.X - frame.Display.BoundsPx.X) * scaleX, (p.Y - frame.Display.BoundsPx.Y) * scaleY)).ToArray()).ToList();
			PaintPass(pixels, width, height, paths, num * 1.35, 0.22, item, item2, item3);
			PaintPass(pixels, width, height, paths, num, 0.55, item, item2, item3);
			using InMemoryRandomAccessStream output = new InMemoryRandomAccessStream();
			BitmapPropertySet encodingOptions = new BitmapPropertySet { ["ImageQuality"] = new BitmapTypedValue(0.8, PropertyType.Single) };
			BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output, encodingOptions);
			obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, pixels);
			await obj.FlushAsync();
			byte[] bytes = new byte[output.Size];
			output.Seek(0uL);
			await output.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);
			return frame with
			{
				Jpeg = bytes
			};
		}
		catch (Exception ex)
		{
			Log.Warn("Highlight", "highlight not composited: " + ex.Message);
			return frame;
		}
	}

	internal static void PaintPass(byte[] bgra, int width, int height, List<(double X, double Y)[]> paths, double lineWidth, double alpha, byte r, byte g, byte b)
	{
		double num = Math.Max(0.5, lineWidth / 2.0);
		byte[] array = new byte[width * height];
		foreach ((double, double)[] path in paths)
		{
			for (int i = 0; i < path.Length - 1; i++)
			{
				(double, double) tuple = path[i];
				double item = tuple.Item1;
				double item2 = tuple.Item2;
				(double, double) tuple2 = path[i + 1];
				double item3 = tuple2.Item1;
				double item4 = tuple2.Item2;
				int num2 = Math.Max(0, (int)Math.Floor(Math.Min(item, item3) - num - 1.0));
				int num3 = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(item, item3) + num + 1.0));
				int num4 = Math.Max(0, (int)Math.Floor(Math.Min(item2, item4) - num - 1.0));
				int num5 = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(item2, item4) + num + 1.0));
				double num6 = item3 - item;
				double num7 = item4 - item2;
				double num8 = num6 * num6 + num7 * num7;
				for (int j = num4; j <= num5; j++)
				{
					for (int k = num2; k <= num3; k++)
					{
						double num9 = (double)k + 0.5 - item;
						double num10 = (double)j + 0.5 - item2;
						double num11 = ((num8 > 0.0) ? Math.Clamp((num9 * num6 + num10 * num7) / num8, 0.0, 1.0) : 0.0);
						double num12 = num9 - num11 * num6;
						double num13 = num10 - num11 * num7;
						double num14 = Math.Sqrt(num12 * num12 + num13 * num13);
						double num15 = Math.Clamp(num + 0.5 - num14, 0.0, 1.0);
						if (!(num15 <= 0.0))
						{
							byte b2 = (byte)(num15 * 255.0);
							int num16 = j * width + k;
							if (b2 > array[num16])
							{
								array[num16] = b2;
							}
						}
					}
				}
			}
		}
		for (int l = 0; l < array.Length; l++)
		{
			if (array[l] != 0)
			{
				double num17 = alpha * (double)(int)array[l] / 255.0;
				int num18 = l * 4;
				bgra[num18] = (byte)((double)(int)bgra[num18] * (1.0 - num17) + (double)(int)b * num17);
				bgra[num18 + 1] = (byte)((double)(int)bgra[num18 + 1] * (1.0 - num17) + (double)(int)g * num17);
				bgra[num18 + 2] = (byte)((double)(int)bgra[num18 + 2] * (1.0 - num17) + (double)(int)r * num17);
			}
		}
	}

	private static (byte R, byte G, byte B) ParseHex(string hex)
	{
		string text = hex.TrimStart('#');
		if (text.Length < 6 || !uint.TryParse(text.AsSpan(0, 6), NumberStyles.HexNumber, null, out var result))
		{
			return (R: 51, G: 128, B: byte.MaxValue);
		}
		return (R: (byte)(result >> 16), G: (byte)(result >> 8), B: (byte)result);
	}

	public static string? GestureSummary(IReadOnlyList<HighlightStroke> strokes, IReadOnlyList<DisplayInfo> displays)
	{
		List<HighlightStroke> list = strokes.Where((HighlightStroke s) => s.PointsPx.Count >= 2).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		List<string> list2 = list.Select((HighlightStroke s) => DescribeStroke(s, displays)).ToList();
		string text;
		if (list2.Count == 1)
		{
			text = "the user " + list2[0];
		}
		else
		{
			string[] ordinals = new string[6] { "first", "then", "then", "then", "then", "then" };
			text = $"the user drew {list2.Count} marks in order — " + string.Join(", ", list2.Select((string d, int i) => ((i < ordinals.Length) ? ordinals[i] : "then") + " " + d));
		}
		return "While asking, " + text + ". Treat this as exactly what they are pointing at.";
	}

	private static string DescribeStroke(HighlightStroke stroke, IReadOnlyList<DisplayInfo> displays)
	{
		IReadOnlyList<PointD> pointsPx = stroke.PointsPx;
		StrokeShape strokeShape = ClassifyShape(pointsPx);
		string text = RegionLabel(Centroid(pointsPx), displays);
		string text2 = RegionLabel(pointsPx[0], displays);
		string text3 = RegionLabel(pointsPx[pointsPx.Count - 1], displays);
		string text4 = strokeShape switch
		{
			StrokeShape.Circle => "circled the " + text + " area", 
			StrokeShape.Line => (text2 == text3) ? ("drew a line across the " + text + " area") : ("drew a line from the " + text2 + " toward the " + text3), 
			_ => "scribbled over the " + text + " area", 
		};
		if (strokeShape != StrokeShape.Line && text2 != text3)
		{
			text4 = text4 + ", moving from the " + text2 + " toward the " + text3;
		}
		if (stroke.DwellsPx.Count > 0)
		{
			text4 = text4 + ", pausing on the " + RegionLabel(stroke.DwellsPx[0], displays);
		}
		return text4;
	}

	private static StrokeShape ClassifyShape(IReadOnlyList<PointD> points)
	{
		if (points.Count < 2)
		{
			return StrokeShape.Scribble;
		}
		double num = points.Min((PointD p) => p.X);
		double num2 = points.Max((PointD p) => p.X);
		double num3 = points.Min((PointD p) => p.Y);
		double num4 = points.Max((PointD p) => p.Y);
		double num5 = num2 - num;
		double num6 = num4 - num3;
		double num7 = Math.Sqrt(num5 * num5 + num6 * num6);
		if (num7 <= 1.0)
		{
			return StrokeShape.Scribble;
		}
		double num8 = Math.Max(num5, num6);
		double num9 = Math.Min(num5, num6);
		if (num8 > 1.0 && num9 / num8 < 0.2)
		{
			return StrokeShape.Line;
		}
		PointD pointD = Centroid(points);
		double num10 = 0.0;
		double num11 = Math.Atan2(points[0].Y - pointD.Y, points[0].X - pointD.X);
		for (int i = 1; i < points.Count; i++)
		{
			double num12 = Math.Atan2(points[i].Y - pointD.Y, points[i].X - pointD.X);
			double num13;
			for (num13 = num12 - num11; num13 > Math.PI; num13 -= Math.PI * 2.0)
			{
			}
			for (; num13 < -Math.PI; num13 += Math.PI * 2.0)
			{
			}
			num10 += num13;
			num11 = num12;
		}
		double num14 = Math.Sqrt(Math.Pow(points[points.Count - 1].X - points[0].X, 2.0) + Math.Pow(points[points.Count - 1].Y - points[0].Y, 2.0));
		if (!(Math.Abs(num10) >= 3.9269908169872414) && !(num14 < num7 * 0.45))
		{
			return StrokeShape.Scribble;
		}
		return StrokeShape.Circle;
	}

	private static PointD Centroid(IReadOnlyList<PointD> points)
	{
		return new PointD(points.Average((PointD p) => p.X), points.Average((PointD p) => p.Y));
	}

	private static string RegionLabel(PointD point, IReadOnlyList<DisplayInfo> displays)
	{
		RectD rectD = displays.FirstOrDefault((DisplayInfo d) => d.BoundsPx.Contains(point))?.BoundsPx ?? displays.FirstOrDefault()?.BoundsPx ?? new RectD(0.0, 0.0, 1.0, 1.0);
		double num = (point.X - rectD.X) / Math.Max(1.0, rectD.Width);
		double num2 = (point.Y - rectD.Y) / Math.Max(1.0, rectD.Height);
		string text = ((num < 0.34) ? "left" : ((num < 0.67) ? "center" : "right"));
		string text2 = ((num2 < 0.34) ? "top" : ((num2 < 0.67) ? "middle" : "bottom"));
		string text3 = text2;
		string text4 = text;
		if (text3 == "middle")
		{
			if (text4 == "center")
			{
				return "center";
			}
			return text4;
		}
		if (text4 == "center")
		{
			return text3;
		}
		return text2 + "-" + text;
	}
}
