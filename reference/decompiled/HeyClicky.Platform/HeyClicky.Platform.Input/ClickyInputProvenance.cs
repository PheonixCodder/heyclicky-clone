namespace HeyClicky.Platform.Input;

public static class ClickyInputProvenance
{
	public static nuint Tag => 1212369995u;

	public static bool IsClickySynthetic(nuint extraInfo)
	{
		return extraInfo == Tag;
	}
}
