using System;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing.Media;

public static class Mp4Soundtrack
{
	public unsafe static void Mux(string videoPath, ReadOnlySpan<short> pcm16Mono, string outputPath)
	{
		MF.Start();
		nint reader = 0;
		nint writer = 0;
		nint num = 0;
		nint num2 = 0;
		nint num3 = 0;
		try
		{
			MF.Check(MF.MFCreateSourceReaderFromURL(videoPath, 0, out reader), "MFCreateSourceReaderFromURL");
			MF.Check(((delegate* unmanaged<nint, uint, int, int>)MF.V(reader)[4])(reader, 4294967294u, 0), "SetStreamSelection(all)");
			MF.Check(((delegate* unmanaged<nint, uint, int, int>)MF.V(reader)[4])(reader, 4294967292u, 1), "SetStreamSelection(video)");
			MF.Check(((delegate* unmanaged<nint, uint, nint*, int>)MF.V(reader)[6])(reader, 4294967292u, &num), "GetCurrentMediaType(video)");
			MF.Check(MF.MFCreateSinkWriterFromURL(outputPath, 0, 0, out writer), "MFCreateSinkWriterFromURL");
			uint num4 = default(uint);
			MF.Check(((delegate* unmanaged<nint, nint, uint*, int>)MF.V(writer)[3])(writer, num, &num4), "AddStream(video)");
			num2 = MF.CreateType(MF.MediaTypeAudio, MF.AudioFormatAac);
			SetAudio(num2, 48000);
			MF.SetUInt32(num2, MF.AudioAvgBytesPerSecond, 24000u);
			uint num5 = default(uint);
			MF.Check(((delegate* unmanaged<nint, nint, uint*, int>)MF.V(writer)[3])(writer, num2, &num5), "AddStream(audio)");
			num3 = MF.CreateType(MF.MediaTypeAudio, MF.AudioFormatPcm);
			SetAudio(num3, 48000);
			MF.SetUInt32(num3, MF.AudioBlockAlignment, 2u);
			MF.SetUInt32(num3, MF.AudioAvgBytesPerSecond, 96000u);
			MF.SetUInt32(num3, MF.AllSamplesIndependent, 1u);
			MF.Check(((delegate* unmanaged<nint, uint, nint, nint, int>)MF.V(writer)[4])(writer, num5, num3, 0), "SetInputMediaType(audio)");
			MF.Check(((delegate* unmanaged<nint, int>)MF.V(writer)[5])(writer), "BeginWriting");
			int written = 0;
			long num6 = 0L;
			nint num7 = writer;
			uint num8 = num4;
			uint stream = num5;
			uint num10 = default(uint);
			uint num11 = default(uint);
			long num12 = default(long);
			long num13 = default(long);
			do
			{
				nint num9 = 0;
				MF.Check(((delegate* unmanaged<nint, uint, uint, uint*, uint*, long*, nint*, int>)MF.V(reader)[9])(reader, 4294967292u, 0u, &num10, &num11, &num12, &num9), "ReadSample");
				if (num9 == 0)
				{
					continue;
				}
				try
				{
					written = WriteAudioUntil(num7, stream, pcm16Mono, written, num12 + 5000000);
					if (((delegate* unmanaged<nint, long*, int>)MF.V(num9)[37])(num9, &num13) < 0)
					{
						num13 = 0L;
					}
					num6 = Math.Max(num6, num12 + num13);
					MF.Check(((delegate* unmanaged<nint, uint, nint, int>)MF.V(num7)[6])(num7, num8, num9), "WriteSample(video)");
				}
				finally
				{
					MF.Release(num9);
				}
			}
			while ((num11 & 2) == 0);
			long val = num6 * 48000 / 10000000;
			WriteAudioUntil(num7, stream, pcm16Mono.Slice(0, (int)Math.Min(pcm16Mono.Length, Math.Max(0L, val))), written, num6);
			MF.Check(((delegate* unmanaged<nint, int>)MF.V(writer)[11])(writer), "Finalize");
		}
		finally
		{
			MF.Release(num3);
			MF.Release(num2);
			MF.Release(num);
			MF.Release(writer);
			MF.Release(reader);
		}
	}

	private static int WriteAudioUntil(nint writer, uint stream, ReadOnlySpan<short> pcm, int written, long time)
	{
		while (written < pcm.Length && (long)written * 10000000L / 48000 < time)
		{
			int num = Math.Min(4800, pcm.Length - written);
			WriteAudio(writer, stream, pcm.Slice(written, num), (long)written * 10000000L / 48000, (long)num * 10000000L / 48000);
			written += num;
		}
		return written;
	}

	private static void SetAudio(nint type, int sampleRate)
	{
		MF.SetUInt32(type, MF.AudioNumChannels, 1u);
		MF.SetUInt32(type, MF.AudioSamplesPerSecond, (uint)sampleRate);
		MF.SetUInt32(type, MF.AudioBitsPerSample, 16u);
	}

	private unsafe static void WriteAudio(nint writer, uint stream, ReadOnlySpan<short> pcm, long time, long duration)
	{
		nint buffer = 0;
		nint sample = 0;
		int num = pcm.Length * 2;
		try
		{
			MF.Check(MF.MFCreateMemoryBuffer((uint)num, out buffer), "MFCreateMemoryBuffer");
			byte* pointer = default(byte*);
			uint num2 = default(uint);
			uint num3 = default(uint);
			MF.Check(((delegate* unmanaged<nint, byte**, uint*, uint*, int>)MF.V(buffer)[3])(buffer, &pointer, &num2, &num3), "Lock");
			MemoryMarshal.AsBytes(pcm).CopyTo(new Span<byte>(pointer, num));
			((delegate* unmanaged<nint, int>)MF.V(buffer)[4])(buffer);
			MF.Check(((delegate* unmanaged<nint, uint, int>)MF.V(buffer)[6])(buffer, (uint)num), "SetCurrentLength");
			MF.Check(MF.MFCreateSample(out sample), "MFCreateSample");
			MF.Check(((delegate* unmanaged<nint, nint, int>)MF.V(sample)[42])(sample, buffer), "AddBuffer");
			MF.Check(((delegate* unmanaged<nint, long, int>)MF.V(sample)[36])(sample, time), "SetSampleTime");
			MF.Check(((delegate* unmanaged<nint, long, int>)MF.V(sample)[38])(sample, duration), "SetSampleDuration");
			MF.Check(((delegate* unmanaged<nint, uint, nint, int>)MF.V(writer)[6])(writer, stream, sample), "WriteSample(audio)");
		}
		finally
		{
			MF.Release(sample);
			MF.Release(buffer);
		}
	}
}
