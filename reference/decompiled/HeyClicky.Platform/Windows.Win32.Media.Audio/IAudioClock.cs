using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("CD63314F-3FBA-4A1B-812C-EF96358728E7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IAudioClock
{
	void GetFrequency(out ulong pu64Frequency);

	unsafe void GetPosition(out ulong pu64Position, [Optional] ulong* pu64QPCPosition);

	void GetCharacteristics(out uint pdwCharacteristics);
}
