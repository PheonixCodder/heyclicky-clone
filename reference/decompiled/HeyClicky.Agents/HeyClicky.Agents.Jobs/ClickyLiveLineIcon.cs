namespace HeyClicky.Agents.Jobs;

public sealed record ClickyLiveLineIcon(string? Host = null, string? Toolkit = null, string? Name = null, string? LogoUrl = null)
{
	public static ClickyLiveLineIcon Site(string host)
	{
		return new ClickyLiveLineIcon(host);
	}

	public static ClickyLiveLineIcon App(string toolkit, string name, string? logoUrl)
	{
		return new ClickyLiveLineIcon(null, toolkit, name, logoUrl);
	}
}
