using System.Collections.Generic;

namespace HeyClicky.Agents.Harness;

public sealed record AgentHarnessLocalMcpCommand(string Command, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Environment);
