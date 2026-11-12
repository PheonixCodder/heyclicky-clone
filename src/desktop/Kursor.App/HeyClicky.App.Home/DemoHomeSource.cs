using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Routines;

namespace HeyClicky.App.Home;

public sealed class DemoHomeSource : IHomeSource
{
	private readonly object gate = new object();

	private readonly List<ClickyInfo> clickys;

	private readonly List<AgentThreadSummary> threads;

	private readonly List<ClickyRoutine> routines;

	private readonly Dictionary<string, List<AgentTranscriptTurn>> transcripts = new Dictionary<string, List<AgentTranscriptTurn>>();

	private readonly Dictionary<string, AgentTaskDetail> details = new Dictionary<string, AgentTaskDetail>();

	private static readonly string Root = Path.Combine(Path.GetTempPath(), "heyclicky-home-demo");

	public IReadOnlyList<AgentTaskSnapshot> Tasks
	{
		get
		{
			lock (gate)
			{
				return details.Values.Select((AgentTaskDetail d) => d.Snapshot).ToList();
			}
		}
	}

	public IReadOnlyList<ClickyInfo> Clickys
	{
		get
		{
			lock (gate)
			{
				return clickys.ToList();
			}
		}
	}

	public IReadOnlyList<ClickyRoutine> Routines
	{
		get
		{
			lock (gate)
			{
				return routines.ToList();
			}
		}
	}

	public event EventHandler? TasksChanged;

	public event EventHandler<AgentTaskEvent>? TaskEvent;

	public event EventHandler? RoutinesChanged;

