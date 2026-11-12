using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com;

[Guid("00020400-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IDispatch_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, out uint, HRESULT> GetTypeInfoCount_4;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint, uint, out ITypeInfo_unmanaged*, HRESULT> GetTypeInfo_5;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, Guid*, PWSTR*, uint, uint, out int, HRESULT> GetIDsOfNames_6;

		internal unsafe delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, int, Guid*, uint, DISPATCH_FLAGS, in DISPPARAMS_unmanaged, out VARIANT_unmanaged, out EXCEPINFO_unmanaged, uint*, HRESULT> Invoke_7;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(132096, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				0, 4, 2, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((IDispatch_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint>)lpVtbl[1])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint>)lpVtbl[2])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe void GetTypeInfoCount(out uint pctinfo)
	{
		((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, out uint, HRESULT>)lpVtbl[3])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this), out pctinfo).ThrowOnFailure();
	}

	public unsafe void GetTypeInfo(uint iTInfo, uint lcid, out ITypeInfo_unmanaged* ppTInfo)
	{
		((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, uint, uint, out ITypeInfo_unmanaged*, HRESULT>)lpVtbl[4])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this), iTInfo, lcid, out ppTInfo).ThrowOnFailure();
	}

	internal unsafe void GetIDsOfNames(in Guid riid, ReadOnlySpan<PWSTR> rgszNames, uint lcid, out int rgDispId)
	{
		fixed (PWSTR* rgszNames2 = rgszNames)
		{
			fixed (Guid* riid2 = &riid)
			{
				GetIDsOfNames(riid2, rgszNames2, (uint)rgszNames.Length, lcid, out rgDispId);
			}
		}
	}

	public unsafe void GetIDsOfNames(Guid* riid, PWSTR* rgszNames, uint cNames, uint lcid, out int rgDispId)
	{
		((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, Guid*, PWSTR*, uint, uint, out int, HRESULT>)lpVtbl[5])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this), riid, rgszNames, cNames, lcid, out rgDispId).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void Invoke(int dispIdMember, in Guid riid, uint lcid, DISPATCH_FLAGS wFlags, in DISPPARAMS_unmanaged pDispParams, out VARIANT_unmanaged pVarResult, out EXCEPINFO_unmanaged pExcepInfo, out uint puArgErr)
	{
		fixed (uint* puArgErr2 = &puArgErr)
		{
			fixed (Guid* riid2 = &riid)
			{
				Invoke(dispIdMember, riid2, lcid, wFlags, in pDispParams, out pVarResult, out pExcepInfo, puArgErr2);
			}
		}
	}

	internal unsafe void Invoke(int dispIdMember, in Guid riid, uint lcid, DISPATCH_FLAGS wFlags, in DISPPARAMS_unmanaged pDispParams, out VARIANT_unmanaged pVarResult, out EXCEPINFO_unmanaged pExcepInfo)
	{
		fixed (Guid* riid2 = &riid)
		{
			Invoke(dispIdMember, riid2, lcid, wFlags, in pDispParams, out pVarResult, out pExcepInfo, null);
		}
	}

	public unsafe void Invoke(int dispIdMember, Guid* riid, uint lcid, DISPATCH_FLAGS wFlags, in DISPPARAMS_unmanaged pDispParams, out VARIANT_unmanaged pVarResult, out EXCEPINFO_unmanaged pExcepInfo, [Optional] uint* puArgErr)
	{
		((delegate* unmanaged[Stdcall]<IDispatch_unmanaged*, int, Guid*, uint, DISPATCH_FLAGS, in DISPPARAMS_unmanaged, out VARIANT_unmanaged, out EXCEPINFO_unmanaged, uint*, HRESULT>)lpVtbl[6])((IDispatch_unmanaged*)Unsafe.AsPointer(ref this), dispIdMember, riid, lcid, wFlags, in pDispParams, out pVarResult, out pExcepInfo, puArgErr).ThrowOnFailure();
	}
}
