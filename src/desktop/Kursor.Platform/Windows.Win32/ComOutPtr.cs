using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class ComOutPtr
{
	internal unsafe static void* FromManaged(object value, in Guid iid)
	{
		if (value == null)
		{
			return null;
		}
		nint identity = GetIdentity(value);
		try
		{
			nint requestedInterface;
			int num = QueryInterface(identity, in iid, out requestedInterface);
			if (num < 0)
			{
				if (requestedInterface != 0)
				{
					Marshal.Release(requestedInterface);
				}
				Marshal.ThrowExceptionForHR(num);
			}
			return (void*)requestedInterface;
		}
		finally
		{
			FreeIdentity(identity);
		}
	}

	private static int QueryInterface(nint identity, in Guid iid, out nint requestedInterface)
	{
		Guid iid2 = iid;
		return Marshal.QueryInterface(identity, in iid2, out requestedInterface);
	}

	private static nint GetIdentity(object value)
	{
		return ComOrWinRTObjectMarshaller.ConvertToUnmanaged(value);
	}

	private static void FreeIdentity(nint identity)
	{
		ComOrWinRTObjectMarshaller.Free(identity);
	}
}
