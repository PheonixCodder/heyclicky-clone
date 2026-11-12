using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HRESULT : IEquatable<HRESULT>
{
	internal readonly int Value;

	internal static readonly HRESULT AUDCLNT_E_DEVICE_INVALIDATED = (HRESULT)(-2004287484);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal bool Succeeded => Value >= 0;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal bool Failed => Value < 0;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"{ToString()} {Marshal.GetPInvokeErrorMessage(Value)}";

	internal HRESULT(int value)
	{
		Value = value;
	}

	public static implicit operator int(HRESULT value)
	{
		return value.Value;
	}

	public static explicit operator HRESULT(int value)
	{
		return new HRESULT(value);
	}

	public static bool operator ==(HRESULT left, HRESULT right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HRESULT left, HRESULT right)
	{
		return !(left == right);
	}

	public bool Equals(HRESULT other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HRESULT other)
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
		return string.Format(CultureInfo.InvariantCulture, "0x{0:X8}", Value);
	}

	public static implicit operator uint(HRESULT value)
	{
		return (uint)value.Value;
	}

	public static explicit operator HRESULT(uint value)
	{
		return new HRESULT((int)value);
	}

	internal HRESULT ThrowOnFailure(nint errorInfo = 0)
	{
		Marshal.ThrowExceptionForHR(Value, errorInfo);
		return this;
	}

	internal string ToString(string format, IFormatProvider formatProvider)
	{
		uint value = (uint)Value;
		return value.ToString(format, formatProvider);
	}
}
