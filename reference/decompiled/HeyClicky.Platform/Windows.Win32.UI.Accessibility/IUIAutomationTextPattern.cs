using System.CodeDom.Compiler;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("32EBA289-3583-42C9-9C59-3B6D9A1E9B6A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationTextPattern
{
	IUIAutomationTextRange RangeFromPoint(Point pt);

	IUIAutomationTextRange RangeFromChild(IUIAutomationElement child);

	IUIAutomationTextRangeArray GetSelection();

	IUIAutomationTextRangeArray GetVisibleRanges();

	IUIAutomationTextRange DocumentRange { get; }

	SupportedTextSelection SupportedTextSelection { get; }
}
