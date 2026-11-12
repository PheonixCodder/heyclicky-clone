using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveClickyDescriptor(string Slug, string Name, string Role, string Description, IReadOnlyList<(string Title, int EveryMinutes)> Routines, IReadOnlyList<string> ThreadIds);
