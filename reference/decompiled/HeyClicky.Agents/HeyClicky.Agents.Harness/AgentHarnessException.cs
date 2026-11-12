using System;

namespace HeyClicky.Agents.Harness;

public sealed class AgentHarnessException : Exception
{
	public bool IsNotAvailable { get; }

	public bool IsRunnerStartFailure { get; }

	public AgentHarnessException(string message, bool notAvailable = false, bool isRunnerStartFailure = false)
	{
		IsNotAvailable = notAvailable;
		IsRunnerStartFailure = isRunnerStartFailure;
		base._002Ector(message);
	}
}
