using System;
using HeyClicky.App.Design;
using Windows.UI;

namespace HeyClicky.App.Dock;

public static class ArcColors
{
	public static readonly Color Ink = DS.Hex(1842724u);

	public static readonly Color Coral = DS.Hex(14242877u);

	public static readonly Color Working = DS.Hex(1755979u);

	public static readonly Color Blue = DS.Hex(3112182u);

	public static readonly Color TitleGreyEdge = DS.Hex(14079447u);

	public static readonly Color TitleGreyMiddle = DS.Hex(15790320u);

	public static Color Of((uint Rgb, double Opacity) c)
	{
		return DS.Hex(c.Rgb, c.Opacity);
	}

	public static Color State(ArcCardState state)
	{
		return Of(ArcCardStates.Color(state));
	}

	public static Color BlendWithWhite(Color c, double fraction)
	{
		return Color.FromArgb(c.A, (byte)Math.Round((double)(int)c.R + (double)(255 - c.R) * fraction), (byte)Math.Round((double)(int)c.G + (double)(255 - c.G) * fraction), (byte)Math.Round((double)(int)c.B + (double)(255 - c.B) * fraction));
	}

	public static Color Opacity(this Color c, double alpha)
	{
		return c.WithAlpha(alpha);
	}
}
