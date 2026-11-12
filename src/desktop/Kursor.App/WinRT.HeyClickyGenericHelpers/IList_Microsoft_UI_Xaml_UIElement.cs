using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml;
using ABI.System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace WinRT.HeyClickyGenericHelpers;

internal static class IList_Microsoft_UI_Xaml_UIElement
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IListMethods<Microsoft.UI.Xaml.UIElement, nint>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_GetView_2), (delegate* unmanaged[Stdcall]<nint, nint, uint*, byte*, int>)(&Do_Abi_IndexOf_3), (delegate* unmanaged[Stdcall]<nint, uint, nint, int>)(&Do_Abi_SetAt_4), (delegate* unmanaged[Stdcall]<nint, uint, nint, int>)(&Do_Abi_InsertAt_5), (delegate* unmanaged[Stdcall]<nint, uint, int>)(&Do_Abi_RemoveAt_6), (delegate* unmanaged[Stdcall]<nint, nint, int>)(&Do_Abi_Append_7), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_RemoveAtEnd_8), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_Clear_9), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_10), (delegate* unmanaged[Stdcall]<nint, int, nint, int>)(&Do_Abi_ReplaceAll_11));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, nint* __return_value__)
	{
		Microsoft.UI.Xaml.UIElement uIElement = null;
		*__return_value__ = 0;
		try
		{
			uIElement = IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_GetAt_0(thisPtr, index);
			*__return_value__ = ABI.Microsoft.UI.Xaml.UIElement.FromManaged(uIElement);
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
		IReadOnlyList<Microsoft.UI.Xaml.UIElement> readOnlyList = null;
		*__return_value__ = 0;
		try
		{
			readOnlyList = IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_GetView_2(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyList<Microsoft.UI.Xaml.UIElement>>.FromManaged(readOnlyList);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_IndexOf_3(nint thisPtr, nint value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_IndexOf_3(thisPtr, ABI.Microsoft.UI.Xaml.UIElement.FromAbi(value), out index2);
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
	private static int Do_Abi_SetAt_4(nint thisPtr, uint index, nint value)
	{
		try
		{
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_SetAt_4(thisPtr, index, ABI.Microsoft.UI.Xaml.UIElement.FromAbi(value));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_InsertAt_5(nint thisPtr, uint index, nint value)
	{
		try
		{
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_InsertAt_5(thisPtr, index, ABI.Microsoft.UI.Xaml.UIElement.FromAbi(value));
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
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_RemoveAt_6(thisPtr, index);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Append_7(nint thisPtr, nint value)
	{
		try
		{
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_Append_7(thisPtr, ABI.Microsoft.UI.Xaml.UIElement.FromAbi(value));
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
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_RemoveAtEnd_8(thisPtr);
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
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_Clear_9(thisPtr);
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
		Microsoft.UI.Xaml.UIElement[] items2 = ABI.Microsoft.UI.Xaml.UIElement.FromAbiArray((__itemsSize, items));
		try
		{
			num = IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_GetMany_10(thisPtr, startIndex, ref items2);
			Marshaler<Microsoft.UI.Xaml.UIElement>.CopyManagedArray(items2, items);
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
			IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_ReplaceAll_11(thisPtr, ABI.Microsoft.UI.Xaml.UIElement.FromAbiArray((__itemsSize, items)));
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
			num = IListMethods<Microsoft.UI.Xaml.UIElement>.Abi_get_Size_1(thisPtr);
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
