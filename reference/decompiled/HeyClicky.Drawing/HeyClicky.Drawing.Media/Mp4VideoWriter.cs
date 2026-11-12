using System;

namespace HeyClicky.Drawing.Media;

public sealed class Mp4VideoWriter : IDisposable
{
	private readonly int width;

	private readonly int height;

	private readonly int framesPerSecond;

	private readonly long frameDuration;

	private nint writer;

	private uint stream;

	private long lastTime = -1L;

	private bool finished;

	public int Width => width;

	public int Height => height;

	private int Stride => width * 4;

	public double LastFrameSeconds
	{
		get
		{
			if (lastTime >= 0)
			{
				return (double)lastTime / 10000000.0;
			}
			return -1.0;
		}
	}

	public unsafe Mp4VideoWriter(string path, int width, int height, int framesPerSecond = 30)
	{
		this.width = width;
		this.height = height;
		this.framesPerSecond = framesPerSecond;
		frameDuration = 10000000L / (long)framesPerSecond;
		MF.Start();
		nint attributes = 0;
		nint num = 0;
		nint num2 = 0;
		try
		{
			MF.Check(MF.MFCreateAttributes(out attributes, 2u), "MFCreateAttributes");
			MF.SetUInt32(attributes, MF.ReadWriteEnableHardwareTransforms, 1u);
			MF.Check(MF.MFCreateSinkWriterFromURL(path, 0, attributes, out writer), "MFCreateSinkWriterFromURL");
			num = MF.CreateType(MF.MediaTypeVideo, MF.VideoFormatH264);
			MF.SetUInt32(num, MF.AvgBitrate, (uint)Math.Min(2147483647L, (long)width * (long)height * 6));
			MF.SetUInt32(num, MF.InterlaceMode, 2u);
			MF.SetUInt32(num, MF.Mpeg2Profile, 100u);
			SetVideoGeometry(num);
			uint num3 = default(uint);
			MF.Check(((delegate* unmanaged<nint, nint, uint*, int>)MF.V(writer)[3])(writer, num, &num3), "AddStream(video)");
			stream = num3;
			num2 = MF.CreateType(MF.MediaTypeVideo, MF.VideoFormatRgb32);
			MF.SetUInt32(num2, MF.InterlaceMode, 2u);
			MF.SetUInt32(num2, MF.DefaultStride, (uint)Stride);
			SetVideoGeometry(num2);
			MF.Check(((delegate* unmanaged<nint, uint, nint, nint, int>)MF.V(writer)[4])(writer, stream, num2, 0), "SetInputMediaType(video)");
			MF.Check(((delegate* unmanaged<nint, int>)MF.V(writer)[5])(writer), "BeginWriting");
		}
		catch
		{
			MF.Release(writer);
			writer = 0;
			throw;
		}
		finally
		{
			MF.Release(attributes);
			MF.Release(num);
			MF.Release(num2);
		}
	}

	private void SetVideoGeometry(nint type)
	{
		MF.SetUInt64(type, MF.FrameSize, (ulong)(((long)width << 32) | (uint)height));
		MF.SetUInt64(type, MF.FrameRate, (ulong)(((long)framesPerSecond << 32) | 1));
		MF.SetUInt64(type, MF.PixelAspectRatio, 4294967297uL);
	}

	public unsafe bool WriteFrame(ReadOnlySpan<byte> bgra, double seconds)
	{
		if (writer == 0 || finished)
		{
			return false;
		}
		long num = (long)Math.Round(Math.Max(0.0, seconds) * 10000000.0);
		if (num <= lastTime)
		{
			return false;
		}
		int num2 = width * 4;
		if (bgra.Length < num2 * height)
		{
			throw new ArgumentException("frame is smaller than the video");
		}
		int num3 = Stride * height;
		nint buffer = 0;
		nint sample = 0;
		try
		{
			MF.Check(MF.MFCreateMemoryBuffer((uint)num3, out buffer), "MFCreateMemoryBuffer");
			byte* ptr = default(byte*);
			uint num4 = default(uint);
			uint num5 = default(uint);
			MF.Check(((delegate* unmanaged<nint, byte**, uint*, uint*, int>)MF.V(buffer)[3])(buffer, &ptr, &num4, &num5), "Lock");
			for (int i = 0; i < height; i++)
			{
				bgra.Slice(i * num2, num2).CopyTo(new Span<byte>(ptr + (long)i * (long)Stride, num2));
			}
			((delegate* unmanaged<nint, int>)MF.V(buffer)[4])(buffer);
			MF.Check(((delegate* unmanaged<nint, uint, int>)MF.V(buffer)[6])(buffer, (uint)num3), "SetCurrentLength");
			MF.Check(MF.MFCreateSample(out sample), "MFCreateSample");
			MF.Check(((delegate* unmanaged<nint, nint, int>)MF.V(sample)[42])(sample, buffer), "AddBuffer");
			MF.Check(((delegate* unmanaged<nint, long, int>)MF.V(sample)[36])(sample, num), "SetSampleTime");
			MF.Check(((delegate* unmanaged<nint, long, int>)MF.V(sample)[38])(sample, frameDuration), "SetSampleDuration");
			MF.Check(((delegate* unmanaged<nint, uint, nint, int>)MF.V(writer)[6])(writer, stream, sample), "WriteSample(video)");
			lastTime = num;
			return true;
		}
		finally
		{
			MF.Release(sample);
			MF.Release(buffer);
		}
	}

	public unsafe bool Finish()
	{
		if (writer == 0 || finished)
		{
			return false;
		}
		finished = true;
		if (lastTime < 0)
		{
			return false;
		}
		int num = ((delegate* unmanaged<nint, int>)MF.V(writer)[11])(writer);
		if (num < 0)
		{
			Log.Warn("Drawing", $"drawing video: finishing the file failed (0x{num:X8})");
		}
		return num >= 0;
	}

	public void Dispose()
	{
		MF.Release(writer);
		writer = 0;
	}
}
