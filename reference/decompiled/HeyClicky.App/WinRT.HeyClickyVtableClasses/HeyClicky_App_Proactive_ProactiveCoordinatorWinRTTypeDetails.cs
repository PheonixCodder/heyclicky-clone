using System.Runtime.InteropServices;
using ABI.System;

namespace WinRT.HeyClickyVtableClasses;

internal sealed class HeyClicky_App_Proactive_ProactiveCoordinatorWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IDisposableMethods.IID,
				Vtable = IDisposableMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
