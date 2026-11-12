using System;

namespace HeyClicky.Agents.Harness;

public static class AgentHarnessPreferences
{
	public const string ModelKey = "ClickyAgentHarnessModel";

	public const string DeviceHelperOffKey = "ClickyAgentHarnessDeviceHelperOff";

	public const string NoteToolsOffKey = "ClickyAgentHarnessNoteToolsOff";

	public const AgentHarness Harness = AgentHarness.H1;

	public const AgentHarnessPlace Place = AgentHarnessPlace.Mac;

	public static AgentHarnessModel Model => AgentHarnessNames.ParseModel(AgentDefaults.String("ClickyAgentHarnessModel"));

	public static bool UsesDeviceHelper => !AgentDefaults.Bool("ClickyAgentHarnessDeviceHelperOff");

	public static bool UsesNoteTools(AgentHarness harness, AgentHarnessPlace place)
	{
		if (harness == AgentHarness.H1 && place == AgentHarnessPlace.Mac)
		{
			return !AgentDefaults.Bool("ClickyAgentHarnessNoteToolsOff");
		}
		return false;
	}

	public static string ThreadIdPrefix(AgentHarness harness, AgentHarnessPlace place)
	{
		return $"harness-{harness.RawValue()}-{place.RawValue()}-";
	}

	public static bool IsRetiredHarnessThread(string threadId)
	{
		if (IsHarnessThread(threadId))
		{
			return !RuntimeChoice(threadId).HasValue;
		}
		return false;
	}

	public static bool IsHarnessThread(string? threadId)
	{
		return threadId?.StartsWith("harness-", StringComparison.Ordinal) ?? false;
	}

	public static (AgentHarness Harness, AgentHarnessPlace Place)? RuntimeChoice(string threadId)
	{
		if (!threadId.StartsWith("harness-", StringComparison.Ordinal))
		{
			return null;
		}
		string[] array = threadId.Split('-', 4);
		if (array.Length < 3)
		{
			return null;
		}
		AgentHarness? agentHarness = AgentHarnessNames.ParseHarness(array[1]);
		AgentHarnessPlace? agentHarnessPlace = AgentHarnessNames.ParsePlace(array[2]);
		if (agentHarness.HasValue)
		{
			AgentHarness valueOrDefault = agentHarness.GetValueOrDefault();
			if (agentHarnessPlace.HasValue)
			{
				AgentHarnessPlace valueOrDefault2 = agentHarnessPlace.GetValueOrDefault();
				return (valueOrDefault, valueOrDefault2);
			}
		}
		return null;
	}
}
