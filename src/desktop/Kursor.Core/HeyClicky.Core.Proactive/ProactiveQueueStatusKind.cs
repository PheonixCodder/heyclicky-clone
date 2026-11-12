namespace HeyClicky.Core.Proactive;

public enum ProactiveQueueStatusKind
{
	Ready,
	Starting,
	Paused,
	WaitingForClicky,
	WaitingForEarlierTask
}
