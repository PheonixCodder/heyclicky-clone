using System;
using System.Linq;
using HeyClicky.Core.Settings;
using HeyClicky.Platform.Win32;

namespace HeyClicky.App.Services;

public sealed class CoreAppSettings : IAppSettings
{
	private readonly SettingsStore store;

	private bool paused;

	public SettingsStore Store => store;

	public string CursorColorHex
	{
		get
		{
			return store.Read((AppSettings s) => s.CursorColorHex);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.CursorColorHex = value;
			});
		}
	}

	public bool ScreenHighlightEnabled
	{
		get
		{
			return store.Read((AppSettings s) => s.ScreenHighlightEnabled);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.ScreenHighlightEnabled = value;
			});
		}
	}

	public bool LibraryModeEnabled
	{
		get
		{
			return store.Read((AppSettings s) => s.TypedTurnsRespondWithVoice);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.TypedTurnsRespondWithVoice = value;
			});
		}
	}

	public bool HasCompletedIntro
	{
		get
		{
			return store.Read((AppSettings s) => s.Onboarding.PreSignInIntroDismissed);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.PreSignInIntroDismissed = value;
			});
		}
	}

	public bool HasCompletedOnboarding
	{
		get
		{
			return store.Read((AppSettings s) => s.Onboarding.HasCompletedOnboarding);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.HasCompletedOnboarding = value;
			});
		}
	}

	public bool HasCompletedTutorial
	{
		get
		{
			return store.Read((AppSettings s) => s.Onboarding.TutorialCompletedAtEpochSeconds).HasValue;
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.TutorialCompletedAtEpochSeconds = (value ? new double?(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : ((double?)null));
			});
		}
	}

	public string? SquadPack
	{
		get
		{
			return store.Read((AppSettings s) => s.Onboarding.SquadPackId);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.SquadPackId = value;
			});
		}
	}

	public string? DiscoveryChannel
	{
		get
		{
			return store.Read((AppSettings s) => s.Onboarding.DiscoveryChannel);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.Onboarding.DiscoveryChannel = value;
			});
		}
	}

	public bool IsPaused
	{
		get
		{
			return paused;
		}
		set
		{
			paused = value;
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool CursorDocked
	{
		get
		{
			return store.Read((AppSettings s) => s.CursorDocked);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.CursorDocked = value;
			});
		}
	}

	public bool ShakeToDetachEnabled
	{
		get
		{
			return store.Read((AppSettings s) => s.ShakeToDetachEnabled);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.ShakeToDetachEnabled = value;
			});
		}
	}

	public string PushToTalkDisplay => store.Read((AppSettings s) => s.EffectiveHotkeys.PushToTalk.DisplayText);

	public HotkeyBinding PushToTalkBinding => store.Read((AppSettings s) => s.EffectiveHotkeys.PushToTalk);

	public HotkeyBinding DictationBinding => store.Read((AppSettings s) => s.EffectiveHotkeys.Dictation);

	public bool DictationAutoDetect
	{
		get
		{
			return store.Read((AppSettings s) => s.DictationLanguageCode) == "multi";
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.DictationLanguageCode = (value ? "multi" : s.DictationSpecificLanguageCode);
			});
		}
	}

	public string DictationSpecificLanguageCode
	{
		get
		{
			return store.Read((AppSettings s) => s.DictationSpecificLanguageCode);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.DictationSpecificLanguageCode = value;
				if (s.DictationLanguageCode != "multi")
				{
					s.DictationLanguageCode = value;
				}
			});
		}
	}

	public string DictationCustomWordsText
	{
		get
		{
			return string.Join("\n", store.Read((AppSettings s) => s.DictationCustomWords.ToList()));
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.DictationCustomWords = (from line in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
					select line.Trim() into line
					where line.Length > 0
					select line).ToList();
			});
		}
	}

	public string TextModeDisplay => store.Read((AppSettings s) => string.Join(" + ", s.EffectiveHotkeys.TextMode.KeyNames));

	public bool LaunchAtLogin
	{
		get
		{
			return store.Read((AppSettings s) => s.LaunchAtLogin);
		}
		set
		{
			store.Update(delegate(AppSettings s)
			{
				s.LaunchAtLogin = value;
			});
			try
			{
				HeyClicky.Platform.Win32.LaunchAtLogin.SetEnabled(value);
			}
			catch (Exception ex)
			{
				Log.Warn("settings", "launch at login: " + ex.Message);
			}
		}
	}

	public event EventHandler? Changed;

	public CoreAppSettings(SettingsStore store)
	{
		this.store = store;
		store.Changed += delegate
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		};
	}
}
