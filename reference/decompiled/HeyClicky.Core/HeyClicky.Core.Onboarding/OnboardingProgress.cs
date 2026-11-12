namespace HeyClicky.Core.Onboarding;

public sealed class OnboardingProgress
{
	public bool PreSignInIntroDismissed { get; set; }

	public bool HasAcknowledgedPermissionIntro { get; set; }

	public bool HasCompletedOnboarding { get; set; }

	public double? TutorialCompletedAtEpochSeconds { get; set; }

	public string? DiscoveryChannel { get; set; }

	public string? DiscoveryChannelOther { get; set; }

	public bool DiscoveryChannelSynced { get; set; }

	public string? SquadPackId { get; set; }
}
