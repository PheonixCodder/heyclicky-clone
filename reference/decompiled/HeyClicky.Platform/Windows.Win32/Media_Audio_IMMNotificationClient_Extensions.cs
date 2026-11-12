using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IMMNotificationClient_Extensions
{
	internal unsafe static void OnDeviceStateChanged(this IMMNotificationClient @this, string pwstrDeviceId, DEVICE_STATE dwNewState)
	{
		fixed (char* ptr = pwstrDeviceId)
		{
			@this.OnDeviceStateChanged(ptr, dwNewState);
		}
	}

	internal unsafe static void OnDeviceAdded(this IMMNotificationClient @this, string pwstrDeviceId)
	{
		fixed (char* ptr = pwstrDeviceId)
		{
			@this.OnDeviceAdded(ptr);
		}
	}

	internal unsafe static void OnDeviceRemoved(this IMMNotificationClient @this, string pwstrDeviceId)
	{
		fixed (char* ptr = pwstrDeviceId)
		{
			@this.OnDeviceRemoved(ptr);
		}
	}

	internal unsafe static void OnDefaultDeviceChanged(this IMMNotificationClient @this, EDataFlow flow, ERole role, [Optional] string pwstrDefaultDeviceId)
	{
		fixed (char* ptr = pwstrDefaultDeviceId)
		{
			@this.OnDefaultDeviceChanged(flow, role, ptr);
		}
	}

	internal unsafe static void OnPropertyValueChanged(this IMMNotificationClient @this, string pwstrDeviceId, PROPERTYKEY key)
	{
		fixed (char* ptr = pwstrDeviceId)
		{
			@this.OnPropertyValueChanged(ptr, key);
		}
	}
}
