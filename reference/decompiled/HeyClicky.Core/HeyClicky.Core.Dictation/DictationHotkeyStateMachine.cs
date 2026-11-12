using System;

namespace HeyClicky.Core.Dictation;

public sealed class DictationHotkeyStateMachine
{
	public static readonly TimeSpan QuickTapMaxHold = TimeSpan.FromSeconds(0.4);

	public static readonly TimeSpan LockDoubleTapMaxGap = TimeSpan.FromSeconds(0.4);

	public static readonly TimeSpan KeyBounceWindow = TimeSpan.FromSeconds(0.08);

	private readonly IDictationScheduler scheduler;

	private readonly Func<bool> isSessionActive;

	private readonly Action begin;

	private readonly Action end;

	private readonly Action lockedHandsFree;

	private DateTimeOffset? holdPressStartTime;

	private IDisposable? pendingQuickTapFinish;

	private IDisposable? pendingBounceRelease;

	private bool shouldIgnoreNextReleaseAfterLockStop;

	public DictationEngagement Engagement { get; private set; }

	public bool SessionUsedHandsFree { get; private set; }

	public bool IsDedicatedHandsFreeShortcutConfigured { get; set; }

	public Func<bool> IsOnboardingTutorialActive { get; set; } = () => false;

	public bool HasPendingQuickTapFinish => pendingQuickTapFinish != null;

	public bool HasPendingBounceRelease => pendingBounceRelease != null;

	public DictationHotkeyStateMachine(IDictationScheduler scheduler, Func<bool> isSessionActive, Action begin, Action end, Action lockedHandsFree)
	{
		this.scheduler = scheduler;
		this.isSessionActive = isSessionActive;
		this.begin = begin;
		this.end = end;
		this.lockedHandsFree = lockedHandsFree;
	}

	public void Pressed()
	{
		if (pendingBounceRelease != null)
		{
			pendingBounceRelease.Dispose();
			pendingBounceRelease = null;
			Log.Info("Dictation", "key bounce ignored; still holding");
		}
		else if (pendingQuickTapFinish != null)
		{
			pendingQuickTapFinish.Dispose();
			pendingQuickTapFinish = null;
			holdPressStartTime = null;
			Engagement = DictationEngagement.HandsFreeLocked;
			SessionUsedHandsFree = true;
			lockedHandsFree();
		}
		else if (Engagement == DictationEngagement.HandsFreeLocked)
		{
			Engagement = DictationEngagement.Idle;
			shouldIgnoreNextReleaseAfterLockStop = true;
			end();
		}
		else if (Engagement == DictationEngagement.Idle)
		{
			bool flag = isSessionActive();
			begin();
			if (isSessionActive() && !flag)
			{
				Engagement = DictationEngagement.Holding;
				SessionUsedHandsFree = false;
				holdPressStartTime = scheduler.Now;
			}
		}
	}

	public void Released()
	{
		if (shouldIgnoreNextReleaseAfterLockStop)
		{
			shouldIgnoreNextReleaseAfterLockStop = false;
		}
		else
		{
			if (Engagement != DictationEngagement.Holding)
			{
				return;
			}
			DateTimeOffset? dateTimeOffset = holdPressStartTime;
			TimeSpan timeSpan;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				timeSpan = scheduler.Now - valueOrDefault;
			}
			else
			{
				timeSpan = QuickTapMaxHold + TimeSpan.FromSeconds(1L);
			}
			TimeSpan timeSpan2 = timeSpan;
			if (timeSpan2 < KeyBounceWindow && pendingBounceRelease == null)
			{
				pendingBounceRelease = scheduler.Schedule(KeyBounceWindow, delegate
				{
					if (pendingBounceRelease != null)
					{
						pendingBounceRelease = null;
						Released();
					}
				});
				return;
			}
			holdPressStartTime = null;
			if (timeSpan2 >= QuickTapMaxHold || IsDedicatedHandsFreeShortcutConfigured || IsOnboardingTutorialActive())
			{
				Engagement = DictationEngagement.Idle;
				end();
				return;
			}
			pendingQuickTapFinish = scheduler.Schedule(LockDoubleTapMaxGap, delegate
			{
				if (pendingQuickTapFinish != null)
				{
					pendingQuickTapFinish = null;
					Engagement = DictationEngagement.Idle;
					end();
				}
			});
		}
	}

	public void ToggleHandsFree()
	{
		switch (Engagement)
		{
		case DictationEngagement.HandsFreeLocked:
			Engagement = DictationEngagement.Idle;
			end();
			break;
		case DictationEngagement.Holding:
			pendingQuickTapFinish?.Dispose();
			pendingQuickTapFinish = null;
			pendingBounceRelease?.Dispose();
			pendingBounceRelease = null;
			holdPressStartTime = null;
			Engagement = DictationEngagement.HandsFreeLocked;
			SessionUsedHandsFree = true;
			lockedHandsFree();
			break;
		case DictationEngagement.Idle:
		{
			bool flag = isSessionActive();
			begin();
			if (isSessionActive() && !flag)
			{
				Engagement = DictationEngagement.HandsFreeLocked;
				SessionUsedHandsFree = true;
				lockedHandsFree();
			}
			break;
		}
		}
	}

	public void Reset()
	{
		pendingQuickTapFinish?.Dispose();
		pendingQuickTapFinish = null;
		pendingBounceRelease?.Dispose();
		pendingBounceRelease = null;
		holdPressStartTime = null;
		shouldIgnoreNextReleaseAfterLockStop = false;
		Engagement = DictationEngagement.Idle;
	}
}
