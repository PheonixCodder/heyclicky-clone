using System;
using System.Runtime.InteropServices;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;

namespace HeyClicky.Platform.Audio;

internal static class CoreAudio
{
	public static readonly Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

	public static readonly Guid IID_IAudioCaptureClient = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");

	public static readonly Guid IID_IAudioRenderClient = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");

	public static readonly Guid IID_IAudioClock = new Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7");

	private static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");

	public const uint AUDCLNT_BUFFERFLAGS_SILENT = 2u;

	public const int AUDCLNT_E_DEVICE_INVALIDATED = -2004287484;

	public const int AUDCLNT_E_SERVICE_NOT_RUNNING = -2004287472;

	public const int AUDCLNT_E_RESOURCES_INVALIDATED = -2004287450;

	public const int E_NOTFOUND = -2147023728;

	public const uint SharedEventDrivenConvertingFlags = 2281963520u;

	public static IMMDeviceEnumerator CreateDeviceEnumerator()
	{
		return (IMMDeviceEnumerator)new MMDeviceEnumerator();
	}

	public static IMMDevice? TryGetDefaultDevice(IMMDeviceEnumerator enumerator, EDataFlow flow, ERole role)
	{
		try
		{
			enumerator.GetDefaultAudioEndpoint(flow, role, out var ppEndpoint);
			return ppEndpoint;
		}
		catch (COMException ex) when (ex.HResult == -2147023728)
		{
			return null;
		}
	}

	public unsafe static IAudioClient ActivateAudioClient(IMMDevice device)
	{
		Guid iID_IAudioClient = IID_IAudioClient;
		device.Activate(&iID_IAudioClient, CLSCTX.CLSCTX_ALL, null, out var ppInterface);
		return (IAudioClient)ppInterface;
	}

	public unsafe static T GetService<T>(IAudioClient client, Guid iid) where T : class
	{
		client.GetService(&iid, out var ppv);
		try
		{
			return (T)Marshal.GetObjectForIUnknown((nint)ppv);
		}
		finally
		{
			Marshal.Release((nint)ppv);
		}
	}

	public static WAVEFORMATEX Pcm16(int sampleRate, int channels)
	{
		WAVEFORMATEX result = default(WAVEFORMATEX);
		result.wFormatTag = 1;
		result.nChannels = (ushort)channels;
		result.nSamplesPerSec = (uint)sampleRate;
		result.wBitsPerSample = 16;
		result.nBlockAlign = (ushort)(channels * 2);
		result.nAvgBytesPerSec = (uint)(sampleRate * channels * 2);
		result.cbSize = 0;
		return result;
	}

	public static WAVEFORMATEXTENSIBLE Float32(int sampleRate, int channels)
	{
		WAVEFORMATEXTENSIBLE result = default(WAVEFORMATEXTENSIBLE);
		result.Format = new WAVEFORMATEX
		{
			wFormatTag = 65534,
			nChannels = (ushort)channels,
			nSamplesPerSec = (uint)sampleRate,
			wBitsPerSample = 32,
			nBlockAlign = (ushort)(channels * 4),
			nAvgBytesPerSec = (uint)(sampleRate * channels * 4),
			cbSize = 22
		};
		result.Samples = new WAVEFORMATEXTENSIBLE._Samples_e__Union
		{
			wValidBitsPerSample = 32
		};
		result.dwChannelMask = ((channels == 2) ? 3u : 4u);
		result.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
		return result;
	}

	public static bool IsDeviceGone(Exception ex)
	{
		if (ex is COMException ex2)
		{
			if (ex2.HResult != -2004287484 && ex2.HResult != -2004287472 && ex2.HResult != -2004287450)
			{
				return ex2.HResult == -2147023728;
			}
			return true;
		}
		return false;
	}

	public static void ReleaseComObject(object? comObject)
	{
		if (comObject != null && Marshal.IsComObject(comObject))
		{
			Marshal.ReleaseComObject(comObject);
		}
	}
}
