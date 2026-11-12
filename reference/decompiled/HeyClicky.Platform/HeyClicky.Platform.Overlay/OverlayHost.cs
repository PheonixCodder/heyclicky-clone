using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Platform.Input;
using HeyClicky.Platform.Win32;
using SharpGen.Runtime;
using Vortice;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Overlay;

internal sealed class OverlayHost
{
	private sealed class FrameTiming
	{
		private long runStart;

		private long lastReport;

		private int frames;

		private double waitTotal;

		private double waitMax;

		private double workTotal;

		private double workMax;

		private long gpuWaitsAtStart;

		public void NoteWork(double ms, long frameStart, OverlayHost host)
		{
			if (frames == 0)
			{
				runStart = frameStart;
				gpuWaitsAtStart = host.GpuWaits;
				host.LongestGpuFrameMs = 0.0;
			}
			frames++;
			workTotal += ms;
			workMax = Math.Max(workMax, ms);
		}

		public void NoteWait(double ms)
		{
			waitTotal += ms;
			waitMax = Math.Max(waitMax, ms);
		}

		public void RunEnded(OverlayHost host)
		{
			if (frames != 0)
			{
				double totalSeconds = Stopwatch.GetElapsedTime(runStart).TotalSeconds;
				int num = frames;
				frames = 0;
				double value = waitTotal / (double)num;
				double value2 = workTotal / (double)num;
				double num2 = waitMax;
				double num3 = workMax;
				waitTotal = (waitMax = (workTotal = (workMax = 0.0)));
				double num4 = (double)num / Math.Max(0.001, totalSeconds);
				bool flag = num4 >= 45.0 && num2 <= 50.0 && num3 <= 50.0;
				if (ReportEveryRunForTests || (!(totalSeconds < 0.5 || flag) && !(Stopwatch.GetElapsedTime(lastReport).TotalSeconds < 5.0)))
				{
					lastReport = Stopwatch.GetTimestamp();
					Log.Info("Overlay", $"animated {totalSeconds:0.0} s at {num4:0} frames a second: waiting for the display {value:0} ms on average ({num2:0} at most), drawing {value2:0} ms ({num3:0} at most); GPU frames waited {host.GpuWaits - gpuWaitsAtStart}, slowest GPU frame {host.LongestGpuFrameMs:0} ms");
				}
			}
		}
	}

	private static readonly object SharedGate = new object();

	private static OverlayHost? shared;

	private static int sharedReferences;

	private readonly Thread thread;

	private readonly ManualResetEventSlim ready = new ManualResetEventSlim(initialState: false);

	private readonly AutoResetEvent wakeEvent = new AutoResetEvent(initialState: false);

	private readonly ConcurrentQueue<Action> pendingWork = new ConcurrentQueue<Action>();

	private volatile bool shuttingDown;

	private HWND windowHandle;

	private static WNDPROC? windowProcedure;

	public ID3D11Device D3DDevice;

	public ID2D1Factory1 D2DFactory;

	public ID2D1Device D2DDevice;

	public ID2D1DeviceContext D2DContext;

	public IDWriteFactory DWriteFactory;

	public IDCompositionDesktopDevice CompositionDevice;

	private IDCompositionTarget compositionTarget;

	public IDCompositionVisual2 RootVisual;

	private readonly List<IOverlayLayer> layers = new List<IOverlayLayer>();

	private bool windowVisible;

	private int visibleContentRequests;

	private bool commitPending;

	public long FramesRendered;

	private int managedThreadId;

	private bool framesRequested;

	private readonly FrameTiming frameTiming = new FrameTiming();

	internal static bool ReportEveryRunForTests;

	private readonly FramePacer framePacer = new FramePacer();

	private static readonly int OffscreenShift = ((Environment.GetEnvironmentVariable("HEYCLICKY_OFFSCREEN") == "1") ? (-24000) : 0);

	private static bool drawnSinceFence;

