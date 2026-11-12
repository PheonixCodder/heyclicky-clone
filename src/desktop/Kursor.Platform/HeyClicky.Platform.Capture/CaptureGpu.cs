using System;
using System.Collections.Generic;
using System.Numerics;
using Vortice;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace HeyClicky.Platform.Capture;

internal sealed class CaptureGpu : IDisposable
{
	public readonly object Gate = new object();

	public readonly ID3D11Device Device;

	public readonly ID3D11DeviceContext Context;

	public readonly IDXGIAdapter1 Adapter;

	private readonly ID2D1Factory1 d2dFactory;

	private readonly ID2D1Device d2dDevice;

	private readonly ID2D1DeviceContext d2dContext;

	private readonly Dictionary<(int, int), ID3D11Texture2D> sourceCopies = new Dictionary<(int, int), ID3D11Texture2D>();

	private readonly Dictionary<(int, int), (ID3D11Texture2D Target, ID2D1Bitmap1 TargetBitmap, ID3D11Texture2D Staging)> scaledTargets = new Dictionary<(int, int), (ID3D11Texture2D, ID2D1Bitmap1, ID3D11Texture2D)>();

	public CaptureGpu(IDXGIAdapter1 adapter)
	{
		Adapter = adapter;
		Vortice.Direct3D.FeatureLevel[] featureLevels = new Vortice.Direct3D.FeatureLevel[4]
		{
			Vortice.Direct3D.FeatureLevel.Level_11_1,
			Vortice.Direct3D.FeatureLevel.Level_11_0,
			Vortice.Direct3D.FeatureLevel.Level_10_1,
			Vortice.Direct3D.FeatureLevel.Level_10_0
		};
		D3D11.D3D11CreateDevice((IDXGIAdapter?)adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, featureLevels, out ID3D11Device device, out ID3D11DeviceContext immediateContext).CheckError();
		Device = device;
		Context = immediateContext;
		using (ID3D11Multithread iD3D11Multithread = Device.QueryInterfaceOrNull<ID3D11Multithread>())
		{
			iD3D11Multithread?.SetMultithreadProtected(true);
		}
		d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
		using IDXGIDevice dxgiDevice = Device.QueryInterface<IDXGIDevice>();
		d2dDevice = d2dFactory.CreateDevice(dxgiDevice);
		d2dContext = d2dDevice.CreateDeviceContext(DeviceContextOptions.None);
	}

	public ID3D11Texture2D GetSourceCopyTexture(int width, int height)
	{
		if (sourceCopies.TryGetValue((width, height), out ID3D11Texture2D value))
		{
			return value;
		}
		ID3D11Texture2D iD3D11Texture2D = Device.CreateTexture2D(new Texture2DDescription
		{
			Width = (uint)width,
			Height = (uint)height,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(1u, 0u),
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.ShaderResource
		});
		sourceCopies[(width, height)] = iD3D11Texture2D;
		return iD3D11Texture2D;
	}

	public ID3D11Texture2D ScaleToStaging(ID3D11Texture2D source, int width, int height, ModeRotation rotation)
	{
		if (!scaledTargets.TryGetValue((width, height), out (ID3D11Texture2D, ID2D1Bitmap1, ID3D11Texture2D) value))
		{
			ID3D11Device device = Device;
			Texture2DDescription description = new Texture2DDescription
			{
				Width = (uint)width,
				Height = (uint)height,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = Format.B8G8R8A8_UNorm,
				SampleDescription = new SampleDescription(1u, 0u),
				Usage = ResourceUsage.Default,
				BindFlags = (BindFlags.ShaderResource | BindFlags.RenderTarget)
			};
			ID3D11Texture2D iD3D11Texture2D = device.CreateTexture2D(in description);
			using IDXGISurface surface = iD3D11Texture2D.QueryInterface<IDXGISurface>();
			ID2D1Bitmap1 item = d2dContext.CreateBitmapFromDxgiSurface(surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Ignore), 96f, 96f, BitmapOptions.Target | BitmapOptions.CannotDraw));
			ID3D11Device device2 = Device;
			description = new Texture2DDescription
			{
				Width = (uint)width,
				Height = (uint)height,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = Format.B8G8R8A8_UNorm,
				SampleDescription = new SampleDescription(1u, 0u),
				Usage = ResourceUsage.Staging,
				CPUAccessFlags = CpuAccessFlags.Read
			};
			ID3D11Texture2D item2 = device2.CreateTexture2D(in description);
			value = (iD3D11Texture2D, item, item2);
			scaledTargets[(width, height)] = value;
		}
		using IDXGISurface surface2 = source.QueryInterface<IDXGISurface>();
		using ID2D1Bitmap1 iD2D1Bitmap = d2dContext.CreateBitmapFromDxgiSurface(surface2, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Ignore), 96f, 96f, BitmapOptions.None));
		SizeI pixelSize = iD2D1Bitmap.PixelSize;
		d2dContext.Target = value.Item2;
		d2dContext.BeginDraw();
		d2dContext.Transform = RotationTransform(rotation, width, height);
		bool flag = ((rotation == ModeRotation.Rotate90 || rotation == ModeRotation.Rotate270) ? true : false);
		RawRectF value2 = (flag ? new RawRectF(0f, 0f, height, width) : new RawRectF(0f, 0f, width, height));
		d2dContext.DrawBitmap(iD2D1Bitmap, value2, 1f, Vortice.Direct2D1.InterpolationMode.HighQualityCubic, new RawRectF(0f, 0f, pixelSize.Width, pixelSize.Height), null);
		d2dContext.EndDraw();
		d2dContext.Target = null;
		Context.CopyResource(value.Item3, value.Item1);
		return value.Item3;
	}

	private static Matrix3x2 RotationTransform(ModeRotation rotation, int width, int height)
	{
		return rotation switch
		{
			ModeRotation.Rotate90 => Matrix3x2.CreateRotation((float)Math.PI / 2f) * Matrix3x2.CreateTranslation(width, 0f), 
			ModeRotation.Rotate180 => Matrix3x2.CreateRotation((float)Math.PI) * Matrix3x2.CreateTranslation(width, height), 
			ModeRotation.Rotate270 => Matrix3x2.CreateRotation(-(float)Math.PI / 2f) * Matrix3x2.CreateTranslation(0f, height), 
			_ => Matrix3x2.Identity, 
		};
	}

	public unsafe void ReadStaging(ID3D11Texture2D staging, int width, int height, byte[] destination)
	{
		MappedSubresource mappedSubresource = Context.Map(staging, 0u);
		try
		{
			int num = width * 4;
			for (int i = 0; i < height; i++)
			{
				new ReadOnlySpan<byte>((void*)(mappedSubresource.DataPointer + (nint)(i * mappedSubresource.RowPitch)), num).CopyTo(destination.AsSpan(i * num, num));
			}
		}
		finally
		{
			Context.Unmap(staging, 0u);
		}
	}

	public void Dispose()
	{
		foreach (ID3D11Texture2D value in sourceCopies.Values)
		{
			value.Dispose();
		}
		foreach (var value2 in scaledTargets.Values)
		{
			value2.TargetBitmap.Dispose();
			value2.Target.Dispose();
			value2.Staging.Dispose();
		}
		d2dContext.Dispose();
		d2dDevice.Dispose();
		d2dFactory.Dispose();
		Context.Dispose();
		Device.Dispose();
		Adapter.Dispose();
	}
}
