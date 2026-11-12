using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkDashboardFormNeeds
{
	public static Color Up => ClickyInkDashboardPalette.Up;

	public static Color Down => ClickyInkDashboardPalette.Down;

	public static Color Pencil => ClickyInkDashboardPalette.Pencil;

	public static Color Lamp(ClickyInkStatusState state)
	{
		return ClickyInkDashboardPalette.Lamp(state);
	}

	public static Color Word(ClickyInkStatusState state)
	{
		return ClickyInkDashboardPalette.Word(state);
	}

	public static Color CountdownColor(ClickyInkCountdownReading reading, ClickyInkStyle style)
	{
		return ClickyInkCountdown.Color(reading, style);
	}

	public static FrameworkElement RunStampMark(string? number, ClickyInkDocument.Block block, double clock, double landsAt, double fontSize = 11.0)
	{
		return ClickyInkRunStampMark.Create(number, block, clock, landsAt, fontSize);
	}

	public static FrameworkElement TickingNumber(ClickyInkDocument.Block block, string key, double clock, ClickyInkStyle style, double size = 30.0, double delay = 0.0, string? value = null, string? previous = null, bool showsWas = true)
	{
		return ClickyInkTickingNumber.Create(block, key, clock, style, size, delay, value, previous, showsWas);
	}

	public static FrameworkElement LampView(ClickyInkStatusState state, double diameter = 22.0, int seed = 1, double appear = 1.0)
	{
		return ClickyInkLamp.Create(state, null, 1.0, diameter, seed, appear);
	}

	public static FrameworkElement Quiet(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width, bool isHeadline = false)
	{
		return ClickyInkQuiet.Create(block, clock, style, width, isHeadline);
	}

	public static FrameworkElement Tiles(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style, double width, ClickyInkTiles.Look look = ClickyInkTiles.Look.Ruled)
	{
		return ClickyInkTiles.Create(blocks, clock, style, width, null, look);
	}

	public static FrameworkElement Tile(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, int index)
	{
		return ClickyInkTile.Create(block, clock, style, index, ClickyInkTiles.Look.Ruled, null);
	}

	public static string TileStateWord(string word)
	{
		return ClickyInkTile.StateWord(word);
	}

	public static double TileHeight(ClickyInkDocument.Block block)
	{
		return ClickyInkTile.Height(block, null);
	}

	public static FrameworkElement SparkLine(IReadOnlyList<double> values, ClickyInkStyle style, int seed = 1, double height = 12.0)
	{
		double? height2 = height;
		return ClickyInkSparkLine.Create(values, null, 1.0, 1.0, null, style, null, seed, marksNew: true, null, height2);
	}

	public static FrameworkElement ProgressBar(double fraction, double height, ClickyInkStyle style, int seed = 1)
	{
		return ClickyInkProgressBar.Create(fraction, null, 1.0, 1.0, height, style, seed, null);
	}
}
