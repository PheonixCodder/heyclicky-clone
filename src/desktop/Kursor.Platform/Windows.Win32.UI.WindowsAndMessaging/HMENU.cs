using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.WindowsAndMessaging;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HMENU : IEquatable<HMENU>
{
	internal unsafe readonly void* Value;

	internal static HMENU Null => default(HMENU);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HMENU(void* value)
	{
		Value = value;
	}

	internal unsafe HMENU(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HMENU value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HMENU(void* value)
	{
		return new HMENU(value);
	}

	public unsafe static bool operator ==(HMENU left, HMENU right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HMENU left, HMENU right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HMENU other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HMENU other)
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

	public unsafe static implicit operator nint(HMENU value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HMENU(nint value)
	{
		return new HMENU(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HMENU(nuint value)
	{
		return new HMENU(((UIntPtr)value).ToPointer());
	}
}
