using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyJobStepWatcher : IDisposable
{
	private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2.5);

	private static readonly TimeSpan MaximumWait = TimeSpan.FromHours(6);

	private readonly ClickyJobStore store;

	private readonly object gate = new object();

	private Timer? timer;

	private readonly HashSet<string> jobIdsSeenNotDone = new HashSet<string>();

	private readonly HashSet<string> jobIdsContinued = new HashSet<string>();

	public Func<string?>? FrontmostBrowserAddress { get; set; }

	public event Action<string, string>? StepDone;

	public ClickyJobStepWatcher(ClickyJobStore store)
	{
		this.store = store;
		store.Changed += delegate
		{
			Reconcile();
		};
	}

	private List<ClickyJob> WatchedJobs()
	{
		DateTimeOffset now = DateTimeOffset.Now;
		lock (gate)
		{
			return store.Jobs.Where(delegate(ClickyJob job)
			{
				if (!job.IsDismissed)
				{
					ClickyJobUserStep waitingStep = job.WaitingStep;
					if ((object)waitingStep != null && waitingStep.DoneWhenUrlContains != null && now - waitingStep.AskedAt < MaximumWait)
					{
						return !jobIdsContinued.Contains(job.Id);
					}
				}
				return false;
			}).ToList();
		}
	}

	public void Reconcile()
	{
		HashSet<string> hashSet = (from j in WatchedJobs()
			select j.Id).ToHashSet();
		lock (gate)
		{
			jobIdsSeenNotDone.IntersectWith(hashSet);
			jobIdsContinued.RemoveWhere((string id) => (object)store.Job(id)?.WaitingStep == null);
			if (hashSet.Count == 0)
			{
				timer?.Dispose();
				timer = null;
			}
			else if (timer == null)
			{
				timer = new Timer(delegate
				{
					Check();
				}, null, CheckInterval, CheckInterval);
			}
		}
	}

	private void Check()
	{
		try
		{
			List<ClickyJob> list = WatchedJobs();
			if (list.Count == 0)
			{
				Reconcile();
				return;
			}
			string text = FrontmostBrowserAddress?.Invoke();
			if (text == null || text.Length <= 0)
			{
				return;
			}
			string text2 = text.ToLowerInvariant();
			foreach (ClickyJob item in list)
			{
				string text3 = item.WaitingStep?.DoneWhenUrlContains?.ToLowerInvariant();
				if (text3 == null)
				{
					continue;
				}
				bool flag;
				lock (gate)
				{
					if (!text2.Contains(text3))
					{
						jobIdsSeenNotDone.Add(item.Id);
						flag = false;
					}
					else if (jobIdsSeenNotDone.Contains(item.Id))
					{
						jobIdsContinued.Add(item.Id);
						flag = true;
					}
					else
					{
						flag = false;
					}
				}
				if (flag)
				{
					this.StepDone?.Invoke(item.Id, text);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "step watcher: " + ex.Message);
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			timer?.Dispose();
			timer = null;
		}
	}
}
