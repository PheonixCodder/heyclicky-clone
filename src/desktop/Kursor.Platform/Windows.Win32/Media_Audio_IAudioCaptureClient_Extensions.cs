using System.CodeDom.Compiler;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IAudioCaptureClient_Extensions
{
	internal unsafe static void GetBuffer(this IAudioCaptureClient @this, out byte* ppData, out uint pNumFramesToRead, out uint pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition)
	{
		fixed (ulong* pu64QPCPosition2 = &pu64QPCPosition)
		{
			fixed (ulong* pu64DevicePosition2 = &pu64DevicePosition)
			{
				fixed (byte** ppData2 = &ppData)
				{
					@this.GetBuffer(ppData2, out pNumFramesToRead, out pdwFlags, pu64DevicePosition2, pu64QPCPosition2);
				}
			}
		}
	}

	internal unsafe static void GetBuffer(this IAudioCaptureClient @this, out byte* ppData, out uint pNumFramesToRead, out uint pdwFlags)
	{
		fixed (byte** ppData2 = &ppData)
		{
			@this.GetBuffer(ppData2, out pNumFramesToRead, out pdwFlags, null, null);
		}
	}
}
