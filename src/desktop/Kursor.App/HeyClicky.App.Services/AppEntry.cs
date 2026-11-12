namespace HeyClicky.App.Services;

public sealed record AppEntry(string Id, string DisplayName, string? ToolkitSlug, string? LogoUrl, string Status, string Description, bool IsLocal = false)
{
	public bool IsConnected
	{
		get
		{
			string status = Status;
			if (status == "connected" || status == "partial")
			{
				return true;
			}
			return false;
		}
	}
}
