using System;
using System.CodeDom.Compiler;
using Windows.Win32.System.Com;

namespace Windows.Win32.System.Ole;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct ARRAYDESC
{
	internal TYPEDESC tdescElem;

	internal ushort cDims;

	internal VariableLengthInlineArray<SAFEARRAYBOUND> rgbounds;

	internal unsafe static int SizeOf(int count)
	{
		int num = sizeof(ARRAYDESC);
		if (count > 1)
		{
			num += checked((count - 1) * sizeof(SAFEARRAYBOUND));
		}
		else if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		return num;
	}
}
