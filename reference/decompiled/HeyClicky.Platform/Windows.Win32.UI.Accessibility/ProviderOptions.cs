using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.Accessibility;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum ProviderOptions
{
	ProviderOptions_ClientSideProvider = 1,
	ProviderOptions_ServerSideProvider = 2,
	ProviderOptions_NonClientAreaProvider = 4,
	ProviderOptions_OverrideProvider = 8,
	ProviderOptions_ProviderOwnsSetFocus = 0x10,
	ProviderOptions_UseComThreading = 0x20,
	ProviderOptions_RefuseNonClientSupport = 0x40,
	ProviderOptions_HasNativeIAccessible = 0x80,
	ProviderOptions_UseClientCoordinates = 0x100
}
