using HeyClicky.Agents;
using HeyClicky.App.Drive;

namespace HeyClicky.App.Dock;

internal sealed class DriveJob(AgentService agents, string taskId) : IAgentDriveJob
{
	private AgentTaskDetail? Detail => agents.GetTaskDetail(taskId);

	public string TaskId => taskId;

	public string? ThreadId => Detail?.Snapshot.ThreadId;

	public string Name
	{
		get
		{
			AgentTaskDetail detail = Detail;
			string text = detail?.ClickyName;
			if (text != null && text.Length > 0)
			{
				return text;
			}
			string text2 = detail?.JobId;
			if (text2 != null)
			{
				string text3 = agents.Jobs.Job(text2)?.Title;
				if (text3 != null && text3.Length > 0)
				{
					return text3;
				}
			}
			return "Your job";
		}
	}

	public string Title => Detail?.Snapshot.Title ?? "";

	public string? AgentTurnLeaseId => agents.TurnLeaseId(taskId);

	public bool IsRunning
	{
		get
		{
			AgentTaskStatus? agentTaskStatus = Detail?.Snapshot.Status;
			if (agentTaskStatus.HasValue)
			{
				AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool NeedsChromeConnection
	{
		get
		{
			string threadId = ThreadId;
			if (threadId != null)
			{
				return agents.NeedsChromeConnection(threadId);
			}
			return false;
		}
	}

	public bool FoundChromeNotConnectedThisTurn
	{
		get
		{
			string threadId = ThreadId;
			if (threadId != null)
			{
				return agents.FoundChromeNotConnectedThisTurn(threadId);
			}
			return false;
		}
	}

	public string? PendingComputerUseRequest
	{
		get
		{
			return agents.PendingComputerUseRequest(taskId);
		}
		set
		{
			agents.SetPendingComputerUseRequest(taskId, value);
		}
	}

	public string? PendingComputerUseSummary
	{
		get
		{
			return agents.PendingComputerUseSummary(taskId);
		}
		set
		{
			agents.SetPendingComputerUseSummary(taskId, value);
		}
	}

	public bool DidAnnounceComputerUseRequest
	{
		get
		{
			return agents.DidAnnounceComputerUseRequest(taskId);
		}
		set
		{
			agents.SetDidAnnounceComputerUseRequest(taskId, value);
		}
	}
}
