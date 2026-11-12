using System.Runtime.CompilerServices;

namespace HeyClicky;

public sealed record HotkeyBindings(HotkeyBinding PushToTalk, HotkeyBinding TextMode, HotkeyBinding Dictation, HotkeyBinding HandsFreeDictation)
{
	public static readonly HotkeyBindings Defaults = new HotkeyBindings(HotkeyBinding.PushToTalkDefault, HotkeyBinding.TextModeDefault, HotkeyBinding.DictationDefault, HotkeyBinding.HandsFreeDictationDefault);

	[CompilerGenerated]
	private HotkeyBindings(HotkeyBindings original)
	{
		PushToTalk = original.PushToTalk;
		TextMode = original.TextMode;
		Dictation = original.Dictation;
		HandsFreeDictation = original.HandsFreeDictation;
	}
}
