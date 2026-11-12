using System;
using System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.Audio.Endpoints;
using Windows.Win32.System.Com;

namespace HeyClicky.Platform.Audio;

public static class SystemAudioOutputVolume
{
	private sealed class Endpoint(IMMDeviceEnumerator enumerator, IMMDevice device, IAudioEndpointVolume volume) : IDisposable
	{
		public IAudioEndpointVolume Volume => volume;

		public void Dispose()
		{
			CoreAudio.ReleaseComObject(volume);
			CoreAudio.ReleaseComObject(device);
			CoreAudio.ReleaseComObject(enumerator);
		}
	}

	private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

	public static string? CurrentSilenceKind()
	{
		return OnMta(delegate
		{
			using Endpoint endpoint = Open();
			if (endpoint == null)
			{
				return (string)null;
			}
			var (flag, num) = Read(endpoint.Volume);
			if (flag)
			{
				return "muted";
			}
			return (num <= 0.0005f) ? "zero_volume" : null;
		}, null);
	}

	public unsafe static bool RestoreAudibleOutput(float minimumScalar = 0.5f)
	{
		return OnMta(delegate
		{
			using Endpoint endpoint = Open();
			if (endpoint == null)
			{
				return false;
			}
			bool flag = false;
			bool item = Read(endpoint.Volume).Muted;
			if (item)
			{
				endpoint.Volume.SetMute(false, null);
				flag = true;
			}
			float item2 = Read(endpoint.Volume).Level;
			if (item2 <= 0.0005f)
			{
				endpoint.Volume.SetMasterVolumeLevelScalar(Math.Clamp(minimumScalar, 0f, 1f), null);
				flag = true;
			}
			Log.Info("Audio", $"restore audible output: was muted={item} level={item2:0.00}, changed={flag}");
			return flag;
		}, fallback: false);
	}

	private unsafe static (bool Muted, float Level) Read(IAudioEndpointVolume volume)
	{
		BOOL bOOL = default(BOOL);
		volume.GetMute(&bOOL);
		volume.GetMasterVolumeLevelScalar(out var pfLevel);
		return (Muted: bOOL, Level: pfLevel);
	}

	public unsafe static bool SetMuted(bool muted)
	{
		return OnMta(delegate
		{
			using Endpoint endpoint = Open();
			if (endpoint == null)
			{
				return false;
			}
			endpoint.Volume.SetMute(muted, null);
			return true;
		}, fallback: false);
	}

	private unsafe static Endpoint? Open()
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
		IMMDevice iMMDevice = CoreAudio.TryGetDefaultDevice(iMMDeviceEnumerator, EDataFlow.eRender, ERole.eMultimedia);
		if (iMMDevice == null)
		{
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
			return null;
		}
		Guid iID_IAudioEndpointVolume = IID_IAudioEndpointVolume;
		iMMDevice.Activate(&iID_IAudioEndpointVolume, CLSCTX.CLSCTX_ALL, null, out var ppInterface);
		return new Endpoint(iMMDeviceEnumerator, iMMDevice, (IAudioEndpointVolume)ppInterface);
	}

	private static T OnMta<T>(Func<T> work, T fallback)
	{
		T result = fallback;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				result = work();
			}
			catch (Exception ex)
			{
				Log.Warn("Audio", "output volume: " + ex.Message);
				result = fallback;
			}
		});
		thread.IsBackground = true;
		thread.Name = "HeyClicky output volume";
		thread.SetApartmentState(ApartmentState.MTA);
		thread.Start();
		if (!thread.Join(TimeSpan.FromSeconds(1L)))
		{
			return fallback;
		}
		return result;
	}
}
