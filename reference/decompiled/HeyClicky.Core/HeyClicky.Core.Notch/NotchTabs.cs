using System;
using System.IO;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Notch;

public sealed class NotchTabs
{
	private sealed record RememberedDocument(string Path, string? ThreadId, NotchTab TabBefore, (string EntryId, NotchTab? ReturnTab)? ExpandedReplyBefore);

	private sealed record RememberedPlace(NotchTab Tab, string? ExpandedReplyEntryId, NotchTab? TabBeforeFullReply, RememberedDocument? Document, DateTimeOffset ClosedAt);

	private readonly NotchPresenter presenter;

	private readonly object gate;

	private NotchTab selected;

	private string? chatFocusEntryId;

	private string? expandedReplyEntryId;

	private double? fullReplyContentHeight;

	private NotchSettingsSection settingsSection;

	private string? documentPath;

	private string? documentThreadId;

	private NotchTab tabBeforeDocument;

	private (string EntryId, NotchTab? ReturnTab)? expandedReplyBeforeDocument;

	private int composerFocusRequestId;

	private bool hasPendingComposerFocusRequest;

	private NotchTab? pendingTab;

	private string? pendingExpandedReplyEntryId;

	private NotchTab? tabBeforeFullReply;

	private DateTimeOffset? fullReplyOpenedAt;

	public static readonly TimeSpan RememberedPlaceLifetime = TimeSpan.FromMinutes(15L);

	private RememberedPlace? rememberedPlace;

	private RememberedDocument? documentLeftOnClose;

	private bool forgetsReplyAndDocumentOnClose;

	public Func<string, ChatHandOff?>? EntryLookup { get; set; }

	public Func<string, bool>? OpensFullReplyInWindow { get; set; }

	public NotchTab Selected
	{
		get
		{
			lock (gate)
			{
				return selected;
			}
		}
	}

	public string? ChatFocusEntryId
	{
		get
		{
			lock (gate)
			{
				return chatFocusEntryId;
			}
		}
		set
		{
			Mutate(delegate
			{
				chatFocusEntryId = value;
			});
		}
	}

	public string? ExpandedReplyEntryId
	{
		get
		{
			lock (gate)
			{
				return expandedReplyEntryId;
			}
		}
		set
		{
			Mutate(delegate
			{
				SetExpandedReply(value);
			});
		}
	}

	public double? FullReplyContentHeight
	{
		get
		{
			lock (gate)
			{
				return fullReplyContentHeight;
			}
		}
		set
		{
			Mutate(delegate
			{
				fullReplyContentHeight = value;
			});
		}
	}

	public NotchSettingsSection SettingsSection
	{
		get
		{
			lock (gate)
			{
				return settingsSection;
			}
		}
		set
		{
			Mutate(delegate
			{
				settingsSection = value;
			});
		}
	}

	public string? DocumentPath
	{
		get
		{
			lock (gate)
			{
				return documentPath;
			}
		}
	}

	public string? DocumentThreadId
	{
		get
		{
			lock (gate)
			{
				return documentThreadId;
			}
		}
	}

	public NotchTab TabBeforeDocument
	{
		get
		{
			lock (gate)
			{
				return tabBeforeDocument;
			}
		}
	}

	public int ComposerFocusRequestId
	{
		get
		{
			lock (gate)
			{
				return composerFocusRequestId;
			}
		}
	}

	public bool HasPendingComposerFocusRequest
	{
		get
		{
			lock (gate)
			{
				return hasPendingComposerFocusRequest;
			}
		}
	}

	public bool IsShowingDocument
	{
		get
		{
			lock (gate)
			{
				return selected == NotchTab.Document && presenter.IsNotchDocumentOpen && presenter.IsShowingNotchAgentPeekUnlocked;
			}
		}
	}

	internal NotchTab SelectedCore => selected;

	public event Action? Changed;

	public event Action<NotchTab>? SelectionChanged;

	public event Action? ComposerFocusRequested;

	internal NotchTabs(NotchPresenter presenter, object gate)
	{
		this.presenter = presenter;
		this.gate = gate;
	}

	private void SetExpandedReply(string? value)
	{
		if (expandedReplyEntryId != value)
		{
			fullReplyContentHeight = null;
		}
		expandedReplyEntryId = value;
	}

	private void Mutate(Action change)
	{
		NotchTab notchTab;
		NotchTab notchTab2;
		lock (gate)
		{
			notchTab = selected;
			change();
			notchTab2 = selected;
		}
		this.Changed?.Invoke();
		if (notchTab != notchTab2)
		{
			this.SelectionChanged?.Invoke(notchTab2);
		}
	}

