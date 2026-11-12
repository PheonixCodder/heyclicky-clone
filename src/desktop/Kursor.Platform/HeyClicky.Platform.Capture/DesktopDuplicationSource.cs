using System;
using SharpGen.Runtime;
using Vortice.DXGI;
using Vortice.Direct3D11;

namespace HeyClicky.Platform.Capture;

internal sealed class DesktopDuplicationSource : IDisposable
{
	public readonly string DeviceName;

	private readonly CaptureGpu gpu;

	private readonly IDXGIOutput1 output;

	private IDXGIOutputDuplication? duplication;

	private ID3D11Texture2D? lastFrameCopy;

	private bool hasLastFrame;

	public ModeRotation Rotation { get; private set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public bool IsOpen => (object)duplication != null;

	public DesktopDuplicationSource(CaptureGpu gpu, IDXGIOutput1 output, string deviceName)
	{
		this.gpu = gpu;
		this.output = output;
		DeviceName = deviceName;
	}

	public void EnsureOpen()
	{
		if ((object)duplication != null)
		{
			return;
		}
		using IDXGIOutput5 iDXGIOutput = output.QueryInterfaceOrNull<IDXGIOutput5>();
		if ((object)iDXGIOutput != null)
		{
			IDXGIOutputDuplication iDXGIOutputDuplication;
			int num = DuplicateOutput1(iDXGIOutput, gpu.Device, out iDXGIOutputDuplication);
			if (num >= 0)
			{
				duplication = iDXGIOutputDuplication;
			}
			else
			{
				Log.Warn("Capture", $"DuplicateOutput1 failed on {DeviceName} (0x{num:X8}); trying DuplicateOutput");
			}
		}
		if ((object)duplication == null)
		{
			duplication = output.DuplicateOutput(gpu.Device);
		}
		OutduplDescription description = duplication.Description;
		Rotation = description.Rotation;
		Width = (int)description.ModeDescription.Width;
		Height = (int)description.ModeDescription.Height;
		hasLastFrame = false;
	}

	public ID3D11Texture2D? AcquireCurrentFrame(uint firstFrameTimeoutMs)
	{
		long num = Environment.TickCount64 + firstFrameTimeoutMs;
		int num2 = 0;
		while (true)
		{
			try
			{
				EnsureOpen();
				uint timeoutInMilliseconds = (uint)((!hasLastFrame) ? Math.Max(0L, num - Environment.TickCount64) : 0u);
				OutduplFrameInfo frameInfo;
				IDXGIResource desktopResource;
				Result result = duplication.AcquireNextFrame(timeoutInMilliseconds, out frameInfo, out desktopResource);
				if (result.Code == Vortice.DXGI.ResultCode.WaitTimeout.Code)
				{
					return hasLastFrame ? lastFrameCopy : null;
				}
				if (result.Code == Vortice.DXGI.ResultCode.AccessLost.Code || result.Failure)
				{
					Log.Info("Capture", $"duplication of {DeviceName} lost (0x{result.Code:X8}); reopening");
					Close();
					if (++num2 > 1)
					{
						return null;
					}
					continue;
				}
				try
				{
					if (frameInfo.LastPresentTime == 0L)
					{
						if (hasLastFrame)
						{
							return lastFrameCopy;
						}
						if (Environment.TickCount64 >= num)
						{
							return null;
						}
						continue;
					}
					using ID3D11Texture2D iD3D11Texture2D = desktopResource.QueryInterface<ID3D11Texture2D>();
					Texture2DDescription description = iD3D11Texture2D.Description;
					lastFrameCopy = gpu.GetSourceCopyTexture((int)description.Width, (int)description.Height);
					gpu.Context.CopyResource(lastFrameCopy, iD3D11Texture2D);
					hasLastFrame = true;
					return lastFrameCopy;
				}
				finally
				{
					desktopResource?.Dispose();
					duplication.ReleaseFrame();
				}
			}
			catch (SharpGenException ex)
			{
				Log.Warn("Capture", $"desktop duplication of {DeviceName} failed (0x{ex.HResult:X8})");
				Close();
				if (++num2 > 1)
				{
					return null;
				}
			}
		}
	}

	private unsafe static int DuplicateOutput1(IDXGIOutput5 output, ID3D11Device device, out IDXGIOutputDuplication? duplication)
	{
		duplication = null;
		Format* ptr = stackalloc Format[1];
		*ptr = Format.B8G8R8A8_UNorm;
		nint num = 0;
		int num2 = ((delegate* unmanaged[Stdcall]<nint, nint, uint, uint, Format*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)output.NativePointer) + (nint)26 * (nint)sizeof(nint))))(output.NativePointer, device.NativePointer, 0u, 1u, ptr, &num);
		if (num2 >= 0 && num != 0)
		{
			duplication = new IDXGIOutputDuplication(num);
		}
		return num2;
	}

	public void Close()
	{
		duplication?.Dispose();
		duplication = null;
		hasLastFrame = false;
	}

	public void Dispose()
	{
		Close();
		output.Dispose();
	}
}
