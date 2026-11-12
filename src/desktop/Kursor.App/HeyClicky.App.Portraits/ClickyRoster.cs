using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HeyClicky.Agents.Clickys;
using HeyClicky.App.Home;
using HeyClicky.Core.Telemetry;
using Windows.UI;

namespace HeyClicky.App.Portraits;

public sealed class ClickyRoster
{
	private sealed class Stored
	{
		public List<RosterClicky> Clickys { get; set; } = new List<RosterClicky>();

		public Dictionary<string, PortraitAppearance> Appearances { get; set; } = new Dictionary<string, PortraitAppearance>();

		public Dictionary<string, string> TaskOwners { get; set; } = new Dictionary<string, string>();

		public List<StarterTaskCard> StarterCards { get; set; } = new List<StarterTaskCard>();

		public string? GoalLine { get; set; }

		public bool? UserKnowsAgents { get; set; }

		public List<string>? UserCustomizedSlugs { get; set; }
	}

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private readonly string path;

	private readonly object gate = new object();

	private Stored stored;

	public static ClickyRoster Shared { get; private set; } = new ClickyRoster(DefaultPath());

	public Func<string?> AssignedPackId { get; set; } = () => (string?)null;

	public PortraitPack AssignedPack => PortraitData.PackFromId(AssignedPackId()) ?? PortraitPack.ClassicClouds;

	public IHomeSource? Source { get; private set; }

	public IReadOnlyList<RosterClicky> Clickys
	{
		get
		{
			IHomeSource source = Source;
			if (source == null)
			{
				lock (gate)
				{
					return stored.Clickys.ToList();
				}
			}
			IReadOnlyList<ClickyInfo> clickys = source.Clickys;
			lock (gate)
			{
				return clickys.OrderBy((ClickyInfo c) => c.CreatedAt).Select(delegate(ClickyInfo c)
				{
					RosterClicky rosterClicky = stored.Clickys.FirstOrDefault((RosterClicky x) => x.Slug == c.Slug);
					return ((object)rosterClicky == null) ? new RosterClicky(c.Slug, c.Name, c.RoleText, c.OneLiner, Array.Empty<string>(), Array.Empty<string>(), null, c.CreatedAt) : rosterClicky with
					{
						Name = c.Name,
						RoleText = c.RoleText,
						OneLiner = c.OneLiner
					};
				}).ToList();
			}
		}
	}

	public IReadOnlyList<StarterTaskCard> StarterCards
	{
		get
		{
			lock (gate)
			{
				return stored.StarterCards.ToList();
			}
		}
	}

	public string? GoalLine
	{
		get
		{
			lock (gate)
			{
				return stored.GoalLine;
			}
		}
	}

	public bool UserKnowsAgents
	{
		get
		{
			lock (gate)
			{
				return stored.UserKnowsAgents == true;
			}
		}
	}

	public static Func<bool> IsOnboardingEngaged { get; set; } = () => false;

	public event EventHandler? Changed;

	public static void UseDirectory(string directory)
	{
		Shared = new ClickyRoster(Path.Combine(directory, "clickys.json"));
	}

	private static string DefaultPath()
	{
		return AppPaths.Combine("clickys.json");
	}

	private ClickyRoster(string path)
	{
		this.path = path;
		try
		{
			stored = (File.Exists(path) ? (JsonSerializer.Deserialize<Stored>(File.ReadAllText(path), Json) ?? new Stored()) : new Stored());
		}
		catch (Exception ex)
		{
			Log.Warn("roster", "unreadable " + path + ": " + ex.Message);
			stored = new Stored();
		}
		if (MoveUncustomizedClickysToHomeClouds())
		{
			Save();
		}
	}

	private bool MoveUncustomizedClickysToHomeClouds()
	{
		lock (gate)
		{
			HashSet<string> hashSet = new HashSet<string>(stored.UserCustomizedSlugs ?? new List<string>());
			bool result = false;
			foreach (string item in stored.Appearances.Keys.Order<string>(StringComparer.Ordinal).ToList())
			{
				if (!hashSet.Contains(item) && !stored.Appearances[item].IsHomeCloud)
				{
					stored.Appearances[item] = PortraitAppearance.RandomHomeCloud(stored.Appearances.Values);
					result = true;
				}
			}
			return result;
		}
	}

	public void OutfitStarterCast(IEnumerable<string> castSlugs)
	{
		lock (gate)
		{
			HashSet<string> hashSet = new HashSet<string>(stored.UserCustomizedSlugs ?? new List<string>());
			foreach (string castSlug in castSlugs)
			{
				if (!hashSet.Contains(castSlug) && (!stored.Appearances.TryGetValue(castSlug, out PortraitAppearance value) || !value.IsHomeCloud))
				{
					stored.Appearances[castSlug] = PortraitAppearance.RandomHomeCloud(stored.Appearances.Values);
				}
			}
		}
		Save();
	}

