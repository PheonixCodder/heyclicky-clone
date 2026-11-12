using System;

namespace HeyClicky.App.Browser;

public interface IChromeClickyDrive
{
	bool IsAvailable { get; }

	bool IsDrivingNow { get; }

	bool DriveForApp(string goal, string? targetProcessName, Action<ClickyDriveEnding> ended);

	bool HandleEscape();

	void SayVerbatim(string line);
}
