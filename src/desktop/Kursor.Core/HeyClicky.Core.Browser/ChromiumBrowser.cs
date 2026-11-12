using System.Collections.Generic;

namespace HeyClicky.Core.Browser;

public sealed record ChromiumBrowser(string Name, string? RegistryPath, string UserDataFolder, IReadOnlyList<string> Executables, string? AppPathsName = null, string? PackageFamilyPrefix = null, string? ProgIdPrefix = null);
