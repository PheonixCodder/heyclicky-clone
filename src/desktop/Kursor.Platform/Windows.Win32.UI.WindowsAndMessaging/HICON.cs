using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.WindowsAndMessaging;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HICON : IEquatable<HICON>
{
	internal unsafe readonly void* Value;

	internal static HICON Null => default(HICON);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HICON(void* value)
	{
		Value = value;
	}

	internal unsafe HICON(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HICON value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HICON(void* value)
	{
		return new HICON(value);
	}

	public unsafe static bool operator ==(HICON left, HICON right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HICON left, HICON right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HICON other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HICON other)
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

	public unsafe static implicit operator nint(HICON value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HICON(nint value)
	{
		return new HICON(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HICON(nuint value)
	{
		return new HICON(((UIntPtr)value).ToPointer());
	}
}
