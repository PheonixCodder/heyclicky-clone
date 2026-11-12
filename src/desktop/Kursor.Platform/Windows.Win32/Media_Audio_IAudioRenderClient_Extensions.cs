using System.CodeDom.Compiler;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IAudioRenderClient_Extensions
{
	internal unsafe static void GetBuffer(this IAudioRenderClient @this, uint NumFramesRequested, out byte* ppData)
	{
		fixed (byte** ppData2 = &ppData)
		{
			@this.GetBuffer(NumFramesRequested, ppData2);
		}
	}
}
