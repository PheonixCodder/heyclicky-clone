using System;
using System.Text;
using System.Text.Json;

namespace HeyClicky.Core.Auth;

public static class JwtPayload
{
	public static bool TryRead(string? jwt, out long expiresAtEpochSeconds, out string? subject, out string? email)
	{
		expiresAtEpochSeconds = 0L;
		subject = null;
		email = null;
		if (string.IsNullOrEmpty(jwt))
		{
			return false;
		}
		int num = jwt.IndexOf('.');
		if (num < 0)
		{
			return false;
		}
		int num2 = jwt.IndexOf('.', num + 1);
		ReadOnlySpan<char> value = ((num2 < 0) ? jwt.AsSpan(num + 1) : jwt.AsSpan(num + 1, num2 - num - 1));
		try
		{
			StringBuilder stringBuilder = new StringBuilder(value.Length + 3).Append(value).Replace('-', '+').Replace('_', '/');
			while (stringBuilder.Length % 4 != 0)
			{
				stringBuilder.Append('=');
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(Convert.FromBase64String(stringBuilder.ToString()));
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				return false;
			}
			if (rootElement.TryGetProperty("exp", out var value2) && value2.ValueKind == JsonValueKind.Number)
			{
				expiresAtEpochSeconds = (long)value2.GetDouble();
			}
			if (rootElement.TryGetProperty("sub", out var value3) && value3.ValueKind == JsonValueKind.String)
			{
				subject = value3.GetString();
			}
			if (rootElement.TryGetProperty("email", out var value4) && value4.ValueKind == JsonValueKind.String)
			{
				email = value4.GetString();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
