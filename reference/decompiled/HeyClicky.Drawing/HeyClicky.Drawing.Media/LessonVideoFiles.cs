using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HeyClicky.Drawing.Media;

public static class LessonVideoFiles
{
	public const int SampleRate = 48000;

	public static (int Width, int Height) VideoSize(double width, double height)
	{
		width = Math.Round(width);
		height = Math.Round(height);
		double num = Math.Min(1.0, Math.Min(3840.0 / Math.Max(1.0, width), 2160.0 / Math.Max(1.0, height)));
		int val = (int)Math.Floor(width * num / 16.0) * 16;
		int val2 = (int)Math.Floor(height * num / 2.0) * 2;
		return (Width: Math.Max(16, val), Height: Math.Max(2, val2));
	}

	public static short[]? MixSoundtrack(byte[]? narrationPcm16, int narrationSampleRate, double narrationOffsetSeconds, float[]? effects, double seconds)
	{
		float[] array = new float[Math.Max(0, (int)(seconds * 48000.0))];
		bool flag = false;
		if (narrationPcm16 != null && narrationPcm16.Length > 1 && narrationSampleRate > 0)
		{
			int num = narrationPcm16.Length / 2;
			for (int i = 0; i < array.Length; i++)
			{
				double num2 = ((double)i / 48000.0 + narrationOffsetSeconds) * (double)narrationSampleRate;
				int num3 = (int)num2;
				if (num3 >= 0)
				{
					if (num3 >= num)
					{
						break;
					}
					float num4 = Sample(narrationPcm16, num3);
					float num5 = ((num3 + 1 < num) ? Sample(narrationPcm16, num3 + 1) : num4);
					array[i] += num4 + (num5 - num4) * (float)(num2 - (double)num3);
				}
			}
			flag = true;
		}
		if (effects != null && effects.Length > 0)
		{
			for (int j = 0; j < Math.Min(array.Length, effects.Length); j++)
			{
				array[j] += effects[j];
			}
			flag = true;
		}
		if (!flag)
		{
			return null;
		}
		short[] array2 = new short[array.Length];
		for (int k = 0; k < array.Length; k++)
		{
			array2[k] = (short)Math.Round(Math.Clamp(array[k], -1f, 1f) * 32767f);
		}
		return array2;
	}

	private static float Sample(byte[] pcm16, int index)
	{
		return (float)(short)(pcm16[index * 2] | (pcm16[index * 2 + 1] << 8)) / 32768f;
	}

	public static string SafeFileName(string title)
	{
		string text = string.Join(" ", (title ?? "").Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/))).Trim();
		if (text.Length != 0)
		{
			return text.Substring(0, Math.Min(80, text.Length));
		}
		return "HeyClicky drawing";
	}

	public static string UniquePath(string folder, string baseName, string extension, Func<string, bool>? exists = null)
	{
		if (exists == null)
		{
			exists = File.Exists;
		}
		string text = Path.Combine(folder, baseName + "." + extension);
		int num = 2;
		while (exists(text))
		{
			text = Path.Combine(folder, $"{baseName} {num}.{extension}");
			num++;
		}
		return text;
	}
}
