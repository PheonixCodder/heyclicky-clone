using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Graphics.Gdi;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HGDIOBJ : IEquatable<HGDIOBJ>
{
	internal unsafe readonly void* Value;

	internal static HGDIOBJ Null => default(HGDIOBJ);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HGDIOBJ(void* value)
	{
		Value = value;
	}

	internal unsafe HGDIOBJ(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HGDIOBJ value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HGDIOBJ(void* value)
	{
		return new HGDIOBJ(value);
	}

	public unsafe static bool operator ==(HGDIOBJ left, HGDIOBJ right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HGDIOBJ left, HGDIOBJ right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HGDIOBJ other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HGDIOBJ other)
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

	public unsafe static implicit operator nint(HGDIOBJ value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HGDIOBJ(nint value)
	{
		return new HGDIOBJ(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HGDIOBJ(nuint value)
	{
		return new HGDIOBJ(((UIntPtr)value).ToPointer());
	}
}
