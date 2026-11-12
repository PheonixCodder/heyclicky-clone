using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vortice.DXGI;
using Vortice.Direct3D11;
using WinRT;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace HeyClicky.Platform.Capture;

internal static class GraphicsCaptureSource
{
	private static readonly Guid IID_IGraphicsCaptureItemInterop = new Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");

	private static readonly Guid IID_IGraphicsCaptureItem = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

	private static readonly Guid IID_IDirect3DDxgiInterfaceAccess = new Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");

	private static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

	private static IDirect3DDevice? winrtDevice;

	private static CaptureGpu? winrtDeviceOwner;

	public static bool IsSupported
	{
		get
		{
			try
			{
				return GraphicsCaptureSession.IsSupported();
			}
			catch
			{
				return false;
			}
		}
	}

	private static IDirect3DDevice GetWinRtDevice(CaptureGpu gpu)
	{
		if (winrtDevice != null && winrtDeviceOwner == gpu)
		{
			return winrtDevice;
		}
		using IDXGIDevice iDXGIDevice = gpu.Device.QueryInterface<IDXGIDevice>();
		Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(iDXGIDevice.NativePointer, out var graphicsDevice));
		try
		{
			winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);
		}
		finally
		{
			Marshal.Release(graphicsDevice);
		}
		winrtDeviceOwner = gpu;
		return winrtDevice;
	}

	public static async Task<ID3D11Texture2D?> CaptureFrameAsync(CaptureGpu gpu, nint monitorHandle, TimeSpan timeout, CancellationToken cancellationToken)
	{
		GraphicsCaptureItem graphicsCaptureItem = CreateItemForMonitor(monitorHandle);
		SizeInt32 size = graphicsCaptureItem.Size;
		IDirect3DDevice winRtDevice = GetWinRtDevice(gpu);
		using Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(winRtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, size);
		using GraphicsCaptureSession session = framePool.CreateCaptureSession(graphicsCaptureItem);
		try
		{
			session.IsCursorCaptureEnabled = false;
		}
		catch
		{
		}
		try
		{
			session.IsBorderRequired = false;
		}
		catch (Exception ex)
		{
			Log.Warn("Capture", "IsBorderRequired=false refused: " + ex.Message);
		}
		TaskCompletionSource<Direct3D11CaptureFrame?> firstFrame = new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
		framePool.FrameArrived += delegate(Direct3D11CaptureFramePool pool, object _)
		{
			firstFrame.TrySetResult(pool.TryGetNextFrame());
		};
		session.StartCapture();
		using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutSource.CancelAfter(timeout);
		using (timeoutSource.Token.Register(delegate
		{
			firstFrame.TrySetResult(null);
		}))
		{
			using Direct3D11CaptureFrame direct3D11CaptureFrame = await firstFrame.Task.ConfigureAwait(continueOnCapturedContext: false);
			if ((object)direct3D11CaptureFrame == null)
			{
				return null;
			}
			using ID3D11Texture2D iD3D11Texture2D = TextureFromSurface(direct3D11CaptureFrame.Surface);
			lock (gpu.Gate)
			{
				Texture2DDescription description = iD3D11Texture2D.Description;
				ID3D11Texture2D sourceCopyTexture = gpu.GetSourceCopyTexture((int)description.Width, (int)description.Height);
				gpu.Context.CopyResource(sourceCopyTexture, iD3D11Texture2D);
				return sourceCopyTexture;
			}
		}
	}

	private unsafe static GraphicsCaptureItem CreateItemForMonitor(nint monitorHandle)
	{
		nint thisPtr = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem", IID_IGraphicsCaptureItemInterop).ThisPtr;
		Guid iID_IGraphicsCaptureItem = IID_IGraphicsCaptureItem;
		nint num = default(nint);
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)thisPtr) + (nint)4 * (nint)sizeof(nint))))(thisPtr, monitorHandle, &iID_IGraphicsCaptureItem, &num));
		try
		{
			return GraphicsCaptureItem.FromAbi(num);
		}
		finally
		{
			Marshal.Release(num);
		}
	}

	private unsafe static ID3D11Texture2D TextureFromSurface(IDirect3DSurface surface)
	{
		nint pUnk = MarshalInterface<IDirect3DSurface>.FromManaged(surface);
		try
		{
			Guid iid = IID_IDirect3DDxgiInterfaceAccess;
			Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pUnk, in iid, out var ppv));
			try
			{
				Guid iID_ID3D11Texture2D = IID_ID3D11Texture2D;
				nint nativePtr = default(nint);
				Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)3 * (nint)sizeof(nint))))(ppv, &iID_ID3D11Texture2D, &nativePtr));
				return new ID3D11Texture2D(nativePtr);
			}
			finally
			{
				Marshal.Release(ppv);
			}
		}
		finally
		{
			Marshal.Release(pUnk);
		}
	}

	[DllImport("d3d11.dll", ExactSpelling = true)]
	private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint graphicsDevice);
}
