using System;
using System.Globalization;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class PaperColor
{
	public static readonly Color White = Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public static readonly Color Black = Color.FromArgb(byte.MaxValue, 0, 0, 0);

	public static readonly Color Clear = Color.FromArgb(0, 0, 0, 0);

	public static Color Hex(string hex)
	{
		string text = hex.Trim('#');
		ulong.TryParse(text, NumberStyles.HexNumber, null, out var result);
		bool flag = text.Length == 8;
		byte r = (byte)((result >> (flag ? 24 : 16)) & 0xFF);
		byte g = (byte)((result >> (flag ? 16 : 8)) & 0xFF);
		byte b = (byte)((result >> (flag ? 8 : 0)) & 0xFF);
		return Color.FromArgb(flag ? ((byte)(result & 0xFF)) : byte.MaxValue, r, g, b);
	}

	public static Color Opacity(this Color c, double opacity)
	{
		return Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * (double)(int)c.A), c.R, c.G, c.B);
	}

	public static Color Brightness(this Color c, double amount)
	{
		return Color.FromArgb(c.A, Add(c.R, amount), Add(c.G, amount), Add(c.B, amount));
		static byte Add(byte v, double a)
		{
			return (byte)Math.Clamp(Math.Round((double)(int)v + a * 255.0), 0.0, 255.0);
		}
	}

	public static Color Saturation(this Color c, double amount)
	{
		double l2 = 0.2126 * (double)(int)c.R + 0.7152 * (double)(int)c.G + 0.0722 * (double)(int)c.B;
		return Color.FromArgb(c.A, Mix((int)c.R, l2, amount), Mix((int)c.G, l2, amount), Mix((int)c.B, l2, amount));
		static byte Mix(double v, double l, double a)
		{
			return (byte)Math.Clamp(Math.Round(l + (v - l) * a), 0.0, 255.0);
		}
	}
}
