using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record RegisteredDevice(string? Id, bool IsThisDevice, string? Platform, string? DisplayName, string? Model, string? OsVersion, string? AppVersion, string? AppBuild, IReadOnlyList<string>? Capabilities, string? FirstSeenAt, string? LastSeenAt, string? LastActiveAt, int? MessagesLast14Days);
