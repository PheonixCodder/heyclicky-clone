using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.OneBrain;

public sealed class DrawnOnboardingNotchShowcase
{
	private const string Area = "drawn tour";

	public static Action? OpenNotchOnHome;

	public static Action? FoldNotch;

	public static Action<bool>? SetDockTourShowcase;

	private readonly List<string> sampleIntegrationIds = new List<string>();

	private int generation;

	private long holdOpenUntil;

	private bool hasPoppedApps;

	private long? openedAt;

	private const double MinimumOpenSeconds = 4.5;

	public static readonly string[] PreferredAppKeys = new string[4] { "gmail", "notion", "figma", "github" };

	public static DrawnOnboardingNotchShowcase Shared { get; } = new DrawnOnboardingNotchShowcase();

	public IReadOnlyList<string> SampleIntegrationIds => sampleIntegrationIds;

	public bool IsShowing { get; private set; }

	public bool ShowsDock { get; private set; }

	public event Action? Changed;

	private static void Track(string action, JsonObject? properties = null)
	{
		ClickyTourJobCards.TrackFlowAction(action, GoodMorningBrief.IsPlaying && !DrawnOnboardingTour.IsRunning, properties);
	}

	private DrawnOnboardingNotchShowcase()
	{
	}

	public void Show(AppHost host)
	{
		if (IsShowing)
		{
			return;
		}
		IsShowing = true;
		ShowsDock = true;
		hasPoppedApps = false;
		holdOpenUntil = Environment.TickCount64 + 4500;
		openedAt = Environment.TickCount64;
		int showGeneration = ++generation;
		Track("notch_shown");
		host.Real?.Canvas.SetBelowNotch(isBelow: true);
		SetDockTourShowcase?.Invoke(obj: true);
		this.Changed?.Invoke();
		Action openNotchOnHome = OpenNotchOnHome;
		if (openNotchOnHome != null)
		{
			openNotchOnHome();
		}
		else
		{
			Log.Info("drawn tour", "notch shown (no notch Home to open in this build)");
		}
		Sounds.Play("clicky-text-open", 0.3);
		Log.Info("drawn tour", "notch shown");
		After(host, 7.0, delegate
		{
			if (IsShowing && generation == showGeneration)
			{
				PopApps(host, fromBeat: false);
			}
		});
		After(host, 22.0, delegate
		{
			if (IsShowing && generation == showGeneration)
			{
				Close(host, "timeout");
			}
		});
	}

	public void PopApps(AppHost host, bool fromBeat = true)
	{
		if (!IsShowing)
		{
			Show(host);
		}
		if (hasPoppedApps)
		{
			return;
		}
		hasPoppedApps = true;
		int showGeneration = generation;
		IReadOnlyList<AgentIntegrationInfo> readOnlyList = SampleIntegrations(host.Proactive?.Integrations ?? Array.Empty<AgentIntegrationInfo>());
		Track("apps_popped", new JsonObject((JsonNodeOptions?)null)
		{
			["count"] = readOnlyList.Count,
			["from_beat"] = fromBeat
		});
		long val = Environment.TickCount64 + (long)((0.5 + 0.55 * (double)readOnlyList.Count + 2.5) * 1000.0);
		holdOpenUntil = Math.Max(holdOpenUntil, val);
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			AgentIntegrationInfo integration = readOnlyList[i];
			After(host, 0.5 + 0.55 * (double)i, delegate
			{
				if (IsShowing && generation == showGeneration)
				{
					sampleIntegrationIds.Add(integration.Id);
					this.Changed?.Invoke();
					Sounds.Play("skill-up", 0.3);
				}
			});
		}
	}

	public void Close(AppHost host, string reason = "tour_end")
	{
		if (!IsShowing)
		{
			return;
		}
		IsShowing = false;
		JsonObject obj = new JsonObject((JsonNodeOptions?)null) { ["reason"] = reason };
		long? num = openedAt;
		double seconds;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			seconds = (double)(Environment.TickCount64 - valueOrDefault) / 1000.0;
		}
		else
		{
			seconds = 0.0;
		}
		obj["seconds_open"] = ClickyAnalytics.RoundedSeconds(seconds);
		obj["apps_popped"] = hasPoppedApps;
		Track("notch_closed", obj);
		openedAt = null;
		int closeGeneration = ++generation;
		Action foldNotch = FoldNotch;
		if (foldNotch != null)
		{
			foldNotch();
		}
		else
		{
			host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.AgentPeek));
		}
		Log.Info("drawn tour", "notch closed");
		After(host, 0.7, delegate
		{
			if (generation == closeGeneration)
			{
				host.Real?.Canvas.SetBelowNotch(isBelow: false);
				SetDockTourShowcase?.Invoke(obj: false);
				ShowsDock = false;
				sampleIntegrationIds.Clear();
				this.Changed?.Invoke();
			}
		});
	}

	public void FadeDrawingAway(AppHost host)
	{
		if (!IsShowing)
		{
			return;
		}
		double seconds = Math.Max(0.0, (double)(holdOpenUntil - Environment.TickCount64) / 1000.0);
		int showGeneration = generation;
		After(host, seconds, delegate
		{
			if (generation == showGeneration)
			{
				Close(host, "fade_away");
			}
		});
	}

	public void Reset(AppHost host)
	{
		Close(host, "reset");
	}

	public static IReadOnlyList<AgentIntegrationInfo> SampleIntegrations(IReadOnlyList<AgentIntegrationInfo> catalog)
	{
		List<AgentIntegrationInfo> sample = new List<AgentIntegrationInfo>();
		string[] preferredAppKeys = PreferredAppKeys;
		foreach (string key2 in preferredAppKeys)
		{
			AgentIntegrationInfo agentIntegrationInfo = catalog.FirstOrDefault((AgentIntegrationInfo i) => Matches(i, key2) && sample.All((AgentIntegrationInfo s) => s.Id != i.Id));
			if ((object)agentIntegrationInfo != null)
			{
				sample.Add(agentIntegrationInfo);
			}
		}
		foreach (AgentIntegrationInfo integration2 in catalog)
		{
			if (sample.Count >= 4)
			{
				break;
			}
			if (sample.All((AgentIntegrationInfo s) => s.Id != integration2.Id))
			{
				sample.Add(integration2);
			}
		}
		return sample;
		static bool Matches(AgentIntegrationInfo integration, string key)
		{
			global::_003C_003Ey__InlineArray3<string> buffer = default(global::_003C_003Ey__InlineArray3<string>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 0) = integration.Id;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 1) = integration.PrimaryToolkitSlug;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 2) = integration.DisplayName;
			return string.Join(" ", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<string>, string>(in buffer, 3)).ToLowerInvariant().Replace(" ", "")
				.Contains(key);
		}
	}

	private static void After(AppHost host, double seconds, Action action)
	{
		DispatcherQueueTimer t = host.DispatcherQueue.CreateTimer().Rooted();
		t.Interval = TimeSpan.FromSeconds(Math.Max(0.001, seconds));
		t.IsRepeating = false;
		t.Tick += delegate
		{
			t.Stop();
			action();
		};
		t.Start();
	}
}
