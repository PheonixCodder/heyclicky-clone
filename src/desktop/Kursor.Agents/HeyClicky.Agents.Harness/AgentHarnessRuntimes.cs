using System;
using System.Collections.Generic;

namespace HeyClicky.Agents.Harness;

public static class AgentHarnessRuntimes
{
	private static readonly object Gate = new object();

	private static readonly Dictionary<string, IAgentHarnessRuntime> Instances = new Dictionary<string, IAgentHarnessRuntime>();

	public static Dictionary<string, Func<IAgentHarnessRuntime>> Factories { get; } = BuildFactories();

	private static Dictionary<string, Func<IAgentHarnessRuntime>> BuildFactories()
	{
		Dictionary<string, Func<IAgentHarnessRuntime>> dictionary = new Dictionary<string, Func<IAgentHarnessRuntime>>();
		dictionary[Key(AgentHarness.H1, AgentHarnessPlace.Mac)] = () => new AgentHarnessRunnerRuntime(AgentHarness.H1, AgentHarnessPlace.Mac);
		return dictionary;
	}

	public static string Key(AgentHarness harness, AgentHarnessPlace place)
	{
		return harness.RawValue() + "-" + place.RawValue();
	}

	public static IAgentHarnessRuntime? Runtime(AgentHarness harness, AgentHarnessPlace place)
	{
		string key = Key(harness, place);
		lock (Gate)
		{
			if (Instances.TryGetValue(key, out IAgentHarnessRuntime value))
			{
				return value;
			}
			if (!Factories.TryGetValue(key, out Func<IAgentHarnessRuntime> value2))
			{
				return null;
			}
			value = value2();
			Instances[key] = value;
			return value;
		}
	}
}
