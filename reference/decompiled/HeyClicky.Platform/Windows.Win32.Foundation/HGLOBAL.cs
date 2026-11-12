using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HGLOBAL : IEquatable<HGLOBAL>
{
	internal unsafe readonly void* Value;

	internal static HGLOBAL Null => default(HGLOBAL);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HGLOBAL(void* value)
	{
		Value = value;
	}

	internal unsafe HGLOBAL(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HGLOBAL value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HGLOBAL(void* value)
	{
		return new HGLOBAL(value);
	}

	public unsafe static bool operator ==(HGLOBAL left, HGLOBAL right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HGLOBAL left, HGLOBAL right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HGLOBAL other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HGLOBAL other)
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

	public unsafe static implicit operator nint(HGLOBAL value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HGLOBAL(nint value)
	{
		return new HGLOBAL(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HGLOBAL(nuint value)
	{
		return new HGLOBAL(((UIntPtr)value).ToPointer());
	}
}
