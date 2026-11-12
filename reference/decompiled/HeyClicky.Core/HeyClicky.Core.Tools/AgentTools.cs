using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Documents;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Tools;

public sealed class AgentTools
{
	private sealed record Artifact(string Path, bool IsLink, long? Bytes, DateTimeOffset? Modified);

	private sealed record ArtifactCandidate(string? ThreadId, string Title, string Uri, string Kind, DateTimeOffset UpdatedAt);

	public static readonly IReadOnlySet<string> Names = new HashSet<string>
	{
		"sessions_spawn", "sessions_send", "session_status", "sessions_interrupt", "sessions_yield", "sessions_answer_request", "clicky_memory", "clicky_artifacts", "open_artifact", "reveal_artifact",
		"open_agent_dashboard", "skip_onboarding_interview", "save_drawing", "show_result", "routines", "find_jobs"
	};

	private const int ArtifactsListed = 8;

	private const int ArtifactsExcerpted = 3;

	private const int ExcerptCharacters = 1200;

	private const int FullFileCharacters = 12000;

	private const int DefaultFindJobsLimit = 5;

	private const string SpawnContext = "The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it.";

	private readonly IAgentToolHost host;

	private const string UnknownJobJson = "{\"status\":\"unknown_job\",\"message\":\"No job with that codexThreadId: use one from the [Your jobs] note.\"}";

	public IAgentToolHost Host => host;

	public event Action<string>? LaunchedByBrain;

	public AgentTools(IAgentToolHost host)
	{
		this.host = host;
	}