	private IDXGIDevice2? gpuFenceDevice;

	private readonly ManualResetEvent gpuIdle = new ManualResetEvent(initialState: true);

	private bool fenceOutstanding;

	private long fenceSubmittedAt;

	public long GpuWaits;

	private long longestGpuFrameTicks;

	public nint WindowHandle => (nint)windowHandle;

	public int ThreadId { get; private set; }

	public IReadOnlyList<DisplayInfo> Displays { get; private set; } = Array.Empty<DisplayInfo>();

	public RectD VirtualScreenPx { get; private set; }

	public double LongestGpuFrameMs
	{
		get
		{
			return (double)Interlocked.Read(in longestGpuFrameTicks) * 1000.0 / (double)Stopwatch.Frequency;
		}
		set
		{
			Interlocked.Exchange(ref longestGpuFrameTicks, (long)(value * (double)Stopwatch.Frequency / 1000.0));
		}
	}

	public bool GpuCaughtUp
	{
		get
		{
			if (!fenceOutstanding)
			{
				return true;
			}
			if (!gpuIdle.WaitOne(0))
			{
				GpuWaits++;
				return false;
			}
			fenceOutstanding = false;
			return true;
		}
	}

	public static OverlayHost Acquire()
	{
		lock (SharedGate)
		{
			if (shared == null)
			{
				shared = new OverlayHost();
			}
			sharedReferences++;
			return shared;
		}
	}

	public static void Release()
	{
		lock (SharedGate)
		{
			if (--sharedReferences <= 0 && shared != null)
			{
				shared.Shutdown();
				shared = null;
			}
		}
	}

	private OverlayHost()
	{
		thread = new Thread(ThreadMain)
		{
			IsBackground = true,
			Name = "HeyClicky overlay",
			Priority = ThreadPriority.Highest
		};
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		ready.Wait(5000);
	}

	public void Post(Action action)
	{
		pendingWork.Enqueue(action);
		wakeEvent.Set();
	}

	public T Invoke<T>(Func<T> function, int timeoutMs = 2000)
	{
		if (Environment.CurrentManagedThreadId == managedThreadId)
		{
			return function();
		}
		T result = default(T);
		Exception failure = null;
		ManualResetEventSlim done = new ManualResetEventSlim(initialState: false);
		try
		{
			Post(delegate
			{
				try
				{
					result = function();
				}
				catch (Exception ex)
				{
					failure = ex;
				}
				finally
				{
					done.Set();
				}
			});
			if (!done.Wait(timeoutMs))
			{
				throw new TimeoutException("overlay thread did not respond");
			}
			if (failure != null)
			{
				throw new InvalidOperationException("overlay call failed: " + failure.Message, failure);
			}
			return result;
		}
		finally
		{
			if (done != null)
			{
				((IDisposable)done).Dispose();
			}
		}
	}

	public void AddLayer(IOverlayLayer layer)
	{
		layers.Add(layer);
		layer.OnDisplaysChanged(Displays, VirtualScreenPx);
	}

	public void RemoveLayer(IOverlayLayer layer)
	{
		layers.Remove(layer);
	}

