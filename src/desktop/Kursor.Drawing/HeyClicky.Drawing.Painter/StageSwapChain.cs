using System;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Drawing3D;
using Microsoft.Win32.SafeHandles;
using SharpGen.Runtime;
using Vortice.DXGI;
using Vortice.Direct3D11;
using WinRT;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class StageSwapChain : IFigureSurface, IStageSurface, IDisposable
{
	private sealed class FrameLatencyHandle : WaitHandle
	{
		public FrameLatencyHandle(nint handle)
		{
			base.SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
		}
	}

	private static readonly Guid ICompositorInterop = new Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330");

	private static readonly bool Enabled = Environment.GetEnvironmentVariable("CANVAS_3D_SWAPCHAIN") != "0";

	private const SwapChainFlags Flags = SwapChainFlags.FrameLatencyWaitableObject;

	private const uint BufferCount = 3u;

	private const uint FramesInFlight = 2u;

	private readonly IDXGISwapChain2 swapChain;

	private readonly FrameLatencyHandle shown;

	private int width;

	private int height;

	private bool disposed;

	private bool waited;

	public ICompositionSurface Surface { get; }

	public static StageSwapChain? Create(Compositor compositor, int width, int height)
	{
		if (!Enabled)
		{
			return null;
		}
		try
		{
			return new StageSwapChain(compositor, width, height);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "3D figure has no swap chain, its frames are copied instead: " + ex.Message);
			return null;
		}
	}

	private StageSwapChain(Compositor compositor, int width, int height)
	{
		this.width = Math.Max(1, width);
		this.height = Math.Max(1, height);
		GpuDevice shared = GpuDevice.Shared;
		using (IDXGISwapChain1 iDXGISwapChain = shared.Factory.CreateSwapChainForComposition(shared.Device, new SwapChainDescription1
		{
			Width = (uint)this.width,
			Height = (uint)this.height,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(1u, 0u),
			BufferUsage = Usage.RenderTargetOutput,
			BufferCount = 3u,
			Scaling = Scaling.Stretch,
			SwapEffect = SwapEffect.FlipSequential,
			AlphaMode = AlphaMode.Premultiplied,
			Flags = SwapChainFlags.FrameLatencyWaitableObject
		}))
		{
			swapChain = iDXGISwapChain.QueryInterface<IDXGISwapChain2>();
		}
		try
		{
			swapChain.MaximumFrameLatency = 2u;
			shown = new FrameLatencyHandle(swapChain.FrameLatencyWaitableObject);
			Surface = CreateSurface(compositor, swapChain.NativePointer);
		}
		catch
		{
			shown?.Dispose();
			swapChain.Dispose();
			throw;
		}
	}

	private unsafe static ICompositionSurface CreateSurface(Compositor compositor, nint swapChain)
	{
		nint thisPtr = ((IWinRTObject)compositor).NativeObject.ThisPtr;
		Guid iid = ICompositorInterop;
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(thisPtr, in iid, out var ppv));
		try
		{
			nint num = default(nint);
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)4 * (nint)sizeof(nint))))(ppv, swapChain, &num));
			try
			{
				return MarshalInterface<ICompositionSurface>.FromAbi(num);
			}
			finally
			{
				Marshal.Release(num);
			}
		}
		finally
		{
			Marshal.Release(ppv);
		}
	}

	public void WaitUntilReady(int milliseconds)
	{
		if (disposed || waited)
		{
			return;
		}
		waited = true;
		try
		{
			shown.WaitOne(milliseconds);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static bool IsDeviceLost(int hr)
	{
		return hr == unchecked((int)0x887A0005) || hr == unchecked((int)0x887A0006) || hr == unchecked((int)0x887A0007) || hr == unchecked((int)0x8899000C);
	}

	public void Present(ID3D11Texture2D frame, int frameWidth, int frameHeight)
	{
		if (disposed)
		{
			return;
		}
		try
		{
			waited = false;
			if (frameWidth != width || frameHeight != height)
			{
				swapChain.ResizeBuffers(3u, (uint)frameWidth, (uint)frameHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.FrameLatencyWaitableObject).CheckError();
				width = frameWidth;
				height = frameHeight;
			}
			using (ID3D11Texture2D dstResource = swapChain.GetBuffer<ID3D11Texture2D>(0u))
			{
				GpuDevice.Shared.Context.CopyResource(dstResource, frame);
			}
			Result result = swapChain.Present(1u, PresentFlags.DoNotWait);
			if (IsDeviceLost(result.Code))
			{
				Log.Warn("Drawing", $"GPU device lost in StageSwapChain.Present (0x{result.Code:X8}); disposing swapChain for recreation");
				Dispose();
				return;
			}
			if (result != Vortice.DXGI.ResultCode.WasStillDrawing)
			{
				result.CheckError();
			}
		}
		catch (COMException ex) when (IsDeviceLost(ex.HResult))
		{
			Log.Warn("Drawing", $"GPU device lost in StageSwapChain.Present (0x{ex.HResult:X8}); disposing swapChain for recreation");
			Dispose();
		}
		catch (Exception ex) when (IsDeviceLost(ex.HResult))
		{
			Log.Warn("Drawing", $"GPU device lost in StageSwapChain.Present (0x{ex.HResult:X8}); disposing swapChain for recreation");
			Dispose();
		}
	}

	public void Dispose()
	{
		lock (GpuDevice.Shared.ContextLock)
		{
			if (disposed)
			{
				return;
			}
			disposed = true;
			swapChain.Dispose();
		}
		shown.Dispose();
		(Surface as IDisposable)?.Dispose();
	}
}