	public DemoHomeSource()
	{
		DateTimeOffset now = DateTimeOffset.Now;
		int num = 4;
		List<ClickyInfo> list = new List<ClickyInfo>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<ClickyInfo> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new ClickyInfo("trip-planner", "Trip Planner", "Researcher", "Plans trips and compares flights", "t-trip", now.AddDays(-6.0));
		num2++;
		span[num2] = new ClickyInfo("launch-writer", "Launch Writer", "Writer", "Drafts launch emails and posts", "t-launch", now.AddDays(-3.0));
		num2++;
		span[num2] = new ClickyInfo("metrics-buddy", "Metrics Buddy", "Analyst", "Summarizes the weekly numbers", "t-metrics", now.AddDays(-9.0));
		num2++;
		span[num2] = new ClickyInfo("inbox-zero", "Inbox Zero", "Helper", "Keeps your inbox tidy", null, now.AddDays(-1.0));
		clickys = list;
		num2 = 3;
		List<AgentThreadSummary> list2 = new List<AgentThreadSummary>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		Span<AgentThreadSummary> span2 = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span2[num] = new AgentThreadSummary("t-trip", "Trip Planner", "Find me flights to Tokyo in March under $900", now.AddDays(-6.0), now.AddMinutes(-2.0), "active", null, "trip-planner");
		num++;
		span2[num] = new AgentThreadSummary("t-launch", "Launch Writer", "Draft the launch email for the Windows app", now.AddDays(-3.0), now.AddHours(-3.0), "idle", null, "launch-writer");
		num++;
		span2[num] = new AgentThreadSummary("t-metrics", "Metrics Buddy", "Summarize Q3 metrics", now.AddDays(-9.0), now.AddDays(-1.0).AddHours(-2.0), "idle", null, "metrics-buddy");
		threads = list2;
		Directory.CreateDirectory(Path.Combine(Root, "metrics-buddy", "output"));
		string[] array = new string[2] { "q3-summary.md", "revenue.csv" };
		foreach (string path in array)
		{
			string path2 = Path.Combine(Root, "metrics-buddy", "output", path);
			if (!File.Exists(path2))
			{
				File.WriteAllText(path2, "demo");
			}
		}
		DemoFiles.WriteChartPng(Path.Combine(Root, "metrics-buddy", "output", "chart.png"));
		DemoFiles.WritePdf(Path.Combine(Root, "metrics-buddy", "output", "q3-report.pdf"), "Q3 at a glance", "Revenue +12% QoQ. Churn 2.1%. NRR 108%.");
		Dictionary<string, List<AgentTranscriptTurn>> dictionary = transcripts;
		num = 2;
		List<AgentTranscriptTurn> list3 = new List<AgentTranscriptTurn>(num);
		CollectionsMarshal.SetCount(list3, num);
		Span<AgentTranscriptTurn> span3 = CollectionsMarshal.AsSpan(list3);
		num2 = 0;
		span3[num2] = new AgentTranscriptTurn("0", new global::_003C_003Ez__ReadOnlySingleElementList<string>("Pull the Q3 dashboard export"), new global::_003C_003Ez__ReadOnlySingleElementList<string>("Opening the dashboard export."), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "failed", "unexpected status 502 Bad Gateway: {\"error\":\"upstream timed out\"}", now.AddDays(-1.0).AddHours(-3.0), now.AddDays(-1.0).AddHours(-3.0).AddSeconds(41.0));
		num2++;
		span3[num2] = new AgentTranscriptTurn("1", new global::_003C_003Ez__ReadOnlySingleElementList<string>("Summarize Q3 metrics from the dashboard export"), new global::_003C_003Ez__ReadOnlyArray<string>(new string[6] { "Reading the export first.", "$ Get-Content -Path revenue.csv -TotalCount 20", "Search: SaaS net revenue retention benchmark", "$ python summarize.py revenue.csv", "*** Add File: output/q3-summary.md", "Writing the summary now." }), new global::_003C_003Ez__ReadOnlySingleElementList<string>("## Q3 at a glance\n\nRevenue grew **12%** quarter over quarter, led by the Pro plan.\n\n| Metric | Q2 | Q3 |\n|---|---|---|\n| Revenue | $1.20M | $1.34M |\n| Active users | 41k | 47k |\n\n- Churn fell to **2.1%**\n- Support tickets dropped by a third\n\n```\nnet_revenue_retention: 108%\n```"), new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "Turn this into slides", "Email it to the team" }), new global::_003C_003Ez__ReadOnlyArray<string>(new string[4]
		{
			Path.Combine(Root, "metrics-buddy", "output", "q3-summary.md"),
			Path.Combine(Root, "metrics-buddy", "output", "q3-report.pdf"),
			Path.Combine(Root, "metrics-buddy", "output", "revenue.csv"),
			Path.Combine(Root, "metrics-buddy", "output", "chart.png")
		}), "completed", null, now.AddDays(-1.0).AddHours(-2.0).AddSeconds(-72.0), now.AddDays(-1.0).AddHours(-2.0));
		dictionary["t-metrics"] = list3;
		Dictionary<string, List<AgentTranscriptTurn>> dictionary2 = transcripts;
		num2 = 1;
		list3 = new List<AgentTranscriptTurn>(num2);
		CollectionsMarshal.SetCount(list3, num2);
		span3 = CollectionsMarshal.AsSpan(list3);
		num = 0;
		span3[num] = new AgentTranscriptTurn("1", new global::_003C_003Ez__ReadOnlySingleElementList<string>("Draft the launch email for the Windows app"), new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "Reading the release notes", "Drafting" }), new global::_003C_003Ez__ReadOnlySingleElementList<string>("Here's a draft:\n\n**Subject:** HeyClicky is on Windows\n\nHi there,\n\nHeyClicky now lives in your Windows notch. Hold *Ctrl + Alt* and ask for anything.\n\n1. Talk to it\n2. Watch it draw on your screen\n3. Hand off work to a Clicky"), Array.Empty<string>(), Array.Empty<string>(), "completed", null, null, null);
		dictionary2["t-launch"] = list3;
		Dictionary<string, List<AgentTranscriptTurn>> dictionary3 = transcripts;
		num = 1;
		List<AgentTranscriptTurn> list4 = new List<AgentTranscriptTurn>(num);
		CollectionsMarshal.SetCount(list4, num);
		span3 = CollectionsMarshal.AsSpan(list4);
		num2 = 0;
		span3[num2] = new AgentTranscriptTurn("1", new global::_003C_003Ez__ReadOnlySingleElementList<string>("Find me flights to Tokyo in March under $900"), new global::_003C_003Ez__ReadOnlySingleElementList<string>("Searching the web"), new global::_003C_003Ez__ReadOnlySingleElementList<string>("I found three options under $900. Want me to keep watching prices?"), Array.Empty<string>(), Array.Empty<string>(), "completed", null, null, null);
		dictionary3["t-trip"] = list4;
		details["demo-run"] = new AgentTaskDetail(new AgentTaskSnapshot("demo-run", "t-trip", "Trip Planner", AgentTaskStatus.Running, "Comparing three airlines on the March dates", null, now.AddSeconds(-42.0)), "trip-planner", "Trip Planner", Path.Combine(Root, "trip-planner"), "turn-2", "Comparing three airlines", "Searching the web", "", "", new global::_003C_003Ez__ReadOnlyArray<AgentActivityItem>(new AgentActivityItem[3]
		{
			new AgentActivityItem("a1", AgentActivityKind.Commentary, "Checking ANA, JAL and United for the week of March 9.", null, now.AddSeconds(-30.0)),
			new AgentActivityItem("a2", AgentActivityKind.ToolCall, "Searching the web", true, now.AddSeconds(-20.0)),
			new AgentActivityItem("a3", AgentActivityKind.Commentary, "Comparing three airlines on the March dates.", null, now.AddSeconds(-8.0))
		}), new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "Find me flights to Tokyo in March under $900", "Also check nonstop only" }), null, null, Array.Empty<string>(), Array.Empty<string>(), null, null, RequiresExtraUsageApproval: false, DidRefundEarlyStop: false, null, 0L, 0L, 0L, IsRoutineRun: false);
		num2 = 2;
		List<ClickyRoutine> list5 = new List<ClickyRoutine>(num2);
		CollectionsMarshal.SetCount(list5, num2);
		Span<ClickyRoutine> span4 = CollectionsMarshal.AsSpan(list5);
		num = 0;
		span4[num] = new ClickyRoutine
		{
			ClickySlug = "metrics-buddy",
			Title = "Monday metrics digest",
			TaskPrompt = "Every Monday morning, summarize last week's numbers and post them to the team.",
			IntervalSeconds = 604800.0,
			NextRunAt = now.AddDays(2.0).Date.AddHours(9.0),
			RunCount = 6,
			LastRunFinishedAt = now.AddDays(-5.0)
		};
		num++;
		span4[num] = new ClickyRoutine
		{
			ClickySlug = "inbox-zero",
			Title = "Tidy the inbox",
			TaskPrompt = "Archive newsletters older than a week.",
			IntervalSeconds = 14400.0,
			IsPaused = true,
			RunCount = 14
		};
		routines = list5;
	}

	public AgentTaskDetail? GetTaskDetail(string taskId)
	{
		lock (gate)
		{
			return details.GetValueOrDefault(taskId);
		}
	}

	public Task<IReadOnlyList<AgentThreadSummary>> ListThreadsAsync(CancellationToken ct = default(CancellationToken))
	{
		lock (gate)
		{
			return Task.FromResult((IReadOnlyList<AgentThreadSummary>)threads.ToList());
		}
	}

	public Task<IReadOnlyList<AgentTranscriptTurn>> ReadThreadAsync(string threadId, CancellationToken ct = default(CancellationToken))
	{
		lock (gate)
		{
			return Task.FromResult((IReadOnlyList<AgentTranscriptTurn>)(transcripts.GetValueOrDefault(threadId)?.ToList() ?? new List<AgentTranscriptTurn>()));
		}
	}

	public Task ArchiveThreadAsync(string threadId)
	{
		lock (gate)
		{
			threads.RemoveAll((AgentThreadSummary t) => t.ThreadId == threadId);
			int num = clickys.FindIndex((ClickyInfo c) => c.ThreadId == threadId);
			if (num >= 0)
			{
				clickys.RemoveAt(num);
			}
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public void ArchiveClicky(string slug)
	{
		lock (gate)
		{
			clickys.RemoveAll((ClickyInfo c) => c.Slug == slug);
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
	}

	public bool KnowsClicky(string slug)
	{
		lock (gate)
		{
			return clickys.Any((ClickyInfo c) => c.Slug == slug);
		}
	}

	public ClickyInfo CreateClicky(string name, string role, string oneLiner)
	{
		ClickyInfo clickyInfo = new ClickyInfo(name.Trim().ToLowerInvariant().Replace(' ', '-'), name, role, oneLiner, null, DateTimeOffset.Now);
		lock (gate)
		{
			clickys.Add(clickyInfo);
		}
		this.TasksChanged?.Invoke(this, EventArgs.Empty);
		return clickyInfo;
	}

	public string OpenThread(string threadId)
	{
		return details.Values.FirstOrDefault((AgentTaskDetail d) => d.Snapshot.ThreadId == threadId)?.Snapshot.TaskId ?? ("open-" + threadId);
	}

	public Task<string> LaunchAsync(AgentLaunchRequest request)
	{
		return Task.FromResult("demo-run");
	}

	public Task SteerAsync(string taskId, string text)
	{
		this.TaskEvent?.Invoke(this, new AgentTaskEvent(taskId, AgentTaskEventKind.ProgressChanged, "Steering the agent…"));
		this.TaskEvent?.Invoke(this, new AgentTaskEvent(taskId, AgentTaskEventKind.SteerAccepted, text));
		return Task.CompletedTask;
	}

	public Task StopAsync(string taskId)
	{
		return Task.CompletedTask;
	}

	public Task ApproveExtraUsageAsync(string taskId, bool always)
	{
		return Task.CompletedTask;
	}

	public string? WorkspacePath(string clickySlug)
	{
		return Path.Combine(Root, clickySlug);
	}

	public void SetRoutinePaused(string routineId, bool paused)
	{
		lock (gate)
		{
			ClickyRoutine clickyRoutine = routines.FirstOrDefault((ClickyRoutine x) => x.Id == routineId);
			if (clickyRoutine != null)
			{
				clickyRoutine.IsPaused = paused;
			}
		}
		this.RoutinesChanged?.Invoke(this, EventArgs.Empty);
	}

	public Task RunRoutineNowAsync(string routineId)
	{
		return Task.CompletedTask;
	}

	public void CreateRoutine(string clickySlug, string title, string taskPrompt, int intervalMinutes, bool runImmediately)
	{
		lock (gate)
		{
			routines.Add(ClickyRoutine.Create(clickySlug, title, taskPrompt, intervalMinutes));
		}
		this.RoutinesChanged?.Invoke(this, EventArgs.Empty);
	}

	public void DeleteRoutine(string routineId)
	{
		lock (gate)
		{
			routines.RemoveAll((ClickyRoutine r) => r.Id == routineId);
		}
		this.RoutinesChanged?.Invoke(this, EventArgs.Empty);
	}
}
