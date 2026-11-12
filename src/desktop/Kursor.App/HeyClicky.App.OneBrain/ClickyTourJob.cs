using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Tools;

namespace HeyClicky.App.OneBrain;

public sealed class ClickyTourJob
{
	public required string Title { get; init; }

	public required string Pitch { get; init; }

	public JsonObject? SpawnArguments { get; init; }

	public ProactiveSuggestionCard? PlannedCard { get; init; }

	public required TourPaperStock Paper { get; init; }

	public string RailKey { get; } = "tour-job:" + Guid.NewGuid().ToString().ToUpperInvariant();

	public static ClickyTourJob? FromPayload(string payload)
	{
		JsonObject o;
		try
		{
			o = JsonNode.Parse(payload, null) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
		if (o == null)
		{
			return null;
		}
		string? Str(string key)
		{
			if (!(o[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value2))
			{
				return null;
			}
			return value2;
		}
		string text = Str("card")?.Trim().ToLowerInvariant();
		if (text != null && DrawnOnboardingTour.PlannedCardsById.TryGetValue(text, out ProactiveSuggestionCard value))
		{
			string text2 = Str("name")?.Trim() ?? "";
			return new ClickyTourJob
			{
				Title = WholeWordTitle((text2.Length == 0 || text2.Length > 48) ? value.Title : text2),
				Pitch = value.Description.Trim(),
				PlannedCard = value,
				Paper = (TourPaperStocks.Parse(Str("color")) ?? TourPaperStocks.ForTitle(value.Title, value.Description))
			};
		}
		string text3 = Str("title")?.Trim();
		if (text3 == null || text3.Length <= 0)
		{
			return null;
		}
		string text4 = AgentTools.HandOffArguments(o.ToJsonString());
		if (text4 == null || !(JsonNode.Parse(text4, null) is JsonObject spawnArguments))
		{
			return null;
		}
		string pitch = Str("pitch")?.Trim() ?? "";
		return new ClickyTourJob
		{
			Title = WholeWordTitle(text3),
			Pitch = pitch,
			SpawnArguments = spawnArguments,
			Paper = (TourPaperStocks.Parse(Str("color")) ?? TourPaperStocks.ForTitle(text3, Str("pitch") ?? ""))
		};
	}

	public static string WholeWordTitle(string title, int maximumLength = 80)
	{
		return DrawnTourBriefs.WholeWordTitle(title, maximumLength);
	}

	public ProactiveSuggestionCard SuggestionCard(ProactiveCheckReason checkReason)
	{
		ProactiveSuggestionCard plannedCard = PlannedCard;
		if ((object)plannedCard != null)
		{
			return plannedCard;
		}
		JsonObject jsonObject = SpawnArguments ?? new JsonObject((JsonNodeOptions?)null);
		object obj;
		if (jsonObject["new_clicky"] is JsonObject jsonObject2)
		{
			string text = jsonObject2["name"]?.GetValue<string>();
			if (text != null && text.Length > 0)
			{
				obj = new ProactiveNewClicky(text, jsonObject2["role"]?.GetValue<string>() ?? "", jsonObject2["description"]?.GetValue<string>() ?? "");
				goto IL_00ab;
			}
		}
		obj = null;
		goto IL_00ab;
		IL_00ab:
		ProactiveNewClicky newClicky = (ProactiveNewClicky)obj;
		return new ProactiveSuggestionCard
		{
			Title = Title,
			Description = ((Pitch.Length == 0) ? Title : Pitch),
			AgentPrompt = (jsonObject["task"]?.GetValue<string>() ?? Title),
			CheckReason = checkReason,
			ClickySlug = jsonObject["clicky_slug"]?.GetValue<string>(),
			NewClicky = newClicky
		};
	}
}
