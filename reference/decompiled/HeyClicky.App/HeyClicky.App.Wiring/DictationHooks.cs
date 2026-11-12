using System;

namespace HeyClicky.App.Wiring;

public static class DictationHooks
{
	public static volatile Action<string>? RemoveLearnedWord;

	public static volatile Func<bool>? TutorialAllowsPress;

	public static volatile Func<string, bool>? ConsumeTutorialTranscript;
}
