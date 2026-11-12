using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace HeyClicky.App.Notch;

public static class NotchAccessibility
{
	private static string? last;

	public static string Describe(NotchPresentation presentation)
	{
		if (!(presentation is NotchPresentation.Idle))
		{
			if (!(presentation is NotchPresentation.TextInput))
			{
				if (presentation is NotchPresentation.TextResponse textResponse)
				{
					if (textResponse.IsThinking)
					{
						return "HeyClicky is thinking";
					}
					return string.IsNullOrWhiteSpace(textResponse.Answer) ? "HeyClicky answer" : ("HeyClicky: " + Trim(textResponse.Answer));
				}
				if (!(presentation is NotchPresentation.OnboardingPinned))
				{
					if (presentation is NotchPresentation.LiveEvent liveEvent)
					{
						NotchLiveEvent @event = liveEvent.Event;
						return (@event is NotchLiveEvent.ActivityListening) ? "Listening" : ((@event is NotchLiveEvent.ActivityThinking) ? "Thinking" : ((@event is NotchLiveEvent.ActivitySpeaking) ? "Speaking" : ((@event is NotchLiveEvent.ActivityCustomStatus activityCustomStatus) ? activityCustomStatus.Label : ((@event is NotchLiveEvent.Dictation dictation) ? dictation.Label : ((@event is NotchLiveEvent.AgentTask agentTask) ? (agentTask.Title + ": " + agentTask.Status) : ((@event is NotchLiveEvent.AgentMultiActivity agentMultiActivity) ? $"{agentMultiActivity.Count} Clickys working" : ((@event is NotchLiveEvent.MicrophoneTrouble microphoneTrouble) ? microphoneTrouble.Message : ((@event is NotchLiveEvent.VoiceReplyMutedCopiedToClipboard) ? "Your sound is off. The reply was copied to your clipboard" : ((@event is NotchLiveEvent.DictationCopiedToClipboard) ? "Copied to your clipboard. Press Ctrl+V to paste" : ((@event is NotchLiveEvent.AppUpdated appUpdated) ? ("HeyClicky updated. You're on version " + appUpdated.Version) : ((@event is NotchLiveEvent.DictionaryWordLearned dictionaryWordLearned) ? ("Added " + dictionaryWordLearned.Word + " to your dictionary") : ((@event is NotchLiveEvent.RestingMark) ? "HeyClicky, working" : ((@event is NotchLiveEvent.ClickyUpdate clickyUpdate) ? ((clickyUpdate.Kind == "done") ? "A job is done" : ((clickyUpdate.Kind == "failed") ? "A job didn't finish" : "A job needs you")) : ((!(@event is NotchLiveEvent.AgentPeek)) ? "HeyClicky" : "HeyClicky"))))))))))))));
					}
					return "HeyClicky";
				}
				return "HeyClicky setup";
			}
			return "Type to HeyClicky";
		}
		return "HeyClicky";
	}

	private static string Trim(string text)
	{
		if (text.Length > 200)
		{
			return text.Substring(0, 200);
		}
		return text;
	}

	public static void Announce(FrameworkElement root, NotchPresentation presentation)
	{
		string text = Describe(presentation);
		if (text == last)
		{
			return;
		}
		last = text;
		AutomationProperties.SetName(root, text);
		AutomationProperties.SetLiveSetting(root, AutomationLiveSetting.Polite);
		try
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
			{
				(FrameworkElementAutomationPeer.FromElement(root) ?? FrameworkElementAutomationPeer.CreatePeerForElement(root))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			}
		}
		catch
		{
		}
	}
}
