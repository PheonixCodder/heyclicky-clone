using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public static class MeetingCountdownRules
{
	public enum Change
	{
		None,
		Cleared,
		Updated,
		Presented
	}

	public static readonly TimeSpan PresentWindow = TimeSpan.FromSeconds(75L);

	public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15L);

	public static readonly TimeSpan RefreshStaleness = TimeSpan.FromMinutes(5L);

	public static readonly TimeSpan NotConnectedRecheck = TimeSpan.FromMinutes(30L);

	public static readonly TimeSpan NotchExpandStaleness = TimeSpan.FromSeconds(30L);

	public static readonly TimeSpan FailedFetchBackoff = TimeSpan.FromSeconds(60L);

	public static (UpcomingCalendarEvent? Active, Change Change) Step(UpcomingCalendarEvent? active, IReadOnlyList<UpcomingCalendarEvent> schedule, ISet<string> announced, DateTimeOffset now, bool canPresent)
	{
		Change item = Change.None;
		if ((object)active != null && now > active.StartsAt && !active.IsOngoing(now))
		{
			active = null;
			item = Change.Cleared;
		}
		if ((object)active != null)
		{
			UpcomingCalendarEvent upcomingCalendarEvent = schedule.FirstOrDefault((UpcomingCalendarEvent e) => e.AnnouncementKey == active.AnnouncementKey);
			if ((object)upcomingCalendarEvent == null)
			{
				active = null;
				item = Change.Cleared;
			}
			else if (upcomingCalendarEvent != active)
			{
				active = upcomingCalendarEvent;
				item = Change.Updated;
			}
		}
		if ((object)active != null || !canPresent)
		{
			return (Active: active, Change: item);
		}
		UpcomingCalendarEvent upcomingCalendarEvent2 = schedule.FirstOrDefault(delegate(UpcomingCalendarEvent e)
		{
			if (e.IsAllDay || announced.Contains(e.AnnouncementKey))
			{
				return false;
			}
			TimeSpan timeSpan = e.StartsAt - now;
			return timeSpan <= PresentWindow && (timeSpan > TimeSpan.Zero || e.IsOngoing(now));
		});
		if ((object)upcomingCalendarEvent2 == null)
		{
			return (Active: null, Change: item);
		}
		announced.Add(upcomingCalendarEvent2.AnnouncementKey);
		return (Active: upcomingCalendarEvent2, Change: Change.Presented);
	}

	public static bool ShouldRefresh(DateTimeOffset now, DateTimeOffset? lastRefresh, DateTimeOffset? lastFailure, bool backendConnected, bool triggeredByNotchExpand)
	{
		TimeSpan timeSpan = (triggeredByNotchExpand ? NotchExpandStaleness : ((lastRefresh.HasValue && !backendConnected) ? NotConnectedRecheck : RefreshStaleness));
		if (lastRefresh.HasValue)
		{
			DateTimeOffset valueOrDefault = lastRefresh.GetValueOrDefault();
			if (now - valueOrDefault < timeSpan)
			{
				return false;
			}
		}
		if (lastFailure.HasValue)
		{
			DateTimeOffset valueOrDefault2 = lastFailure.GetValueOrDefault();
			if (now - valueOrDefault2 < FailedFetchBackoff)
			{
				return false;
			}
		}
		return true;
	}
}
