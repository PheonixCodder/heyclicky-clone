using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Turns;
using HeyClicky.App.Dock;
using HeyClicky.Core.Browser;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Tools;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Browser;

public sealed class ChromeSetupFlow
{
	public enum Choice
	{
		DoItForMe,
		ShowMeHow,
		NotNow
	}

	public sealed record Target(string? JobId, string? ThreadId)
	{
		public static readonly Target None = new Target(null, null);

		[CompilerGenerated]
		private Target(Target original)
		{
			JobId = original.JobId;
			ThreadId = original.ThreadId;
		}
	}

	private bool isShowingHow;

	private bool isSettingUpForThem;

	private bool isPreview;

	private AppHost? app;

	private readonly Dictionary<string, Action<bool>> heldStarts = new Dictionary<string, Action<bool>>();

	private readonly HashSet<string> askedKeys = new HashSet<string>();

	private CancellationTokenSource? showHowRun;

	private readonly HashSet<string> skippedThreads = new HashSet<string>();

	public const string WithoutChromeSteer = "HeyClicky is passing on the user's answer to your Chrome question: they'd rather not connect Chrome now. Carry on without it: read public pages by fetching them or searching the web, and for a step on a website they're signed into, call mac_step (HeyClicky does it on their screen while they watch). What only their login shows (their home feed, inbox, cart, orders, settings) isn't on any public page, so a search or a guessed account won't find it: get that part with mac_step. Anything public, their own public posts included, search or fetch as usual. If your work is already done, there's nothing more to do.";

	public const string WithoutChromeMessage = "I'd rather not connect Chrome now. Carry on without it: read public pages by fetching them or searching the web, and for each step on a website I'm signed into, call mac_step and HeyClicky does it on my screen while I watch. What only my login shows (my home feed, inbox, cart, orders, settings) isn't on any public page, so a search or a guessed account won't find it: get that part with mac_step. Anything public, my own public posts included, search or fetch as usual.";

	public const string ChromeConnectedSteer = "HeyClicky is passing on news about your Chrome question: their Chrome is connected now, so the browser tool works. Carry on with the website part. If your work is already done, there's nothing more to do.";

	public const string ChromeConnectedMessage = "Chrome is connected now. Carry on with the task.";

	public const string ChromeAnswerLaunchContext = "HeyClicky is passing on the user's answer to your Chrome question (they chose it on HeyClicky's card; they didn't type this message).";

	public const string HelperContextWithoutChrome = "They chose not to connect Chrome for this job: don't ask about Chrome again. For a step on a website they're signed into, call mac_step and HeyClicky does it on their screen while they watch.";

	public static ChromeSetupFlow Shared { get; } = new ChromeSetupFlow();

	public IChromeClickyDrive? Drive { get; set; }

	public IChromeSetupJobs? Jobs { get; set; }

	public bool IsShowingHow => isShowingHow;

	public bool IsSettingUpForThem => isSettingUpForThem;

	public bool IsBusy
	{
		get
		{
			if (!isShowingHow && !isSettingUpForThem)
			{
				return ChromeAutoInstall.IsRunning;
			}
			return true;
		}
	}

	public bool AddsAgain => Bridge.HasOldCopyToRemove;

	public bool CanDoItForThem
	{
		get
		{
			if (AddsAgain)
			{
				return Drive?.IsAvailable ?? false;
			}
			return false;
		}
	}

	public string BrowserName => Bridge.SetupBrowserName;

	private ClickyBrowserBridge Bridge => ClickyBrowserBridge.Shared;

	private AgentService? Agents => app?.Real?.Agents;

	private DispatcherQueue? Ui => app?.DispatcherQueue;

	public bool HoldsNewWebsiteJobs
	{
		get
		{
			if (!Bridge.IsConnected)
			{
				return ChromiumBrowsers.InstalledNames.Count > 0;
			}
			return false;
		}
	}

