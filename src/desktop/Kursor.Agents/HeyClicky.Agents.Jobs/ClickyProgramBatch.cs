using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyProgramBatch
{
	public sealed record SiteEntry(string Host, bool Done);

	public sealed record AppEntry
	{
		public required string Toolkit { get; init; }

		public string? Slug { get; init; }

		public required string Name { get; init; }

		public string? LogoUrl { get; init; }

		public string Kind { get; init; } = "read";

		public int Count { get; init; }

		public int Done { get; init; }

		[CompilerGenerated]
		[SetsRequiredMembers]
		private AppEntry(AppEntry original)
		{
			Toolkit = original.Toolkit;
			Slug = original.Slug;
			Name = original.Name;
			LogoUrl = original.LogoUrl;
			Kind = original.Kind;
			Count = original.Count;
			Done = original.Done;
		}

		public AppEntry()
		{
		}
	}

	public required string Id { get; init; }

	public int SearchCount { get; init; }

	public int SearchesDone { get; init; }

	public string? FirstQuery { get; init; }

	public IReadOnlyList<SiteEntry> Sites { get; init; } = Array.Empty<SiteEntry>();

	public int SiteCount { get; init; }

	public int SitesDone { get; init; }

	public IReadOnlyList<AppEntry> Apps { get; init; } = Array.Empty<AppEntry>();

	public (string Text, ClickyInkWorkStep.StepKind Glyph)? Summary
	{
		get
		{
			List<(int, int, string, ClickyInkWorkStep.StepKind)> list = new List<(int, int, string, ClickyInkWorkStep.StepKind)>();
			List<AppEntry> list2 = Apps.Where((AppEntry a) => a.Kind != "read").ToList();
			HashSet<string> writingApps = list2.Select((AppEntry a) => a.Toolkit).ToHashSet();
			List<string> list3 = UniqueNames(Apps.Where((AppEntry a) => a.Kind == "read" && !writingApps.Contains(a.Toolkit)));
			bool flag = new bool[4]
			{
				list2.Count == 0,
				SiteCount == 0,
				SearchCount == 0,
				list3.Count == 0
			}.Count((bool empty) => !empty) == 1;
			foreach (AppEntry item4 in list2)
			{
				list.Add((0, 2, WriteText(item4), ClickyInkWorkStep.StepKind.Apps));
			}
			if (SiteCount > 0)
			{
				string item = ((!flag || SiteCount != 1) ? ((SitesDone < SiteCount && SiteCount > 1) ? $"{SitesDone} of {SiteCount} sites" : $"{SiteCount} site{((SiteCount == 1) ? "" : "s")}") : (Sites.FirstOrDefault()?.Host ?? "a page"));
				list.Add((1, 1, item, ClickyInkWorkStep.StepKind.Read));
			}
			if (SearchCount > 0)
			{
				string item2 = ClickyLiveSteps.ReadableSearch(FirstQuery ?? "").Words;
				string item3 = ((!flag || SearchCount != 1) ? $"{SearchCount} search{((SearchCount == 1) ? "" : "es")}" : ((item2.Length == 0) ? "the web" : ClickyLiveSteps.Shortened(item2, 34).Replace("…", "")));
				list.Add((2, 0, item3, ClickyInkWorkStep.StepKind.Search));
			}
			if (list3.Count > 0)
			{
				list.Add((3, 3, (flag ? "" : "checking ") + Joined(list3), ClickyInkWorkStep.StepKind.Apps));
			}
			List<(int Priority, int Display, string Text, ClickyInkWorkStep.StepKind Glyph)> list4 = list.OrderBy(((int Priority, int Display, string Text, ClickyInkWorkStep.StepKind Glyph) p) => p.Priority).Take(2).ToList();
			if (list4.Count == 0)
			{
				return null;
			}
			return (string.Join(" · ", from p in list4
				orderby p.Display
				select p.Text), list4[0].Item4);
		}
	}

	public IReadOnlyList<ClickyLiveLineIcon> Icons
	{
		get
		{
			HashSet<string> seen = new HashSet<string>();
			return (from a in Apps
				where seen.Add(a.Toolkit)
				select ClickyLiveLineIcon.App(a.Toolkit, a.Name, a.LogoUrl)).Concat(Sites.Select((SiteEntry s) => ClickyLiveLineIcon.Site(s.Host))).ToList();
		}
	}

	public IReadOnlyList<string> SitesRead => (from s in Sites
		where s.Done
		select s.Host).ToList();

	public const int IconLimit = 4;

	public static ClickyProgramBatch? FromEvent(JsonObject e)
	{
		string text = Str(e["id"]);
		if (text == null || text.Length <= 0)
		{
			return null;
		}
		JsonObject jsonObject = e["searches"] as JsonObject;
		List<SiteEntry> list = ((e["sites"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().Select(delegate(JsonObject s)
		{
			string text2 = Str(s["host"]);
			return (text2 == null || text2.Length <= 0) ? null : new SiteEntry(text2, Bool(s["done"]) == true);
		}).OfType<SiteEntry>()
			.ToList();
		List<AppEntry> apps = ((e["apps"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().Select(delegate(JsonObject a)
		{
			string text3 = Str(a["toolkit"]);
			return (text3 == null || text3.Length <= 0) ? null : new AppEntry
			{
				Toolkit = text3,
				Slug = Str(a["slug"]),
				Name = text3,
				Kind = (Str(a["kind"]) ?? "read"),
				Count = Int(a["count"]).GetValueOrDefault(),
				Done = Int(a["done"]).GetValueOrDefault()
			};
		}).OfType<AppEntry>()
			.ToList();
		return new ClickyProgramBatch
		{
			Id = text,
			SearchCount = Int(jsonObject?["count"]).GetValueOrDefault(),
			SearchesDone = Int(jsonObject?["done"]).GetValueOrDefault(),
			FirstQuery = Str(jsonObject?["query"]),
			Sites = list,
			SiteCount = Math.Max(list.Count, Int(e["siteCount"]).GetValueOrDefault()),
			SitesDone = (Int(e["sitesDone"]) ?? list.Count((SiteEntry s) => s.Done)),
			Apps = apps
		};
		static bool? Bool(JsonNode? n)
		{
			if (!(n is JsonValue jsonValue2) || !jsonValue2.TryGetValue<bool>(out var value2))
			{
				return null;
			}
			return value2;
		}
		static int? Int(JsonNode? n)
		{
			if (!(n is JsonValue jsonValue3) || !jsonValue3.TryGetValue<int>(out var value3))
			{
				return null;
			}
			return value3;
		}
		static string? Str(JsonNode? n)
		{
			if (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
			{
				return null;
			}
			return value;
		}
	}

	public ClickyInkWorkStep? WorkStep(DateTimeOffset at)
	{
		if (SiteCount > 1)
		{
			return new ClickyInkWorkStep(ClickyInkWorkStep.StepKind.Read, $"{SiteCount} sites", at);
		}
		SiteEntry siteEntry = Sites.FirstOrDefault();
		if ((object)siteEntry != null && SiteCount == 1)
		{
			return new ClickyInkWorkStep(ClickyInkWorkStep.StepKind.Read, siteEntry.Host, at);
		}
		if (SearchCount > 0)
		{
			return new ClickyInkWorkStep(ClickyInkWorkStep.StepKind.Search, null, at);
		}
		AppEntry appEntry = Apps.FirstOrDefault();
		if ((object)appEntry != null)
		{
			return new ClickyInkWorkStep(ClickyInkWorkStep.StepKind.Apps, appEntry.Name, at);
		}
		return null;
	}

	private static string WriteText(AppEntry app)
	{
		bool flag = app.Done < app.Count;
		string text = app.Kind switch
		{
			"send" => flag ? "sending" : "sent", 
			"create" => flag ? "creating" : "created", 
			"update" => flag ? "updating" : "updated", 
			"delete" => flag ? "deleting" : "deleted", 
			_ => flag ? "working" : "worked", 
		};
		if (app.Count <= 1)
		{
			return text + " in " + app.Name;
		}
		return $"{text} {app.Count} in {app.Name}";
	}

	private static List<string> UniqueNames(IEnumerable<AppEntry> apps)
	{
		HashSet<string> hashSet = new HashSet<string>();
		return apps.Select((AppEntry a) => a.Name).Where(hashSet.Add).ToList();
	}

	private static string Joined(IReadOnlyList<string> names)
	{
		if (names.Count <= 1)
		{
			return names.FirstOrDefault() ?? "";
		}
		if (names.Count > 3)
		{
			return string.Join(", ", names.Take(2)) + $" and {names.Count - 2} more";
		}
		return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
	}

	public ClickyProgramBatch NamingApps(IReadOnlyList<(string DisplayName, string? LogoUrl, IReadOnlyList<(string Slug, string? Logo)> Toolkits)> integrations)
	{
		return this with
		{
			Apps = Apps.Select(delegate(AppEntry app)
			{
				string text = app.Toolkit.ToLowerInvariant();
				string text2 = app.Slug?.ToLowerInvariant() ?? text;
				(string, string, string, string)? tuple = null;
				foreach (var integration in integrations)
				{
					foreach (var item in integration.Toolkits)
					{
						string text3 = item.Slug.ToLowerInvariant();
						if ((!(text3 != text) || text2.StartsWith(text3 + "_", StringComparison.Ordinal)) && text3.Length > (tuple?.Item3.Length ?? 0))
						{
							tuple = (integration.DisplayName, integration.LogoUrl, text3, item.Logo);
						}
					}
				}
				string? text4;
				if (!tuple.HasValue)
				{
					string name;
					if (text.Length != 0)
					{
						char upper = char.ToUpperInvariant(text[0]);
						ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(in upper);
						text4 = text;
						name = string.Concat(readOnlySpan, text4.Substring(1, text4.Length - 1));
					}
					else
					{
						name = text;
					}
					return app with
					{
						Name = name
					};
				}
				(string, string?, string, string?) valueOrDefault = tuple.GetValueOrDefault();
				text4 = valueOrDefault.Item4;
				AppEntry obj = app with
				{
					Toolkit = valueOrDefault.Item3,
					Name = valueOrDefault.Item1,
					LogoUrl = ((text4 != null && text4.Length > 0) ? valueOrDefault.Item4 : valueOrDefault.Item2)
				};
				return obj;
			}).ToList()
		};
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyProgramBatch(ClickyProgramBatch original)
	{
		Id = original.Id;
		SearchCount = original.SearchCount;
		SearchesDone = original.SearchesDone;
		FirstQuery = original.FirstQuery;
		Sites = original.Sites;
		SiteCount = original.SiteCount;
		SitesDone = original.SitesDone;
		Apps = original.Apps;
	}

	public ClickyProgramBatch()
	{
	}
}
