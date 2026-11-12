using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

internal static class PWSTRHolder
{
	public unsafe static void GetId(IMMDevice device, out string id)
	{
		PWSTR pWSTR = default(PWSTR);
		device.GetId(&pWSTR);
		id = pWSTR.ToString();
		Marshal.FreeCoTaskMem((nint)pWSTR.Value);
	}
}
