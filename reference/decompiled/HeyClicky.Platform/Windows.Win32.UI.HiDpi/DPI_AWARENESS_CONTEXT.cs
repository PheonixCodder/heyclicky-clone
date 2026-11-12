using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.HiDpi;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct DPI_AWARENESS_CONTEXT : IEquatable<DPI_AWARENESS_CONTEXT>
{
	internal unsafe readonly void* Value;

	internal static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (DPI_AWARENESS_CONTEXT)(-4);

	internal static DPI_AWARENESS_CONTEXT Null => default(DPI_AWARENESS_CONTEXT);

	internal unsafe bool IsNull => Value == null;

	internal unsafe DPI_AWARENESS_CONTEXT(void* value)
	{
		Value = value;
	}

	internal unsafe DPI_AWARENESS_CONTEXT(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(DPI_AWARENESS_CONTEXT value)
	{
		return value.Value;
	}

	public unsafe static explicit operator DPI_AWARENESS_CONTEXT(void* value)
	{
		return new DPI_AWARENESS_CONTEXT(value);
	}

	public unsafe static bool operator ==(DPI_AWARENESS_CONTEXT left, DPI_AWARENESS_CONTEXT right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(DPI_AWARENESS_CONTEXT left, DPI_AWARENESS_CONTEXT right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(DPI_AWARENESS_CONTEXT other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is DPI_AWARENESS_CONTEXT other)
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

	public unsafe static implicit operator nint(DPI_AWARENESS_CONTEXT value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator DPI_AWARENESS_CONTEXT(nint value)
	{
		return new DPI_AWARENESS_CONTEXT(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator DPI_AWARENESS_CONTEXT(nuint value)
	{
		return new DPI_AWARENESS_CONTEXT(((UIntPtr)value).ToPointer());
	}
}
