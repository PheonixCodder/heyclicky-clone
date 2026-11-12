using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com.StructuredStorage;

[Guid("0000000B-0000-0000-C000-000000000046")]
[SupportedOSPlatform("windows5.0")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IStorage_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, STGM, uint, uint, out IStream_unmanaged*, HRESULT> CreateStream_4;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, void*, STGM, uint, out IStream_unmanaged*, HRESULT> OpenStream_5;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, STGM, uint, uint, out IStorage_unmanaged*, HRESULT> CreateStorage_6;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, IStorage_unmanaged*, STGM, ushort**, uint, out IStorage_unmanaged*, HRESULT> OpenStorage_7;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, Guid*, ushort**, IStorage_unmanaged*, HRESULT> CopyTo_8;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, IStorage_unmanaged*, PCWSTR, uint, HRESULT> MoveElementTo_9;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, HRESULT> Commit_10;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, HRESULT> Revert_11;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, void*, uint, out IEnumSTATSTG_unmanaged*, HRESULT> EnumElements_12;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, HRESULT> DestroyElement_13;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, PCWSTR, HRESULT> RenameElement_14;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, FILETIME*, FILETIME*, FILETIME*, HRESULT> SetElementTimes_15;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, Guid*, HRESULT> SetClass_16;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, uint, HRESULT> SetStateBits_17;

		internal unsafe delegate* unmanaged[Stdcall]<IStorage_unmanaged*, STATSTG*, uint, HRESULT> Stat_18;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(11, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				11, 0, 0, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((IStorage_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint>)lpVtbl[1])((IStorage_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint>)lpVtbl[2])((IStorage_unmanaged*)Unsafe.AsPointer(ref this));
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void CreateStream(string pwcsName, STGM grfMode, uint reserved1, uint reserved2, out IStream_unmanaged* ppstm)
	{
		fixed (char* ptr = pwcsName)
		{
			CreateStream(ptr, grfMode, reserved1, reserved2, out ppstm);
		}
	}

	public unsafe void CreateStream(PCWSTR pwcsName, STGM grfMode, uint reserved1, uint reserved2, out IStream_unmanaged* ppstm)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, STGM, uint, uint, out IStream_unmanaged*, HRESULT>)lpVtbl[3])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, grfMode, reserved1, reserved2, out ppstm).ThrowOnFailure();
	}

	internal unsafe void OpenStream(string pwcsName, STGM grfMode, uint reserved2, out IStream_unmanaged* ppstm)
	{
		fixed (char* ptr = pwcsName)
		{
			OpenStream(ptr, null, grfMode, reserved2, out ppstm);
		}
	}

	public unsafe void OpenStream(PCWSTR pwcsName, [Optional] void* reserved1, STGM grfMode, uint reserved2, out IStream_unmanaged* ppstm)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, void*, STGM, uint, out IStream_unmanaged*, HRESULT>)lpVtbl[4])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, reserved1, grfMode, reserved2, out ppstm).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void CreateStorage(string pwcsName, STGM grfMode, uint reserved1, uint reserved2, out IStorage_unmanaged* ppstg)
	{
		fixed (char* ptr = pwcsName)
		{
			CreateStorage(ptr, grfMode, reserved1, reserved2, out ppstg);
		}
	}

	public unsafe void CreateStorage(PCWSTR pwcsName, STGM grfMode, uint reserved1, uint reserved2, out IStorage_unmanaged* ppstg)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, STGM, uint, uint, out IStorage_unmanaged*, HRESULT>)lpVtbl[5])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, grfMode, reserved1, reserved2, out ppstg).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void OpenStorage(string pwcsName, IStorage_unmanaged* pstgPriority, STGM grfMode, in ushort* snbExclude, uint reserved, out IStorage_unmanaged* ppstg)
	{
		fixed (ushort** snbExclude2 = &snbExclude)
		{
			fixed (char* ptr = pwcsName)
			{
				OpenStorage(ptr, pstgPriority, grfMode, snbExclude2, reserved, out ppstg);
			}
		}
	}

	public unsafe void OpenStorage(PCWSTR pwcsName, IStorage_unmanaged* pstgPriority, STGM grfMode, ushort** snbExclude, uint reserved, out IStorage_unmanaged* ppstg)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, IStorage_unmanaged*, STGM, ushort**, uint, out IStorage_unmanaged*, HRESULT>)lpVtbl[6])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, pstgPriority, grfMode, snbExclude, reserved, out ppstg).ThrowOnFailure();
	}

	internal unsafe void CopyTo([Optional] ReadOnlySpan<Guid> rgiidExclude, in ushort* snbExclude, IStorage_unmanaged* pstgDest)
	{
		fixed (ushort** snbExclude2 = &snbExclude)
		{
			fixed (Guid* rgiidExclude2 = rgiidExclude)
			{
				CopyTo((uint)rgiidExclude.Length, rgiidExclude2, snbExclude2, pstgDest);
			}
		}
	}

	public unsafe void CopyTo(uint ciidExclude, [Optional] Guid* rgiidExclude, [Optional] ushort** snbExclude, IStorage_unmanaged* pstgDest)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, Guid*, ushort**, IStorage_unmanaged*, HRESULT>)lpVtbl[7])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), ciidExclude, rgiidExclude, snbExclude, pstgDest).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void MoveElementTo(string pwcsName, IStorage_unmanaged* pstgDest, string pwcsNewName, uint grfFlags)
	{
		//The blocks IL_0021 are reachable both inside and outside the pinned region starting at IL_001e. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		fixed (char* ptr2 = pwcsNewName)
		{
			char* intPtr;
			if (pwcsName == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				MoveElementTo(ptr, pstgDest, ptr2, grfFlags);
				return;
			}
			fixed (char* ptr3 = &pwcsName.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr3);
				MoveElementTo(ptr, pstgDest, ptr2, grfFlags);
			}
		}
	}

	public unsafe void MoveElementTo(PCWSTR pwcsName, IStorage_unmanaged* pstgDest, PCWSTR pwcsNewName, uint grfFlags)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, IStorage_unmanaged*, PCWSTR, uint, HRESULT>)lpVtbl[8])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, pstgDest, pwcsNewName, grfFlags).ThrowOnFailure();
	}

	public unsafe void Commit(uint grfCommitFlags)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, HRESULT>)lpVtbl[9])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), grfCommitFlags).ThrowOnFailure();
	}

	public unsafe void Revert()
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, HRESULT>)lpVtbl[10])((IStorage_unmanaged*)Unsafe.AsPointer(ref this)).ThrowOnFailure();
	}

	internal unsafe void EnumElements(out IEnumSTATSTG_unmanaged* ppenum)
	{
		EnumElements(0u, null, 0u, out ppenum);
	}

	public unsafe void EnumElements([Optional] uint reserved1, [Optional] void* reserved2, [Optional] uint reserved3, out IEnumSTATSTG_unmanaged* ppenum)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, void*, uint, out IEnumSTATSTG_unmanaged*, HRESULT>)lpVtbl[11])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), reserved1, reserved2, reserved3, out ppenum).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void DestroyElement(string pwcsName)
	{
		fixed (char* ptr = pwcsName)
		{
			DestroyElement(ptr);
		}
	}

	public unsafe void DestroyElement(PCWSTR pwcsName)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, HRESULT>)lpVtbl[12])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void RenameElement(string pwcsOldName, string pwcsNewName)
	{
		//The blocks IL_0021 are reachable both inside and outside the pinned region starting at IL_001e. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		fixed (char* ptr2 = pwcsNewName)
		{
			char* intPtr;
			if (pwcsOldName == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				RenameElement(ptr, ptr2);
				return;
			}
			fixed (char* ptr3 = &pwcsOldName.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr3);
				RenameElement(ptr, ptr2);
			}
		}
	}

	public unsafe void RenameElement(PCWSTR pwcsOldName, PCWSTR pwcsNewName)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, PCWSTR, HRESULT>)lpVtbl[13])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsOldName, pwcsNewName).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void SetElementTimes(string pwcsName, in FILETIME pctime, in FILETIME patime, in FILETIME pmtime)
	{
		fixed (FILETIME* pmtime2 = &pmtime)
		{
			fixed (FILETIME* patime2 = &patime)
			{
				fixed (FILETIME* pctime2 = &pctime)
				{
					fixed (char* ptr = pwcsName)
					{
						SetElementTimes(ptr, pctime2, patime2, pmtime2);
					}
				}
			}
		}
	}

	public unsafe void SetElementTimes(PCWSTR pwcsName, FILETIME* pctime, FILETIME* patime, FILETIME* pmtime)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, PCWSTR, FILETIME*, FILETIME*, FILETIME*, HRESULT>)lpVtbl[14])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pwcsName, pctime, patime, pmtime).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void SetClass(in Guid clsid)
	{
		fixed (Guid* @class = &clsid)
		{
			SetClass(@class);
		}
	}

	public unsafe void SetClass(Guid* clsid)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, Guid*, HRESULT>)lpVtbl[15])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), clsid).ThrowOnFailure();
	}

	public unsafe void SetStateBits(uint grfStateBits, uint grfMask)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, uint, uint, HRESULT>)lpVtbl[16])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), grfStateBits, grfMask).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void Stat(out STATSTG pstatstg, uint grfStatFlag)
	{
		fixed (STATSTG* pstatstg2 = &pstatstg)
		{
			Stat(pstatstg2, grfStatFlag);
		}
	}

	public unsafe void Stat(STATSTG* pstatstg, uint grfStatFlag)
	{
		((delegate* unmanaged[Stdcall]<IStorage_unmanaged*, STATSTG*, uint, HRESULT>)lpVtbl[17])((IStorage_unmanaged*)Unsafe.AsPointer(ref this), pstatstg, grfStatFlag).ThrowOnFailure();
	}
}
