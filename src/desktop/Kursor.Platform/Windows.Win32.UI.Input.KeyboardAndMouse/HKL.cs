using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.Input.KeyboardAndMouse;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HKL : IEquatable<HKL>
{
	internal unsafe readonly void* Value;

	internal static HKL Null => default(HKL);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HKL(void* value)
	{
		Value = value;
	}

	internal unsafe HKL(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HKL value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HKL(void* value)
	{
		return new HKL(value);
	}

	public unsafe static bool operator ==(HKL left, HKL right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HKL left, HKL right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HKL other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HKL other)
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

	public unsafe static implicit operator nint(HKL value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HKL(nint value)
	{
		return new HKL(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HKL(nuint value)
	{
		return new HKL(((UIntPtr)value).ToPointer());
	}
}
