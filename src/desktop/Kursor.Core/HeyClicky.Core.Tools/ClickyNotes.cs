using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Tools;

public static class ClickyNotes
{
	public sealed record Report(string Slug, string Name, string Verb, DateTimeOffset At);

	public const int ResultReportReplyLimit = 6000;

	public const string EmptyReplyStandInSummary = "Codex child worker completed successfully.";

	public const int ShortRequestCharacters = 110;

	public const int MaximumSummaryCharacters = 240;

	public static string? RosterNote(IReadOnlyList<AgentClickyInfo> clickys, IReadOnlyList<AgentTaskInfo> tasks, Report? lastReport, DateTimeOffset now)
	{
		if (clickys.Count == 0)
		{
			return null;
		}
		IEnumerable<string> values = clickys.Select(delegate(AgentClickyInfo clicky)
		{
			AgentTaskInfo agentTaskInfo = tasks.FirstOrDefault((AgentTaskInfo t) => t.ClickySlug == clicky.Slug && t.IsWorking);
			string value;
			if ((object)agentTaskInfo != null)
			{
				string text = LiveWorkDescription(agentTaskInfo, now);
				value = "WORKING NOW" + ((text == null) ? "" : (": " + text));
			}
			else
			{
				value = ((clicky.ThreadId != null) ? "has history" : "fresh");
			}
			return $"- \"{clicky.Name}\" ({clicky.RoleText.ToLowerInvariant()}: {clicky.OneLiner}) — clicky_slug: {clicky.Slug}, {value}";
		});
		string text2 = "[Your Clickys] The user's persistent named agents — ongoing relationships, not one-off tasks. Clickys are ALWAYS addressed by clicky_slug (never by codexThreadId — their thread ids can change):\n" + string.Join("\n", values) + "\nWhat a Clicky knows, found, decided, or did → clicky_memory with its clicky_slug (a read — answer from it; never guess a Clicky's findings). Routing: new background work that fits one of these → sessions_spawn with its clicky_slug. A follow-up or steer for one → sessions_send with its clicky_slug. Work that fits none of these runs as a one-off job on the right edge of the screen: sessions_spawn with just task and label (see [Your jobs]). Found a new Clicky (new_clicky {name, role, description}) only when the user asks for a Clicky, or for something lasting they will keep coming back to.";
		List<string> list = new List<string>();
		if ((object)lastReport != null && clickys.Any((AgentClickyInfo c) => c.Slug == lastReport.Slug))
		{
			list.Add($"MOST RECENT REPORT: \"{lastReport.Name}\" (clicky_slug: {lastReport.Slug}) {lastReport.Verb} {Ago(now - lastReport.At)}. " + "A reply, steer, or follow-up that names no Clicky is for THIS one: sessions_send with its clicky_slug, and answer questions about \"it\" from its report (clicky_memory for details).");
		}
		List<string> list2 = (from t in tasks
			where !t.IsDemo && t.ClickySlug != null && (t.RequiresExtraUsageApproval || t.PendingComputerUseRequest != null)
			select $"\"{t.ClickyName ?? t.Title}\" (clicky_slug: {t.ClickySlug}) is waiting on a yes: " + (t.RequiresExtraUsageApproval ? "extra_usage (paused: a big task needing more agent messages from the user's plan; approving resumes it to completion)" : "computer_use (it asked for HeyClicky to drive the PC for one step)")).ToList();
		if (list2.Count > 0)
		{
			list.Add("PENDING REQUEST" + ((list2.Count == 1) ? "" : "S") + ": " + string.Join("; ", list2) + ". A yes / approve / go ahead / allow from the user answers it: sessions_answer_request with that clicky_slug (decision approve); no / not now is decision decline. When exactly one request is pending, a bare yes or no means that one.");
		}
		if (list.Count != 0)
		{
			return text2 + "\n" + string.Join("\n", list);
		}
		return text2;
	}

