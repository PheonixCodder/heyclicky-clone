using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public sealed class TimeStretcher
{
	private readonly int window;

	private readonly int hop;

	private readonly int tolerance;

	private readonly float[] hann;

	private readonly float[] accumulator;

	private float[] input = new float[8192];

	private int inputCount;

	private long inputBase;

	private double nominal;

	private long previous = -1L;

	private long totalInput;

	private bool flushed;

	public double Rate { get; set; }

	public TimeStretcher(int sampleRate, double rate)
	{
		window = Math.Max(64, sampleRate * 30 / 1000 / 2 * 2);
		hop = window / 2;
		tolerance = Math.Max(8, sampleRate * 10 / 1000);
		hann = new float[window];
		for (int i = 0; i < window; i++)
		{
			hann[i] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * (double)i / (double)window));
		}
		accumulator = new float[window];
		Rate = rate;
	}

	public byte[] Process(ReadOnlySpan<byte> pcm16)
	{
		if (flushed)
		{
			throw new InvalidOperationException("the stretcher was flushed");
		}
		int num = pcm16.Length / 2;
		EnsureCapacity(inputCount + num);
		for (int i = 0; i < num; i++)
		{
			input[inputCount + i] = (float)(short)(pcm16[2 * i] | (pcm16[2 * i + 1] << 8)) / 32768f;
		}
		inputCount += num;
		totalInput += num;
		List<float> list = new List<float>();
		RunFrames(list, endOfInput: false);
		return ToPcm16(list);
	}

	public byte[] Flush()
	{
		if (flushed)
		{
			return Array.Empty<byte>();
		}
		flushed = true;
		long realEnd = inputBase + inputCount;
		EnsureCapacity(inputCount + window + 2 * tolerance + hop);
		Array.Clear(input, inputCount, window + 2 * tolerance + hop);
		inputCount += window + 2 * tolerance + hop;
		List<float> list = new List<float>();
		RunFrames(list, endOfInput: true, realEnd);
		if (previous >= 0)
		{
			for (int i = 0; i < hop; i++)
			{
				list.Add(accumulator[i]);
			}
		}
		return ToPcm16(list);
	}

	private void RunFrames(List<float> output, bool endOfInput, long realEnd = long.MaxValue)
	{
		long num = inputBase + inputCount;
		while (true)
		{
			long num2 = (long)Math.Round(nominal);
			if (endOfInput && num2 >= realEnd)
			{
				break;
			}
			long low = Math.Max(inputBase, num2 - tolerance);
			long num3 = num2 + tolerance;
			if (num3 + window > num || (previous >= 0 && previous + hop + hop > num))
			{
				break;
			}
			long start = ((previous < 0) ? Math.Max(inputBase, num2) : BestStart(low, num3));
			AddFrame(start, previous < 0);
			for (int i = 0; i < hop; i++)
			{
				output.Add(accumulator[i]);
			}
			Array.Copy(accumulator, hop, accumulator, 0, window - hop);
			Array.Clear(accumulator, window - hop, hop);
			previous = start;
			nominal += (double)hop * Math.Max(0.25, Rate);
			Trim();
		}
	}

	private long BestStart(long low, long high)
	{
		long natural = previous + hop;
		long num = low;
		double num2 = double.NegativeInfinity;
		for (long num3 = low; num3 <= high; num3 += 2)
		{
			double num4 = Similarity(num3, natural);
			if (num4 > num2)
			{
				num2 = num4;
				num = num3;
			}
		}
		long[] array = new long[2]
		{
			num - 1,
			num + 1
		};
		foreach (long num5 in array)
		{
			if (num5 >= low && num5 <= high)
			{
				double num6 = Similarity(num5, natural);
				if (num6 > num2)
				{
					num2 = num6;
					num = num5;
				}
			}
		}
		return num;
	}

	private double Similarity(long candidate, long natural)
	{
		int num = (int)(candidate - inputBase);
		int num2 = (int)(natural - inputBase);
		double num3 = 0.0;
		double num4 = 1E-09;
		for (int i = 0; i < hop; i++)
		{
			float num5 = input[num + i];
			num3 += (double)(num5 * input[num2 + i]);
			num4 += (double)(num5 * num5);
		}
		return num3 / Math.Sqrt(num4);
	}

	private void AddFrame(long start, bool first)
	{
		int num = (int)(start - inputBase);
		for (int i = 0; i < window; i++)
		{
			accumulator[i] += input[num + i] * ((first && i < hop) ? 1f : hann[i]);
		}
	}

	private void Trim()
	{
		int num = (int)Math.Clamp(Math.Min(previous + hop, (long)Math.Round(nominal) - tolerance) - inputBase, 0L, inputCount);
		if (num >= 4096)
		{
			Array.Copy(input, num, input, 0, inputCount - num);
			inputCount -= num;
			inputBase += num;
		}
	}

	private void EnsureCapacity(int needed)
	{
		if (needed > input.Length)
		{
			Array.Resize(ref input, Math.Max(needed, input.Length * 2));
		}
	}

	private static byte[] ToPcm16(List<float> samples)
	{
		byte[] array = new byte[samples.Count * 2];
		for (int i = 0; i < samples.Count; i++)
		{
			short num = (short)Math.Clamp(Math.Round(samples[i] * 32768f), -32768.0, 32767.0);
			array[2 * i] = (byte)num;
			array[2 * i + 1] = (byte)(num >> 8);
		}
		return array;
	}
}
