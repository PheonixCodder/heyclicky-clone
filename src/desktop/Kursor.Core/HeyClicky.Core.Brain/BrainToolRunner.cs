using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeyClicky.Core.Brain;

public sealed class BrainToolRunner
{
	private readonly Func<BrainToolCall, Task<string>> run;

	private readonly Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();

	private readonly List<BrainToolCall> startOrder = new List<BrainToolCall>();

	private readonly object gate = new object();

	private Task<string>? lastOrderedTask;

	public BrainToolRunner(Func<BrainToolCall, Task<string>> run)
	{
		this.run = run;
	}

	public void Start(BrainToolCall call)
	{
		lock (gate)
		{
			if (!tasks.ContainsKey(call.CallId))
			{
				startOrder.Add(call);
				Task<string> value;
				if (BrainToolCallPolicy.RunsInParallel(call.Name))
				{
					value = SafeRun(call);
				}
				else
				{
					Task<string> previous = lastOrderedTask;
					value = (lastOrderedTask = RunAfter(previous, call));
				}
				tasks[call.CallId] = value;
			}
		}
	}

	private async Task<string> RunAfter(Task<string>? previous, BrainToolCall call)
	{
		if (previous != null)
		{
			await previous.ConfigureAwait(continueOnCapturedContext: false);
		}
		return await SafeRun(call).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<string> SafeRun(BrainToolCall call)
	{
		try
		{
			return await run(call).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			return ToolOutput.Error(ex.Message);
		}
	}

	public bool HasStarted(string callId)
	{
		lock (gate)
		{
			return tasks.ContainsKey(callId);
		}
	}

	public async Task<List<string>> OutputsAsync(IReadOnlyList<BrainToolCall> calls)
	{
		foreach (BrainToolCall call in calls)
		{
			Start(call);
		}
		List<string> outputs = new List<string>(calls.Count);
		foreach (BrainToolCall call2 in calls)
		{
			Task<string> value;
			lock (gate)
			{
				tasks.TryGetValue(call2.CallId, out value);
			}
			List<string> list = outputs;
			string item = ((value != null) ? (await value.ConfigureAwait(continueOnCapturedContext: false)) : "{\"status\":\"error\",\"message\":\"the call never ran\"}");
			list.Add(item);
		}
		return outputs;
	}

	public List<(BrainToolCall Call, string Output)> FinishedCallsWithOutputs()
	{
		lock (gate)
		{
			return (from c in startOrder
				where tasks[c.CallId].IsCompletedSuccessfully
				select (c: c, Result: tasks[c.CallId].Result)).ToList();
		}
	}
}
