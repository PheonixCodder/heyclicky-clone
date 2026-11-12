using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class OnboardingCardSurface : NotchSurface
{
	public const int Welcome = 0;

	public const int Microphone = 1;

	public const int ScreenCapture = 2;

	public const int AllSet = 3;

	public const int SignedOut = -1;

	private const int TotalPermissionSteps = 2;

	private readonly Grid view = new Grid();

	private readonly ContentControl cardSlot = new ContentControl
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		IsTabStop = false
	};

	private int step;

	private FrameworkElement? shownCard;

	public override FrameworkElement View => view;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Stretch;

	private bool PermissionsGranted
	{
		get
		{
			if (base.Host.Services.Permissions.GetStatus(PermissionKind.Microphone) == PermissionStatus.Granted)
			{
				return base.Host.Services.Permissions.GetStatus(PermissionKind.ScreenCapture) == PermissionStatus.Granted;
			}
			return false;
		}
	}

	private bool MicrophoneOff
	{
		get
		{
			PermissionStatus status = base.Host.Services.Permissions.GetStatus(PermissionKind.Microphone);
			if ((status == PermissionStatus.WaitingInSettings || status == PermissionStatus.Denied) ? true : false)
			{
				return base.Host.Services.Permissions.GetStatus(PermissionKind.ScreenCapture) == PermissionStatus.Granted;
			}
			return false;
		}
	}

	public OnboardingCardSurface(int step)
	{
		this.step = step;
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		return p is NotchPresentation.OnboardingPinned;
	}

	public override void Update(NotchPresentation p)
	{
		if (p is NotchPresentation.OnboardingPinned onboardingPinned && onboardingPinned.Step != step)
		{
			step = onboardingPinned.Step;
			Build(swap: true);
		}
	}

	protected override void OnAttached()
	{
		base.Host.Services.Permissions.Changed += OnPermissions;
		base.Host.Services.Auth.SignInErrorChanged += OnSignInErrorChanged;
		Build(swap: false);
	}

	public override void Detached()
	{
		base.Host.Services.Permissions.Changed -= OnPermissions;
		base.Host.Services.Auth.SignInErrorChanged -= OnSignInErrorChanged;
	}

	private void OnPermissions(object? s, EventArgs e)
	{
		view.DispatcherQueue.TryEnqueue(delegate
		{
			Build(swap: false);
		});
	}

	private void OnSignInErrorChanged(object? s, EventArgs e)
	{
		view.DispatcherQueue.TryEnqueue(delegate
		{
			if (step == -1)
			{
				Build(swap: false);
			}
		});
	}

	private void Build(bool swap)
	{
		FrameworkElement oldCard = shownCard;
		view.Children.Clear();
		FrameworkElement frameworkElement = (shownCard = Card());
		bool flag = step == -1;
		frameworkElement.VerticalAlignment = (flag ? VerticalAlignment.Center : VerticalAlignment.Top);
		frameworkElement.Margin = new Thickness(16.0, 12.0, 16.0, 12.0);
		if (swap && !Anim.ReduceMotion)
		{
			double num = Math.Max(view.ActualWidth, 300.0);
			CubicBezierEasingFunction easing = Easing.EaseInOut(Ui.Compositor);
			if ((object)oldCard != null)
			{
				oldCard.IsHitTestVisible = false;
				view.Children.Add(oldCard);
				Anim.Opacity(oldCard, 0f, 0.22, easing);
				Anim.Translate(oldCard, new Vector3((float)(0.0 - num), 0f, 0f), 0.22, easing);
				Once.After(0.24, delegate
				{
					view.Children.Remove(oldCard);
				});
			}
			frameworkElement.Opacity = 0.0;
			frameworkElement.Translation = new Vector3((float)num, 0f, 0f);
			Anim.Opacity(frameworkElement, 1f, 0.22, easing);
			Anim.Translate(frameworkElement, Vector3.Zero, 0.22, easing);
		}
		else if (swap)
		{
			frameworkElement.Opacity = 0.0;
			Anim.Opacity(frameworkElement, 1f, 0.22, Easing.EaseInOut(Ui.Compositor));
		}
		view.Children.Add(frameworkElement);
		if (step != 0)
		{
			view.Children.Add(ChromeRow());
		}
	}

	private FrameworkElement ChromeRow()
	{
		Grid grid = new Grid
		{
			Height = 22.0,
			Margin = new Thickness(8.0, 6.0, 12.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		Pressable more = ChromeButton("\ue712", "More", pressFeedback: false);
		MenuFlyout flyout = new MenuFlyout();
		Item("Check for Updates…", delegate
		{
			base.Host.Actions.OpenUrl("https://heyclicky.com/download");
		});
		if (base.Host.Services.Auth.IsSignedIn)
		{
			Item("Log Out", delegate
			{
				base.Host.Services.Auth.SignOutAsync();
			});
		}
		flyout.Items.Add(new MenuFlyoutSeparator());
		Item("Quit HeyClicky", delegate
		{
			base.Host.Actions.Quit();
		});
		more.Click += delegate
		{
			flyout.ShowAt(more);
		};
		Pressable pressable = ChromeButton("\ue711", "Close");
		pressable.Click += delegate
		{
			base.Host.Actions.DismissOnboardingPin();
		};
		StackPanel stackPanel = Ui.H(4.0, more, pressable);
		stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
		grid.Children.Add(stackPanel);
		return grid;
		void Item(string text, Action a)
		{
			MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
			{
				Text = text
			};
			menuFlyoutItem.Click += delegate
			{
				a();
			};
			flyout.Items.Add(menuFlyoutItem);
		}
	}

	private static Pressable ChromeButton(string glyph, string tip, bool pressFeedback = true)
	{
		Pressable pressable = new Pressable
		{
			Width = 22.0,
			Height = 22.0
		};
		TextBlock g = Icons.Glyph(glyph, 10.0, DS.Colors.TextSecondary);
		g.FontWeight = FontWeights.SemiBold;
		pressable.Children.Add(g);
		pressable.WithIconHover(22.0);
		if (pressFeedback)
		{
			pressable.WithPressScale(0.96f, 0.12, easeOut: true);
			pressable.PressedChanged += delegate(bool down)
			{
				Anim.Opacity(g, down ? 0.85f : 1f, 0.12);
			};
		}
		return pressable.Tip(tip);
	}

	private FrameworkElement Card()
	{
		if (step != -1)
		{
			if (base.Host.Services.Auth.IsSignedIn || step == 0)
			{
			}
			switch (step)
			{
			case 1:
				return PermissionCard(PermissionKind.Microphone, 1);
			case 2:
				return PermissionCard(PermissionKind.ScreenCapture, 2);
			case 3:
				return Simple("All set.", "Turn your sound on and tap the button below.", "Meet HeyClicky", delegate
				{
					base.Host.Actions.OnboardingCardAction(3);
				});
			default:
				if (PermissionsGranted)
				{
					return Simple("I live in your notch now.", "Turn your sound on and tap the button below.", "Meet HeyClicky", delegate
					{
						base.Host.Actions.OnboardingCardAction(0);
					});
				}
				if (MicrophoneOff)
				{
					return MicrophoneOffCard();
				}
				return Simple("I live in your notch now.", "Need permissions to get started.", "Let's start", delegate
				{
					base.Host.Actions.OnboardingCardAction(0);
				});
			}
		}
		return SignedOutPlaceholder();
	}

	private FrameworkElement MicrophoneOffCard()
	{
		var (title, subtitle, cta) = NotchOnboardingCardViewModel.MicrophoneOffCopy(base.Host.Services.Permissions.MicrophoneBlockedBy);
		return Simple(title, subtitle, cta, delegate
		{
			base.Host.Actions.OnboardingCardAction(0);
		});
	}

	private FrameworkElement Simple(string title, string subtitle, string cta, Action action)
	{
		Border item = new Border
		{
			Width = 1.0,
			Height = 6.0
		};
		TextBlock item2 = Ui.Text(title, 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true);
		TextBlock item3 = Ui.Text(subtitle, 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
		GelButton gelButton = new GelButton(cta, GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		gelButton.Click += action;
		return new StackPanel
		{
			Spacing = 10.0,
			Children = 
			{
				(UIElement)item,
				(UIElement)new StackPanel
				{
					Spacing = 6.0,
					Children = 
					{
						(UIElement)item2,
						(UIElement)item3
					}
				},
				(UIElement)gelButton
			}
		};
	}

	private FrameworkElement SignedOutPlaceholder()
	{
		TextBlock item = Ui.Text("Sign in to use HeyClicky", 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true);
		string lastSignInErrorMessage = base.Host.Services.Auth.LastSignInErrorMessage;
		TextBlock item2 = ((lastSignInErrorMessage != null) ? Ui.Text(lastSignInErrorMessage, 12.0, FontWeights.Normal, DS.Hex(16737129u), TextAlignment.Center, 3, wrap: true) : Ui.Text("Sees your screen, talks back, runs your apps.", 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true));
		GelButton gelButton = new GelButton("Sign in with Google", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Center
		};
		gelButton.Click += delegate
		{
			base.Host.Actions.SignIn();
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Children = 
			{
				(UIElement)new StackPanel
				{
					Spacing = 6.0,
					Children = 
					{
						(UIElement)item,
						(UIElement)item2
					}
				},
				(UIElement)gelButton
			}
		};
		GelButton gelButton2 = DevSignIn.Button(base.Host.Actions, GelButton.Sizing.Compact);
		if ((object)gelButton2 != null)
		{
			gelButton2.Margin = new Thickness(0.0, -6.0, 0.0, 0.0);
			stackPanel.Children.Add(gelButton2);
		}
		return stackPanel;
	}

	public static (string title, string explanation, string button, bool enabled, bool success) PermissionCopy(PermissionKind kind, PermissionStatus status)
	{
		if (kind == PermissionKind.Microphone)
		{
			switch (status)
			{
			case PermissionStatus.Prompting:
				return (title: "This lets me hear your voice.", explanation: "Click Allow in the Windows prompt.", button: "Granting…", enabled: false, success: false);
			case PermissionStatus.WaitingInSettings:
			case PermissionStatus.WaitingForPicker:
			case PermissionStatus.Denied:
				return (title: "This lets me hear your voice.", explanation: "Turn on microphone access for HeyClicky, then come back.", button: "Open", enabled: true, success: false);
			case PermissionStatus.RestartRequired:
				return (title: "This lets me hear your voice.", explanation: "Already enabled in Settings? Reopen to apply it.", button: "Quit & Reopen HeyClicky", enabled: true, success: false);
			case PermissionStatus.Granted:
				return (title: "Got it!", explanation: "Microphone is ready.", button: "Granted", enabled: false, success: true);
			default:
				return (title: "This lets me hear your voice.", explanation: "Only while you’re holding the shortcut.", button: "Grant Microphone", enabled: true, success: false);
			}
		}
		switch (status)
		{
		case PermissionStatus.Prompting:
			return (title: "I need screen share.", explanation: "Click Allow when Windows asks.", button: "Waiting…", enabled: false, success: false);
		case PermissionStatus.WaitingForPicker:
			return (title: "I need screen share.", explanation: "Pick a display in the picker.", button: "Waiting…", enabled: false, success: false);
		case PermissionStatus.WaitingInSettings:
		case PermissionStatus.Denied:
			return (title: "I need screen share.", explanation: "Turn on screen capture for HeyClicky, then come back.", button: "Open", enabled: true, success: false);
		case PermissionStatus.RestartRequired:
			return (title: "I need screen share.", explanation: "Already enabled in Settings? Reopen to apply it.", button: "Quit & Reopen HeyClicky", enabled: true, success: false);
		case PermissionStatus.Granted:
			return (title: "Got it!", explanation: "Screen capture is ready.", button: "Granted", enabled: false, success: true);
		default:
			return (title: "I need screen share.", explanation: "I only see your screen when you press the hotkey.", button: "Grant Screen Capture", enabled: true, success: false);
		}
	}

	private FrameworkElement PermissionCard(PermissionKind kind, int progressIndex)
	{
		PermissionStatus status = base.Host.Services.Permissions.GetStatus(kind);
		(string title, string explanation, string button, bool enabled, bool success) tuple = PermissionCopy(kind, status);
		string item = tuple.title;
		string item2 = tuple.explanation;
		string item3 = tuple.button;
		bool item4 = tuple.enabled;
		bool item5 = tuple.success;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			Height = 6.0
		};
		for (int i = 0; i < 2; i++)
		{
			bool flag = i < progressIndex;
			stackPanel.Children.Add(new GelSurface((!flag) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, 3.0)
			{
				Width = 22.0,
				Height = 6.0,
				Opacity = (flag ? 1.0 : 0.3)
			});
		}
		AutomationProperties.SetName(stackPanel, $"Step {progressIndex} of {2}");
		TextBlock textBlock = Ui.Text(item, 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true);
		FrameworkElement item6 = textBlock;
		if (item5)
		{
			TextBlock item7 = Icons.Glyph("\uec61", 13.0, DS.Colors.Success);
			item6 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				Children = 
				{
					(UIElement)item7,
					(UIElement)textBlock
				}
			};
		}
		TextBlock item8 = Ui.Text(item2, 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
		FrameworkElement frameworkElement;
		if (item5)
		{
			Grid grid = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Center
			};
			grid.Children.Add(new ShadowLayer(100.0, DS.Black(0.11), 5.0, 4.0));
			grid.Children.Add(new GelSurface(GelAppearance.Mint, 100.0));
			TextBlock textBlock2 = Icons.Glyph("\ue73e", 13.0, DS.Black(0.85));
			textBlock2.FontWeight = FontWeights.Bold;
			Grid grid2 = Ui.Embossed(Ui.Text(item3, 14.0, FontWeights.SemiBold, DS.Black(0.85)));
			grid.Children.Add(Ui.H(6.0, Ui.Embossed(textBlock2), grid2).Pad(22.0, 12.0, 22.0, 12.0));
			frameworkElement = grid;
		}
		else
		{
			GelButton gelButton = new GelButton(item3, GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, (status == PermissionStatus.RestartRequired) ? "\ue72c" : null, null, null);
			gelButton.HorizontalAlignment = HorizontalAlignment.Center;
			gelButton.IsEnabledGel = item4;
			gelButton.Click += async delegate
			{
				PermissionStatus permissionStatus = status;
				if ((permissionStatus == PermissionStatus.WaitingInSettings || permissionStatus == PermissionStatus.Denied) ? true : false)
				{
					base.Host.Services.Permissions.OpenSystemSettings(kind);
					ClickyAnalytics.TrackPermissionSettingsOpened(kind);
				}
				else
				{
					switch (await base.Host.Services.Permissions.RequestAsync(kind))
					{
					case PermissionStatus.WaitingInSettings:
						ClickyAnalytics.TrackPermissionSettingsOpened(kind);
						break;
					case PermissionStatus.Prompting:
						ClickyAnalytics.TrackPermissionNativePromptTriggered(kind);
						break;
					case PermissionStatus.Denied:
						if (kind == PermissionKind.Microphone)
						{
							ClickyAnalytics.TrackPermissionDenied(kind);
						}
						break;
					}
				}
				Build(swap: false);
			};
			frameworkElement = gelButton;
		}
		frameworkElement.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		return new StackPanel
		{
			Spacing = 10.0,
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)new StackPanel
				{
					Spacing = 6.0,
					Children = 
					{
						(UIElement)item6,
						(UIElement)item8
					}
				},
				(UIElement)frameworkElement
			}
		};
	}
}
