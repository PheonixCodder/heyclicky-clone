using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IMMNotificationClient
{
	void OnDeviceStateChanged(PCWSTR pwstrDeviceId, DEVICE_STATE dwNewState);

	void OnDeviceAdded(PCWSTR pwstrDeviceId);

	void OnDeviceRemoved(PCWSTR pwstrDeviceId);

	void OnDefaultDeviceChanged(EDataFlow flow, ERole role, [Optional] PCWSTR pwstrDefaultDeviceId);

	void OnPropertyValueChanged(PCWSTR pwstrDeviceId, PROPERTYKEY key);
}
