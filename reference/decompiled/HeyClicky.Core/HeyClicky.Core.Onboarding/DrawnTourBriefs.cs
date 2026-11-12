using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Onboarding;

public static class DrawnTourBriefs
{
	public static JsonObject TourBrief(string userName, IReadOnlyList<InterviewLine> interview, string talkKeys, IReadOnlyList<ProactiveSuggestionCard>? plannedCards, out Dictionary<string, ProactiveSuggestionCard> plannedById)
	{
		plannedById = (plannedCards ?? Array.Empty<ProactiveSuggestionCard>()).Take(3).Select((ProactiveSuggestionCard card, int i) => (Key: $"c{i + 1}", Card: card)).ToDictionary(((string Key, ProactiveSuggestionCard Card) p) => p.Key, ((string Key, ProactiveSuggestionCard Card) p) => p.Card);
		return new JsonObject((JsonNodeOptions?)null)
		{
			["kind"] = "tour_finale",
			["notch_showcase"] = true,
			["user_name"] = userName,
			["talk_keys"] = talkKeys,
			["interview"] = new JsonArray(((IEnumerable<InterviewLine>)interview).Select((Func<InterviewLine, JsonNode>)((InterviewLine l) => new JsonObject((JsonNodeOptions?)null)
			{
				["role"] = ((l.Role == "user") ? "user" : "assistant"),
				["text"] = l.Text
			})).ToArray()),
			["planned_cards"] = new JsonArray(((IEnumerable<KeyValuePair<string, ProactiveSuggestionCard>>)plannedById.OrderBy<KeyValuePair<string, ProactiveSuggestionCard>, string>((KeyValuePair<string, ProactiveSuggestionCard> p) => p.Key, StringComparer.Ordinal)).Select((Func<KeyValuePair<string, ProactiveSuggestionCard>, JsonNode>)delegate(KeyValuePair<string, ProactiveSuggestionCard> p)
			{
				JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
				{
					["id"] = p.Key,
					["title"] = p.Value.Title,
					["description"] = p.Value.Description
				};
				ProactiveRoutine routine = p.Value.Routine;
				if ((object)routine != null)
				{
					jsonObject["routine"] = new JsonObject((JsonNodeOptions?)null)
					{
						["title"] = routine.Title,
						["every_minutes"] = routine.ClampedIntervalMinutes
					};
				}
				return jsonObject;
			}).ToArray())
		};
	}

	public static string FittedTranscript(IReadOnlyList<InterviewLine> interview, string userName, int budget)
	{
		List<(string Speaker, string Text)> lines = interview.Select((InterviewLine l) => (Speaker: (l.Role == "user") ? userName : "Clicky", Text: l.Text)).ToList();
		if (Rendered().Length <= budget)
		{
			return Rendered();
		}
		lines = lines.Select(((string Speaker, string Text) l) => (!(l.Speaker == "Clicky") || l.Text.Length <= 80) ? l : (Speaker: l.Speaker, l.Text.Substring(0, 80) + "…")).ToList();
		while (Rendered().Length > budget && lines.Count > 0)
		{
			int index = Enumerable.Range(0, lines.Count).MaxBy((int i) => lines[i].Text.Length);
			if (lines[index].Text.Length <= 120)
			{
				break;
			}
			lines[index] = (lines[index].Speaker, lines[index].Text.Substring(0, lines[index].Text.Length - 40) + "…");
		}
		return Rendered();
		string Rendered()
		{
			return string.Join("\n", lines.Select(((string Speaker, string Text) l) => l.Speaker + ": " + l.Text));
		}
	}

