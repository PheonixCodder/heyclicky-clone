using System;

namespace HeyClicky.Agents.Harness;

public static class AgentHarnessThreads
{
	private const string ThreadMapPrefix = "ClickyAgentHarnessThreadMap.";

	private static string MapKey(AgentHarness harness, AgentHarnessPlace place, string appThreadId)
	{
		return $"{"ClickyAgentHarnessThreadMap."}{harness.RawValue()}-{place.RawValue()}.{appThreadId}";
	}

	public static (AgentHarness Harness, AgentHarnessPlace Place)? TakenOverBy(string appThreadId)
	{
		if (AgentHarnessPreferences.RuntimeChoice(appThreadId).HasValue)
		{
			return null;
		}
		AgentHarness[] values = Enum.GetValues<AgentHarness>();
		foreach (AgentHarness agentHarness in values)
		{
			foreach (AgentHarnessPlace item in agentHarness.Places())
			{
				if (AgentDefaults.String(MapKey(agentHarness, item, appThreadId)) != null)
				{
					return (agentHarness, item);
				}
			}
		}
		return null;
	}

	public static (AgentHarness Harness, AgentHarnessPlace Place) Choice(string? appThreadId)
	{
		if (appThreadId != null)
		{
			(AgentHarness, AgentHarnessPlace)? tuple = AgentHarnessPreferences.RuntimeChoice(appThreadId);
			if (tuple.HasValue)
			{
				return tuple.GetValueOrDefault();
			}
			tuple = TakenOverBy(appThreadId);
			if (tuple.HasValue)
			{
				return tuple.GetValueOrDefault();
			}
		}
		return (Harness: AgentHarness.H1, Place: AgentHarnessPlace.Mac);
	}

	public static string HarnessThreadId(string appThreadId, AgentHarness harness, AgentHarnessPlace place)
	{
		if (AgentHarnessPreferences.RuntimeChoice(appThreadId).HasValue)
		{
			return appThreadId;
		}
		string key = MapKey(harness, place, appThreadId);
		string text = AgentDefaults.String(key);
		if (text != null)
		{
			return text;
		}
		string text2 = AgentHarnessPreferences.ThreadIdPrefix(harness, place) + Guid.NewGuid().ToString("D").ToLowerInvariant();
		AgentDefaults.Set(key, text2);
		return text2;
	}

	public static string? ExistingHarnessThreadId(string appThreadId)
	{
		(AgentHarness, AgentHarnessPlace)? tuple = TakenOverBy(appThreadId);
		if (tuple.HasValue)
		{
			(AgentHarness, AgentHarnessPlace) valueOrDefault = tuple.GetValueOrDefault();
			return AgentDefaults.String(MapKey(valueOrDefault.Item1, valueOrDefault.Item2, appThreadId));
		}
		return null;
	}

	public static bool ContinuesOnNewConversationHarness(string appThreadId)
	{
		if (AgentHarnessPreferences.IsRetiredHarnessThread(appThreadId))
		{
			return false;
		}
		(AgentHarness, AgentHarnessPlace)? tuple = AgentHarnessPreferences.RuntimeChoice(appThreadId) ?? TakenOverBy(appThreadId);
		if (tuple.HasValue)
		{
			(AgentHarness, AgentHarnessPlace) valueOrDefault = tuple.GetValueOrDefault();
			(AgentHarness, AgentHarnessPlace) tuple2 = valueOrDefault;
			(AgentHarness, AgentHarnessPlace) tuple3 = Choice(null);
			if (tuple2.Item1 == tuple3.Item1)
			{
				return tuple2.Item2 == tuple3.Item2;
			}
			return false;
		}
		return false;
	}
}
