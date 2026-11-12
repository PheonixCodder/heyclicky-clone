using System;
using System.Collections.Generic;
using System.IO;

namespace HeyClicky.Core.Onboarding;

public static class IntroVideoSources
{
	public const string HostedOnboardingVideoUrl = "https://stream.mux.com/7bOYF1PGo02RKwcQW00L00Aa01iRKElKXoch4xhtsJzv8QU.m3u8";

	public const string BundledOnboardingVideoFileName = "onboarding-intro-v2-windows.mp4";

	public const string PreferHostedEnvironmentVariable = "HEYCLICKY_PREFER_HOSTED_INTRO_VIDEOS";

	public const string ForceHostedFailureEnvironmentVariable = "HEYCLICKY_FORCE_HOSTED_INTRO_VIDEO_FAILURE";

	public static bool PrefersHosted => IsTruthy(Environment.GetEnvironmentVariable("HEYCLICKY_PREFER_HOSTED_INTRO_VIDEOS"));

	public static bool ForcesHostedFailure => IsTruthy(Environment.GetEnvironmentVariable("HEYCLICKY_FORCE_HOSTED_INTRO_VIDEO_FAILURE"));

	public static IReadOnlyList<IntroVideoSourceKind> PlaybackOrder(bool preferHosted)
	{
		if (!preferHosted)
		{
			return new global::_003C_003Ez__ReadOnlyArray<IntroVideoSourceKind>(new IntroVideoSourceKind[2]
			{
				IntroVideoSourceKind.Bundled,
				IntroVideoSourceKind.Hosted
			});
		}
		return new global::_003C_003Ez__ReadOnlyArray<IntroVideoSourceKind>(new IntroVideoSourceKind[2]
		{
			IntroVideoSourceKind.Hosted,
			IntroVideoSourceKind.Bundled
		});
	}

	public static IntroVideoSourceKind Alternate(IntroVideoSourceKind kind)
	{
		if (kind != IntroVideoSourceKind.Hosted)
		{
			return IntroVideoSourceKind.Hosted;
		}
		return IntroVideoSourceKind.Bundled;
	}

	public static string AnalyticsName(IntroVideoSourceKind kind)
	{
		if (kind != IntroVideoSourceKind.Hosted)
		{
			return "bundled";
		}
		return "hosted";
	}

	public static bool IsTruthy(string? value)
	{
		switch (value?.Trim().ToLowerInvariant())
		{
		case "1":
		case "true":
		case "yes":
		case "y":
		case "on":
			return true;
		default:
			return false;
		}
	}

	public static string Redacted(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return "";
		}
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
		{
			return url;
		}
		if (result.IsFile)
		{
			return "Assets/Video/" + Path.GetFileName(result.LocalPath);
		}
		return result.GetLeftPart(UriPartial.Path);
	}
}
