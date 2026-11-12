using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IReadOnlyList_System_DateTimeOffset
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IReadOnlyListMethods<System.DateTimeOffset, ABI.System.DateTimeOffset>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, ABI.System.DateTimeOffset*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, ABI.System.DateTimeOffset, uint*, byte*, int>)(&Do_Abi_IndexOf_2), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_3));
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, ABI.System.DateTimeOffset* __return_value__)
	{
		System.DateTimeOffset dateTimeOffset = default(System.DateTimeOffset);
		*__return_value__ = default(ABI.System.DateTimeOffset);
		try
		{
			dateTimeOffset = IReadOnlyListMethods<System.DateTimeOffset>.Abi_GetAt_0(thisPtr, index);
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
	private unsafe static int Do_Abi_IndexOf_2(nint thisPtr, ABI.System.DateTimeOffset value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IReadOnlyListMethods<System.DateTimeOffset>.Abi_IndexOf_2(thisPtr, ABI.System.DateTimeOffset.FromAbi(value), out index2);
			*index = index2;
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
	private unsafe static int Do_Abi_GetMany_3(nint thisPtr, uint startIndex, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		System.DateTimeOffset[] items2 = MarshalNonBlittable<System.DateTimeOffset>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IReadOnlyListMethods<System.DateTimeOffset>.Abi_GetMany_3(thisPtr, startIndex, ref items2);
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
	private unsafe static int Do_Abi_get_Size_1(nint thisPtr, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		try
		{
			num = IReadOnlyListMethods<System.DateTimeOffset>.Abi_get_Size_1(thisPtr);
			*__return_value__ = num;
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
