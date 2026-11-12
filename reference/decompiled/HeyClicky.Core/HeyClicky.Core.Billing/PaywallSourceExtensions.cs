namespace HeyClicky.Core.Billing;

public static class PaywallSourceExtensions
{
	public static bool OffersFreeOption(this PaywallSource source)
	{
		if ((uint)(source - 1) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static string WireName(this PaywallSource source)
	{
		return source switch
		{
			PaywallSource.OnboardingCompleted => "onboarding_completed", 
			PaywallSource.DeveloperOnboardingPreview => "developer_onboarding_preview", 
			PaywallSource.NotchUpgradeButton => "notch_upgrade_button", 
			PaywallSource.SettingsUpgradeButton => "settings_upgrade_button", 
			PaywallSource.NotchPeekUpgradeButton => "notch_peek_upgrade_button", 
			PaywallSource.HomeUpgradeButton => "home_upgrade_button", 
			PaywallSource.ReferralPage => "referral_page", 
			_ => "limit_hit", 
		};
	}
}
