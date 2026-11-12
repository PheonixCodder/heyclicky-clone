using System;

namespace HeyClicky.Core.Dictation;

public sealed class DictationControllerOptions
{
	public Func<string> UsageContext { get; set; } = () => "companion";

	public Func<bool> IsOnboardingTutorialRestricting { get; set; } = () => false;

	public Func<bool> OnboardingTutorialAllowsPress { get; set; } = () => true;

	public Func<bool> IsOnboardingTutorialDictationStepActive { get; set; } = () => false;

	public Func<string, bool> ConsumeOnboardingTutorialTranscript { get; set; } = (string _) => false;

	public Func<bool> CanStartRecording { get; set; } = () => true;

	public Func<bool> IsMicrophoneDenied { get; set; } = () => false;

	public Action<string, float> PlayCue { get; set; } = delegate
	{
	};

	public Action<string, float>? PlayMicrophoneStartCue { get; set; }
}
