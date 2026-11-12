using System;

namespace HeyClicky.Core.Dictation;

public sealed class DictationControllerOptions
{
	public Func<string> UsageContext { get; init; } = () => "companion";

	public Func<bool> IsOnboardingTutorialRestricting { get; init; } = () => false;

	public Func<bool> OnboardingTutorialAllowsPress { get; init; } = () => true;

	public Func<bool> IsOnboardingTutorialDictationStepActive { get; init; } = () => false;

	public Func<string, bool> ConsumeOnboardingTutorialTranscript { get; init; } = (string _) => false;

	public Func<bool> CanStartRecording { get; init; } = () => true;

	public Func<bool> IsMicrophoneDenied { get; init; } = () => false;

	public Action<string, float> PlayCue { get; init; } = delegate
	{
	};

	public Action<string, float>? PlayMicrophoneStartCue { get; init; }
}
