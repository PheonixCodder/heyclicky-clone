using System.Threading.Tasks;

namespace HeyClicky.Agents.Routines;

internal interface IRoutineHost
{
	bool IsSignedIn { get; }

	bool ClickyExists(string slug);

	bool IsClickyBusy(string slug);

	bool IsClickyAwaitingExtraUsage(string slug);

	Task<string?> LaunchRoutineRunAsync(ClickyRoutine routine, int runNumber, string prompt, string launchContext);

	bool JobExists(string jobId);

	bool IsJobBusy(string jobId);

	bool IsJobAwaitingExtraUsage(string jobId);

	Task<string?> LaunchJobRoutineRunAsync(ClickyRoutine routine, int runNumber, string prompt, bool scheduled);

	string? CreateJobForMovedRoutine(ClickyRoutine routine);

	void RetireClickysNoteForMovedRoutineJob(string jobId, bool onlyWhenLeft);

	void BackfillWorkReportsIfNeeded()
	{
	}
}
