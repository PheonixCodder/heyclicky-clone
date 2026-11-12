using System;

namespace HeyClicky.Agents;

public sealed class AgentPaywallException(CompanionError error) : Exception(error.Message)
{
	public CompanionError Error { get; } = error;
}
