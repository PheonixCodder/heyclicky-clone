using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com;

[Guid("00020401-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct ITypeInfo_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, TYPEATTR**, HRESULT> GetTypeAttr_4;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, out ITypeComp_unmanaged*, HRESULT> GetTypeComp_5;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out FUNCDESC_unmanaged*, HRESULT> GetFuncDesc_6;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out VARDESC_unmanaged*, HRESULT> GetVarDesc_7;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, uint, out uint, HRESULT> GetNames_8;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out uint, HRESULT> GetRefTypeOfImplType_9;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, IMPLTYPEFLAGS*, HRESULT> GetImplTypeFlags_10;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, PWSTR*, uint, out int, HRESULT> GetIDsOfNames_11;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, void*, int, DISPATCH_FLAGS, ref DISPPARAMS_unmanaged, out VARIANT_unmanaged, out EXCEPINFO_unmanaged, out uint, HRESULT> Invoke_12;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, BSTR*, out uint, BSTR*, HRESULT> GetDocumentation_13;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, INVOKEKIND, BSTR*, BSTR*, out ushort, HRESULT> GetDllEntry_14;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out ITypeInfo_unmanaged*, HRESULT> GetRefTypeInfo_15;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, INVOKEKIND, void**, HRESULT> AddressOfMember_16;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, IUnknown*, Guid*, void**, HRESULT> CreateInstance_17;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, HRESULT> GetMops_18;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, out ITypeLib_unmanaged*, out uint, HRESULT> GetContainingTypeLib_19;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, TYPEATTR*, void> ReleaseTypeAttr_20;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, in FUNCDESC_unmanaged, void> ReleaseFuncDesc_21;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, in VARDESC_unmanaged, void> ReleaseVarDesc_22;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(132097, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				1, 4, 2, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint>)lpVtbl[1])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint>)lpVtbl[2])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this));
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetTypeAttr(out TYPEATTR* ppTypeAttr)
	{
		fixed (TYPEATTR** ppTypeAttr2 = &ppTypeAttr)
		{
			GetTypeAttr(ppTypeAttr2);
		}
	}

	public unsafe void GetTypeAttr(TYPEATTR** ppTypeAttr)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, TYPEATTR**, HRESULT>)lpVtbl[3])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), ppTypeAttr).ThrowOnFailure();
	}

	public unsafe void GetTypeComp(out ITypeComp_unmanaged* ppTComp)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, out ITypeComp_unmanaged*, HRESULT>)lpVtbl[4])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), out ppTComp).ThrowOnFailure();
	}

	public unsafe void GetFuncDesc(uint index, out FUNCDESC_unmanaged* ppFuncDesc)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out FUNCDESC_unmanaged*, HRESULT>)lpVtbl[5])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), index, out ppFuncDesc).ThrowOnFailure();
	}

	public unsafe void GetVarDesc(uint index, out VARDESC_unmanaged* ppVarDesc)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out VARDESC_unmanaged*, HRESULT>)lpVtbl[6])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), index, out ppVarDesc).ThrowOnFailure();
	}

	internal unsafe void GetNames(int memid, Span<BSTR> rgBstrNames, out uint pcNames)
	{
		fixed (BSTR* rgBstrNames2 = rgBstrNames)
		{
			GetNames(memid, rgBstrNames2, (uint)rgBstrNames.Length, out pcNames);
		}
	}

	public unsafe void GetNames(int memid, BSTR* rgBstrNames, uint cMaxNames, out uint pcNames)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, uint, out uint, HRESULT>)lpVtbl[7])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), memid, rgBstrNames, cMaxNames, out pcNames).ThrowOnFailure();
	}

	public unsafe void GetRefTypeOfImplType(uint index, out uint pRefType)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out uint, HRESULT>)lpVtbl[8])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), index, out pRefType).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetImplTypeFlags(uint index, out IMPLTYPEFLAGS pImplTypeFlags)
	{
		fixed (IMPLTYPEFLAGS* pImplTypeFlags2 = &pImplTypeFlags)
		{
			GetImplTypeFlags(index, pImplTypeFlags2);
		}
	}

	public unsafe void GetImplTypeFlags(uint index, IMPLTYPEFLAGS* pImplTypeFlags)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, IMPLTYPEFLAGS*, HRESULT>)lpVtbl[9])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), index, pImplTypeFlags).ThrowOnFailure();
	}

	internal unsafe void GetIDsOfNames(ReadOnlySpan<PWSTR> rgszNames, out int pMemId)
	{
		fixed (PWSTR* rgszNames2 = rgszNames)
		{
			GetIDsOfNames(rgszNames2, (uint)rgszNames.Length, out pMemId);
		}
	}

	public unsafe void GetIDsOfNames(PWSTR* rgszNames, uint cNames, out int pMemId)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, PWSTR*, uint, out int, HRESULT>)lpVtbl[10])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), rgszNames, cNames, out pMemId).ThrowOnFailure();
	}

	public unsafe void Invoke(void* pvInstance, int memid, DISPATCH_FLAGS wFlags, ref DISPPARAMS_unmanaged pDispParams, out VARIANT_unmanaged pVarResult, out EXCEPINFO_unmanaged pExcepInfo, out uint puArgErr)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, void*, int, DISPATCH_FLAGS, ref DISPPARAMS_unmanaged, out VARIANT_unmanaged, out EXCEPINFO_unmanaged, out uint, HRESULT>)lpVtbl[11])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), pvInstance, memid, wFlags, ref pDispParams, out pVarResult, out pExcepInfo, out puArgErr).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetDocumentation(int memid, out SysFreeStringSafeHandle pBstrName, out SysFreeStringSafeHandle pBstrDocString, out uint pdwHelpContext, out SysFreeStringSafeHandle pBstrHelpFile)
	{
		pBstrName = new SysFreeStringSafeHandle(0);
		pBstrDocString = new SysFreeStringSafeHandle(0);
		pBstrHelpFile = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		BSTR bSTR2 = default(BSTR);
		BSTR bSTR3 = default(BSTR);
		GetDocumentation(memid, &bSTR, &bSTR2, out pdwHelpContext, &bSTR3);
		Marshal.InitHandle(pBstrName, (nint)bSTR);
		Marshal.InitHandle(pBstrDocString, (nint)bSTR2);
		Marshal.InitHandle(pBstrHelpFile, (nint)bSTR3);
	}

	internal unsafe void GetDocumentation(int memid, out uint pdwHelpContext)
	{
		GetDocumentation(memid, null, null, out pdwHelpContext, null);
	}

	public unsafe void GetDocumentation(int memid, [Optional] BSTR* pBstrName, [Optional] BSTR* pBstrDocString, out uint pdwHelpContext, [Optional] BSTR* pBstrHelpFile)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, BSTR*, out uint, BSTR*, HRESULT>)lpVtbl[12])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), memid, pBstrName, pBstrDocString, out pdwHelpContext, pBstrHelpFile).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetDllEntry(int memid, INVOKEKIND invKind, out SysFreeStringSafeHandle pBstrDllName, out SysFreeStringSafeHandle pBstrName, out ushort pwOrdinal)
	{
		pBstrDllName = new SysFreeStringSafeHandle(0);
		pBstrName = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		BSTR bSTR2 = default(BSTR);
		GetDllEntry(memid, invKind, &bSTR, &bSTR2, out pwOrdinal);
		Marshal.InitHandle(pBstrDllName, (nint)bSTR);
		Marshal.InitHandle(pBstrName, (nint)bSTR2);
	}

	internal unsafe void GetDllEntry(int memid, INVOKEKIND invKind, out ushort pwOrdinal)
	{
		GetDllEntry(memid, invKind, null, null, out pwOrdinal);
	}

	public unsafe void GetDllEntry(int memid, INVOKEKIND invKind, [Optional] BSTR* pBstrDllName, [Optional] BSTR* pBstrName, out ushort pwOrdinal)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, INVOKEKIND, BSTR*, BSTR*, out ushort, HRESULT>)lpVtbl[13])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), memid, invKind, pBstrDllName, pBstrName, out pwOrdinal).ThrowOnFailure();
	}

	public unsafe void GetRefTypeInfo(uint hRefType, out ITypeInfo_unmanaged* ppTInfo)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, uint, out ITypeInfo_unmanaged*, HRESULT>)lpVtbl[14])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), hRefType, out ppTInfo).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void AddressOfMember(int memid, INVOKEKIND invKind, out void* ppv)
	{
		fixed (void** ppv2 = &ppv)
		{
			AddressOfMember(memid, invKind, ppv2);
		}
	}

	public unsafe void AddressOfMember(int memid, INVOKEKIND invKind, void** ppv)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, INVOKEKIND, void**, HRESULT>)lpVtbl[15])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), memid, invKind, ppv).ThrowOnFailure();
	}

	internal unsafe void CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(IUnknown* pUnkOuter, out T ppvObj) where T : class
	{
		void* ptr = null;
		try
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			CreateInstance(pUnkOuter, &iID, &ptr);
			ppvObj = (T)ComOrWinRTObjectMarshaller.ConvertToManaged((nint)ptr);
		}
		finally
		{
			if (ptr != null)
			{
				Marshal.Release((nint)ptr);
			}
		}
	}

	public unsafe void CreateInstance(IUnknown* pUnkOuter, Guid* riid, void** ppvObj)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, IUnknown*, Guid*, void**, HRESULT>)lpVtbl[16])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), pUnkOuter, riid, ppvObj).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetMops(int memid, out SysFreeStringSafeHandle pBstrMops)
	{
		pBstrMops = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		GetMops(memid, &bSTR);
		Marshal.InitHandle(pBstrMops, (nint)bSTR);
	}

	public unsafe void GetMops(int memid, BSTR* pBstrMops)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, int, BSTR*, HRESULT>)lpVtbl[17])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), memid, pBstrMops).ThrowOnFailure();
	}

	public unsafe void GetContainingTypeLib(out ITypeLib_unmanaged* ppTLib, out uint pIndex)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, out ITypeLib_unmanaged*, out uint, HRESULT>)lpVtbl[18])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), out ppTLib, out pIndex).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void ReleaseTypeAttr(in TYPEATTR pTypeAttr)
	{
		fixed (TYPEATTR* pTypeAttr2 = &pTypeAttr)
		{
			ReleaseTypeAttr(pTypeAttr2);
		}
	}

	public unsafe void ReleaseTypeAttr(TYPEATTR* pTypeAttr)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, TYPEATTR*, void>)lpVtbl[19])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), pTypeAttr);
	}

	public unsafe void ReleaseFuncDesc(in FUNCDESC_unmanaged pFuncDesc)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, in FUNCDESC_unmanaged, void>)lpVtbl[20])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), in pFuncDesc);
	}

	public unsafe void ReleaseVarDesc(in VARDESC_unmanaged pVarDesc)
	{
		((delegate* unmanaged[Stdcall]<ITypeInfo_unmanaged*, in VARDESC_unmanaged, void>)lpVtbl[21])((ITypeInfo_unmanaged*)Unsafe.AsPointer(ref this), in pVarDesc);
	}
}
