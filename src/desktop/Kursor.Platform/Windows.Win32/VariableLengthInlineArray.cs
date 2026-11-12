using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32;

internal struct VariableLengthInlineArray<T> where T : unmanaged
{
	internal T e0;

	internal ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[UnscopedRef]
		get
		{
			return ref Unsafe.Add(ref e0, index);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[UnscopedRef]
	internal Span<T> AsSpan(int length)
	{
		return MemoryMarshal.CreateSpan(ref e0, length);
	}
}
