namespace HeyClicky.Core.Voice;

public abstract record RealtimeProviderUnavailableReason
{
	public sealed record RegionUnsupported(string ProviderName, string? LocationName) : RealtimeProviderUnavailableReason();

	public sealed record NetworkBlocksProvider(string ProviderName) : RealtimeProviderUnavailableReason();

	public string UserFacingMessage
	{
		get
		{
			if (!(this is RegionUnsupported regionUnsupported))
			{
				if (this is NetworkBlocksProvider networkBlocksProvider)
				{
					return "I can't reach " + networkBlocksProvider.ProviderName + " from this network. A firewall, proxy or DNS filter seems to be blocking it, so I can't answer until that changes.";
				}
				return "";
			}
			return $"HeyClicky's voice runs on {regionUnsupported.ProviderName}, and {regionUnsupported.ProviderName} isn't available in {regionUnsupported.LocationName ?? "your region"}. That's why I can't answer here.";
		}
	}

	public string AnalyticsKey
	{
		get
		{
			if (!(this is RegionUnsupported))
			{
				return "provider_blocked_by_network";
			}
			return "provider_region_unsupported";
		}
	}
}
