namespace HeyClicky.App.Drive;

public interface IAgentDriveJob
{
	string TaskId { get; }

	string? ThreadId { get; }

	string Name { get; }

	string Title { get; }

	string? AgentTurnLeaseId { get; }

	bool IsRunning { get; }

	bool NeedsChromeConnection { get; }

	bool FoundChromeNotConnectedThisTurn { get; }

	string? PendingComputerUseRequest { get; set; }

	string? PendingComputerUseSummary { get; set; }

	bool DidAnnounceComputerUseRequest { get; set; }
}
