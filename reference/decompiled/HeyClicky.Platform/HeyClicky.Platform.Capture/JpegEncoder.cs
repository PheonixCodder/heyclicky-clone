using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpGen.Runtime.Win32;
using Vortice.WIC;

namespace HeyClicky.Platform.Capture;

internal static class JpegEncoder
{
	private struct PropBag2
	{
		public uint Type;

		public ushort VariantType;

		public ushort ClipboardFormat;

		public uint Hint;

		public nint PropertyName;

		public Guid Clsid;
	}

	[StructLayout(LayoutKind.Explicit, Size = 24)]
	private struct Variant
	{
		[FieldOffset(0)]
		public ushort VariantType;

		[FieldOffset(8)]
		public float FloatValue;
	}

	private static readonly Lazy<IWICImagingFactory> Factory = new Lazy<IWICImagingFactory>(() => new IWICImagingFactory());

	public unsafe static byte[] Encode(byte[] bgra, int width, int height, float quality, MemoryStream scratch)
	{
		scratch.SetLength(0L);
		using IWICBitmapEncoder iWICBitmapEncoder = Factory.Value.CreateEncoder(ContainerFormat.Jpeg, (Guid?)null);
		iWICBitmapEncoder.Initialize(scratch);
		IPropertyBag2 iEncoderOptions;
		using IWICBitmapFrameEncode iWICBitmapFrameEncode = iWICBitmapEncoder.CreateNewFrame(out iEncoderOptions);
		SetImageQuality(iEncoderOptions, quality);
		iWICBitmapFrameEncode.Initialize(iEncoderOptions);
		iEncoderOptions.Dispose();
		iWICBitmapFrameEncode.SetSize((uint)width, (uint)height);
		Guid pixelFormat = PixelFormat.Format24bppBGR;
		iWICBitmapFrameEncode.SetPixelFormat(ref pixelFormat);
		fixed (byte* pbBuffer = bgra)
		{
			using IWICBitmap bitmapSource = Factory.Value.CreateBitmapFromMemory((uint)width, (uint)height, PixelFormat.Format32bppBGR, (uint)(width * 4), (uint)(width * height * 4), pbBuffer);
			iWICBitmapFrameEncode.WriteSource(bitmapSource);
		}
		iWICBitmapFrameEncode.Commit();
		iWICBitmapEncoder.Commit();
		return scratch.ToArray();
	}

	private unsafe static void SetImageQuality(IPropertyBag2 propertyBag, float quality)
	{
		nint num = Marshal.StringToCoTaskMemUni("ImageQuality");
		try
		{
			PropBag2 propBag = default(PropBag2);
			propBag.PropertyName = num;
			Variant variant = default(Variant);
			variant.VariantType = 4;
			variant.FloatValue = quality;
			int num2 = ((delegate* unmanaged[Stdcall]<nint, uint, PropBag2*, Variant*, int>)(*(IntPtr*)((nint)(*(IntPtr*)propertyBag.NativePointer) + (nint)4 * (nint)sizeof(nint))))(propertyBag.NativePointer, 1u, &propBag, &variant);
			if (num2 < 0)
			{
				Log.Warn("Capture", $"JPEG ImageQuality not set (0x{num2:X8})");
			}
		}
		finally
		{
			Marshal.FreeCoTaskMem(num);
		}
	}
}
