using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeyClicky;

public interface ICompanionSession
{
	VoiceState State { get; }

	string ResponseText { get; }

	event EventHandler? StateChanged;

	event EventHandler<string>? UserTranscriptUpdated;

	event EventHandler<string>? ResponseTextUpdated;

	event EventHandler<CompanionError>? Failed;

	Task BeginPushToTalkAsync();

	Task EndPushToTalkAsync();

	Task SubmitTextAsync(string text);

	Task AskFromDrawingAsync(string text, string trigger);

	void Cancel();

	Task SubmitTextAsync(string text, IReadOnlyList<string> attachedFilePaths)
	{
		return SubmitTextAsync(text);
	}
}
