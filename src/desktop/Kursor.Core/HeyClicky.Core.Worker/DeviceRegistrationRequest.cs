using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record DeviceRegistrationRequest(IReadOnlyList<string>? Capabilities, string? DisplayName, string? Model);