	public string AnswerMeaningsForBrain
	{
		get
		{
			if (!CanDoItForThem)
			{
				return "approve or show_how = they add it from the Chrome Web Store with you showing them each click";
			}
			return "approve = you remove the old one and add the new version on their screen, show_how = they do it with you showing them each step";
		}
	}

	public bool HasWaitingJob
	{
		get
		{
			if ((Jobs?.HeldJobIdsNewestFirst().Count ?? 0) <= 0)
			{
				return (Agents?.ThreadsNeedingChrome().Count ?? 0) > 0;
			}
			return true;
		}
	}

	public event Action? Changed;

	public static string Raw(Choice choice)
	{
		return choice switch
		{
			Choice.DoItForMe => "do_it_for_me", 
			Choice.ShowMeHow => "show_me_how", 
			_ => "not_now", 
		};
	}

	public static Choice? Parse(string? raw)
	{
		return raw switch
		{
			"do_it_for_me" => Choice.DoItForMe, 
			"show_me_how" => Choice.ShowMeHow, 
			"not_now" => Choice.NotNow, 
			_ => null, 
		};
	}

	private ChromeSetupFlow()
	{
	}

	private void OnUi(Action action)
	{
		Ui?.TryEnqueue(delegate
		{
			action();
		});
	}

	private void Notify()
	{
		this.Changed?.Invoke();
	}

	public void Attach(AppHost host)
	{
		if (app != null)
		{
			return;
		}
		app = host;
		CardFooterRequest.ChromeSetup = () => (BrowserName: BrowserName, AddsAgain: AddsAgain);
		AgentToolHooks.ChromeSetupBrowserName = () => BrowserName;
		bool wasConnected = Bridge.IsConnected;
		Bridge.ConnectionChanged += delegate
		{
			bool isConnected = Bridge.IsConnected;
			if (isConnected != wasConnected)
			{
				wasConnected = isConnected;
				if (isConnected)
				{
					OnUi(ChromeConnected);
				}
			}
		};
	}

	public void Hold(string jobId, Action<bool> start)
	{
		Bridge.RefreshSetupBrowserName();
		heldStarts[jobId] = start;
		askedKeys.Add(jobId);
		Jobs?.SetWaitsForChrome(jobId, waits: true);
		Jobs?.PresentNeedsYou(jobId);
		ClickyAnalytics.TrackChromeJobAction("chrome_choice_asked", null, new JsonObject((JsonNodeOptions?)null)
		{
			["when"] = "before_start",
			["adds_again"] = AddsAgain
		});
		Notify();
	}

	public void JobNeedsChrome(string threadId)
	{
		AgentService agents = Agents;
		if (agents != null && agents.TaskIdForThread(threadId) != null)
		{
			Bridge.RefreshSetupBrowserName();
			string item = Jobs?.JobIdForThread(threadId) ?? threadId;
			if (!askedKeys.Contains(item) && !IsBusy)
			{
				askedKeys.Add(item);
				app?.AnnounceJobNeedsChrome(threadId);
				ClickyAnalytics.TrackChromeJobAction("chrome_choice_asked", threadId, new JsonObject((JsonNodeOptions?)null)
				{
					["when"] = "while_working",
					["adds_again"] = AddsAgain
				});
				Notify();
			}
		}
	}

	public void TurnEndedWithoutChrome(string threadId)
	{
		askedKeys.Remove(Jobs?.JobIdForThread(threadId) ?? threadId);
	}

	public Target? TargetFor(string? threadId)
	{
		AgentService agents = Agents;
		if (agents == null)
		{
			return null;
		}
		if (threadId != null && agents.NeedsChromeConnection(threadId))
		{
			return new Target(Jobs?.JobIdForThread(threadId), threadId);
		}
		string text = Jobs?.HeldJobIdsNewestFirst().FirstOrDefault();
		if (text != null)
		{
			return new Target(text, null);
		}
		string text2 = agents.ThreadsNeedingChrome().LastOrDefault();
		if (text2 != null)
		{
			return new Target(Jobs?.JobIdForThread(text2), text2);
		}
		return null;
	}

