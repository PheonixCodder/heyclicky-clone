using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Proactive;

public sealed record AgentNotification(string Id, string SourceType, string? SourceId, string? RunId, string Title, string Summary, AgentNotificationSeverity Severity, string? ReadAt, string CreatedAt)
{
	public DateTimeOffset? CreatedAtDate => ParseTimestamp(CreatedAt);

	public string DisplayDateTime
	{
		get
		{
			DateTimeOffset? createdAtDate = CreatedAtDate;
			if (!createdAtDate.HasValue)
			{
				return "";
			}
			return createdAtDate.GetValueOrDefault().ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.GetCultureInfo("en-US"));
		}
	}

	public static DateTimeOffset? ParseTimestamp(string raw)
	{
		string text = raw.Trim();
		if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
		{
			return result;
		}
		string text2 = text.Replace(' ', 'T');
		if (text2.Length >= 3)
		{
			string text3 = text2;
			if (text3[text3.Length - 3] != '+')
			{
				string text4 = text2;
				if (text4[text4.Length - 3] != '-')
				{
					goto IL_0063;
				}
			}
			text2 += ":00";
		}
		goto IL_0063;
		IL_0063:
		if (!DateTimeOffset.TryParse(text2, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
		{
			return null;
		}
		return result;
	}

	public static IReadOnlyList<AgentNotification> ParseList(byte[] bytes)
	{
		List<AgentNotification> list = new List<AgentNotification>();
		if (!(JsonNode.Parse(bytes, null)?["notifications"] is JsonArray source))
		{
			return list;
		}
		foreach (JsonObject item in source.OfType<JsonObject>())
		{
			JsonObject n = item;
			string text = Str("id");
			if (text != null)
			{
				AgentNotificationSeverity severity = Str("severity") switch
				{
					"success" => AgentNotificationSeverity.Success, 
					"warning" => AgentNotificationSeverity.Warning, 
					"error" => AgentNotificationSeverity.Error, 
					_ => AgentNotificationSeverity.Info, 
				};
				list.Add(new AgentNotification(text, Str("source_type") ?? "", Str("source_id"), Str("run_id"), Str("title") ?? "", Str("summary") ?? "", severity, Str("read_at"), Str("created_at") ?? ""));
			}
			string? Str(string k)
			{
				if (!(n[k] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
				{
					return null;
				}
				return value;
			}
		}
		return list;
	}
}
