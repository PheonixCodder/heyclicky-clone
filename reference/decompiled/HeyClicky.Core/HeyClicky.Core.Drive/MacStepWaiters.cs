using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Drive;

public sealed class MacStepWaiters
{
	private sealed class Waiting(string step, DateTimeOffset askedAt)
	{
		public readonly string Step = step;

		public readonly DateTimeOffset AskedAt = askedAt;

		public readonly List<TaskCompletionSource<MacStepAnswer>> Calls = new List<TaskCompletionSource<MacStepAnswer>>();
	}

	public static readonly MacStepWaiters Shared = new MacStepWaiters();

	private readonly object gate = new object();

	private readonly Dictionary<string, Waiting> waiting = new Dictionary<string, Waiting>();

	private readonly HashSet<string> driving = new HashSet<string>();

	private readonly Dictionary<string, string> unanswered = new Dictionary<string, string>();

	private readonly Func<DateTimeOffset> now;

	public static readonly TimeSpan DeclinedStepMemory = TimeSpan.FromMinutes(30L);

	private readonly Dictionary<string, List<(string Step, DateTimeOffset At)>> declinedSteps = new Dictionary<string, List<(string, DateTimeOffset)>>();

	public MacStepWaiters(Func<DateTimeOffset>? clock = null)
	{
		now = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
	}

	public bool IsWaiting(string taskId)
	{
		lock (gate)
		{
			return waiting.ContainsKey(taskId);
		}
	}

	public int WaitingCallCount(string taskId)
	{
		lock (gate)
		{
			Waiting value;
			return waiting.TryGetValue(taskId, out value) ? value.Calls.Count : 0;
		}
	}

	public string? Step(string taskId)
	{
		lock (gate)
		{
			Waiting value;
			return waiting.TryGetValue(taskId, out value) ? value.step : null;
		}
	}

	public void MarkDriving(string taskId)
	{
		lock (gate)
		{
			driving.Add(taskId);
		}
	}

	public void EndDrive(string taskId)
	{
		lock (gate)
		{
			driving.Remove(taskId);
		}
	}

	public bool HasStepAsked(TimeSpan within)
	{
		lock (gate)
		{
			return waiting.Values.Any((Waiting w) => now() - w.askedAt < within);
		}
	}

	public void RememberUnanswered(string taskId, string request)
	{
		lock (gate)
		{
			unanswered[taskId] = request;
		}
	}

	public void ForgetUnanswered(string taskId)
	{
		lock (gate)
		{
			unanswered.Remove(taskId);
		}
	}

	public void RememberDeclined(string taskId, string step)
	{
		string text = step.Trim();
		if (text.Length == 0)
		{
			return;
		}
		lock (gate)
		{
			if (!declinedSteps.TryGetValue(taskId, out List<(string, DateTimeOffset)> value))
			{
				value = (declinedSteps[taskId] = new List<(string, DateTimeOffset)>());
			}
			value.Add((text, now()));
		}
	}

	public void ForgetDeclined(string taskId)
	{
		lock (gate)
		{
			declinedSteps.Remove(taskId);
		}
	}

	public bool WasRecentlyDeclined(string taskId, string step)
	{
		string trimmed = step.Trim();
		lock (gate)
		{
			if (!declinedSteps.TryGetValue(taskId, out List<(string, DateTimeOffset)> value))
			{
				return false;
			}
			value.RemoveAll(((string Step, DateTimeOffset At) d) => now() - d.At >= DeclinedStepMemory);
			if (value.Count == 0)
			{
				declinedSteps.Remove(taskId);
				return false;
			}
			return trimmed.Length > 0 && value.Any<(string, DateTimeOffset)>(((string Step, DateTimeOffset At) d) => d.Step == trimmed || AgentDriveReports.DriveGoalSharesWordsWithStep(d.Step, trimmed));
		}
	}

	public string? TakeUnansweredStep(string taskId)
	{
		lock (gate)
		{
			string value;
			return unanswered.Remove(taskId, out value) ? value : null;
		}
	}

	public Task<MacStepAnswer> WaitAsync(string taskId, string step, TimeSpan openCall, Func<bool> stillRunning, CancellationToken ct = default(CancellationToken))
	{
		TaskCompletionSource<MacStepAnswer> taskCompletionSource = new TaskCompletionSource<MacStepAnswer>(TaskCreationOptions.RunContinuationsAsynchronously);
		bool flag;
		lock (gate)
		{
			flag = !this.waiting.TryGetValue(taskId, out Waiting value);
			if (flag)
			{
				value = (this.waiting[taskId] = new Waiting(step, now()));
			}
			value.Calls.Add(taskCompletionSource);
		}
		if (flag)
		{
			WatchAsync(taskId, openCall, stillRunning, ct);
		}
		return taskCompletionSource.Task;
	}

	private async Task WatchAsync(string taskId, TimeSpan openCall, Func<bool> stillRunning, CancellationToken ct)
	{
		DateTimeOffset deadline = now() + openCall;
		while (true)
		{
			lock (gate)
			{
				if (!waiting.ContainsKey(taskId))
				{
					return;
				}
			}
			bool flag;
			try
			{
				flag = stillRunning();
			}
			catch
			{
				flag = false;
			}
			if (!flag || ct.IsCancellationRequested)
			{
				Resolve(taskId, MacStepAnswer.NoAnswerAnswer);
				return;
			}
			if (now() > deadline)
			{
				break;
			}
			try
			{
				await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(2000.0, Math.Max(50.0, openCall.TotalMilliseconds / 4.0))), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				return;
			}
		}
		bool flag2;
		lock (gate)
		{
			flag2 = driving.Contains(taskId);
		}
		Resolve(taskId, flag2 ? MacStepAnswer.BeingDrivenAnswer : MacStepAnswer.NotYetAnswer);
	}

	public void Resolve(string taskId, MacStepAnswer answer)
	{
		List<TaskCompletionSource<MacStepAnswer>> calls;
		lock (gate)
		{
			if (!(answer is MacStepAnswer.BeingDriven))
			{
				driving.Remove(taskId);
			}
			if (!waiting.Remove(taskId, out Waiting value))
			{
				return;
			}
			calls = value.Calls;
		}
		foreach (TaskCompletionSource<MacStepAnswer> item in calls)
		{
			item.TrySetResult(answer);
		}
	}
}
