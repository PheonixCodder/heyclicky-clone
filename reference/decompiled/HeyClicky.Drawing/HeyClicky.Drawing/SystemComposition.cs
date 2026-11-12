using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using WinRT;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

namespace HeyClicky.Drawing;

internal static class SystemComposition
{
	internal sealed class SurfaceDrawing : IDisposable
	{
		private readonly CompositionDrawingSurface surface;

		private readonly CanvasRenderTarget picture;

		public CanvasDrawingSession Session { get; }

		public SurfaceDrawing(CompositionDrawingSurface surface, CanvasDevice device)
		{
			this.surface = surface;
			SizeInt32 sizeInt = surface.SizeInt32;
			picture = new CanvasRenderTarget(device, Math.Max(1, sizeInt.Width), Math.Max(1, sizeInt.Height), 96f);
			Session = picture.CreateDrawingSession();
			Session.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));
		}

		public void Dispose()
		{
			Session.Dispose();
			try
			{
				CopyInto(surface, picture);
			}
			finally
			{
				picture.Dispose();
			}
		}
	}

	private struct Point32
	{
		public int X;

		public int Y;
	}

	private static readonly Guid ICompositorDesktopInterop = new Guid("29E691FA-4567-4DCA-B319-D0F207EB6807");

	private static readonly Guid ICompositorInterop = new Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330");

	private static readonly Guid ICompositionDrawingSurfaceInterop = new Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE");

	private static readonly Guid ICanvasResourceWrapperNative = new Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A");

	private static readonly Guid IID_ID2D1Device = new Guid("47DD575D-AC05-4CDD-8049-9B02CD16F44C");

	private static readonly Guid IID_ID2D1DeviceContext = new Guid("E8F7FE7A-191C-466D-AD95-975678BDA998");

	private static readonly Guid IID_ID2D1Bitmap1 = new Guid("A898A84C-3873-4588-B08B-EBBF978DF041");

	private static readonly ConditionalWeakTable<Compositor, CompositionGraphicsDevice> graphicsDevices = new ConditionalWeakTable<Compositor, CompositionGraphicsDevice>();

	private static readonly ConditionalWeakTable<CompositionDrawingSurface, CanvasDevice> surfaceDevices = new ConditionalWeakTable<CompositionDrawingSurface, CanvasDevice>();

	public unsafe static DesktopWindowTarget CreateWindowTarget(Compositor compositor, nint window, bool topmost = true)
	{
		nint num = QueryInterface(compositor, ICompositorDesktopInterop);
		try
		{
			nint num2 = default(nint);
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)3 * (nint)sizeof(nint))))(num, window, topmost ? 1 : 0, &num2));
			try
			{
				return DesktopWindowTarget.FromAbi(num2);
			}
			finally
			{
				Marshal.Release(num2);
			}
		}
		finally
		{
			Marshal.Release(num);
		}
	}

	public unsafe static CompositionGraphicsDevice GraphicsDevice(Compositor compositor, CanvasDevice canvasDevice)
	{
		return graphicsDevices.GetValue(compositor, delegate(Compositor c)
		{
			nint num = QueryInterface(c, ICompositorInterop);
			nint num2 = NativeResource(canvasDevice, IID_ID2D1Device);
			try
			{
				nint num3 = default(nint);
				Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)5 * (nint)sizeof(nint))))(num, num2, &num3));
				try
				{
					return CompositionGraphicsDevice.FromAbi(num3);
				}
				finally
				{
					Marshal.Release(num3);
				}
			}
			finally
			{
				Marshal.Release(num2);
				Marshal.Release(num);
			}
		});
	}

	public static CompositionDrawingSurface CreateSurface(Compositor compositor, CanvasDevice canvasDevice, Windows.Foundation.Size size)
	{
		CompositionDrawingSurface compositionDrawingSurface = GraphicsDevice(compositor, canvasDevice).CreateDrawingSurface(size, DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
		surfaceDevices.AddOrUpdate(compositionDrawingSurface, canvasDevice);
		return compositionDrawingSurface;
	}

	public static SurfaceDrawing Draw(CompositionDrawingSurface surface)
	{
		CanvasDevice value;
		CanvasDevice device = (surfaceDevices.TryGetValue(surface, out value) ? value : CanvasDevice.GetSharedDevice());
		return new SurfaceDrawing(surface, device);
	}

	public static void Resize(CompositionDrawingSurface surface, Windows.Foundation.Size size)
	{
		surface.Resize(new SizeInt32
		{
			Width = (int)Math.Ceiling(size.Width),
			Height = (int)Math.Ceiling(size.Height)
		});
	}

	private unsafe static void CopyInto(CompositionDrawingSurface surface, CanvasRenderTarget picture)
	{
		SizeInt32 sizeInt = surface.SizeInt32;
		if (sizeInt.Width <= 0 || sizeInt.Height <= 0)
		{
			return;
		}
		nint num = QueryInterface(surface, ICompositionDrawingSurfaceInterop);
		nint num2 = NativeResource(picture, IID_ID2D1Bitmap1);
		try
		{
			delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, Point32*, int> delegate_002A = (delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, Point32*, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)3 * (nint)sizeof(nint)));
			delegate* unmanaged[Stdcall]<nint, int> delegate_002A2 = (delegate* unmanaged[Stdcall]<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)4 * (nint)sizeof(nint)));
			Guid iID_ID2D1DeviceContext = IID_ID2D1DeviceContext;
			nint nativePtr = default(nint);
			Point32 point = default(Point32);
			Marshal.ThrowExceptionForHR(delegate_002A(num, IntPtr.Zero, &iID_ID2D1DeviceContext, &nativePtr, &point));
			try
			{
				using ID2D1DeviceContext iD2D1DeviceContext = new ID2D1DeviceContext(nativePtr);
				using ID2D1Bitmap1 image = new ID2D1Bitmap1(num2);
				Marshal.AddRef(num2);
				iD2D1DeviceContext.Transform = Matrix3x2.CreateTranslation(point.X, point.Y);
				iD2D1DeviceContext.PushAxisAlignedClip(new RawRectF(0f, 0f, sizeInt.Width, sizeInt.Height), AntialiasMode.Aliased);
				iD2D1DeviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
				iD2D1DeviceContext.DrawImage(image, Vector2.Zero, null, InterpolationMode.NearestNeighbor, CompositeMode.SourceCopy);
				iD2D1DeviceContext.PopAxisAlignedClip();
				iD2D1DeviceContext.Transform = Matrix3x2.Identity;
			}
			finally
			{
				Marshal.ThrowExceptionForHR(delegate_002A2(num));
			}
		}
		finally
		{
			Marshal.Release(num2);
			Marshal.Release(num);
		}
	}

	private static nint QueryInterface(object winrtObject, Guid iid)
	{
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(((IWinRTObject)winrtObject).NativeObject.ThisPtr, in iid, out var ppv));
		return ppv;
	}

	private unsafe static nint NativeResource(object win2DObject, Guid iid)
	{
		nint num = QueryInterface(win2DObject, ICanvasResourceWrapperNative);
		try
		{
			nint result = default(nint);
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, float, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)3 * (nint)sizeof(nint))))(num, IntPtr.Zero, 0f, &iid, &result));
			return result;
		}
		finally
		{
			Marshal.Release(num);
		}
	}
}
