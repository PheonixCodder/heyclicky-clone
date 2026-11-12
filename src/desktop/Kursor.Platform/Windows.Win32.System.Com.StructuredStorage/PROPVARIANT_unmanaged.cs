using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Windows.Win32.Foundation;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com.StructuredStorage;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct PROPVARIANT_unmanaged
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
				[FieldOffset(0)]
				internal CHAR cVal;

				[FieldOffset(0)]
				internal byte bVal;

				[FieldOffset(0)]
				internal short iVal;

				[FieldOffset(0)]
				internal ushort uiVal;

				[FieldOffset(0)]
				internal int lVal;

				[FieldOffset(0)]
				internal uint ulVal;

				[FieldOffset(0)]
				internal int intVal;

				[FieldOffset(0)]
				internal uint uintVal;

				[FieldOffset(0)]
				internal long hVal;

				[FieldOffset(0)]
				internal ulong uhVal;

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
				internal FILETIME filetime;

				[FieldOffset(0)]
				internal unsafe Guid* puuid;

				[FieldOffset(0)]
				internal unsafe CLIPDATA* pclipdata;

				[FieldOffset(0)]
				internal BSTR bstrVal;

				[FieldOffset(0)]
				internal BSTRBLOB bstrblobVal;

				[FieldOffset(0)]
				internal BLOB blob;

				[FieldOffset(0)]
				internal PSTR pszVal;

				[FieldOffset(0)]
				internal PWSTR pwszVal;

				[FieldOffset(0)]
				internal unsafe IUnknown* punkVal;

				[FieldOffset(0)]
				internal unsafe IDispatch_unmanaged* pdispVal;

				[FieldOffset(0)]
				internal unsafe IStream_unmanaged* pStream;

				[FieldOffset(0)]
				internal unsafe IStorage_unmanaged* pStorage;

				[FieldOffset(0)]
				internal unsafe VERSIONEDSTREAM_unmanaged* pVersionedStream;

				[FieldOffset(0)]
				internal unsafe SAFEARRAY* parray;

				[FieldOffset(0)]
				internal CAC cac;

				[FieldOffset(0)]
				internal CAUB caub;

				[FieldOffset(0)]
				internal CAI cai;

				[FieldOffset(0)]
				internal CAUI caui;

				[FieldOffset(0)]
				internal CAL cal;

				[FieldOffset(0)]
				internal CAUL caul;

				[FieldOffset(0)]
				internal CAH cah;

				[FieldOffset(0)]
				internal CAUH cauh;

				[FieldOffset(0)]
				internal CAFLT caflt;

				[FieldOffset(0)]
				internal CADBL cadbl;

				[FieldOffset(0)]
				internal CABOOL cabool;

				[FieldOffset(0)]
				internal CASCODE cascode;

				[FieldOffset(0)]
				internal CACY cacy;

				[FieldOffset(0)]
				internal CADATE cadate;

				[FieldOffset(0)]
				internal CAFILETIME cafiletime;

				[FieldOffset(0)]
				internal CACLSID cauuid;

				[FieldOffset(0)]
				internal CACLIPDATA caclipdata;

				[FieldOffset(0)]
				internal CABSTR cabstr;

				[FieldOffset(0)]
				internal CABSTRBLOB cabstrblob;

				[FieldOffset(0)]
				internal CALPSTR calpstr;

				[FieldOffset(0)]
				internal CALPWSTR calpwstr;

				[FieldOffset(0)]
				internal CAPROPVARIANT_unmanaged capropvar;

				[FieldOffset(0)]
				internal PSTR pcVal;

				[FieldOffset(0)]
				internal unsafe byte* pbVal;

				[FieldOffset(0)]
				internal unsafe short* piVal;

				[FieldOffset(0)]
				internal unsafe ushort* puiVal;

				[FieldOffset(0)]
				internal unsafe int* plVal;

				[FieldOffset(0)]
				internal unsafe uint* pulVal;

				[FieldOffset(0)]
				internal unsafe int* pintVal;

				[FieldOffset(0)]
				internal unsafe uint* puintVal;

				[FieldOffset(0)]
				internal unsafe float* pfltVal;

				[FieldOffset(0)]
				internal unsafe double* pdblVal;

				[FieldOffset(0)]
				internal unsafe VARIANT_BOOL* pboolVal;

				[FieldOffset(0)]
				internal unsafe DECIMAL* pdecVal;

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
				internal unsafe PROPVARIANT_unmanaged* pvarVal;
			}

			internal VARENUM vt;

			internal ushort wReserved1;

			internal ushort wReserved2;

			internal ushort wReserved3;

			internal _Anonymous_e__Union_unmanaged Anonymous;

			[UnscopedRef]
			internal ref CHAR cVal => ref Anonymous.cVal;

			[UnscopedRef]
			internal ref byte bVal => ref Anonymous.bVal;

			[UnscopedRef]
			internal ref short iVal => ref Anonymous.iVal;

			[UnscopedRef]
			internal ref ushort uiVal => ref Anonymous.uiVal;

			[UnscopedRef]
			internal ref int lVal => ref Anonymous.lVal;

			[UnscopedRef]
			internal ref uint ulVal => ref Anonymous.ulVal;

			[UnscopedRef]
			internal ref int intVal => ref Anonymous.intVal;

			[UnscopedRef]
			internal ref uint uintVal => ref Anonymous.uintVal;

			[UnscopedRef]
			internal ref long hVal => ref Anonymous.hVal;

			[UnscopedRef]
			internal ref ulong uhVal => ref Anonymous.uhVal;

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
			internal ref FILETIME filetime => ref Anonymous.filetime;

			[UnscopedRef]
			internal unsafe ref Guid* puuid => ref Anonymous.puuid;

			[UnscopedRef]
			internal unsafe ref CLIPDATA* pclipdata => ref Anonymous.pclipdata;

			[UnscopedRef]
			internal ref BSTR bstrVal => ref Anonymous.bstrVal;

			[UnscopedRef]
			internal ref BSTRBLOB bstrblobVal => ref Anonymous.bstrblobVal;

			[UnscopedRef]
			internal ref BLOB blob => ref Anonymous.blob;

			[UnscopedRef]
			internal ref PSTR pszVal => ref Anonymous.pszVal;

			[UnscopedRef]
			internal ref PWSTR pwszVal => ref Anonymous.pwszVal;

			[UnscopedRef]
			internal unsafe ref IUnknown* punkVal => ref Anonymous.punkVal;

			[UnscopedRef]
			internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.pdispVal;

			[UnscopedRef]
			internal unsafe ref IStream_unmanaged* pStream => ref Anonymous.pStream;

			[UnscopedRef]
			internal unsafe ref IStorage_unmanaged* pStorage => ref Anonymous.pStorage;

			[UnscopedRef]
			internal unsafe ref VERSIONEDSTREAM_unmanaged* pVersionedStream => ref Anonymous.pVersionedStream;

			[UnscopedRef]
			internal unsafe ref SAFEARRAY* parray => ref Anonymous.parray;

			[UnscopedRef]
			internal ref CAC cac => ref Anonymous.cac;

			[UnscopedRef]
			internal ref CAUB caub => ref Anonymous.caub;

			[UnscopedRef]
			internal ref CAI cai => ref Anonymous.cai;

			[UnscopedRef]
			internal ref CAUI caui => ref Anonymous.caui;

			[UnscopedRef]
			internal ref CAL cal => ref Anonymous.cal;

			[UnscopedRef]
			internal ref CAUL caul => ref Anonymous.caul;

			[UnscopedRef]
			internal ref CAH cah => ref Anonymous.cah;

			[UnscopedRef]
			internal ref CAUH cauh => ref Anonymous.cauh;

			[UnscopedRef]
			internal ref CAFLT caflt => ref Anonymous.caflt;

			[UnscopedRef]
			internal ref CADBL cadbl => ref Anonymous.cadbl;

			[UnscopedRef]
			internal ref CABOOL cabool => ref Anonymous.cabool;

			[UnscopedRef]
			internal ref CASCODE cascode => ref Anonymous.cascode;

			[UnscopedRef]
			internal ref CACY cacy => ref Anonymous.cacy;

			[UnscopedRef]
			internal ref CADATE cadate => ref Anonymous.cadate;

			[UnscopedRef]
			internal ref CAFILETIME cafiletime => ref Anonymous.cafiletime;

			[UnscopedRef]
			internal ref CACLSID cauuid => ref Anonymous.cauuid;

			[UnscopedRef]
			internal ref CACLIPDATA caclipdata => ref Anonymous.caclipdata;

			[UnscopedRef]
			internal ref CABSTR cabstr => ref Anonymous.cabstr;

			[UnscopedRef]
			internal ref CABSTRBLOB cabstrblob => ref Anonymous.cabstrblob;

			[UnscopedRef]
			internal ref CALPSTR calpstr => ref Anonymous.calpstr;

			[UnscopedRef]
			internal ref CALPWSTR calpwstr => ref Anonymous.calpwstr;

			[UnscopedRef]
			internal ref CAPROPVARIANT_unmanaged capropvar => ref Anonymous.capropvar;

			[UnscopedRef]
			internal ref PSTR pcVal => ref Anonymous.pcVal;

			[UnscopedRef]
			internal unsafe ref byte* pbVal => ref Anonymous.pbVal;

			[UnscopedRef]
			internal unsafe ref short* piVal => ref Anonymous.piVal;

			[UnscopedRef]
			internal unsafe ref ushort* puiVal => ref Anonymous.puiVal;

			[UnscopedRef]
			internal unsafe ref int* plVal => ref Anonymous.plVal;

			[UnscopedRef]
			internal unsafe ref uint* pulVal => ref Anonymous.pulVal;

			[UnscopedRef]
			internal unsafe ref int* pintVal => ref Anonymous.pintVal;

			[UnscopedRef]
			internal unsafe ref uint* puintVal => ref Anonymous.puintVal;

			[UnscopedRef]
			internal unsafe ref float* pfltVal => ref Anonymous.pfltVal;

			[UnscopedRef]
			internal unsafe ref double* pdblVal => ref Anonymous.pdblVal;

			[UnscopedRef]
			internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.pboolVal;

			[UnscopedRef]
			internal unsafe ref DECIMAL* pdecVal => ref Anonymous.pdecVal;

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
			internal unsafe ref PROPVARIANT_unmanaged* pvarVal => ref Anonymous.pvarVal;
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
		internal ref CHAR cVal => ref Anonymous.Anonymous.cVal;

		[UnscopedRef]
		internal ref byte bVal => ref Anonymous.Anonymous.bVal;

		[UnscopedRef]
		internal ref short iVal => ref Anonymous.Anonymous.iVal;

		[UnscopedRef]
		internal ref ushort uiVal => ref Anonymous.Anonymous.uiVal;

		[UnscopedRef]
		internal ref int lVal => ref Anonymous.Anonymous.lVal;

		[UnscopedRef]
		internal ref uint ulVal => ref Anonymous.Anonymous.ulVal;

		[UnscopedRef]
		internal ref int intVal => ref Anonymous.Anonymous.intVal;

		[UnscopedRef]
		internal ref uint uintVal => ref Anonymous.Anonymous.uintVal;

		[UnscopedRef]
		internal ref long hVal => ref Anonymous.Anonymous.hVal;

		[UnscopedRef]
		internal ref ulong uhVal => ref Anonymous.Anonymous.uhVal;

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
		internal ref FILETIME filetime => ref Anonymous.Anonymous.filetime;

		[UnscopedRef]
		internal unsafe ref Guid* puuid => ref Anonymous.Anonymous.puuid;

		[UnscopedRef]
		internal unsafe ref CLIPDATA* pclipdata => ref Anonymous.Anonymous.pclipdata;

		[UnscopedRef]
		internal ref BSTR bstrVal => ref Anonymous.Anonymous.bstrVal;

		[UnscopedRef]
		internal ref BSTRBLOB bstrblobVal => ref Anonymous.Anonymous.bstrblobVal;

		[UnscopedRef]
		internal ref BLOB blob => ref Anonymous.Anonymous.blob;

		[UnscopedRef]
		internal ref PSTR pszVal => ref Anonymous.Anonymous.pszVal;

		[UnscopedRef]
		internal ref PWSTR pwszVal => ref Anonymous.Anonymous.pwszVal;

		[UnscopedRef]
		internal unsafe ref IUnknown* punkVal => ref Anonymous.Anonymous.punkVal;

		[UnscopedRef]
		internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.Anonymous.pdispVal;

		[UnscopedRef]
		internal unsafe ref IStream_unmanaged* pStream => ref Anonymous.Anonymous.pStream;

		[UnscopedRef]
		internal unsafe ref IStorage_unmanaged* pStorage => ref Anonymous.Anonymous.pStorage;

		[UnscopedRef]
		internal unsafe ref VERSIONEDSTREAM_unmanaged* pVersionedStream => ref Anonymous.Anonymous.pVersionedStream;

		[UnscopedRef]
		internal unsafe ref SAFEARRAY* parray => ref Anonymous.Anonymous.parray;

		[UnscopedRef]
		internal ref CAC cac => ref Anonymous.Anonymous.cac;

		[UnscopedRef]
		internal ref CAUB caub => ref Anonymous.Anonymous.caub;

		[UnscopedRef]
		internal ref CAI cai => ref Anonymous.Anonymous.cai;

		[UnscopedRef]
		internal ref CAUI caui => ref Anonymous.Anonymous.caui;

		[UnscopedRef]
		internal ref CAL cal => ref Anonymous.Anonymous.cal;

		[UnscopedRef]
		internal ref CAUL caul => ref Anonymous.Anonymous.caul;

		[UnscopedRef]
		internal ref CAH cah => ref Anonymous.Anonymous.cah;

		[UnscopedRef]
		internal ref CAUH cauh => ref Anonymous.Anonymous.cauh;

		[UnscopedRef]
		internal ref CAFLT caflt => ref Anonymous.Anonymous.caflt;

		[UnscopedRef]
		internal ref CADBL cadbl => ref Anonymous.Anonymous.cadbl;

		[UnscopedRef]
		internal ref CABOOL cabool => ref Anonymous.Anonymous.cabool;

		[UnscopedRef]
		internal ref CASCODE cascode => ref Anonymous.Anonymous.cascode;

		[UnscopedRef]
		internal ref CACY cacy => ref Anonymous.Anonymous.cacy;

		[UnscopedRef]
		internal ref CADATE cadate => ref Anonymous.Anonymous.cadate;

		[UnscopedRef]
		internal ref CAFILETIME cafiletime => ref Anonymous.Anonymous.cafiletime;

		[UnscopedRef]
		internal ref CACLSID cauuid => ref Anonymous.Anonymous.cauuid;

		[UnscopedRef]
		internal ref CACLIPDATA caclipdata => ref Anonymous.Anonymous.caclipdata;

		[UnscopedRef]
		internal ref CABSTR cabstr => ref Anonymous.Anonymous.cabstr;

		[UnscopedRef]
		internal ref CABSTRBLOB cabstrblob => ref Anonymous.Anonymous.cabstrblob;

		[UnscopedRef]
		internal ref CALPSTR calpstr => ref Anonymous.Anonymous.calpstr;

		[UnscopedRef]
		internal ref CALPWSTR calpwstr => ref Anonymous.Anonymous.calpwstr;

		[UnscopedRef]
		internal ref CAPROPVARIANT_unmanaged capropvar => ref Anonymous.Anonymous.capropvar;

		[UnscopedRef]
		internal ref PSTR pcVal => ref Anonymous.Anonymous.pcVal;

		[UnscopedRef]
		internal unsafe ref byte* pbVal => ref Anonymous.Anonymous.pbVal;

		[UnscopedRef]
		internal unsafe ref short* piVal => ref Anonymous.Anonymous.piVal;

		[UnscopedRef]
		internal unsafe ref ushort* puiVal => ref Anonymous.Anonymous.puiVal;

		[UnscopedRef]
		internal unsafe ref int* plVal => ref Anonymous.Anonymous.plVal;

		[UnscopedRef]
		internal unsafe ref uint* pulVal => ref Anonymous.Anonymous.pulVal;

		[UnscopedRef]
		internal unsafe ref int* pintVal => ref Anonymous.Anonymous.pintVal;

		[UnscopedRef]
		internal unsafe ref uint* puintVal => ref Anonymous.Anonymous.puintVal;

		[UnscopedRef]
		internal unsafe ref float* pfltVal => ref Anonymous.Anonymous.pfltVal;

		[UnscopedRef]
		internal unsafe ref double* pdblVal => ref Anonymous.Anonymous.pdblVal;

		[UnscopedRef]
		internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.Anonymous.pboolVal;

		[UnscopedRef]
		internal unsafe ref DECIMAL* pdecVal => ref Anonymous.Anonymous.pdecVal;

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
		internal unsafe ref PROPVARIANT_unmanaged* pvarVal => ref Anonymous.Anonymous.pvarVal;
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
	internal ref CHAR cVal => ref Anonymous.Anonymous.Anonymous.cVal;

	[UnscopedRef]
	internal ref byte bVal => ref Anonymous.Anonymous.Anonymous.bVal;

	[UnscopedRef]
	internal ref short iVal => ref Anonymous.Anonymous.Anonymous.iVal;

	[UnscopedRef]
	internal ref ushort uiVal => ref Anonymous.Anonymous.Anonymous.uiVal;

	[UnscopedRef]
	internal ref int lVal => ref Anonymous.Anonymous.Anonymous.lVal;

	[UnscopedRef]
	internal ref uint ulVal => ref Anonymous.Anonymous.Anonymous.ulVal;

	[UnscopedRef]
	internal ref int intVal => ref Anonymous.Anonymous.Anonymous.intVal;

	[UnscopedRef]
	internal ref uint uintVal => ref Anonymous.Anonymous.Anonymous.uintVal;

	[UnscopedRef]
	internal ref long hVal => ref Anonymous.Anonymous.Anonymous.hVal;

	[UnscopedRef]
	internal ref ulong uhVal => ref Anonymous.Anonymous.Anonymous.uhVal;

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
	internal ref FILETIME filetime => ref Anonymous.Anonymous.Anonymous.filetime;

	[UnscopedRef]
	internal unsafe ref Guid* puuid => ref Anonymous.Anonymous.Anonymous.puuid;

	[UnscopedRef]
	internal unsafe ref CLIPDATA* pclipdata => ref Anonymous.Anonymous.Anonymous.pclipdata;

	[UnscopedRef]
	internal ref BSTR bstrVal => ref Anonymous.Anonymous.Anonymous.bstrVal;

	[UnscopedRef]
	internal ref BSTRBLOB bstrblobVal => ref Anonymous.Anonymous.Anonymous.bstrblobVal;

	[UnscopedRef]
	internal ref BLOB blob => ref Anonymous.Anonymous.Anonymous.blob;

	[UnscopedRef]
	internal ref PSTR pszVal => ref Anonymous.Anonymous.Anonymous.pszVal;

	[UnscopedRef]
	internal ref PWSTR pwszVal => ref Anonymous.Anonymous.Anonymous.pwszVal;

	[UnscopedRef]
	internal unsafe ref IUnknown* punkVal => ref Anonymous.Anonymous.Anonymous.punkVal;

	[UnscopedRef]
	internal unsafe ref IDispatch_unmanaged* pdispVal => ref Anonymous.Anonymous.Anonymous.pdispVal;

	[UnscopedRef]
	internal unsafe ref IStream_unmanaged* pStream => ref Anonymous.Anonymous.Anonymous.pStream;

	[UnscopedRef]
	internal unsafe ref IStorage_unmanaged* pStorage => ref Anonymous.Anonymous.Anonymous.pStorage;

	[UnscopedRef]
	internal unsafe ref VERSIONEDSTREAM_unmanaged* pVersionedStream => ref Anonymous.Anonymous.Anonymous.pVersionedStream;

	[UnscopedRef]
	internal unsafe ref SAFEARRAY* parray => ref Anonymous.Anonymous.Anonymous.parray;

	[UnscopedRef]
	internal ref CAC cac => ref Anonymous.Anonymous.Anonymous.cac;

	[UnscopedRef]
	internal ref CAUB caub => ref Anonymous.Anonymous.Anonymous.caub;

	[UnscopedRef]
	internal ref CAI cai => ref Anonymous.Anonymous.Anonymous.cai;

	[UnscopedRef]
	internal ref CAUI caui => ref Anonymous.Anonymous.Anonymous.caui;

	[UnscopedRef]
	internal ref CAL cal => ref Anonymous.Anonymous.Anonymous.cal;

	[UnscopedRef]
	internal ref CAUL caul => ref Anonymous.Anonymous.Anonymous.caul;

	[UnscopedRef]
	internal ref CAH cah => ref Anonymous.Anonymous.Anonymous.cah;

	[UnscopedRef]
	internal ref CAUH cauh => ref Anonymous.Anonymous.Anonymous.cauh;

	[UnscopedRef]
	internal ref CAFLT caflt => ref Anonymous.Anonymous.Anonymous.caflt;

	[UnscopedRef]
	internal ref CADBL cadbl => ref Anonymous.Anonymous.Anonymous.cadbl;

	[UnscopedRef]
	internal ref CABOOL cabool => ref Anonymous.Anonymous.Anonymous.cabool;

	[UnscopedRef]
	internal ref CASCODE cascode => ref Anonymous.Anonymous.Anonymous.cascode;

	[UnscopedRef]
	internal ref CACY cacy => ref Anonymous.Anonymous.Anonymous.cacy;

	[UnscopedRef]
	internal ref CADATE cadate => ref Anonymous.Anonymous.Anonymous.cadate;

	[UnscopedRef]
	internal ref CAFILETIME cafiletime => ref Anonymous.Anonymous.Anonymous.cafiletime;

	[UnscopedRef]
	internal ref CACLSID cauuid => ref Anonymous.Anonymous.Anonymous.cauuid;

	[UnscopedRef]
	internal ref CACLIPDATA caclipdata => ref Anonymous.Anonymous.Anonymous.caclipdata;

	[UnscopedRef]
	internal ref CABSTR cabstr => ref Anonymous.Anonymous.Anonymous.cabstr;

	[UnscopedRef]
	internal ref CABSTRBLOB cabstrblob => ref Anonymous.Anonymous.Anonymous.cabstrblob;

	[UnscopedRef]
	internal ref CALPSTR calpstr => ref Anonymous.Anonymous.Anonymous.calpstr;

	[UnscopedRef]
	internal ref CALPWSTR calpwstr => ref Anonymous.Anonymous.Anonymous.calpwstr;

	[UnscopedRef]
	internal ref CAPROPVARIANT_unmanaged capropvar => ref Anonymous.Anonymous.Anonymous.capropvar;

	[UnscopedRef]
	internal ref PSTR pcVal => ref Anonymous.Anonymous.Anonymous.pcVal;

	[UnscopedRef]
	internal unsafe ref byte* pbVal => ref Anonymous.Anonymous.Anonymous.pbVal;

	[UnscopedRef]
	internal unsafe ref short* piVal => ref Anonymous.Anonymous.Anonymous.piVal;

	[UnscopedRef]
	internal unsafe ref ushort* puiVal => ref Anonymous.Anonymous.Anonymous.puiVal;

	[UnscopedRef]
	internal unsafe ref int* plVal => ref Anonymous.Anonymous.Anonymous.plVal;

	[UnscopedRef]
	internal unsafe ref uint* pulVal => ref Anonymous.Anonymous.Anonymous.pulVal;

	[UnscopedRef]
	internal unsafe ref int* pintVal => ref Anonymous.Anonymous.Anonymous.pintVal;

	[UnscopedRef]
	internal unsafe ref uint* puintVal => ref Anonymous.Anonymous.Anonymous.puintVal;

	[UnscopedRef]
	internal unsafe ref float* pfltVal => ref Anonymous.Anonymous.Anonymous.pfltVal;

	[UnscopedRef]
	internal unsafe ref double* pdblVal => ref Anonymous.Anonymous.Anonymous.pdblVal;

	[UnscopedRef]
	internal unsafe ref VARIANT_BOOL* pboolVal => ref Anonymous.Anonymous.Anonymous.pboolVal;

	[UnscopedRef]
	internal unsafe ref DECIMAL* pdecVal => ref Anonymous.Anonymous.Anonymous.pdecVal;

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
	internal unsafe ref PROPVARIANT_unmanaged* pvarVal => ref Anonymous.Anonymous.Anonymous.pvarVal;

	[UnscopedRef]
	internal ref DECIMAL decVal => ref Anonymous.decVal;
}
