using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeyClicky.Drawing.Media;

internal static class MF
{
	private static int started;

	public const uint FirstVideoStream = 4294967292u;

	public const uint FirstAudioStream = 4294967293u;

	public const uint AllStreams = 4294967294u;

	public const uint MediaSource = uint.MaxValue;

	public const uint SourceReaderEndOfStream = 2u;

	public static readonly Guid MajorType = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");

	public static readonly Guid Subtype = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");

	public static readonly Guid AvgBitrate = new Guid("20332624-fb0d-4d9e-bd0d-cbf6786c102e");

	public static readonly Guid InterlaceMode = new Guid("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");

	public static readonly Guid FrameSize = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");

	public static readonly Guid FrameRate = new Guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");

	public static readonly Guid PixelAspectRatio = new Guid("c6376a1e-8d0a-4027-be45-6d9a0ad39bb6");

	public static readonly Guid DefaultStride = new Guid("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");

	public static readonly Guid Mpeg2Profile = new Guid("ad76a80b-2d5c-4e0b-b375-64e520137036");

	public static readonly Guid AllSamplesIndependent = new Guid("c9173739-5e56-461c-b713-46fb995cb95f");

	public static readonly Guid AudioNumChannels = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a");

	public static readonly Guid AudioSamplesPerSecond = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");

	public static readonly Guid AudioBitsPerSample = new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");

	public static readonly Guid AudioAvgBytesPerSecond = new Guid("1aab75c8-cfef-451c-ab95-ac034b8e1731");

	public static readonly Guid AudioBlockAlignment = new Guid("322de230-9eeb-43bd-ab7a-ff412251541d");

	public static readonly Guid ReadWriteEnableHardwareTransforms = new Guid("a634a91c-822b-41b9-a494-4de4643612b0");

	public static readonly Guid SourceReaderEnableVideoProcessing = new Guid("fb394f3d-ccf1-42ee-bbb3-f9b845d5681d");

	public static readonly Guid PresentationDuration = new Guid("6c990d33-bb8e-477a-8598-0d5d96fcd88a");

	public static readonly Guid MediaTypeVideo = new Guid("73646976-0000-0010-8000-00aa00389b71");

	public static readonly Guid MediaTypeAudio = new Guid("73647561-0000-0010-8000-00aa00389b71");

	public static readonly Guid VideoFormatRgb32 = new Guid("00000016-0000-0010-8000-00aa00389b71");

	public static readonly Guid VideoFormatH264 = new Guid("34363248-0000-0010-8000-00aa00389b71");

	public static readonly Guid AudioFormatAac = new Guid("00001610-0000-0010-8000-00aa00389b71");

	public static readonly Guid AudioFormatPcm = new Guid("00000001-0000-0010-8000-00aa00389b71");

	[DllImport("mfplat.dll")]
	private static extern int MFStartup(uint version, uint flags);

	[DllImport("mfplat.dll")]
	public static extern int MFCreateMediaType(out nint type);

	[DllImport("mfplat.dll")]
	public static extern int MFCreateSample(out nint sample);

	[DllImport("mfplat.dll")]
	public static extern int MFCreateMemoryBuffer(uint maxLength, out nint buffer);

	[DllImport("mfplat.dll")]
	public static extern int MFCreateAttributes(out nint attributes, uint initialSize);

