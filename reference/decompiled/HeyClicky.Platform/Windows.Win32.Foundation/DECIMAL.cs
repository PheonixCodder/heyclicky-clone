using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct DECIMAL
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous1_e__Union
	{
		[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
		internal struct _Anonymous_e__Struct
		{
			internal byte scale;

			internal byte sign;
		}

		[FieldOffset(0)]
		internal _Anonymous_e__Struct Anonymous;

		[FieldOffset(0)]
		internal ushort signscale;

		[UnscopedRef]
		internal ref byte scale => ref Anonymous.scale;

		[UnscopedRef]
		internal ref byte sign => ref Anonymous.sign;
	}

	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous2_e__Union
	{
		[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
		internal struct _Anonymous_e__Struct
		{
			internal uint Lo32;

			internal uint Mid32;
		}

		[FieldOffset(0)]
		internal _Anonymous_e__Struct Anonymous;

		[FieldOffset(0)]
		internal ulong Lo64;

		[UnscopedRef]
		internal ref uint Lo32 => ref Anonymous.Lo32;

		[UnscopedRef]
		internal ref uint Mid32 => ref Anonymous.Mid32;
	}

	internal ushort wReserved;

	internal _Anonymous1_e__Union Anonymous1;

	internal uint Hi32;

	internal _Anonymous2_e__Union Anonymous2;

	[UnscopedRef]
	internal ref byte scale => ref Anonymous1.Anonymous.scale;

	[UnscopedRef]
	internal ref byte sign => ref Anonymous1.Anonymous.sign;

	[UnscopedRef]
	internal ref ushort signscale => ref Anonymous1.signscale;

	[UnscopedRef]
	internal ref uint Lo32 => ref Anonymous2.Anonymous.Lo32;

	[UnscopedRef]
	internal ref uint Mid32 => ref Anonymous2.Anonymous.Mid32;

	[UnscopedRef]
	internal ref ulong Lo64 => ref Anonymous2.Lo64;

	public unsafe DECIMAL(decimal value)
	{
		this = *(DECIMAL*)(&value);
	}

	public unsafe static implicit operator decimal(DECIMAL value)
	{
		return *(decimal*)(&value);
	}

	public static implicit operator DECIMAL(decimal value)
	{
		return new DECIMAL(value);
	}
}
