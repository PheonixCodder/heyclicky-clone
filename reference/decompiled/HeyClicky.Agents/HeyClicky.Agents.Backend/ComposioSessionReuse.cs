using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Backend;

public static class ComposioSessionReuse
{
	public static bool IsStale(DateTimeOffset fetchedAt, DateTimeOffset now, TimeSpan lifetime)
	{
		TimeSpan timeSpan = now - fetchedAt;
		if (!(timeSpan < TimeSpan.Zero))
		{
			return timeSpan >= lifetime;
		}
		return true;
	}

	public static IReadOnlySet<string>? ConnectedToolkits(AgentIntegrationCatalog? catalog)
	{
		return (from t in catalog?.Integrations.Where((AgentIntegration i) => !i.IsLocalIntegration).SelectMany((AgentIntegration i) => i.Toolkits)
			where t.IsConnected
			select t.Slug.ToLowerInvariant()).ToHashSet();
	}

	public static bool ShouldRefetchForConnectedApps(IReadOnlySet<string>? knownToApp, IReadOnlySet<string> sessionToolkits, ref IReadOnlySet<string>? lastRefetchedFor)
	{
		if (knownToApp == null || knownToApp.SetEquals(sessionToolkits) || (lastRefetchedFor != null && knownToApp.SetEquals(lastRefetchedFor)))
		{
			return false;
		}
		lastRefetchedFor = knownToApp;
		return true;
	}
}
