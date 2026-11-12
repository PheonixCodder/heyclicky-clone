using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HeyClicky.Drawing;

public static class DrawingTimelineReport
{
	public static string? Format(string? json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder("timeline (seconds on the drawing's clock):");
			JsonElement value;
			List<JsonElement> list = ((rootElement.TryGetProperty("statements", out value) && value.ValueKind == JsonValueKind.Array) ? value.EnumerateArray().ToList() : new List<JsonElement>());
			if (rootElement.TryGetProperty("lines", out var value2) && value2.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value2.EnumerateArray())
				{
					double? num = Number(item, "k");
					int num2;
					if (num.HasValue)
					{
						double valueOrDefault = num.GetValueOrDefault();
						num2 = (int)valueOrDefault;
					}
					else
					{
						num2 = 0;
					}
					int num3 = num2;
					double? value3 = Number(item, "start");
					double? value4 = Number(item, "length");
					JsonElement value5;
					string value6 = ((item.TryGetProperty("text", out value5) && value5.ValueKind == JsonValueKind.String) ? value5.GetString() : "");
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 4, stringBuilder2);
					handler.AppendLiteral("\nbeat ");
					handler.AppendFormatted(num3 + 1);
					handler.AppendLiteral(" heard ");
					handler.AppendFormatted(Seconds(value3));
					handler.AppendLiteral(" for ");
					handler.AppendFormatted(Seconds(value4));
					handler.AppendLiteral("s \"");
					handler.AppendFormatted(value6);
					handler.AppendLiteral("\"");
					stringBuilder3.Append(ref handler);
					foreach (JsonElement item2 in list)
					{
						num = Number(item2, "line");
						if (!num.HasValue)
						{
							continue;
						}
						double valueOrDefault2 = num.GetValueOrDefault();
						if ((int)valueOrDefault2 != num3)
						{
							continue;
						}
						JsonElement value7;
						string value8 = ((item2.TryGetProperty("label", out value7) && value7.ValueKind == JsonValueKind.String) ? value7.GetString() : "?");
						num = Number(item2, "first");
						double valueOrDefault3;
						object obj;
						if (num.HasValue)
						{
							valueOrDefault3 = num.GetValueOrDefault();
							if (value3.HasValue)
							{
								double valueOrDefault4 = value3.GetValueOrDefault();
								if (value4.HasValue)
								{
									double valueOrDefault5 = value4.GetValueOrDefault();
									if (valueOrDefault3 > valueOrDefault4 + valueOrDefault5)
									{
										obj = string.Format(CultureInfo.InvariantCulture, "  << started {0:0.00}s after its beat ended", valueOrDefault3 - (valueOrDefault4 + valueOrDefault5));
										goto IL_02c8;
									}
								}
							}
							obj = "";
							goto IL_02c8;
						}
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
						handler.AppendLiteral("\n    ");
						handler.AppendFormatted(value8);
						handler.AppendLiteral(": not drawn");
						stringBuilder4.Append(ref handler);
						continue;
						IL_02c8:
						string value9 = (string)obj;
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder5 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(8, 4, stringBuilder2);
						handler.AppendLiteral("\n    ");
						handler.AppendFormatted(value8);
						handler.AppendLiteral(": ");
						handler.AppendFormatted(Seconds(valueOrDefault3));
						handler.AppendLiteral("→");
						handler.AppendFormatted(Seconds(Number(item2, "end")));
						handler.AppendFormatted(value9);
						stringBuilder5.Append(ref handler);
					}
				}
			}
			if (rootElement.TryGetProperty("frames", out var frames) && frames.ValueKind == JsonValueKind.Object)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(59, 6, stringBuilder2);
				handler.AppendLiteral("\nframes: ");
				handler.AppendFormatted(Value("frames"));
				handler.AppendLiteral(" at ");
				handler.AppendFormatted(Value("typicalMs"));
				handler.AppendLiteral(" ms typical, p95 ");
				handler.AppendFormatted(Value("p95Ms"));
				handler.AppendLiteral(" ms, worst ");
				handler.AppendFormatted(Value("worstMs"));
				handler.AppendLiteral(" ms at ");
				handler.AppendFormatted(Value("worstAt"));
				handler.AppendLiteral("s, ");
				handler.AppendFormatted(Value("dropped"));
				handler.AppendLiteral(" dropped");
				stringBuilder6.Append(ref handler);
			}
			return stringBuilder.ToString();
			string Value(string key)
			{
				if (!frames.TryGetProperty(key, out var value10) || value10.ValueKind != JsonValueKind.Number)
				{
					return "-";
				}
				return value10.GetRawText();
			}
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static double? Number(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
		{
			return null;
		}
		return value.GetDouble();
	}

	private static string Seconds(double? value)
	{
		if (!value.HasValue)
		{
			return "-";
		}
		return value.GetValueOrDefault().ToString("0.00", CultureInfo.InvariantCulture);
	}
}
