using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class ComOrWinRTObjectMarshaller
{
	private const int E_NOINTERFACE = -2147467262;

	private static readonly Guid IID_IUnknown = new Guid(0, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	private static readonly Guid IID_IInspectable = new Guid(2944852704u, 45357, 19562, 156, 90, 215, 170, 101, 16, 30, 144);

	internal static Guid GetIID<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : class
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(object))
		{
			return IID_IUnknown;
		}
		if (!Projections.IsTypeWindowsRuntimeType(typeFromHandle))
		{
			return typeFromHandle.GUID;
		}
		if (typeFromHandle.IsClass)
		{
			ProjectedRuntimeClassAttribute customAttribute = typeFromHandle.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
			if (customAttribute != null)
			{
				return GetRuntimeClassIID(customAttribute);
			}
		}
		return GuidGenerator.CreateIID(typeFromHandle);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Type-based C#/WinRT projections supply trim-safe GUID metadata. CreateIID requires public fields only for legacy projections, which this method rejects.")]
	private static Guid GetRuntimeClassIID(ProjectedRuntimeClassAttribute projection)
	{
		return GuidGenerator.CreateIID(projection.DefaultInterface ?? throw new NotSupportedException("Runtime-class outputs require a C#/WinRT projection that identifies its default interface by type."));
	}

	internal static object ConvertToManaged(nint value)
	{
		if (value == 0)
		{
			return null;
		}
		Guid iid = IID_IInspectable;
		nint ppv;
		int num = Marshal.QueryInterface(value, in iid, out ppv);
		if (num >= 0)
		{
			try
			{
				return MarshalInspectable<object>.FromAbi(ppv);
			}
			finally
			{
				Marshal.Release(ppv);
			}
		}
		if (num != -2147467262)
		{
			Marshal.ThrowExceptionForHR(num);
		}
		return Marshal.GetObjectForIUnknown(value);
	}

	internal static object ConvertToManaged(object value)
	{
		if (value == null)
		{
			return null;
		}
		nint iUnknownForObject = Marshal.GetIUnknownForObject(value);
		try
		{
			Guid iid = IID_IInspectable;
			nint ppv;
			int num = Marshal.QueryInterface(iUnknownForObject, in iid, out ppv);
			if (num >= 0)
			{
				try
				{
					return MarshalInspectable<object>.FromAbi(ppv);
				}
				finally
				{
					Marshal.Release(ppv);
				}
			}
			if (num != -2147467262)
			{
				Marshal.ThrowExceptionForHR(num);
			}
			return value;
		}
		finally
		{
			Marshal.Release(iUnknownForObject);
		}
	}

	internal static nint ConvertToUnmanaged(object value)
	{
		if (value == null)
		{
			return 0;
		}
		if (ComWrappers.TryGetComInstance(value, out var unknown))
		{
			return unknown;
		}
		return Marshal.GetIUnknownForObject(value);
	}

	internal static void Free(nint value)
	{
		if (value != 0)
		{
			Marshal.Release(value);
		}
	}
}
