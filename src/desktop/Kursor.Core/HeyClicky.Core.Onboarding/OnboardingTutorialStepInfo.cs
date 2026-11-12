namespace HeyClicky.Core.Onboarding;

public sealed record OnboardingTutorialStepInfo(OnboardingTutorialStep Step, bool AllowsPushToTalk, bool AllowsTextMode, bool CompletesOnTurn, bool AutoAdvancesAfterCompletion, double AutoAdvanceDelaySeconds, string? SuccessCursorLine, string Title, string? Body);
