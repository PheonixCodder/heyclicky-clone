using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Harness;

public sealed record AgentHarnessTurnContext
{
	public required string Instructions { get; init; }

	public IReadOnlyList<AgentHarnessMcpServer> McpServers { get; init; } = Array.Empty<AgentHarnessMcpServer>();

	public IReadOnlyList<string> SkillDirectories { get; init; } = Array.Empty<string>();

	public IReadOnlyList<string> UserSkillDirectories { get; init; } = Array.Empty<string>();

	public required string WorkerBaseUrl { get; init; }

	public required string SessionToken { get; init; }

	public string? NotePath { get; init; }

	public bool IsKeptThing { get; init; }

	public bool WritesNoteWithTools { get; init; }

	public bool UsesPlanTool { get; init; }

	public AgentHarnessModel Model { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private AgentHarnessTurnContext(AgentHarnessTurnContext original)
	{
		Instructions = original.Instructions;
		McpServers = original.McpServers;
		SkillDirectories = original.SkillDirectories;
		UserSkillDirectories = original.UserSkillDirectories;
		WorkerBaseUrl = original.WorkerBaseUrl;
		SessionToken = original.SessionToken;
		NotePath = original.NotePath;
		IsKeptThing = original.IsKeptThing;
		WritesNoteWithTools = original.WritesNoteWithTools;
		UsesPlanTool = original.UsesPlanTool;
		Model = original.Model;
	}

	public AgentHarnessTurnContext()
	{
	}
}
