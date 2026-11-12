using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Tools;

public sealed class WorkerTools
{
	private sealed record Track(string Uri, string? Name, string? Artists);

	public static readonly IReadOnlySet<string> Names = new HashSet<string> { "google_calendar_find_events", "google_calendar_create_event", "save_memory", "play_on_spotify" };

	private readonly WorkerPost post;

	private readonly Func<string> timeZone;

	private readonly Func<string, bool> openUri;

	private readonly Func<bool> spotifyAppInstalled;

	public WorkerTools(WorkerPost post, Func<string> timeZone, Func<string, bool> openUri, Func<bool> spotifyAppInstalled)
	{
		this.post = post;
		this.timeZone = timeZone;
		this.openUri = openUri;
		this.spotifyAppInstalled = spotifyAppInstalled;
	}

	public Task<string> RunAsync(string toolName, ToolArgs args, CancellationToken ct = default(CancellationToken))
	{
		return toolName switch
		{
			"google_calendar_find_events" => RouteAsync("/agent/realtime/google-calendar/events", WithTimeZone(args), ct), 
			"google_calendar_create_event" => RouteAsync("/agent/realtime/google-calendar/events/create", WithTimeZone(args), ct), 
			"save_memory" => RouteAsync("/me/memory/save", args.Raw, ct), 
			"play_on_spotify" => PlayOnSpotifyAsync(args, ct), 
			_ => Task.FromResult(ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["error"] = "not_implemented",
				["tool"] = toolName
			})), 
		};
	}

	private string WithTimeZone(ToolArgs args)
	{
		JsonObject jsonObject = (JsonObject)args.Json.DeepClone();
		if (jsonObject["timeZone"] == null)
		{
			jsonObject["timeZone"] = timeZone();
		}
		return jsonObject.ToJsonString();
	}

	private async Task<string> RouteAsync(string path, string body, CancellationToken ct)
	{
		(int, string)? tuple = await post(path, body, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (tuple.HasValue)
		{
			(int, string) valueOrDefault = tuple.GetValueOrDefault();
			var (num, _) = valueOrDefault;
			if ((num < 200 || num >= 300) ? true : false)
			{
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["error"] = "upstream",
					["status"] = valueOrDefault.Item1,
					["body"] = valueOrDefault.Item2
				});
			}
			return string.IsNullOrWhiteSpace(valueOrDefault.Item2) ? "{}" : valueOrDefault.Item2;
		}
		return "{\"error\":\"not_signed_in\"}";
	}

	private async Task<string> PlayOnSpotifyAsync(ToolArgs args, CancellationToken ct)
	{
		string query = SearchQuery(args);
		string text = args.Str("spotifyURI", "spotifyUri", "uri", "url");
		string text2 = ((text != null) ? NormalizedTrackUri(text) : null);
		Log.Info("Tools", "play_on_spotify query=" + (query ?? "-") + " uri=" + (text2 ?? "-"));
		Track track;
		if (text2 != null)
		{
			track = new Track(text2, null, null);
		}
		else
		{
			if (query == null)
			{
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = "invalid_request",
					["summary"] = "No song or artist to play. Ask the user what they want to hear on Spotify.",
					["toolName"] = "play_on_spotify",
					["result"] = new JsonObject((JsonNodeOptions?)null)
					{
						["played"] = false,
						["opened"] = false
					}
				});
			}
			var (track2, text3, text4) = await SearchAsync(query, ct).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)track2 == null)
			{
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = text3,
					["summary"] = text4,
					["toolName"] = "play_on_spotify",
					["result"] = new JsonObject((JsonNodeOptions?)null)
					{
						["played"] = false,
						["opened"] = false,
						["query"] = query
					}
				});
			}
			track = track2;
		}
		bool flag;
		string text5;
		if (spotifyAppInstalled())
		{
			flag = openUri(track.Uri);
			text5 = "url_scheme";
		}
		else
		{
			Func<string, bool> func = openUri;
			string uri = track.Uri;
			int length = "spotify:track:".Length;
			flag = func("https://open.spotify.com/track/" + uri.Substring(length, uri.Length - length));
			text5 = "web_player";
		}
		Log.Info("Tools", $"play_on_spotify {text5} opened={flag} uri={track.Uri}");
		if (!flag)
		{
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "failed",
				["summary"] = "Could not play or open the Spotify track.",
				["toolName"] = "play_on_spotify",
				["result"] = new JsonObject((JsonNodeOptions?)null)
				{
					["spotifyURI"] = track.Uri,
					["played"] = false,
					["opened"] = false
				}
			});
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["spotifyURI"] = track.Uri,
			["played"] = text5 == "url_scheme",
			["via"] = text5
		};
		string name = track.Name;
		if (name != null)
		{
			jsonObject["trackName"] = name;
		}
		string artists = track.Artists;
		if (artists != null)
		{
			jsonObject["artists"] = artists;
		}
		if (query != null)
		{
			jsonObject["query"] = query;
		}
		string text6 = ((track.Name == null) ? "Started Spotify playback." : ((track.Artists == null) ? ("Now playing " + track.Name + " on Spotify.") : $"Now playing {track.Name} by {track.Artists} on Spotify."));
		if (text5 == "web_player")
		{
			text6 = ((track.Name == null) ? "Opened the track" : ("Opened " + track.Name)) + " in Spotify's web player (the Spotify app isn't installed on this PC).";
		}
		return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "completed",
			["summary"] = text6,
			["toolName"] = "play_on_spotify",
			["result"] = jsonObject
		});
	}

	private async Task<(Track? Track, string Status, string Summary)> SearchAsync(string query, CancellationToken ct)
	{
		(int, string)? tuple;
		try
		{
			tuple = await post("/agent/realtime/spotify/search", new JsonObject((JsonNodeOptions?)null) { ["query"] = query }.ToJsonString(), ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			Log.Warn("Tools", "spotify search: " + ex.Message);
			return (Track: null, Status: "error", Summary: "Couldn't reach Spotify right now. Ask the user to try again in a moment.");
		}
		if (tuple.HasValue)
		{
			(int, string) valueOrDefault = tuple.GetValueOrDefault();
			var (num, _) = valueOrDefault;
			if ((num < 200 || num >= 300) ? true : false)
			{
				return (Track: null, Status: "error", Summary: "Couldn't search Spotify right now. Ask the user to try again in a moment.");
			}
			JsonObject jsonObject;
			try
			{
				jsonObject = JsonNode.Parse(valueOrDefault.Item2, null) as JsonObject;
			}
			catch (JsonException)
			{
				jsonObject = null;
			}
			string text = ((string?)jsonObject?["status"]) ?? "error";
			if (text == "ok" && jsonObject?["track"] is JsonObject jsonObject2)
			{
				string text2 = (string?)jsonObject2["uri"];
				if (text2 != null)
				{
					string text3 = NormalizedTrackUri(text2);
					if (text3 != null)
					{
						return (Track: new Track(text3, NonEmpty((string?)jsonObject2["name"]), NonEmpty((string?)jsonObject2["artists"])), Status: "ok", Summary: "");
					}
				}
			}
			return (Track: null, Status: text, Summary: NonEmpty((string?)jsonObject?["message"]) ?? "Couldn't find that on Spotify. Ask the user to try a different song or artist.");
		}
		return (Track: null, Status: "not_signed_in", Summary: "Tell the user they need to be signed in to HeyClicky to play music on Spotify.");
	}

	internal static string? SearchQuery(ToolArgs args)
	{
		string text = args.Str("query", "search", "searchQuery");
		if (text != null)
		{
			return Normalized(text);
		}
		List<string> list = (from p in new string[3]
			{
				args.Str("song", "track", "title"),
				args.Str("artist"),
				args.Str("album")
			}
			where p != null
			select Normalized(p) into p
			where p.Length > 0
			select p).ToList();
		if (list.Count != 0)
		{
			return string.Join(" ", list);
		}
		return null;
	}

	internal static string? NormalizedTrackUri(string raw)
	{
		string text = raw.Trim();
		if (!text.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string text2 = text;
		int length = "spotify:track:".Length;
		string text3 = text2.Substring(length, text2.Length - length);
		if (!SpotifyId().IsMatch(text3))
		{
			return null;
		}
		return "spotify:track:" + text3;
	}

	private static string Normalized(string text)
	{
		return string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static string? NonEmpty(string? text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return null;
	}

	// [GeneratedRegex("^[A-Za-z0-9]{22}$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex SpotifyId()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__SpotifyId_47.Instance;
	}
}
