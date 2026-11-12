using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public sealed class LogAnalyticsSink : IAnalyticsSink
{
	private readonly object gate = new object();

	private readonly List<AnalyticsEvent> recent = new List<AnalyticsEvent>();

	public string Name => "log";

	public IReadOnlyList<AnalyticsEvent> Recent
	{
		get
		{
			lock (gate)
			{
				return recent.ToArray();
			}
		}
	}

	public void Enqueue(AnalyticsEvent analyticsEvent)
	{
		lock (gate)
		{
			recent.Add(analyticsEvent);
			if (recent.Count > 500)
			{
				recent.RemoveRange(0, recent.Count - 500);
			}
		}
		Log.Info("analytics", analyticsEvent.ToPostHogJson().ToJsonString());
	}

	public Task FlushAsync()
	{
		return Task.CompletedTask;
	}
}
