using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Proactive;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class TextInputSurface : NotchSurface
{
	private readonly ComposerBox composer;

	private readonly StackPanel view = new StackPanel
	{
		Spacing = 6.0,
		VerticalAlignment = VerticalAlignment.Top
	};

	private readonly Border followUpPill;

	private readonly TextBlock followUpTitle;

	private readonly Grid overlay;

	private readonly Grid readAloudSlot = new Grid
	{
		Width = 20.0,
		Height = 20.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Rectangle earDivider = new Rectangle
	{
		Width = 1.0,
		Height = 12.0,
		RadiusX = 0.5,
		RadiusY = 0.5,
		Fill = Ui.Brush(DS.White(0.14)),
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Func<nint> hwnd;

	private NotchAgentFollowUp? agentFollowUp;

	public override FrameworkElement View => view;

	public override FrameworkElement? Overlay => overlay;

	public override bool WantsKeyboard => true;

	internal NotchAgentFollowUp? AgentFollowUp => agentFollowUp;

	internal ComposerBox Composer => composer;

	private bool IsOnboardingTextBox
	{
		get
		{
			if ((object)agentFollowUp == null)
			{
				if (base.Host.Services.Presenter is NotchPresenter notchPresenter)
				{
					return !notchPresenter.RoutesTextModeToNotchChat;
				}
				return false;
			}
			return false;
		}
	}

	public static event Action? Typing;

	public TextInputSurface(NotchPresentation.TextInput p, bool signedIn, Func<nint> hwnd)
	{
		this.hwnd = hwnd;
		composer = new ComposerBox("ask HeyClicky…", signedIn)
		{
			WindowHandle = hwnd,
			VerticalAlignment = VerticalAlignment.Top
		};
		if (!string.IsNullOrEmpty(p.Draft))
		{
			composer.Input.Text = p.Draft;
		}
		(followUpPill, followUpTitle) = BuildFollowUpPill();
		view.Children.Add(followUpPill);
		view.Children.Add(composer);
		ApplyFollowUp(p.AgentFollowUp);
		overlay = BuildEar();
	}

	public override void FocusInput()
	{
		composer.FocusInput();
		composer.Input.SelectionStart = composer.Input.Text.Length;
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		return p is NotchPresentation.TextInput;
	}

	public override void Update(NotchPresentation p)
	{
		if (p is NotchPresentation.TextInput textInput)
		{
			ApplyFollowUp(textInput.AgentFollowUp);
			if (!string.IsNullOrEmpty(textInput.Draft) && textInput.Draft != composer.Input.Text)
			{
				composer.Input.Text = textInput.Draft;
			}
		}
	}

	private void ApplyFollowUp(NotchAgentFollowUp? followUp)
	{
		agentFollowUp = followUp;
		composer.SetPlaceholder(((object)followUp == null) ? "ask HeyClicky…" : "follow up with agent…");
		followUpPill.Visibility = (((object)followUp == null) ? Visibility.Collapsed : Visibility.Visible);
		if ((object)followUp != null)
		{
			followUpTitle.Text = followUp.Title;
			ToolTipService.SetToolTip(followUpPill, "Following up with " + followUp.Title);
		}
	}

	private static (Border Pill, TextBlock Title) BuildFollowUpPill()
	{
		TextBlock textBlock = Icons.Glyph("\ue89c", 8.5, DS.White(0.9));
		textBlock.FontWeight = FontWeights.Bold;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		TextBlock textBlock2 = Ui.Text("", 11.0, FontWeights.SemiBold, DS.White(0.9), TextAlignment.Left, 1);
		textBlock2.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		return (Pill: new Border
		{
			Height = 22.0,
			Padding = new Thickness(9.0, 0.0, 9.0, 0.0),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
			CornerRadius = new CornerRadius(11.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Ui.Brush(DS.White(0.08)),
			BorderBrush = Ui.Brush(DS.White(0.12)),
			BorderThickness = new Thickness(0.5),
			Tag = "agent-follow-up-pill",
			Child = PillRow(textBlock, textBlock2)
		}, Title: textBlock2);
	}

	private static Grid PillRow(UIElement arrow, TextBlock title)
	{
		Grid obj = new Grid
		{
			ColumnSpacing = 5.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				}
			}
		};
		Grid.SetColumn(title, 1);
		obj.Children.Add(arrow);
		obj.Children.Add(title);
		return obj;
	}

	protected override void OnAttached()
	{
		if (IsOnboardingTextBox)
		{
			readAloudSlot.Visibility = Visibility.Collapsed;
			earDivider.Visibility = Visibility.Collapsed;
			composer.RemoveAttachButton();
		}
		else if (readAloudSlot.Children.Count == 0)
		{
			readAloudSlot.Children.Add(new ReadAloudSwitch(base.Host.Services, 20.0, tooltipBelow: true));
		}
		base.Host.SizeContext.TextInputExtraLines = 0;
		base.Host.SizeContext.TextInputAttachmentCount = 0;
		composer.MetricsChanged += delegate(int extra, int attachments)
		{
			base.Host.SizeContext.TextInputExtraLines = extra;
			base.Host.SizeContext.TextInputAttachmentCount = attachments;
			base.Host.Relayout();
		};
		composer.Submitted += delegate(string text, IReadOnlyList<string> files)
		{
			ProactiveCoordinator? current = ProactiveCoordinator.Current;
			if (current != null && current.SubmitRemoteTaskModification(text))
			{
				base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
			}
			else
			{
				string text2 = agentFollowUp?.TaskId;
				if (text2 != null && base.Host.Actions is INotchCardActions notchCardActions)
				{
					base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
					notchCardActions.SendAgentFollowUp(text2, text, files);
				}
				else
				{
					if (base.Host.Services.Settings.LibraryModeEnabled)
					{
						base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
					}
					base.Host.Services.Companion.SubmitTextAsync(text, files);
				}
			}
		};
		composer.Cancelled += delegate
		{
			ProactiveCoordinator.Current?.CancelRemoteTaskModification();
			base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
		};
		AttachDropped(base.Host.TakePendingFiles());
		composer.Input.TextChanged += delegate
		{
			if (composer.Input.Text.Length > 0)
			{
				TextInputSurface.Typing?.Invoke();
			}
		};
	}

	public override bool AcceptFiles(IReadOnlyList<string> paths)
	{
		AttachDropped(paths);
		return true;
	}

	private void AttachDropped(IReadOnlyList<string> paths)
	{
		if (paths.Count != 0)
		{
			int num = composer.AddAttachments(paths);
			if (num > 0)
			{
				ClickyAnalytics.TrackNotchDroppedFilesAttached(num);
			}
		}
	}

	public override void SetDropTargeting(bool on)
	{
		composer.SetDropTargeting(on, DS.CursorColor(base.Host.Services.Settings.CursorColorHex));
	}

	public override void Detached()
	{
		base.Host.SizeContext.TextInputExtraLines = 0;
		base.Host.SizeContext.TextInputAttachmentCount = 0;
	}

	private Grid BuildEar()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 5.0, 26.0, 0.0),
			Height = 24.0
		};
		stackPanel.Children.Add(readAloudSlot);
		stackPanel.Children.Add(earDivider);
		Pressable pressable = new Pressable
		{
			Width = 24.0,
			Height = 24.0,
			VerticalAlignment = VerticalAlignment.Center
		}.Named("Close");
		pressable.Children.Add(new Ellipse
		{
			Width = 20.0,
			Height = 20.0,
			Fill = Ui.Brush(DS.White(0.1)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
		TextBlock textBlock = Icons.Glyph("\ue711", 8.5, DS.Colors.TextSecondary);
		textBlock.FontWeight = FontWeights.Bold;
		pressable.Children.Add(textBlock);
		pressable.Click += delegate
		{
			base.Host.Services.Presenter.Present(new NotchPresentation.Idle());
		};
		pressable.WithIconHover(24.0);
		stackPanel.Children.Add(pressable);
		return new Grid
		{
			IsHitTestVisible = true,
			Children = { (UIElement)stackPanel }
		};
	}
}
