using System;
using System.Collections.Generic;

namespace HeyClicky.Drawing.Media;

public static class Mp4Probe
{
	public sealed record Info(double DurationSeconds, int Width, int Height, int VideoFrames, double AudioSeconds, int AudioChannels, int AudioSampleRate, byte[]? MiddleFrameBgra);

	public unsafe static Info Read(string path)
	{
		MF.Start();
		double durationSeconds = 0.0;
		int width = 0;
		int height = 0;
		int num = 0;
		int audioChannels = 0;
		int audioSampleRate = 0;
		double audioSeconds = 0.0;
		byte[] middleFrameBgra = null;
		nint attributes = 0;
		nint reader = 0;
		try
		{
			MF.Check(MF.MFCreateAttributes(out attributes, 1u), "MFCreateAttributes");
			MF.SetUInt32(attributes, MF.SourceReaderEnableVideoProcessing, 1u);
			MF.Check(MF.MFCreateSourceReaderFromURL(path, attributes, out reader), "MFCreateSourceReaderFromURL");
			byte* ptr = stackalloc byte[24];
			Guid presentationDuration = MF.PresentationDuration;
			if (((delegate* unmanaged<nint, uint, Guid*, byte*, int>)MF.V(reader)[12])(reader, uint.MaxValue, &presentationDuration, ptr) >= 0)
			{
				durationSeconds = (double)(*(ulong*)(ptr + 8)) / 10000000.0;
			}
			MF.PropVariantClear(ptr);
			nint num2 = MF.CreateType(MF.MediaTypeVideo, MF.VideoFormatRgb32);
			int hr = ((delegate* unmanaged<nint, uint, uint*, nint, int>)MF.V(reader)[7])(reader, 4294967292u, null, num2);
			MF.Release(num2);
			MF.Check(hr, "SetCurrentMediaType(RGB32)");
			nint num3 = 0;
			MF.Check(((delegate* unmanaged<nint, uint, nint*, int>)MF.V(reader)[6])(reader, 4294967292u, &num3), "GetCurrentMediaType(video)");
			ulong uInt = MF.GetUInt64(num3, MF.FrameSize);
			int uInt2 = (int)MF.GetUInt32(num3, MF.DefaultStride, (uint)((int)(uInt >> 32) * 4));
			MF.Release(num3);
			width = (int)(uInt >> 32);
			height = (int)(uInt & 0xFFFFFFFFu);
			List<long> list = new List<long>();
			List<byte[]> list2 = new List<byte[]>();
			uint num5 = default(uint);
			uint num6 = default(uint);
			long item = default(long);
			do
			{
				nint num4 = 0;
				MF.Check(((delegate* unmanaged<nint, uint, uint, uint*, uint*, long*, nint*, int>)MF.V(reader)[9])(reader, 4294967292u, 0u, &num5, &num6, &item, &num4), "ReadSample(video)");
				if (num4 != 0)
				{
					num++;
					if (num % 15 == 1 && list2.Count < 64)
					{
						list2.Add(MF.ContiguousBytes(num4, width, height, uInt2));
						list.Add(item);
					}
					MF.Release(num4);
				}
			}
			while ((num6 & 2) == 0);
			if (list2.Count > 0)
			{
				middleFrameBgra = list2[list2.Count / 2];
			}
			nint num7 = 0;
			if (((delegate* unmanaged<nint, uint, nint*, int>)MF.V(reader)[6])(reader, 4294967293u, &num7) >= 0 && num7 != 0)
			{
				audioChannels = (int)MF.GetUInt32(num7, MF.AudioNumChannels, 0u);
				audioSampleRate = (int)MF.GetUInt32(num7, MF.AudioSamplesPerSecond, 0u);
				MF.Release(num7);
				long num8 = 0L;
				uint num10 = default(uint);
				uint num11 = default(uint);
				long num12 = default(long);
				long num13 = default(long);
				do
				{
					nint num9 = 0;
					if (((delegate* unmanaged<nint, uint, uint, uint*, uint*, long*, nint*, int>)MF.V(reader)[9])(reader, 4294967293u, 0u, &num10, &num11, &num12, &num9) < 0)
					{
						break;
					}
					if (num9 != 0)
					{
						if (((delegate* unmanaged<nint, long*, int>)MF.V(num9)[37])(num9, &num13) < 0)
						{
							num13 = 0L;
						}
						num8 = Math.Max(num8, num12 + num13);
						MF.Release(num9);
					}
				}
				while ((num11 & 2) == 0);
				audioSeconds = (double)num8 / 10000000.0;
			}
		}
		finally
		{
			MF.Release(reader);
			MF.Release(attributes);
		}
		return new Info(durationSeconds, width, height, num, audioSeconds, audioChannels, audioSampleRate, middleFrameBgra);
	}
}
