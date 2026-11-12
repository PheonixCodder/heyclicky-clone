using System;
using System.IO;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackInkCard : Grid
{
	private readonly Action onOpen;

	private readonly Action onPutBack;

	private readonly Action onUnpin;

	private readonly ClickyInkTimeline timeline;

	private DispatcherTimerHandle? penDone;

	private bool isWriting;

	public ClickyInkCardSubject Thing { get; }

	public ClickyHomeInkNote Ink { get; }

	public ClickyEdgeStackInkPresentation Presentation { get; }

	public string? LiveStep { get; }

	public DateTimeOffset? LookedAt { get; }

	public ClickyInkActions Actions { get; }

	public double PulledRoomHeight { get; }

	public bool FillsScreen { get; }

	public double? PulledHeight { get; private set; }

	private ClickyHomeSpreadPaper Paper
	{
		get
		{
			if (Thing.Kind != ClickyInkCardSubject.SubjectKind.Kept)
			{
				return ClickyHomeSpreadPaper.Note;
			}
			return ClickyHomeSpreadPaper.Of(Thing.Stock);
		}
	}

	private bool Animates
	{
		get
		{
			if (isWriting && Presentation.LetsThePenMove && !Anim.ReduceMotion)
			{
				return !ClickyHomeRendering.IsOffscreen;
			}
			return false;
		}
	}

	public event Action<double>? PulledHeightChanged;

	public ClickyEdgeStackInkCard(ClickyInkCardSubject thing, ClickyHomeInkNote ink, ClickyEdgeStackInkPresentation presentation, string? liveStep = null, DateTimeOffset? lookedAt = null, ClickyInkActions? actions = null, Action? onOpen = null, Action? onPutBack = null, Action? onUnpin = null, double pulledRoomHeight = 640.0, bool fillsScreen = false)
	{
		Thing = thing;
		Ink = ink;
		Presentation = presentation;
		LiveStep = liveStep;
		LookedAt = lookedAt;
		Actions = actions ?? new ClickyInkActions();
		this.onOpen = onOpen ?? ((Action)delegate
		{
		});
		this.onPutBack = onPutBack ?? ((Action)delegate
		{
		});
		this.onUnpin = onUnpin ?? ((Action)delegate
		{
		});
		PulledRoomHeight = pulledRoomHeight;
		FillsScreen = fillsScreen;
		isWriting = (DateTimeOffset.Now - ink.StartedAt).TotalSeconds < ink.Note.FinishedClock;
		timeline = new ClickyInkTimeline(Card, ClickyInkTimeline.Interval(Animates ? new double?(1.0 / 30.0) : ((double?)null)));
		base.Children.Add(timeline);
		base.Loaded += delegate
		{
			WriteUntilThePenIsDone();
		};
		base.Unloaded += delegate
		{
			penDone?.Cancel();
			penDone = null;
		};
	}

	public bool IsEquivalent(ClickyEdgeStackInkCard other)
	{
		if (Thing == other.Thing && Ink.StartedAt == other.Ink.StartedAt && Ink.Note.VersionCount == other.Ink.Note.VersionCount && Ink.Note.PenFreeAt == other.Ink.Note.PenFreeAt && Ink.Note.LastVersionAt == other.Ink.Note.LastVersionAt && Ink.Note.Pages.Count == other.Ink.Note.Pages.Count && Presentation == other.Presentation && LiveStep == other.LiveStep)
		{
			DateTimeOffset? lookedAt = LookedAt;
			DateTimeOffset? lookedAt2 = other.LookedAt;
			if (lookedAt.HasValue == lookedAt2.HasValue && (!lookedAt.HasValue || lookedAt.GetValueOrDefault() == lookedAt2.GetValueOrDefault()) && Actions.Feedback.Equals(other.Actions.Feedback))
			{
				return PulledRoomHeight == other.PulledRoomHeight;
			}
		}
		return false;
	}

	private double StillClock(DateTimeOffset now)
	{
		double totalSeconds = (now - Ink.StartedAt).TotalSeconds;
		if (ClickyHomeRendering.IsOffscreen && Presentation.LetsThePenMove)
		{
			return totalSeconds;
		}
		return Math.Max(totalSeconds, Ink.Note.FinishedClock + 1.0);
	}

	private FrameworkElement Card(DateTimeOffset now)
	{
		double clock = (Animates ? (now - Ink.StartedAt).TotalSeconds : StillClock(now));
		ClickyInkLiveNote note = Ink.Note;
		ClickyInkCardSubject thing = Thing;
		DateTimeOffset? lookedAt = LookedAt;
		double? seenClock;
		if (lookedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = lookedAt.GetValueOrDefault();
			seenClock = (valueOrDefault - Ink.StartedAt).TotalSeconds;
		}
		else
		{
			seenClock = null;
		}
		ClickyEdgeStackInkReading reading = ClickyEdgeStackInkReadings.Make(note, clock, thing, seenClock, Actions.Feedback);
		bool isJob = Thing.Kind != ClickyInkCardSubject.SubjectKind.Kept;
		if (Presentation is ClickyEdgeStackInkPresentation.Pulled)
		{
			ClickyEdgeStackInkPulledCard clickyEdgeStackInkPulledCard = new ClickyEdgeStackInkPulledCard(Ink.Note, reading, clock, Paper, isJob, LiveStep, Actions, onOpen, onPutBack, onUnpin, Math.Max(200.0, PulledRoomHeight - 236.0));
			clickyEdgeStackInkPulledCard.VerticalAlignment = VerticalAlignment.Top;
			clickyEdgeStackInkPulledCard.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				if (PulledHeight != e.NewSize.Height)
				{
					PulledHeight = e.NewSize.Height;
					this.PulledHeightChanged?.Invoke(e.NewSize.Height);
				}
			};
			return clickyEdgeStackInkPulledCard;
		}
		return new ClickyEdgeStackInkFace(Ink.Note, reading, clock, Paper, isJob, LiveStep, Presentation is ClickyEdgeStackInkPresentation.Rest, FillsScreen);
	}

	private void WriteUntilThePenIsDone()
	{
		double num = Ink.Note.FinishedClock - (DateTimeOffset.Now - Ink.StartedAt).TotalSeconds;
		if (num <= 0.0)
		{
			if (isWriting)
			{
				isWriting = false;
				timeline.MinimumInterval = null;
			}
			return;
		}
		isWriting = true;
		timeline.MinimumInterval = ClickyInkTimeline.Interval(Animates ? new double?(1.0 / 30.0) : ((double?)null));
		penDone?.Cancel();
		penDone = DispatcherTimerHandle.After(base.DispatcherQueue, num, delegate
		{
			isWriting = false;
			timeline.MinimumInterval = null;
			timeline.Refresh();
		});
	}

	public static void OpenInItsApp(string target)
	{
		string text;
		if (target.Contains("://"))
		{
			if (!Uri.TryCreate(target, UriKind.Absolute, out Uri result))
			{
				return;
			}
			text = result.AbsoluteUri;
		}
		else
		{
			text = ClickyInkPaths.ExpandingTilde(target);
		}
		try
		{
			ShellOpen.Start(text);
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "couldn't open " + Path.GetFileName(text) + ": " + ex.Message);
		}
	}
}
