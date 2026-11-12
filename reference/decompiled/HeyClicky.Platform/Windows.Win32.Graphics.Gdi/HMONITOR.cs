using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Graphics.Gdi;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HMONITOR : IEquatable<HMONITOR>
{
	internal unsafe readonly void* Value;

	internal static HMONITOR Null => default(HMONITOR);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HMONITOR(void* value)
	{
		Value = value;
	}

	internal unsafe HMONITOR(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HMONITOR value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HMONITOR(void* value)
	{
		return new HMONITOR(value);
	}

	public unsafe static bool operator ==(HMONITOR left, HMONITOR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HMONITOR left, HMONITOR right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HMONITOR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HMONITOR other)
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

	public unsafe static implicit operator nint(HMONITOR value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HMONITOR(nint value)
	{
		return new HMONITOR(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HMONITOR(nuint value)
	{
		return new HMONITOR(((UIntPtr)value).ToPointer());
	}
}
