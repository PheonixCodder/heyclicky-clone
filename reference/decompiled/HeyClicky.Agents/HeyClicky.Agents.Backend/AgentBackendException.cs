using System;
using System.Text.Json;

namespace HeyClicky.Agents.Backend;

public sealed class AgentBackendException(string route, int statusCode, string body) : Exception($"{route} returned {statusCode}: {Truncate(body)}")
{
	public string Route { get; } = route;

	public int StatusCode { get; } = statusCode;

	public string Body { get; } = body;

	public bool IsPaywall
	{
		get
		{
			if (StatusCode != 402)
			{
				return Body.Contains("\"paywall\"", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	public JsonElement? BodyJson
	{
		get
		{
			try
			{
				return JsonDocument.Parse(Body).RootElement.Clone();
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}

	private static string Truncate(string s)
	{
		if (s.Length <= 400)
		{
			return s;
		}
		return s.Substring(0, 400) + "...";
	}
}
