using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HeyClicky.Drawing.Engine;

internal sealed class TextLayout
{
	public readonly record struct Line(double OriginX, double OriginY, TextShaper.ShapedLine Shaped);

	public required List<Line> Lines { get; init; }

	public required (double X, double Y, double Width, double Height) Box { get; init; }

	public required TextShaper.Font Font { get; init; }

	public required double Size { get; init; }

	public required double SyntheticBoldStrokeWidth { get; init; }

	public static TextLayout Make(TextShaper shaper, JsonElement description)
	{
		JsonElement value;
		string cssFamilyList = ((description.TryGetProperty("family", out value) && value.ValueKind == JsonValueKind.String) ? value.GetString() : "sans-serif");
		JsonElement value2;
		double size = ((description.TryGetProperty("size", out value2) && value2.ValueKind == JsonValueKind.Number) ? value2.GetDouble() : 16.0);
		JsonElement value3;
		int weight = ((description.TryGetProperty("weight", out value3) && value3.ValueKind == JsonValueKind.Number) ? ((int)value3.GetDouble()) : 400);
		JsonElement value4;
		string text = ((description.TryGetProperty("anchor", out value4) && value4.ValueKind == JsonValueKind.String) ? value4.GetString() : "start");
		JsonElement value5;
		string text2 = ((description.TryGetProperty("baseline", out value5) && value5.ValueKind == JsonValueKind.String) ? value5.GetString() : "auto");
		TextShaper.Font font = shaper.GetFont(cssFamilyList, size, weight);
		TextShaper.FontMetrics metrics = font.Metrics;
		double num;
		switch (text2)
		{
		case "middle":
			num = metrics.XHeight / 2.0;
			break;
		case "central":
			num = (metrics.Ascent - metrics.Descent) / 2.0;
			break;
		case "hanging":
			num = metrics.Ascent * 0.8;
			break;
		case "mathematical":
			num = metrics.Ascent / 2.0;
			break;
		case "before-edge":
		case "text-before-edge":
		case "text-top":
			num = metrics.Ascent;
			break;
		case "ideographic":
		case "text-bottom":
		case "text-after-edge":
		case "after-edge":
			num = 0.0 - metrics.Descent;
			break;
		default:
			num = 0.0;
			break;
		}
		double num2 = num;
		JsonElement value6;
		double num3 = ((description.TryGetProperty("x", out value6) && value6.ValueKind == JsonValueKind.Number) ? value6.GetDouble() : 0.0);
		JsonElement value7;
		double num4 = ((description.TryGetProperty("y", out value7) && value7.ValueKind == JsonValueKind.Number) ? value7.GetDouble() : 0.0);
		List<Line> list = new List<Line>();
		double num5 = double.PositiveInfinity;
		double num6 = double.PositiveInfinity;
		double num7 = double.NegativeInfinity;
		double num8 = double.NegativeInfinity;
		if (description.TryGetProperty("lines", out var value8) && value8.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value8.EnumerateArray())
			{
				if (item.TryGetProperty("x", out var value9) && value9.ValueKind == JsonValueKind.Number)
				{
					num3 = value9.GetDouble();
				}
				if (item.TryGetProperty("dy", out var value10) && value10.ValueKind == JsonValueKind.Number)
				{
					num4 += value10.GetDouble();
				}
				JsonElement value11;
				string text3 = (item.TryGetProperty("text", out value11) ? ((value11.ValueKind == JsonValueKind.String) ? value11.GetString() : value11.ToString()) : "");
				TextShaper.ShapedLine shapedLine = shaper.Shape(text3, font);
				num = ((text == "middle") ? (num3 - shapedLine.LayoutAdvance / 2.0) : ((!(text == "end")) ? num3 : (num3 - shapedLine.LayoutAdvance)));
				double num9 = num;
				double num10 = num4 + num2;
				list.Add(new Line(num9, num10, shapedLine));
				num5 = Math.Min(num5, num9);
				num7 = Math.Max(num7, num9 + shapedLine.LayoutAdvance);
				num6 = Math.Min(num6, num10 - metrics.Ascent);
				num8 = Math.Max(num8, num10 + metrics.Descent);
				num3 = num9 + shapedLine.LayoutAdvance;
			}
		}
		(double, double, double, double) box = (double.IsPositiveInfinity(num5) ? (0.0, 0.0, 0.0, 0.0) : (num5, num6, num7 - num5, num8 - num6));
		return new TextLayout
		{
			Lines = list,
			Box = box,
			Font = font,
			Size = size,
			SyntheticBoldStrokeWidth = shaper.SyntheticBoldStrokeWidth(cssFamilyList, size, weight)
		};
	}
}