	public IReadOnlyList<string> CloudColorsInUse()
	{
		return Clickys.Select((RosterClicky c) => AppearanceFor("clicky:" + c.Slug).HomeCloudColorName).OfType<string>().ToList();
	}

	private void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			string contents;
			lock (gate)
			{
				contents = JsonSerializer.Serialize(stored, Json);
			}
			File.WriteAllText(path, contents);
		}
		catch (Exception ex)
		{
			Log.Warn("roster", "save: " + ex.Message);
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Attach(IHomeSource source)
	{
		if (Source == source)
		{
			return;
		}
		Source = source;
		source.TasksChanged += delegate
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		};
		List<RosterClicky> list;
		lock (gate)
		{
			list = stored.Clickys.Where((RosterClicky c) => !source.KnowsClicky(c.Slug)).ToList();
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (RosterClicky item in list)
		{
			try
			{
				ClickyInfo clickyInfo = source.CreateClicky(item.Name, item.RoleText, item.OneLiner);
				if (clickyInfo.Slug != item.Slug)
				{
					MoveSlug(item.Slug, clickyInfo.Slug);
				}
				Log.Info("roster", "moved \"" + item.Name + "\" into the agents' roster as " + clickyInfo.Slug);
			}
			catch (Exception ex)
			{
				Log.Warn("roster", "couldn't move " + item.Slug + " to the agents: " + ex.Message);
			}
		}
		Save();
	}

	private void MoveSlug(string from, string to)
	{
		lock (gate)
		{
			int num = stored.Clickys.FindIndex((RosterClicky c) => c.Slug == from);
			if (num >= 0)
			{
				stored.Clickys[num] = stored.Clickys[num]with
				{
					Slug = to
				};
			}
			if (stored.Appearances.Remove(from, out PortraitAppearance value))
			{
				stored.Appearances[to] = value;
			}
			foreach (string item in (from kv in stored.TaskOwners
				where kv.Value == @from
				select kv.Key).ToList())
			{
				stored.TaskOwners[item] = to;
			}
			stored.StarterCards = stored.StarterCards.Select((StarterTaskCard card) => (!(card.ClickySlug == from)) ? card : card with
			{
				ClickySlug = to
			}).ToList();
		}
	}

	public RosterClicky? Clicky(string slug)
	{
		return Clickys.FirstOrDefault((RosterClicky c) => c.Slug == slug);
	}

	public void ForgetAll()
	{
		lock (gate)
		{
			stored.Clickys.Clear();
			stored.Appearances.Clear();
			stored.TaskOwners.Clear();
			stored.StarterCards.Clear();
			stored.GoalLine = null;
			stored.UserKnowsAgents = null;
		}
		Save();
	}

	public PortraitAppearance AppearanceFor(string seed)
	{
		string text = ResolveSeed(seed);
		lock (gate)
		{
			PortraitAppearance value;
			return stored.Appearances.TryGetValue(text, out value) ? value : PortraitAppearance.SeededHomeCloud(text);
		}
	}

	public string ResolveSeed(string seed)
	{
		if (seed.StartsWith("clicky:", StringComparison.Ordinal))
		{
			return seed.Substring(7, seed.Length - 7);
		}
		lock (gate)
		{
			if (stored.TaskOwners.TryGetValue(seed, out string value))
			{
				return value;
			}
		}
		return Source?.GetTaskDetail(seed)?.ClickySlug ?? seed;
	}

	public void SetSuggestedAsks(string slug, IReadOnlyList<string> asks)
	{
		List<string> suggestedAsks = (from a in asks
			select a.Trim() into a
			where a.Length > 0
			select a).DistinctBy((string a) => a.ToLowerInvariant()).Take(3).ToList();
		lock (gate)
		{
			int num = stored.Clickys.FindIndex((RosterClicky c) => c.Slug == slug);
			if (num >= 0)
			{
				stored.Clickys[num] = stored.Clickys[num]with
				{
					SuggestedAsks = suggestedAsks
				};
			}
			else
			{
				RosterClicky rosterClicky = Clicky(slug);
				if ((object)rosterClicky != null)
				{
					stored.Clickys.Add(rosterClicky with
					{
						SuggestedAsks = suggestedAsks
					});
				}
			}
		}
		Save();
	}

	public Color HairColorFor(string seed)
	{
		return AppearanceFor(seed).Hair;
	}

	public void SetAppearance(string slug, PortraitAppearance appearance)
	{
		lock (gate)
		{
			this.stored.Appearances[slug] = appearance;
			Stored stored = this.stored;
			if (stored.UserCustomizedSlugs == null)
			{
				List<string> list2 = (stored.UserCustomizedSlugs = new List<string>());
			}
			if (!this.stored.UserCustomizedSlugs.Contains(slug))
			{
				this.stored.UserCustomizedSlugs.Add(slug);
			}
		}
		Save();
	}

	public void Rename(string slug, string name)
	{
		lock (gate)
		{
			int num = stored.Clickys.FindIndex((RosterClicky c) => c.Slug == slug);
			if (num < 0 || stored.Clickys[num].Name == name)
			{
				return;
			}
			stored.Clickys[num] = stored.Clickys[num]with
			{
				Name = name
			};
		}
		Save();
	}

	public void BindTask(string taskId, string slug)
	{
		lock (gate)
		{
			stored.TaskOwners[taskId] = slug;
		}
		Save();
	}

	public PortraitAppearance MakeNewAppearance(IEnumerable<PortraitAppearance>? avoiding = null)
	{
		List<string> slugs = Clickys.Select((RosterClicky c) => c.Slug).ToList();
		List<PortraitAppearance> list;
		lock (gate)
		{
			slugs.AddRange(stored.Appearances.Keys.Where((string k) => !slugs.Contains(k)));
			list = slugs.Select((string s) => (!stored.Appearances.TryGetValue(s, out PortraitAppearance value)) ? PortraitAppearance.SeededHomeCloud(s) : value).ToList();
		}
		if (avoiding != null)
		{
			list.AddRange(avoiding);
		}
		return PortraitAppearance.RandomHomeCloud(list);
	}

	public RosterClicky CreateClicky(string name, string roleText, string oneLiner, IReadOnlyList<string> introMessages, IReadOnlyList<string> suggestedAsks, string? revealLine, PortraitAppearance? initialAppearance = null, string? source = "agent")
	{
		PortraitAppearance value = initialAppearance ?? MakeNewAppearance();
		ClickyInfo clickyInfo = Source?.CreateClicky(name, roleText, oneLiner);
		RosterClicky rosterClicky;
		lock (gate)
		{
			string slug = clickyInfo?.Slug;
			if (slug == null)
			{
				string value2 = (slug = Slugify(name));
				int num = 2;
				while (stored.Clickys.Any((RosterClicky c) => c.Slug == slug))
				{
					slug = $"{value2}-{num}";
					num++;
				}
			}
			rosterClicky = new RosterClicky(slug, clickyInfo?.Name ?? name, roleText, oneLiner, introMessages, suggestedAsks, revealLine, clickyInfo?.CreatedAt ?? DateTimeOffset.Now);
			stored.Clickys.RemoveAll((RosterClicky c) => c.Slug == slug);
			stored.Clickys.Add(rosterClicky);
			if (!stored.Appearances.ContainsKey(slug) || (object)clickyInfo == null || clickyInfo.CreatedAt > DateTimeOffset.Now.AddMinutes(-1.0))
			{
				stored.Appearances[slug] = value;
			}
		}
		Save();
		if (source != null)
		{
			TrackCreated(rosterClicky.Slug, source, suggestedAsks.Count);
		}
		return rosterClicky;
	}

	public void TrackCreated(string slug, string source, int firstAskCount = 0)
	{
		try
		{
			ClickyAnalytics.TrackProductEvent("clicky_created", slug, null, new JsonObject((JsonNodeOptions?)null)
			{
				["source"] = source,
				["is_onboarding"] = IsOnboardingEngaged(),
				["pack"] = AppearanceFor("clicky:" + slug).Pack.Id(),
				["roster_count"] = Clickys.Count,
				["first_ask_count"] = firstAskCount
			});
		}
		catch (Exception ex)
		{
			Log.Warn("roster", "analytics clicky_created: " + ex.Message);
		}
	}

	public void SetStarterPlan(IReadOnlyList<StarterTaskCard> cards, string? goalLine, bool userKnowsAgents)
	{
		lock (gate)
		{
			stored.StarterCards = cards.ToList();
			stored.GoalLine = goalLine;
			stored.UserKnowsAgents = userKnowsAgents;
		}
		Save();
	}

	public void RemoveStarterCard(StarterTaskCard card)
	{
		lock (gate)
		{
			stored.StarterCards.Remove(card);
		}
		Save();
	}

	private static string Slugify(string name)
	{
		char[] value = (from ch in name.Trim().ToLowerInvariant()
			select (!char.IsLetterOrDigit(ch)) ? '-' : ch).ToArray();
		string text = string.Join('-', new string(value).Split('-', StringSplitOptions.RemoveEmptyEntries));
		if (text.Length != 0)
		{
			return text;
		}
		return "clicky";
	}
}
