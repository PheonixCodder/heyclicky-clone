using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Harness;

public sealed record AgentHarnessTurn
{
	public required string HarnessThreadId { get; init; }

	public required string AppThreadId { get; init; }

	public bool IsFirstTurn { get; init; }

	public required string Text { get; init; }

	public IReadOnlyList<string> ImagePaths { get; init; } = Array.Empty<string>();

	public required string Workspace { get; init; }

	public AgentHarnessModel Model { get; init; }

	public required AgentHarnessTurnContext Context { get; init; }

	public IReadOnlyDictionary<string, string>? LeaseMetadata { get; init; }

	public string? TurnId { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private AgentHarnessTurn(AgentHarnessTurn original)
	{
		HarnessThreadId = original.HarnessThreadId;
		AppThreadId = original.AppThreadId;
		IsFirstTurn = original.IsFirstTurn;
		Text = original.Text;
		ImagePaths = original.ImagePaths;
		Workspace = original.Workspace;
		Model = original.Model;
		Context = original.Context;
		LeaseMetadata = original.LeaseMetadata;
		TurnId = original.TurnId;
	}

	public AgentHarnessTurn()
	{
	}
}
