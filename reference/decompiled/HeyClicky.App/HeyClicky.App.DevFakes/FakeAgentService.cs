using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;

namespace HeyClicky.App.DevFakes;

public sealed class FakeAgentService : IAgentService, IAsyncDisposable
{
	public sealed class FakeDetail
	{
		public List<AgentActivityItem> Activity { get; } = new List<AgentActivityItem>();

		public List<string> UserPrompts { get; } = new List<string>();

		public List<string> Artifacts { get; } = new List<string>();

		public List<string> NextActions { get; } = new List<string>();

		public string? Summary { get; set; }

		public string? WorkspacePath { get; set; }

		public string? FailureMessage { get; set; }

		public bool RequiresExtraUsageApproval { get; set; }

		public bool DidRefundEarlyStop { get; set; }
	}

	private readonly List<AgentTaskSnapshot> tasks = new List<AgentTaskSnapshot>();

	private readonly object gate = new object();

	private readonly Dictionary<string, FakeDetail> details = new Dictionary<string, FakeDetail>();

	public IReadOnlyList<AgentTaskSnapshot> Tasks
	{
		get
		{
			lock (gate)
			{
				return tasks.ToList();
			}
		}
	}

	public bool IsRuntimeReady => true;

	public event EventHandler? TasksChanged;

	public event EventHandler<AgentTaskEvent>? TaskEvent;

	public Task EnsureRuntimeReadyAsync(CancellationToken ct = default(CancellationToken))
	{
		return Task.CompletedTask;
	}

	private void Upsert(AgentTaskSnapshot s)
	{
		lock (gate)
		{
			int num = tasks.FindIndex((AgentTaskSnapshot t) => t.TaskId == s.TaskId);
			if (num >= 0)
			{
				tasks[num] = s;
			}
			else
			{
				tasks.Add(s);
			}
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
	}

	public async Task<string> LaunchAsync(string prompt, IReadOnlyList<byte[]>? imagesJpeg = null, CancellationToken ct = default(CancellationToken))
	{
		string id = Guid.NewGuid().ToString("N").Substring(0, 8);
		string title = ((prompt.Length > 28) ? (prompt.Substring(0, 28).TrimEnd() + "…") : prompt);
		AgentTaskSnapshot snap = new AgentTaskSnapshot(id, null, title, AgentTaskStatus.Starting, "", null, DateTimeOffset.Now);
		DetailFor(id).UserPrompts.Add(prompt);
		Upsert(snap);
		Task.Run(async delegate
		{
			string[] steps = new string[4] { "Reading the request", "Searching the web for sources", "Comparing three options", "Writing the summary" };
			await Task.Delay(600);
			string[] array = steps;
			foreach (string latestCommentary in array)
			{
				lock (gate)
				{
					AgentTaskSnapshot agentTaskSnapshot = tasks.Find((AgentTaskSnapshot t) => t.TaskId == id);
					if ((object)agentTaskSnapshot != null && agentTaskSnapshot.Status == AgentTaskStatus.Stopped)
					{
						return;
					}
				}
				Upsert(snap = snap with
				{
					Status = AgentTaskStatus.Running,
					LatestCommentary = latestCommentary
				});
				await Task.Delay(1800);
			}
			Upsert(snap with
			{
				Status = AgentTaskStatus.Finished,
				LatestCommentary = "Done. Summary ready.",
				FinalResponseMarkdown = "## Summary\n\nI compared **three** options:\n\n- Option A is fastest\n- Option B is cheapest\n- Option C balances both\n\n```\nrecommendation: C\n```"
			});
		});
		await Task.Yield();
		return id;
	}

	public Task SteerAsync(string taskId, string text, CancellationToken ct = default(CancellationToken))
	{
		lock (gate)
		{
			int num = tasks.FindIndex((AgentTaskSnapshot t) => t.TaskId == taskId);
			if (num >= 0)
			{
				tasks[num] = tasks[num]with
				{
					LatestCommentary = "Got it: " + text,
					Status = AgentTaskStatus.Running
				};
			}
		}
		DetailFor(taskId).UserPrompts.Add(text);
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
		this.TaskEvent?.Invoke(this, new AgentTaskEvent(taskId, AgentTaskEventKind.ProgressChanged, "Steering the agent…"));
		this.TaskEvent?.Invoke(this, new AgentTaskEvent(taskId, AgentTaskEventKind.SteerAccepted, text));
		return Task.CompletedTask;
	}

	public Task StopAsync(string taskId)
	{
		lock (gate)
		{
			int num = tasks.FindIndex((AgentTaskSnapshot t) => t.TaskId == taskId);
			if (num >= 0)
			{
				tasks[num] = tasks[num]with
				{
					Status = AgentTaskStatus.Stopped,
					LatestCommentary = "Stopped."
				};
			}
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public void Seed(params AgentTaskSnapshot[] snapshots)
	{
		lock (gate)
		{
			tasks.Clear();
			tasks.AddRange(snapshots);
			details.Clear();
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
	}

	public FakeDetail DetailFor(string taskId)
	{
		lock (gate)
		{
			if (!details.TryGetValue(taskId, out FakeDetail value))
			{
				value = (details[taskId] = new FakeDetail());
			}
			return value;
		}
	}

	public AgentTaskDetail? GetTaskDetail(string taskId)
	{
		AgentTaskSnapshot agentTaskSnapshot;
		FakeDetail fakeDetail;
		lock (gate)
		{
			agentTaskSnapshot = tasks.Find((AgentTaskSnapshot t) => t.TaskId == taskId);
			fakeDetail = (details.TryGetValue(taskId, out FakeDetail value) ? value : new FakeDetail());
		}
		if ((object)agentTaskSnapshot == null)
		{
			return null;
		}
		return new AgentTaskDetail(agentTaskSnapshot, null, agentTaskSnapshot.Title, fakeDetail.WorkspacePath, null, agentTaskSnapshot.LatestCommentary, null, "", "", fakeDetail.Activity.ToList(), fakeDetail.UserPrompts.ToList(), fakeDetail.Summary, null, fakeDetail.NextActions.ToList(), fakeDetail.Artifacts.ToList(), fakeDetail.FailureMessage, null, fakeDetail.RequiresExtraUsageApproval, fakeDetail.DidRefundEarlyStop, null, 0L, 0L, 0L, IsRoutineRun: false);
	}

	public Task ApproveExtraUsageAsync(string taskId, bool asManyAsNeeded, CancellationToken ct = default(CancellationToken))
	{
		DetailFor(taskId).RequiresExtraUsageApproval = false;
		lock (gate)
		{
			int num = tasks.FindIndex((AgentTaskSnapshot t) => t.TaskId == taskId);
			if (num >= 0)
			{
				tasks[num] = tasks[num]with
				{
					Status = AgentTaskStatus.Running,
					LatestCommentary = "Picking up where I left off"
				};
			}
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public void DismissTask(string taskId)
	{
		lock (gate)
		{
			tasks.RemoveAll((AgentTaskSnapshot t) => t.TaskId == taskId);
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
	}

	public void RaiseTaskEvent(AgentTaskEvent e)
	{
		this.TaskEvent?.Invoke(this, e);
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}
