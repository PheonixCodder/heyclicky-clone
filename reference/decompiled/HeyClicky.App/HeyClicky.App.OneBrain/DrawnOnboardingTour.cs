using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HeyClicky.App.Shell;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Input;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.OneBrain;

public static class DrawnOnboardingTour
{
	private const string Area = "drawn tour";

	private static JsonObject? pendingBrief;

	private static readonly object gate = new object();

	private static volatile bool isRunning;

	private static long? analyticsStartedAt;

	private static string analyticsSource = "onboarding";

	private static long lastNotchClickNudgeAt;

	private static int holdGeneration;

	private static IDisposable? notchClickWatch;

	private static long lastTourCardClickNudgeAt;

	public static readonly IReadOnlyList<InterviewLine> SampleInterview = new global::_003C_003Ez__ReadOnlyArray<InterviewLine>(new InterviewLine[8]
	{
		new InterviewLine("assistant", "Hi, I'm Clicky. I'm not real, of course, but I'm a buddy that exists to help you work on your ideas. So what are you working on these days?"),
		new InterviewLine("user", "I'm building my first app ever. It's a running app, kind of a coach for people who just started running. Like couch to 5k, but it actually adapts to how your runs went."),
		new InterviewLine("assistant", "Oh I love that. What would winning look like for you?"),
		new InterviewLine("user", "Honestly? Making my first dollar. One stranger paying me one dollar for something I built. I've never made money from my own thing."),
		new InterviewLine("assistant", "Where's the app at right now?"),
		new InterviewLine("user", "I have a half working SwiftUI prototype. It tracks a run and plays a voice cue every kilometer. No name, no website, and nobody's seen it. I keep tweaking the code instead of showing it to anyone."),
		new InterviewLine("assistant", "Last one. Have you used an AI agent before, like Claude Code or Codex, or mostly ChatGPT?"),
		new InterviewLine("user", "Mostly ChatGPT. I tried Cursor for the app a bit. I've never had something just go do work for me in the background.")
	});

	public const string RealInterviewsFileName = "DrawnTourTestInterviews.json";

	private static readonly string[] PlaceholderNames = new string[10] { "Alex", "Maya", "Sam", "Priya", "Leo", "Nina", "Omar", "Jules", "Kai", "Rosa" };

	public static bool IsRunning
	{
		get
		{
			return isRunning;
		}
		set
		{
			if (isRunning != value)
			{
				isRunning = value;
				DrawnOnboardingTour.NotchHoverSuppressionChanged?.Invoke();
			}
		}
	}

	public static bool HoldsNotch { get; private set; }

	public static bool SuppressesNotchHover
	{
		get
		{
			if (!IsRunning)
			{
				return HoldsNotch;
			}
			return true;
		}
	}

	public static bool IsFinaleEnabled => true;

	public static Dictionary<string, ProactiveSuggestionCard> PlannedCardsById { get; set; } = new Dictionary<string, ProactiveSuggestionCard>();

	public static event Action? NotchHoverSuppressionChanged;

	public static JsonObject? ConsumePendingBrief()
	{
		lock (gate)
		{
			JsonObject? result = pendingBrief;
			pendingBrief = null;
			return result;
		}
	}

	public static void Arm(JsonObject brief)
	{
		lock (gate)
		{
			pendingBrief = brief;
		}
	}

	public static void TrackFinaleStarted(string source, int plannedCardCount, int interviewAnswerCount, bool prefetched = false)
	{
		analyticsStartedAt = Environment.TickCount64;
		analyticsSource = source;
		ClickyTourJobCards.TrackFlowAction("started", isMorning: false, new JsonObject((JsonNodeOptions?)null)
		{
			["source"] = source,
			["has_interview"] = interviewAnswerCount > 0,
			["interview_answer_count"] = interviewAnswerCount,
			["planned_card_count"] = plannedCardCount,
			["prefetched"] = prefetched
		});
	}

