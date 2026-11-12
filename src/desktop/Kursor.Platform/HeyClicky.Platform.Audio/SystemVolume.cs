using System;
using System.Runtime.InteropServices;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;

namespace HeyClicky.Platform.Audio;

public static class SystemVolume
{
	[ComImport]
	[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioEndpointVolume
	{
		int RegisterControlChangeNotify(nint notify);

		int UnregisterControlChangeNotify(nint notify);

		int GetChannelCount(out uint count);

		int SetMasterVolumeLevel(float levelDb, ref Guid context);

		int SetMasterVolumeLevelScalar(float level, ref Guid context);

		int GetMasterVolumeLevel(out float levelDb);

		int GetMasterVolumeLevelScalar(out float level);

		int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid context);

		int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);

		int GetChannelVolumeLevel(uint channel, out float levelDb);

		int GetChannelVolumeLevelScalar(uint channel, out float level);

		int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);

		int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
	}

	[ComImport]
	[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioMeterInformation
	{
		int GetPeakValue(out float peak);
	}

	private unsafe static T? Activate<T>(Guid iid) where T : class
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		IMMDevice iMMDevice = null;
		try
		{
			iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
			iMMDevice = CoreAudio.TryGetDefaultDevice(iMMDeviceEnumerator, EDataFlow.eRender, ERole.eMultimedia);
			if (iMMDevice == null)
			{
				return null;
			}
			iMMDevice.Activate(&iid, CLSCTX.CLSCTX_ALL, null, out var ppInterface);
			return ppInterface as T;
		}
		catch (Exception ex)
		{
			Log.Warn("volume", "activate: " + ex.Message);
			return null;
		}
		finally
		{
			CoreAudio.ReleaseComObject(iMMDevice);
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
		}
	}

	public static double EffectiveLevel()
	{
		IAudioEndpointVolume audioEndpointVolume = Activate<IAudioEndpointVolume>(new Guid("5CDF2C82-841E-4546-9722-0CF74078229A"));
		if (audioEndpointVolume == null)
		{
			return 1.0;
		}
		try
		{
			audioEndpointVolume.GetMute(out var mute);
			audioEndpointVolume.GetMasterVolumeLevelScalar(out var level);
			return mute ? 0f : level;
		}
		catch
		{
			return 1.0;
		}
		finally
		{
			CoreAudio.ReleaseComObject(audioEndpointVolume);
		}
	}

	public static bool SetLevel(double level)
	{
		IAudioEndpointVolume audioEndpointVolume = Activate<IAudioEndpointVolume>(new Guid("5CDF2C82-841E-4546-9722-0CF74078229A"));
		if (audioEndpointVolume == null)
		{
			return false;
		}
		try
		{
			Guid context = Guid.Empty;
			audioEndpointVolume.SetMute(mute: false, ref context);
			return audioEndpointVolume.SetMasterVolumeLevelScalar((float)Math.Clamp(level, 0.0, 1.0), ref context) == 0;
		}
		catch (Exception ex)
		{
			Log.Warn("volume", "set: " + ex.Message);
			return false;
		}
		finally
		{
			CoreAudio.ReleaseComObject(audioEndpointVolume);
		}
	}

	public static float Peak()
	{
		IAudioMeterInformation audioMeterInformation = Activate<IAudioMeterInformation>(new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"));
		if (audioMeterInformation == null)
		{
			return 0f;
		}
		try
		{
			audioMeterInformation.GetPeakValue(out var peak);
			return peak;
		}
		catch
		{
			return 0f;
		}
		finally
		{
			CoreAudio.ReleaseComObject(audioMeterInformation);
		}
	}
}
