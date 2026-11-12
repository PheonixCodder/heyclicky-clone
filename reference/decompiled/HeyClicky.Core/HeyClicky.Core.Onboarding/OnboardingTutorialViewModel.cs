using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using HeyClicky.Core.Settings;

namespace HeyClicky.Core.Onboarding;

public sealed class OnboardingTutorialViewModel : ObservableObject
{
	public const double MutedOutputVolumeThreshold = 0.02;

	public const double SpeakerFixedVolumeThreshold = 0.05;

	public static readonly TimeSpan TextModeComposerAutoDismiss = TimeSpan.FromSeconds(3L);

	private readonly SettingsStore settings;

	private readonly Func<double> outputVolume;

	private readonly HashSet<OnboardingTutorialStep> completed = new HashSet<OnboardingTutorialStep>();

	private int turnStampGeneration;

	private OnboardingTutorialStep? stepTheActiveTurnStartedOn;

	private bool drawDidPresentThisStep;

	private CancellationTokenSource? composerCountdown;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsEngaged", new string[] { "IsRestrictingNormalUse", "CurrentStepInfo" })]
	private OnboardingTutorialPhase phase;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsRestrictingNormalUse", new string[] { "CurrentStepInfo", "IsCurrentStepCompleted", "CanGoBack" })]
	private OnboardingTutorialStep? currentStep;

	[ObservableProperty]
	private string dictationText = "";

	[ObservableProperty]
	private int furthestVisitedStep;

	[ObservableProperty]
	private bool didShowSpeakerCheck;

	[ObservableProperty]
	private bool skipsDrawingSteps;

	public bool IsEngaged => Phase != OnboardingTutorialPhase.Inactive;

