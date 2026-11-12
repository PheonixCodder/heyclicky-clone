using System.Collections.Generic;

namespace HeyClicky.Agents.Runtime;

public sealed record CustomConnectorRegistration(string ServerKey, string DisplayName, string CapabilityDescription, CustomConnectorTransport Transport, IReadOnlyDictionary<string, string> Environment);
