using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HeyClicky;

public readonly record struct MemorySample(long PrivateBytes, long WorkingSet, long GcHeap, long GcCommitted, long GpuBytes, int Handles, int GdiObjects, int UserObjects, int Threads)
{
	public static Func<long>? GpuMemory { get; set; }

	public static MemorySample Take()
	{
		using Process process = Process.GetCurrentProcess();
		GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
		long gpuBytes = -1L;
		try
		{
			gpuBytes = GpuMemory?.Invoke() ?? (-1);
		}
		catch
		{
		}
		return new MemorySample(process.PrivateMemorySize64, process.WorkingSet64, GC.GetTotalMemory(forceFullCollection: false), gCMemoryInfo.TotalCommittedBytes, gpuBytes, process.HandleCount, (int)GetGuiResources(process.Handle, 0u), (int)GetGuiResources(process.Handle, 1u), process.Threads.Count);
	}

	private static string Mb(long bytes)
	{
		return $"{(double)bytes / 1048576.0:F0} MB";
	}

	public override string ToString()
	{
		return $"private {Mb(PrivateBytes)}, working set {Mb(WorkingSet)}, GC heap {Mb(GcHeap)} (committed {Mb(GcCommitted)}), GPU {((GpuBytes >= 0) ? Mb(GpuBytes) : "none")}, handles {Handles}, GDI {GdiObjects}, USER {UserObjects}, threads {Threads}";
	}

	[DllImport("user32.dll")]
	private static extern uint GetGuiResources(nint process, uint flags);
}
