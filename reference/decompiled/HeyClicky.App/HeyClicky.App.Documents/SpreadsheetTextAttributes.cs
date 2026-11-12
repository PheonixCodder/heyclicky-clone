using System;
using System.Collections.Generic;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Documents;

public sealed class SpreadsheetTextAttributes : ISpreadsheetTextMeasurer
{
	public static readonly SpreadsheetTextAttributes Shared = new SpreadsheetTextAttributes();

	public static readonly Color DefaultTextColor = Rgb(0.12, 0.12, 0.12);

	public static readonly Color HyperlinkColor = Rgb(0.067, 0.333, 0.8);

	private const double SfAscender = 0.9521484375;

	private const double SfDescender = 0.2412109375;

	private readonly Dictionary<string, CanvasTextFormat> formats = new Dictionary<string, CanvasTextFormat>();

	private readonly Dictionary<string, double> baselines = new Dictionary<string, double>();

	public static Color Rgb(double r, double g, double b, double a = 1.0)
	{
		return Color.FromArgb((byte)Math.Round(a * 255.0), (byte)Math.Round(r * 255.0), (byte)Math.Round(g * 255.0), (byte)Math.Round(b * 255.0));
	}

	public static Color ColorOf(SpreadsheetColor c)
	{
		return Rgb(c.Red, c.Green, c.Blue, c.Alpha);
	}

	public static double Ascender(double size)
	{
		return size * 0.9521484375;
	}

	public static double LineHeight(double size)
	{
		return Math.Ceiling(size * 1.193359375);
	}

	public static double LabelHeight(double size)
	{
		return size * 1.193359375;
	}

	public static double WrappedLinePitch(double size)
	{
		return Math.Ceiling(size * 1.193359375) + 1.0;
	}

	private static string FamilyFor(double size)
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

	public CanvasTextFormat Format(double size, FontWeight weight, bool italic, bool wraps = false, CanvasHorizontalAlignment alignment = CanvasHorizontalAlignment.Left)
	{
		string key = $"{size}|{weight.Weight}|{italic}|{wraps}|{alignment}";
		if (formats.TryGetValue(key, out CanvasTextFormat value))
		{
			return value;
		}
		CanvasTextFormat canvasTextFormat = new CanvasTextFormat
		{
			FontFamily = FamilyFor(size),
			FontSize = (float)size,
			FontWeight = weight,
			FontStyle = (italic ? FontStyle.Italic : FontStyle.Normal),
			WordWrapping = ((!wraps) ? CanvasWordWrapping.NoWrap : CanvasWordWrapping.Wrap),
			HorizontalAlignment = alignment,
			Options = CanvasDrawTextOptions.EnableColorFont
		};
		if (wraps)
		{
			canvasTextFormat.LineSpacingMode = CanvasLineSpacingMode.Uniform;
			canvasTextFormat.LineSpacing = (float)WrappedLinePitch(size);
			canvasTextFormat.LineSpacingBaseline = (float)Ascender(size);
		}
		formats[key] = canvasTextFormat;
		return canvasTextFormat;
	}

	public CanvasTextFormat CellFormat(SpreadsheetCellStyle style, bool wraps = false, CanvasHorizontalAlignment alignment = CanvasHorizontalAlignment.Left)
	{
		return Format(style.FontSize, style.IsBold ? FontWeights.SemiBold : FontWeights.Normal, style.IsItalic, wraps, alignment);
	}

	public double Baseline(CanvasTextFormat format)
	{
		string key = $"{format.FontFamily}|{format.FontSize}|{format.FontWeight.Weight}|{format.FontStyle}";
		if (baselines.TryGetValue(key, out var value))
		{
			return value;
		}
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), "Ag", format, 1000f, 1000f);
		double num = ((canvasTextLayout.LineMetrics.Length != 0) ? ((double)canvasTextLayout.LineMetrics[0].Baseline) : ((double)format.FontSize * 0.8));
		baselines[key] = num;
		return num;
	}

	public CanvasTextLayout SingleLineLayout(ICanvasResourceCreator device, string text, CanvasTextFormat format, bool underline, bool strike)
	{
		CanvasTextLayout canvasTextLayout = new CanvasTextLayout(device, text, format, 100000f, 1000f);
		if (underline && text.Length > 0)
		{
			canvasTextLayout.SetUnderline(0, text.Length, hasUnderline: true);
		}
		if (strike && text.Length > 0)
		{
			canvasTextLayout.SetStrikethrough(0, text.Length, hasStrikethrough: true);
		}
		return canvasTextLayout;
	}

	public CanvasTextLayout WrappedLayout(ICanvasResourceCreator device, string text, SpreadsheetCellStyle style, double width, CanvasHorizontalAlignment alignment, bool underline)
	{
		CanvasTextLayout canvasTextLayout = new CanvasTextLayout(device, text, CellFormat(style, wraps: true, alignment), (float)Math.Max(1.0, width), 100000f);
		if ((style.IsUnderlined || underline) && text.Length > 0)
		{
			canvasTextLayout.SetUnderline(0, text.Length, hasUnderline: true);
		}
		if (style.IsStruckThrough && text.Length > 0)
		{
			canvasTextLayout.SetStrikethrough(0, text.Length, hasStrikethrough: true);
		}
		return canvasTextLayout;
	}

	public double SingleLineWidth(string text, SpreadsheetCellStyle style)
	{
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, CellFormat(style), 100000f, 1000f);
		return canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
	}

	public double WrappedHeight(string text, SpreadsheetCellStyle style, double width)
	{
		using CanvasTextLayout canvasTextLayout = WrappedLayout(CanvasDevice.GetSharedDevice(), text, style, width, CanvasHorizontalAlignment.Left, underline: false);
		return (double)canvasTextLayout.LineMetrics.Length * WrappedLinePitch(style.FontSize);
	}
}
