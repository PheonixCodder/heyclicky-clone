using System;
using System.Globalization;

namespace Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS;

internal class AutoInitialize
{
	internal static void AccessWindowsAppSDK()
	{
		Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
		Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY_PID", Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
		NativeMethods.WindowsAppRuntime_EnsureIsLoaded();
	}
}
