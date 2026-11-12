using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Overlay;

public sealed class CursorOverlay : ICursorOverlay, IDisposable
{
	public static class Colors
	{
		public const string Red = "#FF4C4C";

		public const string Blue = "#3380FF";

		public const string Yellow = "#D4A017";

		public const string Green = "#26C281";

		public const string Default = "#3380FF";
	}

	private readonly OverlayHost host;

	private readonly CursorLayer layer;

	private readonly ClickTargetLayer clickTarget;

	private bool disposed;

	public nint WindowHandle => host.WindowHandle;

	public bool ShakeToDetachEnabled
	{
		get
		{
			return layer.ShakeToDetachEnabled;
		}
		set
		{
			layer.ShakeToDetachEnabled = value;
		}
	}

	public bool IsDetached => layer.IsDetached;

	public bool IsDocked => layer.IsDocked;

	public bool IsSayingGoodbye => layer.IsSayingGoodbye;

	public event Action? DetachedCursorClicked;

	public event Action? CursorDetached;

	public event Action? DetachedCursorAutoReattached;

	public CursorOverlay()
	{
		host = OverlayHost.Acquire();
		(layer, clickTarget) = host.Invoke(delegate
		{
			CursorLayer item = new CursorLayer(host)
			{
				DetachedClicked = delegate
				{
					Task.Run(delegate
					{
						try
						{
							this.DetachedCursorClicked?.Invoke();
						}
						catch (Exception ex)
						{
							Log.Error("Overlay", "DetachedCursorClicked handler threw", ex);
						}
					});
				},
				DetachBegan = delegate
				{
					Task.Run(delegate
					{
						try
						{
							this.CursorDetached?.Invoke();
						}
						catch (Exception ex2)
						{
							Log.Error("Overlay", "CursorDetached handler threw", ex2);
						}
					});
				},
				DetachAutoReattached = delegate
				{
					Task.Run(delegate
					{
						try
						{
							this.DetachedCursorAutoReattached?.Invoke();
						}
						catch (Exception ex3)
						{
							Log.Error("Overlay", "DetachedCursorAutoReattached handler threw", ex3);
						}
					});
				}
			};
			host.AddLayer(item);
			ClickTargetLayer item2 = new ClickTargetLayer(host);
			host.AddLayer(item2);
			return (created: item, target: item2);
		});
	}

	public void Show()
	{
		host.Post(layer.Show);
	}

	public void Hide()
	{
		host.Post(layer.Hide);
	}

	public void SetColor(string hex)
	{
		Color4 color = ParseHexColor(hex);
		host.Post(delegate
		{
			layer.SetTint(color);
			clickTarget.SetTint(color, hex);
		});
	}

	public void SetVoiceState(VoiceState state, float level)
	{
		layer.LatestLevel = level;
		if (layer.RequestedVoiceState != state)
		{
			layer.RequestedVoiceState = state;
			host.Post(layer.ApplyRequestedVoiceState);
		}
	}

