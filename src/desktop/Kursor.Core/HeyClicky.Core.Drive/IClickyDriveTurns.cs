using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Drive;

public interface IClickyDriveTurns
{
	bool OffersDrive { get; }

	bool IsDriving { get; }

	IReadOnlyList<string> ActionLog { get; }

	bool IsDriveToolName(string name);

	JsonObject TakeDriveBillingRequestFields();

	void NoteDriveLeaseId(string leaseId);

	void NoteSpokenStep(string sentence);

	Task<ScreenCaptureFrame?> PerformComputerActionsAsync(IReadOnlyList<JsonObject> actions, int audioPlaybackBeganBefore);

	void StopForSafetyCheck(IReadOnlyList<JsonObject> checks);

	void StopForOutOfAgentMessages();

	void NoteDriveRequestFailed();

	void NoteTurnBeforeTools(bool programSpoke, string? turnToken);

	Task<string> HandleToolAsync(string name, string argumentsJson);

	void VoiceSilentToolTurn(string name, string argumentsJson);

	void StopBecauseUserSpoke();
}
