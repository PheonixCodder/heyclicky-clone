using System.Collections.Generic;

namespace HeyClicky.App.Browser;

public interface IChromeSetupJobs
{
	string? JobIdForThread(string threadId);

	bool IsHeldForChrome(string jobId);

	IReadOnlyList<string> HeldJobIdsNewestFirst();

	void SetWaitsForChrome(string jobId, bool waits);

	void SetSkipsChrome(string jobId);

	bool SkipsChrome(string jobId);

	void PresentNeedsYou(string jobId);

	void StartFromSavedPrompt(string jobId, bool withoutChrome);

	string? CreatePreviewJob(string title, string prompt);

	void Dismiss(string jobId);
}
