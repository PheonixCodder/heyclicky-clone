using System;
using System.Diagnostics;
using System.Threading;
using HeyClicky.App.Shell;
using HeyClicky.Platform.Input;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Dock;

internal sealed class PointerEvaluationDriver
{
	private readonly double maximumPointerRateHz;

	private readonly Action evaluation;

	private readonly DispatcherQueue dq;

	private readonly DispatcherQueueTimer slowCheckTimer;

	private readonly DispatcherQueueTimer deadlineTimer;

	private readonly DispatcherQueueTimer soonTimer;

	private PointerMoveFeed? pointerFeed;

	private double? scheduledDeadline;

	private bool isEvaluationScheduled;

	private double lastEvaluationTime;

	private int isMoveQueued;

	public bool IsRunning { get; private set; }

	public static double Now => (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

	public PointerEvaluationDriver(double maximumPointerRateHz, double slowCheckIntervalSeconds, Action evaluation)
	{
		this.maximumPointerRateHz = maximumPointerRateHz;
		this.evaluation = evaluation;
		dq = DispatcherQueue.GetForCurrentThread();
		slowCheckTimer = dq.CreateTimer();
		slowCheckTimer.Interval = TimeSpan.FromSeconds(slowCheckIntervalSeconds);
		slowCheckTimer.Tick += delegate
		{
			RunEvaluation();
		};
		deadlineTimer = dq.CreateTimer();
		deadlineTimer.IsRepeating = false;
		deadlineTimer.Tick += delegate
		{
			scheduledDeadline = null;
			RunEvaluation();
		};
		soonTimer = dq.CreateTimer();
		soonTimer.IsRepeating = false;
		soonTimer.Tick += delegate
		{
			isEvaluationScheduled = false;
			RunEvaluation();
		};
	}

	public void Start()
	{
		if (IsRunning)
		{
			return;
		}
		IsRunning = true;
		if (!OverlayWindow.Offscreen)
		{
			pointerFeed = new PointerMoveFeed(maximumPointerRateHz, delegate
			{
				if (Interlocked.Exchange(ref isMoveQueued, 1) == 0)
				{
					dq.TryEnqueue(delegate
					{
						Volatile.Write(ref isMoveQueued, 0);
						RunEvaluation();
					});
				}
			});
		}
		slowCheckTimer.Start();
		EvaluateSoon();
	}

	public void Stop()
	{
		IsRunning = false;
		pointerFeed?.Dispose();
		pointerFeed = null;
		slowCheckTimer.Stop();
		deadlineTimer.Stop();
		soonTimer.Stop();
		isEvaluationScheduled = false;
		scheduledDeadline = null;
	}

	public void EvaluateSoon()
	{
		if (!IsRunning || isEvaluationScheduled)
		{
			return;
		}
		isEvaluationScheduled = true;
		double num = Math.Max(0.0, lastEvaluationTime + 1.0 / maximumPointerRateHz - Now);
		if (num <= 0.0)
		{
			dq.TryEnqueue(DispatcherQueuePriority.Low, delegate
			{
				if (isEvaluationScheduled)
				{
					isEvaluationScheduled = false;
					RunEvaluation();
				}
			});
		}
		else
		{
			soonTimer.Interval = TimeSpan.FromSeconds(num);
			soonTimer.Start();
		}
	}

	public void EvaluateAt(double? deadline)
	{
		if (deadline != scheduledDeadline)
		{
			deadlineTimer.Stop();
			scheduledDeadline = deadline;
			if (IsRunning && deadline.HasValue)
			{
				double valueOrDefault = deadline.GetValueOrDefault();
				deadlineTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.0, valueOrDefault - Now));
				deadlineTimer.Start();
			}
		}
	}

	private void RunEvaluation()
	{
		if (IsRunning)
		{
			lastEvaluationTime = Now;
			evaluation();
		}
	}
}
