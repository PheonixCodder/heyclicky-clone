using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Portraits;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class ClickyUpdateSurface : NotchSurface
{
	private readonly Grid view = new Grid();

	private readonly Grid content = new Grid();

	private readonly Grid row = new Grid
	{
		Height = 30.0,
		VerticalAlignment = VerticalAlignment.Top
	};

	private NotchPresenter? presenter;

	private NotchUpdate? shown;

	private RingCloseButton? close;

	private string? outputAnalyticsKind;

	private string? builtLook;

	public override FrameworkElement View => view;

	public ClickyUpdateSurface()
	{
		content.Margin = new Thickness(12.0, ClickyUpdateLayout.ContentTopPadding, 12.0, 14.0);
		view.Children.Add(content);
		row.Margin = new Thickness(0.0, -2.0, 12.0, 0.0);
		view.Children.Add(row);
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			return liveEvent.Event is NotchLiveEvent.ClickyUpdate;
		}
		return false;
	}

	public override void Update(NotchPresentation p)
	{
		Build();
	}

	protected override void OnAttached()
	{
		presenter = base.Host.Services.Presenter as NotchPresenter;
		if (presenter != null)
		{
			presenter.Updates.RemainingChanged += OnRemaining;
			presenter.Updates.NoteShowing(showing: true, base.Host.IsHovering);
		}
		base.Host.HoverChanged += OnHover;
		INotchUpdateContentSource updateContent = NotchExtensions.UpdateContent;
		if (updateContent != null)
		{
			updateContent.Changed += OnContentChanged;
		}
		Build();
	}

	public override void Detached()
	{
		presenter?.Updates.NoteShowing(showing: false, hovered: false);
		if (presenter != null)
		{
			presenter.Updates.RemainingChanged -= OnRemaining;
		}
		base.Host.HoverChanged -= OnHover;
		INotchUpdateContentSource updateContent = NotchExtensions.UpdateContent;
		if (updateContent != null)
		{
			updateContent.Changed -= OnContentChanged;
		}
	}

	private void OnHover(bool hovering)
	{
		presenter?.Updates.NoteShowing(showing: true, hovering);
	}

	private void OnContentChanged()
	{
		view.DispatcherQueue.TryEnqueue(delegate
		{
			NotchUpdate notchUpdate = shown;
			if ((object)notchUpdate != null && notchUpdate.Kind != NotchUpdateKind.Done)
			{
				NotchUpdateContent notchUpdateContent = NotchExtensions.UpdateContent?.Content(notchUpdate.RailKey);
				if ((object)notchUpdateContent != null && (object)notchUpdateContent.Request == null)
				{
					presenter?.Updates.Track("request_resolved");
					presenter?.Updates.DismissCurrent();
					return;
				}
			}
			Build();
		});
	}

	private void OnRemaining()
	{
		view.DispatcherQueue.TryEnqueue(delegate
		{
			if ((object)close != null)
			{
				TimeSpan? timeSpan = presenter?.Updates.RemainingTime;
				if (timeSpan.HasValue)
				{
					TimeSpan valueOrDefault = timeSpan.GetValueOrDefault();
					close.GlideTo(Math.Clamp(valueOrDefault.TotalSeconds / NotchUpdates.AutoDismiss.TotalSeconds, 0.0, 1.0), 0.1);
				}
			}
		});
	}

	private static string Look(NotchUpdate update, NotchUpdateContent card)
	{
		_003C_003Ey__InlineArray11<object> buffer = default(_003C_003Ey__InlineArray11<object>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 0) = update.RailKey;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 1) = update.Kind;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 2) = update.ClosesOnItsOwn;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 3) = card.Title;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 4) = card.Summary;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 5) = card.IsStopped;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 6) = card.RoutineBadge;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 7) = card.RoutineTitle;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 8) = card.ReadyOutput?.ToString();
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 9) = string.Join(",", card.Files);
		ref object reference = ref global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray11<object>, object>(ref buffer, 10);
		NotchUpdateRequest request = card.Request;
		reference = (((object)request != null) ? (request.Title + "/" + request.Detail + "/" + string.Join(",", request.Buttons.Select((NotchUpdateButton b) => b.Title + b.IsPrimary + RuntimeHelpers.GetHashCode(b.Action)))) : "");
		return string.Join("|", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray11<object>, object>(in buffer, 11));
	}

	private void Build()
	{
		NotchUpdate notchUpdate = presenter?.Updates.Current;
		NotchUpdateContent notchUpdateContent = (((object)notchUpdate == null) ? null : NotchExtensions.UpdateContent?.Content(notchUpdate.RailKey));
		string text = (((object)notchUpdate == null || (object)notchUpdateContent == null) ? null : Look(notchUpdate, notchUpdateContent));
		if (text != null && text == builtLook && (object)notchUpdate == shown)
		{
			return;
		}
		builtLook = text;
		content.Children.Clear();
		row.Children.Clear();
		shown = notchUpdate;
		if ((object)shown == null || (object)notchUpdateContent == null)
		{
			return;
		}
		NotchUpdateContent notchUpdateContent2 = notchUpdateContent;
		close = new RingCloseButton(shown.ClosesOnItsOwn)
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		close.Click += delegate
		{
			presenter?.Updates.Track("dismissed");
			presenter?.Updates.DismissCurrent();
		};
		close.Tip("Dismiss");
		row.Children.Add(close);
		TimeSpan? timeSpan = presenter?.Updates.RemainingTime;
		if (timeSpan.HasValue)
		{
			TimeSpan valueOrDefault = timeSpan.GetValueOrDefault();
			close.SetProgress(Math.Clamp(valueOrDefault.TotalSeconds / NotchUpdates.AutoDismiss.TotalSeconds, 0.0, 1.0));
		}
		if (shown.Kind != NotchUpdateKind.Done)
		{
			NotchUpdateRequest request = notchUpdateContent2.Request;
			if ((object)request != null)
			{
				content.Children.Add(Ask(notchUpdateContent2, request));
				goto IL_01bc;
			}
		}
		content.Children.Add(Finished(shown, notchUpdateContent2));
		goto IL_01bc;
		IL_01bc:
		TrackCardShownIfNeeded(shown, notchUpdateContent2);
	}

	private void TrackCardShownIfNeeded(NotchUpdate update, NotchUpdateContent card)
	{
		if (presenter == null)
		{
			return;
		}
		(string, JsonObject)? tuple = NotchExtensions.UpdateContent?.ShownAnalytics(update.RailKey, update.Kind);
		if (tuple.HasValue)
		{
			(string, JsonObject) valueOrDefault = tuple.GetValueOrDefault();
			JsonObject item = valueOrDefault.Item2;
			outputAnalyticsKind = item["output_kind"]?.GetValue<string>();
			if (update.Kind == NotchUpdateKind.Done)
			{
				item["has_preview"] = PreviewEntry(update, card) != null;
				item["has_summary"] = ClickyUpdateLayout.SummaryText(card) != null;
			}
			presenter.Updates.TrackShownIfNeeded(valueOrDefault.Item1, item);
		}
	}

	private static string? PreviewEntry(NotchUpdate update, NotchUpdateContent card)
	{
		if (update.Kind != NotchUpdateKind.Done)
		{
			return null;
		}
		return card.Files.FirstOrDefault((string f) => !ArtifactPaths.IsWebLink(f) && File.Exists(f));
	}

	private FrameworkElement Ask(NotchUpdateContent card, NotchUpdateRequest request)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0
		};
		stackPanel.Children.Add(Ui.Text(request.Title, 14.0, FontWeights.SemiBold, DS.White(0.95), TextAlignment.Left, 1));
		TextBlock textBlock = Ui.Text(request.Detail, 11.0, FontWeights.Normal, DS.White(0.62), TextAlignment.Left, 0, wrap: true);
		textBlock.MaxLines = 4;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		stackPanel.Children.Add(textBlock);
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		UIElementCollection children = grid.Children;
		FrameworkElement item;
		if (request.IsChromeAsk)
		{
			FrameworkElement frameworkElement = ChromeIcon.Make(32.0);
			if ((object)frameworkElement != null)
			{
				item = frameworkElement;
				goto IL_010f;
			}
		}
		item = Chip(card);
		goto IL_010f;
		IL_010f:
		children.Add(item);
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			Margin = new Thickness(42.0, 0.0, 0.0, 0.0)
		};
		foreach (NotchUpdateButton b in request.Buttons)
		{
			GelButton gelButton = new GelButton(b.Title, (!b.IsPrimary) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, null, null)
			{
				Height = 34.0
			};
			gelButton.Click += delegate
			{
				b.Action();
				presenter?.Updates.Track("request_resolved");
				presenter?.Updates.DismissCurrent();
			};
			stackPanel2.Children.Add(gelButton);
		}
		return new StackPanel
		{
			Spacing = 9.0,
			Children = 
			{
				(UIElement)grid,
				(UIElement)stackPanel2
			}
		};
	}

	private FrameworkElement Finished(NotchUpdate update, NotchUpdateContent card)
	{
		string text = ClickyUpdateLayout.SummaryText(card);
		string clickySlug = card.ClickySlug;
		string clickyName = ((clickySlug == null) ? null : ClickyRoster.Shared.Clicky(clickySlug)?.Name);
		string text2 = ((update.Kind == NotchUpdateKind.Done) ? ClickyUpdateLayout.FinishedTitle(card, clickyName) : card.Title);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		TextBlock textBlock = Ui.Text(text2, 14.0, FontWeights.SemiBold, DS.White(0.95), TextAlignment.Left, 1);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		stackPanel.Children.Add(textBlock);
		int seed = ChatStamp.Seed(update.RailKey);
		UIElementCollection children = stackPanel.Children;
		children.Add(update.Kind switch
		{
			NotchUpdateKind.Done => ChatStamp.Create(ChatStamp.Status.Ready, "done", seed), 
			NotchUpdateKind.NeedsYou => ChatStamp.Create(ChatStamp.Status.NeedsYou, "you", seed), 
			_ => card.IsStopped ? ChatStamp.Create(ChatStamp.Status.Paused, "stopped", seed) : ChatStamp.Create(ChatStamp.Status.NeedsYou, "oops", seed), 
		});
		string routineBadge = card.RoutineBadge;
		if (routineBadge != null)
		{
			TextBlock textBlock2 = Icons.Glyph("\ue72c", 9.0, DS.White(0.7));
			textBlock2.FontWeight = FontWeights.Bold;
			stackPanel.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(100.0),
				Background = Ui.Brush(DS.White(0.1)),
				Padding = new Thickness(7.0, 3.0, 7.0, 3.0),
				VerticalAlignment = VerticalAlignment.Center,
				Child = Ui.H(4.0, textBlock2, Ui.Text(routineBadge, 10.5, FontWeights.SemiBold, DS.White(0.7), TextAlignment.Left, 1))
			}.Capsule().Tip("Routine: " + card.RoutineTitle));
		}
		string text3 = text;
		if (text3 == null)
		{
			text3 = update.Kind switch
			{
				NotchUpdateKind.Done => "Done", 
				NotchUpdateKind.Failed => card.IsStopped ? "Interrupted" : "Didn't finish", 
				_ => "Needs you", 
			};
		}
		TextBlock textBlock3 = Ui.Text(text3, 11.0, FontWeights.Normal, DS.White(0.62), TextAlignment.Left, 0, wrap: true);
		textBlock3.MaxLines = ((text == null) ? 1 : 3);
		textBlock3.TextTrimming = TextTrimming.CharacterEllipsis;
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = ((text == null) ? 1 : 3),
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)textBlock3
			}
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0,
			MinHeight = 32.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		FrameworkElement frameworkElement = Chip(card);
		frameworkElement.VerticalAlignment = ((text == null) ? VerticalAlignment.Center : VerticalAlignment.Top);
		grid.Children.Add(frameworkElement);
		stackPanel2.VerticalAlignment = ((text == null) ? VerticalAlignment.Center : VerticalAlignment.Top);
		Grid.SetColumn(stackPanel2, 1);
		grid.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = ((text == null) ? 12 : 9),
			Children = 
			{
				(UIElement)grid,
				(UIElement)FinishedButtons(card)
			}
		};
		string preview = PreviewEntry(update, card);
		if (preview == null)
		{
			return stackPanel3;
		}
		Grid obj = new Grid
		{
			ColumnSpacing = 14.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			},
			Children = { (UIElement)stackPanel3 }
		};
		(double, double) size = ClickyUpdateLayout.PreviewSize(text);
		int moreCount = card.Files.Count - 1;
		FrameworkElement frameworkElement2 = Preview(preview, moreCount, size, delegate
		{
			presenter?.Updates.Track("button_pressed", new JsonObject((JsonNodeOptions?)null)
			{
				["button"] = "file_preview",
				["artifact_kind"] = ClickyAnalytics.ProductArtifactKind(preview),
				["more_count"] = moreCount
			});
			card.ReadyOutput?.Open();
			presenter?.Updates.DismissCurrent();
			NotchExtensions.AnnounceFinishedWorkOnRequest?.Invoke(card);
		});
		Grid.SetColumn(frameworkElement2, 1);
		obj.Children.Add(frameworkElement2);
		return obj;
	}

	private FrameworkElement FinishedButtons(NotchUpdateContent card)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			Margin = new Thickness(42.0, 0.0, 0.0, 0.0)
		};
		string text = NotchChat.HandOffs?.LatestReplyEntryId(card.JobId, card.ClickySlug);
		bool flag = text != null && (card.ReadyOutput?.OpensFullReplyOnly ?? true);
		NotchUpdateReadyOutput output = card.ReadyOutput;
		if ((object)output != null && !flag)
		{
			GelButton gelButton = new GelButton(output.ButtonTitle, GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, output.Glyph, null, null)
			{
				Height = 34.0
			};
			gelButton.Click += delegate
			{
				NotchPresenter? notchPresenter = presenter;
				if (notchPresenter != null)
				{
					NotchUpdates updates = notchPresenter.Updates;
					JsonObject obj = new JsonObject((JsonNodeOptions?)null) { ["button"] = "open_output" };
					string text2 = outputAnalyticsKind;
					obj["output_kind"] = ((text2 != null && text2 != "none") ? text2 : (output.IsLink ? "link" : (output.OpensFullReplyOnly ? "several" : "file")));
					updates.Track("button_pressed", obj);
				}
				output.Open();
				presenter?.Updates.DismissCurrent();
				NotchExtensions.AnnounceFinishedWorkOnRequest?.Invoke(card);
			};
			stackPanel.Children.Add(gelButton);
		}
		if (text != null)
		{
			string entryId = text;
			GelButton gelButton2 = new GelButton("Full report", (!flag) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, "\ue8a5", null, null)
			{
				Height = 34.0
			};
			gelButton2.Click += delegate
			{
				presenter?.Updates.Track("button_pressed", new JsonObject((JsonNodeOptions?)null) { ["button"] = "full_report" });
				presenter?.Updates.DismissCurrent();
				NotchExtensions.MarkFinishedWorkSeen?.Invoke(card);
				presenter?.Tabs.ShowFullReply(entryId, null);
				NotchExtensions.AnnounceFinishedWorkOnRequest?.Invoke(card);
			};
			stackPanel.Children.Add(gelButton2);
		}
		return stackPanel;
	}

	private static FrameworkElement Chip(NotchUpdateContent card)
	{
		ChatHandOff entry = new ChatHandOff
		{
			Id = card.RailKey,
			Kind = ChatHandOffKind.CameBack,
			JobId = card.JobId,
			ClickySlug = card.ClickySlug,
			Title = card.Title,
			CreatedAt = DateTimeOffset.UtcNow
		};
		FrameworkElement frameworkElement = NotchExtensions.LiveWork?.Chip(entry, 32.0);
		if ((object)frameworkElement != null)
		{
			return frameworkElement;
		}
		TextBlock textBlock = Icons.Glyph("\ue73e", 14.0, DS.Black(0.85));
		textBlock.FontWeight = FontWeights.Bold;
		return new Grid
		{
			Width = 32.0,
			Height = 32.0,
			VerticalAlignment = VerticalAlignment.Top,
			Children = 
			{
				(UIElement)new Ellipse
				{
					Fill = Ui.Brush(Colors.White)
				},
				(UIElement)textBlock
			}
		};
	}

	private static FrameworkElement Preview(string path, int moreCount, (double W, double H) size, Action open)
	{
		NotchDocumentStyle notchDocumentStyle = NotchDocumentStyle.For(path);
		Pressable p = new Pressable
		{
			Width = size.W,
			Height = size.H,
			CornerRadius = new CornerRadius(9.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		p.Children.Add(new ShadowLayer(9.0, DS.Black(0.35), 6.0, 2.0));
		p.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(9.0),
			Background = Ui.Brush(Colors.White)
		});
		Border picture = new Border
		{
			Margin = new Thickness(3.0),
			CornerRadius = new CornerRadius(6.0),
			Background = Ui.Brush(notchDocumentStyle.Tint.WithAlpha(0.14))
		};
		if (ChatBlockViews.IsImage(path))
		{
			picture.Background = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(path))
				{
					DecodePixelWidth = 400
				},
				Stretch = Stretch.UniformToFill,
				AlignmentY = AlignmentY.Top
			};
		}
		else
		{
			TextBlock textBlock = Icons.Glyph(notchDocumentStyle.Glyph, 22.0, notchDocumentStyle.Tint.WithAlpha(0.9));
			textBlock.FontWeight = FontWeights.SemiBold;
			picture.Child = textBlock;
			ArtifactThumbnails.GetAsync(path, 400).ContinueWith(delegate(Task<ImageSource?> t)
			{
				ImageSource src = t.Result;
				if ((object)src != null)
				{
					p.DispatcherQueue.TryEnqueue(delegate
					{
						picture.Background = new ImageBrush
						{
							ImageSource = src,
							Stretch = Stretch.UniformToFill,
							AlignmentY = AlignmentY.Top
						};
					});
				}
			}, TaskScheduler.Default);
		}
		p.Children.Add(picture);
		TextBlock textBlock2 = Icons.Glyph(notchDocumentStyle.Glyph, 9.0, Colors.Black);
		textBlock2.FontWeight = FontWeights.SemiBold;
		Grid grid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(6.0)
		};
		grid.Children.Add(new GelSurface(GelAppearance.SecondaryWhite, 100.0));
		TextBlock ink = Ui.Text(notchDocumentStyle.KindLabel, 10.0, FontWeights.SemiBold, Colors.Black, TextAlignment.Left, 1);
		grid.Children.Add(new Border
		{
			Padding = new Thickness(8.0, 3.5, 8.0, 3.5),
			Child = Ui.H(4.0, Ui.Embossed(textBlock2), Ui.Embossed(ink))
		});
		p.Children.Add(grid);
		if (moreCount > 0)
		{
			p.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(100.0),
				Background = Ui.Brush(DS.Black(0.62)),
				Padding = new Thickness(6.0, 2.5, 6.0, 2.5),
				Margin = new Thickness(6.0),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Child = Ui.Text($"+{moreCount}", 9.5, FontWeights.Bold, Colors.White)
			}.Capsule());
		}
		p.Rotation = ((!Anim.ReduceMotion) ? 2 : 0);
		p.Loaded += delegate
		{
			p.CenterPoint = new Vector3((float)size.W / 2f, (float)size.H / 2f, 0f);
		};
		SwiftSpring spring = new SwiftSpring(0.28, 0.75);
		p.HoverChanged += delegate(bool h2)
		{
			if (!Anim.ReduceMotion)
			{
				p.Rotation = ((!h2) ? 2 : 0);
				Anim.ScaleSpring(p, h2 ? 1.03f : 1f, spring, null);
			}
		};
		p.Click += open;
		p.Tip("Open " + System.IO.Path.GetFileName(path));
		return p;
	}
}
