using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Tools;

public static class ToolResults
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static string Json(JsonObject result)
	{
		return result.ToJsonString(Options);
	}

	public static string Status(string status, string message)
	{
		return Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = status,
			["message"] = message
		});
	}

	public static string Summary(string status, string toolName, string summary, JsonObject? result = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = status,
			["toolName"] = toolName,
			["summary"] = summary
		};
		if (result != null)
		{
			jsonObject["result"] = result;
		}
		return Json(jsonObject);
	}

	public static string Truncate(string text, int limit)
	{
		if (text.Length > limit)
		{
			return text.Substring(0, limit) + "…";
		}
		return text;
	}
}
