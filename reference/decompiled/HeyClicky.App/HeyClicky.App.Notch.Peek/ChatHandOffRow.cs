using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ChatHandOffRow : StackPanel
{
	private sealed class StartMark : Grid
	{
		private readonly ChatHandOff entry;

		private readonly IChatLiveWork live;

		private readonly int seed;

		private bool? showsPencil;

		private bool listening;

		public StartMark(ChatHandOff entry, IChatLiveWork live, int seed)
		{
			StartMark startMark = this;
			this.entry = entry;
			this.live = live;
			this.seed = seed;
			base.VerticalAlignment = VerticalAlignment.Center;
			Update();
			base.Loaded += delegate
			{
				if (!startMark.listening)
				{
					startMark.listening = true;
					live.Changed += startMark.OnWorkChanged;
				}
				startMark.Update();
			};
			base.Unloaded += delegate
			{
				if (startMark.listening && !startMark.IsLoaded)
				{
					startMark.listening = false;
					live.Changed -= startMark.OnWorkChanged;
				}
			};
		}

		private void OnWorkChanged()
		{
			if (base.DispatcherQueue.HasThreadAccess)
			{
				Update();
			}
			else
			{
				base.DispatcherQueue.TryEnqueue(Update);
			}
		}

		private void Update()
		{
			bool flag = live.IsWorking(entry);
			if (showsPencil != flag)
			{
				showsPencil = flag;
				base.Children.Clear();
				if (flag)
				{
					base.Children.Add(BusyPencil());
				}
				base.Visibility = ((base.Children.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	private static readonly HashSet<string> foldedEntryIds = new HashSet<string>();

	public ChatHandOffRow(INotchHost host, ChatHandOff entry, IReadOnlyList<ChatHandOff> allEntries)
	{
		base.Spacing = 6.0;
		IChatLiveWork liveWork = NotchExtensions.LiveWork;
		bool flag = allEntries.LastOrDefault((ChatHandOff e) => e.TargetKey == entry.TargetKey)?.Id == entry.Id;
		bool flag2 = entry.Kind == ChatHandOffKind.Started && flag;
		string text = liveWork?.DisplayName(entry) ?? entry.Title;
		FrameworkElement item = Header(entry, text, flag2, liveWork);
		IHeyClickyWindowHost window = host as IHeyClickyWindowHost;
		bool flag3 = window != null;
		if (flag3)
		{
			ChatHandOffKind kind = entry.Kind;
			flag3 = (uint)kind <= 1u;
		}
		if (flag3)
		{
			Pressable obj = new Pressable
			{
				Background = Ui.Transparent,
				Children = { (UIElement)item }
			};
			obj.Tip("Open " + text + "'s chat");
			obj.Click += delegate
			{
				NotchPeekSurface.PeekAction("window_chat_event_opened");
				window.OpenAgentForEntry(entry.Id);
			};
			item = obj;
		}
		base.Children.Add(item);
		if (flag2)
		{
			FrameworkElement frameworkElement = liveWork?.WorkCard(entry);
			if ((object)frameworkElement != null)
			{
				frameworkElement.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
				base.Children.Add(frameworkElement);
			}
		}
		if (entry.Kind != ChatHandOffKind.CameBack)
		{
			return;
		}
		int count = base.Children.Count;
		FrameworkElement reply = null;
		FrameworkElement nextStep = ((!flag || entry.Outcome != ChatHandOffOutcome.Finished) ? null : liveWork?.NextStepButton(entry));
		string fullReply = entry.FullReply;
		if (fullReply != null && fullReply.Length > 0)
		{
			base.Children.Add(reply = ReplyBlock(host, entry.Id, fullReply, nextStep));
		}
		else
		{
			string replyText = entry.ReplyText;
			if (replyText != null && replyText.Length > 0)
			{
				FrameworkElement frameworkElement2 = ChatBubbles.Bubble(replyText, outgoing: false, tail: true);
				frameworkElement2.MaxWidth = 420.0;
				frameworkElement2.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
				base.Children.Add(reply = frameworkElement2);
			}
		}
		IReadOnlyList<string> sitesRead = entry.SitesRead;
		if (sitesRead != null && sitesRead.Count > 0)
		{
			base.Children.Add(new ChatSitesReadChip(sitesRead)
			{
				Margin = new Thickness(30.0, 0.0, 0.0, 0.0)
			});
		}
		IReadOnlyList<string> artifactEntries = entry.ArtifactEntries;
		if (artifactEntries != null && artifactEntries.Count > 0)
		{
			FrameworkElement frameworkElement3 = liveWork?.FileCards(artifactEntries, Open) ?? FilePills(artifactEntries, Open);
			frameworkElement3.Margin = new Thickness(30.0, 4.0, 0.0, 0.0);
			base.Children.Add(frameworkElement3);
		}
		ChatHandOffOutcome? outcome = entry.Outcome;
		int num;
		if (outcome.HasValue)
		{
			ChatHandOffOutcome valueOrDefault = outcome.GetValueOrDefault();
			num = ((valueOrDefault != ChatHandOffOutcome.Finished) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			FrameworkElement frameworkElement4 = liveWork?.WorkCard(entry);
			if ((object)frameworkElement4 != null)
			{
				frameworkElement4.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
				base.Children.Add(frameworkElement4);
			}
		}
		StartFoldIfFresh(entry, liveWork, count, reply);
		void Open(string artifact)
		{
			NotchPeekSurface.PeekAction("chat_artifact_opened", NotchChatAnalytics.ChatArtifactOpened("chat", artifact));
			(host.Actions as INotchActions)?.OpenChatArtifact(artifact, entry.ThreadId, entry.ClickySlug);
		}
	}

	private void StartFoldIfFresh(ChatHandOff entry, IChatLiveWork? live, int replyStart, FrameworkElement? reply)
	{
		ChatHandOffOutcome? outcome = entry.Outcome;
		bool flag = ((!outcome.HasValue || outcome.GetValueOrDefault() == ChatHandOffOutcome.Finished) ? true : false);
		if (!flag || Anim.ReduceMotion || live == null || DateTimeOffset.UtcNow - entry.CreatedAt >= TimeSpan.FromSeconds(6L) || foldedEntryIds.Contains(entry.Id))
		{
			return;
		}
		List<UIElement> replyPart = base.Children.Skip(replyStart).ToList();
		FrameworkElement fold = null;
		fold = live.PlanFold(entry, delegate
		{
			base.Children.Remove(fold);
			foreach (UIElement item in replyPart)
			{
				item.Visibility = Visibility.Visible;
			}
			if ((object)reply != null)
			{
				SwiftSpring spring = new SwiftSpring(0.42, 0.86);
				reply.Opacity = 0.0;
				reply.Translation = new Vector3(0f, 10f, 0f);
				Anim.OpacitySpring(reply, 1f, spring);
				Anim.TranslationSpring(reply, Vector3.Zero, spring);
			}
		});
		if ((object)fold == null)
		{
			return;
		}
		foldedEntryIds.Add(entry.Id);
		fold.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
		foreach (UIElement item2 in replyPart)
		{
			item2.Visibility = Visibility.Collapsed;
		}
		base.Children.Insert(replyStart, fold);
	}

	private static FrameworkElement Header(ChatHandOff entry, string displayName, bool isNewestStart, IChatLiveWork? live)
	{
		MessagesPalette current = MessagesPalette.Current;
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		FrameworkElement item = live?.Chip(entry, 22.0) ?? PlainChip(entry);
		grid.Children.Add(item);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock text = new TextBlock
		{
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			FontFamily = DS.Fonts.Small,
			IsTextScaleFactorEnabled = false,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.NoWrap,
			VerticalAlignment = VerticalAlignment.Center
		};
		Color primaryText = current.PrimaryText;
		ChatHandOffKind kind = entry.Kind;
		if ((uint)kind <= 1u)
		{
			Part(displayName, primaryText, bold: true);
		}
		else
		{
			Part(entry.Title, primaryText);
		}
		stackPanel.Children.Add(text);
		int seed = ChatStamp.Seed(entry.Id);
		FrameworkElement frameworkElement = entry.Kind switch
		{
			ChatHandOffKind.Started => StartStamp(entry, isNewestStart, live, seed), 
			ChatHandOffKind.CameBack => CameBackMark(entry, seed), 
			_ => null, 
		};
		if ((object)frameworkElement != null)
		{
			stackPanel.Children.Add(frameworkElement);
		}
		stackPanel.Children.Add(new TextBlock
		{
			Text = ChatBubbles.ShortTime(entry.CreatedAt),
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			FontFamily = DS.Fonts.Small,
			IsTextScaleFactorEnabled = false,
			Foreground = Ui.Brush(current.TertiaryText),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		return grid;
		void Part(string s, Color c, bool bold = false)
		{
			text.Inlines.Add(new Run
			{
				Text = s,
				Foreground = Ui.Brush(c),
				FontWeight = (bold ? FontWeights.Bold : FontWeights.Medium)
			});
		}
	}

	private static FrameworkElement StartStamp(ChatHandOff entry, bool isNewestStart, IChatLiveWork? live, int seed)
	{
		FrameworkElement frameworkElement = ChatStamp.Create(ChatStamp.Status.Working, entry.IsFollowUp ? "sent" : "started", seed);
		if (!isNewestStart || live == null)
		{
			return frameworkElement;
		}
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)frameworkElement,
				(UIElement)new StartMark(entry, live, seed)
			}
		};
	}

	private static FrameworkElement CameBackMark(ChatHandOff entry, int seed)
	{
		MessagesPalette current = MessagesPalette.Current;
		FrameworkElement frameworkElement = entry.Outcome.GetValueOrDefault() switch
		{
			ChatHandOffOutcome.Finished => ChatStamp.Create(ChatStamp.Status.Ready, "done", seed), 
			ChatHandOffOutcome.Failed => ChatStamp.Create(ChatStamp.Status.NeedsYou, "oops", seed), 
			ChatHandOffOutcome.Stopped => ChatStamp.Create(ChatStamp.Status.Paused, "stopped", seed), 
			_ => ChatStamp.Create(ChatStamp.Status.NeedsYou, "you", seed), 
		};
		if (entry.IsRoutineRun != true)
		{
			return frameworkElement;
		}
		StackPanel obj = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock textBlock = Icons.Glyph("\ue72c", 10.0, current.TertiaryText);
		ToolTipService.SetToolTip(textBlock, "A routine run");
		obj.Children.Add(textBlock);
		obj.Children.Add(frameworkElement);
		return obj;
	}

	private static FrameworkElement BusyPencil()
	{
		TextBlock pencil = Icons.Glyph("\ue70f", 11.0, ChatStamp.OnBlack(ChatStamp.Status.Working));
		pencil.FontWeight = FontWeights.SemiBold;
		pencil.Loaded += delegate
		{
			if (!Anim.ReduceMotion)
			{
				Anim.LoopScalar(pencil, "Opacity", 1.2, (double x) => (float)(0.45 + 0.55 * Math.Abs(Math.Cos(Math.PI * x / 1.2))), 24);
			}
		};
		ToolTipService.SetToolTip(pencil, "Working on it");
		return pencil;
	}

	private static FrameworkElement PlainChip(ChatHandOff entry)
	{
		MessagesPalette current = MessagesPalette.Current;
		Border obj = new Border
		{
			Width = 22.0,
			Height = 22.0,
			CornerRadius = new CornerRadius(6.6),
			Background = Ui.Brush(current.AvatarPlate),
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock textBlock = Icons.Glyph((entry.JobId != null) ? "\ue821" : "\ue77b", 10.0, current.SecondaryText);
		textBlock.FontWeight = FontWeights.SemiBold;
		obj.Child = textBlock;
		return obj;
	}

	private static FrameworkElement ReplyBlock(INotchHost host, string entryId, string markdown, FrameworkElement? nextStep = null)
	{
		(string, bool) tuple = ChatReplyPreview.Parts(markdown);
		(string, bool) tuple2 = ChatReplyPreview.Preview(ChatReplyPreview.BubbleText(ChatReplyPreview.WithoutCitations(tuple.Item1 ?? markdown)));
		StackPanel obj = new StackPanel
		{
			Spacing = 4.0,
			MaxWidth = 420.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(30.0, 0.0, 0.0, 0.0),
			Children = { (UIElement)ChatBubbles.Bubble(tuple2.Item1, outgoing: false, tail: true) }
		};
		bool hadMore = tuple2.Item2 || (tuple.Item1 != null && tuple.Item2);
		GelButton item = ArcButtons.Gel("Full report", "doc.text", GelAppearance.SecondaryWhite, 30.0, 13.0, GelButton.Sizing.Normal, delegate
		{
			NotchPeekSurface.PeekAction("chat_reply_show_more", new JsonObject((JsonNodeOptions?)null) { ["had_more"] = hadMore });
			if (host is IHeyClickyWindowHost heyClickyWindowHost)
			{
				heyClickyWindowHost.OpenAgentForEntry(entryId);
			}
			else if (host.Services.Presenter is NotchPresenter notchPresenter)
			{
				notchPresenter.Tabs.TrackFullReplyOpened(entryId, NotchChatFullReplyOpenSource.ShowMore);
				notchPresenter.Tabs.ExpandedReplyEntryId = entryId;
			}
		});
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		if ((object)nextStep != null)
		{
			stackPanel.Children.Add(nextStep);
		}
		stackPanel.Children.Add(item);
		obj.Children.Add(stackPanel);
		return obj;
	}

	public static FrameworkElement FilePills(IReadOnlyList<string> entries, Action<string> open)
	{
		WrapPanel wrapPanel = new WrapPanel
		{
			HorizontalSpacing = 6.0,
			VerticalSpacing = 6.0
		};
		foreach (string entry in entries.Take(8))
		{
			Uri result;
			string text = (ArtifactPaths.IsWebLink(entry) ? (Uri.TryCreate(entry, UriKind.Absolute, out result) ? result.Host : entry) : Path.GetFileName(entry.TrimEnd('/', '\\')));
			Pressable pressable = new Pressable
			{
				CornerRadius = new CornerRadius(9.0),
				Background = Ui.Brush(Colors.White),
				Padding = new Thickness(8.0, 5.0, 10.0, 5.0)
			};
			TextBlock textBlock = Icons.Glyph(ArtifactPaths.IsWebLink(entry) ? "\ue774" : "\ue8a5", 11.0, DS.Black(0.7));
			TextBlock textBlock2 = Ui.Text(text, 11.5, FontWeights.SemiBold, DS.Black(0.85), TextAlignment.Left, 1);
			textBlock2.MaxWidth = 220.0;
			textBlock2.TextTrimming = TextTrimming.CharacterEllipsis;
			pressable.Children.Add(Ui.H(6.0, textBlock, textBlock2));
			pressable.WithHoverScale(1.03f);
			pressable.Click += delegate
			{
				open(entry);
			};
			pressable.Tip(entry);
			wrapPanel.Children.Add(pressable);
		}
		return wrapPanel;
	}
}
