using System;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Drive;

public static class ClickyDriveAnalytics
{
	public static class Origin
	{
		public const string OfferCard = "offer_card";

		public const string OfferSpoken = "offer_spoken";

		public const string RecentYes = "recent_yes";

		public const string JobStepSpoken = "job_step_spoken";

		public const string JobStepApproved = "job_step_approved";

		public const string AppSetup = "app_setup";
	}

	public static void Track(string action, string? threadId = null, JsonObject? properties = null)
	{
		if (properties == null)
		{
			properties = new JsonObject((JsonNodeOptions?)null);
		}
		properties["action"] = action;
		ClickyAnalytics.TrackProductEvent("clicky_drive_action", null, threadId, properties);
	}

	public static string Billing(ClickyDriveBilling billing)
	{
		if (!(billing is ClickyDriveBilling.Job))
		{
			if (billing is ClickyDriveBilling.Setup)
			{
				return "setup";
			}
			return "user";
		}
		return "job";
	}

	public static string Outcome(AgentDriveEnding ending)
	{
		return ending switch
		{
			AgentDriveEnding.Finished => "finished", 
			AgentDriveEnding.UserTookOver => "user_took_over", 
			AgentDriveEnding.SafetyStop => "safety_check", 
			AgentDriveEnding.CouldNotSee => "could_not_see", 
			AgentDriveEnding.OutOfAgentMessages => "out_of_agent_messages", 
			_ => "failed", 
		};
	}

	public static JsonObject StartedProperties(string driveId, string origin, ClickyDriveBilling billing, bool isJobStep, bool hasThen)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["drive_id"] = driveId,
			["origin"] = origin,
			["billing"] = Billing(billing),
			["is_job_step"] = isJobStep,
			["has_then"] = hasThen
		};
	}

	public static JsonObject EndedProperties(string driveId, string origin, ClickyDriveBilling billing, AgentDriveEnding ending, double durationSeconds, int actionCount, int stepCount, bool brainCalledEnd, bool isJobStep, bool earlyRefundRequested, string? takenBackBy, int mousePauses = 0)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["drive_id"] = driveId,
			["origin"] = origin,
			["billing"] = Billing(billing),
			["outcome"] = Outcome(ending),
			["duration_seconds"] = (int)Math.Round(durationSeconds, MidpointRounding.AwayFromZero),
			["action_count"] = actionCount,
			["step_count"] = stepCount,
			["brain_called_end"] = brainCalledEnd,
			["is_job_step"] = isJobStep,
			["early_refund_requested"] = earlyRefundRequested,
			["mouse_pauses"] = mousePauses
		};
		if (ending == AgentDriveEnding.UserTookOver)
		{
			jsonObject["taken_back_by"] = takenBackBy ?? "unknown";
		}
		return jsonObject;
	}

	public static void TrackMacStepRequested(string? threadId, string requestKind, bool driveAvailable)
	{
		Track("mac_step_requested", threadId, new JsonObject((JsonNodeOptions?)null)
		{
			["request_kind"] = requestKind,
			["drive_available"] = driveAvailable
		});
	}

	public static void TrackMacStepAnswered(string? threadId, string decision, string path)
	{
		Track("mac_step_answered", threadId, new JsonObject((JsonNodeOptions?)null)
		{
			["decision"] = decision,
			["path"] = path
		});
	}

	public static void TrackMacStepResultDelivered(string? threadId, string via)
	{
		Track("mac_step_result_delivered", threadId, new JsonObject((JsonNodeOptions?)null) { ["delivered_via"] = via });
	}

	public static void TrackMacStepCallReturned(string threadId, string status, bool joinedWaitingCall = false)
	{
		Track("mac_step_call_returned", threadId, new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = status,
			["joined_waiting_call"] = joinedWaitingCall
		});
	}

	public static void TrackMacStepCallReturned(string threadId, MacStepAnswer answer, bool joinedWaitingCall)
	{
		TrackMacStepCallReturned(threadId, CallStatus(answer), joinedWaitingCall);
	}

	public static string CallStatus(MacStepAnswer answer)
	{
		if (!(answer is MacStepAnswer.Drove))
		{
			if (!(answer is MacStepAnswer.Declined))
			{
				if (!(answer is MacStepAnswer.NoAnswer))
				{
					if (!(answer is MacStepAnswer.NotYet))
					{
						if (answer is MacStepAnswer.BeingDriven)
						{
							return "in_progress";
						}
						return "could_not_start";
					}
					return "waiting";
				}
				return "no_answer";
			}
			return "declined";
		}
		return "done";
	}

	public static void TrackOnboardingDriveStarted(bool isRepeat)
	{
		ClickyAnalytics.TrackProductEvent("onboarding_drive_step_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "started",
			["is_repeat"] = isRepeat
		});
	}

	public static void TrackOnboardingDriveEnded(string outcome, double durationSeconds)
	{
		ClickyAnalytics.TrackProductEvent("onboarding_drive_step_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "ended",
			["outcome"] = outcome,
			["duration_seconds"] = (int)Math.Round(durationSeconds, MidpointRounding.AwayFromZero)
		});
	}
}
