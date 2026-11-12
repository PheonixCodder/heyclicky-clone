using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.Services;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;
using HeyClicky.Platform.Input;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI;

namespace HeyClicky.App.Onboarding;

public sealed class IntroWindow
{
	public enum Step
	{
		WhatIsClicky,
		WhereHeardAbout,
		PickLanguage,
		OpenClicky,
		Hatched
	}

	public readonly record struct Point2(double X, double Y);

	private enum CompositionEasingFunctionKind
	{
		InOut,
		In
	}

	private readonly AppServices s;

	private readonly IAppActions actions;

	private readonly PanelWindow window;

	private readonly IntroStageLayout layout = new IntroStageLayout();

	private readonly Microsoft.UI.Dispatching.DispatcherQueue dq;

	private Step step;

	private string? pickedChannel;

	private bool otherPicked;

	private Color pickedColor = DS.Colors.CursorBlue;

	private double grownHeight;

	private bool? lastSignedIn;

	private FrameworkElement? hatchGlyphHost;

	private static readonly SwiftSpring StepSpring = new SwiftSpring(0.38, 0.86);

	private const double ContentWidth = 392.0;

	private TextBlock? titleText;

	private TextBlock? subtitleText;

	private FrameworkElement? footerHost;

	private FrameworkElement? welcomeMark;

	private long lastShake;

	private static readonly (string label, string? logo, string? glyph)[] Channels = new(string, string, string)[13]
	{
		("Instagram Ad", "instagram-logo", null),
		("Instagram", "instagram-logo", null),
		("YouTube Ad", "youtube-logo", null),
		("YouTube", "youtube-logo", null),
		("TikTok", "tiktok-logo", null),
		("X/Twitter", "x-logo-tinted", null),
		("LinkedIn", "linkedin-logo", null),
		("Reddit", "reddit-logo", null),
		("ChatGPT", "chatgpt-logo-tinted", null),
		("Claude", "claude-logo", null),
		("Google", "google-logo", null),
		("A friend", null, "\ue716"),
		("Other", null, "\ue712")
	};

	private TextBox? otherBox;

	private List<string>? pickedLanguages;

	private List<string>? suggestedLanguages;

	private bool regionLanguagesRequested;

	private string languageSearch = "";

	private bool languageLimitReached;

	private StackPanel? languageSections;

	private TextBlock? languageNote;

	private Grid? hatchGlow;

	private FrameworkElement? hatchMark;

	public PanelWindow Window => window;

	public PreSignInIntroViewModel? Vm { get; init; }

	public Step CurrentStep => step;

	public bool IsShowing => Native.IsWindowVisible(window.Hwnd);

	private Step? Previous => step switch
	{
		Step.WhereHeardAbout => Step.WhatIsClicky, 
		Step.PickLanguage => Step.WhereHeardAbout, 
		Step.OpenClicky => (!LanguageStepEnabled) ? Step.WhereHeardAbout : Step.PickLanguage, 
		_ => null, 
	};

	private bool LanguageStepEnabled => Vm?.IsLanguageStepEnabled ?? false;

	private Step StepAfterDiscovery
	{
		get
		{
			if (!LanguageStepEnabled)
			{
				return Step.OpenClicky;
			}
			return Step.PickLanguage;
		}
	}

	public event Action<Point2>? FlightRequested;

	public event Action? ReducedMotionHandOff;

	public event Action? Finished;

	public void OnSignedInChanged(bool signedIn)
	{
		bool flag = lastSignedIn != signedIn;
		lastSignedIn = signedIn;
		if (signedIn && flag && IsShowing)
		{
			window.BringToFrontRecentered();
			ClickyAnalytics.TrackPreSignInIntroAttentionRequested(AnalyticsName(this.step));
			Log.Info("intro", "signed in: intro brought to front and re-centered");
		}
		if (this.step == Step.WhatIsClicky && signedIn)
		{
			Go(Step.WhereHeardAbout);
			return;
		}
		Step step = this.step;
		if ((step == Step.WhatIsClicky || step == Step.Hatched) ? true : false)
		{
			Render(animate: false);
		}
	}

	private void OnSignInErrorChanged()
	{
		if (step == Step.WhatIsClicky && !s.Auth.IsSignedIn)
		{
			Render(animate: false);
			if (IsShowing && s.Auth.LastSignInErrorMessage != null)
			{
				window.BringToFrontRecentered();
			}
		}
	}

	public IntroWindow(AppServices services, IAppActions actions, bool excludeFromCapture)
	{
		s = services;
		this.actions = actions;
		dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		lastSignedIn = s.Auth.IsSignedIn;
		window = new PanelWindow("HeyClicky", excludeFromCapture, showInTaskbar: true, 14.0, null);
		layout.Margin = new Thickness(24.0);
		layout.Width = 392.0;
		layout.MinimumHeight = 232.0;
		window.Panel.Children.Add(layout);
		layout.SizeChanged += delegate
		{
			Grow();
		};
		s.Auth.Changed += delegate
		{
			dq.TryEnqueue(delegate
			{
				OnSignedInChanged(s.Auth.IsSignedIn);
			});
		};
		s.Auth.SignInErrorChanged += delegate
		{
			dq.TryEnqueue(OnSignInErrorChanged);
		};
		window.Panel.PointerEntered += delegate
		{
			if (step == Step.WhatIsClicky)
			{
				Shake();
			}
		};
		window.SetPanelSize(440.0, 280.0);
		Render(animate: false);
	}

	public void Show(Step at = Step.WhatIsClicky)
	{
		step = at;
		Render(animate: false);
		window.ShowCentered();
		ClickyAnalytics.TrackPreSignInIntroStepViewed(AnalyticsName(at));
	}

