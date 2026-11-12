namespace HeyClicky.Platform.Input;

internal struct RawInputDevice
{
	public ushort UsagePage;

	public ushort Usage;

	public uint Flags;

	public nint Target;
}
