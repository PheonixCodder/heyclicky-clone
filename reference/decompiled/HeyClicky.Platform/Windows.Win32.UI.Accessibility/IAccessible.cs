using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("618736E0-3C3D-11CF-810C-00AA00389B71")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
[SupportedOSPlatform("windows5.0")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IAccessible
{
	object accParent
	{
		[return: MarshalAs(UnmanagedType.IDispatch)]
		get;
	}

	int accChildCount { get; }

	[return: MarshalAs(UnmanagedType.IDispatch)]
	object get_accChild([MarshalAs(UnmanagedType.Struct)] object varChild);

	BSTR get_accName([MarshalAs(UnmanagedType.Struct)] object varChild);

	BSTR get_accValue([MarshalAs(UnmanagedType.Struct)] object varChild);

	BSTR get_accDescription([MarshalAs(UnmanagedType.Struct)] object varChild);

	[return: MarshalAs(UnmanagedType.Struct)]
	object get_accRole([MarshalAs(UnmanagedType.Struct)] object varChild);

	[return: MarshalAs(UnmanagedType.Struct)]
	object get_accState([MarshalAs(UnmanagedType.Struct)] object varChild);

	BSTR get_accHelp([MarshalAs(UnmanagedType.Struct)] object varChild);

	unsafe int get_accHelpTopic(BSTR* pszHelpFile, [MarshalAs(UnmanagedType.Struct)] object varChild);

	BSTR get_accKeyboardShortcut([MarshalAs(UnmanagedType.Struct)] object varChild);

	object accFocus
	{
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	object accSelection
	{
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	BSTR get_accDefaultAction([MarshalAs(UnmanagedType.Struct)] object varChild);

	void accSelect(int flagsSelect, [MarshalAs(UnmanagedType.Struct)] object varChild);

	void accLocation(out int pxLeft, out int pyTop, out int pcxWidth, out int pcyHeight, [MarshalAs(UnmanagedType.Struct)] object varChild);

	[return: MarshalAs(UnmanagedType.Struct)]
	object accNavigate(int navDir, [MarshalAs(UnmanagedType.Struct)] object varStart);

	[return: MarshalAs(UnmanagedType.Struct)]
	object accHitTest(int xLeft, int yTop);

	void accDoDefaultAction([MarshalAs(UnmanagedType.Struct)] object varChild);

	void put_accName([MarshalAs(UnmanagedType.Struct)] object varChild, BSTR szName);

	void put_accValue([MarshalAs(UnmanagedType.Struct)] object varChild, BSTR szValue);
}
