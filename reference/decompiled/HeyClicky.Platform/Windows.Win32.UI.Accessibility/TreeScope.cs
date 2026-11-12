using System.CodeDom.Compiler;

namespace Windows.Win32.UI.Accessibility;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum TreeScope
{
	TreeScope_None = 0,
	TreeScope_Element = 1,
	TreeScope_Children = 2,
	TreeScope_Descendants = 4,
	TreeScope_Parent = 8,
	TreeScope_Ancestors = 16,
	TreeScope_Subtree = 7
}
