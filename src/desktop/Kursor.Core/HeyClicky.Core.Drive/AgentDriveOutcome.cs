using System.Collections.Generic;

namespace HeyClicky.Core.Drive;

public sealed record AgentDriveOutcome(AgentDriveEnding Ending, string? Summary, IReadOnlyList<string> Actions);