	internal static string? LiveWorkDescription(AgentTaskInfo task, DateTimeOffset? now = null)
	{
		List<string> list = new List<string>();
		string text2 = Clipped(task.UserPrompts.LastOrDefault() ?? "", 140);
		if (text2.Length > 0)
		{
			list.Add("on \"" + text2 + "\"");
		}
		int num = (int)((now ?? DateTimeOffset.Now) - task.StartedAt).TotalMinutes;
		list.Add((num < 1) ? "for under a minute" : $"for {num} min");
		string text3 = task.Commentary.LastOrDefault((string c) => !string.IsNullOrWhiteSpace(c));
		if (text3 != null)
		{
			list.Add("latest update \"" + Clipped(text3, 160) + "\"");
		}
		else if (task.Status == AgentTaskStatus.Starting)
		{
			list.Add("waiting to start");
		}
		return string.Join(", ", list);
		static string Clipped(string text, int limit)
		{
			string text4 = text.Replace("\r", " ").Replace("\n", " ").Trim();
			if (text4.Length <= limit)
			{
				return text4;
			}
			return text4.Substring(0, limit) + "…";
		}
	}

	private static string Ago(TimeSpan elapsed)
	{
		if (elapsed < TimeSpan.FromMinutes(1L))
		{
			return "just now";
		}
		if (!(elapsed < TimeSpan.FromHours(1)))
		{
			return $"{(int)elapsed.TotalHours} hour{(((int)elapsed.TotalHours == 1) ? "" : "s")} ago";
		}
		return $"{(int)elapsed.TotalMinutes} minute{(((int)elapsed.TotalMinutes == 1) ? "" : "s")} ago";
	}