	public static JsonObject MorningBrief(string? firstName, string researchedAgo, string curatedEvidence, string? headline, IReadOnlyList<string> todaysEvents, IReadOnlyList<ProactiveSuggestionCard> plateCards)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["kind"] = "morning",
			["user_name"] = firstName ?? "them",
			["researched_ago"] = researchedAgo,
			["calendar"] = new JsonArray(((IEnumerable<string>)todaysEvents).Select((Func<string, JsonNode>)((string e) => e)).ToArray()),
			["cards"] = new JsonArray(((IEnumerable<ProactiveSuggestionCard>)plateCards).Select((Func<ProactiveSuggestionCard, int, JsonNode>)((ProactiveSuggestionCard card, int i) => new JsonObject((JsonNodeOptions?)null)
			{
				["id"] = $"c{i + 1}",
				["title"] = card.Title,
				["description"] = card.Description,
				["brief"] = card.AgentPrompt
			})).ToArray()),
			["evidence"] = curatedEvidence
		};
		if (headline != null)
		{
			jsonObject["headline"] = headline;
		}
		return jsonObject;
	}

	public static string RelativeTime(DateTimeOffset date, DateTimeOffset now)
	{
		double totalSeconds = (now - date).TotalSeconds;
		bool flag = totalSeconds >= 0.0;
		double num = Math.Abs(totalSeconds);
		string text = ((num < 1.0) ? "" : ((num < 60.0) ? Unit(num, "second") : ((num < 3600.0) ? Unit(num / 60.0, "minute") : ((num < 86400.0) ? Unit(num / 3600.0, "hour") : ((num < 604800.0) ? Unit(num / 86400.0, "day") : ((num < 2592000.0) ? Unit(num / 604800.0, "week") : ((num < 31536000.0) ? Unit(num / 2592000.0, "month") : Unit(num / 31536000.0, "year"))))))));
		if (text.Length == 0)
		{
			return "now";
		}
		if (!flag)
		{
			return "in " + text;
		}
		return text + " ago";
		static string Unit(double value, string unit)
		{
			int num2 = (int)Math.Floor(value);
			return $"{num2} {unit}{((num2 == 1) ? "" : "s")}";
		}
	}

	public static IReadOnlyList<(string Text, string Plan)> ParseNextQuestions(string payload)
	{
		JsonArray jsonArray;
		try
		{
			jsonArray = JsonNode.Parse(payload, null) as JsonArray;
		}
		catch (JsonException)
		{
			return Array.Empty<(string, string)>();
		}
		if (jsonArray == null)
		{
			return Array.Empty<(string, string)>();
		}
		List<(string, string)> list = new List<(string, string)>();
		foreach (JsonObject item2 in jsonArray.OfType<JsonObject>())
		{
			JsonObject item = item2;
			string text = Str("text")?.Trim();
			if (text != null && text.Length > 0)
			{
				string text2 = (Str("plan") ?? Str("ask") ?? "").Trim();
				list.Add(((text.Length > 48) ? text.Substring(0, 48) : text, (text2 == text) ? "" : ((text2.Length > 600) ? text2.Substring(0, 600) : text2)));
			}
			string? Str(string key)
			{
				if (!(item[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
				{
					return null;
				}
				return value;
			}
		}
		return list.Take(3).ToList();
	}

	public static string WholeWordTitle(string title, int maximumLength = 80)
	{
		string text = title.Trim();
		if (text.Length <= maximumLength)
		{
			return text;
		}
		string text2 = text.Substring(0, maximumLength);
		int num = text2.LastIndexOf(' ');
		if (num < 0)
		{
			return text2;
		}
		return text2.Substring(0, num).Trim(' ', ',', '.', ';', ':', '-');
	}

	public static string PaperForTitle(string title, string summary = "")
	{
		string words = (title + " " + summary).ToLowerInvariant();
		if (Has(new string[8] { "flight", "trip", "travel", "hotel", "fare", "lisbon", "tokyo", "airbnb" }))
		{
			return "peach";
		}
		if (Has(new string[10] { "inbox", "email", "mail", "invoice", "revenue", "price", "budget", "stock", "money", "$" }))
		{
			return "mint";
		}
		if (Has(new string[6] { "meeting", "board", "calendar", "sync", "prep", "agenda" }))
		{
			return "periwinkle";
		}
		if (Has(new string[4] { "draw", "sketch", "design", "photo" }))
		{
			return "blush";
		}
		if (Has(new string[5] { "buy", "shop", "list", "desk", "groceries" }))
		{
			return "kraft";
		}
		if (Has(new string[5] { "news", "digest", "brief", "summary", "trend" }))
		{
			return "butter";
		}
		return "sky";
		bool Has(string[] keys)
		{
			return keys.Any(words.Contains);
		}
	}
}
