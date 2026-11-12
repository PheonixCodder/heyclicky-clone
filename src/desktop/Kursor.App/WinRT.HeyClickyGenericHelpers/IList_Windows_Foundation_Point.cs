using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;
using Windows.Foundation;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IList_Windows_Foundation_Point
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IListMethods<Point, Point>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, Point*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_GetView_2), (delegate* unmanaged[Stdcall]<nint, Point, uint*, byte*, int>)(&Do_Abi_IndexOf_3), (delegate* unmanaged[Stdcall]<nint, uint, Point, int>)(&Do_Abi_SetAt_4), (delegate* unmanaged[Stdcall]<nint, uint, Point, int>)(&Do_Abi_InsertAt_5), (delegate* unmanaged[Stdcall]<nint, uint, int>)(&Do_Abi_RemoveAt_6), (delegate* unmanaged[Stdcall]<nint, Point, int>)(&Do_Abi_Append_7), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_RemoveAtEnd_8), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_Clear_9), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_10), (delegate* unmanaged[Stdcall]<nint, int, nint, int>)(&Do_Abi_ReplaceAll_11));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, Point* __return_value__)
	{
		Point point = default(Point);
		*__return_value__ = default(Point);
		try
		{
			point = IListMethods<Point>.Abi_GetAt_0(thisPtr, index);
			*__return_value__ = point;
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
		IReadOnlyList<Point> readOnlyList = null;
		*__return_value__ = 0;
		try
		{
			readOnlyList = IListMethods<Point>.Abi_GetView_2(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyList<Point>>.FromManaged(readOnlyList);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_IndexOf_3(nint thisPtr, Point value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IListMethods<Point>.Abi_IndexOf_3(thisPtr, value, out index2);
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
	private static int Do_Abi_SetAt_4(nint thisPtr, uint index, Point value)
	{
		try
		{
			IListMethods<Point>.Abi_SetAt_4(thisPtr, index, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_InsertAt_5(nint thisPtr, uint index, Point value)
	{
		try
		{
			IListMethods<Point>.Abi_InsertAt_5(thisPtr, index, value);
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
			IListMethods<Point>.Abi_RemoveAt_6(thisPtr, index);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Append_7(nint thisPtr, Point value)
	{
		try
		{
			IListMethods<Point>.Abi_Append_7(thisPtr, value);
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
			IListMethods<Point>.Abi_RemoveAtEnd_8(thisPtr);
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
			IListMethods<Point>.Abi_Clear_9(thisPtr);
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
		Point[] items2 = MarshalBlittable<Point>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IListMethods<Point>.Abi_GetMany_10(thisPtr, startIndex, ref items2);
			MarshalBlittable<Point>.CopyManagedArray(items2, items);
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
			IListMethods<Point>.Abi_ReplaceAll_11(thisPtr, MarshalBlittable<Point>.FromAbiArray((__itemsSize, items)));
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
			num = IListMethods<Point>.Abi_get_Size_1(thisPtr);
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
