using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

[Guid("0000000C-0000-0000-C000-000000000046")]
[SupportedOSPlatform("windows5.0")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct IStream_unmanaged : IComIID
{
	internal struct Vtbl
	{
		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, Guid*, void**, HRESULT> QueryInterface_1;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint> AddRef_2;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint> Release_3;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, void*, uint, uint*, HRESULT> Read_4;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, void*, uint, uint*, HRESULT> Write_5;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, long, SeekOrigin, ulong*, HRESULT> Seek_6;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, HRESULT> SetSize_7;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, IStream_unmanaged*, ulong, ulong*, ulong*, HRESULT> CopyTo_8;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint, HRESULT> Commit_9;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, HRESULT> Revert_10;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, ulong, uint, HRESULT> LockRegion_11;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, ulong, uint, HRESULT> UnlockRegion_12;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, STATSTG*, uint, HRESULT> Stat_13;

		internal unsafe delegate* unmanaged[Stdcall]<IStream_unmanaged*, out IStream_unmanaged*, HRESULT> Clone_14;
	}

	private unsafe void** lpVtbl;

	internal static readonly Guid IID_Guid = new Guid(12, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)new byte[16]
			{
				12, 0, 0, 0, 0, 0, 0, 0, 192, 0,
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
		return ((delegate* unmanaged[Stdcall]<IStream_unmanaged*, Guid*, void**, HRESULT>)(*lpVtbl))((IStream_unmanaged*)Unsafe.AsPointer(ref this), riid, ppvObject);
	}

	public unsafe uint AddRef()
	{
		return ((delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint>)lpVtbl[1])((IStream_unmanaged*)Unsafe.AsPointer(ref this));
	}

	public unsafe uint Release()
	{
		return ((delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint>)lpVtbl[2])((IStream_unmanaged*)Unsafe.AsPointer(ref this));
	}

	internal unsafe HRESULT Read(Span<byte> pv, out uint pcbRead)
	{
		fixed (uint* pcbRead2 = &pcbRead)
		{
			fixed (byte* pv2 = pv)
			{
				return Read(pv2, (uint)pv.Length, pcbRead2);
			}
		}
	}

	internal unsafe HRESULT Read(Span<byte> pv)
	{
		fixed (byte* pv2 = pv)
		{
			return Read(pv2, (uint)pv.Length, null);
		}
	}

	public unsafe HRESULT Read(void* pv, uint cb, [Optional] uint* pcbRead)
	{
		return ((delegate* unmanaged[Stdcall]<IStream_unmanaged*, void*, uint, uint*, HRESULT>)lpVtbl[3])((IStream_unmanaged*)Unsafe.AsPointer(ref this), pv, cb, pcbRead);
	}

	internal unsafe HRESULT Write(ReadOnlySpan<byte> pv, out uint pcbWritten)
	{
		fixed (uint* pcbWritten2 = &pcbWritten)
		{
			fixed (byte* pv2 = pv)
			{
				return Write(pv2, (uint)pv.Length, pcbWritten2);
			}
		}
	}

	internal unsafe HRESULT Write(ReadOnlySpan<byte> pv)
	{
		fixed (byte* pv2 = pv)
		{
			return Write(pv2, (uint)pv.Length, null);
		}
	}

	public unsafe HRESULT Write(void* pv, uint cb, [Optional] uint* pcbWritten)
	{
		return ((delegate* unmanaged[Stdcall]<IStream_unmanaged*, void*, uint, uint*, HRESULT>)lpVtbl[4])((IStream_unmanaged*)Unsafe.AsPointer(ref this), pv, cb, pcbWritten);
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void Seek(long dlibMove, SeekOrigin dwOrigin, out ulong plibNewPosition)
	{
		fixed (ulong* plibNewPosition2 = &plibNewPosition)
		{
			Seek(dlibMove, dwOrigin, plibNewPosition2);
		}
	}

	internal unsafe void Seek(long dlibMove, SeekOrigin dwOrigin)
	{
		Seek(dlibMove, dwOrigin, null);
	}

	public unsafe void Seek(long dlibMove, SeekOrigin dwOrigin, [Optional] ulong* plibNewPosition)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, long, SeekOrigin, ulong*, HRESULT>)lpVtbl[5])((IStream_unmanaged*)Unsafe.AsPointer(ref this), dlibMove, dwOrigin, plibNewPosition).ThrowOnFailure();
	}

	public unsafe void SetSize(ulong libNewSize)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, HRESULT>)lpVtbl[6])((IStream_unmanaged*)Unsafe.AsPointer(ref this), libNewSize).ThrowOnFailure();
	}

	[OverloadResolutionPriority(1)]
	internal unsafe void CopyTo(IStream_unmanaged* pstm, ulong cb, out ulong pcbRead, out ulong pcbWritten)
	{
		fixed (ulong* pcbWritten2 = &pcbWritten)
		{
			fixed (ulong* pcbRead2 = &pcbRead)
			{
				CopyTo(pstm, cb, pcbRead2, pcbWritten2);
			}
		}
	}

	internal unsafe void CopyTo(IStream_unmanaged* pstm, ulong cb)
	{
		CopyTo(pstm, cb, null, null);
	}

	public unsafe void CopyTo(IStream_unmanaged* pstm, ulong cb, [Optional] ulong* pcbRead, [Optional] ulong* pcbWritten)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, IStream_unmanaged*, ulong, ulong*, ulong*, HRESULT>)lpVtbl[7])((IStream_unmanaged*)Unsafe.AsPointer(ref this), pstm, cb, pcbRead, pcbWritten).ThrowOnFailure();
	}

	public unsafe void Commit(uint grfCommitFlags)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, uint, HRESULT>)lpVtbl[8])((IStream_unmanaged*)Unsafe.AsPointer(ref this), grfCommitFlags).ThrowOnFailure();
	}

	public unsafe void Revert()
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, HRESULT>)lpVtbl[9])((IStream_unmanaged*)Unsafe.AsPointer(ref this)).ThrowOnFailure();
	}

	public unsafe void LockRegion(ulong libOffset, ulong cb, uint dwLockType)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, ulong, uint, HRESULT>)lpVtbl[10])((IStream_unmanaged*)Unsafe.AsPointer(ref this), libOffset, cb, dwLockType).ThrowOnFailure();
	}

	public unsafe void UnlockRegion(ulong libOffset, ulong cb, uint dwLockType)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, ulong, ulong, uint, HRESULT>)lpVtbl[11])((IStream_unmanaged*)Unsafe.AsPointer(ref this), libOffset, cb, dwLockType).ThrowOnFailure();
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
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, STATSTG*, uint, HRESULT>)lpVtbl[12])((IStream_unmanaged*)Unsafe.AsPointer(ref this), pstatstg, grfStatFlag).ThrowOnFailure();
	}

	public unsafe void Clone(out IStream_unmanaged* ppstm)
	{
		((delegate* unmanaged[Stdcall]<IStream_unmanaged*, out IStream_unmanaged*, HRESULT>)lpVtbl[13])((IStream_unmanaged*)Unsafe.AsPointer(ref this), out ppstm).ThrowOnFailure();
	}
}
