using System;
using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.Media.Audio;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct AUDIO_VOLUME_NOTIFICATION_DATA
{
	internal Guid guidEventContext;

	internal BOOL bMuted;

	internal float fMasterVolume;

	internal uint nChannels;

	internal VariableLengthInlineArray<float> afChannelVolumes;

	internal unsafe static int SizeOf(int count)
	{
		int num = sizeof(AUDIO_VOLUME_NOTIFICATION_DATA);
		if (count > 1)
		{
			num += checked((count - 1) * 4);
		}
		else if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		return num;
	}
}
