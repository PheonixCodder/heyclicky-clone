using System.CodeDom.Compiler;

namespace Windows.Win32.System.Com.StructuredStorage;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct CLIPDATA
{
	internal uint cbSize;

	internal int ulClipFmt;

	internal unsafe byte* pClipData;
}
