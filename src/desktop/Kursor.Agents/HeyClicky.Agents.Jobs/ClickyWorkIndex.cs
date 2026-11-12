using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Jobs;

public static class ClickyWorkIndex
{
	public sealed record ReportRun(string Heading, string Ask, string FirstLine);

	public const string ReportFileName = "report.md";

	public const int DefaultLimit = 5;

	public const int MaximumLimit = 10;

	private static readonly HashSet<string> StopWords = new HashSet<string>
	{
		"the", "an", "and", "or", "of", "to", "for", "in", "on", "at",
		"by", "about", "with", "from", "my", "me", "it", "that", "this", "what",
		"which", "did", "do", "you", "your", "was", "were", "is", "job", "jobs",
		"work", "thing"
	};

	private static readonly Regex RunHeadingPattern = new Regex("^## [0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2} · ", RegexOptions.CultureInvariant);

	private static readonly Regex ReplyHeadingPattern = new Regex("^#{1,4} ", RegexOptions.CultureInvariant);

	public static readonly string OwnerFileRelativePath = Path.Combine(".clicky", "owner");

	public static IReadOnlyList<ClickyWorkEntry> Search(IReadOnlyList<ClickyWorkEntry> entries, string query, int limit)
	{
		limit = Math.Max(1, Math.Min(10, limit));
		List<string> list = (from t in Tokens(query)
			where !StopWords.Contains(t)
			select t).ToList();
		if (list.Count == 0)
		{
			return entries.OrderByDescending((ClickyWorkEntry e) => e.LastActive).Take(limit).ToList();
		}
		List<(ClickyWorkEntry Entry, int Score)> list2 = new List<(ClickyWorkEntry Entry, int Score)>();
		foreach (ClickyWorkEntry entry in entries)
		{
			(IReadOnlyList<string>, int)[] array = new(IReadOnlyList<string>, int)[4]
			{
				(Tokens(entry.Title), 4),
				(Tokens(string.Join(" ", entry.Asks)), 2),
				(Tokens(entry.Result ?? ""), 2),
				(Tokens(entry.ReportText ?? ""), 1)
			};
			int num = 0;
			foreach (string queryToken in list)
			{
				(IReadOnlyList<string>, int)[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					(IReadOnlyList<string>, int) tuple = array2[i];
					if (tuple.Item1.Any((string word) => Matches(word, queryToken)))
					{
						num += tuple.Item2;
					}
				}
			}
			if (num > 0)
			{
				list2.Add((entry, num));
			}
		}
		return (from s in (from s in list2
				orderby s.Score descending, s.Entry.LastActive descending
				select s).Take(limit)
			select s.Entry).ToList();
	}

	private static bool Matches(string word, string queryToken)
	{
		if (!(word == queryToken))
		{
			if (queryToken.Length >= 3)
			{
				return word.StartsWith(queryToken, StringComparison.Ordinal);
			}
			return false;
		}
		return true;
	}