	public void SetLayerVisible(bool visible)
	{
		visibleContentRequests = Math.Max(0, visibleContentRequests + (visible ? 1 : (-1)));
		bool flag = visibleContentRequests > 0;
		if (flag != windowVisible)
		{
			windowVisible = flag;
			if (flag)
			{
				PInvoke.SetWindowPos(windowHandle, new HWND(FullscreenGuard.TopmostInsertAfter()), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
			}
			else
			{
				PInvoke.ShowWindow(windowHandle, SHOW_WINDOW_CMD.SW_HIDE);
			}
		}
	}

	public void BringToTop()
	{
		if (windowVisible)
		{
			PInvoke.SetWindowPos(windowHandle, new HWND(FullscreenGuard.TopmostInsertAfter()), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
		}
	}

	public void RequestCommit()
	{
		commitPending = true;
	}

	public void RequestFrames()
	{
		framesRequested = true;
	}

	private void ThreadMain()
	{
		managedThreadId = Environment.CurrentManagedThreadId;
		ThreadId = (int)PInvoke.GetCurrentThreadId();
		PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
		try
		{
			CreateWindow();
			CreateGraphics();
			RefreshDisplays();
		}
		catch (Exception ex)
		{
			Log.Error("Overlay", "overlay init failed", ex);
			ready.Set();
			return;
		}
		ready.Set();
		LowLevelInputHub.Shared.SetMouseMoveWaker(wakeEvent);
		LowLevelInputHub.Shared.Acquire();
		try
		{
			RunLoop();
		}
		catch (Exception ex2)
		{
			Log.Error("Overlay", "overlay loop crashed", ex2);
		}
		finally
		{
			LowLevelInputHub.Shared.SetMouseMoveWaker(null);
			LowLevelInputHub.Shared.Release();
			DestroyGraphics();
			if (!windowHandle.IsNull)
			{
				CaptureExclusion.UnregisterCursorChrome(WindowHandle);
				PInvoke.DestroyWindow(windowHandle);
			}
		}
	}

	private unsafe void RunLoop()
	{
		bool flag = false;
		long startingTimestamp = Stopwatch.GetTimestamp();
		nint num = wakeEvent.SafeWaitHandle.DangerousGetHandle();
		while (!shuttingDown)
		{
			if (!flag)
			{
				int num2 = -1;
				bool flag2 = false;
				foreach (IOverlayLayer layer in layers)
				{
					int idleWakeIntervalMs = layer.IdleWakeIntervalMs;
					if (idleWakeIntervalMs >= 0 && (num2 < 0 || idleWakeIntervalMs < num2))
					{
						num2 = idleWakeIntervalMs;
					}
					flag2 |= layer.WantsMouseMoveWake;
				}
				if (flag2)
				{
					LowLevelInputHub.Shared.ArmMouseMoveWaker();
				}
				HANDLE hANDLE = new HANDLE(num);
				PInvoke.MsgWaitForMultipleObjectsEx(1u, &hANDLE, (num2 < 0) ? uint.MaxValue : ((uint)num2), QUEUE_STATUS_FLAGS.QS_ALLINPUT, MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS.MWMO_INPUTAVAILABLE);
			}
			if (!PumpMessages())
			{
				break;
			}
			RunPendingWork();
			if (shuttingDown)
			{
				break;
			}
			long timestamp = Stopwatch.GetTimestamp();
			double num3 = Math.Clamp(Stopwatch.GetElapsedTime(startingTimestamp, timestamp).TotalSeconds, 0.0, 0.1);
			startingTimestamp = timestamp;
			OverlayFrame frame = new OverlayFrame((double)timestamp / (double)Stopwatch.Frequency, flag ? num3 : 0.0, DisplayEnumeratorFast.CursorPositionPx());
			bool flag3 = framesRequested;
			framesRequested = false;
			if (flag)
			{
				foreach (IOverlayLayer layer2 in layers)
				{
					flag3 |= layer2.OnFrame(in frame);
				}
				FramesRendered++;
			}
			else
			{
				foreach (IOverlayLayer layer3 in layers)
				{
					flag3 |= layer3.OnIdleWake(in frame);
				}
				if (flag3)
				{
					foreach (IOverlayLayer layer4 in layers)
					{
						layer4.OnFrame(in frame);
					}
					FramesRendered++;
				}
			}
			if (commitPending)
			{
				commitPending = false;
				CompositionDevice.Commit();
			}
			if (drawnSinceFence)
			{
				FenceDrawing();
			}
			if (flag || flag3)
			{
				frameTiming.NoteWork(Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds, timestamp, this);
			}
			flag = flag3;
			if (flag)
			{
				long timestamp2 = Stopwatch.GetTimestamp();
				WaitForNextCompositorFrame(num);
				frameTiming.NoteWait(Stopwatch.GetElapsedTime(timestamp2).TotalMilliseconds);
			}
			else
			{
				frameTiming.RunEnded(this);
			}
		}
	}

	private bool PumpMessages()
	{
		MSG lpMsg;
		while ((bool)PInvoke.PeekMessage(out lpMsg, default(HWND), 0u, 0u, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
		{
			if (lpMsg.message == 18)
			{
				return false;
			}
			PInvoke.TranslateMessage(in lpMsg);
			PInvoke.DispatchMessage(in lpMsg);
		}
		return true;
	}

	private void RunPendingWork()
	{
		Action result;
		while (pendingWork.TryDequeue(out result))
		{
			try
			{
				result();
			}
			catch (Exception ex)
			{
				Log.Error("Overlay", "overlay work item failed", ex);
			}
		}
	}

	private void WaitForNextCompositorFrame(nint wakeHandle)
	{
		framePacer.WaitForNextFrame();
	}

	private unsafe void CreateWindow()
	{
		if (windowProcedure == null)
		{
			windowProcedure = WindowProcedure;
		}
		fixed (char* ptr = &"HeyClickyOverlayWindow".GetPinnableReference())
		{
			WNDCLASSEXW wNDCLASSEXW = default(WNDCLASSEXW);
			wNDCLASSEXW.cbSize = (uint)Unsafe.SizeOf<WNDCLASSEXW>();
			wNDCLASSEXW.lpfnWndProc = windowProcedure;
			wNDCLASSEXW.hInstance = (HINSTANCE)PInvoke.GetModuleHandle((string)null).DangerousGetHandle();
			wNDCLASSEXW.lpszClassName = ptr;
			WNDCLASSEXW param = wNDCLASSEXW;
			PInvoke.RegisterClassEx(in param);
		}
		RectD rectD = DisplayEnumerator.VirtualScreenPx();
		windowHandle = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_NOACTIVATE, "HeyClickyOverlayWindow", "HeyClicky Overlay", WINDOW_STYLE.WS_POPUP, (int)rectD.X + OffscreenShift, (int)rectD.Y + OffscreenShift, (int)rectD.Width, (int)rectD.Height, HWND.Null, null, null, null);
		if (windowHandle.IsNull)
		{
			throw new InvalidOperationException($"CreateWindowEx failed ({Marshal.GetLastWin32Error()})");
		}
		PInvoke.SetLayeredWindowAttributes(windowHandle, new COLORREF(0u), byte.MaxValue, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
		CaptureExclusion.RegisterCursorChrome((nint)windowHandle.Value, Post);
		NonRudeWindow.Mark((nint)windowHandle.Value);
		ScreenCaptureYield.Register((nint)windowHandle.Value);
	}

	private LRESULT WindowProcedure(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
	{
		switch (message)
		{
		case 132u:
			return new LRESULT(-1);
		case 33u:
			return new LRESULT(3);
		case 736u:
			return new LRESULT(0);
		case 126u:
		{
			OverlayHost overlayHost = shared;
			overlayHost?.Post(overlayHost.RefreshDisplays);
			break;
		}
		}
		return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
	}

	private void RefreshDisplays()
	{
		Displays = DisplayEnumerator.GetDisplays();
		VirtualScreenPx = DisplayEnumerator.VirtualScreenPx(Displays);
		PInvoke.SetWindowPos(windowHandle, new HWND(FullscreenGuard.TopmostInsertAfter()), (int)VirtualScreenPx.X + OffscreenShift, (int)VirtualScreenPx.Y + OffscreenShift, (int)VirtualScreenPx.Width, (int)VirtualScreenPx.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
		foreach (IOverlayLayer layer in layers)
		{
			layer.OnDisplaysChanged(Displays, VirtualScreenPx);
		}
		RequestCommit();
	}

	private void CreateGraphics()
	{
		Vortice.Direct3D.FeatureLevel[] featureLevels = new Vortice.Direct3D.FeatureLevel[4]
		{
			Vortice.Direct3D.FeatureLevel.Level_11_1,
			Vortice.Direct3D.FeatureLevel.Level_11_0,
			Vortice.Direct3D.FeatureLevel.Level_10_1,
			Vortice.Direct3D.FeatureLevel.Level_10_0
		};
		D3D11.D3D11CreateDevice((IDXGIAdapter?)null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, featureLevels, out ID3D11Device device, out ID3D11DeviceContext immediateContext).CheckError();
		immediateContext.Dispose();
		D3DDevice = device;
		using IDXGIDevice iDXGIDevice = D3DDevice.QueryInterface<IDXGIDevice>();
		Result result = iDXGIDevice.SetGPUThreadPriority(7);
		Log.Info("Overlay", "GPU thread priority: " + (result.Success ? "raised to 7" : $"left as it was (0x{result.Code:X8})"));
		gpuFenceDevice = D3DDevice.QueryInterfaceOrNull<IDXGIDevice2>();
		D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
		D2DDevice = D2DFactory.CreateDevice(iDXGIDevice);
		D2DContext = D2DDevice.CreateDeviceContext(DeviceContextOptions.None);
		DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
		if (NativeComposition.DCompositionCreateDevice3(D2DDevice.NativePointer, typeof(IDCompositionDesktopDevice).GUID, out var compositionDevice) < 0)
		{
			Marshal.ThrowExceptionForHR(NativeComposition.DCompositionCreateDevice2(D2DDevice.NativePointer, typeof(IDCompositionDesktopDevice).GUID, out compositionDevice));
		}
		CompositionDevice = new IDCompositionDesktopDevice(compositionDevice);
		CompositionDevice.CreateTargetForHwnd((nint)windowHandle, true, out compositionTarget).CheckError();
		RootVisual = CompositionDevice.CreateVisual();
		compositionTarget.SetRoot(RootVisual);
		CompositionDevice.Commit();
	}

	private void DestroyGraphics()
	{
		RootVisual?.Dispose();
		compositionTarget?.Dispose();
		CompositionDevice?.Dispose();
		DWriteFactory?.Dispose();
		D2DContext?.Dispose();
		D2DDevice?.Dispose();
		D2DFactory?.Dispose();
		D3DDevice?.Dispose();
	}

	public static ID2D1DeviceContext BeginDraw(IDCompositionSurface surface, RawRect? updateRect, out Int2 offset)
	{
		drawnSinceFence = true;
		return surface.BeginDraw<ID2D1DeviceContext>(updateRect, out offset);
	}

	private void NoteGpuFrame(long submittedAt)
	{
		long num = Stopwatch.GetTimestamp() - submittedAt;
		long num2;
		while (num > (num2 = Interlocked.Read(in longestGpuFrameTicks)) && Interlocked.CompareExchange(ref longestGpuFrameTicks, num, num2) != num2)
		{
		}
	}

	private void FenceDrawing()
	{
		drawnSinceFence = false;
		if ((object)gpuFenceDevice != null)
		{
			gpuIdle.Reset();
			try
			{
				gpuFenceDevice.EnqueueSetEvent(gpuIdle.SafeWaitHandle.DangerousGetHandle());
			}
			catch (Exception ex)
			{
				Log.Warn("Overlay", "GPU fence: " + ex.Message);
				gpuFenceDevice = null;
				gpuIdle.Set();
				return;
			}
			fenceOutstanding = true;
			fenceSubmittedAt = Stopwatch.GetTimestamp();
			long submittedAt = fenceSubmittedAt;
			ThreadPool.RegisterWaitForSingleObject(gpuIdle, delegate
			{
				NoteGpuFrame(submittedAt);
			}, null, 5000, executeOnlyOnce: true);
		}
	}

	private void Shutdown()
	{
		shuttingDown = true;
		wakeEvent.Set();
		thread.Join(2000);
	}
}
