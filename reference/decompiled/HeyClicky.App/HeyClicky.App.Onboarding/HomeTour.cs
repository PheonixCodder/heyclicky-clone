using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Portraits;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Onboarding;

public sealed class HomeTour
{
	private sealed record Beat(string Key, string Brief, string Fallback, Action? OnAudioBegins);

	public const string OpenerWords = "Okay, check this out. So, I live in your PC's notch. It looks like this.";

	private const string NarrationRules = "[Home tour] You are showing this person their new Clickys, one beat at a time, while their Home is open in the PC's notch. Each \"[Home tour beat]\" gives only the facts for that beat. Use your own warm, plain words and their interview context, but add no extra explanation, recap, promises, or transition sentence. Follow each beat's word limit; otherwise use at most 25 words in one or two short sentences, then stop. Say each Clicky's name exactly. A Clicky is an AI agent that does work for them; explain this only in the introduction. After that, call them Clickys. The personalized Clickys are the focus: keep the defaults brief. Do not read instructions aloud or mention beats or tour mechanics. No lists, dashes, or questions. Finish with the short goodbye you are given.";

	private const int MaximumClickyCount = 3;

	private const double LineWritingDeadlineSeconds = 25.0;

	private const int LineIndexBase = 4000;

	private readonly AppHost host;

	private readonly InterviewController? interview;

	private readonly DispatcherQueue dq;

	private readonly CancellationTokenSource abort = new CancellationTokenSource();

	private readonly Dictionary<int, TaskCompletionSource<double>> audioQueued = new Dictionary<int, TaskCompletionSource<double>>();

	private readonly object gate = new object();

	private int nextLine;

	private bool holdingNotch;

	private long noVoiceUntil;

	public bool IsRunning { get; private set; }

	public bool WasAborted { get; private set; }

	public bool IsDebugPreview { get; init; }

	private NotchPresenter? Presenter => host.Services.Presenter as NotchPresenter;

	public HomeTour(AppHost host, InterviewController? interview)
	{
		this.host = host;
		this.interview = interview;
		dq = DispatcherQueue.GetForCurrentThread();
	}

	public void AbortForEscape()
	{
		if (IsRunning && !WasAborted)
		{
			WasAborted = true;
			Log.Info("tour", "reveal aborted (double Escape)");
			abort.Cancel();
			host.Real?.Companion.Mouth.Interrupt();
			FoldNotch();
			Presenter?.DismissNotchAgentPeek();
		}
	}

	private void OpenNotch()
	{
		Sounds.Play("home-reveal", 0.7);
		NotchPresenter presenter = Presenter;
		if (presenter != null)
		{
			presenter.Tabs.Select(NotchTab.Home);
			presenter.OpenNotchAgentPeekOnRequest();
			presenter.SetNotchHeldOpenForPresentation(held: true);
			holdingNotch = true;
			Log.Info("tour", "reveal: notch opened on Home; introducing");
		}
	}

	private void FoldNotch()
	{
		if (holdingNotch)
		{
			holdingNotch = false;
			Presenter?.SetNotchHeldOpenForPresentation(held: false);
			Presenter?.DismissNotchAgentPeek();
		}
	}

