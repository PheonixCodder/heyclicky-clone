using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IMMDeviceCollection
{
	void GetCount(out uint pcDevices);

	void Item(uint nDevice, out IMMDevice ppDevice);
}
