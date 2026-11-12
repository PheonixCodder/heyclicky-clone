using System;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveHistoryEntry(string Title, string? Description, ProactiveResolution Resolution, DateTimeOffset DecidedAt, string? DenyReason = null, bool? WasViewed = null)
{
	public string WireStatus => Resolution switch
	{
		ProactiveResolution.Approved => "approved", 
		ProactiveResolution.Denied => "denied", 
		_ => (WasViewed == true) ? "viewed_no_action" : ((Resolution == ProactiveResolution.Expired) ? "expired" : "replaced"), 
	};
}
