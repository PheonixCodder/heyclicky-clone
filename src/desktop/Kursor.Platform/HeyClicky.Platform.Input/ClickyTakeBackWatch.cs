using System;
using System.Threading;
using HeyClicky.Platform.Overlay;

namespace HeyClicky.Platform.Input;

public sealed class ClickyTakeBackWatch : IDisposable, ILowLevelInputSink, IThreadPoolWorkItem
{
	public enum Kind
	{
		Move,
		Press,
		Key,
		Wheel
	}

	private readonly Action<Kind, PointD> onInput;

	private int disposed;

	private int moveQueued;

	public ClickyTakeBackWatch(Action<Kind, PointD> onInput)
	{
		this.onInput = onInput;
		LowLevelInputHub.Shared.AddSink(this);
		LowLevelInputHub.Shared.Acquire();
	}

	void ILowLevelInputSink.OnKey(in LowLevelKeyEvent keyEvent)
	{
		if (Volatile.Read(in disposed) != 1 && keyEvent.IsDown && keyEvent.ExtraInfo != 1212369995)
		{
			bool flag;
			switch (keyEvent.VirtualKey)
			{
			case 16u:
			case 17u:
			case 18u:
			case 20u:
			case 91u:
			case 92u:
			case 160u:
			case 161u:
			case 162u:
			case 163u:
			case 164u:
			case 165u:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				Report(Kind.Key);
			}
		}
	}

	void ILowLevelInputSink.OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs)
	{
	}

	void ILowLevelInputSink.OnRawMouse(ushort buttonFlags, bool isPhysical, nuint extraInfo)
	{
		if (Volatile.Read(in disposed) == 1 || !isPhysical || extraInfo == 1212369995)
		{
			return;
		}
		if (buttonFlags == 0)
		{
			if (Interlocked.Exchange(ref moveQueued, 1) == 0)
			{
				ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
			}
		}
		else if ((buttonFlags & 0x15) != 0)
		{
			Report(Kind.Press);
		}
		else if ((buttonFlags & 0xC00) != 0)
		{
			Report(Kind.Wheel);
		}
	}

	private void Report(Kind kind)
	{
		PointD pointer = DisplayEnumeratorFast.CursorPositionPx();
		ThreadPool.UnsafeQueueUserWorkItem(delegate
		{
			if (Volatile.Read(in disposed) == 1)
			{
				return;
			}
			try
			{
				onInput(kind, pointer);
			}
			catch (Exception ex)
			{
				Log.Warn("TakeBack", "handler failed: " + ex.Message);
			}
		}, null);
	}

	void IThreadPoolWorkItem.Execute()
	{
		Volatile.Write(ref moveQueued, 0);
		if (Volatile.Read(in disposed) == 1)
		{
			return;
		}
		try
		{
			onInput(Kind.Move, DisplayEnumeratorFast.CursorPositionPx());
		}
		catch (Exception ex)
		{
			Log.Warn("TakeBack", "handler failed: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 1)
		{
			LowLevelInputHub.Shared.RemoveSink(this);
			LowLevelInputHub.Shared.Release();
		}
	}
}
