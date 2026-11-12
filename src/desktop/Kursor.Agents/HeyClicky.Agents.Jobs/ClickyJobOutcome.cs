using System.Text.Json.Serialization;

namespace HeyClicky.Agents.Jobs;

[JsonConverter(typeof(JsonStringEnumConverter<ClickyJobOutcome>))]
public enum ClickyJobOutcome
{
	[JsonStringEnumMemberName("working")]
	Working,
	[JsonStringEnumMemberName("done")]
	Done,
	[JsonStringEnumMemberName("failed")]
	Failed,
	[JsonStringEnumMemberName("stopped")]
	Stopped
}
