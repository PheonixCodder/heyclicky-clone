using System;

namespace HeyClicky.Core.Ink;

public sealed record ClickyHomeInkNote(ClickyInkLiveNote Note, DateTimeOffset StartedAt);
