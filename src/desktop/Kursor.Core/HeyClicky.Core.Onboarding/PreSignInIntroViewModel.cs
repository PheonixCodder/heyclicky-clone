using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Onboarding;

public sealed class PreSignInIntroViewModel : ObservableObject
{
	private readonly SettingsStore settings;

	private readonly Func<string, string?, Task>? syncDiscoveryChannel;

	[ObservableProperty]
	[NotifyPropertyChangedFor("Title", new string[] { "Body", "CanGoBack", "AnalyticsName" })]
	private PreSignInIntroStep step;

	[ObservableProperty]
	private bool isSignedIn;

	[ObservableProperty]
	private string? userFirstName;

	[ObservableProperty]
	private string? selectedDiscoveryChannel;

	[ObservableProperty]
	private bool isOtherDiscoveryFieldVisible;

	[ObservableProperty]
	private string otherDiscoveryText = "";

	[ObservableProperty]
	private string? selectedCursorColorHex;

	private readonly Func<string?, Task<IReadOnlyList<string>>>? fetchRegionLanguages;

	public IReadOnlyList<DiscoveryChannelOption> DiscoveryChannels => OnboardingCatalog.DiscoveryChannels;

	public IReadOnlyList<CursorColorOption> CursorColors => OnboardingCatalog.CursorColors;

	public string AnalyticsName => Step switch
	{
		PreSignInIntroStep.WhatIsClicky => "what_is_clicky", 
		PreSignInIntroStep.WhereClickyLives => "where_clicky_lives", 
		PreSignInIntroStep.ClickyGetsLost => "clicky_gets_lost", 
		PreSignInIntroStep.WhereHeardAbout => "where_heard_about", 
		PreSignInIntroStep.OpenClicky => "open_clicky", 
		PreSignInIntroStep.Hatched => "hatched", 
		PreSignInIntroStep.SkillSelection => "skill_selection", 
		_ => "ready_to_start", 
	};

	public string Title => Step switch
	{
		PreSignInIntroStep.WhatIsClicky => "Welcome to HeyClicky.", 
		PreSignInIntroStep.WhereClickyLives => "I live in your notch now.", 
		PreSignInIntroStep.ClickyGetsLost => "Sometimes HeyClicky gets lost", 
		PreSignInIntroStep.WhereHeardAbout => "How’d you find this?", 
		PreSignInIntroStep.OpenClicky => "Pick a color", 
		PreSignInIntroStep.Hatched => string.IsNullOrWhiteSpace(UserFirstName) ? "Hi, friend." : ("Hi, " + UserFirstName + "."), 
		PreSignInIntroStep.SkillSelection => "What should HeyClicky be good at?", 
		_ => "You're all set!", 
	};

	public string? Body => Step switch
	{
		PreSignInIntroStep.WhatIsClicky => "A tiny AI buddy that lives on your PC.", 
		PreSignInIntroStep.WhereClickyLives => "Try it out! Hover the notch at the top of your screen and you'll see me in my new home.", 
		PreSignInIntroStep.WhereHeardAbout => "We're a tiny team, and knowing where you heard about us helps a ton.", 
		PreSignInIntroStep.OpenClicky => "Click one to bring me to life. Choose wisely.", 
		PreSignInIntroStep.Hatched => "I'm HeyClicky.", 
		_ => null, 
	};

	public bool CanGoBack => PreviousStep(Step).HasValue;

	public bool IsLanguageStepEnabled => settings.Read((AppSettings s) => s.OnboardingLanguageStepEnabled);

