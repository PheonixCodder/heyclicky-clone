using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.Documents;
using HeyClicky.App.Home;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Notch.Window;

internal sealed class HeyClickyWindowConversation : Grid
{
	[CompilerGenerated]
	private sealed class _003CFromJobRecord_003Ed__41 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private FrameworkElement _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public HeyClickyWindowConversation _003C_003E4__this;

		private ClickyJob _003Cjob_003E5__2;

		FrameworkElement IEnumerator<FrameworkElement>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CFromJobRecord_003Ed__41(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cjob_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			HeyClickyWindowConversation heyClickyWindowConversation = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				if (!(heyClickyWindowConversation.host.Services.Agents is AgentService agentService))
				{
					return false;
				}
				string text = ClickyJob.JobIdFromRailKey(heyClickyWindowConversation.agentKey);
				if (text != null)
				{
					_003Cjob_003E5__2 = agentService.Jobs.Job(text);
					if ((object)_003Cjob_003E5__2 != null)
					{
						_003C_003E2__current = SentBubble((_003Cjob_003E5__2.Prompt.Length > 0) ? _003Cjob_003E5__2.Prompt : _003Cjob_003E5__2.Title, _003Cjob_003E5__2.CreatedAt);
						_003C_003E1__state = 1;
						return true;
					}
				}
				return false;
			}
			case 1:
			{
				_003C_003E1__state = -1;
				string replyText = _003Cjob_003E5__2.ReplyText;
				if (replyText != null && replyText.Length > 0)
				{
					_003C_003E2__current = LeadingNineTenths(Markdown(replyText));
					_003C_003E1__state = 2;
					return true;
				}
				string summary = _003Cjob_003E5__2.Summary;
				if (summary != null && summary.Length > 0)
				{
					_003C_003E2__current = ChatBubbles.Bubble(summary, outgoing: false, tail: true);
					_003C_003E1__state = 3;
					return true;
				}
				break;
			}
			case 2:
				_003C_003E1__state = -1;
				break;
			case 3:
				_003C_003E1__state = -1;
				break;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<FrameworkElement> IEnumerable<FrameworkElement>.GetEnumerator()
		{
			_003CFromJobRecord_003Ed__41 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CFromJobRecord_003Ed__41(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
		}
	}

	private const int PageSize = 40;

	private readonly INotchHost host;

	private readonly string agentKey;

	private readonly MessagesPalette look = MessagesPalette.Current;

	private readonly StackPanel list = new StackPanel
	{
		Spacing = 10.0,
		Padding = new Thickness(24.0, 18.0, 24.0, 0.0),
		MaxWidth = 728.0
	};

	private readonly ScrollViewer scroller;

	private readonly HeyClickyWindowComposerBacking backing = new HeyClickyWindowComposerBacking();

	private readonly NotchChatComposer composer;

	private readonly Pressable jumpDisc;

	private string? composerEntryId;

	private int shownCount = 40;

	private bool isAwayFromBottom;

	private readonly Dictionary<string, (FrameworkElement View, string Look)> built = new Dictionary<string, (FrameworkElement, string)>();

	private FrameworkElement? earlierButton;

	private FrameworkElement? liveCard;

	private FrameworkElement? emptyLine;

	private List<FrameworkElement>? recordViews;

	public string AgentKey => agentKey;

	public bool IsGone { get; private set; }

