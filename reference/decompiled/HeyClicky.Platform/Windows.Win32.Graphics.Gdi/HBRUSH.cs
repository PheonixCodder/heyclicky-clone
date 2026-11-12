using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Graphics.Gdi;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HBRUSH : IEquatable<HBRUSH>
{
	internal unsafe readonly void* Value;

	internal static HBRUSH Null => default(HBRUSH);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HBRUSH(void* value)
	{
		Value = value;
	}

	internal unsafe HBRUSH(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HBRUSH value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HBRUSH(void* value)
	{
		return new HBRUSH(value);
	}

	public unsafe static bool operator ==(HBRUSH left, HBRUSH right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HBRUSH left, HBRUSH right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HBRUSH other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HBRUSH other)
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

	public unsafe static implicit operator nint(HBRUSH value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HBRUSH(nint value)
	{
		return new HBRUSH(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HBRUSH(nuint value)
	{
		return new HBRUSH(((UIntPtr)value).ToPointer());
	}

	public unsafe static implicit operator HGDIOBJ(HBRUSH value)
	{
		return new HGDIOBJ(value.Value);
	}
}
