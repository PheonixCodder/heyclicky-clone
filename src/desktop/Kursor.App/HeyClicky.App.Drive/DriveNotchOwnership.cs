using System;

namespace HeyClicky.App.Drive;

public static class DriveNotchOwnership
{
	private static bool consentShown;

	private static bool drivingShown;

	public static Action<bool>? OwnsNotchChanged { get; set; }

	public static bool OwnsNotch
	{
		get
		{
			if (!consentShown)
			{
				return drivingShown;
			}
			return true;
		}
	}

	public static void SetConsentShown(bool shown)
	{
		Set(ref consentShown, shown);
	}

	public static void SetDrivingShown(bool shown)
	{
		Set(ref drivingShown, shown);
	}

	private static void Set(ref bool field, bool value)
	{
		bool ownsNotch = OwnsNotch;
		field = value;
		if (OwnsNotch != ownsNotch)
		{
			OwnsNotchChanged?.Invoke(OwnsNotch);
		}
	}
}
