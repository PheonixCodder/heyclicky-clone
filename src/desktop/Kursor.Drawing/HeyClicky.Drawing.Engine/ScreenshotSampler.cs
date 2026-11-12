using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.Drawing.Engine;

internal sealed class ScreenshotSampler
{
	private byte[] pixels = Array.Empty<byte>();

	private int width;

	private int height;

	public static async Task<(byte[] Pixels, int Width, int Height)?> DecodeAsync(byte[] jpeg, int width, int height)
	{
		if (jpeg.Length == 0 || width <= 0 || height <= 0)
		{
			return null;
		}
		using IRandomAccessStream stream = new MemoryStream(jpeg).AsRandomAccessStream();
		BitmapDecoder obj = await BitmapDecoder.CreateAsync(stream);
		BitmapTransform transform = new BitmapTransform
		{
			ScaledWidth = (uint)width,
			ScaledHeight = (uint)height,
			InterpolationMode = BitmapInterpolationMode.Fant
		};
		return ((await obj.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData(), width, height);
	}

	public void Set((byte[] Pixels, int Width, int Height)? decoded)
	{
		if (decoded.HasValue)
		{
			(byte[], int, int) valueOrDefault = decoded.GetValueOrDefault();
			if (valueOrDefault.Item1.Length >= valueOrDefault.Item2 * valueOrDefault.Item3 * 4)
			{
				(pixels, width, height) = valueOrDefault;
				return;
			}
		}
		Clear();
	}

	public void Clear()
	{
		pixels = Array.Empty<byte>();
		width = (height = 0);
	}

	private static double Luminance(byte[] p, int index)
	{
		return (0.2126 * (double)(int)p[index] + 0.7152 * (double)(int)p[index + 1] + 0.0722 * (double)(int)p[index + 2]) / 255.0;
	}

	public double BusyFor(double x, double y, double w, double h)
	{
		if (pixels.Length == 0)
		{
			return 0.0;
		}
		int num = Math.Max(0, (int)Math.Floor(x));
		int num2 = Math.Max(0, (int)Math.Floor(y));
		int num3 = Math.Min(width - 2, (int)Math.Ceiling(x + w));
		int num4 = Math.Min(height - 2, (int)Math.Ceiling(y + h));
		if (num3 <= num || num4 <= num2)
		{
			return 0.0;
		}
		int num5 = Math.Max(1, (num4 - num2) / 160);
		int num6 = Math.Max(1, (num3 - num) / 160);
		byte[] p = pixels;
		double num7 = 0.0;
		long num8 = 0L;
		for (int i = num2; i <= num4; i += num5)
		{
			int num9 = (i * width + num) * 4;
			double num10 = Luminance(p, num9);
			for (int j = num; j < num3; j++)
			{
				num9 += 4;
				double num11 = Luminance(p, num9);
				num7 += Math.Abs(num11 - num10);
				num10 = num11;
			}
			num8 += num3 - num;
		}
		int num12 = width * 4;
		for (int k = num; k <= num3; k += num6)
		{
			int num13 = (num2 * width + k) * 4;
			double num14 = Luminance(p, num13);
			for (int l = num2; l < num4; l++)
			{
				num13 += num12;
				double num15 = Luminance(p, num13);
				num7 += Math.Abs(num15 - num14);
				num14 = num15;
			}
			num8 += num4 - num2;
		}
		if (num8 <= 0)
		{
			return 0.0;
		}
		return num7 / (double)num8;
	}

	public string InkFor(double x, double y, double w, double h)
	{
		double? num = BackgroundLuminance(x, y, w, h);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > 0.58)
			{
				return "#15171C";
			}
		}
		return "#FFFFFF";
	}

	public double? BackgroundLuminance(double x, double y, double w, double h)
	{
		if (pixels.Length == 0)
		{
			return null;
		}
		int num = Math.Max(0, (int)Math.Floor(x));
		int num2 = Math.Max(0, (int)Math.Floor(y));
		int num3 = Math.Min(width - 1, (int)Math.Ceiling(x + w));
		int num4 = Math.Min(height - 1, (int)Math.Ceiling(y + h));
		if (num3 <= num || num4 <= num2)
		{
			return null;
		}
		int num5 = Math.Max(1, (int)Math.Floor(Math.Sqrt((double)(num3 - num) * (double)(num4 - num2) / 400.0)));
		Span<double> span = stackalloc double[20];
		Span<int> span2 = stackalloc int[20];
		for (int i = num2; i <= num4; i += num5)
		{
			for (int j = num; j <= num3; j += num5)
			{
				double num6 = Luminance(pixels, (i * width + j) * 4);
				int index = Math.Min(19, (int)(num6 * 20.0));
				span[index] += num6;
				span2[index]++;
			}
		}
		int index2 = 0;
		for (int k = 1; k < 20; k++)
		{
			if (span2[k] > span2[index2])
			{
				index2 = k;
			}
		}
		if (span2[index2] <= 0)
		{
			return null;
		}
		return span[index2] / (double)span2[index2];
	}
}
