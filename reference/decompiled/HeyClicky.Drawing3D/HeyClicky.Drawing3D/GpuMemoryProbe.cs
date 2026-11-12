using System;
using Vortice.DXGI;

namespace HeyClicky.Drawing3D;

public static class GpuMemoryProbe
{
	private static IDXGIAdapter3? adapter;

	public static void Install()
	{
		if (MemorySample.GpuMemory != null)
		{
			return;
		}
		try
		{
			using IDXGIFactory1 iDXGIFactory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
			iDXGIFactory.EnumAdapters1(0u, out var adapterOut).CheckError();
			adapter = adapterOut.QueryInterfaceOrNull<IDXGIAdapter3>();
			adapterOut.Dispose();
			if ((object)adapter != null)
			{
				MemorySample.GpuMemory = () => (long)(adapter.QueryVideoMemoryInfo(0u, MemorySegmentGroup.Local).CurrentUsage + adapter.QueryVideoMemoryInfo(0u, MemorySegmentGroup.NonLocal).CurrentUsage);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("3D", "GPU memory probe: " + ex.Message);
		}
	}
}
