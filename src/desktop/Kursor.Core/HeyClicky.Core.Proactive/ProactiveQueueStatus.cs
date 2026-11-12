using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public readonly record struct ProactiveQueueStatus(ProactiveQueueStatusKind Kind, bool EarlierIsPaused = false)
{
	public string Title => Kind switch
	{
		ProactiveQueueStatusKind.Ready => "Ready to start", 
		ProactiveQueueStatusKind.Starting => "Starting", 
		ProactiveQueueStatusKind.Paused => "Ready to resume", 
		ProactiveQueueStatusKind.WaitingForClicky => "Up next", 
		_ => "Queued", 
	};

	public string Acknowledgment
	{
		get
		{
			switch (Kind)
			{
			case ProactiveQueueStatusKind.Ready:
			case ProactiveQueueStatusKind.Starting:
				return "On it.";
			case ProactiveQueueStatusKind.Paused:
				return "Saved for later.";
			default:
				return "Queued up.";
			}
		}
	}

	public static ProactiveQueueStatus Resolve(QueuedProactiveTask entry, IReadOnlyList<QueuedProactiveTask> queue, bool isClickyBusy)
	{
		if (entry.IsLaunching)
		{
			return new ProactiveQueueStatus(ProactiveQueueStatusKind.Starting);
		}
		if (entry.IsPaused)
		{
			return new ProactiveQueueStatus(ProactiveQueueStatusKind.Paused);
		}
		QueuedProactiveTask queuedProactiveTask = queue.TakeWhile((QueuedProactiveTask e) => e.Id != entry.Id).FirstOrDefault((QueuedProactiveTask e) => e.ClickySlug == entry.ClickySlug);
		if ((object)queuedProactiveTask != null)
		{
			return new ProactiveQueueStatus(ProactiveQueueStatusKind.WaitingForEarlierTask, queuedProactiveTask.IsPaused);
		}
		if (!isClickyBusy)
		{
			return new ProactiveQueueStatus(ProactiveQueueStatusKind.Ready);
		}
		return new ProactiveQueueStatus(ProactiveQueueStatusKind.WaitingForClicky);
	}

	public string Detail(string clickyName)
	{
		return Kind switch
		{
			ProactiveQueueStatusKind.Ready => "Approved — starting shortly", 
			ProactiveQueueStatusKind.Starting => "Approved — getting started", 
			ProactiveQueueStatusKind.Paused => "Approved — saved for later", 
			ProactiveQueueStatusKind.WaitingForClicky => "Starts after " + clickyName + "’s current task", 
			_ => EarlierIsPaused ? "Resume the earlier approved task first" : "Starts after the earlier approved task", 
		};
	}
}
