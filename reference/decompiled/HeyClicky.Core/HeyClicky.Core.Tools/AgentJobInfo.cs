using System.Collections.Generic;

namespace HeyClicky.Core.Tools;

public sealed record AgentJobInfo(string? ThreadId, string Title, string State, string FolderPath, IReadOnlyList<AgentJobRunInfo> Runs, string LatestReply, string? Summary, IReadOnlyList<string> ArtifactEntries, AgentTaskInfo? Live);
