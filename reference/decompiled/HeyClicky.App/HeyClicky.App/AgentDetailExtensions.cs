using HeyClicky.Agents;

namespace HeyClicky.App;

internal static class AgentDetailExtensions
{
	public static string? FinalResponseOrSummary(this AgentTaskDetail d)
	{
		if (string.IsNullOrWhiteSpace(d.Snapshot.FinalResponseMarkdown))
		{
			return d.Summary;
		}
		return d.Snapshot.FinalResponseMarkdown;
	}
}
