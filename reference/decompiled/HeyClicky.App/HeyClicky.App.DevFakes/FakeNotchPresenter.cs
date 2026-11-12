using System;
using System.Threading;

namespace HeyClicky.App.DevFakes;

public sealed class FakeNotchPresenter : INotchPresenter
{
	private readonly object gate = new object();

	private NotchPresentation presentation = new NotchPresentation.Idle();

	private Timer? autoDismiss;

	private int generation;

	public NotchPresentation Presentation
	{
		get
		{
			lock (gate)
			{
				return presentation;
			}
		}
	}

	public bool IsHovering { get; set; }

	public event EventHandler? PresentationChanged;

	public void Present(NotchPresentation p)
	{
		lock (gate)
		{
			presentation = p;
			generation++;
			autoDismiss?.Dispose();
			autoDismiss = null;
		}
		this.PresentationChanged?.Invoke(this, EventArgs.Empty);
	}

	public void ShowLiveEvent(NotchLiveEvent liveEvent, TimeSpan? autoDismissAfter = null)
	{
		Present(new NotchPresentation.LiveEvent(liveEvent));
		if (!autoDismissAfter.HasValue)
		{
			return;
		}
		TimeSpan valueOrDefault = autoDismissAfter.GetValueOrDefault();
		int gen;
		lock (gate)
		{
			gen = generation;
		}
		autoDismiss = new Timer(delegate
		{
			lock (gate)
			{
				if (gen != generation)
				{
					return;
				}
			}
			ReturnToIdle();
		}, null, valueOrDefault, Timeout.InfiniteTimeSpan);
	}

	public void DismissLiveEvent(Type liveEventType)
	{
		if (Presentation is NotchPresentation.LiveEvent liveEvent && liveEventType.IsInstanceOfType(liveEvent.Event))
		{
			ReturnToIdle();
		}
	}

	public void ReturnToIdle()
	{
		Present(new NotchPresentation.Idle());
	}
}
