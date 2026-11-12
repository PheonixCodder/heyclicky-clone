using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct VARIANT_BOOL : IEquatable<VARIANT_BOOL>
{
	internal readonly short Value;

	internal static readonly VARIANT_BOOL VARIANT_TRUE = (VARIANT_BOOL)(-1);

	internal static readonly VARIANT_BOOL VARIANT_FALSE = (VARIANT_BOOL)0;

	internal VARIANT_BOOL(short value)
	{
		Value = value;
	}

	public static implicit operator short(VARIANT_BOOL value)
	{
		return value.Value;
	}

	public static explicit operator VARIANT_BOOL(short value)
	{
		return new VARIANT_BOOL(value);
	}

	public static bool operator ==(VARIANT_BOOL left, VARIANT_BOOL right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(VARIANT_BOOL left, VARIANT_BOOL right)
	{
		return !(left == right);
	}

	public bool Equals(VARIANT_BOOL other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is VARIANT_BOOL other)
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

	internal VARIANT_BOOL(bool value)
	{
		Value = (value ? VARIANT_TRUE : VARIANT_FALSE);
	}

	public static implicit operator bool(VARIANT_BOOL value)
	{
		return value != VARIANT_FALSE;
	}

	public static implicit operator VARIANT_BOOL(bool value)
	{
		if (!value)
		{
			return VARIANT_FALSE;
		}
		return VARIANT_TRUE;
	}
}
