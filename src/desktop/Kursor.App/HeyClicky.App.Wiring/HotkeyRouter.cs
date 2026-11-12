using System;
using HeyClicky.App.Browser;
using HeyClicky.App.Proactive;
using HeyClicky.App.Services;
using HeyClicky.Platform.Input;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Wiring;

public sealed class HotkeyRouter : IDisposable
{
	private readonly AppServices s;

	private readonly RealStack real;

	private readonly DispatcherQueue dq;

	private long pressStarted;

	private volatile bool textInputOpen;

	public Func<bool> AllowsPushToTalk { get; set; } = () => true;

	public Func<bool> AllowsTextMode { get; set; } = () => true;

	public Func<bool>? NotchChatToggle { get; set; }

	public Func<bool>? NotchEscape { get; set; }

	public Func<bool>? NotchEscapeWouldClaim { get; set; }

	public Func<bool>? NotchViewerEscapeWouldClaim { get; set; }

	public Func<bool>? InterceptReleaseAfterPress { get; set; }

	public Func<bool>? DriveClaimsEscape { get; set; }

	public Func<bool>? DriveEscape { get; set; }

	public Func<bool>? LessonReplaying { get; set; }

	public Action? StopLessonReplay { get; set; }

	public HotkeyRouter(AppServices services, RealStack real, DispatcherQueue dq)
	{
		HotkeyRouter hotkeyRouter = this;
		s = services;
		this.real = real;
		this.dq = dq;
		s.Shortcuts.ShortcutTriggered += OnShortcut;
		ShortcutMonitor shortcuts = real.Shortcuts;
		if (shortcuts != null)
		{
			shortcuts.ConsumesEscape = WouldHandleEscape;
		}
		s.Presenter.PresentationChanged += delegate
		{
			bool flag = hotkeyRouter.s.Presenter.Presentation is NotchPresentation.TextInput;
			if (flag != hotkeyRouter.textInputOpen)
			{
				hotkeyRouter.textInputOpen = flag;
				if (flag)
				{
					real.Companion.BeginPageReadForTextInput(() => hotkeyRouter.s.Presenter.Presentation is NotchPresentation.TextInput);
				}
				else
				{
					real.Companion.TextInputClosed();
				}
			}
		};
	}

	public bool WouldHandleEscape()
	{
		Func<bool>? driveClaimsEscape = DriveClaimsEscape;
		if (driveClaimsEscape != null && driveClaimsEscape())
		{
			return true;
		}
		Func<bool>? lessonReplaying = LessonReplaying;
		if (lessonReplaying != null && lessonReplaying())
		{
			return true;
		}
		if (s.Settings.IsPaused)
		{
			return false;
		}
		if (ChromeSetupFlow.Shared.IsShowingHow)
		{
			return true;
		}
		if (real.Dictation.IsSessionActive)
		{
			return true;
		}
		Func<bool>? notchViewerEscapeWouldClaim = NotchViewerEscapeWouldClaim;
		if (notchViewerEscapeWouldClaim != null && notchViewerEscapeWouldClaim())
		{
			return true;
		}
		if (pressStarted != 0L)
		{
			return true;
		}
		if (real.Companion.IsOnboardingAnswerInFlight)
		{
			return false;
		}
		Func<bool>? notchEscapeWouldClaim = NotchEscapeWouldClaim;
		if (notchEscapeWouldClaim != null && notchEscapeWouldClaim())
		{
			return true;
		}
		if (s.Companion.State != 0)
		{
			return true;
		}
		if (real.Canvas.IsShowing)
		{
			return true;
		}
		NotchPresentation presentation = s.Presenter.Presentation;
		if (presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse)
		{
			return true;
		}
		return false;
	}