	public Task<string> RunAsync(string toolName, ToolArgs args, CompanionToolContext context, CancellationToken ct = default(CancellationToken))
	{
		return toolName switch
		{
			"sessions_spawn" => GuardedSpawnAsync(args, context, ct), 
			"show_result" => Task.FromResult(AgentToolHooks.ShowResult?.Invoke(args.Json.ToJsonString()) ?? ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "error",
				["message"] = "No job came back just now."
			})), 
			"sessions_send" => SendAsync(args, context, ct), 
			"session_status" => StatusAsync(ct), 
			"sessions_interrupt" => InterruptAsync(args), 
			"sessions_yield" => YieldAsync(args, ct), 
			"sessions_answer_request" => AnswerRequestAsync(args, ct), 
			"clicky_memory" => MemoryAsync(args, ct), 
			"clicky_artifacts" => Task.Run(() => Artifacts(args), ct), 
			"open_artifact" => Task.FromResult(ArtifactAction(args, reveal: false)), 
			"reveal_artifact" => Task.FromResult(ArtifactAction(args, reveal: true)), 
			"open_agent_dashboard" => Task.FromResult(Dashboard(args)), 
			"skip_onboarding_interview" => Task.FromResult(SkipInterview()), 
			"save_drawing" => SaveDrawingAsync(args), 
			"routines" => RoutinesAsync(args), 
			"find_jobs" => host.FindJobsAsync(args.Str("query") ?? "", args.Int("limit") ?? 5), 
			_ => Task.FromResult(ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["error"] = "not_implemented",
				["tool"] = toolName
			})), 
		};
	}

	private static async Task<string> RoutinesAsync(ToolArgs args)
	{
		Func<string, Task<string>> routines = AgentToolHooks.Routines;
		if (routines == null)
		{
			return ToolResults.Status("error", "Routines aren't available right now.");
		}
		try
		{
			return await routines(args.Json.ToJsonString()).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "routines: " + ex.Message);
			return ToolResults.Status("error", "The routine couldn't be changed just now.");
		}
	}

	private static async Task<string> SaveDrawingAsync(ToolArgs args)
	{
		Func<string, Task<string>> saveDrawing = AgentToolHooks.SaveDrawing;
		if (saveDrawing == null)
		{
			return ToolResults.Status("failed", "The drawing couldn't be saved.");
		}
		try
		{
			return await saveDrawing(args.Str("drawing")).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "save_drawing: " + ex.Message);
			return ToolResults.Status("failed", "The drawing couldn't be saved.");
		}
	}

	private async Task<string> GuardedSpawnAsync(ToolArgs args, CompanionToolContext context, CancellationToken ct)
	{
		string spawnTask = args.Str("task") ?? "";
		string text = SpawnRepeatGuard.TryClaim(spawnTask);
		if (text != null)
		{
			return text;
		}
		string text2 = await SpawnAsync(args, context, ct).ConfigureAwait(continueOnCapturedContext: false);
		SpawnRepeatGuard.Settle(spawnTask, text2);
		return text2;
	}

	private async Task<string> SpawnAsync(ToolArgs args, CompanionToolContext context, CancellationToken ct)
	{
		string task = args.Str("task");
		if (task == null)
		{
			return "{\"status\":\"error\",\"message\":\"missing task\"}";
		}
		string text = await host.HandleSuggestionActionAsync(task).ConfigureAwait(continueOnCapturedContext: false);
		if (text != null)
		{
			return text;
		}
		if (AgentToolHooks.IsTourInterviewFoundingOnly?.Invoke() ?? false)
		{
			return new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "not_now",
				["message"] = "Nothing starts during the welcome tour's interview. Keep the conversation going; their first jobs come after it."
			}.ToJsonString();
		}
		string label = args.Str("label") ?? "";
		bool includeScreenshot = args.Bool("include_screenshot") ?? true;
		string requestedSlug = SpawnRepeatGuard.RosterSlug(args.Str("clicky_slug"), delegate(string slug)
		{
			AgentClickyInfo agentClickyInfo = host.FindClicky(slug);
			return (object)agentClickyInfo != null && !agentClickyInfo.IsArchived;
		}) ?? args.Str("clicky_slug");
		if (AgentToolHooks.JobsEnabled)
		{
			if (requestedSlug != null && requestedSlug.Length > 0)
			{
				AgentClickyInfo agentClickyInfo2 = host.FindClicky(requestedSlug);
				if ((object)agentClickyInfo2 != null && !agentClickyInfo2.IsArchived)
				{
					goto IL_02bd;
				}
			}
			return await SpawnJobAsync(task, label, includeScreenshot, context, ct, args.Str("chrome"), SessionsSendContext.BuildsOnJobIds(args.Json)).ConfigureAwait(continueOnCapturedContext: false);
		}
		goto IL_02bd;
		IL_02bd:
		JsonObject newClicky = args.Object("new_clicky");
		string newName = null;
		string text2 = null;
		string text3 = null;
		bool flag = false;
		if (newClicky != null)
		{
			ToolArgs toolArgs = new ToolArgs(newClicky.ToJsonString());
			newName = toolArgs.Str("name");
			text2 = toolArgs.Str("role");
			text3 = toolArgs.Str("description", "one_liner", "oneLiner");
			flag = toolArgs.Bool("start_idle") == true;
		}
		AgentClickyInfo existing = ((requestedSlug == null) ? null : host.FindClicky(requestedSlug));
		if ((object)existing != null && existing.IsArchived)
		{
			existing = null;
		}
		if ((object)existing == null && requestedSlug == null && newName == null)
		{
			(string, string) tuple = FallbackClickyIdentity.For((label.Length > 0) ? label : task);
			newName = tuple.Item1;
			text2 = tuple.Item2;
			text2 = ((text2 == "Agent") ? null : text2);
		}
		if ((object)existing == null && newName != null && !flag)
		{
			string wanted = FallbackClickyIdentity.NormalizedName(newName);
			existing = host.Clickys.FirstOrDefault((AgentClickyInfo c) => !c.IsArchived && FallbackClickyIdentity.NormalizedName(c.Name) == wanted);
		}
		if ((object)existing == null && newName != null && flag)
		{
			AgentClickyInfo agentClickyInfo3 = host.CreateClicky(newName, string.IsNullOrWhiteSpace(text2) ? "Helper" : text2, text3 ?? OneLinerFrom(task));
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "founded",
				["clicky_slug"] = agentClickyInfo3.Slug,
				["clicky_name"] = agentClickyInfo3.Name,
				["founded_new_clicky"] = true,
				["message"] = "\"" + agentClickyInfo3.Name + "\" is made. It has NOT started anything: it waits for the user to tell it what to do. Say one short sentence introducing it by name and what it is for, and that it's ready for its first message. Never say it is working, starting, or on it."
			});
		}
		HashSet<string> knownSlugs = host.Clickys.Select((AgentClickyInfo c) => c.Slug).ToHashSet();
		string text4 = HandoffContext(context, task, includeScreenshot);
		string prompt = task;
		string clickySlug = existing?.Slug;
		string newClickyName = (((object)existing == null) ? newName : null);
		string newClickyRole = (string.IsNullOrWhiteSpace(text2) ? "Helper" : text2);
		string newClickyOneLiner = text3;
		string launchContext = ((text4 == null) ? "The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it." : ("The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it.\n\n" + text4));
		string launchSource = (context.Typed ? "text" : "voice");
		AgentToolLaunch launch = new AgentToolLaunch(prompt, clickySlug, newClickyName, newClickyRole, newClickyOneLiner, launchContext, launchSource, await LaunchImagesAsync(context, includeScreenshot).ConfigureAwait(continueOnCapturedContext: false), (context.AttachedFilePaths.Count > 0) ? context.AttachedFilePaths : null, "companion", AsJob: false, null, NullIfEmpty(SessionsSendContext.BuildsOnJobIds(args.Json)));
		string text5;
		try
		{
			text5 = await host.LaunchAsync(launch, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "blocked",
				["label"] = label,
				["message"] = "Couldn't start the agent: " + ex.Message + ". Tell the user in one short line; don't say it is on it."
			});
		}
		this.LaunchedByBrain?.Invoke(text5);
		AgentTaskInfo obj = await WaitForClickyAsync(text5, ((object)existing == null && newName == null) ? 4000 : 1500, ct).ConfigureAwait(continueOnCapturedContext: false);
		string text6 = obj?.ClickySlug ?? existing?.Slug;
		string text7 = obj?.ClickyName ?? existing?.Name ?? newName;
		bool flag2 = text6 != null && !knownSlugs.Contains(text6);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "dispatched",
			["label"] = label
		};
		if (text6 != null)
		{
			jsonObject["clicky_slug"] = text6;
		}
		if (text7 != null)
		{
			jsonObject["clicky_name"] = text7;
		}
		if (flag2)
		{
			jsonObject["founded_new_clicky"] = true;
		}
		if (newClicky == null && requestedSlug == null && text7 != null)
		{
			jsonObject["message"] = (flag2 ? ("HeyClicky founded a new Clicky named \"" + text7 + "\" for this task and it is on it now. Mention it by name in one short sentence (it lives in the user's Home; it works when messaged and remembers).") : ("HeyClicky routed this task to the user's existing Clicky \"" + text7 + "\" — it is on it now. Say so by name in one short sentence."));
		}
		return ToolResults.Json(jsonObject);
	}

	private async Task<string> SpawnJobAsync(string task, string label, bool includeScreenshot, CompanionToolContext context, CancellationToken ct, string? chrome = null, IReadOnlyList<string>? buildsOnJobIds = null)
	{
		Func<string, string, Task<string>> startWaitingJobNamed = AgentToolHooks.StartWaitingJobNamed;
		bool flag = startWaitingJobNamed != null;
		string text = default(string);
		if (flag)
		{
			text = await startWaitingJobNamed(label, task).ConfigureAwait(continueOnCapturedContext: false);
			flag = text != null;
		}
		if (flag)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "dispatched",
				["label"] = label,
				["job_title"] = text
			});
		}
		string text2 = HandoffContext(context, task, includeScreenshot);
		string newClickyName = ((label.Trim().Length > 0) ? label.Trim() : null);
		string launchContext = ((text2 == null) ? "The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it." : ("The voice decided this needs background work and called sessions_spawn, so HeyClicky is starting a helper on it.\n\n" + text2));
		string launchSource = (context.Typed ? "text" : "voice");
		AgentToolLaunch agentToolLaunch = new AgentToolLaunch(task, null, newClickyName, "Helper", null, launchContext, launchSource, await LaunchImagesAsync(context, includeScreenshot).ConfigureAwait(continueOnCapturedContext: false), (context.AttachedFilePaths.Count > 0) ? context.AttachedFilePaths : null, "companion", AsJob: true, (chrome == "without") ? "without" : null, NullIfEmpty(buildsOnJobIds));
		if (chrome == "ask")
		{
			string text3 = AgentToolHooks.HoldJobForChrome?.Invoke(agentToolLaunch);
			if (text3 != null)
			{
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = "waiting_on_chrome",
					["label"] = label,
					["job_title"] = text3,
					["message"] = "Not started yet: it waits on the dock for their Chrome choice and starts by itself once Chrome is connected, or right away without it if they pass. If your line hasn't asked yet, ask now in one short line whether you should set up their Chrome."
				});
			}
		}
		string taskId;
		try
		{
			taskId = await host.LaunchAsync(agentToolLaunch, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "blocked",
				["label"] = label,
				["message"] = "Couldn't start the job: " + ex.Message + ". Tell the user in one short line; don't say it is on it."
			});
		}
		this.LaunchedByBrain?.Invoke(taskId);
		ClickyAnalytics.TrackProductEvent("job_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "started" });
		string text4 = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.TaskId == taskId)?.Title ?? label;
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "dispatched",
			["label"] = label,
			["job_title"] = text4
		});
	}

	private async Task<AgentTaskInfo?> WaitForClickyAsync(string taskId, int milliseconds, CancellationToken ct)
	{
		long deadline = Environment.TickCount64 + milliseconds;
		AgentTaskInfo agentTaskInfo;
		while (true)
		{
			agentTaskInfo = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.TaskId == taskId);
			if (agentTaskInfo?.ClickySlug != null || Environment.TickCount64 > deadline)
			{
				break;
			}
			await Task.Delay(100, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		return agentTaskInfo;
	}

	private static IReadOnlyList<string>? NullIfEmpty(IReadOnlyList<string>? ids)
	{
		if (ids == null || ids.Count <= 0)
		{
			return null;
		}
		return ids;
	}

	private static Task<IReadOnlyList<byte[]>?> LaunchImagesAsync(CompanionToolContext context, bool includeScreenshot)
	{
		return LaunchImagesAsync(context, includeScreenshot, context.AttachedFilePaths);
	}

	private static async Task<IReadOnlyList<byte[]>?> LaunchImagesAsync(CompanionToolContext context, bool includeScreenshot, IReadOnlyList<string> attachedFilePaths)
	{
		List<byte[]> images = new List<byte[]>();
		if (includeScreenshot)
		{
			byte[] screenshotJpeg = context.ScreenshotJpeg;
			if (screenshotJpeg != null && screenshotJpeg.Length > 0)
			{
				images.Add(screenshotJpeg);
			}
		}
		foreach (string item in Attachments.NormalizedPaths(attachedFilePaths).Where(Attachments.IsImage).Take(4))
		{
			byte[] array = await Attachments.JpegAsync(item).ConfigureAwait(continueOnCapturedContext: false);
			if (array != null)
			{
				images.Add(array);
			}
		}
		return (images.Count > 0) ? images : null;
	}

	internal static string? HandoffContext(CompanionToolContext context, string task, bool includeScreenshot)
	{
		List<string> list = new List<string>();
		string text = HandoffBlock(context.UserWords, task);
		if (text != null)
		{
			list.Add(text);
		}
		if (includeScreenshot)
		{
			string highlightGesture = context.HighlightGesture;
			if (highlightGesture != null && highlightGesture.Length > 0)
			{
				list.Add("The user highlighted part of the attached screenshot before handing this off — focus on what they marked. " + highlightGesture);
			}
		}
		PageReference page = context.Page;
		if ((object)page != null)
		{
			list.Add(page.AgentPromptContextLine);
		}
		if (list.Count != 0)
		{
			return string.Join("\n\n", list);
		}
		return null;
	}

	internal static string? HandoffBlock(string? rawUserRequest, string inferredTask)
	{
		string text = inferredTask.Trim();
		string value = rawUserRequest?.Trim();
		if (text.Length == 0 || string.IsNullOrEmpty(value))
		{
			return null;
		}
		return $"{"<AGENT_HANDOFF>"}\n{"Original user request (typed/STT text):"}\n{value}\n\n{"HeyClicky's brief:"}\n{text}\n\n{"How to read this:"} the brief is HeyClicky's: do it. Where it disagrees with the user's own words, their words win. On a small ambiguity, take the likeliest reading and go; ask only when acting could hit the wrong account or project or do something risky. Answer in your reply, and make a file only when they asked for one or the work is itself a file (a site, an app, code).\n{"</AGENT_HANDOFF>"}";
	}

	public static string? HandOffArguments(string payload)
	{
		ToolArgs toolArgs = new ToolArgs(payload);
		string text = toolArgs.Str("task");
		if (text == null)
		{
			return null;
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		jsonObject["task"] = text;
		jsonObject["label"] = toolArgs.Str("label") ?? "";
		jsonObject["include_screenshot"] = true;
		JsonObject jsonObject2 = jsonObject;
		string text2 = toolArgs.Str("clicky", "clicky_slug");
		if (text2 != null)
		{
			jsonObject2["clicky_slug"] = text2;
		}
		else
		{
			JsonObject jsonObject3 = toolArgs.Object("newClicky") ?? toolArgs.Object("new_clicky");
			if (jsonObject3 != null)
			{
				ToolArgs toolArgs2 = new ToolArgs(jsonObject3.ToJsonString());
				string text3 = toolArgs2.Str("name");
				if (text3 != null)
				{
					jsonObject2["new_clicky"] = new JsonObject((JsonNodeOptions?)null)
					{
						["name"] = text3,
						["role"] = toolArgs2.Exact("role") ?? "",
						["description"] = toolArgs2.Exact("description") ?? ""
					};
				}
			}
		}
		return jsonObject2.ToJsonString();
	}

	private static string OneLinerFrom(string task)
	{
		string text = string.Join(" ", task.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		if (text.Length <= 120)
		{
			return text;
		}
		return text.Substring(0, 120).TrimEnd() + "…";
	}

	private async Task<string> SendAsync(ToolArgs args, CompanionToolContext context, CancellationToken ct)
	{
		string text = args.Str("message");
		if (text == null)
		{
			return "{\"status\":\"error\",\"message\":\"missing message\"}";
		}
		string usersOwnWords = context.UserWords?.Trim() ?? "";
		bool flag = SessionsSendContext.IsHeyClickysOwnInstruction(args.Str("from"), text);
		bool forwardsUsersOwnWords = usersOwnWords.Length > 0 && !flag;
		string messageForAgent = (forwardsUsersOwnWords ? usersOwnWords : text);
		IReadOnlyList<string> readOnlyList;
		if (usersOwnWords.Length != 0)
		{
			readOnlyList = context.AttachedFilePaths;
		}
		else
		{
			IReadOnlyList<string> readOnlyList2 = Array.Empty<string>();
			readOnlyList = readOnlyList2;
		}
		IReadOnlyList<string> filesForAgent = readOnlyList;
		string launchContext = (forwardsUsersOwnWords ? SessionsSendContext.UsersOwnWordsLaunchContext(usersOwnWords, text, context.Typed) : ((usersOwnWords.Length > 0) ? SessionsSendContext.HeyClickyInstructionLaunchContext(usersOwnWords, context.Typed) : "HeyClicky is passing the user's message on to this work (sessions_send)."));
		IReadOnlyList<string> buildsOn = SessionsSendContext.BuildsOnJobIds(args.Json);
		IReadOnlyList<byte[]> imagesJpeg = await LaunchImagesAsync(context, args.Bool("include_screenshot") ?? true, filesForAgent).ConfigureAwait(continueOnCapturedContext: false);
		string text2 = args.Str("clicky_slug");
		string threadId = args.Str("codexThreadId", "childSessionId");
		object obj;
		if (text2 != null)
		{
			string text3 = SpawnRepeatGuard.RosterSlug(text2, delegate(string s)
			{
				AgentClickyInfo agentClickyInfo = host.FindClicky(s);
				return (object)agentClickyInfo != null && !agentClickyInfo.IsArchived;
			});
			obj = ((text3 != null) ? host.FindClicky(text3) : null);
		}
		else
		{
			obj = null;
		}
		AgentClickyInfo clicky = (AgentClickyInfo)obj;
		if (text2 != null && ((object)clicky == null || clicky.IsArchived))
		{
			return ToolResults.Status("error", "Unknown clicky_slug — check the [Your Clickys] note for valid slugs.");
		}
		if ((object)clicky == null && threadId == null)
		{
			return ToolResults.Status("error", "missing clicky_slug or codexThreadId — address a persistent Clicky by clicky_slug; for one-off agents call session_status first and pass its codexThreadId.");
		}
		try
		{
			string taskId;
			if ((object)clicky != null)
			{
				taskId = await host.LaunchAsync(new AgentToolLaunch(messageForAgent, clicky.Slug, null, "Helper", null, launchContext, context.Typed ? "text" : "voice", imagesJpeg, (filesForAgent.Count > 0) ? filesForAgent : null, "companion", AsJob: false, null, NullIfEmpty(buildsOn)), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				taskId = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.ThreadId == threadId)?.TaskId ?? host.OpenThread(threadId);
				string text4 = SteerText(messageForAgent, launchContext, forwardsUsersOwnWords, usersOwnWords.Length > 0, filesForAgent, (buildsOn.Count > 0) ? host.BuildsOnContext(buildsOn, threadId) : null);
				await host.SteerAsync(taskId, text4, imagesJpeg, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			this.LaunchedByBrain?.Invoke(taskId);
			string text5 = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.TaskId == taskId)?.ThreadId ?? clicky?.ThreadId ?? threadId ?? "";
			if ((object)clicky == null && threadId != null)
			{
				string text6 = args.Str("label");
				if (text6 != null && text6.Length > 0)
				{
					host.RetitleJob(threadId, text6);
				}
			}
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "sent",
				["codexThreadId"] = text5,
				["sent_as"] = (forwardsUsersOwnWords ? "the user's own words, unchanged" : "your message")
			});
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "blocked",
				["codexThreadId"] = threadId ?? clicky?.ThreadId ?? "",
				["message"] = "Couldn't send the follow-up: " + ex.Message
			});
		}
	}

	internal static string SteerText(string messageForAgent, string launchContext, bool forwardsUsersOwnWords, bool hadUsersWords, IReadOnlyList<string> filesForAgent, string? buildsOnContext)
	{
		List<string> list = new List<string> { messageForAgent };
		if (forwardsUsersOwnWords)
		{
			int num = launchContext.IndexOf("(their words win where the two differ): ", StringComparison.Ordinal);
			if (num >= 0)
			{
				int num2 = num + "(their words win where the two differ): ".Length;
				list.Add("HeyClicky's note on what this refers to and what was agreed with them (their words win where the two differ): " + launchContext.Substring(num2, launchContext.Length - num2).Trim());
			}
		}
		else if (hadUsersWords)
		{
			list.Add(launchContext);
		}
		if (buildsOnContext != null && buildsOnContext.Length > 0)
		{
			list.Add(buildsOnContext);
		}
		if (filesForAgent.Count > 0)
		{
			list.Add("Files the user attached:\n" + string.Join("\n", filesForAgent.Select((string path) => "- " + path)));
		}
		return string.Join("\n\n", list);
	}

	private async Task<string> InterruptAsync(ToolArgs args)
	{
		string threadId = args.Str("codexThreadId", "childSessionId");
		string slug = args.Str("clicky_slug");
		if (threadId == null && slug == null)
		{
			return ToolResults.Status("error", "missing codexThreadId — call session_status first to get the agent's codexThreadId, then pass it here.");
		}
		AgentTaskInfo target = host.Tasks.FirstOrDefault((AgentTaskInfo t) => (threadId != null && t.ThreadId == threadId) || (slug != null && t.ClickySlug == slug));
		if ((object)target == null || (!target.IsWorking && target.Status != AgentTaskStatus.WaitingForUser))
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "not_found",
				["codexThreadId"] = threadId ?? target?.ThreadId ?? ""
			});
		}
		await host.StopAsync(target.TaskId).ConfigureAwait(continueOnCapturedContext: false);
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "stopped",
			["codexThreadId"] = target.ThreadId ?? threadId ?? ""
		});
	}

	private async Task<string> YieldAsync(ToolArgs args, CancellationToken ct)
	{
		string threadId = args.Str("codexThreadId", "childSessionId");
		if (threadId == null)
		{
			return await StatusAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		AgentTaskInfo agentTaskInfo = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.ThreadId == threadId);
		if ((object)agentTaskInfo != null)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["codexThreadId"] = threadId,
				["title"] = agentTaskInfo.Title,
				["state"] = StateText(agentTaskInfo.Status),
				["summary"] = (string.IsNullOrWhiteSpace(agentTaskInfo.Summary) ? agentTaskInfo.ProgressSummary : agentTaskInfo.Summary),
				["status"] = "ok"
			});
		}
		AgentThreadInfo agentThreadInfo = (await SafeHistoryAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((AgentThreadInfo h) => h.ThreadId == threadId);
		if ((object)agentThreadInfo != null)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["codexThreadId"] = threadId,
				["title"] = agentThreadInfo.Title ?? "Agent task",
				["state"] = agentThreadInfo.Status,
				["summary"] = agentThreadInfo.Preview,
				["status"] = "ok"
			});
		}
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "not_found",
			["codexThreadId"] = threadId
		});
	}

	private async Task<string> StatusAsync(CancellationToken ct)
	{
		Dictionary<string, List<ArtifactCandidate>> artifactsByThread = (from a in ArtifactCandidates()
			group a by a.ThreadId ?? "").ToDictionary((IGrouping<string, ArtifactCandidate> g) => g.Key, (IGrouping<string, ArtifactCandidate> g) => g.ToList());
		IReadOnlyList<AgentTaskInfo> tasks = host.Tasks;
		JsonArray active = new JsonArray((JsonNodeOptions?)null);
		foreach (AgentTaskInfo item in tasks)
		{
			string? threadId = item.ThreadId;
			string? clickySlug = item.ClickySlug;
			string? title = item.ClickyName ?? item.Title;
			string? prompt = item.UserPrompts.LastOrDefault() ?? "";
			string state = StateText(item.Status);
			string summary = (string.IsNullOrWhiteSpace(item.Summary) ? item.ProgressSummary : item.Summary);
			string threadId2 = item.ThreadId;
			active.Add(Entry(threadId, clickySlug, title, prompt, state, summary, (threadId2 != null && artifactsByThread.TryGetValue(threadId2, out var value)) ? value : new List<ArtifactCandidate>()));
		}
		HashSet<string> activeThreads = (from t in tasks
			select t.ThreadId into t
			where t != null
			select t).ToHashSet();
		JsonArray previous = new JsonArray((JsonNodeOptions?)null);
		foreach (AgentThreadInfo item2 in (from h in await SafeHistoryAsync(25, ct).ConfigureAwait(continueOnCapturedContext: false)
			where !activeThreads.Contains(h.ThreadId)
			orderby h.UpdatedAt ?? DateTimeOffset.MinValue descending
			select h).Take(10))
		{
			previous.Add(Entry(item2.ThreadId, item2.ClickySlug, item2.Title ?? "Agent task", item2.Preview, item2.Status, item2.Preview, artifactsByThread.TryGetValue(item2.ThreadId, out var value2) ? value2 : new List<ArtifactCandidate>()));
		}
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "ok",
			["activeCount"] = active.Count,
			["previousCount"] = previous.Count,
			["activeAgents"] = active,
			["previousAgents"] = previous,
			["note"] = ((active.Count == 0 && previous.Count == 0) ? "No agents have run yet this session." : "These are the agents you've started. To open a listed artifact, call open_artifact with its codexThreadId or uri.")
		});
	}

	private static JsonObject Entry(string? threadId, string? slug, string title, string prompt, string state, string summary, List<ArtifactCandidate> artifacts)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["title"] = title,
			["prompt"] = prompt,
			["state"] = state,
			["summary"] = summary
		};
		if (threadId != null)
		{
			jsonObject["codexThreadId"] = threadId;
		}
		if (slug != null)
		{
			jsonObject["clicky_slug"] = slug;
			jsonObject["is_persistent_clicky"] = true;
		}
		if (artifacts.Count > 0)
		{
			jsonObject["artifactCount"] = artifacts.Count;
			jsonObject["artifacts"] = new JsonArray(artifacts.Take(6).Select((Func<ArtifactCandidate, JsonNode>)((ArtifactCandidate a) => new JsonObject((JsonNodeOptions?)null)
			{
				["title"] = a.Title,
				["uri"] = a.Uri,
				["kind"] = a.Kind
			})).ToArray());
		}
		return jsonObject;
	}

	private static string StateText(AgentTaskStatus status)
	{
		return status switch
		{
			AgentTaskStatus.Starting => "queued", 
			AgentTaskStatus.Running => "running", 
			AgentTaskStatus.WaitingForUser => "waiting_for_user", 
			AgentTaskStatus.Finished => "completed", 
			AgentTaskStatus.Failed => "failed", 
			_ => "interrupted", 
		};
	}

	private async Task<IReadOnlyList<AgentThreadInfo>> SafeHistoryAsync(int limit, CancellationToken ct)
	{
		try
		{
			return await host.HistoryAsync(limit, ct).WaitAsync(TimeSpan.FromSeconds(4L), ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
		{
			Log.Warn("Tools", "agent history: " + ex.Message);
			return Array.Empty<AgentThreadInfo>();
		}
	}

	private async Task<string> AnswerRequestAsync(ToolArgs args, CancellationToken ct)
	{
		string text = args.Str("request")?.Trim().ToLowerInvariant() ?? "";
		string text2 = args.Str("decision")?.Trim().ToLowerInvariant() ?? "";
		string slug = args.Str("clicky_slug");
		string threadId = args.Str("codexThreadId");
		bool flag;
		switch (text)
		{
		case "chrome_setup":
			return await AnswerChromeSetupAsync(text2, threadId).ConfigureAwait(continueOnCapturedContext: false);
		case "extra_usage":
		case "computer_use":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return ToolResults.Status("error", "request must be extra_usage or computer_use");
		}
		if ((!(text2 == "approve") && !(text2 == "decline")) || 1 == 0)
		{
			return ToolResults.Status("error", "decision must be approve or decline");
		}
		if (text == "computer_use")
		{
			List<AgentTaskInfo> list = host.Tasks.Where((AgentTaskInfo t) => !t.IsDemo && t.PendingComputerUseRequest != null).ToList();
			AgentTaskInfo agentTaskInfo = ((slug != null) ? list.FirstOrDefault((AgentTaskInfo t) => t.ClickySlug == slug) : ((threadId != null) ? list.FirstOrDefault((AgentTaskInfo t) => t.ThreadId == threadId) : ((list.Count == 1) ? list[0] : null)));
			if ((object)agentTaskInfo == null)
			{
				IEnumerable<string> values = from t in list
					select (t.ClickySlug == null) ? ((t.ThreadId == null) ? null : (t.Title + " (codexThreadId: " + t.ThreadId + ")")) : ((t.ClickyName ?? t.Title) + " (clicky_slug: " + t.ClickySlug + ")") into n
					where n != null
					select n;
				return ToolResults.Status("error", (list.Count == 0) ? "No Clicky is waiting on a computer_use answer right now — nothing to approve or decline; tell the user so." : ("Several are waiting on a computer_use answer; pass the clicky_slug or codexThreadId of the one the user means: " + string.Join(", ", values) + "."));
			}
			string text3 = agentTaskInfo.ClickyName ?? agentTaskInfo.Title;
			if (text2 == "approve")
			{
				host.ApproveComputerUse(agentTaskInfo.TaskId);
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = "approved",
					["clicky_name"] = text3,
					["message"] = $"HeyClicky is driving the PC now for {text3}'s step while the user watches; {text3} carries on after. Confirm in one short line."
				});
			}
			host.DeclineComputerUse(agentTaskInfo.TaskId);
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "declined",
				["clicky_name"] = text3,
				["message"] = "Declined: " + text3 + " will finish without controlling the PC. Confirm in one short line."
			});
		}
		List<AgentTaskInfo> list2 = host.Tasks.Where((AgentTaskInfo t) => !t.IsDemo && t.RequiresExtraUsageApproval).ToList();
		AgentTaskInfo agentTaskInfo2 = ((slug != null) ? list2.FirstOrDefault((AgentTaskInfo t) => t.ClickySlug == slug) : ((threadId != null) ? list2.FirstOrDefault((AgentTaskInfo t) => t.ThreadId == threadId) : ((list2.Count == 1) ? list2[0] : null)));
		if ((object)agentTaskInfo2 == null)
		{
			IEnumerable<string> values2 = from t in list2
				select (t.ClickySlug == null) ? ((t.ThreadId == null) ? null : (t.Title + " (codexThreadId: " + t.ThreadId + ")")) : ((t.ClickyName ?? t.Title) + " (clicky_slug: " + t.ClickySlug + ")") into n
				where n != null
				select n;
			return ToolResults.Status("error", (list2.Count == 0) ? ("No Clicky is waiting on a " + text + " answer right now — nothing to approve or decline; tell the user so.") : $"Several are waiting on a {text} answer; pass the clicky_slug or codexThreadId of the one the user means: {string.Join(", ", values2)}.");
		}
		string name = agentTaskInfo2.ClickyName ?? agentTaskInfo2.Title;
		if (text2 == "decline")
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "left_paused",
				["clicky_name"] = name,
				["message"] = name + " stays paused; the Approve button stays on its card if the user changes their mind. Confirm in one short line."
			});
		}
		try
		{
			await host.ApproveExtraUsageAsync(agentTaskInfo2.TaskId, asManyAsNeeded: true, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			return ToolResults.Status("error", "The approval didn't go through (" + ex.Message + "). Tell the user in one short line.");
		}
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "approved",
			["clicky_name"] = name,
			["message"] = "Approved: " + name + " is resuming and will run to completion. Confirm in one short line."
		});
	}

	private static async Task<string> AnswerChromeSetupAsync(string decision, string? threadId)
	{
		string text = decision switch
		{
			"approve" => "do_it_for_me", 
			"show_how" => "show_me_how", 
			"decline" => "not_now", 
			_ => null, 
		};
		if (text == null)
		{
			return ToolResults.Status("error", "decision must be approve, show_how or decline");
		}
		Func<string, string, Task<string>> answerChromeSetup = AgentToolHooks.AnswerChromeSetup;
		string text2 = ((answerChromeSetup == null) ? null : (await answerChromeSetup(string.IsNullOrWhiteSpace(threadId) ? null : threadId.Trim(), text).ConfigureAwait(continueOnCapturedContext: false)));
		string text3 = text2;
		if (text3 == null)
		{
			return ToolResults.Status("error", "No job is waiting on a Chrome choice right now; nothing to answer. Tell the user so.");
		}
		string text4 = AgentToolHooks.ChromeSetupBrowserName?.Invoke() ?? "Chrome";
		text2 = ((text3 == "do_it_for_me") ? ("HeyClicky is removing the old copy of its " + text4 + " extension on their screen now, then adding the new version; the job starts or carries on by itself once it's connected. Confirm in one short line.") : ((!(text3 == "show_me_how")) ? "The job goes on without the Chrome extension and does the website part on their screen instead. Confirm in one short line, mentioning the extension makes this quicker next time." : ("HeyClicky opens its Chrome Web Store page in " + text4 + ", and you'll be asked to show them each click in a moment. Say one short line, like \"Here we go.\"")));
		string text5 = text2;
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = text3,
			["message"] = text5
		});
	}

	private string SlugForJobToolArguments(ToolArgs args, string threadId)
	{
		string text = args.Str("clicky_slug")?.Trim() ?? "";
		if (text.Length > 0 || threadId.Length == 0 || (object)host.FindJob(threadId) != null)
		{
			return text;
		}
		return host.ClickySlugForThread(threadId) ?? "";
	}

	private static string Tail(string id)
	{
		if (id.Length <= 8)
		{
			return id;
		}
		int length = id.Length;
		int num = length - 8;
		return id.Substring(num, length - num);
	}

	private async Task<string> MemoryAsync(ToolArgs args, CancellationToken ct)
	{
		string requestedThreadId = args.Str("codexThreadId")?.Trim() ?? "";
		string text = SlugForJobToolArguments(args, requestedThreadId);
		if (text.Length == 0 && requestedThreadId.Length > 0)
		{
			JsonObject jsonObject = await JobResultPayloadAsync(requestedThreadId, ct).ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("Tools", $"clicky_memory job thread={Tail(requestedThreadId)} status={jsonObject["status"]}");
			return ToolResults.Json(jsonObject);
		}
		AgentClickyInfo clicky = ((text.Length == 0) ? null : host.FindClicky(text));
		if ((object)clicky == null)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "unknown_clicky",
				["message"] = "No Clicky with that clicky_slug — use a slug from the [Your Clickys] note.",
				["known_slugs"] = new JsonArray(((IEnumerable<AgentClickyInfo>)host.Clickys).Select((Func<AgentClickyInfo, JsonNode>)((AgentClickyInfo c) => c.Slug)).ToArray())
			});
		}
		string memory = "";
		string text2 = host.WorkspacePath(clicky.Slug);
		if (text2 != null)
		{
			try
			{
				memory = File.ReadAllText(Path.Combine(text2, "AGENTS.md"));
			}
			catch
			{
			}
		}
		if (memory.Length > 6000)
		{
			memory = memory.Substring(0, 6000) + "\n…(memory file truncated)";
		}
		AgentTaskInfo live = host.Tasks.FirstOrDefault((AgentTaskInfo t) => t.ClickySlug == clicky.Slug);
		bool working = live?.IsWorking ?? false;
		string threadId = clicky.ThreadId ?? live?.ThreadId;
		IReadOnlyList<(string Role, string Text)> finished = Array.Empty<(string, string)>();
		if (threadId != null)
		{
			try
			{
				finished = await host.RecentMessagesAsync(threadId, 20, ct).WaitAsync(TimeSpan.FromSeconds(4L), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
			{
				Log.Warn("Tools", "clicky_memory thread: " + ex.Message);
			}
		}
		string text3 = (((object)live != null && !string.IsNullOrWhiteSpace(live.Summary)) ? live.Summary : (finished.LastOrDefault<(string, string)>(((string Role, string Text) m) => m.Role == "clicky").Item2 ?? ""));
		JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "ok",
			["clicky_slug"] = clicky.Slug,
			["clicky_name"] = clicky.Name,
			["role"] = clicky.RoleText,
			["description"] = clicky.OneLiner,
			["is_archived"] = clicky.IsArchived,
			["is_working_now"] = working,
			["has_conversation"] = threadId != null,
			["memory_markdown"] = ((memory.Length == 0) ? "(no memory file yet — this Clicky hasn't worked yet)" : memory)
		};
		if (working)
		{
			jsonObject2["current_progress"] = (string.IsNullOrWhiteSpace(live.ProgressSummary) ? "working" : live.ProgressSummary.Trim());
		}
		if (text3.Length > 0)
		{
			jsonObject2["latest_reply"] = ((text3.Length > 2000) ? text3.Substring(0, 2000) : text3);
		}
		List<(string, string)> list = RecentMessages(finished, live, 20);
		if (list.Count > 0)
		{
			jsonObject2["recent_messages"] = new JsonArray(list.Select<(string, string), JsonNode>(((string Role, string Text) m) => new JsonObject((JsonNodeOptions?)null)
			{
				["role"] = m.Role,
				["text"] = m.Text
			}).ToArray());
			jsonObject2["recent_messages_note"] = $"The last {list.Count} messages of this Clicky's chat, oldest first. role user = what the user sent it; role clicky = its replies; role clicky_progress = a live progress text from the run in flight.";
		}
		return ToolResults.Json(jsonObject2);
	}

	private async Task<JsonObject> JobResultPayloadAsync(string threadId, CancellationToken ct)
	{
		AgentJobInfo job = host.FindJob(threadId);
		if ((object)job == null)
		{
			return (JsonObject)JsonNode.Parse("{\"status\":\"unknown_job\",\"message\":\"No job with that codexThreadId: use one from the [Your jobs] note.\"}", null);
		}
		JsonObject payload = new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "ok",
			["codexThreadId"] = job.ThreadId ?? threadId,
			["job_title"] = job.Title,
			["state"] = job.State,
			["folder"] = job.FolderPath
		};
		JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
		foreach (AgentJobRunInfo run in job.Runs)
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["asked"] = ((run.Ask.Length > 400) ? run.Ask.Substring(0, 400) : run.Ask),
				["started"] = run.StartedAt.ToLocalTime().ToString("MMM d, HH:mm", CultureInfo.InvariantCulture),
				["outcome"] = run.Outcome
			};
			if (!string.IsNullOrEmpty(run.Summary))
			{
				jsonObject["summary"] = ((run.Summary.Length > 300) ? run.Summary.Substring(0, 300) : run.Summary);
			}
			jsonArray.Add(jsonObject);
		}
		if (jsonArray.Count > 0)
		{
			payload["runs"] = jsonArray;
		}
		AgentTaskInfo live = job.Live;
		if ((object)live != null && live.IsWorking)
		{
			payload["current_progress"] = (string.IsNullOrWhiteSpace(live.ProgressSummary) ? "working" : live.ProgressSummary.Trim());
		}
		string text = job.LatestReply.Trim();
		if (text.Length > 0)
		{
			payload["latest_reply"] = ((text.Length > 12000) ? (text.Substring(0, 12000) + "\n…(cut)") : text);
		}
		else if (!string.IsNullOrEmpty(job.Summary))
		{
			payload["latest_reply"] = job.Summary;
		}
		IReadOnlyList<(string Role, string Text)> finished = Array.Empty<(string, string)>();
		string threadId2 = job.ThreadId;
		if (threadId2 != null)
		{
			try
			{
				finished = await host.RecentMessagesAsync(threadId2, 12, ct).WaitAsync(TimeSpan.FromSeconds(4L), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
			{
				Log.Warn("Tools", "clicky_memory job thread: " + ex.Message);
			}
		}
		List<(string, string)> list = RecentMessages(finished, job.Live, 12);
		if (list.Count > 0)
		{
			payload["recent_messages"] = new JsonArray(list.Select<(string, string), JsonNode>(((string Role, string Text) m) => new JsonObject((JsonNodeOptions?)null)
			{
				["role"] = m.Role,
				["text"] = m.Text
			}).ToArray());
			payload["recent_messages_note"] = $"The last {list.Count} messages of this job's newest conversation, oldest first: role user = what the user sent it, clicky = its replies, clicky_progress = a live progress line.";
		}
		payload["note"] = "latest_reply is what the chat shows: speak to it in your own short words, never read it out. Its files: clicky_artifacts with this codexThreadId. A change goes to the job with sessions_send.";
		return payload;
	}

	internal static List<(string Role, string Text)> RecentMessages(IReadOnlyList<(string Role, string Text)> finished, AgentTaskInfo? live, int limit)
	{
		List<(string, string)> list = (from m in finished
			where !string.IsNullOrWhiteSpace(m.Text)
			select (Role: m.Role, Capped(m.Text))).ToList();
		if ((object)live != null && live.IsWorking)
		{
			string prompt = live.UserPrompts.LastOrDefault();
			if (prompt != null && prompt.Length > 0 && !list.Any<(string, string)>(((string Role, string) m) => m.Role == "user" && m.Item2 == Capped(prompt)))
			{
				list.Add(("user", Capped(prompt)));
			}
			foreach (string item in live.Commentary.Where((string c) => !string.IsNullOrWhiteSpace(c)))
			{
				list.Add(("clicky_progress", Capped(item)));
			}
			if (!string.IsNullOrWhiteSpace(live.StreamingResponse))
			{
				list.Add(("clicky", Capped(live.StreamingResponse) + " (still writing)"));
			}
		}
		return list.TakeLast(limit).ToList();
		static string Capped(string text)
		{
			string text2 = text.Trim();
			if (text2.Length <= 700)
			{
				return text2;
			}
			return text2.Substring(0, 700) + "…";
		}
	}

	private string Artifacts(ToolArgs args)
	{
		string text = args.Str("codexThreadId")?.Trim() ?? "";
		string text2 = SlugForJobToolArguments(args, text);
		if (text2.Length == 0 && text.Length > 0)
		{
			AgentJobInfo agentJobInfo = host.FindJob(text);
			if ((object)agentJobInfo == null)
			{
				return "{\"status\":\"unknown_job\",\"message\":\"No job with that codexThreadId: use one from the [Your jobs] note.\"}";
			}
			string jobFolder = agentJobInfo.FolderPath;
			List<string> named = (from p in agentJobInfo.ArtifactEntries.Concat(agentJobInfo.Live?.Artifacts ?? Array.Empty<string>())
				select AbsoluteArtifactPath(p, jobFolder)).ToList();
			JsonObject jsonObject = ArtifactsPayload(new JsonObject((JsonNodeOptions?)null)
			{
				["codexThreadId"] = agentJobInfo.ThreadId ?? "",
				["job_title"] = agentJobInfo.Title
			}, "\"" + agentJobInfo.Title + "\"", CollectArtifacts(named, Path.Combine(jobFolder, "output")), args.Str("file"));
			Log.Info("Tools", $"clicky_artifacts job thread={Tail(text)} status={jsonObject["status"]}");
			return ToolResults.Json(jsonObject);
		}
		AgentClickyInfo clicky = ((text2.Length == 0) ? null : host.FindClicky(text2));
		if ((object)clicky == null)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "unknown_clicky",
				["message"] = "No Clicky with that clicky_slug: use a slug from the [Your Clickys] note.",
				["known_slugs"] = new JsonArray(((IEnumerable<AgentClickyInfo>)host.Clickys).Select((Func<AgentClickyInfo, JsonNode>)((AgentClickyInfo c) => c.Slug)).ToArray())
			});
		}
		string workspace = host.WorkspacePath(clicky.Slug);
		List<Artifact> artifacts = CollectArtifacts((from p in host.Tasks.Where((AgentTaskInfo t) => t.ClickySlug == clicky.Slug).SelectMany((AgentTaskInfo t) => t.Artifacts)
			select AbsoluteArtifactPath(p, workspace)).ToList(), (workspace == null) ? null : Path.Combine(workspace, "output"));
		return ToolResults.Json(ArtifactsPayload(new JsonObject((JsonNodeOptions?)null)
		{
			["clicky_slug"] = clicky.Slug,
			["clicky_name"] = clicky.Name
		}, clicky.Name, artifacts, args.Str("file")));
	}

	private static JsonObject ArtifactsPayload(JsonObject owner, string ownerName, List<Artifact> artifacts, string? requestedFile)
	{
		owner["status"] = "ok";
		if (artifacts.Count == 0)
		{
			owner["files"] = new JsonArray((JsonNodeOptions?)null);
			owner["message"] = ownerName + " hasn't made any files yet. Its answers are in its replies (clicky_memory).";
			return owner;
		}
		string text = requestedFile?.Trim();
		if (text != null && text.Length > 0)
		{
			Artifact artifact = BestMatch(text, artifacts);
			if ((object)artifact == null)
			{
				owner["status"] = "not_found";
				owner["message"] = $"No file of {ownerName}'s matches \"{text}\".";
				owner["files"] = new JsonArray(((IEnumerable<Artifact>)artifacts).Select((Func<Artifact, JsonNode>)((Artifact a) => Summary(a))).ToArray());
				return owner;
			}
			JsonObject jsonObject = Summary(artifact);
			if (!artifact.IsLink)
			{
				(string, bool)? tuple = ReadText(artifact.Path, 12000);
				if (tuple.HasValue)
				{
					(string, bool) valueOrDefault = tuple.GetValueOrDefault();
					jsonObject["text"] = valueOrDefault.Item1;
					if (valueOrDefault.Item2)
					{
						jsonObject["truncated"] = true;
					}
				}
				else
				{
					jsonObject["text_note"] = "This kind of file has no readable text (an image, an app, a binary). Open it with open_artifact to show it.";
				}
			}
			owner["file"] = jsonObject;
			return owner;
		}
		JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
		int num = 0;
		foreach (Artifact artifact2 in artifacts)
		{
			JsonObject jsonObject2 = Summary(artifact2);
			if (!artifact2.IsLink && num < 3)
			{
				(string, bool)? tuple = ReadText(artifact2.Path, 1200);
				if (tuple.HasValue)
				{
					(string, bool) valueOrDefault2 = tuple.GetValueOrDefault();
					num++;
					jsonObject2["opening_text"] = valueOrDefault2.Item1;
					if (valueOrDefault2.Item2)
					{
						jsonObject2["more_text"] = true;
					}
				}
			}
			jsonArray.Add(jsonObject2);
		}
		owner["files"] = jsonArray;
		owner["note"] = "Newest first. Pass file (a name or path) to read one in full; open one with open_artifact (path); copy its text with copy_to_clipboard.";
		return owner;
	}

	private static List<Artifact> CollectArtifacts(IEnumerable<string> named, string? outputFolder)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Artifact> list = new List<Artifact>();
		List<Artifact> list2 = new List<Artifact>();
		List<string> second = new List<string>();
		if (outputFolder != null && Directory.Exists(outputFolder))
		{
			try
			{
				second = (from e in (from e in new DirectoryInfo(outputFolder).EnumerateFileSystemInfos("*", new EnumerationOptions
						{
							RecurseSubdirectories = true,
							MaxRecursionDepth = 2
						})
						where e is FileInfo
						orderby e.LastWriteTimeUtc descending
						select e).Take(12)
					select e.FullName).ToList();
			}
			catch
			{
			}
		}
		foreach (string item in named.Concat(second))
		{
			string text = item.Trim();
			if (text.Length != 0 && hashSet.Add(text))
			{
				if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(new Artifact(text, IsLink: true, null, null));
				}
				else if (File.Exists(text))
				{
					FileInfo fileInfo = new FileInfo(text);
					list.Add(new Artifact(text, IsLink: false, fileInfo.Length, fileInfo.LastWriteTime));
				}
				else if (Directory.Exists(text))
				{
					list.Add(new Artifact(text, IsLink: false, null, new DirectoryInfo(text).LastWriteTime));
				}
			}
		}
		return list.OrderByDescending((Artifact f) => f.Modified ?? DateTimeOffset.MinValue).Concat(list2).Take(8)
			.ToList();
	}

	private static Artifact? BestMatch(string requested, List<Artifact> artifacts)
	{
		Artifact artifact = artifacts.FirstOrDefault((Artifact a) => string.Equals(a.Path, requested, StringComparison.OrdinalIgnoreCase));
		if ((object)artifact != null)
		{
			return artifact;
		}
		string text = requested.ToLowerInvariant();
		string requestedName = Path.GetFileName(text.TrimEnd('\\', '/'));
		Artifact artifact2 = artifacts.FirstOrDefault((Artifact a) => Name(a) == requestedName);
		if ((object)artifact2 != null)
		{
			return artifact2;
		}
		List<string> words = (from w in new string(text.Select((char c) => (!char.IsLetterOrDigit(c)) ? ' ' : c).ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries)
			where w.Length > 1
			select w).ToList();
		if (words.Count == 0)
		{
			return null;
		}
		return artifacts.FirstOrDefault((Artifact a) => words.All((string w) => Name(a).Contains(w)));
		static string Name(Artifact a)
		{
			return Path.GetFileName(a.Path.TrimEnd('\\', '/')).ToLowerInvariant();
		}
	}

	private static JsonObject Summary(Artifact artifact)
	{
		if (artifact.IsLink)
		{
			return new JsonObject((JsonNodeOptions?)null)
			{
				["name"] = artifact.Path,
				["kind"] = "link",
				["path"] = artifact.Path
			};
		}
		string text = Path.GetExtension(artifact.Path).TrimStart('.').ToLowerInvariant();
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		jsonObject["name"] = Path.GetFileName(artifact.Path.TrimEnd('\\', '/'));
		jsonObject["kind"] = ((text.Length == 0) ? "folder or file" : text);
		jsonObject["path"] = artifact.Path;
		JsonObject jsonObject2 = jsonObject;
		long? bytes = artifact.Bytes;
		if (bytes.HasValue)
		{
			long valueOrDefault = bytes.GetValueOrDefault();
			jsonObject2["size"] = Attachments.FormatBytes(valueOrDefault);
		}
		DateTimeOffset? modified = artifact.Modified;
		if (modified.HasValue)
		{
			jsonObject2["modified"] = modified.GetValueOrDefault().ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture);
		}
		return jsonObject2;
	}

	private static (string Text, bool Truncated)? ReadText(string path, int limit)
	{
		PageContext pageContext = DocumentText.Extract(path);
		string text = pageContext.Text?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string text2 = Path.GetExtension(path).ToLowerInvariant();
		if ((text2 == ".html" || text2 == ".htm") ? true : false)
		{
			text = DocumentText.TextWithoutMarkup(text);
		}
		if (text.Length == 0)
		{
			return null;
		}
		bool item = text.Length > limit || pageContext.IsTruncated;
		return ((text.Length > limit) ? (text.Substring(0, limit) + "…") : text, item);
	}

	private List<ArtifactCandidate> ArtifactCandidates()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<ArtifactCandidate> list = new List<ArtifactCandidate>();
		foreach (AgentTaskInfo task in host.Tasks)
		{
			object obj = task.WorkspacePath;
			if (obj == null)
			{
				string clickySlug = task.ClickySlug;
				obj = ((clickySlug != null) ? host.WorkspacePath(clickySlug) : null);
			}
			string workspace = (string)obj;
			foreach (string artifact in task.Artifacts)
			{
				string text = AbsoluteArtifactPath(artifact, workspace);
				if (text.Length > 0 && hashSet.Add(text))
				{
					list.Add(new ArtifactCandidate(task.ThreadId, ArtifactTitle(text), text, ArtifactKind(text), task.StartedAt));
				}
			}
		}
		return list;
	}

	private string ArtifactAction(ToolArgs args, bool reveal)
	{
		string text = (reveal ? "reveal_artifact" : "open_artifact");
		ArtifactCandidate artifactCandidate = ResolveArtifact(args);
		if ((object)artifactCandidate == null)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "not_found",
				["summary"] = "No generated artifact matched that request.",
				["toolName"] = text
			});
		}
		bool flag;
		if (artifactCandidate.Uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || artifactCandidate.Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			flag = !reveal && host.OpenPath(artifactCandidate.Uri);
		}
		else
		{
			Uri result;
			string text2 = ((artifactCandidate.Uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(artifactCandidate.Uri, UriKind.Absolute, out result)) ? result.LocalPath : artifactCandidate.Uri);
			bool flag2 = File.Exists(text2) || Directory.Exists(text2);
			if (!reveal && flag2)
			{
				flag = host.OpenPath(text2);
			}
			else if (flag2)
			{
				flag = host.RevealPath(text2);
			}
			else
			{
				string text3 = NearestExistingFolder(text2);
				flag = text3 != null && host.OpenPath(text3);
			}
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["title"] = artifactCandidate.Title,
			["uri"] = artifactCandidate.Uri,
			["kind"] = artifactCandidate.Kind,
			["opened"] = flag
		};
		if (artifactCandidate.ThreadId != null)
		{
			jsonObject["threadId"] = artifactCandidate.ThreadId;
		}
		string text4 = (reveal ? "Revealed" : "Opened");
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = (flag ? "completed" : "failed"),
			["summary"] = (flag ? (text4 + " " + artifactCandidate.Title + ".") : $"Could not {(reveal ? "reveal" : "open")} {artifactCandidate.Title}."),
			["toolName"] = text,
			["result"] = jsonObject
		});
	}

	private ArtifactCandidate? ResolveArtifact(ToolArgs args)
	{
		string threadId = args.Str("codexThreadId", "threadId");
		string text = args.Str("uri", "url", "path");
		if (text != null)
		{
			string uri = AbsoluteArtifactPath(text, null);
			return new ArtifactCandidate(threadId, ArtifactTitle(uri), uri, ArtifactKind(uri), DateTimeOffset.Now);
		}
		string text2 = args.Str("artifactId", "id");
		if (text2 != null && (Path.IsPathFullyQualified(text2) || text2.StartsWith('~') || text2.Contains("://", StringComparison.Ordinal)))
		{
			string uri2 = AbsoluteArtifactPath(text2, null);
			return new ArtifactCandidate(threadId, ArtifactTitle(uri2), uri2, ArtifactKind(uri2), DateTimeOffset.Now);
		}
		string text3 = args.Str("kind");
		string kind = ((text3 != null) ? NormalizedKind(text3) : null);
		List<ArtifactCandidate> source = ArtifactCandidates();
		List<ArtifactCandidate> list = source.Where((ArtifactCandidate c) => (threadId == null || c.ThreadId == threadId) && (kind == null || c.Kind == kind)).ToList();
		if (list.Count == 0 && (threadId != null || kind != null))
		{
			list = source.Where((ArtifactCandidate c) => threadId == null || c.ThreadId == threadId).ToList();
		}
		return list.OrderByDescending(OpenScore).ThenByDescending((ArtifactCandidate c) => c.UpdatedAt).FirstOrDefault();
	}

	private string AbsoluteArtifactPath(string raw, string? workspace)
	{
		string text = raw.Trim();
		if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		if (text.Length == 0)
		{
			return "";
		}
		try
		{
			string text2 = LocalRootTools.ResolvePath(text, workspace ?? host.ProjectsRoot);
			if (!Path.IsPathFullyQualified(text.Replace('/', '\\')) && workspace != null && !File.Exists(text2) && !Directory.Exists(text2))
			{
				return LocalRootTools.ResolvePath(text, host.ProjectsRoot);
			}
			return text2;
		}
		catch
		{
			return text;
		}
	}

	private static string ArtifactTitle(string uri)
	{
		Uri result;
		bool flag = Uri.TryCreate(uri, UriKind.Absolute, out result);
		if (flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "http" || scheme == "https") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			string text = result.Segments.LastOrDefault()?.Trim('/') ?? "";
			if (text.Length <= 0)
			{
				return result.Host;
			}
			return Uri.UnescapeDataString(text);
		}
		string fileName = Path.GetFileName(uri.TrimEnd('\\', '/'));
		if (fileName.Length != 0)
		{
			return fileName;
		}
		return uri;
	}

	private static string ArtifactKind(string uri)
	{
		if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return "url";
		}
		switch (Path.GetExtension(uri).TrimStart('.').ToLowerInvariant())
		{
		case "html":
		case "docx":
		case "htm":
		case "pdf":
		case "txt":
		case "rtf":
		case "doc":
		case "md":
		case "markdown":
			return "report";
		case "jpeg":
		case "tiff":
		case "heic":
		case "webp":
		case "png":
		case "jpg":
		case "gif":
			return "screenshot";
		default:
			return "file";
		}
	}

	private static string NormalizedKind(string raw)
	{
		switch (raw.Trim().ToLowerInvariant())
		{
		case "url":
		case "link":
			return "url";
		case "pdf":
		case "doc":
		case "document":
		case "markdown":
		case "report":
			return "report";
		case "image":
		case "screenshot":
			return "screenshot";
		case "app":
			return "app";
		case "other":
			return "other";
		default:
			return "file";
		}
	}

	private static int OpenScore(ArtifactCandidate artifact)
	{
		string text = artifact.Uri.ToLowerInvariant();
		string? fileName = Path.GetFileName(text);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		string text2 = Path.GetExtension(fileName).TrimStart('.');
		int num;
		switch (artifact.Kind)
		{
		case "url":
			num = 180;
			break;
		case "report":
		case "screenshot":
			num = 90;
			break;
		case "file":
			num = 70;
			break;
		default:
			num = 20;
			break;
		}
		int num2 = num;
		if (text.Contains("127.0.0.1") || text.Contains("localhost"))
		{
			num2 += 80;
		}
		num = num2;
		num2 = num + fileNameWithoutExtension switch
		{
			"index" => 70, 
			"main" => 50, 
			"readme" => 30, 
			_ => 0, 
		};
		int num3 = num2;
		switch (text2)
		{
		case "html":
		case "htm":
			num = 45;
			break;
		case "pdf":
			num = 42;
			break;
		case "docx":
		case "doc":
		case "txt":
		case "rtf":
		case "md":
		case "markdown":
			num = 36;
			break;
		case "jpeg":
		case "tiff":
		case "heic":
		case "webp":
		case "png":
		case "jpg":
		case "gif":
			num = 32;
			break;
		case "xlsx":
		case "tsv":
		case "csv":
			num = 24;
			break;
		default:
			num = 5;
			break;
		}
		return num3 + num;
	}

	private static string? NearestExistingFolder(string path)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			while (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				directoryName = Path.GetDirectoryName(directoryName);
			}
			return string.IsNullOrEmpty(directoryName) ? null : directoryName;
		}
		catch
		{
			return null;
		}
	}

	private string Dashboard(ToolArgs args)
	{
		if (args.Str("action")?.ToLowerInvariant() == "close")
		{
			host.CloseHome();
			return "{\"status\":\"ok\",\"dashboard\":\"closed\"}";
		}
		string threadId = args.Str("codexThreadId");
		host.OpenHome(threadId);
		return "{\"status\":\"ok\",\"dashboard\":\"open\"}";
	}

	private string SkipInterview()
	{
		bool flag = host.SkipOnboardingInterview();
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = (flag ? "ok" : "not_in_interview"),
			["note"] = (flag ? "The interview is over. Say nothing more about it; the tour moves on by itself." : "The user is not in the onboarding interview right now, so there is nothing to skip.")
		});
	}
}
