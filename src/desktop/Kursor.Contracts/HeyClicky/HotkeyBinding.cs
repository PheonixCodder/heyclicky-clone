using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HeyClicky;

public sealed record HotkeyBinding(HotkeyModifiers Modifiers, HotkeyTrigger Trigger = HotkeyTrigger.Hold)
{
	[JsonIgnore]
	public IReadOnlyList<string> KeyNames
	{
		get
		{
			List<string> list = new List<string>(4);
			if (Modifiers.HasFlag(HotkeyModifiers.Control))
			{
				list.Add("Ctrl");
			}
			if (Modifiers.HasFlag(HotkeyModifiers.Alt))
			{
				list.Add("Alt");
			}
			if (Modifiers.HasFlag(HotkeyModifiers.Shift))
			{
				list.Add("Shift");
			}
			if (Modifiers.HasFlag(HotkeyModifiers.Win))
			{
				list.Add("Win");
			}
			return list;
		}
	}

	[JsonIgnore]
	public string DisplayText => string.Join(" + ", KeyNames) + ((Trigger == HotkeyTrigger.DoubleTap) ? ", pressed twice" : "");

	public static readonly HotkeyBinding PushToTalkDefault = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Alt);

	public static readonly HotkeyBinding TextModeDefault = new HotkeyBinding(HotkeyModifiers.Control, HotkeyTrigger.DoubleTap);

	public static readonly HotkeyBinding DictationDefault = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Win);

	public static readonly HotkeyBinding HandsFreeDictationDefault = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Win, HotkeyTrigger.DoubleTap);

	[CompilerGenerated]
	private HotkeyBinding(HotkeyBinding original)
	{
		Modifiers = original.Modifiers;
		Trigger = original.Trigger;
	}
}
