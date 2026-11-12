using System;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using WinRT;

namespace HeyClicky.Drawing;

internal static class GpuPriority
{
	private static readonly Guid IID_IDirect3DDxgiInterfaceAccess = new Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");

	private static readonly Guid IID_IDXGIDevice = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C");

	public unsafe static void Raise(CanvasDevice device)
	{
		try
		{
			nint pUnk = MarshalInspectable<CanvasDevice>.FromManaged(device);
			try
			{
				Guid iid = IID_IDirect3DDxgiInterfaceAccess;
				Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pUnk, in iid, out var ppv));
				try
				{
					Guid iID_IDXGIDevice = IID_IDXGIDevice;
					nint num = default(nint);
					Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)3 * (nint)sizeof(nint))))(ppv, &iID_IDXGIDevice, &num));
					try
					{
						int num2 = ((delegate* unmanaged[Stdcall]<nint, int, int>)(*(IntPtr*)((nint)(*(IntPtr*)num) + (nint)10 * (nint)sizeof(nint))))(num, 7);
						Log.Info("Drawing", "GPU thread priority: " + ((num2 >= 0) ? "raised to 7" : $"left as it was (0x{num2:X8})"));
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
			finally
			{
				Marshal.Release(pUnk);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "GPU thread priority: " + ex.Message);
		}
	}
}
