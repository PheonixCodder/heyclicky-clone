using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Onboarding;

public static class OnboardingCatalog
{
	public static readonly IReadOnlyList<DiscoveryChannelOption> DiscoveryChannels = new global::_003C_003Ez__ReadOnlyArray<DiscoveryChannelOption>(new DiscoveryChannelOption[13]
	{
		new DiscoveryChannelOption("instagram_ad", "Instagram Ad"),
		new DiscoveryChannelOption("instagram", "Instagram"),
		new DiscoveryChannelOption("youtube_ad", "YouTube Ad"),
		new DiscoveryChannelOption("youtube", "YouTube"),
		new DiscoveryChannelOption("tiktok", "TikTok"),
		new DiscoveryChannelOption("x", "X/Twitter"),
		new DiscoveryChannelOption("linkedin", "LinkedIn"),
		new DiscoveryChannelOption("reddit", "Reddit"),
		new DiscoveryChannelOption("chatgpt", "ChatGPT"),
		new DiscoveryChannelOption("claude", "Claude"),
		new DiscoveryChannelOption("google", "Google"),
		new DiscoveryChannelOption("friend", "A friend"),
		new DiscoveryChannelOption("other", "Other")
	});

	public static readonly IReadOnlyList<SquadPack> AllSquadPacks = new global::_003C_003Ez__ReadOnlyArray<SquadPack>(new SquadPack[6]
	{
		new SquadPack("puffPals", "Puff Pals", IsCloud: false),
		new SquadPack("kiCrew", "Ki Crew", IsCloud: false),
		new SquadPack("arcadeAllStars", "Arcade All-Stars", IsCloud: false),
		new SquadPack("cloudClub", "Cloud Club", IsCloud: true),
		new SquadPack("classicClouds", "Dreamy Cloud", IsCloud: true),
		new SquadPack("storybookSpirits", "Storybook Spirits", IsCloud: false)
	});

	public static readonly IReadOnlyList<CursorColorOption> CursorColors = new global::_003C_003Ez__ReadOnlyArray<CursorColorOption>(new CursorColorOption[4]
	{
		new CursorColorOption("red", "#FF4C4C"),
		new CursorColorOption("blue", "#3380FF"),
		new CursorColorOption("yellow", "#D4A017"),
		new CursorColorOption("green", "#26C281")
	});

	public static readonly IReadOnlyList<(string Id, string Gender)> Voices = new global::_003C_003Ez__ReadOnlyArray<(string, string)>(new(string, string)[10]
	{
		("cedar", "male"),
		("marin", "female"),
		("alloy", "neutral"),
		("ash", "male"),
		("ballad", "male"),
		("coral", "female"),
		("echo", "male"),
		("sage", "female"),
		("shimmer", "female"),
		("verse", "male")
	});

	public static SquadPack DefaultSquadPack => AllSquadPacks[4];

	public static string VoiceGender(string voiceId)
	{
		return VoiceCatalog.Expressive(voiceId)?.Gender ?? Voices.FirstOrDefault<(string, string)>(((string Id, string Gender) v) => v.Id == voiceId).Item2 ?? "neutral";
	}
}
