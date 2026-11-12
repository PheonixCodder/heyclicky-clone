using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClicky_DrawingGenericHelpers;

internal static class IEnumerable_System_Numerics_Vector2
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IEnumerableMethods<Vector2, Vector2>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_First_0));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_First_0(nint thisPtr, nint* __return_value__)
	{
		*__return_value__ = 0;
		try
		{
			*__return_value__ = MarshalInterface<IEnumerator<Vector2>>.FromManaged(IEnumerableMethods<Vector2>.Abi_First_0(thisPtr));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