	public void Choose(Choice choice, Target target)
	{
		if (app == null)
		{
			return;
		}
		if (choice == Choice.DoItForMe && !CanDoItForThem)
		{
			choice = Choice.ShowMeHow;
		}
		ClickyAnalytics.TrackChromeJobAction("chrome_choice", target.ThreadId, new JsonObject((JsonNodeOptions?)null)
		{
			["choice"] = Raw(choice),
			["for_job"] = target.JobId != null || target.ThreadId != null,
			["adds_again"] = AddsAgain
		});
		app.DismissCurrentNotchCard();
		if (choice != Choice.ShowMeHow && isShowingHow)
		{
			app.ClearDrawing();
			StopShowingHow();
		}
		switch (choice)
		{
		case Choice.DoItForMe:
			if (Bridge.IsConnected && !AddsAgain)
			{
				ChromeConnected();
				break;
			}
			isSettingUpForThem = true;
			Notify();
			ChromeAutoInstall.InstallByDriving(this, target.ThreadId, evenIfConnected: false, delegate(bool connected, bool stoppedByUser)
			{
				OnUi(delegate
				{
					isSettingUpForThem = false;
					Notify();
					if (connected)
					{
						if (AddsAgain)
						{
							ShowHowToRemoveOldCopy();
						}
						CarryOnAfterConnecting();
					}
					else if (stoppedByUser)
					{
						OfferAgain(target);
					}
					else
					{
						ShowHow(afterDrive: true);
					}
				});
			});
			break;
		case Choice.ShowMeHow:
			if (Bridge.IsConnected && !AddsAgain)
			{
				ChromeConnected();
			}
			else
			{
				ShowHow(afterDrive: false);
			}
			break;
		case Choice.NotNow:
			GoWithoutChrome(target);
			break;
		}
	}

	public void PreviewShowMeHow()
	{
		isPreview = true;
		ShowHow(afterDrive: false);
	}

	public void PreviewHeldJob()
	{
		if (app == null)
		{
			return;
		}
		string jobId = Jobs?.CreatePreviewJob("Posting the song to Discord", "Post my song in the HeyClicky Discord's #general.");
		if (jobId == null)
		{
			Log.Info("Browser", "preview held job: jobs aren't ported on Windows yet; saying the line only");
			app.AnnounceLineAsync(NeedsChromeLine("Posting the song to Discord"));
			return;
		}
		Hold(jobId, delegate
		{
			Jobs?.Dismiss(jobId);
		});
		app.AnnounceLineAsync(NeedsChromeLine("Posting the song to Discord"));
	}

	public string NeedsChromeLine(string? jobName)
	{
		return ClickyNotes.NeedsChromeLine(string.IsNullOrWhiteSpace(jobName) ? null : jobName.Trim(), BrowserName, AddsAgain, CanDoItForThem);
	}

	private void OfferAgain(Target target)
	{
		string jobId = target.JobId;
		if (jobId != null)
		{
			IChromeSetupJobs? jobs = Jobs;
			if (jobs != null && jobs.IsHeldForChrome(jobId))
			{
				Jobs.PresentNeedsYou(jobId);
				return;
			}
		}
		string threadId = target.ThreadId;
		if (threadId != null)
		{
			app?.PresentTaskNeedsYou(threadId);
		}
	}

	public void Stop()
	{
		Drive?.HandleEscape();
		StopShowingHowForEscape();
	}

	public bool StopShowingHowForEscape()
	{
		if (!isShowingHow)
		{
			return false;
		}
		app?.ClearDrawing();
		StopShowingHow();
		return true;
	}

	public void ShowHow(bool afterDrive)
	{
		if (app != null)
		{
			isShowingHow = true;
			Notify();
			showHowRun?.Cancel();
			ShowHowAsync(afterDrive, (showHowRun = new CancellationTokenSource()).Token);
		}
	}

