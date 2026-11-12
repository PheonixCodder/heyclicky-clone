using System;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Home;

public sealed record HomeMetrics(double Density)
{
	public double RailWidth => Math.Round(Lerp(220.0, 300.0, Density));

	public double RowAvatarSize => Math.Round(Lerp(32.0, 40.0, Density));

	public double PinnedAvatarCap => Math.Round(Lerp(56.0, 72.0, Density));

	public double HeaderReservedHeight => Math.Round(Lerp(60.0, 76.0, Density));

	public double HeaderIdentityAvatarSize => Math.Round(Lerp(34.0, 42.0, Density));

	public double SettingsSidebarWidth => Math.Round(Lerp(180.0, 228.0, Density));

	public double VoicePillHeight => Math.Round(Lerp(42.0, 52.0, Density));

	public double VoicePillLabelFontSize => 13.0 + Density;

	public double HeroGreetingFontSize => Math.Round(Lerp(18.0, 22.0, Density));

	public double HeroSubtitleFontSize => 12.0 + Density;

	public double MascotFaceScale => Lerp(0.62, 1.0, Density);

	public double ContentPadding => 12.0 + 8.0 * Density;

	public double ArtifactCardWidth => Math.Round(Lerp(124.0, 158.0, Density));

	public double ArtifactPreviewHeight => Math.Round(Lerp(88.0, 116.0, Density));

	public double SectionSpacing => 12.0 + 8.0 * Density;

	public double SuggestionCharacterSize => Math.Round(Lerp(110.0, 195.0, Density));

	public double SuggestionTitleFontSize => 14.0 + 4.0 * Density;

	public double SuggestionBodyFontSize => 12.0 + 1.5 * Density;

	public double SuggestionPortraitTopInset => 16.0 + 16.0 * Density;

	public double SuggestionHandWidth => 18.0 + 10.0 * Density;

	public const double SidebarLeadingInset = 14.0;

	public const double FocusRailLeadingInset = 18.0;

	public const double RailToDetailSpacing = 9.0;

	public const double DetailTrailingPadding = 16.0;

	private static double Lerp(double compact, double full, double t)
	{
		return compact + (full - compact) * t;
	}

	public static HomeMetrics For(double w, double h)
	{
		return new HomeMetrics(Math.Clamp(Math.Min((w - 820.0) / 430.0, (h - 560.0) / 240.0), 0.0, 1.0));
	}

	[CompilerGenerated]
	private HomeMetrics(HomeMetrics original)
	{
		Density = original.Density;
	}
}
