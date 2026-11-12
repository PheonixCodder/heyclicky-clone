using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public static class LessonNarration
{
	public const string FileName = "narration.wav";

	public static byte[] BuildTrack(IReadOnlyList<double> lineStarts, IReadOnlyList<byte[]> lineAudio, int sampleRate, double duration)
	{
		int num = Math.Max(0, (int)(duration * (double)sampleRate));
		int[] array = new int[num];
		for (int i = 0; i < Math.Min(lineStarts.Count, lineAudio.Count); i++)
		{
			int num2 = Math.Max(0, (int)(lineStarts[i] * (double)sampleRate));
			byte[] array2 = lineAudio[i];
			for (int j = 0; j < array2.Length / 2 && num2 + j < num; j++)
			{
				array[num2 + j] += BinaryPrimitives.ReadInt16LittleEndian(array2.AsSpan(j * 2));
			}
		}
		byte[] array3 = new byte[num * 2];
		for (int k = 0; k < num; k++)
		{
			BinaryPrimitives.WriteInt16LittleEndian(array3.AsSpan(k * 2), (short)Math.Clamp(array[k], -32768, 32767));
		}
		return array3;
	}

	public static byte[] ToWav(ReadOnlySpan<byte> pcm16Mono, int sampleRate)
	{
		int num = pcm16Mono.Length & -2;
		byte[] array = new byte[44 + num];
		Span<byte> destination = array.AsSpan();
		"RIFF"u8.CopyTo(destination);
		ref Span<byte> reference = ref destination;
		BinaryPrimitives.WriteInt32LittleEndian(reference.Slice(4, reference.Length - 4), 36 + num);
		ReadOnlySpan<byte> readOnlySpan = "WAVE"u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(8, reference.Length - 8));
		readOnlySpan = "fmt "u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(12, reference.Length - 12));
		reference = ref destination;
		BinaryPrimitives.WriteInt32LittleEndian(reference.Slice(16, reference.Length - 16), 16);
		reference = ref destination;
		BinaryPrimitives.WriteInt16LittleEndian(reference.Slice(20, reference.Length - 20), 1);
		reference = ref destination;
		BinaryPrimitives.WriteInt16LittleEndian(reference.Slice(22, reference.Length - 22), 1);
		reference = ref destination;
		BinaryPrimitives.WriteInt32LittleEndian(reference.Slice(24, reference.Length - 24), sampleRate);
		reference = ref destination;
		BinaryPrimitives.WriteInt32LittleEndian(reference.Slice(28, reference.Length - 28), sampleRate * 2);
		reference = ref destination;
		BinaryPrimitives.WriteInt16LittleEndian(reference.Slice(32, reference.Length - 32), 2);
		reference = ref destination;
		BinaryPrimitives.WriteInt16LittleEndian(reference.Slice(34, reference.Length - 34), 16);
		readOnlySpan = "data"u8;
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(36, reference.Length - 36));
		reference = ref destination;
		BinaryPrimitives.WriteInt32LittleEndian(reference.Slice(40, reference.Length - 40), num);
		readOnlySpan = pcm16Mono.Slice(0, num);
		reference = ref destination;
		readOnlySpan.CopyTo(reference.Slice(44, reference.Length - 44));
		return array;
	}

	public static (byte[] Pcm16Mono, int SampleRate)? FromWav(ReadOnlySpan<byte> wav)
	{
		if (wav.Length < 12 || !wav.Slice(0, 4).SequenceEqual("RIFF"u8) || !wav.Slice(8, 4).SequenceEqual("WAVE"u8))
		{
			return null;
		}
		int num = 0;
		bool flag = false;
		int num4;
		int num5;
		for (int num2 = 12; num2 + 8 <= wav.Length; num2 = num5 + num4 + (num4 & 1))
		{
			ReadOnlySpan<byte> span = wav.Slice(num2, 4);
			ref ReadOnlySpan<byte> reference = ref wav;
			int num3 = num2 + 4;
			num4 = BinaryPrimitives.ReadInt32LittleEndian(reference.Slice(num3, reference.Length - num3));
			num5 = num2 + 8;
			if (num4 < 0 || num5 + num4 > wav.Length)
			{
				num4 = wav.Length - num5;
			}
			int num6;
			if (span.SequenceEqual("fmt "u8) && num4 >= 16)
			{
				reference = ref wav;
				num3 = num5;
				if (BinaryPrimitives.ReadInt16LittleEndian(reference.Slice(num3, reference.Length - num3)) == 1)
				{
					reference = ref wav;
					num3 = num5 + 2;
					if (BinaryPrimitives.ReadInt16LittleEndian(reference.Slice(num3, reference.Length - num3)) == 1)
					{
						reference = ref wav;
						num3 = num5 + 14;
						num6 = ((BinaryPrimitives.ReadInt16LittleEndian(reference.Slice(num3, reference.Length - num3)) == 16) ? 1 : 0);
						goto IL_013b;
					}
				}
				num6 = 0;
				goto IL_013b;
			}
			if (span.SequenceEqual("data"u8))
			{
				if (!flag || num <= 0)
				{
					return null;
				}
				return (wav.Slice(num5, num4 & -2).ToArray(), num);
			}
			continue;
			IL_013b:
			flag = (byte)num6 != 0;
			reference = ref wav;
			num3 = num5 + 4;
			num = BinaryPrimitives.ReadInt32LittleEndian(reference.Slice(num3, reference.Length - num3));
		}
		return null;
	}
}
