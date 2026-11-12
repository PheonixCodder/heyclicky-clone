using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.WindowsAndMessaging;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HCURSOR : IEquatable<HCURSOR>
{
	internal unsafe readonly void* Value;

	internal static HCURSOR Null => default(HCURSOR);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HCURSOR(void* value)
	{
		Value = value;
	}

	internal unsafe HCURSOR(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HCURSOR value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HCURSOR(void* value)
	{
		return new HCURSOR(value);
	}

	public unsafe static bool operator ==(HCURSOR left, HCURSOR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HCURSOR left, HCURSOR right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HCURSOR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HCURSOR other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		return (int)Value;
	}

	public unsafe override string ToString()
	{
		return $"0x{(nint)Value:x}";
	}

	public unsafe static implicit operator nint(HCURSOR value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HCURSOR(nint value)
	{
		return new HCURSOR(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HCURSOR(nuint value)
	{
		return new HCURSOR(((UIntPtr)value).ToPointer());
	}

	public unsafe static implicit operator HICON(HCURSOR value)
	{
		return new HICON(value.Value);
	}
}
