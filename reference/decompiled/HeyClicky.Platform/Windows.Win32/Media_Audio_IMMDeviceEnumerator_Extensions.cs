using System.CodeDom.Compiler;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IMMDeviceEnumerator_Extensions
{
	internal unsafe static void GetDevice(this IMMDeviceEnumerator @this, string pwstrId, out IMMDevice ppDevice)
	{
		fixed (char* ptr = pwstrId)
		{
			@this.GetDevice(ptr, out ppDevice);
		}
	}
}
