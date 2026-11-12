using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class TextResponseSurface : NotchSurface
{
	private readonly Grid view = new Grid();

	private readonly Grid thinkingRow;

	private readonly Grid answerArea = new Grid();

	private readonly ScrollViewer scroll;

	private readonly StackPanel prose = new StackPanel
	{
		Spacing = 12.0
	};

	private readonly ContentControl markdownSlot = new ContentControl
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		IsTabStop = false
	};

	private readonly ScrollViewer gallery;

	private readonly StackPanel galleryRow = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 8.0
	};

	private readonly Pressable copy;

	private readonly TextBlock copyGlyph;

	private readonly Pressable close;

	private ComposerBox? followUp;

	private string question = "";

	private string answer = "";

	private bool thinking = true;

	private bool finished;

	private DispatcherQueueTimer? renderThrottle;

	private bool renderPending;

	private readonly Grid countdownChrome = new Grid
	{
		HorizontalAlignment = HorizontalAlignment.Right,
		VerticalAlignment = VerticalAlignment.Top,
		Margin = new Thickness(0.0, -2.0, 8.0, 0.0),
		Height = 24.0
	};

	private Pressable clock;

	private Ellipse clockRing;

	private StackPanel hoverPills;

	private GelActionButton pausedDismiss;

	private DispatcherQueueTimer? countdownTimer;

	private string chromeMode = "";

	private const double ClockDiameter = 24.0;

	private const double RingThickness = 2.0;

	private static readonly double RingCircumference = Math.PI * 24.0;

	private readonly List<string> pendingDrop = new List<string>();

	public override FrameworkElement View => view;

	public TextResponseSurface(NotchPresentation.TextResponse r)
	{
		question = r.Question;
		thinkingRow = Ui.Row(8.0, new UIElement[1] { Ui.Semibold("Thinking", 11.0, DS.Colors.TextPrimary) }, new UIElement[1] { ActivityIndicators.ThinkingDots(DS.Colors.ThinkingAccent) }, 6.0);
		thinkingRow.Margin = new Thickness(14.0, 0.0, 14.0, 0.0);
		thinkingRow.Height = 18.0;
		thinkingRow.VerticalAlignment = VerticalAlignment.Top;
		copyGlyph = Icons.Glyph("\ue8c8", 10.0, DS.White(0.5));
		copyGlyph.FontWeight = FontWeights.SemiBold;
		copyGlyph.HorizontalAlignment = HorizontalAlignment.Left;
		copyGlyph.VerticalAlignment = VerticalAlignment.Top;
		copy = new Pressable
		{
			Width = 16.0,
			Height = 16.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		copy.Children.Add(copyGlyph);
		copy.HoverChanged += delegate(bool h)
		{
			if (copyGlyph.Text == "\ue8c8")
			{
				copyGlyph.Foreground = Ui.Brush(DS.White(h ? 0.9 : 0.5));
			}
		};
		copy.Click += CopyAnswer;
		copy.Tip("Copy response");
		gallery = new ScrollViewer
		{
			Content = galleryRow,
			Height = 88.0,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled,
			Visibility = Visibility.Collapsed
		};
		StackPanel item = new StackPanel
		{
			Spacing = 8.0,
			Children = 
			{
				(UIElement)markdownSlot,
				(UIElement)copy
			}
		};
		prose.Children.Add(gallery);
		prose.Children.Add(item);
		scroll = new ScrollViewer
		{
			Content = new Border
			{
				Child = prose,
				Padding = new Thickness(14.0, 34.0, 14.0, 12.0)
			},
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		Grid item2 = new Grid
		{
			RowDefinitions = 
			{
				new RowDefinition
				{
					Height = new GridLength(1.0, GridUnitType.Star)
				},
				new RowDefinition
				{
					Height = GridLength.Auto
				}
			},
			Children = { (UIElement)scroll }
		};
		answerArea.Children.Add(item2);
		close = new Pressable
		{
			Width = 24.0,
			Height = 24.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, -2.0, 8.0, 0.0)
		};
		close.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.White(0.1)),
			Stroke = Ui.Brush(DS.White(0.08)),
			StrokeThickness = 0.5
		});
		TextBlock textBlock = Icons.Glyph("\ue711", 9.0, DS.White(0.75));
		textBlock.FontWeight = FontWeights.Bold;
		close.Children.Add(textBlock);
		close.Click += delegate
		{
			base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
		};
		close.Tip("Close");
		BuildCountdownChrome();
		view.Children.Add(thinkingRow);
		view.Children.Add(answerArea);
		view.Children.Add(close);
		view.Children.Add(countdownChrome);
		Apply(r);
	}

	private void BuildCountdownChrome()
	{
		clock = new Pressable
		{
			Width = 24.0,
			Height = 24.0,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		clock.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.White(0.1))
		});
		clock.Children.Add(new Ellipse
		{
			Width = 26.0,
			Height = 26.0,
			Margin = new Thickness(-1.0),
			Stroke = Ui.Brush(DS.White(0.08)),
			StrokeThickness = 2.0
		});
		clockRing = new Ellipse
		{
			Width = 26.0,
			Height = 26.0,
			Margin = new Thickness(-1.0),
			Stroke = Ui.Brush(DS.Colors.Blue300.WithAlpha(0.95)),
			StrokeThickness = 2.0,
			StrokeDashCap = PenLineCap.Round,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = new RotateTransform
			{
				Angle = -90.0
			}
		};
		clock.Children.Add(clockRing);
		TextBlock textBlock = Icons.Glyph("\ue823", 9.0, DS.White(0.78));
		textBlock.FontWeight = FontWeights.Bold;
		clock.Children.Add(textBlock);
		clock.Click += Dismiss;
		clock.Tip("Dismiss now");
		GelActionButton gelActionButton = Pill("\ue769", "Pause", "Pause auto-dismiss");
		gelActionButton.Click += delegate
		{
			(base.Host.Services.Presenter as NotchPresenter)?.PauseTextResponseAutoDismiss();
		};
		GelActionButton gelActionButton2 = Pill("\ue70e", "Dismiss", "Dismiss");
		gelActionButton2.Click += Dismiss;
		hoverPills = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Children = 
			{
				(UIElement)gelActionButton,
				(UIElement)gelActionButton2
			}
		};
		pausedDismiss = Pill("\ue70e", "Dismiss", "Dismiss");
		pausedDismiss.HorizontalAlignment = HorizontalAlignment.Right;
		pausedDismiss.Click += Dismiss;
		countdownChrome.Children.Add(clock);
		countdownChrome.Children.Add(hoverPills);
		countdownChrome.Children.Add(pausedDismiss);
		countdownChrome.Visibility = Visibility.Collapsed;
	}

	private static GelActionButton Pill(string glyph, string title, string tip)
	{
		GelActionButton obj = new GelActionButton(title, glyph, GelAppearance.SecondaryWhite, 24.0, 8.0, 0.0, 11.0, 9.0, 4.0)
		{
			VerticalAlignment = VerticalAlignment.Top
		};
		obj.Tip(tip);
		return obj;
	}

	private void Dismiss()
	{
		base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
	}

	public override bool AcceptFiles(IReadOnlyList<string> paths)
	{
		if ((object)followUp != null)
		{
			AttachDropped(followUp, paths);
		}
		else
		{
			pendingDrop.AddRange(paths);
		}
		return true;
	}

	private static void AttachDropped(ComposerBox box, IReadOnlyList<string> paths)
	{
		if (paths.Count != 0)
		{
			int num = box.AddAttachments(paths);
			if (num > 0)
			{
				ClickyAnalytics.TrackNotchDroppedFilesAttached(num);
			}
		}
	}

	public override void SetDropTargeting(bool on)
	{
		followUp?.SetDropTargeting(on, DS.CursorColor(base.Host.Services.Settings.CursorColorHex));
	}

	private void UpdateChrome()
	{
		if (base.Host == null)
		{
			return;
		}
		(TimeSpan, TimeSpan, bool)? tuple = (base.Host.Services.Presenter as NotchPresenter)?.TextResponseCountdown;
		bool flag = finished && tuple.HasValue;
		string text = (thinking ? "none" : ((!flag) ? "close" : (tuple.Value.Item3 ? "paused" : (base.Host.IsHovering ? "hover" : "clock"))));
		if (text != chromeMode)
		{
			chromeMode = text;
			close.Visibility = ((!(text == "close")) ? Visibility.Collapsed : Visibility.Visible);
			Grid grid = countdownChrome;
			bool flag2;
			switch (text)
			{
			case "clock":
			case "hover":
			case "paused":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			grid.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
			clock.Visibility = ((!(text == "clock")) ? Visibility.Collapsed : Visibility.Visible);
			hoverPills.Visibility = ((!(text == "hover")) ? Visibility.Collapsed : Visibility.Visible);
			pausedDismiss.Visibility = ((!(text == "paused")) ? Visibility.Collapsed : Visibility.Visible);
			switch (text)
			{
			case "clock":
			case "hover":
			case "paused":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (flag2 && !Anim.Instant)
			{
				countdownChrome.Opacity = 0.0;
				Anim.Opacity(countdownChrome, 1f, 0.18);
			}
		}
		if (flag && tuple.HasValue)
		{
			(TimeSpan, TimeSpan, bool) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item2 > TimeSpan.Zero)
			{
				double num = Math.Clamp(valueOrDefault.Item1.TotalSeconds / valueOrDefault.Item2.TotalSeconds, 0.0, 1.0);
				double val = num * RingCircumference / 2.0;
				clockRing.StrokeDashArray = new DoubleCollection
				{
					Math.Max(0.001, val),
					1000.0
				};
				clockRing.Visibility = ((num <= 0.001) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		if (p is NotchPresentation.TextResponse r)
		{
			return !NotchGeometry.IsAwaitingProse(r);
		}
		return false;
	}

	public override void Update(NotchPresentation p)
	{
		if (p is NotchPresentation.TextResponse r)
		{
			Apply(r);
		}
	}

	protected override void OnAttached()
	{
		view.Height = double.NaN;
		view.VerticalAlignment = VerticalAlignment.Stretch;
		base.Host.Services.Companion.StateChanged += OnCompanionState;
		renderThrottle = DispatcherQueue.GetForCurrentThread().CreateTimer();
		renderThrottle.Interval = TimeSpan.FromMilliseconds(33L, 0L);
		renderThrottle.IsRepeating = false;
		renderThrottle.Tick += delegate
		{
			renderPending = false;
			RenderNow();
		};
		UpdateFinished();
		countdownTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		countdownTimer.Interval = TimeSpan.FromMilliseconds(100L, 0L);
		countdownTimer.Tick += delegate
		{
			UpdateChrome();
		};
		countdownTimer.Start();
		UpdateChrome();
		RenderNow();
	}

	public override void Detached()
	{
		if (pendingDrop.Count > 0)
		{
			ClickyAnalytics.TrackNotchDroppedAttachmentsDiscarded(pendingDrop.Count);
			pendingDrop.Clear();
		}
		base.Host.Services.Companion.StateChanged -= OnCompanionState;
		base.Host.SizeContext.ResponseFinishedStreaming = false;
		base.Host.SizeContext.ResponseHasGallery = false;
		base.Host.SizeContext.FollowUpExtraLines = 0;
		countdownTimer?.Stop();
		(base.Host.Services.Presenter as NotchPresenter)?.SetTextResponseFollowUpActive(active: false);
		if ((object)followUp != null)
		{
			base.Host.Services.Presenter.PresentationChanged -= ReleaseIfLeaving;
		}
	}

	private void ReleaseIfLeaving(object? s, EventArgs e)
	{
	}

	private void OnCompanionState(object? s, EventArgs e)
	{
		view.DispatcherQueue.TryEnqueue(delegate
		{
			UpdateFinished();
			base.Host.Relayout();
		});
	}

	private void Apply(NotchPresentation.TextResponse r)
	{
		question = r.Question;
		answer = r.Answer ?? "";
		thinking = r.IsThinking || answer.Trim().Length == 0;
		thinkingRow.Visibility = ((!thinking) ? Visibility.Collapsed : Visibility.Visible);
		answerArea.Visibility = (thinking ? Visibility.Collapsed : Visibility.Visible);
		close.Visibility = (thinking ? Visibility.Collapsed : Visibility.Visible);
		chromeMode = "";
		if (base.Host != null)
		{
			UpdateChrome();
		}
		if (base.Host != null)
		{
			if (!renderPending)
			{
				renderPending = true;
				renderThrottle?.Start();
			}
			UpdateFinished();
		}
	}

	private void RenderNow()
	{
		if (thinking)
		{
			return;
		}
		List<(string, string)> images;
		StackPanel content = Markdown.Render(answer, Markdown.NotchResponse, out images);
		markdownSlot.Content = content;
		copy.Visibility = ((answer.Trim().Length <= 0) ? Visibility.Collapsed : Visibility.Visible);
		bool flag = images.Count > 0;
		if (flag && galleryRow.Children.Count != images.Count)
		{
			galleryRow.Children.Clear();
			foreach (var item2 in images)
			{
				string item = item2.Item1;
				string link = item2.Item2;
				Pressable pressable = new Pressable
				{
					Width = 116.0,
					Height = 88.0,
					CornerRadius = new CornerRadius(8.0),
					Background = Ui.Brush(DS.White(0.06)),
					BorderBrush = Ui.Brush(DS.White(0.08)),
					BorderThickness = new Thickness(0.5)
				};
				if (Uri.TryCreate(item, UriKind.Absolute, out Uri result))
				{
					pressable.Children.Add(new Border
					{
						CornerRadius = new CornerRadius(8.0),
						Background = new ImageBrush
						{
							ImageSource = new BitmapImage(result)
							{
								DecodePixelWidth = 232
							},
							Stretch = Stretch.UniformToFill
						}
					});
				}
				pressable.Click += delegate
				{
					base.Host.Actions.OpenUrl(link);
				};
				galleryRow.Children.Add(pressable);
			}
		}
		gallery.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (base.Host.SizeContext.ResponseHasGallery != flag)
		{
			base.Host.SizeContext.ResponseHasGallery = flag;
			base.Host.Relayout();
		}
	}

	private void UpdateFinished()
	{
		if (base.Host == null)
		{
			return;
		}
		bool flag = !thinking && base.Host.Services.Companion.State == VoiceState.Idle;
		if (flag == finished)
		{
			return;
		}
		finished = flag;
		base.Host.SizeContext.ResponseFinishedStreaming = finished;
		if (!finished || (object)followUp != null)
		{
			return;
		}
		followUp = new ComposerBox("ask a follow-up…", base.Host.Services.Auth.IsSignedIn)
		{
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		Grid.SetRow(followUp, 1);
		((Grid)answerArea.Children[0]).Children.Add(followUp);
		followUp.Opacity = 0.0;
		ComposerBox box = followUp;
		followUp.SizeChanged += Grow;
		followUp.MetricsChanged += delegate(int extra, int files)
		{
			base.Host.SizeContext.FollowUpExtraLines = extra;
			base.Host.SizeContext.FollowUpAttachmentCount = files;
			base.Host.Relayout();
		};
		followUp.Input.GotFocus += delegate
		{
			base.Host.RequestKeyboard();
		};
		followUp.Input.PointerPressed += delegate
		{
			base.Host.RequestKeyboard();
		};
		followUp.Input.TextChanged += delegate
		{
			SyncSuspension();
		};
		followUp.AttachmentsChanged += SyncSuspension;
		AttachDropped(followUp, pendingDrop.ToList());
		pendingDrop.Clear();
		followUp.Submitted += delegate(string text, IReadOnlyList<string> files)
		{
			base.Host.ReleaseKeyboard();
			if (base.Host.Services.Settings.LibraryModeEnabled)
			{
				base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
			}
			base.Host.Services.Companion.SubmitTextAsync(text, files);
		};
		followUp.Cancelled += delegate
		{
			base.Host.ReleaseKeyboard();
			base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
		};
		void Grow(object sender, SizeChangedEventArgs e)
		{
			box.SizeChanged -= Grow;
			Anim.ScaleUpFrom(box, 0.94f, NotchSprings.Control, new Vector3((float)e.NewSize.Width, 0f, 0f));
			Anim.OpacitySpring(box, 1f, NotchSprings.Control);
		}
		void SyncSuspension()
		{
			(base.Host.Services.Presenter as NotchPresenter)?.SetTextResponseFollowUpActive(followUp.HasDraft);
		}
	}

	private void CopyAnswer()
	{
		DataPackage dataPackage = new DataPackage();
		dataPackage.SetText(answer);
		Clipboard.SetContent(dataPackage);
		copyGlyph.Text = "\ue73e";
		copyGlyph.Foreground = Ui.Brush(DS.Colors.Blue300.WithAlpha(0.95));
		ToolTipService.SetToolTip(copy, "Copied");
		DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1.5);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			copyGlyph.Text = "\ue8c8";
			copyGlyph.Foreground = Ui.Brush(DS.White(copy.IsHovered ? 0.9 : 0.5));
			ToolTipService.SetToolTip(copy, "Copy response");
		};
		dispatcherQueueTimer.Start();
	}
}
