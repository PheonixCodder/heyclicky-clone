using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using WinRT.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchChatComposer : Grid
{
	private sealed record DispatcherQueueTimerHandle(Microsoft.UI.Dispatching.DispatcherQueueTimer Timer);

	public const double WindowColumnWidth = 680.0;

	public const double WindowColumnSidePadding = 24.0;

	private readonly double previewSize;

	private readonly MessagesPalette look = MessagesPalette.Current;

	private readonly INotchHost host;

	private readonly NotchPresenter? presenter;

	private readonly Pressable voicePlate;

	private readonly TextBlock voiceLabel;

	private readonly TextBlock micGlyph;

	private readonly Grid micAtRest;

	private readonly Grid textBox = new Grid();

	private readonly Border textBoxPlate = new Border();

	private readonly Border textBoxStroke = new Border
	{
		IsHitTestVisible = false
	};

	private readonly Grid selectionRing;

	private readonly TextBox input;

	private readonly TextBlock placeholder;

	private readonly TextBlock dropLabel;

	private readonly Pressable attach;

	private readonly Pressable send;

	private Pressable? stopButton;

	private Action? stopAction;

	private readonly GelSurface sendGel;

	private readonly TextBlock sendGlyph;

	private readonly TextBlock attachGlyph;

	private readonly ReadAloudSwitch readAloud;

	private readonly StackPanel previews = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 6.0,
		Padding = new Thickness(4.0, 4.0, 4.0, 0.0)
	};

	private readonly ScrollViewer previewRow;

	private readonly Grid controls = new Grid
	{
		ColumnSpacing = 8.0
	};

	private readonly List<string> attachments = new List<string>();

	private readonly Border capNotice;

	private bool isFocused;

	private bool isDropTargeting;

	private bool isTyping;

	private int focusGeneration;

	private string? followUpEntryId;

	private readonly ChatHandOff? agentWithoutEvents;

	private readonly bool isInWindow;

	private readonly Grid voiceIndicator = new Grid();

	private VoiceState shownVoiceState;

	private DispatcherQueueTimerHandle? capTimer;

	public double ExtraHeight { get; private set; }

	private bool IsFollowUp
	{
		get
		{
			if (followUpEntryId == null)
			{
				return (object)agentWithoutEvents != null;
			}
			return true;
		}
	}

	public bool HoldsKeyboard => isFocused;

	private bool CanSend
	{
		get
		{
			if (input.Text.Trim().Length <= 0)
			{
				return attachments.Count > 0;
			}
			return true;
		}
	}

	private bool IsFieldFocused
	{
		get
		{
			XamlRoot xamlRoot = base.XamlRoot;
			if ((object)xamlRoot != null)
			{
				return FocusManager.GetFocusedElement(xamlRoot) == input;
			}
			return false;
		}
	}

	private bool HasKeyboardNow
	{
		get
		{
			if (host.HasKeyboard)
			{
				return IsFieldFocused;
			}
			return false;
		}
	}

	public event Action<double>? ExtraHeightChanged;

	private static event Action? EveryDraftCleared;

	public static Grid GelSelection(double radius)
	{
		Grid grid = new Grid();
		grid.IsHitTestVisible = false;
		grid.Visibility = Visibility.Collapsed;
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(radius),
			BorderThickness = new Thickness(3.0),
			BorderBrush = Ui.Vertical((0.0, DS.Hex(15594751u)), (0.18, DS.Hex(14084095u)), (0.5, DS.Hex(10668031u)), (0.8, DS.Hex(10079224u)), (1.0, DS.Hex(10670839u)))
		});
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(radius),
			BorderThickness = new Thickness(0.6),
			BorderBrush = Ui.Brush(DS.Hex(2234520u))
		});
		grid.Children.Add(new Border
		{
			Margin = new Thickness(2.4),
			CornerRadius = new CornerRadius(Math.Max(0.0, radius - 2.4)),
			BorderThickness = new Thickness(0.6),
			BorderBrush = Ui.Brush(DS.White(0.6))
		});
		return grid;
	}

	public void RetargetFollowUp(string entryId)
	{
		if (IsFollowUp)
		{
			followUpEntryId = entryId;
		}
	}

	public NotchChatComposer(INotchHost host, string? followUpEntryId = null, bool isInWindow = false, string? placeholderText = null, ChatHandOff? agentWithoutEvents = null)
	{
		NotchChatComposer notchChatComposer = this;
		this.host = host;
		this.followUpEntryId = followUpEntryId;
		this.isInWindow = isInWindow;
		this.agentWithoutEvents = agentWithoutEvents;
		presenter = host.Services.Presenter as NotchPresenter;
		previewSize = (isInWindow ? 52 : 40);
		if (isInWindow)
		{
			base.Padding = new Thickness(24.0, 10.0, 24.0, 12.0);
			base.MaxWidth = 728.0;
		}
		else
		{
			base.Padding = new Thickness(12.0, 10.0, 12.0, 12.0);
		}
		base.VerticalAlignment = VerticalAlignment.Bottom;
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0,
			MinHeight = 42.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		voicePlate = new Pressable
		{
			Height = 42.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		voicePlate.Children.Add(new GelSurface(GelAppearance.PrimaryBlue, 21.0));
		micGlyph = Icons.Glyph("\uec71", 13.0, DS.Black(0.85));
		micGlyph.FontWeight = FontWeights.SemiBold;
		micGlyph.HorizontalAlignment = HorizontalAlignment.Center;
		micGlyph.TextAlignment = TextAlignment.Center;
		voiceLabel = Ui.Text("", 13.0, FontWeights.SemiBold, DS.Black(0.85), TextAlignment.Left, 1);
		voiceLabel.VerticalAlignment = VerticalAlignment.Center;
		voiceLabel.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		voiceIndicator.MinWidth = 14.0;
		voiceIndicator.HorizontalAlignment = HorizontalAlignment.Center;
		voiceIndicator.VerticalAlignment = VerticalAlignment.Center;
		micAtRest = Ui.Embossed(micGlyph);
		voiceIndicator.Children.Add(micAtRest);
		stackPanel.Children.Add(voiceIndicator);
		stackPanel.Children.Add(Ui.Embossed(voiceLabel));
		voicePlate.Children.Add(stackPanel);
		voicePlate.WithPressScale();
		voicePlate.Click += delegate
		{
			notchChatComposer.SetFocused(focused: false);
		};
		AutomationProperties.SetName(voicePlate, "Talk to HeyClicky");
		grid.Children.Add(voicePlate);
		double num = 21.0;
		textBoxPlate.CornerRadius = new CornerRadius(num);
		textBoxPlate.Background = Ui.Brush(PlateFill(typing: false));
		textBoxStroke.CornerRadius = new CornerRadius(num);
		textBoxStroke.BorderThickness = new Thickness(1.0);
		textBoxStroke.BorderBrush = Ui.Brush(look.InkAt(0.22));
		selectionRing = GelSelection(num);
		textBox.Children.Add(textBoxPlate);
		textBox.Children.Add(textBoxStroke);
		textBox.Children.Add(selectionRing);
		textBox.MinHeight = 42.0;
		textBox.Width = 150.0;
		textBox.VerticalAlignment = VerticalAlignment.Bottom;
		Grid.SetColumn(textBox, 1);
		grid.Children.Add(textBox);
		if (!isInWindow)
		{
			NotchPresenter? notchPresenter = presenter;
			if (notchPresenter == null || notchPresenter.Tabs.Selected != NotchTab.Document)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				Pressable pressable = NotchPeekSurface.ExpandToWindowButton(delegate
				{
					(host.Actions as INotchActions)?.ExpandNotchIntoWindow(notchChatComposer.IsFollowUp ? "full_reply" : "chat");
				});
				pressable.VerticalAlignment = VerticalAlignment.Bottom;
				pressable.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
				Grid.SetColumn(pressable, 2);
				grid.Children.Add(pressable);
			}
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 6.0,
			Padding = new Thickness(16.0, 6.0, 16.0, 6.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		previewRow = new ScrollViewer
		{
			Content = previews,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled,
			Height = previewSize + 10.0,
			Visibility = Visibility.Collapsed
		};
		stackPanel2.Children.Add(previewRow);
		controls.MinHeight = 30.0;
		controls.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		controls.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		controls.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		controls.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		controls.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		double num2 = (isInWindow ? 30 : 28);
		attach = new Pressable
		{
			Width = num2,
			Height = num2,
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		Ellipse attachDisc = new Ellipse
		{
			Fill = Ui.Brush(look.InkAt(0.08))
		};
		attachGlyph = Icons.Glyph("\ue710", 13.0, look.InkAt(0.7));
		attachGlyph.FontWeight = FontWeights.SemiBold;
		attach.Children.Add(attachDisc);
		attach.Children.Add(attachGlyph);
		attach.HoverChanged += delegate(bool h)
		{
			bool flag = notchChatComposer.attachments.Count >= 10;
			attachDisc.Fill = Ui.Brush(notchChatComposer.look.InkAt((h && !flag) ? 0.14 : 0.08));
			notchChatComposer.attachGlyph.Foreground = Ui.Brush(notchChatComposer.look.InkAt(flag ? 0.3 : (h ? 0.95 : 0.7)));
		};
		attach.Click += PickFiles;
		attach.Tip("Attach files");
		AutomationProperties.SetName(attach, "Attach files");
		controls.Children.Add(attach);
		input = new TextBox
		{
			FontSize = 13.0,
			FontFamily = DS.Fonts.Text,
			Foreground = Ui.Brush(look.InkAt(0.92)),
			Background = Ui.Transparent,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(0.0),
			MinHeight = 0.0,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			IsSpellCheckEnabled = false,
			IsTextScaleFactorEnabled = false,
			SelectionHighlightColor = Ui.Brush(DS.Colors.CursorBlue),
			VerticalAlignment = VerticalAlignment.Center,
			MaxHeight = ComposerBox.LineHeight * 4.0 + 2.0
		};
		ComposerBox.StripChrome(input);
		if (!look.IsDark)
		{
			string[] array = new string[3] { "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused" };
			foreach (string key in array)
			{
				input.Resources[key] = Ui.Brush(look.InkAt(0.92));
			}
		}
		ScrollViewer.SetVerticalScrollBarVisibility(input, ScrollBarVisibility.Auto);
		AutomationProperties.SetName(input, placeholderText?.TrimEnd('…') ?? "Ask HeyClicky");
		placeholder = Ui.Text(placeholderText ?? (IsFollowUp ? "Follow up…" : "Ask HeyClicky…"), 13.0, FontWeights.Normal, look.InkAt(0.45), TextAlignment.Left, 1);
		placeholder.IsHitTestVisible = false;
		placeholder.VerticalAlignment = VerticalAlignment.Center;
		dropLabel = Ui.Text("Drop to attach", 13.0, FontWeights.Medium, look.InkAt(0.8), TextAlignment.Left, 1);
		dropLabel.IsHitTestVisible = false;
		dropLabel.VerticalAlignment = VerticalAlignment.Center;
		dropLabel.Opacity = 0.0;
		Grid grid2 = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)input,
				(UIElement)placeholder,
				(UIElement)dropLabel
			}
		};
		Grid.SetColumn(grid2, 1);
		controls.Children.Add(grid2);
		readAloud = new ReadAloudSwitch(host.Services, 28.0)
		{
			Visibility = Visibility.Collapsed,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(readAloud, 2);
		controls.Children.Add(readAloud);
		readAloud.Toggled += delegate(bool on)
		{
			if (notchChatComposer.isTyping)
			{
				ClickyAnalytics.TrackProductEvent("notch_chat_action", null, null, NotchChatAnalytics.ReadAloudToggled(on, notchChatComposer.CanSend));
			}
		};
		send = new Pressable
		{
			Width = 30.0,
			Height = 30.0,
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		sendGel = new GelSurface(GelAppearance.PrimaryBlue, 15.0);
		sendGlyph = Icons.Glyph("\ue74a", 13.0, DS.Black(0.85));
		sendGlyph.FontWeight = FontWeights.Bold;
		send.Children.Add(sendGel);
		send.Children.Add(sendGlyph);
		send.HoverChanged += delegate(bool h)
		{
			notchChatComposer.sendGel.Hovered = h;
		};
		send.Click += Send;
		send.Tip("Send");
		Grid.SetColumn(send, 3);
		controls.Children.Add(send);
		stackPanel2.Children.Add(controls);
		textBox.Children.Add(stackPanel2);
		base.Children.Add(grid);
		capNotice = new Border
		{
			Height = 24.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(DS.Black(0.75)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top,
			Opacity = 0.0,
			IsHitTestVisible = false,
			Margin = new Thickness(0.0, -10.0, 0.0, 0.0),
			Child = Ui.Text(ComposerAttachmentPolicy.CapNotice, 11.0, FontWeights.Medium, DS.White(0.85)).Center()
		};
		base.Children.Add(capNotice);
		textBox.PointerPressed += delegate
		{
			notchChatComposer.FocusField();
		};
		input.AddHandler(UIElement.PointerPressedEvent, (PointerEventHandler)delegate
		{
			notchChatComposer.FocusField();
		}, handledEventsToo: true);
		input.GotFocus += delegate
		{
			notchChatComposer.SetFocused(focused: true);
			FocusState focusState = notchChatComposer.input.FocusState;
			if ((uint)(focusState - 1) <= 1u)
			{
				notchChatComposer.ClaimKeyboard("focus", consumesFocusRequest: false);
			}
		};
		input.LostFocus += delegate
		{
			notchChatComposer.SetFocused(focused: false);
		};
		input.TextChanged += delegate
		{
			notchChatComposer.placeholder.Visibility = ((notchChatComposer.input.Text.Length != 0 || notchChatComposer.isDropTargeting) ? Visibility.Collapsed : Visibility.Visible);
			notchChatComposer.Changed();
		};
		input.PreviewKeyDown += OnKey;
		input.Paste += OnPaste;
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			double num3 = Math.Max(0.0, e.NewSize.Height - 64.0);
			if (Math.Abs(num3 - notchChatComposer.ExtraHeight) > 0.5)
			{
				notchChatComposer.ExtraHeight = num3;
				notchChatComposer.ExtraHeightChanged?.Invoke(num3);
			}
		};
		RefreshVoiceLabel();
		Apply(animated: false);
		if (!IsFollowUp && !isInWindow)
		{
			EveryDraftCleared += ClearDraft;
		}
		base.Loaded += delegate
		{
			host.KeyboardChanged += notchChatComposer.OnKeyboardChanged;
			host.Services.Companion.StateChanged += notchChatComposer.OnVoiceStateChanged;
			notchChatComposer.RefreshVoiceLabel();
		};
		base.Unloaded += delegate
		{
			host.KeyboardChanged -= notchChatComposer.OnKeyboardChanged;
			host.Services.Companion.StateChanged -= notchChatComposer.OnVoiceStateChanged;
			if (notchChatComposer.IsFollowUp)
			{
				notchChatComposer.Detached();
			}
		};
	}

	private void OnKeyboardChanged(bool hasKeyboard)
	{
		if (!hasKeyboard && isFocused)
		{
			SetFocused(focused: false);
		}
	}

	private void OnVoiceStateChanged(object? sender, EventArgs e)
	{
		base.DispatcherQueue.TryEnqueue(RefreshVoiceLabel);
	}

	private void ShowVoiceIndicator(VoiceState state)
	{
		if (state != shownVoiceState)
		{
			shownVoiceState = state;
			voiceIndicator.Children.Clear();
			Color tint = DS.Black(0.65);
			UIElementCollection children = voiceIndicator.Children;
			children.Add(state switch
			{
				VoiceState.Listening => ActivityIndicators.ListeningBars(tint).view, 
				VoiceState.Responding => ActivityIndicators.SpeakingBars(tint), 
				VoiceState.Processing => ActivityIndicators.ThinkingDots(tint), 
				_ => micAtRest, 
			});
		}
	}

	private void RefreshVoiceLabel()
	{
		string pushToTalkDisplay = host.Services.Settings.PushToTalkDisplay;
		VoiceState state = host.Services.Companion.State;
		TextBlock textBlock = voiceLabel;
		textBlock.Text = state switch
		{
			VoiceState.Listening => "Listening… release to send", 
			VoiceState.Processing => "Thinking…", 
			VoiceState.Responding => "Speaking…", 
			_ => "Hold " + pushToTalkDisplay + " to talk", 
		};
		ShowVoiceIndicator(state);
		voicePlate.Tip(IsFollowUp ? ("Hold " + pushToTalkDisplay + " to follow up") : ("Hold " + pushToTalkDisplay + " to talk to HeyClicky"));
		AutomationProperties.SetHelpText(voicePlate, "Hold " + pushToTalkDisplay + " to talk. Release to send.");
	}

	public void SetStopAction(Action? stop)
	{
		stopAction = stop;
		if (stop == null)
		{
			if ((object)stopButton != null)
			{
				stopButton.Visibility = Visibility.Collapsed;
			}
			return;
		}
		if ((object)stopButton == null)
		{
			Pressable pressable = new Pressable
			{
				Width = 30.0,
				Height = 30.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			GelSurface surface = new GelSurface(GelAppearance.SecondaryWhite, 15.0);
			pressable.Children.Add(surface);
			Grid grid = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			grid.Children.Add(new Rectangle
			{
				Width = 9.0,
				Height = 9.0,
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = Ui.Brush(DS.White(0.8)),
				RenderTransform = new TranslateTransform
				{
					Y = 1.0
				}
			});
			grid.Children.Add(new Rectangle
			{
				Width = 9.0,
				Height = 9.0,
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = Ui.Brush(DS.Black(0.85))
			});
			pressable.Children.Add(grid);
			pressable.HoverChanged += delegate(bool h)
			{
				surface.Hovered = h;
			};
			pressable.WithPressScale();
			pressable.Click += delegate
			{
				stopAction?.Invoke();
			};
			pressable.Tip("Stop this task");
			AutomationProperties.SetName(pressable, "Stop this task");
			AutomationProperties.SetHelpText(pressable, "Ends what it's doing now. The chat stays so you can retry.");
			Grid.SetColumn(pressable, 4);
			controls.Children.Add(pressable);
			stopButton = pressable;
		}
		stopButton.Visibility = Visibility.Visible;
	}

	private void Changed()
	{
		ReportDraft();
		Apply(animated: true);
	}

	private void Apply(bool animated)
	{
		bool flag = isFocused || input.Text.Length > 0 || attachments.Count > 0 || isDropTargeting;
		bool flag2 = flag != isTyping;
		isTyping = flag;
		textBox.Width = ((flag || isInWindow) ? double.NaN : 150.0);
		if (textBox.Parent is Grid grid)
		{
			grid.ColumnDefinitions[0].Width = (flag ? GridLength.Auto : new GridLength(1.0, GridUnitType.Star));
			grid.ColumnDefinitions[1].Width = ((flag || isInWindow) ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto);
		}
		voicePlate.Width = (flag ? 42.0 : double.NaN);
		voiceLabel.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		textBox.Children.OfType<StackPanel>().First().Padding = (isInWindow ? new Thickness(flag ? 8 : 14, 6.0, 6.0, 6.0) : new Thickness(flag ? 8 : 16, 6.0, flag ? 6 : 16, 6.0));
		textBoxPlate.Background = Ui.Brush(PlateFill(flag));
		bool flag3 = isFocused || isDropTargeting;
		textBoxStroke.Visibility = (flag3 ? Visibility.Collapsed : Visibility.Visible);
		selectionRing.Visibility = ((!flag3) ? Visibility.Collapsed : Visibility.Visible);
		UIElement[] array = new UIElement[2] { attach, send };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
		readAloud.Visibility = ((!flag || isInWindow) ? Visibility.Collapsed : Visibility.Visible);
		bool canSend = CanSend;
		sendGel.Enabled = canSend;
		sendGlyph.Foreground = Ui.Brush(canSend ? DS.Black(0.85) : DS.Colors.GelDisabledContent);
		send.Enabled = canSend;
		bool flag4 = attachments.Count >= 10;
		attach.Enabled = !flag4;
		attachGlyph.Foreground = Ui.Brush(look.InkAt(flag4 ? 0.3 : 0.7));
		attach.Tip(flag4 ? $"Up to {10} files" : "Attach files");
		previewRow.Visibility = ((attachments.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		if (!(flag2 && animated) || Anim.ReduceMotion)
		{
			return;
		}
		FrameworkElement[] array2 = new FrameworkElement[3] { attach, readAloud, send };
		foreach (FrameworkElement frameworkElement in array2)
		{
			if (flag)
			{
				frameworkElement.Opacity = 0.0;
				Anim.Opacity(frameworkElement, 1f, 0.26, Easing.EaseOut(Ui.Compositor));
			}
		}
	}

	private Color PlateFill(bool typing)
	{
		if (!isInWindow)
		{
			return DS.White(typing ? 0.16 : 0.12);
		}
		if (!typing)
		{
			return look.ComposerButtonFill;
		}
		return look.ComposerFieldFill;
	}

	private void SetFocused(bool focused)
	{
		if (isFocused != focused)
		{
			isFocused = focused;
			if (!isInWindow)
			{
				presenter?.SetNotchAgentPeekHeldOpen(focused);
				presenter?.SetNotchChatComposerHasKeyboard(focused);
			}
			if (!focused)
			{
				host.ReleaseKeyboardIfUnheld();
			}
			Apply(animated: true);
		}
	}

	private void ReportDraft()
	{
		if (!IsFollowUp && !isInWindow)
		{
			presenter?.SetNotchChatComposerHasDraft(input.Text.Trim().Length > 0 || attachments.Count > 0);
		}
	}

	public void FocusField()
	{
		if (!IsFollowUp && !isInWindow)
		{
			NotchPresenter notchPresenter = presenter;
			if (notchPresenter != null && !notchPresenter.IsNotchChatSessionOpen)
			{
				notchPresenter.OpenNotchChatSession(NotchChatSessionOpenVia.ComposerClick);
			}
		}
		ClaimKeyboard("click", consumesFocusRequest: false);
	}

	public void TakeFocusIfRequested()
	{
		if (presenter != null && presenter.Tabs.HasPendingComposerFocusRequest)
		{
			ClaimKeyboard("request", consumesFocusRequest: true);
		}
	}

	private void ClaimKeyboard(string via, bool consumesFocusRequest)
	{
		int generation = ++focusGeneration;
		nint startForeground = Native.GetForegroundWindow();
		double[] delays = new double[7] { 0.0, 0.06, 0.15, 0.3, 0.5, 0.8, 1.2 };
		bool done = false;
		Try(0);
		for (int i = 1; i < delays.Length; i++)
		{
			if (done)
			{
				break;
			}
			int tryIndex2 = i;
			Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = base.DispatcherQueue.CreateTimer().Rooted();
			dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(delays[i]);
			dispatcherQueueTimer.IsRepeating = false;
			dispatcherQueueTimer.Tick += delegate
			{
				Try(tryIndex2);
			};
			dispatcherQueueTimer.Start();
		}
		void Finish()
		{
			done = true;
			NotchPresenter? notchPresenter3 = presenter;
			if (notchPresenter3 != null && notchPresenter3.Tabs.HasPendingComposerFocusRequest)
			{
				presenter.Tabs.ConsumeComposerFocusRequest();
			}
		}
		void Try(int tryIndex)
		{
			if (!done && generation == focusGeneration)
			{
				if (consumesFocusRequest)
				{
					NotchPresenter? notchPresenter = presenter;
					if (notchPresenter == null || !notchPresenter.Tabs.HasPendingComposerFocusRequest)
					{
						done = true;
						return;
					}
				}
				if (!IsFollowUp && !isInWindow)
				{
					NotchPresenter notchPresenter2 = presenter;
					if (notchPresenter2 != null && !notchPresenter2.IsNotchChatSessionOpen)
					{
						Finish();
						return;
					}
				}
				nint foregroundWindow = Native.GetForegroundWindow();
				if (tryIndex > 0 && foregroundWindow != 0 && foregroundWindow != startForeground && !Native.IsOwnWindow(foregroundWindow))
				{
					Finish();
				}
				else
				{
					if (!HasKeyboardNow)
					{
						host.RequestKeyboard();
						input.Focus(FocusState.Programmatic);
					}
					if (HasKeyboardNow)
					{
						Finish();
						if (tryIndex > 1)
						{
							Log.Info("notch-chat", $"{via}: the composer took the keyboard on try {tryIndex + 1}");
						}
					}
					else if (tryIndex == delays.Length - 1)
					{
						Finish();
						string value = Native.DescribeForeground(Native.GetForegroundWindow());
						Log.Warn("notch-chat", $"{via}: the composer never got the keyboard (window has it: {host.HasKeyboard}, box focused: {IsFieldFocused}, foreground: {value})");
						ClickyAnalytics.ReportInternalError("notch_chat", "composer_no_keyboard", null, $"via {via}; window has the keyboard: {host.HasKeyboard}; box focused: {IsFieldFocused}; foreground: {value}", CrashReportLevel.Warning, via);
					}
				}
			}
		}
	}

	public void Detached()
	{
		focusGeneration++;
		if (!isInWindow)
		{
			if (isFocused)
			{
				presenter?.SetNotchAgentPeekHeldOpen(held: false);
			}
			presenter?.SetNotchChatComposerHasKeyboard(hasKeyboard: false);
		}
		isFocused = false;
	}

	public static void ClearEveryDraft()
	{
		NotchChatComposer.EveryDraftCleared?.Invoke();
	}

	private void ClearDraft()
	{
		input.Text = "";
		attachments.Clear();
		previews.Children.Clear();
		Changed();
	}

	public void ChatSessionClosed()
	{
		if (!isFocused)
		{
			Changed();
		}
	}

	private void OnKey(object sender, KeyRoutedEventArgs e)
	{
		bool flag = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
		if (e.Key == VirtualKey.Enter && !flag)
		{
			e.Handled = true;
			Send();
		}
		else if (e.Key == VirtualKey.Escape)
		{
			e.Handled = true;
			focusGeneration++;
			SetFocused(focused: false);
			host.ReleaseKeyboard();
		}
	}

	private void Send()
	{
		string text = input.Text.Trim();
		List<string> list = attachments.ToList();
		if (text.Length == 0 && list.Count == 0)
		{
			return;
		}
		input.Text = "";
		attachments.Clear();
		previews.Children.Clear();
		if (IsFollowUp)
		{
			string text2 = followUpEntryId;
			ChatHandOff chatHandOff = ((text2 == null) ? null : NotchChat.HandOffs?.Entry(text2)) ?? agentWithoutEvents;
			if ((object)chatHandOff != null)
			{
				(host.Actions as INotchActions)?.SendTypedChatFollowUp(chatHandOff, text, list, isInWindow);
			}
			Changed();
			return;
		}
		if (!isInWindow)
		{
			NotchPresenter notchPresenter = presenter;
			if (notchPresenter != null && !notchPresenter.IsNotchChatSessionOpen)
			{
				notchPresenter.OpenNotchChatSession(NotchChatSessionOpenVia.Send);
			}
		}
		NotchChat.History?.NoteUserSentText(text, list);
		presenter?.NoteTypedChatMessageSent(list.Count);
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, NotchChatAnalytics.ChatSend(list.Count, text.Length > 0, host.Services.Settings.LibraryModeEnabled));
		(host.Actions as INotchActions)?.SubmitChatText(text, list);
		Changed();
	}

	private async void PickFiles()
	{
		try
		{
			FileOpenPicker obj = new FileOpenPicker
			{
				FileTypeFilter = { "*" }
			};
			InitializeWithWindow.Initialize(obj, host.WindowHandle);
			host.RequestKeyboard();
			AddAttachments((await obj.PickMultipleFilesAsync()).Select((StorageFile f) => f.Path));
		}
		catch (Exception ex)
		{
			Log.Warn("notch-chat", "file picker failed: " + ex.Message);
		}
		FocusField();
	}

	public int AddAttachments(IEnumerable<string> paths)
	{
		List<string> incoming = paths.Where((string p) => !attachments.Contains(p)).ToList();
		var (readOnlyList, flag) = ComposerAttachmentPolicy.Admit(attachments.Count, incoming);
		foreach (string item in readOnlyList)
		{
			attachments.Add(item);
			previews.Children.Add(Preview(item));
		}
		if (flag)
		{
			ShowCapNotice();
		}
		Changed();
		return readOnlyList.Count;
	}

	public void DrainDroppedAttachments()
	{
		if (presenter == null)
		{
			return;
		}
		IReadOnlyList<string> readOnlyList = presenter.TakePendingDroppedAttachments();
		if (readOnlyList.Count != 0)
		{
			if (AddAttachments(readOnlyList) > 0)
			{
				ClickyAnalytics.TrackNotchDroppedFilesAttached(readOnlyList.Count);
			}
			FocusField();
		}
	}

	private void ShowCapNotice()
	{
		Anim.Opacity(capNotice, 1f, 0.15);
		capTimer?.Timer.Stop();
		Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = base.DispatcherQueue.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = ComposerAttachmentPolicy.CapNoticeDuration;
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			Anim.Opacity(capNotice, 0f, 0.15);
		};
		dispatcherQueueTimer.Start();
		capTimer = new DispatcherQueueTimerHandle(dispatcherQueueTimer);
	}

	private FrameworkElement Preview(string path)
	{
		Grid tile = new Grid
		{
			Width = previewSize + 6.0,
			Height = previewSize + 6.0
		};
		tile.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(11.0),
			Background = Ui.Brush(isInWindow ? look.InkAt(0.12) : DS.White(0.3))
		});
		Border border = new Border
		{
			Margin = new Thickness(3.0),
			CornerRadius = new CornerRadius(8.0),
			Background = (isInWindow ? Ui.Transparent : Ui.Brush(DS.Colors.Surface2))
		};
		if (ChatBlockViews.IsImage(path) && File.Exists(path))
		{
			border.Background = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(path))
				{
					DecodePixelWidth = (int)(previewSize * 3.0)
				},
				Stretch = Stretch.UniformToFill
			};
		}
		else
		{
			ComposerBox.LoadShellIconAsync(border, path);
		}
		tile.Children.Add(border);
		Pressable remove = new Pressable
		{
			Width = 13.0,
			Height = 13.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 3.0, 3.0, 0.0),
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		remove.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.Black(0.6))
		});
		TextBlock textBlock = Icons.Glyph("\ue711", 7.0, Colors.White);
		textBlock.FontWeight = FontWeights.Bold;
		remove.Children.Add(textBlock);
		remove.Click += delegate
		{
			attachments.Remove(path);
			previews.Children.Remove(tile);
			Changed();
		};
		tile.Children.Add(remove);
		tile.PointerEntered += delegate
		{
			remove.IsHitTestVisible = true;
			Anim.Opacity(remove, 1f, 0.12);
		};
		tile.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				remove.IsHitTestVisible = false;
				Anim.Opacity(remove, 0f, 0.12);
			}
		};
		ToolTipService.SetToolTip(tile, System.IO.Path.GetFileName(path));
		return tile;
	}

	private async void OnPaste(object sender, TextControlPasteEventArgs e)
	{
		DataPackageView content = Clipboard.GetContent();
		if (content.Contains(StandardDataFormats.StorageItems))
		{
			e.Handled = true;
			try
			{
				IReadOnlyList<string> readOnlyList = ComposerAttachmentPolicy.PastableImageFiles((await content.GetStorageItemsAsync()).Select((IStorageItem i) => i.Path));
				if (readOnlyList.Count > 0)
				{
					AddAttachments(readOnlyList);
				}
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("notch-chat", "paste files failed: " + ex.Message);
				return;
			}
		}
		if (!content.Contains(StandardDataFormats.Bitmap))
		{
			return;
		}
		e.Handled = true;
		try
		{
			RandomAccessStreamReference obj = await content.GetBitmapAsync();
			string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"clicky-paste-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png");
			using (IRandomAccessStreamWithContentType src = await obj.OpenReadAsync())
			{
				using FileStream dst = File.Create(path);
				await src.AsStreamForRead().CopyToAsync(dst);
			}
			AddAttachments(new global::_003C_003Ez__ReadOnlySingleElementList<string>(path));
		}
		catch (Exception ex2)
		{
			Log.Warn("notch-chat", "paste image failed: " + ex2.Message);
		}
	}

	public void SetDropTargeting(bool on)
	{
		isDropTargeting = on;
		placeholder.Visibility = ((input.Text.Length != 0 || on) ? Visibility.Collapsed : Visibility.Visible);
		Anim.Opacity(input, (!on) ? 1 : 0, 0.15);
		Anim.Opacity(dropLabel, on ? 1 : 0, 0.15);
		Apply(animated: true);
	}
}
