namespace HeyClicky.Core.Notch;

public static class NotchTabNames
{
	public static string RawValue(this NotchTab tab)
	{
		return tab switch
		{
			NotchTab.Home => "home", 
			NotchTab.Chat => "chat", 
			NotchTab.Settings => "settings", 
			_ => "document", 
		};
	}

	public static string Title(this NotchTab tab)
	{
		return tab switch
		{
			NotchTab.Home => "Home", 
			NotchTab.Chat => "Chat", 
			NotchTab.Settings => "Settings", 
			_ => "Document", 
		};
	}

	public static string RawValue(this NotchSettingsSection section)
	{
		if (section == NotchSettingsSection.Referral)
		{
			return "referral";
		}
		return section.ToString().ToLowerInvariant();
	}
}
