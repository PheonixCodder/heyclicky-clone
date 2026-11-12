using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.HeyClicky_DrawingGenericHelpers;

internal static class IList_System_Numerics_Vector2
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IListMethods<Vector2, Vector2>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, Vector2*, int>)(delegate*<nint, uint, Vector2*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(delegate*<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(delegate*<nint, nint*, int>)(&Do_Abi_GetView_2), (delegate* unmanaged[Stdcall]<nint, Vector2, uint*, byte*, int>)(delegate*<nint, Vector2, uint*, byte*, int>)(&Do_Abi_IndexOf_3), (delegate* unmanaged[Stdcall]<nint, uint, Vector2, int>)(delegate*<nint, uint, Vector2, int>)(&Do_Abi_SetAt_4), (delegate* unmanaged[Stdcall]<nint, uint, Vector2, int>)(delegate*<nint, uint, Vector2, int>)(&Do_Abi_InsertAt_5), (delegate* unmanaged[Stdcall]<nint, uint, int>)(delegate*<nint, uint, int>)(&Do_Abi_RemoveAt_6), (delegate* unmanaged[Stdcall]<nint, Vector2, int>)(delegate*<nint, Vector2, int>)(&Do_Abi_Append_7), (delegate* unmanaged[Stdcall]<nint, int>)(delegate*<nint, int>)(&Do_Abi_RemoveAtEnd_8), (delegate* unmanaged[Stdcall]<nint, int>)(delegate*<nint, int>)(&Do_Abi_Clear_9), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(delegate*<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_10), (delegate* unmanaged[Stdcall]<nint, int, nint, int>)(delegate*<nint, int, nint, int>)(&Do_Abi_ReplaceAll_11));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, Vector2* __return_value__)
	{
		Vector2 vector = default(Vector2);
		*__return_value__ = default(Vector2);
		try
		{
			vector = IListMethods<Vector2>.Abi_GetAt_0(thisPtr, index);
			*__return_value__ = vector;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetView_2(nint thisPtr, nint* __return_value__)
	{
		IReadOnlyList<Vector2> readOnlyList = null;
		*__return_value__ = 0;
		try
		{
			readOnlyList = IListMethods<Vector2>.Abi_GetView_2(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyList<Vector2>>.FromManaged(readOnlyList);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_IndexOf_3(nint thisPtr, Vector2 value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IListMethods<Vector2>.Abi_IndexOf_3(thisPtr, value, out index2);
			*index = index2;
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_SetAt_4(nint thisPtr, uint index, Vector2 value)
	{
		try
		{
			IListMethods<Vector2>.Abi_SetAt_4(thisPtr, index, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_InsertAt_5(nint thisPtr, uint index, Vector2 value)
	{
		try
		{
			IListMethods<Vector2>.Abi_InsertAt_5(thisPtr, index, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAt_6(nint thisPtr, uint index)
	{
		try
		{
			IListMethods<Vector2>.Abi_RemoveAt_6(thisPtr, index);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Append_7(nint thisPtr, Vector2 value)
	{
		try
		{
			IListMethods<Vector2>.Abi_Append_7(thisPtr, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAtEnd_8(nint thisPtr)
	{
		try
		{
			IListMethods<Vector2>.Abi_RemoveAtEnd_8(thisPtr);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Clear_9(nint thisPtr)
	{
		try
		{
			IListMethods<Vector2>.Abi_Clear_9(thisPtr);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetMany_10(nint thisPtr, uint startIndex, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		Vector2[] items2 = MarshalBlittable<Vector2>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IListMethods<Vector2>.Abi_GetMany_10(thisPtr, startIndex, ref items2);
			MarshalBlittable<Vector2>.CopyManagedArray(items2, items);
			*__return_value__ = num;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_ReplaceAll_11(nint thisPtr, int __itemsSize, nint items)
	{
		try
		{
			IListMethods<Vector2>.Abi_ReplaceAll_11(thisPtr, MarshalBlittable<Vector2>.FromAbiArray((__itemsSize, items)));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Size_1(nint thisPtr, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		try
		{
			num = IListMethods<Vector2>.Abi_get_Size_1(thisPtr);
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
