using System;
using System.Threading.Tasks;
using HeyClicky.App.Shell;
using Windows.UI;

namespace HeyClicky.App.Drive;

public sealed class OnboardingDriveContext
{
	public required Displays.Monitor Display { get; init; }

	public required Color Accent { get; init; }

	public required Func<string, Task> SpeakLine { get; init; }

	public required Func<int> AudioPlaybackBeganCount { get; init; }

	public required Func<bool> IsResponding { get; init; }

	public string? FirstName { get; init; }

	public required Action StepCompleted { get; init; }

	public required Action<string, double> PlayChime { get; init; }

	public Action<string, float>? PlayDrawingSound { get; init; }
}
