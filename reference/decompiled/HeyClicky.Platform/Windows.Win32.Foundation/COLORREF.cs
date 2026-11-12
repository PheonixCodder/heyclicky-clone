using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct COLORREF : IEquatable<COLORREF>
{
	internal readonly uint Value;

	internal COLORREF(uint value)
	{
		Value = value;
	}

	public static implicit operator uint(COLORREF value)
	{
		return value.Value;
	}

	public static explicit operator COLORREF(uint value)
	{
		return new COLORREF(value);
	}

	public static bool operator ==(COLORREF left, COLORREF right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(COLORREF left, COLORREF right)
	{
		return !(left == right);
	}

	public bool Equals(COLORREF other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is COLORREF other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return $"0x{Value:x}";
	}
}
