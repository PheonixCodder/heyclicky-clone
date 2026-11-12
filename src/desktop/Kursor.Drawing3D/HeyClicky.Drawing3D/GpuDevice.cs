using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SharpGen.Runtime;
using Vortice.D3DCompiler;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;

namespace HeyClicky.Drawing3D;

public sealed class GpuDevice : IDisposable
{
	private static readonly Lazy<GpuDevice> shared = new Lazy<GpuDevice>(() => new GpuDevice(), LazyThreadSafetyMode.ExecutionAndPublication);

	public readonly object ContextLock = new object();

	private static readonly string CacheDirectory = AppPaths.Combine("shadercache");

	public static GpuDevice Shared => shared.Value;

	public ID3D11Device1 Device { get; }

	public ID3D11DeviceContext1 Context { get; }

	public IDXGIFactory2 Factory { get; }

	public ID2D1Factory1 D2DFactory { get; }

	public ID2D1Device D2DDevice { get; }

	public IDWriteFactory DWriteFactory { get; }

	public Vortice.Direct3D.FeatureLevel FeatureLevel { get; }

	public string AdapterName { get; }

	public static bool TryGetShared(out GpuDevice gpu)
	{
		try
		{
			gpu = shared.Value;
			return true;
		}
		catch
		{
			gpu = null;
			return false;
		}
	}

	private GpuDevice()
	{
		long timestamp = Stopwatch.GetTimestamp();
		Vortice.Direct3D.FeatureLevel[] featureLevels = new Vortice.Direct3D.FeatureLevel[2]
		{
			Vortice.Direct3D.FeatureLevel.Level_11_1,
			Vortice.Direct3D.FeatureLevel.Level_11_0
		};
		DeviceCreationFlags deviceCreationFlags = DeviceCreationFlags.BgraSupport;
		if (Environment.GetEnvironmentVariable("HEYCLICKY_D3D_DEBUG") == "1")
		{
			deviceCreationFlags |= DeviceCreationFlags.Debug;
		}
		ID3D11Device device;
		Vortice.Direct3D.FeatureLevel featureLevel;
		ID3D11DeviceContext immediateContext;
		Result result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, deviceCreationFlags, featureLevels, out device, out featureLevel, out immediateContext);
		if (result.Failure)
		{
			Log.Warn("3D", $"no hardware Direct3D 11 device (0x{result.Code:X8}): drawing 3D with WARP");
			D3D11.D3D11CreateDevice(null, DriverType.Warp, deviceCreationFlags, featureLevels, out device, out featureLevel, out immediateContext).CheckError();
		}
		Device = device.QueryInterface<ID3D11Device1>();
		Context = immediateContext.QueryInterface<ID3D11DeviceContext1>();
		device.Dispose();
		immediateContext.Dispose();
		FeatureLevel = featureLevel;
		using (ID3D11Multithread iD3D11Multithread = Context.QueryInterface<ID3D11Multithread>())
		{
			iD3D11Multithread.SetMultithreadProtected(true);
		}
		using IDXGIDevice iDXGIDevice = Device.QueryInterface<IDXGIDevice>();
		using IDXGIAdapter iDXGIAdapter = iDXGIDevice.GetAdapter();
		AdapterName = iDXGIAdapter.Description.Description;
		Factory = iDXGIAdapter.GetParent<IDXGIFactory2>();
		D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>(Vortice.Direct2D1.FactoryType.MultiThreaded);
		D2DDevice = D2DFactory.CreateDevice(iDXGIDevice);
		DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
		Log.Info("3D", $"device: {AdapterName}, feature level {featureLevel}, {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F1} ms");
	}

	public void Trim()
	{
		lock (ContextLock)
		{
			try
			{
				Context.ClearState();
				Context.Flush();
				using IDXGIDevice3 iDXGIDevice = Device.QueryInterfaceOrNull<IDXGIDevice3>();
				iDXGIDevice?.Trim();
			}
			catch (Exception ex)
			{
				Log.Warn("3D", "trim: " + ex.Message);
			}
		}
	}

	public void Dispose()
	{
		DWriteFactory.Dispose();
		D2DDevice.Dispose();
		D2DFactory.Dispose();
		Factory.Dispose();
		Context.Dispose();
		Device.Dispose();
	}

	public static byte[] CompileShader(string source, string entryPoint, string profile, string? defines = null)
	{
		string text = ((defines == null) ? source : (defines + "\n" + source));
		string text2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text + "|" + entryPoint + "|" + profile))).Substring(0, 24);
		string text3 = Path.Combine(CacheDirectory, entryPoint + "-" + text2 + ".cso");
		try
		{
			if (File.Exists(text3))
			{
				return File.ReadAllBytes(text3);
			}
		}
		catch
		{
		}
		long timestamp = Stopwatch.GetTimestamp();
		Blob blob;
		Blob errorBlob;
		Result result = Compiler.Compile(text, entryPoint, "clicky3d.hlsl", profile, out blob, out errorBlob);
		if (result.Failure || (object)blob == null)
		{
			string text4 = (((object)errorBlob == null) ? result.ToString() : Encoding.ASCII.GetString(errorBlob.AsSpan()));
			errorBlob?.Dispose();
			throw new InvalidOperationException("3D shader " + entryPoint + " failed to compile: " + text4);
		}
		errorBlob?.Dispose();
		byte[] array = blob.AsSpan().ToArray();
		blob.Dispose();
		try
		{
			Directory.CreateDirectory(CacheDirectory);
			string text5 = text3 + "." + Environment.ProcessId + ".tmp";
			File.WriteAllBytes(text5, array);
			File.Move(text5, text3, overwrite: true);
		}
		catch
		{
		}
		Log.Info("3D", $"compiled {entryPoint} ({profile}) in {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F1} ms");
		return array;
	}
}
