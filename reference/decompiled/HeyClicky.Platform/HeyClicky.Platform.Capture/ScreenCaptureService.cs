using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Platform.Win32;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace HeyClicky.Platform.Capture;

public sealed class ScreenCaptureService : IScreenCaptureService, IDisposable
{
	private sealed class PendingEncode
	{
		public required DisplayInfo Display;

		public required byte[] Pixels;

		public required int Width;

		public required int Height;
	}

	private readonly object topologyGate = new object();

	private IDXGIFactory1? dxgiFactory;

	private readonly List<CaptureGpu> gpus = new List<CaptureGpu>();

	private readonly Dictionary<string, DesktopDuplicationSource> duplicationByDeviceName = new Dictionary<string, DesktopDuplicationSource>(StringComparer.OrdinalIgnoreCase);

	private readonly Timer coolDownTimer;

	private long lastCaptureTimestamp;

	private long duplicationRetryAfterTimestamp;

	private readonly SemaphoreSlim captureGate = new SemaphoreSlim(1, 1);

	private static readonly TimeSpan OwnCaptureSettleDelay = TimeSpan.FromMilliseconds(50L, 0L);

	private readonly Dictionary<DesktopDuplicationSource, CaptureGpu> sourceGpu = new Dictionary<DesktopDuplicationSource, CaptureGpu>();

	public ScreenCaptureBackend Backend { get; set; }

	public TimeSpan KeepWarmFor { get; set; } = TimeSpan.FromSeconds(30L);

	public ScreenCaptureTimings? LastTimings { get; private set; }

	public ScreenCaptureService()
	{
		coolDownTimer = new Timer(delegate
		{
			CoolDownIfIdle();
		}, null, -1, -1);
	}

	public IReadOnlyList<DisplayInfo> GetDisplays()
	{
		return DisplayEnumerator.GetDisplays();
	}

	public DisplayInfo GetDisplayUnderCursor()
	{
		return DisplayEnumerator.DisplayContaining(DisplayEnumerator.CursorPositionPx());
	}

