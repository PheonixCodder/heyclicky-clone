using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Jobs;

public static class ClickyPendingAgentRequestStore
{
	private const string DefaultsKey = "clicky.agent.pendingRequests.v1";

	private static readonly TimeSpan MaximumAge = TimeSpan.FromDays(7);

	private static readonly object Gate = new object();

	public static IReadOnlyList<ClickyPendingAgentRequest> All()
	{
		DateTimeOffset cutoff = DateTimeOffset.Now - MaximumAge;
		lock (Gate)
		{
			return (AgentDefaults.Get<List<ClickyPendingAgentRequest>>("clicky.agent.pendingRequests.v1") ?? new List<ClickyPendingAgentRequest>()).Where((ClickyPendingAgentRequest r) => r.AskedAt > cutoff).ToList();
		}
	}

	public static void Remove(string threadId)
	{
		lock (Gate)
		{
			IReadOnlyList<ClickyPendingAgentRequest> source = All();
			if (source.Any((ClickyPendingAgentRequest r) => r.ThreadId == threadId))
			{
				Write(source.Where((ClickyPendingAgentRequest r) => r.ThreadId != threadId).ToList());
			}
		}
	}

	public static void RemoveAllForSignOut()
	{
		AgentDefaults.Remove("clicky.agent.pendingRequests.v1");
	}

	public static void Sync(ClickyPendingAgentRequest? request, string threadId)
	{
		lock (Gate)
		{
			if ((object)request == null)
			{
				Remove(threadId);
				return;
			}
			List<ClickyPendingAgentRequest> list = (from r in All()
				where r.ThreadId != threadId
				select r).ToList();
			list.Add(request);
			Write(list);
		}
	}

	private static void Write(IReadOnlyList<ClickyPendingAgentRequest> records)
	{
		if (records.Count == 0)
		{
			AgentDefaults.Remove("clicky.agent.pendingRequests.v1");
		}
		else
		{
			AgentDefaults.Set("clicky.agent.pendingRequests.v1", records);
		}
	}
}
