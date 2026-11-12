using System;
using System.Runtime.InteropServices;

namespace HeyClicky.Agents.Routines;

internal static class RoutineKeepAwake
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct ReasonContext
	{
		public uint Version;

		public uint Flags;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string SimpleReasonString;
	}

	private struct SystemPowerStatus
	{
		public byte ACLineStatus;

		public byte BatteryFlag;

		public byte BatteryLifePercent;

		public byte SystemStatusFlag;

		public int BatteryLifeTime;

		public int BatteryFullLifeTime;
	}

	private const string Reason = "HeyClicky is running a routine on a schedule";

	private const uint PowerRequestContextVersion = 0u;

	private const uint PowerRequestContextSimpleString = 1u;

	private const int PowerRequestSystemRequired = 1;

	private static readonly object gate = new object();

	private static nint request = IntPtr.Zero;

	public static void Update(bool hasActiveRoutines)
	{
		bool flag = hasActiveRoutines && IsOnAcPower();
		lock (gate)
		{
			if (flag && request == IntPtr.Zero)
			{
				ReasonContext reasonContext = default(ReasonContext);
				reasonContext.Version = 0u;
				reasonContext.Flags = 1u;
				reasonContext.SimpleReasonString = "HeyClicky is running a routine on a schedule";
				ReasonContext context = reasonContext;
				nint num = PowerCreateRequest(ref context);
				if (num != IntPtr.Zero && num != new IntPtr(-1))
				{
					if (!PowerSetRequest(num, 1))
					{
						CloseHandle(num);
						return;
					}
					request = num;
					Log.Info("Routines", "keep-awake request ON (routines armed, on AC power)");
				}
			}
			else if (!flag && request != IntPtr.Zero)
			{
				PowerClearRequest(request, 1);
				CloseHandle(request);
				request = IntPtr.Zero;
				Log.Info("Routines", "keep-awake request OFF");
			}
		}
	}

	private static bool IsOnAcPower()
	{
		if (GetSystemPowerStatus(out var status))
		{
			return status.ACLineStatus == 1;
		}
		return false;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint PowerCreateRequest(ref ReasonContext context);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool PowerSetRequest(nint powerRequest, int requestType);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool PowerClearRequest(nint powerRequest, int requestType);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
}
