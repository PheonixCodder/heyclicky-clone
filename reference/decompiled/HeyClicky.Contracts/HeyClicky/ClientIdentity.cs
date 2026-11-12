using System.Collections.Generic;

namespace HeyClicky;

public sealed record ClientIdentity(string Platform, string DeviceId, string AppVersion, string AppBuild, string OsVersion, IReadOnlyList<string> Capabilities);
