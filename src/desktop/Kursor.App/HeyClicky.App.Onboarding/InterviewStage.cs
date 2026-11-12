using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Portraits;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;

namespace HeyClicky.App.Onboarding;

public sealed class InterviewStage : Grid
{
	private enum Stage
	{
		Preparing,
		Idle,
		Listening,
		Thinking,
		Speaking
	}

	public const double StageWidth = 594.0;

	public const double StageHeight = 330.0;

	public const double CharacterSize = 195.0;

	private readonly AppServices s;

	private readonly InterviewController interview;

	private readonly Grid characterFrame = new Grid
	{
		IsHitTestVisible = false
	};

	private PortraitView? portrait;

	private PortraitAppearance? shownAppearance;

	private readonly InterviewThoughtBubbles thoughts = new InterviewThoughtBubbles(360.0, 216.0);

	private readonly Grid controls = new Grid
	{
		Width = 546.0,
		Height = 52.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Bottom,
		Margin = new Thickness(24.0, 0.0, 0.0, 0.0)
	};

	private readonly Grid cta = new Grid
	{
		Height = 52.0,
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private readonly StackPanel ctaContent = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 8.0,
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Grid field = new Grid
	{
		Height = 52.0,
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private readonly Border fieldRim = new Border
	{
		CornerRadius = new CornerRadius(26.0),
		BorderThickness = new Thickness(1.0)
	};

	private readonly TextBox input;

	private readonly Pressable send = new Pressable
	{
		Width = 26.0,
		Height = 26.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Grid hands = new Grid
	{
		Width = 195.0,
		Height = 22.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Bottom,
		Margin = new Thickness(0.0, 0.0, 0.0, 42.0),
		IsHitTestVisible = false
	};

	private bool prefersVoice;

	private bool focused;

	private bool greeted;

	private bool refocusWhenEnabled;

	private string lastStageState = "";

	private readonly ContentControl focusSink = new ContentControl
	{
		Width = 0.0,
		Height = 0.0,
		IsTabStop = true,
		TabIndex = int.MaxValue,
		UseSystemFocusVisuals = false,
		IsHitTestVisible = false,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private bool fieldFocusRequested;

	private bool unwantedFocusArriving;

	private bool Completed
	{
		get
		{
			if (!interview.WasCompleted)
			{
				if (!interview.IsActive)
				{
					return interview.HasHeardFirstQuestion;
				}
				return false;
			}
			return true;
		}
	}

	private bool TypedActive
	{
		get
		{
			if (interview.IsUsersTurn)
			{
				return s.Companion.State != VoiceState.Listening;
			}
			return false;
		}
	}

	private bool EditorExpanded
	{
		get
		{
			if (!prefersVoice)
			{
				if (!focused)
				{
					return input.Text.Trim().Length > 0;
				}
				return true;
			}
			return false;
		}
	}

	private Stage StageState
	{
		get
		{
			if (!interview.IsActive && interview.HasHeardFirstQuestion)
			{
				return Stage.Idle;
			}
			switch (interview.VoiceStateForStage)
			{
			case VoiceState.Listening:
				return Stage.Listening;
			case VoiceState.Processing:
				return Stage.Thinking;
			case VoiceState.Responding:
				return Stage.Speaking;
			default:
				if (interview.IsWrappingUp || interview.IsPlanInFlight)
				{
					return Stage.Thinking;
				}
				if (!interview.HasHeardFirstQuestion)
				{
					return Stage.Preparing;
				}
				return Stage.Idle;
			}
		}
	}

	public InterviewStage(AppServices services, InterviewController interview)
	{
		InterviewStage interviewStage = this;
		s = services;
		this.interview = interview;
		base.Width = 594.0;
		base.Height = 330.0;
		base.HorizontalAlignment = HorizontalAlignment.Center;
		base.Children.Add(focusSink);
		base.Children.Add(characterFrame);
		thoughts.HorizontalAlignment = HorizontalAlignment.Left;
		thoughts.VerticalAlignment = VerticalAlignment.Bottom;
		thoughts.Margin = new Thickness(194.0, 0.0, 0.0, 112.0);
		base.Children.Add(thoughts);
		cta.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(28.5),
			Margin = new Thickness(-2.5),
			BorderBrush = Ui.Brush(DS.White(0.35)),
			BorderThickness = new Thickness(1.0),
			IsHitTestVisible = false
		});
		cta.Children.Add(new ShadowLayer(26.0, DS.Black(0.18), 9.0, 5.0));
		cta.Children.Add(new GelSurface(GelAppearance.PrimaryBlue, 26.0));
		cta.Children.Add(ctaContent);
		Pressable pressable = new Pressable
		{
			Children = { (UIElement)cta }
		};
		pressable.Click += delegate
		{
			interviewStage.prefersVoice = true;
			interviewStage.DropFieldFocus();
			interviewStage.Update();
		};
		AutomationProperties.SetHelpText(pressable, "Switch to voice mode, then hold the keyboard shortcut to answer. Release to send.");
		fieldRim.Background = Ui.Vertical((0.0, DS.Hex(2302755u, 0.95)), (1.0, DS.Hex(2302755u)));
		field.Children.Add(fieldRim);
		TextBlock textBlock = Icons.Glyph("\ue765", 12.0, DS.Colors.TextTertiary.WithAlpha(0.6));
		textBlock.FontWeight = FontWeights.SemiBold;
		input = new TextBox
		{
			PlaceholderText = "Or type here",
			FontSize = 13.5,
			Background = Ui.Transparent,
			BorderThickness = new Thickness(0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = Ui.Brush(DS.Hex(16053492u)),
			IsTextScaleFactorEnabled = false
		};
		input.Resources["TextControlBackgroundFocused"] = Ui.Transparent;
		input.Resources["TextControlBackgroundPointerOver"] = Ui.Transparent;
		input.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.0);
		input.Resources["TextControlBackgroundDisabled"] = Ui.Transparent;
		input.Resources["TextControlPlaceholderForeground"] = Ui.Brush(DS.Colors.TextTertiary.WithAlpha(0.7));
		input.KeyDown += delegate(object _, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				interviewStage.Submit();
			}
		};
		input.TextChanged += delegate
		{
			interviewStage.Update();
		};
		input.GettingFocus += delegate(UIElement _, GettingFocusEventArgs e)
		{
			if (!InterviewFieldFocusPolicy.Allows(e.FocusState == FocusState.Pointer || (e.InputDevice == FocusInputDeviceKind.Keyboard && e.Direction != FocusNavigationDirection.None), interviewStage.fieldFocusRequested))
			{
				bool flag = e.TryCancel() || e.TrySetNewFocusedElement(interviewStage.focusSink);
				if (!flag)
				{
					interviewStage.unwantedFocusArriving = true;
					interviewStage.DispatcherQueue.TryEnqueue(delegate
					{
						interviewStage.unwantedFocusArriving = false;
						interviewStage.DropFieldFocus();
					});
				}
				Log.Info("interview", $"typed field: a {e.FocusState} focus it didn't ask for refused ({e.InputDevice}, {e.Direction}, {(flag ? "cancelled" : "handed back")})");
			}
		};
		input.GotFocus += delegate
		{
			if (!interviewStage.unwantedFocusArriving)
			{
				interviewStage.focused = true;
				interviewStage.prefersVoice = false;
				interviewStage.Update();
			}
		};
		input.LostFocus += delegate
		{
			interviewStage.focused = false;
			interviewStage.Update();
		};
		send.Children.Add(new GelSurface(GelAppearance.PrimaryBlue, 13.0));
		TextBlock textBlock2 = Icons.Glyph("\ue74a", 11.0, Colors.Black);
		textBlock2.FontWeight = FontWeights.Bold;
		send.Children.Add(textBlock2);
		send.Click += Submit;
		AutomationProperties.SetName(send, "Send");
		Grid grid = new Grid
		{
			Margin = new Thickness(14.0, 6.0, 6.0, 6.0),
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(input, 1);
		Grid.SetColumn(send, 2);
		grid.Children.Add(textBlock);
		grid.Children.Add(input);
		grid.Children.Add(send);
		field.Children.Add(grid);
		field.Tapped += delegate(object _, TappedRoutedEventArgs e)
		{
			if (interviewStage.input.IsEnabled && (!interviewStage.send.IsHitTestVisible || !(e.OriginalSource is DependencyObject element) || !IsInside(element, interviewStage.send)))
			{
				interviewStage.prefersVoice = false;
				if (!interviewStage.focused)
				{
					interviewStage.FocusField();
				}
			}
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 16.0
		};
		stackPanel.Children.Add(pressable);
		stackPanel.Children.Add(field);
		controls.Children.Add(stackPanel);
		base.Children.Add(controls);
		base.Children.Add(hands);
		interview.Changed += Update;
		base.Unloaded += delegate
		{
			interview.Changed -= interviewStage.Update;
		};
		Update();
	}

	private void FocusField()
	{
		fieldFocusRequested = true;
		try
		{
			input.Focus(FocusState.Programmatic);
		}
		finally
		{
			fieldFocusRequested = false;
		}
	}

	private void DropFieldFocus()
	{
		focusSink.Focus(FocusState.Programmatic);
	}

	private static bool IsInside(DependencyObject element, DependencyObject ancestor)
	{
		DependencyObject dependencyObject = element;
		while ((object)dependencyObject != null)
		{
			if (dependencyObject == ancestor)
			{
				return true;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return false;
	}

	private void Submit()
	{
		string text = input.Text.Trim();
		if (text.Length != 0 && TypedActive)
		{
			input.Text = "";
			interview.SubmitTyped(text);
		}
	}

	public void Update()
	{
		EnsurePortrait();
		Stage stageState = StageState;
		if ((object)portrait != null)
		{
			PortraitView portraitView = portrait;
			portraitView.Expression = stageState switch
			{
				Stage.Preparing => PortraitExpression.Curious, 
				Stage.Listening => PortraitExpression.Listening, 
				Stage.Thinking => PortraitExpression.Thinking, 
				Stage.Speaking => PortraitExpression.Excited, 
				_ => PortraitExpression.Happy, 
			};
			if (stageState.ToString() != lastStageState)
			{
				lastStageState = stageState.ToString();
				var (to, y) = stageState switch
				{
					Stage.Listening => (1.06f, -6f), 
					Stage.Thinking => (0.97f, 3f), 
					Stage.Speaking => (1.03f, -3f), 
					_ => (1f, 0f), 
				};
				Anim.Scale(characterFrame, to, 0.3, Easing.EaseInOut(Ui.Compositor), null);
				Anim.Translate(characterFrame, new Vector3(0f, y, 0f), 0.3, Easing.EaseInOut(Ui.Compositor));
			}
			if (stageState == Stage.Speaking && !greeted && interview.IsActive)
			{
				greeted = true;
				portrait.Perform(PortraitReaction.Wave);
			}
		}
		UpdateBubbles(stageState);
		bool completed = Completed;
		controls.Visibility = (completed ? Visibility.Collapsed : Visibility.Visible);
		hands.Visibility = (completed ? Visibility.Collapsed : Visibility.Visible);
		bool editorExpanded = EditorExpanded;
		cta.Width = (editorExpanded ? 52 : 330);
		field.Width = (editorExpanded ? 478 : 200);
		bool typedActive = TypedActive;
		bool isEnabled = input.IsEnabled;
		if (!typedActive && isEnabled && focused)
		{
			refocusWhenEnabled = true;
		}
		input.IsEnabled = typedActive;
		if (typedActive && !isEnabled && refocusWhenEnabled)
		{
			refocusWhenEnabled = false;
			FocusField();
		}
		field.Opacity = (typedActive ? 1.0 : 0.62);
		if (!typedActive && focused)
		{
			DropFieldFocus();
		}
		fieldRim.BorderBrush = (focused ? ((Brush)Ui.Vertical((0.0, DS.Hex(15594751u)), (0.5, DS.Hex(10668031u)), (1.0, DS.Hex(10670839u)))) : ((Brush)Ui.Brush(DS.Hex(4276545u))));
		fieldRim.BorderThickness = new Thickness((!focused) ? 1 : 2);
		bool flag = input.Text.Trim().Length > 0;
		send.Opacity = (flag ? 1 : 0);
		send.IsHitTestVisible = flag;
		ctaContent.Children.Clear();
		(string, bool) tuple2 = Glyph(stageState);
		string item = tuple2.Item1;
		FrameworkElement item2 = (tuple2.Item2 ? ((FrameworkElement)Icons.PathIcon(item, 15.0, Colors.Black)) : ((FrameworkElement)Bold(Icons.Glyph(item, 13.0, Colors.Black))));
		ctaContent.Children.Add(item2);
		if (!editorExpanded)
		{
			ctaContent.Children.Add(Ui.Embossed(Ui.Semibold(Title(stageState), 14.0, Colors.Black)));
		}
	}

	private static TextBlock Bold(TextBlock t)
	{
		t.FontWeight = FontWeights.Bold;
		return t;
	}

	private string Title(Stage stage)
	{
		if (interview.HasHeardFirstQuestion && !interview.IsActive && !interview.IsPlanInFlight)
		{
			return "You're all set";
		}
		switch (stage)
		{
		case Stage.Listening:
			return "Listening… release to send";
		case Stage.Speaking:
			return "HeyClicky is speaking…";
		default:
			if (interview.IsPlanInFlight)
			{
				return "Setting up your space…";
			}
			if (interview.IsWrappingUp)
			{
				return "Got it…";
			}
			return stage switch
			{
				Stage.Thinking => "Thinking…", 
				Stage.Preparing => "HeyClicky is getting ready…", 
				_ => "Hold " + s.Settings.PushToTalkDisplay + " to answer", 
			};
		}
	}

	private (string, bool) Glyph(Stage stage)
	{
		switch (stage)
		{
		case Stage.Listening:
			return ("M3,10 h2 v4 h-2 z M7,7 h2 v10 h-2 z M11,3 h2 v18 h-2 z M15,6 h2 v12 h-2 z M19,9 h2 v6 h-2 z", true);
		case Stage.Speaking:
			return ("\ue767", false);
		default:
			if (interview.IsPlanInFlight)
			{
				return ("M10,3 C10.6,7.6 12.4,9.4 17,10 C12.4,10.6 10.6,12.4 10,17 C9.4,12.4 7.6,10.6 3,10 C7.6,9.4 9.4,7.6 10,3 Z M18,13 C18.3,15.2 18.8,15.7 21,16 C18.8,16.3 18.3,16.8 18,19 C17.7,16.8 17.2,16.3 15,16 C17.2,15.7 17.7,15.2 18,13 Z M17,2 C17.2,3.4 17.6,3.8 19,4 C17.6,4.2 17.2,4.6 17,6 C16.8,4.6 16.4,4.2 15,4 C16.4,3.8 16.8,3.4 17,2 Z", true);
			}
			if (interview.IsWrappingUp || (!interview.IsActive && interview.HasHeardFirstQuestion))
			{
				return ("\ue73e", false);
			}
			if ((stage == Stage.Preparing || stage == Stage.Thinking) ? true : false)
			{
				return ("\ue712", false);
			}
			return ("\ue720", false);
		}
	}

	private void EnsurePortrait()
	{
		PortraitAppearance appearance = interview.Appearance;
		if (!(shownAppearance == appearance))
		{
			shownAppearance = appearance;
			bool flag = appearance.Pack.IsCloud();
			double num = 195.0 * (flag ? 0.9 : 1.0);
			double num2 = 195.0 * (flag ? 0.74 : 1.0);
			double num3 = (flag ? (-52.0 + num2 * 0.22) : (-12.0));
			characterFrame.Children.Clear();
			characterFrame.Width = 195.0;
			characterFrame.Height = num2;
			characterFrame.HorizontalAlignment = HorizontalAlignment.Left;
			characterFrame.VerticalAlignment = VerticalAlignment.Bottom;
			characterFrame.Margin = new Thickness(0.0, 0.0, 0.0, 0.0 - num3);
			characterFrame.CenterPoint = new Vector3(97.5f, (float)num2 / 2f, 0f);
			portrait = new PortraitView(appearance, num, num, PortraitExpression.Curious, showsAvatarBackground: false, appearance.HairColor.Hair(), num * 0.08)
			{
				AnimatesWhileIdle = true,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0 - (num - num2), 0.0, 0.0 - (num - num2))
			};
			characterFrame.Children.Add(portrait);
			hands.Children.Clear();
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 56.0,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			for (int i = 0; i < 2; i++)
			{
				FrameworkElement frameworkElement = LedgeHand(appearance);
				frameworkElement.CenterPoint = new Vector3(14f, 11f, 0f);
				frameworkElement.Rotation = ((i == 0) ? (-8) : 8);
				stackPanel.Children.Add(frameworkElement);
			}
			hands.Children.Add(stackPanel);
		}
	}

	internal static FrameworkElement LedgeHand(PortraitAppearance a)
	{
		Grid grid = new Grid
		{
			Width = 28.0,
			Height = 22.0
		};
		if (a.Pack == PortraitPack.ClassicClouds)
		{
			Path path = new Path
			{
				Data = PortraitArt.LedgeHand(28.0, 22.0).ToXaml(),
				Fill = Ui.Brush(a.Hair)
			};
			WriteableBitmap writeableBitmap = PortraitWash.For(a, null);
			if ((object)writeableBitmap != null)
			{
				path.Fill = new ImageBrush
				{
					ImageSource = writeableBitmap,
					Stretch = Stretch.Fill
				};
			}
			grid.Children.Add(path);
		}
		else if (a.Pack.IsCloud())
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0f, 0f),
				EndPoint = new Point(1f, 1f)
			};
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.87),
				Offset = 0.0
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.55),
				Offset = 0.5
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.12),
				Offset = 1.0
			});
			grid.Children.Add(new Path
			{
				Data = PortraitArt.LedgeHand(28.0, 22.0).ToXaml(),
				Fill = linearGradientBrush,
				Stroke = Ui.Brush(PortraitData.Ink),
				StrokeThickness = 1.2
			});
		}
		else
		{
			grid.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(11.0),
				Background = Ui.Brush(a.Skin),
				BorderBrush = Ui.Brush(Colors.Black),
				BorderThickness = new Thickness(1.5)
			});
		}
		return grid;
	}

	private void UpdateBubbles(Stage stage)
	{
		string spokenText = interview.SpokenText;
		bool isLoading = spokenText.Length == 0 && interview.IsActive && (stage == Stage.Thinking || !interview.HasHeardFirstQuestion);
		thoughts.Show(spokenText, isLoading);
	}
}