	public static string AnalyticsName(Step st)
	{
		return st switch
		{
			Step.WhatIsClicky => "what_is_clicky", 
			Step.WhereHeardAbout => "where_heard_about", 
			Step.PickLanguage => "pick_language", 
			Step.OpenClicky => "open_clicky", 
			_ => "hatched", 
		};
	}

	private void TrackButton(string action, string? value = null)
	{
		ClickyAnalytics.TrackPreSignInIntroButtonTapped(AnalyticsName(step), action, value);
	}

	public void Hide()
	{
		window.HideWindow();
	}

	private void Grow()
	{
		layout.Measure(new Size(392.0, double.PositiveInfinity));
		double num = Math.Max(grownHeight, layout.DesiredSize.Height + 48.0);
		if (num > grownHeight + 0.5)
		{
			grownHeight = num;
			window.SetPanelSize(440.0, num);
			if (IsShowing && Native.GetWindowRectPublic(window.Hwnd, out var rc))
			{
				Log.Info("intro", $"grew to {num:0} pt on {AnalyticsName(step)}: re-centered at {rc.Left},{rc.Top} {rc.Right - rc.Left}x{rc.Bottom - rc.Top} px");
			}
		}
	}

	public void Go(Step next)
	{
		if (next != step)
		{
			step = next;
			ClickyAnalytics.TrackPreSignInIntroStepViewed(AnalyticsName(next));
			Render(animate: true);
			if (next == Step.Hatched)
			{
				RunHatchTimeline();
			}
		}
	}

	private void Render(bool animate)
	{
		(string, string) tuple = Copy();
		string item = tuple.Item1;
		string item2 = tuple.Item2;
		FrameworkElement header = Header(item, item2);
		FrameworkElement body = Body();
		FrameworkElement frameworkElement = Footer();
		bool hasFooter = (object)frameworkElement != null;
		_ = layout.Body;
		layout.SetParts(header, body, frameworkElement, hasFooter);
		footerHost = frameworkElement;
		if (step == Step.Hatched)
		{
			titleText.Opacity = 0.0;
			subtitleText.Opacity = 0.0;
			if ((object)frameworkElement != null)
			{
				frameworkElement.Opacity = 0.0;
			}
		}
		if (animate)
		{
			body.Opacity = 0.0;
			body.Translation = new Vector3(0f, 12f, 0f);
			body.Scale = new Vector3(0.985f, 0.985f, 1f);
			body.SizeChanged += delegate
			{
				body.CenterPoint = new Vector3((float)body.ActualWidth / 2f, (float)body.ActualHeight / 2f, 0f);
			};
			Anim.OpacitySpring(body, 1f, StepSpring);
			Anim.TranslationSpring(body, Vector3.Zero, StepSpring);
			Anim.ScaleSpring(body, 1f, StepSpring, null);
		}
	}

	private (string, string) Copy()
	{
		return step switch
		{
			Step.WhatIsClicky => ("Welcome to HeyClicky.", "A tiny AI buddy that lives on your PC."), 
			Step.WhereHeardAbout => ("How’d you find this?", "We're a tiny team, and knowing where you heard about us helps a ton."), 
			Step.PickLanguage => ("What should I speak?", "Pick one or more. Automatic answers in whatever language you talk to me in."), 
			Step.OpenClicky => ("Pick a color", "Click one to bring me to life. Choose wisely."), 
			_ => ("Hi, " + (FirstName() ?? "friend") + ".", "I'm HeyClicky."), 
		};
	}

