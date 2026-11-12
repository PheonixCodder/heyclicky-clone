using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public interface IAnalyticsSink
{
	string Name { get; }

	void Enqueue(AnalyticsEvent analyticsEvent);

	Task FlushAsync();
}
