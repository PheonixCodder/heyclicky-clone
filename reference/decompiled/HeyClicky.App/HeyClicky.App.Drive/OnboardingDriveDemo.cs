using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Drive;
using HeyClicky.Platform.Input;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Drive;

public sealed class OnboardingDriveDemo
{
	public enum DemoState
	{
		Idle,
		Driving,
		Done,
		HandedBack
	}

	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	public static readonly OnboardingDriveDemo Shared = new OnboardingDriveDemo();

	private readonly string[] rows = new string[3] { "", "", "" };

	private readonly bool[] checks = new bool[3];

	private readonly Dictionary<string, WeakReference<FrameworkElement>> targets = new Dictionary<string, WeakReference<FrameworkElement>>();

	private CancellationTokenSource? driveCancellation;

	private TakeOverOverlay? overlay;

	private OnboardingDriveContext? running;

	private DateTimeOffset? analyticsStartedAt;

	private static readonly string[] DemoLines = new string[3] { "Show Clicky what you're working on", "Ask Clicky when you're stuck", "Let Clicky do the clicking" };

	private static readonly string[] StepLines = new string[3] { "First, I'll write you a little list.", "Next row: when to ask me.", "And the last one, the clicking." };

	public const double TitleBarHeight = 30.0;

	public const double CaptionButtonWidth = 34.0;

	private static readonly string[] CaptionGlyphs = new string[3] { "\ue921", "\ue922", "\ue8bb" };

	public DemoState State { get; private set; }

	public IReadOnlyList<string> Rows => rows;

	public IReadOnlyList<bool> Checks => checks;

	public event Action? Changed;

