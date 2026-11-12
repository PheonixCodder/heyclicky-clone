using System;
using HeyClicky.Core.Drive;

namespace HeyClicky.App.Drive;

public sealed record AgentDriveBinding(string ClickyName, string? JobLeaseId, Action<AgentDriveOutcome> OnEnded, string? ThreadId = null);
