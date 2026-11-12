using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Platform.Overlay;

namespace HeyClicky.App.Wiring;

public sealed class DeferredCursorOverlay : ICursorOverlay, IDisposable
{
	private volatile CursorOverlay? inner;

	private string? color;

	private bool shown;

	private readonly object gate = new object();

	private bool shakeToDetach;

	private bool dockedAtStart;

	public CursorOverlay? Inner => inner;

	public bool ShakeToDetachEnabled
	{
		get
		{
			return shakeToDetach;
		}
		set
		{
			lock (gate)
			{
				shakeToDetach = value;
				CursorOverlay cursorOverlay = inner;
				if (cursorOverlay != null)
				{
					cursorOverlay.ShakeToDetachEnabled = value;
				}
			}
		}
	}

	public bool IsDetached => inner?.IsDetached ?? false;

	public bool IsDocked => inner?.IsDocked ?? dockedAtStart;

	public bool IsSayingGoodbye => inner?.IsSayingGoodbye ?? false;

	public event Action<CursorOverlay>? Attached;

	public event Action? DetachedCursorClicked;

	public event Action? CursorDetached;

	public event Action? DetachedCursorAutoReattached;

	public void Attach(CursorOverlay real)
	{
		lock (gate)
		{
			inner = real;
			if (color != null)
			{
				real.SetColor(color);
			}
			real.ShakeToDetachEnabled = shakeToDetach;
			real.DetachedCursorClicked += delegate
			{
				this.DetachedCursorClicked?.Invoke();
			};
			real.CursorDetached += delegate
			{
				this.CursorDetached?.Invoke();
			};
			real.DetachedCursorAutoReattached += delegate
			{
				this.DetachedCursorAutoReattached?.Invoke();
			};
			if (dockedAtStart)
			{
				real.DockAsync(default(PointD));
			}
			if (shown)
			{
				real.Show();
			}
		}
		this.Attached?.Invoke(real);
	}

	public Task ShowMessageAsync(string text, bool streamed = true, double holdSeconds = 6.0, CancellationToken ct = default(CancellationToken))
	{
		return inner?.ShowMessageAsync(text, streamed, holdSeconds, ct) ?? Task.CompletedTask;
	}

	public void HideMessage()
	{
		inner?.HideMessage();
	}

	public Task PlayWelcomeAsync()
	{
		return inner?.PlayWelcomeAsync() ?? Task.CompletedTask;
	}

	public void EndDetach()
	{
		inner?.EndDetach();
	}

	public PointD? RenderedPositionPx()
	{
		return inner?.RenderedPositionPx();
	}

	public Task DockAsync(PointD anchorPx)
	{
		lock (gate)
		{
			if (inner == null)
			{
				dockedAtStart = true;
				return Task.CompletedTask;
			}
		}
		return inner.DockAsync(anchorPx);
	}

	public Task SayGoodbyeAndDockAsync(string text)
	{
		lock (gate)
		{
			if (inner == null)
			{
				dockedAtStart = true;
				return Task.CompletedTask;
			}
		}
		return inner.SayGoodbyeAndDockAsync(text);
	}

	public Task UndockAsync(PointD anchorPx)
	{
		lock (gate)
		{
			if (inner == null)
			{
				dockedAtStart = false;
				return Task.CompletedTask;
			}
		}
		return inner.UndockAsync(anchorPx);
	}

	public void Show()
	{
		lock (gate)
		{
			shown = true;
			inner?.Show();
		}
	}

	public void Hide()
	{
		lock (gate)
		{
			shown = false;
			inner?.Hide();
		}
	}

	public void SetColor(string hex)
	{
		lock (gate)
		{
			color = hex;
			inner?.SetColor(hex);
		}
	}

	public void SetVoiceState(VoiceState state, float level)
	{
		inner?.SetVoiceState(state, level);
	}

	public Task PointAtAsync(PointD screenPx, string? label, CancellationToken ct = default(CancellationToken))
	{
		return inner?.PointAtAsync(screenPx, label, ct) ?? Task.CompletedTask;
	}

	public void ReturnToCursor()
	{
		inner?.ReturnToCursor();
	}

	public void SetHoverDwell(double? seconds)
	{
		inner?.SetHoverDwell(seconds);
	}

	public void RidePen(IReadOnlyList<PenStrokePath> strokes)
	{
		inner?.RidePen(strokes);
	}

	public void EndPenRide()
	{
		inner?.EndPenRide();
	}

	public void ShowClickTarget(ClickTargetRing ring)
	{
		inner?.ShowClickTarget(ring);
	}

	public void HideClickTarget()
	{
		inner?.HideClickTarget();
	}

	public void ShowMessage(string text)
	{
		inner?.ShowMessage(text);
	}

	public void Dispose()
	{
		inner?.Dispose();
	}
}