	public Task PointAtAsync(PointD screenPx, string? label, CancellationToken ct = default(CancellationToken))
	{
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.PointAt(screenPx, label, completion);
		});
		if (ct.CanBeCanceled)
		{
			CancellationTokenRegistration registration = ct.Register(delegate
			{
				host.Post(delegate
				{
					layer.ReturnToCursor();
					completion.TrySetCanceled(ct);
				});
			});
			completion.Task.ContinueWith(delegate
			{
				registration.Dispose();
			}, TaskScheduler.Default);
		}
		return completion.Task;
	}

	public void ReturnToCursor()
	{
		host.Post(layer.ReturnToCursor);
	}

	public void SetHoverDwell(double? seconds)
	{
		host.Post(delegate
		{
			layer.SetHoverDwell(seconds);
		});
	}

	public void ShowMessage(string text)
	{
		ShowMessageAsync(text);
	}

	public void RidePen(IReadOnlyList<PenStrokePath> strokes)
	{
		if (strokes.Count != 0)
		{
			host.Post(delegate
			{
				layer.RidePen(strokes);
			});
		}
	}

	public void EndPenRide()
	{
		host.Post(layer.EndPenRide);
	}

	public void ShowClickTarget(ClickTargetRing ring)
	{
		host.Post(delegate
		{
			clickTarget.Show(ring);
		});
	}

	public void HideClickTarget()
	{
		host.Post(clickTarget.Hide);
	}

	public Task ShowMessageAsync(string text, bool streamed = true, double holdSeconds = 6.0, CancellationToken ct = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return Task.CompletedTask;
		}
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.ShowMessage(text.Trim(), streamed, holdSeconds, completion);
		});
		if (ct.CanBeCanceled)
		{
			CancellationTokenRegistration registration = ct.Register(delegate
			{
				host.Post(delegate
				{
					layer.HideMessageIfOwnedBy(completion);
				});
			});
			completion.Task.ContinueWith(delegate
			{
				registration.Dispose();
			}, TaskScheduler.Default);
		}
		return completion.Task;
	}

	public void HideMessage()
	{
		host.Post(delegate
		{
			layer.HideMessage();
		});
	}

	public Task PlayWelcomeAsync()
	{
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.PlayWelcome(completion);
		});
		return completion.Task;
	}

	public void EndDetach()
	{
		host.Post(layer.EndDetach);
	}

	public PointD? RenderedPositionPx()
	{
		try
		{
			return host.Invoke(() => (!layer.IsDocked && !(layer.CurrentOpacity <= 0.01)) ? new PointD?(layer.PositionPx) : ((PointD?)null), 200);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public Task DockAsync(PointD anchorPx)
	{
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.Dock(completion);
		});
		return completion.Task;
	}

	public Task SayGoodbyeAndDockAsync(string text)
	{
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.SayGoodbyeThenDock(text, completion);
		});
		return completion.Task;
	}

	public Task UndockAsync(PointD anchorPx)
	{
		TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		host.Post(delegate
		{
			layer.Undock(anchorPx, completion);
		});
		return completion.Task;
	}

	public (string MessageText, double MessageOpacity, bool Detached, bool DetachHovered, bool Docked, double Opacity) GetStateDiagnostics()
	{
		return host.Invoke(() => (MessageText: layer.MessageText, MessageOpacity: layer.MessageOpacity, IsDetached: layer.IsDetached, DetachHovered: layer.DetachHovered, IsDocked: layer.IsDocked, CurrentOpacity: layer.CurrentOpacity));
	}

	public void SnapshotMessagePng(string path, string backgroundHex = "#F5F5F0")
	{
		host.Invoke(delegate
		{
			layer.SnapshotMessagePng(path, ParseHexColor(backgroundHex));
			return 0;
		});
	}

	public (ClickTargetRing? Ring, double RevealProgress, bool IsPulsing) GetClickTargetDiagnostics()
	{
		return host.Invoke(() => (Ring: clickTarget.Ring, RevealProgress: clickTarget.RevealProgress, IsPulsing: clickTarget.IsPulsing));
	}

	public void SnapshotClickTargetPng(string path, string backgroundHex = "#F5F5F0")
	{
		host.Invoke(delegate
		{
			clickTarget.SnapshotPng(path, ParseHexColor(backgroundHex));
			return 0;
		});
	}

	internal void Nudge()
	{
		host.Post(host.RequestFrames);
	}

	public void SnapshotClusterPng(string path, string backgroundHex = "#F5F5F0")
	{
		host.Invoke(delegate
		{
			layer.SnapshotPng(path, ParseHexColor(backgroundHex));
			return 0;
		});
	}

	public (long HostFrames, long ClusterRenders, PointD PositionPx, string Mode) GetDiagnostics()
	{
		return host.Invoke(() => (FramesRendered: host.FramesRendered, RenderCount: layer.RenderCount, PositionPx: layer.PositionPx, ModeName: layer.ModeName));
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
				host.RemoveLayer(clickTarget);
				clickTarget.Dispose();
				return 0;
			});
			OverlayHost.Release();
		}
	}

	internal static Color4 ParseHexColor(string hex)
	{
		string text = hex.TrimStart('#');
		if (text.Length == 6 && uint.TryParse(text, NumberStyles.HexNumber, null, out var result))
		{
			return new Color4((float)((result >> 16) & 0xFF) / 255f, (float)((result >> 8) & 0xFF) / 255f, (float)(result & 0xFF) / 255f);
		}
		return new Color4(0.2f, 0.5019608f, 1f);
	}
}
