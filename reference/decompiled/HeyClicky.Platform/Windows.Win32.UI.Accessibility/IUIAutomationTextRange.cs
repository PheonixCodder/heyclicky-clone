using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("A543CC6A-F4AE-494B-8239-C814481187A8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationTextRange
{
	IUIAutomationTextRange Clone();

	BOOL Compare(IUIAutomationTextRange range);

	int CompareEndpoints(TextPatternRangeEndpoint srcEndPoint, IUIAutomationTextRange range, TextPatternRangeEndpoint targetEndPoint);

	void ExpandToEnclosingUnit(TextUnit textUnit);

	IUIAutomationTextRange FindAttribute(UIA_TEXTATTRIBUTE_ID attr, [MarshalAs(UnmanagedType.Struct)] object val, BOOL backward);

	IUIAutomationTextRange FindText(BSTR text, BOOL backward, BOOL ignoreCase);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetAttributeValue(UIA_TEXTATTRIBUTE_ID attr);

	unsafe SAFEARRAY* GetBoundingRectangles();

	IUIAutomationElement GetEnclosingElement();

	BSTR GetText(int maxLength);

	int Move(TextUnit unit, int count);

	int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

	void MoveEndpointByRange(TextPatternRangeEndpoint srcEndPoint, IUIAutomationTextRange range, TextPatternRangeEndpoint targetEndPoint);

	void Select();

	void AddToSelection();

	void RemoveFromSelection();

	void ScrollIntoView(BOOL alignToTop);

	IUIAutomationElementArray GetChildren();
}