	public static void TrackFinaleEnded(string outcome, int cardsShownCount, int nextQuestionCount)
	{
		long? num = analyticsStartedAt;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			analyticsStartedAt = null;
			ClickyTourJobCards.TrackFlowAction("ended", isMorning: false, new JsonObject((JsonNodeOptions?)null)
			{
				["source"] = analyticsSource,
				["outcome"] = outcome,
				["duration_seconds"] = ClickyAnalytics.RoundedSeconds((double)(Environment.TickCount64 - valueOrDefault) / 1000.0),
				["cards_shown_count"] = cardsShownCount,
				["next_question_count"] = nextQuestionCount
			});
		}
	}

	public static void HoldNotchUntilPaywallAnswered(AppHost host)
	{
		ReleaseNotch();
		HoldsNotch = true;
		DrawnOnboardingTour.NotchHoverSuppressionChanged?.Invoke();
		int generation = ++holdGeneration;
		DispatcherQueueTimer timer = host.DispatcherQueue.CreateTimer().Rooted();
		timer.Interval = TimeSpan.FromSeconds(600L);
		timer.IsRepeating = false;
		timer.Tick += delegate
		{
			timer.Stop();
			if (holdGeneration == generation)
			{
				ReleaseNotch();
			}
		};
		timer.Start();
		DispatcherQueue dispatcher = host.DispatcherQueue;
		notchClickWatch = PointerInput.WatchAllPresses(delegate(PointD press)
		{
			dispatcher.TryEnqueue(delegate
			{
				if (IsOnHeldNotch(host, press))
				{
					NudgeNotchClicked(host);
				}
			});
		});
	}

	public static void ReleaseNotch()
	{
		notchClickWatch?.Dispose();
		notchClickWatch = null;
		if (HoldsNotch)
		{
			HoldsNotch = false;
			holdGeneration++;
			DrawnOnboardingTour.NotchHoverSuppressionChanged?.Invoke();
		}
	}

	private static bool IsOnHeldNotch(AppHost host, PointD press)
	{
		(int, int, int, int) tuple = host.NotchUi.SurfaceRectPx();
		if (tuple.Item3 <= 0)
		{
			return false;
		}
		Displays.Monitor monitor = Displays.At(tuple.Item1 + tuple.Item3 / 2, tuple.Item2 + Math.Max(1, tuple.Item4 / 2));
		double scale = monitor.Scale;
		double num = (double)monitor.Bounds.Left + (double)monitor.Bounds.Width / 2.0;
		if (DrawnOnboardingNotchShowcase.Shared.IsShowing)
		{
			if (press.X >= num - 560.0 * scale && press.X <= num + 560.0 * scale && press.Y >= (double)monitor.Bounds.Top)
			{
				return press.Y <= (double)monitor.Bounds.Top + (double)monitor.Bounds.Height * 0.6;
			}
			return false;
		}
		if (press.X >= (double)tuple.Item1 - 40.0 * scale && press.X <= (double)(tuple.Item1 + tuple.Item3) + 40.0 * scale && press.Y >= (double)monitor.Bounds.Top)
		{
			return press.Y <= (double)(tuple.Item2 + tuple.Item4) + 30.0 * scale;
		}
		return false;
	}

	public static void NudgeNotchClicked(AppHost host)
	{
		if (HoldsNotch && Environment.TickCount64 - lastNotchClickNudgeAt > 4000)
		{
			lastNotchClickNudgeAt = Environment.TickCount64;
			ClickyTourJobCards.TrackFlowAction("notch_clicked_during_tour", isMorning: false, new JsonObject((JsonNodeOptions?)null) { ["while_notch_shown"] = DrawnOnboardingNotchShowcase.Shared.IsShowing });
			host.SayAtCursorAsync("try this after the demo!", streamed: true, 2.0);
		}
	}

	public static void NudgeTourCardClicked(AppHost host)
	{
		if (Environment.TickCount64 - lastTourCardClickNudgeAt > 4000)
		{
			lastTourCardClickNudgeAt = Environment.TickCount64;
			host.SayAtCursorAsync("you can start these from your notch once we're done here!", streamed: true, 2.0);
		}
	}

	public static (string Name, IReadOnlyList<InterviewLine> Lines, int? Index)? RandomRealInterview()
	{
		try
		{
			string path = AppPaths.Combine("DrawnTourTestInterviews.json");
			if (!File.Exists(path) || !(JsonNode.Parse(File.ReadAllText(path), null)?["interviews"] is JsonArray { Count: not 0 } jsonArray))
			{
				return null;
			}
			int num = Random.Shared.Next(jsonArray.Count);
			List<InterviewLine> item = (from l in (jsonArray[num]?["lines"] as JsonArray)?.OfType<JsonObject>()
				select new InterviewLine((l["role"]?.GetValue<string>() == "user") ? "user" : "assistant", l["text"]?.GetValue<string>() ?? "")).ToList() ?? new List<InterviewLine>();
			return (PlaceholderNames[num % PlaceholderNames.Length], item, num);
		}
		catch (Exception ex) when (((ex is JsonException || ex is IOException || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			Log.Info("drawn tour", "test interviews unreadable: " + ex.Message);
			return null;
		}
	}

	public static (string Name, IReadOnlyList<InterviewLine> Lines, int? Index) TestInterview()
	{
		return RandomRealInterview() ?? ("Sam", SampleInterview, null);
	}

	public static JsonObject TourBrief(string userName, IReadOnlyList<InterviewLine> interview, string talkKeys, IReadOnlyList<ProactiveSuggestionCard>? plannedCards = null)
	{
		Dictionary<string, ProactiveSuggestionCard> plannedById;
		JsonObject result = DrawnTourBriefs.TourBrief(userName, interview, talkKeys, plannedCards, out plannedById);
		PlannedCardsById = plannedById;
		return result;
	}

	public static string FittedTranscript(IReadOnlyList<InterviewLine> interview, string userName, int budget)
	{
		return DrawnTourBriefs.FittedTranscript(interview, userName, budget);
	}
}
