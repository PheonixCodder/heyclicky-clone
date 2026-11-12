using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Win32;

public static class GpuScheduling
{
	private enum PriorityClass
	{
		Idle,
		BelowNormal,
		Normal,
		AboveNormal,
		High,
		Realtime
	}

	[DllImport("gdi32.dll", ExactSpelling = true)]
	[LibraryImport("gdi32.dll")]
	private static extern int D3DKMTSetProcessSchedulingPriorityClass(nint process, int priority);

	[LibraryImport("gdi32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static int D3DKMTGetProcessSchedulingPriorityClass(nint process, out int priority)
	{
		priority = 0;
		int result;
		fixed (int* _priority_native = &priority)
		{
			result = __PInvoke(process, _priority_native);
		}
		return result;
		[DllImport("gdi32.dll", EntryPoint = "D3DKMTGetProcessSchedulingPriorityClass", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __process_native, int* __priority_native);
	}

	public static void RaiseForProcess()
	{
		try
		{
			nint process = -1;
			int num = D3DKMTSetProcessSchedulingPriorityClass(process, 4);
			object obj;
			if (D3DKMTGetProcessSchedulingPriorityClass(process, out var priority) != 0)
			{
				obj = "unknown";
			}
			else
			{
				PriorityClass priorityClass = (PriorityClass)priority;
				obj = priorityClass.ToString();
			}
			string text = (string)obj;
			Log.Info("gpu", (num == 0) ? ("GPU scheduling priority: " + text) : $"GPU scheduling priority left at {text} (status 0x{num:X8})");
		}
		catch (Exception ex)
		{
			Log.Warn("gpu", "GPU scheduling priority: " + ex.Message);
		}
	}
}
