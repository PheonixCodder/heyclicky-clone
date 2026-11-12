using System;
using System.Diagnostics;
using System.Threading;

namespace HeyClicky.Platform.Input;

public sealed class PointerMoveFeed : IDisposable, ILowLevelInputSink
{
	private readonly Action onMove;

	private readonly long minimumIntervalTicks;

	private readonly Timer deliveryTimer;

	private long lastDeliveredAt;

	private int isDeliveryScheduled;

	private int disposed;

	public PointerMoveFeed(double maximumRateHz, Action onMove)
	{
		this.onMove = onMove;
		minimumIntervalTicks = (long)((double)Stopwatch.Frequency / Math.Max(1.0, maximumRateHz));
		deliveryTimer = new Timer(delegate
		{
			Deliver();
		}, null, -1, -1);
		LowLevelInputHub.Shared.AddSink(this);
		LowLevelInputHub.Shared.Acquire();
	}

	void ILowLevelInputSink.OnKey(in LowLevelKeyEvent keyEvent)
	{
	}

	void ILowLevelInputSink.OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs)
	{
	}

	void ILowLevelInputSink.OnRawMouse(ushort buttonFlags, bool isPhysical, nuint extraInfo)
	{
		if (Volatile.Read(in disposed) != 1 && Interlocked.Exchange(ref isDeliveryScheduled, 1) != 1)
		{
			long num = Volatile.Read(in lastDeliveredAt) + minimumIntervalTicks - Stopwatch.GetTimestamp();
			long dueTime = ((num <= 0) ? 0 : ((num * 1000 + Stopwatch.Frequency - 1) / Stopwatch.Frequency));
			deliveryTimer.Change(dueTime, -1L);
		}
	}

	private void Deliver()
	{
		Volatile.Write(ref lastDeliveredAt, Stopwatch.GetTimestamp());
		Volatile.Write(ref isDeliveryScheduled, 0);
		if (Volatile.Read(in disposed) == 1)
		{
			return;
		}
		try
		{
			onMove();
		}
		catch (Exception ex)
		{
			Log.Warn("PointerMoveFeed", "move handler failed: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 1)
		{
			LowLevelInputHub.Shared.RemoveSink(this);
			LowLevelInputHub.Shared.Release();
			deliveryTimer.Dispose();
		}
	}
}
