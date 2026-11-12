using System;
using HeyClicky.App.Browser;
using HeyClicky.Core.Drive;
using HeyClicky.Core.Voice;

namespace HeyClicky.App.Drive;

internal sealed class ChromeSetupDrive : IChromeClickyDrive
{
	private readonly ClickyTakeOverController drive;

	private readonly CompanionSession companion;

	public bool IsAvailable => drive.IsAvailable();

	public bool IsDrivingNow => drive.IsDriving;

	public ChromeSetupDrive(ClickyTakeOverController drive, CompanionSession companion)
	{
		this.drive = drive;
		this.companion = companion;
	}

	public bool DriveForApp(string goal, string? targetProcessName, Action<ClickyDriveEnding> ended)
	{
		return drive.DriveForApp(goal, targetProcessName, delegate(AgentDriveOutcome outcome)
		{
			Action<ClickyDriveEnding> action = ended;
			action(outcome.Ending switch
			{
				AgentDriveEnding.Finished => ClickyDriveEnding.Finished, 
				AgentDriveEnding.UserTookOver => ClickyDriveEnding.UserTookOver, 
				AgentDriveEnding.SafetyStop => ClickyDriveEnding.SafetyStop, 
				AgentDriveEnding.CouldNotSee => ClickyDriveEnding.CouldNotSee, 
				AgentDriveEnding.OutOfAgentMessages => ClickyDriveEnding.OutOfAgentMessages, 
				_ => ClickyDriveEnding.Failed, 
			});
		});
	}

	public bool HandleEscape()
	{
		return drive.HandleEscape();
	}

	public void SayVerbatim(string line)
	{
		companion.SpeakVerbatimLineAsync(line);
	}
}
