using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.App.DevFakes;

public sealed class FakeCursorOverlay : ICursorOverlay, IDisposable
{
	public void Show()
	{
	}

	public void Hide()
	{
	}

	public void SetColor(string hex)
	{
	}

	public void SetVoiceState(VoiceState state, float level)
	{
	}

	public Task PointAtAsync(PointD screenPx, string? label, CancellationToken ct = default(CancellationToken))
	{
		return Task.CompletedTask;
	}

	public void ReturnToCursor()
	{
	}

	public void Dispose()
	{
	}
}
