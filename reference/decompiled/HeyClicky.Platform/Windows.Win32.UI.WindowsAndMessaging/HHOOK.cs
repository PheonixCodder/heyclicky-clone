using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.WindowsAndMessaging;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HHOOK : IEquatable<HHOOK>
{
	internal unsafe readonly void* Value;

	internal static HHOOK Null => default(HHOOK);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HHOOK(void* value)
	{
		Value = value;
	}

	internal unsafe HHOOK(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HHOOK value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HHOOK(void* value)
	{
		return new HHOOK(value);
	}

	public unsafe static bool operator ==(HHOOK left, HHOOK right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HHOOK left, HHOOK right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HHOOK other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HHOOK other)
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
		return $"0x{Value:x}";
	}

	public unsafe static implicit operator nint(HHOOK value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HHOOK(nint value)
	{
		return new HHOOK(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HHOOK(nuint value)
	{
		return new HHOOK(((UIntPtr)value).ToPointer());
	}
}
