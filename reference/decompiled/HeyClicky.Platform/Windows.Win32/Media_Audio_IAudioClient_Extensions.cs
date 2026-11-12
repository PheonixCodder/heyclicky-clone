using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IAudioClient_Extensions
{
	internal unsafe static void Initialize(this IAudioClient @this, AUDCLNT_SHAREMODE ShareMode, uint StreamFlags, long hnsBufferDuration, long hnsPeriodicity, in WAVEFORMATEX pFormat, [Optional] Guid? AudioSessionGuid)
	{
		fixed (WAVEFORMATEX* pFormat2 = &pFormat)
		{
			Guid valueOrDefault = AudioSessionGuid.GetValueOrDefault();
			@this.Initialize(ShareMode, StreamFlags, hnsBufferDuration, hnsPeriodicity, pFormat2, AudioSessionGuid.HasValue ? (&valueOrDefault) : null);
		}
	}

	internal unsafe static HRESULT IsFormatSupported(this IAudioClient @this, AUDCLNT_SHAREMODE ShareMode, in WAVEFORMATEX pFormat, out WAVEFORMATEX* ppClosestMatch)
	{
		fixed (WAVEFORMATEX** ppClosestMatch2 = &ppClosestMatch)
		{
			fixed (WAVEFORMATEX* pFormat2 = &pFormat)
			{
				return @this.IsFormatSupported(ShareMode, pFormat2, ppClosestMatch2);
			}
		}
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static HRESULT IsFormatSupported(this IAudioClient @this, AUDCLNT_SHAREMODE ShareMode, in WAVEFORMATEX pFormat)
	{
		fixed (WAVEFORMATEX* pFormat2 = &pFormat)
		{
			return @this.IsFormatSupported(ShareMode, pFormat2, null);
		}
	}

	internal unsafe static void GetMixFormat(this IAudioClient @this, out WAVEFORMATEX* ppDeviceFormat)
	{
		fixed (WAVEFORMATEX** ppDeviceFormat2 = &ppDeviceFormat)
		{
			@this.GetMixFormat(ppDeviceFormat2);
		}
	}

	internal unsafe static void GetDevicePeriod(this IAudioClient @this, out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod)
	{
		fixed (long* phnsMinimumDevicePeriod2 = &phnsMinimumDevicePeriod)
		{
			fixed (long* phnsDefaultDevicePeriod2 = &phnsDefaultDevicePeriod)
			{
				@this.GetDevicePeriod(phnsDefaultDevicePeriod2, phnsMinimumDevicePeriod2);
			}
		}
	}

	internal unsafe static void GetDevicePeriod(this IAudioClient @this)
	{
		@this.GetDevicePeriod(null, null);
	}

	internal static void SetEventHandle(this IAudioClient @this, SafeHandle eventHandle)
	{
		bool success = false;
		try
		{
			if (eventHandle != null)
			{
				eventHandle.DangerousAddRef(ref success);
				HANDLE eventHandle2 = (HANDLE)eventHandle.DangerousGetHandle();
				@this.SetEventHandle(eventHandle2);
				return;
			}
			throw new ArgumentNullException("eventHandle");
		}
		finally
		{
			if (success)
			{
				eventHandle.DangerousRelease();
			}
		}
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static void GetService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this IAudioClient @this, out T ppv) where T : class
	{
		void* ppv2 = null;
		try
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			@this.GetService(&iID, out ppv2);
			ppv = (T)ComOrWinRTObjectMarshaller.ConvertToManaged((nint)ppv2);
		}
		finally
		{
			if (ppv2 != null)
			{
				Marshal.Release((nint)ppv2);
			}
		}
	}
}
