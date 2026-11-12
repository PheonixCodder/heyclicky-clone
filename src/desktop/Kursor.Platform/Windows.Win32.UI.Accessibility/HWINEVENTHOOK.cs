using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.UI.Accessibility;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct HWINEVENTHOOK : IEquatable<HWINEVENTHOOK>
{
	internal unsafe readonly void* Value;

	internal static HWINEVENTHOOK Null => default(HWINEVENTHOOK);

	internal unsafe bool IsNull => Value == null;

	internal unsafe HWINEVENTHOOK(void* value)
	{
		Value = value;
	}

	internal unsafe HWINEVENTHOOK(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HWINEVENTHOOK value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HWINEVENTHOOK(void* value)
	{
		return new HWINEVENTHOOK(value);
	}

	public unsafe static bool operator ==(HWINEVENTHOOK left, HWINEVENTHOOK right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HWINEVENTHOOK left, HWINEVENTHOOK right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HWINEVENTHOOK other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HWINEVENTHOOK other)
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

	public unsafe static implicit operator nint(HWINEVENTHOOK value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HWINEVENTHOOK(nint value)
	{
		return new HWINEVENTHOOK(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HWINEVENTHOOK(nuint value)
	{
		return new HWINEVENTHOOK(((UIntPtr)value).ToPointer());
	}
}
