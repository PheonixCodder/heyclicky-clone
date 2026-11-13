using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyJobUserStep
{
	public required string Step { get; init; }

	public string? Page { get; init; }

	public string? DoneWhenUrlContains { get; init; }

	public DateTimeOffset AskedAt { get; init; }

	public const string BriefSection = "When the next step is one only the user can do (sign, pay, log in, approve something), get everything ready first (open the page in their Chrome, fill in what you can), then end your turn telling them in one line what to do, and append <WAIT_FOR>{\"step\": \"Sign the lease in DocuSign\", \"page\": \"https://…\", \"done_when_url_contains\": \"/signing/complete\"}</WAIT_FOR>. Give done_when_url_contains only when you know the address they land on once it's done; HeyClicky then continues this work by itself when their browser gets there. Don't wait or poll for it yourself. (A step you could do yourself is never a WAIT_FOR: on a website you do it in the browser; in a desktop app, its own scripting commands or, for its window, the pc_step desktop computer-use tool.)";

	public static ClickyJobUserStep? Parse(string rawBlock, DateTimeOffset? askedAt = null)
	{
		string text = rawBlock.Trim();
		if (text.Length == 0)
		{
			return null;
		}
		DateTimeOffset askedAt2 = askedAt ?? DateTimeOffset.Now;
		JsonObject o = null;
		try
		{
			o = JsonNode.Parse(text, null) as JsonObject;
		}
		catch (JsonException)
		{
		}
		if (o != null)
		{
			string text2 = Text("step", 160);
			if (text2 == null)
			{
				return null;
			}
			return new ClickyJobUserStep
			{
				Step = text2,
				Page = Text("page", 600),
				DoneWhenUrlContains = Text("done_when_url_contains", 200),
				AskedAt = askedAt2
			};
		}
		return new ClickyJobUserStep
		{
			Step = ((text.Length > 160) ? text.Substring(0, 160) : text),
			AskedAt = askedAt2
		};
		string? Text(string key, int limit)
		{
			string value;
			string text3 = ((!(o[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out value)) ? null : value?.Trim());
			if (!string.IsNullOrEmpty(text3))
			{
				if (text3.Length <= limit)
				{
					return text3;
				}
				return text3.Substring(0, limit);
			}
			return null;
		}
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyJobUserStep(ClickyJobUserStep original)
	{
		Step = original.Step;
		Page = original.Page;
		DoneWhenUrlContains = original.DoneWhenUrlContains;
		AskedAt = original.AskedAt;
	}

	public ClickyJobUserStep()
	{
	}
}
