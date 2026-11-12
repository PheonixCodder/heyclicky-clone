using System;
using HeyClicky.App.Services;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Wiring;

public sealed class CompanionNotchBridge : IDisposable
{
	private readonly AppServices s;

	private readonly DispatcherQueue dq;

	private bool voiceTurn;

	public CompanionNotchBridge(AppServices services, DispatcherQueue dispatcher)
	{
		s = services;
		dq = dispatcher;
		s.Shortcuts.ShortcutTriggered += OnShortcut;
		s.Companion.StateChanged += OnState;
		s.Companion.ResponseTextUpdated += OnResponse;
	}

	private void OnShortcut(object? sender, ShortcutAction a)
	{
		dq.TryEnqueue(delegate
		{
			if (!s.Settings.IsPaused || a == ShortcutAction.Escape)
			{
				switch (a)
				{
				case ShortcutAction.PushToTalkDown:
					voiceTurn = true;
					s.Microphone.Start();
					s.Presenter.ShowLiveEvent(new NotchLiveEvent.ActivityListening(), null);
					s.Companion.BeginPushToTalkAsync();
					break;
				case ShortcutAction.PushToTalkUp:
					s.Microphone.Stop();
					s.Companion.EndPushToTalkAsync();
					break;
				case ShortcutAction.TextComposerToggle:
					if (s.Presenter.Presentation is NotchPresentation.TextInput)
					{
						s.Presenter.ReturnToIdle();
					}
					else
					{
						voiceTurn = false;
						s.Presenter.Present(new NotchPresentation.TextInput(""));
					}
					break;
				case ShortcutAction.Escape:
				{
					NotchPresentation presentation = s.Presenter.Presentation;
					bool flag = ((presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse) ? true : false);
					if (flag || s.Companion.State != 0)
					{
						s.Companion.Cancel();
						s.Presenter.ReturnToIdle();
					}
					break;
				}
				case ShortcutAction.DictationDown:
				case ShortcutAction.DictationUp:
					break;
				}
			}
		});
	}

	private void OnState(object? sender, EventArgs e)
	{
		dq.TryEnqueue(delegate
		{
			NotchPresentation presentation = s.Presenter.Presentation;
			bool flag = ((presentation is NotchPresentation.TextResponse || presentation is NotchPresentation.TextInput) ? true : false);
			if (!flag && voiceTurn)
			{
				switch (s.Companion.State)
				{
				case VoiceState.Listening:
					s.Presenter.ShowLiveEvent(new NotchLiveEvent.ActivityListening(), null);
					break;
				case VoiceState.Processing:
					s.Presenter.ShowLiveEvent(new NotchLiveEvent.ActivityThinking(), null);
					break;
				case VoiceState.Responding:
					s.Presenter.ShowLiveEvent(new NotchLiveEvent.ActivitySpeaking(), null);
					break;
				case VoiceState.Idle:
					{
						voiceTurn = false;
						if (s.Presenter.Presentation is NotchPresentation.LiveEvent liveEvent)
						{
							NotchLiveEvent @event = liveEvent.Event;
							if (@event is NotchLiveEvent.ActivityListening || @event is NotchLiveEvent.ActivityThinking || @event is NotchLiveEvent.ActivitySpeaking)
							{
								flag = true;
								goto IL_0105;
							}
						}
						flag = false;
						goto IL_0105;
					}
					IL_0105:
					if (flag)
					{
						s.Presenter.ReturnToIdle();
					}
					break;
				}
			}
		});
	}

	private void OnResponse(object? sender, string text)
	{
		dq.TryEnqueue(delegate
		{
			if (s.Presenter.Presentation is NotchPresentation.TextResponse textResponse)
			{
				s.Presenter.Present(new NotchPresentation.TextResponse(textResponse.Question, text, IsThinking: false));
			}
		});
	}

	public void Dispose()
	{
		s.Shortcuts.ShortcutTriggered -= OnShortcut;
		s.Companion.StateChanged -= OnState;
		s.Companion.ResponseTextUpdated -= OnResponse;
	}
}
