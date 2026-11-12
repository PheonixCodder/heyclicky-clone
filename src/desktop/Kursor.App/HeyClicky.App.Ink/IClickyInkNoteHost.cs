using System;
using System.Collections.Generic;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

public interface IClickyInkNoteHost
{
	ClickyInkClickyLive ClickyLiveStatus(string slug);

	bool IsClickyUnread(string slug);

	bool ClickyExists(string slug);

	ClickyInkJob? Job(string jobId);

	IReadOnlyList<ClickyInkRoutineControl> RoutineControls(string slug, DateTimeOffset now);

	void SetRoutinePaused(string routineId, bool isPaused, string source);

	void SetRoutineInterval(string routineId, int minutes, string source);

	bool RunRoutineNow(string routineId);

	void SendToClicky(string slug, string message, string launchContext);

	void SendToThread(string threadId, string message, string launchContext);

	void OpenJobArtifact(string target, string jobId);

	string? BoundThreadId(string slug);

	bool OpenInViewerIfSupported(string target, string? slug, string? threadId);
}
