using System;

namespace HeyClicky.App.Dock;

public static class TopArcLayout
{
	public static readonly double[] Scales = new double[4] { 0.7, 0.27, 0.2, 0.16 };

	public const double PeekScale = 0.44;

	public const double PeekDrop = 10.0;

	public const double PeekTiltKept = 0.35;

	public static readonly double[] Spreads = new double[4] { 0.0, 275.0, 400.0, 510.0 };

	public static readonly double[] Lifts = new double[4] { 0.0, 8.0, 15.0, 22.0 };

	public static readonly double[] Opacities = new double[4] { 1.0, 1.0, 1.0, 0.0 };

	public const int VisibleSideCount = 2;

	public const double DegreesPerStep = 4.5;

	public const double TopGap = 6.0;

	public const double CardBottomPadding = 22.0;

	public const double FooterHeight = 44.0;

	public const double CardTopGap = 20.0;

	public const double CardSidePadding = 28.0;

	public const double EdgeMargin = 16.0;

	private static double WidthFit(ArcScreenMetrics m)
	{
		return m.PanelWidth / 1512.0;
	}

	private static double ScaleFit(ArcScreenMetrics m)
	{
		return Math.Min(1.0, Math.Min(WidthFit(m), (m.PanelHeight - 120.0) / 760.0));
	}

	private static double ScaleAt(int distance, double fit)
	{
		return Scales[distance] * (0.85 + 0.15 * fit);
	}

	public static (double W, double H) FrontCardSize(double panelWidth, double panelHeight)
	{
		double num = ScaleAt(0, ScaleFit(new ArcScreenMetrics(panelWidth, panelHeight, 0.0, null, null)));
		return (W: 494.0 * num, H: 314.0 * num);
	}

	public static double FrontTop(ArcScreenMetrics m)
	{
		double item = FrontCardSize(m.PanelWidth, m.PanelHeight).H;
		return Math.Max(m.MenuBarHeight + 6.0, (m.NotchSurface?.MaxY ?? 0.0) - 22.0 - 44.0 - item);
	}

	public static (double X, double Y) NotchMouth(ArcScreenMetrics m)
	{
		ArcRect? arcRect = m.NotchSurface ?? m.HardwareNotch;
		return (X: arcRect?.MidX ?? (m.PanelWidth / 2.0), Y: Math.Max(arcRect?.MaxY ?? m.MenuBarHeight, m.MenuBarHeight) - 8.0);
	}

	public static double ArcDepth(ArcScreenMetrics m)
	{
		return FrontTop(m) + 314.0 * ScaleAt(0, ScaleFit(m)) + 90.0;
	}

	public static ArcPlacement Placement(int index, int focus, int? hovered, ArcScreenMetrics m, bool isOpen)
	{
		double num = ScaleFit(m);
		double num2 = Math.Min(1.3, Math.Max(0.7, WidthFit(m)));
		int num3 = index - focus;
		int num4 = Math.Min(Math.Abs(num3), 3);
		bool flag = hovered == index && num3 != 0 && Opacities[num4] > 0.0;
		double num5 = ScaleAt(num4, num);
		double num6 = (flag ? Math.Max(num5, 0.44 * (0.85 + 0.15 * num)) : num5);
		(double, double) tuple = NotchMouth(m);
		double item = tuple.Item1;
		double num7 = ((num3 < 0) ? (item - 16.0) : (m.PanelWidth - 16.0 - item));
		double num8 = Spreads[2] * num2 + 494.0 * ScaleAt(2, num) / 2.0;
		double num9 = Math.Min(1.0, Math.Max(0.55, num7 / num8));
		double num10 = (flag ? (494.0 * (num6 - num5) / 2.0) : 0.0);
		double num11 = Spreads[num4] * num2 * num9 + num10;
		double num12 = 314.0 * num6;
		double num13 = FrontTop(m) - Lifts[num4] * num + (flag ? (10.0 * num) : 0.0);
		double degrees = (double)(-Math.Max(-3, Math.Min(3, num3))) * 4.5 * (flag ? 0.35 : 1.0);
		if (!isOpen)
		{
			return new ArcPlacement(tuple.Item1, tuple.Item2, num6 * 0.2, 0.0, 0.0, 10 - num4);
		}
		return new ArcPlacement(item + ((num3 < 0) ? (0.0 - num11) : num11), num13 + num12 / 2.0, num6, degrees, Opacities[num4], 10 - num4);
	}

	public static ArcRect VisibleFrame(int index, int focus, int? hovered, ArcScreenMetrics m)
	{
		ArcRect result = Frame(index);
		int num = index - focus;
		if (num == 0)
		{
			return result;
		}
		ArcRect arcRect = Frame(index + ((num < 0) ? 1 : (-1)));
		if (num < 0)
		{
			return new ArcRect(result.X, result.Y, Math.Max(6.0, arcRect.X - result.X), result.H);
		}
		double num2 = Math.Max(result.X, arcRect.MaxX);
		return new ArcRect(num2, result.Y, Math.Max(6.0, result.MaxX - num2), result.H);
		ArcRect Frame(int i)
		{
			return Placement(i, focus, hovered, m, isOpen: true).Frame;
		}
	}

	public static (double X, double Y) MorePillCenter(ArcPlacement outermost, int direction, double panelWidth)
	{
		ArcRect frame = outermost.Frame;
		return (X: (direction < 0) ? Math.Max(52.0, frame.X - 46.0) : Math.Min(panelWidth - 52.0, frame.MaxX + 46.0), Y: frame.MidY);
	}
}
