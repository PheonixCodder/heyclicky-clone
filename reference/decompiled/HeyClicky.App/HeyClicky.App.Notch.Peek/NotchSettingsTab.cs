using System;
using HeyClicky.App.Settings;
using HeyClicky.Core.Notch;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchSettingsTab : Grid
{
	private readonly INotchHost host;

	private readonly NotchPresenter? presenter;

	private readonly SettingsContent content;

	private bool holdsKeyboard;

	private bool syncing;

	private bool listening;

	private int settleGeneration;

	private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(360L, 0L);

	private static (bool Pending, string? Toolkit) integrationCallback;

	public bool HoldsKeyboard => holdsKeyboard;

	public static SettingsContent? ShownContent { get; private set; }

	private static event Action? IntegrationCallbackArrived;

	public NotchSettingsTab(INotchHost host)
	{
		this.host = host;
		presenter = host.Services.Presenter as NotchPresenter;
		content = new SettingsContent(host.Services, host.Actions);
		content.UseNotchGrid(12.0, 12.0);
		content.Margin = new Thickness(0.0, 6.0, 12.0, 0.0);
		base.Children.Add(content);
		AddHandler(UIElement.PointerPressedEvent, (PointerEventHandler)delegate
		{
			TakeKeyboard();
		}, handledEventsToo: true);
		content.PageChanged += delegate(SettingsWindow.Page page)
		{
			if (!syncing && presenter != null)
			{
				NotchSettingsSection notchSettingsSection = ToSection(page);
				if (presenter.Tabs.SettingsSection != notchSettingsSection)
				{
					presenter.Tabs.SettingsSection = notchSettingsSection;
				}
			}
		};
		Attached();
	}

	public void Attached()
	{
		if (!listening)
		{
			listening = true;
			host.KeyboardChanged += OnKeyboardChanged;
		}
	}

	public void Shown()
	{
		ShownContent = content;
		if (presenter == null)
		{
			return;
		}
		NotchSettingsSection settingsSection = presenter.Tabs.SettingsSection;
		content.IsSurfaceSettled = false;
		int generation = ++settleGeneration;
		DispatcherQueueTimer dispatcherQueueTimer = base.DispatcherQueue.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = SettleDelay;
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (generation == settleGeneration)
			{
				content.IsSurfaceSettled = true;
			}
		};
		dispatcherQueueTimer.Start();
		syncing = true;
		content.Show(ToPage(settingsSection));
		syncing = false;
		NotchPeekSurface.PeekAction("settings_" + settingsSection.RawValue());
		if (host.Services.Auth.IsSignedIn)
		{
			content.RefreshReferralIfStale();
		}
		presenter.Tabs.Changed -= OnTabsChanged;
		presenter.Tabs.Changed += OnTabsChanged;
		IntegrationCallbackArrived -= DeliverIntegrationCallback;
		IntegrationCallbackArrived += DeliverIntegrationCallback;
		DeliverIntegrationCallback();
	}

	public static void NoteIntegrationCallback(string? toolkit)
	{
		integrationCallback = (Pending: true, Toolkit: toolkit);
		NotchSettingsTab.IntegrationCallbackArrived?.Invoke();
	}

	private void DeliverIntegrationCallback()
	{
		if (integrationCallback.Pending)
		{
			string item = integrationCallback.Toolkit;
			integrationCallback = default((bool, string));
			content.OnIntegrationCallback(item);
		}
	}

	public void Hidden()
	{
		content.Hidden();
		IntegrationCallbackArrived -= DeliverIntegrationCallback;
		if (presenter != null)
		{
			presenter.Tabs.Changed -= OnTabsChanged;
		}
		ReleaseKeyboard();
	}

	public void Detached()
	{
		if (ShownContent == content)
		{
			ShownContent = null;
		}
		Hidden();
		if (listening)
		{
			listening = false;
			host.KeyboardChanged -= OnKeyboardChanged;
		}
	}

	private void OnTabsChanged()
	{
		base.DispatcherQueue.TryEnqueue(delegate
		{
			if (presenter != null && presenter.Tabs.Selected == NotchTab.Settings)
			{
				SettingsWindow.Page page = ToPage(presenter.Tabs.SettingsSection);
				if (content.CurrentPage != page)
				{
					syncing = true;
					content.Show(page);
					syncing = false;
				}
			}
		});
	}

	private void TakeKeyboard()
	{
		if (!holdsKeyboard)
		{
			holdsKeyboard = true;
			presenter?.SetNotchAgentPeekHeldOpen(held: true);
			host.RequestKeyboard();
		}
	}

	private void ReleaseKeyboard()
	{
		if (holdsKeyboard)
		{
			holdsKeyboard = false;
			presenter?.SetNotchAgentPeekHeldOpen(held: false);
			host.ReleaseKeyboardIfUnheld();
		}
	}

	private void OnKeyboardChanged(bool hasKeyboard)
	{
		if (!hasKeyboard && holdsKeyboard)
		{
			holdsKeyboard = false;
			presenter?.SetNotchAgentPeekHeldOpen(held: false);
		}
	}

	public static SettingsWindow.Page ToPage(NotchSettingsSection section)
	{
		return Enum.Parse<SettingsWindow.Page>(section.ToString());
	}

	public static NotchSettingsSection ToSection(SettingsWindow.Page page)
	{
		return Enum.Parse<NotchSettingsSection>(page.ToString());
	}
}
