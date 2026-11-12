using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Onboarding;

public sealed class NotchOnboardingCardViewModel : ObservableObject
{
	private static readonly PermissionKind[] PermissionOrder = new PermissionKind[2]
	{
		PermissionKind.Microphone,
		PermissionKind.ScreenCapture
	};

	private readonly SettingsStore settings;

	private readonly IPermissionService permissions;

	[ObservableProperty]
	private bool isSignedIn;

	[ObservableProperty]
	private bool isUpgradingUser;

	[ObservableProperty]
	private NotchOnboardingCardKind kind;

	[ObservableProperty]
	private PermissionKind? activePermission;

	[ObservableProperty]
	private PermissionStatus activePermissionStatus;

	[ObservableProperty]
	private int progressIndex;

	[ObservableProperty]
	private string title = "";

	[ObservableProperty]
	private string? subtitle;

	[ObservableProperty]
	private string? primaryActionLabel;

	[ObservableProperty]
	private bool isPrimaryActionEnabled = true;

	[ObservableProperty]
	private bool isIntroHomeWelcomeActive;

	private readonly object funnelGate = new object();

	private readonly Dictionary<PermissionKind, PermissionStatus> lastPermissionStatus = new Dictionary<PermissionKind, PermissionStatus>();

	private bool permissionFlowShownTracked;

	private PermissionKind? lastTrackedPermissionStep;

	private bool? hadAllPermissions;

	private PermissionKind? celebratingPermission;

	private CancellationTokenSource? celebrationCancellation;

	public TimeSpan GrantedSuccessHold { get; set; } = TimeSpan.FromSeconds(1.2);

	public PermissionKind? CelebratingPermission
	{
		get
		{
			lock (funnelGate)
			{
				return celebratingPermission;
			}
		}
	}

	public int TotalPermissionSteps => PermissionOrder.Length;

	public bool NeedsPin => Kind != NotchOnboardingCardKind.Ready;

	public bool CanAdvancePastPermissionsStep => PermissionOrder.All((PermissionKind p) => permissions.GetStatus(p) == PermissionStatus.Granted);

	public bool WelcomeMeetsClicky
	{
		get
		{
			if (!IsUpgradingUser)
			{
				return CanAdvancePastPermissionsStep;
			}
			return false;
		}
	}

