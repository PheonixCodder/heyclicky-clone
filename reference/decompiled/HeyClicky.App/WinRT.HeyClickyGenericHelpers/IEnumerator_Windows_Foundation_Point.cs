using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;
using Windows.Foundation;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IEnumerator_Windows_Foundation_Point
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IEnumeratorMethods<Point, Point>.InitCcw((delegate* unmanaged[Stdcall]<nint, Point*, int>)(delegate*<nint, Point*, int>)(&Do_Abi_get_Current_0), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(delegate*<nint, byte*, int>)(&Do_Abi_get_HasCurrent_1), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(delegate*<nint, byte*, int>)(&Do_Abi_MoveNext_2), (delegate* unmanaged[Stdcall]<nint, int, nint, uint*, int>)(delegate*<nint, int, nint, uint*, int>)(&Do_Abi_GetMany_3));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_MoveNext_2(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<Point>.Abi_MoveNext_2(thisPtr);
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
	private unsafe static int Do_Abi_GetMany_3(nint thisPtr, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		Point[] items2 = MarshalBlittable<Point>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IEnumeratorMethods<Point>.Abi_GetMany_3(thisPtr, ref items2);
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
	private unsafe static int Do_Abi_get_Current_0(nint thisPtr, Point* __return_value__)
	{
		Point point = default(Point);
		*__return_value__ = default(Point);
		try
		{
			point = IEnumeratorMethods<Point>.Abi_get_Current_0(thisPtr);
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
	private unsafe static int Do_Abi_get_HasCurrent_1(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<Point>.Abi_get_HasCurrent_1(thisPtr);
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
