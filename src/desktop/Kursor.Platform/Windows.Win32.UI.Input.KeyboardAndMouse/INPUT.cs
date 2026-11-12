using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Windows.Win32.UI.Input.KeyboardAndMouse;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct INPUT
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Union
	{
		[FieldOffset(0)]
		internal MOUSEINPUT mi;

		[FieldOffset(0)]
		internal KEYBDINPUT ki;

		[FieldOffset(0)]
		internal HARDWAREINPUT hi;
	}

	internal INPUT_TYPE type;

	internal _Anonymous_e__Union Anonymous;

	[UnscopedRef]
	internal ref MOUSEINPUT mi => ref Anonymous.mi;

	[UnscopedRef]
	internal ref KEYBDINPUT ki => ref Anonymous.ki;

	[UnscopedRef]
	internal ref HARDWAREINPUT hi => ref Anonymous.hi;
}
