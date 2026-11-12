using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Routines;

namespace HeyClicky.App.Home;

public interface IHomeSource
{
	IReadOnlyList<AgentTaskSnapshot> Tasks { get; }

	IReadOnlyList<ClickyInfo> Clickys { get; }

	IReadOnlyList<ClickyRoutine> Routines { get; }

	event EventHandler? TasksChanged;

	event EventHandler<AgentTaskEvent>? TaskEvent;

	event EventHandler? RoutinesChanged;

	AgentTaskDetail? GetTaskDetail(string taskId);

	Task<IReadOnlyList<AgentThreadSummary>> ListThreadsAsync(CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<AgentTranscriptTurn>> ReadThreadAsync(string threadId, CancellationToken ct = default(CancellationToken));

	Task ArchiveThreadAsync(string threadId);

	void ArchiveClicky(string slug);

	ClickyInfo CreateClicky(string name, string role, string oneLiner);

	bool KnowsClicky(string slug);

	string OpenThread(string threadId);

	Task<string> LaunchAsync(AgentLaunchRequest request);

	Task SteerAsync(string taskId, string text);

	Task StopAsync(string taskId);

	Task ApproveExtraUsageAsync(string taskId, bool always);

	string? WorkspacePath(string clickySlug);

	void SetRoutinePaused(string routineId, bool paused);

	Task RunRoutineNowAsync(string routineId);

	void DeleteRoutine(string routineId);

	void CreateRoutine(string clickySlug, string title, string taskPrompt, int intervalMinutes, bool runImmediately);
}
