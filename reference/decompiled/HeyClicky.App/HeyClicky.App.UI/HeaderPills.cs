using System;
using System.Text.Json.Nodes;
using HeyClicky.App.Billing;
using HeyClicky.App.Services;
using HeyClicky.App.Services.Updates;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Xaml.Automation;

namespace HeyClicky.App.UI;

public static class HeaderPills
{
	private static bool previewUpdateReady;

	public static Func<bool> HidesPromotionalButtons { get; set; } = () => false;

	public static bool PreviewUpdateReady
	{
		get
		{
			return previewUpdateReady;
		}
		set
		{
			if (previewUpdateReady != value)
			{
				previewUpdateReady = value;
				RaiseChanged();
			}
		}
	}

	public static event Action? Changed;

	public static void RaiseChanged()
	{
		HeaderPills.Changed?.Invoke();
	}

	public static bool ShowsUpdate(AppServices s)
	{
		if (!IsUpdateReady(s))
		{
			return IsUpdateDeferred(s);
		}
		return true;
	}

	private static bool IsUpdateReady(AppServices s)
	{
		if (!PreviewUpdateReady)
		{
			return s.Updates.State == UpdateState.ReadyToInstall;
		}
		return true;
	}

	private static bool IsUpdateDeferred(AppServices s)
	{
		if (s.Updates.State == UpdateState.Failed)
		{
			return s.Updates.AvailableVersion != null;
		}
		return false;
	}

	public static bool ShowsUpgrade(AppServices s)
	{
		if (!ShowsUpdate(s))
		{
			BillingUi? billing = s.Billing;
			if (billing != null && billing.State.ShouldOfferManualUpgrade)
			{
				return !HidesPromotionalButtons();
			}
		}
		return false;
	}

	public static GelButton UpdateButton(AppServices s, string surface, bool compact, bool iconOnly, Action? before = null)
	{
		bool isReady = IsUpdateReady(s);
		GelButton gelButton = new GelButton(iconOnly ? "" : "Update app", GelAppearance.Orange, compact ? GelButton.Sizing.Compact : GelButton.Sizing.Normal, 100.0, "\ue896", null, null);
		gelButton.Tip(isReady ? "Install the downloaded update and relaunch HeyClicky" : "Download and install the new version of HeyClicky");
		AutomationProperties.SetName(gelButton, "Update app");
		gelButton.Click += delegate
		{
			before?.Invoke();
			if (surface == "notch_peek")
			{
				PeekAction("update_app");
			}
			if (!isReady)
			{
				s.Updates.CheckNowAsync(surface);
			}
			else if (PreviewUpdateReady && s.Updates.State != UpdateState.ReadyToInstall)
			{
				Log.Info("updates", "update pill (" + surface + "): preview only, nothing to install");
			}
			else
			{
				s.Updates.InstallAndRelaunch(surface);
			}
		};
		return gelButton;
	}

	private static void PeekAction(string action)
	{
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = action });
	}

	public static GelButton UpgradeButton(AppServices s, PaywallSource source, bool compact, bool withIcon, Action? before = null)
	{
		GelButton gelButton = new GelButton("Upgrade", GelAppearance.PrimaryBlue, compact ? GelButton.Sizing.Compact : GelButton.Sizing.Normal, 100.0, withIcon ? "\uf4a5" : null, null, null);
		gelButton.Tip("See HeyClicky Pro and Max");
		gelButton.Click += delegate
		{
			before?.Invoke();
			Log.Info("billing", "upgrade pill: " + source.WireName());
			if (source == PaywallSource.NotchPeekUpgradeButton)
			{
				PeekAction("upgrade");
			}
			s.Billing?.ShowPaywall(source);
		};
		return gelButton;
	}
}
