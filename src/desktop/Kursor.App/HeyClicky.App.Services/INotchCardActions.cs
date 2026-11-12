using System.Collections.Generic;

namespace HeyClicky.App.Services;

public interface INotchCardActions
{
	void AnswerIntegrationSuggestion(string toolkit, IntegrationAnswer answer);

	void RemoveDictionaryWord(string word);

	void RestoreOutputVolume();

	void ResumeTutorial();

	void SendAgentFollowUp(string taskId, string text, IReadOnlyList<string> files);
}
