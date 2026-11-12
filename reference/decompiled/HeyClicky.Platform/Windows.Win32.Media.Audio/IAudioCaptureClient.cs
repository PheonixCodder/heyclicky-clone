using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IAudioCaptureClient
{
	unsafe void GetBuffer(byte** ppData, out uint pNumFramesToRead, out uint pdwFlags, [Optional] ulong* pu64DevicePosition, [Optional] ulong* pu64QPCPosition);

	void ReleaseBuffer(uint NumFramesRead);

	void GetNextPacketSize(out uint pNumFramesInNextPacket);
}
