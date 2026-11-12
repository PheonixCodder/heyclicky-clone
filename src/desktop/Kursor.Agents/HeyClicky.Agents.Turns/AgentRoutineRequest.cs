using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Turns;

public sealed record AgentRoutineRequest(string Action, string Title, string Task, int EveryMinutes, string NewTitle)
{
	public static AgentRoutineRequest? Parse(string rawBlock)
	{
		string text = rawBlock.Trim();
		int num = text.IndexOf('{');
		int num2 = text.LastIndexOf('}');
		if (num < 0 || num2 <= num)
		{
			return null;
		}
		JsonObject jsonObject;
		try
		{
			int num3 = num;
			jsonObject = JsonNode.Parse(text.Substring(num3, num2 + 1 - num3), null) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
		if (jsonObject == null)
		{
			return null;
		}
		string text2 = (jsonObject["action"]?.ToString() ?? "create").Trim().ToLowerInvariant();
		bool flag;
		switch (text2)
		{
		case "create":
		case "update":
		case "pause":
		case "resume":
		case "delete":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return null;
		}
		string text3 = jsonObject["title"]?.ToString()?.Trim() ?? "";
		string text4 = jsonObject["task"]?.ToString()?.Trim() ?? "";
		string text5 = jsonObject["new_title"]?.ToString()?.Trim() ?? "";
		int num4 = 0;
		if (jsonObject["every_minutes"] is JsonValue jsonValue)
		{
			double value2;
			string value3;
			int result;
			if (jsonValue.TryGetValue<int>(out var value))
			{
				num4 = value;
			}
			else if (jsonValue.TryGetValue<double>(out value2))
			{
				num4 = (int)Math.Round(value2);
			}
			else if (jsonValue.TryGetValue<string>(out value3) && int.TryParse(value3?.Trim(), out result))
			{
				num4 = result;
			}
		}
		if (!((text2 == "create") ? (text4.Length > 0 && num4 > 0) : ((!(text2 == "update")) ? (text3.Length > 0 || text4.Length > 0) : (text3.Length > 0 && (text4.Length > 0 || num4 > 0 || text5.Length > 0)))))
		{
			return null;
		}
		return new AgentRoutineRequest(text2, text3, text4, num4, text5);
	}
}
