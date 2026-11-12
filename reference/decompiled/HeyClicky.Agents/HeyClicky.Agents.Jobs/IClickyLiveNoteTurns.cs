namespace HeyClicky.Agents.Jobs;

public interface IClickyLiveNoteTurns
{
	string? NotePath(string clickySlug, string workspacePath);

	string PromptSection(string notePath, string? userMessage, string? routineRun, ClickyLiveNoteWriter writer);

	void FileActivity(string notePath, string path, string? streamedKind, string? streamedDiff);

	void TurnEnded(string notePath);

	void Forget(string notePath);
}