	[DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode)]
	public static extern int MFCreateSinkWriterFromURL(string url, nint byteStream, nint attributes, out nint writer);

	[DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode)]
	public static extern int MFCreateSourceReaderFromURL(string url, nint attributes, out nint reader);

	[DllImport("ole32.dll")]
	public unsafe static extern int PropVariantClear(byte* variant);

	public static void Start()
	{
		if (Interlocked.Exchange(ref started, 1) != 1)
		{
			Check(MFStartup(131184u, 1u), "MFStartup");
		}
	}

	public unsafe static void** V(nint p)
	{
		return *(void***)p;
	}

	public static void Check(int hr, string what)
	{
		if (hr < 0)
		{
			throw new COMException($"Media Foundation: {what} failed (0x{hr:X8})", hr);
		}
	}

	public unsafe static void Release(nint p)
	{
		if (p != 0)
		{
			((delegate* unmanaged<nint, uint>)V(p)[2])(p);
		}
	}

	public static nint CreateType(Guid major, Guid subtype)
	{
		Check(MFCreateMediaType(out var type), "MFCreateMediaType");
		SetGuid(type, MajorType, major);
		SetGuid(type, Subtype, subtype);
		return type;
	}

	public unsafe static void SetUInt32(nint a, Guid key, uint value)
	{
		Check(((delegate* unmanaged<nint, Guid*, uint, int>)V(a)[21])(a, &key, value), "SetUINT32");
	}

	public unsafe static void SetUInt64(nint a, Guid key, ulong value)
	{
		Check(((delegate* unmanaged<nint, Guid*, ulong, int>)V(a)[22])(a, &key, value), "SetUINT64");
	}

	public unsafe static void SetGuid(nint a, Guid key, Guid value)
	{
		Check(((delegate* unmanaged<nint, Guid*, Guid*, int>)V(a)[24])(a, &key, &value), "SetGUID");
	}

	public unsafe static uint GetUInt32(nint a, Guid key, uint fallback)
	{
		uint result = default(uint);
		if (((delegate* unmanaged<nint, Guid*, uint*, int>)V(a)[7])(a, &key, &result) < 0)
		{
			return fallback;
		}
		return result;
	}

	public unsafe static ulong GetUInt64(nint a, Guid key)
	{
		ulong result = default(ulong);
		Check(((delegate* unmanaged<nint, Guid*, ulong*, int>)V(a)[8])(a, &key, &result), "GetUINT64");
		return result;
	}

	public unsafe static byte[] ContiguousBytes(nint sample, int width, int height, int stride)
	{
		nint num = 0;
		Check(((delegate* unmanaged<nint, nint*, int>)V(sample)[41])(sample, &num), "ConvertToContiguousBuffer");
		try
		{
			byte[] array = new byte[width * height * 4];
			int num2 = width * 4;
			Guid iid = new Guid("7DC9D5F9-9ED9-44ec-9BBF-0600BB589FBB");
			if (Marshal.QueryInterface(num, in iid, out var ppv) >= 0 && ppv != 0)
			{
				try
				{
					byte* ptr = default(byte*);
					int num3 = default(int);
					if (((delegate* unmanaged<nint, byte**, int*, int>)V(ppv)[3])(ppv, &ptr, &num3) >= 0)
					{
						for (int i = 0; i < height; i++)
						{
							new Span<byte>(ptr + (long)i * (long)num3, num2).CopyTo(array.AsSpan(i * num2, num2));
						}
						((delegate* unmanaged<nint, int>)V(ppv)[4])(ppv);
						return array;
					}
				}
				finally
				{
					Release(ppv);
				}
			}
			byte* ptr2 = default(byte*);
			uint num4 = default(uint);
			uint num5 = default(uint);
			Check(((delegate* unmanaged<nint, byte**, uint*, uint*, int>)V(num)[3])(num, &ptr2, &num4, &num5), "Lock");
			int num6 = width * 4;
			int num7 = Math.Abs(stride);
			for (int j = 0; j < height; j++)
			{
				int num8 = ((stride >= 0) ? j : (height - 1 - j));
				if ((long)num8 * (long)num7 + num6 > num5)
				{
					break;
				}
				new Span<byte>(ptr2 + (long)num8 * (long)num7, num6).CopyTo(array.AsSpan(j * num6, num6));
			}
			((delegate* unmanaged<nint, int>)V(num)[4])(num);
			return array;
		}
		finally
		{
			Release(num);
		}
	}
}
