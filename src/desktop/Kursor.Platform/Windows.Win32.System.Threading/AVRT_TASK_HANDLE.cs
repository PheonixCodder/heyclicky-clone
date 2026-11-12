using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.System.Threading;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal readonly struct AVRT_TASK_HANDLE : IEquatable<AVRT_TASK_HANDLE>
{
	internal unsafe readonly void* Value;

	internal static AVRT_TASK_HANDLE Null => default(AVRT_TASK_HANDLE);

	internal unsafe bool IsNull => Value == null;

	internal unsafe AVRT_TASK_HANDLE(void* value)
	{
		Value = value;
	}

	internal unsafe AVRT_TASK_HANDLE(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(AVRT_TASK_HANDLE value)
	{
		return value.Value;
	}

	public unsafe static explicit operator AVRT_TASK_HANDLE(void* value)
	{
		return new AVRT_TASK_HANDLE(value);
	}

	public unsafe static bool operator ==(AVRT_TASK_HANDLE left, AVRT_TASK_HANDLE right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(AVRT_TASK_HANDLE left, AVRT_TASK_HANDLE right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(AVRT_TASK_HANDLE other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is AVRT_TASK_HANDLE other)
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

	public unsafe static implicit operator nint(AVRT_TASK_HANDLE value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator AVRT_TASK_HANDLE(nint value)
	{
		return new AVRT_TASK_HANDLE(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator AVRT_TASK_HANDLE(nuint value)
	{
		return new AVRT_TASK_HANDLE(((UIntPtr)value).ToPointer());
	}
}
