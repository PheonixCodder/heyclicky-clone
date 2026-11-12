using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.Security;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct SECURITY_ATTRIBUTES
{
	internal uint nLength;

	internal unsafe void* lpSecurityDescriptor;

	internal BOOL bInheritHandle;
}