	public static IReadOnlyList<string> Tokens(string text)
	{
		string text2 = new string((from c in text.Normalize(NormalizationForm.FormD)
			where CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
			select c).ToArray()).ToLowerInvariant();
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		string text3 = text2;
		foreach (char c2 in text3)
		{
			if (char.IsLetterOrDigit(c2))
			{
				stringBuilder.Append(c2);
			}
			else if (stringBuilder.Length > 0)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list.Where((string w) => w.Length >= 2).ToList();
	}

	public static string DateText(DateTimeOffset date)
	{
		DateTime dateTime;
		try
		{
			dateTime = ((date.Year <= 1) ? date.DateTime : date.LocalDateTime);
		}
		catch (ArgumentOutOfRangeException)
		{
			dateTime = date.DateTime;
		}
		return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
	}

	public static JsonObject ResultObject(ClickyWorkEntry entry, bool forHelper, bool isCallingJob = false)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["job_id"] = entry.Id,
			["title"] = entry.Title,
			["last_active"] = DateText(entry.LastActive),
			["state"] = entry.State
		};
		if (entry.Asks.Count > 0)
		{
			jsonObject["asked"] = OneLine(entry.Asks[0], 300);
		}
		if (entry.Asks.Count > 1)
		{
			IReadOnlyList<string> asks = entry.Asks;
			jsonObject["latest_ask"] = OneLine(asks[asks.Count - 1], 300);
		}
		if (entry.Asks.Count > 1)
		{
			jsonObject["runs"] = entry.Asks.Count;
		}
		string result = entry.Result;
		if (result != null && result.Length > 0)
		{
			jsonObject["result"] = OneLine(result, 300);
		}
		if (entry.IsEarlierClicky)
		{
			jsonObject["kind"] = "a Clicky from before jobs";
		}
		if (forHelper)
		{
			string folderPath = entry.FolderPath;
			if (folderPath != null)
			{
				jsonObject["folder"] = folderPath;
			}
		}
		if (isCallingJob)
		{
			jsonObject["this_job"] = true;
		}
		return jsonObject;
	}

	public static string OneLine(string text, int limit)
	{
		string text2 = string.Join(" ", from l in text.Split(['\r', '\n'])
			where l.Length > 0
			select l).Trim();
		if (text2.Length > limit)
		{
			return text2.Substring(0, limit - 1).TrimEnd() + "…";
		}
		return text2;
	}

	public static string ReportHeader(string title, string jobId)
	{
		return $"# {title}\n\nHeyClicky's record of this job (job_id {jobId}): what each run was asked and what it reported, oldest first.\n";
	}

	public static string ReportOutcomeText(ClickyJobOutcome outcome)
	{
		return outcome switch
		{
			ClickyJobOutcome.Done => "done", 
			ClickyJobOutcome.Failed => "failed", 
			ClickyJobOutcome.Stopped => "stopped by the user", 
			_ => "working", 
		};
	}

	public static string ReportSection(DateTimeOffset finishedAt, string outcome, string ask, string reply, string? summary, IReadOnlyList<string> files, string folderPath)
	{
		List<string> list = new List<string>
		{
			"## " + DateText(finishedAt) + " · " + outcome,
			"",
			"Asked: " + OneLine(ask, 2000),
			""
		};
		string text = reply.Trim();
		if (text.Length > 0)
		{
			list.Add(HeadingsBelowRuns(text));
		}
		else if (summary != null && summary.Length > 0)
		{
			list.Add(summary);
		}
		else
		{
			list.Add("(no report)");
		}
		if (files.Count > 0)
		{
			string text2 = folderPath.TrimEnd('\\', '/');
			list.Add("");
			list.Add("Files:");
			foreach (string file in files)
			{
				string text3 = file;
				string[] array = new string[2]
				{
					text2 + "\\",
					text2 + "/"
				};
				foreach (string text4 in array)
				{
					if (file.StartsWith(text4, StringComparison.OrdinalIgnoreCase))
					{
						string text5 = file;
						int length = text4.Length;
						text3 = text5.Substring(length, text5.Length - length);
						break;
					}
				}
				list.Add("- " + text3);
			}
		}
		return string.Join("\n", list) + "\n";
	}

	public static bool IsRunHeading(string line)
	{
		return RunHeadingPattern.IsMatch(line);
	}

	public static string HeadingsBelowRuns(string reply)
	{
		bool inCode = false;
		return string.Join("\n", reply.Split('\n').Select(delegate(string line)
		{
			string text = line.Trim(' ', '\t');
			if (text.StartsWith("```", StringComparison.Ordinal) || text.StartsWith("~~~", StringComparison.Ordinal))
			{
				inCode = !inCode;
				return line;
			}
			return (inCode || !line.StartsWith('#') || !ReplyHeadingPattern.IsMatch(line)) ? line : ("##" + line);
		}));
	}

	public static (string? Title, IReadOnlyList<ReportRun> Runs) ParseReport(string text)
	{
		string text2 = null;
		List<ReportRun> list = new List<ReportRun>();
		string[] array = text.Split('\n');
		foreach (string text3 in array)
		{
			if (text2 == null && text3.StartsWith("# ", StringComparison.Ordinal))
			{
				string text4 = text3;
				text2 = text4.Substring(2, text4.Length - 2).Trim(' ', '\t');
			}
			else if (IsRunHeading(text3))
			{
				string text4 = text3;
				list.Add(new ReportRun(text4.Substring(3, text4.Length - 3).Trim(' ', '\t'), "", ""));
			}
			else if (list.Count > 0)
			{
				string text5 = text3.Trim(' ', '\t');
				ReportRun reportRun = list[list.Count - 1];
				if (reportRun.Ask.Length == 0 && text5.StartsWith("Asked: ", StringComparison.Ordinal))
				{
					int index = list.Count - 1;
					string text4 = text5;
					list[index] = reportRun with
					{
						Ask = text4.Substring(7, text4.Length - 7)
					};
				}
				else if (reportRun.Ask.Length > 0 && reportRun.FirstLine.Length == 0 && text5.Length > 0)
				{
					list[list.Count - 1] = reportRun with
					{
						FirstLine = text5
					};
				}
			}
		}
		return (Title: text2, Runs: list);
	}

	public static string TrimmedReport(string text, int characterLimit)
	{
		if (text.Length <= characterLimit)
		{
			return text;
		}
		List<string> list = new List<string>();
		List<List<string>> list2 = new List<List<string>>();
		string[] array = text.Split('\n');
		foreach (string text2 in array)
		{
			if (IsRunHeading(text2))
			{
				int num = 1;
				List<string> list3 = new List<string>(num);
				CollectionsMarshal.SetCount(list3, num);
				Span<string> span = CollectionsMarshal.AsSpan(list3);
				int index = 0;
				span[index] = text2;
				list2.Add(list3);
			}
			else if (list2.Count == 0)
			{
				list.Add(text2);
			}
			else
			{
				list2[list2.Count - 1].Add(text2);
			}
		}
		string text3 = string.Join("\n", list);
		List<string> list4 = list2.Select((List<string> r) => string.Join("\n", r)).ToList();
		List<string> list5 = new List<string>();
		int num2 = text3.Length;
		int num3 = list4.Count - 1;
		while (num3 >= 0 && num2 + list4[num3].Length + 1 <= characterLimit)
		{
			list5.Insert(0, list4[num3]);
			num2 += list4[num3].Length + 1;
			num3--;
		}
		int num4 = list4.Count - list5.Count;
		string text4 = ((num4 > 0) ? $"\n({num4} older run{((num4 == 1) ? "" : "s")} left out)\n" : "");
		return string.Concat(text3, text4, string.Concat(list5.Select((string r) => "\n" + r)));
	}

	public static string JobIdFromFolderName(string folderName)
	{
		string text = folderName.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? folderName;
		if (text.Length != 10 || !text.All(delegate(char c)
		{
			switch (c)
			{
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
				return true;
			default:
				return false;
			}
		}))
		{
			return folderName;
		}
		return text;
	}

	public static string TitleFromFolderName(string folderName)
	{
		bool num = JobIdFromFolderName(folderName) != folderName;
		List<string> list = folderName.Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
		if (num && list.Count > 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		string text = string.Join(" ", list);
		if (text.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text[0]));
			string text2 = text;
			return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
		}
		return folderName;
	}

	public static string? OwnerStamp(string? accountId)
	{
		if (string.IsNullOrEmpty(accountId))
		{
			return null;
		}
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("clicky-work-owner:" + accountId)), 0, 12).ToLowerInvariant();
	}

	public static string? FolderOwnerStamp(string folderPath)
	{
		try
		{
			string path = Path.Combine(folderPath, OwnerFileRelativePath);
			return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static void StampFolder(string folderPath, string ownerStamp)
	{
		if (FolderOwnerStamp(folderPath) == ownerStamp)
		{
			return;
		}
		try
		{
			string path = Path.Combine(folderPath, OwnerFileRelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, ownerStamp, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "owner stamp not written: " + ex.Message);
		}
	}

	public static void RemoveReports(string ownerStamp, string jobsRootPath)
	{
		foreach (string item in SafeDirectories(jobsRootPath))
		{
			if (!Path.GetFileName(item).StartsWith('.') && !(FolderOwnerStamp(item) != ownerStamp))
			{
				try
				{
					File.Delete(Path.Combine(item, "report.md"));
				}
				catch (Exception)
				{
				}
				try
				{
					File.Delete(Path.Combine(item, OwnerFileRelativePath));
				}
				catch (Exception)
				{
				}
			}
		}
	}

	public static string StandardizedPath(string path)
	{
		try
		{
			return Path.GetFullPath(path).TrimEnd('\\', '/');
		}
		catch (Exception)
		{
			return path.TrimEnd('\\', '/');
		}
	}

	public static IReadOnlyList<ClickyWorkEntry> UnlistedFolderEntries(string jobsRootPath, IEnumerable<string> listedFolderPaths, string? ownerStamp)
	{
		if (ownerStamp == null)
		{
			return Array.Empty<ClickyWorkEntry>();
		}
		HashSet<string> hashSet = listedFolderPaths.Select(StandardizedPath).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<ClickyWorkEntry> list = new List<ClickyWorkEntry>();
		foreach (string item in SafeDirectories(jobsRootPath))
		{
			string fileName = Path.GetFileName(item);
			if (fileName.StartsWith('.') || hashSet.Contains(StandardizedPath(item)) || FolderOwnerStamp(item) != ownerStamp)
			{
				continue;
			}
			string path = Path.Combine(item, "report.md");
			string text = null;
			try
			{
				if (File.Exists(path))
				{
					text = File.ReadAllText(path);
				}
			}
			catch (Exception)
			{
			}
			DateTimeOffset lastActive;
			try
			{
				lastActive = ((text == null) ? Directory.GetLastWriteTime(item) : File.GetLastWriteTime(path));
			}
			catch (Exception)
			{
				lastActive = DateTimeOffset.MinValue;
			}
			(string, IReadOnlyList<ReportRun>) tuple = ((text == null) ? (null, Array.Empty<ReportRun>()) : ParseReport(text));
			ReportRun reportRun = tuple.Item2.LastOrDefault();
			list.Add(new ClickyWorkEntry
			{
				Id = JobIdFromFolderName(fileName),
				Title = (tuple.Item1 ?? TitleFromFolderName(fileName)),
				LastActive = lastActive,
				Asks = (from r in tuple.Item2
					select r.Ask into a
					where a.Length > 0
					select a).ToList(),
				Result = reportRun?.FirstLine,
				State = (((object)reportRun == null) ? "no record" : (reportRun.Heading.Split('·', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "").Trim()),
				FolderPath = item,
				ReportText = text
			});
		}
		return list;
	}

	public static IReadOnlyList<ClickyWorkEntry> WithReports(IReadOnlyList<ClickyWorkEntry> entries)
	{
		return entries.Select(delegate(ClickyWorkEntry entry)
		{
			if (entry.ReportText == null)
			{
				string folderPath = entry.FolderPath;
				if (folderPath != null)
				{
					try
					{
						string path = Path.Combine(folderPath, "report.md");
						return File.Exists(path) ? entry with
						{
							ReportText = File.ReadAllText(path)
						} : entry;
					}
					catch (Exception)
					{
						return entry;
					}
				}
			}
			return entry;
		}).ToList();
	}

	private static IEnumerable<string> SafeDirectories(string root)
	{
		try
		{
			return Directory.Exists(root) ? ((IEnumerable<string>)Directory.GetDirectories(root)) : ((IEnumerable<string>)Array.Empty<string>());
		}
		catch (Exception)
		{
			return Array.Empty<string>();
		}
	}
}
