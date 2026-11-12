using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct PSTR : IEquatable<PSTR>
{
	internal unsafe readonly byte* Value;

	internal unsafe int Length => new PCSTR(Value).Length;

	private string DebuggerDisplay => ToString();

	internal unsafe PSTR(byte* value)
	{
		Value = value;
	}

	internal unsafe PSTR(nint value)
		: this((byte*)value)
	{
	}

	public unsafe static implicit operator byte*(PSTR value)
	{
		return value.Value;
	}

	public unsafe static implicit operator PSTR(byte* value)
	{
		return new PSTR(value);
	}

	public unsafe static bool operator ==(PSTR left, PSTR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(PSTR left, PSTR right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(PSTR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is PSTR other)
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
		return new PCSTR(Value).ToString();
	}

	public unsafe static implicit operator PCSTR(PSTR value)
	{
		return new PCSTR(value.Value);
	}

	internal unsafe Span<byte> AsSpan()
	{
		if (Value != null)
		{
			return new Span<byte>(Value, Length);
		}
		return default(Span<byte>);
	}
}
