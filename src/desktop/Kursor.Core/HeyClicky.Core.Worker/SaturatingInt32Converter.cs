using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Worker;

internal sealed class SaturatingInt32Converter : JsonConverter<int>
{
	public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			if (!double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				throw new JsonException("not a number");
			}
			return Saturate(result);
		}
		if (reader.TokenType != JsonTokenType.Number)
		{
			throw new JsonException("not a number");
		}
		if (!reader.TryGetInt64(out var value))
		{
			return Saturate(reader.GetDouble());
		}
		return (int)Math.Clamp(value, -2147483648L, 2147483647L);
	}

	public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value);
	}

	private static int Saturate(double value)
	{
		if (!double.IsNaN(value))
		{
			return (int)Math.Clamp(Math.Truncate(value), -2147483648.0, 2147483647.0);
		}
		return 0;
	}
}
