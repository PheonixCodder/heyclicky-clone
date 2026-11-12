using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record StarterPlanClickyInput(string Slug, string Name, string Role, string Description, IReadOnlyList<StarterPlanRoutine> Routines, IReadOnlyList<string> ThreadIds);
