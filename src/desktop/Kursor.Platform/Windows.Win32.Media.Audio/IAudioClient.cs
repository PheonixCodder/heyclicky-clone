using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.Media.Audio;

[ComImport]
[Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IAudioClient
{
	unsafe void Initialize(AUDCLNT_SHAREMODE ShareMode, uint StreamFlags, long hnsBufferDuration, long hnsPeriodicity, WAVEFORMATEX* pFormat, [Optional] Guid* AudioSessionGuid);

	void GetBufferSize(out uint pNumBufferFrames);

	void GetStreamLatency(out long phnsLatency);

	void GetCurrentPadding(out uint pNumPaddingFrames);

	[PreserveSig]
	unsafe HRESULT IsFormatSupported(AUDCLNT_SHAREMODE ShareMode, WAVEFORMATEX* pFormat, [Optional] WAVEFORMATEX** ppClosestMatch);

	unsafe void GetMixFormat(WAVEFORMATEX** ppDeviceFormat);

	unsafe void GetDevicePeriod([Optional] long* phnsDefaultDevicePeriod, [Optional] long* phnsMinimumDevicePeriod);

	void Start();

	void Stop();

	void Reset();

	void SetEventHandle(HANDLE eventHandle);

	unsafe void GetService(Guid* riid, out void* ppv);
}
