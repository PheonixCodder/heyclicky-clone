using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IEnumerator_System_DateTimeOffset
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IEnumeratorMethods<System.DateTimeOffset, ABI.System.DateTimeOffset>.InitCcw((delegate* unmanaged[Stdcall]<nint, ABI.System.DateTimeOffset*, int>)(delegate*<nint, ABI.System.DateTimeOffset*, int>)(&Do_Abi_get_Current_0), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(delegate*<nint, byte*, int>)(&Do_Abi_get_HasCurrent_1), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(delegate*<nint, byte*, int>)(&Do_Abi_MoveNext_2), (delegate* unmanaged[Stdcall]<nint, int, nint, uint*, int>)(delegate*<nint, int, nint, uint*, int>)(&Do_Abi_GetMany_3));
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_MoveNext_2(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<System.DateTimeOffset>.Abi_MoveNext_2(thisPtr);
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetMany_3(nint thisPtr, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		System.DateTimeOffset[] items2 = MarshalNonBlittable<System.DateTimeOffset>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IEnumeratorMethods<System.DateTimeOffset>.Abi_GetMany_3(thisPtr, ref items2);
			MarshalNonBlittable<System.DateTimeOffset>.CopyManagedArray(items2, items);
			*__return_value__ = num;
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Current_0(nint thisPtr, ABI.System.DateTimeOffset* __return_value__)
	{
		System.DateTimeOffset dateTimeOffset = default(System.DateTimeOffset);
		*__return_value__ = default(ABI.System.DateTimeOffset);
		try
		{
			dateTimeOffset = IEnumeratorMethods<System.DateTimeOffset>.Abi_get_Current_0(thisPtr);
			*__return_value__ = ABI.System.DateTimeOffset.FromManaged(dateTimeOffset);
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_HasCurrent_1(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<System.DateTimeOffset>.Abi_get_HasCurrent_1(thisPtr);
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
