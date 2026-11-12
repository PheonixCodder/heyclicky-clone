using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.System.Com;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum ADVANCED_FEATURE_FLAGS : ushort
{
	FADF_AUTO = 1,
	FADF_STATIC = 2,
	FADF_EMBEDDED = 4,
	FADF_FIXEDSIZE = 0x10,
	FADF_RECORD = 0x20,
	FADF_HAVEIID = 0x40,
	FADF_HAVEVARTYPE = 0x80,
	FADF_BSTR = 0x100,
	FADF_UNKNOWN = 0x200,
	FADF_DISPATCH = 0x400,
	FADF_VARIANT = 0x800,
	FADF_RESERVED = 0xF008
}
