using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using HeyClicky.Drawing;
using HeyClicky.Drawing.Media;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ClickyDrawingWindowView : Grid
{
	private sealed class DownloadButton : Pressable
	{
		public enum Phase
		{
			Idle,
			Recording,
			Saving,
			Saved,
			Failed
		}

		private readonly StackPanel content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		private readonly Color ink;

		private TextBlock? progressText;

		public Phase State { get; private set; }

		public string? SavedPath { get; private set; }

		public DownloadButton(Color ink)
		{
			this.ink = ink.WithAlpha(0.8);
			base.Height = 22.0;
			base.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			base.CornerRadius = new CornerRadius(11.0);
			base.Background = Ui.Brush(DS.White(0.55));
			base.BorderBrush = Ui.Brush(DS.Black(0.1));
			base.BorderThickness = new Thickness(0.5);
			base.Children.Add(content);
			Show(Phase.Idle);
		}

		public void Show(Phase phase, string? savedPath = null)
		{
			State = phase;
			SavedPath = savedPath;
			content.Children.Clear();
			progressText = null;
			switch (phase)
			{
			case Phase.Idle:
				content.Children.Add(Glyph("\ue896", 11.0));
				content.Children.Add(Label("Download"));
				break;
			case Phase.Recording:
				content.Children.Add(Spinner());
				content.Children.Add(progressText = Label("Recording 0:00 / 0:00"));
				break;
			case Phase.Saving:
				content.Children.Add(Spinner());
				content.Children.Add(Label("Saving…"));
				break;
			case Phase.Saved:
				content.Children.Add(Glyph("\ue73e", 11.0));
				content.Children.Add(Label("In Downloads"));
				break;
			case Phase.Failed:
				content.Children.Add(Glyph("\ue7ba", 10.5));
				content.Children.Add(Label("Try again"));
				break;
			}
			bool flag = (uint)(phase - 1) <= 1u;
			base.Enabled = !flag;
			ToolTipService.SetToolTip(this, phase switch
			{
				Phase.Recording => "Recording it as it plays…", 
				Phase.Saving => "Saving the video…", 
				Phase.Saved => "Show the video in Explorer", 
				_ => "Save this drawing as a video in your Downloads folder (it plays through once while it records)", 
			});
		}

		public void ShowRecordingProgress(double position, double duration)
		{
			if ((object)progressText != null)
			{
				progressText.Text = "Recording " + ClickyDrawingLaptopScreen.LengthText(position) + " / " + ClickyDrawingLaptopScreen.LengthText(duration);
			}
		}

		private TextBlock Label(string text)
		{
			TextBlock textBlock = Ui.Text(text, 11.5, FontWeights.SemiBold, ink);
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			Typography.SetNumeralAlignment(textBlock, FontNumeralAlignment.Tabular);
			return textBlock;
		}

		private TextBlock Glyph(string glyph, double size)
		{
			TextBlock textBlock = Icons.Glyph(glyph, size, ink);
			textBlock.FontWeight = FontWeights.Bold;
			return textBlock;
		}

		private ProgressRing Spinner()
		{
			return new ProgressRing
			{
				Width = 10.0,
				Height = 10.0,
				MinWidth = 0.0,
				MinHeight = 0.0,
				IsActive = true,
				Foreground = Ui.Brush(ink),
				VerticalAlignment = VerticalAlignment.Center
			};
		}
	}

	private sealed class SpokenCaption : Grid
	{
		private static readonly (double White, double Opacity) Unspoken = (White: 0.56, Opacity: 0.9);

		private static readonly (double White, double Opacity) Spoken = (White: 1.0, Opacity: 0.96);

		private readonly StoredLesson.Meta lesson;

		private readonly double shift;

		private readonly TextBlock text;

		private int? shownBeat = -2;

		public SpokenCaption(StoredLesson.Meta lesson, double shift)
		{
			this.lesson = lesson;
			this.shift = shift;
			text = new TextBlock
			{
				FontSize = 17.0,
				FontWeight = FontWeights.Medium,
				FontFamily = DS.Fonts.Text,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.WrapWholeWords,
				MaxLines = 2,
				TextTrimming = TextTrimming.CharacterEllipsis,
				IsTextScaleFactorEnabled = false,
				LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
				LineHeight = 23.4
			};
			base.Children.Add(text);
		}

		public void Show(int? beat, double position)
		{
			if (beat != shownBeat)
			{
				shownBeat = beat;
				text.Opacity = 0.0;
				Anim.Opacity(text, 1f, 0.2, Easing.EaseOut(Ui.Compositor));
			}
			text.Inlines.Clear();
			if (beat.HasValue)
			{
				int valueOrDefault = beat.GetValueOrDefault();
				if (valueOrDefault >= 0 && valueOrDefault < lesson.Lines.Count)
				{
					string[] array = lesson.Lines[valueOrDefault].Text.Split(' ');
					double num = SpokenFraction(valueOrDefault, position + shift);
					double num2 = Math.Max(1.0, array.Sum((string w) => (double)w.Length + 1.0));
					double num3 = 0.0;
					for (int i = 0; i < array.Length; i++)
					{
						double num4 = (double)array[i].Length + 1.0;
						double num5 = num3 / num2;
						double num6 = (num3 + num4) / num2;
						num3 += num4;
						double progress = Math.Clamp((num - num5) / Math.Max(0.0001, num6 - num5), 0.0, 1.0);
						text.Inlines.Add(Run((i + 1 < array.Length) ? (array[i] + " ") : array[i], progress));
					}
					return;
				}
			}
			if (lesson.Question.Length > 0)
			{
				text.Inlines.Add(Run("“" + lesson.Question + "”", 0.0));
			}
		}

		private double SpokenFraction(int beat, double time)
		{
			StoredLesson.LineData lineData = lesson.Lines[beat];
			double num = lineData.Length ?? ((beat + 1 < lesson.Lines.Count) ? (lesson.Lines[beat + 1].Start - lineData.Start) : ((double)lineData.Text.Length * 0.065));
			if (num <= 0.0)
			{
				return 1.0;
			}
			return Math.Clamp((time - lineData.Start) / num, 0.0, 1.0);
		}

		private static Run Run(string words, double progress)
		{
			double num = Unspoken.White + (Spoken.White - Unspoken.White) * progress;
			double a = Unspoken.Opacity + (Spoken.Opacity - Unspoken.Opacity) * progress;
			return new Run
			{
				Text = words,
				Foreground = Ui.Brush(DS.Rgb(num, num, num, a))
			};
		}
	}

	private sealed class BeatScrubber : Grid
	{
		private const double Spacing = 3.0;

		private readonly ClickyLessonInlinePlayer player;

		private readonly List<(double Start, double End, Pressable Hit, Border Played)> segments = new List<(double, double, Pressable, Border)>();

		private readonly StackPanel row = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 3.0
		};

		private double position;

		public BeatScrubber(ClickyLessonInlinePlayer player)
		{
			BeatScrubber beatScrubber = this;
			this.player = player;
			List<StoredLesson.LineData> lines = player.Lesson.Lines;
			double replayShift = player.Stored.ReplayShift;
			double num = Math.Max(player.Duration, 0.1);
			for (int i = 0; i < lines.Count; i++)
			{
				double item = ((i == 0) ? 0.0 : (lines[i].Start - replayShift));
				double item2 = ((i + 1 < lines.Count) ? (lines[i + 1].Start - replayShift) : num);
				Border border = new Border
				{
					Height = 6.0,
					CornerRadius = new CornerRadius(3.0),
					Background = Ui.Brush(DS.White(0.85)),
					HorizontalAlignment = HorizontalAlignment.Left,
					Width = 0.0
				};
				Pressable pressable = new Pressable
				{
					Height = 14.0
				};
				pressable.Children.Add(new Border
				{
					Height = 6.0,
					CornerRadius = new CornerRadius(3.0),
					Background = Ui.Brush(DS.White(0.14)),
					VerticalAlignment = VerticalAlignment.Center
				});
				border.VerticalAlignment = VerticalAlignment.Center;
				pressable.Children.Add(border);
				int beat = i;
				pressable.Click += delegate
				{
					player.SeekToBeat(beat);
				};
				ToolTipService.SetToolTip(pressable, lines[i].Text);
				segments.Add((item, item2, pressable, border));
				row.Children.Add(pressable);
			}
			base.Children.Add(row);
			base.SizeChanged += delegate
			{
				beatScrubber.Layout();
			};
		}

		public FrameworkElement? SegmentForTests(int beat)
		{
			if (beat < 0 || beat >= segments.Count)
			{
				return null;
			}
			return segments[beat].Hit;
		}

		private void Layout()
		{
			double num = Math.Max(player.Duration, 0.1);
			double num2 = Math.Max(0.0, base.ActualWidth - 3.0 * (double)Math.Max(0, segments.Count - 1));
			foreach (var segment in segments)
			{
				segment.Hit.Width = Math.Max(4.0, num2 * (segment.End - segment.Start) / num);
			}
			Show(position);
		}

		public void Show(double position)
		{
			this.position = position;
			foreach (var segment in segments)
			{
				double item = segment.Start;
				double item2 = segment.End;
				Pressable item3 = segment.Hit;
				Border item4 = segment.Played;
				double num = Math.Clamp((position - item) / Math.Max(0.01, item2 - item), 0.0, 1.0);
				item4.Width = Math.Max(0.0, (double.IsNaN(item3.Width) ? 0.0 : item3.Width) * num);
			}
		}
	}

	private const double TitleBarHeight = 34.0;

	private const double ControlsHeight = 132.0;

	private const double Bezel = 10.0;

	private const double ScreenCornerRadius = 12.0;

	private static readonly SwiftSpring Showing = new SwiftSpring(0.38, 0.86);

	private static readonly ClickyHomeSpreadPaper Paper = ClickyDrawingLaptopScreen.Paper;

	private readonly ClickyLessonInlinePlayer player;

	private readonly Action close;

	private readonly Vector2 stageSize;

	private readonly Microsoft.UI.Dispatching.DispatcherQueue dq;

	private readonly Border dim;

	private readonly Grid card;

	private readonly Border wash = new Border();

	private readonly Image picture = new Image
	{
		Stretch = Stretch.Uniform
	};

	private readonly Grid stageOverlay;

	private readonly GelSurface stageGel;

	private readonly TextBlock stageGlyph;

	private readonly Grid controls;

	private readonly SpokenCaption caption;

	private readonly BeatScrubber scrubber;

	private readonly TextBlock timeLabel;

	private readonly Pressable playPause;

	private readonly TextBlock playPauseGlyph;

	private readonly Control focusTarget = new ContentControl
	{
		IsTabStop = true,
		Width = 1.0,
		Height = 1.0,
		Opacity = 0.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private readonly DownloadButton download;

	private WriteableBitmap? frameBitmap;

	private LessonPlaybackFrame? pendingFrame;

	private LessonVideoRecorder? recorder;

	private CancellationTokenSource? downloadCancellation;

	private bool showsStageButton = true;

	public static Vector2 StageSize(Vector2 screenSize, StoredLesson stored)
	{
		StoredLesson.Meta data = stored.Data;
		double num = ((data.DisplayBounds.Length == 4 && data.DisplayBounds[3] > 0.0) ? (data.DisplayBounds[2] / data.DisplayBounds[3]) : ((data.CanvasHeight > 0) ? ((double)data.CanvasWidth / (double)data.CanvasHeight) : 1.6));
		double val = Math.Min(screenSize.X - 200f, 1500f);
		double num2 = (double)(screenSize.Y - 160f) - 34.0 - 132.0;
		double num3 = Math.Min(val, num2 * num);
		return new Vector2((float)Math.Round(num3), (float)Math.Round(num3 / num));
	}

	public ClickyDrawingWindowView(ClickyLessonInlinePlayer player, Vector2 screenSize, Vector2 stageSize, double displayScale, Action close, Action playFullScreen)
	{
		ClickyDrawingWindowView clickyDrawingWindowView = this;
		this.player = player;
		this.close = close;
		this.stageSize = stageSize;
		dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		base.Width = screenSize.X;
		base.Height = screenSize.Y;
		base.RequestedTheme = ElementTheme.Light;
		dim = new Border
		{
			Background = Ui.Brush(DS.Hex(263692u, 0.82)),
			Opacity = 0.0
		};
		dim.Tapped += delegate(object _, TappedRoutedEventArgs e)
		{
			e.Handled = true;
			close();
		};
		base.Children.Add(dim);
		base.Children.Add(focusTarget);
		double width = (double)stageSize.X + 20.0;
		double height = 34.0 + (double)stageSize.Y + 132.0 + 20.0;
		card = new Grid
		{
			Width = width,
			Height = height,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.0
		};
		card.Children.Add(new ShadowLayer(22.0, DS.Black(0.5), 40.0, 20.0));
		card.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(22.0),
			Background = Ui.Vertical((0.0, DS.Rgb(0.16, 0.16, 0.16)), (1.0, DS.Rgb(0.07, 0.07, 0.07))),
			BorderBrush = Ui.Brush(DS.White(0.16)),
			BorderThickness = new Thickness(1.0)
		});
		Grid grid = new Grid
		{
			Margin = new Thickness(10.0),
			Background = Ui.Brush(DS.Rgb(0.09, 0.09, 0.09))
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(34.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(stageSize.Y)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(132.0)
		});
		Ui.ClipRounded(grid, 12.0);
		card.Children.Add(grid);
		base.Children.Add(card);
		Grid grid2 = new Grid
		{
			Background = Ui.Transparent
		};
		grid2.Children.Add(new Border
		{
			Background = Ui.Horizontal((0.0, DS.Hex(14079447u)), (0.5, DS.Hex(15790320u)), (1.0, DS.Hex(14079447u)))
		});
		wash.Background = Ui.Brush(Paper.TitleWash.WithAlpha(0.62));
		grid2.Children.Add(wash);
		grid2.Children.Add(new Border
		{
			Height = 0.5,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Ui.Brush(DS.Black(0.1))
		});
		CaptionButton captionButton = new CaptionButton(CaptionButton.Kind.Maximize, 34.0, Paper.Ink.WithAlpha(0.8), "Play it full screen over your screen");
		captionButton.Click += playFullScreen;
		CaptionButton captionButton2 = new CaptionButton(CaptionButton.Kind.Close, 34.0, Paper.Ink.WithAlpha(0.8), "Close (esc)");
		captionButton2.Click += close;
		grid2.Children.Add(new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Children = 
			{
				(UIElement)captionButton,
				(UIElement)captionButton2
			}
		});
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (player.Lesson.HasNarration)
		{
			TextBlock textBlock = Icons.Glyph("\ue767", 10.5, Paper.Ink.WithAlpha(0.55));
			textBlock.FontWeight = FontWeights.SemiBold;
			stackPanel.Children.Add(textBlock);
		}
		TextBlock textBlock2 = Ui.Text(ClickyDrawingLaptopScreen.LengthText(player.Lesson.Duration), 12.0, FontWeights.SemiBold, Paper.Ink.WithAlpha(0.55));
		Typography.SetNumeralAlignment(textBlock2, FontNumeralAlignment.Tabular);
		stackPanel.Children.Add(textBlock2);
		download = new DownloadButton(Paper.Ink);
		download.Click += DownloadButtonPressed;
		StackPanel item = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 104.0, 0.0),
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)download
			}
		};
		grid2.Children.Add(item);
		TextBlock textBlock3 = Ui.Text(player.Lesson.Title, 13.0, FontWeights.SemiBold, Paper.Ink.WithAlpha(0.85), TextAlignment.Center, 1);
		textBlock3.Margin = new Thickness(280.0, 0.0, 280.0, 0.0);
		textBlock3.VerticalAlignment = VerticalAlignment.Center;
		textBlock3.IsHitTestVisible = false;
		grid2.Children.Add(textBlock3);
		grid.Children.Add(grid2);
		Pressable pressable = new Pressable
		{
			Width = stageSize.X,
			Height = stageSize.Y,
			Background = Ui.Brush(Colors.White)
		};
		if (File.Exists(player.Stored.PosterPath))
		{
			picture.Source = new BitmapImage(new Uri(player.Stored.PosterPath));
		}
		else if (File.Exists(player.Stored.ScreenPath))
		{
			picture.Source = new BitmapImage(new Uri(player.Stored.ScreenPath));
		}
		pressable.Children.Add(picture);
		stageOverlay = new Grid
		{
			Width = 72.0,
			Height = 72.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false,
			CenterPoint = new Vector3(36f, 36f, 0f)
		};
		stageOverlay.Children.Add(new ShadowLayer(36.0, DS.Black(0.35), 10.0, 3.0));
		stageGel = new GelSurface(GelAppearance.PrimaryBlue, 36.0);
		stageOverlay.Children.Add(stageGel);
		stageGlyph = Icons.Glyph("\uf5b0", 26.0, DS.Black(0.82));
		stageGlyph.FontWeight = FontWeights.Bold;
		stageOverlay.Children.Add(stageGlyph);
		pressable.Children.Add(stageOverlay);
		pressable.HoverChanged += delegate(bool hovered)
		{
			clickyDrawingWindowView.stageGel.Hovered = hovered;
		};
		pressable.Click += delegate
		{
			if (!ClickyDrawingWindowViewer.Shared.IsRecording)
			{
				player.TogglePlay();
			}
		};
		Grid.SetRow(pressable, 1);
		grid.Children.Add(pressable);
		controls = new Grid
		{
			Padding = new Thickness(18.0, 0.0, 18.0, 16.0)
		};
		controls.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		controls.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(32.0)
		});
		caption = new SpokenCaption(player.Lesson, player.Stored.ReplayShift);
		controls.Children.Add(caption);
		Grid grid3 = new Grid
		{
			ColumnSpacing = 14.0
		};
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		playPause = new Pressable
		{
			Width = 32.0,
			Height = 32.0
		};
		playPause.Children.Add(new GelSurface(GelAppearance.PrimaryBlue, 16.0));
		playPauseGlyph = Icons.Glyph("\uf5b0", 13.0, DS.Black(0.82));
		playPauseGlyph.FontWeight = FontWeights.Bold;
		playPause.Children.Add(playPauseGlyph);
		playPause.Click += delegate
		{
			player.TogglePlay();
		};
		grid3.Children.Add(playPause);
		scrubber = new BeatScrubber(player)
		{
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(scrubber, 1);
		grid3.Children.Add(scrubber);
		timeLabel = Ui.Text("", 11.5, FontWeights.Medium, HomeTheme.SecondaryText);
		timeLabel.VerticalAlignment = VerticalAlignment.Center;
		Typography.SetNumeralAlignment(timeLabel, FontNumeralAlignment.Tabular);
		Grid.SetColumn(timeLabel, 2);
		grid3.Children.Add(timeLabel);
		Grid.SetRow(grid3, 1);
		controls.Children.Add(grid3);
		Grid.SetRow(controls, 2);
		grid.Children.Add(controls);
		player.Changed += Refresh;
		player.ProgressChanged += ShowProgress;
		player.FrameReady += OnFrame;
		base.KeyDown += OnKey;
		base.Loaded += delegate
		{
			clickyDrawingWindowView.TakeKeyboardFocus();
		};
		Refresh();
		ShowProgress(player.Position);
		LoadTintAsync();
	}

	public void TakeKeyboardFocus()
	{
		focusTarget.Focus(FocusState.Programmatic);
	}

	private void OnKey(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Escape)
		{
			e.Handled = true;
			close();
		}
		else if (e.Key == VirtualKey.Space)
		{
			e.Handled = true;
			if (!ClickyDrawingWindowViewer.Shared.IsRecording)
			{
				player.TogglePlay();
			}
		}
	}

	public void Appear()
	{
		card.CenterPoint = new Vector3((float)card.Width / 2f, (float)card.Height / 2f, 0f);
		Anim.OpacitySpring(dim, 1f, Showing);
		Anim.OpacitySpring(card, 1f, Showing);
		if (!Anim.ReduceMotion)
		{
			Anim.ScaleUpFrom(card, 0.94f, Showing, null);
		}
	}

	public void Disappear()
	{
		CubicBezierEasingFunction easing = Ui.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(1f, 1f));
		Anim.Opacity(dim, 0f, 0.18, easing);
		Anim.Opacity(card, 0f, 0.18, easing);
		if (!Anim.ReduceMotion)
		{
			Anim.Scale(card, 0.94f, 0.18, easing, card.CenterPoint);
		}
	}

	public void Closing()
	{
		player.Changed -= Refresh;
		player.ProgressChanged -= ShowProgress;
		player.FrameReady -= OnFrame;
		downloadCancellation?.Cancel();
		recorder?.Cancel();
		recorder = null;
	}

	private void Refresh()
	{
		bool isRecording = ClickyDrawingWindowViewer.Shared.IsRecording;
		bool flag = !player.IsPlaying;
		ShowPlayGlyph(stageGlyph, player.HasEnded, pause: false, 4.0);
		if (flag != showsStageButton)
		{
			showsStageButton = flag;
			CubicBezierEasingFunction easing = Easing.EaseOut(Ui.Compositor);
			Anim.Opacity(stageOverlay, flag ? 1 : 0, 0.18, easing);
			Anim.Scale(stageOverlay, (flag || Anim.ReduceMotion) ? 1f : 0.9f, 0.18, easing, new Vector3(36f, 36f, 0f));
		}
		ShowPlayGlyph(playPauseGlyph, !player.IsPlaying && player.HasEnded, player.IsPlaying, 0.0);
		ToolTipService.SetToolTip(playPause, player.IsPlaying ? "Pause (space)" : "Play (space)");
		caption.Show(player.CurrentBeat, player.Position);
		controls.IsHitTestVisible = !isRecording;
		controls.Opacity = (isRecording ? 0.55 : 1.0);
	}

	private static void ShowPlayGlyph(TextBlock glyph, bool turnBack, bool pause, double nudge)
	{
		glyph.Text = (pause ? "\ue769" : (turnBack ? "\ue72c" : "\uf5b0"));
		glyph.Margin = new Thickness((pause || turnBack) ? 0.0 : nudge, 0.0, 0.0, 0.0);
		glyph.RenderTransformOrigin = new Point(0.5, 0.5);
		glyph.RenderTransform = (turnBack ? new ScaleTransform
		{
			ScaleX = -1.0
		} : null);
	}

	private void ShowProgress(double position)
	{
		timeLabel.Text = Clock(position) + " / " + Clock(player.Duration);
		scrubber.Show(position);
		caption.Show(player.CurrentBeat, position);
		download.ShowRecordingProgress(position, player.Duration);
	}

	private static string Clock(double seconds)
	{
		int num = Math.Max(0, (int)Math.Floor(seconds));
		return $"{num / 60}:{num % 60:00}";
	}

	private void OnFrame(LessonPlaybackFrame frame)
	{
		recorder?.Add(frame);
		if ((object)Interlocked.Exchange(ref pendingFrame, frame) != null)
		{
			return;
		}
		dq.TryEnqueue(delegate
		{
			LessonPlaybackFrame lessonPlaybackFrame = Interlocked.Exchange(ref pendingFrame, null);
			if ((object)lessonPlaybackFrame != null)
			{
				if ((object)frameBitmap == null || frameBitmap.PixelWidth != lessonPlaybackFrame.Width || frameBitmap.PixelHeight != lessonPlaybackFrame.Height)
				{
					frameBitmap = new WriteableBitmap(lessonPlaybackFrame.Width, lessonPlaybackFrame.Height);
					picture.Source = frameBitmap;
				}
				using (Stream stream = frameBitmap.PixelBuffer.AsStream())
				{
					stream.Write(lessonPlaybackFrame.Pixels, 0, Math.Min(lessonPlaybackFrame.Pixels.Length, (int)stream.Length));
				}
				frameBitmap.Invalidate();
			}
		});
	}

	private async Task LoadTintAsync()
	{
		Color? color = await ClickyDrawingLaptopScreen.Tint(player.Stored.PosterPath);
		if (color.HasValue)
		{
			Color valueOrDefault = color.GetValueOrDefault();
			wash.Background = Ui.Brush(valueOrDefault.WithAlpha(0.62));
		}
	}

	private void DownloadButtonPressed()
	{
		switch (download.State)
		{
		case DownloadButton.Phase.Idle:
		case DownloadButton.Phase.Failed:
			StartDownloadAsync();
			break;
		case DownloadButton.Phase.Saved:
		{
			string savedPath = download.SavedPath;
			if (savedPath != null)
			{
				ChatBlockViews.RevealPath(savedPath);
			}
			break;
		}
		case DownloadButton.Phase.Recording:
		case DownloadButton.Phase.Saving:
			break;
		}
	}

	private async Task StartDownloadAsync()
	{
		var (num, height) = player.FrameSize;
		if (num <= 0)
		{
			download.Show(DownloadButton.Phase.Failed);
			return;
		}
		CancellationTokenSource cancellation = (downloadCancellation = new CancellationTokenSource());
		download.Show(DownloadButton.Phase.Recording);
		ClickyDrawingWindowViewer.Shared.IsRecording = true;
		Refresh();
		NotchPeekSurface.PeekAction("drawing_download_started");
		try
		{
			LessonVideoRecorder started;
			try
			{
				started = LessonVideoRecorder.Start(num, height);
			}
			catch (Exception ex)
			{
				Log.Warn("lessons", "drawing video: couldn't start recording: " + ex.Message);
				download.Show(DownloadButton.Phase.Failed);
				return;
			}
			recorder = started;
			player.Pause();
			player.Seek(0.0);
			await Task.Delay(250);
			if (cancellation.IsCancellationRequested)
			{
				return;
			}
			player.BeginCapturingSounds();
			started.MarkPlaybackStarted();
			player.Play();
			while (!player.HasEnded)
			{
				await Task.Delay(100);
				if (cancellation.IsCancellationRequested)
				{
					return;
				}
			}
			await Task.Delay(600);
			if (cancellation.IsCancellationRequested)
			{
				return;
			}
			download.Show(DownloadButton.Phase.Saving);
			LessonPlaybackSounds sounds = await player.EndCapturingSoundsAsync();
			double seconds = player.Duration + 0.6;
			recorder = null;
			string videoPath = await started.FinishAsync(seconds);
			if (videoPath == null)
			{
				download.Show(DownloadButton.Phase.Failed);
				return;
			}
			(byte[] Pcm16Mono, int SampleRate)? narration = ((byte[] Pcm16Mono, int SampleRate)?)(player.Lesson.HasNarration ? ((ValueType)player.Narration) : ((ValueType)((byte[], int)?)null));
			double offset = player.Stored.ReplayShift;
			string title = player.Lesson.Title;
			string text = await Task.Run(delegate
			{
				short[] soundtrack = LessonVideoFiles.MixSoundtrack(narration?.Pcm16Mono, narration?.SampleRate ?? 0, offset, (sounds.Count > 0) ? sounds.Mix(seconds) : null, seconds);
				return LessonVideoRecorder.Save(videoPath, soundtrack, title, LessonVideoRecorder.DownloadsFolder());
			});
			if (text != null)
			{
				download.Show(DownloadButton.Phase.Saved, text);
				NotchPeekSurface.PeekAction("drawing_downloaded");
			}
			else
			{
				download.Show(DownloadButton.Phase.Failed);
			}
		}
		finally
		{
			if (downloadCancellation == cancellation)
			{
				ClickyDrawingWindowViewer.Shared.IsRecording = false;
				if (!cancellation.IsCancellationRequested)
				{
					Refresh();
				}
			}
		}
	}
}
