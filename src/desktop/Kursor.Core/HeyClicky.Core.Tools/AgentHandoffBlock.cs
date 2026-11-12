using System;
using System.Linq;

namespace HeyClicky.Core.Tools;

public static class AgentHandoffBlock
{
	public const string OpeningTag = "<AGENT_HANDOFF>";

	public const string ClosingTag = "</AGENT_HANDOFF>";

	public const string UserRequestHeader = "Original user request (typed/STT text):";

	public const string BriefHeader = "HeyClicky's brief:";

	public const string HowToReadHeader = "How to read this:";

	public static readonly string[] OpeningTags = new string[2] { "<AGENT_HANDOFF>", "<REALTIME_AGENT_HANDOFF>" };

	public static readonly string[] ClosingTags = new string[2] { "</AGENT_HANDOFF>", "</REALTIME_AGENT_HANDOFF>" };

	public static readonly string[] BriefHeaders = new string[2] { "HeyClicky's brief:", "Realtime-inferred task:" };

	public static readonly string[] AfterBriefHeaders = new string[3] { "How to read this:", "How to read this (", "Bounded recent Realtime conversation context" };

	public static bool IsPresent(string text)
	{
		return OpeningTags.Any((string tag) => text.Contains(tag, StringComparison.Ordinal));
	}
}