	public bool WelcomeOpensMicrophoneSettings
	{
		get
		{
			bool flag = !CanAdvancePastPermissionsStep;
			if (flag)
			{
				PermissionStatus status = permissions.GetStatus(PermissionKind.Microphone);
				bool flag2 = ((status == PermissionStatus.WaitingInSettings || status == PermissionStatus.Denied) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return permissions.GetStatus(PermissionKind.ScreenCapture) == PermissionStatus.Granted;
			}
			return false;
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
	public bool IsUpgradingUser
	{
		get
		{
			return isUpgradingUser;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isUpgradingUser, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsUpgradingUser);
				isUpgradingUser = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsUpgradingUser);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public NotchOnboardingCardKind Kind
	{
		get
		{
			return kind;
		}
		set
		{
			if (!EqualityComparer<NotchOnboardingCardKind>.Default.Equals(kind, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Kind);
				kind = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Kind);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public PermissionKind? ActivePermission
	{
		get
		{
			return activePermission;
		}
		set
		{
			if (!EqualityComparer<PermissionKind?>.Default.Equals(activePermission, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActivePermission);
				activePermission = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActivePermission);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public PermissionStatus ActivePermissionStatus
	{
		get
		{
			return activePermissionStatus;
		}
		set
		{
			if (!EqualityComparer<PermissionStatus>.Default.Equals(activePermissionStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActivePermissionStatus);
				activePermissionStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActivePermissionStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ProgressIndex
	{
		get
		{
			return progressIndex;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(progressIndex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProgressIndex);
				progressIndex = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProgressIndex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Title
	{
		get
		{
			return title;
		}
		[MemberNotNull("title")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(title, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Title);
				title = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Title);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? Subtitle
	{
		get
		{
			return subtitle;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(subtitle, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Subtitle);
				subtitle = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Subtitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? PrimaryActionLabel
	{
		get
		{
			return primaryActionLabel;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(primaryActionLabel, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PrimaryActionLabel);
				primaryActionLabel = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PrimaryActionLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsPrimaryActionEnabled
	{
		get
		{
			return isPrimaryActionEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isPrimaryActionEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsPrimaryActionEnabled);
				isPrimaryActionEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsPrimaryActionEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsIntroHomeWelcomeActive
	{
		get
		{
			return isIntroHomeWelcomeActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isIntroHomeWelcomeActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsIntroHomeWelcomeActive);
				isIntroHomeWelcomeActive = value;
				OnIsIntroHomeWelcomeActiveChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsIntroHomeWelcomeActive);
			}
		}
	}

	public event EventHandler? OnboardingTriggered;

	public event EventHandler? SignInRequested;

	public event EventHandler? IntroWelcomeFinished;

	public NotchOnboardingCardViewModel(SettingsStore settings, IPermissionService permissions)
	{
		this.settings = settings;
		this.permissions = permissions;
		PermissionKind[] permissionOrder = PermissionOrder;
		foreach (PermissionKind key in permissionOrder)
		{
			lastPermissionStatus[key] = permissions.GetStatus(key);
		}
		permissions.Changed += delegate
		{
			TrackPermissionGrants();
			Refresh();
		};
		settings.Changed += delegate
		{
			Refresh();
		};
		Refresh();
	}

	private void TrackPermissionGrants()
	{
		PermissionKind[] permissionOrder = PermissionOrder;
		foreach (PermissionKind permissionKind in permissionOrder)
		{
			PermissionStatus status = permissions.GetStatus(permissionKind);
			bool flag;
			lock (funnelGate)
			{
				PermissionStatus valueOrDefault = lastPermissionStatus.GetValueOrDefault(permissionKind);
				lastPermissionStatus[permissionKind] = status;
				flag = valueOrDefault != PermissionStatus.Granted && status == PermissionStatus.Granted;
			}
			if (flag)
			{
				ClickyAnalytics.TrackPermissionGranted(permissionKind);
				if (Kind == NotchOnboardingCardKind.Permission && ActivePermission == permissionKind)
				{
					BeginGrantedCelebration(permissionKind);
				}
			}
		}
	}

	private void BeginGrantedCelebration(PermissionKind permission)
	{
		if (!IsSignedIn || GrantedSuccessHold <= TimeSpan.Zero || settings.Read((AppSettings s) => s.Onboarding.HasCompletedOnboarding))
		{
			return;
		}
		CancellationTokenSource cancellation = new CancellationTokenSource();
		lock (funnelGate)
		{
			celebrationCancellation?.Cancel();
			celebrationCancellation = cancellation;
			celebratingPermission = permission;
		}
		Log.Info("Onboarding", $"{permission} granted: holding its success state for {GrantedSuccessHold.TotalSeconds:0.0} s");
		Task.Delay(GrantedSuccessHold, cancellation.Token).ContinueWith(delegate(Task t)
		{
			if (!t.IsCanceled)
			{
				lock (funnelGate)
				{
					if (celebrationCancellation != cancellation)
					{
						return;
					}
					celebrationCancellation = null;
					celebratingPermission = null;
				}
				Refresh();
			}
		}, TaskScheduler.Default);
	}

	public static (string Title, string Subtitle, string Action) MicrophoneOffCopy(MicrophoneBlock block)
	{
		return block switch
		{
			MicrophoneBlock.Policy => (Title: "Your microphone is turned off.", Subtitle: "Your organization turned it off for apps. Ask your IT team to allow it, then come back.", Action: "Open Settings"), 
			MicrophoneBlock.Device => (Title: "Your microphone is turned off.", Subtitle: "Turn on “Microphone access” in Settings, then come back here.", Action: "Open Settings"), 
			MicrophoneBlock.AllApps => (Title: "Your microphone is turned off.", Subtitle: "Turn on “Let apps access your microphone” in Settings, then come back here.", Action: "Open Settings"), 
			MicrophoneBlock.DesktopApps => (Title: "Your microphone is turned off.", Subtitle: "Turn on “Let desktop apps access your microphone” in Settings, then come back here.", Action: "Open Settings"), 
			_ => (Title: "I can't hear you yet.", Subtitle: "Turn on microphone access for HeyClicky in Settings, then come back here.", Action: "Open Settings"), 
		};
	}

	public void Refresh()
	{
		(bool, bool) tuple = settings.Read((AppSettings s) => (HasCompletedOnboarding: s.Onboarding.HasCompletedOnboarding, HasAcknowledgedPermissionIntro: s.Onboarding.HasAcknowledgedPermissionIntro));
		string entryContext = (tuple.Item1 ? "recovery" : "firstRun");
		bool canAdvancePastPermissionsStep = CanAdvancePastPermissionsStep;
		bool flag;
		lock (funnelGate)
		{
			flag = hadAllPermissions == false && canAdvancePastPermissionsStep;
			hadAllPermissions = canAdvancePastPermissionsStep;
			if (!IsSignedIn || canAdvancePastPermissionsStep)
			{
				permissionFlowShownTracked = false;
				lastTrackedPermissionStep = null;
			}
		}
		if (flag)
		{
			ClickyAnalytics.TrackCorePermissionsGranted();
			ClickyAnalytics.TrackAllPermissionsGranted();
			ClickyAnalytics.TrackPermissionFlowCompleted(entryContext);
		}
		ActivePermission = null;
		if (!IsSignedIn)
		{
			Set(NotchOnboardingCardKind.SignedOut, "Sign in to get started", "Sees your screen, talks back, runs your apps.", "Sign in with Google");
		}
		else if (!tuple.Item1 && !tuple.Item2)
		{
			if (IsUpgradingUser && CanAdvancePastPermissionsStep)
			{
				Set(NotchOnboardingCardKind.PermissionIntro, "I live in your notch now.", null, "Got it");
			}
			else if (WelcomeMeetsClicky)
			{
				Set(NotchOnboardingCardKind.PermissionIntro, "I live in your notch now.", "Turn your sound on and tap the button below.", "Meet HeyClicky");
			}
			else if (WelcomeOpensMicrophoneSettings)
			{
				var (cardTitle, cardSubtitle, action) = MicrophoneOffCopy(permissions.MicrophoneBlockedBy);
				Set(NotchOnboardingCardKind.PermissionIntro, cardTitle, cardSubtitle, action);
				bool flag2;
				lock (funnelGate)
				{
					flag2 = !permissionFlowShownTracked;
					permissionFlowShownTracked = true;
					lastTrackedPermissionStep = PermissionKind.Microphone;
				}
				if (flag2)
				{
					ClickyAnalytics.TrackPermissionFlowShown(entryContext);
					ClickyAnalytics.TrackPermissionStepShown(PermissionKind.Microphone);
				}
			}
			else
			{
				Set(NotchOnboardingCardKind.PermissionIntro, "I live in your notch now.", "Need permissions to get started.", "Let's start");
			}
		}
		else
		{
			PermissionKind? permissionKind = CelebratingPermission;
			if (permissionKind.HasValue)
			{
				PermissionKind valueOrDefault = permissionKind.GetValueOrDefault();
				if (!tuple.Item1)
				{
					ActivePermission = valueOrDefault;
					ActivePermissionStatus = PermissionStatus.Granted;
					ProgressIndex = Array.IndexOf(PermissionOrder, valueOrDefault) + 1;
					Set(NotchOnboardingCardKind.Permission, "Got it!", ((valueOrDefault == PermissionKind.Microphone) ? "Microphone" : "Screen capture") + " is ready.", "Granted");
					IsPrimaryActionEnabled = false;
					OnPropertyChanged("NeedsPin");
					return;
				}
			}
			if (!CanAdvancePastPermissionsStep)
			{
				int num = Array.FindIndex(PermissionOrder, (PermissionKind p) => permissions.GetStatus(p) != PermissionStatus.Granted);
				PermissionKind permissionKind2 = PermissionOrder[num];
				PermissionStatus status = permissions.GetStatus(permissionKind2);
				ActivePermission = permissionKind2;
				ActivePermissionStatus = status;
				ProgressIndex = num + 1;
				bool flag3;
				bool flag4;
				lock (funnelGate)
				{
					flag3 = !permissionFlowShownTracked;
					permissionFlowShownTracked = true;
					flag4 = lastTrackedPermissionStep != permissionKind2;
					lastTrackedPermissionStep = permissionKind2;
				}
				if (flag3)
				{
					ClickyAnalytics.TrackPermissionFlowShown(entryContext);
				}
				if (flag4)
				{
					ClickyAnalytics.TrackPermissionStepShown(permissionKind2);
				}
				string text;
				string text2;
				if (permissionKind2 != 0)
				{
					text = "Let me see your screen";
					text2 = "I look at your screen when you ask, so I can point at things and draw on it.";
				}
				else
				{
					text = "Let me hear you";
					text2 = "Hold Ctrl+Alt and talk: I need your microphone for that.";
				}
				string cardTitle2 = text;
				string cardSubtitle2 = text2;
				string action2;
				switch (status)
				{
				case PermissionStatus.WaitingInSettings:
				case PermissionStatus.Denied:
					action2 = "Open Settings";
					break;
				case PermissionStatus.Granted:
					action2 = "Granted";
					break;
				default:
					action2 = "Allow";
					break;
				}
				Set(NotchOnboardingCardKind.Permission, cardTitle2, cardSubtitle2, action2);
				bool flag5;
				switch (status)
				{
				case PermissionStatus.Prompting:
				case PermissionStatus.WaitingForPicker:
				case PermissionStatus.Granted:
					flag5 = true;
					break;
				default:
					flag5 = false;
					break;
				}
				IsPrimaryActionEnabled = !flag5;
				OnPropertyChanged("NeedsPin");
				return;
			}
			if (!tuple.Item1)
			{
				Set(NotchOnboardingCardKind.MeetClicky, "All set.", "Turn your sound on and tap the button below.", "Meet HeyClicky");
			}
			else
			{
				Set(NotchOnboardingCardKind.Ready, "", null, null);
			}
		}
		IsPrimaryActionEnabled = true;
		OnPropertyChanged("NeedsPin");
	}

	private void Set(NotchOnboardingCardKind cardKind, string cardTitle, string? cardSubtitle, string? action)
	{
		Kind = cardKind;
		Title = cardTitle;
		Subtitle = cardSubtitle;
		PrimaryActionLabel = action;
	}

	public async Task PrimaryActionAsync()
	{
		switch (Kind)
		{
		case NotchOnboardingCardKind.SignedOut:
			this.SignInRequested?.Invoke(this, EventArgs.Empty);
			break;
		case NotchOnboardingCardKind.PermissionIntro:
		{
			bool welcomeOpensMicrophoneSettings = WelcomeOpensMicrophoneSettings;
			if (!welcomeOpensMicrophoneSettings)
			{
				settings.Update(delegate(AppSettings s)
				{
					s.Onboarding.HasAcknowledgedPermissionIntro = true;
				});
			}
			if (IsIntroHomeWelcomeActive)
			{
				ClickyAnalytics.TrackPreSignInIntroNotchFinalSetupTapped();
				if (!settings.Read((AppSettings s) => s.Onboarding.PreSignInIntroDismissed))
				{
					ClickyAnalytics.TrackPreSignInIntroDismissed();
				}
				settings.Update(delegate(AppSettings s)
				{
					s.Onboarding.PreSignInIntroDismissed = true;
				});
				IsIntroHomeWelcomeActive = false;
				this.IntroWelcomeFinished?.Invoke(this, EventArgs.Empty);
			}
			if (welcomeOpensMicrophoneSettings)
			{
				permissions.OpenSystemSettings(PermissionKind.Microphone);
				ClickyAnalytics.TrackPermissionSettingsOpened(PermissionKind.Microphone);
				Refresh();
			}
			else if (CanAdvancePastPermissionsStep)
			{
				TriggerOnboarding();
			}
			else
			{
				Refresh();
			}
			break;
		}
		case NotchOnboardingCardKind.Permission:
		{
			PermissionKind? permissionKind = ActivePermission;
			if (!permissionKind.HasValue)
			{
				break;
			}
			PermissionKind permission = permissionKind.GetValueOrDefault();
			if (CelebratingPermission.HasValue)
			{
				break;
			}
			PermissionStatus permissionStatus = ActivePermissionStatus;
			if ((permissionStatus == PermissionStatus.WaitingInSettings || permissionStatus == PermissionStatus.Denied) ? true : false)
			{
				permissions.OpenSystemSettings(permission);
				ClickyAnalytics.TrackPermissionSettingsOpened(permission);
			}
			else
			{
				switch (await permissions.RequestAsync(permission).ConfigureAwait(continueOnCapturedContext: true))
				{
				case PermissionStatus.WaitingInSettings:
					ClickyAnalytics.TrackPermissionSettingsOpened(permission);
					break;
				case PermissionStatus.Prompting:
					ClickyAnalytics.TrackPermissionNativePromptTriggered(permission);
					break;
				case PermissionStatus.Denied:
					if (permission == PermissionKind.Microphone)
					{
						ClickyAnalytics.TrackPermissionDenied(permission);
					}
					break;
				}
			}
			Refresh();
			break;
		}
		case NotchOnboardingCardKind.MeetClicky:
			TriggerOnboarding();
			break;
		}
	}

	public void TriggerOnboarding()
	{
		ClickyAnalytics.TrackOnboardingStarted();
		settings.Update(delegate(AppSettings s)
		{
			s.Onboarding.HasCompletedOnboarding = true;
		});
		Refresh();
		this.OnboardingTriggered?.Invoke(this, EventArgs.Empty);
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnIsSignedInChanged(bool value)
	{
		Refresh();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnIsIntroHomeWelcomeActiveChanged(bool value)
	{
		if (value)
		{
			ClickyAnalytics.TrackPreSignInIntroNotchWelcomeShown();
		}
	}
}
