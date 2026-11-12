using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HeyClicky.Drawing3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using WinRT;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class StageSurface : IFigureSurface, IStageSurface, IDisposable
{
	private struct Point32
	{
		public int X;

		public int Y;
	}

	private struct Size32
	{
		public int Width;

		public int Height;
	}

	private static readonly Guid ICompositorInterop = new Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330");

	private static readonly Guid ICompositionDrawingSurfaceInterop = new Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE");

	private static readonly Guid ID3D11Texture2D = new Guid("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

	private static readonly ConditionalWeakTable<Compositor, CompositionGraphicsDevice> devices = new ConditionalWeakTable<Compositor, CompositionGraphicsDevice>();

	private readonly CompositionDrawingSurface surface;

	private nint interop;

	private int width;

	private int height;

	private bool disposed;

	public ICompositionSurface Surface => surface;

	public StageSurface(Compositor compositor, int width, int height)
	{
		CompositionGraphicsDevice value = devices.GetValue(compositor, CreateGraphicsDevice);
		surface = value.CreateDrawingSurface2(new SizeInt32(Math.Max(1, width), Math.Max(1, height)), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
		nint thisPtr = ((IWinRTObject)surface).NativeObject.ThisPtr;
		Guid iid = ICompositionDrawingSurfaceInterop;
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(thisPtr, in iid, out interop));
		this.width = Math.Max(1, width);
		this.height = Math.Max(1, height);
	}

	private unsafe static CompositionGraphicsDevice CreateGraphicsDevice(Compositor compositor)
	{
		nint thisPtr = ((IWinRTObject)compositor).NativeObject.ThisPtr;
		Guid iid = ICompositorInterop;
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(thisPtr, in iid, out var ppv));
		try
		{
			nint num = default(nint);
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)5 * (nint)sizeof(nint))))(ppv, GpuDevice.Shared.Device.NativePointer, &num));
			try
			{
				return MarshalInterface<CompositionGraphicsDevice>.FromAbi(num);
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

	public unsafe void Present(ID3D11Texture2D frame, int frameWidth, int frameHeight)
	{
		if (disposed)
		{
			return;
		}
		nint* ptr = *(nint**)interop;
		if (frameWidth != width || frameHeight != height)
		{
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Size32, int>)ptr[5])(interop, new Size32
			{
				Width = frameWidth,
				Height = frameHeight
			}));
			width = frameWidth;
			height = frameHeight;
		}
		delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, Point32*, int> delegate_002A = (delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, Point32*, int>)ptr[3];
		delegate* unmanaged[Stdcall]<nint, int> delegate_002A2 = (delegate* unmanaged[Stdcall]<nint, int>)ptr[4];
		Guid iD3D11Texture2D = ID3D11Texture2D;
		nint nativePtr = default(nint);
		Point32 point = default(Point32);
		int num = delegate_002A(interop, IntPtr.Zero, &iD3D11Texture2D, &nativePtr, &point);
		if (num < 0)
		{
			Log.Warn("Drawing", $"3D surface BeginDraw failed (0x{num:X8})");
			return;
		}
		try
		{
			using ID3D11Texture2D dstResource = new ID3D11Texture2D(nativePtr);
			GpuDevice.Shared.Context.CopySubresourceRegion(dstResource, 0u, (uint)point.X, (uint)point.Y, 0u, frame, 0u, new Box(0, 0, 0, frameWidth, frameHeight, 1));
		}
		finally
		{
			delegate_002A2(interop);
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
			if (interop != IntPtr.Zero)
			{
				Marshal.Release(interop);
				interop = IntPtr.Zero;
			}
		}
		surface.Dispose();
	}
}