	private void TrackEnded(string outcome)
	{
		DateTimeOffset? dateTimeOffset = analyticsStartedAt;
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			analyticsStartedAt = null;
			ClickyDriveAnalytics.TrackOnboardingDriveEnded(outcome, (DateTimeOffset.Now - valueOrDefault).TotalSeconds);
		}
	}

	public void Reset()
	{
		driveCancellation?.Cancel();
		driveCancellation = null;
		for (int i = 0; i < 3; i++)
		{
			rows[i] = "";
			checks[i] = false;
		}
		State = DemoState.Idle;
		this.Changed?.Invoke();
	}

	public string? StatusLine(bool stepCompleted, bool listening)
	{
		if (State != DemoState.Driving)
		{
			if (!stepCompleted)
			{
				if (!listening)
				{
					return "waiting for you…";
				}
				return "listening…";
			}
			return null;
		}
		return "driving… press esc to take it back";
	}

	public void Run(OnboardingDriveContext context)
	{
		if (State != DemoState.Driving)
		{
			DemoState state = State;
			bool isRepeat = (uint)(state - 2) <= 1u;
			Reset();
			State = DemoState.Driving;
			analyticsStartedAt = DateTimeOffset.Now;
			ClickyDriveAnalytics.TrackOnboardingDriveStarted(isRepeat);
			this.Changed?.Invoke();
			running = context;
			RunAsync(context, (driveCancellation = new CancellationTokenSource()).Token);
		}
	}

	private async Task RunAsync(OnboardingDriveContext context, CancellationToken ct)
	{
		DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
		_ = SynchronizationContext.Current;
		try
		{
			TakeOverOverlay pill = (overlay = new TakeOverOverlay());
			pill.Show(context.Display, context.Accent, "Getting started");
			context.PlayChime("agent-launch", 0.5);
			string firstName = context.FirstName;
			string line = ((firstName != null && firstName.Length > 0) ? ("Alright " + firstName + ", I'm taking over your mouse now. Just watch me.") : "Alright, I'm taking over your mouse now. Just watch me.");
			await SayStepAsync(context, line, "Taking over your mouse", pill, 1.6, ct);
			await WaitForLineToFinishAsync(context, 5.0, ct);
			if (ct.IsCancellationRequested || State != DemoState.Driving)
			{
				return;
			}
			ClickyUserInputBlocker.Shared.Start(45.0, delegate
			{
				dispatcher.TryEnqueue(StopForEscape);
			}, delegate(Action action)
			{
				dispatcher.TryEnqueue(delegate
				{
					action();
				});
			});
			await Task.Delay(300, ct);
			for (int index = 0; index < DemoLines.Length; index++)
			{
				if (ct.IsCancellationRequested)
				{
					break;
				}
				PointD? pointD = ScreenCenter($"row{index}.field");
				if (!pointD.HasValue)
				{
					break;
				}
				PointD point = pointD.GetValueOrDefault();
				await SayStepAsync(context, StepLines[index], null, pill, 1.6, ct);
				pill.SetActing(acting: true);
				await ClickyScreenActuator.GlideAsync(point, (index == 0) ? 0.85 : 0.6, ct);
				await Task.Delay(180, ct);
				await ClickyScreenActuator.ClickAsync(point);
				await Task.Delay(200, ct);
				string text = DemoLines[index];
				foreach (char c in text)
				{
					if (ct.IsCancellationRequested)
					{
						break;
					}
					await ClickyScreenActuator.TypeAsync(c.ToString(), ct);
					await Task.Delay(Random.Shared.Next(15, 36), ct);
				}
				pill.SetActing(acting: false);
				await Task.Delay(350, ct);
			}
			if (!ct.IsCancellationRequested)
			{
				await SayStepAsync(context, "Now I'll tick off the first two.", null, pill, 1.6, ct);
			}
			for (int index = 0; index < 2; index++)
			{
				if (ct.IsCancellationRequested)
				{
					break;
				}
				PointD? pointD = ScreenCenter($"row{index}.check");
				if (!pointD.HasValue)
				{
					break;
				}
				PointD point = pointD.GetValueOrDefault();
				pill.SetActing(acting: true);
				await ClickyScreenActuator.GlideAsync(point, 0.55, ct);
				await Task.Delay(180, ct);
				await ClickyScreenActuator.ClickAsync(point);
				context.PlayDrawingSound?.Invoke("copy-tick", 0.22f);
				pill.SetActing(acting: false);
				await Task.Delay(400, ct);
			}
			if (ct.IsCancellationRequested)
			{
				pill.Hide(animated: false);
				return;
			}
			ClickyUserInputBlocker.Shared.Stop();
			context.PlayChime("agent-done", 0.55);
			pill.SetStatus("Done");
			await Task.Delay(600, ct);
			pill.Hide();
			overlay = null;
			State = DemoState.Done;
			driveCancellation = null;
			TrackEnded("completed");
			this.Changed?.Invoke();
			await context.SpeakLine("That was me on your mouse. Ask me to do it for you, in any app, and I will.");
			context.StepCompleted();
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Log.Error("drive", "the tour's drive failed", ex2);
		}
		finally
		{
			ClickyUserInputBlocker.Shared.Stop();
			if (ct.IsCancellationRequested)
			{
				overlay?.Hide(animated: false);
			}
		}
	}

	private static async Task SayStepAsync(OnboardingDriveContext context, string line, string? pillText, TakeOverOverlay pill, double maximumWaitSeconds, CancellationToken ct)
	{
		pill.SetStatus(pillText ?? line);
		int before = context.AudioPlaybackBeganCount();
		DateTimeOffset deadline = DateTimeOffset.Now.AddSeconds(maximumWaitSeconds);
		await context.SpeakLine(line);
		while (context.AudioPlaybackBeganCount() <= before && DateTimeOffset.Now < deadline)
		{
			await Task.Delay(50, ct);
		}
	}

	private static async Task WaitForLineToFinishAsync(OnboardingDriveContext context, double maximumSeconds, CancellationToken ct)
	{
		DateTimeOffset deadline = DateTimeOffset.Now.AddSeconds(maximumSeconds);
		DateTimeOffset? quietSince = null;
		while (DateTimeOffset.Now < deadline && !ct.IsCancellationRequested)
		{
			if (context.IsResponding())
			{
				quietSince = null;
			}
			else if (quietSince.HasValue)
			{
				DateTimeOffset valueOrDefault = quietSince.GetValueOrDefault();
				if (DateTimeOffset.Now - valueOrDefault >= TimeSpan.FromSeconds(0.25))
				{
					break;
				}
			}
			else
			{
				quietSince = DateTimeOffset.Now;
			}
			await Task.Delay(50, ct);
		}
	}

	private void StopForEscape()
	{
		if (State == DemoState.Driving)
		{
			driveCancellation?.Cancel();
			driveCancellation = null;
			overlay?.Hide(animated: false);
			overlay = null;
			running?.PlayChime("agent-close", 0.5);
			State = DemoState.HandedBack;
			TrackEnded("escaped");
			this.Changed?.Invoke();
			running?.StepCompleted();
		}
	}

	public void Cancel()
	{
		if (State == DemoState.Driving)
		{
			ClickyUserInputBlocker.Shared.Stop();
			overlay?.Hide(animated: false);
			overlay = null;
			TrackEnded("cancelled");
			Reset();
		}
	}

	public PointD? ScreenCenter(string id)
	{
		if (!targets.TryGetValue(id, out WeakReference<FrameworkElement> value) || !value.TryGetTarget(out var target) || (object)target.XamlRoot == null)
		{
			return null;
		}
		try
		{
			Point point = target.TransformToVisual(null).TransformPoint(new Point(target.ActualWidth / 2.0, target.ActualHeight / 2.0));
			double rasterizationScale = target.XamlRoot.RasterizationScale;
			nint windowFromWindowId = Win32Interop.GetWindowFromWindowId(target.XamlRoot.ContentIslandEnvironment.AppWindowId);
			NativePoint point2 = default(NativePoint);
			ClientToScreen(windowFromWindowId, ref point2);
			return new PointD((double)point2.X + point.X * rasterizationScale, (double)point2.Y + point.Y * rasterizationScale);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "target " + id + " not placed: " + ex.Message);
			return null;
		}
	}

	private void Register(FrameworkElement element, string id)
	{
		targets[id] = new WeakReference<FrameworkElement>(element);
	}

	public FrameworkElement CreateWindowView(Color accent)
	{
		Grid grid = new Grid
		{
			Height = 30.0,
			Padding = new Thickness(12.0, 0.0, 0.0, 0.0),
			Background = Ui.Brush(DS.Rgb(0.92, 0.92, 0.92)),
			IsHitTestVisible = false
		};
		TextBlock textBlock = Ui.Text("Your list", 12.0, FontWeights.SemiBold, DS.Black(0.55));
		textBlock.HorizontalAlignment = HorizontalAlignment.Left;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(textBlock);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		string[] captionGlyphs = CaptionGlyphs;
		foreach (string glyph in captionGlyphs)
		{
			Grid grid2 = new Grid
			{
				Width = 34.0,
				Height = 30.0
			};
			grid2.Children.Add(Icons.Glyph(glyph, 10.0, DS.Black(0.55)));
			stackPanel.Children.Add(grid2);
		}
		grid.Children.Add(stackPanel);
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 8.0,
			Padding = new Thickness(14.0)
		};
		for (int j = 0; j < 3; j++)
		{
			stackPanel2.Children.Add(Row(j, accent));
		}
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Children.Add(grid);
		stackPanel3.Children.Add(stackPanel2);
		Border border = new Border
		{
			Width = 340.0,
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(DS.Rgb(0.98, 0.98, 0.98)),
			BorderBrush = Ui.Brush(DS.Black(0.12)),
			BorderThickness = new Thickness(1.0),
			Child = stackPanel3,
			RequestedTheme = ElementTheme.Light
		};
		Ui.ClipRounded(border, 12.0);
		return new Grid
		{
			Width = 340.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			Children = 
			{
				(UIElement)new ShadowLayer(12.0, DS.Black(0.3), 14.0, 6.0),
				(UIElement)border
			}
		};
	}

	private FrameworkElement Row(int index, Color accent)
	{
		Grid row = new Grid
		{
			Height = 34.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			ColumnSpacing = 10.0,
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(DS.Black(0.04))
		};
		row.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		row.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Pressable pressable = new Pressable
		{
			Width = 20.0,
			Height = 20.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		Ellipse ring = new Ellipse
		{
			Width = 16.0,
			Height = 16.0,
			Stroke = Ui.Brush(DS.Black(0.3)),
			StrokeThickness = 1.6
		};
		TextBlock filled = Icons.Glyph("\uec61", 18.0, accent);
		filled.HorizontalAlignment = HorizontalAlignment.Center;
		filled.VerticalAlignment = VerticalAlignment.Center;
		pressable.Children.Add(ring);
		pressable.Children.Add(filled);
		pressable.Click += delegate
		{
			checks[index] = !checks[index];
			this.Changed?.Invoke();
		};
		row.Children.Add(pressable);
		Register(pressable, $"row{index}.check");
		TextBox field = new TextBox
		{
			PlaceholderText = "New item",
			FontSize = 14.0,
			FontWeight = FontWeights.Medium,
			FontFamily = DS.Fonts.For(14.0),
			BorderThickness = new Thickness(0.0),
			Background = Ui.Transparent,
			Padding = new Thickness(0.0),
			MinHeight = 0.0,
			VerticalAlignment = VerticalAlignment.Center,
			IsSpellCheckEnabled = false,
			Foreground = Ui.Brush(DS.Black(0.85))
		};
		string[] array = new string[5] { "TextControlBackgroundFocused", "TextControlBackgroundPointerOver", "TextControlBorderBrushFocused", "TextControlBorderBrushPointerOver", "TextControlBorderBrush" };
		foreach (string key in array)
		{
			field.Resources[key] = Ui.Transparent;
		}
		field.Resources["TextControlForegroundFocused"] = Ui.Brush(DS.Black(0.85));
		field.Resources["TextControlForegroundPointerOver"] = Ui.Brush(DS.Black(0.85));
		field.TextChanged += delegate
		{
			rows[index] = field.Text;
		};
		field.Loaded += delegate
		{
			if (FindDescendant(field, "DeleteButton") is FrameworkElement frameworkElement)
			{
				frameworkElement.Width = 0.0;
				frameworkElement.Opacity = 0.0;
			}
		};
		TextBlock struck = Ui.Text("", 14.0, FontWeights.Medium, DS.Black(0.4));
		struck.TextDecorations = TextDecorations.Strikethrough;
		struck.VerticalAlignment = VerticalAlignment.Center;
		struck.Visibility = Visibility.Collapsed;
		Grid grid = new Grid();
		grid.Children.Add(field);
		grid.Children.Add(struck);
		Grid.SetColumn(grid, 1);
		row.Children.Add(grid);
		Register(field, $"row{index}.field");
		Sync();
		Changed += delegate
		{
			row.DispatcherQueue.TryEnqueue(Sync);
		};
		return row;
		void Sync()
		{
			filled.Opacity = (checks[index] ? 1 : 0);
			ring.Opacity = ((!checks[index]) ? 1 : 0);
			struck.Text = rows[index];
			struck.Visibility = ((!checks[index]) ? Visibility.Collapsed : Visibility.Visible);
			field.Opacity = ((!checks[index]) ? 1 : 0);
			if (field.Text != rows[index])
			{
				field.Text = rows[index];
			}
		}
	}

	private static DependencyObject? FindDescendant(DependencyObject root, string name)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is FrameworkElement { Name: var name2 } && name2 == name)
			{
				return child;
			}
			DependencyObject dependencyObject = FindDescendant(child, name);
			if ((object)dependencyObject != null)
			{
				return dependencyObject;
			}
		}
		return null;
	}

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(nint hwnd, ref NativePoint point);
}
