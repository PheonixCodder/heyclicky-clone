using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("09E31E18-872D-4873-93D1-1E541EC133FD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationProxyFactoryMapping
{
	uint Count { get; }

	unsafe SAFEARRAY* GetTable();

	IUIAutomationProxyFactoryEntry GetEntry(uint index);

	unsafe void SetTable(SAFEARRAY* factoryList);

	unsafe void InsertEntries(uint before, SAFEARRAY* factoryList);

	void InsertEntry(uint before, IUIAutomationProxyFactoryEntry factory);

	void RemoveEntry(uint index);

	void ClearTable();

	void RestoreDefaultTable();
}
