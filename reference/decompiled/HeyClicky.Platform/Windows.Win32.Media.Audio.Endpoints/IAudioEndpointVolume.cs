using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.Media.Audio.Endpoints;

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.0.6000")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IAudioEndpointVolume
{
	void RegisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);

	void UnregisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);

	void GetChannelCount(out uint pnChannelCount);

	unsafe void SetMasterVolumeLevel(float fLevelDB, Guid* pguidEventContext);

	unsafe void SetMasterVolumeLevelScalar(float fLevel, Guid* pguidEventContext);

	void GetMasterVolumeLevel(out float pfLevelDB);

	void GetMasterVolumeLevelScalar(out float pfLevel);

	unsafe void SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid* pguidEventContext);

	unsafe void SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid* pguidEventContext);

	void GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

	void GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

	unsafe void SetMute(BOOL bMute, Guid* pguidEventContext);

	unsafe void GetMute(BOOL* pbMute);

	void GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

	unsafe void VolumeStepUp(Guid* pguidEventContext);

	unsafe void VolumeStepDown(Guid* pguidEventContext);

	void QueryHardwareSupport(out uint pdwHardwareSupportMask);

	void GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}
