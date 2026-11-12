namespace HeyClicky.Agents;

public enum AgentTaskEventKind
{
	StatusChanged,
	ProgressChanged,
	ThinkingHeading,
	CommentaryDelta,
	Commentary,
	ResponseDelta,
	ToolActivity,
	TitleChanged,
	Finished,
	Failed,
	Stopped,
	NeedsExtraUsageApproval,
	EarlyStopRefunded,
	SteerAccepted,
	IntegrationUsed,
	PlanUpdated,
	ExtraUsageApproved
}
