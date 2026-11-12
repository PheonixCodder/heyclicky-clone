using System;

namespace HeyClicky.App.Ink;

public sealed record ClickyInkClickyLive(ClickyInkClickyPhase Phase, DateTimeOffset? FinishedAt = null);
