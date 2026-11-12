using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct CHAR : IEquatable<CHAR>
{
	internal readonly sbyte Value;

	internal CHAR(sbyte value)
	{
		Value = value;
	}

	public static implicit operator sbyte(CHAR value)
	{
		return value.Value;
	}

	public static explicit operator CHAR(sbyte value)
	{
		return new CHAR(value);
	}

	public static bool operator ==(CHAR left, CHAR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(CHAR left, CHAR right)
	{
		return !(left == right);
	}

	public bool Equals(CHAR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is CHAR other)
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
