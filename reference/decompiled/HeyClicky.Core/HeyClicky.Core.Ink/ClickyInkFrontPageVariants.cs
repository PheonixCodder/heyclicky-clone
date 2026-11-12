namespace HeyClicky.Core.Ink;

public static class ClickyInkFrontPageVariants
{
	public static string RawValue(this ClickyInkFrontPageVariant variant)
	{
		if (variant != 0)
		{
			return "missionControl";
		}
		return "broadsheet";
	}
}
