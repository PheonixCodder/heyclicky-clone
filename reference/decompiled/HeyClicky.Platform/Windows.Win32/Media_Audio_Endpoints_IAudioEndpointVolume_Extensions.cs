using System;
using System.CodeDom.Compiler;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio.Endpoints;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_Endpoints_IAudioEndpointVolume_Extensions
{
	internal unsafe static void SetMasterVolumeLevel(this IAudioEndpointVolume @this, float fLevelDB, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.SetMasterVolumeLevel(fLevelDB, pguidEventContext2);
		}
	}

	internal unsafe static void SetMasterVolumeLevelScalar(this IAudioEndpointVolume @this, float fLevel, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.SetMasterVolumeLevelScalar(fLevel, pguidEventContext2);
		}
	}

	internal unsafe static void SetChannelVolumeLevel(this IAudioEndpointVolume @this, uint nChannel, float fLevelDB, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.SetChannelVolumeLevel(nChannel, fLevelDB, pguidEventContext2);
		}
	}

	internal unsafe static void SetChannelVolumeLevelScalar(this IAudioEndpointVolume @this, uint nChannel, float fLevel, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.SetChannelVolumeLevelScalar(nChannel, fLevel, pguidEventContext2);
		}
	}

	internal unsafe static void SetMute(this IAudioEndpointVolume @this, BOOL bMute, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.SetMute(bMute, pguidEventContext2);
		}
	}

	internal unsafe static void GetMute(this IAudioEndpointVolume @this, out BOOL pbMute)
	{
		fixed (BOOL* pbMute2 = &pbMute)
		{
			@this.GetMute(pbMute2);
		}
	}

	internal unsafe static void VolumeStepUp(this IAudioEndpointVolume @this, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.VolumeStepUp(pguidEventContext2);
		}
	}

	internal unsafe static void VolumeStepDown(this IAudioEndpointVolume @this, in Guid pguidEventContext)
	{
		fixed (Guid* pguidEventContext2 = &pguidEventContext)
		{
			@this.VolumeStepDown(pguidEventContext2);
		}
	}
}
