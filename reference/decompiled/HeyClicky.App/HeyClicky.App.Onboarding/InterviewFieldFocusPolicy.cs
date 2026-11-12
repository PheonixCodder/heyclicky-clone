namespace HeyClicky.App.Onboarding;

public static class InterviewFieldFocusPolicy
{
	public static bool Allows(bool fromPointerOrKeyboard, bool requestedByStage)
	{
		return fromPointerOrKeyboard || requestedByStage;
	}
}
