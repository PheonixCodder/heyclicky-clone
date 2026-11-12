using System;

namespace HeyClicky.App.Notch.Peek;

public static class ClickyDrawingTint
{
	public const int SampleSide = 32;

	public static (double Hue, double Saturation, double Brightness)? DominantColor(byte[] rgba, int side = 32)
	{
		double[] array = new double[24];
		double[] array2 = new double[24];
		double[] array3 = new double[24];
		double[] array4 = new double[24];
		double[] array5 = new double[24];
		for (int i = 0; i + 3 < Math.Min(rgba.Length, side * side * 4); i += 4)
		{
			double num = (double)(int)rgba[i] / 255.0;
			double num2 = (double)(int)rgba[i + 1] / 255.0;
			double num3 = (double)(int)rgba[i + 2] / 255.0;
			double num4 = Math.Max(num, Math.Max(num2, num3));
			double num5 = Math.Min(num, Math.Min(num2, num3));
			double num6 = num4 - num5;
			if (num4 > 0.12 && num6 > 0.08)
			{
				double num7 = ((num4 == num) ? ((num2 - num3) / num6 % 6.0) : ((num4 != num2) ? ((num - num2) / num6 + 4.0) : ((num3 - num) / num6 + 2.0)));
				num7 /= 6.0;
				if (num7 < 0.0)
				{
					num7 += 1.0;
				}
				double num8 = num6 / num4;
				double num9 = num8 * num8 * num4;
				int num10 = Math.Min(23, (int)(num7 * 24.0));
				array[num10] += num9;
				array2[num10] += num8 * num9;
				array3[num10] += num4 * num9;
				array4[num10] += Math.Sin(num7 * 2.0 * Math.PI) * num9;
				array5[num10] += Math.Cos(num7 * 2.0 * Math.PI) * num9;
			}
		}
		int num11 = -1;
		double num12 = 0.0;
		for (int j = 0; j < 24; j++)
		{
			double num13 = array[j] + 0.5 * (array[(j + 1) % 24] + array[(j + 24 - 1) % 24]);
			if (num13 > num12)
			{
				num12 = num13;
				num11 = j;
			}
		}
		if (num11 < 0 || num12 <= (double)(side * side) * 0.012 || array[num11] <= 0.0)
		{
			return null;
		}
		double num14 = Math.Atan2(array4[num11], array5[num11]) / (Math.PI * 2.0);
		if (num14 < 0.0)
		{
			num14 += 1.0;
		}
		return (num14, array2[num11] / array[num11], array3[num11] / array[num11]);
	}

	public static (byte R, byte G, byte B) Pastel((double Hue, double Saturation, double Brightness) color)
	{
		double saturation = Math.Min(0.42, Math.Max(0.18, color.Saturation * 0.55));
		return FromHsb(color.Hue, saturation, 0.97);
	}

	public static (byte R, byte G, byte B) FromHsb(double hue, double saturation, double brightness)
	{
		double num = (hue - Math.Floor(hue)) * 6.0;
		int num2 = (int)Math.Floor(num) % 6;
		double num3 = num - Math.Floor(num);
		double num4 = brightness * (1.0 - saturation);
		double num5 = brightness * (1.0 - saturation * num3);
		double num6 = brightness * (1.0 - saturation * (1.0 - num3));
		var (v2, v3, v4) = num2 switch
		{
			0 => (brightness, num6, num4), 
			1 => (num5, brightness, num4), 
			2 => (num4, brightness, num6), 
			3 => (num4, num5, brightness), 
			4 => (num6, num4, brightness), 
			_ => (brightness, num4, num5), 
		};
		return (R: Byte(v2), G: Byte(v3), B: Byte(v4));
		static byte Byte(double v)
		{
			return (byte)Math.Round(Math.Clamp(v, 0.0, 1.0) * 255.0);
		}
	}
}
