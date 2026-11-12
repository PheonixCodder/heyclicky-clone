using System;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Input;

internal static class ForegroundIntegrity
{
	private static readonly int ours = Level(-1);

	public static bool IsAboveOurs()
	{
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0 || ours < 0)
		{
			return false;
		}
		GetWindowThreadProcessId(foregroundWindow, out var processId);
		if (processId == 0 || processId == (uint)Environment.ProcessId)
		{
			return false;
		}
		nint num = OpenProcess(4096u, inheritHandle: false, processId);
		if (num == 0)
		{
			return false;
		}
		try
		{
			return Level(num) > ours;
		}
		finally
		{
			CloseHandle(num);
		}
	}

	private unsafe static int Level(nint process)
	{
		if (!OpenProcessToken(process, 8u, out var token))
		{
			return -1;
		}
		try
		{
			byte* ptr = stackalloc byte[64];
			if (!GetTokenInformation(token, 25, ptr, 64, out var _))
			{
				return -1;
			}
			byte* ptr2 = *(byte**)ptr;
			int num = ptr2[1];
			return (num == 0) ? (-1) : (*(int*)(ptr2 + 8 + 4 * (num - 1)));
		}
		finally
		{
			CloseHandle(token);
		}
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint OpenProcess(uint access, bool inheritHandle, uint processId);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool OpenProcessToken(nint process, uint access, out nint token);

	[DllImport("advapi32.dll", SetLastError = true)]
	private unsafe static extern bool GetTokenInformation(nint token, int informationClass, void* information, int length, out int returnLength);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(nint handle);
}
