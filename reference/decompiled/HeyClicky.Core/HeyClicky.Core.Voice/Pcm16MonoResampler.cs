using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Voice;

internal sealed class Pcm16MonoResampler
{
	private const int ZeroCrossings = 16;

	private readonly int inputRate;

	private readonly int outputRate;

	private readonly long step;

	private readonly int phaseCount;

	private readonly double cutoff;

	private readonly int halfWidth;

	private readonly float[]?[] phases;

	private float[] history;

	private int historyLength;

	private long historyStart;

	private long inputCount;

	private long outputIndex;

	private bool drained;

	public Pcm16MonoResampler(int inputRate, int outputRate)
	{
		this.inputRate = inputRate;
		this.outputRate = outputRate;
		int num = Gcd(inputRate, outputRate);
		step = inputRate / num;
		phaseCount = outputRate / num;
		cutoff = 0.5 * Math.Min(1.0, (double)outputRate / (double)inputRate) * 0.92;
		halfWidth = (int)Math.Ceiling(16.0 / (2.0 * cutoff));
		phases = new float[phaseCount][];
		history = new float[4096];
		historyLength = halfWidth;
		historyStart = -halfWidth;
	}

	public byte[] Process(ReadOnlySpan<byte> pcm16)
	{
		if (drained || pcm16.Length < 2)
		{
			return Array.Empty<byte>();
		}
		if (inputRate == outputRate)
		{
			return pcm16.Slice(0, pcm16.Length & -2).ToArray();
		}
		ReadOnlySpan<short> readOnlySpan = MemoryMarshal.Cast<byte, short>(pcm16);
		Span<float> span = Reserve(readOnlySpan.Length);
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			span[i] = (float)readOnlySpan[i] / 32768f;
		}
		inputCount += readOnlySpan.Length;
		return Produce(inputCount - 1);
	}

	public byte[] Drain()
	{
		if (drained)
		{
			return Array.Empty<byte>();
		}
		drained = true;
		if (inputRate == outputRate)
		{
			return Array.Empty<byte>();
		}
		Reserve(halfWidth).Clear();
		return Produce(inputCount - 1 + halfWidth, inputCount);
	}

	private Span<float> Reserve(int count)
	{
		if (historyLength + count > history.Length)
		{
			Array.Resize(ref history, Math.Max(history.Length * 2, historyLength + count));
		}
		Span<float> result = history.AsSpan(historyLength, count);
		historyLength += count;
		return result;
	}

	private byte[] Produce(long lastInputIndex, long endAt = long.MaxValue)
	{
		List<short> list = new List<short>();
		while (true)
		{
			long num = outputIndex * step;
			long num2 = num / phaseCount;
			if (num2 + halfWidth > lastInputIndex || num2 >= endAt)
			{
				break;
			}
			float[] array = Taps((int)(num % phaseCount));
			int num3 = (int)(num2 - halfWidth + 1 - historyStart);
			double num4 = 0.0;
			for (int i = 0; i < array.Length; i++)
			{
				num4 += (double)(array[i] * history[num3 + i]);
			}
			list.Add((short)Math.Clamp(Math.Round(num4 * 32768.0), -32768.0, 32767.0));
			outputIndex++;
		}
		int num5 = (int)Math.Clamp(outputIndex * step / phaseCount - halfWidth + 1 - historyStart, 0L, historyLength);
		if (num5 > 0)
		{
			Array.Copy(history, num5, history, 0, historyLength - num5);
			historyLength -= num5;
			historyStart += num5;
		}
		return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(list)).ToArray();
	}

	private float[] Taps(int phase)
	{
		float[] array = phases[phase];
		if (array != null)
		{
			return array;
		}
		double num = (double)phase / (double)phaseCount;
		float[] array2 = new float[2 * halfWidth];
		double num2 = 0.0;
		double[] array3 = new double[array2.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			double num3 = (double)(i - halfWidth + 1) - num;
			double num4 = 2.0 * cutoff * num3;
			double num5 = ((Math.Abs(num4) < 1E-12) ? 1.0 : (Math.Sin(Math.PI * num4) / (Math.PI * num4)));
			double num6 = num3 / (double)halfWidth;
			double num7 = ((Math.Abs(num6) >= 1.0) ? 0.0 : (0.42 + 0.5 * Math.Cos(Math.PI * num6) + 0.08 * Math.Cos(Math.PI * 2.0 * num6)));
			array3[i] = num5 * num7;
			num2 += array3[i];
		}
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = (float)(array3[j] / num2);
		}
		return phases[phase] = array2;
	}

	private static int Gcd(int a, int b)
	{
		while (b != 0)
		{
			int num = b;
			b = a % b;
			a = num;
		}
		return a;
	}
}
