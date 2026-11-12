using System;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Platform.Overlay;

namespace HeyClicky.Platform.Input;

public sealed class PointerInput : IPointerInput
{
	private sealed class PressWatch : IDisposable, ILowLevelInputSink, IThreadPoolWorkItem
	{
		private struct NativePoint
		{
			public int X;

			public int Y;
		}

		private readonly Action<PointD> onPress;

		private readonly int ownProcessId = Environment.ProcessId;

		private long pending;

		private int queued;

		private int disposed;

		private readonly bool includeOwnWindows;

		private const uint GaRoot = 2u;

		public PressWatch(Action<PointD> onPress, bool includeOwnWindows)
		{
			this.onPress = onPress;
			this.includeOwnWindows = includeOwnWindows;
			LowLevelInputHub.Shared.AddSink(this);
			LowLevelInputHub.Shared.Acquire();
		}

		void ILowLevelInputSink.OnKey(in LowLevelKeyEvent keyEvent)
		{
		}

		void ILowLevelInputSink.OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs)
		{
			bool flag = kind <= LowLevelMouseButtonKind.Right;
			if (flag && Volatile.Read(in disposed) != 1)
			{
				long value = (long)(0x4000000000000000L | (((ulong)x & 0x7FFFFFFFuL) << 31) | ((ulong)y & 0x7FFFFFFFuL));
				Interlocked.Exchange(ref pending, value);
				if (Interlocked.Exchange(ref queued, 1) == 0)
				{
					ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
				}
			}
		}

		void IThreadPoolWorkItem.Execute()
		{
			Volatile.Write(ref queued, 0);
			long num = Interlocked.Exchange(ref pending, 0L);
			if (num == 0L || Volatile.Read(in disposed) == 1)
			{
				return;
			}
			int num2 = SignExtend31((num >> 31) & 0x7FFFFFFF);
			int num3 = SignExtend31(num & 0x7FFFFFFF);
			NativePoint point = default(NativePoint);
			point.X = num2;
			point.Y = num3;
			nint ancestor = GetAncestor(WindowFromPoint(point), 2u);
			if (ancestor != 0 && !includeOwnWindows)
			{
				GetWindowThreadProcessId(ancestor, out var processId);
				if (processId == (uint)ownProcessId)
				{
					return;
				}
			}
			try
			{
				onPress(new PointD(num2, num3));
			}
			catch (Exception ex)
			{
				Log.Warn("PointerInput", "press handler failed: " + ex.Message);
			}
		}

		private static int SignExtend31(long value)
		{
			return (int)(value << 33 >> 33);
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref disposed, 1) != 1)
			{
				LowLevelInputHub.Shared.RemoveSink(this);
				LowLevelInputHub.Shared.Release();
			}
		}

		[DllImport("user32.dll")]
		private static extern nint WindowFromPoint(NativePoint point);

		[DllImport("user32.dll")]
		private static extern nint GetAncestor(nint window, uint flags);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
	}

	private const int VkLButton = 1;

	private const int VkRButton = 2;

	private const int VkMButton = 4;

	public PointD CursorPosition => DisplayEnumeratorFast.CursorPositionPx();

	public bool IsAnyButtonDown
	{
		get
		{
			if ((GetAsyncKeyState(1) & 0x8000) == 0 && (GetAsyncKeyState(2) & 0x8000) == 0)
			{
				return (GetAsyncKeyState(4) & 0x8000) != 0;
			}
			return true;
		}
	}

	public IDisposable WatchPresses(Action<PointD> onPress)
	{
		return new PressWatch(onPress, includeOwnWindows: false);
	}

	public static IDisposable WatchAllPresses(Action<PointD> onPress)
	{
		return new PressWatch(onPress, includeOwnWindows: true);
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int key);
}