	public HeyClickyWindowConversation(INotchHost host, string agentKey)
	{
		this.host = host;
		this.agentKey = agentKey;
		scroller = new ScrollViewer
		{
			Content = new Grid
			{
				Children = { (UIElement)list }
			},
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		list.HorizontalAlignment = HorizontalAlignment.Stretch;
		scroller.ViewChanged += delegate
		{
			OnScrolled();
		};
		list.SizeChanged += delegate
		{
			if (!isAwayFromBottom && base.IsLoaded)
			{
				ScrollToNewest();
			}
		};
		base.Children.Add(scroller);
		base.Children.Add(backing);
		composerEntryId = Entries().LastOrDefault()?.Id;
		composer = new NotchChatComposer(host, composerEntryId, isInWindow: true, HeyClickyWindowRules.AgentComposerPlaceholder(AgentName()), StandInEntry(agentKey));
		composer.ExtraHeightChanged += delegate
		{
			LayoutComposerBand();
		};
		base.Children.Add(composer);
		jumpDisc = NotchChatView.JumpDisc(look, delegate
		{
			NotchPeekSurface.PeekAction("window_chat_scroll_to_bottom");
			scroller.ChangeView(null, scroller.ScrollableHeight, null, disableAnimation: false);
		});
		jumpDisc.Scale = new Vector3(0.8f, 0.8f, 1f);
		base.Children.Add(jumpDisc);
		LayoutComposerBand();
		Refresh();
		base.Loaded += delegate
		{
			ScrollToNewest();
		};
	}

	public void SetStopAction(Action? stop)
	{
		composer.SetStopAction(stop);
	}

	public void SetDropTargeting(bool on)
	{
		composer.SetDropTargeting(on);
	}

	public void AttachDroppedFiles(IReadOnlyList<string> paths)
	{
		if (composer.AddAttachments(paths) > 0)
		{
			composer.FocusField();
		}
	}

	private void LayoutComposerBand()
	{
		backing.Layout(composer.ExtraHeight);
		Thickness padding = list.Padding;
		double num = HeyClickyWindowRules.BottomRoom(64.0, composer.ExtraHeight, isInWindow: true);
		list.Padding = new Thickness(padding.Left, padding.Top, padding.Right, num);
		jumpDisc.Margin = new Thickness(0.0, 0.0, 18.0, num + 8.0);
		if (!isAwayFromBottom && base.IsLoaded)
		{
			ScrollToNewest();
		}
	}

	private void OnScrolled()
	{
		bool flag = scroller.ExtentHeight - scroller.VerticalOffset - scroller.ViewportHeight > 120.0;
		if (flag != isAwayFromBottom)
		{
			isAwayFromBottom = flag;
			jumpDisc.IsHitTestVisible = flag;
			Anim.Opacity(jumpDisc, flag ? 1 : 0, 0.18, Easing.EaseOut(Ui.Compositor));
			Anim.Scale(jumpDisc, flag ? 1f : 0.8f, 0.18, Easing.EaseOut(Ui.Compositor), new Vector3(17f, 17f, 0f));
		}
	}

	private string? AgentName()
	{
		if (!(host.Services.Agents is AgentService agentService))
		{
			return null;
		}
		string text = ClickyJob.JobIdFromRailKey(agentKey);
		if (text != null)
		{
			return agentService.Jobs.Job(text)?.DisplayName;
		}
		return agentService.FindClicky(agentKey)?.Name;
	}

	internal static ChatHandOff StandInEntry(string agentKey)
	{
		string text = ClickyJob.JobIdFromRailKey(agentKey);
		return new ChatHandOff
		{
			Id = "window-" + agentKey,
			Kind = ChatHandOffKind.Started,
			JobId = text,
			ClickySlug = ((text == null) ? agentKey : null),
			Title = "",
			CreatedAt = DateTimeOffset.UtcNow
		};
	}

	private IReadOnlyList<ChatHandOff> Entries()
	{
		string text = ClickyJob.JobIdFromRailKey(agentKey);
		string target = ((text != null) ? ("job:" + text) : ("clicky:" + agentKey));
		return NotchChat.HandOffs?.Entries.Where(delegate(ChatHandOff e)
		{
			bool flag = e.TargetKey == target;
			if (flag)
			{
				ChatHandOffKind kind = e.Kind;
				bool flag2 = (uint)kind <= 1u;
				flag = flag2;
			}
			return flag;
		}).ToList() ?? new List<ChatHandOff>();
	}

	public void Refresh()
	{
		using (MessagesPalette.Use(look))
		{
			if (!AgentExists())
			{
				ShowGone();
				return;
			}
			IReadOnlyList<ChatHandOff> readOnlyList = Entries();
			IChatLiveWork liveWork = NotchExtensions.LiveWork;
			List<ChatHandOff> list = readOnlyList.Skip(Math.Max(0, readOnlyList.Count - shownCount)).ToList();
			string text = readOnlyList.LastOrDefault()?.Id;
			List<FrameworkElement> list2 = new List<FrameworkElement>(list.Count + 4);
			if (readOnlyList.Count > list.Count)
			{
				list2.Add(earlierButton ?? (earlierButton = EarlierButton()));
			}
			if (readOnlyList.Count == 0)
			{
				list2.AddRange(recordViews ?? (recordViews = FromJobRecord().ToList()));
			}
			HashSet<string> keep = new HashSet<string>();
			bool flag = built.Count == 0;
			foreach (ChatHandOff item in list)
			{
				keep.Add(item.Id);
				bool flag2 = item.Id == text;
				string text2 = RowLook(item, flag2);
				bool flag3 = !built.ContainsKey(item.Id);
				if (!built.TryGetValue(item.Id, out (FrameworkElement, string) value) || value.Item2 != text2)
				{
					FrameworkElement frameworkElement = ((item.Kind == ChatHandOffKind.Started) ? Sent(item) : Reply(item, flag2, liveWork));
					if (!flag && flag3 && flag2)
					{
						FadeIn(frameworkElement);
					}
					value = (frameworkElement, text2);
					built[item.Id] = value;
				}
				list2.Add(value.Item1);
				if (flag2 && item.Kind == ChatHandOffKind.Started)
				{
					FrameworkElement frameworkElement2 = LiveCard(item, liveWork);
					if ((object)frameworkElement2 != null)
					{
						list2.Add(frameworkElement2);
					}
				}
			}
			foreach (string item2 in built.Keys.Where((string k) => !keep.Contains(k)).ToList())
			{
				built.Remove(item2);
			}
			if (readOnlyList.Count == 0)
			{
				FrameworkElement frameworkElement3 = LiveCard(StandInEntry(agentKey), liveWork);
				if ((object)frameworkElement3 != null)
				{
					list2.Add(frameworkElement3);
				}
				if (recordViews.Count == 0)
				{
					list2.Add(EmptyLine());
				}
			}
			ReconcileKeepingPlace(list2);
			SyncComposer(readOnlyList.LastOrDefault());
		}
	}

	private static string RowLook(ChatHandOff e, bool isNewest)
	{
		global::_003C_003Ey__InlineArray8<object> buffer = default(global::_003C_003Ey__InlineArray8<object>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 0) = e.Kind;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 1) = e.Outcome;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 2) = e.FullReply?.Length ?? e.ReplyText?.Length ?? 0;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 3) = e.ArtifactEntries?.Count ?? 0;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 4) = e.SentText?.Length ?? (-1);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 5) = e.SentFiles?.Count ?? 0;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 6) = e.TaskTitle ?? e.Title;
		ref object reference = ref global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray8<object>, object>(ref buffer, 7);
		int num;
		if (e.Kind != ChatHandOffKind.Started && isNewest)
		{
			ChatHandOffOutcome? outcome = e.Outcome;
			if (outcome.HasValue)
			{
				ChatHandOffOutcome valueOrDefault = outcome.GetValueOrDefault();
				num = ((valueOrDefault != ChatHandOffOutcome.Finished) ? 1 : 0);
				goto IL_0113;
			}
		}
		num = 0;
		goto IL_0113;
		IL_0113:
		reference = (byte)num != 0;
		return string.Join("\u001f", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray8<object>, object>(in buffer, 8));
	}

	private FrameworkElement? LiveCard(ChatHandOff entry, IChatLiveWork? live)
	{
		if ((object)liveCard == null)
		{
			FrameworkElement frameworkElement = live?.WorkCard(entry, hidesFinalizing: true);
			if ((object)frameworkElement != null)
			{
				liveCard = frameworkElement;
				frameworkElement.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, delegate
				{
					FollowLiveCard();
				});
			}
		}
		return liveCard;
	}

	private void FollowLiveCard()
	{
		if ((object)emptyLine != null)
		{
			FrameworkElement? frameworkElement = emptyLine;
			FrameworkElement frameworkElement2 = liveCard;
			frameworkElement.Visibility = (((object)frameworkElement2 != null && frameworkElement2.Visibility == Visibility.Visible && list.Children.Contains(liveCard)) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private FrameworkElement EmptyLine()
	{
		if ((object)emptyLine != null)
		{
			return emptyLine;
		}
		TextBlock textBlock = Ui.Text("Nothing here yet. Send it a message below.", 12.0, FontWeights.Normal, look.SecondaryText);
		textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		textBlock.Margin = new Thickness(0.0, 40.0, 0.0, 0.0);
		return emptyLine = textBlock;
	}

	private static void FadeIn(FrameworkElement view)
	{
		if (!Anim.ReduceMotion)
		{
			view.Opacity = 0.0;
			Anim.Opacity(view, 1f, 0.22, Easing.EaseOut(Ui.Compositor));
		}
	}

	private void ReconcileKeepingPlace(List<FrameworkElement> views)
	{
		HashSet<UIElement> hashSet = new HashSet<UIElement>(views);
		FrameworkElement frameworkElement = null;
		double num = 0.0;
		UIElement visual = (UIElement)scroller.Content;
		if (isAwayFromBottom && base.IsLoaded)
		{
			foreach (FrameworkElement item in list.Children.OfType<FrameworkElement>())
			{
				if (hashSet.Contains(item) && !(item.ActualHeight <= 0.0))
				{
					double y = item.TransformToVisual(visual).TransformPoint(default(Point)).Y;
					if (!(y + item.ActualHeight <= scroller.VerticalOffset))
					{
						frameworkElement = item;
						num = y;
						break;
					}
				}
			}
		}
		bool flag = false;
		UIElementCollection children = list.Children;
		for (int num2 = children.Count - 1; num2 >= 0; num2--)
		{
			if (!hashSet.Contains(children[num2]))
			{
				children.RemoveAt(num2);
				flag = true;
			}
		}
		for (int i = 0; i < views.Count; i++)
		{
			if (i >= children.Count || (object)children[i] != views[i])
			{
				int num3 = children.IndexOf(views[i]);
				if (num3 >= 0)
				{
					children.RemoveAt(num3);
				}
				children.Insert(i, views[i]);
				flag = true;
			}
		}
		FollowLiveCard();
		if (!flag || !base.IsLoaded)
		{
			return;
		}
		if (!isAwayFromBottom)
		{
			ScrollToNewest();
		}
		else if ((object)frameworkElement != null)
		{
			scroller.UpdateLayout();
			double num4 = frameworkElement.TransformToVisual(visual).TransformPoint(default(Point)).Y - num;
			if (Math.Abs(num4) > 0.5)
			{
				scroller.ChangeView(null, Math.Max(0.0, scroller.VerticalOffset + num4), null, disableAnimation: true);
			}
		}
	}

	private bool AgentExists()
	{
		if (!(host.Services.Agents is AgentService agentService))
		{
			return true;
		}
		string text = ClickyJob.JobIdFromRailKey(agentKey);
		if (text != null)
		{
			return (object)agentService.Jobs.Job(text) != null;
		}
		return (object)agentService.FindClicky(agentKey) != null;
	}

	private void ShowGone()
	{
		if (!IsGone)
		{
			IsGone = true;
			base.Children.Clear();
			TextBlock textBlock = Ui.Text("This chat is gone.", 13.0, FontWeights.Normal, look.SecondaryText);
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			base.Children.Add(textBlock);
		}
	}

	[IteratorStateMachine(typeof(_003CFromJobRecord_003Ed__41))]
	private IEnumerable<FrameworkElement> FromJobRecord()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CFromJobRecord_003Ed__41(-2)
		{
			_003C_003E4__this = this
		};
	}

	private FrameworkElement EarlierButton()
	{
		Pressable pressable = new Pressable();
		pressable.HorizontalAlignment = HorizontalAlignment.Center;
		pressable.Padding = new Thickness(12.0, 5.0, 12.0, 5.0);
		pressable.CornerRadius = new CornerRadius(12.0);
		pressable.Background = Ui.Brush(look.InkAt(0.07));
		pressable.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		pressable.Children.Add(Ui.Text("Earlier messages", 11.5, FontWeights.Medium, look.SecondaryText));
		pressable.Click += delegate
		{
			shownCount += 40;
			Refresh();
		};
		return pressable;
	}

	private FrameworkElement Sent(ChatHandOff entry)
	{
		if (entry.SentText == null && entry.SentFiles == null)
		{
			return SentBubble(entry.TaskTitle ?? entry.Title, entry.CreatedAt);
		}
		return SentBubble(entry.SentText, entry.CreatedAt, entry.SentFiles, Open);
		void Open(string path)
		{
			(host.Actions as INotchActions)?.OpenChatArtifact(path, null, null);
		}
	}

	private static FrameworkElement SentBubble(string? text, DateTimeOffset at, IReadOnlyList<string>? files = null, Action<string>? open = null)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			MaxWidth = 520.0
		};
		if (files != null && files.Count > 0 && open != null)
		{
			stackPanel.Children.Add(new ChatSentAttachments(files, delegate(string path)
			{
				NotchPeekSurface.PeekAction("window_sent_attachment_opened");
				open(path);
			})
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0.0, 0.0, 0.0, (text != null) ? 3 : 0)
			});
		}
		if (text != null)
		{
			FrameworkElement frameworkElement = ChatBubbles.Bubble(text, outgoing: true, tail: true);
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Right;
			stackPanel.Children.Add(frameworkElement);
		}
		TextBlock textBlock = Ui.Text(ChatBubbles.ShortTime(at), 10.5, FontWeights.Normal, MessagesPalette.Current.TertiaryText);
		textBlock.HorizontalAlignment = HorizontalAlignment.Right;
		textBlock.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		stackPanel.Children.Add(textBlock);
		return stackPanel;
	}

	private static FrameworkElement Markdown(string markdown)
	{
		return new MarkdownViewerInlineDocument(markdown);
	}

	private FrameworkElement Reply(ChatHandOff entry, bool isNewest, IChatLiveWork? live)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0
		};
		FrameworkElement frameworkElement = entry.Outcome.GetValueOrDefault() switch
		{
			ChatHandOffOutcome.Finished => ChatStamp.Create(ChatStamp.Status.Ready, "done", ChatStamp.Seed(entry.Id)), 
			ChatHandOffOutcome.Failed => ChatStamp.Create(ChatStamp.Status.NeedsYou, "oops", ChatStamp.Seed(entry.Id)), 
			ChatHandOffOutcome.Stopped => ChatStamp.Create(ChatStamp.Status.Paused, "stopped", ChatStamp.Seed(entry.Id)), 
			_ => ChatStamp.Create(ChatStamp.Status.NeedsYou, "you", ChatStamp.Seed(entry.Id)), 
		};
		TextBlock textBlock = Ui.Text(ChatBubbles.ShortTime(entry.CreatedAt), 11.0, FontWeights.Medium, look.TertiaryText);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Children.Add(Ui.H(8.0, frameworkElement, textBlock));
		string fullReply = entry.FullReply;
		if (fullReply != null && fullReply.Length > 0)
		{
			stackPanel.Children.Add(Markdown(ChatReplyPreview.FullReplyMarkdown(entry) ?? fullReply));
		}
		else
		{
			string replyText = entry.ReplyText;
			if (replyText != null && replyText.Length > 0)
			{
				stackPanel.Children.Add(ChatBubbles.Bubble(replyText, outgoing: false, tail: true));
			}
		}
		IReadOnlyList<string> artifactEntries = entry.ArtifactEntries;
		if (artifactEntries != null && artifactEntries.Count > 0)
		{
			stackPanel.Children.Add(live?.FileCards(artifactEntries, Open) ?? ChatHandOffRow.FilePills(artifactEntries, Open));
		}
		if (isNewest && (entry.Outcome ?? ChatHandOffOutcome.Finished) != 0)
		{
			FrameworkElement frameworkElement2 = live?.WorkCard(entry, hidesFinalizing: true);
			if ((object)frameworkElement2 != null)
			{
				stackPanel.Children.Add(frameworkElement2);
			}
		}
		return LeadingNineTenths(stackPanel);
		void Open(string artifact)
		{
			(host.Actions as INotchActions)?.OpenChatArtifact(artifact, entry.ThreadId, entry.ClickySlug);
		}
	}

	private static FrameworkElement LeadingNineTenths(FrameworkElement content)
	{
		return new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(9.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				}
			},
			Children = { (UIElement)content }
		};
	}

	private void SyncComposer(ChatHandOff? newest)
	{
		if ((object)newest != null && !(newest.Id == composerEntryId))
		{
			composerEntryId = newest.Id;
			composer.RetargetFollowUp(newest.Id);
		}
	}

	private void ScrollToNewest()
	{
		base.DispatcherQueue.TryEnqueue(delegate
		{
			scroller.UpdateLayout();
			scroller.ChangeView(null, scroller.ScrollableHeight, null, disableAnimation: true);
		});
	}
}
