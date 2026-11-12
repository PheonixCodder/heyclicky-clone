using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

public sealed class HighlightOverlay : IHighlightOverlay, IDisposable
{
	private readonly OverlayHost host;

	private readonly HighlightLayer layer;

	private bool disposed;

	public bool IsEnabled { get; set; } = true;

	public nint WindowHandle => host.WindowHandle;

	public HighlightOverlay()
	{
		host = OverlayHost.Acquire();
		layer = host.Invoke(delegate
		{
			HighlightLayer result = new HighlightLayer(host);
			host.AddLayer(result);
			return result;
		});
	}

	public void BeginWatch(string colorHex)
	{
		if (IsEnabled)
		{
			Color4 color = CursorOverlay.ParseHexColor(colorHex);
			host.Post(delegate
			{
				layer.BeginWatching(color);
			});
		}
	}

	public IReadOnlyList<HighlightStroke> EndWatch()
	{
		return host.Invoke(layer.EndWatching);
	}

	public void Clear()
	{
		host.Post(layer.Clear);
	}

	public void SnapshotInkPng(string path, string backgroundHex = "#F5F5F0")
	{
		host.Invoke(delegate
		{
			layer.SnapshotPng(path, CursorOverlay.ParseHexColor(backgroundHex), (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
			return 0;
		});
	}

	internal void Nudge()
	{
		host.Post(host.RequestFrames);
	}

	public (long Renders, int PointCount, bool Watching) GetDiagnostics()
	{
		return host.Invoke(() => (RenderCount: layer.RenderCount, PointCount: layer.PointCount, IsWatching: layer.IsWatching));
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			host.Invoke(delegate
			{
				host.RemoveLayer(layer);
				layer.Dispose();
				return 0;
			});
			OverlayHost.Release();
		}
	}
}
