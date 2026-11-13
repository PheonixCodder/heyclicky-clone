using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Harness;

public static class AgentHarnessNames
{
	public static IReadOnlyList<AgentHarnessModel> AllModels { get; } = Enum.GetValues<AgentHarnessModel>();

	public static string RawValue(this AgentHarness harness)
	{
		return harness switch
		{
			AgentHarness.Codex => "codex", 
			AgentHarness.Pi => "pi", 
			_ => "h1", 
		};
	}

	public static string DisplayName(this AgentHarness harness)
	{
		return harness switch
		{
			AgentHarness.Codex => "Codex", 
			AgentHarness.Pi => "Pi", 
			_ => "H1", 
		};
	}

	public static IReadOnlyList<AgentHarnessPlace> Places(this AgentHarness harness)
	{
		return new global::_003C_003Ez__ReadOnlyArray<AgentHarnessPlace>(new AgentHarnessPlace[1]
		{
			AgentHarnessPlace.Mac
		});
	}

	public static AgentHarness? ParseHarness(string? raw)
	{
		return raw?.Trim().ToLowerInvariant() switch
		{
			"codex" => AgentHarness.Codex, 
			"pi" => AgentHarness.Pi, 
			"h1" => AgentHarness.H1, 
			_ => null, 
		};
	}

	public static string RawValue(this AgentHarnessPlace place)
	{
		if (place != AgentHarnessPlace.Cloud)
		{
			return "mac";
		}
		return "cloud";
	}

	public static string DisplayName(this AgentHarnessPlace place)
	{
		if (place != AgentHarnessPlace.Cloud)
		{
			return "On this PC";
		}
		return "In the cloud";
	}

	public static AgentHarnessPlace? ParsePlace(string? raw)
	{
		string text = raw?.Trim().ToLowerInvariant();
		if (!(text == "mac"))
		{
			if (text == "cloud")
			{
				return AgentHarnessPlace.Cloud;
			}
			return null;
		}
		return AgentHarnessPlace.Mac;
	}

	public static string RawValue(this AgentHarnessModel model)
	{
		return model switch
		{
			AgentHarnessModel.Astra => "gpt-6-astra", 
			AgentHarnessModel.Sol => "gpt-6.1-sol", 
			AgentHarnessModel.Terra => "gpt-6.1-terra", 
			AgentHarnessModel.Luna => "gpt-6-luna", 
			AgentHarnessModel.Fable => "claude-fable-5-1", 
			AgentHarnessModel.Opus => "claude-opus-5-5", 
			AgentHarnessModel.Sonnet => "claude-sonnet-5-5", 
			AgentHarnessModel.Haiku => "claude-haiku-5-5", 
			_ => "default", 
		};
	}

	public static string DisplayName(this AgentHarnessModel model)
	{
		return model switch
		{
			AgentHarnessModel.Astra => "GPT-6 Astra", 
			AgentHarnessModel.Sol => "GPT-6.1 Sol", 
			AgentHarnessModel.Terra => "GPT-6.1 Terra", 
			AgentHarnessModel.Luna => "GPT-6 Luna", 
			AgentHarnessModel.Fable => "Claude Fable 5.1", 
			AgentHarnessModel.Opus => "Claude Opus 5.5", 
			AgentHarnessModel.Sonnet => "Claude Sonnet 5.5", 
			AgentHarnessModel.Haiku => "Claude Haiku 5.5", 
			_ => "Default", 
		};
	}

	public static bool IsClaude(this AgentHarnessModel model)
	{
		if ((uint)(model - 5) <= 3u)
		{
			return true;
		}
		return false;
	}

	public static AgentHarnessModel ParseModel(string? raw)
	{
		return AllModels.FirstOrDefault((AgentHarnessModel model) => model.RawValue() == raw);
	}
}
