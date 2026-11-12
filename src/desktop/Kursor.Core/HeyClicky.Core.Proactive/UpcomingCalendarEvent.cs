using System;

namespace HeyClicky.Core.Proactive;

public sealed record UpcomingCalendarEvent(string Id, string Title, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool IsAllDay, Uri? MeetingUrl, string? Location, Uri? HtmlLink)
{
	public Uri? DirectionsUrl
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Location))
			{
				return new Uri("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(Location));
			}
			return null;
		}
	}

	public Uri? PrimaryActionUrl => MeetingUrl ?? DirectionsUrl;

	public bool IsKnownMeetingProvider
	{
		get
		{
			string text = MeetingUrl?.Host.ToLowerInvariant();
			if (text == null)
			{
				return false;
			}
			if (!(text == "meet.google.com") && !(text == "zoom.us") && !text.EndsWith(".zoom.us") && !(text == "zoom.com") && !text.EndsWith(".zoom.com") && !(text == "teams.microsoft.com"))
			{
				return text == "teams.live.com";
			}
			return true;
		}
	}

	public MeetingActionKind? PrimaryActionKind
	{
		get
		{
			if ((object)MeetingUrl == null)
			{
				if ((object)DirectionsUrl == null)
				{
					return null;
				}
				return MeetingActionKind.Directions;
			}
			return (!IsKnownMeetingProvider) ? MeetingActionKind.Open : MeetingActionKind.Join;
		}
	}

	public string AnnouncementKey => $"{Id}|{StartsAt.ToUnixTimeSeconds()}";

	public bool IsOngoing(DateTimeOffset now)
	{
		if (!IsAllDay && StartsAt <= now)
		{
			return (EndsAt ?? StartsAt.AddHours(1.0)) > now;
		}
		return false;
	}
}
