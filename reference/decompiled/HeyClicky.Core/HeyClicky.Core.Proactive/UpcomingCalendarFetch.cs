using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record UpcomingCalendarFetch(bool IsCalendarConnected, IReadOnlyList<UpcomingCalendarEvent> Events);
