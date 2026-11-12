using System;
using System.IO;
using SharpGen.Runtime.Win32;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Vortice.WIC;

namespace HeyClicky.Platform.Overlay;

internal static class OverlayDebug
{
	public unsafe static void SavePng(OverlayHost host, ID2D1Bitmap1 source, string path, Color4 background)
	{
		SizeI pixelSize = source.PixelSize;
		using ID2D1Bitmap1 iD2D1Bitmap = host.D2DContext.CreateBitmap(pixelSize, IntPtr.Zero, 0u, new BitmapProperties1(new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.CannotDraw | BitmapOptions.CpuRead));
		iD2D1Bitmap.CopyFromBitmap(source);
		MappedRectangle mappedRectangle = iD2D1Bitmap.Map(MapOptions.Read);
		byte[] array = new byte[pixelSize.Width * pixelSize.Height * 4];
		try
		{
			for (int i = 0; i < pixelSize.Height; i++)
			{
				byte* ptr = (byte*)(mappedRectangle.Bits + (nint)(i * mappedRectangle.Pitch));
				for (int j = 0; j < pixelSize.Width; j++)
				{
					float num = (float)(int)ptr[j * 4 + 3] / 255f;
					int num2 = (i * pixelSize.Width + j) * 4;
					array[num2] = (byte)Math.Clamp((float)(int)ptr[j * 4] + background.B * 255f * (1f - num), 0f, 255f);
					array[num2 + 1] = (byte)Math.Clamp((float)(int)ptr[j * 4 + 1] + background.G * 255f * (1f - num), 0f, 255f);
					array[num2 + 2] = (byte)Math.Clamp((float)(int)ptr[j * 4 + 2] + background.R * 255f * (1f - num), 0f, 255f);
					array[num2 + 3] = byte.MaxValue;
				}
			}
		}
		finally
		{
			iD2D1Bitmap.Unmap();
		}
		using IWICImagingFactory iWICImagingFactory = new IWICImagingFactory();
		using FileStream stream = File.Create(path);
		using IWICBitmapEncoder iWICBitmapEncoder = iWICImagingFactory.CreateEncoder(ContainerFormat.Png, (Guid?)null);
		iWICBitmapEncoder.Initialize(stream);
		IPropertyBag2 iEncoderOptions;
		using IWICBitmapFrameEncode iWICBitmapFrameEncode = iWICBitmapEncoder.CreateNewFrame(out iEncoderOptions);
		iWICBitmapFrameEncode.Initialize(iEncoderOptions);
		iEncoderOptions.Dispose();
		iWICBitmapFrameEncode.SetSize((uint)pixelSize.Width, (uint)pixelSize.Height);
		Guid pixelFormat = Vortice.WIC.PixelFormat.Format32bppBGRA;
		iWICBitmapFrameEncode.SetPixelFormat(ref pixelFormat);
		fixed (byte* pbPixels = array)
		{
			iWICBitmapFrameEncode.WritePixels((uint)pixelSize.Height, (uint)(pixelSize.Width * 4), (uint)array.Length, pbPixels);
		}
		iWICBitmapFrameEncode.Commit();
		iWICBitmapEncoder.Commit();
	}
}
