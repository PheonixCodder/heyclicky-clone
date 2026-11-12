using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Drawing;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace HeyClicky.App.OneBrain;

public sealed class ClickyTourJobCards
{
	private sealed class Shown(ClickyTourJob job, OverlayWindow window, FrameworkElement laptop)
	{
		public ClickyTourJob Job { get; } = job;

		public OverlayWindow Window { get; } = window;

		public FrameworkElement Laptop { get; } = laptop;
	}

	private const string Area = "drawn tour";

	public static Func<TourJobCardContent, bool, FrameworkElement>? ArcCardFactory;

	public static Action<string, string, Guid>? CreateWaitingJob;

	private AppHost? host;

	private readonly List<Shown> shown = new List<Shown>();

	private int placedCount;

	private List<ClickyTourJob> tuckedJobs = new List<ClickyTourJob>();

	private readonly HashSet<string> startedJobKeys = new HashSet<string>();

	private readonly HashSet<string> parkedJobKeys = new HashSet<string>();

	private bool waitingForQuestions;

	private List<(string Text, string Plan)> nextQuestions = new List<(string, string)>();

	private volatile string? pendingPillPlanNote;

	private readonly List<IDisposable> questionButtons = new List<IDisposable>();

	private DisplayInfo? heldQuestionsDisplay;

	private bool hasHeldQuestions;

	private int questionsGeneration;

	private readonly Dictionary<string, int> placedIndexByKey = new Dictionary<string, int>();

	private readonly HashSet<string> cardKeysClickedDuringTour = new HashSet<string>();

	private int parkedCountThisTour;

	private readonly HashSet<string> startingKeys = new HashSet<string>();

	private readonly HashSet<DispatcherQueueTimer> pending = new HashSet<DispatcherQueueTimer>();

	public static ClickyTourJobCards Shared { get; } = new ClickyTourJobCards();

	public ProactiveCheckReason SuggestionCheckReason { get; set; } = ProactiveCheckReason.Onboarding;

	public int PlacedCardCount => placedCount;

	public int HeldNextQuestionCount
	{
		get
		{
			if (!hasHeldQuestions)
			{
				return 0;
			}
			return nextQuestions.Count;
		}
	}

	private bool IsMorningContext => SuggestionCheckReason == ProactiveCheckReason.Morning;

	internal (int Shown, int Tucked, bool HeldQuestions, int QuestionButtons) StateForTests => (Shown: shown.Count, Tucked: tuckedJobs.Count, HeldQuestions: hasHeldQuestions, QuestionButtons: questionButtons.Count);

	internal IReadOnlyList<UIElement> CardRootsForTests => ((IEnumerable<Shown>)shown).Select((Func<Shown, UIElement>)((Shown s) => s.Window.Root)).ToList();

	private ScreenDrawingCanvas? Canvas => host?.Real?.Canvas;

	private DispatcherQueue Dq => host.DispatcherQueue;

	private bool PlaysSounds => host?.Real?.Core.Settings.Read((AppSettings s) => s.DrawingSoundsEnabled) == true;

	private ClickyTourJobCards()
	{
	}

	public static void TrackFlowAction(string action, bool isMorning, JsonObject? properties = null)
	{
		JsonObject jsonObject = properties ?? new JsonObject((JsonNodeOptions?)null);
		jsonObject["action"] = action;
		ClickyAnalytics.TrackProductEvent(isMorning ? "morning_brief_action" : "onboarding_finale_action", null, null, jsonObject);
	}

	private void TrackCardAction(string action, JsonObject? properties = null)
	{
		TrackFlowAction(action, IsMorningContext, properties);
	}

