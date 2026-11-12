using System;
using System.Text.Json.Nodes;

namespace HeyClicky.App.Dock;

public static class ArcDockAnalytics
{
	public const double MinimumTrackedOpenSeconds = 3.0;

	public static string StateName(ArcCardState state)
	{
		return state switch
		{
			ArcCardState.Queued => "queued", 
			ArcCardState.Working => "working", 
			ArcCardState.NeedsYou => "needs_you", 
			ArcCardState.Failed => "failed", 
			ArcCardState.Stopped => "stopped", 
			ArcCardState.New => "new", 
			ArcCardState.Done => "done", 
			ArcCardState.Idle => "idle", 
			ArcCardState.Pinning => "pinning", 
			_ => "suggested", 
		};
	}

	public static JsonObject WindowProperties(ArcCardState state, bool isFinalizing, bool isJob)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["window_kind"] = ((state == ArcCardState.Suggested) ? "suggested" : (isJob ? "job" : "clicky")),
			["state"] = (isFinalizing ? "finalizing" : StateName(state))
		};
	}

	public static string RequestKind(CardFooterRequest.Kinds kind)
	{
		return kind switch
		{
			CardFooterRequest.Kinds.ComputerUse => "computer_use", 
			CardFooterRequest.Kinds.ExtraUsage => "extra_usage", 
			CardFooterRequest.Kinds.UserStep => "user_step", 
			CardFooterRequest.Kinds.ConnectChrome => "connect_chrome", 
			CardFooterRequest.Kinds.Failed => "failed", 
			CardFooterRequest.Kinds.Stopped => "stopped", 
			_ => "ready", 
		};
	}

	public static string OutputKind(CardReadyOutput.Kinds kind)
	{
		return kind switch
		{
			CardReadyOutput.Kinds.File => "file", 
			CardReadyOutput.Kinds.Link => "link", 
			CardReadyOutput.Kinds.Several => "several", 
			_ => "nothing", 
		};
	}

	public static string Reason(string reason)
	{
		if (reason.StartsWith("notch tab → ", StringComparison.Ordinal))
		{
			string text = reason;
			int length = "notch tab → ".Length;
			return "notch_tab_" + text.Substring(length, text.Length - length);
		}
		switch (reason)
		{
		case "notch open on Home":
			return "notch_home";
		case "pointer at the top edge while tucked":
			return "top_edge_while_tucked";
		case "the welcome tour showing the notch":
			return "tour_showcase";
		case "a window came forward by itself":
			return "auto_presented";
		case "the window beside a removed one":
		case "the deck turned to it":
		case "pointer on a window":
			return "window";
		case "pointer left the arc and the notch":
			return "pointer_left";
		case "the notch's gear":
			return "notch_settings";
		case "the notch folded away":
			return "notch_folded";
		case "the notch opened on another display":
			return "moved_to_other_display";
		case "a window's red button":
		case "a pinned window's red button":
			return "red_button";
		case "the dock's last window was removed":
			return "last_window_removed";
		case "job opened in Home":
			return "job_opened";
		case "job dismissed":
			return "job_dismissed";
		case "a window's file opened":
			return "file_opened";
		case "Show me on a finished job":
			return "show_me";
		case "panel hidden: dock stopped":
			return "panel_hidden_dock_stopped";
		case "panel hidden: dock switched off":
			return "panel_hidden_dock_off";
		case "panel hidden: dock not shown (setup not done or signed out)":
			return "panel_hidden_dock_not_shown";
		case "panel hidden: HeyClicky's visuals suppressed":
			return "panel_hidden_visuals_suppressed";
		default:
			return reason.StartsWith("panel hidden: ", StringComparison.Ordinal) ? "panel_hidden" : "other";
		}
	}

	public static JsonObject? Closed(string reason, string openedReason, int windowCount, double openSeconds)
	{
		if (openSeconds < 3.0)
		{
			return null;
		}
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "closed",
			["reason"] = Reason(reason),
			["opened_reason"] = Reason(openedReason),
			["window_count"] = windowCount,
			["open_seconds"] = Math.Round(openSeconds * 10.0, MidpointRounding.AwayFromZero) / 10.0
		};
	}

	public static JsonObject DeckTurned(string via, int previous, int next, int windowCount, string windowKind)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "deck_turned",
			["via"] = via,
			["direction"] = ((next < previous) ? "left" : "right"),
			["steps"] = Math.Abs(next - previous),
			["window_count"] = windowCount,
			["window_kind"] = windowKind
		};
	}

	public static JsonObject RequestAnswered(string answer, string surface, bool isJob, bool hasLiveTask)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["answer"] = answer,
			["surface"] = surface,
			["is_job"] = isJob
		};
		if (!hasLiveTask)
		{
			jsonObject["has_live_task"] = false;
		}
		return jsonObject;
	}
}
