using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Proactive;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Drawing.Overlay;

namespace HeyClicky.App.OneBrain;

public sealed class GoodMorningBrief
{
	public sealed record SavedResearch
	{
		[JsonPropertyName("userID")]
		public required string UserId { get; init; }

		[JsonPropertyName("savedAt")]
		public required DateTimeOffset SavedAt { get; init; }

		[JsonPropertyName("jobID")]
		public required string JobId { get; init; }

		[JsonPropertyName("curatedEvidence")]
		public required string CuratedEvidence { get; init; }

		[JsonPropertyName("headline")]
		public string? Headline { get; init; }

		[JsonPropertyName("headlineLine")]
		public string? HeadlineLine { get; init; }

		[JsonPropertyName("plateCards")]
		public List<SavedCard>? PlateCards { get; init; }

		[CompilerGenerated]
		[SetsRequiredMembers]
		private SavedResearch(SavedResearch original)
		{
			UserId = original.UserId;
			SavedAt = original.SavedAt;
			JobId = original.JobId;
			CuratedEvidence = original.CuratedEvidence;
			Headline = original.Headline;
			HeadlineLine = original.HeadlineLine;
			PlateCards = original.PlateCards;
		}

		public SavedResearch()
		{
		}
	}

	public sealed record SavedCard(string Title, string Description, string AgentPrompt, string? ClickySlug, int? RoutineEveryMinutes, string? RoutineTitle, string? IntegrationName);

	public sealed record Evidence(IReadOnlyList<JsonObject> Findings, IReadOnlyList<(string Source, string Summary)> Summaries);

	private const string Area = "good morning";

	private readonly AppHost host;

	private readonly RealStack real;

	private static JsonObject? pendingBrief;

	private static readonly object briefGate = new object();

	public static volatile bool IsPlaying;

	private static readonly JsonSerializerOptions FileJson = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static GoodMorningBrief? Shared { get; private set; }

	public string ResearchStatusLine { get; private set; } = "";

	public bool IsResearching { get; private set; }

	public bool HasSavedResearch { get; private set; }

	public bool IsPreparingMorning { get; private set; }

	public string? NotchHeadline { get; private set; }

	public string? NotchHeadlineLine { get; private set; }

	public bool HasPaperToday { get; private set; }

	public static bool IsMorningPaperEnabledByDefault => false;

	private bool PaperEnabled => IsMorningPaperEnabled(real.Core.Settings);

	public static string ResearchFilePath => AppPaths.Combine("GoodMorningResearch.json");

	public event Action? Changed;

	public GoodMorningBrief(AppHost host, RealStack real)
	{
		this.host = host;
		this.real = real;
		Shared = this;
		RefreshStatusFromDisk();
	}

	public static JsonObject? ConsumePendingBrief()
	{
		lock (briefGate)
		{
			JsonObject? result = pendingBrief;
			pendingBrief = null;
			return result;
		}
	}

	public static bool IsMorningPaperEnabled(SettingsStore settings)
	{
		return settings.Read((AppSettings s) => s.MorningPaperEnabled) ?? IsMorningPaperEnabledByDefault;
	}

	public void NoteSettledSweep(ProactiveJobStatus status, string userId, IReadOnlyList<ProactiveSuggestionCard> cards)
	{
		string curatedEvidence = status.CuratedEvidence;
		if (curatedEvidence != null && curatedEvidence.Length > 0 && (status.MorningHeadline != null || ParsedEvidence(curatedEvidence).Findings.Count != 0))
		{
			Save(new SavedResearch
			{
				UserId = userId,
				SavedAt = DateTimeOffset.Now,
				JobId = status.JobId,
				CuratedEvidence = curatedEvidence,
				Headline = status.MorningHeadline,
				HeadlineLine = status.MorningHeadlineLine,
				PlateCards = ((cards.Count == 0) ? null : cards.Select(Saved).ToList())
			});
			host.DispatcherQueue.TryEnqueue(delegate
			{
				RefreshStatusFromDisk(userId);
			});
			Log.Info("good morning", "morning paper research kept: " + (status.MorningHeadline ?? "no headline"));
		}
	}

	private static SavedCard Saved(ProactiveSuggestionCard c)
	{
		return new SavedCard(c.Title, c.Description, c.AgentPrompt, c.ClickySlug, c.Routine?.EveryMinutes, c.Routine?.Title, c.IntegrationName);
	}

