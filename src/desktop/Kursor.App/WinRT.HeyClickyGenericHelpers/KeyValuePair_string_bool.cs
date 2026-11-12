using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClickyGenericHelpers;

internal static class KeyValuePair_string_bool
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return KeyValuePairMethods<string, nint, bool, byte>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_get_Key_0), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(&Do_Abi_get_Value_1));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Key_0(nint thisPtr, nint* __return_value__)
	{
		string text = null;
		*__return_value__ = 0;
		try
		{
			text = KeyValuePairMethods<string, bool>.Abi_get_Key_0(thisPtr);
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
	private unsafe static int Do_Abi_get_Value_1(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = KeyValuePairMethods<string, bool>.Abi_get_Value_1(thisPtr);
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
