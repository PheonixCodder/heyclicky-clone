using System;
using HeyClicky.App.Billing;
using HeyClicky.Core.Worker;

namespace HeyClicky.App.Settings;

public static class PlanFixtures
{
	public static readonly string[] Names = new string[6] { "Free", "Free at cap", "Pro", "Pro past due", "Pro cancelling", "Max" };

	public static string? Current { get; private set; }

	public static PlanSnapshot Make(string name)
	{
		string windowResetsAt = DateTimeOffset.UtcNow.AddDays(12.0).AddHours(3.0).ToString("O");
		string currentPeriodEnd = DateTimeOffset.UtcNow.AddDays(20.0).ToString("O");
		switch (name)
		{
		case "Free":
			return new PlanSnapshot("free", new PlanUsage(new PlanUsageBucket(7, 25), new PlanUsageBucket(3, 10), null, windowResetsAt), "on", "free", null, null, null, null, null, null, null, null, null);
		case "Free at cap":
			return new PlanSnapshot("free", new PlanUsage(new PlanUsageBucket(25, 25), new PlanUsageBucket(10, 10), null, windowResetsAt), "on", "free", null, null, null, null, null, null, null, null, null);
		case "Pro past due":
			return new PlanSnapshot("unlimited", new PlanUsage(new PlanUsageBucket(40, 0), new PlanUsageBucket(20, 150), null, windowResetsAt), "on", "pro", null, null, new PlanSubscription("past_due", CancelAtPeriodEnd: false, null, null, null, null, "month"), null, null, null, null, null, null);
		case "Pro cancelling":
		{
			PlanUsage usage3 = new PlanUsage(new PlanUsageBucket(40, 0), new PlanUsageBucket(20, 150), null, windowResetsAt);
			PlanSubscription subscription3 = new PlanSubscription("active", CancelAtPeriodEnd: true, null, currentPeriodEnd, null, null, "month");
			bool? proYearlyAvailable = true;
			return new PlanSnapshot("unlimited", usage3, "on", "pro", null, null, subscription3, null, null, null, proYearlyAvailable, null, null);
		}
		case "Max":
		{
			PlanUsage usage2 = new PlanUsage(new PlanUsageBucket(40, 0), new PlanUsageBucket(120, 1000), null, windowResetsAt);
			PlanSubscription subscription2 = new PlanSubscription("active", CancelAtPeriodEnd: false, null, null, null, null, "month");
			bool? proYearlyAvailable = true;
			return new PlanSnapshot("unlimited", usage2, "on", "max", null, null, subscription2, null, null, null, null, proYearlyAvailable, null);
		}
		default:
		{
			PlanUsage usage = new PlanUsage(new PlanUsageBucket(40, 0), new PlanUsageBucket(20, 150), null, windowResetsAt);
			PlanSubscription subscription = new PlanSubscription("active", CancelAtPeriodEnd: false, null, null, null, null, "month");
			bool? proYearlyAvailable = true;
			return new PlanSnapshot("unlimited", usage, "on", "pro", null, null, subscription, null, null, null, proYearlyAvailable, null, null);
		}
		}
	}

	public static void Apply(BillingUi? billing, string? name)
	{
		Current = name;
		Log.Info("plan", "force plan state: " + (name ?? "live"));
		billing?.RefreshAsync();
	}
}
