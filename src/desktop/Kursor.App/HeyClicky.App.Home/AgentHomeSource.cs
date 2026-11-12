using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Routines;
using HeyClicky.Core.Drive;

namespace HeyClicky.App.Home;

public sealed class AgentHomeSource : IHomeSource
{
	private readonly AgentService a;

	public IReadOnlyList<AgentTaskSnapshot> Tasks => a.Tasks;

	public IReadOnlyList<ClickyInfo> Clickys => a.Clickys;

	public IReadOnlyList<ClickyRoutine> Routines => a.Routines.Routines;

	public event EventHandler? TasksChanged
	{
		add
		{
			a.TasksChanged += value;
		}
		remove
		{
			a.TasksChanged -= value;
		}
	}

	public event EventHandler<AgentTaskEvent>? TaskEvent
	{
		add
		{
			a.TaskEvent += value;
		}
		remove
		{
			a.TaskEvent -= value;
		}
	}

	public event EventHandler? RoutinesChanged
	{
		add
		{
			a.RoutinesChanged += value;
		}
		remove
		{
			a.RoutinesChanged -= value;
		}
	}

	public AgentHomeSource(AgentService agents)
	{
		a = agents;
	}

	public IReadOnlyList<string>? TakeLegacyClickyMigration()
	{
		return a.TakeLegacyClickyMigration();
	}

	public AgentTaskDetail? GetTaskDetail(string taskId)
	{
		return a.GetTaskDetail(taskId);
	}

	public async Task<IReadOnlyList<AgentThreadSummary>> ListThreadsAsync(CancellationToken ct = default(CancellationToken))
	{
		List<AgentThreadSummary> all = new List<AgentThreadSummary>();
		string cursor = null;
		for (int page = 0; page < 2; page++)
		{
			var (readOnlyList, text) = await a.ListThreadsAsync(100, cursor, null, ct).ConfigureAwait(continueOnCapturedContext: false);
			all.AddRange(readOnlyList);
			if (text == null || readOnlyList.Count == 0)
			{
				break;
			}
			cursor = text;
		}
		return all;
	}

	public Task<IReadOnlyList<AgentTranscriptTurn>> ReadThreadAsync(string threadId, CancellationToken ct = default(CancellationToken))
	{
		return a.ReadThreadAsync(threadId, ct);
	}

	public Task ArchiveThreadAsync(string threadId)
	{
		return a.ArchiveThreadAsync(threadId);
	}

	public void ArchiveClicky(string slug)
	{
		a.ArchiveClicky(slug);
	}

	public ClickyInfo CreateClicky(string name, string role, string oneLiner)
	{
		return a.CreateClicky(name, role, oneLiner);
	}

	public bool KnowsClicky(string slug)
	{
		return a.KnowsClicky(slug);
	}

	public string OpenThread(string threadId)
	{
		return a.OpenThread(threadId);
	}

	public Task<string> LaunchAsync(AgentLaunchRequest request)
	{
		return a.LaunchAsync(request);
	}

	public Task SteerAsync(string taskId, string text)
	{
		MacStepWaiters.Shared.ForgetDeclined(taskId);
		return a.SteerAsync(taskId, text);
	}

	public Task StopAsync(string taskId)
	{
		return a.StopAsync(taskId);
	}

	public Task ApproveExtraUsageAsync(string taskId, bool always)
	{
		return a.ApproveExtraUsageAsync(taskId, always);
	}

	public string? WorkspacePath(string clickySlug)
	{
		return a.WorkspacePath(clickySlug);
	}

	public void SetRoutinePaused(string routineId, bool paused)
	{
		a.Routines.SetPaused(routineId, paused);
	}

	public Task RunRoutineNowAsync(string routineId)
	{
		return a.Routines.RunNowAsync(routineId);
	}

	public void DeleteRoutine(string routineId)
	{
		a.Routines.Delete(routineId);
	}

	public void CreateRoutine(string clickySlug, string title, string taskPrompt, int intervalMinutes, bool runImmediately)
	{
		if (!a.Routines.Routines.Any((ClickyRoutine r) => r.ClickySlug == clickySlug && string.Equals(r.Title, title, StringComparison.OrdinalIgnoreCase)))
		{
			a.Routines.CreateRoutine(clickySlug, title, taskPrompt, intervalMinutes, runImmediately);
		}
	}
}