	private string? FirstName()
	{
		UserProfile user = s.Auth.User;
		if ((object)user == null)
		{
			return null;
		}
		string text = null;
		if (!string.IsNullOrWhiteSpace(user.DisplayName))
		{
			text = user.DisplayName.Trim().Split(' ')[0];
		}
		else if (!string.IsNullOrEmpty(user.Email))
		{
			string text2 = user.Email.Split('@')[0];
			string text3 = text2.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/))[0];
			text = ((text3.Length >= 2) ? text3 : text2);
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text[0]));
		string text4 = text;
		return string.Concat(readOnlySpan, text4.Substring(1, text4.Length - 1));
	}

	private FrameworkElement Header(string title, string subtitle)
	{
		Grid grid = new Grid
		{
			Height = 28.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		Step? previous = Previous;
		if (previous.HasValue)
		{
			Step prev = previous.GetValueOrDefault();
			Pressable pressable = new Pressable
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(4.0, 6.0, 4.0, 6.0)
			};
			TextBlock textBlock = Icons.Glyph("\ue76b", 10.0, DS.Colors.TextSecondary);
			textBlock.FontWeight = FontWeights.SemiBold;
			pressable.Children.Add(Ui.H(3.0, textBlock, Ui.Medium("Back", 12.0, DS.Colors.TextSecondary)));
			pressable.WithPressScale();
			pressable.Click += delegate
			{
				Go(prev);
			};
			grid.Children.Add(pressable);
		}
		titleText = Ui.Text(title, 20.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true);
		subtitleText = Ui.Text(subtitle, 13.0, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
		subtitleText.LineHeight = 19.29;
		subtitleText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
		StackPanel item = new StackPanel
		{
			Spacing = 8.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 22.0),
			Children = 
			{
				(UIElement)titleText,
				(UIElement)subtitleText
			}
		};
		return new StackPanel
		{
			Children = 
			{
				(UIElement)grid,
				(UIElement)item
			}
		};
	}

	private FrameworkElement Body()
	{
		return step switch
		{
			Step.WhatIsClicky => WelcomeGlyph(), 
			Step.WhereHeardAbout => Discovery(), 
			Step.PickLanguage => LanguagePicker(), 
			Step.OpenClicky => ColorPicker(), 
			_ => HatchGlyph(), 
		};
	}

	private FrameworkElement? Footer()
	{
		GelButton gelButton = null;
		switch (step)
		{
		case Step.WhatIsClicky:
			if (s.Auth.IsSignedIn)
			{
				gelButton = new GelButton("Continue", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null);
				gelButton.Click += ContinueFromWelcome;
			}
			else
			{
				gelButton = new GelButton("Sign in with Google", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null);
				gelButton.Click += SignInFromWelcome;
			}
			break;
		case Step.WhereHeardAbout:
			if (!otherPicked)
			{
				return null;
			}
			gelButton = new GelButton("Continue", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null);
			gelButton.Click += CommitOther;
			break;
		case Step.PickLanguage:
			gelButton = new GelButton("Continue", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null);
			gelButton.Click += ContinueLanguage;
			break;
		case Step.OpenClicky:
			return null;
		case Step.Hatched:
			gelButton = new GelButton("Give me a home", GelAppearance.PrimaryBlue, GelButton.Sizing.Large, 100.0, null, null, null);
			gelButton.Click += GiveHome;
			break;
		}
		if ((object)gelButton == null)
		{
			return null;
		}
		gelButton.HorizontalAlignment = HorizontalAlignment.Center;
		StackPanel stackPanel = new StackPanel
		{
			Padding = new Thickness(0.0, 22.0, 0.0, 0.0),
			Spacing = 8.0,
			Children = { (UIElement)gelButton }
		};
		if (step == Step.WhatIsClicky && !s.Auth.IsSignedIn)
		{
			string lastSignInErrorMessage = s.Auth.LastSignInErrorMessage;
			if (lastSignInErrorMessage != null)
			{
				TextBlock textBlock = Ui.Text(lastSignInErrorMessage, 12.0, FontWeights.Normal, DS.Hex(16737129u), TextAlignment.Center, 3, wrap: true);
				textBlock.MaxWidth = 320.0;
				stackPanel.Children.Add(textBlock);
			}
		}
		if (step == Step.WhatIsClicky && !s.Auth.IsSignedIn)
		{
			GelButton gelButton2 = DevSignIn.Button(actions, GelButton.Sizing.Compact);
			if ((object)gelButton2 != null)
			{
				stackPanel.Children.Add(gelButton2);
			}
		}
		return stackPanel;
	}

	internal void ContinueFromWelcome()
	{
		TrackButton("continue");
		Go(Step.WhereHeardAbout);
	}

	internal void SignInFromWelcome()
	{
		TrackButton("sign_in_with_google");
		PreSignInIntroViewModel vm = Vm;
		if (vm != null)
		{
			vm.ContinueFromWelcome();
		}
		else
		{
			actions.SignIn();
		}
	}

	private FrameworkElement WelcomeGlyph()
	{
		Color color = DS.Hex(2450411u);
		Grid obj = new Grid
		{
			Height = 58.0,
			Width = 120.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		Microsoft.UI.Xaml.Shapes.Path path = IntroMark.Create(38.0, color);
		Grid grid = new Grid
		{
			Width = 38.0,
			Height = 38.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid.Children.Add(ShapeGlow(path, color, 0.4, 36.0));
		grid.Children.Add(ShapeGlow(path, color, 0.65, 18.0));
		grid.Children.Add(path);
		grid.CenterPoint = new Vector3(19f, 19f, 0f);
		obj.Children.Add(grid);
		welcomeMark = grid;
		return obj;
	}

	private static Grid ShapeGlow(Shape shape, Color color, double alpha, double swiftRadius)
	{
		Grid obj = new Grid
		{
			Width = shape.Width,
			Height = shape.Height,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		DropShadow dropShadow = Ui.Compositor.CreateDropShadow();
		dropShadow.Color = color;
		dropShadow.Opacity = (float)alpha;
		dropShadow.BlurRadius = (float)(swiftRadius * 2.0);
		dropShadow.Mask = shape.GetAlphaMask();
		SpriteVisual spriteVisual = Ui.Compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2((float)shape.Width, (float)shape.Height);
		spriteVisual.Shadow = dropShadow;
		ElementCompositionPreview.SetElementChildVisual(obj, spriteVisual);
		return obj;
	}

	private void Shake()
	{
		if ((object)welcomeMark != null && !Anim.ReduceMotion && Environment.TickCount64 - lastShake >= 450)
		{
			lastShake = Environment.TickCount64;
			Anim.KeyframesScalar(welcomeMark, "Rotation", new(float, double)[6]
			{
				(0f, 0.04),
				(-12f, 0.07),
				(10f, 0.07),
				(-7f, 0.07),
				(4f, 0.07),
				(0f, 0.07)
			});
		}
	}

	private FrameworkElement Discovery()
	{
		CenteredWrapPanel centeredWrapPanel = new CenteredWrapPanel
		{
			HorizontalSpacing = 8.0,
			VerticalSpacing = 8.0
		};
		(string, string, string)[] channels = Channels;
		for (int i = 0; i < channels.Length; i++)
		{
			var (label, logo, glyph) = channels[i];
			centeredWrapPanel.Children.Add(Chip(label, logo, glyph));
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Children = { (UIElement)centeredWrapPanel }
		};
		if (otherPicked)
		{
			TextBox box = new TextBox
			{
				PlaceholderText = "Where'd you hear about us? (optional)",
				FontSize = 13.0,
				MaxLength = 280,
				MaxWidth = 320.0,
				Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
				CornerRadius = new CornerRadius(12.0),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Background = Ui.Brush(DS.White(0.06)),
				BorderBrush = Ui.Brush(DS.White(0.16)),
				BorderThickness = new Thickness(0.5),
				Foreground = Ui.Brush(DS.Colors.TextPrimary),
				IsTextScaleFactorEnabled = false
			};
			box.Resources["TextControlBorderBrushFocused"] = Ui.Brush(DS.White(0.28));
			box.Resources["TextControlBackgroundFocused"] = Ui.Brush(DS.White(0.06));
			box.Resources["TextControlBackgroundPointerOver"] = Ui.Brush(DS.White(0.06));
			box.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.5);
			box.KeyDown += delegate(object _, KeyRoutedEventArgs e)
			{
				if (e.Key == VirtualKey.Enter)
				{
					CommitOther();
				}
			};
			box.Loaded += delegate
			{
				box.Focus(FocusState.Programmatic);
			};
			otherBox = box;
			stackPanel.Children.Add(box);
		}
		return stackPanel;
	}

	internal void CommitOther()
	{
		string text = otherBox?.Text.Trim() ?? "";
		PreSignInIntroViewModel vm = Vm;
		if (vm != null)
		{
			vm.OtherDiscoveryText = otherBox?.Text ?? "";
			vm.CommitOtherDiscoveryChannelAsync();
		}
		else
		{
			s.Settings.DiscoveryChannel = "Other" + ((text.Length == 0) ? "" : (": " + text));
			ClickyAnalytics.TrackDiscoveryChannelSelected("other", (text.Length == 0) ? null : text);
		}
		TrackButton("submit_discovery_channel_other", (text.Length == 0) ? "skipped" : "filled");
		ApplyDefaultOnboardingLook();
		Go(StepAfterDiscovery);
	}

	private static async Task PersistDiscovery(PreSignInIntroViewModel vm, string id)
	{
		try
		{
			await vm.PickDiscoveryChannelAsync(id);
		}
		catch (Exception ex)
		{
			Log.Warn("intro", "discovery channel: " + ex.Message);
		}
	}

	private FrameworkElement Chip(string label, string? logo, string? glyph)
	{
		bool picked = pickedChannel == label;
		Pressable p = new Pressable
		{
			CornerRadius = new CornerRadius(100.0),
			Padding = new Thickness(13.0, 9.0, 13.0, 9.0),
			BorderThickness = new Thickness(0.5)
		};
		FrameworkElement frameworkElement;
		if (logo != null)
		{
			frameworkElement = new Image
			{
				Source = new SvgImageSource(new Uri(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", (picked ? logo.Replace("-tinted", "") : logo) + ".svg")))
				{
					RasterizePixelWidth = 34.0,
					RasterizePixelHeight = 34.0
				},
				Width = 17.0,
				Height = 17.0,
				Stretch = Stretch.Uniform
			};
		}
		else
		{
			TextBlock textBlock = Icons.Glyph(glyph, 13.0, picked ? DS.Black(0.7) : DS.Colors.TextSecondary);
			textBlock.FontWeight = FontWeights.SemiBold;
			frameworkElement = new Grid
			{
				Width = 17.0,
				Height = 17.0,
				Children = { (UIElement)textBlock }
			};
		}
		TextBlock textBlock2 = Ui.Medium(label, 13.0, picked ? DS.Black(0.9) : DS.Colors.TextPrimary);
		p.Children.Add(Ui.H(7.0, frameworkElement, textBlock2));
		Paint(hover: false);
		p.HoverChanged += delegate(bool h)
		{
			Paint(h);
			if (!picked)
			{
				Anim.Scale(p, h ? 1.05f : 1f, 0.14, Easing.EaseOut(Ui.Compositor), null);
			}
		};
		p.SizeChanged += delegate
		{
			p.CenterPoint = new Vector3((float)p.ActualWidth / 2f, (float)p.ActualHeight / 2f, 0f);
		};
		p.WithPressScale();
		p.Click += delegate
		{
			PickChannel(label);
		};
		return p;
		void Paint(bool hover)
		{
			p.Background = Ui.Brush(picked ? Colors.White : DS.White(hover ? 0.12 : 0.05));
			p.BorderBrush = Ui.Brush(picked ? Colors.Transparent : DS.White(hover ? 0.22 : 0.1));
		}
	}

	internal void PickChannel(string label)
	{
		Sounds.Play("skill-up", 0.3);
		pickedChannel = label;
		string text = OnboardingCatalog.DiscoveryChannels.FirstOrDefault((DiscoveryChannelOption c) => c.Label == label)?.Id ?? "other";
		TrackButton("pick_discovery_channel", text);
		if (label == "Other")
		{
			otherPicked = true;
			Vm?.PickDiscoveryChannelAsync("other");
			Render(animate: false);
			return;
		}
		otherPicked = false;
		PreSignInIntroViewModel vm = Vm;
		if (vm != null)
		{
			PersistDiscovery(vm, text);
		}
		else
		{
			s.Settings.DiscoveryChannel = label;
			ClickyAnalytics.TrackDiscoveryChannelSelected(text);
		}
		Render(animate: false);
		After(0.14, delegate
		{
			ApplyDefaultOnboardingLook();
			Go(StepAfterDiscovery);
		});
	}

	private static IReadOnlyList<string> SystemLanguageTags()
	{
		try
		{
			return GlobalizationPreferences.Languages.ToList();
		}
		catch
		{
			return new global::_003C_003Ez__ReadOnlySingleElementList<string>(CultureInfo.CurrentUICulture.Name);
		}
	}

	private static string? DeviceRegion()
	{
		try
		{
			return GlobalizationPreferences.HomeGeographicRegion;
		}
		catch
		{
			return RegionInfo.CurrentRegion.TwoLetterISORegionName;
		}
	}

	private FrameworkElement LanguagePicker()
	{
		IReadOnlyList<string> readOnlyList = VoiceCatalog.SystemLanguageCodes(SystemLanguageTags());
		if (suggestedLanguages == null)
		{
			suggestedLanguages = readOnlyList.Concat(from code in VoiceCatalog.SystemLanguageCodes(KeyboardLayoutInfo.InstalledLayoutLanguageTags())
				where !VoiceCatalog.IsEnglish(code)
				select code).Distinct().ToList();
		}
		if (pickedLanguages == null)
		{
			List<string> list = Vm?.VoiceLanguages.ToList() ?? new List<string>();
			pickedLanguages = ((list.Count > 0) ? list : readOnlyList.Take(1).ToList());
		}
		if (!regionLanguagesRequested)
		{
			PreSignInIntroViewModel vm = Vm;
			if (vm != null)
			{
				regionLanguagesRequested = true;
				AddRegionLanguagesAsync(vm);
			}
		}
		TextBox box = new TextBox
		{
			PlaceholderText = $"Search {VoiceCatalog.Languages.Count} languages",
			Text = languageSearch,
			FontSize = 13.0,
			MaxWidth = 320.0,
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			CornerRadius = new CornerRadius(12.0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = Ui.Brush(DS.White(0.06)),
			BorderBrush = Ui.Brush(DS.White(0.16)),
			BorderThickness = new Thickness(0.5),
			Foreground = Ui.Brush(DS.Colors.TextPrimary),
			IsTextScaleFactorEnabled = false
		};
		box.Resources["TextControlBorderBrushFocused"] = Ui.Brush(DS.White(0.28));
		box.Resources["TextControlBackgroundFocused"] = Ui.Brush(DS.White(0.06));
		box.Resources["TextControlBackgroundPointerOver"] = Ui.Brush(DS.White(0.06));
		box.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.5);
		box.TextChanged += delegate
		{
			languageSearch = box.Text;
			FillLanguageChips();
		};
		languageSections = new StackPanel
		{
			Spacing = 14.0
		};
		languageNote = Ui.Text("", 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
		FillLanguageChips();
		return new StackPanel
		{
			Spacing = 14.0,
			Children = 
			{
				(UIElement)box,
				(UIElement)languageSections,
				(UIElement)languageNote
			}
		};
	}

	private async Task AddRegionLanguagesAsync(PreSignInIntroViewModel vm)
	{
		IReadOnlyList<string> region = await vm.RegionLanguagesAsync(DeviceRegion());
		dq.TryEnqueue(delegate
		{
			if (suggestedLanguages != null && region.Count != 0)
			{
				int count = suggestedLanguages.Count;
				foreach (string item in region)
				{
					if (!suggestedLanguages.Contains(item) && suggestedLanguages.Count < 6)
					{
						suggestedLanguages.Add(item);
					}
				}
				if (suggestedLanguages.Count != count && step == Step.PickLanguage)
				{
					FillLanguageChips();
				}
			}
		});
	}

	private void FillLanguageChips()
	{
		if ((object)languageSections == null || pickedLanguages == null || suggestedLanguages == null)
		{
			return;
		}
		languageSections.Children.Clear();
		string text = languageSearch.Trim();
		int num;
		if (text.Length == 0)
		{
			List<VoiceLanguage> list = Named(suggestedLanguages);
			List<VoiceLanguage> list2 = Named(from c in VoiceCatalog.PopularCodes.Concat(pickedLanguages.Where((string c) => !VoiceCatalog.PopularCodes.Contains(c)))
				where !suggestedLanguages.Contains(c)
				select c);
			if (list.Count > 0)
			{
				languageSections.Children.Add(ChipSection("Suggested for you", list, includesAutomatic: false));
			}
			languageSections.Children.Add(ChipSection((list.Count > 0) ? "More languages" : null, list2, includesAutomatic: true));
			num = list.Count + list2.Count;
		}
		else
		{
			List<VoiceLanguage> list3 = VoiceCatalog.PickerList(text).Take(18).ToList();
			languageSections.Children.Add(ChipSection(null, list3, includesAutomatic: false));
			num = list3.Count;
		}
		if ((object)languageNote != null)
		{
			languageNote.Text = ((num == 0) ? ("No languages match \"" + text + "\".") : (languageLimitReached ? $"Up to {5} languages. Take one off to add another." : ""));
			languageNote.Visibility = ((languageNote.Text.Length == 0) ? Visibility.Collapsed : Visibility.Visible);
		}
		Grow();
		static List<VoiceLanguage> Named(IEnumerable<string> codes)
		{
			return codes.Select(VoiceCatalog.Language).OfType<VoiceLanguage>().ToList();
		}
	}

	private FrameworkElement ChipSection(string? title, IReadOnlyList<VoiceLanguage> languages, bool includesAutomatic)
	{
		CenteredWrapPanel centeredWrapPanel = new CenteredWrapPanel
		{
			HorizontalSpacing = 8.0,
			VerticalSpacing = 8.0
		};
		if (includesAutomatic)
		{
			centeredWrapPanel.Children.Add(TextChip("Automatic", pickedLanguages.Count == 0, delegate
			{
				PickLanguage(null);
			}));
		}
		foreach (VoiceLanguage language in languages)
		{
			string code = language.Code;
			centeredWrapPanel.Children.Add(TextChip(language.Name, pickedLanguages.Contains(code), delegate
			{
				PickLanguage(code);
			}));
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0
		};
		if (title != null)
		{
			TextBlock textBlock = Ui.Text(title.ToUpperInvariant(), 10.0, FontWeights.SemiBold, DS.Colors.TextTertiary, TextAlignment.Center);
			textBlock.CharacterSpacing = 110;
			stackPanel.Children.Add(textBlock);
		}
		stackPanel.Children.Add(centeredWrapPanel);
		return stackPanel;
	}

	internal void PickLanguage(string? code)
	{
		if (pickedLanguages == null)
		{
			pickedLanguages = new List<string>();
		}
		if (code == null)
		{
			pickedLanguages.Clear();
		}
		else if (!pickedLanguages.Remove(code))
		{
			if (pickedLanguages.Count >= 5)
			{
				languageLimitReached = true;
				FillLanguageChips();
				return;
			}
			pickedLanguages.Add(code);
		}
		languageLimitReached = false;
		Sounds.Play("skill-up", 0.3);
		TrackButton("pick_voice_language", code ?? "automatic");
		FillLanguageChips();
	}

	internal void ContinueLanguage()
	{
		List<string> list = pickedLanguages ?? new List<string>();
		TrackButton("continue_voice_language", (list.Count == 0) ? "automatic" : string.Join(",", list));
		Vm?.PickVoiceLanguages(list);
		languageSearch = "";
		Go(Step.OpenClicky);
	}

	private static FrameworkElement TextChip(string label, bool picked, Action onPick)
	{
		Pressable p = new Pressable
		{
			CornerRadius = new CornerRadius(100.0),
			Padding = new Thickness(13.0, 9.0, 13.0, 9.0),
			BorderThickness = new Thickness(0.5)
		};
		p.Children.Add(Ui.Medium(label, 13.0, picked ? DS.Black(0.9) : DS.Colors.TextPrimary));
		Paint(hover: false);
		p.HoverChanged += delegate(bool h)
		{
			Paint(h);
			if (!picked)
			{
				Anim.Scale(p, h ? 1.05f : 1f, 0.14, Easing.EaseOut(Ui.Compositor), null);
			}
		};
		p.SizeChanged += delegate
		{
			p.CenterPoint = new Vector3((float)p.ActualWidth / 2f, (float)p.ActualHeight / 2f, 0f);
		};
		p.WithPressScale();
		p.Click += onPick;
		return p;
		void Paint(bool hover)
		{
			p.Background = Ui.Brush(picked ? Colors.White : DS.White(hover ? 0.12 : 0.05));
			p.BorderBrush = Ui.Brush(picked ? Colors.Transparent : DS.White(hover ? 0.22 : 0.1));
		}
	}

	private void ApplyDefaultOnboardingLook()
	{
		PreSignInIntroViewModel vm = Vm;
		if (vm != null)
		{
			vm.ApplyDefaultOnboardingLook();
		}
		else
		{
			s.Settings.SquadPack = OnboardingCatalog.DefaultSquadPack.Id;
		}
		StarterCast.Outfit();
	}

	public static FrameworkElement GelCheckmark(double size)
	{
		Grid obj = new Grid
		{
			Width = size,
			Height = size,
			Children = { (UIElement)new GelSurface(GelAppearance.PrimaryBlue, size / 2.0) }
		};
		TextBlock textBlock = Icons.Glyph("\ue73e", size * 0.45, Colors.Black);
		textBlock.FontWeight = FontWeights.Bold;
		obj.Children.Add(textBlock);
		return obj;
	}

	private FrameworkElement ColorPicker()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 20.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		List<FrameworkElement> dots = new List<FrameworkElement>();
		(string, Color)[] array = new(string, Color)[4]
		{
			("Red", DS.Colors.CursorRed),
			("Blue", DS.Colors.CursorBlue),
			("Yellow", DS.Colors.CursorYellow),
			("Green", DS.Colors.CursorGreen)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, Color) tuple = array[i];
			string name = tuple.Item1;
			Color color = tuple.Item2;
			Pressable p = new Pressable
			{
				Width = 36.0,
				Height = 36.0,
				ShowsDefaultHover = false
			};
			if (DS.ToHex(color) == DS.ToHex(pickedColor))
			{
				_ = step != Step.OpenClicky;
			}
			else
				_ = 0;
			Ellipse ellipse = new Ellipse
			{
				Width = 26.0,
				Height = 26.0,
				Fill = Ui.Brush(color)
			};
			Grid restGlow = ShapeGlow(ellipse, color, 0.35, 5.0);
			Grid pickedGlow = ShapeGlow(ellipse, color, 0.65, 10.0);
			pickedGlow.Opacity = 0.0;
			p.Children.Add(restGlow);
			p.Children.Add(pickedGlow);
			p.Children.Add(ellipse);
			Ellipse ring = new Ellipse
			{
				Width = 32.0,
				Height = 32.0,
				Stroke = Ui.Brush(DS.White(0.95)),
				StrokeThickness = 1.5,
				Opacity = 0.0
			};
			p.Children.Add(ring);
			p.CenterPoint = new Vector3(18f, 18f, 0f);
			p.WithPressScale();
			p.Click += delegate
			{
				ring.Opacity = 1.0;
				Anim.Opacity(pickedGlow, 1f, 0.16, Easing.Expressive(Ui.Compositor));
				Anim.Opacity(restGlow, 0f, 0.16, Easing.Expressive(Ui.Compositor));
				Anim.Scale(p, 1.1f, 0.16, Easing.Expressive(Ui.Compositor), null);
				PickColor(name, color);
			};
			AutomationProperties.SetName(p, name);
			p.Opacity = 0.0;
			p.Translation = new Vector3(0f, 10f, 0f);
			p.Scale = new Vector3(0.78f, 0.78f, 1f);
			dots.Add(p);
			stackPanel.Children.Add(p);
		}
		After(Anim.ReduceMotion ? 0.02 : 0.12, delegate
		{
			for (int j = 0; j < dots.Count; j++)
			{
				RevealDot(dots[j], j);
			}
		});
		return stackPanel;
	}

	private static void RevealDot(UIElement d, int index)
	{
		if (Anim.Instant || Anim.ReduceMotion)
		{
			d.Translation = Vector3.Zero;
			d.Scale = Vector3.One;
			Anim.Opacity(d, 1f, 0.12, Easing.EaseOut(Ui.Compositor));
			return;
		}
		Compositor compositor = Ui.Compositor;
		LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		Vector3KeyFrameAnimation vector3KeyFrameAnimation2 = compositor.CreateVector3KeyFrameAnimation();
		for (int i = 1; i <= 36; i++)
		{
			float num = (float)i / 36f;
			float num2 = (float)Math.Clamp((CubicBezier(0.23, 1.0, 0.32, 1.0, num) - 0.11 * (double)index) / 0.56, 0.0, 1.0);
			scalarKeyFrameAnimation.InsertKeyFrame(num, num2, easingFunction);
			vector3KeyFrameAnimation.InsertKeyFrame(num, new Vector3(0f, (1f - num2) * 10f, 0f), easingFunction);
			float num3 = 0.78f + 0.22f * num2;
			vector3KeyFrameAnimation2.InsertKeyFrame(num, new Vector3(num3, num3, 1f), easingFunction);
		}
		(KeyFrameAnimation, string)[] array = new(KeyFrameAnimation, string)[3]
		{
			(scalarKeyFrameAnimation, "Opacity"),
			(vector3KeyFrameAnimation, "Translation"),
			(vector3KeyFrameAnimation2, "Scale")
		};
		for (int j = 0; j < array.Length; j++)
		{
			(KeyFrameAnimation, string) tuple = array[j];
			KeyFrameAnimation item = tuple.Item1;
			string item2 = tuple.Item2;
			item.Duration = TimeSpan.FromSeconds(0.58);
			item.Target = item2;
			d.StartAnimation(item);
		}
	}

	private static double CubicBezier(double x1, double y1, double x2, double y2, double x)
	{
		double num = 0.0;
		double num2 = 1.0;
		double num3 = x;
		for (int i = 0; i < 40; i++)
		{
			num3 = (num + num2) / 2.0;
			if (B(x1, x2, num3) < x)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return B(y1, y2, num3);
		static double B(double a, double b, double t)
		{
			return 3.0 * a * (1.0 - t) * (1.0 - t) * t + 3.0 * b * (1.0 - t) * t * t + t * t * t;
		}
	}

	internal void PickColor(string name, Color color)
	{
		pickedColor = color;
		PreSignInIntroViewModel vm = Vm;
		if (vm != null)
		{
			vm.PickCursorColorAsync(DS.ToHex(color));
		}
		else
		{
			s.Settings.CursorColorHex = DS.ToHex(color);
		}
		s.Cursor.SetColor(DS.ToHex(color));
		TrackButton("pick_color", name.ToLowerInvariant());
		After(0.12, delegate
		{
			Go(Step.Hatched);
		});
	}

	private FrameworkElement HatchGlyph()
	{
		Grid grid = new Grid
		{
			Width = 96.0,
			Height = 96.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hatchGlow = new Grid
		{
			Width = 88.0,
			Height = 88.0,
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		hatchGlow.Children.Add(new ShadowLayer(44.0, Color.FromArgb((byte)Math.Round(140.25), pickedColor.R, pickedColor.G, pickedColor.B), 26.0, 0.0));
		grid.Children.Add(hatchGlow);
		Grid grid2 = new Grid
		{
			Width = 44.0,
			Height = 44.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.0
		};
		Microsoft.UI.Xaml.Shapes.Path path = IntroMark.Create(44.0, pickedColor);
		grid2.Children.Add(ShapeGlow(path, pickedColor, 0.35, 32.0));
		grid2.Children.Add(ShapeGlow(path, pickedColor, 0.55, 16.0));
		grid2.Children.Add(path);
		grid2.CenterPoint = new Vector3(22f, 22f, 0f);
		grid2.Scale = new Vector3(0.3f, 0.3f, 1f);
		grid.Children.Add(grid2);
		hatchMark = grid2;
		hatchGlyphHost = grid;
		return grid;
	}

	private void RunHatchTimeline()
	{
		Sounds.Stop("hatching");
		Sounds.Play("hatching", 0.85);
		if ((object)hatchMark == null || (object)hatchGlow == null)
		{
			return;
		}
		Compositor c = Ui.Compositor;
		if (Anim.ReduceMotion)
		{
			Log.Info("intro", "hatch: reduced motion (a 0.6 s fade)");
			hatchMark.Scale = Vector3.One;
			hatchMark.Rotation = 0f;
			Anim.ReducedMotionFade(hatchGlow, 1f, 0.6);
			Anim.ReducedMotionFade(hatchMark, 1f, 0.6);
			After(0.7, delegate
			{
				if ((object)titleText != null)
				{
					Anim.ReducedMotionFade(titleText, 1f, 0.18);
				}
			});
			After(0.73, delegate
			{
				if ((object)subtitleText != null)
				{
					Anim.ReducedMotionFade(subtitleText, 1f, 0.18);
				}
			});
			After(0.76, delegate
			{
				if ((object)footerHost != null)
				{
					Anim.ReducedMotionFade(footerHost, 1f, 0.18);
				}
			});
			return;
		}
		CubicBezierEasingFunction inOut = Easing.EaseInOut(c);
		Anim.Opacity(hatchGlow, 1f, 0.55, Easing.EaseOut(c));
		After(0.6, delegate
		{
			Anim.Opacity(hatchMark, 1f, 0.3, Easing.EaseOut(c));
		});
		Pose(0.6, 0.5f, 14f, 0.35, CompositionEasingFunctionKind.InOut);
		Pose(0.95, 0.34f, -12f, 0.35, CompositionEasingFunctionKind.InOut);
		Pose(1.3, 0.55f, 16f, 0.35, CompositionEasingFunctionKind.InOut);
		Pose(1.65, 0.32f, -10f, 0.35, CompositionEasingFunctionKind.InOut);
		Pose(2.0, 0.22f, -6f, 0.4, CompositionEasingFunctionKind.In);
		After(2.4, delegate
		{
			Anim.ScaleSpring(hatchMark, 1f, new SwiftSpring(0.5, 0.55), null);
			Anim.Rotate(hatchMark, 0f, 0.5, Easing.EaseOut(c));
		});
		CubicBezierEasingFunction reveal = Easing.Expressive(c);
		After(3.1, delegate
		{
			if ((object)titleText != null)
			{
				Anim.Opacity(titleText, 1f, 0.4, reveal);
			}
		});
		After(3.16, delegate
		{
			if ((object)subtitleText != null)
			{
				Anim.Opacity(subtitleText, 1f, 0.4, reveal);
			}
		});
		After(3.22, delegate
		{
			if ((object)footerHost != null)
			{
				Anim.Opacity(footerHost, 1f, 0.4, reveal);
			}
		});
		void Pose(double at, float scale, float deg, double dur, CompositionEasingFunctionKind kind)
		{
			After(at, delegate
			{
				CubicBezierEasingFunction easing = ((kind == CompositionEasingFunctionKind.In) ? c.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), Vector2.One) : inOut);
				Anim.Scale(hatchMark, scale, dur, easing, null);
				Anim.Rotate(hatchMark, deg, dur, easing);
			});
		}
	}

	public void SettleHatched()
	{
		if ((object)hatchMark != null && (object)hatchGlow != null)
		{
			hatchGlow.Opacity = 1.0;
			hatchMark.Opacity = 1.0;
			hatchMark.Scale = Vector3.One;
			hatchMark.Rotation = 0f;
			if ((object)titleText != null)
			{
				titleText.Opacity = 1.0;
			}
			if ((object)subtitleText != null)
			{
				subtitleText.Opacity = 1.0;
			}
			if ((object)footerHost != null)
			{
				footerHost.Opacity = 1.0;
			}
		}
	}

	internal void GiveHome()
	{
		TrackButton("give_clicky_a_home");
		Sounds.Play("clicky-question", 0.95);
		if (Anim.ReduceMotion)
		{
			Anim.Opacity(window.Root, 0f, 1.0, Easing.EaseOut(Ui.Compositor));
			After(0.25, delegate
			{
				this.ReducedMotionHandOff?.Invoke();
			});
			return;
		}
		if ((object)hatchMark != null)
		{
			Anim.KeyframesScalar(hatchMark, "Rotation", new(float, double)[4]
			{
				(0f, 0.04),
				(10f, 0.08),
				(-7f, 0.08),
				(0f, 0.1)
			});
			if (!Anim.Instant)
			{
				Compositor compositor = Ui.Compositor;
				Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
				double num = 0.3;
				double num2 = 0.0;
				(float, double)[] array = new(float, double)[4]
				{
					(1f, 0.04),
					(1.18f, 0.1),
					(0.94f, 0.08),
					(1f, 0.08)
				};
				for (int i = 0; i < array.Length; i++)
				{
					(float, double) tuple = array[i];
					float item = tuple.Item1;
					double item2 = tuple.Item2;
					num2 += item2;
					vector3KeyFrameAnimation.InsertKeyFrame((float)(num2 / num), new Vector3(item, item, 1f), Easing.EaseInOut(compositor));
				}
				vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(num);
				vector3KeyFrameAnimation.Target = "Scale";
				hatchMark.StartAnimation(vector3KeyFrameAnimation);
			}
		}
		Anim.Opacity(window.Root, 0f, 1.0, Easing.EaseOut(Ui.Compositor), 0.5);
		After(0.32, delegate
		{
			if ((object)hatchGlyphHost != null)
			{
				Point point = hatchGlyphHost.TransformToVisual(null).TransformPoint(new Point(48f, 48f));
				double scale = window.Scale;
				Native.GetWindowRectPublic(window.Hwnd, out var rc);
				this.FlightRequested?.Invoke(new Point2((double)rc.Left + point.X * scale, (double)rc.Top + point.Y * scale));
			}
			if ((object)hatchMark != null)
			{
				Anim.Opacity(hatchMark, 0f, 0.18);
			}
		});
	}

	public void HandOffComplete()
	{
		window.HideWindow();
		window.Root.Opacity = 1.0;
		if (Vm == null)
		{
			s.Settings.HasCompletedIntro = true;
		}
		this.Finished?.Invoke();
	}

	private void After(double seconds, Action a)
	{
		Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(seconds);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			a();
		};
		dispatcherQueueTimer.Start();
	}
}