	private async Task ShowHowAsync(bool afterDrive, CancellationToken ct)
	{
		bool registered = await Bridge.ConnectAsync();
		await OnUiAsync(delegate
		{
		});
		if (Bridge.IsConnected && !isPreview)
		{
			OnUi(ChromeConnected);
			return;
		}
		if (!registered && !isPreview)
		{
			OnUi(StopShowingHow);
			return;
		}
		if (isPreview)
		{
			Bridge.OpenStoreListing();
		}
		try
		{
			await Task.Delay(1200, ct);
		}
		catch
		{
			return;
		}
		bool flag = !isShowingHow;
		if (!flag)
		{
			flag = !(await WaitForQuietVoiceAsync(ct));
		}
		if (!flag)
		{
			app.StartAppFactTurn(ChromeSetupBriefs.ShowHowFact(BrowserName, AddsAgain, afterDrive));
		}
	}

	public void ShowHowToRemoveOldCopy()
	{
		if (app != null)
		{
			isShowingHow = true;
			Bridge.SetHasOldCopyToRemove(value: false);
			Notify();
			showHowRun?.Cancel();
			RemoveOldCopyAsync((showHowRun = new CancellationTokenSource()).Token);
		}
		async Task RemoveOldCopyAsync(CancellationToken ct)
		{
			await Bridge.OpenExtensionsPageAsync();
			try
			{
				await Task.Delay(1200, ct);
			}
			catch
			{
				return;
			}
			if (isShowingHow)
			{
				await OnUiAsync(delegate
				{
					isShowingHow = false;
					Notify();
				});
				if (await WaitForQuietVoiceAsync(ct))
				{
					app.StartAppFactTurn(ChromeSetupBriefs.RemoveOldCopyFact(BrowserName));
				}
			}
		}
	}

	private async Task<bool> WaitForQuietVoiceAsync(CancellationToken ct)
	{
		for (int i = 0; i < 30; i++)
		{
			if (app.VoiceState == VoiceState.Idle)
			{
				break;
			}
			if (app.VoiceState == VoiceState.Listening)
			{
				return false;
			}
			try
			{
				await Task.Delay(500, ct);
			}
			catch
			{
				return false;
			}
		}
		return app.VoiceState == VoiceState.Idle;
	}

	private Task OnUiAsync(Action action)
	{
		TaskCompletionSource done = new TaskCompletionSource();
		DispatcherQueue ui = Ui;
		if ((object)ui == null || !ui.TryEnqueue(delegate
		{
			try
			{
				action();
			}
			finally
			{
				done.TrySetResult();
			}
		}))
		{
			done.TrySetResult();
		}
		return done.Task;
	}

	private void StopShowingHow()
	{
		isShowingHow = false;
		isPreview = false;
		showHowRun?.Cancel();
		Notify();
	}

	private void ChromeConnected()
	{
		if (app == null || isPreview || isSettingUpForThem || ChromeAutoInstall.IsRunning)
		{
			return;
		}
		bool flag = isShowingHow;
		if (flag)
		{
			app.ClearDrawing();
		}
		StopShowingHow();
		if (AddsAgain)
		{
			if (flag)
			{
				ShowHowToRemoveOldCopy();
			}
			else
			{
				Bridge.SetHasOldCopyToRemove(value: false);
			}
		}
		CarryOnAfterConnecting();
	}

	private void CarryOnAfterConnecting()
	{
		if (app == null)
		{
			return;
		}
		foreach (string item in Jobs?.HeldJobIdsNewestFirst() ?? Array.Empty<string>())
		{
			Start(item, withoutChrome: false);
		}
		foreach (string item2 in Agents?.ThreadsNeedingChrome() ?? Array.Empty<string>())
		{
			Agents.ClearNeedsChromeConnection(item2);
			PassAnswer("HeyClicky is passing on news about your Chrome question: their Chrome is connected now, so the browser tool works. Carry on with the website part. If your work is already done, there's nothing more to do.", "Chrome is connected now. Carry on with the task.", item2);
		}
		askedKeys.Clear();
	}

