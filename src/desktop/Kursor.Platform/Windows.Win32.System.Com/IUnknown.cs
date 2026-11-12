using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

[Guid("00000000-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IUnknown : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IUnknown*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IUnknown*, uint> Release_3;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(0, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				0, 0, 0, 0, 0, 0, 0, 0, 192, 0,
				0, 0, 0, 0, 0, 70
			}));
		}
	}

	internal unsafe HRESULT QueryInterface<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(out T ppvObject) where T : class
	{
		void* ptr = null;
		try
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			HRESULT result = QueryInterface(&iID, &ptr);
			ppvObject = (T)ComOrWinRTObjectMarshaller.ConvertToManaged((nint)ptr);
			return result;
		}
		finally
		{
			if (ptr != null)
			{
				Marshal.Release((nint)ptr);
			}
		}
	}

	public unsafe HRESULT QueryInterface(Guid* riid, void** ppvObject)
	{
		return ((delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)(*lpVtbl))((IUnknown*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IUnknown*, uint>)lpVtbl[1])((IUnknown*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IUnknown*, uint>)lpVtbl[2])((IUnknown*)Unsafe.AsPointer(ref this));
	}
}
