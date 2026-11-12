using System;

namespace HeyClicky.App.Settings;

public static class HotkeyRules
{
	public static string Title(ShortcutRecorder.Slot slot)
	{
		return slot switch
		{
			ShortcutRecorder.Slot.PushToTalk => "Push to talk", 
			ShortcutRecorder.Slot.TextMode => "Text mode", 
			ShortcutRecorder.Slot.Dictation => "Dictate", 
			_ => "Hands-free dictation", 
		};
	}

	public static HotkeyBinding Get(HotkeyBindings b, ShortcutRecorder.Slot slot)
	{
		return slot switch
		{
			ShortcutRecorder.Slot.PushToTalk => b.PushToTalk, 
			ShortcutRecorder.Slot.TextMode => b.TextMode, 
			ShortcutRecorder.Slot.Dictation => b.Dictation, 
			_ => b.HandsFreeDictation, 
		};
	}

	public static HotkeyBindings With(HotkeyBindings b, ShortcutRecorder.Slot slot, HotkeyBinding binding)
	{
		return slot switch
		{
			ShortcutRecorder.Slot.PushToTalk => b with
			{
				PushToTalk = binding with
				{
					Trigger = HotkeyTrigger.Hold
				}
			}, 
			ShortcutRecorder.Slot.TextMode => b with
			{
				TextMode = binding
			}, 
			ShortcutRecorder.Slot.Dictation => b with
			{
				Dictation = binding with
				{
					Trigger = HotkeyTrigger.Hold
				}
			}, 
			_ => b with
			{
				HandsFreeDictation = binding
			}, 
		};
	}

	public static ShortcutRecorder.Slot? Conflict(HotkeyBindings b, ShortcutRecorder.Slot slot, HotkeyBinding binding)
	{
		ShortcutRecorder.Slot[] values = Enum.GetValues<ShortcutRecorder.Slot>();
		foreach (ShortcutRecorder.Slot slot2 in values)
		{
			if (slot2 != slot)
			{
				HotkeyBinding hotkeyBinding = Get(b, slot2);
				if (hotkeyBinding.Modifiers == binding.Modifiers && (slot != ShortcutRecorder.Slot.HandsFreeDictation || slot2 != ShortcutRecorder.Slot.Dictation || binding.Trigger != HotkeyTrigger.DoubleTap) && (slot != ShortcutRecorder.Slot.Dictation || slot2 != ShortcutRecorder.Slot.HandsFreeDictation || hotkeyBinding.Trigger != HotkeyTrigger.DoubleTap))
				{
					return slot2;
				}
			}
		}
		return null;
	}
}
