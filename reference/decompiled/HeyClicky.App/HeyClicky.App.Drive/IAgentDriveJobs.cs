using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeyClicky.App.Drive;

public interface IAgentDriveJobs
{
	IEnumerable<IAgentDriveJob> Jobs { get; }

	bool ChromeIsConnected { get; }

	IAgentDriveJob? Find(string taskId);

	bool IsThreadActive(string threadId);

	void SubmitFollowUp(string threadId, string text, string? preauthorizedLeaseId, string launchContext);

	Task<string?> ContinueLeaseAfterConsentAsync(string leaseId);

	void CompleteLease(string leaseId, string threadId);

	void PresentNeedsYou(IAgentDriveJob job);

	void NoteEventForBrain(IAgentDriveJob job, string eventText, string summary);

	void AnnounceVerbatim(string line);

	bool SkipsChrome(string threadId);
}
