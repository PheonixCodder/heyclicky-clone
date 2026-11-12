using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Notch.Surfaces;
using Windows.UI;

namespace HeyClicky.App.Notch;

public static class NotchSurfaces
{
	public static Func<string, IReadOnlyList<string>> UseCasesFor { get; set; } = (string toolkit) => toolkit.ToLowerInvariant() switch
	{
		"gmail" => new string[4] { "Draft replies from your inbox", "Summarize unread threads", "Find that one email", "Clean up newsletters" }, 
		"github" => new string[4] { "Review open pull requests", "Triage new issues", "Summarize a repo", "Write release notes" }, 
		"googlecalendar" => new string[4] { "Plan your week", "Find a free slot", "Prep for your next meeting", "Move a meeting" }, 
		_ => new string[4] { "Answer questions with your data", "Automate busywork", "Draft and summarize", "Keep things in sync" }, 
	};

	public static NotchSurface? Create(NotchPresentation p, INotchHost host)
	{
		p = NotchGeometry.Shown(p);
		if (!(p is NotchPresentation.TextInput p2))
		{
			if (!(p is NotchPresentation.TextResponse r))
			{
				if (!(p is NotchPresentation.OnboardingPinned onboardingPinned))
				{
					if (p is NotchPresentation.LiveEvent liveEvent)
					{
						NotchLiveEvent @event = liveEvent.Event;
						(ActivityPhase, string, bool)? tuple = ActivitySurface.Classify(@event);
						if (tuple.HasValue)
						{
							var (phase, label, recording) = tuple.GetValueOrDefault();
							return new ActivitySurface(phase, label, recording, Cursor, null);
						}
						if (@event is NotchLiveEvent.AgentTask agentTask)
						{
							return new AgentTaskSurface(agentTask, AgentColors.For(agentTask.TaskId, 0));
						}
						if (@event is NotchLiveEvent.RestingMark)
						{
							if (!NotchGeometry.IsWorkingBar(p))
							{
								return new RestingMarkSurface();
							}
							return null;
						}
						if (@event is NotchLiveEvent.ClickyUpdate)
						{
							return new ClickyUpdateSurface();
						}
						if (@event is NotchLiveEvent.AppUpdated e)
						{
							return new AppUpdatedSurface(e);
						}
						if (@event is NotchLiveEvent.DictationCopiedToClipboard)
						{
							return new DictationClipboardSurface();
						}
						if (@event is NotchLiveEvent.VoiceReplyMutedCopiedToClipboard e2)
						{
							return new MutedSpeakerSurface(e2);
						}
						if (@event is NotchLiveEvent.MicrophoneTrouble e3)
						{
							return new MicrophoneTroubleSurface(e3);
						}
						if (@event is NotchLiveEvent.MeetingCountdown e4)
						{
							return new MeetingCountdownSurface(e4);
						}
						if (@event is NotchLiveEvent.OnboardingTutorialInvite)
						{
							return new TutorialInviteSurface(TutorialInviteSurface.Mode.Invite);
						}
						if (@event is NotchLiveEvent.OnboardingTutorialResume)
						{
							return new TutorialInviteSurface(TutorialInviteSurface.Mode.Resume, host.SizeContext.TutorialResumeShowsContinue);
						}
						if (@event is NotchLiveEvent.MorningSuggestions e5)
						{
							return new MorningSuggestionsSurface(e5);
						}
						if (@event is NotchLiveEvent.IntegrationSuggestion integrationSuggestion)
						{
							IReadOnlyList<string> readOnlyList = UseCasesFor(integrationSuggestion.Toolkit);
							host.SizeContext.IntegrationHasUseCases = readOnlyList.Count > 0;
							return new IntegrationSuggestionSurface(integrationSuggestion, readOnlyList);
						}
						if (@event is NotchLiveEvent.IntegrationConnected integrationConnected)
						{
							IReadOnlyList<string> readOnlyList2 = UseCasesFor(integrationConnected.Toolkit);
							host.SizeContext.IntegrationHasUseCases = readOnlyList2.Count > 0;
							return new IntegrationConnectedSurface(integrationConnected, readOnlyList2);
						}
						if (@event is NotchLiveEvent.AgentPeek)
						{
							return new NotchPeekSurface();
						}
						if (@event is NotchLiveEvent.HandoffQueued || @event is NotchLiveEvent.HandoffSent)
						{
							return new HandoffSurface(@event);
						}
						if (@event is NotchLiveEvent.SkillPinned e6)
						{
							return new SkillPinnedSurface(e6);
						}
						if (@event is NotchLiveEvent.IntroSkillPing e7)
						{
							return new IntroSkillPingSurface(e7, Cursor());
						}
						if (@event is NotchLiveEvent.SidecarLearningSuggestion || @event is NotchLiveEvent.SpotifyVoiceTip)
						{
							return new AppTipSurface(@event);
						}
						if (@event is NotchLiveEvent.DictionaryWordLearned e8)
						{
							return new DictionaryLearnedSurface(e8);
						}
						if (@event is NotchLiveEvent.DocumentFolderAccessDenied e9)
						{
							return new DocumentAccessSurface(e9);
						}
					}
					return null;
				}
				return new OnboardingCardSurface(onboardingPinned.Step);
			}
			return new TextResponseSurface(r);
		}
		return new TextInputSurface(p2, host.Services.Auth.IsSignedIn, () => host.WindowHandle);
		Color Cursor()
		{
			return DS.CursorColor(host.Services.Settings.CursorColorHex);
		}
	}
}