	public IReadOnlyList<string> VoiceLanguages => VoiceCatalog.PickedLanguages(settings.Read((AppSettings s) => s.VoiceLanguageCode));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public PreSignInIntroStep Step
	{
		get
		{
			return step;
		}
		set
		{
			if (!EqualityComparer<PreSignInIntroStep>.Default.Equals(step, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Step);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Title);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Body);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanGoBack);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AnalyticsName);
				step = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Step);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Title);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Body);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanGoBack);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AnalyticsName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSignedIn
	{
		get
		{
			return isSignedIn;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isSignedIn, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSignedIn);
				isSignedIn = value;
				OnIsSignedInChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSignedIn);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? UserFirstName
	{
		get
		{
			return userFirstName;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(userFirstName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UserFirstName);
				userFirstName = value;
				OnUserFirstNameChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UserFirstName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedDiscoveryChannel
	{
		get
		{
			return selectedDiscoveryChannel;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(selectedDiscoveryChannel, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedDiscoveryChannel);
				selectedDiscoveryChannel = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedDiscoveryChannel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsOtherDiscoveryFieldVisible
	{
		get
		{
			return isOtherDiscoveryFieldVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isOtherDiscoveryFieldVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsOtherDiscoveryFieldVisible);
				isOtherDiscoveryFieldVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsOtherDiscoveryFieldVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string OtherDiscoveryText
	{
		get
		{
			return otherDiscoveryText;
		}
		[MemberNotNull("otherDiscoveryText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(otherDiscoveryText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OtherDiscoveryText);
				otherDiscoveryText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OtherDiscoveryText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedCursorColorHex
	{
		get
		{
			return selectedCursorColorHex;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(selectedCursorColorHex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedCursorColorHex);
				selectedCursorColorHex = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedCursorColorHex);
			}
		}
	}

	public event EventHandler? HandOffToNotchRequested;

	public event EventHandler? Dismissed;

	public event EventHandler? SignInRequested;

	public PreSignInIntroViewModel(SettingsStore settings, Func<string, string?, Task>? syncDiscoveryChannel = null, Func<string?, Task<IReadOnlyList<string>>>? fetchRegionLanguages = null)
	{
		this.settings = settings;
		this.syncDiscoveryChannel = syncDiscoveryChannel;
		this.fetchRegionLanguages = fetchRegionLanguages;
		OnboardingProgress onboardingProgress = settings.Read((AppSettings s) => s.Onboarding);
		selectedDiscoveryChannel = onboardingProgress.DiscoveryChannel;
	}

	public static bool ShouldPresent(OnboardingProgress progress, bool isSignedIn)
	{
		if (!progress.PreSignInIntroDismissed)
		{
			if (isSignedIn)
			{
				return !progress.HasCompletedOnboarding;
			}
			return true;
		}
		return false;
	}

	public void ContinueFromWelcome()
	{
		if (Step == PreSignInIntroStep.WhatIsClicky)
		{
			if (IsSignedIn)
			{
				Step = PreSignInIntroStep.WhereHeardAbout;
			}
			else
			{
				this.SignInRequested?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public async Task PickDiscoveryChannelAsync(string channelId)
	{
		SelectedDiscoveryChannel = channelId;
		if (channelId == "other")
		{
			IsOtherDiscoveryFieldVisible = true;
			return;
		}
		IsOtherDiscoveryFieldVisible = false;
		await SaveDiscoveryChannelAsync(channelId, null).ConfigureAwait(continueOnCapturedContext: true);
		await Task.Delay(140).ConfigureAwait(continueOnCapturedContext: true);
		if (Step == PreSignInIntroStep.WhereHeardAbout)
		{
			ApplyDefaultOnboardingLook();
			Step = PreSignInIntroStep.OpenClicky;
		}
	}

	public async Task CommitOtherDiscoveryChannelAsync()
	{
		string text = OtherDiscoveryText.Trim();
		if (text.Length > 280)
		{
			text = text.Substring(0, 280);
		}
		await SaveDiscoveryChannelAsync("other", (text.Length == 0) ? null : text).ConfigureAwait(continueOnCapturedContext: true);
		if (Step == PreSignInIntroStep.WhereHeardAbout)
		{
			ApplyDefaultOnboardingLook();
			Step = PreSignInIntroStep.OpenClicky;
		}
	}

	private async Task SaveDiscoveryChannelAsync(string channelId, string? other)
	{
		ClickyAnalytics.TrackDiscoveryChannelSelected(channelId, other);
		settings.Update(delegate(AppSettings s)
		{
			s.Onboarding.DiscoveryChannel = channelId;
			s.Onboarding.DiscoveryChannelOther = other;
			s.Onboarding.DiscoveryChannelSynced = false;
		});
		if (!IsSignedIn || syncDiscoveryChannel == null)
		{
			return;
		}
		try
		{
			await syncDiscoveryChannel(channelId, other).ConfigureAwait(continueOnCapturedContext: true);
			settings.Update(delegate(AppSettings s)
			{
				s.Onboarding.DiscoveryChannelSynced = true;
			});
		}
		catch (Exception ex)
		{
			Log.Warn("Onboarding", "discovery channel not synced yet: " + ex.Message);
		}
	}

	public void ApplyDefaultOnboardingLook()
	{
		SquadPack pack = OnboardingCatalog.DefaultSquadPack;
		settings.Update(delegate(AppSettings s)
		{
			s.Onboarding.SquadPackId = pack.Id;
			s.SquadMemberId = pack.Id;
			s.VoiceIdentifier = pack.VoiceIdentifier;
		});
	}

	public async Task<IReadOnlyList<string>> RegionLanguagesAsync(string? deviceRegion)
	{
		if (fetchRegionLanguages == null)
		{
			return Array.Empty<string>();
		}
		try
		{
			Task<IReadOnlyList<string>> fetch = fetchRegionLanguages(deviceRegion);
			if (await Task.WhenAny(fetch, Task.Delay(4000)).ConfigureAwait(continueOnCapturedContext: true) != fetch)
			{
				return Array.Empty<string>();
			}
			return (await fetch.ConfigureAwait(continueOnCapturedContext: true)).Where((string c) => (object)VoiceCatalog.Language(c) != null).ToList();
		}
		catch (Exception ex)
		{
			Log.Warn("Onboarding", "language hints: " + ex.Message);
			return Array.Empty<string>();
		}
	}

	public void PickVoiceLanguages(IReadOnlyList<string> codes)
	{
		string stored = VoiceCatalog.StoredLanguages(codes);
		bool needsNativeAccent = VoiceCatalog.PickedLanguages(stored).Any((string c) => !VoiceCatalog.IsEnglish(c));
		settings.Update(delegate(AppSettings s)
		{
			s.VoiceLanguageCode = stored;
			s.VoiceIdentifier = (needsNativeAccent ? "gemini:Puck" : OnboardingCatalog.DefaultSquadPack.VoiceIdentifier);
		});
	}

	public async Task PickCursorColorAsync(string hex)
	{
		SelectedCursorColorHex = hex;
		settings.Update(delegate(AppSettings s)
		{
			s.CursorColorHex = hex;
		});
		await Task.Delay(120).ConfigureAwait(continueOnCapturedContext: true);
		if (Step == PreSignInIntroStep.OpenClicky)
		{
			Step = PreSignInIntroStep.Hatched;
		}
	}

	public void GiveClickyAHome()
	{
		if (Step == PreSignInIntroStep.Hatched)
		{
			this.HandOffToNotchRequested?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Back()
	{
		PreSignInIntroStep? preSignInIntroStep = PreviousStep(Step);
		if (preSignInIntroStep.HasValue)
		{
			PreSignInIntroStep valueOrDefault = preSignInIntroStep.GetValueOrDefault();
			Step = valueOrDefault;
		}
	}

	private static PreSignInIntroStep? PreviousStep(PreSignInIntroStep step)
	{
		return step switch
		{
			PreSignInIntroStep.WhereHeardAbout => PreSignInIntroStep.WhatIsClicky, 
			PreSignInIntroStep.OpenClicky => PreSignInIntroStep.WhereHeardAbout, 
			PreSignInIntroStep.SkillSelection => PreSignInIntroStep.OpenClicky, 
			PreSignInIntroStep.ReadyToStart => PreSignInIntroStep.OpenClicky, 
			_ => null, 
		};
	}

	public void Dismiss()
	{
		if (!settings.Read((AppSettings s) => s.Onboarding.PreSignInIntroDismissed))
		{
			ClickyAnalytics.TrackPreSignInIntroDismissed();
		}
		settings.Update(delegate(AppSettings s)
		{
			s.Onboarding.PreSignInIntroDismissed = true;
		});
		this.Dismissed?.Invoke(this, EventArgs.Empty);
	}

	public void Reset()
	{
		Step = PreSignInIntroStep.WhatIsClicky;
		IsOtherDiscoveryFieldVisible = false;
		OtherDiscoveryText = "";
	}

	public static async Task FlushPendingDiscoveryChannelAsync(SettingsStore settings, Func<string, string?, Task> syncDiscoveryChannel)
	{
		(string, string, bool) tuple = settings.Read((AppSettings s) => (DiscoveryChannel: s.Onboarding.DiscoveryChannel, DiscoveryChannelOther: s.Onboarding.DiscoveryChannelOther, DiscoveryChannelSynced: s.Onboarding.DiscoveryChannelSynced));
		if (tuple.Item1 == null || tuple.Item3)
		{
			return;
		}
		try
		{
			await syncDiscoveryChannel(tuple.Item1, tuple.Item2).ConfigureAwait(continueOnCapturedContext: false);
			settings.Update(delegate(AppSettings s)
			{
				s.Onboarding.DiscoveryChannelSynced = true;
			});
		}
		catch (Exception ex)
		{
			Log.Warn("Onboarding", "discovery channel sync failed: " + ex.Message);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnIsSignedInChanged(bool value)
	{
		if (value && Step == PreSignInIntroStep.WhatIsClicky)
		{
			Step = PreSignInIntroStep.WhereHeardAbout;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnUserFirstNameChanged(string? value)
	{
		OnPropertyChanged("Title");
	}
}
