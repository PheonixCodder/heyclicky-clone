using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices.ComTypes;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct STATSTG
{
	internal PWSTR pwcsName;

	internal uint type;

	internal ulong cbSize;

	internal FILETIME mtime;

	internal FILETIME ctime;

	internal FILETIME atime;

	internal STGM grfMode;

	private uint _grfLocksSupported;

	internal Guid clsid;

	internal uint grfStateBits;

	internal uint reserved;

	internal LOCKTYPE grfLocksSupported
	{
		get
		{
			return (LOCKTYPE)_grfLocksSupported;
		}
		set
		{
			_grfLocksSupported = (uint)value;
		}
	}
}