	private void OnShortcut(object? sender, ShortcutAction action)
	{
		if (action == ShortcutAction.SystemScreenshotShortcut)
		{
			ScreenCaptureYield.NoteCaptureUiMayBeOpening("screenshot shortcut pressed");
			return;
		}
		bool flag = s.Settings.IsPaused;
		if (flag)
		{
			ShortcutAction shortcutAction = action;
			bool flag2 = (uint)(shortcutAction - 4) <= 2u;
			flag = !flag2;
		}
		if (flag)
		{
			return;
		}
		switch (action)
		{
		case ShortcutAction.DictationDown:
		case ShortcutAction.DictationUp:
		case ShortcutAction.DictationCancel:
			real.Dictation.HandleShortcut(action);
			break;
		case ShortcutAction.PushToTalkDown:
			if (AllowsPushToTalk())
			{
				if (real.Dictation.IsCapturingAudio)
				{
					Log.Info("hotkeys", "push-to-talk ignored: dictation is recording");
					pressStarted = 0L;
					break;
				}
				pressStarted = Environment.TickCount64;
				real.Capture.Prewarm();
				real.MicrophoneStartCue.Play("textOpen", null);
				s.Companion.BeginPushToTalkAsync();
			}
			break;
		case ShortcutAction.PushToTalkUp:
		{
			if (pressStarted == 0L)
			{
				break;
			}
			long num2 = Environment.TickCount64 - pressStarted;
			pressStarted = 0L;
			Func<bool>? interceptReleaseAfterPress = InterceptReleaseAfterPress;
			if (interceptReleaseAfterPress != null && interceptReleaseAfterPress())
			{
				s.Companion.Cancel();
				real.Highlight.Clear();
				break;
			}
			s.Companion.EndPushToTalkAsync();
			if (num2 < 500)
			{
				real.Highlight.Clear();
			}
			break;
		}
		case ShortcutAction.TextComposerToggle:
			if (!AllowsTextMode())
			{
				break;
			}
			dq.TryEnqueue(delegate
			{
				Func<bool>? notchChatToggle = NotchChatToggle;
				if (notchChatToggle == null || !notchChatToggle())
				{
					if (s.Presenter.Presentation is NotchPresentation.TextInput)
					{
						s.Presenter.Present(new NotchPresentation.Idle());
					}
					else
					{
						real.Audio.PlayEarcon("textOpen");
						s.Presenter.Present(new NotchPresentation.TextInput(""));
					}
				}
			});
			break;
		case ShortcutAction.ProactiveDebugSweep:
		case ShortcutAction.MorningPreviewChord:
			dq.TryEnqueue(delegate
			{
				ProactiveCoordinator.Current?.OnDebugChord(action);
			});
			break;
		case ShortcutAction.Escape:
		{
			Func<bool>? driveEscape = DriveEscape;
			if (driveEscape != null && driveEscape())
			{
				break;
			}
			if (ChromeSetupFlow.Shared.IsShowingHow)
			{
				dq.TryEnqueue(delegate
				{
					ChromeSetupFlow.Shared.StopShowingHowForEscape();
				});
				break;
			}
			Func<bool>? lessonReplaying = LessonReplaying;
			if (lessonReplaying != null && lessonReplaying())
			{
				Action stopReplay = StopLessonReplay;
				if (stopReplay != null)
				{
					dq.TryEnqueue(delegate
					{
						stopReplay();
					});
					break;
				}
			}
			ProactiveCoordinator proactive = ProactiveCoordinator.Current;
			if (proactive != null && (proactive.AdjustingCardId.HasValue || proactive.IsRemoteTaskVoiceActive))
			{
				dq.TryEnqueue(delegate
				{
					proactive.CancelRemoteTaskVoice();
					proactive.CancelAdjustingFromEscape();
				});
			}
			else
			{
				if (real.Dictation.CancelActiveSession())
				{
					break;
				}
				Func<bool>? notchViewerEscapeWouldClaim = NotchViewerEscapeWouldClaim;
				if (notchViewerEscapeWouldClaim != null && notchViewerEscapeWouldClaim())
				{
					Func<bool> closeViewer = NotchEscape;
					if (closeViewer != null)
					{
						dq.TryEnqueue(delegate
						{
							closeViewer();
						});
						break;
					}
				}
				if (pressStarted == 0L && real.Companion.IsOnboardingAnswerInFlight)
				{
					break;
				}
				Func<bool>? notchEscapeWouldClaim = NotchEscapeWouldClaim;
				if (notchEscapeWouldClaim != null && notchEscapeWouldClaim())
				{
					Func<bool> notchEscape = NotchEscape;
					if (notchEscape != null)
					{
						dq.TryEnqueue(delegate
						{
							notchEscape();
						});
						break;
					}
				}
				bool num = s.Companion.State != VoiceState.Idle;
				NotchPresentation presentation = s.Presenter.Presentation;
				flag = ((presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse) ? true : false);
				bool flag3 = flag;
				if (!num && !flag3 && !real.Canvas.IsShowing)
				{
					break;
				}
				real.Canvas.PlayClearSoundIfShowingDrawing();
				s.Companion.Cancel();
				real.Highlight.Clear();
				if (flag3)
				{
					dq.TryEnqueue(delegate
					{
						s.Presenter.Present(new NotchPresentation.Idle());
					});
				}
			}
			break;
		}
		case ShortcutAction.HandsFreeDictationToggle:
			real.Dictation.HandleShortcut(action);
			break;
		}
	}

	public void Dispose()
	{
		s.Shortcuts.ShortcutTriggered -= OnShortcut;
	}
}