	public static bool HasTodaysResearch(SettingsStore settings, string? userId)
	{
		if (IsMorningPaperEnabled(settings) && userId != null)
		{
			SavedResearch savedResearch = LoadSavedResearch(userId);
			if ((object)savedResearch != null)
			{
				return IsToday(savedResearch.SavedAt);
			}
		}
		return false;
	}

	public static (string Headline, string? Line)? TodaysHeadline(SettingsStore settings, string? userId)
	{
		if (IsMorningPaperEnabled(settings) && userId != null)
		{
			SavedResearch savedResearch = LoadSavedResearch(userId);
			if ((object)savedResearch != null && IsToday(savedResearch.SavedAt))
			{
				string headline = savedResearch.Headline;
				if (headline != null && headline.Length > 0)
				{
					return (headline, savedResearch.HeadlineLine);
				}
			}
		}
		return null;
	}

	private static bool IsToday(DateTimeOffset at)
	{
		return at.LocalDateTime.Date == DateTime.Now.Date;
	}

	public static SavedResearch? LoadSavedResearch(string? userId)
	{
		try
		{
			if (!File.Exists(ResearchFilePath))
			{
				return null;
			}
			SavedResearch savedResearch = JsonSerializer.Deserialize<SavedResearch>(File.ReadAllText(ResearchFilePath), FileJson);
			if ((object)savedResearch == null || (userId != null && savedResearch.UserId != userId))
			{
				return null;
			}
			return savedResearch;
		}
		catch (Exception ex)
		{
			Log.Info("good morning", "saved research unreadable: " + ex.Message);
			return null;
		}
	}