	private JsonObject CardProperties(ClickyTourJob job)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["is_planned_card"] = (object)job.PlannedCard != null };
		if (placedIndexByKey.TryGetValue(job.RailKey, out var value))
		{
			jsonObject["index"] = value;
		}
		return jsonObject;
	}

	private void NoteCardClickedDuringTour(ClickyTourJob job)
	{
		if (cardKeysClickedDuringTour.Add(job.RailKey))
		{
			TrackCardAction("card_clicked_during_tour", CardProperties(job));
		}
	}

	public void Attach(AppHost app)
	{
		host = app;
	}

	public void Start(ClickyTourJob job)
	{
		int num = shown.FindIndex((Shown s) => s.Job.RailKey == job.RailKey);
		if (num >= 0)
		{
			Shown card = shown[num];
			shown.RemoveAt(num);
			startedJobKeys.Add(job.RailKey);
			TrackCardAction("card_started", CardProperties(job));
			FlyAsync(card);
			ProactiveSuggestionCard card2 = job.SuggestionCard(SuggestionCheckReason);
			ProactiveSuggestionCard proactiveSuggestionCard = host?.Proactive?.AppendTourCard(card2);
			if ((object)proactiveSuggestionCard != null)
			{
				host.Proactive.Approve(proactiveSuggestionCard.Id, null, announce: false);
				Log.Info("drawn tour", "started from its card: " + job.Title);
			}
		}
	}

	public void Show(ClickyTourJob job, DisplayInfo? display)
	{
		if (host == null || placedCount >= 3)
		{
			return;
		}
		Displays.Monitor monitor;
		if ((object)display != null)
		{
			monitor = Displays.At((int)(display.BoundsPx.X + display.BoundsPx.Width / 2.0), (int)(display.BoundsPx.Y + display.BoundsPx.Height / 2.0));
		}
		else
		{
			monitor = Displays.UnderCursor();
		}
		Displays.Monitor monitor2 = monitor;
		int num = placedCount++;
		placedIndexByKey[job.RailKey] = num;
		TrackCardAction("card_shown", CardProperties(job));
		double scale = monitor2.Scale;
		double num2 = (double)monitor2.Work.Right / scale;
		double num3 = (double)monitor2.Work.Top / scale;
		double num4 = num2 - 335.92 - 56.0 - 22.0;
		double num5 = num3 + 70.0 - 22.0 + (double)num * 229.52;
		OverlayWindow overlayWindow = new OverlayWindow("HeyClicky tour card", excludeFromCapture: true);
		FrameworkElement frameworkElement = CardView(job, hovered: false);
		Grid grid = new Grid
		{
			Width = 379.92,
			Height = 257.52
		};
		Grid scaled = new Grid
		{
			Width = 494.0,
			Height = 314.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = new ScaleTransform
			{
				ScaleX = 0.68,
				ScaleY = 0.68
			},
			Margin = new Thickness(22.0)
		};
		scaled.Children.Add(frameworkElement);
		grid.Children.Add(scaled);
		Pressable catcher = new Pressable(!DrawnOnboardingTour.IsRunning)
		{
			Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
			Margin = new Thickness(22.0)
		};
		catcher.HoverChanged += delegate(bool hovering)
		{
			if (!startingKeys.Contains(job.RailKey))
			{
				if (scaled.Children.FirstOrDefault() is ArcCardView arcCardView)
				{
					arcCardView.SetPresentation(!hovering, isLive: false);
				}
				else
				{
					scaled.Children.Clear();
					scaled.Children.Add(CardView(job, hovering));
				}
			}
		};
		catcher.Click += delegate
		{
			OnCardClicked(job, scaled, catcher);
		};
		AutomationProperties.SetName(catcher, job.Title);
		grid.Children.Add(catcher);
		overlayWindow.Root.Children.Add(grid);
		overlayWindow.PlacePx((int)Math.Round(num4 * scale), (int)Math.Round(num5 * scale), (int)Math.Round(379.92 * scale), (int)Math.Round(257.52 * scale));
		grid.Opacity = 0.0;
		overlayWindow.ShowNoActivate();
		shown.Add(new Shown(job, overlayWindow, frameworkElement));
		if (!Anim.Instant && !Anim.ReduceMotion)
		{
			grid.CenterPoint = new Vector3(189.96f, 128.76f, 0f);
			grid.Translation = new Vector3(0f, -14f, 0f);
			SwiftSpring spring = new SwiftSpring(0.468, 0.522);
			Anim.ScaleUpFrom(grid, 0.92f, spring, null);
			Anim.TranslationSpring(grid, Vector3.Zero, spring);
			Anim.Opacity(grid, 1f, 0.22, Easing.EaseInOut(Ui.Compositor));
		}
		else
		{
			grid.Opacity = 1.0;
		}
		if (PlaysSounds)
		{
			Canvas?.PlaySound("paper-place", 0.35f);
		}
	}

	private void OnCardClicked(ClickyTourJob job, Grid scaled, Pressable catcher)
	{
		if (DrawnOnboardingTour.IsRunning)
		{
			NoteCardClickedDuringTour(job);
			DrawnOnboardingTour.NudgeTourCardClicked(host);
		}
		else if (startingKeys.Add(job.RailKey))
		{
			catcher.Enabled = false;
			scaled.Children.Clear();
			scaled.Children.Add(CardView(job, hovered: false));
			Border border = new Border();
			border.Background = new SolidColorBrush(Color.FromArgb(198, 0, 0, 0));
			border.CornerRadius = new CornerRadius(100.0);
			border.Padding = new Thickness(14.0, 9.0, 14.0, 9.0);
			border.HorizontalAlignment = HorizontalAlignment.Center;
			border.VerticalAlignment = VerticalAlignment.Center;
			border.Child = Ui.H(8.0, new ProgressRing
			{
				IsActive = true,
				Width = 14.0,
				Height = 14.0,
				Foreground = new SolidColorBrush(Colors.White)
			}, Ui.Text("Starting in the background", 12.5, FontWeights.SemiBold, Colors.White));
			Border border2 = border.Capsule();
			Grid grid = (Grid)scaled.Parent;
			border2.Margin = new Thickness(22.0);
			grid.Children.Add(border2);
			if (!Anim.Instant)
			{
				grid.CenterPoint = new Vector3(189.96f, 128.76f, 0f);
				Anim.ScaleSpring(grid, 0.97f, new SwiftSpring(0.3, 0.7), null);
			}
			Sounds.Play("clicky-text-send", 0.35);
			After(1.1, delegate
			{
				startingKeys.Remove(job.RailKey);
				Start(job);
			});
		}
	}

	private static FrameworkElement CardView(ClickyTourJob job, bool hovered)
	{
		TourJobCardContent tourJobCardContent = new TourJobCardContent(job.RailKey, job.Title, (job.Pitch.Length == 0) ? null : job.Pitch, job.Paper, DrawnOnboardingTour.IsRunning);
		Func<TourJobCardContent, bool, FrameworkElement> arcCardFactory = ArcCardFactory;
		if (arcCardFactory != null)
		{
			return arcCardFactory(tourJobCardContent, !hovered);
		}
		return StandInCard(tourJobCardContent);
	}

	private static FrameworkElement StandInCard(TourJobCardContent content)
	{
		double num = 270.0 - (content.IsTourRunning ? 50.0 : 0.0);
		StackPanel stackPanel = null;
		(double, double, double)[] suggested = ArcCover.Suggested;
		for (int i = 0; i < suggested.Length; i++)
		{
			StackPanel stackPanel2 = Block(suggested[i]);
			stackPanel2.Measure(new Size(428.0, double.PositiveInfinity));
			stackPanel = stackPanel2;
			if (stackPanel2.DesiredSize.Height <= num)
			{
				break;
			}
		}
		stackPanel.Margin = new Thickness(26.0, 20.0, 26.0, 10.0);
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.Children.Add(stackPanel);
		if (content.IsTourRunning)
		{
			Color color = DS.Hex(3112182u);
			StackPanel child = Ui.H(7.0, Icons.Glyph("\ue823", 13.0, color), Ui.Shrink(Ui.Text("Start it from your notch after the tour", 14.0, FontWeights.SemiBold, color), HorizontalAlignment.Left, 0.8));
			Border border = new Border
			{
				MinHeight = 32.0,
				Padding = new Thickness(18.0, 9.0, 18.0, 9.0),
				Child = child
			};
			Grid.SetRow(border, 1);
			grid.Children.Add(border);
		}
		Border child2 = new Border
		{
			Background = new SolidColorBrush(content.Paper.Paper()),
			CornerRadius = new CornerRadius(10.0),
			Child = grid,
			Width = 480.0,
			Height = 300.0
		};
		return new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 22, 22, 24)),
			CornerRadius = new CornerRadius(17.0),
			Padding = new Thickness(7.0),
			Child = child2,
			Width = 494.0,
			Height = 314.0
		};
		StackPanel Block((double Title, double Body, double Spacing) z)
		{
			TextBlock textBlock = Ui.Text(content.Title, z.Title, FontWeights.Bold, content.Paper.Ink(), TextAlignment.Left, 0, wrap: true);
			textBlock.FontFamily = new FontFamily("Segoe UI Variable Display");
			StackPanel stackPanel3 = new StackPanel
			{
				Spacing = z.Spacing,
				Children = { (UIElement)textBlock }
			};
			string summary = content.Summary;
			if (summary != null)
			{
				stackPanel3.Children.Add(Ui.Text(ArcCover.CardPitch(summary), z.Body, FontWeights.Medium, content.Paper.Ink().WithAlpha(0.72), TextAlignment.Left, 5, wrap: true));
			}
			return stackPanel3;
		}
	}

	public async Task TuckIntoNotchNowAsync()
	{
		List<ClickyTourJob> list = await PlugCardsIntoNotchAsync();
		if (list.Count > 0)
		{
			TrackCardAction("cards_tucked", new JsonObject((JsonNodeOptions?)null)
			{
				["count"] = list.Count,
				["trigger"] = "tuck_jobs"
			});
		}
		tuckedJobs.AddRange(list);
		Park(list.Where((ClickyTourJob j) => !startedJobKeys.Contains(j.RailKey)).ToList());
	}

	private async Task<List<ClickyTourJob>> PlugIntoNotchAsync()
	{
		List<ClickyTourJob> list = await PlugCardsIntoNotchAsync();
		if (list.Count > 0)
		{
			TrackCardAction("cards_tucked", new JsonObject((JsonNodeOptions?)null)
			{
				["count"] = list.Count,
				["trigger"] = "tour_end"
			});
		}
		List<ClickyTourJob> source = tuckedJobs.Concat(list).ToList();
		tuckedJobs = new List<ClickyTourJob>();
		placedCount = 0;
		List<ClickyTourJob> result = source.Where((ClickyTourJob j) => !startedJobKeys.Contains(j.RailKey) && !parkedJobKeys.Contains(j.RailKey)).ToList();
		startedJobKeys.Clear();
		parkedJobKeys.Clear();
		return result;
	}

	public async Task TuckRemainingCardsIntoSuggestionsAsync()
	{
		waitingForQuestions = false;
		Park(await PlugIntoNotchAsync());
		parkedJobKeys.Clear();
		if (parkedCountThisTour > 0)
		{
			TrackCardAction("cards_parked", new JsonObject((JsonNodeOptions?)null) { ["count"] = parkedCountThisTour });
		}
		parkedCountThisTour = 0;
	}

	private void Park(List<ClickyTourJob> jobs)
	{
		foreach (ClickyTourJob job in jobs)
		{
			if (parkedJobKeys.Add(job.RailKey))
			{
				ProactiveSuggestionCard card = job.SuggestionCard(SuggestionCheckReason);
				ProactiveSuggestionCard proactiveSuggestionCard = host?.Proactive?.AppendTourCard(card);
				if ((object)proactiveSuggestionCard != null)
				{
					parkedCountThisTour++;
					CreateWaitingJob?.Invoke(job.Title, proactiveSuggestionCard.Description, proactiveSuggestionCard.Id);
				}
			}
		}
	}

	private async Task<List<ClickyTourJob>> PlugCardsIntoNotchAsync()
	{
		List<Shown> cards = shown.ToList();
		shown.Clear();
		if (cards.Count == 0)
		{
			return new List<ClickyTourJob>();
		}
		if (PlaysSounds)
		{
			Canvas?.PlaySound("whoosh", 0.4f);
		}
		for (int i = 0; i < cards.Count; i++)
		{
			if (i > 0)
			{
				await Task.Delay(170);
			}
			FlyAsync(cards[i]);
		}
		await Task.Delay(650);
		Sounds.Play("clicky-text-receive", 0.35);
		return cards.Select((Shown c) => c.Job).ToList();
	}

	private async Task FlyAsync(Shown card)
	{
		(int X, int Y, int W, int H) rectPx = card.Window.RectPx;
		int item = rectPx.X;
		int item2 = rectPx.Y;
		double scale = card.Window.Scale;
		RectInt32 source = new RectInt32((int)Math.Round((double)item + 22.0 * scale), (int)Math.Round((double)item2 + 22.0 * scale), (int)Math.Round(335.92 * scale), (int)Math.Round(213.52 * scale));
		byte[] picture = null;
		try
		{
			picture = await RenderPngAsync(card.Laptop, source.Width, source.Height);
		}
		catch (Exception ex)
		{
			Log.Info("drawn tour", "card picture: " + ex.Message);
		}
		card.Window.HideWindow();
		Displays.Monitor monitor = Displays.At(source.X + source.Width / 2, source.Y + source.Height / 2);
		DisplayInfo display = new DisplayInfo(0, "", new RectD(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height), new RectD(monitor.Work.Left, monitor.Work.Top, monitor.Work.Width, monitor.Work.Height), monitor.Scale, IsPrimary: false);
		if (picture != null)
		{
			Canvas?.FlyIntoNotch(picture, source, display, delegate
			{
			});
		}
	}

	private static async Task<byte[]?> RenderPngAsync(FrameworkElement element, int widthPx, int heightPx)
	{
		RenderTargetBitmap bitmap = new RenderTargetBitmap();
		await bitmap.RenderAsync(element, widthPx, heightPx);
		IBuffer pixels = await bitmap.GetPixelsAsync();
		using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
		BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
		obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixels.ToArray());
		await obj.FlushAsync();
		byte[] bytes = new byte[stream.Size];
		stream.Seek(0uL);
		await stream.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);
		return bytes;
	}

	public void KeepCardsUntilQuestionsGo()
	{
		if (shown.Count != 0)
		{
			waitingForQuestions = true;
			After(125.0, ReleaseWaitingCards);
		}
	}

	private void ReleaseWaitingCards()
	{
		if (!waitingForQuestions)
		{
			return;
		}
		waitingForQuestions = false;
		List<Shown> list = shown.ToList();
		shown.Clear();
		placedCount = 0;
		int num = list.Count((Shown c) => !startedJobKeys.Contains(c.Job.RailKey));
		if (num > 0)
		{
			TrackCardAction("cards_moved_to_suggestions", new JsonObject((JsonNodeOptions?)null) { ["count"] = num });
		}
		foreach (Shown item in list)
		{
			if (!startedJobKeys.Contains(item.Job.RailKey))
			{
				host?.Proactive?.AppendTourCard(item.Job.SuggestionCard(SuggestionCheckReason));
			}
			OverlayWindow window = item.Window;
			UIElement uIElement = window.Root.Children.FirstOrDefault();
			if ((object)uIElement != null && !Anim.Instant)
			{
				Anim.Opacity(uIElement, 0f, 0.3, Easing.EaseInOut(Ui.Compositor));
				After(0.3, window.HideWindow);
			}
			else
			{
				window.HideWindow();
			}
		}
		startedJobKeys.Clear();
		tuckedJobs = new List<ClickyTourJob>();
	}

	public void NoteNextQuestions(string payload)
	{
		try
		{
			if (!(JsonNode.Parse(payload, null) is JsonArray))
			{
				return;
			}
		}
		catch (JsonException)
		{
			return;
		}
		nextQuestions = DrawnTourBriefs.ParseNextQuestions(payload).ToList();
	}

	public void HoldNextQuestions(DisplayInfo? display)
	{
		if (nextQuestions.Count != 0)
		{
			heldQuestionsDisplay = display;
			hasHeldQuestions = true;
		}
	}

	public void PopHeldNextQuestions(double afterDelay = 0.15)
	{
		if (hasHeldQuestions)
		{
			hasHeldQuestions = false;
			PopNextQuestions(heldQuestionsDisplay, afterDelay);
			heldQuestionsDisplay = null;
		}
	}

	public void PopNextQuestions(DisplayInfo? display, double afterDelay = 0.15)
	{
		List<(string, string)> list = nextQuestions;
		nextQuestions = new List<(string, string)>();
		RetireQuestionsPanel();
		if (list.Count == 0)
		{
			return;
		}
		ScreenDrawingCanvas canvas = Canvas;
		if (canvas == null || host == null)
		{
			return;
		}
		Displays.Monitor monitor;
		if ((object)display != null)
		{
			monitor = Displays.At((int)(display.BoundsPx.X + display.BoundsPx.Width / 2.0), (int)(display.BoundsPx.Y + display.BoundsPx.Height / 2.0));
		}
		else
		{
			monitor = Displays.UnderCursor();
		}
		Displays.Monitor monitor2 = monitor;
		Dictionary<string, string> plansByText = new Dictionary<string, string>();
		foreach (var (key, value) in list)
		{
			plansByText.TryAdd(key, value);
		}
		bool isMorning = IsMorningContext;
		TrackFlowAction("next_questions_shown", isMorning, new JsonObject((JsonNodeOptions?)null) { ["count"] = list.Count });
		List<(string, string)> list2 = list.Select<(string, string), (string, string)>(((string Text, string Plan) q) => (Title: q.Text, Kind: "ask")).Append(("Clear", "dismiss")).ToList();
		List<(double, double)> list3 = list2.Select<(string, string), (double, double)>(((string Title, string Kind) b) => canvas.StandaloneButtonSize(b.Title, b.Kind)).ToList();
		double s = monitor2.Scale;
		double num = (double)monitor2.Bounds.Left / s;
		double num2 = (double)monitor2.Bounds.Top / s;
		double num3 = (double)monitor2.Bounds.Width / s;
		double num4 = list3.Sum<(double, double)>(((double Width, double Height) z) => z.Width) + 4.0 * (double)(list3.Count - 1);
		double num5 = ((double)monitor2.Work.Top + (double)monitor2.Work.Height / 2.0) / s - num2;
		List<(double, double, double, double)> list4 = new List<(double, double, double, double)>();
		if (num4 <= num3 - 64.0)
		{
			double num6 = num3 / 2.0 - num4 / 2.0;
			foreach (var item2 in list3)
			{
				list4.Add((num6, num5 - item2.Item2 / 2.0, item2.Item1, item2.Item2));
				num6 += item2.Item1 + 4.0;
			}
		}
		else
		{
			double num7 = list3.Sum<(double, double)>(((double Width, double Height) z) => z.Height) + 4.0 * (double)(list3.Count - 1);
			double num8 = num5 - num7 / 2.0;
			double item = list3.FirstOrDefault().Item2;
			double num9 = num8 + ((item > 0.0) ? item : 48.0) / 2.0;
			foreach (var item3 in list3)
			{
				list4.Add((num3 / 2.0 - item3.Item1 / 2.0, num9 - item3.Item2 / 2.0, item3.Item1, item3.Item2));
				num9 += item3.Item2 + 4.0;
			}
		}
		int generation = ++questionsGeneration;
		for (int i = 0; i < list2.Count; i++)
		{
			(string, string) tuple2 = list2[i];
			string title = tuple2.Item1;
			string kind = tuple2.Item2;
			(double, double, double, double) tuple3 = list4[i];
			RectInt32 rect = new RectInt32((int)Math.Round((num + tuple3.Item1) * s), (int)Math.Round((num2 + tuple3.Item2) * s), (int)Math.Round(tuple3.Item3 * s), (int)Math.Round(tuple3.Item4 * s));
			int index = i;
			After(afterDelay + (double)index * 0.32, delegate
			{
				if (questionsGeneration == generation)
				{
					questionButtons.Add(canvas.ShowStandaloneButton(title, kind, rect, s, delegate
					{
						Dq.TryEnqueue(delegate
						{
							RetireNextQuestions();
							if (kind != "ask")
							{
								TrackFlowAction("next_questions_cleared", isMorning);
							}
							else
							{
								TrackFlowAction("next_question_tapped", isMorning, new JsonObject((JsonNodeOptions?)null)
								{
									["index"] = index,
									["has_plan"] = plansByText.TryGetValue(title, out string value2) && value2.Length > 0
								});
								host.Real?.Cursor.HideMessage();
								pendingPillPlanNote = ((plansByText.TryGetValue(title, out string value3) && value3.Length > 0) ? $"[Pill] They tapped your pill \"{title}\". When you wrote it, this is how you planned to answer it: {value3} Do that, unless their screen or what they've said since points somewhere better." : null);
								host.Services.Companion.AskFromDrawingAsync(title, "button");
							}
						});
					}));
					if (PlaysSounds)
					{
						canvas.PlaySound("badge-pop", 0.55f);
						if (index == 0)
						{
							canvas.PlaySound("sparkle", 0.35f);
						}
					}
				}
			});
		}
		After(120.0, delegate
		{
			if (questionsGeneration == generation)
			{
				TrackFlowAction("next_questions_timed_out", isMorning);
				RetireNextQuestions();
			}
		});
	}

	public string? TakePendingPillPlanNote()
	{
		return Interlocked.Exchange(ref pendingPillPlanNote, null);
	}

	private void RetireNextQuestions()
	{
		ReleaseWaitingCards();
		RetireQuestionsPanel();
	}

	private void RetireQuestionsPanel()
	{
		questionsGeneration++;
		List<IDisposable> list = questionButtons.ToList();
		questionButtons.Clear();
		foreach (IDisposable item in list)
		{
			item.Dispose();
		}
	}

	public void DismissAll()
	{
		foreach (Shown item in shown)
		{
			item.Window.HideWindow();
		}
		shown.Clear();
		tuckedJobs = new List<ClickyTourJob>();
		startedJobKeys.Clear();
		parkedJobKeys.Clear();
		nextQuestions = new List<(string, string)>();
		hasHeldQuestions = false;
		heldQuestionsDisplay = null;
		RetireNextQuestions();
		placedCount = 0;
		placedIndexByKey.Clear();
		cardKeysClickedDuringTour.Clear();
		parkedCountThisTour = 0;
	}

	private void After(double seconds, Action action)
	{
		if (host != null)
		{
			DispatcherQueueTimer t = Dq.CreateTimer().Rooted();
			t.Interval = TimeSpan.FromSeconds(Math.Max(0.001, seconds));
			t.IsRepeating = false;
			pending.Add(t);
			t.Tick += delegate
			{
				t.Stop();
				pending.Remove(t);
				action();
			};
			t.Start();
		}
	}
}
