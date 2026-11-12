using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Harness;

public interface IAgentHarnessRuntime
{
	Task RunTurnAsync(AgentHarnessTurn turn, Action<AgentHarnessEvent> emit, CancellationToken ct);

	Task<bool> SteerAsync(string harnessThreadId, string text, bool informational, CancellationToken ct, IReadOnlyList<string>? imagePaths = null);

	Task InterruptAsync(string harnessThreadId);
}
