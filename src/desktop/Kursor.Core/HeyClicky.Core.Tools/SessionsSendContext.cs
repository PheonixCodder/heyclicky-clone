using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Tools;

public static class SessionsSendContext
{
	public const string AppTurnLaunchContext = "HeyClicky is passing the user's message on to this work (sessions_send).";

	public static string UsersOwnWordsLaunchContext(string usersOwnWords, string brainNote, bool typed)
	{
		string text = "HeyClicky passed the user's message to you exactly as they " + (typed ? "typed" : "said") + " it.";
		if (!BrainNoteAddsToWords(brainNote, usersOwnWords))
		{
			return text;
		}
		return text + " HeyClicky's note on what it refers to and what was agreed with them in the conversation (their words win where the two differ): " + brainNote.Trim();
	}

	public static bool BrainNoteAddsToWords(string brainNote, string usersOwnWords)
	{
		string text2 = Normalized(brainNote);
		if (text2.Length > 0)
		{
			return text2 != Normalized(usersOwnWords);
		}
		return false;
		static string Normalized(string text)
		{
			return new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
		}
	}

	public static bool IsHeyClickysOwnInstruction(string? from, string? message)
	{
		if (string.Equals(from?.Trim(), "heyclicky", StringComparison.OrdinalIgnoreCase))
		{
			return !string.IsNullOrWhiteSpace(message);
		}
		return false;
	}

	public static string HeyClickyInstructionLaunchContext(string usersOwnWords, bool typed)
	{
		return "This message is HeyClicky's own instruction to you (sessions_send), not the user's words. For background only, what the user " + (typed ? "typed" : "said") + " to HeyClicky this turn (said to HeyClicky, not to you, so don't stop your work to answer it): " + usersOwnWords;
	}

	public static IReadOnlyList<string> BuildsOnJobIds(JsonObject? arguments)
	{
		List<string> list = new List<string>();
		JsonNode jsonNode = arguments?["builds_on"];
		if (!(jsonNode is JsonArray jsonArray))
		{
			if (jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
			{
				list.Add(value);
			}
		}
		else
		{
			foreach (JsonNode item in jsonArray)
			{
				if (item is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value2))
				{
					list.Add(value2);
				}
			}
		}
		return (from id in list
			select id.Trim() into id
			where id.Length > 0
			select id).Distinct().Take(3).ToList();
	}
}
