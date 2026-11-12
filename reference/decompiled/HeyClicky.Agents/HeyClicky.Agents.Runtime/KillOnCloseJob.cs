using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HeyClicky.Agents.Runtime;

internal sealed class KillOnCloseJob : IDisposable
{
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public long PerProcessUserTimeLimit;

		public long PerJobUserTimeLimit;

		public uint LimitFlags;

		public nuint MinimumWorkingSetSize;

		public nuint MaximumWorkingSetSize;

		public uint ActiveProcessLimit;

		public nuint Affinity;

		public uint PriorityClass;

		public uint SchedulingClass;
	}

	private struct IO_COUNTERS
	{
		public ulong ReadOperationCount;

		public ulong WriteOperationCount;

		public ulong OtherOperationCount;

		public ulong ReadTransferCount;

		public ulong WriteTransferCount;

		public ulong OtherTransferCount;
	}

	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;

		public IO_COUNTERS IoInfo;

		public nuint ProcessMemoryLimit;

		public nuint JobMemoryLimit;

		public nuint PeakProcessMemoryUsed;

		public nuint PeakJobMemoryUsed;
	}

	private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeJobHandle()
			: base(ownsHandle: true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return CloseHandle(handle);
		}
	}

	private readonly SafeJobHandle handle;

	private static readonly Lazy<KillOnCloseJob?> SharedJob = new Lazy<KillOnCloseJob>(delegate
	{
		try
		{
			return new KillOnCloseJob();
		}
		catch (Exception ex)
		{
			Log.Error("Agents", "could not create kill-on-close job object", ex);
			return (KillOnCloseJob?)null;
		}
	});

	private const int JobObjectExtendedLimitInformation = 9;

	private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 8192u;

	public static KillOnCloseJob? Shared => SharedJob.Value;

	public KillOnCloseJob(string? name = null)
	{
		handle = CreateJobObjectW(IntPtr.Zero, name);
		if (handle.IsInvalid)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject failed");
		}
		JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
			{
				LimitFlags = 8192u
			}
		};
		int num = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
		nint num2 = Marshal.AllocHGlobal(num);
		try
		{
			Marshal.StructureToPtr(structure, num2, fDeleteOld: false);
			if (!SetInformationJobObject(handle, 9, num2, (uint)num))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject failed");
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}

	public void Assign(Process process)
	{
		if (!AssignProcessToJobObject(handle, process.Handle))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed");
		}
	}

	public void TerminateAll(uint exitCode = 1u)
	{
		TerminateJobObject(handle, exitCode);
	}

	public void Dispose()
	{
		handle.Dispose();
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern SafeJobHandle CreateJobObjectW(nint lpJobAttributes, string? lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetInformationJobObject(SafeJobHandle hJob, int infoClass, nint lpInfo, uint cbInfoLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AssignProcessToJobObject(SafeJobHandle hJob, nint hProcess);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool TerminateJobObject(SafeJobHandle hJob, uint uExitCode);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint hObject);
}
