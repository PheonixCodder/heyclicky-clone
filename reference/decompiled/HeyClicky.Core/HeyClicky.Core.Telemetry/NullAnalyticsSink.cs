using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public sealed class NullAnalyticsSink : IAnalyticsSink
{
	public static readonly NullAnalyticsSink Instance = new NullAnalyticsSink();

	public string Name => "off";

	public void Enqueue(AnalyticsEvent analyticsEvent)
	{
	}

	public Task FlushAsync()
	{
		return Task.CompletedTask;
	}
}
