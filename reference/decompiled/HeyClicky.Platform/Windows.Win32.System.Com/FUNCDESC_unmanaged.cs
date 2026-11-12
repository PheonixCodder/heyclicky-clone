using System.CodeDom.Compiler;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct FUNCDESC_unmanaged
{
	internal int memid;

	internal unsafe int* lprgscode;

	internal unsafe ELEMDESC_unmanaged* lprgelemdescParam;

	internal FUNCKIND funckind;

	internal INVOKEKIND invkind;

	internal CALLCONV callconv;

	internal short cParams;

	internal short cParamsOpt;

	internal short oVft;

	internal short cScodes;

	internal ELEMDESC_unmanaged elemdescFunc;

	internal FUNCFLAGS wFuncFlags;
}
