using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct __char_32
{
	private const int SpanLength = 32;

	internal unsafe fixed char Value[32];

	internal readonly int Length => 32;

	[UnscopedRef]
	internal unsafe ref char this[int index] => ref Value[index];

	[UnscopedRef]
	internal unsafe Span<char> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value[0], 32);
	}

	[UnscopedRef]
	internal unsafe readonly ReadOnlySpan<char> AsReadOnlySpan()
	{
		return MemoryMarshal.CreateReadOnlySpan(in Unsafe.AsRef(in Value[0]), 32);
	}

	internal readonly bool Equals(ReadOnlySpan<char> value)
	{
		if (value.Length != 32)
		{
			return AsReadOnlySpan().SliceAtNull().SequenceEqual(value);
		}
		return AsReadOnlySpan().SequenceEqual(value);
	}

	internal readonly bool Equals(string value)
	{
		return Equals(value.AsSpan());
	}

	internal readonly string ToString(int length)
	{
		ReadOnlySpan<char> readOnlySpan = AsReadOnlySpan();
		readOnlySpan = readOnlySpan.Slice(0, length);
		return readOnlySpan.ToString();
	}

	public override readonly string ToString()
	{
		return AsReadOnlySpan().SliceAtNull().ToString();
	}

	public static implicit operator __char_32(string value)
	{
		return value.AsSpan();
	}

	public static implicit operator __char_32(ReadOnlySpan<char> value)
	{
		Unsafe.SkipInit<__char_32>(out var value2);
		value.CopyTo(value2.AsSpan());
		int length = value.Length;
		Span<char> span = value2.AsSpan();
		span = span.Slice(length, 32 - length);
		span.Clear();
		return value2;
	}
}
