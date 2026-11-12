using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Portraits;
using HeyClicky.Core.Notch;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public static class AgentColors
{
	public static Color For(string taskId, int fallbackIndex)
	{
		return ClickyRoster.Shared.HairColorFor(string.IsNullOrEmpty(taskId) ? $"agent-{fallbackIndex}" : taskId);
	}

	public static AgentTaskSnapshot? FirstRunningTask(IAgentService agents)
	{
		return agents.Tasks.FirstOrDefault(delegate(AgentTaskSnapshot x)
		{
			AgentTaskStatus status = x.Status;
			return (uint)status <= 1u;
		});
	}

	public static Color? FirstRunning(IAgentService agents)
	{
		AgentTaskSnapshot agentTaskSnapshot = FirstRunningTask(agents);
		if ((object)agentTaskSnapshot != null)
		{
			return For(agentTaskSnapshot.TaskId, 0);
		}
		return null;
	}

	public static IReadOnlyList<AgentTaskSnapshot> RunningTasks(IAgentService? agents)
	{
		if (agents != null)
		{
			return agents.Tasks.Where(delegate(AgentTaskSnapshot x)
			{
				AgentTaskStatus status = x.Status;
				return (uint)status <= 1u;
			}).ToList();
		}
		return new List<AgentTaskSnapshot>();
	}

	public static AgentTaskSnapshot? DisplayedRunningTask(IAgentService? agents, MultiAgentRotationCursor rotation, bool isMultiAgentPhase)
	{
		IReadOnlyList<AgentTaskSnapshot> readOnlyList = RunningTasks(agents);
		if (readOnlyList.Count != 0)
		{
			return readOnlyList[rotation.IndexFor(readOnlyList.Count, isMultiAgentPhase)];
		}
		return null;
	}
}