	private void SelectCore(NotchTab tab)
	{
		if (selected != tab)
		{
			selected = tab;
			if (tab != NotchTab.Chat)
			{
				SetExpandedReply(null);
				tabBeforeFullReply = null;
			}
			ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "tab_" + tab.RawValue() });
		}
	}

	public void Select(NotchTab tab)
	{
		Mutate(delegate
		{
			SelectCore(tab);
		});
	}

	public void Show(NotchTab tab, string? focusingEntryId = null)
	{
		Mutate(delegate
		{
			ShowCore(tab, focusingEntryId);
		});
	}

	private void ShowCore(NotchTab tab, string? focusingEntryId)
	{
		pendingTab = tab;
		pendingExpandedReplyEntryId = null;
		tabBeforeFullReply = null;
		chatFocusEntryId = focusingEntryId;
		SetExpandedReply(null);
		SelectCore(tab);
	}

	public void RequestComposerFocus()
	{
		lock (gate)
		{
			hasPendingComposerFocusRequest = true;
			composerFocusRequestId++;
		}
		this.ComposerFocusRequested?.Invoke();
		this.Changed?.Invoke();
	}

	public void ConsumeComposerFocusRequest()
	{
		lock (gate)
		{
			hasPendingComposerFocusRequest = false;
		}
	}

	public void ShowChatForTyping(bool isNotchOpen)
	{
		Mutate(delegate
		{
			if (isNotchOpen)
			{
				chatFocusEntryId = null;
				SetExpandedReply(null);
				tabBeforeFullReply = null;
				SelectCore(NotchTab.Chat);
			}
			else
			{
				ShowCore(NotchTab.Chat, null);
			}
		});
	}

	public void ShowSettings(NotchSettingsSection section)
	{
		Mutate(delegate
		{
			settingsSection = section;
			ShowCore(NotchTab.Settings, null);
		});
		presenter.OpenNotchAgentPeekOnRequest();
	}

	public void ShowDocument(string path, string? threadId)
	{
		Mutate(delegate
		{
			NotchTabs notchTabs;
			(string, NotchTab?)? obj;
			if (selected != NotchTab.Document)
			{
				bool isShowingNotchAgentPeekUnlocked = presenter.IsShowingNotchAgentPeekUnlocked;
				tabBeforeDocument = (isShowingNotchAgentPeekUnlocked ? selected : NotchTab.Home);
				notchTabs = this;
				if (isShowingNotchAgentPeekUnlocked && selected == NotchTab.Chat)
				{
					string text = expandedReplyEntryId;
					if (text != null)
					{
						obj = (text, tabBeforeFullReply);
						goto IL_0082;
					}
				}
				obj = null;
				goto IL_0082;
			}
			goto IL_0087;
			IL_0087:
			documentPath = path;
			documentThreadId = threadId;
			ShowCore(NotchTab.Document, null);
			return;
			IL_0082:
			notchTabs.expandedReplyBeforeDocument = obj;
			goto IL_0087;
		});
		presenter.OpenNotchDocument();
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "document_opened" });
	}

	public void CloseDocument()
	{
		Mutate(delegate
		{
			if (selected == NotchTab.Document)
			{
				SelectCore((tabBeforeDocument != NotchTab.Document) ? tabBeforeDocument : NotchTab.Home);
				if (selected == NotchTab.Chat)
				{
					(string, NotchTab?)? tuple = expandedReplyBeforeDocument;
					if (tuple.HasValue)
					{
						(string, NotchTab?) valueOrDefault = tuple.GetValueOrDefault();
						SetExpandedReply(valueOrDefault.Item1);
						tabBeforeFullReply = valueOrDefault.Item2;
					}
				}
			}
			expandedReplyBeforeDocument = null;
		});
		presenter.CloseNotchDocument();
	}

	public void DropDocumentIfShowing()
	{
		Mutate(DropDocumentIfShowingCore);
	}

	internal void DropDocumentIfShowingCore()
	{
		if (selected == NotchTab.Document)
		{
			string text = documentPath;
			if (text != null && !forgetsReplyAndDocumentOnClose)
			{
				documentLeftOnClose = new RememberedDocument(text, documentThreadId, tabBeforeDocument, expandedReplyBeforeDocument);
			}
		}
		expandedReplyBeforeDocument = null;
		if (selected == NotchTab.Document)
		{
			selected = ((tabBeforeDocument != NotchTab.Document) ? tabBeforeDocument : NotchTab.Home);
		}
	}

	public void ForgetReplyAndDocumentOnNextClose()
	{
		lock (gate)
		{
			forgetsReplyAndDocumentOnClose = true;
		}
	}

	internal void NoteNotchClosedCore()
	{
		if (forgetsReplyAndDocumentOnClose)
		{
			forgetsReplyAndDocumentOnClose = false;
			string text = expandedReplyEntryId;
			if (text != null)
			{
				TrackFullReplyClosed(text, NotchChatFullReplyCloseVia.ExpandedToWindow);
			}
			SetExpandedReply(null);
			tabBeforeFullReply = null;
			documentLeftOnClose = null;
			rememberedPlace = new RememberedPlace(NotchTab.Home, null, null, null, presenter.Now);
			pendingTab = null;
			pendingExpandedReplyEntryId = null;
			return;
		}
		object obj = documentLeftOnClose;
		if (obj == null)
		{
			if (selected == NotchTab.Document)
			{
				string text2 = documentPath;
				if (text2 != null)
				{
					obj = new RememberedDocument(text2, documentThreadId, tabBeforeDocument, expandedReplyBeforeDocument);
					goto IL_00a9;
				}
			}
			obj = null;
		}
		goto IL_00a9;
		IL_00a9:
		RememberedDocument rememberedDocument = (RememberedDocument)obj;
		documentLeftOnClose = null;
		rememberedPlace = new RememberedPlace(((object)rememberedDocument != null) ? NotchTab.Document : selected, (selected == NotchTab.Chat) ? expandedReplyEntryId : null, (selected == NotchTab.Chat) ? tabBeforeFullReply : ((NotchTab?)null), rememberedDocument, presenter.Now);
		pendingTab = null;
		pendingExpandedReplyEntryId = null;
	}

	public void ResetToHome()
	{
		Mutate(delegate
		{
			rememberedPlace = null;
			documentLeftOnClose = null;
			forgetsReplyAndDocumentOnClose = false;
			pendingTab = null;
			pendingExpandedReplyEntryId = null;
			chatFocusEntryId = null;
			SetExpandedReply(null);
			tabBeforeFullReply = null;
			expandedReplyBeforeDocument = null;
			settingsSection = NotchSettingsSection.Account;
			selected = NotchTab.Home;
		});
	}

	public void ShowFullReply(string entryId, NotchChatFullReplyOpenSource? from = null)
	{
		Func<string, bool>? opensFullReplyInWindow = OpensFullReplyInWindow;
		if (opensFullReplyInWindow == null || !opensFullReplyInWindow(entryId))
		{
			bool isShowingNotchAgentPeek = presenter.IsShowingNotchAgentPeek;
			TrackFullReplyOpened(entryId, (NotchChatFullReplyOpenSource)(((int?)from) ?? (isShowingNotchAgentPeek ? 1 : 2)));
			Mutate(delegate
			{
				bool isShowingNotchAgentPeekUnlocked = presenter.IsShowingNotchAgentPeekUnlocked;
				NotchTab? notchTab = ((!isShowingNotchAgentPeekUnlocked) ? new NotchTab?(NotchTab.Home) : ((selected == NotchTab.Chat) ? ((NotchTab?)null) : new NotchTab?((selected != NotchTab.Document) ? selected : NotchTab.Home)));
				pendingTab = (isShowingNotchAgentPeekUnlocked ? ((NotchTab?)null) : new NotchTab?(NotchTab.Chat));
				pendingExpandedReplyEntryId = (isShowingNotchAgentPeekUnlocked ? null : entryId);
				chatFocusEntryId = null;
				SelectCore(NotchTab.Chat);
				tabBeforeFullReply = notchTab;
				SetExpandedReply(entryId);
			});
			presenter.OpenNotchAgentPeekOnRequest();
		}
	}

	public void DropExpandedReply()
	{
		Mutate(delegate
		{
			SetExpandedReply(null);
			tabBeforeFullReply = null;
		});
	}

	public void CloseFullReply(NotchChatFullReplyCloseVia? via = NotchChatFullReplyCloseVia.Escape)
	{
		if (via.HasValue)
		{
			NotchChatFullReplyCloseVia valueOrDefault = via.GetValueOrDefault();
			string text = ExpandedReplyEntryId;
			if (text != null)
			{
				TrackFullReplyClosed(text, valueOrDefault);
			}
		}
		Mutate(delegate
		{
			NotchTab? notchTab = tabBeforeFullReply;
			tabBeforeFullReply = null;
			SetExpandedReply(null);
			if (notchTab.HasValue)
			{
				NotchTab valueOrDefault2 = notchTab.GetValueOrDefault();
				SelectCore(valueOrDefault2);
			}
		});
	}

	public void TrackFullReplyOpened(string entryId, NotchChatFullReplyOpenSource from)
	{
		ChatHandOff chatHandOff = EntryLookup?.Invoke(entryId);
		lock (gate)
		{
			fullReplyOpenedAt = presenter.Now;
		}
		ClickyAnalytics.TrackProductEvent("notch_chat_action", chatHandOff?.ClickySlug, chatHandOff?.ThreadId, NotchChatAnalytics.FullReplyOpened(chatHandOff, from));
	}

	private void TrackFullReplyClosed(string entryId, NotchChatFullReplyCloseVia via)
	{
		ChatHandOff chatHandOff = EntryLookup?.Invoke(entryId);
		TimeSpan? openFor;
		lock (gate)
		{
			DateTimeOffset? dateTimeOffset = fullReplyOpenedAt;
			TimeSpan? obj;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				obj = presenter.Now - valueOrDefault;
			}
			else
			{
				obj = null;
			}
			openFor = obj;
			fullReplyOpenedAt = null;
		}
		ClickyAnalytics.TrackProductEvent("notch_chat_action", chatHandOff?.ClickySlug, chatHandOff?.ThreadId, NotchChatAnalytics.FullReplyClosed(via, openFor));
	}

	internal void NoteFreshOpenCore(bool isHoverOpen = false)
	{
		forgetsReplyAndDocumentOnClose = false;
		if (!isHoverOpen)
		{
			NotchTab? notchTab = pendingTab;
			if (notchTab.HasValue)
			{
				NotchTab valueOrDefault = notchTab.GetValueOrDefault();
				pendingTab = null;
				if (valueOrDefault != NotchTab.Chat)
				{
					chatFocusEntryId = null;
				}
				SetExpandedReply(pendingExpandedReplyEntryId);
				pendingExpandedReplyEntryId = null;
				if (expandedReplyEntryId == null)
				{
					tabBeforeFullReply = null;
				}
				if (selected != valueOrDefault)
				{
					selected = valueOrDefault;
				}
				return;
			}
		}
		pendingTab = null;
		pendingExpandedReplyEntryId = null;
		chatFocusEntryId = null;
		RememberedPlace rememberedPlace = this.rememberedPlace;
		if ((object)rememberedPlace == null || presenter.Now - rememberedPlace.ClosedAt >= RememberedPlaceLifetime)
		{
			this.rememberedPlace = null;
			settingsSection = NotchSettingsSection.Account;
			SetExpandedReply(null);
			tabBeforeFullReply = null;
			if (selected != 0)
			{
				selected = NotchTab.Home;
			}
			return;
		}
		if (rememberedPlace.Tab == NotchTab.Document)
		{
			RememberedDocument document = rememberedPlace.Document;
			if ((object)document != null && File.Exists(document.Path))
			{
				documentPath = document.Path;
				documentThreadId = document.ThreadId;
				tabBeforeDocument = document.TabBefore;
				expandedReplyBeforeDocument = document.ExpandedReplyBefore;
				SetExpandedReply(null);
				tabBeforeFullReply = null;
				if (selected != NotchTab.Document)
				{
					selected = NotchTab.Document;
				}
				return;
			}
		}
		NotchTab notchTab2 = ((rememberedPlace.Tab != NotchTab.Document) ? rememberedPlace.Tab : (rememberedPlace.Document?.TabBefore ?? NotchTab.Home));
		NotchTab notchTab3 = ((notchTab2 != NotchTab.Document) ? notchTab2 : NotchTab.Home);
		if (notchTab3 == NotchTab.Chat)
		{
			string text = rememberedPlace.ExpandedReplyEntryId;
			if (text != null && (object)EntryLookup?.Invoke(text) != null)
			{
				SetExpandedReply(text);
				tabBeforeFullReply = rememberedPlace.TabBeforeFullReply;
				goto IL_01db;
			}
		}
		SetExpandedReply(null);
		tabBeforeFullReply = null;
		goto IL_01db;
		IL_01db:
		if (selected != notchTab3)
		{
			selected = notchTab3;
		}
	}

	internal void RaiseChangedAfterLadder(NotchTab before)
	{
		NotchTab notchTab;
		lock (gate)
		{
			notchTab = selected;
		}
		this.Changed?.Invoke();
		if (before != notchTab)
		{
			this.SelectionChanged?.Invoke(notchTab);
		}
	}

	internal void ConsumeComposerFocusRequestCore()
	{
		hasPendingComposerFocusRequest = false;
	}

	internal void SelectCoreInternal(NotchTab tab)
	{
		SelectCore(tab);
	}
}
