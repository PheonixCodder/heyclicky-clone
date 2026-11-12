using System;

namespace Windows.Win32;

internal interface IComIID
{
	internal static abstract ref readonly Guid Guid { get; }
}
