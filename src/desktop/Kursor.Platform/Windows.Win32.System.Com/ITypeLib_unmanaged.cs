using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

[Guid("00020402-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct ITypeLib_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint> GetTypeInfoCount_4;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint, out ITypeInfo_unmanaged*, HRESULT> GetTypeInfo_5;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint, TYPEKIND*, HRESULT> GetTypeInfoType_6;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, Guid*, out ITypeInfo_unmanaged*, HRESULT> GetTypeInfoOfGuid_7;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, TLIBATTR**, HRESULT> GetLibAttr_8;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, out ITypeComp_unmanaged*, HRESULT> GetTypeComp_9;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, int, BSTR*, BSTR*, out uint, BSTR*, HRESULT> GetDocumentation_10;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, PWSTR, uint, BOOL*, HRESULT> IsName_11;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, PWSTR, uint, out ITypeInfo_unmanaged*, out int, ref ushort, HRESULT> FindName_12;

		internal unsafe delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, TLIBATTR*, void> ReleaseTLibAttr_13;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(132098, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				2, 4, 2, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint>)lpVtbl[1])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint>)lpVtbl[2])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint GetTypeInfoCount()
	{
		return ((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint>)lpVtbl[3])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe void GetTypeInfo(uint index, out ITypeInfo_unmanaged* ppTInfo)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint, out ITypeInfo_unmanaged*, HRESULT>)lpVtbl[4])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), index, out ppTInfo).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetTypeInfoType(uint index, out TYPEKIND pTKind)
	{
		fixed (TYPEKIND* pTKind2 = &pTKind)
		{
			GetTypeInfoType(index, pTKind2);
		}
	}

	public unsafe void GetTypeInfoType(uint index, TYPEKIND* pTKind)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, uint, TYPEKIND*, HRESULT>)lpVtbl[5])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), index, pTKind).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetTypeInfoOfGuid(in Guid guid, out ITypeInfo_unmanaged* ppTinfo)
	{
		fixed (Guid* guid2 = &guid)
		{
			GetTypeInfoOfGuid(guid2, out ppTinfo);
		}
	}

	public unsafe void GetTypeInfoOfGuid(Guid* guid, out ITypeInfo_unmanaged* ppTinfo)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, Guid*, out ITypeInfo_unmanaged*, HRESULT>)lpVtbl[6])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), guid, out ppTinfo).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetLibAttr(out TLIBATTR* ppTLibAttr)
	{
		fixed (TLIBATTR** ppTLibAttr2 = &ppTLibAttr)
		{
			GetLibAttr(ppTLibAttr2);
		}
	}

	public unsafe void GetLibAttr(TLIBATTR** ppTLibAttr)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, TLIBATTR**, HRESULT>)lpVtbl[7])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), ppTLibAttr).ThrowOnFailure();
	}

	public unsafe void GetTypeComp(out ITypeComp_unmanaged* ppTComp)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, out ITypeComp_unmanaged*, HRESULT>)lpVtbl[8])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), out ppTComp).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetDocumentation(int index, out SysFreeStringSafeHandle pBstrName, out SysFreeStringSafeHandle pBstrDocString, out uint pdwHelpContext, out SysFreeStringSafeHandle pBstrHelpFile)
	{
		pBstrName = new SysFreeStringSafeHandle(0);
		pBstrDocString = new SysFreeStringSafeHandle(0);
		pBstrHelpFile = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		BSTR bSTR2 = default(BSTR);
		BSTR bSTR3 = default(BSTR);
		GetDocumentation(index, &bSTR, &bSTR2, out pdwHelpContext, &bSTR3);
		Marshal.InitHandle(pBstrName, (nint)bSTR);
		Marshal.InitHandle(pBstrDocString, (nint)bSTR2);
		Marshal.InitHandle(pBstrHelpFile, (nint)bSTR3);
	}

	internal unsafe void GetDocumentation(int index, out uint pdwHelpContext)
	{
		GetDocumentation(index, null, null, out pdwHelpContext, null);
	}

	public unsafe void GetDocumentation(int index, [Optional] BSTR* pBstrName, [Optional] BSTR* pBstrDocString, out uint pdwHelpContext, [Optional] BSTR* pBstrHelpFile)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, int, BSTR*, BSTR*, out uint, BSTR*, HRESULT>)lpVtbl[9])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), index, pBstrName, pBstrDocString, out pdwHelpContext, pBstrHelpFile).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void IsName(ref Span<char> szNameBuf, uint lHashVal, out BOOL pfName)
	{
		if (szNameBuf != null && szNameBuf.LastIndexOf('\0') == -1)
		{
			throw new ArgumentException("Required null terminator missing.", "szNameBuf");
		}
		fixed (BOOL* pfName2 = &pfName)
		{
			fixed (char* ptr = szNameBuf)
			{
				PWSTR szNameBuf2 = ptr;
				IsName(szNameBuf2, lHashVal, pfName2);
				szNameBuf = szNameBuf.Slice(0, szNameBuf2.Length);
			}
		}
	}

	public unsafe void IsName(PWSTR szNameBuf, uint lHashVal, BOOL* pfName)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, PWSTR, uint, BOOL*, HRESULT>)lpVtbl[10])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), szNameBuf, lHashVal, pfName).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void FindName(ref Span<char> szNameBuf, uint lHashVal, out ITypeInfo_unmanaged* ppTInfo, out int rgMemId, ref ushort pcFound)
	{
		if (szNameBuf != null && szNameBuf.LastIndexOf('\0') == -1)
		{
			throw new ArgumentException("Required null terminator missing.", "szNameBuf");
		}
		fixed (char* ptr = szNameBuf)
		{
			PWSTR szNameBuf2 = ptr;
			FindName(szNameBuf2, lHashVal, out ppTInfo, out rgMemId, ref pcFound);
			szNameBuf = szNameBuf.Slice(0, szNameBuf2.Length);
		}
	}

	public unsafe void FindName(PWSTR szNameBuf, uint lHashVal, out ITypeInfo_unmanaged* ppTInfo, out int rgMemId, ref ushort pcFound)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, PWSTR, uint, out ITypeInfo_unmanaged*, out int, ref ushort, HRESULT>)lpVtbl[11])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), szNameBuf, lHashVal, out ppTInfo, out rgMemId, ref pcFound).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void ReleaseTLibAttr(in TLIBATTR pTLibAttr)
	{
		fixed (TLIBATTR* pTLibAttr2 = &pTLibAttr)
		{
			ReleaseTLibAttr(pTLibAttr2);
		}
	}

	public unsafe void ReleaseTLibAttr(TLIBATTR* pTLibAttr)
	{
		((delegate* unmanaged[Stdcall]<ITypeLib_unmanaged*, TLIBATTR*, void>)lpVtbl[12])((ITypeLib_unmanaged*)Unsafe.AsPointer(ref this), pTLibAttr);
	}
}
