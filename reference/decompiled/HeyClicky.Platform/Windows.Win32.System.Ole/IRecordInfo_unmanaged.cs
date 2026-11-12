using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Ole;

[Guid("0000002F-0000-0000-C000-000000000046")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IRecordInfo_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT> RecordInit_4;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT> RecordClear_5;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, void*, HRESULT> RecordCopy_6;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, Guid*, HRESULT> GetGuid_7;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, BSTR*, HRESULT> GetName_8;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, out uint, HRESULT> GetSize_9;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, out ITypeInfo_unmanaged*, HRESULT> GetTypeInfo_10;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, PCWSTR, out VARIANT_unmanaged, HRESULT> GetField_11;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, PCWSTR, out VARIANT_unmanaged, void**, HRESULT> GetFieldNoCopy_12;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint, void*, PCWSTR, in VARIANT_unmanaged, HRESULT> PutField_13;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint, void*, PCWSTR, in VARIANT_unmanaged, HRESULT> PutFieldNoCopy_14;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, ref uint, BSTR*, HRESULT> GetFieldNames_15;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, IRecordInfo_unmanaged*, BOOL> IsMatchingType_16;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*> RecordCreate_17;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, void**, HRESULT> RecordCreateCopy_18;

		internal unsafe delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT> RecordDestroy_19;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(47, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				47, 0, 0, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint>)lpVtbl[1])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint>)lpVtbl[2])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe void RecordInit(void* pvNew)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT>)lpVtbl[3])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvNew).ThrowOnFailure();
	}

	public unsafe void RecordClear(void* pvExisting)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT>)lpVtbl[4])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvExisting).ThrowOnFailure();
	}

	public unsafe void RecordCopy(void* pvExisting, void* pvNew)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, void*, HRESULT>)lpVtbl[5])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvExisting, pvNew).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetGuid(out Guid pguid)
	{
		fixed (Guid* pguid2 = &pguid)
		{
			GetGuid(pguid2);
		}
	}

	public unsafe void GetGuid(Guid* pguid)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, Guid*, HRESULT>)lpVtbl[6])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pguid).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetName(out SysFreeStringSafeHandle pbstrName)
	{
		pbstrName = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		GetName(&bSTR);
		Marshal.InitHandle(pbstrName, (nint)bSTR);
	}

	public unsafe void GetName(BSTR* pbstrName)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, BSTR*, HRESULT>)lpVtbl[7])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pbstrName).ThrowOnFailure();
	}

	public unsafe void GetSize(out uint pcbSize)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, out uint, HRESULT>)lpVtbl[8])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), out pcbSize).ThrowOnFailure();
	}

	public unsafe void GetTypeInfo(out ITypeInfo_unmanaged* ppTypeInfo)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, out ITypeInfo_unmanaged*, HRESULT>)lpVtbl[9])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), out ppTypeInfo).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetField(void* pvData, string szFieldName, out VARIANT_unmanaged pvarField)
	{
		fixed (char* ptr = szFieldName)
		{
			GetField(pvData, ptr, out pvarField);
		}
	}

	public unsafe void GetField(void* pvData, PCWSTR szFieldName, out VARIANT_unmanaged pvarField)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, PCWSTR, out VARIANT_unmanaged, HRESULT>)lpVtbl[10])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvData, szFieldName, out pvarField).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetFieldNoCopy(void* pvData, string szFieldName, out VARIANT_unmanaged pvarField, out void* ppvDataCArray)
	{
		fixed (void** ppvDataCArray2 = &ppvDataCArray)
		{
			fixed (char* ptr = szFieldName)
			{
				GetFieldNoCopy(pvData, ptr, out pvarField, ppvDataCArray2);
			}
		}
	}

	public unsafe void GetFieldNoCopy(void* pvData, PCWSTR szFieldName, out VARIANT_unmanaged pvarField, void** ppvDataCArray)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, PCWSTR, out VARIANT_unmanaged, void**, HRESULT>)lpVtbl[11])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvData, szFieldName, out pvarField, ppvDataCArray).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void PutField(uint wFlags, void* pvData, string szFieldName, in VARIANT_unmanaged pvarField)
	{
		fixed (char* ptr = szFieldName)
		{
			PutField(wFlags, pvData, ptr, in pvarField);
		}
	}

	public unsafe void PutField(uint wFlags, void* pvData, PCWSTR szFieldName, in VARIANT_unmanaged pvarField)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint, void*, PCWSTR, in VARIANT_unmanaged, HRESULT>)lpVtbl[12])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), wFlags, pvData, szFieldName, in pvarField).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void PutFieldNoCopy(uint wFlags, void* pvData, string szFieldName, in VARIANT_unmanaged pvarField)
	{
		fixed (char* ptr = szFieldName)
		{
			PutFieldNoCopy(wFlags, pvData, ptr, in pvarField);
		}
	}

	public unsafe void PutFieldNoCopy(uint wFlags, void* pvData, PCWSTR szFieldName, in VARIANT_unmanaged pvarField)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, uint, void*, PCWSTR, in VARIANT_unmanaged, HRESULT>)lpVtbl[13])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), wFlags, pvData, szFieldName, in pvarField).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void GetFieldNames(ref uint pcNames, Span<BSTR> rgBstrNames)
	{
		fixed (BSTR* rgBstrNames2 = rgBstrNames)
		{
			GetFieldNames(ref pcNames, rgBstrNames2);
		}
	}

	public unsafe void GetFieldNames(ref uint pcNames, BSTR* rgBstrNames)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, ref uint, BSTR*, HRESULT>)lpVtbl[14])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), ref pcNames, rgBstrNames).ThrowOnFailure();
	}

	public unsafe BOOL IsMatchingType(IRecordInfo_unmanaged* pRecordInfo)
	{
		return ((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, IRecordInfo_unmanaged*, BOOL>)lpVtbl[15])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pRecordInfo);
	}

	public unsafe void* RecordCreate()
	{
		return ((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*>)lpVtbl[16])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this));
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void RecordCreateCopy(void* pvSource, out void* ppvDest)
	{
		fixed (void** ppvDest2 = &ppvDest)
		{
			RecordCreateCopy(pvSource, ppvDest2);
		}
	}

	public unsafe void RecordCreateCopy(void* pvSource, void** ppvDest)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, void**, HRESULT>)lpVtbl[17])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvSource, ppvDest).ThrowOnFailure();
	}

	public unsafe void RecordDestroy(void* pvRecord)
	{
		((delegate* unmanaged[Stdcall]<IRecordInfo_unmanaged*, void*, HRESULT>)lpVtbl[18])((IRecordInfo_unmanaged*)Unsafe.AsPointer(ref this), pvRecord).ThrowOnFailure();
	}
}
