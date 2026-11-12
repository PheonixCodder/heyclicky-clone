using System.Collections.Generic;

namespace HeyClicky.Agents.Backend;

public sealed record AgentIntegrationCatalog(bool ComposioConfigured, IReadOnlyList<AgentIntegration> Integrations);