	public static string? PlanNote(PlanSnapshot? plan, PlanUsageBucket? displayedMessages = null)
	{
		if ((object)plan == null || plan.IsFailOpenFallback == true)
		{
			return null;
		}
		string effectiveTier = plan.EffectiveTier;
		string text = ((effectiveTier == "max") ? "Max" : ((!(effectiveTier == "pro")) ? "Free" : "Pro"));
		string value = text;
		string value2 = ((plan.TierSource == "team") ? (" (a seat on the " + (plan.TeamName ?? "company") + " team plan)") : "");
		PlanUsageBucket agents = plan.Usage.Agents;
		string text2 = $"[Your plan] {value}{value2}. Agent messages this period: {agents.Used} of {agents.Cap} used, {Math.Max(0, agents.Cap - agents.Used)} left";
		if (plan.Plan == "unlimited")
		{
			text2 += ". Talk messages (talking or typing to HeyClicky): unlimited";
		}
		else
		{
			PlanUsageBucket planUsageBucket = displayedMessages ?? plan.Usage.Messages;
			text2 += $". Talk messages (talking or typing to HeyClicky): {planUsageBucket.Used} of {planUsageBucket.Cap} used, {Math.Max(0, planUsageBucket.Cap - planUsageBucket.Used)} left";
		}
		string windowResetsAt = plan.Usage.WindowResetsAt;
		if (windowResetsAt != null && DateTimeOffset.TryParse(windowResetsAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
		{
			text2 = text2 + "; resets " + result.ToLocalTime().ToString("MMMM d", CultureInfo.InvariantCulture);
		}
		return text2 + ". Silent context, not a message to answer.";
	}

	public static string EventNote(string? clickyName, string? clickySlug, string title, string @event, string? summary, bool jobsWorld = false, string? threadId = null, string? jobTitle = null, string? resultReport = null)
	{
		string text = title.Trim();
		string text2 = ((threadId != null && threadId.Length > 0) ? JobReference(threadId) : "");
		string text3 = ((clickyName != null && clickyName.Length > 0 && clickySlug != null && clickySlug.Length > 0) ? (jobsWorld ? ("Job \"" + clickyName + "\"" + text2) : $"Clicky \"{clickyName}\" (clicky_slug: {clickySlug})") : ((jobTitle == null) ? ("\"" + ((text.Length == 0) ? "A task" : text) + "\"") : ("Job \"" + ((jobTitle.Length > 0) ? jobTitle : ((text.Length > 0) ? text : "A task")) + "\"" + text2)));
		if (@event == "finished" && resultReport != null && resultReport.Length > 0)
		{
			return $"[App] {text3} {@event}. " + resultReport;
		}
		string text4 = (summary ?? "").Replace("\n", " ").Trim();
		if (text4.Length > 400)
		{
			text4 = text4.Substring(0, 400);
		}
		return "[App] " + text3 + " " + @event + ((text4.Length == 0) ? "." : (": " + text4));
	}

	public static string? ResultReport(string? factsJson, string? fullReply, IReadOnlyList<string> files)
	{
		List<string> list = new List<string>();
		if (factsJson != null && factsJson.Length > 0)
		{
			list.Add("Its facts, to build their card from: " + factsJson);
		}
		if (fullReply != null && fullReply.Length > 0)
		{
			string text = ((fullReply.Length > 6000) ? (fullReply.Substring(0, 6000) + "… (cut here; clicky_memory has the rest)") : fullReply);
			list.Add("Its full report, which they can open from the chat:\n<report>\n" + text + "\n</report>");
		}
		if (files.Count > 0)
		{
			list.Add("Files it made: " + string.Join(", ", files.Take(6)));
		}
		if (list.Count != 0)
		{
			return string.Join("\n", list);
		}
		return null;
	}

	public static string? BrowserStateNote(bool connected, string browser, bool hasChromiumBrowser, bool addsAgain, bool canDrive)
	{
		if (connected)
		{
			return "Browser: their " + browser + " is connected to HeyClicky (jobs can work in their signed-in sites).";
		}
		if (!hasChromiumBrowser)
		{
			if (!canDrive)
			{
				return null;
			}
			return "Browser: this PC has no Chrome or other Chromium browser, so jobs can't work in websites; a website task is yours to drive.";
		}
		string value;
		string value2;
		string value3;
		if (addsAgain)
		{
			value = "their " + browser + " extension needs swapping for the new version: HeyClicky's extension is in the Chrome Web Store now and updates itself, and their old preview copy no longer connects";
			value2 = "My " + browser + " extension got an upgrade, and it updates itself now. " + (canDrive ? "Want me to swap it in? Takes a few seconds." : "Want me to show you how to swap it in? Takes a few seconds.");
			value3 = (canDrive ? "approve = you remove the old one and add the new version on their screen, show_how = they do it with you showing them each step" : "approve or show_how = they add the new version with you showing them each step, then remove the old one");
		}
		else
		{
			value = "their " + browser + " isn't connected to HeyClicky yet (HeyClicky's extension is in the Chrome Web Store, a couple of clicks to add)";
			value2 = "This needs my " + browser + " extension. Want to add it? It takes a couple of clicks.";
			value3 = "approve or show_how = they add it from the Chrome Web Store with you showing them each click";
		}
		return $"Browser: {value}. Before starting a job that must act in their own account on a website (their cart, bank, a post or DM, a booking under their name) that no connected app covers, ask once: in your first line say it needs HeyClicky's {browser} extension and ask (\"{value2}\"), and call sessions_spawn with chrome \"ask\": the job waits for their choice and starts by itself once {browser} is connected. Public information the web search finds (flights, prices, news, research) and their connected apps never need it: start those as usual. Their answer is sessions_answer_request request chrome_setup ({value3}, decline = the job starts without it and does the website part on their screen). Use chrome \"without\" only when they already said they don't want it connected. Never drive the website yourself because {browser} isn't set up.";
	}

	public static string ProgressNote(string displayName, string? clickySlug, string? threadId, string commentary, bool jobsWorld = false, bool isJob = false)
	{
		string value = (jobsWorld ? null : ((clickySlug != null) ? (" (clicky_slug: " + clickySlug + ")") : null)) ?? ((threadId != null) ? JobReference(threadId) : "");
		string value2 = ((jobsWorld || (clickySlug == null && isJob)) ? "Job" : "Clicky");
		return $"[App] {value2} \"{displayName}\"{value} is still working and last said: \"{Condensed(commentary)}\"";
	}

	public static string JobEventNote(string name, string? threadId, string @event, string? summary, string? report = null)
	{
		string text = "Job \"" + (string.IsNullOrWhiteSpace(name) ? "A task" : name.Trim()) + "\"" + ((threadId == null) ? "" : JobReference(threadId));
		if (@event == "finished" && report != null && report.Length > 0)
		{
			return $"[App] {text} {@event}. " + report;
		}
		string text2 = (summary ?? "").Replace("\n", " ").Trim();
		if (text2.Length > 400)
		{
			text2 = text2.Substring(0, 400);
		}
		return "[App] " + text + " " + @event + ((text2.Length == 0) ? "." : (": " + text2));
	}

	public static string JobReference(string threadId)
	{
		return " (job_id: " + (AgentToolHooks.JobIdForThread?.Invoke(threadId) ?? threadId) + ")";
	}

	public static string ProgressNoteKey(string? clickySlug, string? threadId, string taskId)
	{
		return "progress:" + (clickySlug ?? threadId ?? taskId);
	}

	public static string Condensed(string commentary)
	{
		string text = string.Join(" ", from l in commentary.Split('\n')
			select l.Trim() into l
			where l.Length > 0
			select l);
		if (text.Length <= 240)
		{
			return text;
		}
		return text.Substring(0, 240).Trim() + "…";
	}

	public static string FinishedLine(string? clickyName, string summary, bool openedResult = false, bool hasAnotherTask = false)
	{
		string text = Subject(clickyName) + " is done.";
		string text2 = SpeakableSentence(summary);
		if (text2.Length > 0 && text2 != "Codex child worker completed successfully.")
		{
			text = text + " " + text2;
		}
		if (openedResult)
		{
			text += " It's open on your screen.";
		}
		if (hasAnotherTask)
		{
			text += " It has another task to do next.";
		}
		return text;
	}

	public static string StartedLine(string startLine)
	{
		return SpeakableSentence(startLine);
	}

	public static string AsksToUsePcLine(string? clickyName, string request)
	{
		string text = SpeakableSentence(request, 110);
		object obj;
		if (text.Length != 0)
		{
			string text2 = text.Substring(0, 1).ToLowerInvariant();
			string text3 = text;
			obj = text2 + text3.Substring(1, text3.Length - 1);
		}
		else
		{
			obj = "";
		}
		string text4 = (string)obj;
		return Subject(clickyName) + " needs one step on your PC: " + text4 + " Want me to do it?";
	}

	public static string NeedsChromeLine(string? jobName, string browserName, bool addsAgain, bool canDoItForThem)
	{
		if (!addsAgain)
		{
			return Subject(jobName) + " needs HeyClicky's " + browserName + " extension. Want to add it? It takes a couple of clicks.";
		}
		return Subject(jobName) + " needs my " + browserName + " extension. It got an upgrade and now updates itself, " + (canDoItForThem ? "so want me to swap it in?" : "so want me to show you how to swap it in?");
	}

	public static string PausedForExtraUsageLine(string? clickyName)
	{
		return Subject(clickyName) + " is paused because it's a big task that needs more agent messages from your plan to finish. A yes from you keeps it going until it's done.";
	}

	private static string Subject(string? clickyName)
	{
		if (!string.IsNullOrWhiteSpace(clickyName))
		{
			return clickyName.Trim();
		}
		return "Your job";
	}

	public static string SpeakableSentence(string text, int limit = 240)
	{
		string text2 = string.Join(" ", text.Replace("`", "").Replace("*", "").Replace("_", "")
			.Replace("#", "")
			.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		if (text2.Length == 0)
		{
			return "";
		}
		if (text2.Length > limit)
		{
			string text3 = text2.Substring(0, limit);
			int num = text3.LastIndexOfAny(new char[3] { '.', '!', '?' });
			if (num > 40)
			{
				text2 = text3.Substring(0, num + 1);
			}
			else
			{
				string[] array = text3.Split(' ');
				text2 = ((array.Length > 1) ? string.Join(" ", array[..^1]) : text3).TrimEnd(',', ';', ':', ' ');
			}
		}
		string text4 = text2;
		if (!".!?…".Contains(text4[text4.Length - 1]))
		{
			text2 += ".";
		}
		return text2;
	}
}