	private void GoWithoutChrome(Target target)
	{
		string jobId = target.JobId;
		if (jobId != null)
		{
			IChromeSetupJobs? jobs = Jobs;
			if (jobs != null && jobs.IsHeldForChrome(jobId))
			{
				Jobs.SetSkipsChrome(jobId);
				Start(jobId, withoutChrome: true);
				return;
			}
		}
		string threadId = target.ThreadId;
		if (threadId != null)
		{
			string text = target.JobId ?? Jobs?.JobIdForThread(threadId);
			if (text != null)
			{
				Jobs?.SetSkipsChrome(text);
			}
			else
			{
				skippedThreads.Add(threadId);
			}
			Agents?.ClearNeedsChromeConnection(threadId);
			PassAnswer("HeyClicky is passing on the user's answer to your Chrome question: they'd rather not connect Chrome now. Carry on without it: read public pages by fetching them or searching the web, and for a step on a website they're signed into, call mac_step (HeyClicky does it on their screen while they watch). What only their login shows (their home feed, inbox, cart, orders, settings) isn't on any public page, so a search or a guessed account won't find it: get that part with mac_step. Anything public, their own public posts included, search or fetch as usual. If your work is already done, there's nothing more to do.", "I'd rather not connect Chrome now. Carry on without it: read public pages by fetching them or searching the web, and for each step on a website I'm signed into, call mac_step and HeyClicky does it on my screen while I watch. What only my login shows (my home feed, inbox, cart, orders, settings) isn't on any public page, so a search or a guessed account won't find it: get that part with mac_step. Anything public, my own public posts included, search or fetch as usual.", threadId);
		}
	}

	private void Start(string jobId, bool withoutChrome)
	{
		Jobs?.SetWaitsForChrome(jobId, waits: false);
		if (heldStarts.Remove(jobId, out Action<bool> value))
		{
			value(withoutChrome);
		}
		else
		{
			Jobs?.StartFromSavedPrompt(jobId, withoutChrome);
		}
	}

	private void PassAnswer(string steer, string message, string threadId)
	{
		AgentService agents = Agents;
		if (agents != null)
		{
			PassAsync();
		}
		async Task PassAsync()
		{
			_ = 2;
			try
			{
				if (!(await agents.PassInformationToRunningTurnAsync(threadId, steer).ConfigureAwait(continueOnCapturedContext: false)))
				{
					int waited = 0;
					while (agents.IsThreadRunning(threadId) && waited < 60)
					{
						await Task.Delay(250).ConfigureAwait(continueOnCapturedContext: false);
						waited++;
					}
					string text = agents.TaskIdForThread(threadId);
					if (text != null)
					{
						AgentTaskDetail taskDetail = agents.GetTaskDetail(text);
						if ((object)taskDetail != null && taskDetail.Snapshot.Status == AgentTaskStatus.Finished && ChromeTurnEnd.HelperStoppedForChrome(taskDetail.Snapshot.FinalResponseMarkdown, taskDetail.Summary))
						{
							await agents.SubmitThreadFollowUpAsync(threadId, message, null, default(CancellationToken), "HeyClicky is passing on the user's answer to your Chrome question (they chose it on HeyClicky's card; they didn't type this message).").ConfigureAwait(continueOnCapturedContext: false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Browser", "Chrome answer for " + threadId + ": " + ex.Message);
			}
		}
	}

	public bool SkipsChrome(string threadId)
	{
		if (!skippedThreads.Contains(threadId))
		{
			string text = Jobs?.JobIdForThread(threadId);
			if (text != null)
			{
				return Jobs.SkipsChrome(text);
			}
			return false;
		}
		return true;
	}
}
