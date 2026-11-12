using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClickyGenericHelpers;

internal static class KeyValuePair_string_int
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return KeyValuePairMethods<string, nint, int, int>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_get_Key_0), (delegate* unmanaged[Stdcall]<nint, int*, int>)(&Do_Abi_get_Value_1));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Key_0(nint thisPtr, nint* __return_value__)
	{
		string text = null;
		*__return_value__ = 0;
		try
		{
			text = KeyValuePairMethods<string, int>.Abi_get_Key_0(thisPtr);
			*__return_value__ = MarshalString.FromManaged(text);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Value_1(nint thisPtr, int* __return_value__)
	{
		int num = 0;
		*__return_value__ = 0;
		try
		{
			num = KeyValuePairMethods<string, int>.Abi_get_Value_1(thisPtr);
			*__return_value__ = num;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
