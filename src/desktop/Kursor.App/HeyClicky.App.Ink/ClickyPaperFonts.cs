using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public static class ClickyPaperFonts
{
	public const bool WritesInHandwriting = false;

	private const double SystemFontSizeScale = 0.86;

	private const double HeadingSizeThreshold = 22.0;

	private static readonly Dictionary<string, PaperFont> cache = new Dictionary<string, PaperFont>();

	private static string? PatrickHandPath
	{
		get
		{
			string[] obj = new string[2]
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
			obj[1] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray5<string>, string>(in buffer, 5));
			return obj.FirstOrDefault(File.Exists);
		}
	}

	private static string SystemFamilyName(double size)
	{
		if (!(size < 12.0))
		{
			if (!(size >= 20.0))
			{
				return "Segoe UI Variable Text";
			}
			return "Segoe UI Variable Display";
		}
		return "Segoe UI Variable Small";
	}

	private static PaperFont SystemFont(double size, FontWeight weight)
	{
		string familyName = SystemFamilyName(size);
		return new PaperFont(DS.Fonts.For(size), familyName, size, weight);
	}

	public static PaperFont Hand(double size)
	{
		string key = "hand|" + size.ToString(CultureInfo.InvariantCulture);
		if (cache.TryGetValue(key, out PaperFont value))
		{
			return value;
		}
		FontWeight weight = ((size >= 22.0) ? FontWeights.SemiBold : FontWeights.Normal);
		PaperFont paperFont = SystemFont(Math.Round(size * 0.86, MidpointRounding.ToEven), weight);
		cache[key] = paperFont;
		return paperFont;
	}

	public static PaperFont Voice(double size, double weight = 500.0)
	{
		return SystemFont(size, FontWeights.Medium);
	}

	public static PaperFont Typed(double size, FontWeight? weight = null)
	{
		return SystemFont(size, weight ?? FontWeights.Normal);
	}

	public static PaperFont BigNumber(double size)
	{
		return new PaperFont(DS.Fonts.Display, "Segoe UI Variable Display", size, FontWeights.Bold);
	}

	public static PaperFont Rounded(double size, FontWeight weight)
	{
		return new PaperFont(DS.Fonts.For(size), SystemFamilyName(size), size, weight);
	}
}
