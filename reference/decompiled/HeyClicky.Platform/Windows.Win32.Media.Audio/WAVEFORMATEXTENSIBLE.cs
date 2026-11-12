using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32.Media.Audio;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct WAVEFORMATEXTENSIBLE
{
	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Samples_e__Union
	{
		[FieldOffset(0)]
		internal ushort wValidBitsPerSample;

		[FieldOffset(0)]
		internal ushort wSamplesPerBlock;

		[FieldOffset(0)]
		internal ushort wReserved;
	}

	internal WAVEFORMATEX Format;

	internal _Samples_e__Union Samples;

	internal uint dwChannelMask;

	internal Guid SubFormat;
}
