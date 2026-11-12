using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;

namespace Windows.Win32.System.Variant;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct VARIANT_unmanaged
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Union_unmanaged
	{
		[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
		internal struct _Anonymous_e__Struct_unmanaged
		{
			[StructLayout(LayoutKind.Explicit)]
			[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
			internal struct _Anonymous_e__Union_unmanaged
			{
				[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
				internal struct _Anonymous_e__Struct_unmanaged
				{
					internal unsafe void* pvRecord;

					internal unsafe IRecordInfo_unmanaged* pRecInfo;
				}

				[FieldOffset(0)]
				internal long llVal;

				[FieldOffset(0)]
				internal int lVal;

				[FieldOffset(0)]
				internal byte bVal;

				[FieldOffset(0)]
				internal short iVal;

				[FieldOffset(0)]
				internal float fltVal;

				[FieldOffset(0)]
				internal double dblVal;

				[FieldOffset(0)]
				internal VARIANT_BOOL boolVal;

				[FieldOffset(0)]
				internal VARIANT_BOOL __OBSOLETE__VARIANT_BOOL;

				[FieldOffset(0)]
				internal int scode;

				[FieldOffset(0)]
				internal CY cyVal;

				[FieldOffset(0)]
				internal double date;

				[FieldOffset(0)]
				internal BSTR bstrVal;

				[FieldOffset(0)]
				internal unsafe IUnknown* punkVal;

				[FieldOffset(0)]
				internal unsafe IDispatch_unmanaged* pdispVal;

				[FieldOffset(0)]
				internal unsafe SAFEARRAY* parray;

				[FieldOffset(0)]
				internal unsafe byte* pbVal;

				[FieldOffset(0)]
				internal unsafe short* piVal;

				[FieldOffset(0)]
				internal unsafe int* plVal;

				[FieldOffset(0)]
				internal unsafe long* pllVal;

				[FieldOffset(0)]
				internal unsafe float* pfltVal;

				[FieldOffset(0)]
				internal unsafe double* pdblVal;

				[FieldOffset(0)]
				internal unsafe VARIANT_BOOL* pboolVal;

				[FieldOffset(0)]
				internal unsafe VARIANT_BOOL* __OBSOLETE__VARIANT_PBOOL;

				[FieldOffset(0)]
				internal unsafe int* pscode;

				[FieldOffset(0)]
				internal unsafe CY* pcyVal;

				[FieldOffset(0)]
				internal unsafe double* pdate;

				[FieldOffset(0)]
				internal unsafe BSTR* pbstrVal;

				[FieldOffset(0)]
				internal unsafe IUnknown** ppunkVal;

				[FieldOffset(0)]
				internal unsafe IDispatch_unmanaged** ppdispVal;

				[FieldOffset(0)]
				internal unsafe SAFEARRAY** pparray;

				[FieldOffset(0)]
				internal unsafe VARIANT_unmanaged* pvarVal;

				[FieldOffset(0)]
				internal unsafe void* byref;

				[FieldOffset(0)]
				internal CHAR cVal;

				[FieldOffset(0)]
				internal ushort uiVal;

				[FieldOffset(0)]
				internal uint ulVal;

				[FieldOffset(0)]
				internal ulong ullVal;

				[FieldOffset(0)]
				internal int intVal;

				[FieldOffset(0)]
				internal uint uintVal;

				[FieldOffset(0)]
				internal unsafe DECIMAL* pdecVal;

				[FieldOffset(0)]
				internal PSTR pcVal;

				[FieldOffset(0)]
				internal unsafe ushort* puiVal;

				[FieldOffset(0)]
				internal unsafe uint* pulVal;

				[FieldOffset(0)]
				internal unsafe ulong* pullVal;

				[FieldOffset(0)]
				internal unsafe int* pintVal;

				[FieldOffset(0)]
				internal unsafe uint* puintVal;

				[FieldOffset(0)]
				internal _Anonymous_e__Struct_unmanaged Anonymous;

				[UnscopedRef]
				internal unsafe ref void* pvRecord => ref Anonymous.pvRecord;

				[UnscopedRef]
				internal unsafe ref IRecordInfo_unmanaged* pRecInfo => ref Anonymous.pRecInfo;
			}

			internal VARENUM vt;

			internal ushort wReserved1;

			internal ushort wReserved2;

			internal ushort wReserved3;

			internal _Anonymous_e__Union_unmanaged Anonymous;

			[UnscopedRef]
			internal ref long llVal => ref Anonymous.llVal;

			[UnscopedRef]
			internal ref int lVal => ref Anonymous.lVal;

			[UnscopedRef]
			internal ref byte bVal => ref Anonymous.bVal;

			[UnscopedRef]
			internal ref short iVal => ref Anonymous.iVal;

			[UnscopedRef]
			internal ref float fltVal => ref Anonymous.fltVal;

			[UnscopedRef]
			internal ref double dblVal => ref Anonymous.dblVal;

			[UnscopedRef]
			internal ref VARIANT_BOOL boolVal => ref Anonymous.boolVal;

			[UnscopedRef]
			internal ref VARIANT_BOOL __OBSOLETE__VARIANT_BOOL => ref Anonymous.__OBSOLETE__VARIANT_BOOL;

			[UnscopedRef]
			internal ref int scode => ref Anonymous.scode;

			[UnscopedRef]
			internal ref CY cyVal => ref Anonymous.cyVal;

			[UnscopedRef]
			internal ref double date => ref Anonymous.date;

			[UnscopedRef]
			internal ref BSTR bstrVal => ref Anonymous.bstrVal;

			[UnscopedRef]
			internal unsafe ref IUnknown* punkVal => ref Anonymous.punkVal;

			[UnscopedRef]
			internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.pdispVal;

			[UnscopedRef]
			internal unsafe ref SAFEARRAY* parray => ref Anonymous.parray;

			[UnscopedRef]
			internal unsafe ref byte* pbVal => ref Anonymous.pbVal;

			[UnscopedRef]
			internal unsafe ref short* piVal => ref Anonymous.piVal;

			[UnscopedRef]
			internal unsafe ref int* plVal => ref Anonymous.plVal;

			[UnscopedRef]
			internal unsafe ref long* pllVal => ref Anonymous.pllVal;

			[UnscopedRef]
			internal unsafe ref float* pfltVal => ref Anonymous.pfltVal;

			[UnscopedRef]
			internal unsafe ref double* pdblVal => ref Anonymous.pdblVal;

			[UnscopedRef]
			internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.pboolVal;

			[UnscopedRef]
			internal unsafe ref VARIANT_BOOL* __OBSOLETE__VARIANT_PBOOL => ref Anonymous.__OBSOLETE__VARIANT_PBOOL;

			[UnscopedRef]
			internal unsafe ref int* pscode => ref Anonymous.pscode;

			[UnscopedRef]
			internal unsafe ref CY* pcyVal => ref Anonymous.pcyVal;

			[UnscopedRef]
			internal unsafe ref double* pdate => ref Anonymous.pdate;

			[UnscopedRef]
			internal unsafe ref BSTR* pbstrVal => ref Anonymous.pbstrVal;

			[UnscopedRef]
			internal unsafe ref IUnknown** ppunkVal => ref Anonymous.ppunkVal;

			[UnscopedRef]
			internal unsafe ref IDispatch_unmanaged** ppdispVal => ref Anonymous.ppdispVal;

			[UnscopedRef]
			internal unsafe ref SAFEARRAY** pparray => ref Anonymous.pparray;

			[UnscopedRef]
			internal unsafe ref VARIANT_unmanaged* pvarVal => ref Anonymous.pvarVal;

			[UnscopedRef]
			internal unsafe ref void* byref => ref Anonymous.byref;

			[UnscopedRef]
			internal ref CHAR cVal => ref Anonymous.cVal;

			[UnscopedRef]
			internal ref ushort uiVal => ref Anonymous.uiVal;

			[UnscopedRef]
			internal ref uint ulVal => ref Anonymous.ulVal;

			[UnscopedRef]
			internal ref ulong ullVal => ref Anonymous.ullVal;

			[UnscopedRef]
			internal ref int intVal => ref Anonymous.intVal;

			[UnscopedRef]
			internal ref uint uintVal => ref Anonymous.uintVal;

			[UnscopedRef]
			internal unsafe ref DECIMAL* pdecVal => ref Anonymous.pdecVal;

			[UnscopedRef]
			internal ref PSTR pcVal => ref Anonymous.pcVal;

			[UnscopedRef]
			internal unsafe ref ushort* puiVal => ref Anonymous.puiVal;

			[UnscopedRef]
			internal unsafe ref uint* pulVal => ref Anonymous.pulVal;

			[UnscopedRef]
			internal unsafe ref ulong* pullVal => ref Anonymous.pullVal;

			[UnscopedRef]
			internal unsafe ref int* pintVal => ref Anonymous.pintVal;

			[UnscopedRef]
			internal unsafe ref uint* puintVal => ref Anonymous.puintVal;

			[UnscopedRef]
			internal unsafe ref void* pvRecord => ref Anonymous.Anonymous.pvRecord;

			[UnscopedRef]
			internal unsafe ref IRecordInfo_unmanaged* pRecInfo => ref Anonymous.Anonymous.pRecInfo;
		}

		[FieldOffset(0)]
		internal _Anonymous_e__Struct_unmanaged Anonymous;

		[FieldOffset(0)]
		internal DECIMAL decVal;

		[UnscopedRef]
		internal ref VARENUM vt => ref Anonymous.vt;

		[UnscopedRef]
		internal ref ushort wReserved1 => ref Anonymous.wReserved1;

		[UnscopedRef]
		internal ref ushort wReserved2 => ref Anonymous.wReserved2;

		[UnscopedRef]
		internal ref ushort wReserved3 => ref Anonymous.wReserved3;

		[UnscopedRef]
		internal ref long llVal => ref Anonymous.Anonymous.llVal;

		[UnscopedRef]
		internal ref int lVal => ref Anonymous.Anonymous.lVal;

		[UnscopedRef]
		internal ref byte bVal => ref Anonymous.Anonymous.bVal;

		[UnscopedRef]
		internal ref short iVal => ref Anonymous.Anonymous.iVal;

		[UnscopedRef]
		internal ref float fltVal => ref Anonymous.Anonymous.fltVal;

		[UnscopedRef]
		internal ref double dblVal => ref Anonymous.Anonymous.dblVal;

		[UnscopedRef]
		internal ref VARIANT_BOOL boolVal => ref Anonymous.Anonymous.boolVal;

		[UnscopedRef]
		internal ref VARIANT_BOOL __OBSOLETE__VARIANT_BOOL => ref Anonymous.Anonymous.__OBSOLETE__VARIANT_BOOL;

		[UnscopedRef]
		internal ref int scode => ref Anonymous.Anonymous.scode;

		[UnscopedRef]
		internal ref CY cyVal => ref Anonymous.Anonymous.cyVal;

		[UnscopedRef]
		internal ref double date => ref Anonymous.Anonymous.date;

		[UnscopedRef]
		internal ref BSTR bstrVal => ref Anonymous.Anonymous.bstrVal;

		[UnscopedRef]
		internal unsafe ref IUnknown* punkVal => ref Anonymous.Anonymous.punkVal;

		[UnscopedRef]
		internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.Anonymous.pdispVal;

		[UnscopedRef]
		internal unsafe ref SAFEARRAY* parray => ref Anonymous.Anonymous.parray;

		[UnscopedRef]
		internal unsafe ref byte* pbVal => ref Anonymous.Anonymous.pbVal;

		[UnscopedRef]
		internal unsafe ref short* piVal => ref Anonymous.Anonymous.piVal;

		[UnscopedRef]
		internal unsafe ref int* plVal => ref Anonymous.Anonymous.plVal;

		[UnscopedRef]
		internal unsafe ref long* pllVal => ref Anonymous.Anonymous.pllVal;

		[UnscopedRef]
		internal unsafe ref float* pfltVal => ref Anonymous.Anonymous.pfltVal;

		[UnscopedRef]
		internal unsafe ref double* pdblVal => ref Anonymous.Anonymous.pdblVal;

		[UnscopedRef]
		internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.Anonymous.pboolVal;

		[UnscopedRef]
		internal unsafe ref VARIANT_BOOL* __OBSOLETE__VARIANT_PBOOL => ref Anonymous.Anonymous.__OBSOLETE__VARIANT_PBOOL;

		[UnscopedRef]
		internal unsafe ref int* pscode => ref Anonymous.Anonymous.pscode;

		[UnscopedRef]
		internal unsafe ref CY* pcyVal => ref Anonymous.Anonymous.pcyVal;

		[UnscopedRef]
		internal unsafe ref double* pdate => ref Anonymous.Anonymous.pdate;

		[UnscopedRef]
		internal unsafe ref BSTR* pbstrVal => ref Anonymous.Anonymous.pbstrVal;

		[UnscopedRef]
		internal unsafe ref IUnknown** ppunkVal => ref Anonymous.Anonymous.ppunkVal;

		[UnscopedRef]
		internal unsafe ref IDispatch_unmanaged** ppdispVal => ref Anonymous.Anonymous.ppdispVal;

		[UnscopedRef]
		internal unsafe ref SAFEARRAY** pparray => ref Anonymous.Anonymous.pparray;

		[UnscopedRef]
		internal unsafe ref VARIANT_unmanaged* pvarVal => ref Anonymous.Anonymous.pvarVal;

		[UnscopedRef]
		internal unsafe ref void* byref => ref Anonymous.Anonymous.byref;

		[UnscopedRef]
		internal ref CHAR cVal => ref Anonymous.Anonymous.cVal;

		[UnscopedRef]
		internal ref ushort uiVal => ref Anonymous.Anonymous.uiVal;

		[UnscopedRef]
		internal ref uint ulVal => ref Anonymous.Anonymous.ulVal;

		[UnscopedRef]
		internal ref ulong ullVal => ref Anonymous.Anonymous.ullVal;

		[UnscopedRef]
		internal ref int intVal => ref Anonymous.Anonymous.intVal;

		[UnscopedRef]
		internal ref uint uintVal => ref Anonymous.Anonymous.uintVal;

		[UnscopedRef]
		internal unsafe ref DECIMAL* pdecVal => ref Anonymous.Anonymous.pdecVal;

		[UnscopedRef]
		internal ref PSTR pcVal => ref Anonymous.Anonymous.pcVal;

		[UnscopedRef]
		internal unsafe ref ushort* puiVal => ref Anonymous.Anonymous.puiVal;

		[UnscopedRef]
		internal unsafe ref uint* pulVal => ref Anonymous.Anonymous.pulVal;

		[UnscopedRef]
		internal unsafe ref ulong* pullVal => ref Anonymous.Anonymous.pullVal;

		[UnscopedRef]
		internal unsafe ref int* pintVal => ref Anonymous.Anonymous.pintVal;

		[UnscopedRef]
		internal unsafe ref uint* puintVal => ref Anonymous.Anonymous.puintVal;

		[UnscopedRef]
		internal unsafe ref void* pvRecord => ref Anonymous.Anonymous.Anonymous.pvRecord;

		[UnscopedRef]
		internal unsafe ref IRecordInfo_unmanaged* pRecInfo => ref Anonymous.Anonymous.Anonymous.pRecInfo;
	}

	internal _Anonymous_e__Union_unmanaged Anonymous;

	[UnscopedRef]
	internal ref VARENUM vt => ref Anonymous.Anonymous.vt;

	[UnscopedRef]
	internal ref ushort wReserved1 => ref Anonymous.Anonymous.wReserved1;

	[UnscopedRef]
	internal ref ushort wReserved2 => ref Anonymous.Anonymous.wReserved2;

	[UnscopedRef]
	internal ref ushort wReserved3 => ref Anonymous.Anonymous.wReserved3;

	[UnscopedRef]
	internal ref long llVal => ref Anonymous.Anonymous.Anonymous.llVal;

	[UnscopedRef]
	internal ref int lVal => ref Anonymous.Anonymous.Anonymous.lVal;

	[UnscopedRef]
	internal ref byte bVal => ref Anonymous.Anonymous.Anonymous.bVal;

	[UnscopedRef]
	internal ref short iVal => ref Anonymous.Anonymous.Anonymous.iVal;

	[UnscopedRef]
	internal ref float fltVal => ref Anonymous.Anonymous.Anonymous.fltVal;

	[UnscopedRef]
	internal ref double dblVal => ref Anonymous.Anonymous.Anonymous.dblVal;

	[UnscopedRef]
	internal ref VARIANT_BOOL boolVal => ref Anonymous.Anonymous.Anonymous.boolVal;

	[UnscopedRef]
	internal ref VARIANT_BOOL __OBSOLETE__VARIANT_BOOL => ref Anonymous.Anonymous.Anonymous.__OBSOLETE__VARIANT_BOOL;

	[UnscopedRef]
	internal ref int scode => ref Anonymous.Anonymous.Anonymous.scode;

	[UnscopedRef]
	internal ref CY cyVal => ref Anonymous.Anonymous.Anonymous.cyVal;

	[UnscopedRef]
	internal ref double date => ref Anonymous.Anonymous.Anonymous.date;

	[UnscopedRef]
	internal ref BSTR bstrVal => ref Anonymous.Anonymous.Anonymous.bstrVal;

	[UnscopedRef]
	internal unsafe ref IUnknown* punkVal => ref Anonymous.Anonymous.Anonymous.punkVal;

	[UnscopedRef]
	internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.Anonymous.Anonymous.pdispVal;

	[UnscopedRef]
	internal unsafe ref SAFEARRAY* parray => ref Anonymous.Anonymous.Anonymous.parray;

	[UnscopedRef]
	internal unsafe ref byte* pbVal => ref Anonymous.Anonymous.Anonymous.pbVal;

	[UnscopedRef]
	internal unsafe ref short* piVal => ref Anonymous.Anonymous.Anonymous.piVal;

	[UnscopedRef]
	internal unsafe ref int* plVal => ref Anonymous.Anonymous.Anonymous.plVal;

	[UnscopedRef]
	internal unsafe ref long* pllVal => ref Anonymous.Anonymous.Anonymous.pllVal;

	[UnscopedRef]
	internal unsafe ref float* pfltVal => ref Anonymous.Anonymous.Anonymous.pfltVal;

	[UnscopedRef]
	internal unsafe ref double* pdblVal => ref Anonymous.Anonymous.Anonymous.pdblVal;

	[UnscopedRef]
	internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.Anonymous.Anonymous.pboolVal;

	[UnscopedRef]
	internal unsafe ref VARIANT_BOOL* __OBSOLETE__VARIANT_PBOOL => ref Anonymous.Anonymous.Anonymous.__OBSOLETE__VARIANT_PBOOL;

	[UnscopedRef]
	internal unsafe ref int* pscode => ref Anonymous.Anonymous.Anonymous.pscode;

	[UnscopedRef]
	internal unsafe ref CY* pcyVal => ref Anonymous.Anonymous.Anonymous.pcyVal;

	[UnscopedRef]
	internal unsafe ref double* pdate => ref Anonymous.Anonymous.Anonymous.pdate;

	[UnscopedRef]
	internal unsafe ref BSTR* pbstrVal => ref Anonymous.Anonymous.Anonymous.pbstrVal;

	[UnscopedRef]
	internal unsafe ref IUnknown** ppunkVal => ref Anonymous.Anonymous.Anonymous.ppunkVal;

	[UnscopedRef]
	internal unsafe ref IDispatch_unmanaged** ppdispVal => ref Anonymous.Anonymous.Anonymous.ppdispVal;

	[UnscopedRef]
	internal unsafe ref SAFEARRAY** pparray => ref Anonymous.Anonymous.Anonymous.pparray;

	[UnscopedRef]
	internal unsafe ref VARIANT_unmanaged* pvarVal => ref Anonymous.Anonymous.Anonymous.pvarVal;

	[UnscopedRef]
	internal unsafe ref void* byref => ref Anonymous.Anonymous.Anonymous.byref;

	[UnscopedRef]
	internal ref CHAR cVal => ref Anonymous.Anonymous.Anonymous.cVal;

	[UnscopedRef]
	internal ref ushort uiVal => ref Anonymous.Anonymous.Anonymous.uiVal;

	[UnscopedRef]
	internal ref uint ulVal => ref Anonymous.Anonymous.Anonymous.ulVal;

	[UnscopedRef]
	internal ref ulong ullVal => ref Anonymous.Anonymous.Anonymous.ullVal;

	[UnscopedRef]
	internal ref int intVal => ref Anonymous.Anonymous.Anonymous.intVal;

	[UnscopedRef]
	internal ref uint uintVal => ref Anonymous.Anonymous.Anonymous.uintVal;

	[UnscopedRef]
	internal unsafe ref DECIMAL* pdecVal => ref Anonymous.Anonymous.Anonymous.pdecVal;

	[UnscopedRef]
	internal ref PSTR pcVal => ref Anonymous.Anonymous.Anonymous.pcVal;

	[UnscopedRef]
	internal unsafe ref ushort* puiVal => ref Anonymous.Anonymous.Anonymous.puiVal;

	[UnscopedRef]
	internal unsafe ref uint* pulVal => ref Anonymous.Anonymous.Anonymous.pulVal;

	[UnscopedRef]
	internal unsafe ref ulong* pullVal => ref Anonymous.Anonymous.Anonymous.pullVal;

	[UnscopedRef]
	internal unsafe ref int* pintVal => ref Anonymous.Anonymous.Anonymous.pintVal;

	[UnscopedRef]
	internal unsafe ref uint* puintVal => ref Anonymous.Anonymous.Anonymous.puintVal;

	[UnscopedRef]
	internal unsafe ref void* pvRecord => ref Anonymous.Anonymous.Anonymous.Anonymous.pvRecord;

	[UnscopedRef]
	internal unsafe ref IRecordInfo_unmanaged* pRecInfo => ref Anonymous.Anonymous.Anonymous.Anonymous.pRecInfo;

	[UnscopedRef]
	internal ref DECIMAL decVal => ref Anonymous.decVal;
}
