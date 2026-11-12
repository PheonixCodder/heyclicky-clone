using System;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Voice;

public static class Pcm16
{
	public static byte[] WavFromPcm16Mono(ReadOnlySpan<byte> pcm16, int sampleRate)
	{
		byte[] array = new byte[44 + pcm16.Length];
		Span<byte> destination = array.AsSpan();
		"RIFF"u8.CopyTo(destination);
		ref Span<byte> reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(4, reference.Length - 4), (uint)(36 + pcm16.Length));
		ReadOnlySpan<byte> readOnlySpan = "WAVE"u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(8, reference.Length - 8));
		readOnlySpan = "fmt "u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(12, reference.Length - 12));
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(16, reference.Length - 16), 16u);
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(20, reference.Length - 20), (ushort)1);
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(22, reference.Length - 22), (ushort)1);
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(24, reference.Length - 24), (uint)sampleRate);
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(28, reference.Length - 28), (uint)(sampleRate * 2));
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(32, reference.Length - 32), (ushort)2);
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(34, reference.Length - 34), (ushort)16);
		readOnlySpan = "data"u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(36, reference.Length - 36));
		reference = ref destination;
		BitConverter.TryWriteBytes(reference.Slice(40, reference.Length - 40), (uint)pcm16.Length);
		reference = ref destination;
		pcm16.CopyTo(reference.Slice(44, reference.Length - 44));
		return array;
	}

	public static float PowerLevel(ReadOnlySpan<byte> pcm16)
	{
		ReadOnlySpan<short> readOnlySpan = MemoryMarshal.Cast<byte, short>(pcm16);
		if (readOnlySpan.IsEmpty)
		{
			return 0f;
		}
		double num = 0.0;
		ReadOnlySpan<short> readOnlySpan2 = readOnlySpan;
		for (int i = 0; i < readOnlySpan2.Length; i++)
		{
			double num2 = (double)readOnlySpan2[i] / 32768.0;
			num += num2 * num2;
		}
		return (float)Math.Clamp(Math.Sqrt(num / (double)readOnlySpan.Length) * 10.2, 0.0, 1.0);
	}
}
