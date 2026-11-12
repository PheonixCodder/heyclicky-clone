using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkConnections
{
	public sealed record App(string Slug, string DisplayName, Uri? LogoUrl, bool IsConnected, bool NeedsReconnect);

	public abstract record Phase
	{
		public sealed record Opening : Phase;

		public sealed record WaitingForSignIn : Phase;

		public sealed record Checking : Phase;

		public sealed record Connected : Phase;

		public sealed record Failed(string Message) : Phase();
	}

	private Dictionary<string, App> apps = new Dictionary<string, App>();

	private readonly Dictionary<string, Phase> phases = new Dictionary<string, Phase>();

	private readonly Dictionary<string, Action> completions = new Dictionary<string, Action>();

	public static ClickyInkConnections Shared { get; } = new ClickyInkConnections();

	public IReadOnlyDictionary<string, App> Apps => apps;

	public IReadOnlyDictionary<string, Phase> Phases => phases;

	public Action<string>? Connect { get; set; }

	public event Action? Changed;

	public void SetApps(IEnumerable<App> list)
	{
		Dictionary<string, App> dictionary = new Dictionary<string, App>();
		foreach (App item in list)
		{
			dictionary[Normalized(item.Slug)] = item;
		}
		if (dictionary.Count != apps.Count || !dictionary.All((KeyValuePair<string, App> kv) => apps.TryGetValue(kv.Key, out App value) && value == kv.Value))
		{
			apps = dictionary;
			this.Changed?.Invoke();
		}
	}

	public App? AppNamed(string name)
	{
		string wanted = Normalized(name);
		if (wanted.Length == 0)
		{
			return null;
		}
		if (apps.TryGetValue(wanted, out App value))
		{
			return value;
		}
		return apps.Values.FirstOrDefault((App a) => Normalized(a.DisplayName) == wanted) ?? apps.Values.FirstOrDefault((App a) => Normalized(a.DisplayName).HasPrefix(wanted) || wanted.HasPrefix(Normalized(a.DisplayName)));
	}

	public Phase? PhaseFor(string slug)
	{
		if (!phases.TryGetValue(Normalized(slug), out Phase value))
		{
			return null;
		}
		return value;
	}

	public void StartConnecting(string slug, Action whenConnected)
	{
		string text = Normalized(slug);
		completions[text] = whenConnected;
		phases[text] = new Phase.Opening();
		this.Changed?.Invoke();
		Connect?.Invoke(text);
	}

	public void SetPhase(Phase? phase, string slug)
	{
		string text = Normalized(slug);
		Phase phase2 = PhaseFor(text);
		if ((object)phase == null)
		{
			phases.Remove(text);
		}
		else
		{
			phases[text] = phase;
		}
		this.Changed?.Invoke();
		if (phase is Phase.Connected && completions.Remove(text, out Action value))
		{
			value();
		}
		if (phase is Phase.Failed && completions.Remove(text))
		{
			ClickyAnalytics.TrackProductEvent("note_object_action", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "connect_failed",
				["object"] = ClickyInkDocument.Kind.Connect.RawValue(),
				["sends_to_agent"] = false,
				["stage"] = AnalyticsStage(phase2)
			});
		}
	}

	public static string AnalyticsStage(Phase? phase)
	{
		if (!(phase is Phase.Opening))
		{
			if (!(phase is Phase.WaitingForSignIn))
			{
				if (!(phase is Phase.Checking))
				{
					if (!(phase is Phase.Connected))
					{
						if (phase is Phase.Failed)
						{
							return "failed";
						}
						return "none";
					}
					return "connected";
				}
				return "checking";
			}
			return "waiting_for_sign_in";
		}
		return "opening";
	}

	public bool IsConnectingFromNote(string slug)
	{
		Phase phase = PhaseFor(slug);
		if ((object)phase != null)
		{
			return !(phase is Phase.Connected);
		}
		return false;
	}

	public static string Normalized(string text)
	{
		return string.Concat(from c in text.Lowered().Characters()
			where InkText.IsLetter(c) || InkText.IsNumber(c)
			select c);
	}
}
