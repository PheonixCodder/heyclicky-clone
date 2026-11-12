using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IMMDevice
{
	unsafe void Activate(Guid* iid, CLSCTX dwClsCtx, [Optional] PROPVARIANT_unmanaged* pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

	void OpenPropertyStore(STGM stgmAccess, out IPropertyStore ppProperties);

	unsafe void GetId(PWSTR* ppstrId);

	unsafe void GetState(DEVICE_STATE* pdwState);
}
