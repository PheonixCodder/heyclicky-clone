using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using HeyClicky.Core.Onboarding;

namespace HeyClicky.Core.Settings;

public sealed class AppSettings
{
	public const string DefaultCursorColorHex = "#3380FF";

	public string CursorColorHex { get; set; } = "#3380FF";

	public string? SquadMemberId { get; set; }

	public string VoiceIdentifier { get; set; } = "gemini:Puck";

	public double SpeechSpeed { get; set; } = 1.0;

	public string? VoiceLanguageCode { get; set; }

	public bool ScreenHighlightEnabled { get; set; } = true;

	public bool CursorDocked { get; set; }

	public bool ShakeToDetachEnabled { get; set; }

	public bool LaunchAtLogin { get; set; } = true;

	public bool ShowInTaskbar { get; set; } = true;

	public bool TypedTurnsRespondWithVoice { get; set; } = true;

	public string DictationLanguageCode { get; set; } = "multi";

	public string DictationSpecificLanguageCode { get; set; } = "en";

	public double? DictationLastCompletedAtEpochSeconds { get; set; }

	public List<string> DictationCustomWords { get; set; }

	public bool DictationClaudeSeeded { get; set; }

	public string? DictationAccountNameSeeded { get; set; }

	public string? DiscoveryChannelPendingSync { get; set; }

	public string? MicrophoneDeviceName { get; set; }

	public bool AgentAnnouncementsVoiceEnabled { get; set; }

	public int DocumentAccessNoticeShowCount { get; set; }

	public bool DocumentAccessNoticeSuppressed { get; set; }

	public OnboardingProgress Onboarding { get; set; }

	public HotkeyBindings? Hotkeys { get; set; }

	public string? MicrophoneDeviceId { get; set; }

	public string? OutputDeviceId { get; set; }

	public bool DrawingSoundsEnabled { get; set; }

	public bool DrawingVoiceCaptions { get; set; }

	public bool? KeepDrawingsInChat { get; set; }

	[JsonIgnore]
	public bool KeepDrawingsInChatEffective => false;

	public bool DrawingPenSoundsEnabled { get; set; }

	public bool DrawingJavaScriptJit { get; set; }

	public bool? DrawingSubtitles { get; set; }

	[JsonIgnore]
	public bool DrawingSubtitlesEffective => false;

	public bool SaveDrawings { get; set; }

	[JsonIgnore]
	public bool SaveDrawingsEffective => false;

	public bool ShowAgentLabelsInActivityStrip { get; set; }

	public bool ShowInScreenRecordings { get; set; }

	public string? AgentFolderPath { get; set; }

	public string? ObsidianVaultPath { get; set; }

	public bool AgentAutoApproveExtraUsage { get; set; }

	public bool AgentAlwaysApproveFileUploads { get; set; }

	public bool AgentCursorTextAnnouncementsEnabled { get; set; }

	public string? OneBrainModel { get; set; }

	public bool OneBrainReadsApps { get; set; }

	public bool OneBrainKeepsScreensInHistory { get; set; }

	public bool ShowOneBrainStats { get; set; }

	public bool? MorningPaperEnabled { get; set; }

	public bool StreamMode { get; set; }

	public Dictionary<string, string>? DeveloperChatModels { get; set; }

	public bool HasShownPaywallIntroVideo { get; set; }

	[JsonIgnore]
	public HotkeyBindings EffectiveHotkeys => Hotkeys ?? HotkeyBindings.Defaults;

	public bool ProactiveAgentSuggestionsEnabled { get; set; }

	public bool UpcomingMeetingsEnabled { get; set; }

	public bool OnboardingLanguageStepEnabled { get; set; }

	public bool RemoteTasksEnabled { get; set; }

	public bool ProactiveDebugChordEnabled { get; set; }

	public int ProactiveDebugCadence { get; set; }

	public bool? LiveNotesEnabled { get; set; }

	public bool? LiveNotesStreamPatches { get; set; }

	public AppSettings()
	{
		int num = 2;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "HeyClicky";
		span[num2 + 1] = "Claude";
		DictationCustomWords = list;
		AgentAnnouncementsVoiceEnabled = true;
		Onboarding = new OnboardingProgress();
		DrawingSoundsEnabled = true;
		DrawingJavaScriptJit = true;
		ShowInScreenRecordings = true;
		AgentCursorTextAnnouncementsEnabled = true;
		OneBrainKeepsScreensInHistory = true;
		ProactiveAgentSuggestionsEnabled = true;
		ProactiveDebugCadence = 1;
	}
}
