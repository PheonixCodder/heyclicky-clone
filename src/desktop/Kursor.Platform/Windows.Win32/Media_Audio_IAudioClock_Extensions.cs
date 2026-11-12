using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using Windows.Win32.Media.Audio;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class Media_Audio_IAudioClock_Extensions
{
	internal unsafe static void GetPosition(this IAudioClock @this, out ulong pu64Position, out ulong pu64QPCPosition)
	{
		fixed (ulong* pu64QPCPosition2 = &pu64QPCPosition)
		{
			@this.GetPosition(out pu64Position, pu64QPCPosition2);
		}
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static void GetPosition(this IAudioClock @this, out ulong pu64Position)
	{
		@this.GetPosition(out pu64Position, null);
	}
}
