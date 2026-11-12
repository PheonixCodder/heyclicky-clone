using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeyClicky.Core.Proactive;

public static class SuggestionVoiceAction
{
	public enum Kind
	{
		NotSuggestionAction,
		Invalid,
		Approve,
		Skip
	}

	public readonly record struct Request(Kind Kind, Guid CardId = default(Guid), string? Detail = null);

	private const string ApprovalPrefix = "[Approve suggested task ";

	private const string SkipPrefix = "[Skip suggested task ";

	public static string Reference(Guid id)
	{
		return "[Approve suggested task " + id.ToString("D").ToUpperInvariant() + "]";
	}

	public static string SkipReference(Guid id)
	{
		return "[Skip suggested task " + id.ToString("D").ToUpperInvariant() + "]";
	}

	public static Request Parse(string task)
	{
		string[] array = task.Trim().Split('\n');
		string text = array[0];
		bool flag = text.StartsWith("[Skip suggested task", StringComparison.Ordinal);
		if (!flag && !text.StartsWith("[Approve suggested task", StringComparison.Ordinal))
		{
			return new Request(Kind.NotSuggestionAction);
		}
		string text2 = (flag ? "[Skip suggested task " : "[Approve suggested task ");
		if (text.StartsWith(text2, StringComparison.Ordinal) && text.EndsWith(']'))
		{
			string text3 = text;
			int length = text2.Length;
			if (Guid.TryParse(text3.Substring(length, text3.Length - 1 - length), out var result))
			{
				string text4 = string.Join("\n", array.Skip(1)).Trim();
				return new Request(flag ? Kind.Skip : Kind.Approve, result, (text4.Length == 0) ? null : text4);
			}
		}
		return new Request(Kind.Invalid);
	}

	public static string ContextNote(ProactiveSuggestionCard? card, DateTimeOffset now)
	{
		if ((object)card == null || card.IsExpired(now))
		{
			return "[Suggestions page] The user is looking at Suggestions, but there is no available task on screen to approve or skip. Do not act on an earlier suggestion reference or invent a new task.";
		}
		string value = JsonSerializer.Serialize(new Dictionary<string, string>
		{
			["title"] = card.Title,
			["description"] = card.Description
		}, ProactiveJsonContext.Default.DictionaryStringString);
		return $"[Suggestions page — current turn] The user is reading this proposed task: {value}\nInterpret their intent naturally; the examples below are not a fixed phrase list.\nAPPROVE: If they approve it (\"go do it\", \"yes\", \"start this\"), call sessions_spawn with task exactly \"{Reference(card.Id)}\". If they explicitly approve with changes, append a newline and only their requested changes. The app resolves the full brief, owning Clicky and routine, records approval in that Clicky's chat, and advances to the next suggestion. Do not copy the brief into an ordinary spawn or send it to a previous chat.\nSKIP: If they reject this proposal or ask for the next one (\"skip this\", \"show me the next\", \"I don't want to do this\", \"I don't like this\"), call sessions_spawn with task exactly \"{SkipReference(card.Id)}\". This is an app UI action: it skips the card and shows the next without starting any agent. If they gave a reason, append a newline and only that reason in their words; do not invent feedback or ask them to fill out a reason box. A bare rejection is enough to skip.\nADJUST: If they want to change this task, ask what they want different and any necessary follow-up one question at a time. The opening request to adjust is never permission to launch. Once their changes are clear and they are ready, use the APPROVE reference above with their changes after a newline; the app preserves the full original brief and advances only this card.\nQuestions about the proposal are not approval or a request to skip. A requested adjustment is not rejection. If their intent is ambiguous, clarify without changing the card. Act only on the current card, once for this user turn; never approve or skip another card from earlier context or continue through the deck without a new user request. After approval, say only the short acknowledgment specified by the tool result, with no recap or follow-up. After skipping, briefly acknowledge its actual result (the next card shown or no suggestions left). Do not navigate away from Suggestions or automatically generate more tasks.";
	}

	public static string AdjustmentBudget(int answerCount)
	{
		if (answerCount > 0)
		{
			if (answerCount == 1)
			{
				return "The user answered question 1 of 2. If their adjustment is clear, start it now. Otherwise ask exactly ONE final clarification (question 2 of 2), then wait. Never bundle multiple questions.";
			}
			return "The user answered question 2 of 2. The question budget is exhausted: ask NO further questions, including confirmation or permission to proceed. Start this suggestion now with their stated adjustments using the APPROVE reference. Preserve the original brief for anything unchanged; do not invent missing facts. If essential details remain unknown, include them as open inputs for the task. An explicit cancellation or skip still takes priority over starting.";
		}
		return "Question 1 of 2: ask one short question about what they want changed, then wait. Do not start yet.";
	}
}
