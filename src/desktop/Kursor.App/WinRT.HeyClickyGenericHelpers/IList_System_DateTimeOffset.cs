using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IList_System_DateTimeOffset
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IListMethods<System.DateTimeOffset, ABI.System.DateTimeOffset>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, ABI.System.DateTimeOffset*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_GetView_2), (delegate* unmanaged[Stdcall]<nint, ABI.System.DateTimeOffset, uint*, byte*, int>)(&Do_Abi_IndexOf_3), (delegate* unmanaged[Stdcall]<nint, uint, ABI.System.DateTimeOffset, int>)(&Do_Abi_SetAt_4), (delegate* unmanaged[Stdcall]<nint, uint, ABI.System.DateTimeOffset, int>)(&Do_Abi_InsertAt_5), (delegate* unmanaged[Stdcall]<nint, uint, int>)(&Do_Abi_RemoveAt_6), (delegate* unmanaged[Stdcall]<nint, ABI.System.DateTimeOffset, int>)(&Do_Abi_Append_7), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_RemoveAtEnd_8), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_Clear_9), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_10), (delegate* unmanaged[Stdcall]<nint, int, nint, int>)(&Do_Abi_ReplaceAll_11));
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, ABI.System.DateTimeOffset* __return_value__)
	{
		System.DateTimeOffset dateTimeOffset = default(System.DateTimeOffset);
		*__return_value__ = default(ABI.System.DateTimeOffset);
		try
		{
			dateTimeOffset = IListMethods<System.DateTimeOffset>.Abi_GetAt_0(thisPtr, index);
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
	private unsafe static int Do_Abi_GetView_2(nint thisPtr, nint* __return_value__)
	{
		IReadOnlyList<System.DateTimeOffset> readOnlyList = null;
		*__return_value__ = 0;
		try
		{
			readOnlyList = IListMethods<System.DateTimeOffset>.Abi_GetView_2(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyList<System.DateTimeOffset>>.FromManaged(readOnlyList);
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_IndexOf_3(nint thisPtr, ABI.System.DateTimeOffset value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IListMethods<System.DateTimeOffset>.Abi_IndexOf_3(thisPtr, ABI.System.DateTimeOffset.FromAbi(value), out index2);
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
	private static int Do_Abi_SetAt_4(nint thisPtr, uint index, ABI.System.DateTimeOffset value)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_SetAt_4(thisPtr, index, ABI.System.DateTimeOffset.FromAbi(value));
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_InsertAt_5(nint thisPtr, uint index, ABI.System.DateTimeOffset value)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_InsertAt_5(thisPtr, index, ABI.System.DateTimeOffset.FromAbi(value));
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAt_6(nint thisPtr, uint index)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_RemoveAt_6(thisPtr, index);
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Append_7(nint thisPtr, ABI.System.DateTimeOffset value)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_Append_7(thisPtr, ABI.System.DateTimeOffset.FromAbi(value));
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAtEnd_8(nint thisPtr)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_RemoveAtEnd_8(thisPtr);
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Clear_9(nint thisPtr)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_Clear_9(thisPtr);
		}
		catch (System.Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new System.Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetMany_10(nint thisPtr, uint startIndex, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		System.DateTimeOffset[] items2 = MarshalNonBlittable<System.DateTimeOffset>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IListMethods<System.DateTimeOffset>.Abi_GetMany_10(thisPtr, startIndex, ref items2);
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
	private static int Do_Abi_ReplaceAll_11(nint thisPtr, int __itemsSize, nint items)
	{
		try
		{
			IListMethods<System.DateTimeOffset>.Abi_ReplaceAll_11(thisPtr, MarshalNonBlittable<System.DateTimeOffset>.FromAbiArray((__itemsSize, items)));
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
			num = IListMethods<System.DateTimeOffset>.Abi_get_Size_1(thisPtr);
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