	public void Prewarm()
	{
		Task.Run(async delegate
		{
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				EnsureTopology(DisplayEnumerator.GetDisplays());
				await WithOwnCaptureExclusionsAsync(delegate
				{
					lock (topologyGate)
					{
						foreach (DesktopDuplicationSource value in duplicationByDeviceName.Values)
						{
							lock (GpuFor(value).Gate)
							{
								value.AcquireCurrentFrame(500u);
							}
						}
					}
					return Task.FromResult(result: true);
				}).ConfigureAwait(continueOnCapturedContext: false);
				Interlocked.Exchange(ref lastCaptureTimestamp, Stopwatch.GetTimestamp());
				coolDownTimer.Change(KeepWarmFor, Timeout.InfiniteTimeSpan);
				Log.Info("Capture", $"prewarmed {duplicationByDeviceName.Count} duplication(s) in {stopwatch.ElapsedMilliseconds} ms");
			}
			catch (Exception ex)
			{
				Log.Warn("Capture", "prewarm failed: " + ex.Message);
			}
		});
	}

	public async Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllAsync(int maxDimension = 1280, int jpegQuality = 80, CancellationToken ct = default(CancellationToken))
	{
		await captureGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			return await WithOwnCaptureExclusionsAsync(() => Task.Run(() => CaptureAllCoreAsync((DisplayInfo _) => maxDimension, jpegQuality, ct), ct)).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			captureGate.Release();
			Interlocked.Exchange(ref lastCaptureTimestamp, Stopwatch.GetTimestamp());
			coolDownTimer.Change(KeepWarmFor, Timeout.InfiniteTimeSpan);
		}
	}

	public async Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllAtDisplayPointSizeAsync(int jpegQuality = 80, CancellationToken ct = default(CancellationToken))
	{
		await captureGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			return await WithOwnCaptureExclusionsAsync(() => Task.Run(() => CaptureAllCoreAsync(DisplayPointSizeMaxDimension, jpegQuality, ct), ct)).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			captureGate.Release();
			Interlocked.Exchange(ref lastCaptureTimestamp, Stopwatch.GetTimestamp());
			coolDownTimer.Change(KeepWarmFor, Timeout.InfiniteTimeSpan);
		}
	}

	private static async Task<T> WithOwnCaptureExclusionsAsync<T>(Func<Task<T>> capture)
	{
		bool changed = CaptureExclusion.SetCursorChromeHiddenFromOwnCapture(hidden: true);
		try
		{
			return await WithDrawingHiddenFromOwnCaptureAsync(capture, changed).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			CaptureExclusion.SetCursorChromeHiddenFromOwnCapture(hidden: false);
		}
	}

	private static async Task<T> WithDrawingHiddenFromOwnCaptureAsync<T>(Func<Task<T>> capture, bool changed)
	{
		Func<bool, Task<bool>> setHidden = CaptureExclusion.SetHiddenFromOwnCapture;
		if (setHidden == null)
		{
			if (changed)
			{
				await Task.Delay(OwnCaptureSettleDelay).ConfigureAwait(continueOnCapturedContext: false);
			}
			return await capture().ConfigureAwait(continueOnCapturedContext: false);
		}
		try
		{
			bool flag = changed;
			changed = flag | await setHidden(arg: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Capture", "hiding the drawing from our capture failed: " + ex.Message);
		}
		T result;
		try
		{
			if (changed)
			{
				await Task.Delay(OwnCaptureSettleDelay).ConfigureAwait(continueOnCapturedContext: false);
			}
			result = await capture().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			try
			{
				await setHidden(arg: false).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex2)
			{
				Log.Warn("Capture", "showing the drawing to recordings again failed: " + ex2.Message);
			}
		}
		return result;
	}

	public static int DisplayPointSizeMaxDimension(DisplayInfo display)
	{
		double num = ((display.Scale > 0.0) ? display.Scale : 1.0);
		double num2 = Math.Max(display.BoundsPx.Width, display.BoundsPx.Height) / num;
		return Math.Min(2000, Math.Max(1280, (int)num2));
	}

	public async Task<(byte[] Jpeg, int Width, int Height)> CaptureRegionAsync(DisplayInfo display, RectD regionPx, int maximumLongEdgeInPixels = 1600, CancellationToken ct = default(CancellationToken))
	{
		int displayWidth = (int)display.BoundsPx.Width;
		int displayHeight = (int)display.BoundsPx.Height;
		int left = Math.Clamp((int)Math.Floor(regionPx.X), 0, displayWidth);
		int top = Math.Clamp((int)Math.Floor(regionPx.Y), 0, displayHeight);
		int right = Math.Clamp((int)Math.Ceiling(regionPx.Right), 0, displayWidth);
		int bottom = Math.Clamp((int)Math.Ceiling(regionPx.Bottom), 0, displayHeight);
		if (right - left < 2 || bottom - top < 2)
		{
			throw new InvalidOperationException("That region is outside the screen");
		}
		await captureGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		byte[] pixels = null;
		try
		{
			EnsureTopology(DisplayEnumerator.GetDisplays());
			byte[] buffer;
			pixels = (buffer = ArrayPool<byte>.Shared.Rent(displayWidth * displayHeight * 4));
			if (!(await WithOwnCaptureExclusionsAsync(async delegate
			{
				bool flag = TryCaptureWithDuplication(display, displayWidth, displayHeight, buffer);
				if (!flag)
				{
					bool flag2 = GraphicsCaptureSource.IsSupported;
					if (flag2)
					{
						flag2 = await TryCaptureWithGraphicsCaptureAsync(display, displayWidth, displayHeight, buffer, ct).ConfigureAwait(continueOnCapturedContext: false);
					}
					flag = flag2;
				}
				return flag;
			}).ConfigureAwait(continueOnCapturedContext: false)))
			{
				throw new InvalidOperationException("No display available for capture");
			}
			int num = right - left;
			int num2 = bottom - top;
			double num3 = Math.Min(1.0, (double)maximumLongEdgeInPixels / (double)Math.Max(num, num2));
			int num4 = Math.Max(2, (int)((double)num * num3));
			int num5 = Math.Max(2, (int)((double)num2 * num3));
			byte[] array = new byte[num4 * num5 * 4];
			for (int i = 0; i < num5; i++)
			{
				int num6 = top + Math.Min(num2 - 1, (int)((double)i / num3));
				for (int j = 0; j < num4; j++)
				{
					int num7 = left + Math.Min(num - 1, (int)((double)j / num3));
					Buffer.BlockCopy(pixels, (num6 * displayWidth + num7) * 4, array, (i * num4 + j) * 4, 4);
				}
			}
			using MemoryStream scratch = new MemoryStream(524288);
			return (Jpeg: JpegEncoder.Encode(array, num4, num5, 0.85f, scratch), Width: num4, Height: num5);
		}
		finally
		{
			if (pixels != null)
			{
				ArrayPool<byte>.Shared.Return(pixels);
			}
			captureGate.Release();
			Interlocked.Exchange(ref lastCaptureTimestamp, Stopwatch.GetTimestamp());
			coolDownTimer.Change(KeepWarmFor, Timeout.InfiniteTimeSpan);
		}
	}

	private async Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllCoreAsync(Func<DisplayInfo, int> maxDimensionFor, int jpegQuality, CancellationToken ct)
	{
		Stopwatch totalStopwatch = Stopwatch.StartNew();
		IReadOnlyList<DisplayInfo> displays = DisplayEnumerator.GetDisplays();
		PointD cursor = DisplayEnumerator.CursorPositionPx();
		List<DisplayInfo> list = (from display in displays
			orderby (!display.BoundsPx.Contains(cursor)) ? 1 : 0, display.Index
			select display).ToList();
		EnsureTopology(displays);
		List<PendingEncode> pending = new List<PendingEncode>(list.Count);
		string backendUsed = "";
		foreach (DisplayInfo display2 in list)
		{
			ct.ThrowIfCancellationRequested();
			(int, int) tuple = TargetSize(display2, maxDimensionFor(display2));
			int width = tuple.Item1;
			int height = tuple.Item2;
			byte[] pixels = ArrayPool<byte>.Shared.Rent(width * height * 4);
			bool flag = false;
			bool flag2 = Backend != ScreenCaptureBackend.GraphicsCapture && Stopwatch.GetTimestamp() >= duplicationRetryAfterTimestamp;
			if (flag2 && TryCaptureWithDuplication(display2, width, height, pixels))
			{
				flag = true;
				backendUsed = "DesktopDuplication";
			}
			else if (Backend != ScreenCaptureBackend.DesktopDuplication && GraphicsCaptureSource.IsSupported)
			{
				if (flag2)
				{
					duplicationRetryAfterTimestamp = Stopwatch.GetTimestamp() + 2 * Stopwatch.Frequency;
				}
				flag = await TryCaptureWithGraphicsCaptureAsync(display2, width, height, pixels, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (flag)
				{
					backendUsed = "GraphicsCapture";
				}
			}
			if (flag)
			{
				pending.Add(new PendingEncode
				{
					Display = display2,
					Pixels = pixels,
					Width = width,
					Height = height
				});
			}
			else
			{
				ArrayPool<byte>.Shared.Return(pixels);
				Log.Warn("Capture", "could not capture " + display2.DeviceName);
			}
		}
		double gpuMs = totalStopwatch.Elapsed.TotalMilliseconds;
		float quality = Math.Clamp((float)jpegQuality / 100f, 0.05f, 1f);
		byte[][] array = await Task.WhenAll(pending.Select((PendingEncode item) => Task.Run(delegate
		{
			using MemoryStream scratch = new MemoryStream(524288);
			try
			{
				return JpegEncoder.Encode(item.Pixels, item.Width, item.Height, quality, scratch);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(item.Pixels);
			}
		}, ct)).ToArray()).ConfigureAwait(continueOnCapturedContext: false);
		List<ScreenCaptureFrame> list2 = new List<ScreenCaptureFrame>(pending.Count);
		for (int i = 0; i < pending.Count; i++)
		{
			PendingEncode pendingEncode = pending[i];
			bool flag3 = pendingEncode.Display.BoundsPx.Contains(cursor);
			PointD pointD = new PointD((cursor.X - pendingEncode.Display.BoundsPx.X) * (double)pendingEncode.Width / pendingEncode.Display.BoundsPx.Width, (cursor.Y - pendingEncode.Display.BoundsPx.Y) * (double)pendingEncode.Height / pendingEncode.Display.BoundsPx.Height);
			list2.Add(new ScreenCaptureFrame(pendingEncode.Display, array[i], pendingEncode.Width, pendingEncode.Height, flag3, flag3 ? pointD : default(PointD)));
		}
		double totalMilliseconds = totalStopwatch.Elapsed.TotalMilliseconds;
		LastTimings = new ScreenCaptureTimings(totalMilliseconds, gpuMs, totalMilliseconds - gpuMs, backendUsed, list2.Count);
		return list2;
	}

	private static (int Width, int Height) TargetSize(DisplayInfo display, int maxDimension)
	{
		double width = display.BoundsPx.Width;
		double height = display.BoundsPx.Height;
		double num = width / Math.Max(1.0, height);
		int num2 = (int)Math.Min(maxDimension, Math.Max(width, height));
		if (!(width >= height))
		{
			return (Width: Math.Max(1, (int)((double)num2 * num)), Height: num2);
		}
		return (Width: num2, Height: Math.Max(1, (int)((double)num2 / num)));
	}

	private bool TryCaptureWithDuplication(DisplayInfo display, int width, int height, byte[] pixels)
	{
		DesktopDuplicationSource value;
		lock (topologyGate)
		{
			duplicationByDeviceName.TryGetValue(display.DeviceName, out value);
		}
		if (value == null)
		{
			return false;
		}
		CaptureGpu captureGpu = GpuFor(value);
		lock (captureGpu.Gate)
		{
			ID3D11Texture2D iD3D11Texture2D = value.AcquireCurrentFrame(100u);
			if ((object)iD3D11Texture2D == null)
			{
				return false;
			}
			ID3D11Texture2D staging = captureGpu.ScaleToStaging(iD3D11Texture2D, width, height, value.Rotation);
			captureGpu.ReadStaging(staging, width, height, pixels);
			return true;
		}
	}

	private async Task<bool> TryCaptureWithGraphicsCaptureAsync(DisplayInfo display, int width, int height, byte[] pixels, CancellationToken ct)
	{
		try
		{
			CaptureGpu gpu;
			lock (topologyGate)
			{
				if (gpus.Count <= 0)
				{
					throw new InvalidOperationException("no GPU");
				}
				gpu = gpus[0];
			}
			nint monitorHandle = (nint)PInvoke.MonitorFromPoint(new Point((int)(display.BoundsPx.X + display.BoundsPx.Width / 2.0), (int)(display.BoundsPx.Y + display.BoundsPx.Height / 2.0)), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
			ID3D11Texture2D iD3D11Texture2D = await GraphicsCaptureSource.CaptureFrameAsync(gpu, monitorHandle, TimeSpan.FromMilliseconds(500L, 0L), ct).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)iD3D11Texture2D == null)
			{
				return false;
			}
			lock (gpu.Gate)
			{
				ID3D11Texture2D staging = gpu.ScaleToStaging(iD3D11Texture2D, width, height, ModeRotation.Identity);
				gpu.ReadStaging(staging, width, height, pixels);
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Capture", "Graphics Capture of " + display.DeviceName + " failed: " + ex.Message);
			return false;
		}
	}

	private CaptureGpu GpuFor(DesktopDuplicationSource source)
	{
		lock (topologyGate)
		{
			return sourceGpu[source];
		}
	}

	private void EnsureTopology(IReadOnlyList<DisplayInfo> displays)
	{
		lock (topologyGate)
		{
			if (gpus.Count > 0 && displays.All((DisplayInfo display) => duplicationByDeviceName.ContainsKey(display.DeviceName)))
			{
				return;
			}
			DisposeTopology();
			dxgiFactory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
			IDXGIAdapter1 adapterOut;
			for (uint num = 0u; dxgiFactory.EnumAdapters1(num, out adapterOut).Success; num++)
			{
				if ((adapterOut.Description1.Flags & AdapterFlags.Software) != 0)
				{
					adapterOut.Dispose();
					continue;
				}
				List<(IDXGIOutput1, string)> list = new List<(IDXGIOutput1, string)>();
				IDXGIOutput outputOut;
				for (uint num2 = 0u; adapterOut.EnumOutputs(num2, out outputOut).Success; num2++)
				{
					OutputDescription description = outputOut.Description;
					IDXGIOutput1 iDXGIOutput = outputOut.QueryInterfaceOrNull<IDXGIOutput1>();
					outputOut.Dispose();
					if ((object)iDXGIOutput == null || !description.AttachedToDesktop)
					{
						iDXGIOutput?.Dispose();
					}
					else
					{
						list.Add((iDXGIOutput, description.DeviceName));
					}
				}
				if (list.Count == 0 && gpus.Count > 0)
				{
					adapterOut.Dispose();
					continue;
				}
				CaptureGpu captureGpu = new CaptureGpu(adapterOut);
				gpus.Add(captureGpu);
				foreach (var item3 in list)
				{
					IDXGIOutput1 item = item3.Item1;
					string item2 = item3.Item2;
					DesktopDuplicationSource desktopDuplicationSource = new DesktopDuplicationSource(captureGpu, item, item2);
					duplicationByDeviceName[item2] = desktopDuplicationSource;
					sourceGpu[desktopDuplicationSource] = captureGpu;
				}
			}
			Log.Info("Capture", $"capture topology: {gpus.Count} adapter(s), outputs [{string.Join(", ", duplicationByDeviceName.Keys)}]");
		}
	}

	private void CoolDownIfIdle()
	{
		if (Stopwatch.GetElapsedTime(Interlocked.Read(in lastCaptureTimestamp)) < KeepWarmFor - TimeSpan.FromMilliseconds(50L, 0L) || !captureGate.Wait(0))
		{
			return;
		}
		try
		{
			lock (topologyGate)
			{
				foreach (var (desktopDuplicationSource2, captureGpu2) in sourceGpu)
				{
					lock (captureGpu2.Gate)
					{
						desktopDuplicationSource2.Close();
					}
				}
			}
			Log.Info("Capture", "released desktop duplications after idle");
		}
		finally
		{
			captureGate.Release();
		}
	}

	private void DisposeTopology()
	{
		foreach (DesktopDuplicationSource value in duplicationByDeviceName.Values)
		{
			value.Dispose();
		}
		duplicationByDeviceName.Clear();
		sourceGpu.Clear();
		foreach (CaptureGpu gpu in gpus)
		{
			gpu.Dispose();
		}
		gpus.Clear();
		dxgiFactory?.Dispose();
		dxgiFactory = null;
	}

	public void Dispose()
	{
		coolDownTimer.Dispose();
		lock (topologyGate)
		{
			DisposeTopology();
		}
	}
}