	public async Task<bool> RunAsync()
	{
		IsRunning = true;
		CancellationToken ct = abort.Token;
		ClickyRoster roster = ClickyRoster.Shared;
		List<string> founded = (interview?.FoundedSlugs ?? Array.Empty<string>()).Where((string s) => (object)roster.Clicky(s) != null).TakeLast(3).ToList();
		if (founded.Count == 0 && IsDebugPreview)
		{
			founded = (from c in (from c in roster.Clickys
					where !StarterCast.IsCast(c.Slug)
					orderby c.CreatedAt descending
					select c).Take(3).Reverse()
				select c.Slug).ToList();
		}
		List<string> list = StarterCast.PresentSlugs().ToList();
		int num = host.Proactive?.VisibleCards.Count((ProactiveSuggestionCard c) => c.CheckReason == ProactiveCheckReason.Onboarding) ?? 0;
		if (num == 0)
		{
			num = host.Proactive?.VisibleCards.Count ?? roster.StarterCards.Count;
		}
		List<RosterClicky> foundedClickys = founded.Select((string s) => roster.Clicky(s)).ToList();
		List<RosterClicky> castClickys = list.Select((string s) => roster.Clicky(s)).ToList();
		Log.Info("tour", $"reveal: cast [{string.Join(", ", list)}], founded [{string.Join(", ", founded)}], {num} cards");
		if (founded.Count == 0 && num == 0 && list.Count == 0)
		{
			IsRunning = false;
			return true;
		}
		long analyticsStartedAt = Environment.TickCount64;
		string interviewId = interview?.InterviewId ?? "";
		ClickyAnalytics.TrackProductEvent("onboarding_reveal_started", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["clicky_count"] = founded.Count,
			["suggestion_count"] = num,
			["interview_id"] = interviewId,
			["is_debug_preview"] = IsDebugPreview
		});
		RealStack real = host.Real;
		RealtimeMouth mouth = real?.Companion.Mouth;
		if (mouth != null)
		{
			mouth.LineAudioQueued += OnLineAudioQueued;
		}
		try
		{
			bool flag = mouth != null;
			if (flag)
			{
				flag = await WithTimeout(mouth.EnsureOpenAsync(ct), 12.0, fallback: false);
			}
			bool voiceReady = flag;
			ct.ThrowIfCancellationRequested();
			List<Beat> beats = new List<Beat>();
			List<string> list2 = castClickys.Select((RosterClicky c) => c.Name).ToList();
			string fallback = ((list2.Count == 0) ? "Clickys are AI agents that do work for you on your PC, each with its own memory." : ("Farza made " + string.Join(" and ", list2) + ", which everyone starts with. Each Clicky is an AI agent with its own memory that does work for you on your PC.")) + ((founded.Count > 0) ? $" And I made {founded.Count} more just for you." : "");
			beats.Add(new Beat("intro", IntroBrief(castClickys, founded.Count), fallback, null));
			foreach (RosterClicky item in foundedClickys)
			{
				string text = interview?.RevealLines.GetValueOrDefault(item.Slug) ?? item.RevealLine ?? item.OneLiner;
				beats.Add(new Beat("clicky:" + item.Slug, ClickyBrief(item, text), item.Name + ". " + text, null));
			}
			beats.Add(new Beat("ending", EndingBrief(foundedClickys.Select((RosterClicky c) => c.Name).ToList()), "All of this lives up here in your PC's notch, so hover it any time. Wherever you are on your PC, just hold Control and Alt and talk to me. Good luck!", delegate
			{
				Log.Info("tour", "reveal: intro finished; the notch folds and the goodbye");
				FoldNotch();
			}));
			DateTimeOffset writtenAt = DateTimeOffset.Now;
			Task<Dictionary<int, string>?> written = (Task<Dictionary<int, string>?>)((real == null) ? ((Task)Task.FromResult<Dictionary<int, string>>(null)) : ((Task)WriteLinesAsync(real, beats, ct)));
			int index = Speak("Okay, check this out. So, I live in your PC's notch. It looks like this.", voiceReady);
			await Delay((!(await PlaybackStartAsync(index, 12.0, ct)).HasValue) ? 0.0 : 1.0, ct);
			OpenNotch();
			for (int b = 0; b < beats.Count; b++)
			{
				ct.ThrowIfCancellationRequested();
				Beat beat = beats[b];
				string line = beat.Fallback;
				double val = 25.0 - (DateTimeOffset.Now - writtenAt).TotalSeconds;
				Dictionary<int, string> dictionary = await WithTimeout(written, Math.Max(0.0, val), null);
				if (dictionary != null && dictionary.TryGetValue(b + 1, out var value) && value.Length > 0 && Fits(beat, value, dictionary.Count, beats.Count))
				{
					line = value;
				}
				int index2 = Speak(line, voiceReady);
				if (!(await PlaybackStartAsync(index2, 30.0, ct)).HasValue && voiceReady)
				{
					Log.Warn("tour", "beat " + beat.Key + ": no audio");
				}
				beat.OnAudioBegins?.Invoke();
				Log.Info("tour", "beat " + beat.Key + " heard");
				await WaitForLineToPlayOutAsync(index2, ct, b == beats.Count - 1);
			}
			FoldNotch();
			await WaitForQuietAsync(45.0, ct);
			return true;
		}
		catch (OperationCanceledException)
		{
			FoldNotch();
			return false;
		}
		catch (Exception ex2)
		{
			Log.Error("tour", "reveal failed", ex2);
			FoldNotch();
			return false;
		}
		finally
		{
			if (mouth != null)
			{
				mouth.LineAudioQueued -= OnLineAudioQueued;
			}
			IsRunning = false;
			ClickyAnalytics.TrackProductEvent("onboarding_reveal_ended", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["interview_id"] = interviewId,
				["outcome"] = (WasAborted ? "aborted" : (ct.IsCancellationRequested ? "cancelled" : "completed")),
				["duration_seconds"] = (double)(Environment.TickCount64 - analyticsStartedAt) / 1000.0
			});
		}
	}

	private static bool Fits(Beat beat, string line, int written, int expected)
	{
		if (beat.Key.StartsWith("clicky:", StringComparison.Ordinal))
		{
			string text = beat.Fallback.Split('.')[0];
			if (text != null && text.Length > 0)
			{
				return line.Contains(text, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return written == expected;
	}

	private int Speak(string line, bool voiceReady)
	{
		int num;
		lock (gate)
		{
			num = 4000 + nextLine++;
		}
		lock (gate)
		{
			audioQueued[num] = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
		Log.Info("tour", $"line {num - 4000}: {line}");
		if (voiceReady)
		{
			host.Real.Companion.Mouth.SpeakLine(line, num);
		}
		else
		{
			lock (gate)
			{
				audioQueued[num].TrySetResult(0.0);
			}
			noVoiceUntil = Environment.TickCount64 + (long)(Math.Max(2.5, (double)line.Length / 15.0) * 1000.0);
		}
		return num;
	}

	private void OnLineAudioQueued(int index, double offset)
	{
		TaskCompletionSource<double> value;
		lock (gate)
		{
			audioQueued.TryGetValue(index, out value);
		}
		double num = host.Real?.Audio.Buffered.TotalSeconds ?? 0.0;
		value?.TrySetResult(Math.Max(0.0, num - 0.1));
	}

	private async Task<double?> PlaybackStartAsync(int index, double timeoutSeconds, CancellationToken ct)
	{
		TaskCompletionSource<double> tcs;
		lock (gate)
		{
			tcs = audioQueued[index];
		}
		Task obj = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct));
		ct.ThrowIfCancellationRequested();
		if (obj != tcs.Task)
		{
			return null;
		}
		double wait = tcs.Task.Result;
		if (wait > 0.0)
		{
			await Task.Delay(TimeSpan.FromSeconds(wait), ct);
		}
		return wait;
	}

	private async Task WaitForLineToPlayOutAsync(int index, CancellationToken ct, bool last)
	{
		if (last)
		{
			return;
		}
		RealStack real = host.Real;
		if (real == null || noVoiceUntil > 0)
		{
			while (Environment.TickCount64 < noVoiceUntil)
			{
				await Task.Delay(50, ct);
			}
			return;
		}
		long started = Environment.TickCount64;
		while (Environment.TickCount64 - started < 60000)
		{
			ct.ThrowIfCancellationRequested();
			if (!real.Companion.Mouth.IsResponseInFlight && real.Audio.Buffered < TimeSpan.FromMilliseconds(250L, 0L))
			{
				break;
			}
			await Task.Delay(40, ct);
		}
	}

	private async Task WaitForQuietAsync(double maxSeconds, CancellationToken ct)
	{
		RealStack real = host.Real;
		if (real == null)
		{
			while (Environment.TickCount64 < noVoiceUntil)
			{
				await Task.Delay(50, ct);
			}
			return;
		}
		long started = Environment.TickCount64;
		long? quietSince = null;
		while ((double)(Environment.TickCount64 - started) / 1000.0 < maxSeconds)
		{
			if (real.Companion.Mouth.IsResponseInFlight || real.Audio.Buffered > TimeSpan.FromMilliseconds(60L, 0L))
			{
				quietSince = null;
			}
			else
			{
				long tickCount = Environment.TickCount64;
				long valueOrDefault = quietSince.GetValueOrDefault();
				long num;
				if (!quietSince.HasValue)
				{
					valueOrDefault = Environment.TickCount64;
					quietSince = valueOrDefault;
					num = valueOrDefault;
				}
				else
				{
					num = valueOrDefault;
				}
				if (tickCount - num >= 200)
				{
					break;
				}
			}
			await Task.Delay(40, ct);
		}
	}

	private async Task<Dictionary<int, string>?> WriteLinesAsync(RealStack real, List<Beat> beats, CancellationToken ct)
	{
		string fact = "[App] The welcome tour's last part is starting: their Home is opening in the notch, and you narrate it one beat at a time. You already said the opener: \"Okay, check this out. So, I live in your PC's notch. It looks like this.\" Write every beat's line now, in one go.\n[Home tour] You are showing this person their new Clickys, one beat at a time, while their Home is open in the PC's notch. Each \"[Home tour beat]\" gives only the facts for that beat. Use your own warm, plain words and their interview context, but add no extra explanation, recap, promises, or transition sentence. Follow each beat's word limit; otherwise use at most 25 words in one or two short sentences, then stop. Say each Clicky's name exactly. A Clicky is an AI agent that does work for them; explain this only in the introduction. After that, call them Clickys. The personalized Clickys are the focus: keep the defaults brief. Do not read instructions aloud or mention beats or tour mechanics. No lists, dashes, or questions. Finish with the short goodbye you are given.\n" + string.Join("\n", beats.Select((Beat b, int i) => $"[Home tour beat {i + 1} of {beats.Count}] {b.Brief}")) + "\nAnswer with ONE ```js block holding, for each beat n in order, step(n) then exactly one say(\"…\") with that beat's whole line, and nothing else: nothing drawn (no marks, cards, photos or buttons), no tool calls.";
		Stopwatch sw = Stopwatch.StartNew();
		string text = await real.Companion.WriteSpokenProgramAsync(fact, ct);
		Dictionary<int, string> dictionary = ((text == null) ? null : SayLines.ByStep(text));
		if (text != null)
		{
			Log.Info("tour", "written program: " + text.Replace('\n', ' ').Substring(0, Math.Min(text.Length, 1500)));
		}
		Log.Info("tour", (dictionary == null) ? "lines not written (fallbacks)" : $"one brain wrote {dictionary.Count} line(s) in {sw.Elapsed.TotalSeconds:0.0} s");
		return dictionary;
	}

	private static string IntroBrief(IReadOnlyList<RosterClicky> cast, int foundedCount)
	{
		string value = ((cast.Count == 0) ? "Say that Clickys are AI agents that do work for them on their PC, each with its own memory." : $"Say that Farza made {((cast.Count == 1) ? "one Clicky" : $"{cast.Count} Clickys")} everyone starts with: {string.Join(" and ", cast.Select((RosterClicky c) => c.Name + " (" + c.OneLiner + ")"))}. Name each exactly, with a few words on what it does. Then say each Clicky is an AI agent with its own memory that does work for them on their PC.");
		string value2 = ((foundedCount > 0) ? $"End by saying you made {foundedCount} more just for them from what they told you. No names or goal recap yet." : "");
		return $"In at most 50 words total. {value} {value2} At most four short sentences.";
	}

	private static string ClickyBrief(RosterClicky c, string revealLine)
	{
		global::_003C_003Ey__InlineArray3<string> buffer = default(global::_003C_003Ey__InlineArray3<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 0) = "Land, in order: its name, " + c.Name + ", said first and exactly;";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 1) = "and what this Clicky is for, tied to what they told you (the fact to convey: \"" + revealLine + "\").";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 2) = "At most 25 words in one or two short sentences. No general explanation of Clickys.";
		return string.Join(" ", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<string>, string>(in buffer, 3));
	}

	private static string EndingBrief(IReadOnlyList<string> names)
	{
		string goalLine = ClickyRoster.Shared.GoalLine;
		string value = ((goalLine == null) ? "" : (" (what they told you they are going for, in their words: \"" + goalLine + "\"; use THEIR words, not a paraphrase)"));
		string value2 = ((names.Count == 0) ? "" : (" The Clickys you just introduced: " + string.Join(", ", names) + "."));
		string text = $"the goodbye. SHORT, two or three sentences, but never at the cost of warmth: if two sentences would sound cold, say three. This is you saying goodbye to THIS person at the end of THIS conversation, so it must carry something only they would recognize: their name if they gave it, the thing they are going for{value}, a detail they mentioned or a Clicky by name.{value2} Woven in, in whatever order reads naturally, never as instructions: all of this lives up here in their PC's notch, hover it any time; and wherever they are on their PC they can hold Control and Alt (say the shortcut exactly: \"hold Control and Alt\") and just talk to you. End on the good luck, aimed at their thing, said the way a friend who was rooting for them would say it. No recap, no list, no example asks, nothing that could be said to anyone else. Then stop.";
		return "[Home tour beat] Last beat. On screen, as you speak: the notch folds back up; the user's own desktop is what's left. Land: " + text + " Say it now, in your own words, per the Home tour rules, then stop for good.";
	}

	private static async Task Delay(double seconds, CancellationToken ct)
	{
		if (seconds > 0.0)
		{
			await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
		}
	}

	private static async Task<T> WithTimeout<T>(Task<T> task, double seconds, T fallback)
	{
		return (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(Math.Max(0.0, seconds)))) == task && task.IsCompletedSuccessfully) ? task.Result : fallback;
	}
}
