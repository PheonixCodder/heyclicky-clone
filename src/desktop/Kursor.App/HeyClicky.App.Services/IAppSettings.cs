using System;

namespace HeyClicky.App.Services;

public interface IAppSettings
{
	string CursorColorHex { get; set; }

	bool LaunchAtLogin { get; set; }

	bool ScreenHighlightEnabled { get; set; }

	bool LibraryModeEnabled { get; set; }

	bool IsPaused { get; set; }

	bool HasCompletedIntro { get; set; }

	bool HasCompletedOnboarding { get; set; }

	bool HasCompletedTutorial { get; set; }

	string? SquadPack { get; set; }

	string? DiscoveryChannel { get; set; }

	string PushToTalkDisplay { get; }

	HotkeyBinding PushToTalkBinding => HotkeyBinding.PushToTalkDefault;

	HotkeyBinding DictationBinding => HotkeyBinding.DictationDefault;

	bool DictationAutoDetect { get; set; }

	string DictationSpecificLanguageCode { get; set; }

	string DictationCustomWordsText { get; set; }

	string TextModeDisplay { get; }

	bool CursorDocked { get; set; }

	bool ShakeToDetachEnabled { get; set; }

	event EventHandler? Changed;
}
