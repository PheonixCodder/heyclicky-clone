using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky;

public interface ICursorOverlay : IDisposable
{
	bool ShakeToDetachEnabled
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	bool IsDetached => false;

	bool IsDocked => false;

	bool IsSayingGoodbye => false;

	event Action? DetachedCursorClicked
	{
		add
		{
		}
		remove
		{
		}
	}

	event Action? CursorDetached
	{
		add
		{
		}
		remove
		{
		}
	}

	event Action? DetachedCursorAutoReattached
	{
		add
		{
		}
		remove
		{
		}
	}

	void Show();

	void Hide();

	void SetColor(string hex);

	void SetVoiceState(VoiceState state, float level);

	Task PointAtAsync(PointD screenPx, string? label, CancellationToken ct = default(CancellationToken));

	void ReturnToCursor();

	void SetHoverDwell(double? seconds)
	{
	}

	void RidePen(IReadOnlyList<PenStrokePath> strokes)
	{
	}

	void EndPenRide()
	{
	}

	void ShowClickTarget(ClickTargetRing ring)
	{
	}

	void HideClickTarget()
	{
	}

	void ShowMessage(string text)
	{
		ShowMessageAsync(text);
	}

	Task ShowMessageAsync(string text, bool streamed = true, double holdSeconds = 6.0, CancellationToken ct = default(CancellationToken))
	{
		return Task.CompletedTask;
	}

	void HideMessage()
	{
	}

	Task PlayWelcomeAsync()
	{
		return Task.CompletedTask;
	}

	void EndDetach()
	{
	}

	Task DockAsync(PointD anchorPx)
	{
		return Task.CompletedTask;
	}

	Task SayGoodbyeAndDockAsync(string text)
	{
		return DockAsync(default(PointD));
	}

	Task UndockAsync(PointD anchorPx)
	{
		return Task.CompletedTask;
	}
}
