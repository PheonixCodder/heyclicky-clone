using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record DevicesResponse(IReadOnlyList<RegisteredDevice>? Devices);
