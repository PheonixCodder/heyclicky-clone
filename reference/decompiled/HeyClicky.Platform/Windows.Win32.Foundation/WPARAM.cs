using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct WPARAM : IEquatable<WPARAM>
{
	internal readonly nuint Value;

	internal WPARAM(nuint value)
	{
		Value = value;
	}

	public static implicit operator nuint(WPARAM value)
	{
		return value.Value;
	}

	public static implicit operator WPARAM(nuint value)
	{
		return new WPARAM(value);
	}

	public static bool operator ==(WPARAM left, WPARAM right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(WPARAM left, WPARAM right)
	{
		return !(left == right);
	}

	public bool Equals(WPARAM other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is WPARAM other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((UIntPtr)Value).GetHashCode();
	}

	public override string ToString()
	{
		return $"0x{Value:x}";
	}
}
