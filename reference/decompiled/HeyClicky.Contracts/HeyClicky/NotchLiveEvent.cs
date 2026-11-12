using System;
using System.Collections.Generic;

namespace HeyClicky;

public abstract record NotchLiveEvent
{
	public sealed record ActivityListening : NotchLiveEvent;

	public sealed record ActivityThinking : NotchLiveEvent;

	public sealed record ActivitySpeaking : NotchLiveEvent;

	public sealed record ActivityCustomStatus(string Label) : NotchLiveEvent();

	public sealed record Dictation(string Label, bool IsRecording) : NotchLiveEvent();

	public sealed record AgentMultiActivity(string LongestLabel, int Count) : NotchLiveEvent();

	public sealed record AgentTask(string TaskId, string Title, string Status) : NotchLiveEvent();

	public sealed record IntegrationSuggestion(string Toolkit, string Title) : NotchLiveEvent();

	public sealed record IntegrationConnected(string Toolkit, string? DisplayName = null, string? LogoUrl = null) : NotchLiveEvent();

	public sealed record MicrophoneTrouble(string Message, string? Subtitle = null, bool ShowsMicrophoneSettingsAction = true) : NotchLiveEvent();

	public sealed record VoiceReplyMutedCopiedToClipboard(string Kind = "zero_volume") : NotchLiveEvent();

	public sealed record DictationCopiedToClipboard : NotchLiveEvent;

	public sealed record AppUpdated(string Version) : NotchLiveEvent();

	public sealed record MeetingCountdown(string Title, DateTimeOffset StartsAt, Uri? JoinUrl, string? ActionKind = null) : NotchLiveEvent();

	public sealed record ClickyUpdate(string RailKey, string Kind) : NotchLiveEvent();

	public sealed record OnboardingTutorialInvite : NotchLiveEvent;

	public sealed record OnboardingTutorialResume : NotchLiveEvent;

	public sealed record IntroHatchPeek : NotchLiveEvent;

	public sealed record MorningSuggestions(int Count) : NotchLiveEvent();

	public sealed record RestingMark : NotchLiveEvent;

	public sealed record AgentPeek(double ListContentHeight = 0.0) : NotchLiveEvent();

	public sealed record HandoffSent(string TargetDisplayName) : NotchLiveEvent();

	public sealed record HandoffQueued(int Count) : NotchLiveEvent();

	public sealed record SidecarLearningSuggestion(string AppDisplayName, string? AppIconPath = null) : NotchLiveEvent();

	public sealed record SpotifyVoiceTip : NotchLiveEvent;

	public sealed record DictionaryWordLearned(string Word) : NotchLiveEvent();

	public sealed record DocumentFolderAccessDenied(string Subtitle, bool ShowsDontShowAgain, string? Path = null) : NotchLiveEvent();

	public sealed record IntroSkillPing(int WiggleToken = 0, bool LastToggleAdded = true, double ScaleBias = 0.0, string? GlyphColorHex = null) : NotchLiveEvent();

	public sealed record SkillPinned(int Count, IReadOnlyList<string>? Names = null) : NotchLiveEvent();
}
