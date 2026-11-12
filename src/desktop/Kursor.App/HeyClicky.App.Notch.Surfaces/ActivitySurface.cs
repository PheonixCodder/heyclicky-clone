using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Portraits;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class ActivitySurface : NotchSurface
{
	private readonly Grid root;

	private readonly ContentControl labelSlot = new ContentControl
	{
		VerticalAlignment = VerticalAlignment.Center,
		IsTabStop = false
	};

	private readonly ContentControl indicatorSlot = new ContentControl
	{
		VerticalAlignment = VerticalAlignment.Center,
		HorizontalAlignment = HorizontalAlignment.Right,
		IsTabStop = false
	};

	private ActivityPhase phase;

	private string label = "";

	private bool recording;

	private CompositionPropertySet? listeningClock;

	private DispatcherQueueTimer? levelTimer;

	private readonly Func<Color> cursorColor;

	private readonly bool isCompact;

	private AppServices? pillServices;

	private Grid? faceHost;

	private Grid? titleHost;

	private TextBlock? currentTitle;

	public override FrameworkElement View => root;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	private bool AgentDriving
	{
		get
		{
			if (base.Host != null)
			{
				return AgentColors.RunningTasks(base.Host.Services.Agents).Count > 0;
			}
			return false;
		}
	}

	public ActivitySurface(ActivityPhase phase, string label, bool recording, Func<Color> cursorColor, double? compactGap = null)
	{
		this.cursorColor = cursorColor;
		isCompact = compactGap.HasValue;
		root = new Grid
		{
			Margin = new Thickness(isCompact ? 12 : 14, 0.0, isCompact ? 12 : 14, 0.0),
			ColumnSpacing = (compactGap ?? 8.0)
		};
		root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		root.ColumnDefinitions.Add(isCompact ? new ColumnDefinition
		{
			Width = new GridLength(0.0)
		} : new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MinWidth = 6.0
		});
		root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		if (isCompact)
		{
			root.ColumnDefinitions.RemoveAt(1);
			Grid.SetColumn(indicatorSlot, 1);
			root.HorizontalAlignment = HorizontalAlignment.Left;
		}
		else
		{
			Grid.SetColumn(indicatorSlot, 2);
		}
		root.Children.Add(labelSlot);
		root.Children.Add(indicatorSlot);
		Set(phase, label, recording);
	}

	public void AttachAsPill(AppServices services)
	{
		pillServices = services;
		StartLevelPump();
	}

	public void DetachPill()
	{
		levelTimer?.Stop();
	}

	public bool Matches(ActivityPhase phase, string label)
	{
		if (this.phase == phase)
		{
			if (phase == ActivityPhase.Custom)
			{
				return this.label == label;
			}
			return true;
		}
		return false;
	}

	public static string DisplayLabel(ActivityPhase p, string custom)
	{
		return p switch
		{
			ActivityPhase.Listening => "Listening", 
			ActivityPhase.Thinking => "Thinking", 
			ActivityPhase.Speaking => "Speaking", 
			ActivityPhase.AgentMulti => "Agents", 
			ActivityPhase.Docked => "HeyClicky is docked", 
			_ => custom, 
		};
	}

	public static (ActivityPhase, string, bool)? Classify(NotchLiveEvent e)
	{
		if (!(e is NotchLiveEvent.ActivityListening))
		{
			if (!(e is NotchLiveEvent.ActivityThinking))
			{
				if (!(e is NotchLiveEvent.ActivitySpeaking))
				{
					if (!(e is NotchLiveEvent.ActivityCustomStatus activityCustomStatus))
					{
						if (!(e is NotchLiveEvent.AgentMultiActivity agentMultiActivity))
						{
							if (!(e is NotchLiveEvent.IntroHatchPeek))
							{
								if (e is NotchLiveEvent.Dictation dictation)
								{
									return (ActivityPhase.Dictation, dictation.Label, dictation.IsRecording);
								}
								return null;
							}
							return (ActivityPhase.Docked, "", false);
						}
						return (ActivityPhase.AgentMulti, agentMultiActivity.LongestLabel, false);
					}
					return (ActivityPhase.Custom, activityCustomStatus.Label, false);
				}
				return (ActivityPhase.Speaking, "", false);
			}
			return (ActivityPhase.Thinking, "", false);
		}
		return (ActivityPhase.Listening, "", false);
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		if (NotchGeometry.Shown(p) is NotchPresentation.LiveEvent liveEvent)
		{
			NotchLiveEvent @event = liveEvent.Event;
			return Classify(@event).HasValue;
		}
		return false;
	}

	public override void Update(NotchPresentation p)
	{
		if (NotchGeometry.Shown(p) is NotchPresentation.LiveEvent liveEvent)
		{
			NotchLiveEvent @event = liveEvent.Event;
			(ActivityPhase, string, bool)? tuple = Classify(@event);
			if (tuple.HasValue)
			{
				var (newPhase, newLabel, newRecording) = tuple.GetValueOrDefault();
				Set(newPhase, newLabel, newRecording);
			}
		}
	}

	protected override void OnAttached()
	{
		if (phase == ActivityPhase.AgentMulti)
		{
			labelSlot.Content = AgentMultiLabel();
			indicatorSlot.Content = AgentMultiDots();
		}
		else
		{
			ActivityPhase activityPhase = phase;
			bool flag = ((activityPhase == ActivityPhase.Thinking || activityPhase == ActivityPhase.Custom) ? true : false);
			if (flag && AgentDriving)
			{
				if (phase == ActivityPhase.Custom)
				{
					labelSlot.Content = AgentCustomLabel();
				}
				indicatorSlot.Content = ActivityIndicators.ThinkingDots(ThinkingTint());
			}
		}
		base.Host.MultiAgentRotated += OnMultiAgentRotated;
		base.Host.BindToEdges(labelSlot, -1.0, 0.5);
		base.Host.BindToEdges(indicatorSlot, 1.0, 0.5);
		StartLevelPump();
	}

	public override void Detached()
	{
		levelTimer?.Stop();
		base.Host.MultiAgentRotated -= OnMultiAgentRotated;
	}

	private void Set(ActivityPhase newPhase, string newLabel, bool newRecording)
	{
		bool flag = newPhase != phase || labelSlot.Content == null;
		bool num = flag || newRecording != recording;
		phase = newPhase;
		label = newLabel;
		recording = newRecording;
		if (phase == ActivityPhase.Docked)
		{
			labelSlot.Content = null;
		}
		else if (phase == ActivityPhase.AgentMulti)
		{
			labelSlot.Content = AgentMultiLabel();
		}
		else if (phase == ActivityPhase.Custom && AgentDriving)
		{
			labelSlot.Content = AgentCustomLabel();
		}
		else
		{
			string text = DisplayLabel(phase, (phase == ActivityPhase.Custom) ? ClampForStrip(label) : label);
			if (labelSlot.Content is TextBlock textBlock && !flag)
			{
				textBlock.Text = text;
			}
			else
			{
				labelSlot.Content = StripText(text);
			}
		}
		if (num)
		{
			listeningClock = null;
			ContentControl contentControl = indicatorSlot;
			FrameworkElement content;
			switch (phase)
			{
			case ActivityPhase.Listening:
				content = Listening();
				break;
			case ActivityPhase.Speaking:
				content = ActivityIndicators.SpeakingBars(DS.Colors.SpeakingAccent);
				break;
			case ActivityPhase.Docked:
				content = ActivityIndicators.DockedCursor(cursorColor());
				break;
			case ActivityPhase.Dictation:
				content = ActivityIndicators.DictationDashes(DS.Colors.DictationAccent, recording);
				break;
			case ActivityPhase.AgentMulti:
				if (base.Host != null)
				{
					content = AgentMultiDots();
					break;
				}
				goto default;
			default:
				content = ActivityIndicators.ThinkingDots(ThinkingTint());
				break;
			}
			contentControl.Content = content;
		}
		if (base.Host != null || pillServices != null)
		{
			StartLevelPump();
		}
	}

	private FrameworkElement Listening()
	{
		(FrameworkElement view, CompositionPropertySet clock) tuple = ActivityIndicators.ListeningBars(DS.Colors.ListeningAccent);
		FrameworkElement item = tuple.view;
		CompositionPropertySet item2 = tuple.clock;
		listeningClock = item2;
		return item;
	}

	private void StartLevelPump()
	{
		if (phase != 0)
		{
			levelTimer?.Stop();
			return;
		}
		if ((object)levelTimer == null)
		{
			levelTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			levelTimer.Interval = TimeSpan.FromMilliseconds(33L, 0L);
			levelTimer.Tick += delegate
			{
				listeningClock?.InsertScalar("Level", Math.Max(0.15f, Math.Min(1f, (pillServices ?? base.Host.Services).Microphone.Level)));
			};
		}
		levelTimer.Start();
	}

	private static TextBlock StripText(string text)
	{
		TextBlock textBlock = Ui.Semibold(text, 11.0, DS.Colors.TextPrimary);
		Typography.SetNumeralAlignment(textBlock, FontNumeralAlignment.Tabular);
		return textBlock;
	}

	private Color ThinkingTint()
	{
		return ((base.Host != null) ? AgentColors.FirstRunning(base.Host.Services.Agents) : ((Color?)null)) ?? DS.Colors.ThinkingAccent;
	}

	private FrameworkElement AgentCustomLabel()
	{
		PortraitView portraitView = ClickyFace.NotchBadge(AgentColors.FirstRunningTask(base.Host.Services.Agents)?.TaskId ?? "agent-0");
		if (base.Host.SizeContext.AgentLabelsHidden)
		{
			return portraitView;
		}
		TextBlock textBlock = StripText(ClampForStrip(label));
		textBlock.MaxWidth = 200.0;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			Children = 
			{
				(UIElement)portraitView,
				(UIElement)textBlock
			}
		};
	}

	private static string ClampForStrip(string s)
	{
		s = s.Trim();
		if (s.Length > 25)
		{
			return s.Substring(0, 24) + "…";
		}
		return s;
	}

	private AgentTaskSnapshot? DisplayedAgent()
	{
		if (base.Host != null)
		{
			return AgentColors.DisplayedRunningTask(base.Host.Services.Agents, base.Host.MultiAgentRotation, isMultiAgentPhase: true);
		}
		return null;
	}

	private FrameworkElement AgentMultiLabel()
	{
		AgentTaskSnapshot agentTaskSnapshot = DisplayedAgent();
		faceHost = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center
		};
		faceHost.Children.Add(ClickyFace.NotchBadge(agentTaskSnapshot?.TaskId ?? "agent-0"));
		titleHost = null;
		currentTitle = null;
		INotchHost host = base.Host;
		if (host == null || host.SizeContext.AgentLabelsHidden)
		{
			return faceHost;
		}
		titleHost = new Grid
		{
			Height = 14.0,
			MaxWidth = 200.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		Ui.ClipRounded(titleHost, 0.0);
		currentTitle = TitleText(agentTaskSnapshot?.Title ?? "Agent working");
		titleHost.Children.Add(currentTitle);
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			Children = 
			{
				(UIElement)faceHost,
				(UIElement)titleHost
			}
		};
	}

	private static TextBlock TitleText(string s)
	{
		return StripText(ClampForStrip(s));
	}

	private FrameworkElement AgentMultiDots()
	{
		AgentTaskSnapshot agentTaskSnapshot = DisplayedAgent();
		return ActivityIndicators.ThinkingDots(((object)agentTaskSnapshot != null) ? AgentColors.For(agentTaskSnapshot.TaskId, 0) : DS.Colors.ThinkingAccent);
	}

	private void OnMultiAgentRotated()
	{
		if (phase != ActivityPhase.AgentMulti)
		{
			return;
		}
		AgentTaskSnapshot agentTaskSnapshot = DisplayedAgent();
		if ((object)agentTaskSnapshot == null)
		{
			return;
		}
		CubicBezierEasingFunction easing = Easing.Expressive(Ui.Compositor);
		if ((object)titleHost != null && (object)currentTitle != null)
		{
			TextBlock textBlock = TitleText(agentTaskSnapshot.Title);
			TextBlock textBlock2 = currentTitle;
			textBlock.Translation = new Vector3(0f, 14f, 0f);
			textBlock.Opacity = 0.0;
			titleHost.Children.Add(textBlock);
			Anim.Translate(textBlock, Vector3.Zero, 0.34, easing);
			Anim.Opacity(textBlock, 1f, 0.34, easing);
			Anim.Translate(textBlock2, new Vector3(0f, -14f, 0f), 0.34, easing);
			Anim.Opacity(textBlock2, 0f, 0.34, easing);
			currentTitle = textBlock;
			RemoveAfterFade(titleHost, textBlock2);
		}
		if ((object)faceHost != null)
		{
			PortraitView portraitView = ClickyFace.NotchBadge(agentTaskSnapshot.TaskId);
			portraitView.Opacity = 0.0;
			foreach (UIElement item in faceHost.Children.ToList())
			{
				Anim.Opacity(item, 0f, 0.34, easing);
				RemoveAfterFade(faceHost, item);
			}
			faceHost.Children.Add(portraitView);
			Anim.Opacity(portraitView, 1f, 0.34, easing);
		}
		FrameworkElement frameworkElement = AgentMultiDots();
		frameworkElement.Opacity = 0.0;
		indicatorSlot.Content = frameworkElement;
		Anim.Opacity(frameworkElement, 1f, 0.34, easing);
	}

	private static void RemoveAfterFade(Panel host, UIElement element)
	{
		DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.4);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			host.Children.Remove(element);
		};
		dispatcherQueueTimer.Start();
	}
}
