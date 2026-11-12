using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record StarterPlanClicky(string Key, string Name, string Role, string Description, IReadOnlyList<string>? IntroMessages, string? RevealLine, IReadOnlyList<string>? FirstAsks, StarterPlanClickyRoutine? Routine = null);
