using System;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Design;

public static class DS
{
	public static class Colors
	{
		public static readonly Color Background = Hex(1645087u);

		public static readonly Color Surface1 = Hex(2039845u);

		public static readonly Color Surface2 = Hex(2500397u);

		public static readonly Color NotchCardSurface = Hex(1710618u);

		public static readonly Color TextPrimary = Hex(15527151u);

		public static readonly Color TextSecondary = Hex(9474976u);

		public static readonly Color TextTertiary = Hex(6053482u);

		public static readonly Color Success = Hex(366185u);

		public static readonly Color Destructive = Hex(15026253u);

		public static readonly Color Warning = Hex(16757284u);

		public static readonly Color Blue300 = Hex(9684477u);

		public static readonly Color Blue400 = Hex(6333946u);

		public static readonly Color BorderStrong = White(0.14);

		public static readonly Color BorderSubtle = White(0.08);

		public static readonly Color Black = Hex(0u);

		public static readonly Color Secondary = White(0.55);

		public static readonly Color CursorRed = Hex(16731212u);

		public static readonly Color CursorBlue = Hex(3375359u);

		public static readonly Color CursorYellow = Hex(13934615u);

		public static readonly Color CursorGreen = Hex(2540161u);

		public static readonly Color ListeningAccent = Rgb(0.4, 0.95, 1.0);

		public static readonly Color ThinkingAccent = Rgb(1.0, 0.5, 0.9);

		public static readonly Color SpeakingAccent = Rgb(1.0, 0.65, 0.35);

		public static readonly Color DictationAccent = Rgb(0.129, 0.773, 0.369);

		public static readonly Color GelDisabledContent = Hex(3425626u);
	}

	public static class Spacing
	{
		public const double Xxs = 2.0;

		public const double Xs = 4.0;

		public const double Sm = 8.0;

		public const double Md = 12.0;

		public const double Lg = 16.0;

		public const double Xl = 20.0;

		public const double Xxl = 24.0;

		public const double Xxxl = 32.0;
	}

	public static class Fonts
	{
		public static readonly FontFamily Small = new FontFamily("Segoe UI Variable Small");

		public static readonly FontFamily Text = new FontFamily("Segoe UI Variable Text");

		public static readonly FontFamily Display = new FontFamily("Segoe UI Variable Display");

		public static readonly FontFamily Mono = new FontFamily("Cascadia Mono, Consolas");

		public static readonly FontFamily Symbols = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

		public static FontFamily For(double size)
		{
			if (!(size < 12.0))
			{
				if (!(size >= 20.0))
				{
					return Text;
				}
				return Display;
			}
			return Small;
		}
	}

	public static class Weights
	{
		public static readonly FontWeight Regular = FontWeights.Normal;

		public static readonly FontWeight Medium = FontWeights.Medium;

		public static readonly FontWeight Semibold = FontWeights.SemiBold;

		public static readonly FontWeight Bold = FontWeights.Bold;
	}

	public static Color Hex(uint rgb, double alpha = 1.0)
	{
		return Color.FromArgb((byte)Math.Round(alpha * 255.0), (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
	}

	public static Color White(double alpha)
	{
		return Color.FromArgb((byte)Math.Round(alpha * 255.0), byte.MaxValue, byte.MaxValue, byte.MaxValue);
	}

	public static Color Black(double alpha)
	{
		return Color.FromArgb((byte)Math.Round(alpha * 255.0), 0, 0, 0);
	}

	public static Color Rgb(double r, double g, double b, double a = 1.0)
	{
		return Color.FromArgb((byte)Math.Round(a * 255.0), (byte)Math.Round(r * 255.0), (byte)Math.Round(g * 255.0), (byte)Math.Round(b * 255.0));
	}

	public static Color WithAlpha(this Color c, double alpha)
	{
		return Color.FromArgb((byte)Math.Round(alpha * (double)(int)c.A), c.R, c.G, c.B);
	}

	public static Color CursorColor(string? hexOrName)
	{
		if (string.IsNullOrWhiteSpace(hexOrName))
		{
			return Colors.CursorBlue;
		}
		string text = hexOrName.Trim().TrimStart('#');
		uint result;
		return text.ToLowerInvariant() switch
		{
			"red" => Colors.CursorRed, 
			"blue" => Colors.CursorBlue, 
			"yellow" => Colors.CursorYellow, 
			"green" => Colors.CursorGreen, 
			_ => (text.Length == 6 && uint.TryParse(text, NumberStyles.HexNumber, null, out result)) ? Hex(result) : Colors.CursorBlue, 
		};
	}

	public static string ToHex(Color c)
	{
		return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
	}
}
