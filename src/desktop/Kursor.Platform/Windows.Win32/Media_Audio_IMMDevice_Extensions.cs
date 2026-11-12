using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IMMDevice_Extensions
{
	internal unsafe static void Activate(this IMMDevice @this, in Guid iid, CLSCTX dwClsCtx, [Optional] PROPVARIANT_unmanaged? pActivationParams, out object ppInterface)
	{
		fixed (Guid* iid2 = &iid)
		{
			PROPVARIANT_unmanaged valueOrDefault = pActivationParams.GetValueOrDefault();
			@this.Activate(iid2, dwClsCtx, pActivationParams.HasValue ? (&valueOrDefault) : null, out ppInterface);
		}
	}

	internal unsafe static void GetId(this IMMDevice @this, out PWSTR ppstrId)
	{
		fixed (PWSTR* ppstrId2 = &ppstrId)
		{
			@this.GetId(ppstrId2);
		}
	}

	internal unsafe static void GetState(this IMMDevice @this, out DEVICE_STATE pdwState)
	{
		fixed (DEVICE_STATE* pdwState2 = &pdwState)
		{
			@this.GetState(pdwState2);
		}
	}
}
