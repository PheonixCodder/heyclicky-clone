namespace HeyClicky.Platform.Input;

internal readonly struct LowLevelKeyEvent
{
	public readonly uint VirtualKey;

	public readonly uint ScanCode;

	public readonly uint Flags;

	public readonly uint TimeMs;

	public readonly bool IsDown;

	public readonly nuint ExtraInfo;

	public bool IsInjected => (Flags & 0x10) != 0;

	public bool IsExtended => (Flags & 1) != 0;

	public LowLevelKeyEvent(uint virtualKey, uint scanCode, uint flags, uint timeMs, bool isDown, nuint extraInfo)
	{
		VirtualKey = virtualKey;
		ScanCode = scanCode;
		Flags = flags;
		TimeMs = timeMs;
		IsDown = isDown;
		ExtraInfo = extraInfo;
	}
}
