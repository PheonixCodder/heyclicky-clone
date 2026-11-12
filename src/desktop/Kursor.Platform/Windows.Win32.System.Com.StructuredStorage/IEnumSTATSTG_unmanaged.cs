using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com.StructuredStorage;

[Guid("0000000D-0000-0000-C000-000000000046")]
[SupportedOSPlatform("windows5.0")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IEnumSTATSTG_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint, STATSTG*, uint*, HRESULT> Next_4;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint, HRESULT> Skip_5;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, HRESULT> Reset_6;

		internal unsafe delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, out IEnumSTATSTG_unmanaged*, HRESULT> Clone_7;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(13, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				13, 0, 0, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint>)lpVtbl[1])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint>)lpVtbl[2])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this));
	}

	internal unsafe void Next(Span<STATSTG> rgelt, out uint pceltFetched)
	{
		fixed (uint* pceltFetched2 = &pceltFetched)
		{
			fixed (STATSTG* rgelt2 = rgelt)
			{
				Next((uint)rgelt.Length, rgelt2, pceltFetched2);
			}
		}
	}

	internal unsafe void Next(Span<STATSTG> rgelt)
	{
		fixed (STATSTG* rgelt2 = rgelt)
		{
			Next((uint)rgelt.Length, rgelt2, null);
		}
	}

	public unsafe void Next(uint celt, STATSTG* rgelt, [Optional] uint* pceltFetched)
	{
		((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint, STATSTG*, uint*, HRESULT>)lpVtbl[3])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this), celt, rgelt, pceltFetched).ThrowOnFailure();
	}

	public unsafe void Skip(uint celt)
	{
		((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, uint, HRESULT>)lpVtbl[4])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this), celt).ThrowOnFailure();
	}

	public unsafe void Reset()
	{
		((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, HRESULT>)lpVtbl[5])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this)).ThrowOnFailure();
	}

	public unsafe void Clone(out IEnumSTATSTG_unmanaged* ppenum)
	{
		((delegate* unmanaged[Stdcall]<IEnumSTATSTG_unmanaged*, out IEnumSTATSTG_unmanaged*, HRESULT>)lpVtbl[6])((IEnumSTATSTG_unmanaged*)Unsafe.AsPointer(ref this), out ppenum).ThrowOnFailure();
	}
}
