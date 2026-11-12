using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct BSTR : IEquatable<BSTR>
{
	internal unsafe readonly char* Value;

	internal unsafe int Length
	{
		get
		{
			checked
			{
				if (Value != null)
				{
					return (int)unchecked(*(uint*)checked(unchecked((nuint)Value) - (nuint)4u) / 2);
				}
				return 0;
			}
		}
	}

	internal unsafe BSTR(char* value)
	{
		Value = value;
	}

	internal unsafe BSTR(nint value)
		: this((char*)value)
	{
	}

	public unsafe static implicit operator char*(BSTR value)
	{
		return value.Value;
	}

	public unsafe static explicit operator BSTR(char* value)
	{
		return new BSTR(value);
	}

	public unsafe static bool operator ==(BSTR left, BSTR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(BSTR left, BSTR right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(BSTR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is BSTR other)
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
		if (Value == null)
		{
			return null;
		}
		return Marshal.PtrToStringBSTR(new IntPtr(Value));
	}

	public unsafe static implicit operator nint(BSTR value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator BSTR(nint value)
	{
		return new BSTR((char*)((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator BSTR(nuint value)
	{
		return new BSTR((char*)((UIntPtr)value).ToPointer());
	}

	public unsafe static implicit operator ReadOnlySpan<char>(BSTR bstr)
	{
		if (bstr.Value == null)
		{
			return default(ReadOnlySpan<char>);
		}
		return new ReadOnlySpan<char>(bstr.Value, *(int*)(bstr.Value - 2) / 2);
	}

	internal ReadOnlySpan<char> AsSpan()
	{
		return this;
	}
}