	private static void Save(SavedResearch research)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ResearchFilePath));
			string text = ResearchFilePath + ".tmp";
			File.WriteAllText(text, JsonSerializer.Serialize(research, FileJson));
			File.Move(text, ResearchFilePath, overwrite: true);
		}
		catch (Exception ex)
		{
			Log.Warn("good morning", "research not saved: " + ex.Message);
		}
	}

	public void RefreshStatusFromDisk(string? userId = null)
	{
		if (!IsResearching)
		{
			if (userId == null)
			{
				userId = real.Core.Auth.UserId;
			}
			SavedResearch savedResearch = LoadSavedResearch(userId);
			bool flag = (object)savedResearch != null && PaperEnabled && IsToday(savedResearch.SavedAt);
			NotchHeadline = (flag ? savedResearch.Headline : null);
			NotchHeadlineLine = (flag ? savedResearch.HeadlineLine : null);
			HasPaperToday = flag;
			if ((object)savedResearch != null)
			{
				HasSavedResearch = true;
				int count = ParsedEvidence(savedResearch.CuratedEvidence).Findings.Count;
				ResearchStatusLine = $"Research saved {RelativeTime(savedResearch.SavedAt, null)} · {count} finding{((count == 1) ? "" : "s")}";
			}
			else
			{
				HasSavedResearch = false;
				ResearchStatusLine = "No research saved yet.";
			}
			this.Changed?.Invoke();
		}
	}

	public void StartResearch()
	{
		if (IsResearching)
		{
			return;
		}
		string userId = real.Core.Auth.UserId;
		if (userId == null)
		{
			ResearchStatusLine = "Sign in first.";
			this.Changed?.Invoke();
			return;
		}
		ProactiveCoordinator proactive = host.Proactive;
		if (proactive == null)
		{
			return;
		}
		IsResearching = true;
		ResearchStatusLine = "Reading your apps and the news… (a few minutes)";
		this.Changed?.Invoke();
		DateTimeOffset started = DateTimeOffset.Now;
		Task.Run(async delegate
		{
			try
			{
				SavedResearch savedResearch = await RunResearchAsync(proactive, userId, started, delegate(string line)
				{
					host.DispatcherQueue.TryEnqueue(delegate
					{
						ResearchStatusLine = line;
						this.Changed?.Invoke();
					});
				});
				Save(savedResearch);
				host.DispatcherQueue.TryEnqueue(delegate
				{
					IsResearching = false;
					RefreshStatusFromDisk(userId);
				});
				Log.Info("good morning", $"research saved after {(int)(DateTimeOffset.Now - started).TotalSeconds}s: {ParsedEvidence(savedResearch.CuratedEvidence).Findings.Count} findings");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				host.DispatcherQueue.TryEnqueue(delegate
				{
					IsResearching = false;
					ResearchStatusLine = "Research failed: " + ex3.Message;
					this.Changed?.Invoke();
				});
				Log.Warn("good morning", "research failed: " + ex3.Message);
			}
		});
	}

	private async Task<SavedResearch> RunResearchAsync(ProactiveCoordinator proactive, string userId, DateTimeOffset started, Action<string> onProgress)
	{
		ConfiguredTaskAwaitable<ProactiveRequestInputs> configuredTaskAwaitable = proactive.BuildRequestInputsAsync(ProactiveCheckReason.Debug, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		ProactiveRequestInputs inputs = await configuredTaskAwaitable with
		{
			ResearchOnlyForGoodMorning = true
		};
		ProactiveAgentsClient client = new ProactiveAgentsClient(real.Core.Worker);
		ProactiveJobStatus status = await client.StartJobAsync(inputs, DateTimeOffset.Now).ConfigureAwait(continueOnCapturedContext: false);
		Log.Info("good morning", $"research job {status.JobId} {status.State}");
		while (!status.IsSettled)
		{
			if (DateTimeOffset.Now - started > TimeSpan.FromMinutes(20L))
			{
				throw new InvalidOperationException("Took over 20 minutes.");
			}
			await Task.Delay(5000).ConfigureAwait(continueOnCapturedContext: false);
			int value = (int)(DateTimeOffset.Now - started).TotalMinutes;
			onProgress($"Reading your apps and the news… {value} min ({status.State.ToString().ToLowerInvariant()})");
			try
			{
				status = await client.PollJobAsync(status.JobId, ProactiveCheckReason.Debug, DateTimeOffset.Now).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Info("good morning", "research poll: " + ex.Message);
			}
		}
		if (status.State == ProactiveJobState.Failed)
		{
			throw new InvalidOperationException(status.Error ?? "The research job failed.");
		}
		string curatedEvidence = status.CuratedEvidence;
		if (curatedEvidence == null || curatedEvidence.Length <= 0)
		{
			throw new InvalidOperationException("A suggestion sweep was already running, so this joined it (no research came back). Try again once it's done.");
		}
		return new SavedResearch
		{
			UserId = userId,
			SavedAt = DateTimeOffset.Now,
			JobId = status.JobId,
			CuratedEvidence = curatedEvidence
		};
	}

	public void Play(string source = "notch_read_it")
	{
		if (!real.Core.Auth.IsSignedIn)
		{
			return;
		}
		SavedResearch savedResearch = LoadSavedResearch(real.Core.Auth.UserId);
		if ((object)savedResearch == null)
		{
			ResearchStatusLine = "Research first: nothing saved yet.";
			this.Changed?.Invoke();
			ClickyTourJobCards.TrackFlowAction("play_skipped", isMorning: true, new JsonObject((JsonNodeOptions?)null)
			{
				["source"] = source,
				["reason"] = "no_research"
			});
		}
		else
		{
			host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.AgentPeek));
			if (host.Services.Presenter.Presentation is NotchPresentation.TextInput)
			{
				host.Services.Presenter.Present(new NotchPresentation.Idle());
			}
			PlayAsync(savedResearch, source);
		}
	}

	private async Task PlayAsync(SavedResearch research, string source)
	{
		IsPlaying = true;
		try
		{
			IReadOnlyList<string> readOnlyList = await TodaysCalendarLinesAsync();
			List<ProactiveSuggestionCard> list = (host.Proactive?.LiveCards(ProactiveCheckReason.Morning, ProactiveCheckReason.Debug) ?? Array.Empty<ProactiveSuggestionCard>()).Take(4).ToList();
			DrawingSuggestionCards.Set(list.Select((ProactiveSuggestionCard c) => c.Id));
			ClickyTourJobCards.Shared.DismissAll();
			ClickyTourJobCards.Shared.SuggestionCheckReason = ProactiveCheckReason.Morning;
			DrawnOnboardingTour.PlannedCardsById = list.Select((ProactiveSuggestionCard c, int i) => (Key: $"c{i + 1}", Card: c)).ToDictionary(((string Key, ProactiveSuggestionCard Card) p) => p.Key, ((string Key, ProactiveSuggestionCard Card) p) => p.Card);
			lock (briefGate)
			{
				pendingBrief = MorningBrief(FirstName(), research, readOnlyList, list);
			}
			Log.Info("good morning", $"playing from research saved {RelativeTime(research.SavedAt, null)} ({list.Count} card(s))");
			long playStartedAt = Environment.TickCount64;
			ClickyTourJobCards.TrackFlowAction("played", isMorning: true, new JsonObject((JsonNodeOptions?)null)
			{
				["source"] = source,
				["card_count"] = list.Count,
				["story_count"] = ParsedEvidence(research.CuratedEvidence).Findings.Count,
				["has_headline"] = !string.IsNullOrEmpty(research.Headline),
				["calendar_event_count"] = readOnlyList.Count,
				["research_age_minutes"] = (int)((DateTimeOffset.Now - research.SavedAt).TotalSeconds / 60.0)
			});
			await Task.Delay(700);
			await real.Companion.SubmitFactAsync("[App] It's the user's first moment at their PC this morning. Draw their good morning now, as the note says.");
			ConsumePendingBrief();
			long waitStarted = Environment.TickCount64;
			while (Environment.TickCount64 - waitStarted < 180000 && (real.Canvas.IsStreamingNarration || real.Companion.State == VoiceState.Responding))
			{
				await Task.Delay(300);
			}
			bool flag = Environment.TickCount64 - waitStarted >= 180000;
			ClickyTourJobCards.TrackFlowAction("ended", isMorning: true, new JsonObject((JsonNodeOptions?)null)
			{
				["source"] = source,
				["outcome"] = (flag ? "narration_timeout" : (((object)real.Canvas.DrawingDisplay == null) ? "no_drawing" : "finished")),
				["duration_seconds"] = ClickyAnalytics.RoundedSeconds((double)(Environment.TickCount64 - playStartedAt) / 1000.0),
				["cards_shown_count"] = ClickyTourJobCards.Shared.PlacedCardCount
			});
			ClickyTourJobCards.Shared.KeepCardsUntilQuestionsGo();
			DrawnOnboardingTour.PlannedCardsById = new Dictionary<string, ProactiveSuggestionCard>();
		}
		catch (Exception ex)
		{
			Log.Warn("good morning", "play failed: " + ex.Message);
		}
		finally
		{
			IsPlaying = false;
		}
	}

	public static JsonObject MorningBrief(string? firstName, SavedResearch research, IReadOnlyList<string> todaysEvents, IReadOnlyList<ProactiveSuggestionCard> plateCards)
	{
		return DrawnTourBriefs.MorningBrief(firstName, RelativeTime(research.SavedAt, null), research.CuratedEvidence, research.Headline, todaysEvents, plateCards);
	}

	public static Evidence ParsedEvidence(string json)
	{
		try
		{
			if (!(JsonNode.Parse(json, null) is JsonObject jsonObject))
			{
				return new Evidence(Array.Empty<JsonObject>(), Array.Empty<(string, string)>());
			}
			List<JsonObject> findings = (jsonObject["findings"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();
			List<(string, string)> list = new List<(string, string)>();
			foreach (JsonObject item in (jsonObject["summaries"] as JsonArray)?.OfType<JsonObject>() ?? Array.Empty<JsonObject>())
			{
				if (item["source"] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && item["summary"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value2) && value2.Length > 0)
				{
					list.Add((value, value2));
				}
			}
			return new Evidence(findings, list);
		}
		catch (JsonException)
		{
			return new Evidence(Array.Empty<JsonObject>(), Array.Empty<(string, string)>());
		}
	}

	private async Task<IReadOnlyList<string>> TodaysCalendarLinesAsync()
	{
		UpcomingCalendarFetch upcomingCalendarFetch = await new UpcomingMeetingsClient(real.Core.Worker).FetchAsync();
		if ((object)upcomingCalendarFetch == null)
		{
			return Array.Empty<string>();
		}
		return upcomingCalendarFetch.Events.Where((UpcomingCalendarEvent e) => e.StartsAt.LocalDateTime.Date == DateTime.Now.Date).Take(8).Select(delegate(UpcomingCalendarEvent e)
		{
			string value = (e.IsAllDay ? "all day" : e.StartsAt.LocalDateTime.ToString("h:mm tt", CultureInfo.GetCultureInfo("en-US")));
			string location = e.Location;
			string value2 = ((location != null) ? (" (" + location + ")") : (((object)e.MeetingUrl != null) ? " (video call)" : ""));
			return $"- {value}: {e.Title}{value2}";
		})
			.ToList();
	}

	private string? FirstName()
	{
		string text = real.Core.Auth.User?.DisplayName?.Trim();
		if (text != null && text.Length > 0)
		{
			string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (array != null && array.Length > 0)
			{
				return array[0];
			}
		}
		return null;
	}

	public static string RelativeTime(DateTimeOffset date, DateTimeOffset? now = null)
	{
		return DrawnTourBriefs.RelativeTime(date, now ?? DateTimeOffset.Now);
	}
}
