using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

[Guid("00020403-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct ITypeComp_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, PWSTR, uint, ushort, out ITypeInfo_unmanaged*, DESCKIND*, out BINDPTR_unmanaged, HRESULT> Bind_4;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, PWSTR, uint, out ITypeInfo_unmanaged*, out ITypeComp_unmanaged*, HRESULT> BindType_5;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(132099, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				3, 4, 2, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((ITypeComp_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, uint>)lpVtbl[1])((ITypeComp_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, uint>)lpVtbl[2])((ITypeComp_unmanaged*)Unsafe.AsPointer(ref this));
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void Bind(PWSTR szName, uint lHashVal, ushort wFlags, out ITypeInfo_unmanaged* ppTInfo, out DESCKIND pDescKind, out BINDPTR_unmanaged pBindPtr)
	{
		fixed (DESCKIND* pDescKind2 = &pDescKind)
		{
			Bind(szName, lHashVal, wFlags, out ppTInfo, pDescKind2, out pBindPtr);
		}
	}

	public unsafe void Bind(PWSTR szName, uint lHashVal, ushort wFlags, out ITypeInfo_unmanaged* ppTInfo, DESCKIND* pDescKind, out BINDPTR_unmanaged pBindPtr)
	{
		((delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, PWSTR, uint, ushort, out ITypeInfo_unmanaged*, DESCKIND*, out BINDPTR_unmanaged, HRESULT>)lpVtbl[3])((ITypeComp_unmanaged*)Unsafe.AsPointer(ref this), szName, lHashVal, wFlags, out ppTInfo, pDescKind, out pBindPtr).ThrowOnFailure();
	}

	public unsafe void BindType(PWSTR szName, uint lHashVal, out ITypeInfo_unmanaged* ppTInfo, out ITypeComp_unmanaged* ppTComp)
	{
		((delegate* unmanaged[Stdcall]<ITypeComp_unmanaged*, PWSTR, uint, out ITypeInfo_unmanaged*, out ITypeComp_unmanaged*, HRESULT>)lpVtbl[4])((ITypeComp_unmanaged*)Unsafe.AsPointer(ref this), szName, lHashVal, out ppTInfo, out ppTComp).ThrowOnFailure();
	}
}
