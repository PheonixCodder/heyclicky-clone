using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct SAFEARRAY
{
	internal ushort cDims;

	internal ADVANCED_FEATURE_FLAGS fFeatures;

	internal uint cbElements;

	internal uint cLocks;

	internal unsafe void* pvData;

	internal VariableLengthInlineArray<SAFEARRAYBOUND> rgsabound;

	internal unsafe static int SizeOf(int count)
	{
		int num = sizeof(SAFEARRAY);
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