	public bool IsRestrictingNormalUse
	{
		get
		{
			bool flag = IsEngaged;
			if (flag)
			{
				bool flag2;
				switch (CurrentStep)
				{
				case OnboardingTutorialStep.ClickyInterview:
				case OnboardingTutorialStep.Finale:
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = !flag2;
			}
			return flag;
		}
	}

	public bool IsCurrentStepCompleted
	{
		get
		{
			OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
			if (onboardingTutorialStep.HasValue)
			{
				OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
				return completed.Contains(valueOrDefault);
			}
			return false;
		}
	}

	public bool CanGoBack
	{
		get
		{
			OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
			if (onboardingTutorialStep.HasValue)
			{
				OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
				return PreviousStep(valueOrDefault).HasValue;
			}
			return false;
		}
	}

	public OnboardingTutorialStepInfo? CurrentStepInfo
	{
		get
		{
			OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
			if (onboardingTutorialStep.HasValue)
			{
				OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
				return Info(valueOrDefault);
			}
			return null;
		}
	}

	public bool HasCompletedTutorial => settings.Read((AppSettings s) => s.Onboarding.TutorialCompletedAtEpochSeconds).HasValue;

	public Func<bool>? DrawStepStageOwnsCompletion { get; set; }

	public bool DrawDidPresentThisStep => drawDidPresentThisStep;

	public bool AllowsPushToTalk
	{
		get
		{
			if (IsEngaged)
			{
				OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
				if (onboardingTutorialStep.HasValue)
				{
					OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
					return Info(valueOrDefault).AllowsPushToTalk;
				}
				return false;
			}
			return true;
		}
	}

	public bool AllowsTextMode
	{
		get
		{
			if (IsEngaged)
			{
				OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
				if (onboardingTutorialStep.HasValue)
				{
					OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
					return Info(valueOrDefault).AllowsTextMode;
				}
				return false;
			}
			return true;
		}
	}

	public bool AllowsDictationPress
	{
		get
		{
			bool result;
			if (Phase == OnboardingTutorialPhase.Running)
			{
				OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
				if (onboardingTutorialStep.HasValue)
				{
					OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
					if ((uint)(valueOrDefault - 9) <= 1u)
					{
						result = true;
						goto IL_002e;
					}
				}
				result = false;
				goto IL_002e;
			}
			return true;
			IL_002e:
			return result;
		}
	}

	public bool IsOnDictationStep
	{
		get
		{
			if (Phase == OnboardingTutorialPhase.Running)
			{
				return CurrentStep == OnboardingTutorialStep.Dictation;
			}
			return false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public OnboardingTutorialPhase Phase
	{
		get
		{
			return phase;
		}
		set
		{
			if (!EqualityComparer<OnboardingTutorialPhase>.Default.Equals(phase, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Phase);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsEngaged);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRestrictingNormalUse);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentStepInfo);
				phase = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Phase);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsEngaged);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRestrictingNormalUse);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentStepInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public OnboardingTutorialStep? CurrentStep
	{
		get
		{
			return currentStep;
		}
		set
		{
			if (!EqualityComparer<OnboardingTutorialStep?>.Default.Equals(currentStep, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentStep);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRestrictingNormalUse);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentStepInfo);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsCurrentStepCompleted);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanGoBack);
				currentStep = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentStep);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRestrictingNormalUse);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentStepInfo);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsCurrentStepCompleted);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanGoBack);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string DictationText
	{
		get
		{
			return dictationText;
		}
		[MemberNotNull("dictationText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(dictationText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DictationText);
				dictationText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DictationText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int FurthestVisitedStep
	{
		get
		{
			return furthestVisitedStep;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(furthestVisitedStep, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FurthestVisitedStep);
				furthestVisitedStep = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FurthestVisitedStep);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DidShowSpeakerCheck
	{
		get
		{
			return didShowSpeakerCheck;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(didShowSpeakerCheck, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DidShowSpeakerCheck);
				didShowSpeakerCheck = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DidShowSpeakerCheck);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool SkipsDrawingSteps
	{
		get
		{
			return skipsDrawingSteps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(skipsDrawingSteps, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SkipsDrawingSteps);
				skipsDrawingSteps = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SkipsDrawingSteps);
			}
		}
	}

	public event EventHandler<string>? SuccessLine;

	public event EventHandler? StepCompletedChime;

	public event EventHandler<(string Reason, string CursorLine)>? Ended;

	public event EventHandler? StartMusicRequested;

	public event EventHandler? CloseComposerRequested;

	public event EventHandler? InterviewRequested;

	public event EventHandler? SetVolumeToHalfRequested;

	public OnboardingTutorialViewModel(SettingsStore settings, Func<double>? outputVolume = null)
	{
		this.settings = settings;
		this.outputVolume = outputVolume ?? ((Func<double>)(() => 1.0));
	}

	public bool ShouldOfferAfterVideo(bool isUpgradingUser, bool isSignedIn)
	{
		if (!isUpgradingUser && isSignedIn)
		{
			return !HasCompletedTutorial;
		}
		return false;
	}

	public static bool InterceptsPushToTalkRelease(OnboardingTutorialStep step)
	{
		if (step == OnboardingTutorialStep.ClickyDrive || step == OnboardingTutorialStep.EmailDraft)
		{
			return true;
		}
		return false;
	}

	public static bool SkipsScreenCapture(OnboardingTutorialStep step)
	{
		if ((uint)step <= 2u || (uint)(step - 6) <= 2u)
		{
			return true;
		}
		return false;
	}

	public bool IsRetired(OnboardingTutorialStep step)
	{
		bool flag = step == OnboardingTutorialStep.EmailDraft;
		if (!flag)
		{
			bool flag2 = SkipsDrawingSteps;
			if (flag2)
			{
				bool flag3 = (uint)(step - 4) <= 1u;
				flag2 = flag3;
			}
			flag = flag2;
		}
		return flag;
	}

	public static OnboardingTutorialStepInfo Info(OnboardingTutorialStep step)
	{
		return step switch
		{
			OnboardingTutorialStep.MicCheck => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: false, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 1.6, "loud and clear ٩(◕\u203f◕)۶", "First, can I hear you?", "Say anything out loud. If the bars dance, your mic works. I hear best on built-in or wired mics: Bluetooth ones get flaky."), 
			OnboardingTutorialStep.SpeakerCheck => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: false, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: true, 1.6, "much better. now you'll actually hear me ヽ(^o^)ノ", "Your volume is at zero", "You'd see me talking and hear nothing. One click fixes it before we start."), 
			OnboardingTutorialStep.VoiceHello => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: false, CompletesOnTurn: true, AutoAdvancesAfterCompletion: false, 1.6, "nice to meet you (◠\u203f◠)", "This is how you talk to me", "Hold the keys down, say hi or tell me your name, then let go. I'm listening the whole time you hold them."), 
			OnboardingTutorialStep.ClickyInterview => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 1.6, "thanks for the chat (◕\u203f◕)", "I'm curious about you.", "Four quick questions about you, so I can make this whole thing actually useful for you."), 
			OnboardingTutorialStep.DrawDemo => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: false, CompletesOnTurn: true, AutoAdvancesAfterCompletion: false, 1.6, null, "Here's how I'd help you", "I see what you see. Hold the keys, ask, and watch your screen."), 
			OnboardingTutorialStep.DrawToAsk => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: false, CompletesOnTurn: true, AutoAdvancesAfterCompletion: true, 1.6, "exactly. marked and understood (¬\u203f¬)", "Circle anything, ask about it", null), 
			OnboardingTutorialStep.ClickyDrive => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 1.6, "and i can do that in any app. just ask me to do it for you (•\u0300ᴗ•\u0301)و", "I can drive your computer too", "Ask me to do it and I'll take the mouse while you watch. Move it anytime and it's yours again."), 
			OnboardingTutorialStep.TextMode => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: false, AllowsTextMode: true, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 3.6, "found it. that box is always a double-tap away ( \u02d8▽\u02d8)", "Not in the mood to talk? Type", null), 
			OnboardingTutorialStep.Dictation => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: false, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: true, 3.6, "that works in every text field on your PC ᕕ( ᐛ )ᕗ", "You can also dictate word-for-word", "I type exactly what you say, clean it up, and instantly put it into any box. Works in any program."), 
			OnboardingTutorialStep.Finale => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: true, AllowsTextMode: true, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 1.6, null, "There's one more thing...", "Voice, drawing, text mode, agents: you've seen it all. But, hey, can I show you one more thing?"), 
			_ => new OnboardingTutorialStepInfo(step, AllowsPushToTalk: false, AllowsTextMode: false, CompletesOnTurn: false, AutoAdvancesAfterCompletion: false, 1.6, null, "", null), 
		};
	}

	public OnboardingTutorialStep? NextStep(OnboardingTutorialStep step)
	{
		for (int i = (int)(step + 1); i <= 10; i++)
		{
			if (!IsRetired((OnboardingTutorialStep)i))
			{
				return (OnboardingTutorialStep)i;
			}
		}
		return null;
	}

	public OnboardingTutorialStep? PreviousStep(OnboardingTutorialStep step)
	{
		for (int num = (int)(step - 1); num >= 0; num--)
		{
			OnboardingTutorialStep onboardingTutorialStep = (OnboardingTutorialStep)num;
			if (!IsRetired(onboardingTutorialStep) && (onboardingTutorialStep != OnboardingTutorialStep.SpeakerCheck || DidShowSpeakerCheck))
			{
				return onboardingTutorialStep;
			}
		}
		return null;
	}

	public bool PresentInvite()
	{
		if (Phase != 0)
		{
			return false;
		}
		Phase = OnboardingTutorialPhase.InvitePresented;
		return true;
	}

	public void StartFromInvite()
	{
		completed.Clear();
		DidShowSpeakerCheck = false;
		DictationText = "";
		FurthestVisitedStep = 0;
		stepTheActiveTurnStartedOn = null;
		Phase = OnboardingTutorialPhase.Running;
		GoTo(OnboardingTutorialStep.MicCheck);
	}

	public void BypassInvite()
	{
		if (Phase == OnboardingTutorialPhase.InvitePresented)
		{
			Phase = OnboardingTutorialPhase.Inactive;
		}
	}

	public void Skip()
	{
		End("skipped");
	}

	public void Finish()
	{
		End("finished");
	}

	public void End(string reason)
	{
		if (Phase != 0)
		{
			composerCountdown?.Cancel();
			settings.Update(delegate(AppSettings s)
			{
				s.Onboarding.TutorialCompletedAtEpochSeconds = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
			});
			Phase = OnboardingTutorialPhase.Inactive;
			CurrentStep = null;
			string item = ((reason == "skipped") ? "skipped? very brave. god speed my friend. here if you need me, just press ctrl + alt" : "that's it! i'm all yours now. hold ctrl + alt and im here. learn something new, get feedback on a design, spawn a research agent, whatever!");
			this.Ended?.Invoke(this, (reason, item));
		}
	}

	public void Advance()
	{
		OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
		if (!onboardingTutorialStep.HasValue)
		{
			return;
		}
		OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
		if (valueOrDefault == OnboardingTutorialStep.MicCheck)
		{
			this.StartMusicRequested?.Invoke(this, EventArgs.Empty);
		}
		if (valueOrDefault == OnboardingTutorialStep.MicCheck)
		{
			bool flag2 = (DidShowSpeakerCheck = outputVolume() <= 0.02);
			GoTo(flag2 ? OnboardingTutorialStep.SpeakerCheck : OnboardingTutorialStep.VoiceHello);
			return;
		}
		onboardingTutorialStep = NextStep(valueOrDefault);
		if (onboardingTutorialStep.HasValue)
		{
			OnboardingTutorialStep valueOrDefault2 = onboardingTutorialStep.GetValueOrDefault();
			GoTo(valueOrDefault2);
		}
		else
		{
			Finish();
		}
	}

	public void Back()
	{
		OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
		if (onboardingTutorialStep.HasValue)
		{
			OnboardingTutorialStep valueOrDefault = onboardingTutorialStep.GetValueOrDefault();
			onboardingTutorialStep = PreviousStep(valueOrDefault);
			if (onboardingTutorialStep.HasValue)
			{
				OnboardingTutorialStep valueOrDefault2 = onboardingTutorialStep.GetValueOrDefault();
				GoTo(valueOrDefault2);
			}
		}
	}

	public void BeginAt(OnboardingTutorialStep step)
	{
		if (Phase != 0 && !IsRetired(step))
		{
			GoTo(step);
		}
	}

	public void JumpTo(OnboardingTutorialStep step)
	{
		if ((int)step <= FurthestVisitedStep && !IsRetired(step))
		{
			GoTo(step);
		}
	}

	private void GoTo(OnboardingTutorialStep step)
	{
		composerCountdown?.Cancel();
		drawDidPresentThisStep = false;
		if (step == OnboardingTutorialStep.Dictation)
		{
			DictationText = "";
		}
		CurrentStep = step;
		FurthestVisitedStep = Math.Max(FurthestVisitedStep, (int)step);
		if (step == OnboardingTutorialStep.ClickyInterview && !completed.Contains(step))
		{
			this.InterviewRequested?.Invoke(this, EventArgs.Empty);
		}
		OnPropertyChanged("IsCurrentStepCompleted");
	}

	public void NoteMicHeardSustainedInput()
	{
		MarkCompleted(OnboardingTutorialStep.MicCheck);
	}

	public void NoteOutputVolume(double volume)
	{
		if (volume > 0.05)
		{
			MarkCompleted(OnboardingTutorialStep.SpeakerCheck);
		}
	}

	public void SetVolumeToHalf()
	{
		this.SetVolumeToHalfRequested?.Invoke(this, EventArgs.Empty);
		MarkCompleted(OnboardingTutorialStep.SpeakerCheck);
	}

	public void NoteCompanionTurnStarted()
	{
		turnStampGeneration++;
		stepTheActiveTurnStartedOn = CurrentStep;
	}

	public void NoteDrawingPresented()
	{
		drawDidPresentThisStep = true;
	}

	public async Task NoteCompanionTurnCompletedAsync()
	{
		OnboardingTutorialStep? onboardingTutorialStep = CurrentStep;
		if (!onboardingTutorialStep.HasValue)
		{
			return;
		}
		OnboardingTutorialStep step = onboardingTutorialStep.GetValueOrDefault();
		if (stepTheActiveTurnStartedOn != step)
		{
			return;
		}
		switch (step)
		{
		case OnboardingTutorialStep.VoiceHello:
		case OnboardingTutorialStep.DrawToAsk:
			MarkCompleted(step);
			break;
		case OnboardingTutorialStep.DrawDemo:
			if (!drawDidPresentThisStep)
			{
				break;
			}
			stepTheActiveTurnStartedOn = null;
			if (!(DrawStepStageOwnsCompletion?.Invoke() ?? false))
			{
				await Task.Delay(600).ConfigureAwait(continueOnCapturedContext: true);
				if (CurrentStep == step)
				{
					MarkCompleted(step);
					Advance();
				}
			}
			break;
		case OnboardingTutorialStep.ClickyInterview:
			break;
		}
	}

	public void CompleteDrawStepFromStage()
	{
		if (CurrentStep == OnboardingTutorialStep.DrawDemo)
		{
			stepTheActiveTurnStartedOn = null;
			MarkCompleted(OnboardingTutorialStep.DrawDemo);
			Advance();
		}
	}

	public void NoteDriveDemoFinished()
	{
		MarkCompleted(OnboardingTutorialStep.ClickyDrive);
	}

	public void NoteInterviewFinished()
	{
		MarkCompleted(OnboardingTutorialStep.ClickyInterview);
	}

	public void NoteTextComposerOpened()
	{
		if (CurrentStep == OnboardingTutorialStep.TextMode)
		{
			MarkCompleted(OnboardingTutorialStep.TextMode, speakSuccessLine: false);
			composerCountdown?.Cancel();
			RunComposerCountdownAsync((composerCountdown = new CancellationTokenSource()).Token);
		}
	}

	private async Task RunComposerCountdownAsync(CancellationToken ct)
	{
		_ = 1;
		try
		{
			await Task.Delay(TextModeComposerAutoDismiss, ct).ConfigureAwait(continueOnCapturedContext: true);
			this.CloseComposerRequested?.Invoke(this, EventArgs.Empty);
			this.SuccessLine?.Invoke(this, Info(OnboardingTutorialStep.TextMode).SuccessCursorLine);
			await Task.Delay(2000, ct).ConfigureAwait(continueOnCapturedContext: true);
			if (CurrentStep == OnboardingTutorialStep.TextMode)
			{
				Advance();
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	public void NoteTextComposerTyping()
	{
		composerCountdown?.Cancel();
	}

	public void NoteTextComposerClosedByUser()
	{
		if (CurrentStep == OnboardingTutorialStep.TextMode)
		{
			composerCountdown?.Cancel();
			Advance();
		}
	}

	public async Task NoteTypedTurnFinishedAsync()
	{
		if (CurrentStep == OnboardingTutorialStep.TextMode)
		{
			composerCountdown?.Cancel();
			await Task.Delay(500).ConfigureAwait(continueOnCapturedContext: true);
			if (CurrentStep == OnboardingTutorialStep.TextMode)
			{
				this.SuccessLine?.Invoke(this, Info(OnboardingTutorialStep.TextMode).SuccessCursorLine);
				Advance();
			}
		}
	}

	public bool ConsumeDictationTranscript(string transcript)
	{
		if (!IsOnDictationStep)
		{
			return false;
		}
		DictationText = ((DictationText.Length == 0) ? transcript : (DictationText + " " + transcript));
		MarkCompleted(OnboardingTutorialStep.Dictation);
		return true;
	}

	public void NoteDictationTextInserted(string text)
	{
		if (CurrentStep == OnboardingTutorialStep.Dictation && !string.IsNullOrWhiteSpace(text))
		{
			MarkCompleted(OnboardingTutorialStep.Dictation);
		}
	}

	private void MarkCompleted(OnboardingTutorialStep step, bool speakSuccessLine = true)
	{
		if (CurrentStep != step || !completed.Add(step))
		{
			return;
		}
		this.StepCompletedChime?.Invoke(this, EventArgs.Empty);
		OnboardingTutorialStepInfo onboardingTutorialStepInfo = Info(step);
		if (speakSuccessLine)
		{
			string successCursorLine = onboardingTutorialStepInfo.SuccessCursorLine;
			if (successCursorLine != null && step != OnboardingTutorialStep.TextMode)
			{
				this.SuccessLine?.Invoke(this, successCursorLine);
			}
		}
		OnPropertyChanged("IsCurrentStepCompleted");
		if (onboardingTutorialStepInfo.AutoAdvancesAfterCompletion)
		{
			AutoAdvanceAsync(step, onboardingTutorialStepInfo.AutoAdvanceDelaySeconds);
		}
	}

	private async Task AutoAdvanceAsync(OnboardingTutorialStep step, double delaySeconds)
	{
		await Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ConfigureAwait(continueOnCapturedContext: true);
		if (CurrentStep == step && Phase == OnboardingTutorialPhase.Running)
		{
			Advance();
		}
	}
}
