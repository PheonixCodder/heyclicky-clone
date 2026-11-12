using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky;

public interface IAgentService : IAsyncDisposable
{
	IReadOnlyList<AgentTaskSnapshot> Tasks { get; }

	bool IsRuntimeReady { get; }

	event EventHandler? TasksChanged;

	Task EnsureRuntimeReadyAsync(CancellationToken ct = default(CancellationToken));

	Task<string> LaunchAsync(string prompt, IReadOnlyList<byte[]>? imagesJpeg = null, CancellationToken ct = default(CancellationToken));

	Task SteerAsync(string taskId, string text, CancellationToken ct = default(CancellationToken));

	Task StopAsync(string taskId);
}
