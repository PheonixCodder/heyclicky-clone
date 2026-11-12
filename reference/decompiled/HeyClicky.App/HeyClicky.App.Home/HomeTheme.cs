using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Home;

public static class HomeTheme
{
	public static readonly Color PaneFill = DS.Hex(1973790u);

	public static readonly Color SidebarFill = DS.Hex(1842203u);

	public static readonly Color SearchFieldFill = DS.Hex(2236961u);

	public static readonly Color RowDivider = DS.Hex(3881787u);

	public static readonly Color SelectionBlue = DS.Hex(37375u);

	public static readonly Color IncomingBubble = DS.Hex(3881789u);

	public static readonly Color ComposerFieldFill = DS.Hex(2302755u);

	public static readonly Color ComposerFieldStroke = DS.Hex(4276545u);

	public static readonly Color NamePillFill = DS.Hex(2697513u);

	public static readonly Color AvatarPlate = DS.Hex(3815996u);

	public static readonly Color PrimaryText = DS.Hex(16053492u);

	public static readonly Color SecondaryText = DS.Hex(10790052u);

	public static readonly Color TertiaryText = DS.Hex(10066329u);

	public static readonly Color FailureRed = DS.Hex(16729402u);

	public static readonly Color ToolbarGlyph = DS.Hex(14474460u);

	public static readonly Color WindowControlGlyph = DS.Hex(7895162u);

	public static readonly Color LiveOrange = DS.Hex(14251863u);

	public static readonly Color ThemeTextPrimary = DS.Rgb(0.925, 0.925, 0.937);

	public static readonly Color ThemeTextSecondary = DS.Rgb(0.565, 0.576, 0.627);

	public static readonly Color ThemeTextTertiary = DS.Rgb(0.361, 0.369, 0.416);

	public static readonly Color[] CardPastels = new Color[6]
	{
		DS.Hex(16772808u),
		DS.Hex(14149119u),
		DS.Hex(16763105u),
		DS.Hex(13890272u),
		DS.Hex(14999039u),
		DS.Hex(16768200u)
	};

	public const double SheetCornerRadius = 26.0;

	public const double DetachedCornerRadius = 12.0;

	public const double TopFlareRadius = 10.0;

	public const double CanvasSide = 16.0;

	public const double CanvasBottom = 40.0;

	public const double ShadowOpacity = 0.18;

	public const double ShadowRadius = 6.0;

	public const double ShadowY = 3.0;

	public const double AttachedScale = 0.7;

	public const double MinWidth = 720.0;

	public const double MinHeight = 520.0;

	public const double OpenSeconds = 0.43;

	public const double CloseSeconds = 0.3;

	public const double ContentFadeSeconds = 0.18;

	public const double HoverForgiveness = 28.0;

	public const double HoverGraceSeconds = 0.15;

	public static readonly TimeSpan RestoreWindow = TimeSpan.FromMinutes(4L);

	public static Color SurfaceAt(double y)
	{
		double num = Math.Clamp((y - 14.0) / 130.0, 0.0, 1.0);
		double[] array = new double[6] { 0.0, 0.22, 0.42, 0.64, 0.84, 1.0 };
		double[] array2 = new double[6] { 0.0, 0.25, 0.5, 0.75, 0.92, 1.0 };
		double num2 = 1.0;
		for (int i = 1; i < array.Length; i++)
		{
			if (num <= array[i])
			{
				num2 = array2[i - 1] + (array2[i] - array2[i - 1]) * (num - array[i - 1]) / (array[i] - array[i - 1]);
				break;
			}
		}
		return DS.Rgb((double)(int)PaneFill.R / 255.0 * num2, (double)(int)PaneFill.G / 255.0 * num2, (double)(int)PaneFill.B / 255.0 * num2);
	}

	public static LinearGradientBrush TopBandFade(double height)
	{
		List<(double, Color)> list = new List<(double, Color)>();
		int[] array = new int[9] { 0, 14, 30, 45, 60, 80, 100, 120, 144 };
		foreach (double num in array)
		{
			if (num < height * 0.75)
			{
				list.Add((num / height, SurfaceAt(num)));
			}
		}
		list.Add((0.75, SurfaceAt(height * 0.75)));
		list.Add((1.0, SurfaceAt(height).WithAlpha(0.0)));
		return Ui.Vertical(list.ToArray());
	}

	public static (double W, double H) FullSurface(double vw, double vh)
	{
		double val = Math.Max(1250.0, Math.Round(vw * 0.7));
		double val2 = Math.Max(850.0, Math.Round(vh * 0.78));
		return (W: Math.Max(720.0, Math.Min(val, vw - 24.0)), H: Math.Max(520.0, Math.Min(val2, vh - 12.0)));
	}

	public static (double W, double H) AttachedSize(double vw, double vh, (double W, double H)? saved)
	{
		(double, double) tuple = FullSurface(vw, vh);
		return Clamp(saved ?? (Math.Round(tuple.Item1 * 0.7), Math.Round(tuple.Item2 * 0.7)), vw, vh);
	}

	public static (double W, double H) Clamp((double W, double H) req, double vw, double vh)
	{
		return (W: Math.Max(720.0, Math.Min(req.W, vw - 24.0)), H: Math.Max(520.0, Math.Min(req.H, vh - 12.0)));
	}
}
