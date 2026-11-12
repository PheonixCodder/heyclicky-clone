using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Tools;

public static class SpawnRepeatGuard
{
	private sealed class Claim(HashSet<string> words, DateTimeOffset at)
	{
		public HashSet<string> Words { get; } = words;

		public DateTimeOffset At { get; } = at;

		public string? StartedName { get; set; }
	}

	private static readonly object Gate = new object();

	private static readonly List<Claim> Claims = new List<Claim>();

	private static readonly TimeSpan Window = TimeSpan.FromSeconds(30L);

	private const double SameBriefOverlap = 0.6;

	public static string? RosterSlug(string? requestedSlug, Func<string, bool> isRosterSlug)
	{
		string text = requestedSlug?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (isRosterSlug(text))
		{
			return text;
		}
		string text2 = new string(text.ToLowerInvariant().Where(delegate(char c)
		{
			switch (c)
			{
			case '-':
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			default:
				return false;
			}
		}).ToArray()).Trim('-');
		if (text2.Length == 0 || !isRosterSlug(text2))
		{
			return null;
		}
		Log.Info("Tools", $"[spawn] read clicky_slug \"{text}\" as \"{text2}\"");
		return text2;
	}

	public static string? TryClaim(string task)
	{
		DateTimeOffset now = DateTimeOffset.Now;
		lock (Gate)
		{
			Claims.RemoveAll((Claim c) => now - c.At > Window);
			HashSet<string> words = SignificantWords(task);
			if (words.Count == 0)
			{
				return null;
			}
			Claim claim = Claims.FirstOrDefault((Claim c) => Overlap(c.Words, words) >= 0.6);
			if (claim != null)
			{
				Log.Info("Tools", $"[spawn] refused a repeat of work started {(int)(now - claim.At).TotalSeconds}s ago");
				string startedName = claim.StartedName;
				string text = ((startedName != null) ? (" (" + startedName + ")") : "");
				return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = "already_started",
					["message"] = "This same work was already started a moment ago" + text + ". Nothing new was started. Don't start it again; tell the user it's underway."
				});
			}
			Claims.Add(new Claim(words, now));
			return null;
		}
	}

	public static void Settle(string task, string resultJson)
	{
		HashSet<string> words = SignificantWords(task);
		lock (Gate)
		{
			int num = Claims.FindLastIndex((Claim c) => c.Words.SetEquals(words));
			if (num >= 0)
			{
				JsonObject jsonObject = null;
				try
				{
					jsonObject = JsonNode.Parse(resultJson, null) as JsonObject;
				}
				catch (JsonException)
				{
				}
				bool flag;
				switch (jsonObject?["status"]?.ToString() ?? "")
				{
				case "error":
				case "unavailable":
				case "blocked":
				case "paywall":
				case "already_started":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (flag)
				{
					Claims.RemoveAt(num);
				}
				else
				{
					Claims[num].StartedName = jsonObject?["clicky_name"]?.ToString() ?? jsonObject?["job_title"]?.ToString();
				}
			}
		}
	}

	private static HashSet<string> SignificantWords(string text)
	{
		HashSet<string> words = new HashSet<string>();
		StringBuilder current = new StringBuilder();
		string text2 = text.ToLowerInvariant();
		foreach (char c in text2)
		{
			if (char.IsLetterOrDigit(c))
			{
				current.Append(c);
			}
			else
			{
				Flush();
			}
		}
		Flush();
		return words;
		void Flush()
		{
			if (current.Length >= 3)
			{
				words.Add(current.ToString());
			}
			current.Clear();
		}
	}

	private static double Overlap(HashSet<string> first, HashSet<string> second)
	{
		int num = first.Union(second).Count();
		if (num != 0)
		{
			return (double)first.Intersect(second).Count() / (double)num;
		}
		return 0.0;
	}
}
